using System;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    /// <summary>
    /// Estado del diálogo del bot (<c>conversacion_telegram</c>) con versión optimista, y registro de updates ya
    /// procesados (<c>update_telegram</c>). Dos mensajes de la misma persona atendidos a la vez no se
    /// pisan: el segundo en escribir encuentra otra versión y falla con <see cref="CambioConcurrenteException"/>.
    /// </summary>
    public sealed class RepositorioConversacionesTelegramOracle : IRepositorioConversacionesTelegram
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioConversacionesTelegramOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string MensajeCambio = "La conversación cambió mientras se atendía otro mensaje.";

        public async Task<ConversacionTelegram?> ObtenerAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT telegram_user_id, chat_id, estado, id_contenedor, id_interaccion, id_material_propuesto, " +
                "fecha_actualizacion_utc, version FROM conversacion_telegram WHERE telegram_user_id = :telegram");
            comando.Agregar("telegram", OracleDbType.Int64, telegramUserId);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                return null;
            }

            return new ConversacionTelegram(
                RepositorioVinculosTelegramOracle.Largo(lector.GetValue(0)),
                RepositorioVinculosTelegramOracle.Largo(lector.GetValue(1)),
                (EstadoConversacion)ConversionOracle.Entero(lector.GetValue(2)),
                EnteroOpcional(lector.GetValue(3)),
                EnteroOpcional(lector.GetValue(4)),
                EnteroOpcional(lector.GetValue(5)),
                ConversionOracle.FechaUtc(lector.GetValue(6)),
                ConversionOracle.Entero(lector.GetValue(7)));
        }

        /// <summary>
        /// Versión 0: inserta con versión 1; si otra sesión la insertó antes, la llave primaria lo detecta. Versión
        /// mayor: actualiza solo si la versión guardada es la leída y la incrementa.
        /// </summary>
        public async Task GuardarAsync(ConversacionTelegram conversacion, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(conversacion, nameof(conversacion));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            string sql = conversacion.Version == 0
                ? "INSERT INTO conversacion_telegram (telegram_user_id, chat_id, estado, id_contenedor, id_interaccion, " +
                  "id_material_propuesto, fecha_actualizacion_utc, version) " +
                  "VALUES (:telegram, :chat, :estado, :contenedor, :interaccion, :material, :fecha, 1)"
                : "UPDATE conversacion_telegram SET chat_id = :chat, estado = :estado, id_contenedor = :contenedor, " +
                  "id_interaccion = :interaccion, id_material_propuesto = :material, fecha_actualizacion_utc = :fecha, " +
                  "version = version + 1 WHERE telegram_user_id = :telegram AND version = :version";
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            comando.Agregar("telegram", OracleDbType.Int64, conversacion.TelegramUserId);
            comando.Agregar("chat", OracleDbType.Int64, conversacion.ChatId);
            comando.Agregar("estado", OracleDbType.Int32, (int)conversacion.Estado);
            comando.Agregar("contenedor", OracleDbType.Int32, conversacion.IdContenedor);
            comando.Agregar("interaccion", OracleDbType.Int32, conversacion.IdInteraccion);
            comando.Agregar("material", OracleDbType.Int32, conversacion.IdMaterialPropuesto);
            comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(conversacion.FechaActualizacionUtc));
            if (conversacion.Version != 0)
            {
                comando.Agregar("version", OracleDbType.Int32, conversacion.Version);
            }

            int filas;
            try
            {
                filas = await TraductorErroresOracle.TraducirReferenciaAsync(
                    () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            }
            catch (ConflictoUnicidadException error)
            {
                throw new CambioConcurrenteException(MensajeCambio, error);
            }

            if (filas != 1)
            {
                throw new CambioConcurrenteException(MensajeCambio);
            }
        }

        public async Task<bool> RegistrarUpdateAsync(long updateId, DateTime recibidoUtc, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO update_telegram (update_id, recibido_utc) VALUES (:id, :recibido)");
            comando.Agregar("id", OracleDbType.Int64, updateId);
            comando.Agregar("recibido", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(recibidoUtc));
            try
            {
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OracleException error) when (error.Number == TraductorErroresOracle.ViolacionUnicidad)
            {
                return false;
            }
        }

        private static int? EnteroOpcional(object valor) => valor is DBNull ? null : ConversionOracle.Entero(valor);
    }
}
