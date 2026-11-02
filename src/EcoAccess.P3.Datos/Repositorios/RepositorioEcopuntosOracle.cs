using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>Libro de V005. No ofrece UPDATE/DELETE; cada cambio de saldo agrega evidencia.</summary>
    public sealed class RepositorioEcopuntosOracle : IRepositorioEcopuntos
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioEcopuntosOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string ColumnasMovimiento = "m.id_movimiento,m.id_operacion,m.id_deposito,m.id_usuario,m.id_regla,m.tipo,m.puntos," +
            "m.id_credito_revertido,m.id_predecesor,o.id_actor,o.fecha_utc,o.motivo";

        public Task<int> AgregarReglaAsync(ReglaEcopuntos regla, CancellationToken ct = default) =>
            TraductorErroresOracle.TraducirEsperaAsync(async () =>
            {
                using var c = await _fabrica.AbrirAsync(ct);
                using var t = (OracleTransaction)await c.BeginTransactionAsync(ct);
                await ExigirActorAsync(c, t, regla.IdCreador, soloAdministrador: true, ct);
                using var cmd = Crear(c, t, "INSERT INTO regla_ecopuntos(id_material,puntos,descripcion,desde_utc,hasta_utc,id_creador,por_unidad) " +
                    "VALUES(:material,:puntos,:descripcion,:desde,:hasta,:actor,:por_unidad) RETURNING id_regla INTO :id",
                    ("material", regla.IdMaterial), ("puntos", regla.Puntos), ("descripcion", regla.Descripcion),
                    ("desde", regla.DesdeUtc), ("hasta", regla.HastaUtc), ("actor", regla.IdCreador), ("por_unidad", regla.PorUnidad ? 1 : 0));
                var id = cmd.AgregarIdSalida();
                await cmd.ExecuteNonQueryAsync(ct);
                await t.CommitAsync(ct);
                return ComandosOracle.LeerId(id);
            });

        public async Task<ReglaEcopuntos?> ObtenerReglaAsync(int id, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            return await LeerReglaAsync(c, null, id, ct);
        }

        public async Task<IReadOnlyList<ReglaEcopuntos>> ListarReglasAsync(CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            using var cmd = Crear(c, null, "SELECT id_regla,id_material,puntos,descripcion,desde_utc,hasta_utc,id_creador,por_unidad FROM regla_ecopuntos ORDER BY id_regla DESC");
            using var r = await cmd.ExecuteReaderAsync(ct);
            var lista = new List<ReglaEcopuntos>();
            while (await r.ReadAsync(ct)) lista.Add(Regla(r));
            return lista;
        }

        public async Task<MovimientoEcopuntos?> ObtenerMovimientoAsync(int id, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            return (await LeerMovimientosAsync(c, null, "m.id_movimiento=:id", id, ct)).SingleOrDefault();
        }

        public async Task<OperacionEcopuntos?> ObtenerOperacionAsync(string clave, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            return await LeerOperacionAsync(c, null, clave, ct);
        }

        public Task<IReadOnlyList<MovimientoEcopuntos>> AplicarAsync(SolicitudEcopuntos solicitud, CancellationToken ct = default) =>
            TraductorErroresOracle.TraducirEsperaAsync(async () =>
            {
                try { return await AplicarNucleoAsync(solicitud, ct); }
                catch (OracleException ex) when (ex.Number == 1)
                {
                    // Una misma clave puede competir incluso entre depósitos distintos; recuperar solo payload idéntico.
                    var ganadora = await ObtenerOperacionAsync(solicitud.Clave, ct);
                    if (ganadora is not null && Misma(ganadora.Solicitud, solicitud)) return ganadora.Movimientos;
                    throw new CambioConcurrenteException("La clave o el crédito ya fueron utilizados.", ex);
                }
            });

        private async Task<IReadOnlyList<MovimientoEcopuntos>> AplicarNucleoAsync(SolicitudEcopuntos s, CancellationToken ct)
        {
            if (!Compat.IsDefined(s.Tipo) || s.IdDeposito <= 0 || s.IdActor <= 0 || s.FechaUtc.Kind != DateTimeKind.Utc
                || Compat.IsNullOrWhiteSpace(s.Clave) || s.Clave.Length > 80 || s.Clave != s.Clave.Trim()
                || Compat.IsNullOrWhiteSpace(s.Motivo) || s.Motivo.Length > 300 || s.Motivo != s.Motivo.Trim()
                || (s.Tipo == TipoOperacionEcopuntos.Acreditar ? s.IdCredito is not null : s.IdCredito is null)
                || (s.Tipo == TipoOperacionEcopuntos.Revertir ? s.IdRegla is not null : s.IdRegla is null))
                throw new ArgumentException("Solicitud de ecopuntos incoherente.", nameof(s));

            using var c = await _fabrica.AbrirAsync(ct);
            using var t = (OracleTransaction)await c.BeginTransactionAsync(ct);
            // Material → actor → depósito, compartido con la verificación de depósitos.
            object? material = await EscalarAsync(c, t, "SELECT id_material_verificado FROM deposito WHERE id_deposito=:id", s.IdDeposito, ct);
            if (material is null or DBNull) throw new CambioConcurrenteException("El depósito no tiene material verificado.");
            await EscalarAsync(c, t, "SELECT id_material FROM material WHERE id_material=:id FOR UPDATE WAIT 10", material, ct);
            await ExigirActorAsync(c, t, s.IdActor, s.Tipo != TipoOperacionEcopuntos.Acreditar, ct);

            if (await LeerOperacionAsync(c, t, s.Clave, ct) is { } existente)
            {
                if (!Misma(existente.Solicitud, s)) throw new CambioConcurrenteException("La clave ya identifica otra operación.");
                return existente.Movimientos;
            }

            await EscalarAsync(c, t, "SELECT id_deposito FROM deposito WHERE id_deposito=:id FOR UPDATE WAIT 10", s.IdDeposito, ct);
            int ciudadano;
            int? cantidadVerificada;
            using (var evidencia = Crear(c, t, "SELECT i.id_usuario,d.id_verificador,d.id_material_verificado,d.fecha_verificacion_utc,d.cantidad_verificada " +
                "FROM deposito d JOIN orden_apertura o ON o.id_orden=d.id_orden JOIN interaccion i ON i.id_interaccion=o.id_interaccion " +
                "WHERE d.id_deposito=:id", ("id", s.IdDeposito)))
            using (var r = await evidencia.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct) || r.IsDBNull(0) || r.IsDBNull(1) || r.IsDBNull(2) || r.IsDBNull(3))
                    throw new CambioConcurrenteException("El depósito no tiene evidencia verificable y titular.");
                ciudadano = r.GetInt32(0);
                if (ciudadano == s.IdActor || ciudadano == r.GetInt32(1))
                    throw new CambioConcurrenteException("Se requiere verificación y acreditación independientes del titular.");
                material = r.GetInt32(2);
                cantidadVerificada = Nulo(r, 4);
            }

            ReglaEcopuntos? regla = s.IdRegla is { } idRegla ? await LeerReglaAsync(c, t, idRegla, ct) : null;
            if (s.Tipo != TipoOperacionEcopuntos.Revertir
                && (regla is null || regla.IdMaterial != Convert.ToInt32(material) || !regla.EstaVigente(s.FechaUtc)))
                throw new CambioConcurrenteException("La regla no corresponde al material verificado o no está vigente.");

            int puntos = 0;
            if (regla is not null)
            {
                try { puntos = regla.CalcularPuntos(cantidadVerificada); }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException)
                { throw new CambioConcurrenteException(error.Message); }
            }

            MovimientoEcopuntos? anterior = null;
            if (s.Tipo == TipoOperacionEcopuntos.Acreditar)
            {
                if (Convert.ToInt32(await EscalarAsync(c, t, "SELECT COUNT(*) FROM movimiento_ecopuntos WHERE id_deposito=:id", s.IdDeposito, ct)) > 0)
                    throw new CambioConcurrenteException("Ese depósito ya tiene un crédito inicial; usa corrección o reverso.");
            }
            else
            {
                anterior = (await LeerMovimientosAsync(c, t, "m.id_movimiento=:id", s.IdCredito!.Value, ct)).SingleOrDefault();
                if (anterior is null || anterior.Tipo != TipoMovimientoEcopuntos.Credito || anterior.IdDeposito != s.IdDeposito
                    || anterior.IdUsuario != ciudadano || Convert.ToInt32(await EscalarAsync(c, t,
                        "SELECT COUNT(*) FROM movimiento_ecopuntos WHERE id_credito_revertido=:id", anterior.Id, ct)) != 0)
                    throw new CambioConcurrenteException("El crédito objetivo ya no está activo o corresponde a otra operación.");
            }

            int operacion;
            using (var cmd = Crear(c, t, "INSERT INTO operacion_ecopuntos(clave,tipo,id_deposito,id_actor,id_credito,id_regla,motivo,fecha_utc) " +
                "VALUES(:clave,:tipo,:deposito,:actor,:credito,:regla,:motivo,:fecha) RETURNING id_operacion INTO :id",
                ("clave", s.Clave), ("tipo", (int)s.Tipo), ("deposito", s.IdDeposito), ("actor", s.IdActor),
                ("credito", s.IdCredito), ("regla", s.IdRegla), ("motivo", s.Motivo), ("fecha", s.FechaUtc)))
            {
                var id = cmd.AgregarIdSalida();
                await cmd.ExecuteNonQueryAsync(ct);
                operacion = ComandosOracle.LeerId(id);
            }
            if (anterior is not null)
                await InsertarMovimientoAsync(c, t, operacion, s.IdDeposito, ciudadano, anterior.IdRegla,
                    TipoMovimientoEcopuntos.Reverso, -anterior.Puntos, anterior.Id, null, ct);
            if (regla is not null)
                await InsertarMovimientoAsync(c, t, operacion, s.IdDeposito, ciudadano, regla.Id,
                    TipoMovimientoEcopuntos.Credito, puntos, null, anterior?.Id, ct);
            await t.CommitAsync(ct);
            return await LeerMovimientosAsync(c, null, "m.id_operacion=:id", operacion, ct);
        }

        public async Task<ResumenEcopuntos> ConsultarAsync(int idUsuario, CancellationToken ct = default)
        {
            using var c = await _fabrica.AbrirAsync(ct);
            // Una transacción de solo lectura fija la misma instantánea para saldo e historial.
            using var inicio = Crear(c, null, "SET TRANSACTION READ ONLY");
            await inicio.ExecuteNonQueryAsync(ct);
            long saldo, acreditado, revertido;
            using (var cmd = Crear(c, null, "SELECT NVL(SUM(puntos),0),NVL(SUM(CASE WHEN tipo=1 THEN puntos ELSE 0 END),0)," +
                "NVL(SUM(CASE WHEN tipo=2 THEN -puntos ELSE 0 END),0) FROM movimiento_ecopuntos WHERE id_usuario=:id", ("id", idUsuario)))
            using (var r = await cmd.ExecuteReaderAsync(ct))
            {
                await r.ReadAsync(ct);
                saldo = r.GetInt64(0); acreditado = r.GetInt64(1); revertido = r.GetInt64(2);
            }
            var lista = await LeerMovimientosAsync(c, null, "m.id_usuario=:id", idUsuario, ct, limite: true);
            using var fin = Crear(c, null, "COMMIT");
            await fin.ExecuteNonQueryAsync(ct);
            return new ResumenEcopuntos(saldo, lista, acreditado, revertido);
        }

        private static bool Misma(SolicitudEcopuntos a, SolicitudEcopuntos b) => a with { FechaUtc = b.FechaUtc } == b;

        private static async Task ExigirActorAsync(OracleConnection c, OracleTransaction t, int actor, bool soloAdministrador, CancellationToken ct)
        {
            if (Convert.ToInt32(await EscalarAsync(c, t, "SELECT activo FROM usuario WHERE id_usuario=:id FOR UPDATE WAIT 10", actor, ct)) != 1)
                throw new CambioConcurrenteException("La cuenta no está activa.");
            string roles = soloAdministrador ? "3" : "2,3";
            if (Convert.ToInt32(await EscalarAsync(c, t, "SELECT COUNT(*) FROM usuario_rol ur JOIN rol r ON r.id_rol=ur.id_rol " +
                $"WHERE ur.id_usuario=:id AND r.codigo IN ({roles})", actor, ct)) == 0)
                throw new CambioConcurrenteException("La cuenta ya no tiene el rol necesario.");
        }

        private static async Task<OperacionEcopuntos?> LeerOperacionAsync(OracleConnection c, OracleTransaction? t, string clave, CancellationToken ct)
        {
            SolicitudEcopuntos solicitud;
            int id;
            using (var cmd = Crear(c, t, "SELECT id_operacion,tipo,id_deposito,id_credito,id_regla,id_actor,motivo,fecha_utc " +
                "FROM operacion_ecopuntos WHERE clave=:clave", ("clave", clave)))
            using (var r = await cmd.ExecuteReaderAsync(ct))
            {
                if (!await r.ReadAsync(ct)) return null;
                id = r.GetInt32(0);
                solicitud = new SolicitudEcopuntos((TipoOperacionEcopuntos)r.GetInt32(1), r.GetInt32(2), Nulo(r, 3), Nulo(r, 4),
                    r.GetInt32(5), clave, r.GetString(6), Utc(r.GetDateTime(7)));
            }
            return new OperacionEcopuntos(solicitud, await LeerMovimientosAsync(c, t, "m.id_operacion=:id", id, ct));
        }

        private static async Task<IReadOnlyList<MovimientoEcopuntos>> LeerMovimientosAsync(OracleConnection c, OracleTransaction? t,
            string condicion, int id, CancellationToken ct, bool limite = false)
        {
            using var cmd = Crear(c, t, $"SELECT {ColumnasMovimiento} FROM movimiento_ecopuntos m " +
                $"JOIN operacion_ecopuntos o ON o.id_operacion=m.id_operacion WHERE {condicion} ORDER BY m.id_movimiento DESC" +
                (limite ? " FETCH FIRST 50 ROWS ONLY" : ""), ("id", id));
            using var r = await cmd.ExecuteReaderAsync(ct);
            var lista = new List<MovimientoEcopuntos>();
            while (await r.ReadAsync(ct)) lista.Add(new MovimientoEcopuntos(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), r.GetInt32(4),
                (TipoMovimientoEcopuntos)r.GetInt32(5), r.GetInt32(6), Nulo(r, 7), Nulo(r, 8), r.GetInt32(9), Utc(r.GetDateTime(10)), r.GetString(11)));
            return lista;
        }

        private static async Task InsertarMovimientoAsync(OracleConnection c, OracleTransaction t, int operacion, int deposito,
            int usuario, int regla, TipoMovimientoEcopuntos tipo, int puntos, int? revertido, int? predecesor, CancellationToken ct)
        {
            using var cmd = Crear(c, t, "INSERT INTO movimiento_ecopuntos(id_operacion,id_deposito,id_usuario,id_regla,tipo,puntos,id_credito_revertido,id_predecesor) " +
                "VALUES(:operacion,:deposito,:usuario,:regla,:tipo,:puntos,:revertido,:predecesor)",
                ("operacion", operacion), ("deposito", deposito), ("usuario", usuario), ("regla", regla), ("tipo", (int)tipo),
                ("puntos", puntos), ("revertido", revertido), ("predecesor", predecesor));
            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static async Task<ReglaEcopuntos?> LeerReglaAsync(OracleConnection c, OracleTransaction? t, int id, CancellationToken ct)
        {
            using var cmd = Crear(c, t, "SELECT id_regla,id_material,puntos,descripcion,desde_utc,hasta_utc,id_creador,por_unidad FROM regla_ecopuntos WHERE id_regla=:id", ("id", id));
            using var r = await cmd.ExecuteReaderAsync(ct);
            return await r.ReadAsync(ct) ? Regla(r) : null;
        }
        private static ReglaEcopuntos Regla(OracleDataReader r) => new(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), r.GetString(3),
            Utc(r.GetDateTime(4)), r.IsDBNull(5) ? null : Utc(r.GetDateTime(5)), r.GetInt32(6), ConversionOracle.Logico(r.GetValue(7)));
        private static int? Nulo(OracleDataReader r, int i) => r.IsDBNull(i) ? null : r.GetInt32(i);
        private static DateTime Utc(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Utc);
        private static async Task<object?> EscalarAsync(OracleConnection c, OracleTransaction? t, string sql, object id, CancellationToken ct)
        {
            using var cmd = Crear(c, t, sql, ("id", id));
            return await cmd.ExecuteScalarAsync(ct);
        }
        private static OracleCommand Crear(OracleConnection c, OracleTransaction? t, string sql, params (string Nombre, object? Valor)[] valores)
        {
            var cmd = ComandosOracle.Crear(c, sql, t);
            foreach (var (nombre, valor) in valores)
                cmd.Agregar(nombre, valor is DateTime || nombre == "hasta" ? OracleDbType.TimeStamp : valor is string ? OracleDbType.Varchar2 : OracleDbType.Int32, valor);
            return cmd;
        }
    }
}
