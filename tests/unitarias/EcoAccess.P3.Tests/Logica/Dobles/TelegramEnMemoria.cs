using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    // Dobles de Telegram. Igual que los demás repositorios en memoria, guardan copias y repiten las restricciones
    // que en Oracle imponen las llaves únicas y la versión optimista. No sustituyen las pruebas contra Oracle.

    /// <summary>Vínculos y tokens; registra la cuenta en <see cref="RepositorioUsuariosEnMemoria"/> en una sola operación.</summary>
    internal sealed class RepositorioVinculosEnMemoria : IRepositorioVinculosTelegram
    {
        private readonly RepositorioUsuariosEnMemoria _usuarios;

        public RepositorioVinculosEnMemoria(RepositorioUsuariosEnMemoria usuarios)
        {
            _usuarios = usuarios;
        }

        private readonly Dictionary<string, TokenVinculacion> _tokens = new Dictionary<string, TokenVinculacion>();
        private readonly List<VinculoTelegram> _vinculos = new List<VinculoTelegram>();
        private int _siguienteId = 1;

        public IReadOnlyCollection<TokenVinculacion> Tokens => _tokens.Values.ToList();
        public IReadOnlyCollection<VinculoTelegram> Vinculos => _vinculos.ToList();
        public Func<CancellationToken, Task>? AntesDeLeerTokenAsync { get; set; }

        /// <summary>Vincula una cuenta ya existente, como si se hubiera registrado antes.</summary>
        public VinculoTelegram Sembrar(int idUsuario, long telegramUserId)
        {
            var vinculo = new VinculoTelegram(_siguienteId++, idUsuario, telegramUserId, Fechas.Base);
            _vinculos.Add(vinculo);
            return vinculo;
        }

        public Task<VinculoTelegram?> ObtenerVinculoActivoPorTelegramAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<VinculoTelegram?>(_vinculos.FirstOrDefault(v => v.Activo && v.TelegramUserId == telegramUserId));

        public Task<VinculoTelegram?> ObtenerVinculoActivoPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            Task.FromResult<VinculoTelegram?>(_vinculos.FirstOrDefault(v => v.Activo && v.IdUsuario == idUsuario));

        public Task AgregarTokenAsync(TokenVinculacion token, CancellationToken cancellationToken = default)
        {
            if (!_tokens.TryAdd(token.HashToken, token))
            {
                throw new ConflictoUnicidadException("pk_token_vinculacion");
            }

            return Task.CompletedTask;
        }

        public async Task<TokenVinculacion?> ObtenerTokenAsync(string hashToken, CancellationToken cancellationToken = default)
        {
            if (AntesDeLeerTokenAsync is not null) await AntesDeLeerTokenAsync(cancellationToken);
            return _tokens.GetValueOrDefault(hashToken);
        }

        public async Task<int> RegistrarCiudadanoYVincularAsync(
            Usuario usuario,
            string hashToken,
            long telegramUserId,
            DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            // Oracle marca el token con UPDATE ... WHERE uso IS NULL AND vence > ahora; 0 filas es un cambio concurrente.
            if (!_tokens.TryGetValue(hashToken, out var token) || !token.EstaVigente(ahoraUtc) || ahoraUtc < token.CreacionUtc
                || token.TelegramUserId != telegramUserId)
            {
                throw new CambioConcurrenteException("El token ya no está vigente.");
            }

            if (_vinculos.Any(v => v.Activo && v.TelegramUserId == telegramUserId))
            {
                throw new ConflictoUnicidadException("uq_vinculo_telegram_activo");
            }

            int idUsuario = await _usuarios.AgregarAsync(usuario, cancellationToken);
            _tokens[hashToken] = new TokenVinculacion(token.HashToken, token.TelegramUserId, token.ChatId, token.CreacionUtc, token.VenceUtc, ahoraUtc);
            _vinculos.Add(new VinculoTelegram(_siguienteId++, idUsuario, telegramUserId, ahoraUtc));
            return idUsuario;
        }

        public Task DarDeBajaAsync(int idUsuario, DateTime fechaUtc, CancellationToken cancellationToken = default)
        {
            int i = _vinculos.FindIndex(v => v.Activo && v.IdUsuario == idUsuario);
            if (i >= 0)
            {
                var v = _vinculos[i];
                _vinculos[i] = new VinculoTelegram(v.Id, v.IdUsuario, v.TelegramUserId, v.FechaVinculoUtc, fechaUtc);
            }

            return Task.CompletedTask;
        }

        private readonly Dictionary<string, VinculacionPendiente> _pendientes = new Dictionary<string, VinculacionPendiente>();
        public IReadOnlyCollection<VinculacionPendiente> Pendientes => _pendientes.Values.ToList();

        public Task GuardarVinculacionPendienteAsync(VinculacionPendiente pendiente, CancellationToken cancellationToken = default)
        {
            _pendientes[pendiente.HashToken] = pendiente with { Intentos = 0 };
            return Task.CompletedTask;
        }

        /// <summary>Se ejecuta justo después de leer la pendiente: permite simular una preparación simultánea.</summary>
        public Func<Task>? DespuesDeLeerPendiente { get; set; }

        public async Task<VinculacionPendiente?> ObtenerVinculacionPendienteAsync(long telegramUserId, DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            VinculacionPendiente? leida = _pendientes.Values
                .Where(p => p.TelegramUserId == telegramUserId && p.VenceUtc > ahoraUtc
                    && _tokens.TryGetValue(p.HashToken, out var t) && t.UsoUtc is null)
                .OrderByDescending(p => p.CreacionUtc)
                .FirstOrDefault();
            if (DespuesDeLeerPendiente is { } accion)
            {
                DespuesDeLeerPendiente = null;
                await accion();
            }

            return leida;
        }

        public Task<int> RegistrarIntentoFallidoAsync(string hashToken, CancellationToken cancellationToken = default)
        {
            if (!_pendientes.TryGetValue(hashToken, out var p)) return Task.FromResult(int.MaxValue);
            _pendientes[hashToken] = p with { Intentos = p.Intentos + 1 };
            return Task.FromResult(p.Intentos + 1);
        }

        public Func<Task>? AntesDeConfirmarPendienteAsync { get; set; }

        public async Task<int> ConfirmarVinculacionPendienteAsync(VinculacionPendiente verificada, DateTime ahoraUtc,
            CancellationToken cancellationToken = default)
        {
            // Mismas comprobaciones que la transacción Oracle: la pendiente sigue siendo la verificada, pendiente y token
            // vigentes, cuenta habilitada, vínculos libres.
            if (AntesDeConfirmarPendienteAsync is not null) await AntesDeConfirmarPendienteAsync();
            string hashToken = verificada.HashToken;
            if (!_pendientes.TryGetValue(hashToken, out var p) || p.HashCodigo != verificada.HashCodigo
                || p.IdUsuario != verificada.IdUsuario || p.VenceUtc <= ahoraUtc || p.Intentos >= 5
                || !_tokens.TryGetValue(hashToken, out var token) || !token.EstaVigente(ahoraUtc)
                || await _usuarios.ObtenerPorIdAsync(p.IdUsuario, cancellationToken) is not { Activo: true, TieneConsentimiento: true } cuenta
                || !cuenta.TieneRol(CodigoRol.Ciudadano))
            {
                throw new CambioConcurrenteException("La vinculación pendiente ya no es válida.");
            }

            if (_vinculos.Any(v => v.Activo && (v.TelegramUserId == p.TelegramUserId || v.IdUsuario == p.IdUsuario)))
            {
                throw new ConflictoUnicidadException("uq_vinculo_activo");
            }

            _tokens[hashToken] = new TokenVinculacion(token.HashToken, token.TelegramUserId, token.ChatId, token.CreacionUtc, token.VenceUtc, ahoraUtc);
            _vinculos.Add(new VinculoTelegram(_siguienteId++, p.IdUsuario, p.TelegramUserId, ahoraUtc));
            _pendientes.Remove(hashToken);
            return p.IdUsuario;
        }
    }

    /// <summary>Conversaciones con versión optimista y registro de updates ya procesados.</summary>
    internal sealed class RepositorioConversacionesEnMemoria : IRepositorioConversacionesTelegram
    {
        private readonly Dictionary<long, ConversacionTelegram> _conversaciones = new Dictionary<long, ConversacionTelegram>();
        private readonly HashSet<long> _updates = new HashSet<long>();

        /// <summary>Se ejecuta una vez antes de la próxima escritura: simula otro mensaje procesado en paralelo.</summary>
        public Action? AlEscribir { get; set; }

        public ConversacionTelegram? Leer(long telegramUserId) =>
            _conversaciones.TryGetValue(telegramUserId, out var c) ? Copiar(c, c.Version) : null;

        /// <summary>Cambia la versión guardada, como si otra sesión hubiera escrito entre la lectura y la escritura.</summary>
        public void IncrementarVersion(long telegramUserId)
        {
            var c = _conversaciones[telegramUserId];
            _conversaciones[telegramUserId] = Copiar(c, c.Version + 1);
        }

        public Task<ConversacionTelegram?> ObtenerAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(telegramUserId));

        public Task GuardarAsync(ConversacionTelegram conversacion, CancellationToken cancellationToken = default)
        {
            var alEscribir = AlEscribir;
            AlEscribir = null;
            alEscribir?.Invoke();

            int versionGuardada = _conversaciones.TryGetValue(conversacion.TelegramUserId, out var actual) ? actual.Version : 0;
            bool nueva = actual is null;
            if ((nueva && conversacion.Version != 0) || (!nueva && versionGuardada != conversacion.Version))
            {
                throw new CambioConcurrenteException("La conversación cambió.");
            }

            _conversaciones[conversacion.TelegramUserId] = Copiar(conversacion, conversacion.Version + 1);
            return Task.CompletedTask;
        }

        public Task<bool> RegistrarUpdateAsync(long updateId, DateTime recibidoUtc, CancellationToken cancellationToken = default) =>
            Task.FromResult(_updates.Add(updateId));

        private static ConversacionTelegram Copiar(ConversacionTelegram c, int version) =>
            new(c.TelegramUserId, c.ChatId, c.Estado, c.IdContenedor, c.IdInteraccion, c.IdMaterialPropuesto, c.FechaActualizacionUtc, version);
    }

    /// <summary>Devuelve tokens predecibles para poder escribir el enlace esperado en las pruebas.</summary>
    internal sealed class GeneradorTokensSecuencial : IGeneradorTokens
    {
        private int _n;

        public string Generar() => $"token-de-prueba-{++_n:D4}";
    }

    /// <summary>Limitador que se abre o cierra a mano.</summary>
    internal sealed class LimitadorManual : ILimitadorUso
    {
        public bool Permitido { get; set; } = true;

        public bool Permitir(string clave, DateTime ahoraUtc) => Permitido;
    }
}
