namespace SistemaGestionActivos.Models
{
    // ... (Aquí podrían estar tus otros enums como EstadoActivo)

    public enum EstadoOT
    {
        Abierta,
        Asignada,
        EnProgreso,
        EnEsperaDeRepuesto,
        Resuelta,
        Cerrada
    }

    public enum EstadoActivo
    {
        Disponible, // Era "Operativo"
        Asignado,
        EnMantenimiento, // Era "En Reparación"
        DeBaja
    }

    public enum EstadoDevolucion
    {
        Funcional,
        Dañado
    }
    public enum FrecuenciaMantenimiento
    {
        Diaria,
        Semanal,
        Mensual,
        Anual
    }
}