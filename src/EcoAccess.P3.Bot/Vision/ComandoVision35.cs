using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Vision35;

namespace EcoAccess.P3.Bot.Vision
{
    /// <summary>
    /// Comandos CLI desacoplados para auditoría de capacidades de Ollama (Qwen/LFM3-VL) y ejecución repetible
    /// del banco de pruebas de los 35 residuos prioritarios y combinaciones C01–C07.
    /// No modifica Oracle ni activa hardware físico.
    /// </summary>
    internal static class ComandoVision35
    {
        public static async Task<int> AuditarOllamaAsync(string[] args, CancellationToken ct = default)
        {
            string? url = LeerOpcion(args, "--url");
            string? modelo = LeerOpcion(args, "--modelo");
            using var cliente = new ClienteVisionOllamaMultirresiduo(url, modelo);
            AuditoriaOllamaVision auditoria = await cliente.AuditarCapacidadesAsync(ct).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(auditoria, EjecutorEvaluacion35.OpcionesJson));
            return auditoria.ServicioDisponible && auditoria.ModeloVisionSeleccionado is not null ? 0 : 1;
        }

        public static async Task<int> ProbarImagenAsync(string[] args, CancellationToken ct = default)
        {
            if (args.Length < 2 || !File.Exists(args[1]))
            {
                Console.Error.WriteLine("Uso: EcoAccess.P3.Bot --probar-imagen35 <ruta-imagen> [--modelo <nombre>] [--url <url>]");
                return 2;
            }

            string ruta = Path.GetFullPath(args[1]);
            string? url = LeerOpcion(args, "--url");
            string? modelo = LeerOpcion(args, "--modelo");
            byte[] bytes = File.ReadAllBytes(ruta);

            using var cliente = new ClienteVisionOllamaMultirresiduo(url, modelo);
            var motor = new MotorReglasMultirresiduo();
            RespuestaVisionEstructurada obs = await cliente.ObservarAsync(bytes, ct).ConfigureAwait(false);
            ResultadoAnalisisMultirresiduo res = motor.Evaluar(obs);

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                Imagen = Path.GetFileName(ruta),
                Observacion = obs,
                Dictamen = res,
            }, EjecutorEvaluacion35.OpcionesJson));
            return res.EstadoGlobal == EstadoGlobalAnalisis.ModoSeguroError ? 1 : 0;
        }

        public static async Task<int> EvaluarManifiestoAsync(string[] args, CancellationToken ct = default)
        {
            if (args.Length < 2 || !File.Exists(args[1]))
            {
                Console.Error.WriteLine("Uso: EcoAccess.P3.Bot --evaluar-catalogo35 <manifiesto.json> [--material V01] [--categoria VERDE] [--combinacion C07_VerdeBlancaNegra] [--limite N] [--repetir-fallidos informe.json] [--modelo nombre] [--salida prefijo]");
                return 2;
            }

            string manifiesto = Path.GetFullPath(args[1]);
            string? url = LeerOpcion(args, "--url");
            string? modelo = LeerOpcion(args, "--modelo");
            string? material = LeerOpcion(args, "--material");
            string? categoria = LeerOpcion(args, "--categoria");
            string? combinacion = LeerOpcion(args, "--combinacion");
            string? repetirDe = LeerOpcion(args, "--repetir-fallidos");
            string? salida = LeerOpcion(args, "--salida");
            int? limite = int.TryParse(LeerOpcion(args, "--limite"), out int n) && n > 0 ? n : (int?)null;

            IReadOnlyCollection<string>? soloIds = null;
            if (!string.IsNullOrWhiteSpace(repetirDe))
                soloIds = EjecutorEvaluacion35.LeerCasosFallidosDeInforme(Path.GetFullPath(repetirDe!));

            var filtro = new FiltroEvaluacion35(material, categoria, combinacion, null, soloIds, limite);

            using var cliente = new ClienteVisionOllamaMultirresiduo(url, modelo);
            var ejecutor = new EjecutorEvaluacion35(cliente);
            InformeEvaluacion35 informe = await ejecutor.EjecutarManifiestoAsync(manifiesto, filtro, ct).ConfigureAwait(false);

            string json = JsonSerializer.Serialize(informe, EjecutorEvaluacion35.OpcionesJson);
            string md = EjecutorEvaluacion35.FormatearMarkdown(informe);

            if (!string.IsNullOrWhiteSpace(salida))
            {
                string rutaBase = Path.GetFullPath(salida!);
                string? dir = Path.GetDirectoryName(rutaBase);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(rutaBase + ".json", json);
                File.WriteAllText(rutaBase + ".md", md);
                Console.WriteLine($"Informe guardado en {rutaBase}.json y {rutaBase}.md");
            }

            Console.WriteLine(md);
            return informe.FallosIntegracion > 0 ? 1 : 0;
        }

        public static async Task<int> CompararModelosAsync(string[] args, CancellationToken ct = default)
        {
            if (args.Length < 2 || !File.Exists(args[1]))
            {
                Console.Error.WriteLine("Uso: EcoAccess.P3.Bot --comparar-modelos35 <manifiesto.json> --modelo-a lfm3-vl:latest --modelo-b qwen3-vl:4b [--limite N] [--salida prefijo]");
                return 2;
            }

            string manifiesto = Path.GetFullPath(args[1]);
            string? url = LeerOpcion(args, "--url");
            string modeloA = LeerOpcion(args, "--modelo-a") ?? ClienteVisionOllamaMultirresiduo.ModeloPredeterminado;
            string modeloB = LeerOpcion(args, "--modelo-b") ?? "qwen3-vl:4b";
            string? salida = LeerOpcion(args, "--salida");
            int? limite = int.TryParse(LeerOpcion(args, "--limite"), out int n) && n > 0 ? n : (int?)null;

            using var clienteA = new ClienteVisionOllamaMultirresiduo(url, modeloA);
            using var clienteB = new ClienteVisionOllamaMultirresiduo(url, modeloB);
            var filtro = new FiltroEvaluacion35(LimiteCasos: limite);

            InformeComparativoModelos cmp = await ComparadorModelosVision.CompararManifiestoAsync(manifiesto, clienteA, clienteB, filtro, ct).ConfigureAwait(false);
            string json = JsonSerializer.Serialize(cmp, EjecutorEvaluacion35.OpcionesJson);
            string md = ComparadorModelosVision.FormatearMarkdown(cmp);

            if (!string.IsNullOrWhiteSpace(salida))
            {
                string rutaBase = Path.GetFullPath(salida!);
                string? dir = Path.GetDirectoryName(rutaBase);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(rutaBase + ".json", json);
                File.WriteAllText(rutaBase + ".md", md);
                Console.WriteLine($"Comparativa guardada en {rutaBase}.json y {rutaBase}.md");
            }

            Console.WriteLine(md);
            return (cmp.InformeA.FallosIntegracion > 0 || cmp.InformeB.FallosIntegracion > 0) ? 1 : 0;
        }

        private static string? LeerOpcion(string[] args, string bandera)
        {
            for (int i = 1; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], bandera, StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }
            return null;
        }
    }
}
