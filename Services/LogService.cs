using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;

namespace SistemaGestionActivos.Services
{
    public class LogService : ILogService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public LogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task RegistrarLogAsync(string usuarioId, string accion, string? entidad = null, string? entidadId = null)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

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