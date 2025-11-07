using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models
{
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        // Relación 1:1 con OrdenTrabajo
        [Required]
        public int OrdenTrabajoId { get; set; }
        [ForeignKey("OrdenTrabajoId")]
        public virtual OrdenDeTrabajo OrdenDeTrabajo { get; set; }

        [Required]
        [Display(Name = "Fecha de Emisión")]
        public DateTime FechaEmision { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        [Display(Name = "Monto Total")]
        public decimal MontoTotal { get; set; }

        [Required]
        public string Estado { get; set; } // "Pendiente de Pago", "Pagada"

        // Campos para la API de Pagos (Mercado Pago / Stripe)
        public string? MetodoPago { get; set; }
        public string? PagoIdExterno { get; set; } // ID de la transacción en la API
    }
}