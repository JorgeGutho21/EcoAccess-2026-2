using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task MenuCiudadanoSeisOpciones_ComandosEducativosSeConservanYNoCreanOrdenes()
        {
            Assert.HasCount(8, ServicioConversacionTelegram.MenuPrincipal);
            StringAssert.Contains(Unica(await TextoAsync("/proximamente")).Texto, "no son funciones ni instalaciones activas");
            Assert.IsGreaterThan(0, Unica(await TextoAsync("/materiales")).Texto.Length);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task OpinionBot_CuentaVigenteComentarioConsentimientoYReintento()
        {
            var repo = new OpinionesEnMemoria();
            var servicio = ConOpiniones(repo);
            var aviso = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/calificar", null)));
            StringAssert.Contains(aviso.Texto, "aceptas guardar");
            Assert.IsEmpty(repo.Filas);
            var primera = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/calificar 4 acepto Fácil de usar", null)));
            StringAssert.Contains(primera.Texto, "Recibimos");
            await servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/calificar 4 acepto Fácil de usar", null));
            Assert.HasCount(1, repo.Filas);
            Assert.AreEqual(Ciudadano, repo.Filas.Values.Single().IdUsuario);
            Assert.AreEqual("Fácil de usar", repo.Filas.Values.Single().Comentario);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task OpinionBot_InvitadoSinComentario_NoAceptaBotonManipuladoNiGrupos()
        {
            var repo = new OpinionesEnMemoria();
            var servicio = ConOpiniones(repo);
            await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, false, "/calificar 5 acepto", null));
            await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, null, "menu:opinion:99"));
            await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, "/calificar 5 acepto comentario", null));
            Assert.IsEmpty(repo.Filas);
            var respuesta = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, null, "menu:opinion:5")));
            StringAssert.Contains(respuesta.Texto, "Recibimos");
            Assert.IsNull(repo.Filas.Values.Single().IdUsuario);
            Assert.IsNull(repo.Filas.Values.Single().Comentario);
        }

        private ServicioConversacionTelegram ConOpiniones(OpinionesEnMemoria repo) => new(
            _conversaciones, _contenedores, _materiales,
            new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
            _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
            opiniones: new ServicioOpiniones(_usuarios, repo, _reloj));
    }
}
