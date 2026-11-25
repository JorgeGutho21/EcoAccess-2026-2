using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.EstacionWeb;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Estación web → orden en Oracle (aquí, en memoria). El «bridge» se imita registrando los mismos eventos que registra
    /// el bridge real; nada en estas pruebas llega a un puerto COM ni a un ESP32.
    /// </summary>
    [TestClass]
    public sealed class EstacionWebTests
    {
        private static readonly DateTimeOffset Inicio = new(2026, 11, 24, 15, 0, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioInteracciones _interacciones;
        private readonly Material _pet;
        private readonly Material _organico;
        private readonly Contenedor _blanca;
        private readonly Contenedor _verde;
        private readonly Contenedor _negra;
        private Func<Task> _bridge = () => Task.CompletedTask;

        public EstacionWebTests()
        {
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _organico = _materiales.Sembrar("ORGANICO");
            _blanca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _verde = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _organico.Id);
            _negra = _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, _pet.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _interacciones = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
        }

        private ServicioAperturasWeb Servicio(Dictionary<string, int>? materiales = null) => new(
            _interacciones, _contenedores, _materiales,
            new Dictionary<string, int> { ["blanca"] = _blanca.Id, ["verde"] = _verde.Id, ["negra"] = _negra.Id },
            materiales, _reloj, TimeSpan.FromSeconds(20), TimeSpan.FromMilliseconds(500),
            async (t, ct) => { _reloj.Avanzar(t); await _bridge(); });

        /// <summary>Lo que hace el bridge real: marca la orden enviada y registra acuse y apertura.</summary>
        private Func<Task> BridgeQueAbre(params TipoEventoDispositivo[] eventos) => async () =>
        {
            var orden = _repositorio.Ordenes.SingleOrDefault(o => o.Estado is EstadoOrdenApertura.Emitida);
            if (orden is null) return;
            Assert.IsTrue((await _interacciones.MarcarEnviadaAsync(orden.Id)).Exitoso);
            foreach (var tipo in eventos)
                await _interacciones.RegistrarEventoAsync(new EventoRecibido(orden.IdContenedor, $"usb:{orden.Id}:{tipo}", tipo, orden.Id, _reloj.GetUtcNow().UtcDateTime));
        };

        [TestMethod]
        public async Task TocarLaVerde_EmiteUnaOrdenSoloParaLaVerde_YAceptaCuandoElBridgeInformaLaApertura()
        {
            _bridge = BridgeQueAbre(TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura);

            var r = await Servicio().SolicitarAsync(new("3f1c2a9e-0b7d-4c55-9a11-2f0e8c1d7b42", _verde.Id, "verde"));

            Assert.IsTrue(r.Aceptada, r.Mensaje);
            Assert.AreEqual(MensajesTelegram.VentanaDepositoSegundos * 1000, r.AbiertoMs);
            var orden = _repositorio.Ordenes.Single();
            Assert.AreEqual(_verde.Id, orden.IdContenedor);
            Assert.AreEqual("web-3f1c2a9e-0b7d-4c55-9a11-2f0e8c1d7b42", orden.ClaveIdempotencia);
            var interaccion = _repositorio.LeerInteraccion(orden.IdInteraccion)!;
            Assert.IsNull(interaccion.IdUsuario, "La estación web es un visitante: nunca acumula ecopuntos.");
            Assert.AreEqual(CanalInteraccion.Simulador, interaccion.Canal);
            Assert.AreEqual(_organico.Id, interaccion.IdMaterialConfirmado);
        }

        [TestMethod]
        public async Task SinConfirmacionDelBridge_NoSeDaPorAbierta_YLaOrdenQuedaParaVencerSola()
        {
            var r = await Servicio().SolicitarAsync(new("solicitud-sin-bridge", _blanca.Id, "blanca"));

            Assert.IsFalse(r.Aceptada);
            StringAssert.Contains(r.Mensaje, "no confirmó");
            Assert.AreEqual(EstadoOrdenApertura.Emitida, _repositorio.Ordenes.Single().Estado);
        }

        [TestMethod]
        public async Task FalloInformadoPorLaCaneca_NoSeAcepta()
        {
            _bridge = BridgeQueAbre(TipoEventoDispositivo.Fallo);

            var r = await Servicio().SolicitarAsync(new("solicitud-con-fallo", _blanca.Id, "blanca"));

            Assert.IsFalse(r.Aceptada);
            StringAssert.Contains(r.Mensaje, "fallo");
        }

        [TestMethod]
        public async Task CanecaFueraDeServicio_NoCreaSolicitudNiOrden()
        {
            var r = await Servicio().SolicitarAsync(new("solicitud-negra-1", _negra.Id, "negra"));

            Assert.IsFalse(r.Aceptada);
            StringAssert.Contains(r.Mensaje, "fuera de servicio");
            Assert.AreEqual(0, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        [DataRow("verde", 1)]            // el id de la blanca con el color verde
        [DataRow("azul", 0)]             // color que no existe en la estación
        [DataRow("blanca", 999)]         // caneca ajena a la estación
        public async Task ColorYCanecaDebenCoincidirConLaEstacion(string color, int indice)
        {
            int id = indice switch { 0 => _blanca.Id, 1 => _blanca.Id, _ => indice };

            var r = await Servicio().SolicitarAsync(new("solicitud-ajena-1", id, color));

            Assert.IsFalse(r.Aceptada);
            Assert.AreEqual(0, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        [DataRow("corta")]
        [DataRow("con espacios dentro")]
        [DataRow("comillas\"inyeccion")]
        public async Task SolicitudMalFormada_SeRechazaSinTocarOracle(string solicitud)
        {
            var r = await Servicio().SolicitarAsync(new(solicitud, _blanca.Id, "blanca"));

            Assert.IsFalse(r.Aceptada);
            Assert.AreEqual(0, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        public async Task ReintentarLaMismaSolicitud_NoEmiteUnaSegundaOrden()
        {
            var servicio = Servicio();
            await servicio.SolicitarAsync(new("solicitud-repetida", _blanca.Id, "blanca"));
            _bridge = BridgeQueAbre(TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura);

            var r = await servicio.SolicitarAsync(new("solicitud-repetida", _blanca.Id, "blanca"));

            Assert.IsTrue(r.Aceptada, r.Mensaje);
            Assert.AreEqual(1, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        public async Task OtraSolicitudMientrasLaCanecaTieneUnaOrdenVigente_SeRechaza()
        {
            var servicio = Servicio();
            await servicio.SolicitarAsync(new("primera-solicitud", _blanca.Id, "blanca"));

            var r = await servicio.SolicitarAsync(new("segunda-solicitud", _blanca.Id, "blanca"));

            Assert.IsFalse(r.Aceptada);
            Assert.AreEqual(1, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        public async Task MaterialConfiguradoQueLaCanecaNoAdmite_NoAbre()
        {
            var servicio = Servicio(new Dictionary<string, int> { ["blanca"] = _organico.Id });

            var r = await servicio.SolicitarAsync(new("material-equivocado", _blanca.Id, "blanca"));

            Assert.IsFalse(r.Aceptada);
            Assert.AreEqual(0, _repositorio.Ordenes.Count);
        }

        // ── Receptor HTTP ─────────────────────────────────────────────────────────────────────────────────────

        private static byte[] Cuerpo(object o) => JsonSerializer.SerializeToUtf8Bytes(o);

        [TestMethod]
        public async Task Receptor_ExigeEncabezadoDeLaEstacion_YTipoJson()
        {
            var receptor = new ReceptorAperturasWeb(Servicio(), null);
            byte[] cuerpo = Cuerpo(new { solicitud = "receptor-prueba-1", idCaneca = _blanca.Id, color = "blanca" });

            Assert.AreEqual(403, (await receptor.RecibirAsync("application/json", null, null, cuerpo, default)).Codigo);
            Assert.AreEqual(415, (await receptor.RecibirAsync("text/plain", "estacion", null, cuerpo, default)).Codigo);
            Assert.AreEqual(400, (await receptor.RecibirAsync("application/json", "estacion", null, Encoding.UTF8.GetBytes("{no es json"), default)).Codigo);
            Assert.AreEqual(400, (await receptor.RecibirAsync("application/json", "estacion", null, Cuerpo(new { color = "blanca" }), default)).Codigo);
            Assert.AreEqual(413, (await receptor.RecibirAsync("application/json", "estacion", null, new byte[ReceptorAperturasWeb.TamanoMaximo + 1], default)).Codigo);
            Assert.AreEqual(0, _repositorio.Ordenes.Count);
        }

        [TestMethod]
        public async Task Receptor_ConTokenConfigurado_LoExige_YRespondeElContratoDeLaEstacion()
        {
            _bridge = BridgeQueAbre(TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura);
            var receptor = new ReceptorAperturasWeb(Servicio(), "token-de-prueba-123456");
            byte[] cuerpo = Cuerpo(new { solicitud = "receptor-prueba-2", idCaneca = _blanca.Id, color = "blanca", angulo = 80, origen = "estacion-web" });

            Assert.AreEqual(403, (await receptor.RecibirAsync("application/json", "estacion", "otro-token-cualquiera", cuerpo, default)).Codigo);
            var (codigo, salida) = await receptor.RecibirAsync("application/json; charset=utf-8", "estacion", "token-de-prueba-123456", cuerpo, default);

            Assert.AreEqual(200, codigo);
            using var json = JsonDocument.Parse(salida);
            Assert.IsTrue(json.RootElement.GetProperty("aceptada").GetBoolean());
            Assert.AreEqual(7000, json.RootElement.GetProperty("abiertoMs").GetInt32());
        }

        [TestMethod]
        public void TokenCorto_SeRechazaAlConfigurar() =>
            Assert.ThrowsExactly<ArgumentException>(() => new ReceptorAperturasWeb(Servicio(), "corto"));

        [TestMethod]
        [DataRow("http://127.0.0.1:8190/aperturas/", true)]
        [DataRow("http://localhost:8190/aperturas/", false)]
        [DataRow("http://+:8190/aperturas/", false)]
        [DataRow("http://0.0.0.0:8190/aperturas/", false)]
        [DataRow("https://127.0.0.1:8190/aperturas/", false)]
        [DataRow("http://127.0.0.1:80/aperturas/", false)]
        [DataRow("http://127.0.0.1:8190/aperturas", false)]
        [DataRow("http://127.0.0.1:8190/", false)]
        public void Prefijo_SoloLocalYConRuta(string prefijo, bool valido) =>
            Assert.AreEqual(valido, ServidorAperturasWeb.PrefijoValido(prefijo));

        [TestMethod]
        public void Configuracion_UsaLasCanecasDelPiloto_YValidaLosValores()
        {
            var vacia = new ConfigurationBuilder().Build();
            var c = AplicacionEstacionWeb.Leer(vacia);
            CollectionAssert.AreEquivalent(new[] { 1093, 1400, 1401 }, c.Canecas.Values.ToArray());
            Assert.AreEqual(TimeSpan.FromSeconds(20), c.Espera);
            Assert.AreEqual(ServidorAperturasWeb.PrefijoPredeterminado, c.Prefijo);

            var sinNegra = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["EstacionWeb:Canecas:negra"] = "0", ["EstacionWeb:Materiales:verde"] = "7" }).Build();
            var d = AplicacionEstacionWeb.Leer(sinNegra);
            Assert.IsFalse(d.Canecas.ContainsKey("negra"));
            Assert.AreEqual(7, d.Materiales["verde"]);

            foreach (var (clave, valor) in new[] { ("EstacionWeb:Canecas:verde", "x"), ("EstacionWeb:EsperaConfirmacionSegundos", "500"), ("EstacionWeb:Prefijo", "http://0.0.0.0:8190/a/") })
            {
                var mala = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [clave] = valor }).Build();
                Assert.ThrowsExactly<ArgumentException>(() => AplicacionEstacionWeb.Leer(mala), clave);
            }
        }
    }
}
