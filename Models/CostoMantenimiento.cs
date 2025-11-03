using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models
{
    public class CostoMantenimiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Descripcion { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Monto { get; set; }
        
        public DateTime Fecha { get; set; }

        // Relación con la Orden de Trabajo
        public int OrdenDeTrabajoId { get; set; }
        public virtual OrdenDeTrabajo? OrdenDeTrabajo { get; set; }
    }
}