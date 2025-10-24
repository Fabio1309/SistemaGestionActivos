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
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(options =>
        {
            options.MigrationsAssembly("AssetManager"); 
        });
        base.OnConfiguring(optionsBuilder);
    }
}
