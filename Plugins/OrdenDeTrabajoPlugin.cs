using Microsoft.SemanticKernel;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore; // No olvides este using

namespace SistemaGestionActivos.Plugins
{
    public class OrdenDeTrabajoPlugin
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public OrdenDeTrabajoPlugin(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [KernelFunction]
        [Description("Crea una nueva Orden de Trabajo (OT) para un activo dañado.")]
        public async Task<string> CrearOrdenDeTrabajo(
            [Description("El código del activo que está dañado. Ej: 'LP-001'")] string codigoActivo,
            [Description("La descripción detallada del problema que reporta el usuario")] string descripcionProblema,
            [Description("El email del usuario que está reportando el problema")] string emailUsuario)
        {
            // 1. Encontrar el Activo y el Usuario
            var activo = await _context.Activos.FirstOrDefaultAsync(a => a.cod_act == codigoActivo);
            var usuario = await _userManager.FindByEmailAsync(emailUsuario);

            if (activo == null) return $"Error: No se encontró el activo con código {codigoActivo}.";
            if (usuario == null) return $"Error: No se encontró al usuario {emailUsuario}.";

            // 2. Lógica de tu controlador (la hemos copiado aquí)
            if (activo.estado == EstadoActivo.EnMantenimiento)
            {
                return "Error: Este activo ya se encuentra en mantenimiento.";
            }

            activo.estado = EstadoActivo.EnMantenimiento;
            _context.Update(activo);

            // 3. Crear la OT
            var ot = new OrdenDeTrabajo
            {
                ActivoId = activo.activo_id,
                DescripcionProblema = descripcionProblema,
                UsuarioReportaId = usuario.Id,
                FechaCreacion = DateTime.Now,
                Estado = EstadoOT.Abierta
            };

            _context.OrdenesDeTrabajo.Add(ot);
            await _context.SaveChangesAsync();
            
            // 4. Devolver una respuesta exitosa a la IA
            return $"¡Éxito! Se ha creado la Orden de Trabajo N° {ot.Id} para el activo {activo.nom_act}.";
        }
    }
}