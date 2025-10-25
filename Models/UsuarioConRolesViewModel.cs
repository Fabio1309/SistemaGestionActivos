using System.Collections.Generic;

namespace SistemaGestionActivos.Models
{
    // Esta clase nos ayuda a pasar a la vista tanto el objeto del usuario
    // como la lista de roles que tiene asignados.
    public class UsuarioConRolesViewModel
    {
        public Usuario? Usuario { get; set; }
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
