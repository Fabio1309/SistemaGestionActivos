using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.ML;
using System.IO;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;
using SistemaGestionActivos.Models;
using SistemaGestionActivos.Services;
using SistemaGestionActivos.Plugins;
using Microsoft.SemanticKernel;

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

builder.Services.AddHostedService<MantenimientoSchedulerService>();

builder.Services.AddScoped<ILogService, LogService>();

// Registrar PredictionEnginePool si existe el modelo copiado en wwwroot/models/Model.zip
var modelPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "models", "Model.zip");
if (File.Exists(modelPath))
{
    // Registrar el pool y cargar el modelo nombrado "mlModel"
    builder.Services.AddPredictionEnginePool<TicketDataML, TicketPredictionML>()
        .FromFile(modelName: "mlModel", filePath: modelPath, watchForChanges: false);
}
else
{
    // Si no existe, no registramos el pool; el servicio hará fallback cargando Model.zip desde rutas alternativas.
    Console.WriteLine($"Warning: Model.zip no encontrado en {modelPath}. PredictionEnginePool no será registrado.");
}

// Registrar servicio de predicción (usa pool si está disponible, sino fallback)
builder.Services.AddSingleton<SistemaGestionActivos.Services.ICategoryPredictionService, SistemaGestionActivos.Services.CategoryPredictionService>();

builder.Services.AddKernel();
// Read model id from configuration, fallback to a broadly available model if not set
var openAiModel = builder.Configuration["OpenAI:Model"] ?? "gpt-3.5-turbo";
builder.Services.AddOpenAIChatCompletion(
    modelId: openAiModel,
    apiKey: builder.Configuration["OpenAI:ApiKey"] // <-- Pon tu clave en appsettings.json o en variables de entorno
);
// OrdenDeTrabajoPlugin depende de servicios con lifetime 'scoped' (ApplicationDbContext, UserManager)
// por eso debe registrarse como Scoped (no Singleton) para evitar "Cannot consume scoped service ... from singleton".
builder.Services.AddScoped<OrdenDeTrabajoPlugin>();

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
            // --- DIAGNÓSTICO: listar roles del admin para verificar seed ---
            var logger = services.GetRequiredService<ILogger<Program>>();
            try
            {
                var admin = await userManager.FindByEmailAsync("admin@activosys.com");
                if (admin != null)
                {
                    var adminRoles = await userManager.GetRolesAsync(admin);
                    logger.LogInformation("Seed check: admin '{Email}' roles: {Roles}", admin.Email, string.Join(',', adminRoles));
                    // Si no tiene el rol 'Administrador', lo agregamos para asegurarnos de que pueda acceder a las áreas de admin.
                    if (!adminRoles.Contains("Administrador"))
                    {
                        var addResult = await userManager.AddToRoleAsync(admin, "Administrador");
                        if (addResult.Succeeded)
                        {
                            logger.LogInformation("Seed fix: admin '{Email}' fue agregado al rol 'Administrador'", admin.Email);
                        }
                        else
                        {
                            logger.LogWarning("Seed fix: no se pudo agregar el admin '{Email}' al rol 'Administrador': {Errors}", admin.Email, string.Join(';', addResult.Errors.Select(e => e.Description)));
                        }
                    }

                    // Además hacemos una comprobación directa en la BD para ver las filas de AspNetUserRoles vinculadas al admin.
                    try
                    {
                        var userRoles = context.Set<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>()
                            .Where(ur => ur.UserId == admin.Id)
                            .ToList();
                        if (userRoles.Any())
                        {
                            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
                            var roleNames = context.Set<Microsoft.AspNetCore.Identity.IdentityRole>()
                                .Where(r => roleIds.Contains(r.Id))
                                .Select(r => r.Name)
                                .ToList();
                            logger.LogInformation("DB check: admin '{Email}' roles in AspNetUserRoles: {DBRoles}", admin.Email, string.Join(',', roleNames));
                        }
                        else
                        {
                            logger.LogInformation("DB check: admin '{Email}' no tiene entradas en AspNetUserRoles", admin.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error al consultar AspNetUserRoles para admin");
                    }
                }
                else
                {
                    logger.LogWarning("Seed check: admin user 'admin@activosys.com' not found");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while checking admin roles after seed");
            }
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

