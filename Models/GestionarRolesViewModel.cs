namespace SistemaGestionActivos.Models
{
    public class GestionarRolesViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public List<RoleCheckboxViewModel> Roles { get; set; }
    }
}