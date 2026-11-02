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
        public static readonly IReadOnlyDictionary<string, string> Encabezados = new Dictionary<string, string>
        {
            ["Content-Security-Policy"] = "default-src 'none'; style-src 'unsafe-inline'; form-action 'self'; frame-ancestors 'none'; base-uri 'none'",
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

        internal static string Documento(string contenido) => $@"<!doctype html>
<html lang=""es"">
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1"">
<title>EcoAccess · Registro</title>
<style>
  body {"{"} font-family: system-ui, sans-serif; max-width: 32rem; margin: 2rem auto; padding: 0 1rem; color: #1b2a1f; background: #f6faf6; {"}"}
  label {"{"} display: block; margin: 1rem 0; {"}"}
  input[name=nombre], input[name=correo] {"{"} width: 100%; padding: .5rem; box-sizing: border-box; {"}"}
  button {"{"} padding: .6rem 1.2rem; background: #2e7d32; color: #fff; border: 0; border-radius: 4px; {"}"}
  .error {"{"} color: #b00020; {"}"}
</style>
</head>
<body>
{contenido}
</body>
</html>";

        internal static string Html(string texto) => WebUtility.HtmlEncode(texto);

        /// <summary>Los tokens son base64url de 43 caracteres; se admite un margen sin aceptar textos arbitrarios.</summary>
        private static readonly Regex PatronFormatoToken = new Regex("^[A-Za-z0-9_-]{20,128}$");
        private static Regex FormatoToken() => PatronFormatoToken;
    }
}
