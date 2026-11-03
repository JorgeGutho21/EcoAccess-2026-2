using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Bot
{

    public enum AccionSupervisor { Nada, Reiniciar, Detener }

    /// <summary>Qué hacer con un servicio. Sin efectos: se prueba sin procesos.</summary>
    public static class DecisionSupervisor
    {
        public const int MaximoReinicios = 5;
        public static readonly TimeSpan Ventana = TimeSpan.FromMinutes(30);
        public static readonly TimeSpan LatidoMaximo = TimeSpan.FromMinutes(3);

        private static readonly Regex NecesitaPersona = new("Webhook activo|otra instancia|\\b409\\b|rechaz|Falta configurar", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <param name="ultimasLineas">Registro reciente del servicio: un conflicto o un token rechazado no se arregla reiniciando.</param>
        public static AccionSupervisor Decidir(bool vivo, TimeSpan? sinLatido, bool conFallosDeRed, string? ultimasLineas, IReadOnlyCollection<DateTime> reinicios, DateTime ahora)
        {
            bool bloqueado = vivo && !conFallosDeRed && sinLatido > LatidoMaximo;
            if (vivo && !bloqueado) return AccionSupervisor.Nada;
            if (!vivo && ultimasLineas is not null && NecesitaPersona.IsMatch(ultimasLineas)) return AccionSupervisor.Detener;
            return reinicios.Count(r => ahora - r < Ventana) >= MaximoReinicios ? AccionSupervisor.Detener : AccionSupervisor.Reiniciar;
        }
    }

    /// <summary>
    /// Mantiene en marcha los servicios de la sesión registrada: reinicia el que termina o deja de pedir mensajes, con un
    /// límite. Solo actúa sobre sus procesos, un mutex impide dos supervisores y nunca abre un segundo consumidor. Sin secretos.
    /// </summary>
    public static class SupervisorServicios
    {
        private sealed class ProcesoSesion
        {
            public string Componente { get; set; } = "";
            public int IdProceso { get; set; }
            public long InicioTicks { get; set; }
        }

        private sealed class Sesion
        {
            public string Directorio { get; set; } = "";
            public List<ProcesoSesion> Procesos { get; set; } = new();
        }

        private static readonly string Base = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
        public static string ArchivoSesion { get; } = Path.Combine(Base, "Desarrollo", "sesion.json");
        public static string ArchivoEstado { get; } = Path.Combine(Base, "Desarrollo", "supervisor.json");

        /// <summary>Lanza el supervisor sin ventana y vuelve de inmediato; el lanzador no se queda esperando.</summary>
        public static int IniciarEnSegundoPlano()
        {
            using var proceso = Process.Start(new ProcessStartInfo(Process.GetCurrentProcess().MainModule!.FileName, "--supervisar") { UseShellExecute = false, CreateNoWindow = true });
            Console.WriteLine($"Supervisor iniciado (proceso {proceso!.Id}).");
            return 0;
        }

        public static async Task<int> EjecutarAsync(CancellationToken ct = default)
        {
            using var mutex = new Mutex(false, @"Global\EcoAccessP3SupervisorTelegram");
            if (!mutex.WaitOne(0)) { Console.WriteLine("Ya hay un supervisor activo."); return 0; }
            try
            {
                var reinicios = new List<DateTime>();
                while (!ct.IsCancellationRequested && File.Exists(ArchivoSesion))
                {
                    Sesion? sesion = LeerSesion();
                    if (sesion is null) { await Task.Delay(TimeSpan.FromSeconds(5), ct); continue; }
                    bool cambio = false;
                    foreach (ProcesoSesion servicio in sesion.Procesos)
                    {
                        bool vivo = EstaVivo(servicio);
                        EstadoPolling? latido = servicio.Componente == "Telegram" ? LeerLatido(servicio.IdProceso) : null;
                        TimeSpan? sinLatido = latido is null ? null : DateTime.UtcNow - latido.UltimoContactoUtc;
                        AccionSupervisor accion = DecisionSupervisor.Decidir(vivo, sinLatido, latido?.FallosSeguidos > 0, vivo ? null : UltimasLineas(sesion.Directorio, servicio.Componente), reinicios, DateTime.UtcNow);
                        if (accion == AccionSupervisor.Nada) continue;
                        if (accion == AccionSupervisor.Detener)
                        {
                            Anotar(sesion, $"{servicio.Componente} no está en marcha y necesita intervención: no se reinicia.");
                            GuardarEstado("ErrorCritico", servicio.Componente + ": revisar los registros de la sesión");
                            return 1;
                        }
                        if (vivo) Detener(servicio);
                        reinicios.Add(DateTime.UtcNow);
                        Iniciar(sesion, servicio);
                        Anotar(sesion, $"{servicio.Componente} {(vivo ? "dejó de pedir mensajes" : "terminó")}; reiniciado como proceso {servicio.IdProceso}.");
                        cambio = true;
                        await Task.Delay(TimeSpan.FromSeconds(Math.Min(60, 5 * reinicios.Count)), ct);
                    }
                    if (cambio) GuardarSesion(sesion);
                    GuardarEstado("Supervisando", $"{sesion.Procesos.Count} servicios");
                    await Task.Delay(TimeSpan.FromSeconds(20), ct);
                }
                return 0;
            }
            catch (OperationCanceledException) { return 0; }
            finally { mutex.ReleaseMutex(); }
        }

        private static Sesion? LeerSesion()
        {
            try { return JsonSerializer.Deserialize<Sesion>(File.ReadAllText(ArchivoSesion)); }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return null; }
        }

        private static void GuardarSesion(Sesion sesion) => EscribirAtomico(ArchivoSesion, JsonSerializer.Serialize(sesion));

        private static void GuardarEstado(string estado, string detalle) =>
            EscribirAtomico(ArchivoEstado, JsonSerializer.Serialize(new { Proceso = Process.GetCurrentProcess().Id, Estado = estado, Detalle = detalle, FechaUtc = DateTime.UtcNow }));

        private static void EscribirAtomico(string archivo, string contenido)
        {
            try
            {
                string temporal = archivo + ".tmp";
                File.WriteAllText(temporal, contenido);
                Compat.MoverSobrescribiendo(temporal, archivo);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }

        private static bool EstaVivo(ProcesoSesion servicio)
        {
            try
            {
                using Process proceso = Process.GetProcessById(servicio.IdProceso);
                return !proceso.HasExited && proceso.StartTime.ToUniversalTime().Ticks == servicio.InicioTicks;
            }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { return false; }
        }

        private static void Detener(ProcesoSesion servicio)
        {
            try { using Process proceso = Process.GetProcessById(servicio.IdProceso); proceso.Kill(); proceso.WaitForExit(5000); }
            catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception) { }
        }

        private static EstadoPolling? LeerLatido(int proceso)
        {
            try
            {
                var latido = JsonSerializer.Deserialize<EstadoPolling>(File.ReadAllText(EstadoPolling.ArchivoPredeterminado));
                return latido?.Proceso == proceso ? latido : null;
            }
            catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException) { return null; }
        }

        private static string? UltimasLineas(string directorio, string componente)
        {
            try
            {
                FileInfo? ultimo = new DirectoryInfo(directorio).GetFiles(componente + "*.error.log").OrderBy(f => f.LastWriteTimeUtc).LastOrDefault();
                return ultimo is null ? null : string.Join(" ", File.ReadAllLines(ultimo.FullName).Reverse().Take(6));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return null; }
        }

        private static void Iniciar(Sesion sesion, ProcesoSesion servicio)
        {
            string modo = servicio.Componente switch { "Telegram" => "--polling", "Bridge" => "--bridge", "Dispositivo" => "--dispositivo-serial", _ => "--dispositivo-simulado" };
            string sufijo = DateTime.Now.ToString("HHmmss");
            var inicio = new ProcessStartInfo(Process.GetCurrentProcess().MainModule!.FileName, modo)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Environment.CurrentDirectory,
            };
            inicio.EnvironmentVariables["ECOACCESS_ARCHIVO_BRIDGE"] = Path.Combine(Base, "Bridge", "bridge.local.json");
            Process proceso = Process.Start(inicio)!;
            var salida = new StreamWriter(Path.Combine(sesion.Directorio, $"{servicio.Componente}-{sufijo}.salida.log")) { AutoFlush = true };
            var errores = new StreamWriter(Path.Combine(sesion.Directorio, $"{servicio.Componente}-{sufijo}.error.log")) { AutoFlush = true };
            proceso.OutputDataReceived += (_, e) => { if (e.Data is not null) lock (salida) salida.WriteLine(e.Data); };
            proceso.ErrorDataReceived += (_, e) => { if (e.Data is not null) lock (errores) errores.WriteLine(e.Data); };
            proceso.BeginOutputReadLine();
            proceso.BeginErrorReadLine();
            servicio.IdProceso = proceso.Id;
            servicio.InicioTicks = proceso.StartTime.ToUniversalTime().Ticks;
        }

        private static void Anotar(Sesion sesion, string texto)
        {
            try { File.AppendAllText(Path.Combine(sesion.Directorio, "supervisor.log"), $"[{DateTime.UtcNow:s}Z] {texto}{Environment.NewLine}"); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
    }
}
