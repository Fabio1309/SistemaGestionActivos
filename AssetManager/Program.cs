using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using AssetManager.Data;

var builder = WebApplication.CreateBuilder(args);

// --- INICIO: CONFIGURACIÓN DE LA BASE DE DATOS ---

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
    
// --- FIN: CONFIGURACIÓN DE LA BASE DE DATOS ---


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Quick development helper: ensure database is created to avoid runtime errors
// caused by missing tables (useful when migrations are empty or DB file is missing schema).
// In production prefer applying migrations via `dotnet ef database update` or proper migration flow.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AssetManager.Data.ApplicationDbContext>();
    db.Database.EnsureCreated();
}

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

app.Run();