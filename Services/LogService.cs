using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using Microsoft.EntityFrameworkCore;

namespace SistemaGestionActivos.Services
{
    public class LogService : ILogService
    {
        // Pedimos una 'fabrica' de DbContext para no interferir
        // con las operaciones de los controladores.
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

        public LogService(IDbContextFactory<ApplicationDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task RegistrarLogAsync(string usuarioId, string accion, string? entidad = null, string? entidadId = null)
        {
            // Creamos un nuevo DbContext solo para esta operación
            using (var context = _contextFactory.CreateDbContext())
            {
                var log = new LogAuditoria
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Entidad = entidad,
                    EntidadId = entidadId,
                    FechaHora = DateTime.Now
                };

                context.LogsAuditoria.Add(log);
                await context.SaveChangesAsync();
            }
        }
    }
}