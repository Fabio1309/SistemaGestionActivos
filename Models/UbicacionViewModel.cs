using SistemaGestionActivos.Models;
namespace SistemaGestionActivos.Models;

public class UbicacionViewModel
{
    public IEnumerable<Ubicacion> UbicacionesExistentes { get; set; }
    public Ubicacion NuevaUbicacion { get; set; }
}
