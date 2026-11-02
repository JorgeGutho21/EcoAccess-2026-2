using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private async Task<int> AgregarPilotoDirectoAsync(bool enServicio = true)
        {
            var piloto = new EcoAccess.P3.Entidades.Contenedor(1, ServicioCatalogoAmbiental.NombreCanecaPiloto,
                Guid.NewGuid().ToString("N"), _reloj.GetUtcNow().UtcDateTime);
            piloto.AdmitirMaterial(_pet.Id);
            int id = await _contenedores.AgregarAsync(piloto);
            if (enServicio) await _contenedores.CambiarEstadoAsync(id, EcoAccess.P3.Entidades.EstadoContenedor.EnServicio);
            return id;
        }

        [TestMethod]
        public async Task EntradaDirectaOfrecePilotoSinAutorizarYAceptaRegistroExplicito()
        {
            int piloto = await AgregarPilotoDirectoAsync();
            var oferta = Unica(await BotonAsync("menu:depositar"));
            Assert.IsTrue(oferta.Botones.Any(b => b.Dato == "menu:piloto-iniciar"));
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsNull(_conversaciones.Leer(Vinculado)!.IdInteraccion);
            var registro = Unica(await BotonAsync("menu:piloto-iniciar"));
            Assert.AreEqual(piloto, _conversaciones.Leer(Vinculado)!.IdContenedor);
            Assert.IsTrue(registro.Botones.Any(b => b.Dato == MaterialPet));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task EntradaDirectaNoEligeFixturesNiUnPilotoAmbiguo()
        {
            await AgregarPilotoDirectoAsync();
            await AgregarPilotoDirectoAsync();
            Assert.IsFalse(Unica(await BotonAsync("menu:depositar")).Botones.Any(b => b.Dato == "menu:piloto-iniciar"));
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task BotonDePilotoRevalidaDisponibilidadAntesDeCrearSolicitud()
        {
            int piloto = await AgregarPilotoDirectoAsync();
            await BotonAsync("menu:depositar");
            await _contenedores.CambiarEstadoAsync(piloto, EcoAccess.P3.Entidades.EstadoContenedor.FueraDeServicio);
            StringAssert.Contains(Unica(await BotonAsync("menu:piloto-iniciar")).Texto, "no está disponible");
            Assert.IsNull(_conversaciones.Leer(Vinculado)!.IdInteraccion);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task StartSinQrMuestraElMenuPrincipalEnDosColumnas()
        {
            var r = Unica(await TextoAsync("/start"));
            Assert.AreEqual(MensajesTelegram.Bienvenida, r.Texto);
            CollectionAssert.AreEqual(ServicioConversacionTelegram.MenuPrincipal.ToList(), r.Botones.ToList());
            Assert.AreEqual(2, r.Columnas);
        }

        [TestMethod]
        public async Task PilotoConfiguradoIdentificaSimulacionSinAfirmarIngresoFisico()
        {
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, qrDemo: _caneca.IdentificadorQr);
            var respuesta = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true,
                "/start " + _caneca.IdentificadorQr, null)));
            StringAssert.Contains(respuesta.Texto, "Simulación de software");
            StringAssert.Contains(respuesta.Texto, "no demuestran movimiento físico ni ingreso real");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task MaterialEspecialNoSeOfreceNiAceptaPorBotonManipulado()
        {
            var especial = _materiales.Sembrar("JERINGA", "Jeringa usada", EcoAccess.P3.Entidades.CategoriaMaterial.Peligroso);
            await _contenedores.AdmitirMaterialAsync(_caneca.Id, especial.Id);
            var materiales = Unica(await TextoAsync("/start " + _caneca.IdentificadorQr));
            string boton = ServicioConversacionTelegram.PrefijoMaterial + especial.Id;
            Assert.IsFalse(materiales.Botones.Any(b => b.Dato == boton));
            Assert.AreEqual(MensajesTelegram.MaterialNoValido, Unica(await BotonAsync(boton)).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task DemoSinConfigurarNoInventaUnaCaneca()
        {
            Assert.AreEqual(MensajesTelegram.DemoNoDisponible, Unica(await TextoAsync("/demo")).Texto);
            Assert.AreEqual(MensajesTelegram.DemoNoDisponible, Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMenu + "demo-iniciar")).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task BotonDelMenuEquivaleAlComandoYNoAutorizaNada()
        {
            var porBoton = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMenu + "/ecopuntos"));
            var porTexto = Unica(await TextoAsync("/ecopuntos"));
            Assert.AreEqual(porTexto.Texto, porBoton.Texto);
            Assert.AreEqual(MensajesTelegram.ComoDepositar, Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMenu + "depositar")).Texto);
            Assert.AreEqual(MensajesTelegram.NoEntendido, Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMenu + "/confirmar 123")).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task DepositarConQrVigenteRetomaLosMaterialesSinNuevoEscaneo()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            int interaccion = IdInteraccionActual;
            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMenu + "depositar"));
            Assert.AreEqual(MensajesTelegram.ElegirMaterial, r.Texto);
            Assert.IsTrue(r.Botones.Any(b => b.Dato == MaterialPet));
            Assert.AreEqual(interaccion, IdInteraccionActual, "No se crea otra interacción.");
        }

        [TestMethod]
        public async Task FotoAntesDelQrSeAnalizaConElBotonDespuesDelEscaneo()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            });
            Assert.AreEqual(MensajesTelegram.FotoGuardadaSinCaneca, Unica(await EnviarFotoAsync(servicio)).Texto);
            Assert.AreEqual(0, descargas, "Sin caneca no se descarga ni se envía al proveedor.");
            var qr = Unica(await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr));
            Assert.AreEqual(ServicioConversacionTelegram.FotoPrevia, qr.Botones[0].Dato);
            var sugerencia = Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.FotoPrevia));
            Assert.AreEqual(1, descargas);
            Assert.AreEqual(ServicioConversacionTelegram.ConfirmarSi, sugerencia.Botones[0].Dato);
            Assert.IsEmpty(_interacciones.Ordenes, "La sugerencia no autoriza la apertura.");
            StringAssert.Contains(Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.FotoPrevia)).Texto, "venció");
        }

        [TestMethod]
        public async Task FotoPreviaConConsentimientoPideConfirmacionAntesDeDescargar()
        {
            int descargas = 0;
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) =>
            {
                descargas++; return Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png"));
            }, consentimiento: true);
            await EnviarFotoAsync(servicio);
            await EnviarConAsync(servicio, "/start " + _caneca.IdentificadorQr);
            var aviso = Unica(await EnviarConAsync(servicio, boton: ServicioConversacionTelegram.FotoPrevia));
            Assert.AreEqual(MensajesTelegram.AvisoConsentimientoFoto, aviso.Texto);
            Assert.AreEqual(0, descargas);
        }
    }

    [TestClass]
    public sealed class ServicioNotificacionesTelegramTests
    {
        private sealed class RepositorioHechos : IRepositorioNotificacionesTelegram
        {
            public List<HechoNotificable> Hechos { get; } = new List<HechoNotificable>();
            public Task<IReadOnlyList<HechoNotificable>> ListarDesdeAsync(DateTime desdeUtc, CancellationToken cancellationToken = default) =>
                Task.FromResult<IReadOnlyList<HechoNotificable>>(Hechos.Where(h => h.FechaUtc >= desdeUtc).ToList());
        }

        private sealed class RegistroEnMemoria : IRegistroNotificacionesEnviadas
        {
            public EstadoNotificaciones? Guardado { get; private set; }
            public EstadoNotificaciones? Cargar() => Guardado is null ? null
                : new EstadoNotificaciones(new(Guardado.Enviadas), new(Guardado.Fallos));
            public void Guardar(EstadoNotificaciones estado) => Guardado = new EstadoNotificaciones(new(estado.Enviadas), new(estado.Fallos));
        }

        private static readonly DateTimeOffset Ahora = new(2026, 10, 29, 20, 0, 0, TimeSpan.Zero);
        private readonly RepositorioHechos _hechos = new();
        private readonly RegistroEnMemoria _registro = new();
        private readonly List<RespuestaBot> _enviados = new List<RespuestaBot>();

        private static HechoNotificable Hecho(string clave, TipoHechoNotificable tipo, int minutos = -1) =>
            new(clave, tipo, 700_000_001, 1751, 1252, "PET", tipo == TipoHechoNotificable.EcopuntosAcreditados ? 10 : null,
                tipo == TipoHechoNotificable.EcopuntosAcreditados ? 10 : null, Ahora.UtcDateTime.AddMinutes(minutos));

        private ServicioNotificacionesTelegram Servicio() => new(_hechos, _registro, new RelojFijo(Ahora));

        private Task<int> RevisarAsync(ServicioNotificacionesTelegram s, Func<RespuestaBot, Task>? enviar = null) =>
            s.RevisarAsync((r, _) => enviar?.Invoke(r) ?? Task.Run(() => _enviados.Add(r)), _ => { }, CancellationToken.None);

        [TestMethod]
        public async Task PrimerArranqueNoReenviaLoAnteriorYLuegoAvisaUnaSolaVez()
        {
            _hechos.Hechos.Add(Hecho("de:1", TipoHechoNotificable.DepositoRegistrado));
            var servicio = Servicio();
            Assert.AreEqual(0, await RevisarAsync(servicio));
            _hechos.Hechos.Add(Hecho("de:2", TipoHechoNotificable.DepositoRegistrado));
            Assert.AreEqual(1, await RevisarAsync(servicio));
            Assert.AreEqual(0, await RevisarAsync(servicio), "Sin duplicados en la siguiente revisión.");
            Assert.AreEqual(0, await RevisarAsync(Servicio()), "Tras reiniciar se respeta lo ya enviado.");
            Assert.HasCount(1, _enviados);
            StringAssert.Contains(_enviados[0].Texto, "pendiente de verificación");
            Assert.AreEqual(700_000_001, _enviados[0].ChatId);
        }

        [TestMethod]
        public async Task FalloDeTelegramReintentaYSeDescartaTrasElLimite()
        {
            var servicio = Servicio();
            await RevisarAsync(servicio);
            _hechos.Hechos.Add(Hecho("mo:7", TipoHechoNotificable.EcopuntosAcreditados));
            for (int i = 0; i < ServicioNotificacionesTelegram.IntentosMaximos; i++)
                Assert.AreEqual(0, await RevisarAsync(servicio, _ => throw new HttpRequestException("sin red")));
            Assert.AreEqual(0, await RevisarAsync(servicio), "Tras el límite ya no se reintenta.");
            Assert.IsEmpty(_enviados);
        }

        [TestMethod]
        public async Task ChatInexistenteSeDescartaSinReintentar()
        {
            var servicio = Servicio();
            await RevisarAsync(servicio);
            _hechos.Hechos.Add(Hecho("de:5", TipoHechoNotificable.DepositoRegistrado));
            int intentos = 0;
            await RevisarAsync(servicio, _ => { intentos++; throw new DestinoNotificacionNoDisponibleException(); });
            await RevisarAsync(servicio, _ => { intentos++; return Task.CompletedTask; });
            Assert.AreEqual(1, intentos, "Un destino inexistente no se reintenta.");
        }

        [TestMethod]
        public async Task FalloTransitorioSeEntregaDespues()
        {
            var servicio = Servicio();
            await RevisarAsync(servicio);
            _hechos.Hechos.Add(Hecho("ev:9", TipoHechoNotificable.Apertura));
            await RevisarAsync(servicio, _ => throw new HttpRequestException("sin red"));
            Assert.AreEqual(1, await RevisarAsync(servicio));
            StringAssert.Contains(_enviados.Single().Texto, "La caneca está abierta. Tienes 20 segundos");
        }

        [TestMethod]
        public void MensajeDeEcopuntosUsaLosDatosDeOracle()
        {
            var r = ServicioNotificacionesTelegram.Mensaje(Hecho("mo:1", TipoHechoNotificable.EcopuntosAcreditados));
            StringAssert.Contains(r.Texto, "Recibiste 10 ecopuntos");
            StringAssert.Contains(r.Texto, "Saldo actual: 10");
            Assert.AreEqual(ServicioConversacionTelegram.PrefijoMenu + "/ecopuntos", r.Botones.Single().Dato);
        }

        private sealed class RelojFijo : TimeProvider
        {
            private readonly DateTimeOffset _ahora;

            public RelojFijo(DateTimeOffset ahora)
            {
                _ahora = ahora;
            }

            public override DateTimeOffset GetUtcNow() => _ahora;
        }
    }
}
