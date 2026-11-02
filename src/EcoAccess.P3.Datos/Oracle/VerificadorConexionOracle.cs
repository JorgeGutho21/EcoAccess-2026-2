using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Oracle
{

    /// <summary>Resultado de comprobar una conexión; nunca incluye la cadena, el usuario ni la contraseña.</summary>
    public enum EstadoConexionOracle
    {
        Correcta = 1,
        CredencialesInvalidas = 2,
        CuentaBloqueada = 3,
        ServicioNoDisponible = 4,
        EsquemaIncompleto = 5,
        CadenaInvalida = 6,
        Error = 7
    }

    /// <summary>
    /// Comprueba con una operación real que la conexión abre y que el esquema tiene las tablas de EcoAccess. Hace un
    /// solo intento por llamada: repetir credenciales incorrectas puede bloquear la cuenta en Oracle.
    /// </summary>
    public static class VerificadorConexionOracle
    {
        /// <summary>Tablas mínimas sin las que la aplicación no puede funcionar.</summary>
        public static readonly IReadOnlyList<string> TablasRequeridas = new string[] { "USUARIO", "CREDENCIAL_ACCESO", "MATERIAL", "CONTENEDOR", "INTERACCION", "ORDEN_APERTURA", "DEPOSITO" };

        public static async Task<EstadoConexionOracle> ComprobarAsync(string? cadena, CancellationToken cancellationToken = default)
        {
            if (Compat.IsNullOrWhiteSpace(cadena)) return EstadoConexionOracle.CadenaInvalida;
            OracleConnection conexion;
            try { conexion = new OracleConnection(cadena); }
            catch (ArgumentException) { return EstadoConexionOracle.CadenaInvalida; }

            using (conexion)
            {
                try
                {
                    await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);
                    using var comando = conexion.CreateCommand();
                    // Consulta del diccionario propio del esquema: no lee ni modifica datos de personas.
                    comando.CommandText = "SELECT COUNT(*) FROM user_tables WHERE table_name IN (" +
                        string.Join(",", TablasRequeridas.Select((_, i) => ":t" + i)) + ")";
                    comando.BindByName = true;
                    for (int i = 0; i < TablasRequeridas.Count; i++) comando.Parameters.Add("t" + i, TablasRequeridas[i]);
                    int encontradas = Convert.ToInt32(await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
                    return encontradas == TablasRequeridas.Count ? EstadoConexionOracle.Correcta : EstadoConexionOracle.EsquemaIncompleto;
                }
                catch (OracleException ex)
                {
                    return ex.Number switch
                    {
                        1017 or 1005 => EstadoConexionOracle.CredencialesInvalidas,
                        28000 => EstadoConexionOracle.CuentaBloqueada,
                        _ => EstadoConexionOracle.ServicioNoDisponible
                    };
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return EstadoConexionOracle.ServicioNoDisponible; }
                catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or TimeoutException or System.Net.Sockets.SocketException)
                {
                    return EstadoConexionOracle.ServicioNoDisponible;
                }
            }
        }
    }
}
