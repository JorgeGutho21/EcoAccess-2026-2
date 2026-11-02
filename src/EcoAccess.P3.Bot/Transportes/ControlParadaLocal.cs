using System;
using System.IO;
using System.Threading;
using EcoAccess.P3.Bot.Dispositivos;

namespace EcoAccess.P3.Bot.Transportes
{

    /// <summary>El supervisor local solicita cancelación mediante un archivo de su sesión privada; nunca mata procesos.</summary>
    public sealed class ControlParadaLocal : IDisposable
    {
        public const string Variable = "ECOACCESS_PARADA_LOCAL";
        private readonly Timer? _observador;
        public ControlParadaLocal(CancellationTokenSource parar, string? archivo = null)
        {
            archivo ??= Environment.GetEnvironmentVariable(Variable);
            if (Compat.IsNullOrWhiteSpace(archivo)) return;
            if (!Compat.IsPathFullyQualified(archivo) || !Directory.Exists(Path.GetDirectoryName(archivo)))
                throw new ArgumentException("El archivo de parada requiere una carpeta privada existente.");
            ConfiguracionBridge.ValidarRutaPrivada(Path.GetDirectoryName(archivo)!);
            _observador = new Timer(_ =>
            {
                if (!File.Exists(archivo) || parar.IsCancellationRequested) return;
                try { parar.Cancel(); }
                catch (ObjectDisposedException) { }
                catch (AggregateException) { } // La señal ya se distribuyó; el host conserva su cierre y drenaje.
            }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(250));
        }
        public void Dispose() => _observador?.Dispose();
    }
}
