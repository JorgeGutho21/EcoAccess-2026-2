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

    /// <summary>
    /// Vinculación de una cuenta existente en dos canales (P-15): la página autenticada prepara un código y el mismo
    /// Telegram que pidió el enlace lo confirma.
    /// </summary>
    [TestClass]
    public sealed class VinculacionCuentaExistenteTests
    {
        private const long Telegram = 555_000_111;
        private const long OtroTelegram = 555_000_222;
        private const int Ciudadano = 3;
        private const int OtroCiudadano = 4;
        private const int SoloAdministrador = 7;
        private const int SinConsentimiento = 8;
        private const int Inactivo = 9;
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioVinculosEnMemoria _vinculos;
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioVinculacionTelegram _servicio;

        public VinculacionCuentaExistenteTests()
        {
            _usuarios.AgregarCiudadano(Ciudadano);
            _usuarios.AgregarCiudadano(OtroCiudadano);
            _usuarios.Agregar(SoloAdministrador, activo: true, CodigoRol.Administrador);
            _usuarios.AgregarCiudadano(SinConsentimiento, consentimiento: false);
            _usuarios.AgregarCiudadano(Inactivo, activo: false);
            _vinculos = new RepositorioVinculosEnMemoria(_usuarios);
            _servicio = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
        }

        private async Task<string> EnlaceAsync(long telegram = Telegram) =>
            (await _servicio.SolicitarAsync(telegram, telegram)).Valor!.Token;

        private async Task<string> PrepararAsync(int cuenta = Ciudadano, long telegram = Telegram)
        {
            var r = await _servicio.PrepararVinculacionCuentaExistenteAsync(await EnlaceAsync(telegram), cuenta);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return r.Valor!.Codigo;
        }

        [TestMethod]
        public async Task PendienteReemplazadaTrasValidarCodigo_NoVinculaLaCuentaNueva()
        {
            string token = await EnlaceAsync();
            string codigo = (await _servicio.PrepararVinculacionCuentaExistenteAsync(token, Ciudadano)).Valor!.Codigo;
            _vinculos.AntesDeConfirmarPendienteAsync = async () =>
            {
                Assert.IsTrue((await _servicio.PrepararVinculacionCuentaExistenteAsync(token, OtroCiudadano)).Exitoso);
            };
            var resultado = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);
            Assert.IsFalse(resultado.Exitoso);
            Assert.IsEmpty(_vinculos.Vinculos);
            Assert.IsTrue((await _servicio.ConsultarTokenAsync(token)).Exitoso);
        }

        [TestMethod]
        public async Task CodigoCorrectoDesdeElTelegramDelEnlace_CreaUnVinculoSinCambiarRoles()
        {
            string codigo = await PrepararAsync();
            CodigoRol[] rolesAntes = (await _usuarios.ObtenerPorIdAsync(Ciudadano))!.Roles.ToArray();

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(Ciudadano, r.Valor);
            Assert.AreEqual(Ciudadano, await _servicio.ObtenerUsuarioVinculadoAsync(Telegram));
            CollectionAssert.AreEquivalent(rolesAntes, (await _usuarios.ObtenerPorIdAsync(Ciudadano))!.Roles.ToArray());
            Assert.IsNotNull(_vinculos.Tokens.Single().UsoUtc, "El token queda usado.");
            Assert.IsEmpty(_vinculos.Pendientes, "La pendiente se consume.");
        }

        [TestMethod]
        public async Task Preparar_SoloGuardaHashDelCodigoYNoCreaVinculo()
        {
            string codigo = await PrepararAsync();

            Assert.HasCount(ServicioVinculacionTelegram.LargoCodigo, codigo);
            var pendiente = _vinculos.Pendientes.Single();
            Assert.AreNotEqual(codigo, pendiente.HashCodigo);
            Assert.IsFalse(pendiente.HashCodigo.Contains(codigo, StringComparison.OrdinalIgnoreCase));
            Assert.IsNull(await _servicio.ObtenerUsuarioVinculadoAsync(Telegram), "Preparar no vincula.");
        }

        [TestMethod]
        public async Task CodigoEscritoEnMinusculasYConEspacios_SeAcepta()
        {
            string codigo = await PrepararAsync();
            string escrito = " " + codigo[..4].ToLowerInvariant() + " " + codigo[4..].ToLowerInvariant() + " ";

            Assert.IsTrue((await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, escrito)).Exitoso);
        }

        [TestMethod]
        public async Task EnlaceReenviadoAOtroTelegram_SinElCodigoDeLaPaginaNoVincula()
        {
            await PrepararAsync();

            var otro = await _servicio.ConfirmarVinculacionAsync(OtroTelegram, OtroTelegram, "AAAAAAAA");

            Assert.AreEqual(MotivoRechazo.NoEncontrado, otro.Motivo, "El otro Telegram no tiene pendiente propia.");
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task CodigoIncorrecto_CuentaIntentosYAlQuintoAnulaLaPendiente()
        {
            string codigo = await PrepararAsync();

            for (int i = 1; i <= ServicioVinculacionTelegram.MaximoIntentosCodigo; i++)
            {
                var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, "ZZZZZZZZ");
                Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
            }

            var correctoTarde = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);
            Assert.AreEqual(MotivoRechazo.NoEncontrado, correctoTarde.Motivo, "Tras agotar intentos el código correcto ya no sirve.");
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task CodigoVencido_NoVincula()
        {
            string codigo = await PrepararAsync();
            _reloj.Avanzar(ServicioVinculacionTelegram.VigenciaCodigo + TimeSpan.FromSeconds(1));

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task ConfirmacionRepetida_NoCreaUnSegundoVinculo()
        {
            string codigo = await PrepararAsync();
            Assert.IsTrue((await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo)).Exitoso);

            var repetida = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);

            Assert.IsFalse(repetida.Exitoso);
            Assert.HasCount(1, _vinculos.Vinculos);
        }

        [TestMethod]
        public async Task ConfirmarDesdeUnGrupo_SeRechaza()
        {
            string codigo = await PrepararAsync();

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, -100_200_300, codigo);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        [DataRow(SoloAdministrador)]
        [DataRow(SinConsentimiento)]
        [DataRow(Inactivo)]
        [DataRow(999)]
        public async Task Preparar_CuentaNoCiudadanaSinConsentimientoInactivaOInexistente_SeRechaza(int cuenta)
        {
            var r = await _servicio.PrepararVinculacionCuentaExistenteAsync(await EnlaceAsync(), cuenta);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
            Assert.IsEmpty(_vinculos.Pendientes);
        }

        [TestMethod]
        public async Task Preparar_CuentaYaVinculadaAOtroTelegram_SeRechazaSinReemplazar()
        {
            _vinculos.Sembrar(Ciudadano, OtroTelegram);

            var r = await _servicio.PrepararVinculacionCuentaExistenteAsync(await EnlaceAsync(), Ciudadano);

            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
            Assert.AreEqual(Ciudadano, await _servicio.ObtenerUsuarioVinculadoAsync(OtroTelegram));
        }

        [TestMethod]
        public async Task Preparar_TokenInexistenteOVencido_SeRechaza()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.PrepararVinculacionCuentaExistenteAsync("no-existe", Ciudadano)).Motivo);

            string token = await EnlaceAsync();
            _reloj.Avanzar(TokenVinculacion.Vigencia + TimeSpan.FromSeconds(1));
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PrepararVinculacionCuentaExistenteAsync(token, Ciudadano)).Motivo);
        }

        [TestMethod]
        public async Task CuentaDesactivadaEntrePrepararYConfirmar_NoSeVincula()
        {
            string codigo = await PrepararAsync();
            await _usuarios.CambiarActivoAsync(Ciudadano, false);

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.IsEmpty(_vinculos.Vinculos);
        }

        [TestMethod]
        public async Task OtroVinculoCreadoEntrePrepararYConfirmar_SeRechazaComoDuplicado()
        {
            string codigo = await PrepararAsync();
            _vinculos.Sembrar(Ciudadano, OtroTelegram);

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigo);

            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
            Assert.HasCount(1, _vinculos.Vinculos);
        }

        [TestMethod]
        public async Task PendienteSustituidaEntreLeerYConfirmar_ElCodigoAnteriorNoVinculaNingunaCuenta()
        {
            string token = await EnlaceAsync();
            string codigoA = (await _servicio.PrepararVinculacionCuentaExistenteAsync(token, Ciudadano)).Valor!.Codigo;
            _vinculos.DespuesDeLeerPendiente = async () =>
                Assert.IsTrue((await _servicio.PrepararVinculacionCuentaExistenteAsync(token, OtroCiudadano)).Exitoso);

            var r = await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, codigoA);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.IsEmpty(_vinculos.Vinculos, "El código de una cuenta no puede vincular la que la reemplazó.");
            Assert.AreEqual(OtroCiudadano, _vinculos.Pendientes.Single().IdUsuario);
        }

        [TestMethod]
        public async Task PrepararDeNuevo_ReemplazaElCodigoAnterior()
        {
            string token = await EnlaceAsync();
            string primero = (await _servicio.PrepararVinculacionCuentaExistenteAsync(token, Ciudadano)).Valor!.Codigo;
            string segundo = (await _servicio.PrepararVinculacionCuentaExistenteAsync(token, Ciudadano)).Valor!.Codigo;

            Assert.HasCount(1, _vinculos.Pendientes);
            if (primero != segundo)
            {
                Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, primero)).Motivo);
            }

            Assert.IsTrue((await _servicio.ConfirmarVinculacionAsync(Telegram, Telegram, segundo)).Exitoso);
        }
    }
}
