using SistemaGestionActivos.Models;
namespace SistemaGestionActivos.Models;

public class CategoriaViewModel
{
    public IEnumerable<Categoria> CategoriasExistentes { get; set; }
    public Categoria NuevaCategoria { get; set; }
}
