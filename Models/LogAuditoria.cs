using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models
{
    public class LogAuditoria
    {
        [Key]
        public int Id { get; set; }

        // --- Quién lo hizo ---
    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    [ForeignKey("UsuarioId")]
    public virtual Usuario? Usuario { get; set; }

        // --- Qué hizo ---
    [Required]
    [Display(Name = "Acción Realizada")]
    public string Accion { get; set; } = string.Empty; // Ej: "Eliminó el activo", "Cambió el rol de"

        // --- A qué se lo hizo (Opcional) ---
        [Display(Name = "Entidad Afectada")]
        public string? Entidad { get; set; } // Ej: "Activo", "Usuario"

        [Display(Name = "ID de Entidad")]
        public string? EntidadId { get; set; } // El ID del activo o el email del usuario

        // --- Cuándo lo hizo ---
        [Required]
        [Display(Name = "Fecha y Hora")]
        public DateTime FechaHora { get; set; }
    }
}