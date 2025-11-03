using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;

namespace SistemaGestionActivos.Services
{
    public class MantenimientoSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _services;

        public MantenimientoSchedulerService(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Tarea a ejecutar: revisar los planes de mantenimiento
                await CheckPlanesMantenimiento();

                // Esperar 24 horas para la próxima ejecución
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckPlanesMantenimiento()
        {
            using (var scope = _services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var hoy = DateTime.UtcNow.Date;

                var planesParaEjecutar = dbContext.PlanesMantenimiento
                    .Where(p => p.FechaProximaEjecucion.Date <= hoy)
                    .ToList();

                foreach (var plan in planesParaEjecutar)
                {
                    var activosDeCategoria = dbContext.Activos
                        .Where(a => a.categ_id == plan.CategoriaId && a.estado != EstadoActivo.DeBaja)
                        .ToList();

                    foreach (var activo in activosDeCategoria)
                    {
                        var ot = new OrdenDeTrabajo
                        {
                            ActivoId = activo.activo_id,
                            DescripcionProblema = $"Mantenimiento Preventivo: {plan.Titulo}",
                            FechaCreacion = DateTime.Now,
                            Estado = EstadoOT.Abierta,
                            UsuarioReportaId = "system" // O el ID de un usuario sistema
                        };
                        dbContext.OrdenesDeTrabajo.Add(ot);
                    }

                    // Calcular la próxima fecha de ejecución
                    plan.FechaProximaEjecucion = plan.Frecuencia switch
                    {
                        FrecuenciaMantenimiento.Diaria => plan.FechaProximaEjecucion.AddDays(plan.Intervalo),
                        FrecuenciaMantenimiento.Semanal => plan.FechaProximaEjecucion.AddDays(7 * plan.Intervalo),
                        FrecuenciaMantenimiento.Mensual => plan.FechaProximaEjecucion.AddMonths(plan.Intervalo),
                        FrecuenciaMantenimiento.Anual => plan.FechaProximaEjecucion.AddYears(plan.Intervalo),
                        _ => plan.FechaProximaEjecucion
                    };
                    dbContext.Update(plan);
                }
                await dbContext.SaveChangesAsync();
            }
        }
    }
}