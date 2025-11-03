using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Threading.Tasks;

namespace SistemaGestionActivos.Controllers
{
    // Solo los roles especificados pueden acceder a estas funcionalidades.
    [Authorize(Roles = "Administrador, Gestor de Activos")]
    public class AsignacionesController : Controller
    {
    private readonly ApplicationDbContext _context;
    private readonly UserManager<Usuario> _userManager; // Usar la clase de usuario personalizada 'Usuario'

        public AsignacionesController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Asignaciones/Asignar/5
        // Prepara y muestra el formulario para realizar un 'Check-out' de un activo.
        public async Task<IActionResult> Asignar(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            
            // Verificamos si el activo existe y si está en el estado correcto para ser asignado.
            if (activo == null || activo.estado != EstadoActivo.Disponible)
            {
                TempData["ErrorMessage"] = "El activo no está disponible para ser asignado en este momento.";
                return RedirectToAction("Detalles", "Activos", new { id = activoId });
            }

            // Obtenemos la lista de usuarios que tienen el rol de "Empleado" para el dropdown.
            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            ViewData["UsuarioId"] = new SelectList(empleados, "Id", "UserName");
            
            // Creamos un objeto de asignación pre-llenado para la vista.
            var asignacion = new Asignacion { ActivoId = activoId, FechaAsignacion = DateTime.Now };
            ViewBag.NombreActivo = activo.nom_act; // Enviamos el nombre para mostrarlo en el título de la vista.
            
            return View(asignacion);
        }

        // POST: /Asignaciones/Asignar
        // Procesa el formulario de 'Check-out'.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Asignar([Bind("ActivoId,UsuarioId,FechaAsignacion")] Asignacion asignacion)
        {
            // Validamos que se haya seleccionado un empleado.
            if (!string.IsNullOrEmpty(asignacion.UsuarioId))
            {
                var activo = await _context.Activos.FindAsync(asignacion.ActivoId);
                if (activo != null)
                {
                    // 1. Cambiamos el estado del activo a "Asignado".
                    activo.estado = EstadoActivo.Asignado;
                    _context.Update(activo);
                }

                // 2. Creamos y guardamos el nuevo registro de asignación.
                _context.Add(asignacion);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "El activo ha sido asignado correctamente.";
                return RedirectToAction("Detalles", "Activos", new { id = asignacion.ActivoId });
            }

            // Si hay un error (ej. no se seleccionó un usuario), volvemos a cargar el formulario.
            ModelState.AddModelError("UsuarioId", "Debe seleccionar un empleado.");
            var empleados = await _userManager.GetUsersInRoleAsync("Empleado");
            ViewData["UsuarioId"] = new SelectList(empleados, "Id", "UserName", asignacion.UsuarioId);
            var activoConError = await _context.Activos.FindAsync(asignacion.ActivoId);
            ViewBag.NombreActivo = activoConError?.nom_act;
            return View(asignacion);
        }
        
        // GET: /Asignaciones/Devolver/5
        // Prepara y muestra el formulario para realizar un 'Check-in' de un activo.
        public async Task<IActionResult> Devolver(int activoId)
        {
            // Buscamos la asignación activa (la que no tiene fecha de devolución).
            var asignacionActual = await _context.Asignaciones
                .Include(a => a.Activo)
                .FirstOrDefaultAsync(a => a.ActivoId == activoId && a.FechaDevolucion == null);
            
            if (asignacionActual == null)
            {
                TempData["ErrorMessage"] = "Este activo no tiene una asignación activa para registrar su devolución.";
                return RedirectToAction("Detalles", "Activos", new { id = activoId });
            }

            return View(asignacionActual);
        }

        // POST: /Asignaciones/Devolver
        // Procesa el formulario de 'Check-in'.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Devolver(int id, EstadoDevolucion estadoDevolucion)
        {
            var asignacion = await _context.Asignaciones.FindAsync(id);
            if (asignacion != null)
            {
                // 1. Actualizamos el registro de la asignación con la fecha y el estado de devolución.
                asignacion.FechaDevolucion = DateTime.Now;
                // Guardamos el enum como string para mantener compatibilidad con la BD existente
                asignacion.EstadoDevolucion = estadoDevolucion.ToString();

                var activo = await _context.Activos.FindAsync(asignacion.ActivoId);
                if (activo != null)
                {
                    // 2. Actualizamos el estado del activo. Si se devuelve dañado, pasa a mantenimiento.
                    activo.estado = (estadoDevolucion == EstadoDevolucion.Dañado) 
                        ? EstadoActivo.EnMantenimiento 
                        : EstadoActivo.Disponible;
                    _context.Update(activo);
                }

                _context.Update(asignacion);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "La devolución del activo ha sido registrada con éxito.";
                return RedirectToAction("Detalles", "Activos", new { id = asignacion.ActivoId });
            }

            TempData["ErrorMessage"] = "No se encontró la asignación a devolver.";
            return RedirectToAction("Index", "Activos");
        }
    }
}