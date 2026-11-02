using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Otra sesión de Oracle con su propia transacción abierta. Sirve para que una prueba retenga filas
    // (UPDATE o SELECT … FOR UPDATE sin confirmar) y compruebe de forma determinista que el repositorio
    // espera, en lugar de depender de que dos tareas coincidan por azar. Si no se confirma, se deshace al liberarse.
    internal sealed class SesionAjenaOracle : IDisposable
    {
        private readonly OracleConnection _conexion;
        private readonly OracleTransaction _transaccion;
        private bool _terminada;
        private bool _liberada;

        private SesionAjenaOracle(OracleConnection conexion, OracleTransaction transaccion)
        {
            _conexion = conexion;
            _transaccion = transaccion;
        }

        public static async Task<SesionAjenaOracle> AbrirAsync(FabricaConexionesOracle fabrica)
        {
            OracleConnection conexion = await fabrica.AbrirAsync(CancellationToken.None);
            var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync();
            return new SesionAjenaOracle(conexion, transaccion);
        }

        /// <summary>Ejecuta una sentencia con un único parámetro <c>:id</c> dentro de la transacción retenida.</summary>
        public async Task EjecutarAsync(string sql, int id)
        {
            using OracleCommand comando = ComandosOracle.Crear(_conexion, sql, _transaccion);
            comando.Agregar("id", OracleDbType.Int32, id);
            if (!sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            {
                await comando.ExecuteNonQueryAsync();
                return;
            }

            // Un SELECT … FOR UPDATE se recorre completo para que el bloqueo quede tomado sobre todas sus filas.
            using OracleDataReader lector = (OracleDataReader)await comando.ExecuteReaderAsync();
            while (await lector.ReadAsync())
            {
            }
        }

        public async Task ConfirmarAsync()
        {
            await _transaccion.CommitAsync();
            _terminada = true;
        }

        /// <summary>Deshace lo no confirmado y cierra la sesión; se puede llamar más de una vez.</summary>
        public void Dispose()
        {
            if (_liberada)
            {
                return;
            }

            _liberada = true;
            if (!_terminada)
            {
                _transaccion.Rollback();
            }

            _transaccion.Dispose();
            _conexion.Dispose();
        }

        /// <summary>Falla si la operación ya terminó: debe seguir esperando el bloqueo de esta sesión.</summary>
        public static async Task AfirmarQueEsperaAsync(Task operacion)
        {
            Task primera = await Task.WhenAny(operacion, Task.Delay(TimeSpan.FromSeconds(1)));
            Assert.AreNotSame(operacion, primera, "La operación terminó sin esperar el bloqueo de la otra sesión.");
        }
    }
}
