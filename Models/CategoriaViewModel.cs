using System.Collections.Generic;
using System.Linq;
using SistemaGestionActivos.Models;
namespace SistemaGestionActivos.Models;

public class CategoriaViewModel
{
    public IEnumerable<Categoria> CategoriasExistentes { get; set; } = Enumerable.Empty<Categoria>();
    public Categoria NuevaCategoria { get; set; } = new Categoria();
}
