using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models;

public class Ubicacion
{
    [Key]
    public int ubic_id { get; set; }

    [Display(Name = "Ubicación")]
    [Required(ErrorMessage = "El nombre de la ubicación es obligatorio.")]
    public string nom_ubica { get; set; }
}
