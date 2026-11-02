using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Dispositivos
{

    /// <summary>Diario local del prototipo: reserva orden y nonce antes de actuar, sin borrar marcas al reiniciar.</summary>
    /// <remarks>No sustituye NVS del ESP32, almacenamiento replicado ni pruebas de corte eléctrico.</remarks>
    public sealed class RegistroEjecucionArchivo : IRegistroEjecucionDispositivo
    {
        private readonly string _directorio;
        public RegistroEjecucionArchivo(string directorio)
        {
            Compat.ThrowIfNullOrWhiteSpace(directorio, nameof(directorio));
            _directorio = Path.GetFullPath(directorio);
            Directory.CreateDirectory(_directorio);
        }

        public bool IntentarReservar(ComandoApertura comando)
        {
            if (comando is null || comando.IdContenedor <= 0 || comando.IdOrden <= 0
                || Compat.IsNullOrWhiteSpace(comando.ClaveIdempotencia) || comando.ClaveIdempotencia.Length > 80) return false;
            string orden = FormattableString.Invariant($"orden-{comando.IdContenedor}-{comando.IdOrden}");
            string nonce = Compat.ToHexString(Compat.Sha256(Encoding.UTF8.GetBytes(
                FormattableString.Invariant($"{comando.IdContenedor}\n{comando.ClaveIdempotencia}"))));
            try
            {
                // CreateNew excluye otros procesos. Si falla la segunda marca se conserva la primera: fallo seguro.
                Reservar(orden);
                Reservar("nonce-" + nonce);
                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        private void Reservar(string nombre)
        {
            using var archivo = new FileStream(Path.Combine(_directorio, nombre), FileMode.CreateNew, FileAccess.Write, FileShare.None,
                1, FileOptions.WriteThrough);
            archivo.WriteByte(1);
            archivo.Flush(flushToDisk: true);
        }
    }
}
