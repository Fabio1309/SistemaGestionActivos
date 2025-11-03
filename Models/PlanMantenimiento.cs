using System.ComponentModel.DataAnnotations;

namespace SistemaGestionActivos.Models
{
    public class PlanMantenimiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titulo { get; set; }

        public string Tarea { get; set; } // Ej: "Limpieza de filtros y revisión de gas"

        public FrecuenciaMantenimiento Frecuencia { get; set; }
        public int Intervalo { get; set; } // Ej: si Frecuencia=Mensual e Intervalo=3, es cada 3 meses.

        public DateTime FechaProximaEjecucion { get; set; }

        // Un plan puede aplicar a toda una categoría de activos
        public int CategoriaId { get; set; }
        public virtual Categoria? Categoria { get; set; }
    }
}