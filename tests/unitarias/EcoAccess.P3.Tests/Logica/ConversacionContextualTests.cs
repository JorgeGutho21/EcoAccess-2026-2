using System;
using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>
    /// El texto se interpreta según el contexto: reproduce lo observado desde un teléfono, donde «Te equivocaste» se
    /// buscaba como un material. Con motores falsos: no es Telegram ni un modelo reales.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private static bool EsBusquedaFallida(RespuestaBot r) => r.Texto == MensajesTelegram.MaterialNoEncontrado;

        [TestMethod]
        [DataRow("Hola")]
        [DataRow("hola")]
        [DataRow("Buenas")]
        [DataRow("Buenos días")]
        [DataRow("Buenas tardes")]
        [DataRow("¡Hola!")]
        public async Task UnSaludoEsBreveYNoSeBuscaEnElCatalogo(string saludo)
        {
            RespuestaBot r = Unica(await TextoAsync(saludo, Invitado));

            Assert.IsFalse(EsBusquedaFallida(r));
            Assert.AreEqual(MensajesTelegram.SaludoBreve, r.Texto);
            CollectionAssert.AreEqual(ServicioConversacionTelegram.BotonesSaludo.ToList(), r.Botones.ToList());
        }

        [TestMethod]
        [DataRow("Te equivocaste")]
        [DataRow("No es eso")]
        [DataRow("Está mal")]
        [DataRow("Eso no es cartón")]
        [DataRow("No reconociste el objeto")]
        public async Task SinClasificacionReciente_UnaCorreccionLoExplicaYNoBuscaMateriales(string frase)
        {
            RespuestaBot r = Unica(await TextoAsync(frase));

            Assert.IsFalse(EsBusquedaFallida(r), "Una corrección no es un material.");
            Assert.AreEqual(MensajesTelegram.SinClasificacionParaCorregir, r.Texto);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:enviarfoto"));
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:inicio"));
        }

        [TestMethod]
        [DataRow("Te equivocaste")]
        [DataRow("No es eso")]
        [DataRow("Está mal")]
        [DataRow("Eso no es cartón")]
        [DataRow("No reconociste el objeto")]
        public async Task TrasUnaFoto_UnaCorreccionOfreceCambiarOtraFotoSegundaOpinionYMenu(string frase)
        {
            var local = new MotorFalso("ollama-local:modelo", PropuestaPet);
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, local, externo);
            await FotoConCanecaAsync(servicio);

            RespuestaBot r = Unica(await EnviarConAsync(servicio, frase));

            Assert.AreEqual(MensajesTelegram.CorregirClasificacion, r.Texto);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:/manual"), "Cambiar material");
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:otrafoto"), "Otra fotografía");
            Assert.IsTrue(r.Botones.Any(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoSegundaOpinion, StringComparison.Ordinal)), "Segunda opinión");
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:inicio"), "Menú");
            Assert.IsEmpty(externo.Envios, "Corregir no envía la foto a ningún servicio externo.");
            Assert.IsEmpty(_interacciones.Ordenes, "Corregir no abre la caneca.");
        }

        [TestMethod]
        public async Task LaSegundaOpinionDeUnaCorreccionPideAutorizacionAntesDeEnviarLaFoto()
        {
            var local = new MotorFalso("ollama-local:modelo", PropuestaPet);
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, local, externo);
            await FotoConCanecaAsync(servicio);
            RespuestaBot correccion = Unica(await EnviarConAsync(servicio, "Te equivocaste"));

            RespuestaBot aviso = Unica(await EnviarConAsync(servicio, boton: BotonDe(correccion, ServicioConversacionTelegram.PrefijoSegundaOpinion)));
            Assert.AreEqual(MensajesTelegram.AvisoSegundaOpinion, aviso.Texto);
            Assert.IsEmpty(externo.Envios);

            Unica(await EnviarConAsync(servicio, boton: BotonDe(aviso, ServicioConversacionTelegram.PrefijoAutorizarExterna)));
            Assert.AreEqual(EnvioExterno.SegundaOpinion, externo.Envios.Single());
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CambiarMaterialTrasUnaCorreccionMuestraLosMaterialesYDescartaLaPropuesta()
        {
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("ollama-local:modelo", PropuestaPet), new MotorFalso("groq:modelo", PropuestaPet));
            await FotoConCanecaAsync(servicio);
            await EnviarConAsync(servicio, "No es eso");

            RespuestaBot r = Unica(await EnviarConAsync(servicio, boton: "menu:/manual"));

            Assert.IsTrue(r.Botones.Any(b => b.Dato == MaterialPet));
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        public async Task LaBusquedaDeResiduosSigueFuncionando()
        {
            RespuestaBot r = Unica(await TextoAsync(_pet.Codigo));
            Assert.IsFalse(EsBusquedaFallida(r));
            StringAssert.Contains(r.Texto, _pet.Nombre);
        }

        [TestMethod]
        public async Task UnMensajeDesconocidoOfreceAyudaYNoInventaUnaClasificacion()
        {
            RespuestaBot r = Unica(await TextoAsync("xyzqw"));

            Assert.AreEqual(MensajesTelegram.MaterialNoEncontrado, r.Texto);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:inicio"));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CancelarConTextoRegresaAUnEstadoSeguro()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            Assert.IsNotNull(_conversaciones.Leer(Vinculado)!.IdInteraccion);

            RespuestaBot r = Unica(await TextoAsync("Cancelar"));

            Assert.IsFalse(EsBusquedaFallida(r));
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == "menu:inicio"));
        }

        [TestMethod]
        public async Task StartEnFlujoRapidoMuestraElMenuSinCrearNiCancelarSolicitudes()
        {
            await AgregarPilotoDirectoAsync();
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("ollama-local:modelo", PropuestaPet), new MotorFalso("groq:modelo", PropuestaPet));

            RespuestaBot primera = Unica(await EnviarConAsync(servicio, "/start"));
            Assert.AreEqual(MensajesConversacion.SaludoDeNuevo, primera.Texto);
            CollectionAssert.AreEqual(ServicioConversacionTelegram.BotonesSaludo.ToList(), primera.Botones.ToList());
            Assert.IsNull(_conversaciones.Leer(Vinculado)?.IdInteraccion, "Abrir el menú no inicia una solicitud.");

            await FotoConCanecaAsync(servicio);
            int? enCurso = _conversaciones.Leer(Vinculado)!.IdInteraccion;
            RespuestaBot otra = Unica(await EnviarConAsync(servicio, "/start"));
            Assert.AreEqual(MensajesConversacion.SaludoDeNuevo, otra.Texto);
            Assert.AreEqual(enCurso, _conversaciones.Leer(Vinculado)!.IdInteraccion, "Repetir /start no descarta lo que estaba en curso.");
        }

        [TestMethod]
        public async Task LaUltimaFotoNoSeVuelveAEnviarConElQrDeOtraConversacion()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("ollama-local:modelo", PropuestaPet), externo);
            await FotoConCanecaAsync(servicio);
            await EnviarConAsync(servicio, "/cancelar");

            RespuestaBot r = Unica(await EnviarConAsync(servicio, "Te equivocaste"));

            Assert.AreEqual(MensajesTelegram.SinClasificacionParaCorregir, r.Texto, "Tras cancelar ya no hay nada que corregir.");
            Assert.IsFalse(r.Botones.Any(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoSegundaOpinion, StringComparison.Ordinal)));
        }

        [TestMethod]
        public async Task CadaOpcionDelMenuPrincipalResponde()
        {
            await AgregarPilotoDirectoAsync();
            foreach (BotonBot opcion in ServicioConversacionTelegram.MenuPrincipal)
            {
                RespuestaBot r = Unica(await BotonAsync(opcion.Dato));
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.Texto), opcion.Texto);
                Assert.AreNotEqual(MensajesTelegram.NoEntendido, r.Texto, opcion.Texto);
            }
        }

        [TestMethod]
        public void ElMenuPrincipalTieneLasOpcionesAcordadasSinRepetirDestinos()
        {
            string[] esperadas = { "Clasificar", "Escanear", "depósitos", "ecopuntos", "cuenta", "Materiales", "Ayuda", "Acerca" };
            foreach (string texto in esperadas)
                Assert.IsTrue(ServicioConversacionTelegram.MenuPrincipal.Any(b => b.Texto.Contains(texto)), texto);
            Assert.AreEqual(esperadas.Length, ServicioConversacionTelegram.MenuPrincipal.Select(b => b.Dato).Distinct().Count());
        }
    }
}
