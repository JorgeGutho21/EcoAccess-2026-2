using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using EcoAccess.P3.Entidades;
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
            NameValueCollection parametros = HttpUtility.ParseQueryString(consulta ?? "");
            string? token = Unico(parametros, "t");
            var freno = Frenar(token);
            if (freno is not null) return freno;
            var vigente = await _vinculacion.ConsultarTokenAsync(token, ct);
            return vigente.Exitoso ? Formulario(token!, null, Unico(parametros, "m")) : Aviso(Codigo(vigente.Motivo), vigente.Mensaje!);
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
            string? correoVisible = correo?.Length <= 254 ? correo : null;
            if (correo is null || correo.Length > 254 || clave is null || clave.Length > CredencialAcceso.LongitudMaximaContrasena)
                return Formulario(token!, "Revisa los campos del formulario.", modo, new Intento(Correo: correoVisible));
            bool creada = false;
            if (modo == "crear")
            {
                string? nombre = Unico(campos, "nombre"), confirmacion = Unico(campos, "confirmacion");
                string? nombreVisible = nombre?.Length <= 100 ? nombre : null;
                // Cada problema se muestra junto a su campo. El servicio vuelve a validar todo: esto solo orienta.
                var errores = new Dictionary<string, string>(StringComparer.Ordinal);
                if (Compat.IsNullOrWhiteSpace(nombre) || nombre!.Length > 100) errores["nombre"] = "Escribe tu nombre (máximo 100 caracteres).";
                if (PoliticaContrasenaCiudadano.Validar(clave) is { } problema) errores["clave"] = problema;
                else if (!string.Equals(clave, confirmacion, StringComparison.Ordinal)) errores["confirmacion"] = "Las contraseñas no coinciden.";
                if (Unico(campos, "acepta") != "si") errores["acepta"] = "Debes aceptar el tratamiento de datos para crear tu cuenta.";
                if (errores.Count > 0) return Formulario(token!, "Revisa los campos marcados.", modo, new Intento(nombreVisible, correoVisible, errores));
                var alta = await _registro.RegistrarAsync(nombre!, correo, clave, confirmacion, true, ct);
                if (!alta.Exitoso) return Formulario(token!, alta.Mensaje!, modo, new Intento(nombreVisible, correoVisible));
                creada = true;
            }
            else if (modo != "entrar") return Formulario(token!, "Elige crear cuenta o iniciar sesión.");

            var sesion = await _autenticacion.IniciarSesionPublicaAsync(correo, clave, ct);
            if (!sesion.Exitoso) return Formulario(token!, sesion.Mensaje!, modo, new Intento(Correo: correoVisible));
            // La identidad sale exclusivamente de la autenticación del servidor. No hay idUsuario en el formulario.
            var preparada = await _vinculacion.PrepararVinculacionCuentaExistenteAsync(token, sesion.Valor!.IdUsuario, ct);
            if (!preparada.Exitoso)
                return Aviso(Codigo(preparada.Motivo), (creada ? "Tu cuenta quedó creada. Puedes iniciar sesión y pedir otro enlace. " : "") + preparada.Mensaje);
            return new RespuestaPagina(200, Documento($@"<h1>{(creada ? "¡Tu cuenta quedó creada!" : "Sesión iniciada")}</h1>
<p>Falta un último paso: confirma desde tu Telegram para que EcoAccess te reconozca.</p>
<p><a class=""principal"" style=""text-align:center;text-decoration:none"" href=""{Html(EnlaceTelegram.ParaConfirmar(preparada.Valor!.Codigo).ToString())}"">Volver a Eco y confirmar</a></p>
<p>Si el enlace no abre Telegram, escribe en el chat privado del bot oficial de EcoAccess:</p>
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

        private RespuestaPagina Formulario(string token, string? error = null, string? modo = null, Intento? intento = null)
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
            string aviso = error is null ? "" : $"<p class=\"error alerta\" role=\"alert\">{Html(error)}</p>";
            intento ??= new Intento();
            string cuerpo = modo == "entrar"
                ? $@"<h1>Inicia sesión</h1>
<p class=""intro"">Usa la cuenta de EcoAccess que ya tienes. Recuperarás tu historial y tus ecopuntos.</p>
{aviso}
<form method=""post"" action=""./"">{ocultos}<input type=""hidden"" name=""modo"" value=""entrar"">
{Campo(intento, "correo", "correo", "Correo electrónico", "email", "username", 254, valor: intento.Correo)}
{Campo(intento, "clave-actual", "clave", "Contraseña", "password", "current-password", CredencialAcceso.LongitudMaximaContrasena, mostrar: true)}
<button class=""principal"" type=""submit"">Iniciar sesión y continuar</button></form>
<a class=""secundario"" href=""./?t={Html(token)}&amp;m=crear"">¿No tienes cuenta? Crea una</a>"
                : $@"<h1>Crea tu cuenta EcoAccess</h1>
<p class=""intro"">Regístrate una sola vez. Después confirmas desde Telegram y Eco te reconocerá en cada caneca.</p>
{aviso}
<form method=""post"" action=""./"">{ocultos}<input type=""hidden"" name=""modo"" value=""crear"">
{Campo(intento, "nombre", "nombre", "Nombre", "text", "name", 100, valor: intento.Nombre)}
{Campo(intento, "correo", "correo", "Correo electrónico", "email", "email", 254, valor: intento.Correo)}
{Campo(intento, "clave-nueva", "clave", "Contraseña", "password", "new-password", PoliticaContrasenaCiudadano.LongitudMaxima, PoliticaContrasenaCiudadano.LongitudMinima, PoliticaContrasenaCiudadano.Ayuda, mostrar: true)}
{Campo(intento, "confirmacion", "confirmacion", "Confirma la contraseña", "password", "new-password", PoliticaContrasenaCiudadano.LongitudMaxima, PoliticaContrasenaCiudadano.LongitudMinima, mostrar: true)}
<details><summary>Tratamiento de datos personales</summary>
<p>Según la Ley 1581 de 2012, EcoAccess guarda tu nombre, tu correo, tu consentimiento, tu contraseña protegida (nunca en texto)
y tus operaciones para prestar el servicio. Telegram solo se asocia cuando confirmas el código. Puedes pedir consulta,
corrección o supresión de tus datos y usar /desvincular. Versión: {Html(ServicioRegistroCiudadanos.VersionConsentimiento)}.</p></details>
<label class=""acepto""><input type=""checkbox"" name=""acepta"" value=""si"" required><span>Acepto el tratamiento de mis datos personales.</span></label>
{Error(intento, "acepta")}
<button class=""principal"" type=""submit"">Crear cuenta y continuar</button></form>
<a class=""secundario"" href=""./?t={Html(token)}&amp;m=entrar"">Ya tengo cuenta</a>";
            return new RespuestaPagina(200, Documento(cuerpo + @"
<p class=""pie"">El registro público solo crea cuentas de ciudadano. Usa solo el enlace que pediste en el bot oficial y no escribas tu contraseña en Telegram.</p>", conScript: true),
                $"{NombreCookie}={cookie}; Path=/; Secure; HttpOnly; SameSite=Strict; Max-Age=600");
        }

        /// <summary>Lo que la persona ya escribió (nunca la contraseña) y el error de cada campo, para mostrarlo a su lado.</summary>
        private sealed record Intento(string? Nombre = null, string? Correo = null, IReadOnlyDictionary<string, string>? Errores = null);

        private static string Error(Intento intento, string campo) =>
            intento.Errores?.TryGetValue(campo, out string? texto) == true ? $"<p class=\"error\" id=\"error-{campo}\" role=\"alert\">{Html(texto!)}</p>" : "";

        private static string Campo(Intento intento, string id, string nombre, string etiqueta, string tipo, string autocompletar, int maximo,
            int? minimo = null, string? ayuda = null, bool mostrar = false, string? valor = null)
        {
            string error = Error(intento, nombre);
            string descrito = error.Length > 0 ? $" aria-invalid=\"true\" aria-describedby=\"error-{nombre}\"" : ayuda is null ? "" : $" aria-describedby=\"ayuda-{id}\"";
            string entrada = $"<input id=\"{id}\" type=\"{tipo}\" name=\"{nombre}\" maxlength=\"{maximo}\"{(minimo is { } m ? $" minlength=\"{m}\"" : "")}" +
                             $" autocomplete=\"{autocompletar}\"{(tipo == "email" ? " inputmode=\"email\" autocapitalize=\"none\" spellcheck=\"false\"" : "")}" +
                             $"{(valor is null ? "" : $" value=\"{Html(valor)}\"")} required{descrito}>";
            if (mostrar) entrada = $"<div class=\"con-boton\">{entrada}<button class=\"ver\" type=\"button\" data-mostrar=\"{id}\" aria-pressed=\"false\" hidden>Mostrar</button></div>";
            return $"<div class=\"campo{(error.Length > 0 ? " con-error" : "")}\"><label for=\"{id}\">{etiqueta}</label>{entrada}" +
                   $"{(ayuda is null ? "" : $"<p class=\"ayuda\" id=\"ayuda-{id}\">{Html(ayuda)}</p>")}{error}</div>";
        }

        private const string BorrarCookie = NombreCookie + "=; Path=/; Secure; HttpOnly; SameSite=Strict; Max-Age=0";
        private static string? Unico(NameValueCollection campos, string nombre) => campos.GetValues(nombre) is { Length: 1 } valores ? valores[0] : null;
        private static int Codigo(MotivoRechazo? motivo) => motivo switch
        { MotivoRechazo.NoEncontrado => 404, MotivoRechazo.EstadoNoPermitido => 410, MotivoRechazo.Duplicado => 409, MotivoRechazo.SinPermiso => 403, _ => 400 };
        private static RespuestaPagina Aviso(int codigo, string texto) => new(codigo, Documento($"<p>{Html(texto)}</p>"));
    }
}
