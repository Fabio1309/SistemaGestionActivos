using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    // En producción, usa PostgreSQL. La connection string vendrá de Render.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // En desarrollo, sigue usando SQLite para simplicidad local.
    var connectionString = builder.Configuration.GetConnectionString("SQLiteConnection") ?? "Data Source=app.db";
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. AÑADIR Y CONFIGURAR EL SERVICIO DE IDENTITY ---
// Le decimos a la aplicación que use nuestra clase 'Usuario' para la identidad.
// También habilitamos el manejo de Roles.
builder.Services.AddDefaultIdentity<Usuario>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // <-- ¡LÍNEA CLAVE PARA HABILITAR ROLES!
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Esto ya lo tienes, registra los controladores y vistas.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 3. ACTIVAR LA AUTENTICACIÓN Y AUTORIZACIÓN ---
// El orden de estas dos líneas es MUY IMPORTANTE.
app.UseAuthentication(); // Primero, la aplicación identifica quién es el usuario (autenticación).
app.UseAuthorization();  // Después, verifica si ese usuario tiene permiso para acceder (autorización).

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- 4. AÑADIR MAPEO DE RAZOR PAGES ---
// Esto es necesario para que las páginas de Login, Registro, etc., que generaremos después, funcionen.
app.MapRazorPages(); 

// Esta sección crea los roles y un usuario administrador la primera vez que se ejecuta
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        
        // Ejecuta el método para crear roles y el admin
        await SeedRolesAndAdminAsync(roleManager, userManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error durante la siembra de datos.");
    }
}

// --- AÑADE ESTE MÉTODO AL FINAL DE TU ARCHIVO Program.cs ---
async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<Usuario> userManager)
{
    // Lista de roles que tu sistema necesita
    string[] roleNames = { "Administrador", "Gestor de Activos", "Técnico", "Empleado" };
    IdentityResult roleResult;

    foreach (var roleName in roleNames)
    {
        // Verifica si el rol ya existe
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            // Crea el rol si no existe
            roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Opcional: Crear un usuario Administrador por defecto
    var adminUser = await userManager.FindByEmailAsync("admin@activosys.com");
    if (adminUser == null)
    {
        var newAdminUser = new Usuario
        {
            UserName = "admin@activosys.com",
            Email = "admin@activosys.com",
            NombreCompleto = "Admin Principal",
            FechaNacimiento = new DateTime(1990, 1, 1), // Una fecha de ejemplo
            EmailConfirmed = true 
        };
        // ¡CAMBIA ESTA CONTRASEÑA!
        var result = await userManager.CreateAsync(newAdminUser, "Admin123*"); 
        if (result.Succeeded)
        {
            // Asigna el rol "Administrador" al nuevo usuario
            await userManager.AddToRoleAsync(newAdminUser, "Administrador");
        }
    }
}

app.Run();

