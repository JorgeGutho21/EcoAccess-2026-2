using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using static EcoAccess.P3.Bot.PaginaRegistro;

namespace EcoAccess.P3.Bot
{

    /// <summary>Adaptador web del registro público y la autenticación existentes; el bot confirma el segundo canal.</summary>
    public sealed class PaginaVinculacion
    {
        public const string NombreCookie = "__Host-ecoaccess-vinculo";
        private const int MaximoFormularios = 512;
        private static readonly TimeSpan VigenciaFormulario = TimeSpan.FromMinutes(10);
        private readonly ServicioVinculacionTelegram _vinculacion;
        private readonly ServicioAutenticacion _autenticacion;
        private readonly ServicioRegistroCiudadanos _registro;
        private readonly ILimitadorUso _global;
        private readonly ILimitadorUso _porToken;
        private readonly TimeProvider _reloj;
        private readonly Dictionary<string, FormularioPendiente> _formularios = new Dictionary<string, FormularioPendiente>();
        private sealed record FormularioPendiente(string HashToken, string Csrf, DateTimeOffset Hasta);

        public PaginaVinculacion(ServicioVinculacionTelegram vinculacion, ServicioAutenticacion autenticacion,
            ServicioRegistroCiudadanos registro, ILimitadorUso global, ILimitadorUso porToken, TimeProvider reloj)
        {
            _vinculacion = vinculacion ?? throw new ArgumentNullException(nameof(vinculacion));
            _autenticacion = autenticacion ?? throw new ArgumentNullException(nameof(autenticacion));
            _registro = registro ?? throw new ArgumentNullException(nameof(registro));
            _global = global ?? throw new ArgumentNullException(nameof(global));
            _porToken = porToken ?? throw new ArgumentNullException(nameof(porToken));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public async Task<RespuestaPagina> MostrarAsync(string? consulta, CancellationToken ct = default)
        {
            if (consulta?.Length > TamanoMaximo) return Aviso(400, "El enlace no es válido.");
            string? token = Unico(HttpUtility.ParseQueryString(consulta ?? ""), "t");
            var freno = Frenar(token);
            if (freno is not null) return freno;
            var vigente = await _vinculacion.ConsultarTokenAsync(token, ct);
            return vigente.Exitoso ? Formulario(token!) : Aviso(Codigo(vigente.Motivo), vigente.Mensaje!);
        }

        public async Task<RespuestaPagina> EnviarAsync(string? tipo, byte[] cuerpo, string? cookie, CancellationToken ct = default)
        {
            Compat.ThrowIfNull(cuerpo, nameof(cuerpo));
            if (cuerpo.Length > TamanoMaximo) return Aviso(413, "El formulario excede el tamaño permitido.");
            if (!string.Equals(tipo?.Split(';')[0].Trim(), "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                return Aviso(415, "El formato del formulario no es válido.");
            NameValueCollection campos;
            try { campos = HttpUtility.ParseQueryString(new UTF8Encoding(false, true).GetString(cuerpo)); }
            catch (DecoderFallbackException) { return Aviso(400, "El formulario no es válido."); }
            string? token = Unico(campos, "t");
            var freno = Frenar(token);
            if (freno is not null) return freno;
            if (!ConsumirFormulario(cookie, token!, Unico(campos, "csrf")))
                return Aviso(403, "El formulario venció o no pertenece a este navegador. Abre otra vez el enlace del bot.");
            var vigente = await _vinculacion.ConsultarTokenAsync(token, ct);
            if (!vigente.Exitoso) return Aviso(Codigo(vigente.Motivo), vigente.Mensaje!);
            string? modo = Unico(campos, "modo"), correo = Unico(campos, "correo"), clave = Unico(campos, "clave");
            if (correo is null || correo.Length > 254 || clave is null || clave.Length > 128)
                return Formulario(token!, "Revisa los campos del formulario.");
            bool creada = false;
            if (modo == "crear")
            {
                string? nombre = Unico(campos, "nombre"), confirmacion = Unico(campos, "confirmacion");
                if (nombre is null || nombre.Length > 100 || confirmacion?.Length > 128)
                    return Formulario(token!, "Revisa los campos del registro.");
                var alta = await _registro.RegistrarAsync(nombre, correo, clave, confirmacion, Unico(campos, "acepta") == "si", ct);
                if (!alta.Exitoso) return Formulario(token!, alta.Mensaje!);
                creada = true;
            }
            else if (modo != "entrar") return Formulario(token!, "Elige crear cuenta o iniciar sesión.");

            var sesion = await _autenticacion.IniciarSesionPublicaAsync(correo, clave, ct);
            if (!sesion.Exitoso) return Formulario(token!, sesion.Mensaje!);
            // La identidad sale exclusivamente de la autenticación del servidor. No hay idUsuario en el formulario.
            var preparada = await _vinculacion.PrepararVinculacionCuentaExistenteAsync(token, sesion.Valor!.IdUsuario, ct);
            if (!preparada.Exitoso)
                return Aviso(Codigo(preparada.Motivo), (creada ? "Tu cuenta quedó creada. Puedes iniciar sesión y pedir otro enlace. " : "") + preparada.Mensaje);
            return new RespuestaPagina(200, Documento($@"<h1>Confirma desde tu Telegram</h1>
<p>La vinculación todavía no ha terminado. En el chat privado del bot oficial de EcoAccess escribe:</p>
<p><code>/confirmar {Html(preparada.Valor!.Codigo)}</code></p>
<p>El código vence a las {Html(preparada.Valor.VenceUtc.ToString("HH:mm"))} UTC. No lo compartas con otra persona.
Si el enlace te lo envió otra persona, cierra esta página y pide tu propio enlace en el bot oficial.</p>
<p>Solo tras la confirmación del bot quedará vinculado Telegram. Esto no abre ninguna caneca.</p>"), BorrarCookie);
        }

        private RespuestaPagina? Frenar(string? token)
        {
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            if (!_global.Permitir("vinculacion-web", ahora)) return Aviso(429, "Demasiadas solicitudes. Espera un minuto.");
            if (token is null || token.Length is < 20 or > 128 || token.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not '_' and not '-'))
                return Aviso(400, "El enlace no es válido. Pide otro con /vincular.");
            return _porToken.Permitir(ServicioVinculacionTelegram.CalcularHash(token), ahora)
                ? null : Aviso(429, "Demasiadas solicitudes para este enlace. Espera un minuto.");
        }

        private bool ConsumirFormulario(string? cookie, string token, string? csrf)
        {
            if (cookie?.Length != 64 || csrf?.Length != 64) return false;
            lock (_formularios)
            {
                if (!_formularios.TryGetValue(cookie, out var formulario)
                    || formulario.Hasta <= _reloj.GetUtcNow()
                    || formulario.HashToken != ServicioVinculacionTelegram.CalcularHash(token)
                    || !Compat.FixedTimeEquals(Encoding.ASCII.GetBytes(formulario.Csrf), Encoding.ASCII.GetBytes(csrf)))
                    return false;
                _formularios.Remove(cookie); // Un solo POST por formulario, antes de autenticación o escrituras.
                return true;
            }
        }

        private RespuestaPagina Formulario(string token, string? error = null)
        {
            string cookie = Compat.ToHexStringLower(Compat.RandomGetBytes(32));
            string csrf = Compat.ToHexStringLower(Compat.RandomGetBytes(32));
            lock (_formularios)
            {
                foreach (string k in _formularios.Where(p => p.Value.Hasta <= _reloj.GetUtcNow()).Select(p => p.Key).ToArray())
                    _formularios.Remove(k);
                if (_formularios.Count >= MaximoFormularios) return Aviso(503, "No hay capacidad para otro formulario. Intenta más tarde.");
                _formularios.Add(cookie, new(ServicioVinculacionTelegram.CalcularHash(token), csrf, _reloj.GetUtcNow() + VigenciaFormulario));
            }
            string ocultos = $"<input type=\"hidden\" name=\"t\" value=\"{Html(token)}\"><input type=\"hidden\" name=\"csrf\" value=\"{csrf}\">";
            string aviso = error is null ? "" : $"<p class=\"error\" role=\"alert\">{Html(error)}</p>";
            return new RespuestaPagina(200, Documento($@"<h1>Conecta EcoAccess con tu Telegram</h1>
<p>Utiliza únicamente el enlace que pediste en el bot oficial. No escribas tu contraseña en Telegram.</p>
{aviso}
<h2>Ya tengo cuenta</h2>
<form method=""post"" action=""./"">{ocultos}<input type=""hidden"" name=""modo"" value=""entrar"">
<label>Correo electrónico<input type=""email"" name=""correo"" maxlength=""254"" autocomplete=""username"" required></label>
<label>Contraseña<input type=""password"" name=""clave"" maxlength=""128"" autocomplete=""current-password"" required></label>
<button type=""submit"">Iniciar sesión y obtener código</button></form>
<h2>Crear cuenta ciudadana</h2>
<form method=""post"" action=""./"">{ocultos}<input type=""hidden"" name=""modo"" value=""crear"">
<label>Nombre<input name=""nombre"" maxlength=""100"" autocomplete=""name"" required></label>
<label>Correo electrónico<input type=""email"" name=""correo"" maxlength=""254"" autocomplete=""username"" required></label>
<label>Contraseña<input type=""password"" name=""clave"" minlength=""12"" maxlength=""128"" autocomplete=""new-password"" required></label>
<label>Repetir contraseña<input type=""password"" name=""confirmacion"" minlength=""12"" maxlength=""128"" autocomplete=""new-password"" required></label>
<p>Tratamiento de datos personales (Ley 1581 de 2012): EcoAccess guarda nombre, correo, consentimiento,
credencial protegida y operaciones para prestar el servicio. Telegram solo se asocia tras confirmar el código.
Puedes pedir consulta, corrección o supresión de datos y usar /desvincular. Versión: {Html(ServicioRegistroCiudadanos.VersionConsentimiento)}.</p>
<label><input type=""checkbox"" name=""acepta"" value=""si"" required>Acepto el tratamiento de mis datos personales.</label>
<button type=""submit"">Crear cuenta y obtener código</button></form>
<p>El registro público solo crea ciudadanos. También puedes volver al bot y consultar /materiales sin vincularte.</p>"), $"{NombreCookie}={cookie}; Path=/; Secure; HttpOnly; SameSite=Strict; Max-Age=600");
        }

        private const string BorrarCookie = NombreCookie + "=; Path=/; Secure; HttpOnly; SameSite=Strict; Max-Age=0";
        private static string? Unico(NameValueCollection campos, string nombre) => campos.GetValues(nombre) is { Length: 1 } valores ? valores[0] : null;
        private static int Codigo(MotivoRechazo? motivo) => motivo switch
        { MotivoRechazo.NoEncontrado => 404, MotivoRechazo.EstadoNoPermitido => 410, MotivoRechazo.Duplicado => 409, MotivoRechazo.SinPermiso => 403, _ => 400 };
        private static RespuestaPagina Aviso(int codigo, string texto) => new(codigo, Documento($"<p>{Html(texto)}</p>"));
    }
}
