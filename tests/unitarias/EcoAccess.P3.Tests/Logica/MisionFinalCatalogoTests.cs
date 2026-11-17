using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Misión final: catálogo colombiano, matriz de decisiones, seis representaciones y orientación en Valledupar.</summary>
    [TestClass]
    public sealed class MisionFinalCatalogoTests
    {
        private static RutaDisposicion RutaDe(string nombre)
        {
            string? clave = InterpretadorNombreObjeto.Clave(nombre, out _);
            Assert.IsNotNull(clave, nombre);
            return MatrizDecisionResiduos.Ruta(CatalogoObjetos.Buscar(clave)!);
        }

        [TestMethod]
        public void ElCatalogoTieneMasDeTrescientosTiposSinClavesRepetidas()
        {
            Assert.IsGreaterThanOrEqualTo(300, CatalogoObjetos.Todos.Count);
            Assert.AreEqual(CatalogoObjetos.Todos.Count, CatalogoObjetos.Todos.Select(o => o.Clave).Distinct().Count());
            Assert.IsGreaterThan(200, InterpretadorNombreObjeto.PalabrasAmpliadas);
        }

        [TestMethod]
        public void NingunaEntradaAmpliadaPuedeAbrirUnaCanecaFisica()
        {
            foreach (CatalogoAmpliado.Entrada e in CatalogoAmpliado.Entradas)
                Assert.IsNull(CatalogoObjetos.Buscar(e.Clave)!.CodigoMaterial, e.Clave);
        }

        [TestMethod]
        public void CadaClaveTieneUnaRutaYUnMensaje()
        {
            foreach (ObjetoConocido o in CatalogoObjetos.Todos)
            {
                RutaDisposicion ruta = MatrizDecisionResiduos.Ruta(o);
                Assert.IsTrue(Enum.IsDefined(typeof(RutaDisposicion), ruta), o.Clave);
                Assert.IsFalse(string.IsNullOrWhiteSpace(o.Mensaje), o.Clave);
            }
        }

        // Fase 2 y casos difíciles de la fase 4: lo electrónico nunca va a la blanca por tener plástico o metal.
        [TestMethod]
        [DataRow("White AirPods", RutaDisposicion.Raee)]
        [DataRow("Bluetooth speaker", RutaDisposicion.Raee)]
        [DataRow("Wireless earbuds", RutaDisposicion.Raee)]
        [DataRow("Computer mouse", RutaDisposicion.Raee)]
        [DataRow("TV remote control", RutaDisposicion.Raee)]
        [DataRow("Game controller", RutaDisposicion.Raee)]
        [DataRow("Smart watch", RutaDisposicion.Raee)]
        [DataRow("USB flash drive", RutaDisposicion.Raee)]
        [DataRow("External hard drive", RutaDisposicion.Raee)]
        [DataRow("Wifi router", RutaDisposicion.Raee)]
        [DataRow("Old phone", RutaDisposicion.Raee)]
        [DataRow("Phone charger", RutaDisposicion.Raee)]
        [DataRow("Swollen battery", RutaDisposicion.PilasBaterias)]
        [DataRow("Power bank", RutaDisposicion.PilasBaterias)]
        [DataRow("Fluorescent tube", RutaDisposicion.Bombillos)]
        public void ElectronicaYPilasNuncaVanALaBlanca(string nombre, RutaDisposicion esperada)
        {
            RutaDisposicion ruta = RutaDe(nombre);
            Assert.AreEqual(esperada, ruta, nombre);
            Assert.IsFalse(MatrizDecisionResiduos.EsCaneca(ruta));
        }

        [TestMethod]
        [DataRow("Running shoes", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Old sneakers", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Reading glasses", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Leather backpack", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Baseball cap", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Blue jeans", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Umbrella", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Soccer ball", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Book", RutaDisposicion.ReutilizacionDonacion)]
        public void RopaYAccesoriosSeDonanOReutilizan(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, RutaDe(nombre), nombre);

        [TestMethod]
        [DataRow("Cream jar", RutaDisposicion.Evaluar)]
        [DataRow("Sunscreen", RutaDisposicion.Evaluar)]
        [DataRow("Shampoo bottle", RutaDisposicion.Evaluar)]
        [DataRow("Toothpaste tube", RutaDisposicion.Negra)]
        [DataRow("Wet wipes", RutaDisposicion.Negra)]
        [DataRow("Cotton swabs", RutaDisposicion.Negra)]
        [DataRow("Disposable razor", RutaDisposicion.Cortopunzante)]
        [DataRow("Condom wrapper", RutaDisposicion.Negra)]
        [DataRow("Sealed condom", RutaDisposicion.ReutilizacionDonacion)]
        [DataRow("Nail polish", RutaDisposicion.Peligroso)]
        [DataRow("Toothbrush", RutaDisposicion.Negra)]
        public void HigieneDistingueProductoYEmpaque(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, RutaDe(nombre), nombre);

        [TestMethod]
        [DataRow("Arepa", RutaDisposicion.Evaluar)]
        [DataRow("Plantain", RutaDisposicion.Evaluar)]
        [DataRow("Cassava root", RutaDisposicion.Evaluar)]
        [DataRow("Chicken bones", RutaDisposicion.Verde)]
        [DataRow("Fish bones", RutaDisposicion.Verde)]
        [DataRow("Milk bag", RutaDisposicion.Blanca)]
        [DataRow("Paper plate", RutaDisposicion.Negra)]
        [DataRow("Coffee capsule", RutaDisposicion.Negra)]
        [DataRow("Coconut shell", RutaDisposicion.Verde)]
        [DataRow("Tuna can", RutaDisposicion.Blanca)]
        public void CocinaColombiana(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, RutaDe(nombre), nombre);

        [TestMethod]
        [DataRow("Lancet", RutaDisposicion.Cortopunzante)]
        [DataRow("Insulin pen", RutaDisposicion.Cortopunzante)]
        [DataRow("Pill bottle", RutaDisposicion.Medicamentos)]
        [DataRow("Empty blister", RutaDisposicion.Medicamentos)]
        [DataRow("Pesticide container", RutaDisposicion.Peligroso)]
        [DataRow("Gas cylinder", RutaDisposicion.Peligroso)]
        [DataRow("Car tire", RutaDisposicion.Peligroso)]
        [DataRow("Kitchen knife", RutaDisposicion.Cortopunzante)]
        public void SaludYPeligrosos(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, RutaDe(nombre), nombre);

        // Fase 6: la matriz mínima de la misión.
        [TestMethod]
        [DataRow("Plastic water bottle", RutaDisposicion.Blanca)]
        [DataRow("Used napkin", RutaDisposicion.Negra)]
        [DataRow("Banana peel", RutaDisposicion.Verde)]
        [DataRow("Cardboard box", RutaDisposicion.Blanca)]
        [DataRow("Greasy cardboard", RutaDisposicion.Negra)]
        [DataRow("Laptop", RutaDisposicion.Raee)]
        [DataRow("Battery", RutaDisposicion.PilasBaterias)]
        [DataRow("Expired medicine", RutaDisposicion.Medicamentos)]
        [DataRow("Syringe", RutaDisposicion.Cortopunzante)]
        public void MatrizMinimaDeLaMision(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, RutaDe(nombre), nombre);

        [TestMethod]
        public void LaMatrizNoCambiaDecisionesYaMedidas()
        {
            Assert.AreEqual("plastic_bottle", InterpretadorNombreObjeto.Clave("Plastic soda bottle", out _));
            Assert.AreEqual("face_mask", InterpretadorNombreObjeto.Clave("Surgical mask", out _));
            Assert.AreEqual("snack_wrapper", InterpretadorNombreObjeto.Clave("Candy wrapper", out _));
            Assert.AreEqual("can", InterpretadorNombreObjeto.Clave("Canned food label", out _));
        }

        // Fase 8: escenarios D y E.
        private static readonly IReadOnlyList<CanecaEstacion> Virtuales = RepresentacionesContenedor.CanecasVirtuales();

        [TestMethod]
        public void EscenarioD_ParlanteEsRaeeYSoloSePreparaLaBlanca()
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar("bluetooth speaker, plastic water bottle");
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Virtuales);
            Assert.AreEqual("BLANCA", string.Join("+", plan.Aperturas.Select(a => ResolutorDestino.Color(a.Destino))));
            StringAssert.Contains(AnalisisMultiobjeto.Resumen(objetos, plan), "Un parlante Bluetooth → 🔌 RESIDUOS ELECTRÓNICOS (RAEE)");
        }

        [TestMethod]
        public void EscenarioE_CelularCargadorYAirPodsNoAbrenNada()
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar("old phone, phone charger, airpods");
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Virtuales);
            Assert.AreEqual(0, plan.Aperturas.Count);
            Assert.AreEqual(3, plan.SinApertura.Count);
        }

        [TestMethod]
        public void EscenarioB_LataYRestosDeComida()
        {
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("soda can, food scraps"), Virtuales);
            Assert.AreEqual("BLANCA+VERDE", string.Join("+", plan.Aperturas.Select(a => ResolutorDestino.Color(a.Destino))));
        }

        // Fase 1: exactamente seis representaciones.
        [TestMethod]
        public void HayExactamenteSeisRepresentacionesTresRealesYTresVirtuales()
        {
            Assert.HasCount(6, RepresentacionesContenedor.Todas);
            CollectionAssert.AreEqual(new[] { "REAL-BLANCA", "REAL-NEGRA", "REAL-VERDE", "VIRTUAL-BLANCA", "VIRTUAL-NEGRA", "VIRTUAL-VERDE" },
                RepresentacionesContenedor.Todas.Select(r => r.IdLogico).ToArray());
            foreach (ModoContenedor modo in new[] { ModoContenedor.Real, ModoContenedor.Virtual })
                CollectionAssert.AreEquivalent(new[] { DestinoResiduo.Blanca, DestinoResiduo.Negra, DestinoResiduo.Verde },
                    RepresentacionesContenedor.DelModo(modo).Select(r => r.Color).ToArray());
        }

        [TestMethod]
        public void LasRealesConservanSusIdsYGpioYCoincidenConElSimulador()
        {
            Assert.AreEqual((1093, (int?)19), Par("REAL-BLANCA"));
            Assert.AreEqual((1401, (int?)18), Par("REAL-NEGRA"));
            Assert.AreEqual((1400, (int?)23), Par("REAL-VERDE"));
            foreach (RepresentacionContenedor real in RepresentacionesContenedor.DelModo(ModoContenedor.Real))
                Assert.AreEqual(SimuladorEstacionTresCanecas.GpioConfigurado[real.Color], real.Gpio);
            Assert.IsTrue(RepresentacionesContenedor.DelModo(ModoContenedor.Virtual).All(v => v.Gpio is null));
            static (int, int?) Par(string id) { var r = RepresentacionesContenedor.Buscar(id); return (r.IdContenedor, r.Gpio); }
        }

        [TestMethod]
        public async Task LasVirtualesAbrenEnElSimuladorYElCoordinadorRechazaLoReal()
        {
            var reloj = new RelojAjustable(new DateTimeOffset(2026, 11, 12, 8, 0, 0, TimeSpan.Zero));
            SimuladorEstacionTresCanecas virtual_ = RepresentacionesContenedor.CrearEstacionVirtual(reloj);
            var coordinador = new CoordinadorAperturasMultiples(virtual_, reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("cardboard box, used napkin, banana peel"), Virtuales);
            IReadOnlyList<ResultadoApertura> r = await coordinador.EjecutarAsync("virtual-1", plan);
            Assert.IsTrue(r.All(x => x.Completada));
            Assert.AreEqual(1, virtual_.MaximoTapasSimultaneas);
        }

        // Fase 7: orientación en Valledupar sin direcciones inventadas.
        [TestMethod]
        public void LaSemillaSoloTieneDireccionesDeFuentesPublicadas()
        {
            foreach (PuntoDisposicion p in CatalogoPuntosValledupar.Semilla)
            {
                StringAssert.StartsWith(p.Fuente, "https://");
                Assert.AreEqual("Valledupar", p.Municipio);
                Assert.AreNotEqual(EstadoVerificacionPunto.Confirmado, p.Estado, "Nada está confirmado en sitio todavía.");
                Assert.AreEqual(p.Direccion is null, p.Estado == EstadoVerificacionPunto.SoloPrograma, p.Codigo);
            }
        }

        [TestMethod]
        public async Task UnCelularRecibeElPuntoListadoConAdvertenciaDeConfirmar()
        {
            string? texto = await new ServicioOrientacionEspecial().TextoAsync(RutaDisposicion.Raee, "un celular");
            StringAssert.Contains(texto, "📍 Lugar: Centro Comercial Unicentro Valledupar");
            StringAssert.Contains(texto, "🏠 Dirección: Cra. 9 #6-42, Valledupar");
            StringAssert.Contains(texto, "🗺️ Ubicación: https://www.google.com/maps/search/?api=1&query=");
            StringAssert.Contains(texto, "no hemos confirmado que siga activo ni que reciba un celular");
        }

        [TestMethod]
        public async Task SinPuntoConDireccionSeDiceQueEstaPendiente()
        {
            string? medicamento = await new ServicioOrientacionEspecial().TextoAsync(RutaDisposicion.Medicamentos, "un medicamento");
            StringAssert.Contains(medicamento, "pendiente de verificación");
            StringAssert.Contains(medicamento, "Punto Azul");
            string? zapatos = await new ServicioOrientacionEspecial().TextoAsync(RutaDisposicion.ReutilizacionDonacion, "unos zapatos");
            StringAssert.Contains(zapatos, "punto de donación verificado");
            Assert.IsNull(await new ServicioOrientacionEspecial().TextoAsync(RutaDisposicion.Blanca, "una botella"));
        }

        private sealed class OracleCaido : IRepositorioPuntosDisposicion
        {
            public Task<IReadOnlyList<PuntoDisposicion>> ListarAsync(string municipio, System.Threading.CancellationToken ct = default) =>
                throw new InvalidOperationException("ORA-00942");
        }

        [TestMethod]
        public async Task SiOracleNoTieneLaTablaSeUsaLaSemilla()
        {
            string? texto = await new ServicioOrientacionEspecial(new OracleCaido()).TextoAsync(RutaDisposicion.PilasBaterias, "una pila");
            StringAssert.Contains(texto, "Unicentro");
        }
    }
}
