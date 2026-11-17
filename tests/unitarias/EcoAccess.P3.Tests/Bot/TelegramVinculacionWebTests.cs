using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.Text.RegularExpressions;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed partial class TelegramVinculacionWebTests
    {
        private const long Telegram = 778800;
        // Cuenta existente con contraseña de 25 caracteres: el límite de 20 solo aplica a cuentas nuevas.
        private const string Clave = "prueba-web-ciudadana-2026";
        private const string ClaveNueva = "verde-26";
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioCredencialesEnMemoria _credenciales;
        private readonly RepositorioVinculosEnMemoria _vinculos;
        private readonly HasherFalso _hasher = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 29, 12, 0, 0, TimeSpan.Zero));
        private readonly ServicioVinculacionTelegram _servicio;
        private readonly PaginaVinculacion _pagina;

        private sealed class Registro : IRepositorioRegistroCiudadanos
        {
            private readonly RepositorioUsuariosEnMemoria _usuarios;
            private readonly RepositorioCredencialesEnMemoria _credenciales;

            public Registro(RepositorioUsuariosEnMemoria usuarios, RepositorioCredencialesEnMemoria credenciales)
            {
                _usuarios = usuarios;
                _credenciales = credenciales;
            }

            public async Task<int> RegistrarAsync(AltaCiudadano alta, CancellationToken ct = default)
            {
                if (await _usuarios.ExisteCorreoAsync(alta.CorreoElectronico, ct)) throw new ConflictoUnicidadException("correo");
                var usuario = new Usuario(alta.Nombre, alta.CorreoElectronico, CodigoRol.Ciudadano, alta.FechaUtc);
                usuario.RegistrarConsentimiento(alta.VersionConsentimiento, alta.FechaUtc);
                int id = await _usuarios.AgregarAsync(usuario, ct);
                _credenciales.Guardar(new CredencialAcceso(id, alta.Credencial.Hash, alta.Credencial.Sal, alta.Credencial.Iteraciones, alta.FechaUtc));
                return id;
            }
        }

        public TelegramVinculacionWebTests()
        {
            _usuarios.AgregarCiudadano(3);
            _credenciales = new(_usuarios);
            var hash = _hasher.Calcular(Clave);
            _credenciales.Guardar(new(3, hash.Hash, hash.Sal, hash.Iteraciones, _reloj.Ahora.UtcDateTime));
            _vinculos = new(_usuarios);
            _servicio = new(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
            _pagina = new(_servicio, new ServicioAutenticacion(_credenciales, _usuarios, _hasher, _reloj),
                new ServicioRegistroCiudadanos(new Registro(_usuarios, _credenciales), _hasher, _reloj),
                new LimitadorManual(), new LimitadorManual(), _reloj);
        }

        private async Task<(string Token, string Cookie, string Csrf)> FormularioAsync()
        {
            string token = (await _servicio.SolicitarAsync(Telegram, Telegram)).Valor!.Token;
            var pagina = await _pagina.MostrarAsync("t=" + token);
            Assert.AreEqual(200, pagina.Codigo);
            string cookie = pagina.Cookie!.Split(';')[0].Split('=')[1];
            string csrf = Regex.Match(pagina.Html, "name=\"csrf\" value=\"([a-f0-9]{64})\"").Groups[1].Value;
            return (token, cookie, csrf);
        }

        private static byte[] Campos(string token, string csrf, string extra = "") => Encoding.UTF8.GetBytes(
            "t=" + token + "&csrf=" + csrf + "&modo=entrar&correo=usuario3%40ejemplo.com&clave=" + Clave + extra);
        private Task<RespuestaPagina> EnviarAsync(byte[] cuerpo, string? cookie) => _pagina.EnviarAsync("application/x-www-form-urlencoded", cuerpo, cookie);

        [TestMethod]
        public async Task Formulario_OfreceAmbasRutasConCookieSeguraSinIdDeUsuario()
        {
            var (token, _, _) = await FormularioAsync();
            var pagina = await _pagina.MostrarAsync("t=" + token);
            StringAssert.Contains(pagina.Html, "Ya tengo cuenta");
            StringAssert.Contains(pagina.Html, "Crea tu cuenta EcoAccess");
            StringAssert.Contains(pagina.Cookie!, "Secure; HttpOnly; SameSite=Strict");
            Assert.DoesNotContain("name=\"idUsuario\"", pagina.Html);
            Assert.DoesNotContain(Clave, pagina.Html);
        }

        [TestMethod]
        public async Task CuentaExistente_ObtieneCodigoPeroSoloTelegramCompletaVinculo()
        {
            var (token, cookie, csrf) = await FormularioAsync();
            var pagina = await EnviarAsync(Campos(token, csrf), cookie);
            Assert.AreEqual(200, pagina.Codigo);
            string codigo = Regex.Match(pagina.Html, @"/confirmar ([A-Z2-9]{8})").Groups[1].Value;
            Assert.AreEqual(8, codigo.Length);
            Assert.IsEmpty(_vinculos.Vinculos);
            Assert.IsFalse((await _servicio.ConfirmarVinculacionAsync(Telegram + 1, Telegram + 1, codigo)).Exitoso);
            Assert.IsTrue((await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo)).Exitoso);
            Assert.HasCount(1, _vinculos.Vinculos);
            Assert.HasCount(1, _usuarios.Leer(3)!.Roles);
            Assert.DoesNotContain(Clave, pagina.Html);
        }

        [TestMethod]
        public async Task CuentaNueva_GuardaCredencialYCiudadanoSinVincularHastaSegundoCanal()
        {
            var (token, cookie, csrf) = await FormularioAsync();
            byte[] cuerpo = Encoding.UTF8.GetBytes($"t={token}&csrf={csrf}&modo=crear&nombre=Ciudadana+Ficticia&correo=nueva%40ejemplo.com&clave={ClaveNueva}&confirmacion={ClaveNueva}&acepta=si&rol=Administrador");
            var pagina = await EnviarAsync(cuerpo, cookie);
            string codigo = Regex.Match(pagina.Html, @"/confirmar ([A-Z2-9]{8})").Groups[1].Value;
            Assert.AreEqual(8, codigo.Length);
            Assert.IsEmpty(_vinculos.Vinculos);
            var usuario = (await _usuarios.ListarAsync()).Single(u => u.CorreoElectronico == "nueva@ejemplo.com");
            CollectionAssert.AreEqual(new[] { CodigoRol.Ciudadano }, usuario.Roles.ToArray());
            Assert.IsNotNull(_credenciales.Leer(usuario.Id));
            Assert.IsTrue((await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo)).Exitoso);
            Assert.HasCount(1, _vinculos.Vinculos);
        }

        [TestMethod]
        [DataRow("cookie")]
        [DataRow("csrf")]
        [DataRow("duplicado")]
        [DataRow("vencido")]
        [DataRow("otro-token")]
        public async Task Csrf_FaltaCruceRepeticionOVencimiento_NoPreparanVinculo(string caso)
        {
            var (token, cookie, csrf) = await FormularioAsync();
            if (caso == "cookie") cookie = new string('a', 64);
            if (caso == "csrf") csrf = new string('b', 64);
            if (caso == "vencido") _reloj.Avanzar(TimeSpan.FromMinutes(10));
            if (caso == "otro-token") token = (await _servicio.SolicitarAsync(Telegram + 1, Telegram + 1)).Valor!.Token;
            var pagina = await EnviarAsync(Campos(token, csrf, caso == "duplicado" ? "&csrf=" + csrf : ""), cookie);
            Assert.AreEqual(403, pagina.Codigo);
            Assert.IsNull(await _vinculos.ObtenerVinculacionPendienteAsync(Telegram, _reloj.Ahora.UtcDateTime));
        }

        [TestMethod]
        public async Task PostConcurrenteYReenvio_ConsumenUnFormularioUnaSolaVez()
        {
            var (token, cookie, csrf) = await FormularioAsync();
            var respuestas = await Task.WhenAll(EnviarAsync(Campos(token, csrf), cookie), EnviarAsync(Campos(token, csrf), cookie));
            Assert.AreEqual(1, respuestas.Count(r => r.Codigo == 200));
            Assert.AreEqual(1, respuestas.Count(r => r.Codigo == 403));
            Assert.AreEqual(403, (await EnviarAsync(Campos(token, csrf), cookie)).Codigo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task ServidorReal_TransportaCookieYCsrfSinRegistrarCredenciales()
        {
            int puerto = PuertosHttpPruebas.Siguiente();
            var uri = new Uri($"http://127.0.0.1:{puerto}/registro/");
            var logs = new List<string>();
            var servidor = new ServidorRegistro(_pagina, uri.AbsoluteUri, logs.Add);
            using var cancelar = new CancellationTokenSource();
            Task tarea = servidor.EjecutarAsync(cancelar.Token);
            try
            {
                using var http = new HttpClient(new HttpClientHandler { UseCookies = false }) { Timeout = TimeSpan.FromSeconds(10) };
                string token = (await _servicio.SolicitarAsync(Telegram, Telegram)).Valor!.Token;
                using var get = await http.GetAsync(new Uri(uri, "?t=" + token));
                string html = await get.Content.ReadAsStringAsync();
                string cookie = get.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
                string csrf = Regex.Match(html, "name=\"csrf\" value=\"([a-f0-9]{64})\"").Groups[1].Value;
                using var post = new HttpRequestMessage(HttpMethod.Post, uri);
                post.Headers.TryAddWithoutValidation("Cookie", cookie); // HTTPS termina en proxy; esta prueba local transporta la cookie explícitamente.
                post.Content = new ByteArrayContent(Campos(token, csrf));
                post.Content.Headers.ContentType = new("application/x-www-form-urlencoded");
                using var respuesta = await http.SendAsync(post);
                Assert.AreEqual(System.Net.HttpStatusCode.OK, respuesta.StatusCode);
                StringAssert.Contains(await respuesta.Content.ReadAsStringAsync(), "/confirmar ");
                Assert.AreEqual("no-store", respuesta.Headers.CacheControl?.ToString());
                Assert.DoesNotContain(Clave, string.Join(" ", logs));
                Assert.DoesNotContain(token, string.Join(" ", logs));
                Assert.IsEmpty(_vinculos.Vinculos);
            }
            finally { cancelar.Cancel(); await tarea.WaitAsync(TimeSpan.FromSeconds(10)); }
        }

        [TestMethod]
        public async Task CredencialIncorrecta_RotaFormularioSinFiltrarNiPrepararVinculo()
        {
            var (token, cookie, csrf) = await FormularioAsync();
            var pagina = await EnviarAsync(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Campos(token, csrf)).Replace(Clave, "contraseña-incorrecta")), cookie);
            StringAssert.Contains(System.Net.WebUtility.HtmlDecode(pagina.Html), ServicioAutenticacion.MensajeFallo);
            Assert.DoesNotContain("contraseña-incorrecta", System.Net.WebUtility.HtmlDecode(pagina.Html));
            Assert.DoesNotContain(Clave, pagina.Html);
            Assert.IsNotNull(pagina.Cookie);
            Assert.AreEqual(403, (await EnviarAsync(Campos(token, csrf), cookie)).Codigo);
            Assert.IsNull(await _vinculos.ObtenerVinculacionPendienteAsync(Telegram, _reloj.Ahora.UtcDateTime));
        }

        [TestMethod]
        [DataRow("corta", true)]
        [DataRow(Clave, false)]
        public async Task AltaInvalida_NoCreaCuentaNiVinculo(string clave, bool acepta)
        {
            var (token, cookie, csrf) = await FormularioAsync();
            string campos = $"t={token}&csrf={csrf}&modo=crear&nombre=Prueba&correo=invalida%40ejemplo.com&clave={clave}&confirmacion={clave}" + (acepta ? "&acepta=si" : "");
            var pagina = await EnviarAsync(Encoding.UTF8.GetBytes(campos), cookie);
            Assert.DoesNotContain("/confirmar ", pagina.Html);
            Assert.IsFalse(await _usuarios.ExisteCorreoAsync("invalida@ejemplo.com"));
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        [DataRow("http://0.0.0.0:8081/registro/")]
        [DataRow("http://192.0.2.1:8081/registro/")]
        [DataRow("http://127.0.0.1:8081/registro/?x=1")]
        public void ServidorNoExponeRegistroDirectamenteEnLaRed(string prefijo)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ServidorRegistro(_pagina, prefijo, _ => { }));
        }
    }
}
