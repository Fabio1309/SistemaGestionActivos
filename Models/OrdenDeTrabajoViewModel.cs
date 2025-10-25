using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace SistemaGestionActivos.Models
{
    public class OrdenDeTrabajoViewModel
    {
        public List<OrdenDeTrabajo> OrdenesDeTrabajo { get; set; } = new List<OrdenDeTrabajo>();
        public SelectList? TecnicosDisponibles { get; set; }
    }
}