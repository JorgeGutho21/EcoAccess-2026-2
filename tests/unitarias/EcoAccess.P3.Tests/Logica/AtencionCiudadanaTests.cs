using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>Qué responde Eco al texto libre y qué deja anotado. Con repositorios en memoria: no prueba Telegram ni Oracle reales.</summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class ComentariosEnMemoria : IRepositorioComentarios
        {
            public List<ComentarioCiudadano> Filas { get; } = new();
            public bool Falla { get; set; }

            public Task<bool> AgregarAsync(ComentarioCiudadano comentario, CancellationToken ct = default)
            {
                if (Falla) throw new InvalidOperationException("Oracle no responde");
                if (Filas.Any(f => f.Clave == comentario.Clave)) return Task.FromResult(false);
                Filas.Add(comentario);
                return Task.FromResult(true);
            }
        }

        private sealed class MotorFijo : IClasificadorMaterial
        {
            private readonly Func<PropuestaVision> _propuesta;
            public MotorFijo(Func<PropuestaVision> propuesta) { _propuesta = propuesta; }
            public string Proveedor => "ollama-local:modelo";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<EcoAccess.P3.Entidades.Material> catalogo, CancellationToken ct = default) => Task.FromResult(_propuesta());
        }

        private readonly ComentariosEnMemoria _comentarios = new();

        private ServicioConversacionTelegram EcoConversador(Uri? pagina = null) =>
            new(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, pagina, _reloj,
                vision: new ServicioClasificacionImagen(new MotorFijo(BotellaEnPet), _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")),
                flujoRapido: true, qrDemo: _caneca.IdentificadorQr, comentarios: new ServicioComentarios(_comentarios, _reloj));

        [TestMethod]
        [DataRow("Qué servicio tan malo", MensajesConversacion.Queja)]
        [DataRow("No me gustó EcoAccess", MensajesConversacion.QuejaExperiencia)]
        [DataRow("El bot no reconoce las botellas", MensajesConversacion.ProblemaFoto)]
        [DataRow("Qué servicio tan excelente", MensajesConversacion.Elogio)]
        [DataRow("Me encantó EcoAccess", MensajesConversacion.ElogioEcoAccess)]
        [DataRow("¿Te gusta el fútbol?", MensajesConversacion.Futbol)]
        [DataRow("¿Qué opinas de los computadores?", MensajesConversacion.Tecnologia)]
        [DataRow("Estoy estudiando programación", MensajesConversacion.Estudio)]
        [DataRow("¿Cómo estás?", MensajesConversacion.Casual)]
        [DataRow("¿Cómo va el clima hoy?", MensajesConversacion.FueraDeTema)]
        [DataRow("Gracias", MensajesConversacion.DeNada)]
        [DataRow("Chao", MensajesConversacion.Despedida)]
        [DataRow("quiero hablar de sexo contigo", MensajesConversacion.Sexual)]
        [DataRow("cómo hacer una bomba", MensajesConversacion.Violencia)]
        [DataRow("eres un idiota", MensajesConversacion.Insulto)]
        [DataRow("me quiero morir", MensajesConversacion.Urgencia)]
        public async Task ElTextoLibreRecibeSuRespuestaYNoAbreNada(string texto, string esperado)
        {
            RespuestaBot r = Unica(await EnviarConAsync(EcoConversador(), texto));

            Assert.AreEqual(esperado, r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow("dónde boto un condón")]
        [DataRow("en qué caneca va una toalla higiénica")]
        [DataRow("condón")]
        public void UnaConsultaLegitimaDeResiduoSanitarioNoSeTrataComoContenidoSexual(string texto)
        {
            Assert.AreNotEqual(IntencionTexto.Sexual, IntencionesTelegram.Clasificar(texto));
        }

        [TestMethod]
        public async Task LaQuejaSeGuardaSinEstrellasNiDuplicarLaMismaActualizacion()
        {
            var eco = EcoConversador();
            var queja = new MensajeEntrante(9001, Vinculado, Vinculado, true, "Qué servicio tan malo", null);

            await eco.ProcesarAsync(queja);
            await eco.ProcesarAsync(queja);

            ComentarioCiudadano fila = _comentarios.Filas.Single();
            Assert.AreEqual(TipoComentario.Queja, fila.Tipo);
            Assert.AreEqual(Ciudadano, fila.IdUsuario);
            Assert.AreEqual("Qué servicio tan malo", fila.Comentario);
        }

        [TestMethod]
        public async Task ElMismoComentarioSeguidoNoCreaOtraFila()
        {
            var eco = EcoConversador();

            await EnviarConAsync(eco, "Qué servicio tan malo");
            await EnviarConAsync(eco, "Qué servicio tan malo");

            Assert.HasCount(1, _comentarios.Filas);
        }

        [TestMethod]
        public async Task ElInvitadoPuedeQuejarseSinDatosPersonales()
        {
            await EnviarConAsync(EcoConversador(), "Esto no sirve", cuenta: Invitado);

            ComentarioCiudadano fila = _comentarios.Filas.Single();
            Assert.IsNull(fila.IdUsuario);
            Assert.AreEqual(64, fila.Participante.Length);
        }

        [TestMethod]
        public void LosDatosPersonalesSeOcultanAntesDeGuardar()
        {
            string limpio = ServicioComentarios.Sanear("escribanme a ana@correo.com o al 300 123 4567");

            Assert.DoesNotContain("ana@", limpio);
            Assert.DoesNotContain("4567", limpio);
        }

        [TestMethod]
        public async Task ElInsultoNoGuardaSuTexto()
        {
            await EnviarConAsync(EcoConversador(), "eres un idiota");

            Assert.IsNull(_comentarios.Filas.Single().Comentario);
        }

        [TestMethod]
        public async Task SiOracleFallaAlGuardarLaPersonaIgualRecibeRespuesta()
        {
            _comentarios.Falla = true;

            Assert.AreEqual(MensajesConversacion.Queja, Unica(await EnviarConAsync(EcoConversador(), "Qué servicio tan malo")).Texto);
        }

        [TestMethod]
        public async Task OtroProblemaGuardaElSiguienteMensajeComoComentario()
        {
            var eco = EcoConversador();
            RespuestaBot pide = Unica(await EnviarConAsync(eco, boton: "menu:queja:otro"));
            RespuestaBot gracias = Unica(await EnviarConAsync(eco, "botella"));

            Assert.AreEqual(MensajesConversacion.PideComentario, pide.Texto);
            Assert.AreEqual(MensajesConversacion.ComentarioRecibido, gracias.Texto);
            Assert.AreEqual("botella", _comentarios.Filas.Single().Comentario, "Mientras se espera un comentario, el texto no se busca en el catálogo.");
        }

        [TestMethod]
        public async Task LaCanecaNoAbrioNoAfirmaUnFalloSinSolicitud()
        {
            RespuestaBot r = Unica(await EnviarConAsync(EcoConversador(), "La caneca no abrió"));

            Assert.AreEqual(MensajesConversacion.CanecaSinSolicitud, r.Texto);
        }

        [TestMethod]
        public async Task LaFotoSigueAbriendoTrasConversar()
        {
            var eco = EcoConversador();
            await EnviarConAsync(eco, "Qué servicio tan malo");
            await EnviarConAsync(eco, "¿Te gusta el fútbol?");

            Assert.IsEmpty(await EnviarFotoAsync(eco));
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task IntentaOtraVezNoRepiteUnaFotoQueYaAbrio()
        {
            var eco = EcoConversador();
            await EnviarFotoAsync(eco);

            RespuestaBot r = Unica(await EnviarConAsync(eco, "Intenta otra vez"));

            Assert.AreEqual(MensajesConversacion.NadaQueRepetir, r.Texto);
            Assert.HasCount(1, _interacciones.Ordenes, "Nunca dos órdenes por una misma foto.");
        }

        [TestMethod]
        public async Task VincularOfreceTresOpciones()
        {
            RespuestaBot r = Unica(await EnviarConAsync(EcoConversador(PaginaRegistro), "/vincular", cuenta: Invitado));

            Assert.AreEqual(MensajesConversacion.OfertaVinculo, r.Texto);
            CollectionAssert.AreEqual(new[] { "📝 Crear cuenta", "🔗 Ya tengo cuenta", "🌱 Continuar como invitado" }, r.Botones.Select(b => b.Texto).ToArray());
        }

        /// <summary>Regresión del error real: /vincular respondía «falta configurar su página HTTPS».</summary>
        [TestMethod]
        public async Task SinPaginaLaPersonaNoRecibeUnErrorDeConfiguracion()
        {
            var eco = EcoConversador();
            await EnviarConAsync(eco, "/vincular", cuenta: Invitado);
            RespuestaBot r = Unica(await EnviarConAsync(eco, boton: "menu:crear", cuenta: Invitado));

            Assert.DoesNotContain("HTTPS", r.Texto);
            Assert.DoesNotContain("configurar", r.Texto);
            StringAssert.Contains(r.Texto, "invitado");
        }

        [TestMethod]
        public async Task ConPaginaYaTengoCuentaEntregaUnEnlaceSeguro()
        {
            RespuestaBot r = Unica(await EnviarConAsync(EcoConversador(PaginaRegistro), "Ya tengo cuenta", cuenta: Invitado));

            StringAssert.Contains(r.Texto, "https://ecoaccess.example/registro");
        }

        [TestMethod]
        public async Task LaCuentaVinculadaNoVuelveARegistrarseYSeSaludaDeNuevo()
        {
            var eco = EcoConversador(PaginaRegistro);

            RespuestaBot vinculo = Unica(await EnviarConAsync(eco, "/vincular"));
            RespuestaBot inicio = Unica(await EnviarConAsync(eco, "/start"));

            Assert.AreEqual(MensajesTelegram.YaVinculada, vinculo.Texto);
            CollectionAssert.AreEqual(new[] { "🪙 Mis ecopuntos", "📊 Mi actividad", "🏠 Menú" }, vinculo.Botones.Select(b => b.Texto).ToArray());
            Assert.AreEqual(MensajesConversacion.SaludoDeNuevo, inicio.Texto);
        }

        [TestMethod]
        [DataRow("¿Cuántos ecopuntos tengo?")]
        [DataRow("/ecopuntos")]
        public async Task ElInvitadoNoConsultaEcopuntosSinVincular(string texto)
        {
            RespuestaBot r = Unica(await EnviarConAsync(EcoConversador(), texto, cuenta: Invitado));

            Assert.AreEqual(MensajesConversacion.EcopuntosSinCuenta, r.Texto);
            Assert.AreEqual("👤 Vincular cuenta", r.Botones[0].Texto);
        }

        [TestMethod]
        public async Task CuantosTengoSinContextoPreguntaDeQue()
        {
            Assert.AreEqual(MensajesConversacion.CuantosDeQue, Unica(await EnviarConAsync(EcoConversador(), "¿Cuántos tengo esta semana?")).Texto);
        }

        [TestMethod]
        public async Task EstrellasBajasOfrecenComentarSinObligar()
        {
            var eco = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                opiniones: new ServicioOpiniones(_usuarios, new OpinionesEnMemoria(), _reloj), flujoRapido: true);

            RespuestaBot baja = Unica(await EnviarConAsync(eco, boton: "menu:opinion:1", cuenta: Invitado));

            Assert.AreEqual(MensajesConversacion.ValoracionNegativa, baja.Texto);
            CollectionAssert.AreEqual(new[] { "✍️ Escribir comentario", "Omitir" }, baja.Botones.Select(b => b.Texto).ToArray());
        }

        [TestMethod]
        public async Task EsperarComentarioNoAtrapaUnaSituacionDelicada()
        {
            var eco = EcoConversador();
            await EnviarConAsync(eco, boton: "menu:queja:otro");

            Assert.AreEqual(MensajesConversacion.Urgencia, Unica(await EnviarConAsync(eco, "me quiero morir")).Texto);
            Assert.IsEmpty(_comentarios.Filas, "Una situación delicada no se guarda como comentario.");
        }
    }
}
