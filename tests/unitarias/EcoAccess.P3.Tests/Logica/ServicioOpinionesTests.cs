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
    public sealed class ServicioOpinionesTests
    {
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly OpinionesEnMemoria _repo = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 10, 31, 12, 0, 0, TimeSpan.Zero));
        private readonly ServicioOpiniones _servicio;
        public ServicioOpinionesTests()
        {
            _usuarios.AgregarCiudadano(1);
            _usuarios.AgregarCiudadano(2, consentimiento: false);
            _usuarios.Agregar(3, activo: true, CodigoRol.Administrador);
            _servicio = new(_usuarios, _repo, _reloj);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(6)]
        public async Task RechazaEstrellasInvalidasSinEscribir(int estrellas)
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.RegistrarCiudadanoAsync(1, estrellas, null, true)).Motivo);
            Assert.IsEmpty(_repo.Filas);
        }

        [TestMethod]
        public async Task ExigeConsentimientoYCuentaHabilitada()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.RegistrarCiudadanoAsync(1, 5, null, false)).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.RegistrarCiudadanoAsync(2, 5, null, true)).Motivo);
            Assert.AreEqual(MotivoRechazo.ActorNoEncontrado, (await _servicio.RegistrarCiudadanoAsync(900, 5, null, true)).Motivo);
            Assert.IsEmpty(_repo.Filas);
        }

        [TestMethod]
        public async Task UnEnvioDiario_ReintentoIdenticoNoDuplicaYDiaSiguientePermiteOtra()
        {
            Assert.IsTrue((await _servicio.RegistrarCiudadanoAsync(1, 4, "Bien", true)).Exitoso);
            Assert.IsTrue((await _servicio.RegistrarCiudadanoAsync(1, 4, "Bien", true)).Exitoso);
            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.RegistrarCiudadanoAsync(1, 5, "Otro", true)).Motivo);
            Assert.HasCount(1, _repo.Filas);
            _reloj.Avanzar(TimeSpan.FromDays(1));
            Assert.IsTrue((await _servicio.RegistrarCiudadanoAsync(1, 5, null, true)).Exitoso);
            Assert.HasCount(2, _repo.Filas);
        }

        [TestMethod]
        public async Task InvitadoSoloEstrellasYNoPersisteSesionSinHash()
        {
            const string sesion = "sesion-local-ficticia-12345";
            Assert.IsTrue((await _servicio.RegistrarInvitadoAsync(sesion, 3, true)).Exitoso);
            var opinion = _repo.Filas.Values.Single();
            Assert.IsNull(opinion.Comentario); Assert.IsNull(opinion.IdUsuario);
            Assert.AreEqual(64, opinion.Participante.Length); Assert.AreNotEqual(sesion, opinion.Participante);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.RegistrarInvitadoAsync("otra-sesion-local-1234", 3, false)).Motivo);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.RegistrarInvitadoAsync("corta", 3, true)).Motivo);
            Assert.ThrowsExactly<ArgumentException>(() => new Opinion(null, new string('a', 64), 3, "No permitido", _reloj.GetUtcNow().UtcDateTime));
        }

        [TestMethod]
        [DataRow("comentario\ncon salto")]
        [DataRow("comentario\u0000oculto")]
        public async Task RechazaControlesYComentarioLargo(string comentario)
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.RegistrarCiudadanoAsync(1, 5, comentario, true)).Motivo);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.RegistrarCiudadanoAsync(1, 5, new string('a', 501), true)).Motivo);
            Assert.IsEmpty(_repo.Filas);
        }

        [TestMethod]
        public async Task TendenciasExigenAdministradorYPeriodoAcotado()
        {
            DateTime desde = _reloj.GetUtcNow().UtcDateTime.Date;
            Assert.IsTrue((await _servicio.RegistrarCiudadanoAsync(1, 5, null, true)).Exitoso);
            Assert.IsTrue((await _servicio.RegistrarInvitadoAsync("sesion-invitado-12345", 3, true)).Exitoso);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.ConsultarTendenciasAsync(1, desde, desde.AddDays(1))).Motivo);
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.ConsultarTendenciasAsync(3, desde, desde.AddDays(367))).Motivo);
            var fila = (await _servicio.ConsultarTendenciasAsync(3, desde, desde.AddDays(1))).Valor.Single();
            Assert.AreEqual(2, fila.Cantidad); Assert.AreEqual(4m, fila.Promedio); Assert.AreEqual(1, fila.Invitados);
        }

        [TestMethod]
        public async Task LimiteGlobalInvitadosSeAplicaSinConsumirCupoEnReintento()
        {
            for (int i = 0; i < 100; i++) Assert.IsTrue((await _servicio.RegistrarInvitadoAsync("sesion-ficticia-" + i.ToString("D3"), 3, true)).Exitoso);
            Assert.IsTrue((await _servicio.RegistrarInvitadoAsync("sesion-ficticia-000", 3, true)).Exitoso);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.RegistrarInvitadoAsync("sesion-ficticia-100", 3, true)).Motivo);
            Assert.HasCount(100, _repo.Filas);
        }
    }
}
