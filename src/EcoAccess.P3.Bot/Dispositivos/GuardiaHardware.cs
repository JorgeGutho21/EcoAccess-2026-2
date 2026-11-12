using System;
using System.Collections.Concurrent;
using System.Linq;

namespace EcoAccess.P3.Bot.Dispositivos
{
    /// <summary>Se lanzó porque este proceso no puede comunicarse con la estación física.</summary>
    public sealed class HardwareBloqueadoException : InvalidOperationException
    {
        public HardwareBloqueadoException(string mensaje) : base(mensaje) { }
    }

    /// <summary>
    /// Barrera de proceso entre el software y la estación física (misión nocturna del 12/11/2026). Las dos únicas salidas
    /// hacia una tapa son el puerto COM (<see cref="EnlacePuertoSerial"/>) y el envío HTTP a un bridge
    /// (<see cref="CanalDispositivoHttp"/>); ambas preguntan aquí antes de actuar.
    /// <para>
    /// El proceso queda bloqueado si: (1) corre dentro de un ejecutor de pruebas (MSTest, xUnit, NUnit o testhost), lo
    /// detecta solo y no depende de que la prueba lo recuerde; (2) la variable <c>ECOACCESS_SIN_HARDWARE</c> vale 1; o
    /// (3) alguien llamó a <see cref="BloquearEsteProceso"/>. El bloqueo no se puede deshacer dentro del proceso.
    /// </para>
    /// <para>
    /// Bloqueado, no se abre ningún COM, y un comando HTTP solo puede ir a un puerto que escuche ESTE mismo proceso (un
    /// bridge simulado o de prueba). Así una prueba nunca alcanza un bridge real que esté corriendo en otra ventana.
    /// </para>
    /// </summary>
    public static class GuardiaHardware
    {
        public const string Variable = "ECOACCESS_SIN_HARDWARE";

        private static readonly string[] EjecutoresDePrueba =
        {
            "Microsoft.VisualStudio.TestPlatform.TestFramework", "MSTest.TestFramework", "xunit.core", "nunit.framework", "testhost",
        };

        private static readonly ConcurrentDictionary<int, byte> PuertosPropios = new();
        private static volatile string? _motivoExplicito;
        private static readonly Lazy<string?> MotivoAutomatico = new(Detectar);

        /// <summary>Por qué está bloqueado este proceso, o null si puede hablar con el hardware.</summary>
        public static string? Motivo => _motivoExplicito ?? MotivoAutomatico.Value ?? MotivoVariable();

        public static bool Bloqueado => Motivo is not null;

        /// <summary>Bloquea el hardware en este proceso para siempre. Un segundo llamado conserva el primer motivo.</summary>
        public static void BloquearEsteProceso(string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo)) throw new ArgumentException("Indica el motivo del bloqueo.", nameof(motivo));
            _motivoExplicito ??= motivo.Trim();
        }

        /// <summary>Lanza si este proceso no puede usar <paramref name="recurso"/>.</summary>
        /// <exception cref="HardwareBloqueadoException"/>
        public static void ExigirPermitido(string recurso)
        {
            if (Motivo is { } motivo)
                throw new HardwareBloqueadoException($"Acceso a {recurso} bloqueado en este proceso: {motivo}. No se envió nada a la estación.");
        }

        /// <summary>Un servidor HTTP de dispositivo que vive en este proceso; solo a esos puertos puede enviar un proceso bloqueado.</summary>
        internal static void RegistrarPuertoPropio(int puerto) => PuertosPropios[puerto] = 0;

        internal static void LiberarPuertoPropio(int puerto) => PuertosPropios.TryRemove(puerto, out _);

        /// <summary>Lanza si el proceso está bloqueado y el destino no es un servidor de este mismo proceso.</summary>
        internal static void ExigirDestinoSeguro(Uri destino)
        {
            if (!Bloqueado || PuertosPropios.ContainsKey(destino.Port)) return;
            ExigirPermitido($"el bridge de {destino.Host}:{destino.Port} (otro proceso)");
        }

        private static string? MotivoVariable() =>
            Environment.GetEnvironmentVariable(Variable) == "1" ? $"la variable {Variable} vale 1" : null;

        private static string? Detectar()
        {
            string proceso;
            try { proceso = System.Diagnostics.Process.GetCurrentProcess().ProcessName; }
            catch (Exception) { proceso = ""; }
            if (proceso.StartsWith("testhost", StringComparison.OrdinalIgnoreCase) || proceso.StartsWith("vstest", StringComparison.OrdinalIgnoreCase))
                return "proceso de pruebas automatizadas";
            string? marco = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name)
                .FirstOrDefault(n => EjecutoresDePrueba.Any(e => string.Equals(e, n, StringComparison.OrdinalIgnoreCase)));
            return marco is null ? null : $"marco de pruebas cargado ({marco})";
        }
    }
}
