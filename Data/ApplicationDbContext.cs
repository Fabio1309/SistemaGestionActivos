using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SistemaGestionActivos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    public DbSet<SistemaGestionActivos.Models.Categoria> Categorias { get; set; }
    public DbSet<SistemaGestionActivos.Models.Ubicacion> Ubicaciones { get; set; }
    public DbSet<SistemaGestionActivos.Models.Activo> Activos { get; set; }
}
