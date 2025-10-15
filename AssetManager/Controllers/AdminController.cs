using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetManager.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AssetManager.Controllers
{
    // Solo los usuarios con el rol "Administrador" pueden acceder a este controlador.
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

        // Muestra la lista de todos los usuarios
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModel = new List<UsuarioConRolesViewModel>();

            foreach (Usuario user in users)
            {
                var viewModel = new UsuarioConRolesViewModel();
                viewModel.Usuario = user;
                viewModel.Roles = await _userManager.GetRolesAsync(user); // Obtenemos los roles para cada usuario
                userRolesViewModel.Add(viewModel);
            }
            return View(userRolesViewModel);
        }
        
        // --- CÓDIGO AÑADIDO ---

        // GET: Admin/CrearUsuario
        // Muestra el formulario para que el admin cree un nuevo usuario.
        public IActionResult CrearUsuario()
        {
            return View();
        }

        // POST: Admin/CrearUsuario
        // Procesa el formulario y crea el nuevo usuario en la base de datos.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new Usuario
                {
                    UserName = model.Email,
                    Email = model.Email,
                    NombreCompleto = model.NombreCompleto,
                    FechaNacimiento = model.FechaNacimiento,
                    EmailConfirmed = true // Se confirma el email directamente
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Por defecto, a todo nuevo usuario se le asigna el rol "Empleado".
                    // El administrador puede cambiarlo después desde "Gestionar Roles".
                    await _userManager.AddToRoleAsync(user, "Empleado");
                    
                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            // Si el modelo no es válido, se vuelve a mostrar el formulario con los errores.
            return View(model);
        }

        // --- FIN DEL CÓDIGO AÑADIDO ---


        // GET: Muestra la página para gestionar los roles de un usuario específico
        public async Task<IActionResult> GestionarRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

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

        // POST: Procesa el formulario y actualiza los roles del usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GestionarRoles(GestionarRolesViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var selectedRoles = model.Roles.Where(r => r.IsSelected).Select(r => r.RoleName);

            // Roles a añadir: los que están seleccionados ahora pero no estaban antes.
            var result = await _userManager.AddToRolesAsync(user, selectedRoles.Except(userRoles));
            if (!result.Succeeded)
            {
                // Manejar error si es necesario
                return View(model);
            }

            // Roles a quitar: los que estaban antes pero ya no están seleccionados.
            result = await _userManager.RemoveFromRolesAsync(user, userRoles.Except(selectedRoles));
            if (!result.Succeeded)
            {
                // Manejar error si es necesario
                return View(model);
            }
            
            TempData["SuccessMessage"] = "Roles actualizados correctamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}

