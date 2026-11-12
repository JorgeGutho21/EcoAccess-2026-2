using System;
using System.Collections.Generic;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Política de contraseñas para las cuentas NUEVAS de ciudadanos (registro público y registro en el escritorio):
    /// de 6 a 20 caracteres, sin exigir mayúsculas, números ni símbolos (se permiten si la persona quiere).
    /// <para>
    /// Es una política de menor seguridad que la del personal, decidida por el propietario para que el registro desde el
    /// teléfono sea sencillo. Medidas compensatorias que siguen activas: hash PBKDF2 con sal propia por cuenta, bloqueo
    /// temporal tras <see cref="ServicioAutenticacion.MaximoIntentos"/> fallos, límites de uso por enlace y global en la
    /// página, rechazo de las contraseñas más comunes y una cuenta ciudadana sin permisos administrativos.
    /// </para>
    /// <para>
    /// No cambia <see cref="CredencialAcceso.LongitudMinimaContrasena"/> (12) ni
    /// <see cref="CredencialAcceso.LongitudMaximaContrasena"/> (128): Operario y Administrador conservan su política, y
    /// el inicio de sesión sigue aceptando hasta 128 caracteres para las cuentas creadas antes.
    /// </para>
    /// </summary>
    public static class PoliticaContrasenaCiudadano
    {
        public const int LongitudMinima = 6;
        public const int LongitudMaxima = 20;

        /// <summary>Texto de ayuda para la persona, el mismo en la página y en el escritorio.</summary>
        public static string Ayuda { get; } = $"Entre {LongitudMinima} y {LongitudMaxima} caracteres. Puedes usar letras, números o símbolos.";

        // Las más usadas en listas públicas de filtraciones, más variantes obvias del proyecto. Se comparan sin mayúsculas.
        private static readonly HashSet<string> Comunes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "1234567", "12345678", "123456789", "1234567890", "12345678910", "654321", "123123", "111111", "000000",
            "121212", "112233", "123321", "666666", "777777", "888888", "999999", "555555", "222222", "333333", "444444",
            "abc123", "abcdef", "abcd1234", "qwerty", "qwerty123", "qwertyuiop", "asdfgh", "asdfghjkl", "zxcvbn", "1q2w3e",
            "1q2w3e4r", "password", "password1", "passw0rd", "iloveyou", "teamo", "teamo123", "contraseña", "contrasena",
            "clave123", "secreto", "colombia", "colombia1", "ecoaccess", "ecoaccess1", "eco123", "reciclaje", "reciclar",
            "dragon", "monkey", "sunshine", "princess", "football", "futbol", "america", "superman", "batman", "admin",
            "admin123", "usuario", "usuario1", "hola123", "holahola", "123abc", "a12345", "aa123456", "qazwsx",
        };

        /// <summary>Null si la contraseña cumple; si no, el motivo para mostrar junto al campo.</summary>
        public static string? Validar(string? contrasena)
        {
            if (Compat.IsNullOrWhiteSpace(contrasena)) return "Escribe una contraseña.";
            if (contrasena.Length < LongitudMinima) return $"La contraseña debe tener al menos {LongitudMinima} caracteres.";
            if (contrasena.Length > LongitudMaxima) return $"La contraseña puede tener como máximo {LongitudMaxima} caracteres.";
            if (Comunes.Contains(contrasena.Trim())) return "Esa contraseña es demasiado común. Elige otra.";
            return null;
        }
    }
}
