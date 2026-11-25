using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.EstacionWeb
{
    /// <summary>
    /// Modo <c>--estacion-web</c>: recibe las aperturas de la estación web local y crea las órdenes en Oracle. Este proceso
    /// NO habla con el ESP32 (queda bloqueado por <see cref="Dispositivos.GuardiaHardware"/>): las tapas las mueven los
    /// procesos <c>--bridge</c> y <c>--dispositivo-serial</c> de cada caneca, como con Telegram y la aplicación de escritorio.
    /// <para>Configuración (User Secrets o variables <c>ECOACCESS_EstacionWeb__…</c>):</para>
    /// <list type="bullet">
    /// <item><c>EstacionWeb:Prefijo</c> (http://127.0.0.1:8190/aperturas/)</item>
    /// <item><c>EstacionWeb:Canecas:blanca|verde|negra</c> (1093, 1400, 1401)</item>
    /// <item><c>EstacionWeb:Materiales:&lt;color&gt;</c> opcional; sin él se usa el primer material habilitado que la caneca admite</item>
    /// <item><c>EstacionWeb:EsperaConfirmacionSegundos</c> (20, de 5 a 120)</item>
    /// <item><c>EstacionWeb:Token</c> opcional (16 caracteres o más); si está, la estación debe enviarlo</item>
    /// </list>
    /// </summary>
    public static class AplicacionEstacionWeb
    {
        internal static readonly IReadOnlyDictionary<string, int> CanecasPredeterminadas =
            new Dictionary<string, int> { ["blanca"] = 1093, ["verde"] = 1400, ["negra"] = 1401 };

        internal static (Dictionary<string, int> Canecas, Dictionary<string, int> Materiales, TimeSpan Espera, string Prefijo, string? Token) Leer(IConfiguration configuracion)
        {
            var canecas = new Dictionary<string, int>();
            var materiales = new Dictionary<string, int>();
            foreach (var par in CanecasPredeterminadas)
            {
                string? valor = configuracion[$"EstacionWeb:Canecas:{par.Key}"];
                int id = par.Value;
                if (valor is not null && (!int.TryParse(valor, out id) || id < 0)) throw new ArgumentException($"EstacionWeb:Canecas:{par.Key} inválido.");
                if (id > 0) canecas[par.Key] = id; // 0 deja ese color fuera de la estación
                string? material = configuracion[$"EstacionWeb:Materiales:{par.Key}"];
                if (material is not null)
                {
                    if (!int.TryParse(material, out int idMaterial) || idMaterial <= 0) throw new ArgumentException($"EstacionWeb:Materiales:{par.Key} inválido.");
                    materiales[par.Key] = idMaterial;
                }
            }
            string? textoEspera = configuracion["EstacionWeb:EsperaConfirmacionSegundos"];
            int segundos = 20;
            if (textoEspera is not null && (!int.TryParse(textoEspera, out segundos) || segundos is < 5 or > 120))
                throw new ArgumentException("EstacionWeb:EsperaConfirmacionSegundos debe estar entre 5 y 120.");
            string prefijo = configuracion["EstacionWeb:Prefijo"] ?? ServidorAperturasWeb.PrefijoPredeterminado;
            if (!ServidorAperturasWeb.PrefijoValido(prefijo)) throw new ArgumentException("EstacionWeb:Prefijo debe ser http://127.0.0.1:<puerto>/<ruta>/");
            return (canecas, materiales, TimeSpan.FromSeconds(segundos), prefijo, configuracion["EstacionWeb:Token"]);
        }

        public static async Task<int> EjecutarAsync(IConfiguration configuracion, ServicioInteracciones interacciones,
            IRepositorioContenedores contenedores, IRepositorioMateriales materiales, TimeProvider reloj)
        {
            using var parar = new CancellationTokenSource();
            ConsoleCancelEventHandler cancelar = (_, e) => { e.Cancel = true; parar.Cancel(); };
            Console.CancelKeyPress += cancelar;
            try
            {
                var c = Leer(configuracion);
                var servicio = new ServicioAperturasWeb(interacciones, contenedores, materiales, c.Canecas, c.Materiales, reloj, c.Espera);
                var receptor = new ReceptorAperturasWeb(servicio, c.Token);
                Console.WriteLine($"Estación web: canecas {string.Join(", ", c.Canecas)}. Las tapas las mueven los bridges de cada caneca.");
                await new ServidorAperturasWeb(receptor, c.Prefijo, t => Console.WriteLine($"[{reloj.GetUtcNow():O}] {t}")).EjecutarAsync(parar.Token);
                return 0;
            }
            catch (OperationCanceledException) when (parar.IsCancellationRequested) { return 0; }
            catch (ArgumentException error)
            {
                Console.Error.WriteLine($"Configuración de la estación web inválida: {error.Message}");
                return 1;
            }
            catch (System.Net.HttpListenerException)
            {
                Console.Error.WriteLine("No se pudo escuchar en el prefijo de la estación web. ¿Otro proceso usa ese puerto?");
                return 1;
            }
            finally
            {
                Console.CancelKeyPress -= cancelar;
            }
        }
    }
}
