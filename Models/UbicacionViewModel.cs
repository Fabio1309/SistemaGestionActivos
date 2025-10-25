using System.Collections.Generic;
using System.Linq;
using SistemaGestionActivos.Models;
namespace SistemaGestionActivos.Models;

public class UbicacionViewModel
{
    public IEnumerable<Ubicacion> UbicacionesExistentes { get; set; } = Enumerable.Empty<Ubicacion>();
    public Ubicacion NuevaUbicacion { get; set; } = new Ubicacion();
}
