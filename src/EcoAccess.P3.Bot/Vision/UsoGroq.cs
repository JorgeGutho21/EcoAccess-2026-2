using System;
using System.IO;
using System.Text.Json;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Consumo diario de Groq, guardado en ProgramData para que reiniciar el bot no borre el presupuesto. Sin imágenes,
    /// claves ni datos de personas. El día es UTC y se reinicia solo.
    /// </summary>
    public sealed class UsoGroq
    {
        public sealed record Consumo(DateTime DiaUtc, int Solicitudes, long TokensEntrada, long TokensSalida, decimal CostoUsd);

        public static string ArchivoPredeterminado { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Vision", "uso-groq.json");

        private readonly string _archivo;
        private readonly TimeProvider _reloj;
        private readonly object _cerrojo = new();
        private Consumo _actual;

        public UsoGroq(string? archivo = null, TimeProvider? reloj = null)
        {
            _archivo = archivo ?? ArchivoPredeterminado;
            _reloj = reloj ?? TimeProvider.System;
            _actual = Leer(_archivo) ?? new Consumo(Hoy, 0, 0, 0, 0m);
        }

        private DateTime Hoy => DateTime.SpecifyKind(_reloj.GetUtcNow().UtcDateTime.Date, DateTimeKind.Utc);

        public Consumo Actual { get { lock (_cerrojo) { return Vigente(); } } }

        /// <summary>Cuenta la solicitud antes de enviarla: una que falle también puede costar y no debe saltarse el límite.</summary>
        public bool Reservar(int maximoSolicitudes, decimal presupuestoUsd)
        {
            lock (_cerrojo)
            {
                Consumo hoy = Vigente();
                if (hoy.Solicitudes >= maximoSolicitudes || hoy.CostoUsd >= presupuestoUsd) return false;
                _actual = hoy with { Solicitudes = hoy.Solicitudes + 1 };
                Guardar();
                return true;
            }
        }

        public void RegistrarTokens(long entrada, long salida, decimal costoUsd)
        {
            lock (_cerrojo)
            {
                Consumo hoy = Vigente();
                _actual = hoy with { TokensEntrada = hoy.TokensEntrada + entrada, TokensSalida = hoy.TokensSalida + salida, CostoUsd = hoy.CostoUsd + costoUsd };
                Guardar();
            }
        }

        private Consumo Vigente() => _actual.DiaUtc == Hoy ? _actual : new Consumo(Hoy, 0, 0, 0, 0m);

        private void Guardar()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_archivo)!);
                string temporal = _archivo + ".tmp";
                File.WriteAllText(temporal, JsonSerializer.Serialize(_actual));
                Compat.MoverSobrescribiendo(temporal, _archivo);
            }
            catch (Exception) { /* El registro no interrumpe la foto; el límite en memoria sigue activo. */ }
        }

        public static Consumo? Leer(string? archivo = null)
        {
            try
            {
                archivo ??= ArchivoPredeterminado;
                return File.Exists(archivo) ? JsonSerializer.Deserialize<Consumo>(File.ReadAllText(archivo)) : null;
            }
            catch (Exception) { return null; }
        }
    }
}
