using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private async Task<IReadOnlyList<RespuestaBot>> IniciarInvitadoPorQrAsync()
        {
            var respuestas = await TextoAsync("/start " + _caneca.IdentificadorQr, Invitado);
            return respuestas.Single().Botones.Any(b => b.Dato == "menu:invitado")
                ? await BotonAsync("menu:invitado", Invitado) : respuestas;
        }

        [TestMethod]
        public async Task PrimerQrSinCuenta_NoIniciaSolicitudAntesDeElegir_YConservaCaneca()
        {
            var r = Unica(await TextoAsync("/start " + _caneca.IdentificadorQr, Invitado));
            Assert.HasCount(3, r.Botones);
            Assert.IsNull(_conversaciones.Leer(Invitado)!.IdInteraccion);
            Assert.IsEmpty(_interacciones.Ordenes);
            r = Unica(await BotonAsync("menu:invitado", Invitado));
            Assert.IsNotNull(_conversaciones.Leer(Invitado)!.IdInteraccion);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == MaterialPet));
            Assert.DoesNotContain("no acumularás ecopuntos", r.Texto);
        }

        [TestMethod]
        public async Task BotonContinuarTrasVencerQr_NoIniciaSolicitud()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr, Invitado);
            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaConversacion + TimeSpan.FromSeconds(1));
            var r = Unica(await BotonAsync("menu:invitado", Invitado));
            Assert.IsNull(_conversaciones.Leer(Invitado)!.IdInteraccion);
            Assert.IsFalse(r.Botones.Any(b => b.Dato == MaterialPet));
        }
        [TestMethod]
        public async Task PrimeraBienvenidaSinCuenta_OfreceTresOpcionesYNoRepiteAvisoTrasReinicio()
        {
            var respuesta = Unica(await TextoAsync("/start", Invitado));
            CollectionAssert.AreEqual(new[] { "menu:crear", "menu:vincular", "menu:invitado" },
                respuesta.Botones.Select(b => b.Dato).ToArray());
            StringAssert.Contains(respuesta.Texto, "no acumularás ecopuntos");
            Assert.IsEmpty(_interacciones.Ordenes);
            await BotonAsync("menu:invitado", Invitado);
            var otro = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var regreso = Unica(await otro.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, "/start", null)));
            Assert.DoesNotContain("no acumularás ecopuntos", regreso.Texto);
            Assert.HasCount(ServicioConversacionTelegram.MenuPrincipal.Count, regreso.Botones);
            Assert.IsTrue(regreso.Botones.Any(b => b.Dato == "menu:/categorias"));
        }

        [TestMethod]
        public async Task VinculoPersistido_ReinicioYTresDiasDespues_NoSolicitaRegistro()
        {
            await TextoAsync("/start");
            _reloj.Avanzar(TimeSpan.FromDays(3));
            var otro = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var respuesta = Unica(await otro.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/depositos", null)));
            Assert.AreEqual(MensajesTelegram.SinDepositos, respuesta.Texto);
        }

        [TestMethod]
        [DataRow("/estadisticas")]
        [DataRow("/mismateriales")]
        [DataRow("/evolucion")]
        public async Task EstadisticasInvitado_NoRevelaDatosPrivados(string comando)
        {
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await TextoAsync(comando, Invitado)).Texto);
        }

        [TestMethod]
        public async Task BotonInvitadoAntiguo_NoAnunciaIdentidadInvitadaSiYaExisteVinculo()
        {
            var r = Unica(await BotonAsync("menu:invitado", Vinculado));
            StringAssert.Contains(r.Texto, "Tu cuenta sigue vinculada");
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            Assert.AreEqual(Ciudadano, _interacciones.LeerInteraccion(IdInteraccionActual)!.IdUsuario);
        }

        [TestMethod]
        public async Task EstadisticasVinculadas_UsanRepositorioCompartidoYDenieganCuentaDesactivada()
        {
            var estadisticas = new RepositorioEstadisticasEnMemoria();
            var otro = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                estadisticas: new ServicioEstadisticas(_usuarios, estadisticas));
            var r = Unica(await otro.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/estadisticas", null)));
            StringAssert.Contains(r.Texto, "30 días anteriores");
            StringAssert.Contains(r.Texto, "Peso medido: 0.5 kg");
            Assert.DoesNotContain("0.3", r.Texto);
            Assert.IsTrue(estadisticas.Filtros.All(f => f.IdUsuario == Ciudadano));
            await _usuarios.CambiarActivoAsync(Ciudadano, false);
            var denegada = Unica(await otro.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/estadisticas", null)));
            Assert.DoesNotContain("Peso medido", denegada.Texto);
            Assert.AreEqual(4, estadisticas.Llamadas);
        }
    }
}
