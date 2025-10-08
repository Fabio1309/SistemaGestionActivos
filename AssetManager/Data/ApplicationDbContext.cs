using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AssetManager.Models;

namespace AssetManager.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Activo> Activos { get; set; }
    public DbSet<Categoria> Categorias { get; set; }
    public DbSet<Ubicacion> Ubicaciones { get; set; }
}
