using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class PaginaRegistroTests
    {
        private const long Telegram = 555_000_222;
        private const string Formulario = "application/x-www-form-urlencoded";
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioVinculosEnMemoria _vinculos;
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioVinculacionTelegram _servicio;
        private readonly LimitadorManual _global = new();
        private readonly LimitadorManual _porToken = new();
        private readonly PaginaRegistro _pagina;

        public PaginaRegistroTests()
        {
            _usuarios.AgregarCiudadano(3);
            _vinculos = new RepositorioVinculosEnMemoria(_usuarios);
            _servicio = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
            _pagina = new PaginaRegistro(_servicio, _global, _porToken, _reloj);
        }

        private async Task<string> SolicitarTokenAsync()
        {
            var r = await _servicio.SolicitarAsync(Telegram, Telegram);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return r.Valor!.Token;
        }

        private static byte[] Cuerpo(string texto) => Encoding.UTF8.GetBytes(texto);

        private static string Campos(string token, string nombre = "Ana Pérez", string correo = "ana@ejemplo.com", bool acepta = true) =>
            $"t={Uri.EscapeDataString(token)}&nombre={Uri.EscapeDataString(nombre)}&correo={Uri.EscapeDataString(correo)}"
            + (acepta ? "&acepta=si" : string.Empty);

        [TestMethod]
        public async Task Mostrar_TokenVigente_DevuelveFormularioConTokenYConsentimiento()
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.MostrarAsync($"t={token}");

            Assert.AreEqual(200, r.Codigo);
            StringAssert.Contains(r.Html, $"name=\"t\" value=\"{token}\"");
            StringAssert.Contains(r.Html, "Ley 1581 de 2012");
            StringAssert.Contains(r.Html, "action=\"./\"", "El POST no repite el token en la URL.");
            StringAssert.Contains(r.Html, PaginaRegistro.VersionConsentimiento);
            Assert.DoesNotContain("password", r.Html, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("contraseña:", r.Html, StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("t=corto")]
        [DataRow("t=%3Cscript%3Ealert(1)%3C%2Fscript%3E")]
        [DataRow("t=token-de-prueba-0001&t=token-de-prueba-0002")]
        public async Task Mostrar_TokenAusenteMalFormadoORepetido_Devuelve400SinConsultar(string? consulta)
        {
            RespuestaPagina r = await _pagina.MostrarAsync(consulta);

            Assert.AreEqual(400, r.Codigo);
            StringAssert.Contains(r.Html, "/vincular");
            Assert.DoesNotContain("<script>", r.Html);
        }

        [TestMethod]
        public async Task Mostrar_TokenDesconocido_Devuelve404()
        {
            RespuestaPagina r = await _pagina.MostrarAsync("t=token-que-no-existe-0000");

            Assert.AreEqual(404, r.Codigo);
        }

        [TestMethod]
        public async Task Mostrar_TokenVencido_Devuelve410()
        {
            string token = await SolicitarTokenAsync();
            _reloj.Avanzar(TimeSpan.FromMinutes(11));

            RespuestaPagina r = await _pagina.MostrarAsync($"t={token}");

            Assert.AreEqual(410, r.Codigo);
        }

        [TestMethod]
        public async Task LimiteGlobal_Devuelve429AntesDeMirarElToken()
        {
            _global.Permitido = false;

            Assert.AreEqual(429, (await _pagina.MostrarAsync(null)).Codigo);
            Assert.AreEqual(429, (await _pagina.EnviarAsync(Formulario, Cuerpo("t=x"))).Codigo);
        }

        [TestMethod]
        public async Task LimitePorToken_Devuelve429()
        {
            string token = await SolicitarTokenAsync();
            _porToken.Permitido = false;

            Assert.AreEqual(429, (await _pagina.MostrarAsync($"t={token}")).Codigo);
            Assert.AreEqual(429, (await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token)))).Codigo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task LimitadorReal_FrenaReenviosDeUnMismoEnlace()
        {
            string token = await SolicitarTokenAsync();
            var pagina = new PaginaRegistro(
                _servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), new LimitadorUsoEnMemoria(2, TimeSpan.FromMinutes(1)), _reloj);

            Assert.AreEqual(200, (await pagina.MostrarAsync($"t={token}")).Codigo);
            Assert.AreEqual(200, (await pagina.MostrarAsync($"t={token}")).Codigo);
            Assert.AreEqual(429, (await pagina.MostrarAsync($"t={token}")).Codigo);
            Assert.AreEqual(404, (await pagina.MostrarAsync("t=otro-token-distinto-000")).Codigo, "Cada enlace lleva su cuenta.");
        }

        [TestMethod]
        public async Task Enviar_DatosValidos_CreaCuentaYVinculaLaCuentaDeTelegram()
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.EnviarAsync("application/x-www-form-urlencoded; charset=utf-8", Cuerpo(Campos(token)));

            Assert.AreEqual(200, r.Codigo);
            StringAssert.Contains(r.Html, "creada y vinculada");
            Assert.HasCount(1, _vinculos.Vinculos);
            var vinculo = _vinculos.Vinculos.Single();
            Assert.AreEqual(Telegram, vinculo.TelegramUserId);
            var usuario = _usuarios.Leer(vinculo.IdUsuario)!;
            Assert.AreEqual("Ana Pérez", usuario.Nombre);
            Assert.AreEqual(PaginaRegistro.VersionConsentimiento, usuario.VersionConsentimiento);
        }

        [TestMethod]
        public async Task Enviar_DosVecesElMismoEnlace_ElSegundoDevuelve410SinOtraCuenta()
        {
            string token = await SolicitarTokenAsync();

            Assert.AreEqual(200, (await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token)))).Codigo);
            RespuestaPagina segundo = await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token, correo: "otra@ejemplo.com")));

            Assert.AreEqual(410, segundo.Codigo);
            Assert.HasCount(1, _vinculos.Vinculos);
        }

        [TestMethod]
        public async Task Enviar_SinConsentimiento_VuelveAlFormularioConLosDatosYSinGastarElEnlace()
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token, acepta: false)));

            Assert.AreEqual(400, r.Codigo);
            StringAssert.Contains(r.Html, "role=\"alert\"");
            StringAssert.Contains(r.Html, "value=\"ana@ejemplo.com\"");
            Assert.IsEmpty(_vinculos.Vinculos);
            Assert.AreEqual(200, (await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token)))).Codigo, "El enlace sigue vigente.");
        }

        [TestMethod]
        public async Task Enviar_CorreoDeUnaCuentaExistente_Devuelve409SinPedirContrasena()
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token, correo: "usuario3@ejemplo.com")));

            Assert.AreEqual(409, r.Codigo);
            StringAssert.Contains(System.Net.WebUtility.HtmlDecode(r.Html), MensajesTelegram.CorreoExistente);
            Assert.DoesNotContain("type=\"password\"", r.Html);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task Enviar_TextoConHtml_SaleCodificado()
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.EnviarAsync(
                Formulario, Cuerpo(Campos(token, nombre: "\"><script>alert(1)</script>", correo: "no-es-correo", acepta: true)));

            Assert.AreEqual(400, r.Codigo);
            Assert.DoesNotContain("<script>", r.Html);
            StringAssert.Contains(r.Html, "&lt;script&gt;");
        }

        [TestMethod]
        public async Task Enviar_TipoDeContenidoDistinto_Devuelve415()
        {
            string token = await SolicitarTokenAsync();

            Assert.AreEqual(415, (await _pagina.EnviarAsync("application/json", Cuerpo(Campos(token)))).Codigo);
            Assert.AreEqual(415, (await _pagina.EnviarAsync(null, Cuerpo(Campos(token)))).Codigo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task Enviar_CuerpoDemasiadoGrande_Devuelve413()
        {
            RespuestaPagina r = await _pagina.EnviarAsync(Formulario, new byte[PaginaRegistro.TamanoMaximo + 1]);

            Assert.AreEqual(413, r.Codigo);
        }

        [TestMethod]
        [DataRow("nombre=Ana&correo=ana%40ejemplo.com&acepta=si")]
        [DataRow("t=TOKEN&correo=ana%40ejemplo.com&acepta=si")]
        [DataRow("t=TOKEN&nombre=Ana&nombre=Eva&correo=ana%40ejemplo.com&acepta=si")]
        [DataRow("t=TOKEN&nombre=Ana&correo=ana%40ejemplo.com&correo=eva%40ejemplo.com&acepta=si")]
        public async Task Enviar_CampoAusenteORepetido_Devuelve400SinGastarElEnlace(string campos)
        {
            string token = await SolicitarTokenAsync();

            RespuestaPagina r = await _pagina.EnviarAsync(Formulario, Cuerpo(campos.Replace("TOKEN", token, StringComparison.Ordinal)));

            Assert.AreEqual(400, r.Codigo);
            Assert.IsEmpty(_vinculos.Vinculos);
            Assert.AreEqual(200, (await _pagina.EnviarAsync(Formulario, Cuerpo(Campos(token)))).Codigo, "El enlace sigue vigente.");
        }

        [TestMethod]
        public void Encabezados_IncluyenCspMarcosYSinReferer()
        {
            StringAssert.Contains(PaginaRegistro.Encabezados["Content-Security-Policy"], "default-src 'none'");
            StringAssert.Contains(PaginaRegistro.Encabezados["Content-Security-Policy"], "frame-ancestors 'none'");
            Assert.AreEqual("DENY", PaginaRegistro.Encabezados["X-Frame-Options"]);
            Assert.AreEqual("no-referrer", PaginaRegistro.Encabezados["Referrer-Policy"]);
            Assert.AreEqual("no-store", PaginaRegistro.Encabezados["Cache-Control"]);
        }

        [TestMethod]
        public void Constructor_ValidaArgumentos()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new PaginaRegistro(null!, _global, _porToken, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new PaginaRegistro(_servicio, null!, _porToken, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new PaginaRegistro(_servicio, _global, null!, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new PaginaRegistro(_servicio, _global, _porToken, null!));
        }
    }
}
