using System;
using System.IO;
using Microsoft.ML;
using Microsoft.ML.Data;

// Data schema
public class TicketData
{
    [LoadColumn(0)]
    public string? descripcion { get; set; }

    [LoadColumn(1)]
    public string? categoria { get; set; }
}

public class TicketPrediction
{
    [ColumnName("PredictedLabel")]
    public string? PredictedCategory { get; set; }

    public float[]? Score { get; set; }
}

class Program
{
    static void Main(string[] args)
    {
        var mlContext = new MLContext(seed: 0);

        // Buscar el CSV subiendo por directorios desde varios puntos posibles (bin, proyecto, cwd)
        string? FindDataFile()
        {
            string fileName = "ot-data.csv";
            // posibles raíces desde donde buscar
            var candidates = new[] {
                AppContext.BaseDirectory,
                Path.Combine(AppContext.BaseDirectory, ".."),
                Path.Combine(AppContext.BaseDirectory, "..", ".."),
                Path.Combine(AppContext.BaseDirectory, "..", "..", ".."),
                Directory.GetCurrentDirectory(),
                Path.Combine(Directory.GetCurrentDirectory(), ".."),
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..")
            };

            foreach (var root in candidates)
            {
                try
                {
                    var full = Path.GetFullPath(Path.Combine(root, fileName));
                    if (File.Exists(full))
                        return full;
                }
                catch { }
            }
            return null;
        }

        var dataPath = FindDataFile();
        if (dataPath == null)
        {
            Console.WriteLine("No se encontró el archivo de datos 'ot-data.csv' en las rutas buscadas. Asegúrate de que esté en la raíz del proyecto.");
            return;
        }
        Console.WriteLine($"Usando datos desde: {dataPath}");

        // Cargar y dividir los datos
        var data = mlContext.Data.LoadFromTextFile<TicketData>(dataPath, hasHeader: true, separatorChar: ',');
        var split = mlContext.Data.TrainTestSplit(data, testFraction: 0.2, seed: 0);

        // Pipeline: map label, featurize text y entrenar
        var pipeline = mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "Label", inputColumnName: nameof(TicketData.categoria))
            .Append(mlContext.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(TicketData.descripcion)))
            .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "Label", featureColumnName: "Features"))
            .Append(mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));

        Console.WriteLine("Entrenando modelo...");
        var model = pipeline.Fit(split.TrainSet);

        Console.WriteLine("Evaluando en conjunto de prueba...");
        var predictions = model.Transform(split.TestSet);
        var metrics = mlContext.MulticlassClassification.Evaluate(predictions, labelColumnName: "Label", predictedLabelColumnName: "PredictedLabel");

        Console.WriteLine($"MicroAccuracy: {metrics.MicroAccuracy:F4}");
        Console.WriteLine($"MacroAccuracy: {metrics.MacroAccuracy:F4}");
        Console.WriteLine($"LogLoss: {metrics.LogLoss:F4}");

        // Guardar el modelo
        var modelPath = Path.Combine(AppContext.BaseDirectory, "Model.zip");
        mlContext.Model.Save(model, split.TrainSet.Schema, modelPath);
        Console.WriteLine($"Modelo guardado en: {modelPath}");

        // Probar predicción en ejemplos
        var predEngine = mlContext.Model.CreatePredictionEngine<TicketData, TicketPrediction>(model);
        var examples = new[] {
            new TicketData { descripcion = "la pantalla no enciende" },
            new TicketData { descripcion = "necesito instalar office" },
            new TicketData { descripcion = "solicito un disco duro nuevo" }
        };

        Console.WriteLine("Pruebas de predicción:");
        foreach (var ex in examples)
        {
            var p = predEngine.Predict(ex);
            Console.WriteLine($"'{ex.descripcion}' => {p.PredictedCategory}");
        }

        Console.WriteLine("Listo.");
    }
}