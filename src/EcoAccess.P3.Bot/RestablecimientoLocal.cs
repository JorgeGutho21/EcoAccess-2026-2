using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot
{
    /// <summary>
    /// <c>EcoAccess.P3.Bot --restablecer-contrasena</c>: la persona titular de una cuenta de Operario o Supervisor define una
    /// contraseña nueva cuando olvidó la anterior. Solo en una consola interactiva del equipo de la estación (con la conexión
    /// privada de Oracle); la contraseña se escribe oculta y dos veces, nunca llega por argumentos, tuberías ni archivos, no se
    /// imprime y no se registra. Queda constancia local de cuándo, para qué cuenta y desde qué usuario de Windows.
    /// </summary>
    internal static class RestablecimientoLocal
    {
        internal static string ArchivoConstancia => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Auditoria", "restablecimientos.log");

        public static async Task<int> EjecutarAsync(ServicioAutenticacion autenticacion)
        {
            if (Console.IsInputRedirected || Console.IsOutputRedirected)
            {
                Console.Error.WriteLine("Esta herramienta solo funciona en una consola interactiva: la contraseña no se acepta por tubería ni por archivo.");
                return 2;
            }

            Console.WriteLine("EcoAccess · Restablecer la contraseña de una cuenta de Operario o Supervisor");
            Console.WriteLine("La contraseña nueva debe tener entre 12 y 128 caracteres. No se mostrará en pantalla.");
            Console.Write("Correo de la cuenta: ");
            string? correo = Console.ReadLine();
            string primera = LeerOculta("Contraseña nueva: ");
            string segunda = LeerOculta("Repite la contraseña nueva: ");
            if (!string.Equals(primera, segunda, StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Las dos contraseñas no coinciden. No se cambió nada.");
                return 1;
            }

            Resultado<int> resultado;
            try
            {
                using var limite = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                resultado = await autenticacion.RestablecerContrasenaPersonalLocalAsync(correo, primera, limite.Token);
            }
            catch (Exception error)
            {
                Console.Error.WriteLine($"No se pudo completar en Oracle ({error.GetType().Name}). No se cambió nada.");
                return 1;
            }

            if (!resultado.Exitoso)
            {
                Console.Error.WriteLine(resultado.Mensaje);
                return 1;
            }

            Anotar(resultado.Valor);
            Console.WriteLine($"Listo: la cuenta {resultado.Valor} tiene contraseña nueva y quedó desbloqueada. Ya puedes iniciar sesión en la aplicación.");
            return 0;
        }

        private static string LeerOculta(string pregunta)
        {
            Console.Write(pregunta);
            var texto = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);
                if (tecla.Key == ConsoleKey.Enter) break;
                if (tecla.Key == ConsoleKey.Backspace) { if (texto.Length > 0) texto.Length--; continue; }
                if (!char.IsControl(tecla.KeyChar) && texto.Length <= 256) texto.Append(tecla.KeyChar);
            }
            Console.WriteLine();
            return texto.ToString();
        }

        /// <summary>Constancia sin secretos: hora UTC, cuenta y usuario de Windows que ejecutó la herramienta.</summary>
        private static void Anotar(int idUsuario)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ArchivoConstancia)!);
                File.AppendAllText(ArchivoConstancia,
                    $"{DateTime.UtcNow:O}\tcuenta={idUsuario}\twindows={Environment.UserDomainName}\\{Environment.UserName}\trestablecimiento local{Environment.NewLine}");
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                Console.Error.WriteLine("Aviso: no se pudo escribir la constancia local del restablecimiento.");
            }
        }
    }
}
