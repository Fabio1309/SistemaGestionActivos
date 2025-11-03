using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models // Asegúrate de que este namespace coincida con tu proyecto
{
    public class OrdenDeTrabajo
    {
        [Key]
        [Column("ot_id")] // Mapea a la columna 'ot_id' en la base de datos
        public int Id { get; set; }
        
        [Required(ErrorMessage = "La descripción del problema es obligatoria.")]
        [Column("descripcion_problema")]
        [Display(Name = "Descripción del Problema")]
        public string DescripcionProblema { get; set; } = string.Empty;
        
        [Column("fecha_creacion")]
        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; }
        
        [Column("estado_ot")]
        [Display(Name = "Estado")]
        public EstadoOT Estado { get; set; } // Usa el enum que definiremos
        
        [Column("comentarios")]
        public string? Comentarios { get; set; }

        // --- Relaciones (Foreign Keys) ---

        [Column("activo_id")]
        [Display(Name = "Activo")]
        public int ActivoId { get; set; }
        [ForeignKey("ActivoId")]
        public virtual Activo? Activo { get; set; }

        [Column("usuario_reporta_id")]
        public string UsuarioReportaId { get; set; } = string.Empty;
        [ForeignKey("UsuarioReportaId")]
        public virtual Usuario? UsuarioReporta { get; set; }

        [Column("tecnico_asignado_id")]
        public string? TecnicoAsignadoId { get; set; }
        [ForeignKey("TecnicoAsignadoId")]
        public virtual Usuario? TecnicoAsignado { get; set; }

        public virtual ICollection<CostoMantenimiento> Costos { get; set; } = new List<CostoMantenimiento>();
    }
}