using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Foto con varios residuos en la conversación: orienta cada uno y no crea ninguna orden.</summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class MotorQueLista : IClasificadorMaterial, IListadorObjetosVisibles
        {
            private readonly string? _lista;
            public MotorQueLista(string? lista) { _lista = lista; }
            public int Listados { get; private set; }
            public string Proveedor => "ollama-local:modelo";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<EcoAccess.P3.Entidades.Material> catalogo, CancellationToken ct = default) =>
                Task.FromResult(new PropuestaVision(TipoPropuestaVision.ObjetoDesconocido, null, null, ClaveObjeto: CatalogoObjetos.VariosObjetos));
            public Task<string?> ListarAsync(ImagenResiduo imagen, CancellationToken ct = default) { Listados++; return Task.FromResult(_lista); }
        }

        private ServicioConversacionTelegram EcoQueLista(MotorQueLista motor, IClasificadorMaterial? externo = null) =>
            new(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                vision: new ServicioClasificacionImagen(new SelectorVision(ModoVision.Local, motor, externo, TimeSpan.FromSeconds(30), _ => { }),
                    _materiales, new ServicioCatalogoAmbiental(_materiales, _contenedores)),
                descargarFoto: (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")),
                flujoRapido: true, qrDemo: _caneca.IdentificadorQr);

        [TestMethod]
        public async Task UnaFotoConTresResiduosNombraCadaUnoConSuCanecaYNoAbreNada()
        {
            var motor = new MotorQueLista("clean cardboard box, used napkin, banana peel");
            RespuestaBot r = Unica(await EnviarFotoAsync(EcoQueLista(motor), Invitado));

            StringAssert.Contains(r.Texto, "Identifiqué 3 residuos");
            StringAssert.Contains(r.Texto, "Una caja de cartón limpia → ⬜ BLANCA");
            StringAssert.Contains(r.Texto, "Una servilleta usada → ⬛ NEGRA");
            StringAssert.Contains(r.Texto, "Una cáscara de banano → 🟩 VERDE");
            StringAssert.Contains(r.Texto, "de uno en uno");
            Assert.IsEmpty(_interacciones.Ordenes, "Varios residuos en una foto nunca crean órdenes reales.");
        }

        [TestMethod]
        public async Task UnaPilaEntreLosResiduosSeEnviaARecoleccionEspecializada()
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(EcoQueLista(new MotorQueLista("plastic water bottle, battery")), Invitado));
            StringAssert.Contains(r.Texto, "Una pila → 🔋 RECOLECCIÓN DE PILAS");
            StringAssert.Contains(r.Texto, "no va en ninguna caneca");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("blurry thing")]
        public async Task SinUnaListaUtilQuedaLaRespuestaDeSiempre(string? lista)
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(EcoQueLista(new MotorQueLista(lista)), Invitado));
            Assert.DoesNotContain("Identifiqué", r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task LaListaSoloSePideAlModeloLocal()
        {
            var externo = new MotorQueLista("cardboard box, napkin");
            var selector = new SelectorVision(ModoVision.Groq, null, externo, TimeSpan.FromSeconds(30), _ => { });
            Assert.IsNull(await selector.ListarAsync(new ImagenResiduo(FotoDelDoble, "image/png")));
            Assert.AreEqual(0, externo.Listados);
        }
    }
}
