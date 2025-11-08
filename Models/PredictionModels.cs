using Microsoft.ML.Data;

namespace SistemaGestionActivos.Models
{
    // Tipos usados por PredictionEnginePool
    public class TicketDataML
    {
        [LoadColumn(0)]
        public string? descripcion { get; set; }
    }

    public class TicketPredictionML
    {
        [ColumnName("PredictedLabel")]
        public string? PredictedLabel { get; set; }
    }
}
