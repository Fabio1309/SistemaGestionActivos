using System.Collections.Generic;

namespace SistemaGestionActivos.Services
{
    public class PredictionServiceDiagnostics
    {
        public bool PoolResolved { get; set; }
        public bool HasFallbackModel { get; set; }
        public string? LoadedModelPath { get; set; }
        public List<string> CheckedPaths { get; set; } = new List<string>();
    }
}
