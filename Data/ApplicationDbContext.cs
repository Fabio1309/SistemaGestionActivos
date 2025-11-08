using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Models;

namespace SistemaGestionActivos.Data;

public class ApplicationDbContext : IdentityDbContext<Usuario> 
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Activo> Activos { get; set; }
    public DbSet<Asignacion> Asignaciones { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
    public DbSet<OrdenDeTrabajo> OrdenesDeTrabajo { get; set; } 
    public DbSet<CostoMantenimiento> CostosMantenimiento { get; set; }
    public DbSet<PlanMantenimiento> PlanesMantenimiento { get; set; }
    public DbSet<Factura> Facturas { get; set; }
}
