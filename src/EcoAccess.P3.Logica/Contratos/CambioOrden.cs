using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Avance de una orden ya decidido por la lógica. El repositorio lo guarda solo si la orden sigue en
    /// <paramref name="EstadoAnterior"/>; si otra sesión la cambió antes, lanza <see cref="CambioConcurrenteException"/>.
    /// </summary>
    /// <param name="Orden">Orden con el estado nuevo.</param>
    /// <param name="EstadoAnterior">Estado leído antes de decidir el avance.</param>
    /// <param name="InteraccionCerrada">Interacción que se cierra con este avance; se guarda si sigue autorizada.</param>
    public sealed record CambioOrden(OrdenApertura Orden, EstadoOrdenApertura EstadoAnterior, Interaccion? InteraccionCerrada = null);
}
