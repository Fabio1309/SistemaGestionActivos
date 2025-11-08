using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models
{
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        // Relación 1:1 con OrdenDeTrabajo
        [Required]
        [Column("OrdenTrabajoId")]
        public int OrdenTrabajoId { get; set; }

        [ForeignKey("OrdenTrabajoId")]
        public virtual OrdenDeTrabajo? OrdenDeTrabajo { get; set; }

        [Required]
        [Display(Name = "Fecha de Emisión")]
        [Column("FechaEmision")]
        public DateTime FechaEmision { get; set; }

        [Required]
        [Column("MontoTotal", TypeName = "decimal(18, 2)")]
        [Display(Name = "Monto Total")]
        public decimal MontoTotal { get; set; }

        [Required]
        [Column("Estado")]
        public string Estado { get; set; } = string.Empty; // "Pendiente de Pago", "Pagada"

        [Column("MetodoPago")]
        public string? MetodoPago { get; set; }

        [Column("PagoIdExterno")]
        public string? PagoIdExterno { get; set; }
    }
}