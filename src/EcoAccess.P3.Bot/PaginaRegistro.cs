using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot
{

    /// <summary>Respuesta HTML ya armada; <see cref="ServidorRegistro"/> solo la copia a la conexión.</summary>
    public sealed record RespuestaPagina(int Codigo, string Html, string? Cookie = null);

    /// <summary>
    /// Página de registro del enlace de un solo uso. GET <c>?t=TOKEN</c> comprueba el token y
    /// muestra el formulario; POST crea la cuenta de ciudadano con su consentimiento y la vincula a la cuenta de
    /// Telegram que pidió el enlace. Nunca pide contraseña: los ciudadanos no tienen credencial y una
    /// cuenta existente no se vincula desde aquí. Todo texto que viene de la petición sale codificado en HTML.
    /// El token oculto del formulario hace de protección CSRF: sin él no hay registro posible.
    /// </summary>
    public sealed partial class PaginaRegistro
    {
        /// <summary>Versión del texto de tratamiento de datos que se muestra; queda guardada con la cuenta.</summary>
        public const string VersionConsentimiento = "2026-10-telegram";

        /// <summary>Un formulario con nombre y correo no necesita más; el resto se rechaza con 413.</summary>
        public const int TamanoMaximo = 8 * 1024;

        /// <summary>Encabezados de seguridad de toda respuesta. Referrer-Policy evita filtrar el token de la URL.</summary>
        /// <summary>
        /// Único script de la página: muestra u oculta la contraseña y avisa si la confirmación no coincide. No envía nada
        /// ni lee cookies. La CSP lo autoriza por su hash SHA-256, sin «unsafe-inline» para scripts.
        /// </summary>
        internal const string ScriptFormulario =
            "document.querySelectorAll('button[data-mostrar]').forEach(function(b){b.hidden=false;b.addEventListener('click',function(){" +
            "var i=document.getElementById(b.getAttribute('data-mostrar'));var ver=i.type==='password';i.type=ver?'text':'password';" +
            "b.textContent=ver?'Ocultar':'Mostrar';b.setAttribute('aria-pressed',ver?'true':'false');});});" +
            "var c=document.getElementById('confirmacion'),k=document.getElementById('clave-nueva');" +
            "if(c&&k){var f=function(){c.setCustomValidity(c.value&&c.value!==k.value?'Las contraseñas no coinciden.':'');};" +
            "c.addEventListener('input',f);k.addEventListener('input',f);}";

        internal static readonly string HashScript = "sha256-" + Convert.ToBase64String(Compat.Sha256(Encoding.UTF8.GetBytes(ScriptFormulario)));

        public static readonly IReadOnlyDictionary<string, string> Encabezados = new Dictionary<string, string>
        {
            ["Content-Security-Policy"] = $"default-src 'none'; style-src 'unsafe-inline'; script-src '{HashScript}'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'",
            ["X-Frame-Options"] = "DENY",
            ["X-Content-Type-Options"] = "nosniff",
            ["Referrer-Policy"] = "no-referrer",
            ["Cache-Control"] = "no-store",
        };

        private const string DemasiadasSolicitudes = "Recibimos demasiadas solicitudes. Espera un minuto e intenta de nuevo.";
        private const string CuentaCreada = "Listo, tu cuenta de EcoAccess quedó creada y vinculada. Vuelve a Telegram y escanea el QR de la caneca.";
        private const string FormularioNoValido = "El formulario no llegó completo. Abre otra vez el enlace que te dio el bot.";

        private readonly ServicioVinculacionTelegram _vinculacion;
        private readonly ILimitadorUso _limitadorGlobal;
        private readonly ILimitadorUso _limitadorPorToken;
        private readonly TimeProvider _reloj;

        /// <param name="limitadorGlobal">Detrás del proxy inverso todas las peticiones llegan de la misma IP: el
        /// límite global protege a Oracle aunque no distinga clientes.</param>
        /// <param name="limitadorPorToken">Frena los reenvíos repetidos de un mismo enlace.</param>
        public PaginaRegistro(
            ServicioVinculacionTelegram vinculacion,
            ILimitadorUso limitadorGlobal,
            ILimitadorUso limitadorPorToken,
            TimeProvider reloj)
        {
            _vinculacion = vinculacion ?? throw new ArgumentNullException(nameof(vinculacion));
            _limitadorGlobal = limitadorGlobal ?? throw new ArgumentNullException(nameof(limitadorGlobal));
            _limitadorPorToken = limitadorPorToken ?? throw new ArgumentNullException(nameof(limitadorPorToken));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        /// <summary>GET: <paramref name="consulta"/> es la parte de la URL después de «?».</summary>
        public async Task<RespuestaPagina> MostrarAsync(string? consulta, CancellationToken cancellationToken = default)
        {
            string? token = Unico(HttpUtility.ParseQueryString(consulta ?? string.Empty), "t");
            RespuestaPagina? freno = Frenar(token);
            if (freno is not null)
            {
                return freno;
            }

            Resultado resultado = await _vinculacion.ConsultarTokenAsync(token, cancellationToken).ConfigureAwait(false);
            return resultado.Exitoso
                ? new RespuestaPagina(200, Formulario(token!, string.Empty, string.Empty, null))
                : Aviso(CodigoDe(resultado.Motivo), resultado.Mensaje!);
        }

        /// <summary>POST: <paramref name="cuerpo"/> en application/x-www-form-urlencoded y UTF-8.</summary>
        public async Task<RespuestaPagina> EnviarAsync(string? tipoContenido, byte[] cuerpo, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(cuerpo, nameof(cuerpo));
            if (cuerpo.Length > TamanoMaximo)
            {
                return Aviso(413, FormularioNoValido);
            }

            if (tipoContenido is null
                || !tipoContenido.Split(';')[0].Trim().Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                return Aviso(415, FormularioNoValido);
            }

            NameValueCollection campos = HttpUtility.ParseQueryString(Encoding.UTF8.GetString(cuerpo));
            string? token = Unico(campos, "t");
            string? nombre = Unico(campos, "nombre");
            string? correo = Unico(campos, "correo");
            string? acepta = Unico(campos, "acepta");
            RespuestaPagina? freno = Frenar(token);
            if (freno is not null)
            {
                return freno;
            }

            if (nombre is null || correo is null)
            {
                return Aviso(400, FormularioNoValido);
            }

            Resultado<int> resultado = await _vinculacion.RegistrarCiudadanoAsync(
                token, nombre, correo, VersionConsentimiento, acepta == "si", cancellationToken).ConfigureAwait(false);
            if (resultado.Exitoso)
            {
                return Aviso(200, CuentaCreada);
            }

            // Un dato mal escrito o el consentimiento sin marcar no gastan el enlace: se vuelve a mostrar el formulario.
            return resultado.Motivo == MotivoRechazo.DatosInvalidos
                ? new RespuestaPagina(400, Formulario(token!, nombre, correo, resultado.Mensaje))
                : Aviso(CodigoDe(resultado.Motivo), resultado.Mensaje!);
        }

        /// <summary>
        /// Formato antes que límites: un token que no puede ser nuestro no llega a Oracle ni ocupa una clave del
        /// limitador por token.
        /// </summary>
        private RespuestaPagina? Frenar(string? token)
        {
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            if (!_limitadorGlobal.Permitir("registro", ahora))
            {
                return Aviso(429, DemasiadasSolicitudes);
            }

            if (token is null || !FormatoToken().IsMatch(token))
            {
                return Aviso(400, MensajesTelegram.EnlaceInvalido);
            }

            return _limitadorPorToken.Permitir(ServicioVinculacionTelegram.CalcularHash(token), ahora)
                ? null
                : Aviso(429, DemasiadasSolicitudes);
        }

        private static int CodigoDe(MotivoRechazo? motivo) => motivo switch
        {
            MotivoRechazo.NoEncontrado => 404,
            MotivoRechazo.EstadoNoPermitido => 410,
            MotivoRechazo.Duplicado => 409,
            _ => 400,
        };

        /// <summary>Un campo repetido se trata como ausente: no se adivina cuál de los valores quería el cliente.</summary>
        private static string? Unico(NameValueCollection campos, string nombre)
        {
            string[]? valores = campos.GetValues(nombre);
            return valores is { Length: 1 } ? valores[0] : null;
        }

        private static RespuestaPagina Aviso(int codigo, string mensaje) =>
            new(codigo, Documento($"<p>{Html(mensaje)}</p>"));

        private static string Formulario(string token, string nombre, string correo, string? error)
        {
            string aviso = error is null ? string.Empty : $"<p class=\"error\" role=\"alert\">{Html(error)}</p>";
            return Documento($@"<h1>Crea tu cuenta de EcoAccess</h1>
<p>Esta cuenta queda unida a la cuenta de Telegram que pidió el enlace. No necesitas contraseña.</p>
{aviso}
<form method=""post"" action=""./"">
  <input type=""hidden"" name=""t"" value=""{Html(token)}"">
  <label>Nombre<br><input name=""nombre"" maxlength=""100"" required value=""{Html(nombre)}""></label>
  <label>Correo electrónico<br><input name=""correo"" type=""email"" maxlength=""254"" required value=""{Html(correo)}""></label>
  <p><strong>Tratamiento de datos personales (Ley 1581 de 2012).</strong> EcoAccess guarda tu nombre, tu
  correo, la cuenta de Telegram y tus operaciones con las canecas para operar el servicio y calcular
  estadísticas. Puedes pedir la consulta, corrección o supresión de tus datos y desvincular Telegram con
  /desvincular. Versión del texto: {Html(VersionConsentimiento)}.</p>
  <label><input type=""checkbox"" name=""acepta"" value=""si"" required> Acepto el tratamiento de mis datos personales.</label>
  <button type=""submit"">Crear cuenta</button>
</form>");
        }

        /// <summary>
        /// Una sola columna, campos del mismo ancho con la etiqueta encima y letra de 16 px en los campos (Safari en iPhone
        /// no amplía la página al enfocarlos). Antes solo nombre y correo tenían ancho completo: las contraseñas quedaban
        /// estrechas y al lado de su etiqueta.
        /// </summary>
        internal const string Estilos = @"
*,*::before,*::after{box-sizing:border-box}
html{-webkit-text-size-adjust:100%}
body{margin:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',system-ui,sans-serif;background:#eef5ef;color:#1b2a1f;line-height:1.45}
.marco{width:100%;max-width:30rem;margin:0 auto;padding:1rem 1rem 2rem}
.marca{display:flex;align-items:center;gap:.65rem;padding:.5rem 0 1rem}
.logo{width:2.6rem;height:2.6rem;border-radius:.8rem;background:#2e7d32;color:#fff;display:flex;align-items:center;justify-content:center;font-size:1.35rem;flex:none}
.marca b{display:block;font-size:1.1rem}.marca span{display:block;font-size:.85rem;color:#4b5f50}
.tarjeta{background:#fff;border-radius:1rem;padding:1.25rem;box-shadow:0 1px 3px rgba(0,0,0,.08);overflow-wrap:anywhere}
h1{font-size:1.35rem;line-height:1.25;margin:0 0 .4rem}h2{font-size:1.1rem;margin:1.25rem 0 .75rem}
p{margin:.4rem 0 .9rem}
.intro{color:#3b4d40}
.campo{margin:0 0 1rem}
.campo label{display:block;font-weight:600;font-size:.95rem;margin:0 0 .35rem}
.campo input{display:block;width:100%;min-width:0;font-size:16px;padding:.75rem .85rem;border:1px solid #b9c8bd;border-radius:.6rem;background:#fff;color:inherit;-webkit-appearance:none;appearance:none}
.campo input:focus{outline:3px solid rgba(46,125,50,.25);border-color:#2e7d32}
.con-boton{position:relative}.con-boton input{padding-right:6rem}
.ver{position:absolute;right:.35rem;top:.35rem;bottom:.35rem;min-width:5rem;padding:0 .7rem;border:0;border-radius:.45rem;background:#e7f1e9;color:#1b5e20;font-size:.9rem;font-weight:600}
.ayuda{font-size:.85rem;color:#4b5f50;margin:.3rem 0 0}
.error{color:#b00020;font-size:.9rem;margin:.35rem 0 0}
.con-error input{border-color:#b00020}
.alerta{background:#fdecee;border-radius:.6rem;padding:.6rem .8rem;margin:0 0 1rem}
details{font-size:.9rem;background:#f3f7f4;border-radius:.6rem;padding:.6rem .8rem;margin:0 0 .75rem}
summary{cursor:pointer;font-weight:600}
.acepto{display:flex;gap:.6rem;align-items:flex-start;font-size:.95rem;margin:0 0 .3rem}
.acepto input{width:1.3rem;height:1.3rem;flex:none;margin:.1rem 0 0}
.principal{display:block;width:100%;margin:1rem 0 0;font-size:1.05rem;font-weight:600;padding:.85rem;border:0;border-radius:.7rem;background:#2e7d32;color:#fff;-webkit-appearance:none;appearance:none}
.secundario{display:block;text-align:center;margin:1.1rem 0 0;color:#1b5e20;font-weight:600}
.pie{font-size:.85rem;color:#4b5f50;text-align:center;margin:1.25rem 0 0}
code{font-size:1.1rem;background:#f3f7f4;padding:.15rem .4rem;border-radius:.3rem;word-break:break-all}
.exito{border-top:4px solid #2e7d32}";

        internal static string Documento(string contenido, bool conScript = false) => $@"<!doctype html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>EcoAccess · Registro</title>
<style>{Estilos}</style>
</head>
<body>
<main class=""marco"">
<header class=""marca""><div class=""logo"" aria-hidden=""true"">♻</div><div><b>EcoAccess</b><span>Reciclaje con Eco en Telegram</span></div></header>
<div class=""tarjeta"">
{contenido}
</div>
</main>{(conScript ? "<script>" + ScriptFormulario + "</script>" : "")}
</body>
</html>";

        internal static string Html(string texto) => WebUtility.HtmlEncode(texto);

        /// <summary>Los tokens son base64url de 43 caracteres; se admite un margen sin aceptar textos arbitrarios.</summary>
        private static readonly Regex PatronFormatoToken = new Regex("^[A-Za-z0-9_-]{20,128}$");
        private static Regex FormatoToken() => PatronFormatoToken;
    }
}
