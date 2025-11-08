using Microsoft.Extensions.ML;
using Microsoft.ML;
using Microsoft.ML.Data;
using SistemaGestionActivos.Models;
using System;
using System.IO;
using System.Linq;

namespace SistemaGestionActivos.Services
{
    public interface ICategoryPredictionService
    {
        string? PredictCategory(string descripcion);
        PredictionServiceDiagnostics GetDiagnostics();
    }

    // Implementación que usa PredictionEnginePool si está disponible (inyección),
    // y cae al fallback (cargar Model.zip desde disco) si no.
    public class CategoryPredictionService : ICategoryPredictionService
    {
        private readonly MLContext _mlContext = new MLContext(seed: 0);
        private readonly dynamic? _poolDynamic;
        private readonly ITransformer? _fallbackModel;
    private readonly List<string> _checkedPaths = new List<string>();
    private string? _loadedModelPath;
        private readonly Microsoft.Extensions.Logging.ILogger<CategoryPredictionService> _logger;

        // Try to receive a strongly-typed PredictionEnginePool via DI if registered. This avoids brittle reflection.
        public CategoryPredictionService(IServiceProvider services, Microsoft.Extensions.Logging.ILogger<CategoryPredictionService> logger, Microsoft.Extensions.ML.PredictionEnginePool<TicketDataML, TicketPredictionML>? pool = null)
        {
            _logger = logger;

            if (pool != null)
            {
                _poolDynamic = pool;
                _logger.LogInformation("CategoryPredictionService: PredictionEnginePool injected via DI and will be used.");
            }
            else
            {
                // Best-effort: try to resolve via reflection if the strongly-typed generic wasn't injectable.
                try
                {
                    var poolTypeName = $"Microsoft.Extensions.ML.PredictionEnginePool`2[{typeof(TicketDataML).FullName},{typeof(TicketPredictionML).FullName}], Microsoft.Extensions.ML";
                    var poolType = Type.GetType(poolTypeName);
                    if (poolType != null)
                    {
                        var poolObj = services.GetService(poolType);
                        if (poolObj != null)
                        {
                            _poolDynamic = poolObj;
                            _logger.LogInformation("CategoryPredictionService: resolved PredictionEnginePool via reflection ({PoolType})", poolTypeName);
                        }
                        else
                        {
                            _logger.LogInformation("CategoryPredictionService: reflection found pool type but service returned null");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("CategoryPredictionService: PredictionEnginePool type not found via reflection");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "CategoryPredictionService: reflection attempt to resolve pool failed");
                }
            }

            if (_poolDynamic == null)
            {
                _fallbackModel = LoadFallbackModel();
                if (_fallbackModel == null)
                {
                    _logger.LogWarning("CategoryPredictionService: no se cargó ningún modelo de fallback (Model.zip no encontrado o/o inválido)");
                }
                else
                {
                    _logger.LogInformation("CategoryPredictionService: modelo de fallback cargado correctamente desde {Path}", _loadedModelPath);
                }
            }
        }

        private ITransformer? LoadFallbackModel()
        {
            var candidates = new[] {
                Path.Combine(AppContext.BaseDirectory, "wwwroot", "models", "Model.zip"),
                Path.Combine(AppContext.BaseDirectory, "Model.zip"),
                Path.Combine(AppContext.BaseDirectory, "MLModel", "Model.zip"),
                Path.Combine(AppContext.BaseDirectory, "..", "MLModel", "bin", "Debug", "net9.0", "Model.zip"),
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "models", "Model.zip"),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "wwwroot", "models", "Model.zip")
            };

            foreach (var p in candidates)
            {
                _checkedPaths.Add(p);
                try
                {
                    if (File.Exists(p))
                    {
                        _logger.LogInformation("CategoryPredictionService: intentando cargar modelo desde {Path}", p);
                        using var stream = File.OpenRead(p);
                        var model = _mlContext.Model.Load(stream, out var schema);
                        // Log some schema columns for quick inspection
                        try
                        {
                            var cols = string.Join(',', schema.Select(c => c.Name).Take(12));
                            _logger.LogInformation("CategoryPredictionService: modelo cargado desde {Path}; columnas: {Cols}", p, cols);
                        }
                        catch { }
                        _loadedModelPath = p;
                        return model;
                    }
                }
                catch (Exception ex) { _logger.LogError(ex, "CategoryPredictionService: error leyendo modelo desde {Path}", p); }
            }
            return null;
        }

        public PredictionServiceDiagnostics GetDiagnostics()
        {
            return new PredictionServiceDiagnostics
            {
                PoolResolved = _poolDynamic != null,
                HasFallbackModel = _fallbackModel != null,
                LoadedModelPath = _loadedModelPath,
                CheckedPaths = _checkedPaths.ToList()
            };
        }

        public string? PredictCategory(string descripcion)
        {
            if (string.IsNullOrWhiteSpace(descripcion)) return null;

            if (_poolDynamic != null)
            {
                try
                {
                    var input = new TicketDataML { descripcion = descripcion };
                    dynamic pred = _poolDynamic.Predict(modelName: "mlModel", example: input);
                    try
                    {
                        var rawPred = pred == null ? null : Convert.ToString((object)pred);
                        _logger.LogInformation("CategoryPredictionService: raw pool prediction: {Pred}", rawPred);
                    }
                    catch { }

                    // Intent: intentar leer varias propiedades posibles del objeto de predicción
                    try
                    {
                        if (pred == null) return null;
                        // Common property names used in different training code
                        if (((object)pred).GetType().GetProperty("PredictedLabel") != null)
                        {
                            return (string?)pred.PredictedLabel;
                        }
                        if (((object)pred).GetType().GetProperty("PredictedCategory") != null)
                        {
                            return (string?)pred.PredictedCategory;
                        }
                        // Fallback: try ToString()
                        var s = pred.ToString();
                        if (!string.IsNullOrWhiteSpace(s)) return s;
                        // If pool returned null/empty label, try using the fallback model (local) if available
                        _logger.LogInformation("CategoryPredictionService: pool returned empty label, attempting local fallback prediction...");
                        // Ensure fallback model is loaded
                        if (_fallbackModel == null)
                        {
                            try
                            {
                                // attempt to load now (recording checked paths)
                                var loaded = LoadFallbackModel();
                                if (loaded != null)
                                {
                                    _logger.LogInformation("CategoryPredictionService: fallback model loaded on-demand from {Path}", _loadedModelPath);
                                    // assign to field so subsequent calls reuse it
                                    // as LoadFallbackModel returns ITransformer, assign via reflection-safe cast
                                    // (we are in same class so just set)
                                    // note: _fallbackModel is readonly; to set, we will use local engine directly below
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "CategoryPredictionService: error cargando modelo fallback on-demand");
                            }
                        }
                        // If we have a fallback model loaded now, use it
                        if (_fallbackModel != null)
                        {
                            try
                            {
                                var engine = _mlContext.Model.CreatePredictionEngine<TicketDataML, TicketPredictionML>(_fallbackModel);
                                var pred2 = engine.Predict(new TicketDataML { descripcion = descripcion });
                                _logger.LogInformation("CategoryPredictionService: on-demand fallback prediction: {Pred}", pred2?.PredictedLabel);
                                return pred2?.PredictedLabel;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "CategoryPredictionService: error ejecutando predicción local on-demand");
                            }
                        }
                        return null;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CategoryPredictionService: error extrayendo etiqueta de la predicción del pool");
                        return null;
                    }
                }
                catch { return null; }
            }

            if (_fallbackModel != null)
            {
                try
                {
                    var engine = _mlContext.Model.CreatePredictionEngine<TicketDataML, TicketPredictionML>(_fallbackModel);
                    var pred = engine.Predict(new TicketDataML { descripcion = descripcion });
                    _logger.LogInformation("CategoryPredictionService: fallback prediction: {Pred}", pred?.PredictedLabel);
                    return pred?.PredictedLabel;
                }
                catch { return null; }
            }

            return null;
        }
    }
}
