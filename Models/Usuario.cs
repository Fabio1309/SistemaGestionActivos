using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SistemaGestionActivos.Models;

public class Usuario : IdentityUser
{
    

    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [Display(Name = "Nombre Completo")]
    public string NombreCompleto { get; set; }

    [Required(ErrorMessage = "La fecha de nacimiento es obligatoria.")]
    [Display(Name = "Fecha de Nacimiento")]
    [DataType(DataType.Date)]
    public DateTime FechaNacimiento { get; set; }
}

