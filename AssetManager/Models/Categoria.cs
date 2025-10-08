using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models;

public class Categoria
{
    [Key]
    public int categ_id { get; set; }

    [Display(Name = "Categoría")]
    [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
    public string nom_categoria { get; set; }
}
