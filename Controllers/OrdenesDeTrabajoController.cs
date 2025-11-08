using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SistemaGestionActivos.Controllers
{
    [Authorize]
    public class OrdenesDeTrabajoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;

        public OrdenesDeTrabajoController(ApplicationDbContext context, UserManager<Usuario> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /OrdenesDeTrabajo
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
                
                TecnicosDisponibles = new SelectList(await _userManager.GetUsersInRoleAsync("Técnico"), "Id", "UserName")
            };
                
            return View(viewModel);
        }

        // POST: /OrdenesDeTrabajo/Asignar
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
            ordenDeTrabajo.Estado = EstadoOT.Asignada; 
            
            _context.Update(ordenDeTrabajo);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Orden de Trabajo ha sido asignada con éxito.";
            return RedirectToAction(nameof(Index));
        }

        // --- MÉTODOS PARA CREAR UNA NUEVA ORDEN DE TRABAJO ---

        // GET: OrdenesDeTrabajo/Crear/5
        // (Esta se llama desde "Mis Activos" o "Detalles del Activo")
        [Authorize(Roles = "Empleado, Técnico, Administrador")]
        public async Task<IActionResult> Crear(int activoId)
        {
            var activo = await _context.Activos.FindAsync(activoId);
            if (activo == null)
            {
                return NotFound();
            }

            if (activo.estado == EstadoActivo.EnMantenimiento)
            {
                TempData["ErrorMessage"] = "Este activo ya se encuentra en mantenimiento. Resuelva las OTs existentes primero.";
                return RedirectToAction("Detalles", "Activos", new { id = activoId });
            }
            
            var ordenDeTrabajo = new OrdenDeTrabajo
            {
                ActivoId = activoId,
            };

            ViewBag.NombreActivo = activo.nom_act; 
            ViewBag.CodigoActivo = activo.cod_act; 
            return View(ordenDeTrabajo);
        }

        // ===== INICIO DE LA CORRECCIÓN =====
        // GET: OrdenesDeTrabajo/Reportar
        // (Esta es la nueva acción que se llama desde el menú lateral)
        [Authorize(Roles = "Empleado, Técnico, Administrador")]
        public async Task<IActionResult> Reportar() // <-- RENOMBRADA DE "Crear" a "Reportar"
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<Activo> activosDisponibles;

            if (User.IsInRole("Administrador"))
            {
                activosDisponibles = await _context.Activos
                    .Where(a => a.estado != EstadoActivo.DeBaja) 
                    .ToListAsync();
            }
            else
            {
                var asignaciones = await _context.Asignaciones
                    .Where(a => a.UsuarioId == userId && a.FechaDevolucion == null)
                    .Include(a => a.Activo)
                    .ToListAsync();

                activosDisponibles = asignaciones
                    .Where(a => a.Activo != null)
                    .Select(a => a.Activo!)
                    .ToList();
            }

            // El campo PK de Activo en la entidad es 'activo_id'
            ViewData["ActivoId"] = new SelectList(activosDisponibles, "activo_id", "nom_act");
            
            // Le decimos que use la vista "Crear.cshtml"
            return View("Crear", new OrdenDeTrabajo());
        }
        // ===== FIN DE LA CORRECCIÓN =====

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
                var activo = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
                if (activo != null && activo.estado == EstadoActivo.Disponible)
                {
                    activo.estado = EstadoActivo.EnMantenimiento;
                    _context.Update(activo);
                }

                _context.Add(ordenDeTrabajo);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "La orden de trabajo se ha creado y enviado para su gestión.";
                
                if (User.IsInRole("Empleado"))
                {
                     return RedirectToAction(nameof(MisReportes));
                }
                
                return RedirectToAction("Index", "Home");
            }

            // Si el modelo no es válido, recargamos la data necesaria
            if(ViewBag.NombreActivo == null)
            {
                var userIdForm = User.FindFirstValue(ClaimTypes.NameIdentifier);
                List<Activo> activosDisponibles;
                if (User.IsInRole("Administrador"))
                {
                    activosDisponibles = await _context.Activos.Where(a => a.estado != EstadoActivo.DeBaja).ToListAsync();
                }
                else
                {
                    var asignacionesForm = await _context.Asignaciones
                        .Where(a => a.UsuarioId == userIdForm && a.FechaDevolucion == null)
                        .Include(a => a.Activo)
                        .ToListAsync();

                    activosDisponibles = asignacionesForm
                        .Where(a => a.Activo != null)
                        .Select(a => a.Activo!)
                        .ToList();
                }
                ViewData["ActivoId"] = new SelectList(activosDisponibles, "activo_id", "nom_act", ordenDeTrabajo.ActivoId);
            }
            else
            {
                 var activoParaRecarga = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
                if (activoParaRecarga != null)
                {
                    ViewBag.NombreActivo = activoParaRecarga.nom_act;
                    ViewBag.CodigoActivo = activoParaRecarga.cod_act;
                }
            }
            return View(ordenDeTrabajo);
        }

        // GET: /OrdenesDeTrabajo/MisAsignaciones
        [Authorize(Roles = "Técnico, Administrador")]
        public async Task<IActionResult> MisAsignaciones()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            IQueryable<OrdenDeTrabajo> query = _context.OrdenesDeTrabajo;

            if (!User.IsInRole("Administrador"))
            {
                 query = query.Where(o => o.TecnicoAsignadoId == userId);
            }

            var misOrdenesDeTrabajo = await query
                .Include(o => o.Activo)
                .Include(o => o.UsuarioReporta)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return View(misOrdenesDeTrabajo);
        }
        
        // GET: /OrdenesDeTrabajo/Actualizar/5
        [Authorize(Roles = "Técnico, Administrador")]
        public async Task<IActionResult> Actualizar(int id)
        {
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo
                .Include(o => o.Activo)
                .Include(o => o.Costos)
                .FirstOrDefaultAsync(o => o.Id == id);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ordenDeTrabajo == null || (ordenDeTrabajo.TecnicoAsignadoId != userId && !User.IsInRole("Administrador")))
            {
                TempData["ErrorMessage"] = "No tiene permiso para actualizar esta Orden de Trabajo.";
                return RedirectToAction(nameof(MisAsignaciones));
            }

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
        [Authorize(Roles = "Técnico, Administrador")]
        public async Task<IActionResult> Actualizar(int id, EstadoOT estado, string comentarios)
        {
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo.FindAsync(id);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (ordenDeTrabajo == null || (ordenDeTrabajo.TecnicoAsignadoId != userId && !User.IsInRole("Administrador")))
            {
                return Forbid();
            }

            ordenDeTrabajo.Estado = estado;
            ordenDeTrabajo.Comentarios = string.IsNullOrEmpty(ordenDeTrabajo.Comentarios)
                ? $"[{DateTime.Now:g}] {comentarios}"
                : $"{ordenDeTrabajo.Comentarios}\n[{DateTime.Now:g}] {comentarios}";

            try
            {
                _context.Update(ordenDeTrabajo);

                if (estado == EstadoOT.Resuelta)
                {
                    var activo = await _context.Activos.FindAsync(ordenDeTrabajo.ActivoId);
                    if (activo != null && activo.estado == EstadoActivo.EnMantenimiento)
                    {
                        var otrasOTsAbiertas = await _context.OrdenesDeTrabajo
                            .AnyAsync(o => o.ActivoId == ordenDeTrabajo.ActivoId && 
                                           o.Id != ordenDeTrabajo.Id && 
                                           o.Estado != EstadoOT.Resuelta); 

                        if (!otrasOTsAbiertas)
                        {
                            activo.estado = EstadoActivo.Disponible;
                            _context.Update(activo);
                        }
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

        // POST: /OrdenesDeTrabajo/AgregarCosto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Técnico, Administrador")]
        public async Task<IActionResult> AgregarCosto(int ordenDeTrabajoId, string descripcion, decimal monto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var ordenDeTrabajo = await _context.OrdenesDeTrabajo.FindAsync(ordenDeTrabajoId);

            if (ordenDeTrabajo == null || (ordenDeTrabajo.TecnicoAsignadoId != userId && !User.IsInRole("Administrador")))
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

            return RedirectToAction(nameof(Actualizar), new { id = ordenDeTrabajoId });
        }

        // GET: /OrdenesDeTrabajo/Detalles/5
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();

            var ot = await _context.OrdenesDeTrabajo
                .Include(o => o.Costos)
                .Include(o => o.Factura)
                .Include(o => o.Activo)
                .Include(o => o.UsuarioReporta)
                .Include(o => o.TecnicoAsignado)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (ot == null) return NotFound("La Orden de Trabajo no fue encontrada.");
            
            return View(ot);
        }
        
        // GET: /OrdenesDeTrabajo/MisReportes
        [Authorize(Roles = "Empleado, Administrador")]
        public async Task<IActionResult> MisReportes()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }
            
            IQueryable<OrdenDeTrabajo> query = _context.OrdenesDeTrabajo;

            if (!User.IsInRole("Administrador"))
            {
                query = query.Where(o => o.UsuarioReportaId == userId);
            }

            var misReportes = await query
                .Include(o => o.Activo)
                .Include(o => o.TecnicoAsignado)
                .OrderByDescending(o => o.FechaCreacion)
                .ToListAsync();

            return View(misReportes);
        }
    }
}