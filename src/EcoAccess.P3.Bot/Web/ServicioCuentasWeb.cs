using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35.Expo;

namespace EcoAccess.P3.Bot.Web
{
    /// <summary>
    /// Vinculación de la web ciudadana con la cuenta de Telegram e historial oficial (Oracle) para la página.
    ///
    /// Flujo: la página pide un código (10 caracteres, un solo uso, 10 minutos) → abre t.me/EcoAccessP3_JG_bot?start=web_CODIGO
    /// → el bot confirma el código SOLO si ese chat ya tiene una cuenta EcoAccess vinculada → la página canjea el código por
    /// un token de cuenta (aleatorio de 256 bits, 90 días; aquí solo se guarda su SHA-256) → con ese token consulta su propio
    /// historial y sus aperturas quedan con su ID_USUARIO en OPERACION_ESTACION. Nunca se vincula por nombre.
    ///
    /// Escucha solo en 127.0.0.1 (la pasarela ciudadana es quien lo expone, con CORS y límites) y exige la clave de la
    /// estación (%ProgramData%\EcoAccessP3Framework\Panel\clave.txt) en el encabezado X-Clave.
    /// </summary>
    internal sealed class ServicioCuentasWeb
    {
        public const int Puerto = 8094;
        public const string PrefijoStart = "web_";
        private static readonly TimeSpan VigenciaCodigo = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan VigenciaToken = TimeSpan.FromDays(90);
        private const string Alfabeto = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";

        private sealed class Codigo { public DateTime ExpiraUtc; public int? IdUsuario; public bool Usado; }
        private sealed class Token { public int IdUsuario { get; set; } public DateTime ExpiraUtc { get; set; } }

        private readonly Dictionary<string, Codigo> _codigos = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Token> _tokens;
        private readonly string _archivoTokens;
        private readonly Func<int, int, CancellationToken, Task<IReadOnlyList<OperacionEstacion>>>? _historial;
        private readonly Func<DateTime> _ahora;

        public ServicioCuentasWeb(Func<int, int, CancellationToken, Task<IReadOnlyList<OperacionEstacion>>>? historial, string? carpeta = null, Func<DateTime>? ahora = null)
        {
            _historial = historial;
            _ahora = ahora ?? (() => DateTime.UtcNow);
            string baseDir = carpeta ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework", "Ciudadano");
            _archivoTokens = Path.Combine(baseDir, "cuentas-web.json");
            try { _tokens = File.Exists(_archivoTokens) ? JsonSerializer.Deserialize<Dictionary<string, Token>>(File.ReadAllText(_archivoTokens)) ?? new() : new(); }
            catch (Exception) { _tokens = new(); }
        }

        /// <summary>Código nuevo para mostrar en la página (enlace t.me/…?start=web_CODIGO).</summary>
        public string? NuevoCodigo()
        {
            lock (_codigos)
            {
                foreach (var k in _codigos.Where(x => x.Value.ExpiraUtc <= _ahora()).Select(x => x.Key).ToList()) _codigos.Remove(k);
                if (_codigos.Count >= 500) return null;
                string c = Aleatorio(10);
                _codigos[c] = new Codigo { ExpiraUtc = _ahora() + VigenciaCodigo };
                return c;
            }
        }

        /// <summary>Lo llama el bot con «/start web_CODIGO» desde un chat con cuenta vinculada.</summary>
        public bool Confirmar(string codigo, int idUsuario)
        {
            lock (_codigos)
            {
                if (!_codigos.TryGetValue(codigo, out var c) || c.ExpiraUtc <= _ahora() || c.Usado || c.IdUsuario is not null) return false;
                c.IdUsuario = idUsuario;
                return true;
            }
        }

        /// <summary>Canjea un código confirmado por un token de cuenta (un solo uso). Null si aún no se confirmó o ya se usó.</summary>
        public (string? Token, string Estado) Canjear(string codigo)
        {
            int id;
            lock (_codigos)
            {
                if (!_codigos.TryGetValue(codigo, out var c) || c.ExpiraUtc <= _ahora() || c.Usado) return (null, "vencido");
                if (c.IdUsuario is null) return (null, "pendiente");
                c.Usado = true; id = c.IdUsuario.Value;
            }
            string token = Aleatorio(43);
            lock (_tokens)
            {
                foreach (var k in _tokens.Where(x => x.Value.ExpiraUtc <= _ahora()).Select(x => x.Key).ToList()) _tokens.Remove(k);
                _tokens[Huella(token)] = new Token { IdUsuario = id, ExpiraUtc = _ahora() + VigenciaToken };
                Guardar();
            }
            return (token, "vinculado");
        }

        public int? Usuario(string? token)
        {
            if (token is null || token.Length < 30 || token.Length > 80) return null;
            lock (_tokens) return _tokens.TryGetValue(Huella(token), out var t) && t.ExpiraUtc > _ahora() ? t.IdUsuario : null;
        }

        public bool Desvincular(string? token)
        {
            if (token is null) return false;
            lock (_tokens) { bool ok = _tokens.Remove(Huella(token)); if (ok) Guardar(); return ok; }
        }

        /// <summary>Historial oficial del propio usuario (Oracle), en el mismo formato que Telegram.</summary>
        public async Task<object?> HistorialAsync(string? token, CancellationToken ct)
        {
            if (Usuario(token) is not int id || _historial is null) return null;
            var ops = await _historial(id, 30, ct).ConfigureAwait(false);
            return ops.Select(o => new
            {
                fechaUtc = DateTime.SpecifyKind(o.FechaUtc, DateTimeKind.Utc).ToString("o"),
                canal = o.Canal.ToString(),
                tipo = o.Tipo.ToString(),
                estado = o.Estado.ToString(),
                canecas = (o.Canecas ?? "").ToLowerInvariant().Replace(",", " + "),
                residuos = ServicioConversacionTelegram.LineasHistorial(o).ToArray(),
                duracionS = o.DuracionMs / 1000,
                declarado = o.DisposicionDeclarada,
            }).ToArray();
        }

        // ───── HTTP local (solo 127.0.0.1, con la clave de la estación) ─────
        public void Iniciar(string claveEstacion, Action<string> anotar)
        {
            var l = new HttpListener();
            l.Prefixes.Add($"http://127.0.0.1:{Puerto}/");
            try { l.Start(); }
            catch (HttpListenerException e) { anotar($"Cuentas web: no se pudo abrir 127.0.0.1:{Puerto} ({e.ErrorCode}); la vinculación web queda desactivada."); return; }
            anotar($"Cuentas web (vinculación con Telegram e historial de la página) en http://127.0.0.1:{Puerto}/ (solo local).");
            _ = Task.Run(async () =>
            {
                while (l.IsListening)
                {
                    HttpListenerContext ctx;
                    try { ctx = await l.GetContextAsync().ConfigureAwait(false); } catch (Exception) { break; }
                    _ = Task.Run(() => AtenderAsync(ctx, claveEstacion));
                }
            });
        }

        private async Task AtenderAsync(HttpListenerContext ctx, string claveEstacion)
        {
            var req = ctx.Request; var res = ctx.Response;
            try
            {
                if (!IPAddress.IsLoopback(req.RemoteEndPoint.Address) || req.HttpMethod != "POST" || !Iguales(req.Headers["X-Clave"], claveEstacion))
                { await ResponderAsync(res, 403, new { ok = false }); return; }
                if (req.ContentLength64 > 2048) { await ResponderAsync(res, 413, new { ok = false }); return; }
                Dictionary<string, JsonElement> cuerpo;
                using (var lector = new StreamReader(req.InputStream, Encoding.UTF8))
                {
                    string texto = await lector.ReadToEndAsync().ConfigureAwait(false);
                    try { cuerpo = texto.Length == 0 ? new() : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(texto) ?? new(); } catch (JsonException) { cuerpo = new(); }
                }
                string? Txt(string k) => cuerpo.TryGetValue(k, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
                switch (req.Url.AbsolutePath.TrimEnd('/'))
                {
                    case "/cuenta/codigo":
                        string? c = NuevoCodigo();
                        await ResponderAsync(res, c is null ? 429 : 200, c is null ? new { ok = false, error = "Demasiadas vinculaciones pendientes." }
                            : (object)new { ok = true, codigo = c, enlace = $"https://t.me/{EnlaceTelegram.NombreBotPiloto}?start={PrefijoStart}{c}", expiraS = (int)VigenciaCodigo.TotalSeconds });
                        return;
                    case "/cuenta/canjear":
                        var (token, estado) = Txt("codigo") is { Length: 10 } cod ? Canjear(cod) : (null, "vencido");
                        await ResponderAsync(res, 200, new { ok = token is not null, estado, token });
                        return;
                    case "/cuenta/usuario":
                        await ResponderAsync(res, 200, new { ok = true, idUsuario = Usuario(Txt("token")) });
                        return;
                    case "/cuenta/historial":
                        object? h = await HistorialAsync(Txt("token"), CancellationToken.None).ConfigureAwait(false);
                        await ResponderAsync(res, h is null ? 401 : 200, h is null ? new { ok = false, error = "La cuenta no está vinculada o la vinculación venció." } : (object)new { ok = true, operaciones = h });
                        return;
                    case "/cuenta/desvincular":
                        await ResponderAsync(res, 200, new { ok = Desvincular(Txt("token")) });
                        return;
                    default:
                        await ResponderAsync(res, 404, new { ok = false });
                        return;
                }
            }
            catch (Exception) { try { await ResponderAsync(res, 500, new { ok = false, error = "No se pudo consultar Oracle en este momento." }); } catch (Exception) { } }
        }

        private static async Task ResponderAsync(HttpListenerResponse res, int codigo, object cuerpo)
        {
            byte[] datos = JsonSerializer.SerializeToUtf8Bytes(cuerpo);
            res.StatusCode = codigo; res.ContentType = "application/json; charset=utf-8"; res.Headers["Cache-Control"] = "no-store";
            await res.OutputStream.WriteAsync(datos, 0, datos.Length).ConfigureAwait(false);
            res.Close();
        }

        private void Guardar()
        {
            try { Directory.CreateDirectory(Path.GetDirectoryName(_archivoTokens)!); File.WriteAllText(_archivoTokens, JsonSerializer.Serialize(_tokens)); }
            catch (Exception) { /* sin persistencia la vinculación dura hasta reiniciar el bot */ }
        }

        private static string Aleatorio(int n)
        {
            var bytes = new byte[n];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return new string(bytes.Select(b => Alfabeto[b % Alfabeto.Length]).ToArray());
        }

        private static string Huella(string s) { using var sha = SHA256.Create(); return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(s))); }

        private static bool Iguales(string? a, string b)
        {
            if (a is null || b.Length == 0 || a.Length != b.Length) return false;
            int d = 0; for (int i = 0; i < a.Length; i++) d |= a[i] ^ b[i];
            return d == 0;
        }
    }
}
