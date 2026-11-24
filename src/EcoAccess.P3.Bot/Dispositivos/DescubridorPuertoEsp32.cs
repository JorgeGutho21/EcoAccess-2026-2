using System;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;

namespace EcoAccess.P3.Bot.Dispositivos
{
    /// <summary>
    /// Descubrimiento seguro y de solo lectura del puerto serial del controlador ESP32 con firmware
    /// estacion_tres_canecas (FASE 4). Prueba primero el puerto preferido (p. ej. COM3) y, si no está disponible
    /// o cambió de número en Windows, inspecciona los puertos COM disponibles enviando únicamente el desafío '?'.
    /// Nunca envía comandos de apertura ni mueve servomotores.
    /// </summary>
    internal static class DescubridorPuertoEsp32
    {
        private static readonly Regex PatronDesafio = new(@"^\{\""ecoaccessDesafio\"":\""[0-9a-f]{32}\""\}$", RegexOptions.Compiled);

        public static string? ResolverPuerto(string? puertoPreferido, out string detalle)
        {
            GuardiaHardware.ExigirPermitido("descubrimiento de puerto serial ESP32");
            string[] puertos = SerialPort.GetPortNames();
            if (puertos.Length == 0)
            {
                detalle = "No se detectaron puertos COM en el equipo.";
                return null;
            }

            var orden = puertos
                .OrderByDescending(p => !string.IsNullOrWhiteSpace(puertoPreferido) && string.Equals(p, puertoPreferido, StringComparison.OrdinalIgnoreCase))
                .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (string puerto in orden)
            {
                if (ResponderDesafioEstacion(puerto))
                {
                    detalle = string.Equals(puerto, puertoPreferido, StringComparison.OrdinalIgnoreCase)
                        ? $"Controlador ESP32 confirmado en {puerto} (puerto configurado)."
                        : $"Controlador ESP32 reubicado y confirmado automáticamente en {puerto} (configurado: {puertoPreferido ?? "ninguno"}).";
                    return puerto;
                }
            }

            detalle = $"Ningún puerto COM ({string.Join(", ", orden)}) respondió con desafío válido de estacion_tres_canecas.";
            return null;
        }

        private static bool ResponderDesafioEstacion(string puerto)
        {
            using var sp = new SerialPort(puerto, 115200)
            {
                DtrEnable = false,
                RtsEnable = false,
                NewLine = "\n",
                ReadTimeout = 900,
                WriteTimeout = 900,
            };
            try
            {
                sp.Open();
                for (int i = 0; i < 3; i++)
                {
                    System.Threading.Thread.Sleep(350);
                    sp.DiscardInBuffer();
                    sp.WriteLine("?");
                    try
                    {
                        string linea = sp.ReadLine().Trim();
                        if (PatronDesafio.IsMatch(linea)) return true;
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (sp.IsOpen) sp.Close();
            }
        }
    }
}
