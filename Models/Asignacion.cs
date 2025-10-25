using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SistemaGestionActivos.Models
{
    public class Asignacion
    {
        [Key]
        public int Id { get; set; }

        public int ActivoId { get; set; }
        [ForeignKey("ActivoId")]
        public virtual Activo? Activo { get; set; }

    public string? UsuarioId { get; set; }
    [ForeignKey("UsuarioId")]
    public virtual Usuario? Usuario { get; set; }

        public DateTime FechaAsignacion { get; set; }
        public DateTime? FechaDevolucion { get; set; }
        public string? EstadoDevolucion { get; set; }
    }
}
