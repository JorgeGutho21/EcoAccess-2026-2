using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using System;

namespace EcoAccess.P3.Bot.Telegram
{

    internal sealed record IntencionEco(string? Comando, string? Respuesta, string? Boton = null);

    /// <summary>Intenciones acotadas y verificables; los datos personales siempre se consultan en la BLL.</summary>
    internal static partial class BaseConocimientoEco
    {
        internal const string Privacidad = "🔒 Envía solo el residuo, sin personas, documentos ni datos personales. Antes de transferir una foto a un servicio externo te mostraré sus condiciones y pediré tu autorización. Puedes elegir el material con /manual, sin reconocimiento visual. No guardamos fotografías en el historial del bot; Telegram conserva sus archivos según sus propias condiciones.";

        internal static IntencionEco? Resolver(string? texto)
        {
            if (Compat.IsNullOrWhiteSpace(texto) || texto.Length > 600) return null;
            if (texto == "/privacidad") return new(null, Privacidad);
            if (texto.TrimStart().StartsWith('/')) return null;
            string normal = Normalizar(texto);
            return normal switch
            {
                "hola" or "hola eco" or "buenas" or "buenos dias" or "buenas tardes" or "buenas noches" => new(null, "¡Hola! Soy Eco 🌱. Te ayudo a separar tus residuos y consultar EcoAccess. Puedes preguntarme cómo reciclar o escanear el QR de la caneca."),
                "acerca" or "acerca de" or "quien desarrollo ecoaccess" => new(null, "EcoAccess P3 · Proyecto académico de Programación de Computadores III, Universidad Popular del Cesar. Responsable y desarrollador: Jorge Gutierrez Thomas. Eco 🌱 es su asistente ambiental."),
                "gracias" or "muchas gracias" => new(null, "¡Con gusto! 🌱 Cada residuo cuenta."),
                "menu" or "inicio" or "volver al inicio" or "menu principal" => new(null, null, "menu:inicio"),
                "quiero depositar" or "depositar residuo" => new(null, null, "menu:depositar"),
                "escanear qr" => new(null, null, "menu:qr"),
                "mi cuenta" => new(null, null, "menu:cuenta"),
                "enviar foto" or "quiero enviar una foto" => new("/foto", null),
                "elegir material" or "seleccion manual" => new("/manual", null),
                "quiero cancelar" or "cancelar solicitud" => new("/cancelar", null),
                "que es ecoaccess" or "para que sirve" or "para que sirve ecoaccess" => new(null, "🌱 EcoAccess te ayuda a separar residuos, solicitar un depósito y consultar su seguimiento. La caneca DEMO simula apertura y detección. Los ecopuntos requieren identificación y verificación; una foto no los acredita."),
                "como reciclo" or "como separo los residuos" or "como puedo ayudar al medio ambiente" => new("/categorias", null),
                "que es pet" or "que significa pet" => new("PET", null),
                "donde deposito carton" or "donde tiro el carton" => new("cartón", null),
                "que hago con una pila" or "donde tiro las pilas" or "como reciclo pilas" => new("pila", null),
                "que materiales acepta" or "que materiales recibe" or "ver materiales" => new("/materiales", null),
                "mis ecopuntos" or "cuantos ecopuntos tengo" or "cuantos puntos tengo" or "consultar mis puntos" => new("/ecopuntos", null),
                "mis depositos" or "como consulto mis depositos" or "ver mis depositos" or "mi historial" or "donde veo mis depositos" => new("/depositos", null),
                "como funciona" or "como funciona ecoaccess" => new(null, "🌱 Así funciona: 1) escanea el QR de la caneca; 2) elige el material o envía una foto; 3) confirma y la caneca se abre; 4) deposita. Un administrador verifica el material y, si tienes cuenta, recibes tus ecopuntos. Consulta el avance con /estado."),
                "que hago si no reconoce mi foto" or "que hago si no reconoce mi fotografia" or "no reconoce mi foto" or "no reconoce mi fotografia" => new(null, "📸 Intenta otra foto con buena luz, un solo residuo y fondo despejado. Si aun así no lo reconozco, elige el material tú mismo con /manual: tu depósito sigue siendo válido."),
                "como va mi deposito" or "estado de mi operacion" or "por que mi deposito esta pendiente" => new("/estado", null),
                "como consigo ecopuntos" or "como gano puntos" or "como gano ecopuntos" => new(null, "🌱 Los ecopuntos se acreditan a tu cuenta cuando el depósito tiene la evidencia y verificación requeridas. Enviar una foto, confirmar una orden o abrir la caneca no basta. Consulta tu saldo con /ecopuntos."),
                "puedo participar sin registrarme" or "puedo entrar como invitado" or "puedo depositar sin cuenta" => new(null, "Puedes depositar sin cuenta: escanea el QR, elige un material admitido o envía una foto y confirma. Solo las cuentas identificadas pueden acumular ecopuntos por depósitos verificados; los invitados no reciben puntos."),
                "como funciona la caneca" or "como deposito" => new(null, "Escanea su QR, revisa los materiales admitidos y elige uno o envía una foto. Confirma solo si es correcto. Después consulta /estado: una orden no confirma el depósito. En la DEMO, la apertura y el sensor son simulados."),
                "que pasa si no tengo internet" or "funciona sin internet" => new(null, "Telegram necesita internet para comunicarse con Eco. Si se corta la conexión, consulta /estado cuando vuelvas antes de repetir una solicitud. El análisis local puede evitar servicios externos, pero no reemplaza la conexión de Telegram ni funciona si el computador está apagado."),
                "cuanto pesa" or "cuanto pesa mi residuo" or "puedes pesar mi residuo" => new(null, "⚖️ Peso no estimable con esta fotografía. Una imagen no es una medición ni confirma si el envase está lleno. Los pesos estimados no sirven para acreditar ecopuntos; el peso medido requiere un sensor."),
                "cuantos residuos hay" or "cuantas botellas hay" => new(null, "El conteo visual sigue en evaluación. No te daré una cantidad si los objetos se superponen o no se distinguen bien. Una fotografía tampoco permite determinar su peso."),
                "privacidad" or "como usan mi foto" or "que hacen con mi foto" => new(null, Privacidad),
                _ => null
            };
        }

        private static string Normalizar(string texto)
        {
            var salida = new StringBuilder();
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    salida.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            return Espacios().Replace(salida.ToString(), " ").Trim();
        }
        private static readonly Regex PatronEspacios = new Regex(@"\s+", RegexOptions.CultureInvariant);
        private static Regex Espacios() => PatronEspacios;
    }
}
