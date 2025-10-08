using Microsoft.EntityFrameworkCore;
using proyecto_programacion.Models;

namespace proyecto_programacion.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Activo> Activos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Ubicacion> Ubicaciones { get; set; }
    }
}
