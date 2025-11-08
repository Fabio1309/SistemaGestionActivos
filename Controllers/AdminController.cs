using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Data;      // <--- 1. AÑADE ESTE USING
using SistemaGestionActivos.Models;
using SistemaGestionActivos.Services; // <--- 1. AÑADE ESTE USING
using System.Security.Claims;       // <--- 1. AÑADE ESTE USING
using System.Linq;
using System.Threading.Tasks;

namespace SistemaGestionActivos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context; // <--- 2. AÑADE ESTE CAMPO
        private readonly ILogService _logService; // <--- 2. AÑADE ESTE CAMPO

        // 3. REEMPLAZA TU CONSTRUCTOR CON ESTE
        public AdminController(
            UserManager<Usuario> userManager, 
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogService logService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _logService = logService;
        }

        // ... (El método Index() se queda igual) ...
        public async Task<IActionResult> Index(string searchString, string roleFilter)
        {
            ViewData["RolesList"] = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", roleFilter);
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentRole"] = roleFilter;

            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                usersQuery = usersQuery.Where(u => 
                    u.NombreCompleto.ToLower().Contains(searchString.ToLower()) || 
                    u.Email.ToLower().Contains(searchString.ToLower())
                );
            }

            var users = await usersQuery.ToListAsync();
            var userRolesViewModel = new List<UsuarioConRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (string.IsNullOrEmpty(roleFilter) || roles.Contains(roleFilter))
                {
                    userRolesViewModel.Add(new UsuarioConRolesViewModel
                    {
                        Usuario = user,
                        Roles = roles
                    });
                }
            }
            
            return View(userRolesViewModel);
        }
        
        // ... (El método ToggleUserStatus() se queda igual) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            // Buscar el usuario objetivo y validar
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            
            // --- REGISTRAR LOG (Ya estaba en el código anterior) ---
            var currentUser = await _userManager.GetUserAsync(User);

            // Comprobar si el usuario está bloqueado actualmente
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            string accionLog;

            if (!isLockedOut)
            {
                // Si no está bloqueado, bloquearlo (desactivar)
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"Usuario '{user.UserName}' ha sido desactivado.";
                accionLog = $"Desactivó al usuario: {user.UserName}";
            }
            else
            {
                // Si está bloqueado, desbloquearlo (activar)
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Usuario '{user.UserName}' ha sido activado.";
                accionLog = $"Activó al usuario: {user.UserName}";
            }
            
            await _logService.RegistrarLogAsync(currentUser.Id, accionLog, "Usuario", user.Email);
            // ----------------------------------------------------

            return RedirectToAction(nameof(Index));
        }

        // ... (El método ForzarRestablecimiento() se queda igual) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForzarRestablecimiento(string userId)
        {
            // ... (código de ForzarRestablecimiento) ...
            var user = await _userManager.FindByIdAsync(userId);
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = System.Text.Encoding.UTF8.GetBytes(token);
            var validToken = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(encodedToken);

            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = validToken },
                protocol: Request.Scheme);
                
            // --- REGISTRAR LOG (Ya estaba en el código anterior) ---
            var adminUserIdReset = _userManager.GetUserId(User);
            await _logService.RegistrarLogAsync(adminUserIdReset, $"Forzó reseteo de contraseña para: {user.UserName}", "Usuario", user.Email);
            // ---------------------

            TempData["SuccessMessage"] = "Enlace de restablecimiento generado con éxito.";
            TempData["ResetLink"] = resetLink; 

            return RedirectToAction(nameof(Index));
        }

        // GET: CrearUsuario
        public IActionResult CrearUsuario()
        {
            return View(new CrearUsuarioViewModel());
        }

        // POST: CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Usuario { 
                    UserName = model.Email, 
                    Email = model.Email,
                    NombreCompleto = model.NombreCompleto,
                    FechaNacimiento = model.FechaNacimiento,
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Empleado");
                    
                    // ===== 4. AÑADE ESTE BLOQUE DE LOG =====
                    var adminUserId = _userManager.GetUserId(User);
                    await _logService.RegistrarLogAsync(
                        adminUserId, 
                        $"Creó el nuevo usuario: {user.UserName}", 
                        "Usuario", 
                        user.Email
                    );
                    // =======================================

                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // ... (El método GestionarRoles() GET se queda igual) ...
        public async Task<IActionResult> GestionarRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            
            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new GestionarRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Roles = allRoles.Select(role => new RoleCheckboxViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name,
                    IsSelected = userRoles.Contains(role.Name)
                }).ToList()
            };
            return View(model);
        }

        // ... (El método GestionarRoles() POST se queda igual) ...
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarRoles(GestionarRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);

            var resultAdd = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            var resultRemove = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));

            if (!resultAdd.Succeeded || !resultRemove.Succeeded)
            {
               return View(model);
            }
            
            // --- REGISTRAR LOG (Ya estaba en el código anterior) ---
            var adminUserId = _userManager.GetUserId(User);
            var rolesComoTexto = string.Join(", ", selectedRoles);
            await _logService.RegistrarLogAsync(adminUserId, $"Cambió los roles de {user.UserName} a: [{rolesComoTexto}]", "Usuario", user.Email);
            // ---------------------
            
            TempData["SuccessMessage"] = "Roles actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/LogAuditoria
        // (Esta acción ya la tenías y está correcta)
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> LogAuditoria()
        {
            var logs = await _context.LogsAuditoria
                .Include(l => l.Usuario) 
                .OrderByDescending(l => l.FechaHora)
                .Take(100) 
                .ToListAsync();
                
            return View(logs);
        }
    }
}