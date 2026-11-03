using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EcoAccess.P3.Logica.Servicios
{

    public enum IntencionTexto { Ninguna, Saludo, Correccion, Cancelar, Ayuda, Menu, OtraFoto }

    /// <summary>
    /// Reconoce frases habituales antes de buscar en el catálogo. Es una lista cerrada y predecible: no usa ningún modelo
    /// y solo decide qué mensaje mostrar; las acciones siguen pasando por sus validaciones.
    /// </summary>
    public static class IntencionesTelegram
    {
        private static readonly HashSet<string> Saludos = new(StringComparer.Ordinal)
        {
            "hola", "hola eco", "ola", "holi", "hey", "buenas", "buen dia", "buenos dias", "buenas tardes", "buenas noches", "que tal", "saludos",
        };

        private static readonly HashSet<string> Cancelaciones = new(StringComparer.Ordinal) { "cancelar", "cancela", "cancelalo", "cancelar todo" };
        private static readonly HashSet<string> Ayudas = new(StringComparer.Ordinal) { "ayuda", "ayudame", "help", "necesito ayuda" };
        private static readonly HashSet<string> Menus = new(StringComparer.Ordinal) { "menu", "inicio", "opciones", "volver", "volver al menu" };

        private static readonly HashSet<string> OtrasFotos = new(StringComparer.Ordinal) { "otra foto", "otra fotografia", "enviar otra foto", "mandar otra foto" };

        private static readonly string[] FrasesCorreccion =
        {
            "te equivocaste", "te equivocas", "equivocado", "equivocada", "esta mal", "estas mal", "incorrecto", "incorrecta", "no es correcto",
            "no reconociste", "no lo reconociste", "reconociste mal", "identificaste mal", "mal identificado", "no acertaste", "no es asi",
        };

        private static readonly string[] InicioCorreccion = { "no es", "eso no es", "no era", "eso no era" };

        public static IntencionTexto Clasificar(string? texto)
        {
            string normal = Normalizar(texto);
            if (normal.Length == 0) return IntencionTexto.Ninguna;
            if (Saludos.Contains(normal)) return IntencionTexto.Saludo;
            if (Cancelaciones.Contains(normal)) return IntencionTexto.Cancelar;
            if (Ayudas.Contains(normal)) return IntencionTexto.Ayuda;
            if (Menus.Contains(normal)) return IntencionTexto.Menu;
            if (OtrasFotos.Contains(normal)) return IntencionTexto.OtraFoto;
            string conEspacios = " " + normal + " ";
            if (FrasesCorreccion.Any(f => conEspacios.Contains(" " + f + " "))) return IntencionTexto.Correccion;
            return InicioCorreccion.Any(i => normal == i || normal.StartsWith(i + " ", StringComparison.Ordinal)) ? IntencionTexto.Correccion : IntencionTexto.Ninguna;
        }

        private static string Normalizar(string? texto)
        {
            if (Compat.IsNullOrWhiteSpace(texto) || texto!.Length > 200) return "";
            var salida = new StringBuilder(texto.Length);
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    salida.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            return string.Join(" ", salida.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
