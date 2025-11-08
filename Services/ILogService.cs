namespace SistemaGestionActivos.Services
{
    public interface ILogService
    {
        // Define un método que todos los controladores puedan usar
        Task RegistrarLogAsync(string usuarioId, string accion, string? entidad = null, string? entidadId = null);
    }
}