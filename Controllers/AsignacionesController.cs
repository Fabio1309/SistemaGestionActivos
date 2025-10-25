using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using sistemagestionactivos.Data;
using sistemagestionactivos.Models;

namespace sistemagestionactivos.Controllers
{
    [Authorize(Roles = "Administrador, Gestor de Activos")]
    public class AsignacionesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AsignacionesController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Asignaciones/Asignar/5 (Muestra el formulario para asignar)
        public async Task<IActionResult> Asignar(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            if (activo == null || activo.Estado != EstadoActivo.Disponible)
            {
                return NotFound("El activo no está disponible para ser asignado.");
            }

            // Obtener lista de usuarios con rol "Empleado"
            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            ViewData["UsuarioId"] = new SelectList(empleados, "Id", "UserName");
            
            var asignacion = new Asignacion { ActivoId = activoId, FechaAsignacion = DateTime.Now };
            return View(asignacion);
        }

        // POST: Asignaciones/Asignar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar([Bind("ActivoId,UsuarioId,FechaAsignacion")] Asignacion asignacion)
        {
            if (ModelState.IsValid)
            {
                // Actualizar el estado del activo
                var activo = await _context.Activos.FindAsync(asignacion.ActivoId);
                if (activo != null)
                {
                    activo.Estado = EstadoActivo.Asignado;
                    _context.Update(activo);
                }

                _context.Add(asignacion);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Activos", new { id = asignacion.ActivoId });
            }

            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            ViewData["UsuarioId"] = new SelectList(empleados, "Id", "UserName", asignacion.UsuarioId);
            return View(asignacion);
        }
        
        // GET: Asignaciones/Devolver/5 (Muestra el formulario para devolver)
        public async Task<IActionResult> Devolver(int activoId)
        {
            var asignacionActual = await _context.Asignaciones
                .FirstOrDefaultAsync(a => a.ActivoId == activoId && a.FechaDevolucion == null);
            
            if (asignacionActual == null)
            {
                return NotFound("Este activo no tiene una asignación activa para devolver.");
            }
            return View(asignacionActual);
        }

        // POST: Asignaciones/Devolver
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Devolver(int id, EstadoDevolucion estadoDevolucion)
        {
            var asignacion = await _context.Asignaciones.FindAsync(id);
            if (asignacion != null)
            {
                asignacion.FechaDevolucion = DateTime.Now;
                asignacion.EstadoDevolucion = estadoDevolucion;

                var activo = await _context.Activos.FindAsync(asignacion.ActivoId);
                if (activo != null)
                {
                    activo.Estado = (estadoDevolucion == EstadoDevolucion.Dañado) 
                        ? EstadoActivo.EnMantenimiento 
                        : EstadoActivo.Disponible;
                    _context.Update(activo);
                }

                _context.Update(asignacion);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Activos", new { id = asignacion.ActivoId });
            }
            return NotFound();
        }
    }
}