using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using System.Diagnostics;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>Solo SELECT sobre DUAL y vistas USER_*; no necesita privilegios de DBA.</summary>
    public sealed class RepositorioDiagnosticoOracle : IRepositorioDiagnosticoOracle
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioDiagnosticoOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<EstadoEsquemaOracle> ConsultarAsync(CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            string esquema;
            string contenedor;
            double latencia;
            using (OracleCommand comando = Crear(conexion,
                "SELECT SYS_CONTEXT('USERENV', 'SESSION_USER'), SYS_CONTEXT('USERENV', 'CON_NAME') FROM dual"))
            {
                long inicio = Stopwatch.GetTimestamp();
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                    throw new InvalidOperationException("Oracle no devolvió su identidad de sesión.");
                latencia = Compat.GetElapsedTime(inicio).TotalMilliseconds;
                esquema = lector.GetString(0);
                contenedor = lector.GetString(1);
            }

            var tablas = new List<string>();
            using (OracleCommand comando = Crear(conexion, "SELECT table_name FROM user_tables ORDER BY table_name"))
            using (DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false)) tablas.Add(lector.GetString(0));
            }

            var restriccionesValidadas = new List<string>();
            using (OracleCommand comando = Crear(conexion,
                "SELECT table_name || ':' || constraint_name FROM user_constraints WHERE constraint_type = 'C' AND status = 'ENABLED' AND validated = 'VALIDATED'"))
            using (DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
            {
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false)) restriccionesValidadas.Add(lector.GetString(0));
            }
            int restricciones = await ContarAsync(conexion,
                // Oracle representa WITH READ ONLY de una vista con una restricción O no validada;
                // no es una FK, PK o CHECK de datos deshabilitada.
                "SELECT COUNT(*) FROM user_constraints WHERE constraint_type IN ('P','U','R','C') AND (status <> 'ENABLED' OR validated <> 'VALIDATED')", cancellationToken);
            int objetos = await ContarAsync(conexion,
                "SELECT COUNT(*) FROM user_objects WHERE status <> 'VALID'", cancellationToken);
            int indices = await ContarAsync(conexion,
                "SELECT COUNT(*) FROM user_indexes WHERE status NOT IN ('VALID', 'N/A')", cancellationToken);
            return new(esquema, contenedor, latencia, tablas.AsReadOnly(), restricciones, objetos, indices, restriccionesValidadas.AsReadOnly());
        }

        private static OracleCommand Crear(OracleConnection conexion, string sql)
        {
            OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            comando.CommandTimeout = 10;
            return comando;
        }

        private static async Task<int> ContarAsync(OracleConnection conexion, string sql, CancellationToken cancellationToken)
        {
            using OracleCommand comando = Crear(conexion, sql);
            object? valor = await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return ConversionOracle.Entero(valor!);
        }
    }
}
