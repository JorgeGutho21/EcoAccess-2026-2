using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using System.Globalization;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>
    /// Interacciones, órdenes de apertura, eventos del dispositivo y depósitos (V003). Las escrituras de estado son
    /// optimistas: cada UPDATE lleva el estado leído en el WHERE y, si no toca ninguna fila, se averigua si la fila
    /// falta (<see cref="InvalidOperationException"/>) o cambió (<see cref="CambioConcurrenteException"/>).
    /// Emisión: material → ciudadano → caneca → interacción. Verificación: material → personal → depósito.
    /// </summary>
    public sealed class RepositorioInteraccionesOracle : IRepositorioInteracciones
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioInteraccionesOracle(FabricaConexionesOracle fabrica, TimeProvider? reloj = null)
        {
            _fabrica = fabrica;
            _reloj = reloj ?? TimeProvider.System;
        }

        private readonly TimeProvider _reloj;

        private const string ColumnasInteraccion =
            "i.id_interaccion, i.id_contenedor, i.id_usuario, i.canal, i.estado, i.id_material_sugerido, i.confianza_sugerida, " +
            "i.id_material_confirmado, i.id_operario, i.motivo, i.fecha_inicio_utc, i.fecha_cierre_utc, i.cantidad_declarada";

        private const string ColumnasOrden =
            "o.id_orden, o.id_interaccion, o.id_contenedor, o.clave_idempotencia, o.estado, o.fecha_emision_utc, " +
            "o.fecha_vencimiento_utc, o.fecha_cierre_utc";

        private const string ColumnasEvento =
            "e.id_evento, e.id_contenedor, e.id_externo, e.tipo, e.id_orden, e.resultado, e.fecha_dispositivo_utc, e.fecha_recepcion_utc";

        private const string ColumnasDeposito =
            "d.id_deposito, d.id_orden, d.id_evento, d.id_material_declarado, d.id_material_verificado, d.id_verificador, " +
            "d.fecha_verificacion_utc, d.peso_medido_kg, d.peso_estimado_kg, d.fecha_deteccion_utc, d.cantidad_verificada, d.sin_sensor";

        // Vigente = Emitida a Detectada; el índice único uq_orden_vigente usa el mismo rango.
        private const string CondicionVigente = "o.estado BETWEEN 1 AND 5";

        private const string SqlActualizarInteraccion =
            "UPDATE interaccion SET estado = :estado, id_material_sugerido = :sugerido, confianza_sugerida = :confianza, " +
            "id_material_confirmado = :confirmado, motivo = :motivo, fecha_cierre_utc = :cierre " +
            "WHERE id_interaccion = :id";

        // Interacciones

        public async Task<int> AgregarInteraccionAsync(Interaccion interaccion, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(interaccion, nameof(interaccion));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO interaccion (id_contenedor, id_usuario, canal, estado, id_material_sugerido, confianza_sugerida, " +
                "id_material_confirmado, id_operario, motivo, fecha_inicio_utc, fecha_cierre_utc, cantidad_declarada) " +
                "VALUES (:contenedor, :usuario, :canal, :estado, :sugerido, :confianza, :confirmado, :operario, :motivo, :inicio, :cierre, :cantidad) " +
                "RETURNING id_interaccion INTO :id");
            comando.Agregar("contenedor", OracleDbType.Int32, interaccion.IdContenedor);
            comando.Agregar("usuario", OracleDbType.Int32, interaccion.IdUsuario);
            comando.Agregar("canal", OracleDbType.Int32, (int)interaccion.Canal);
            comando.Agregar("cantidad", OracleDbType.Int32, interaccion.CantidadDeclarada);
            comando.Agregar("estado", OracleDbType.Int32, (int)interaccion.Estado);
            comando.Agregar("sugerido", OracleDbType.Int32, interaccion.IdMaterialSugerido);
            comando.Agregar("confianza", OracleDbType.Decimal, interaccion.ConfianzaSugerida);
            comando.Agregar("confirmado", OracleDbType.Int32, interaccion.IdMaterialConfirmado);
            comando.Agregar("operario", OracleDbType.Int32, interaccion.IdOperario);
            comando.Agregar("motivo", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(interaccion.Motivo));
            comando.Agregar("inicio", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(interaccion.FechaInicioUtc));
            comando.Agregar("cierre", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(interaccion.FechaCierreUtc));
            OracleParameter parametroId = comando.AgregarIdSalida();

            await TraductorErroresOracle.TraducirReferenciaAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            return ComandosOracle.LeerId(parametroId);
        }

        public async Task<Interaccion?> ObtenerInteraccionAsync(int idInteraccion, CancellationToken cancellationToken = default) =>
            Primero(await ConsultarAsync($"SELECT {ColumnasInteraccion} FROM interaccion i WHERE i.id_interaccion = :valor",
                LeerInteraccion, idInteraccion, cancellationToken).ConfigureAwait(false));

        public Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorContenedorAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasInteraccion} FROM interaccion i WHERE i.id_contenedor = :valor " +
                "ORDER BY i.fecha_inicio_utc DESC, i.id_interaccion DESC", LeerInteraccion, idContenedor, cancellationToken);

        public Task<IReadOnlyList<Interaccion>> ListarInteraccionesPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasInteraccion} FROM interaccion i WHERE i.id_usuario = :valor " +
                "ORDER BY i.fecha_inicio_utc DESC, i.id_interaccion DESC", LeerInteraccion, idUsuario, cancellationToken);

        public async Task ActualizarInteraccionAsync(Interaccion interaccion, EstadoInteraccion estadoEsperado, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(interaccion, nameof(interaccion));
            // Autorizar y cerrar tienen sus propias operaciones, que mueven la orden en la misma transacción.
            if (interaccion.Estado is not (EstadoInteraccion.Iniciada or EstadoInteraccion.Rechazada))
            {
                throw new ArgumentException("Por aquí solo se guarda una sugerencia o un rechazo.", nameof(interaccion));
            }

            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await GuardarInteraccionAsync(conexion, transaccion, interaccion, estadoEsperado, cancellationToken).ConfigureAwait(false);
            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> CambiarCantidadDeclaradaAsync(int idActor, int idInteraccion, int? anterior, int nueva, CancellationToken ct = default)
        {
            Interaccion.ValidarCantidad(nueva);
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(ct);
            if (await BloquearInteraccionAsync(conexion, transaccion, idInteraccion, ct) != EstadoInteraccion.Iniciada)
                return false;
            using var cmd = ComandosOracle.Crear(conexion,
                "UPDATE interaccion SET cantidad_declarada=:nueva WHERE id_interaccion=:id AND id_usuario=:actor " +
                "AND estado=1 AND NVL(cantidad_declarada,-1)=NVL(:anterior,-1)", transaccion);
            cmd.CommandTimeout = 15;
            cmd.Agregar("nueva", OracleDbType.Int32, nueva); cmd.Agregar("id", OracleDbType.Int32, idInteraccion);
            cmd.Agregar("actor", OracleDbType.Int32, idActor); cmd.Agregar("anterior", OracleDbType.Int32, anterior);
            int filas = await cmd.ExecuteNonQueryAsync(ct);
            await transaccion.CommitAsync(ct);
            return filas == 1;
        }

        // Órdenes

        public async Task<OrdenApertura?> ObtenerOrdenAsync(int idOrden, CancellationToken cancellationToken = default) =>
            Primero(await ConsultarAsync($"SELECT {ColumnasOrden} FROM orden_apertura o WHERE o.id_orden = :valor",
                LeerOrden, idOrden, cancellationToken).ConfigureAwait(false));

        public async Task<OrdenApertura?> ObtenerOrdenPorClaveAsync(string claveIdempotencia, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(claveIdempotencia, nameof(claveIdempotencia));
            return Primero(await ConsultarAsync($"SELECT {ColumnasOrden} FROM orden_apertura o WHERE o.clave_idempotencia = :valor",
                LeerOrden, claveIdempotencia, cancellationToken).ConfigureAwait(false));
        }

        public async Task<OrdenApertura?> ObtenerOrdenVigenteAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            Primero(await ConsultarAsync($"SELECT {ColumnasOrden} FROM orden_apertura o WHERE o.id_contenedor = :valor AND {CondicionVigente}",
                LeerOrden, idContenedor, cancellationToken).ConfigureAwait(false));

        public Task<IReadOnlyList<OrdenApertura>> ListarOrdenesVencidasAsync(DateTime ahoraUtc, CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasOrden} FROM orden_apertura o WHERE {CondicionVigente} AND o.fecha_vencimiento_utc <= :valor " +
                "ORDER BY o.id_orden", LeerOrden, ConversionOracle.ParaColumnaUtc(ahoraUtc), cancellationToken);

        public async Task<int> EmitirOrdenAsync(Interaccion interaccionAutorizada, OrdenApertura orden, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(interaccionAutorizada, nameof(interaccionAutorizada));
            Compat.ThrowIfNull(orden, nameof(orden));
            int idMaterial = interaccionAutorizada.IdMaterialConfirmado
                ?? throw new ArgumentException("La interacción autorizada debe traer el material confirmado.", nameof(interaccionAutorizada));
            if (orden.IdInteraccion != interaccionAutorizada.Id || orden.IdContenedor != interaccionAutorizada.IdContenedor)
            {
                throw new ArgumentException("La orden debe ser de la interacción y de su caneca.", nameof(orden));
            }

            if (orden.Estado != EstadoOrdenApertura.Emitida)
            {
                throw new ArgumentException("Una orden nueva se guarda emitida.", nameof(orden));
            }

            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // La clave repetida se informa antes que las reglas, para que un reintento del emisor reciba siempre el
            // mismo conflicto; el INSERT la vuelve a comprobar con uq_orden_clave si dos reintentos se cruzan.
            if (await ContarAsync(conexion, transaccion, "SELECT COUNT(*) FROM orden_apertura WHERE clave_idempotencia = :valor",
                    orden.ClaveIdempotencia, cancellationToken).ConfigureAwait(false) > 0)
            {
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new ConflictoUnicidadException("uq_orden_clave");
            }

            await BloquearMaterialHabilitadoAsync(conexion, transaccion, idMaterial, cancellationToken).ConfigureAwait(false);
            // Sin titular es un visitante: solo se admite por el canal Simulador (caneca DEMO) y no hay ciudadano que bloquear.
            int? idActor = interaccionAutorizada.IdUsuario;
            if (idActor is null && interaccionAutorizada.Canal is not (CanalInteraccion.Simulador or CanalInteraccion.Telegram or CanalInteraccion.TelegramSimulado))
                throw new ArgumentException("Una solicitud sin ciudadano requiere un canal de invitado admitido.", nameof(interaccionAutorizada));
            if (idActor is { } titular)
            {
                await BloquearEstadoAsync(conexion, transaccion,
                    "SELECT id_usuario FROM usuario WHERE id_usuario = :id FOR UPDATE WAIT 10", "usuario", titular, cancellationToken)
                    .ConfigureAwait(false);
                int habilitado = await ContarAsync(conexion, transaccion,
                    "SELECT COUNT(*) FROM usuario u WHERE u.id_usuario = :valor AND u.activo = 1 " +
                    "AND u.version_consentimiento IS NOT NULL AND u.fecha_consentimiento_utc IS NOT NULL " +
                    "AND EXISTS(SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol " +
                    "WHERE ur.id_usuario = u.id_usuario AND r.codigo = 1)", titular, cancellationToken).ConfigureAwait(false);
                if (habilitado == 0)
                {
                    await RechazarAsync(transaccion, "El ciudadano ya no está habilitado o retiró su consentimiento.").ConfigureAwait(false);
                }
            }

            if (idActor is null && interaccionAutorizada.Canal is CanalInteraccion.Telegram or CanalInteraccion.TelegramSimulado)
            {
                // La sesión debe existir en Oracle, no solo en un objeto del bot. Se repite antes de emitir.
                int sesiones = await ContarAsync(conexion, transaccion,
                    "SELECT COUNT(*) FROM conversacion_telegram WHERE id_interaccion = :valor " +
                    "AND chat_id = telegram_user_id AND telegram_user_id > 0", interaccionAutorizada.Id, cancellationToken).ConfigureAwait(false);
                if (sesiones != 1)
                    await RechazarAsync(transaccion, "La solicitud invitada no tiene una sesión privada vigente.").ConfigureAwait(false);
            }

            EstadoContenedor estadoCaneca = await BloquearCanecaAsync(conexion, transaccion, orden.IdContenedor, cancellationToken).ConfigureAwait(false);
            if (estadoCaneca != EstadoContenedor.EnServicio)
            {
                await RechazarAsync(transaccion, MensajesInteracciones.CanecaNoEnServicio).ConfigureAwait(false);
            }

            using (OracleCommand admite = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM contenedor_material WHERE id_contenedor = :id AND id_material = :material", transaccion))
            {
                admite.Agregar("id", OracleDbType.Int32, orden.IdContenedor);
                admite.Agregar("material", OracleDbType.Int32, idMaterial);
                if (ConversionOracle.Entero((await admite.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!) == 0)
                {
                    await RechazarAsync(transaccion, MensajesInteracciones.CanecaNoAdmiteMaterial).ConfigureAwait(false);
                }
            }

            // Con la caneca bloqueada ninguna otra emisión para ella avanza hasta el commit, así que esta lectura es firme.
            if (await ContarAsync(conexion, transaccion, $"SELECT COUNT(*) FROM orden_apertura o WHERE o.id_contenedor = :valor AND {CondicionVigente}",
                    orden.IdContenedor, cancellationToken).ConfigureAwait(false) > 0)
            {
                await RechazarAsync(transaccion, MensajesInteracciones.CanecaOcupada).ConfigureAwait(false);
            }

            EstadoInteraccion estadoInteraccion = await BloquearInteraccionAsync(conexion, transaccion, interaccionAutorizada.Id, cancellationToken)
                .ConfigureAwait(false);
            if (estadoInteraccion != EstadoInteraccion.Iniciada)
            {
                await RechazarAsync(transaccion, MensajesInteracciones.InteraccionNoIniciada).ConfigureAwait(false);
            }

            // Leer la titularidad y fecha persistidas DESPUÉS de los bloqueos; el objeto previo no es autoridad.
            using (OracleCommand leer = ComandosOracle.Crear(conexion,
                "SELECT id_usuario, id_contenedor, fecha_inicio_utc, canal FROM interaccion WHERE id_interaccion = :id", transaccion))
            {
                leer.Agregar("id", OracleDbType.Int32, interaccionAutorizada.Id);
                using DbDataReader lector = await leer.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("La solicitud ya no existe.");
                bool titularCorrecto = idActor is { } esperado
                    ? !lector.IsDBNull(0) && lector.GetInt32(0) == esperado
                    : lector.IsDBNull(0) && ConversionOracle.Entero(lector.GetValue(3)) == (int)interaccionAutorizada.Canal;
                if (!titularCorrecto || lector.GetInt32(1) != orden.IdContenedor)
                    await RechazarAsync(transaccion, "La solicitud no pertenece al ciudadano o a la caneca indicados.").ConfigureAwait(false);
                DateTime inicio = ConversionOracle.FechaUtc(lector.GetValue(2));
                DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
                if (!PoliticaVigenciaSolicitud.EstaVigente(inicio, ahora))
                    await RechazarAsync(transaccion, MensajesInteracciones.SolicitudVencida).ConfigureAwait(false);
                if (orden.EstaVencida(ahora))
                    await RechazarAsync(transaccion, MensajesInteracciones.OrdenNoVigente).ConfigureAwait(false);
            }

            await GuardarInteraccionAsync(conexion, transaccion, interaccionAutorizada, EstadoInteraccion.Iniciada, cancellationToken, exigirCantidad: true).ConfigureAwait(false);

            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO orden_apertura (id_interaccion, id_contenedor, clave_idempotencia, estado, fecha_emision_utc, fecha_vencimiento_utc) " +
                "VALUES (:interaccion, :contenedor, :clave, :estado, :emision, :vencimiento) RETURNING id_orden INTO :id", transaccion);
            comando.Agregar("interaccion", OracleDbType.Int32, orden.IdInteraccion);
            comando.Agregar("contenedor", OracleDbType.Int32, orden.IdContenedor);
            comando.Agregar("clave", OracleDbType.Varchar2, orden.ClaveIdempotencia);
            comando.Agregar("estado", OracleDbType.Int32, (int)orden.Estado);
            comando.Agregar("emision", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(orden.FechaEmisionUtc));
            comando.Agregar("vencimiento", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(orden.FechaVencimientoUtc));
            OracleParameter parametroId = comando.AgregarIdSalida();
            try
            {
                await TraductorErroresOracle.TraducirReferenciaAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            }
            catch (ConflictoUnicidadException ex) when (ex.Message == "uq_orden_vigente")
            {
                // Respaldo del bloqueo de la caneca: el índice garantiza RN-16 aunque alguien escriba sin pasar por aquí.
                await RechazarAsync(transaccion, MensajesInteracciones.CanecaOcupada).ConfigureAwait(false);
            }

            int id = ComandosOracle.LeerId(parametroId);
            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return id;
        }

        public async Task ActualizarOrdenAsync(CambioOrden cambio, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(cambio, nameof(cambio));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await GuardarCambioAsync(conexion, transaccion, cambio, cancellationToken).ConfigureAwait(false);
            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Eventos

        public async Task<EventoDispositivo?> ObtenerEventoAsync(int idContenedor, string idExterno, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(idExterno, nameof(idExterno));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {ColumnasEvento} FROM evento_dispositivo e WHERE e.id_contenedor = :contenedor AND e.id_externo = :externo");
            comando.Agregar("contenedor", OracleDbType.Int32, idContenedor);
            comando.Agregar("externo", OracleDbType.Varchar2, idExterno);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? LeerEvento(lector) : null;
        }

        public async Task<int> RegistrarEventoAsync(EventoDispositivo evento, CambioOrden? cambio, Deposito? deposito, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(evento, nameof(evento));
            ExigirCoherencia(evento, cambio, deposito);
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // El evento va primero: una retransmisión choca con uq_evento_externo antes de tocar la orden.
            int idEvento;
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO evento_dispositivo (id_contenedor, id_externo, tipo, id_orden, resultado, fecha_dispositivo_utc, fecha_recepcion_utc) " +
                "VALUES (:contenedor, :externo, :tipo, :orden, :resultado, :dispositivo, :recepcion) RETURNING id_evento INTO :id", transaccion))
            {
                comando.Agregar("contenedor", OracleDbType.Int32, evento.IdContenedor);
                comando.Agregar("externo", OracleDbType.Varchar2, evento.IdExterno);
                comando.Agregar("tipo", OracleDbType.Int32, (int)evento.Tipo);
                comando.Agregar("orden", OracleDbType.Int32, evento.IdOrden);
                comando.Agregar("resultado", OracleDbType.Int32, (int)evento.Resultado);
                comando.Agregar("dispositivo", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(evento.FechaDispositivoUtc));
                comando.Agregar("recepcion", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(evento.FechaRecepcionUtc));
                OracleParameter parametroId = comando.AgregarIdSalida();
                await TraductorErroresOracle.TraducirReferenciaAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
                idEvento = ComandosOracle.LeerId(parametroId);
            }

            if (cambio is not null)
            {
                await GuardarCambioAsync(conexion, transaccion, cambio, cancellationToken).ConfigureAwait(false);
            }

            if (deposito is not null)
            {
                using OracleCommand comando = ComandosOracle.Crear(conexion,
                    "INSERT INTO deposito (id_orden, id_evento, id_material_declarado, peso_medido_kg, peso_estimado_kg, fecha_deteccion_utc, sin_sensor) " +
                    "VALUES (:orden, :evento, :declarado, :medido, :estimado, :deteccion, :sinsensor)", transaccion);
                comando.Agregar("orden", OracleDbType.Int32, deposito.IdOrden);
                comando.Agregar("evento", OracleDbType.Int32, idEvento);
                comando.Agregar("declarado", OracleDbType.Int32, deposito.IdMaterialDeclarado);
                comando.Agregar("medido", OracleDbType.Decimal, deposito.PesoMedidoKg);
                comando.Agregar("estimado", OracleDbType.Decimal, deposito.PesoEstimadoKg);
                comando.Agregar("deteccion", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(deposito.FechaDeteccionUtc));
                comando.Agregar("sinsensor", OracleDbType.Int32, deposito.SinSensor ? 1 : 0);
                try
                {
                    await TraductorErroresOracle.TraducirReferenciaAsync(
                        () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
                }
                catch (ConflictoUnicidadException ex) when (ex.Message == "uq_deposito_orden")
                {
                    // RN-17: otra detección ya dejó el depósito de esta orden; no se guarda tampoco el evento.
                    await RechazarAsync(transaccion, "La orden ya tiene depósito.").ConfigureAwait(false);
                }
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return idEvento;
        }

        public Task<IReadOnlyList<EventoDispositivo>> ListarEventosEnRevisionAsync(CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasEvento} FROM evento_dispositivo e WHERE e.resultado = :valor " +
                "ORDER BY e.fecha_recepcion_utc DESC, e.id_evento DESC", LeerEvento, (int)ResultadoEvento.Revision, cancellationToken);

        // Depósitos

        public async Task<Deposito?> ObtenerDepositoAsync(int idDeposito, CancellationToken cancellationToken = default) =>
            Primero(await ConsultarAsync($"SELECT {ColumnasDeposito} FROM deposito d WHERE d.id_deposito = :valor",
                LeerDeposito, idDeposito, cancellationToken).ConfigureAwait(false));

        public async Task<Deposito?> ObtenerDepositoPorOrdenAsync(int idOrden, CancellationToken cancellationToken = default) =>
            Primero(await ConsultarAsync($"SELECT {ColumnasDeposito} FROM deposito d WHERE d.id_orden = :valor",
                LeerDeposito, idOrden, cancellationToken).ConfigureAwait(false));

        public Task<IReadOnlyList<Deposito>> ListarDepositosPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasDeposito} FROM deposito d " +
                "JOIN orden_apertura o ON o.id_orden = d.id_orden JOIN interaccion i ON i.id_interaccion = o.id_interaccion " +
                "WHERE i.id_usuario = :valor ORDER BY d.fecha_deteccion_utc DESC, d.id_deposito DESC",
                LeerDeposito, idUsuario, cancellationToken);

        public Task<IReadOnlyList<Deposito>> ListarDepositosSinVerificarAsync(CancellationToken cancellationToken = default) =>
            ConsultarAsync($"SELECT {ColumnasDeposito} FROM deposito d WHERE d.id_material_verificado IS NULL ORDER BY d.id_deposito",
                LeerDeposito, null, cancellationToken);

        public async Task VerificarDepositoAsync(Deposito depositoVerificado, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(depositoVerificado, nameof(depositoVerificado));
            if (!depositoVerificado.EstaVerificado)
            {
                throw new ArgumentException("El depósito debe traer la verificación.", nameof(depositoVerificado));
            }

            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            // Igual que en el libro de ecopuntos: material → actor → depósito. El material puede estar deshabilitado:
            // verificar un hecho histórico no autoriza una nueva apertura ni altera el catálogo.
            await BloquearEstadoAsync(conexion, transaccion,
                "SELECT id_material FROM material WHERE id_material = :id FOR UPDATE WAIT 10", "material",
                depositoVerificado.IdMaterialVerificado!.Value, cancellationToken).ConfigureAwait(false);
            int idActor = depositoVerificado.IdVerificador!.Value;
            await BloquearEstadoAsync(conexion, transaccion,
                "SELECT id_usuario FROM usuario WHERE id_usuario = :id FOR UPDATE WAIT 10", "usuario", idActor, cancellationToken)
                .ConfigureAwait(false);
            // Nueva sentencia después de obtener el bloqueo: no reutilizar una autorización leída antes de esperar.
            int activo = await ContarAsync(conexion, transaccion,
                "SELECT activo FROM usuario WHERE id_usuario = :valor", idActor, cancellationToken).ConfigureAwait(false);
            int roles = await ContarAsync(conexion, transaccion,
                "SELECT COUNT(*) FROM usuario_rol ur JOIN rol r ON r.id_rol = ur.id_rol " +
                "WHERE ur.id_usuario = :valor AND r.codigo IN (2,3)", idActor, cancellationToken).ConfigureAwait(false);
            if (activo != 1 || roles == 0)
            {
                await RechazarAsync(transaccion, "La cuenta ya no está activa o no tiene el rol necesario para verificar.").ConfigureAwait(false);
            }
            await BloquearEstadoAsync(conexion, transaccion,
                "SELECT id_deposito FROM deposito WHERE id_deposito = :id FOR UPDATE WAIT 10", "depósito",
                depositoVerificado.Id, cancellationToken).ConfigureAwait(false);
            int pendiente = await ContarAsync(conexion, transaccion,
                "SELECT COUNT(*) FROM deposito WHERE id_deposito = :valor AND id_material_verificado IS NULL",
                depositoVerificado.Id, cancellationToken).ConfigureAwait(false);
            if (pendiente == 0)
            {
                await RechazarAsync(transaccion, MensajesInteracciones.DepositoYaVerificado).ConfigureAwait(false);
            }
            using (OracleCommand titular = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM deposito d JOIN orden_apertura o ON o.id_orden = d.id_orden " +
                "JOIN interaccion i ON i.id_interaccion = o.id_interaccion " +
                "WHERE d.id_deposito = :deposito AND i.id_usuario = :actor", transaccion))
            {
                titular.Agregar("deposito", OracleDbType.Int32, depositoVerificado.Id);
                titular.Agregar("actor", OracleDbType.Int32, idActor);
                int propio = ConversionOracle.Entero((await titular.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
                if (propio != 0)
                {
                    await RechazarAsync(transaccion, "Nadie puede verificar su propio depósito (RN-21).").ConfigureAwait(false);
                }
            }
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE deposito SET id_material_verificado = :material, id_verificador = :verificador, fecha_verificacion_utc = :fecha, cantidad_verificada = :cantidad " +
                "WHERE id_deposito = :id AND id_material_verificado IS NULL", transaccion))
            {
                comando.Agregar("material", OracleDbType.Int32, depositoVerificado.IdMaterialVerificado);
                comando.Agregar("verificador", OracleDbType.Int32, idActor);
                comando.Agregar("cantidad", OracleDbType.Int32, depositoVerificado.CantidadVerificada);
                comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(depositoVerificado.FechaVerificacionUtc));
                comando.Agregar("id", OracleDbType.Int32, depositoVerificado.Id);
                int filas = await TraductorErroresOracle.TraducirReferenciaAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
                ComandosOracle.ExigirUnaFila(filas, "depósito", depositoVerificado.Id);
            }
            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        // Escrituras compartidas

        /// <summary>Guarda el avance de la orden y, si lo trae, el cierre de su interacción (orden → interacción).</summary>
        private static async Task GuardarCambioAsync(OracleConnection conexion, OracleTransaction transaccion, CambioOrden cambio,
            CancellationToken cancellationToken)
        {
            ExigirOrdenGuardada(cambio.Orden);
            if (cambio.InteraccionCerrada is { } cerrada && cerrada.Id != cambio.Orden.IdInteraccion)
            {
                throw new ArgumentException("La interacción cerrada debe ser la de la orden.", nameof(cambio));
            }

            // Bloquear antes de escribir: un UPDATE sobre una fila retenida esperaría sin límite.
            var actual = (EstadoOrdenApertura)await BloquearEstadoAsync(conexion, transaccion,
                "SELECT estado FROM orden_apertura WHERE id_orden = :id FOR UPDATE WAIT 10", "orden", cambio.Orden.Id, cancellationToken)
                .ConfigureAwait(false);
            if (actual != cambio.EstadoAnterior)
            {
                await RechazarAsync(transaccion, "La orden cambió de estado.").ConfigureAwait(false);
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE orden_apertura SET estado = :estado, fecha_cierre_utc = :cierre WHERE id_orden = :id", transaccion))
            {
                comando.Agregar("estado", OracleDbType.Int32, (int)cambio.Orden.Estado);
                comando.Agregar("cierre", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(cambio.Orden.FechaCierreUtc));
                comando.Agregar("id", OracleDbType.Int32, cambio.Orden.Id);
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            if (cambio.InteraccionCerrada is { } interaccion)
            {
                await GuardarInteraccionAsync(conexion, transaccion, interaccion, EstadoInteraccion.Autorizada, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>El cambio y el depósito deben ser de la orden del evento, y solo una detección aplicada deja depósito.</summary>
        private static void ExigirCoherencia(EventoDispositivo evento, CambioOrden? cambio, Deposito? deposito)
        {
            if (cambio is not null && cambio.Orden.Id != evento.IdOrden)
            {
                throw new ArgumentException("El cambio debe ser de la orden del evento.", nameof(cambio));
            }

            if (deposito is null)
            {
                return;
            }

            if (deposito.IdOrden != evento.IdOrden)
            {
                throw new ArgumentException("El depósito debe ser de la orden del evento.", nameof(deposito));
            }

            if (evento is not { Tipo: TipoEventoDispositivo.Deteccion or TipoEventoDispositivo.CierreSinSensor, Resultado: ResultadoEvento.Aplicado })
            {
                throw new ArgumentException("Solo una detección aplicada deja depósito.", nameof(deposito));
            }
        }

        private static void ExigirOrdenGuardada(OrdenApertura orden)
        {
            if (orden.EsNuevo)
            {
                throw new ArgumentException("La orden debe estar guardada.", nameof(orden));
            }
        }

        /// <summary>Bloquea la interacción y reescribe los campos que cambian si sigue en <paramref name="esperado"/>.</summary>
        private static async Task GuardarInteraccionAsync(OracleConnection conexion, OracleTransaction transaccion, Interaccion interaccion,
            EstadoInteraccion esperado, CancellationToken cancellationToken, bool exigirCantidad = false)
        {
            if (await BloquearInteraccionAsync(conexion, transaccion, interaccion.Id, cancellationToken).ConfigureAwait(false) != esperado)
            {
                await RechazarAsync(transaccion, "La interacción cambió de estado.").ConfigureAwait(false);
            }

            using OracleCommand comando = ComandosOracle.Crear(conexion, SqlActualizarInteraccion, transaccion);
            if (exigirCantidad)
            {
                using var declaracion = ComandosOracle.Crear(conexion, "SELECT cantidad_declarada FROM interaccion WHERE id_interaccion=:id", transaccion);
                declaracion.Agregar("id", OracleDbType.Int32, interaccion.Id);
                object? valor = await declaracion.ExecuteScalarAsync(cancellationToken);
                int? cantidad = valor is null or DBNull ? null : ConversionOracle.Entero(valor);
                if (cantidad != interaccion.CantidadDeclarada)
                    await RechazarAsync(transaccion, "La cantidad cambió antes de emitir la orden; vuelve a confirmar.");
            }
            comando.Agregar("estado", OracleDbType.Int32, (int)interaccion.Estado);
            comando.Agregar("sugerido", OracleDbType.Int32, interaccion.IdMaterialSugerido);
            comando.Agregar("confianza", OracleDbType.Decimal, interaccion.ConfianzaSugerida);
            comando.Agregar("confirmado", OracleDbType.Int32, interaccion.IdMaterialConfirmado);
            comando.Agregar("motivo", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(interaccion.Motivo));
            comando.Agregar("cierre", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtcOpcional(interaccion.FechaCierreUtc));
            comando.Agregar("id", OracleDbType.Int32, interaccion.Id);
            await TraductorErroresOracle.TraducirReferenciaAsync(() => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
        }

        // Bloqueos

        private static async Task BloquearMaterialHabilitadoAsync(OracleConnection conexion, OracleTransaction transaccion, int idMaterial,
            CancellationToken cancellationToken)
        {
            string nombre;
            bool habilitado;
            CategoriaMaterial categoria;
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT nombre, habilitado, categoria FROM material WHERE id_material = :id FOR UPDATE WAIT 10", transaccion))
            {
                comando.Agregar("id", OracleDbType.Int32, idMaterial);
                using DbDataReader lector = await TraductorErroresOracle.TraducirEsperaAsync(
                    () => comando.ExecuteReaderAsync(cancellationToken)).ConfigureAwait(false);
                if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"No existe material con id {idMaterial}.");
                }

                nombre = lector.GetString(0);
                habilitado = ConversionOracle.Logico(lector.GetValue(1));
                categoria = (CategoriaMaterial)ConversionOracle.Entero(lector.GetValue(2));
            }

            if (!habilitado)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.MaterialDeshabilitado(nombre)).ConfigureAwait(false);
            }
            if (categoria == CategoriaMaterial.Peligroso)
                await RechazarAsync(transaccion, "La caneca piloto no admite residuos de gestión especial.").ConfigureAwait(false);
        }

        private static async Task<EstadoContenedor> BloquearCanecaAsync(OracleConnection conexion, OracleTransaction transaccion, int idContenedor,
            CancellationToken cancellationToken) =>
            (EstadoContenedor)await BloquearEstadoAsync(conexion, transaccion,
                "SELECT estado FROM contenedor WHERE id_contenedor = :id FOR UPDATE WAIT 10", "caneca", idContenedor, cancellationToken).ConfigureAwait(false);

        private static async Task<EstadoInteraccion> BloquearInteraccionAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idInteraccion, CancellationToken cancellationToken) =>
            (EstadoInteraccion)await BloquearEstadoAsync(conexion, transaccion,
                "SELECT estado FROM interaccion WHERE id_interaccion = :id FOR UPDATE WAIT 10", "interacción", idInteraccion, cancellationToken).ConfigureAwait(false);

        private static async Task<int> BloquearEstadoAsync(OracleConnection conexion, OracleTransaction transaccion, string sql, string entidad,
            int id, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql, transaccion);
            comando.Agregar("id", OracleDbType.Int32, id);
            object? valor = await TraductorErroresOracle.TraducirEsperaAsync(
                () => comando.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false);
            return ConversionOracle.Entero(valor ?? throw new InvalidOperationException($"No existe {entidad} con id {id}."));
        }

        /// <summary>Deshace la transacción y lanza <see cref="CambioConcurrenteException"/>; nunca vuelve.</summary>
        private static async Task RechazarAsync(OracleTransaction transaccion, string mensaje)
        {
            // Sin token: el rollback debe ocurrir aunque la operación se haya cancelado.
            await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new CambioConcurrenteException(mensaje);
        }

        // Lecturas

        private static async Task<int> ContarAsync(OracleConnection conexion, OracleTransaction? transaccion, string sql, object valor,
            CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql, transaccion);
            comando.Agregar("valor", valor is string ? OracleDbType.Varchar2 : OracleDbType.Int32, valor);
            return ConversionOracle.Entero((await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
        }

        /// <summary>Ejecuta una consulta con a lo sumo un parámetro <c>:valor</c> (entero, texto o fecha) y mapea cada fila.</summary>
        private async Task<IReadOnlyList<T>> ConsultarAsync<T>(string sql, Func<DbDataReader, T> leer, object? valor, CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            if (valor is not null)
            {
                OracleDbType tipo = valor switch
                {
                    string => OracleDbType.Varchar2,
                    DateTime => OracleDbType.TimeStamp,
                    _ => OracleDbType.Int32,
                };
                comando.Agregar("valor", tipo, valor);
            }

            var filas = new List<T>();
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                filas.Add(leer(lector));
            }

            return filas;
        }

        private static T? Primero<T>(IReadOnlyList<T> filas) where T : class => filas.Count == 0 ? null : filas[0];

        private static Interaccion LeerInteraccion(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            ConversionOracle.Entero(lector.GetValue(1)),
            EnteroOpcional(lector.GetValue(2)),
            (CanalInteraccion)ConversionOracle.Entero(lector.GetValue(3)),
            (EstadoInteraccion)ConversionOracle.Entero(lector.GetValue(4)),
            EnteroOpcional(lector.GetValue(5)),
            DecimalOpcional(lector.GetValue(6)),
            EnteroOpcional(lector.GetValue(7)),
            EnteroOpcional(lector.GetValue(8)),
            ConversionOracle.TextoOpcional(lector.GetValue(9)),
            ConversionOracle.FechaUtc(lector.GetValue(10)),
            ConversionOracle.FechaUtcOpcional(lector.GetValue(11)),
            EnteroOpcional(lector.GetValue(12)));

        private static OrdenApertura LeerOrden(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            ConversionOracle.Entero(lector.GetValue(1)),
            ConversionOracle.Entero(lector.GetValue(2)),
            lector.GetString(3),
            (EstadoOrdenApertura)ConversionOracle.Entero(lector.GetValue(4)),
            ConversionOracle.FechaUtc(lector.GetValue(5)),
            ConversionOracle.FechaUtc(lector.GetValue(6)),
            ConversionOracle.FechaUtcOpcional(lector.GetValue(7)));

        private static EventoDispositivo LeerEvento(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            ConversionOracle.Entero(lector.GetValue(1)),
            lector.GetString(2),
            (TipoEventoDispositivo)ConversionOracle.Entero(lector.GetValue(3)),
            EnteroOpcional(lector.GetValue(4)),
            (ResultadoEvento)ConversionOracle.Entero(lector.GetValue(5)),
            ConversionOracle.FechaUtcOpcional(lector.GetValue(6)),
            ConversionOracle.FechaUtc(lector.GetValue(7)));

        private static Deposito LeerDeposito(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            ConversionOracle.Entero(lector.GetValue(1)),
            ConversionOracle.Entero(lector.GetValue(2)),
            ConversionOracle.Entero(lector.GetValue(3)),
            EnteroOpcional(lector.GetValue(4)),
            EnteroOpcional(lector.GetValue(5)),
            ConversionOracle.FechaUtcOpcional(lector.GetValue(6)),
            DecimalOpcional(lector.GetValue(7)),
            DecimalOpcional(lector.GetValue(8)),
            ConversionOracle.FechaUtc(lector.GetValue(9)),
            EnteroOpcional(lector.GetValue(10)),
            ConversionOracle.Entero(lector.GetValue(11)) == 1);

        private static int? EnteroOpcional(object valor) => valor is DBNull ? null : ConversionOracle.Entero(valor);

        private static decimal? DecimalOpcional(object valor) => valor is DBNull ? null : Convert.ToDecimal(valor, CultureInfo.InvariantCulture);
    }
}
