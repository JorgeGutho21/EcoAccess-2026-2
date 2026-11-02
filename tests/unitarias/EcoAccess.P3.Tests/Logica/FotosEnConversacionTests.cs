using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using EcoAccess.P3.Bot.Telegram;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        // Firma de prueba para el simulador de clasificación; las pruebas del decodificador usan imágenes reales.
        private static readonly byte[] FotoDelDoble = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0 };

        private ServicioConversacionTelegram ConVision(TipoPropuestaVision tipo,
            Func<string, CancellationToken, Task<ImagenResiduo>> descargar, string? codigo = null, bool consentimiento = false, bool flujoRapido = false)
        {
            var clasificador = new ClasificadorMaterialSimulado();
            clasificador.Preparar(FotoDelDoble, new PropuestaVision(tipo, codigo ?? _pet.Codigo, 0.95m));
            return new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                vision: new ServicioClasificacionImagen(clasificador, _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: descargar, requiereConsentimientoFoto: consentimiento, flujoRapido: flujoRapido);
        }

        private Task<IReadOnlyList<RespuestaBot>> EnviarFotoAsync(ServicioConversacionTelegram servicio, long cuenta = Vinculado, bool privado = true, long? update = null) =>
            servicio.ProcesarAsync(new MensajeEntrante(update ?? Interlocked.Increment(ref _update), cuenta, cuenta, privado, null, null, "archivo-ficticio"));

        [TestMethod]
        public async Task FlujoRapido_SaludaPideFotoYConfirmaAntesDeEmitirOrden()
        {
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), flujoRapido: true);
            var saludo = Unica(await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr));
            StringAssert.StartsWith(saludo.Texto, MensajesTelegram.SaludoEco);
            Assert.AreEqual("📸 Enviar foto", saludo.Botones[0].Texto);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            Assert.AreEqual("✅ Sí, continuar", respuesta.Botones[0].Texto);
            Assert.AreEqual("✏️ Corregir material", respuesta.Botones[1].Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
            await EnviarConAsync(servicio, boton: respuesta.Botones[0].Dato);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        public async Task FlujoRapido_ConfirmacionRepetidaNoEmiteOtraOrden()
        {
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), flujoRapido: true);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            await EnviarConAsync(servicio, boton: respuesta.Botones[0].Dato);
            await EnviarConAsync(servicio, boton: respuesta.Botones[0].Dato);
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task FlujoRapido_CorregirMaterialMuestraListaYNoEmiteOrden()
        {
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), flujoRapido: true);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            var lista = Unica(await EnviarConAsync(servicio, boton: respuesta.Botones[1].Dato));
            Assert.IsTrue(lista.Botones.Any(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoMaterial)));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow(TipoPropuestaVision.Incierta)]
        [DataRow(TipoPropuestaVision.ObjetoDesconocido)]
        public async Task FlujoRapido_ClasificacionInciertaNoAbre(TipoPropuestaVision tipo)
        {
            var servicio = ConVision(tipo, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), flujoRapido: true);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            Assert.IsTrue(respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            Assert.IsEmpty(_interacciones.Ordenes);
        }


        [TestMethod]
        public async Task FotoIdentificadaGuardaSugerenciaPeroNecesitaConfirmacionParaEmitirOrden()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            Assert.AreEqual(1, descargas);
            Assert.AreEqual(ServicioConversacionTelegram.ConfirmarSi, respuesta.Botones[0].Dato);
            var interaccion = _interacciones.LeerInteraccion(IdInteraccionActual)!;
            Assert.AreEqual(_pet.Id, interaccion.IdMaterialSugerido);
            Assert.AreEqual(0.95m, interaccion.ConfianzaSugerida);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
            await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        [DataRow(TipoPropuestaVision.Incierta)]
        [DataRow(TipoPropuestaVision.ImagenBorrosa)]
        [DataRow(TipoPropuestaVision.ObjetoDesconocido)]
        [DataRow(TipoPropuestaVision.VariosObjetos)]
        [DataRow(TipoPropuestaVision.SinResiduo)]
        public async Task AbstencionInvalidaLaSeleccionAnteriorYSusBotones(TipoPropuestaVision tipo)
        {
            var servicio = ConVision(tipo, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")));
            await PrepararConAsync(servicio);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            Assert.IsFalse(respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            Assert.IsTrue(respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.PrefijoMenu + "/manual"));
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.AreEqual(MensajesTelegram.NadaQueConfirmar, Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi)).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task InvitadoGrupoYUpdateDuplicadoNoDescarganFotografias()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.Incierta, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            Assert.IsEmpty(await EnviarFotoAsync(servicio, privado: false));
            Assert.AreEqual(MensajesTelegram.FotoGuardadaSinCaneca, Unica(await EnviarFotoAsync(servicio, Invitado)).Texto);
            // Sin QR la foto se guarda sin descargarla, para analizarla después del escaneo.
            Assert.AreEqual(MensajesTelegram.FotoGuardadaSinCaneca, Unica(await EnviarFotoAsync(servicio)).Texto);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            long id = Interlocked.Increment(ref _update);
            await EnviarFotoAsync(servicio, update: id);
            Assert.IsEmpty(await EnviarFotoAsync(servicio, update: id));
            Assert.AreEqual(1, descargas);
        }

        [TestMethod]
        [DataRow(false, true)]
        [DataRow(true, false)]
        public async Task CuentaDesactivadaOSinConsentimientoNoConsumeProveedor(bool activa, bool consentimiento)
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.Incierta, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            await PrepararConAsync(servicio);
            _usuarios.AgregarCiudadano(Ciudadano, consentimiento: consentimiento, activo: activa);
            await EnviarFotoAsync(servicio);
            Assert.AreEqual(0, descargas);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CuartaFotoEnUnMinutoNoSeDescarga()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.Incierta, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            await PrepararConAsync(servicio);
            for (int i = 0; i < 3; i++) await EnviarFotoAsync(servicio);
            StringAssert.Contains(Unica(await EnviarFotoAsync(servicio)).Texto, "límite de fotografías");
            Assert.AreEqual(3, descargas);
        }

        [TestMethod]
        public async Task PeligrosoAdmitidoAvisaYPideConfirmarSinAbrir()
        {
            var pila = _materiales.Sembrar("PILA", "Pila ficticia", CategoriaMaterial.Peligroso);
            var especial = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id, pila.Id);
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), pila.Codigo);
            await EnviarConAsync(servicio, "/start " + especial.IdentificadorQr);
            var respuesta = Unica(await EnviarFotoAsync(servicio));
            Assert.IsTrue(respuesta.Botones.Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            StringAssert.Contains(respuesta.Texto, "peligroso");
            StringAssert.Contains(respuesta.Texto, "¿Es correcto?");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task ConfirmacionViejaEsperaLaFotoYNoAutorizaUnaSugerenciaIncierta()
        {
            var entro = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var liberar = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var servicio = ConVision(TipoPropuestaVision.Incierta, async (_, ct) =>
            {
                entro.SetResult(true); await liberar.Task.WaitAsync(TimeSpan.FromSeconds(2), ct);
                return new ImagenResiduo(FotoDelDoble, "image/png");
            });
            await PrepararConAsync(servicio);
            var foto = EnviarFotoAsync(servicio);
            await entro.Task.WaitAsync(TimeSpan.FromSeconds(1));
            var confirmar = EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.IsFalse(confirmar.IsCompleted, "La cuenta no procesa el botón mientras clasifica su foto.");
            // Otra cuenta usa otra banda y puede consultar sin esperar la foto.
            Assert.AreEqual(MensajesTelegram.Ayuda, Unica(await EnviarConAsync(servicio, "/ayuda", cuenta: Invitado)).Texto);
            liberar.SetResult(true);
            await foto;
            Assert.AreEqual(MensajesTelegram.NadaQueConfirmar, Unica(await confirmar).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public void TraductorSeleccionaMejorFotoQueRespetaLimiteYDescartaMetadatosInvalidos()
        {
            using var json = JsonDocument.Parse(@"{""update_id"":10,""message"":{""from"":{""id"":7},""chat"":{""id"":7,""type"":""private""},""photo"":[null,{""file_id"":""invalida"",""file_size"":""enorme""},{""file_id"":""pequeña"",""width"":100,""height"":100,""file_size"":100},{""file_id"":""buena"",""width"":800,""height"":600,""file_size"":1000},{""file_id"":""grande"",""width"":900,""height"":900,""file_size"":6000000}]}}");
            var mensaje = TraductorUpdates.Traducir(json.RootElement).Mensaje;
            Assert.IsNotNull(mensaje); Assert.AreEqual("buena", mensaje.ArchivoFoto); Assert.IsNull(mensaje.Texto);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task FalloPosteriorALaInvalidacionPersistidaNoReviveBotonesAnteriores(bool fallaSugerencia)
        {
            var servicio = ConVision(fallaSugerencia ? TipoPropuestaVision.MaterialIdentificado : TipoPropuestaVision.Incierta, (_, _) =>
            {
                if (fallaSugerencia) _interacciones.AlEscribir = () => throw new IOException("Fallo de sugerencia simulado.");
                else _conversaciones.AlEscribir = () => throw new IOException("Fallo de diálogo después de la foto.");
                return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            await PrepararConAsync(servicio);
            await Assert.ThrowsExactlyAsync<IOException>(() => EnviarFotoAsync(servicio));
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.AreEqual(MensajesTelegram.NadaQueConfirmar, Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.ConfirmarSi)).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }
    }
}
