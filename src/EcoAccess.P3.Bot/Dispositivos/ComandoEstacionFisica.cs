using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;

namespace EcoAccess.P3.Bot.Dispositivos
{
    /// <summary>
    /// Comando CLI supervisado (--probar-estacion-fisica) para pruebas presenciales de las 3 canecas físicas sobre COM3
    /// (individuales y combinaciones de 2 o 3) mediante CoordinadorEstacionUnificado + EjecutorEstacionSerialHmac.
    /// Exige confirmación explícita (--confirmar-presencia-supervisada) y no modifica datos en Oracle Cloud.
    /// </summary>
    internal static class ComandoEstacionFisica
    {
        public static readonly IReadOnlyList<CanecaEstacion> EstacionEstandar = new[]
        {
            new CanecaEstacion(1093, "Blanca (Aprovechables)", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde (Orgánicos)", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra (No aprovechables)", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        public static async Task<int> EjecutarAsync(string[] args)
        {
            bool presenciaConfirmada = args.Any(a => string.Equals(a, "--confirmar-presencia-supervisada", StringComparison.OrdinalIgnoreCase));
            bool modoDemo = args.Any(a => string.Equals(a, "--simulado", StringComparison.OrdinalIgnoreCase));
            string[] tokensCanecas = args.Skip(1)
                .Where(a => !a.StartsWith("--", StringComparison.Ordinal))
                .ToArray();

            if (tokensCanecas.Length == 0)
            {
                Console.Error.WriteLine("Uso: EcoAccess.P3.Bot --probar-estacion-fisica <verde|blanca|negra ...> [--simulado | --confirmar-presencia-supervisada]");
                return 2;
            }

            string comandoSintetico = tokensCanecas.Length == 1
                ? "/" + tokensCanecas[0].Trim().ToLowerInvariant()
                : "/abrir " + string.Join(" ", tokensCanecas.Select(t => t.Trim().ToLowerInvariant()));

            if (!CoordinadorEstacionUnificado.IntentarParsearComandoCanecas(comandoSintetico, out IReadOnlyList<CategoriaMaterial> categorias))
            {
                Console.Error.WriteLine($"Combinación inválida: '{string.Join(" ", tokensCanecas)}'. Usa verde, blanca y/o negra.");
                return 2;
            }

            if (!modoDemo && !presenciaConfirmada)
            {
                Console.Error.WriteLine("SEGURIDAD: Se requiere --confirmar-presencia-supervisada para accionar hardware físico real (o --simulado para modo demo).");
                return 3;
            }

            var sim = new SimuladorEstacionTresCanecas(
                new Dictionary<int, DestinoResiduo>
                {
                    [1093] = DestinoResiduo.Blanca,
                    [1400] = DestinoResiduo.Verde,
                    [1401] = DestinoResiduo.Negra,
                },
                TimeProvider.System,
                TimeSpan.Zero);

            using EjecutorEstacionSerialHmac? fisico = modoDemo ? null : EjecutorEstacionSerialHmac.IntentarCargarDesdeProgramData(TimeProvider.System);
            if (!modoDemo && fisico is null)
            {
                Console.Error.WriteLine("No se encontraron las configuraciones HMAC privadas en ProgramData\\EcoAccessP3Framework\\Bridge.");
                return 1;
            }

            var coordinador = new CoordinadorEstacionUnificado(sim, EstacionEstandar, fisico);
            if (!modoDemo)
            {
                coordinador.HabilitarSesionFisicaSupervisada(TimeSpan.FromMinutes(10), "Jorge Gutierrez (presencial)", conSensoresFisicos: false);
            }

            var sw = Stopwatch.StartNew();
            string clave = $"cli:{DateTime.UtcNow:yyyyMMddHHmmssfff}:{Guid.NewGuid():N}".Substring(0, 48);
            var resultado = await coordinador.SolicitarAperturaAsync(
                clave,
                OrigenSolicitudEstacion.WindowsPc,
                "Jorge Gutierrez",
                categorias,
                solicitarModoFisico: !modoDemo,
                usuarioAutorizado: true,
                CancellationToken.None);
            sw.Stop();

            var salida = new
            {
                resultado.ClaveIdempotencia,
                FechaUtc = resultado.FechaUtc.ToString("O"),
                Combinacion = resultado.Combinacion.ToString(),
                Modo = resultado.ModoEjecucion.ToString(),
                resultado.Autorizada,
                resultado.Exitosa,
                DuracionMs = sw.ElapsedMilliseconds,
                resultado.Mensaje,
                Canecas = resultado.ResultadosCanecas.Select(r => new
                {
                    Caneca = MotorReglasMultirresiduo.NombreCortoCaneca(r.Caneca),
                    r.IdContenedor,
                    Estado = r.EstadoControlador.ToString(),
                    r.AperturaConfirmada,
                    r.CierreConfirmado,
                    r.EsSimulado,
                    r.TieneSensorFisico,
                    r.ConfirmacionObservacionalRequerida,
                    r.Mensaje,
                }).ToArray(),
            };

            Console.WriteLine(JsonSerializer.Serialize(salida, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            }));
            return resultado.Exitosa ? 0 : 1;
        }
    }
}
