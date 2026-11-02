using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Token recién emitido para el enlace de registro. El token en claro solo existe aquí y en el chat.</summary>
    public sealed record EnlaceRegistro(string Token, DateTime VenceUtc);

    /// <summary>Código que la página muestra a la persona autenticada para escribirlo en el chat del bot.</summary>
    public sealed record CodigoVinculacion(string Codigo, DateTime VenceUtc);

    /// <summary>
    /// Registro de ciudadanos desde Telegram. El bot entrega un enlace con un token de un solo
    /// uso; la persona escribe nombre y correo en una página HTTPS, acepta el tratamiento de datos (RN-02) y la cuenta
    /// queda creada y vinculada a su cuenta de Telegram. El chat nunca recibe ni pide datos personales ni contraseñas.
    /// </summary>
    /// <remarks>
    /// Una cuenta que ya existe se vincula en dos pasos (P-15): la página autenticada prepara un código y el mismo
    /// Telegram que pidió el enlace lo escribe en el chat. Así un enlace reenviado no basta: hace falta acceso a la
    /// cuenta de EcoAccess y al Telegram a la vez. El vínculo con Telegram es el contacto verificado de RN-02: Telegram ya comprobó el
    /// número de teléfono y el identificador de la cuenta no se puede falsificar desde un chat privado.
    /// </remarks>
    public sealed class ServicioVinculacionTelegram
    {
        private readonly IRepositorioUsuarios _usuarios;
        private readonly IRepositorioVinculosTelegram _vinculos;
        private readonly IGeneradorTokens _generador;
        private readonly TimeProvider _reloj;

        public ServicioVinculacionTelegram(
            IRepositorioUsuarios usuarios,
            IRepositorioVinculosTelegram vinculos,
            IGeneradorTokens generador,
            TimeProvider reloj)
        {
            _usuarios = usuarios ?? throw new ArgumentNullException(nameof(usuarios));
            _vinculos = vinculos ?? throw new ArgumentNullException(nameof(vinculos));
            _generador = generador ?? throw new ArgumentNullException(nameof(generador));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

        /// <summary>SHA-256 del token en hexadecimal minúsculo: lo único que se guarda en la base.</summary>
        public static string CalcularHash(string token)
        {
            Compat.ThrowIfNull(token, nameof(token));
            return Compat.ToHexStringLower(Compat.Sha256(Encoding.UTF8.GetBytes(token)));
        }

        /// <summary>
        /// Emite un token para esa cuenta de Telegram. Solo desde un chat privado (el chat es la propia cuenta), para
        /// que el enlace no quede a la vista de un grupo.
        /// </summary>
        public async Task<Resultado<EnlaceRegistro>> SolicitarAsync(
            long telegramUserId,
            long chatId,
            CancellationToken cancellationToken = default)
        {
            if (chatId != telegramUserId)
            {
                return Resultado<EnlaceRegistro>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesTelegram.SoloChatPrivado);
            }

            if (telegramUserId <= 0)
            {
                return Resultado<EnlaceRegistro>.Rechazo(MotivoRechazo.DatosInvalidos, "La cuenta de Telegram no es válida.");
            }

            if (await _vinculos.ObtenerVinculoActivoPorTelegramAsync(telegramUserId, cancellationToken) is not null)
            {
                return Resultado<EnlaceRegistro>.Rechazo(MotivoRechazo.Duplicado, MensajesTelegram.YaVinculada);
            }

            string token = _generador.Generar();
            var registro = new TokenVinculacion(CalcularHash(token), telegramUserId, chatId, Ahora);
            await _vinculos.AgregarTokenAsync(registro, cancellationToken);
            return Resultado<EnlaceRegistro>.Exito(new EnlaceRegistro(token, registro.VenceUtc));
        }

        /// <summary>La página de registro comprueba el token antes de mostrar el formulario.</summary>
        public async Task<Resultado> ConsultarTokenAsync(string? token, CancellationToken cancellationToken = default) =>
            (await CargarTokenVigenteAsync(token, cancellationToken)).Rechazo ?? Resultado.Exito();

        /// <summary>
        /// Crea la cuenta de ciudadano con su consentimiento y la vincula a la cuenta de Telegram del token.
        /// Devuelve el id de la cuenta. El token queda usado aunque la persona vuelva a enviar el formulario.
        /// </summary>
        public async Task<Resultado<int>> RegistrarCiudadanoAsync(
            string? token,
            string nombre,
            string correoElectronico,
            string versionConsentimiento,
            bool aceptaTratamientoDatos,
            CancellationToken cancellationToken = default)
        {
            (TokenVinculacion? registro, Resultado? rechazo) = await CargarTokenVigenteAsync(token, cancellationToken);
            if (rechazo is not null)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            if (!aceptaTratamientoDatos)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, MensajesTelegram.SinConsentimiento);
            }

            Usuario usuario;
            try
            {
                usuario = new Usuario(nombre, correoElectronico, CodigoRol.Ciudadano, Ahora);
                usuario.RegistrarConsentimiento(versionConsentimiento, Ahora);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            if (await _usuarios.ExisteCorreoAsync(usuario.CorreoElectronico, cancellationToken))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, MensajesTelegram.CorreoExistente);
            }

            try
            {
                int id = await _vinculos.RegistrarCiudadanoYVincularAsync(
                    usuario, registro!.HashToken, registro.TelegramUserId, Ahora, cancellationToken);
                return Resultado<int>.Exito(id);
            }
            catch (CambioConcurrenteException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesTelegram.EnlaceVencido);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, MensajesTelegram.CorreoOCuentaExistente);
            }
        }

        /// <summary>Largo del código de vinculación: 8 símbolos de 31 posibles, unos 39 bits.</summary>
        public const int LargoCodigo = 8;

        /// <summary>Intentos permitidos para escribir el código antes de anular la vinculación pendiente.</summary>
        public const int MaximoIntentosCodigo = 5;

        /// <summary>El código vence a los 10 minutos, o antes si vence el token del enlace.</summary>
        public static readonly TimeSpan VigenciaCodigo = TimeSpan.FromMinutes(10);

        // Sin 0/O, 1/I/L: la persona copia el código de la pantalla al chat sin confundirse.
        private const string AlfabetoCodigo = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

        /// <summary>Hash del código ligado al token: el mismo código en otro enlace no coincide.</summary>
        public static string CalcularHashCodigo(string hashToken, string codigo) =>
            CalcularHash(hashToken + ":" + NormalizarCodigo(codigo));

        /// <summary>
        /// Vincula una cuenta EXISTENTE (P-15), paso 1. Lo llama la página HTTPS después de autenticar a la persona con
        /// <see cref="ServicioAutenticacion"/>: <paramref name="idUsuarioAutenticado"/> sale de esa sesión del servidor,
        /// nunca de un campo del navegador. Devuelve el código que la página muestra; el vínculo todavía NO existe.
        /// </summary>
        public async Task<Resultado<CodigoVinculacion>> PrepararVinculacionCuentaExistenteAsync(
            string? token,
            int idUsuarioAutenticado,
            CancellationToken cancellationToken = default)
        {
            (TokenVinculacion? registro, Resultado? rechazo) = await CargarTokenVigenteAsync(token, cancellationToken);
            if (rechazo is not null)
            {
                return Resultado<CodigoVinculacion>.DesdeRechazo(rechazo);
            }

            Usuario? usuario = idUsuarioAutenticado > 0 ? await _usuarios.ObtenerPorIdAsync(idUsuarioAutenticado, cancellationToken) : null;
            if (usuario is null || !usuario.Activo || !usuario.TieneRol(CodigoRol.Ciudadano) || !usuario.TieneConsentimiento)
            {
                return Resultado<CodigoVinculacion>.Rechazo(MotivoRechazo.SinPermiso, CuentaNoVinculable);
            }

            if (await _vinculos.ObtenerVinculoActivoPorUsuarioAsync(usuario.Id, cancellationToken) is not null
                || await _vinculos.ObtenerVinculoActivoPorTelegramAsync(registro!.TelegramUserId, cancellationToken) is not null)
            {
                return Resultado<CodigoVinculacion>.Rechazo(MotivoRechazo.Duplicado, MensajesTelegram.CorreoOCuentaExistente);
            }

            string codigo = Compat.RandomGetString(AlfabetoCodigo, LargoCodigo);
            DateTime vence = Ahora + VigenciaCodigo < registro.VenceUtc ? Ahora + VigenciaCodigo : registro.VenceUtc;
            await _vinculos.GuardarVinculacionPendienteAsync(new VinculacionPendiente(registro.HashToken, registro.TelegramUserId,
                usuario.Id, CalcularHashCodigo(registro.HashToken, codigo), Ahora, vence, 0), cancellationToken);
            return Resultado<CodigoVinculacion>.Exito(new CodigoVinculacion(codigo, vence));
        }

        /// <summary>
        /// Paso 2: el bot entrega el código que la persona escribió en su chat privado. Solo acepta la cuenta de Telegram
        /// que pidió el enlace; tras <see cref="MaximoIntentosCodigo"/> errores la pendiente deja de servir. Devuelve el id
        /// de la cuenta vinculada. No agrega roles ni abre ninguna caneca.
        /// </summary>
        public async Task<Resultado<int>> ConfirmarVinculacionAsync(
            long telegramUserId,
            long chatId,
            string? codigo,
            CancellationToken cancellationToken = default)
        {
            if (chatId != telegramUserId || telegramUserId <= 0)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesTelegram.SoloChatPrivado);
            }

            VinculacionPendiente? pendiente = await _vinculos.ObtenerVinculacionPendienteAsync(telegramUserId, Ahora, cancellationToken);
            if (pendiente is null || pendiente.Intentos >= MaximoIntentosCodigo)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, SinVinculacionPendiente);
            }

            bool coincide = !Compat.IsNullOrWhiteSpace(codigo) && codigo.Length <= 32 && Compat.FixedTimeEquals(
                Encoding.ASCII.GetBytes(CalcularHashCodigo(pendiente.HashToken, codigo)), Encoding.ASCII.GetBytes(pendiente.HashCodigo));
            if (!coincide)
            {
                int intentos = await _vinculos.RegistrarIntentoFallidoAsync(pendiente.HashToken, cancellationToken);
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, intentos >= MaximoIntentosCodigo
                    ? "Código incorrecto. Se agotaron los intentos: abre de nuevo el enlace con /vincular."
                    : $"Código incorrecto. Te quedan {MaximoIntentosCodigo - intentos} intentos.");
            }

            try
            {
                return Resultado<int>.Exito(await _vinculos.ConfirmarVinculacionPendienteAsync(pendiente, Ahora, cancellationToken));
            }
            catch (CambioConcurrenteException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, SinVinculacionPendiente);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, MensajesTelegram.CorreoOCuentaExistente);
            }
        }

        private const string CuentaNoVinculable =
            "Esta cuenta no se puede vincular: debe estar activa, ser de ciudadano y tener el consentimiento vigente.";

        private const string SinVinculacionPendiente =
            "No hay una vinculación pendiente vigente para esta cuenta de Telegram. Abre de nuevo el enlace con /vincular.";

        private static string NormalizarCodigo(string codigo) =>
            new(codigo.Where(c => !char.IsWhiteSpace(c) && c != '-').Select(char.ToUpperInvariant).ToArray());

        /// <summary>Cuenta de EcoAccess vinculada a esa cuenta de Telegram, o null si es un invitado.</summary>
        public async Task<int?> ObtenerUsuarioVinculadoAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            (await _vinculos.ObtenerVinculoActivoPorTelegramAsync(telegramUserId, cancellationToken))?.IdUsuario;

        /// <summary>Cierra el vínculo; la cuenta y su historial se conservan (trazabilidad, RN-24).</summary>
        public async Task<Resultado> DesvincularAsync(long telegramUserId, CancellationToken cancellationToken = default)
        {
            VinculoTelegram? vinculo = await _vinculos.ObtenerVinculoActivoPorTelegramAsync(telegramUserId, cancellationToken);
            if (vinculo is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, MensajesTelegram.SinVinculo);
            }

            await _vinculos.DarDeBajaAsync(vinculo.IdUsuario, Ahora, cancellationToken);
            return Resultado.Exito();
        }

        private async Task<(TokenVinculacion? Token, Resultado? Rechazo)> CargarTokenVigenteAsync(
            string? token,
            CancellationToken cancellationToken)
        {
            if (Compat.IsNullOrWhiteSpace(token))
            {
                return (null, Resultado.Rechazo(MotivoRechazo.NoEncontrado, MensajesTelegram.EnlaceInvalido));
            }

            TokenVinculacion? registro = await _vinculos.ObtenerTokenAsync(CalcularHash(token.Trim()), cancellationToken);
            if (registro is null)
            {
                return (null, Resultado.Rechazo(MotivoRechazo.NoEncontrado, MensajesTelegram.EnlaceInvalido));
            }

            return registro.EstaVigente(Ahora)
                ? (registro, null)
                : (null, Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesTelegram.EnlaceVencido));
        }
    }
}
