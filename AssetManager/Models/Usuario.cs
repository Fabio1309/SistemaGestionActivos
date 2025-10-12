using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models;

public class Usuario : IdentityUser
{
    // --- NUEVOS CAMPOS AÑADIDOS ---

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    [Display(Name = "Fecha de Nacimiento")]
    [DataType(DataType.Date)]
    public DateTime FechaNacimiento { get; set; }
}

