using System;
using System.Globalization;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace EcoAccess.P3.Datos.Oracle
{

    /// <summary>Creación de comandos con los valores por defecto del proyecto.</summary>
    internal static class ComandosOracle
    {
        /// <summary>
        /// ODP.NET enlaza los parámetros por posición si no se indica lo contrario; con
        /// <see cref="OracleCommand.BindByName"/> se enlazan por nombre y el orden en el SQL deja de importar.
        /// </summary>
        public static OracleCommand Crear(OracleConnection conexion, string sql, OracleTransaction? transaccion = null)
        {
            var comando = conexion.CreateCommand();
            comando.CommandText = sql;
            comando.BindByName = true;
            comando.Transaction = transaccion;
            // Sin límite, una conexión con Oracle Cloud que se cae (cambio de red, equipo suspendido) dejaba la llamada
            // esperando para siempre: el bridge de la caneca se congelaba sin error (21/11/2026). Quien necesite otro
            // valor lo cambia después de crear el comando.
            comando.CommandTimeout = TiempoMaximoSegundos;
            return comando;
        }

        /// <summary>Límite por defecto de cada sentencia, en segundos.</summary>
        public const int TiempoMaximoSegundos = 30;

        public static OracleParameter Agregar(this OracleCommand comando, string nombre, OracleDbType tipo, object? valor)
        {
            return comando.Parameters.Add(nombre, tipo, valor ?? DBNull.Value, System.Data.ParameterDirection.Input);
        }

        /// <summary>Parámetro de salida para <c>RETURNING id INTO :id</c>.</summary>
        public static OracleParameter AgregarIdSalida(this OracleCommand comando, string nombre = "id")
        {
            return comando.Parameters.Add(nombre, OracleDbType.Decimal, System.Data.ParameterDirection.Output);
        }

        /// <summary>
        /// Lee el id de un parámetro de salida. ODP.NET lo entrega como tipo propio (<see cref="OracleDecimal"/>
        /// para <see cref="OracleDbType.Decimal"/>); se aceptan también tipos .NET por si cambia el proveedor.
        /// </summary>
        public static int LeerId(OracleParameter parametro) => parametro.Value switch
        {
            OracleDecimal { IsNull: false } numero => numero.ToInt32(),
            IConvertible valor when parametro.Value is not DBNull => Convert.ToInt32(valor, CultureInfo.InvariantCulture),
            _ => throw new InvalidOperationException("La base de datos no devolvió el identificador generado."),
        };

        /// <summary>Un UPDATE por id debe tocar exactamente una fila; si no, la entidad no existe.</summary>
        public static void ExigirUnaFila(int filas, string entidad, int id)
        {
            if (filas != 1)
            {
                throw new InvalidOperationException($"No existe {entidad} con id {id}.");
            }
        }
    }
}
