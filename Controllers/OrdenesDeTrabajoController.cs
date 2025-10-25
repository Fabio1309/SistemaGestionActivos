using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using sistemagestionactivos.Data;
using sistemagestionactivos.Models;
using System.Security.Claims; // Necesario para obtener el ID del usuario logueado

namespace sistemagestionactivos.Controllers
{
    [Authorize] // Todos los usuarios logueados pueden acceder a este controlador
    public class OrdenesDeTrabajoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public OrdenesDeTrabajoController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: OrdenesDeTrabajo/Crear/5
        // Muestra el formulario para crear una OT para un activo específico.
        public async Task<IActionResult> Crear(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            if (activo == null)
            {
                return NotFound();
            }

            // Creamos un modelo de OT pre-llenado
            var ordenDeTrabajo = new OrdenDeTrabajo
            {
                ActivoId = activoId,
                FechaCreacion = DateTime.Now,
                Estado = EstadoOT.Abierta,
                // El ID del usuario que reporta se asignará en el POST
            };

            ViewBag.NombreActivo = activo.Nombre;
            ViewBag.CodigoActivo = activo.CodigoActivo;
            return View(ordenDeTrabajo);
        }

        // POST: OrdenesDeTrabajo/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([Bind("ActivoId,DescripcionProblema")] OrdenDeTrabajo ordenDeTrabajo)
        {
            // Obtenemos el ID del usuario que está actualmente logueado
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                // Si no podemos obtener el ID del usuario, es un problema de autorización
                return Unauthorized();
            }

            // Asignamos los valores que no vienen del formulario
            ordenDeTrabajo.UsuarioReportaId = userId;
            ordenDeTrabajo.FechaCreacion = DateTime.Now;
            ordenDeTrabajo.Estado = EstadoOT.Abierta;

            // Removemos del ModelState las propiedades que no queremos validar en este punto
            ModelState.Remove("UsuarioReportaId"); 
            ModelState.Remove("TecnicoAsignadoId");

            if (ModelState.IsValid)
            {
                _context.Add(ordenDeTrabajo);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Orden de Trabajo creada con éxito.";
                // Redirigimos al perfil del activo para que el usuario vea que se registró
                return RedirectToAction("Details", "Activos", new { id = ordenDeTrabajo.ActivoId });
            }

            // Si el modelo no es válido, volvemos a mostrar el formulario
            var activo = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
            ViewBag.NombreActivo = activo?.Nombre;
            ViewBag.CodigoActivo = activo?.CodigoActivo;
            return View(ordenDeTrabajo);
        }

        // --- Los otros métodos (Asignar, Actualizar, etc.) se añadirán en futuras funcionalidades ---
    }
}