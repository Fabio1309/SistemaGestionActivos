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

// --- INICIO: CÓDIGO DE DIAGNÓSTICO TEMPORAL ---
// Este bloque nos dirá si la variable de entorno de Render se está leyendo correctamente.
using var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
var logger = loggerFactory.CreateLogger<Program>();

var connectionStringFromConfig = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionStringFromConfig))
{
    logger.LogWarning("ALERTA: La cadena de conexión 'DefaultConnection' es NULA o VACÍA.");
}
else
{
    // No imprimimos la cadena completa por seguridad, solo una confirmación.
    logger.LogInformation("ÉXITO: Cadena de conexión 'DefaultConnection' encontrada. Longitud: {Length}", connectionStringFromConfig.Length);
}
// --- FIN: CÓDIGO DE DIAGNÓSTICO TEMPORAL ---


// --- 1. CONFIGURACIÓN DE LA BASE DE DATOS (MODIFICADO PARA RENDER) ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// Configuramos Entity Framework Core para que use siempre PostgreSQL (Npgsql).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// --- 2. AÑADIR Y CONFIGURAR EL SERVICIO DE IDENTITY ---
builder.Services.AddDefaultIdentity<Usuario>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddHostedService<MantenimientoSchedulerService>();
builder.Services.AddScoped<ILogService, LogService>();

var modelPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "models", "Model.zip");
if (File.Exists(modelPath))
{
    builder.Services.AddPredictionEnginePool<TicketDataML, TicketPredictionML>()
        .FromFile(modelName: "mlModel", filePath: modelPath, watchForChanges: false);
}
else
{
    Console.WriteLine($"Warning: Model.zip no encontrado en {modelPath}. PredictionEnginePool no será registrado.");
}

builder.Services.AddSingleton<SistemaGestionActivos.Services.ICategoryPredictionService, SistemaGestionActivos.Services.CategoryPredictionService>();
builder.Services.AddKernel();
builder.Services.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-pro-latest",
    apiKey: builder.Configuration["GoogleAI:ApiKey"]
);
builder.Services.AddScoped<OrdenDeTrabajoPlugin>();

var app = builder.Build();

// --- INICIO: APLICAR MIGRACIONES AUTOMÁTICAMENTE ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var migrationLogger = services.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogError(ex, "Ocurrió un error al intentar migrar la base de datos en el arranque.");
    }
}
// --- FIN: APLICAR MIGRACIONES AUTOMÁTICAMENTE ---

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

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
        
        await SeedRolesAndAdminAsync(roleManager, userManager);
            var seedLogger = services.GetRequiredService<ILogger<Program>>();
            try
            {
                var admin = await userManager.FindByEmailAsync("admin@activosys.com");
                if (admin != null)
                {
                    var adminRoles = await userManager.GetRolesAsync(admin);
                    seedLogger.LogInformation("Seed check: admin '{Email}' roles: {Roles}", admin.Email, string.Join(',', adminRoles));
                    if (!adminRoles.Contains("Administrador"))
                    {
                        var addResult = await userManager.AddToRoleAsync(admin, "Administrador");
                        if (addResult.Succeeded)
                        {
                            seedLogger.LogInformation("Seed fix: admin '{Email}' fue agregado al rol 'Administrador'", admin.Email);
                        }
                        else
                        {
                            seedLogger.LogWarning("Seed fix: no se pudo agregar el admin '{Email}' al rol 'Administrador': {Errors}", admin.Email, string.Join(';', addResult.Errors.Select(e => e.Description)));
                        }
                    }

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
                            seedLogger.LogInformation("DB check: admin '{Email}' roles in AspNetUserRoles: {DBRoles}", admin.Email, string.Join(',', roleNames));
                        }
                        else
                        {
                            seedLogger.LogInformation("DB check: admin '{Email}' no tiene entradas en AspNetUserRoles", admin.Email);
                        }
                    }
                    catch (Exception ex)
                    {
                        seedLogger.LogError(ex, "Error al consultar AspNetUserRoles para admin");
                    }
                }
                else
                {
                    seedLogger.LogWarning("Seed check: admin user 'admin@activosys.com' not found");
                }
            }
            catch (Exception ex)
            {
                seedLogger.LogError(ex, "Error while checking admin roles after seed");
            }
    }
    catch (Exception ex)
    {
        var seedLogger = services.GetRequiredService<ILogger<Program>>();
        seedLogger.LogError(ex, "Ocurrió un error durante la siembra de datos.");
    }
}

async Task SeedRolesAndAdminAsync(RoleManager<IdentityRole> roleManager, UserManager<Usuario> userManager)
{
    string[] roleNames = { "Administrador", "Gestor de Activos", "Técnico", "Empleado" };
    IdentityResult roleResult;

    foreach (var roleName in roleNames)
    {
        var roleExist = await roleManager.RoleExistsAsync(roleName);
        if (!roleExist)
        {
            roleResult = await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    var adminUser = await userManager.FindByEmailAsync("admin@activosys.com");
    if (adminUser == null)
    {
        var newAdminUser = new Usuario
        {
            UserName = "admin@activosys.com",
            Email = "admin@activosys.com",
            NombreCompleto = "Admin Principal",
            FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EmailConfirmed = true 
        };
        var result = await userManager.CreateAsync(newAdminUser, "Admin123*"); 
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(newAdminUser, "Administrador");
        }
    }
}

app.Run();