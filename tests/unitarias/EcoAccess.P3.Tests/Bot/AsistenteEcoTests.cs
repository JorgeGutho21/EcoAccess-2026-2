using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class AsistenteEcoTests
    {
        private static MensajeEntrante Mensaje(string texto, long persona = 700) => new(1, persona, persona, true, texto, null);

        [TestMethod]
        [DataRow("¿Cuántos ecopuntos tengo?", "/ecopuntos")]
        [DataRow("Mis depósitos", "/depositos")]
        [DataRow("¿Por qué mi depósito está pendiente?", "/estado")]
        [DataRow("¿Qué materiales acepta?", "/materiales")]
        [DataRow("¿Qué hago con una pila?", "pila")]
        [DataRow("¿Dónde deposito cartón?", "cartón")]
        [DataRow("¿Dónde veo mis depósitos?", "/depositos")]
        public async Task IntencionConsultaLaBllSinInventarDatos(string pregunta, string esperado)
        {
            MensajeEntrante? atendido = null;
            var eco = new AsistenteEco((m, _) => { atendido = m; return Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new(m.ChatId, "dato real del servicio") }); });
            var resultado = await eco.ProcesarAsync(Mensaje(pregunta));
            Assert.AreEqual(esperado, atendido!.Texto);
            Assert.AreEqual(1L, atendido.UpdateId);
            Assert.AreEqual(700L, atendido.TelegramUserId);
            Assert.AreEqual("dato real del servicio", resultado.Single().Texto);
        }

        [TestMethod]
        [DataRow("¿Cómo funciona?", "escanea el QR")]
        [DataRow("¿Qué hago si no reconoce mi fotografía?", "/manual")]
        [DataRow("¿Puedo participar sin registrarme?", "los invitados no reciben puntos")]
        public async Task PreguntaFrecuenteRespondeSinConsultarDatosPersonales(string pregunta, string fragmento)
        {
            var eco = new AsistenteEco((m, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new(m.ChatId, MensajesTelegram.NoEntendido) }));
            string texto = (await eco.ProcesarAsync(Mensaje(pregunta))).Single().Texto;
            StringAssert.Contains(texto, fragmento);
        }

        [TestMethod]
        public async Task InformacionNoResucitaUpdatesDescartadosNiReemplazaErrores()
        {
            var eco = new AsistenteEco((_, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()));
            Assert.IsEmpty(await eco.ProcesarAsync(Mensaje("¿Qué es EcoAccess?")));
            eco = new((m, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new(m.ChatId, MensajesTelegram.DemasiadosMensajes) }));
            Assert.AreEqual(MensajesTelegram.DemasiadosMensajes, (await eco.ProcesarAsync(Mensaje("hola"))).Single().Texto);
        }

        [TestMethod]
        [DataRow("Volver al inicio", "menu:inicio")]
        [DataRow("Quiero depositar", "menu:depositar")]
        [DataRow("Mi cuenta", "menu:cuenta")]
        public async Task NavegacionNaturalReutilizaElCallbackExistente(string texto, string callback)
        {
            MensajeEntrante? visto = null;
            var eco = new AsistenteEco((m, _) => { visto = m; return Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()); });
            await eco.ProcesarAsync(Mensaje(texto));
            Assert.AreEqual(callback, visto!.DatoBoton);
            Assert.IsNull(visto.Texto);
        }

        [TestMethod]
        public async Task FotoBotonYComandoPrivadoNoSeInterpretanComoPregunta()
        {
            var vistos = new List<MensajeEntrante>();
            var eco = new AsistenteEco((m, _) => { vistos.Add(m); return Task.FromResult<IReadOnlyList<RespuestaBot>>(Array.Empty<RespuestaBot>()); });
            var foto = Mensaje("hola") with { ArchivoFoto = "referencia-sintetica" };
            var boton = Mensaje("hola") with { DatoBoton = "conf:si" };
            var confirmar = Mensaje("/confirmar codigo-sintetico");
            foreach (var m in new[] { foto, boton, confirmar }) await eco.ProcesarAsync(m);
            CollectionAssert.AreEqual(new[] { foto, boton, confirmar }, vistos.ToArray());
        }

        [TestMethod]
        public async Task IdentidadInvalidaNoLlamaAlServicio()
        {
            var eco = new AsistenteEco((_, _) => throw new AssertFailedException("No debe atender."));
            Assert.IsEmpty(await eco.ProcesarAsync(Mensaje("hola") with { ChatPrivado = false }));
            Assert.IsEmpty(await eco.ProcesarAsync(Mensaje("hola") with { ChatId = 701 }));
            Assert.IsEmpty(await eco.ProcesarAsync(Mensaje("hola", 0)));
        }

        [TestMethod]
        public async Task MensajeFotoBreveConservaPrivacidadYConsentimiento()
        {
            var eco = new AsistenteEco((m, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new(m.ChatId, MensajesTelegram.GuiaFoto) }));
            var r = (await eco.ProcesarAsync(Mensaje("/foto"))).Single();

            Assert.IsTrue(r.Botones.Any(b => b.Dato == "eco:privacidad"));
            eco = new((m, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[]
    {
        new(m.ChatId, MensajesTelegram.AvisoConsentimientoFoto,
                new BotonBot[] { new("etiqueta larga", ServicioConversacionTelegram.PrefijoConsentirFoto + "nonce"), new("Cancelar", ServicioConversacionTelegram.ConfirmarNo) })
    }));
            r = (await eco.ProcesarAsync(Mensaje("foto"))).Single();
            Assert.AreEqual(MensajesTelegram.AvisoConsentimientoFoto, r.Texto);
            Assert.AreEqual(ServicioConversacionTelegram.PrefijoConsentirFoto + "nonce", r.Botones[0].Dato);
            Assert.IsLessThan(25, r.Botones[0].Texto.Length);
            Assert.AreEqual("Cancelar", r.Botones[1].Texto);
        }

        [TestMethod]
        public async Task ProgresoUnaVezPorDescargaValidadaYSeparadoPorConversacion()
        {
            var avisos = new System.Collections.Concurrent.ConcurrentBag<RespuestaBot>();
            AsistenteEco? eco = null;
            eco = new(async (m, ct) =>
            {
                if (m.ArchivoFoto is not null)
                {
                    await Task.Yield();
                    await eco!.NotificarAnalisisFotoAsync(ct);
                    await eco.NotificarAnalisisFotoAsync(ct);
                }
                return Array.Empty<RespuestaBot>();
            }, (r, _) => { avisos.Add(r); return Task.CompletedTask; });
            await Task.WhenAll(Enumerable.Range(1, 8).Select(i => eco.ProcesarAsync(Mensaje("", i) with { ArchivoFoto = "foto" })));
            Assert.HasCount(8, avisos);
            Assert.AreEqual(8, avisos.Select(r => r.ChatId).Distinct().Count());
            await eco.ProcesarAsync(Mensaje("consentimiento inválido") with { DatoBoton = "foto:consentir:viejo" });
            Assert.HasCount(8, avisos);
            await eco.NotificarAnalisisFotoAsync(CancellationToken.None);
            Assert.HasCount(8, avisos);
        }

        [TestMethod]
        public async Task RegistroQueFallaNoInterrumpeLaRespuesta()
        {
            var cliente = new ClienteTelegramSimulado();
            var p = new ProcesadorUpdates((m, _) => Task.FromResult<IReadOnlyList<RespuestaBot>>(new RespuestaBot[] { new(m.ChatId, "respuesta") }), cliente,
                _ => throw new IOException("fallo sintético"));
            await p.ProcesarAsync(new(1, Mensaje("hola"), null), CancellationToken.None);
            Assert.AreEqual("respuesta", cliente.Enviadas.Single().Texto);
        }
    }
}
