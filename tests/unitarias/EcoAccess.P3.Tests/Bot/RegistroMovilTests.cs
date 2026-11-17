using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Formulario móvil de registro (fotos del iPhone del propietario, 11/11/2026): una columna, etiquetas encima, todos los
    /// campos del mismo ancho, errores junto a su campo y política ciudadana de 6 a 20 caracteres. Las protecciones
    /// (CSRF de un solo uso, cookie __Host segura, token del bot, validación en el servidor) no cambian.
    /// </summary>
    public sealed partial class TelegramVinculacionWebTests
    {
        private async Task<RespuestaPagina> CrearAsync(string clave, string? confirmacion = null, bool acepta = true, string nombre = "Ciudadana+Movil",
            string correo = "movil%40ejemplo.com")
        {
            var (token, cookie, csrf) = await FormularioAsync();
            string campos = $"t={token}&csrf={csrf}&modo=crear&nombre={nombre}&correo={correo}&clave={Uri.EscapeDataString(clave)}" +
                            $"&confirmacion={Uri.EscapeDataString(confirmacion ?? clave)}" + (acepta ? "&acepta=si" : "");
            return await EnviarAsync(Encoding.UTF8.GetBytes(campos), cookie);
        }

        private static string Texto(RespuestaPagina pagina) => WebUtility.HtmlDecode(pagina.Html);

        [TestMethod]
        public async Task Plantilla_UnaColumnaCamposDelMismoAnchoYLetraQueNoAmpliaEnIphone()
        {
            var (token, _, _) = await FormularioAsync();
            string html = (await _pagina.MostrarAsync("t=" + token)).Html;

            StringAssert.Contains(html, "box-sizing:border-box");
            StringAssert.Contains(html, ".campo input{display:block;width:100%");
            StringAssert.Contains(html, "font-size:16px");
            StringAssert.Contains(html, "name=\"viewport\" content=\"width=device-width, initial-scale=1\"");
            // Cada campo tiene su etiqueta encima, unida por for/id; las dos contraseñas usan la misma clase y el mismo ancho.
            foreach (string id in new[] { "nombre", "correo", "clave-nueva", "confirmacion" })
                StringAssert.Contains(html, $"<label for=\"{id}\">");
            Assert.AreEqual(2, Regex.Matches(html, "type=\"password\"").Count);
            Assert.AreEqual(2, Regex.Matches(html, "data-mostrar=").Count, "Mostrar u ocultar en las dos contraseñas.");
            StringAssert.Contains(html, $"minlength=\"{PoliticaContrasenaCiudadano.LongitudMinima}\"");
            StringAssert.Contains(html, $"maxlength=\"{PoliticaContrasenaCiudadano.LongitudMaxima}\"");
            StringAssert.Contains(html, "<details><summary>Tratamiento de datos personales</summary>");
            StringAssert.Contains(html, "Ya tengo cuenta");
            await GuardarVistaPreviaAsync("registro_crear.html", html);
            await GuardarVistaPreviaAsync("registro_error.html", (await CrearAsync("corta")).Html);
            await GuardarVistaPreviaAsync("registro_entrar.html", (await _pagina.MostrarAsync("t=" + (await FormularioAsync()).Token + "&m=entrar")).Html);
        }

        /// <summary>Solo si se define ECOACCESS_VISTA_PREVIA_REGISTRO: guarda el HTML para revisarlo en un navegador móvil.</summary>
        private static Task GuardarVistaPreviaAsync(string archivo, string html)
        {
            string? carpeta = Environment.GetEnvironmentVariable("ECOACCESS_VISTA_PREVIA_REGISTRO");
            if (!string.IsNullOrWhiteSpace(carpeta))
            {
                System.IO.Directory.CreateDirectory(carpeta);
                System.IO.File.WriteAllText(System.IO.Path.Combine(carpeta, archivo), html, new UTF8Encoding(false));
            }
            return Task.CompletedTask;
        }

        [TestMethod]
        public void Csp_SoloPermiteElScriptPropioPorSuHash()
        {
            string esperado = "sha256-" + Convert.ToBase64String(SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(PaginaRegistro.ScriptFormulario)));
            string csp = PaginaRegistro.Encabezados["Content-Security-Policy"];
            StringAssert.Contains(csp, $"script-src '{esperado}'");
            Assert.DoesNotContain("'unsafe-inline'", csp.Split(';').Single(p => p.Trim().StartsWith("script-src")));
            StringAssert.Contains(PaginaRegistro.Documento("x", conScript: true), "<script>" + PaginaRegistro.ScriptFormulario + "</script>");
        }

        [TestMethod]
        [DataRow("verde2", DisplayName = "seis caracteres")]
        [DataRow("veinte-caracteres-26", DisplayName = "veinte caracteres")]
        [DataRow("solotexto", DisplayName = "sin símbolos")]
        [DataRow("eco#verde!", DisplayName = "con símbolos")]
        public async Task ContrasenaDentroDeLaPolitica_CreaLaCuenta(string clave)
        {
            var pagina = await CrearAsync(clave);

            Assert.AreEqual(200, pagina.Codigo);
            StringAssert.Contains(Texto(pagina), "¡Tu cuenta quedó creada!");
            Assert.IsTrue(await _usuarios.ExisteCorreoAsync("movil@ejemplo.com"));
            Assert.DoesNotContain(clave, pagina.Html);
        }

        [TestMethod]
        [DataRow("corta", null, true, "clave", "al menos 6", DisplayName = "cinco caracteres")]
        [DataRow("veintiun-caracteres-x", null, true, "clave", "como máximo 20", DisplayName = "veintiún caracteres")]
        [DataRow("verde-26", "verde-27", true, "confirmacion", "no coinciden", DisplayName = "confirmación distinta")]
        [DataRow("verde-26", null, false, "acepta", "aceptar el tratamiento", DisplayName = "sin consentimiento")]
        [DataRow("123456", null, true, "clave", "común", DisplayName = "muy común")]
        public async Task ErrorSeMuestraJuntoASuCampoYConservaNombreYCorreo(string clave, string? confirmacion, bool acepta, string campo, string texto)
        {
            var pagina = await CrearAsync(clave, confirmacion, acepta);
            string html = Texto(pagina);

            StringAssert.Contains(html, $"id=\"error-{campo}\"");
            StringAssert.Contains(html, texto);
            StringAssert.Contains(html, "value=\"Ciudadana Movil\"");
            StringAssert.Contains(html, "value=\"movil@ejemplo.com\"");
            Assert.DoesNotContain("value=\"" + clave, html, "La contraseña nunca vuelve a la página.");
            Assert.IsFalse(await _usuarios.ExisteCorreoAsync("movil@ejemplo.com"));
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task CorreoExistente_NoCreaOtraCuentaNiRevelaDatos()
        {
            Assert.AreEqual(200, (await CrearAsync("verde-26")).Codigo);
            int cuentas = (await _usuarios.ListarAsync()).Count;

            var segunda = await CrearAsync("otra-clave", nombre: "Otra+Persona");

            StringAssert.Contains(Texto(segunda), "inicia sesión");
            Assert.AreEqual(cuentas, (await _usuarios.ListarAsync()).Count);
        }

        [TestMethod]
        public async Task CuentaAntiguaConContrasenaDeMasDeVeinte_SigueIniciandoSesion()
        {
            Assert.IsGreaterThan(PoliticaContrasenaCiudadano.LongitudMaxima, Clave.Length);
            var (token, cookie, csrf) = await FormularioAsync();

            var pagina = await EnviarAsync(Campos(token, csrf), cookie);

            Assert.AreEqual(200, pagina.Codigo);
            StringAssert.Contains(Texto(pagina), "Sesión iniciada");
        }

        [TestMethod]
        public async Task UsuarioBloqueado_NoIniciaSesionAunqueLaClaveSeaCorrecta()
        {
            for (int i = 0; i < ServicioAutenticacion.MaximoIntentos; i++)
            {
                var (t, c, s) = await FormularioAsync();
                await EnviarAsync(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(Campos(t, s)).Replace(Clave, "incorrecta-" + i)), c);
            }
            var (token, cookie, csrf) = await FormularioAsync();

            var pagina = await EnviarAsync(Campos(token, csrf), cookie);

            Assert.DoesNotContain("/confirmar ", pagina.Html);
            Assert.IsNull(await _vinculos.ObtenerVinculacionPendienteAsync(Telegram, _reloj.Ahora.UtcDateTime));
        }

        [TestMethod]
        public async Task EnvioDuplicadoDelMismoFormulario_SoloCreaUnaCuenta()
        {
            var (token, cookie, csrf) = await FormularioAsync();
            byte[] cuerpo = Encoding.UTF8.GetBytes($"t={token}&csrf={csrf}&modo=crear&nombre=Doble&correo=doble%40ejemplo.com&clave=verde-26&confirmacion=verde-26&acepta=si");

            var primera = await EnviarAsync(cuerpo, cookie);
            var segunda = await EnviarAsync(cuerpo, cookie);

            Assert.AreEqual(200, primera.Codigo);
            Assert.AreEqual(403, segunda.Codigo);
            Assert.AreEqual(1, (await _usuarios.ListarAsync()).Count(u => u.CorreoElectronico == "doble@ejemplo.com"));
        }

        [TestMethod]
        public async Task EntrarMuestraSoloElInicioDeSesionConEnlaceParaCrear()
        {
            var (token, _, _) = await FormularioAsync();
            string html = Texto(await _pagina.MostrarAsync("t=" + token + "&m=entrar"));

            StringAssert.Contains(html, "<h1>Inicia sesión</h1>");
            StringAssert.Contains(html, "autocomplete=\"current-password\"");
            StringAssert.Contains(html, $"maxlength=\"{CredencialAcceso.LongitudMaximaContrasena}\"");
            StringAssert.Contains(html, "¿No tienes cuenta? Crea una");
            Assert.DoesNotContain("name=\"confirmacion\"", html);
        }
    }
}
