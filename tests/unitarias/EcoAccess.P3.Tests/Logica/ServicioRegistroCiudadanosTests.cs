using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioRegistroCiudadanosTests
    {
        private const string Clave = "verde-ciudad-26";
        private readonly PuertoRegistro _puerto = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 26, 16, 0, 0, TimeSpan.Zero));
        private ServicioRegistroCiudadanos Servicio() => new(_puerto, new HasherFalso(), _reloj);

        [TestMethod]
        public async Task RegistroValidoNormalizaDatosYEntregaSoloHashYConsentimiento()
        {
            var resultado = await Servicio().RegistrarAsync("  Ciudadana Ficticia  ", "  CIUDADANA@Ejemplo.com ", Clave, Clave, true);
            Assert.IsTrue(resultado.Exitoso);
            Assert.AreEqual(77, resultado.Valor);
            Assert.AreEqual("Ciudadana Ficticia", _puerto.Alta!.Nombre);
            Assert.AreEqual("ciudadana@ejemplo.com", _puerto.Alta.CorreoElectronico);
            Assert.AreEqual(_reloj.Ahora.UtcDateTime, _puerto.Alta.FechaUtc);
            Assert.AreEqual(ServicioRegistroCiudadanos.VersionConsentimiento, _puerto.Alta.VersionConsentimiento);
            Assert.HasCount(32, _puerto.Alta.Credencial.Hash);
            Assert.IsTrue(new HasherFalso().Verificar(Clave, _puerto.Alta.Credencial.Sal, _puerto.Alta.Credencial.Iteraciones, _puerto.Alta.Credencial.Hash));
        }

        [TestMethod]
        [DataRow("", "persona@ejemplo.com", Clave, Clave, true)]
        [DataRow("Ciudadano", "incorrecto", Clave, Clave, true)]
        [DataRow("Ciudadano", "persona@ejemplo.com", "corta", "corta", true)]
        [DataRow("Ciudadano", "persona@ejemplo.com", Clave, "otra-clave-distinta", true)]
        [DataRow("Ciudadano", "persona@ejemplo.com", Clave, Clave, false)]
        [DataRow("Ciudadano", "persona@ejemplo.com", "            ", "            ", true)]
        [DataRow("Ciudadano", "persona@ejemplo.com", null, null, true)]
        public async Task CamposInvalidosNoLleganAOracle(string nombre, string correo, string? clave, string? confirmacion, bool consentimiento)
        {
            var resultado = await Servicio().RegistrarAsync(nombre, correo, clave, confirmacion, consentimiento);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.AreEqual(0, _puerto.Llamadas);
        }

        /// <summary>Política ciudadana pedida por el propietario: de 6 a 20 caracteres, sin exigir símbolos.</summary>
        [TestMethod]
        [DataRow(5, false)]
        [DataRow(6, true)]
        [DataRow(20, true)]
        [DataRow(21, false)]
        [DataRow(128, false)]
        public async Task LimitesDeContrasenaRespetanLaPoliticaCiudadana(int longitud, bool esperado)
        {
            string clave = string.Concat(Enumerable.Range(0, longitud).Select(i => (char)('a' + (i * 7) % 26)));
            Assert.AreEqual(esperado, (await Servicio().RegistrarAsync("Ciudadano", "persona@ejemplo.com", clave, clave, true)).Exitoso);
        }

        [TestMethod]
        [DataRow("solotexto")]
        [DataRow("verde2026")]
        [DataRow("eco!#$%&*")]
        [DataRow("Árbol ñandú")]
        public async Task SimbolosSonOpcionalesYSePermiten(string clave)
        {
            Assert.IsTrue((await Servicio().RegistrarAsync("Ciudadano", "persona@ejemplo.com", clave, clave, true)).Exitoso);
        }

        [TestMethod]
        [DataRow("123456")]
        [DataRow("PASSWORD")]
        [DataRow("qwerty")]
        [DataRow("ecoaccess")]
        public async Task ContrasenasMuyComunesSeRechazan(string clave)
        {
            var resultado = await Servicio().RegistrarAsync("Ciudadano", "persona@ejemplo.com", clave, clave, true);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            StringAssert.Contains(resultado.Mensaje, "común");
            Assert.AreEqual(0, _puerto.Llamadas);
        }

        /// <summary>La política ciudadana no toca la del personal ni los límites de las credenciales existentes.</summary>
        [TestMethod]
        public void LaPoliticaDelPersonalNoCambia()
        {
            Assert.AreEqual(12, CredencialAcceso.LongitudMinimaContrasena);
            Assert.AreEqual(128, CredencialAcceso.LongitudMaximaContrasena);
        }

        [TestMethod]
        public async Task ConflictoConcurrenteNoSePresentaComoAltaCorrecta()
        {
            _puerto.Duplicado = true;
            Assert.AreEqual(MotivoRechazo.Duplicado, (await Servicio().RegistrarAsync("Ciudadano", "persona@ejemplo.com", Clave, Clave, true)).Motivo);
        }

        [TestMethod]
        public async Task CancelacionNoEscribe()
        {
            using var ct = new CancellationTokenSource(); ct.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Servicio().RegistrarAsync("Ciudadano", "persona@ejemplo.com", Clave, Clave, true, ct.Token));
            Assert.AreEqual(0, _puerto.Llamadas);
        }

        private sealed class PuertoRegistro : IRepositorioRegistroCiudadanos
        {
            internal int Llamadas { get; private set; }
            internal AltaCiudadano? Alta { get; private set; }
            internal bool Duplicado { get; set; }
            public Task<int> RegistrarAsync(AltaCiudadano alta, CancellationToken cancellationToken = default)
            {
                Llamadas++; Alta = alta;
                if (Duplicado) throw new ConflictoUnicidadException("uq_usuario_correo");
                return Task.FromResult(77);
            }
        }
    }
}
