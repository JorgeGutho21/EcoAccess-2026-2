using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Interacciones, órdenes de apertura, eventos del dispositivo y depósitos (tablas INTERACCION, ORDEN_APERTURA,
    /// EVENTO_DISPOSITIVO y DEPOSITO). Van en un solo repositorio porque sus escrituras se confirman juntas: emitir
    /// una orden autoriza la interacción, y un evento de detección avanza la orden, crea el depósito y cierra la
    /// interacción en la misma transacción.
    /// </summary>
    /// <remarks>
    /// Las escrituras de estado son optimistas: cada una indica el estado que leyó la lógica y no guarda nada si
    /// otra sesión lo cambió. Orden de bloqueo común con los demás repositorios: material, caneca, orden, interacción.
    /// </remarks>
    public interface IRepositorioInteracciones
    {
        /// <summary>Guarda una interacción nueva (iniciada o apertura manual) y devuelve su identificador.</summary>
        /// <exception cref="InvalidOperationException">Si la caneca o el usuario no existen.</exception>
        Task<int> AgregarInteraccionAsync(Interaccion interaccion, CancellationToken cancellationToken = default);

        /// <summary>Devuelve la interacción, o null si no existe.</summary>
        Task<Interaccion?> ObtenerInteraccionAsync(int idInteraccion, CancellationToken cancellationToken = default);

        /// <summary>Interacciones de una caneca, de la más reciente a la más antigua.</summary>
        Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorContenedorAsync(int idContenedor, CancellationToken cancellationToken = default);

        /// <summary>Interacciones iniciadas por un ciudadano, de la más reciente a la más antigua.</summary>
        Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

        /// <summary>Guarda la sugerencia, el rechazo o el cierre de una interacción que sigue en <paramref name="estadoEsperado"/>.</summary>
        /// <exception cref="CambioConcurrenteException">Si la interacción ya no está en ese estado.</exception>
        /// <exception cref="InvalidOperationException">Si la interacción no existe.</exception>
        Task ActualizarInteraccionAsync(Interaccion interaccion, EstadoInteraccion estadoEsperado, CancellationToken cancellationToken = default);

        /// <summary>Cambia solo la declaración de una solicitud propia iniciada si conserva el valor leído; no autoriza ni acredita.</summary>
        Task<bool> CambiarCantidadDeclaradaAsync(int idActor, int idInteraccion, int? anterior, int nueva, CancellationToken ct = default);

        /// <summary>Devuelve la orden, o null si no existe.</summary>
        Task<OrdenApertura?> ObtenerOrdenAsync(int idOrden, CancellationToken cancellationToken = default);

        /// <summary>Devuelve la orden emitida con esa clave de idempotencia, o null si no hay ninguna.</summary>
        Task<OrdenApertura?> ObtenerOrdenPorClaveAsync(string claveIdempotencia, CancellationToken cancellationToken = default);

        /// <summary>Devuelve la orden vigente de la caneca (como máximo una, RN-16), o null si no hay.</summary>
        Task<OrdenApertura?> ObtenerOrdenVigenteAsync(int idContenedor, CancellationToken cancellationToken = default);

        /// <summary>Órdenes vigentes cuyo vencimiento ya llegó.</summary>
        Task<IReadOnlyList<OrdenApertura>> ListarOrdenesVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda la interacción autorizada y su orden nueva en una transacción. Bloquea el material y la caneca y
        /// vuelve a comprobar con los datos confirmados que el material esté habilitado, que la caneca esté en
        /// servicio y lo admita (RN-05, RN-06), y que la interacción siga iniciada. Devuelve el id de la orden.
        /// </summary>
        /// <exception cref="CambioConcurrenteException">
        /// Si alguna de esas reglas ya no se cumple o la caneca ya tiene otra orden vigente (RN-16).
        /// </exception>
        /// <exception cref="ConflictoUnicidadException">Si otra orden ya usa la clave de idempotencia.</exception>
        /// <exception cref="InvalidOperationException">Si la interacción, la caneca o el material no existen.</exception>
        Task<int> EmitirOrdenAsync(Interaccion interaccionAutorizada, OrdenApertura orden, CancellationToken cancellationToken = default);

        /// <summary>Guarda el avance de una orden y, si lo trae, el cierre de su interacción.</summary>
        /// <exception cref="CambioConcurrenteException">Si la orden o la interacción cambiaron desde la lectura.</exception>
        /// <exception cref="InvalidOperationException">Si la orden no existe.</exception>
        Task ActualizarOrdenAsync(CambioOrden cambio, CancellationToken cancellationToken = default);

        /// <summary>Devuelve el evento que la caneca envió con ese identificador externo, o null si es nuevo.</summary>
        Task<EventoDispositivo?> ObtenerEventoAsync(int idContenedor, string idExterno, CancellationToken cancellationToken = default);

        /// <summary>
        /// Guarda un evento nuevo y, en la misma transacción, el avance de la orden y el depósito que produce.
        /// Un evento en revisión se guarda sin cambio ni depósito (RN-09). Devuelve el id del evento.
        /// </summary>
        /// <exception cref="ConflictoUnicidadException">Si la caneca ya envió un evento con ese identificador externo.</exception>
        /// <exception cref="CambioConcurrenteException">Si la orden o la interacción cambiaron desde la lectura; no se guarda nada.</exception>
        Task<int> RegistrarEventoAsync(EventoDispositivo evento, CambioOrden? cambio, Deposito? deposito, CancellationToken cancellationToken = default);

        /// <summary>Eventos marcados para revisión, del más reciente al más antiguo.</summary>
        Task<IReadOnlyList<EventoDispositivo>> ListarEventosEnRevisionAsync(CancellationToken cancellationToken = default);

        /// <summary>Devuelve el depósito, o null si no existe.</summary>
        Task<Deposito?> ObtenerDepositoAsync(int idDeposito, CancellationToken cancellationToken = default);

        /// <summary>Devuelve el depósito de la orden (como máximo uno, RN-17), o null si no hubo.</summary>
        Task<Deposito?> ObtenerDepositoPorOrdenAsync(int idOrden, CancellationToken cancellationToken = default);

        /// <summary>Depósitos de las interacciones de un ciudadano, del más reciente al más antiguo.</summary>
        Task<IReadOnlyList<Deposito>> ListarDepositosPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default);

        /// <summary>Depósitos sin verificar, del más antiguo al más reciente.</summary>
        Task<IReadOnlyList<Deposito>> ListarDepositosSinVerificarAsync(CancellationToken cancellationToken = default);

        /// <summary>Guarda la verificación de un depósito que seguía sin verificar.</summary>
        /// <exception cref="CambioConcurrenteException">Si otra persona ya lo verificó.</exception>
        /// <exception cref="InvalidOperationException">Si el depósito o el material no existen.</exception>
        Task VerificarDepositoAsync(Deposito depositoVerificado, CancellationToken cancellationToken = default);
    }
}
