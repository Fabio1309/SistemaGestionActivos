using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionActivos.Controllers
{
    [Authorize(Roles = "Administrador, Gestor de Activos")]
    public class PlanesMantenimientoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanesMantenimientoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlanesMantenimiento
        public async Task<IActionResult> Index()
        {
            var planes = _context.PlanesMantenimiento.Include(p => p.Categoria);
            return View(await planes.ToListAsync());
        }

        // GET: PlanesMantenimiento/Create
        public IActionResult Create()
        {
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria");
            return View();
        }

        // POST: PlanesMantenimiento/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,Tarea,Frecuencia,Intervalo,FechaProximaEjecucion,CategoriaId")] PlanMantenimiento planMantenimiento)
        {
            if (ModelState.IsValid)
            {
                _context.Add(planMantenimiento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Plan de mantenimiento creado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", planMantenimiento.CategoriaId);
            return View(planMantenimiento);
        }

        // GET: PlanesMantenimiento/Editar/5
        public async Task<IActionResult> Editar(int? id)
        {
            if (id == null) return NotFound();
            var planMantenimiento = await _context.PlanesMantenimiento.FindAsync(id);
            if (planMantenimiento == null) return NotFound();
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", planMantenimiento.CategoriaId);
            return View("Editar", planMantenimiento);
        }

        // POST: PlanesMantenimiento/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, [Bind("Id,Titulo,Tarea,Frecuencia,Intervalo,FechaProximaEjecucion,CategoriaId")] PlanMantenimiento planMantenimiento)
        {
            if (id != planMantenimiento.Id) return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(planMantenimiento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Plan de mantenimiento actualizado con éxito.";
                return RedirectToAction(nameof(Index));
            }
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", planMantenimiento.CategoriaId);
            return View("Editar", planMantenimiento);
        }

        // GET: PlanesMantenimiento/Eliminar/5
        public async Task<IActionResult> Eliminar(int? id)
        {
            if (id == null) return NotFound();
            var planMantenimiento = await _context.PlanesMantenimiento
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (planMantenimiento == null) return NotFound();
            return View("Eliminar", planMantenimiento);
        }

        // POST: PlanesMantenimiento/Eliminar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Esta acción se llama "Eliminar" para coincidir con el asp-action del formulario
        public async Task<IActionResult> Eliminar(int id)
        {
            var planMantenimiento = await _context.PlanesMantenimiento.FindAsync(id);
            if (planMantenimiento != null)
            {
                _context.PlanesMantenimiento.Remove(planMantenimiento);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Plan de mantenimiento eliminado con éxito.";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}