using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaGestionActivos.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace SistemaGestionActivos.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminController : Controller
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<Usuario> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // --- MÉTODO INDEX MEJORADO CON LÓGICA DE FILTROS ---
        public async Task<IActionResult> Index(string searchString, string roleFilter)
        {
            // Preparamos la lista de roles para el menú desplegable del filtro
            ViewData["RolesList"] = new SelectList(await _roleManager.Roles.ToListAsync(), "Name", "Name", roleFilter);
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentRole"] = roleFilter;

            var usersQuery = _userManager.Users.AsQueryable();

            // Filtro por texto de búsqueda (nombre o email)
            if (!string.IsNullOrEmpty(searchString))
            {
                // Protegemos propiedades que pueden ser null usando coalescencia
                usersQuery = usersQuery.Where(u => 
                    (u.NombreCompleto ?? string.Empty).ToLower().Contains(searchString.ToLower()) || 
                    (u.Email ?? string.Empty).ToLower().Contains(searchString.ToLower())
                );
            }

            var users = await usersQuery.ToListAsync();
            var userRolesViewModel = new List<UsuarioConRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                // Filtro por rol (si se ha seleccionado uno)
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

        // --- NUEVA ACCIÓN PARA ACTIVAR/DESACTIVAR USUARIOS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            // Evitar que el admin se desactive a sí mismo
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                // Si no podemos obtener el usuario actual, abortamos la operación de forma segura
                TempData["ErrorMessage"] = "No se pudo verificar el usuario actual.";
                return RedirectToAction(nameof(Index));
            }

            if (user.Id == currentUser.Id)
            {
                TempData["ErrorMessage"] = "No puedes desactivar tu propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            // Cambiar el estado de bloqueo
            if (await _userManager.IsLockedOutAsync(user))
            {
                // Si está bloqueado, lo desbloqueamos
                await _userManager.SetLockoutEndDateAsync(user, null);
                TempData["SuccessMessage"] = $"Usuario '{user.UserName}' ha sido activado.";
            }
            else
            {
                // Si está activo, lo bloqueamos indefinidamente
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                TempData["SuccessMessage"] = $"Usuario '{user.UserName}' ha sido desactivado.";
            }

            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForzarRestablecimiento(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // 1. Genera el token original (como ya lo hacías)
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // 2. ¡NUEVO! Codifica el token a un formato seguro para URL
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

            // 3. ¡MODIFICADO! Usa el token codificado (encodedToken) para crear el enlace
            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { area = "Identity", code = encodedToken }, // ¡OJO! No pasamos el email aquí
                protocol: Request.Scheme);

            TempData["SuccessMessage"] = "Enlace de restablecimiento generado con éxito.";
            TempData["ResetLink"] = resetLink;

            return RedirectToAction(nameof(Index));
        }
        // --- El resto de tus métodos (CrearUsuario, GestionarRoles) se quedan igual ---
        public IActionResult CrearUsuario()
        {
            return View(new CrearUsuarioViewModel());
        }

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

        public async Task<IActionResult> GestionarRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();
            
            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = new GestionarRolesViewModel
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                Roles = allRoles.Select(role => new RoleCheckboxViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name ?? string.Empty,
                    IsSelected = userRoles.Contains(role.Name ?? string.Empty)
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarRoles(GestionarRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var rolesCollection = model.Roles ?? Enumerable.Empty<RoleCheckboxViewModel>();
            var selectedRoles = rolesCollection.Where(r => r.IsSelected).Select(r => r.RoleName ?? string.Empty);

            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
            {
                 return View(model);
            }

            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
             if (!result.Succeeded)
            {
                 return View(model);
            }
            
            TempData["SuccessMessage"] = "Roles actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}