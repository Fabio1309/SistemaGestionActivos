using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaGestionActivos.Models;

public class Activo
{
    [Key]
    public int activo_id { get; set; }

    [Display(Name = "Nombre del Activo")]
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string nom_act { get; set; } = string.Empty;

    [Display(Name = "Código del Activo")]
    [Required(ErrorMessage = "El código es obligatorio.")]
    public string cod_act { get; set; } = string.Empty;

    [Display(Name = "Modelo")]
    public string? modelo { get; set; }

    [Display(Name = "Número de Serie")]
    public string? num_serie { get; set; }

    [Display(Name = "Costo de Compra")]
    [Column(TypeName = "decimal(18, 2)")]
    public decimal costo { get; set; }

    [Display(Name = "Fecha de Compra")]
    [DataType(DataType.Date)]
    public DateTime fecha_com { get; set; }

    [Display(Name = "Proveedor")]
    public string? proveedor { get; set; }

    [Display(Name = "Estado")]
    [Required(ErrorMessage = "El estado es obligatorio.")]
    public EstadoActivo estado { get; set; }

    [Display(Name = "Categoría")]
    public int? categ_id { get; set; } 

    [ForeignKey("categ_id")] 
    public virtual Categoria? Categoria { get; set; }

    [Display(Name = "Ubicación")]
    public int? ubic_id { get; set; }

    [ForeignKey("ubic_id")] 
    public virtual Ubicacion? Ubicacion { get; set; }

    // Compatibilidad: propiedades más legibles usadas por controladores/vistas
    [NotMapped]
    public string Nombre { get => nom_act; set => nom_act = value; }

    [NotMapped]
    public string CodigoActivo { get => cod_act; set => cod_act = value; }

    // Historial de asignaciones relacionado (navegación)
    public virtual ICollection<Asignacion> HistorialAsignaciones { get; set; } = new List<Asignacion>();
}

