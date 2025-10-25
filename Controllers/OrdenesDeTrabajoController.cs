using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Security.Claims; // Necesario para obtener el ID del usuario logueado
using Microsoft.EntityFrameworkCore; // Para Include, ToListAsync, etc.
using System.Linq; // Para OrderByDescending y operadores LINQ
using Microsoft.AspNetCore.Mvc.Rendering; // Para SelectList

namespace SistemaGestionActivos.Controllers
{
    [Authorize]
    public class OrdenesDeTrabajoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager; // Usamos la clase personalizada Usuario

        public OrdenesDeTrabajoController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /OrdenesDeTrabajo - Página Principal de Gestión de OTs
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Index()
        {
            var viewModel = new OrdenDeTrabajoViewModel
            {
                OrdenesDeTrabajo = await _context.OrdenesDeTrabajo
                    .Include(o => o.Activo)
                    .Include(o => o.UsuarioReporta)
                    .Include(o => o.TecnicoAsignado)
                    .OrderByDescending(o => o.FechaCreacion)
                    .ToListAsync(),
                
                // Preparamos la lista de técnicos para el dropdown del modal de asignación
                TecnicosDisponibles = new SelectList(await _userManager.GetUsersInRoleAsync("Técnico"), "Id", "UserName")
            };
                
            return View(viewModel);
        }

        // POST: /OrdenesDeTrabajo/Asignar - Procesa la asignación desde el modal
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Asignar(int id, string tecnicoAsignadoId)
        {
            if (string.IsNullOrEmpty(tecnicoAsignadoId))
            {
                TempData["ErrorMessage"] = "Error: Debe seleccionar un técnico para asignar la OT.";
                return RedirectToAction(nameof(Index));
            }

            var ordenDeTrabajo = await _context.OrdenesDeTrabajo.FindAsync(id);

            if (ordenDeTrabajo == null || ordenDeTrabajo.Estado != EstadoOT.Abierta)
            {
                TempData["ErrorMessage"] = "Esta Orden de Trabajo ya no se puede asignar o no existe.";
                return RedirectToAction(nameof(Index));
            }

            ordenDeTrabajo.TecnicoAsignadoId = tecnicoAsignadoId;
            ordenDeTrabajo.Estado = EstadoOT.Asignada; // Se cambia el estado a "Asignada"
            
            _context.Update(ordenDeTrabajo);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Orden de Trabajo ha sido asignada con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // --- MÉTODOS PARA CREAR UNA NUEVA ORDEN DE TRABAJO ---

        // GET: OrdenesDeTrabajo/Crear/5
        // Muestra el formulario para crear una OT desde el perfil de un activo.
        public async Task<IActionResult> Crear(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            if (activo == null)
            {
                return NotFound();
            }

            var ordenDeTrabajo = new OrdenDeTrabajo
            {
                ActivoId = activoId,
            };

            ViewBag.NombreActivo = activo.nom_act; // Adaptado a tu nombre de propiedad
            ViewBag.CodigoActivo = activo.cod_act; // Adaptado a tu nombre de propiedad
            return View(ordenDeTrabajo);
        }

        // POST: OrdenesDeTrabajo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("ActivoId,DescripcionProblema")] OrdenDeTrabajo ordenDeTrabajo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            ordenDeTrabajo.UsuarioReportaId = userId;
            ordenDeTrabajo.FechaCreacion = DateTime.Now;
            ordenDeTrabajo.Estado = EstadoOT.Abierta;

            ModelState.Remove("UsuarioReportaId"); 
            ModelState.Remove("TecnicoAsignadoId");

            if (ModelState.IsValid)
            {
                _context.Add(ordenDeTrabajo);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "La orden de trabajo se ha creado y enviado para su gestión.";
                return RedirectToAction("Detalles", "Activos", new { id = ordenDeTrabajo.ActivoId });
            }

            var activo = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
            if (activo != null)
            {
                ViewBag.NombreActivo = activo.nom_act;
                ViewBag.CodigoActivo = activo.cod_act;
            }
            return View(ordenDeTrabajo);
        }
    }
}