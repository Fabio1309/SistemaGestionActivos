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
    // Solo los roles de gestión pueden acceder a esta sección
    [Authorize(Roles = "Administrador, Gestor de Activos")]
    public class PlanesMantenimientoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PlanesMantenimientoController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PlanesMantenimiento (Muestra la lista de planes existentes)
        public async Task<IActionResult> Index()
        {
            var planes = _context.PlanesMantenimiento.Include(p => p.Categoria);
            return View(await planes.ToListAsync());
        }

        // GET: PlanesMantenimiento/Create (Muestra el formulario para crear un plan)
        public IActionResult Create()
        {
            // Preparamos los datos para los dropdowns de la vista
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
            // Si hay un error, volvemos a cargar el dropdown de categorías
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", planMantenimiento.CategoriaId);
            return View(planMantenimiento);
        }

        // GET: PlanesMantenimiento/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var planMantenimiento = await _context.PlanesMantenimiento.FindAsync(id);
            if (planMantenimiento == null) return NotFound();
            ViewData["CategoriaId"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", planMantenimiento.CategoriaId);
            return View(planMantenimiento);
        }

        // POST: PlanesMantenimiento/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Tarea,Frecuencia,Intervalo,FechaProximaEjecucion,CategoriaId")] PlanMantenimiento planMantenimiento)
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
            return View(planMantenimiento);
        }
        
        // ... (Los métodos GET y POST para Delete generados por el scaffolder están bien)
    }
}