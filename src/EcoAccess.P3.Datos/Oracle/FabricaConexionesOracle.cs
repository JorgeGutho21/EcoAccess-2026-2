using System;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Oracle
{

    /// <summary>
    /// Abre conexiones a Oracle con la cadena que recibe de quien compone la aplicación (Program.cs o las
    /// pruebas de integración), que la lee de los User Secrets <c>ecoaccess-p3</c>, clave
    /// <c>ConnectionStrings:OracleP3</c>. La cadena nunca se escribe en el código ni en los mensajes
    /// de error: contiene la contraseña.
    /// </summary>
    public sealed class FabricaConexionesOracle
    {
        private readonly string _cadenaConexion;

        public FabricaConexionesOracle(string cadenaConexion)
        {
            if (Compat.IsNullOrWhiteSpace(cadenaConexion))
            {
                throw new ArgumentException("La cadena de conexión de Oracle está vacía.", nameof(cadenaConexion));
            }

            _cadenaConexion = cadenaConexion;
        }

        /// <summary>Devuelve una conexión abierta; quien la pide la cierra con <c>await using</c>.</summary>
        internal async Task<OracleConnection> AbrirAsync(CancellationToken cancellationToken)
        {
            var conexion = new OracleConnection(_cadenaConexion);
            try
            {
                await conexion.OpenAsync(cancellationToken).ConfigureAwait(false);
                return conexion;
            }
            catch
            {
                conexion.Dispose();
                throw;
            }
        }
    }
}
