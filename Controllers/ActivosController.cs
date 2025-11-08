using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SistemaGestionActivos.Services; // <-- 1. AÑADE ESTE USING

namespace SistemaGestionActivos.Controllers
{
    [Authorize] 
    public class ActivosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Usuario> _userManager;
        private readonly ILogService _logService; // <-- 2. AÑADE EL SERVICIO DE LOG

        // 3. MODIFICA EL CONSTRUCTOR PARA INYECTAR EL SERVICIO
        public ActivosController(ApplicationDbContext context, UserManager<Usuario> userManager, ILogService logService)
        {
            _context = context;
            _userManager = userManager;
            _logService = logService; // <-- 4. ASÍGNALO
        }

        // GET: /Activos (Lista de Activos con Filtros)
        [AllowAnonymous] 
        public async Task<IActionResult> Index(string searchString, int? categoriaId, int? ubicacionId, EstadoActivo? estado)
        {
            ViewData["CategoriasList"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", categoriaId);
            ViewData["UbicacionesList"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", ubicacionId);
            ViewData["EstadosList"] = new SelectList(Enum.GetValues(typeof(EstadoActivo)));

            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentCategoria"] = categoriaId;
            ViewData["CurrentUbicacion"] = ubicacionId;
            ViewData["CurrentEstado"] = estado;

            var activosQuery = _context.Activos
                .Include(a => a.Categoria)
                .Include(a => a.Ubicacion)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                activosQuery = activosQuery.Where(a => 
                    a.nom_act.ToLower().Contains(searchString.ToLower()) || 
                    a.cod_act.ToLower().Contains(searchString.ToLower())
                );
            }
            if (categoriaId.HasValue)
            {
                activosQuery = activosQuery.Where(a => a.categ_id == categoriaId.Value);
            }
            if (ubicacionId.HasValue)
            {
                activosQuery = activosQuery.Where(a => a.ubic_id == ubicacionId.Value);
            }
            if (estado.HasValue)
            {
                activosQuery = activosQuery.Where(a => a.estado == estado.Value);
            }

            return View(await activosQuery.ToListAsync());
        }

        
        // GET: /Activos/Detalles/5
        [AllowAnonymous] 
        public async Task<IActionResult> Detalles(int? id)
        {
            if (id == null) return NotFound();
            var activo = await _context.Activos
                .Include(a => a.Categoria)
                .Include(a => a.Ubicacion)
                .Include(a => a.HistorialAsignaciones)
                    .ThenInclude(h => h.Usuario)
                .FirstOrDefaultAsync(m => m.activo_id == id);
            if (activo == null) return NotFound();
            return View(activo);
        }

        // GET: /Activos/Creacion
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public IActionResult Creacion()
        {
            ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria");
            ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica");
            ViewData["EstadosList"] = new SelectList(new List<EstadoActivo> { EstadoActivo.Disponible, EstadoActivo.EnMantenimiento, EstadoActivo.DeBaja });
            return View();
        }

        // POST: /Activos/Creacion
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Creacion([Bind("activo_id,nom_act,cod_act,modelo,num_serie,costo,fecha_com,proveedor,estado,categ_id,ubic_id")] Activo activo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(activo);
                await _context.SaveChangesAsync();
                
                // --- 5. AÑADIR REGISTRO DE LOG ---
                var userId = _userManager.GetUserId(User);
                await _logService.RegistrarLogAsync(userId, $"Creó el activo: {activo.nom_act}", "Activo", activo.activo_id.ToString());
                // ---------------------------------

                TempData["SuccessMessage"] = "Activo creado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
            ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
            return View(activo);
        }

        // GET: /Activos/Editar/5
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var activo = await _context.Activos.FindAsync(id);
            if (activo == null) return NotFound();
                
            ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
            ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
            ViewData["EstadosList"] = new SelectList(Enum.GetValues(typeof(EstadoActivo)), activo.estado);
            return View(activo);
        }

        // POST: /Activos/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, Gestor de Activos")]
        public async Task<IActionResult> Editar(int id, [Bind("activo_id,nom_act,cod_act,modelo,num_serie,costo,fecha_com,proveedor,estado,categ_id,ubic_id")] Activo activo)
        {
            if (id != activo.activo_id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(activo);
                    await _context.SaveChangesAsync();
                    
                    // --- 5. AÑADIR REGISTRO DE LOG ---
                    var userId = _userManager.GetUserId(User);
                    await _logService.RegistrarLogAsync(userId, $"Editó el activo: {activo.nom_act}", "Activo", activo.activo_id.ToString());
                    // ---------------------------------
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ActivoExists(activo.activo_id)) return NotFound();
                    else throw;
                }
                TempData["SuccessMessage"] = "Activo actualizado con éxito.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
            ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
            ViewData["EstadosList"] = new SelectList(Enum.GetValues(typeof(EstadoActivo)), activo.estado);
            
            return View(activo);
        }

        // GET: /Activos/Eliminar/5
        [Authorize(Roles = "Administrador")] 
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null) return NotFound();
            var activo = await _context.Activos
                .Include(a => a.Categoria)
                .Include(a => a.Ubicacion)
                .FirstOrDefaultAsync(m => m.activo_id == id);
            if (activo == null) return NotFound();
            return View(activo);
        }

        // POST: /Activos/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var activo = await _context.Activos.FindAsync(id);
            if (activo != null)
            {
                _context.Activos.Remove(activo);
                await _context.SaveChangesAsync();
                
                // --- 5. AÑADIR REGISTRO DE LOG ---
                var userId = _userManager.GetUserId(User);
                await _logService.RegistrarLogAsync(userId, $"Eliminó el activo: {activo.nom_act}", "Activo", activo.activo_id.ToString());
                // ---------------------------------

                TempData["SuccessMessage"] = "Activo eliminado permanentemente.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ActivoExists(int id)
        {
            return _context.Activos.Any(e => e.activo_id == id);
        }
        
        [Authorize(Roles = "Empleado, Técnico, Administrador")]
        public async Task<IActionResult> MisActivos()
        {
            var userId = _userManager.GetUserId(User);

            var misActivosAsignados = await _context.Asignaciones
                .Where(a => a.UsuarioId == userId && a.FechaDevolucion == null)
                .Include(a => a.Activo) 
                .Select(a => a.Activo)  
                .ToListAsync();

            return View(misActivosAsignados);
        }
    }
}