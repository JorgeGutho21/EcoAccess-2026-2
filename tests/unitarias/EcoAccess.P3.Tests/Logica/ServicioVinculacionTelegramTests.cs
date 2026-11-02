using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioVinculacionTelegramTests
    {
        private const long Telegram = 555_000_111;
        private const int Existente = 3;
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioVinculosEnMemoria _vinculos;
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioVinculacionTelegram _servicio;

        public ServicioVinculacionTelegramTests()
        {
            _usuarios.AgregarCiudadano(Existente);
            _vinculos = new RepositorioVinculosEnMemoria(_usuarios);
            _servicio = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
        }

        private async Task<string> SolicitarTokenAsync()
        {
            var r = await _servicio.SolicitarAsync(Telegram, Telegram);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return r.Valor!.Token;
        }

        [TestMethod]
        public async Task SolicitarAsync_ChatPrivado_GuardaSoloElHashConVigenciaDeDiezMinutos()
        {
            var r = await _servicio.SolicitarAsync(Telegram, Telegram);

            Assert.IsTrue(r.Exitoso);
            Assert.AreEqual("token-de-prueba-0001", r.Valor!.Token);
            Assert.AreEqual(Inicio.UtcDateTime + TokenVinculacion.Vigencia, r.Valor.VenceUtc);
            var guardado = _vinculos.Tokens.Single();
            Assert.AreEqual(ServicioVinculacionTelegram.CalcularHash(r.Valor.Token), guardado.HashToken);
            Assert.AreNotEqual(r.Valor.Token, guardado.HashToken);
        }

        [TestMethod]
        public async Task SolicitarAsync_ChatDistintoDeLaCuenta_RechazaSinEmitirToken()
        {
            var r = await _servicio.SolicitarAsync(Telegram, -100_200_300);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesTelegram.SoloChatPrivado, r.Mensaje);
            Assert.IsEmpty(_vinculos.Tokens);
        }

        [TestMethod]
        public async Task SolicitarAsync_CuentaYaVinculada_RechazaDuplicado()
        {
            _vinculos.Sembrar(Existente, Telegram);

            var r = await _servicio.SolicitarAsync(Telegram, Telegram);

            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
            Assert.AreEqual(MensajesTelegram.YaVinculada, r.Mensaje);
        }

        [TestMethod]
        public async Task ConsultarTokenAsync_TokenVigente_Acepta()
        {
            string token = await SolicitarTokenAsync();

            Assert.IsTrue((await _servicio.ConsultarTokenAsync(token)).Exitoso);
        }

        [TestMethod]
        public async Task ConsultarTokenAsync_TokenVencido_RechazaEnlaceVencido()
        {
            string token = await SolicitarTokenAsync();
            _reloj.Avanzar(TokenVinculacion.Vigencia);

            var r = await _servicio.ConsultarTokenAsync(token);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesTelegram.EnlaceVencido, r.Mensaje);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("  ")]
        [DataRow("token-que-no-existe")]
        public async Task ConsultarTokenAsync_TokenAusenteOInexistente_RechazaEnlaceInvalido(string? token)
        {
            var r = await _servicio.ConsultarTokenAsync(token);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo);
            Assert.AreEqual(MensajesTelegram.EnlaceInvalido, r.Mensaje);
        }

        [TestMethod]
        public async Task RegistrarCiudadanoAsync_DatosValidos_CreaCuentaConConsentimientoYVinculo()
        {
            string token = await SolicitarTokenAsync();

            var r = await _servicio.RegistrarCiudadanoAsync(token, "Ana Pérez", "Ana@Ejemplo.com", "v1", aceptaTratamientoDatos: true);

            Assert.IsTrue(r.Exitoso, r.ToString());
            var cuenta = _usuarios.Leer(r.Valor)!;
            Assert.AreEqual("ana@ejemplo.com", cuenta.CorreoElectronico);
            Assert.AreEqual("v1", cuenta.VersionConsentimiento);
            Assert.IsTrue(cuenta.TieneRol(CodigoRol.Ciudadano));
            Assert.AreEqual(r.Valor, await _servicio.ObtenerUsuarioVinculadoAsync(Telegram));
            Assert.IsNotNull(_vinculos.Tokens.Single().UsoUtc);
        }

        [TestMethod]
        public async Task RegistrarCiudadanoAsync_TokenReutilizado_RechazaEnlaceVencido()
        {
            string token = await SolicitarTokenAsync();
            Assert.IsTrue((await _servicio.RegistrarCiudadanoAsync(token, "Ana Pérez", "ana@ejemplo.com", "v1", true)).Exitoso);

            var r = await _servicio.RegistrarCiudadanoAsync(token, "Otra Persona", "otra@ejemplo.com", "v1", true);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesTelegram.EnlaceVencido, r.Mensaje);
            Assert.HasCount(1, _vinculos.Vinculos);
        }

        [TestMethod]
        public async Task RegistrarCiudadanoAsync_SinConsentimiento_RechazaSinCrearCuenta()
        {
            string token = await SolicitarTokenAsync();

            var r = await _servicio.RegistrarCiudadanoAsync(token, "Ana Pérez", "ana@ejemplo.com", "v1", aceptaTratamientoDatos: false);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
            Assert.AreEqual(MensajesTelegram.SinConsentimiento, r.Mensaje);
            Assert.IsNull(_vinculos.Tokens.Single().UsoUtc);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task RegistrarCiudadanoAsync_CorreoExistente_RechazaDuplicadoSinGastarToken()
        {
            string token = await SolicitarTokenAsync();

            var r = await _servicio.RegistrarCiudadanoAsync(token, "Ana Pérez", $"usuario{Existente}@ejemplo.com", "v1", true);

            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
            Assert.AreEqual(MensajesTelegram.CorreoExistente, r.Mensaje);
            Assert.IsTrue((await _servicio.ConsultarTokenAsync(token)).Exitoso);
        }

        [TestMethod]
        public async Task RegistrarCiudadanoAsync_CorreoInvalido_RechazaDatosInvalidos()
        {
            string token = await SolicitarTokenAsync();

            var r = await _servicio.RegistrarCiudadanoAsync(token, "Ana Pérez", "no-es-correo", "v1", true);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task DesvincularAsync_ConVinculo_CierraElVinculoYConservaLaCuenta()
        {
            _vinculos.Sembrar(Existente, Telegram);

            var r = await _servicio.DesvincularAsync(Telegram);

            Assert.IsTrue(r.Exitoso);
            Assert.IsNull(await _servicio.ObtenerUsuarioVinculadoAsync(Telegram));
            Assert.IsFalse(_vinculos.Vinculos.Single().Activo);
            Assert.IsNotNull(_usuarios.Leer(Existente));
        }

        [TestMethod]
        public async Task DesvincularAsync_SinVinculo_RechazaNoEncontrado()
        {
            var r = await _servicio.DesvincularAsync(Telegram);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo);
            Assert.AreEqual(MensajesTelegram.SinVinculo, r.Mensaje);
        }
    }
}
