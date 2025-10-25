using System.ComponentModel.DataAnnotations;

namespace SistemaGestionActivos.Models
{
    public class CrearUsuarioViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime FechaNacimiento { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y como máximo {1} caracteres de longitud.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
