using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>Autorización para enviar fotos fuera del equipo, con el selector real y motores falsos. No es Telegram ni Groq reales.</summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class ConsentimientosEnMemoria : IRepositorioConsentimientoVision
        {
            private readonly Dictionary<long, string> _vigentes = new();
            public bool FallaOracle { get; set; }
            public Task<bool> EstaVigenteAsync(long id, string version, CancellationToken ct = default)
            {
                if (FallaOracle) throw new InvalidOperationException("Oracle no responde.");
                return Task.FromResult(_vigentes.TryGetValue(id, out string? v) && v == version);
            }
            public Task RegistrarAsync(long id, string version, DateTime fechaUtc, CancellationToken ct = default) { _vigentes[id] = version; return Task.CompletedTask; }
            public Task<bool> RevocarAsync(long id, DateTime fechaUtc, CancellationToken ct = default) => Task.FromResult(_vigentes.Remove(id));
        }

        private sealed class MotorFalso : IClasificadorMaterial
        {
            private readonly Func<PropuestaVision> _responder;
            public MotorFalso(string nombre, Func<PropuestaVision> responder) { Proveedor = nombre; _responder = responder; }
            public string Proveedor { get; }
            public List<EnvioExterno> Envios { get; } = new();
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            {
                Envios.Add(imagen.Envio);
                return Task.FromResult(_responder());
            }
        }

        private readonly ConsentimientosEnMemoria _consentimientos = new();

        private PropuestaVision PropuestaPet() => new(TipoPropuestaVision.MaterialIdentificado, _pet.Codigo, 0.95m);

        private static PropuestaVision MotorCaido() => throw new InvalidOperationException("motor caído");

        private ServicioConversacionTelegram ConMotores(ModoVision modo, MotorFalso? local, MotorFalso? externo)
        {
            var selector = new SelectorVision(modo, local, externo, TimeSpan.FromSeconds(30), _ => { });
            return new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                vision: new ServicioClasificacionImagen(selector, _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")),
                flujoRapido: true, consentimientosVision: _consentimientos, politicaExterna: selector.Politica);
        }

        private async Task<RespuestaBot> FotoConCanecaAsync(ServicioConversacionTelegram servicio)
        {
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            return Unica(await EnviarFotoAsync(servicio));
        }

        private static string BotonDe(RespuestaBot respuesta, string prefijo) => respuesta.Botones.Single(b => b.Dato.StartsWith(prefijo, StringComparison.Ordinal)).Dato;

        [TestMethod]
        public async Task Groq_SinAutorizacionLaFotoNoSaleHastaPulsarElBoton()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Groq, null, externo);

            RespuestaBot aviso = await FotoConCanecaAsync(servicio);
            Assert.AreEqual(MensajesTelegram.AvisoFotoExterna, aviso.Texto);
            Assert.IsEmpty(externo.Envios);
            Assert.IsFalse(await _consentimientos.EstaVigenteAsync(Vinculado, MensajesTelegram.VersionAvisoExterno));

            var respuesta = Unica(await EnviarConAsync(servicio, boton: BotonDe(aviso, ServicioConversacionTelegram.PrefijoAutorizarExterna)));

            Assert.AreEqual(EnvioExterno.Autorizado, externo.Envios.Single());
            Assert.IsTrue(await _consentimientos.EstaVigenteAsync(Vinculado, MensajesTelegram.VersionAvisoExterno));
            Assert.AreEqual("✅ Sí, continuar", respuesta.Botones[0].Texto);
            Assert.IsEmpty(_interacciones.Ordenes, "La autorización de la foto no abre la caneca.");
        }

        [TestMethod]
        public async Task Groq_LaAutorizacionSeReutilizaYRevocarlaDetieneLosEnvios()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Groq, null, externo);
            await _consentimientos.RegistrarAsync(Vinculado, MensajesTelegram.VersionAvisoExterno, _reloj.GetUtcNow().UtcDateTime);

            await FotoConCanecaAsync(servicio);
            Assert.HasCount(1, externo.Envios);

            Assert.AreEqual(MensajesTelegram.AutorizacionRevocada, Unica(await EnviarConAsync(servicio, "/revocarfotos")).Texto);
            Assert.AreEqual(MensajesTelegram.SinAutorizacionExterna, Unica(await EnviarConAsync(servicio, "/revocarfotos")).Texto);

            RespuestaBot aviso = Unica(await EnviarFotoAsync(servicio));
            Assert.AreEqual(MensajesTelegram.AvisoFotoExterna, aviso.Texto);
            Assert.HasCount(1, externo.Envios);
        }

        [TestMethod]
        public async Task Groq_UnaAutorizacionDeOtraCuentaNoSirve()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Groq, null, externo);
            await _consentimientos.RegistrarAsync(Invitado, MensajesTelegram.VersionAvisoExterno, _reloj.GetUtcNow().UtcDateTime);
            Assert.AreEqual(MensajesTelegram.AvisoFotoExterna, (await FotoConCanecaAsync(servicio)).Texto);
            Assert.IsEmpty(externo.Envios);
        }

        [TestMethod]
        public async Task Automatico_LocalCaidoSinAutorizacion_PreguntaYNoEnvia()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("local", MotorCaido), externo);

            RespuestaBot aviso = await FotoConCanecaAsync(servicio);

            Assert.AreEqual(MensajesTelegram.AvisoRespaldoExterno, aviso.Texto);
            Assert.IsTrue(aviso.Botones.Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            Assert.IsEmpty(externo.Envios);

            await EnviarConAsync(servicio, boton: BotonDe(aviso, ServicioConversacionTelegram.PrefijoAutorizarExterna));
            Assert.AreEqual(EnvioExterno.Autorizado, externo.Envios.Single());
        }

        [TestMethod]
        public async Task Automatico_LocalCaidoConAutorizacion_UsaElRespaldoSinPreguntar()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("local", MotorCaido), externo);
            await _consentimientos.RegistrarAsync(Vinculado, MensajesTelegram.VersionAvisoExterno, _reloj.GetUtcNow().UtcDateTime);

            RespuestaBot respuesta = await FotoConCanecaAsync(servicio);

            Assert.HasCount(1, externo.Envios);
            Assert.AreEqual("✅ Sí, continuar", respuesta.Botones[0].Texto);
        }

        [TestMethod]
        public async Task Automatico_FotoAmbigua_NoSaleYOfreceSegundaOpinion()
        {
            var local = new MotorFalso("local", () => new PropuestaVision(TipoPropuestaVision.Incierta, null, null));
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, local, externo);
            await _consentimientos.RegistrarAsync(Vinculado, MensajesTelegram.VersionAvisoExterno, _reloj.GetUtcNow().UtcDateTime);

            RespuestaBot ambigua = await FotoConCanecaAsync(servicio);

            Assert.IsEmpty(externo.Envios, "Aun con autorización, la incertidumbre no envía la foto sola.");
            await EnviarConAsync(servicio, boton: BotonDe(ambigua, ServicioConversacionTelegram.PrefijoSegundaOpinion));
            Assert.AreEqual(EnvioExterno.SegundaOpinion, externo.Envios.Single());
            Assert.HasCount(1, local.Envios);
        }

        [TestMethod]
        public async Task Automatico_SegundaOpinionSinAutorizacion_PideAutorizarPrimero()
        {
            var local = new MotorFalso("local", () => new PropuestaVision(TipoPropuestaVision.Incierta, null, null));
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Automatico, local, externo);

            RespuestaBot ambigua = await FotoConCanecaAsync(servicio);
            RespuestaBot aviso = Unica(await EnviarConAsync(servicio, boton: BotonDe(ambigua, ServicioConversacionTelegram.PrefijoSegundaOpinion)));

            Assert.AreEqual(MensajesTelegram.AvisoSegundaOpinion, aviso.Texto);
            Assert.IsEmpty(externo.Envios);
            await EnviarConAsync(servicio, boton: BotonDe(aviso, ServicioConversacionTelegram.PrefijoAutorizarExterna));
            Assert.AreEqual(EnvioExterno.SegundaOpinion, externo.Envios.Single());
        }

        [TestMethod]
        public async Task AmbosMotoresCaidos_QuedaLaSeleccionManualSinOrdenes()
        {
            var servicio = ConMotores(ModoVision.Automatico, new MotorFalso("local", MotorCaido), new MotorFalso("groq:modelo", MotorCaido));
            await _consentimientos.RegistrarAsync(Vinculado, MensajesTelegram.VersionAvisoExterno, _reloj.GetUtcNow().UtcDateTime);

            RespuestaBot respuesta = await FotoConCanecaAsync(servicio);

            Assert.IsTrue(respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsNotNull(Unica(await EnviarConAsync(servicio, "/materiales")).Texto, "El catálogo sigue disponible sin IA.");
        }

        [TestMethod]
        public async Task SinPoderConsultarLaAutorizacion_LaFotoNoSale()
        {
            var externo = new MotorFalso("groq:modelo", PropuestaPet);
            var servicio = ConMotores(ModoVision.Groq, null, externo);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            _consentimientos.FallaOracle = true;

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => EnviarFotoAsync(servicio));

            Assert.IsEmpty(externo.Envios);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public void PoliticaExternaSinRepositorio_NoSePuedeConstruir()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, politicaExterna: PoliticaVisionExterna.Principal));
        }

        [TestMethod]
        public async Task ArchivoNoCompatible_RecibeExplicacionEnVezDeSilencio()
        {
            var servicio = ConMotores(ModoVision.Local, new MotorFalso("local", PropuestaPet), null);
            var respuestas = await servicio.ProcesarAsync(new MensajeEntrante(Interlocked.Increment(ref _update), Vinculado, Vinculado, true, null, null, null, true));
            Assert.AreEqual(MensajesTelegram.ArchivoNoCompatible, Unica(respuestas).Texto);
        }
    }
}
