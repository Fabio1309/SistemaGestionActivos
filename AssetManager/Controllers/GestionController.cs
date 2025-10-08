using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using proyecto_programacion.Data;
using proyecto_programacion.Models;
using System.Threading.Tasks;
using System.Linq;

namespace proyecto_programacion.Controllers;

public class GestionController : Controller
{
    private readonly ApplicationDbContext _context;

    public GestionController(ApplicationDbContext context)
    {
        _context = context;
    }


    public async Task<IActionResult> Categorias()
    {
        var viewModel = new CategoriaViewModel
        {
            CategoriasExistentes = await _context.Categorias.OrderBy(c => c.nom_categoria).ToListAsync(),
            NuevaCategoria = new Categoria()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCategoria(Categoria nuevaCategoria)
    {
        if (ModelState.IsValid)
        {
            _context.Add(nuevaCategoria);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Categoría creada exitosamente.";
            return RedirectToAction(nameof(Categorias));
        }
        
        var viewModel = new CategoriaViewModel
        {
            CategoriasExistentes = await _context.Categorias.ToListAsync(),
            NuevaCategoria = nuevaCategoria
        };
        return View("Categorias", viewModel);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCategoria(int id, string nom_categoria)
    {
        if (string.IsNullOrEmpty(nom_categoria))
        {
            TempData["ErrorMessage"] = "El nombre de la categoría no puede estar vacío.";
            return RedirectToAction(nameof(Categorias));
        }

        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return NotFound();

        categoria.nom_categoria = nom_categoria;
        _context.Update(categoria);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Categoría actualizada exitosamente.";
        return RedirectToAction(nameof(Categorias));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarCategoria(int id)
    {
        var categoria = await _context.Categorias.FindAsync(id);
        if (categoria == null) return NotFound();

        var activoUsaCategoria = await _context.Activos.AnyAsync(a => a.categ_id == id);
        if (activoUsaCategoria)
        {
            TempData["ErrorMessage"] = "No se puede eliminar la categoría porque está siendo utilizada por uno o más activos.";
            return RedirectToAction(nameof(Categorias));
        }

        _context.Categorias.Remove(categoria);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Categoría eliminada exitosamente.";
        return RedirectToAction(nameof(Categorias));
    }


    public async Task<IActionResult> Ubicaciones()
    {
        var viewModel = new UbicacionViewModel
        {
            UbicacionesExistentes = await _context.Ubicaciones.OrderBy(u => u.nom_ubica).ToListAsync(),
            NuevaUbicacion = new Ubicacion()
        };
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearUbicacion(Ubicacion nuevaUbicacion)
    {
        if (ModelState.IsValid)
        {
            _context.Add(nuevaUbicacion);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ubicación creada exitosamente.";
            return RedirectToAction(nameof(Ubicaciones));
        }
        
        var viewModel = new UbicacionViewModel
        {
            UbicacionesExistentes = await _context.Ubicaciones.ToListAsync(),
