using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    /// <summary>
    /// Repite en memoria las comprobaciones que el repositorio Oracle hace dentro de su transacción: estado esperado,
    /// una sola orden vigente por caneca, clave e identificador externo únicos y un depósito por orden. Guarda copias.
    /// No prueba el SQL: eso lo cubren las pruebas de integración contra Oracle.
    /// </summary>
    internal sealed class RepositorioInteraccionesEnMemoria : IRepositorioInteracciones
    {
        private readonly Dictionary<int, Interaccion> _interacciones = new Dictionary<int, Interaccion>();
        private readonly Dictionary<int, OrdenApertura> _ordenes = new Dictionary<int, OrdenApertura>();
        private readonly Dictionary<int, EventoDispositivo> _eventos = new Dictionary<int, EventoDispositivo>();
        private readonly Dictionary<int, Deposito> _depositos = new Dictionary<int, Deposito>();
        private int _siguienteId = 1;

        /// <summary>Canecas y materiales con los que se repiten RN-05 y RN-06 al emitir; sin enlazar, no se comprueban.</summary>
        public RepositorioContenedoresEnMemoria? Contenedores { get; set; }
        public RepositorioMaterialesEnMemoria? Materiales { get; set; }

        /// <summary>Se ejecuta una vez justo antes de la próxima escritura: simula otra sesión entre la lectura y la escritura.</summary>
        public Action? AlEscribir { get; set; }

        /// <summary>Si se asigna, la próxima escritura de evento lanza el conflicto de identificador repetido.</summary>
        public bool SimularEventoRepetido { get; set; }

        public int EmisionesIntentadas { get; private set; }

        public IReadOnlyCollection<EventoDispositivo> Eventos => _eventos.Values.Select(Copiar).ToList();
        public IReadOnlyCollection<OrdenApertura> Ordenes => _ordenes.Values.Select(Copiar).ToList();
        public IReadOnlyCollection<Deposito> Depositos => _depositos.Values.Select(Copiar).ToList();

        public Interaccion? LeerInteraccion(int id) => _interacciones.TryGetValue(id, out var i) ? Copiar(i) : null;
        public OrdenApertura? LeerOrden(int id) => _ordenes.TryGetValue(id, out var o) ? Copiar(o) : null;

        /// <summary>Reemplaza la orden guardada, como haría otra sesión, sin pasar por las reglas.</summary>
        public void CambiarOrdenGuardada(int id, Action<OrdenApertura> cambio) => cambio(_ordenes[id]);

        // Interacciones

        public Task<int> AgregarInteraccionAsync(Interaccion interaccion, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            int id = _siguienteId++;
            _interacciones[id] = ConId(interaccion, id);
            return Task.FromResult(id);
        }

        public Task<Interaccion?> ObtenerInteraccionAsync(int idInteraccion, CancellationToken cancellationToken = default) =>
            Task.FromResult(LeerInteraccion(idInteraccion));

        public Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorContenedorAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            ListarInteracciones(i => i.IdContenedor == idContenedor);

        public Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            ListarInteracciones(i => i.IdUsuario == idUsuario);

        public Task ActualizarInteraccionAsync(Interaccion interaccion, EstadoInteraccion estadoEsperado, CancellationToken cancellationToken = default)
        {
            if (interaccion.Estado is not (EstadoInteraccion.Iniciada or EstadoInteraccion.Rechazada))
            {
                throw new ArgumentException("Por aquí solo se guarda una sugerencia o un rechazo.", nameof(interaccion));
            }

            AntesDeEscribir();
            ExigirEstadoInteraccion(interaccion.Id, estadoEsperado);
            _interacciones[interaccion.Id] = ConCantidad(interaccion, _interacciones[interaccion.Id].CantidadDeclarada);
            return Task.CompletedTask;
        }

        public Task<bool> CambiarCantidadDeclaradaAsync(int actor, int id, int? anterior, int nueva, CancellationToken ct = default)
        {
            AntesDeEscribir();
            if (!_interacciones.TryGetValue(id, out var i) || i.IdUsuario != actor || i.Estado != EstadoInteraccion.Iniciada || i.CantidadDeclarada != anterior)
                return Task.FromResult(false);
            _interacciones[id] = new(i.Id, i.IdContenedor, i.IdUsuario, i.Canal, i.Estado, i.IdMaterialSugerido, i.ConfianzaSugerida,
                i.IdMaterialConfirmado, i.IdOperario, i.Motivo, i.FechaInicioUtc, i.FechaCierreUtc, nueva);
            return Task.FromResult(true);
        }

        // Órdenes

        public Task<OrdenApertura?> ObtenerOrdenAsync(int idOrden, CancellationToken cancellationToken = default) =>
            Task.FromResult(LeerOrden(idOrden));

        public Task<OrdenApertura?> ObtenerOrdenPorClaveAsync(string claveIdempotencia, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrdenApertura?>(_ordenes.Values.Where(o => o.ClaveIdempotencia == claveIdempotencia).Select(Copiar).SingleOrDefault());

        public Task<OrdenApertura?> ObtenerOrdenVigenteAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrdenApertura?>(_ordenes.Values.Where(o => o.IdContenedor == idContenedor && o.EsVigente).Select(Copiar).SingleOrDefault());

        public Task<IReadOnlyList<OrdenApertura>> ListarOrdenesVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrdenApertura>>(_ordenes.Values
                .Where(o => o.EstaVencida(ahoraUtc)).OrderBy(o => o.Id).Select(Copiar).ToList());

        public Task<int> EmitirOrdenAsync(Interaccion interaccionAutorizada, OrdenApertura orden, CancellationToken cancellationToken = default)
        {
            EmisionesIntentadas++;
            AntesDeEscribir();
            if (_ordenes.Values.Any(o => o.ClaveIdempotencia == orden.ClaveIdempotencia))
            {
                throw new ConflictoUnicidadException("uq_orden_clave");
            }

            int idMaterial = interaccionAutorizada.IdMaterialConfirmado!.Value;
            if (LeerInteraccion(interaccionAutorizada.Id)?.CantidadDeclarada != interaccionAutorizada.CantidadDeclarada)
                throw new CambioConcurrenteException("La cantidad cambió antes de emitir la orden; vuelve a confirmar.");
            if (Materiales is not null && !Materiales.EstaHabilitado(idMaterial))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.MaterialDeshabilitado(Materiales.NombreDe(idMaterial)));
            }

            if (Contenedores?.Leer(orden.IdContenedor) is { } caneca)
            {
                if (!caneca.EstaEnServicio)
                {
                    throw new CambioConcurrenteException(MensajesInteracciones.CanecaNoEnServicio);
                }

                if (!caneca.AdmiteMaterial(idMaterial))
                {
                    throw new CambioConcurrenteException(MensajesInteracciones.CanecaNoAdmiteMaterial);
                }
            }

            if (_ordenes.Values.Any(o => o.IdContenedor == orden.IdContenedor && o.EsVigente))
            {
                throw new CambioConcurrenteException(MensajesInteracciones.CanecaOcupada);
            }

            if (_interacciones[interaccionAutorizada.Id].Estado != EstadoInteraccion.Iniciada)
            {
                throw new CambioConcurrenteException(MensajesInteracciones.InteraccionNoIniciada);
            }

            int id = _siguienteId++;
            _interacciones[interaccionAutorizada.Id] = Copiar(interaccionAutorizada);
            _ordenes[id] = ConId(orden, id);
            return Task.FromResult(id);
        }

        public Task ActualizarOrdenAsync(CambioOrden cambio, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            AplicarCambio(cambio);
            return Task.CompletedTask;
        }

        // Eventos

        public Task<EventoDispositivo?> ObtenerEventoAsync(int idContenedor, string idExterno, CancellationToken cancellationToken = default) =>
            Task.FromResult<EventoDispositivo?>(_eventos.Values
                .Where(e => e.IdContenedor == idContenedor && e.IdExterno == idExterno).Select(Copiar).SingleOrDefault());

        public Task<int> RegistrarEventoAsync(EventoDispositivo evento, CambioOrden? cambio, Deposito? deposito, CancellationToken cancellationToken = default)
        {
            // Mismas guardas que el repositorio Oracle.
            if (cambio is not null && cambio.Orden.Id != evento.IdOrden
                || cambio?.InteraccionCerrada is { } cerrada && cerrada.Id != cambio.Orden.IdInteraccion
                || deposito is not null && (deposito.IdOrden != evento.IdOrden
                    || evento is not { Tipo: TipoEventoDispositivo.Deteccion, Resultado: ResultadoEvento.Aplicado }))
            {
                throw new ArgumentException("El cambio o el depósito no corresponden al evento.");
            }

            AntesDeEscribir();
            if (SimularEventoRepetido || _eventos.Values.Any(e => e.IdContenedor == evento.IdContenedor && e.IdExterno == evento.IdExterno))
            {
                SimularEventoRepetido = false;
                throw new ConflictoUnicidadException("uq_evento_externo");
            }

            if (cambio is not null)
            {
                ComprobarCambio(cambio);
            }

            if (deposito is not null && _depositos.Values.Any(d => d.IdOrden == deposito.IdOrden))
            {
                throw new CambioConcurrenteException("La orden ya tiene depósito.");
            }

            int idEvento = _siguienteId++;
            _eventos[idEvento] = new EventoDispositivo(idEvento, evento.IdContenedor, evento.IdExterno, evento.Tipo, evento.IdOrden,
                evento.Resultado, evento.FechaDispositivoUtc, evento.FechaRecepcionUtc);
            if (cambio is not null)
            {
                AplicarCambio(cambio);
            }

            if (deposito is not null)
            {
                int idDeposito = _siguienteId++;
                _depositos[idDeposito] = new Deposito(idDeposito, deposito.IdOrden, idEvento, deposito.IdMaterialDeclarado, null, null, null,
                    deposito.PesoMedidoKg, deposito.PesoEstimadoKg, deposito.FechaDeteccionUtc);
            }

            return Task.FromResult(idEvento);
        }

        public Task<IReadOnlyList<EventoDispositivo>> ListarEventosEnRevisionAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EventoDispositivo>>(_eventos.Values
                .Where(e => e.Resultado == ResultadoEvento.Revision).OrderBy(e => e.Id).Select(Copiar).ToList());

        // Depósitos

        public Task<Deposito?> ObtenerDepositoAsync(int idDeposito, CancellationToken cancellationToken = default) =>
            Task.FromResult(_depositos.TryGetValue(idDeposito, out var d) ? Copiar(d) : null);

        public Task<Deposito?> ObtenerDepositoPorOrdenAsync(int idOrden, CancellationToken cancellationToken = default) =>
            Task.FromResult<Deposito?>(_depositos.Values.Where(d => d.IdOrden == idOrden).Select(Copiar).SingleOrDefault());

        public Task<IReadOnlyList<Deposito>> ListarDepositosPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Deposito>>(_depositos.Values
                .Where(d => _interacciones[_ordenes[d.IdOrden].IdInteraccion].IdUsuario == idUsuario)
                .OrderByDescending(d => d.FechaDeteccionUtc).ThenByDescending(d => d.Id).Select(Copiar).ToList());

        public Task<IReadOnlyList<Deposito>> ListarDepositosSinVerificarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Deposito>>(_depositos.Values
                .Where(d => !d.EstaVerificado).OrderBy(d => d.Id).Select(Copiar).ToList());

        public Task VerificarDepositoAsync(Deposito depositoVerificado, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            if (_depositos[depositoVerificado.Id].EstaVerificado)
            {
                throw new CambioConcurrenteException(MensajesInteracciones.DepositoYaVerificado);
            }

            _depositos[depositoVerificado.Id] = Copiar(depositoVerificado);
            return Task.CompletedTask;
        }

        private void ComprobarCambio(CambioOrden cambio)
        {
            if (_ordenes[cambio.Orden.Id].Estado != cambio.EstadoAnterior)
            {
                throw new CambioConcurrenteException("La orden cambió de estado.");
            }

            if (cambio.InteraccionCerrada is { } interaccion)
            {
                ExigirEstadoInteraccion(interaccion.Id, EstadoInteraccion.Autorizada);
            }
        }

        private void AplicarCambio(CambioOrden cambio)
        {
            ComprobarCambio(cambio);
            _ordenes[cambio.Orden.Id] = Copiar(cambio.Orden);
            if (cambio.InteraccionCerrada is { } interaccion)
            {
                _interacciones[interaccion.Id] = Copiar(interaccion);
            }
        }

        private void ExigirEstadoInteraccion(int id, EstadoInteraccion esperado)
        {
            if (_interacciones[id].Estado != esperado)
            {
                throw new CambioConcurrenteException("La interacción cambió de estado.");
            }
        }

        private void AntesDeEscribir()
        {
            Action? accion = AlEscribir;
            AlEscribir = null;
            accion?.Invoke();
        }

        private Task<IReadOnlyList<Interaccion>> ListarInteracciones(Func<Interaccion, bool> filtro) =>
            Task.FromResult<IReadOnlyList<Interaccion>>(_interacciones.Values
                .Where(filtro).OrderByDescending(i => i.FechaInicioUtc).ThenByDescending(i => i.Id).Select(Copiar).ToList());

        private static Interaccion Copiar(Interaccion i) => ConId(i, i.Id);

        private static Interaccion ConCantidad(Interaccion i, int? cantidad) =>
            new(i.Id, i.IdContenedor, i.IdUsuario, i.Canal, i.Estado, i.IdMaterialSugerido, i.ConfianzaSugerida,
                i.IdMaterialConfirmado, i.IdOperario, i.Motivo, i.FechaInicioUtc, i.FechaCierreUtc, cantidad);

        private static Interaccion ConId(Interaccion i, int id) =>
            new(id, i.IdContenedor, i.IdUsuario, i.Canal, i.Estado, i.IdMaterialSugerido, i.ConfianzaSugerida, i.IdMaterialConfirmado,
                i.IdOperario, i.Motivo, i.FechaInicioUtc, i.FechaCierreUtc, i.CantidadDeclarada);

        private static OrdenApertura Copiar(OrdenApertura o) => ConId(o, o.Id);

        private static OrdenApertura ConId(OrdenApertura o, int id) =>
            new(id, o.IdInteraccion, o.IdContenedor, o.ClaveIdempotencia, o.Estado, o.FechaEmisionUtc, o.FechaVencimientoUtc, o.FechaCierreUtc);

        private static EventoDispositivo Copiar(EventoDispositivo e) =>
            new(e.Id, e.IdContenedor, e.IdExterno, e.Tipo, e.IdOrden, e.Resultado, e.FechaDispositivoUtc, e.FechaRecepcionUtc);

        private static Deposito Copiar(Deposito d) =>
            new(d.Id, d.IdOrden, d.IdEvento, d.IdMaterialDeclarado, d.IdMaterialVerificado, d.IdVerificador, d.FechaVerificacionUtc,
                d.PesoMedidoKg, d.PesoEstimadoKg, d.FechaDeteccionUtc, d.CantidadVerificada);
    }
}
