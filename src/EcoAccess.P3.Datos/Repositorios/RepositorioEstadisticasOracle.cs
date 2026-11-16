using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using System.Globalization;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>
    /// Estadísticas de solo lectura sobre las tablas de V003. Toda la agregación ocurre en Oracle con SQL
    /// parametrizado: los filtros opcionales se escriben <c>(:p IS NULL OR columna = :p)</c>, así una sola sentencia
    /// sirve para cualquier combinación sin concatenar valores. Ninguna consulta abre transacción ni bloquea filas.
    /// </summary>
    /// <remarks>
    /// Cada tabla se filtra por su propia fecha (inicio, emisión, recepción, detección) y el usuario se alcanza por la
    /// interacción: orden → interacción, depósito → orden → interacción, evento → orden → interacción.
    /// </remarks>
    public sealed class RepositorioEstadisticasOracle : IRepositorioEstadisticas
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioEstadisticasOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        // Depósitos del filtro con el material elegido por el criterio (1 declarado, 2 verificado). Es la base de todas
        // las consultas de depósitos; el criterio verificado excluye los que aún no tienen verificación.
        private const string CteDepositos =
            "WITH dep AS (" +
            " SELECT d.id_deposito, d.fecha_deteccion_utc, o.id_contenedor, i.id_usuario, d.id_material_declarado," +
            " d.id_material_verificado, d.peso_medido_kg, d.peso_estimado_kg," +
            " CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END AS id_material" +
            " FROM deposito d" +
            " JOIN orden_apertura o ON o.id_orden = d.id_orden" +
            " JOIN interaccion i ON i.id_interaccion = o.id_interaccion" +
            " WHERE d.fecha_deteccion_utc >= :desde AND d.fecha_deteccion_utc < :hasta" +
            " AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)" +
            " AND (:usuario IS NULL OR i.id_usuario = :usuario)" +
            " AND (:criterio = 1 OR d.id_material_verificado IS NOT NULL)" +
            " AND (:material IS NULL OR (CASE WHEN :criterio = 2 THEN d.id_material_verificado ELSE d.id_material_declarado END) = :material)" +
            " AND (:verificacion = 0" +
            "      OR (:verificacion = 1 AND d.id_material_verificado IS NULL)" +
            "      OR (:verificacion = 2 AND d.id_material_verificado IS NOT NULL))" +
            ") ";

        // Columnas de conteo y peso comunes al resumen y a las agrupaciones. COUNT(columna) ignora NULL, así cuenta solo
        // los depósitos con ese dato; NVL evita que la suma de un grupo sin pesos llegue como NULL.
        private const string ColumnasPeso =
            "COUNT(dep.peso_medido_kg), NVL(SUM(dep.peso_medido_kg), 0), COUNT(dep.peso_estimado_kg), NVL(SUM(dep.peso_estimado_kg), 0)";

        // Una sola fila con los cuatro bloques del resumen: un viaje a la base de datos y un único juego de parámetros.
        private const string SqlResumen =
            CteDepositos +
            "SELECT ia.*, oa.*, ea.*, da.* FROM" +
            " (SELECT COUNT(*) AS i_total," +
            "  COUNT(CASE WHEN i.estado = 1 THEN 1 END) AS i_iniciadas, COUNT(CASE WHEN i.estado = 2 THEN 1 END) AS i_autorizadas," +
            "  COUNT(CASE WHEN i.estado = 3 THEN 1 END) AS i_con_deposito, COUNT(CASE WHEN i.estado = 4 THEN 1 END) AS i_sin_deposito," +
            "  COUNT(CASE WHEN i.estado = 5 THEN 1 END) AS i_rechazadas," +
            "  COUNT(CASE WHEN i.canal = 1 THEN 1 END) AS i_telegram, COUNT(CASE WHEN i.canal IN (2,5) THEN 1 END) AS i_manual," +
            "  COUNT(CASE WHEN i.canal IN (3,4) THEN 1 END) AS i_simulador" +
            "  FROM interaccion i" +
            "  WHERE i.fecha_inicio_utc >= :desde AND i.fecha_inicio_utc < :hasta" +
            "  AND (:contenedor IS NULL OR i.id_contenedor = :contenedor)" +
            "  AND (:usuario IS NULL OR i.id_usuario = :usuario)) ia" +
            " CROSS JOIN" +
            " (SELECT COUNT(*) AS o_total," +
            "  COUNT(CASE WHEN x.estado BETWEEN 1 AND 5 THEN 1 END) AS o_vigentes, COUNT(CASE WHEN x.estado = 6 THEN 1 END) AS o_cerradas," +
            "  COUNT(CASE WHEN x.estado = 7 THEN 1 END) AS o_vencidas, COUNT(CASE WHEN x.estado = 8 THEN 1 END) AS o_fallidas," +
            "  COUNT(CASE WHEN x.apertura = 1 THEN 1 END) AS o_apertura, COUNT(x.id_deposito) AS o_deposito" +
            // EXISTS por orden usa ix_evento_orden y solo revisa las órdenes del período; una orden con dos
            // aperturas aplicadas cuenta una vez.
            "  FROM (SELECT o.estado, dp.id_deposito," +
            "   CASE WHEN EXISTS (SELECT 1 FROM evento_dispositivo ea" +
            "    WHERE ea.id_orden = o.id_orden AND ea.tipo = 2 AND ea.resultado = 1) THEN 1 ELSE 0 END AS apertura" +
            "   FROM orden_apertura o" +
            "   JOIN interaccion i ON i.id_interaccion = o.id_interaccion" +
            "   LEFT JOIN deposito dp ON dp.id_orden = o.id_orden" +
            "   WHERE o.fecha_emision_utc >= :desde AND o.fecha_emision_utc < :hasta" +
            "   AND (:contenedor IS NULL OR o.id_contenedor = :contenedor)" +
            "   AND (:usuario IS NULL OR i.id_usuario = :usuario)) x) oa" +
            " CROSS JOIN" +
            " (SELECT COUNT(*) AS e_total," +
            "  COUNT(CASE WHEN e.resultado = 1 THEN 1 END) AS e_aplicados, COUNT(CASE WHEN e.resultado = 2 THEN 1 END) AS e_revision," +
            "  COUNT(CASE WHEN e.tipo = 5 THEN 1 END) AS e_fallos" +
            "  FROM evento_dispositivo e" +
            "  LEFT JOIN orden_apertura o ON o.id_orden = e.id_orden" +
            "  LEFT JOIN interaccion i ON i.id_interaccion = o.id_interaccion" +
            "  WHERE e.fecha_recepcion_utc >= :desde AND e.fecha_recepcion_utc < :hasta" +
            "  AND (:contenedor IS NULL OR e.id_contenedor = :contenedor)" +
            "  AND (:usuario IS NULL OR i.id_usuario = :usuario)) ea" +
            " CROSS JOIN" +
            " (SELECT COUNT(*) AS d_total, COUNT(dep.id_material_verificado) AS d_verificados," +
            "  COUNT(CASE WHEN dep.id_material_verificado = dep.id_material_declarado THEN 1 END) AS d_coincidentes, " +
            ColumnasPeso + " FROM dep) da";

        // Incidencias por caneca. En revisión no repite los fallos, que ya tienen su columna: cada evento cuenta una vez.
        private const string SqlFallos =
            "SELECT c.id_contenedor, c.nombre, NVL(ev.fallos, 0), NVL(ev.revision, 0), NVL(od.fallidas, 0), NVL(od.vencidas, 0)" +
            " FROM contenedor c" +
            " LEFT JOIN (SELECT e.id_contenedor," +
            "   COUNT(CASE WHEN e.tipo = 5 THEN 1 END) AS fallos," +
            "   COUNT(CASE WHEN e.resultado = 2 AND e.tipo <> 5 THEN 1 END) AS revision" +
            "   FROM evento_dispositivo e" +
            "   LEFT JOIN orden_apertura o ON o.id_orden = e.id_orden" +
            "   LEFT JOIN interaccion i ON i.id_interaccion = o.id_interaccion" +
            "   WHERE e.fecha_recepcion_utc >= :desde AND e.fecha_recepcion_utc < :hasta" +
            "   AND (:usuario IS NULL OR i.id_usuario = :usuario)" +
            "   GROUP BY e.id_contenedor) ev ON ev.id_contenedor = c.id_contenedor" +
            " LEFT JOIN (SELECT o.id_contenedor," +
            "   COUNT(CASE WHEN o.estado = 8 THEN 1 END) AS fallidas," +
            "   COUNT(CASE WHEN o.estado = 7 THEN 1 END) AS vencidas" +
            "   FROM orden_apertura o" +
            "   JOIN interaccion i ON i.id_interaccion = o.id_interaccion" +
            "   WHERE o.fecha_emision_utc >= :desde AND o.fecha_emision_utc < :hasta" +
            "   AND (:usuario IS NULL OR i.id_usuario = :usuario)" +
            "   GROUP BY o.id_contenedor) od ON od.id_contenedor = c.id_contenedor" +
            " WHERE (:contenedor IS NULL OR c.id_contenedor = :contenedor)" +
            " AND NVL(ev.fallos, 0) + NVL(ev.revision, 0) + NVL(od.fallidas, 0) + NVL(od.vencidas, 0) > 0" +
            " ORDER BY NVL(ev.fallos, 0) + NVL(ev.revision, 0) + NVL(od.fallidas, 0) + NVL(od.vencidas, 0) DESC, c.nombre, c.id_contenedor";

        private const string SqlContarDepositos = CteDepositos + "SELECT COUNT(*) FROM dep";

        private const string SqlPaginaDepositos =
            CteDepositos +
            "SELECT dep.id_deposito, dep.fecha_deteccion_utc, dep.id_contenedor, c.nombre, dep.id_usuario, md.nombre, mv.nombre," +
            " dep.peso_medido_kg, dep.peso_estimado_kg" +
            " FROM dep" +
            " JOIN contenedor c ON c.id_contenedor = dep.id_contenedor" +
            " JOIN material md ON md.id_material = dep.id_material_declarado" +
            " LEFT JOIN material mv ON mv.id_material = dep.id_material_verificado" +
            " ORDER BY dep.fecha_deteccion_utc DESC, dep.id_deposito DESC" +
            " OFFSET :salto ROWS FETCH NEXT :tamano ROWS ONLY";

        // Día civil de Colombia: UTC−5 todo el año. Se resta el desfase y se trunca a la fecha.
        private const string DiaColombia = "TRUNC(CAST(dep.fecha_deteccion_utc - INTERVAL '5' HOUR AS DATE))";

        public async Task<ResumenActividad> ObtenerResumenAsync(FiltroEstadisticas filtro, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            IReadOnlyList<ResumenActividad> filas = await ConsultarAsync(SqlResumen, filtro, null, LeerResumen, cancellationToken)
                .ConfigureAwait(false);
            return filas[0];
        }

        public Task<IReadOnlyList<FilaAgrupada>> AgruparDepositosAsync(
            FiltroEstadisticas filtro,
            AgrupacionDepositos agrupacion,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            return ConsultarAsync(SqlAgrupacion(agrupacion), filtro, null, lector => LeerAgrupada(lector, agrupacion), cancellationToken);
        }

        public Task<IReadOnlyList<FilaFallosContenedor>> ListarFallosPorContenedorAsync(
            FiltroEstadisticas filtro,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            return ConsultarAsync(SqlFallos, filtro, null, LeerFallos, cancellationToken);
        }

        public async Task<PaginaDepositos> ListarDepositosAsync(
            FiltroEstadisticas filtro,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            Compat.ThrowIfLessThan(pagina, 1, nameof(pagina));
            Compat.ThrowIfLessThan(tamanoPagina, 1, nameof(tamanoPagina));

            IReadOnlyList<int> total = await ConsultarAsync(SqlContarDepositos, filtro, null,
                lector => ConversionOracle.Entero(lector.GetValue(0)), cancellationToken).ConfigureAwait(false);
            IReadOnlyList<FilaDeposito> filas = await ConsultarAsync(SqlPaginaDepositos, filtro,
                comando =>
                {
                    // En long: (pagina - 1) * tamano no debe desbordar un int con una página muy alta.
                    comando.Agregar("salto", OracleDbType.Int64, (long)(pagina - 1) * tamanoPagina);
                    comando.Agregar("tamano", OracleDbType.Int32, tamanoPagina);
                },
                LeerDeposito, cancellationToken).ConfigureAwait(false);
            return new PaginaDepositos(filas, total[0], pagina, tamanoPagina);
        }

        /// <summary>
        /// Arma la agrupación con fragmentos fijos según el enum (nunca con texto del usuario). Las etiquetas de día y
        /// de usuario se calculan en C#: el día es un DateOnly y el usuario se muestra solo por id (RN-13).
        /// </summary>
        private static string SqlAgrupacion(AgrupacionDepositos agrupacion)
        {
            (string clave, string etiqueta, string union) = agrupacion switch
            {
                AgrupacionDepositos.Material => ("dep.id_material", "m.nombre", " JOIN material m ON m.id_material = dep.id_material"),
                AgrupacionDepositos.Contenedor => ("dep.id_contenedor", "c.nombre", " JOIN contenedor c ON c.id_contenedor = dep.id_contenedor"),
                AgrupacionDepositos.Punto => ("p.id_punto_instalacion", "p.nombre",
                    " JOIN contenedor c ON c.id_contenedor = dep.id_contenedor" +
                    " JOIN punto_instalacion p ON p.id_punto_instalacion = c.id_punto_instalacion"),
                AgrupacionDepositos.Dia => (DiaColombia, "NULL", string.Empty),
                AgrupacionDepositos.Usuario => ("dep.id_usuario", "NULL", string.Empty),
                _ => throw new ArgumentOutOfRangeException(nameof(agrupacion), agrupacion, "Agrupación no válida."),
            };

            string orden = agrupacion == AgrupacionDepositos.Dia ? "1" : "3 DESC, 2, 1";
            return CteDepositos +
                $"SELECT {clave}, {etiqueta}, COUNT(*), COUNT(dep.id_material_verificado), {ColumnasPeso}" +
                $" FROM dep{union} GROUP BY {clave}, {etiqueta} ORDER BY {orden}";
        }

        /// <summary>
        /// Abre una conexión, enlaza los parámetros del filtro y los extra, y mapea cada fila. Oracle enlaza por nombre
        /// (<c>BindByName</c>), así un mismo parámetro puede aparecer varias veces en la sentencia.
        /// </summary>
        private async Task<IReadOnlyList<T>> ConsultarAsync<T>(
            string sql,
            FiltroEstadisticas filtro,
            Action<OracleCommand>? parametrosExtra,
            Func<DbDataReader, T> leer,
            CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            AgregarFiltro(comando, sql, filtro);
            parametrosExtra?.Invoke(comando);

            var filas = new List<T>();
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                filas.Add(leer(lector));
            }

            return filas;
        }

        // Con BindByName, ODP.NET rechaza un parámetro que no aparece en la sentencia (ORA-01036); por eso solo se
        // enlazan los que el texto usa. La consulta de fallos, por ejemplo, no lleva material ni verificación.
        private static void AgregarFiltro(OracleCommand comando, string sql, FiltroEstadisticas filtro)
        {
            comando.Agregar("desde", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(filtro.DesdeUtc));
            comando.Agregar("hasta", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(filtro.HastaUtc));
            comando.Agregar("contenedor", OracleDbType.Int32, filtro.IdContenedor);
            comando.Agregar("usuario", OracleDbType.Int32, filtro.IdUsuario);
            if (sql.Contains(":material", StringComparison.Ordinal))
            {
                comando.Agregar("material", OracleDbType.Int32, filtro.IdMaterial);
                comando.Agregar("criterio", OracleDbType.Int32, (int)filtro.Criterio);
                comando.Agregar("verificacion", OracleDbType.Int32, (int)filtro.Verificacion);
            }
        }

        private static ResumenActividad LeerResumen(DbDataReader lector)
        {
            int Entero(int i) => ConversionOracle.Entero(lector.GetValue(i));
            return new ResumenActividad(
                new ConteoInteracciones(Entero(0), Entero(1), Entero(2), Entero(3), Entero(4), Entero(5), Entero(6), Entero(7), Entero(8)),
                new ConteoOrdenes(Entero(9), Entero(10), Entero(11), Entero(12), Entero(13), Entero(14), Entero(15)),
                new ConteoEventos(Entero(16), Entero(17), Entero(18), Entero(19)),
                new ResumenDepositos(Entero(20), Entero(21), Entero(22), Entero(23), Decimal(lector.GetValue(24)), Entero(25),
                    Decimal(lector.GetValue(26))));
        }

        private static FilaAgrupada LeerAgrupada(DbDataReader lector, AgrupacionDepositos agrupacion)
        {
            object clave = lector.GetValue(0);
            int? id = null;
            DateOnly? dia = null;
            string etiqueta;
            switch (agrupacion)
            {
                case AgrupacionDepositos.Dia:
                    dia = DateOnly.FromDateTime((DateTime)clave);
                    etiqueta = dia.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    break;
                case AgrupacionDepositos.Usuario:
                    id = clave is DBNull ? null : ConversionOracle.Entero(clave);
                    etiqueta = id is null ? "Sin ciudadano (apertura manual)" : $"Usuario #{id}";
                    break;
                default:
                    id = ConversionOracle.Entero(clave);
                    etiqueta = lector.GetString(1);
                    break;
            }

            return new FilaAgrupada(
                id,
                dia,
                etiqueta,
                ConversionOracle.Entero(lector.GetValue(2)),
                ConversionOracle.Entero(lector.GetValue(3)),
                ConversionOracle.Entero(lector.GetValue(4)),
                Decimal(lector.GetValue(5)),
                ConversionOracle.Entero(lector.GetValue(6)),
                Decimal(lector.GetValue(7)));
        }

        private static FilaFallosContenedor LeerFallos(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            lector.GetString(1),
            ConversionOracle.Entero(lector.GetValue(2)),
            ConversionOracle.Entero(lector.GetValue(3)),
            ConversionOracle.Entero(lector.GetValue(4)),
            ConversionOracle.Entero(lector.GetValue(5)));

        private static FilaDeposito LeerDeposito(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            ConversionOracle.FechaUtc(lector.GetValue(1)),
            ConversionOracle.Entero(lector.GetValue(2)),
            lector.GetString(3),
            lector.GetValue(4) is DBNull ? null : ConversionOracle.Entero(lector.GetValue(4)),
            lector.GetString(5),
            ConversionOracle.TextoOpcional(lector.GetValue(6)),
            DecimalOpcional(lector.GetValue(7)),
            DecimalOpcional(lector.GetValue(8)));

        private static decimal Decimal(object valor) => Convert.ToDecimal(valor, CultureInfo.InvariantCulture);

        private static decimal? DecimalOpcional(object valor) => valor is DBNull ? null : Decimal(valor);
    }
}
