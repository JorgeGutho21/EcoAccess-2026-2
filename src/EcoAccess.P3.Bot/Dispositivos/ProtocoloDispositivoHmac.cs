using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Dispositivos
{

    /// <summary>Binding explícito, no un secreto por defecto. El host conserva las claves fuera del repositorio.</summary>
    public sealed class CredencialDispositivo : IDisposable
    {
        private readonly byte[] _clave;
        private readonly object _bloqueo = new();
        private bool _liberada;
        public string Dispositivo { get; }
        public int VersionClave { get; }
        public int IdContenedor { get; }
        public bool Habilitada { get; }
        public CredencialDispositivo(string dispositivo, int versionClave, int idContenedor, ReadOnlySpan<byte> clave, bool habilitada = true)
        {
            if (Compat.IsNullOrWhiteSpace(dispositivo) || dispositivo.Length > 40
                || dispositivo.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not '_' and not '-')
                || versionClave <= 0 || idContenedor <= 0 || clave.Length < 32 || clave.Length > 128)
                throw new ArgumentException("Binding de dispositivo inválido.");
            Dispositivo = dispositivo; VersionClave = versionClave; IdContenedor = idContenedor; Habilitada = habilitada; _clave = clave.ToArray();
        }
        internal byte[] Firmar(byte[] bytes)
        {
            lock (_bloqueo)
            {
                Compat.ThrowIfDisposed(_liberada, this);
                return Compat.HmacSha256(_clave, bytes);
            }
        }
        public void Dispose()
        {
            lock (_bloqueo) { Compat.ZeroMemory(_clave); _liberada = true; }
        }
    }

    public sealed record SobreDispositivo(int Version, string Dispositivo, int VersionClave, string Tipo, string Datos, string Firma);
    public sealed record ComandoFirmable(ComandoApertura Comando, DateTime EmitidoUtc);
    public sealed record EventoFirmable(EventoRecibido Evento, DateTime EmitidoUtc, DateTime VenceUtc);
    public sealed record ConsultaVigenciaFirmable(ComandoApertura Comando, string IdConsulta, DateTime EmitidoUtc, DateTime VenceUtc);
    public sealed record RecepcionFirmable(string TipoOriginal, string Sha256Paquete, bool Aceptado,
        DateTime EmitidoUtc, DateTime VenceUtc, EventoRegistrado? Registro = null);

    /// <summary>Propuesta de protocolo de aplicación, independiente de HTTP/Serial/MQTT. No abre conexiones físicas.</summary>
    public sealed class ProtocoloDispositivoHmac
    {
        private readonly TimeProvider _reloj;
        private readonly Func<string, int, CredencialDispositivo?> _resolver;

        public ProtocoloDispositivoHmac(TimeProvider reloj, Func<string, int, CredencialDispositivo?> resolver)
        {
            _reloj = reloj;
            _resolver = resolver;
        }

        public const int MaximoPaquete = 4096;
        public static readonly TimeSpan VigenciaMaxima = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan DesfaseMaximo = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan VigenciaRecepcion = TimeSpan.FromSeconds(30);
        private static readonly JsonSerializerOptions Json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            MaxDepth = 12,
        };
        private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

        public byte[] FirmarComando(CredencialDispositivo credencial, ComandoApertura comando)
        {
            if (!ComandoValido(credencial, comando)) throw new ArgumentException("El comando no es válido o ya venció.");
            return CrearSobre(credencial, "comando", new ComandoFirmable(comando, Ahora));
        }
        public byte[] FirmarEvento(CredencialDispositivo credencial, EventoRecibido evento)
        {
            DateTime ahora = Ahora;
            var cuerpo = new EventoFirmable(evento, ahora, ahora + VigenciaMaxima);
            if (!EventoValido(credencial, cuerpo)) throw new ArgumentException("El evento no es válido para ese dispositivo.");
            return CrearSobre(credencial, "evento", cuerpo);
        }
        public Resultado<ComandoApertura> ValidarComando(byte[] paquete)
        {
            var leido = Leer<ComandoFirmable>(paquete, "comando");
            return leido is { } p && ComandoValido(p.Credencial, p.Cuerpo.Comando)
                && p.Cuerpo.EmitidoUtc.Kind == DateTimeKind.Utc && p.Cuerpo.EmitidoUtc <= Ahora + DesfaseMaximo
                && p.Cuerpo.Comando.VenceUtc > p.Cuerpo.EmitidoUtc
                && p.Cuerpo.Comando.VenceUtc - p.Cuerpo.EmitidoUtc <= VigenciaMaxima + DesfaseMaximo
                ? Resultado<ComandoApertura>.Exito(p.Cuerpo.Comando)
                : Resultado<ComandoApertura>.Rechazo(MotivoRechazo.SinPermiso, "Comando no autenticado, no vigente o de otro dispositivo.");
        }
        public Resultado<EventoRecibido> ValidarEvento(byte[] paquete)
        {
            var leido = Leer<EventoFirmable>(paquete, "evento");
            return leido is { } p && EventoValido(p.Credencial, p.Cuerpo)
                ? Resultado<EventoRecibido>.Exito(p.Cuerpo.Evento)
                : Resultado<EventoRecibido>.Rechazo(MotivoRechazo.SinPermiso, "Evento no autenticado, no vigente o de otro dispositivo.");
        }

        public byte[] FirmarConsultaVigencia(CredencialDispositivo credencial, ComandoApertura comando)
        {
            DateTime ahora = Ahora;
            var consulta = new ConsultaVigenciaFirmable(comando, Guid.NewGuid().ToString("N"), ahora,
                comando.VenceUtc < ahora + VigenciaRecepcion ? comando.VenceUtc : ahora + VigenciaRecepcion);
            if (!ConsultaValida(credencial, consulta)) throw new ArgumentException("Consulta no vigente.");
            return CrearSobre(credencial, "consulta-vigencia", consulta);
        }

        public Resultado<ComandoApertura> ValidarConsultaVigencia(byte[] paquete)
        {
            var leido = Leer<ConsultaVigenciaFirmable>(paquete, "consulta-vigencia");
            return leido is { } p && ConsultaValida(p.Credencial, p.Cuerpo)
                ? Resultado<ComandoApertura>.Exito(p.Cuerpo.Comando)
                : Resultado<ComandoApertura>.Rechazo(MotivoRechazo.SinPermiso, "Consulta no autenticada o no vigente.");
        }

        private bool ConsultaValida(CredencialDispositivo c, ConsultaVigenciaFirmable p) =>
            ComandoValido(c, p.Comando) && p.IdConsulta is { Length: 32 } && Guid.TryParseExact(p.IdConsulta, "N", out _)
            && p.EmitidoUtc.Kind == DateTimeKind.Utc && p.VenceUtc.Kind == DateTimeKind.Utc
            && p.EmitidoUtc <= Ahora + DesfaseMaximo && p.VenceUtc > Ahora && p.VenceUtc > p.EmitidoUtc
            && p.VenceUtc <= p.Comando.VenceUtc && p.VenceUtc - p.EmitidoUtc <= VigenciaRecepcion;

        // Recibo de comando: transporte; de evento: persistencia; de consulta: decisión de vigencia.
        // Ninguno modifica órdenes por sí mismo ni demuestra apertura física.
        public byte[] FirmarRecepcion(CredencialDispositivo credencial, byte[] paqueteOriginal, bool aceptado, EventoRegistrado? registro = null)
        {
            Compat.ThrowIfNull(paqueteOriginal, nameof(paqueteOriginal));
            if (paqueteOriginal.Length is 0 or > MaximoPaquete) throw new ArgumentException("Mensaje original inválido.");
            var original = JsonSerializer.Deserialize<SobreDispositivo>(paqueteOriginal, Json);
            bool valido = original?.Tipo switch
            {
                "comando" => ValidarComando(paqueteOriginal).Exitoso,
                "evento" => ValidarEvento(paqueteOriginal).Exitoso,
                "consulta-vigencia" => ValidarConsultaVigencia(paqueteOriginal).Exitoso,
                _ => false
            };
            if (!valido || !credencial.Habilitada || original!.Dispositivo != credencial.Dispositivo
                || original.VersionClave != credencial.VersionClave
                || (registro is not null && (original.Tipo != "evento" || !aceptado || registro.IdEvento <= 0 || !Compat.IsDefined(registro.Resultado))))
                throw new ArgumentException("Mensaje original no autenticado para ese dispositivo.");
            DateTime ahora = Ahora;
            return CrearSobre(credencial, "recepcion", new RecepcionFirmable(original.Tipo,
                Compat.ToHexString(Compat.Sha256(paqueteOriginal)), aceptado, ahora, ahora + VigenciaRecepcion, registro));
        }

        public Resultado<RecepcionFirmable> ValidarRecepcion(byte[] paquete, byte[] paqueteEsperado)
        {
            var rechazo = Resultado<RecepcionFirmable>.Rechazo(MotivoRechazo.SinPermiso, "Recepción no autenticada, vencida o de otro mensaje.");
            if (paqueteEsperado is null || paqueteEsperado.Length is 0 or > MaximoPaquete) return rechazo;
            var leido = Leer<RecepcionFirmable>(paquete, "recepcion");
            if (leido is not { } p) return rechazo;
            try
            {
                var original = JsonSerializer.Deserialize<SobreDispositivo>(paqueteEsperado, Json);
                var r = p.Cuerpo;
                return original is { Version: 1, Tipo: "comando" or "evento" or "consulta-vigencia" }
                    && (original.Tipo != "consulta-vigencia" || ValidarConsultaVigencia(paqueteEsperado).Exitoso)
                    && p.Credencial.Dispositivo == original.Dispositivo && p.Credencial.VersionClave == original.VersionClave
                    && r.TipoOriginal == original.Tipo && r.Sha256Paquete == Compat.ToHexString(Compat.Sha256(paqueteEsperado))
                    && r.EmitidoUtc.Kind == DateTimeKind.Utc && r.VenceUtc.Kind == DateTimeKind.Utc
                    && r.EmitidoUtc <= Ahora + DesfaseMaximo && r.VenceUtc > Ahora && r.VenceUtc > r.EmitidoUtc
                    && r.VenceUtc - r.EmitidoUtc <= VigenciaRecepcion
                    && (r.Registro is null || (r.TipoOriginal == "evento" && r.Aceptado && r.Registro.IdEvento > 0 && Compat.IsDefined(r.Registro.Resultado)))
                    ? Resultado<RecepcionFirmable>.Exito(r) : rechazo;
            }
            catch (JsonException) { return rechazo; }
        }
        private bool ComandoValido(CredencialDispositivo c, ComandoApertura? p)
        {
            DateTime ahora = Ahora;
            return c.Habilitada && p is not null && p.IdContenedor == c.IdContenedor && p.IdOrden > 0 && !Compat.IsNullOrWhiteSpace(p.ClaveIdempotencia)
                && p.ClaveIdempotencia.Length <= 80 && p.VenceUtc.Kind == DateTimeKind.Utc && p.VenceUtc > ahora
                && p.VenceUtc - ahora <= VigenciaMaxima + DesfaseMaximo;
        }
        private bool EventoValido(CredencialDispositivo c, EventoFirmable p)
        {
            DateTime ahora = Ahora;
            return c.Habilitada && p.Evento is { } e && e.IdContenedor == c.IdContenedor && e.IdOrden > 0
                && !Compat.IsNullOrWhiteSpace(e.IdExterno) && e.IdExterno.Length <= 64 && Compat.IsDefined(e.Tipo)
                && p.EmitidoUtc.Kind == DateTimeKind.Utc && p.VenceUtc.Kind == DateTimeKind.Utc
                && p.EmitidoUtc <= ahora + DesfaseMaximo && p.VenceUtc > p.EmitidoUtc && p.VenceUtc > ahora
                && p.VenceUtc - p.EmitidoUtc <= VigenciaMaxima
                && e.FechaDispositivoUtc is { Kind: DateTimeKind.Utc } fecha
                && fecha <= p.EmitidoUtc + DesfaseMaximo && fecha >= p.EmitidoUtc - VigenciaMaxima;
        }

        internal static byte[] CrearSobre<T>(CredencialDispositivo c, string tipo, T cuerpo)
        {
            string datos = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cuerpo, Json));
            byte[] firma = c.Firmar(MaterialFirmado(c.Dispositivo, c.VersionClave, tipo, datos));
            byte[] paquete = JsonSerializer.SerializeToUtf8Bytes(new SobreDispositivo(1, c.Dispositivo, c.VersionClave, tipo, datos, Convert.ToBase64String(firma)), Json);
            if (paquete.Length > MaximoPaquete) throw new ArgumentException("Paquete demasiado grande.");
            return paquete;
        }
        internal (CredencialDispositivo Credencial, T Cuerpo)? Leer<T>(byte[] paquete, string tipo)
        {
            if (paquete is null || paquete.Length == 0 || paquete.Length > MaximoPaquete) return null;
            try
            {
                using var doc = JsonDocument.Parse(paquete, new JsonDocumentOptions { MaxDepth = 12 });
                if (!SinDuplicados(doc.RootElement)) return null;
                var sobre = doc.RootElement.Deserialize<SobreDispositivo>(Json);
                if (sobre is null || sobre.Version != 1 || sobre.Tipo != tipo || Compat.IsNullOrWhiteSpace(sobre.Dispositivo)
                    || sobre.Dispositivo.Length > 40 || sobre.VersionClave <= 0 || Compat.IsNullOrEmpty(sobre.Datos) || Compat.IsNullOrEmpty(sobre.Firma)) return null;
                var credencial = _resolver(sobre.Dispositivo, sobre.VersionClave);
                if (credencial is not { Habilitada: true } || credencial.Dispositivo != sobre.Dispositivo || credencial.VersionClave != sobre.VersionClave) return null;
                byte[] firma = Convert.FromBase64String(sobre.Firma);
                if (firma.Length != 32 || !Compat.FixedTimeEquals(firma,
                    credencial.Firmar(MaterialFirmado(sobre.Dispositivo, sobre.VersionClave, sobre.Tipo, sobre.Datos)))) return null;
                byte[] datos = Convert.FromBase64String(sobre.Datos);
                using var cuerpo = JsonDocument.Parse(datos, new JsonDocumentOptions { MaxDepth = 12 });
                if (!SinDuplicados(cuerpo.RootElement)) return null;
                T? valor = cuerpo.RootElement.Deserialize<T>(Json);
                return valor is null ? null : (credencial, valor);
            }
            catch (Exception) { return null; } // Nunca revelar claves, paquetes o excepciones de criptografía/JSON.
        }
        private static byte[] MaterialFirmado(string dispositivo, int versionClave, string tipo, string datos) =>
            Encoding.UTF8.GetBytes(FormattableString.Invariant($"ecoaccess-p3/v1\n{dispositivo}\n{versionClave}\n{tipo}\n{datos}"));
        private static bool SinDuplicados(JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                var nombres = new HashSet<string>(StringComparer.Ordinal);
                return e.EnumerateObject().All(p => nombres.Add(p.Name) && SinDuplicados(p.Value));
            }
            return e.ValueKind != JsonValueKind.Array || e.EnumerateArray().All(SinDuplicados);
        }
    }

    /// <summary>Frontera de eventos: únicamente remite a Interacciones un paquete autenticado.</summary>
    public sealed class ReceptorEventosDispositivo
    {
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>> _registrar;

        public ReceptorEventosDispositivo(ProtocoloDispositivoHmac protocolo, Func<EventoRecibido, CancellationToken, Task<Resultado<EventoRegistrado>>> registrar)
        {
            _protocolo = protocolo;
            _registrar = registrar;
        }

        public Task<Resultado<EventoRegistrado>> RecibirAsync(byte[] paquete, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            var evento = _protocolo.ValidarEvento(paquete);
            return evento.Exitoso ? _registrar(evento.Valor, ct)
                : Task.FromResult(Resultado<EventoRegistrado>.DesdeRechazo(evento));
        }
    }
}
