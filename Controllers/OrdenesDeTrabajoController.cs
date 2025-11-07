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
        [Authorize(Roles = "Técnico")]
        public async Task<IActionResult> MisAsignaciones()
        {
            // Obtenemos el ID del técnico que ha iniciado sesión
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            // Buscamos solo las OTs asignadas a este técnico
            var misOrdenesDeTrabajo = await _context.OrdenesDeTrabajo
                .Where(o => o.TecnicoAsignadoId == userId)
                .Include(o => o.Activo)
                .Include(o => o.UsuarioReporta)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return View(misOrdenesDeTrabajo);
        }
        
        [Authorize(Roles = "Técnico")]
        public async Task<IActionResult> Actualizar(int id)
        {
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo
                .Include(o => o.Activo)
                .Include(o => o.Costos)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Doble validación de seguridad: la OT debe existir y debe estar asignada al técnico actual.
            if (ordenDeTrabajo == null || ordenDeTrabajo.TecnicoAsignadoId != userId)
            {
                TempData["ErrorMessage"] = "No tiene permiso para actualizar esta Orden de Trabajo.";
                return RedirectToAction(nameof(MisAsignaciones));
            }

            // Creamos una lista de los estados que el técnico puede seleccionar
            var estadosPermitidos = new List<EstadoOT>
            {
                EstadoOT.EnProgreso,
                EstadoOT.EnEsperaDeRepuesto,
                EstadoOT.Resuelta
            };
            ViewData["EstadosList"] = new SelectList(estadosPermitidos, ordenDeTrabajo.Estado);

            return View(ordenDeTrabajo);
        }

        // POST: /OrdenesDeTrabajo/Actualizar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Técnico")]
        public async Task<IActionResult> Actualizar(int id, EstadoOT estado, string comentarios)
        {
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ordenDeTrabajo == null || ordenDeTrabajo.TecnicoAsignadoId != userId)
            {
                return Forbid(); // Prohibido si no es su OT
            }

            // Actualizamos los campos
            ordenDeTrabajo.Estado = estado;
            // Añadimos el nuevo comentario al historial de comentarios (si el campo es nulo, lo inicializamos)
            ordenDeTrabajo.Comentarios = string.IsNullOrEmpty(ordenDeTrabajo.Comentarios)
                ? $"[{DateTime.Now:g}] {comentarios}"
                : $"{ordenDeTrabajo.Comentarios}\n[{DateTime.Now:g}] {comentarios}";

            try
            {
                _context.Update(ordenDeTrabajo);

                // Si el estado se marca como "Resuelta", también actualizamos el estado del activo
                if (estado == EstadoOT.Resuelta)
                {
                    var activo = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
                    if (activo != null && activo.estado == EstadoActivo.EnMantenimiento)
                    {
                        activo.estado = EstadoActivo.Disponible;
                        _context.Update(activo);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Orden de Trabajo actualizada con éxito.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Ocurrió un error al guardar los cambios.";
            }

            return RedirectToAction(nameof(MisAsignaciones));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Técnico")]
        public async Task<IActionResult> AgregarCosto(int ordenDeTrabajoId, string descripcion, decimal monto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo.FindAsync(ordenDeTrabajoId);

            // Validar que el técnico sea el dueño de la OT
            if (ordenDeTrabajo == null || ordenDeTrabajo.TecnicoAsignadoId != userId)
            {
                TempData["ErrorMessage"] = "No tiene permiso para añadir costos a esta OT.";
                return RedirectToAction(nameof(MisAsignaciones));
            }

            if (!string.IsNullOrEmpty(descripcion) && monto > 0)
            {
                var nuevoCosto = new CostoMantenimiento
                {
                    OrdenDeTrabajoId = ordenDeTrabajoId,
                    Descripcion = descripcion,
                    Monto = monto,
                    Fecha = DateTime.Now
                };
                _context.CostosMantenimiento.Add(nuevoCosto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Costo agregado correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "La descripción y un monto mayor a cero son requeridos.";
            }

            // Redirigir de vuelta a la misma página de actualización
            return RedirectToAction(nameof(Actualizar), new { id = ordenDeTrabajoId });
        }

        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();

            var ot = await _context.OrdenesDeTrabajo
                .Include(o => o.Costos)
                .Include(o => o.Factura) // Para saber si ya fue facturada
                .Include(o => o.Activo)
                .Include(o => o.UsuarioReporta)
                .Include(o => o.TecnicoAsignado)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ot == null) return NotFound("La Orden de Trabajo no fue encontrada.");
            
            return View(ot);
        }
    }
}