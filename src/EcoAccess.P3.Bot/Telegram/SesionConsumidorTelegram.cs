using System;
using System.IO;
using System.Globalization;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>Exclusión local por identidad pública del bot, compartida entre polling y webhook.</summary>
    public sealed class SesionConsumidorTelegram : IDisposable
    {
        private readonly FileStream _bloqueo;
        public SesionConsumidorTelegram(long idBot, string? directorio = null)
        {
            if (idBot <= 0) throw new ArgumentOutOfRangeException(nameof(idBot));
            directorio ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3", "Telegram");
            if (!Compat.IsPathFullyQualified(directorio)) throw new ArgumentException("El bloqueo requiere una carpeta absoluta estable.");
            Directory.CreateDirectory(directorio);
            // No contiene token, cadena Oracle ni mensajes. Nunca borrar mientras haya un consumidor activo.
            _bloqueo = new FileStream(Path.Combine(directorio, idBot.ToString(CultureInfo.InvariantCulture) + ".lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public void Dispose() => _bloqueo.Dispose();
    }
}
