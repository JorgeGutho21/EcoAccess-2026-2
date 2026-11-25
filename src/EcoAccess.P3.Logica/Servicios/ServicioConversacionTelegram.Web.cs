using System;
using System.Text.RegularExpressions;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Vinculación de la web ciudadana: la página muestra t.me/EcoAccessP3_JG_bot?start=web_CODIGO. El código (un solo uso,
    /// 10 minutos) se confirma solo desde un chat que ya tiene una cuenta EcoAccess vinculada; nunca por nombre.
    /// </summary>
    public sealed partial class ServicioConversacionTelegram
    {
        public const string PrefijoStartWeb = "web_";
        private static readonly Regex CodigoWeb = new("^[A-Za-z0-9]{10}$", RegexOptions.Compiled);

        /// <summary>Confirma un código de la página para la cuenta indicada (lo conecta el bot). Devuelve false si venció o ya se usó.</summary>
        public Func<string, int, bool>? ConfirmarVinculoWeb { get; set; }

        private RespuestaBot VincularWeb(MensajeEntrante mensaje, int? idUsuario, string codigo)
        {
            if (ConfirmarVinculoWeb is null || !CodigoWeb.IsMatch(codigo))
                return ConMenu(Responder(mensaje, "Ese enlace de vinculación no es válido. Genera uno nuevo desde la página EcoAccess Web."));
            if (idUsuario is not int id)
                return ConMenu(Responder(mensaje, "Para ver tu historial en la página, primero crea o vincula tu cuenta EcoAccess aquí en Telegram (👤 Mi cuenta) y vuelve a abrir el enlace de la página."));
            return ConMenu(Responder(mensaje, ConfirmarVinculoWeb(codigo, id)
                ? "🔗 Listo: la página EcoAccess Web quedó vinculada a tu cuenta. Vuelve a la página; tu historial oficial (Oracle) aparecerá allí y en /historial."
                : "Ese código ya se usó o venció (dura 10 minutos). Genera uno nuevo desde la página EcoAccess Web."));
        }
    }
}
