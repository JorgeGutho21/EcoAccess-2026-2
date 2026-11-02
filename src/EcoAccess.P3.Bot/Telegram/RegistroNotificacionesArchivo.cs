using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Telegram
{

    /// <summary>
    /// Avisos ya enviados en un JSON local del perfil (sin datos personales: solo claves como «de:1252» y fechas).
    /// Escritura atómica: un corte deja el archivo anterior o el nuevo, nunca uno a medias.
    /// </summary>
    public sealed class RegistroNotificacionesArchivo : IRegistroNotificacionesEnviadas
    {
        private readonly string _ruta;

        public RegistroNotificacionesArchivo(string ruta)
        {
            _ruta = ruta;
        }

        private sealed record Archivo(int Version, Dictionary<string, DateTime> Enviadas, Dictionary<string, int> Fallos);

        public EstadoNotificaciones? Cargar()
        {
            if (!File.Exists(_ruta)) return null;
            try
            {
                if (new FileInfo(_ruta).Length > 4 * 1024 * 1024) return null;
                var archivo = JsonSerializer.Deserialize<Archivo>(File.ReadAllBytes(_ruta));
                return archivo is { Version: 1, Enviadas: not null, Fallos: not null } ? new EstadoNotificaciones(archivo.Enviadas, archivo.Fallos) : null;
            }
            catch (JsonException) { return null; }
        }

        public void Guardar(EstadoNotificaciones estado)
        {
            Compat.ThrowIfNull(estado, nameof(estado));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_ruta))!);
            string temporal = _ruta + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporal, JsonSerializer.SerializeToUtf8Bytes(new Archivo(1, estado.Enviadas, estado.Fallos)));
                Compat.MoverSobrescribiendo(temporal, _ruta);
            }
            finally { if (File.Exists(temporal)) File.Delete(temporal); }
        }
    }
}
