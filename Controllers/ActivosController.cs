using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionActivos.Controllers;

public class ActivosController : Controller
{
    private readonly ApplicationDbContext _context;

    public ActivosController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string searchString, int? categoriaId, int? ubicacionId, string estado)
    {
        ViewData["CategoriasList"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", categoriaId);
        ViewData["UbicacionesList"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", ubicacionId);
        var estadosList = new List<string> { "Operativo", "En Reparación", "De Baja" };
        ViewData["EstadosList"] = new SelectList(estadosList, estado);

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

        if (!string.IsNullOrEmpty(estado))
        {
            activosQuery = activosQuery.Where(a => a.estado == estado);
        }

        return View(await activosQuery.ToListAsync());
    }

    
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

    public IActionResult Creacion()
    {
        ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria");
        ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica");
        
        ViewData["EstadosList"] = new SelectList(new List<string> { "Operativo", "En Reparación", "De Baja" });

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Creacion([Bind("activo_id,nom_act,cod_act,modelo,num_serie,costo,fecha_com,proveedor,estado,categ_id,ubic_id")] Activo activo)
    {
        if (ModelState.IsValid)
        {
            _context.Add(activo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
        ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
        return View(activo);
    }

    public async Task<IActionResult> Editar(int? id)
    {
        if (id == null) return NotFound();

        var activo = await _context.Activos.FindAsync(id);
        if (activo == null) return NotFound();
            
        ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
        ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
        ViewData["EstadosList"] = new SelectList(new List<string> { "Operativo", "En Reparación", "De Baja" }, activo.estado);

        return View(activo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, [Bind("activo_id,nom_act,cod_act,modelo,num_serie,costo,fecha_com,proveedor,estado,categ_id,ubic_id")] Activo activo)
    {
        if (id != activo.activo_id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(activo);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ActivoExists(activo.activo_id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }

        ViewData["categ_id"] = new SelectList(_context.Categorias, "categ_id", "nom_categoria", activo.categ_id);
        ViewData["ubic_id"] = new SelectList(_context.Ubicaciones, "ubic_id", "nom_ubica", activo.ubic_id);
        ViewData["EstadosList"] = new SelectList(new List<string> { "Operativo", "En Reparación", "De Baja" }, activo.estado);
        
        return View(activo);
    }


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

    [HttpPost, ActionName("Eliminar")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarConfirmado(int id)
    {
        var activo = await _context.Activos.FindAsync(id);
        if (activo != null)
        {
            _context.Activos.Remove(activo);
        }
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool ActivoExists(int id)
    {
        return _context.Activos.Any(e => e.activo_id == id);
    }
}

    