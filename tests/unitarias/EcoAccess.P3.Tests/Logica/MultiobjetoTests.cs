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
    /// <summary>Descripción concreta, catálogo ampliado y análisis de fotos con varios residuos (misión nocturna, 12/11/2026).</summary>
    [TestClass]
    public sealed class MultiobjetoTests
    {
        [TestMethod]
        [DataRow("Green apple", "whole_produce", "una manzana verde")]
        [DataRow("Ripe bananas", "whole_produce", "bananos maduros")]
        [DataRow("Banana peel", "food_scraps", "una cáscara de banano")]
        [DataRow("Orange peels", "food_scraps", "cáscaras de naranja")]
        [DataRow("Rice leftovers", "food_scraps", "restos de arroz")]
        [DataRow("Used coffee grounds", "food_scraps", "residuos de café")]
        [DataRow("Plastic water bottle", "plastic_bottle", "una botella plástica de agua")]
        [DataRow("Crushed soda can", "can", "una lata de bebida aplastada")]
        [DataRow("Clean cardboard box", "cardboard", "una caja de cartón limpia")]
        [DataRow("Used napkin", "used_tissue", "una servilleta usada")]
        [DataRow("AA batteries", "battery", "pilas")]
        [DataRow("Dry leaves", "plant_waste", "hojas secas")]
        [DataRow("Glass wine bottle", "glass_bottle", "una botella de vidrio")]
        public void DescribeLoQueSeVeEnEspanol(string nombre, string clave, string esperado)
        {
            Assert.AreEqual(clave, InterpretadorNombreObjeto.Clave(nombre, out _));
            Assert.AreEqual(esperado, DescriptorObjeto.Describir(nombre, clave)?.Frase);
        }

        [TestMethod]
        [DataRow("Orange plastic bottle", "plastic_bottle")]
        [DataRow("Apple logo laptop", "laptop")]
        public void LaDescripcionNuncaContradiceLaClave(string nombre, string clave)
        {
            Assert.AreEqual(clave, InterpretadorNombreObjeto.Clave(nombre, out _));
            DescripcionObjeto? d = DescriptorObjeto.Describir(nombre, clave);
            Assert.IsFalse(d?.Frase.Contains("naranja") == true || d?.Frase.Contains("manzana") == true, d?.Frase);
        }

        [TestMethod]
        [DataRow("Styrofoam food tray", "styrofoam", DestinoResiduo.Negra)]
        [DataRow("Foam cup", "styrofoam", DestinoResiduo.Negra)]
        [DataRow("Greasy pizza box", "greasy_paper", DestinoResiduo.Negra)]
        [DataRow("Cigarette butt", "cigarette_butt", DestinoResiduo.Negra)]
        [DataRow("Store receipt", "thermal_paper", DestinoResiduo.Negra)]
        [DataRow("Plastic fork", "disposable_cutlery", DestinoResiduo.Negra)]
        [DataRow("Broken glass shards", "broken_glass", DestinoResiduo.Posconsumo)]
        [DataRow("Spray can", "aerosol", DestinoResiduo.Posconsumo)]
        [DataRow("Bleach bottle", "chemical_container", DestinoResiduo.Posconsumo)]
        [DataRow("Used cooking oil", "cooking_oil", DestinoResiduo.Posconsumo)]
        [DataRow("Cracked mirror", "mirror_or_ceramic", DestinoResiduo.Negra)]
        public void CatalogoAmpliadoTieneDestinoPorTipoYEstado(string nombre, string clave, DestinoResiduo destino)
        {
            ObjetoEnFoto objeto = AnalisisMultiobjeto.AnalizarUno(1, nombre);
            Assert.AreEqual(clave, objeto.Clave);
            Assert.AreEqual(destino, objeto.Destino);
        }

        [TestMethod]
        public void CartonNoSignificaSiempreBlanca()
        {
            Assert.AreEqual(DestinoResiduo.Blanca, AnalisisMultiobjeto.AnalizarUno(1, "clean cardboard box").Destino);
            Assert.AreEqual(DestinoResiduo.Negra, AnalisisMultiobjeto.AnalizarUno(1, "dirty cardboard box").Destino);
            Assert.AreEqual(DestinoResiduo.Negra, AnalisisMultiobjeto.AnalizarUno(1, "greasy cardboard").Destino);
        }

        [TestMethod]
        public void LosAlimentosEnterosNoSeSuponenResiduo()
        {
            ObjetoEnFoto manzana = AnalisisMultiobjeto.AnalizarUno(1, "green apple");
            Assert.AreEqual(DestinoResiduo.Verde, manzana.Destino);
            Assert.IsFalse(manzana.ElegibleApertura);
            StringAssert.Contains(manzana.Motivo, "alimento");
        }

        [TestMethod]
        public void SeparaLaListaYConservaLasCantidades()
        {
            CollectionAssert.AreEqual(new[] { "3 plastic bottles", "banana peel", "used napkin" },
                AnalisisMultiobjeto.Separar("1) 3 plastic bottles, banana peel and used napkin.").ToArray());
            Assert.AreEqual(5, AnalisisMultiobjeto.Separar("a, b, c, d, e, f, g").Count);
            Assert.AreEqual(0, AnalisisMultiobjeto.Separar("  ").Count);
        }

        // Los seis casos de la fase 5 de la misión.
        private static readonly IReadOnlyList<CanecaEstacion> Estacion = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, "esp32-blanca", "izquierda"),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, "esp32-negra", "centro"),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, "esp32-verde", "derecha"),
        };

        [TestMethod]
        [DataRow("clean cardboard box, used napkin", "BLANCA+NEGRA")]
        [DataRow("plastic soda bottle, banana peel", "BLANCA+VERDE")]
        [DataRow("dirty napkin, rice leftovers", "NEGRA+VERDE")]
        [DataRow("clean cardboard box, used napkin, food scraps", "BLANCA+NEGRA+VERDE")]
        [DataRow("plastic water bottle, battery", "BLANCA")]
        [DataRow("three plastic water bottles", "BLANCA")]
        public void CasosDeLaMision(string lista, string canecas)
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar(lista);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Estacion);
            Assert.AreEqual(canecas, string.Join("+", plan.Aperturas.Select(a => ResolutorDestino.Color(a.Destino))));
        }

        [TestMethod]
        public void LaPilaVaARecoleccionEspecializadaYSoloAbreLaBlanca()
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar("plastic water bottle, battery");
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Estacion);
            Assert.AreEqual(DestinoResiduo.Posconsumo, objetos[1].Destino);
            Assert.AreEqual(1, plan.SinApertura.Count);
            string texto = AnalisisMultiobjeto.Resumen(objetos, plan, simulacion: true);
            StringAssert.Contains(texto, "🔋 Pilas → ⚠️ RECOLECCIÓN ESPECIALIZADA".Replace("Pilas", "Una pila"));
            StringAssert.Contains(texto, "PRUEBA SIMULADA");
            StringAssert.Contains(texto, "no se movió ninguna tapa");
        }

        [TestMethod]
        public void TresBotellasSonTresObjetosYUnaSolaApertura()
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar("plastic bottle, plastic bottle, plastic bottle");
            Assert.AreEqual(1, objetos.Count);
            Assert.AreEqual(3, objetos[0].Cantidad);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Estacion);
            Assert.AreEqual(1, plan.Aperturas.Count);
            StringAssert.Contains(AnalisisMultiobjeto.Resumen(objetos, plan), "Identifiqué 3 residuos");
        }

        [TestMethod]
        public void UnaCanecaFueraDeServicioNoEntraAlPlan()
        {
            var estacion = Estacion.Select(c => c.IdContenedor == 1401 ? c with { EnServicio = false } : c).ToList();
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("cardboard box, used napkin"), estacion);
            Assert.AreEqual("BLANCA", string.Join("+", plan.Aperturas.Select(a => ResolutorDestino.Color(a.Destino))));
            StringAssert.Contains(plan.SinApertura.Single().Motivo, "fuera de servicio");
        }

        [TestMethod]
        public void LoConfundibleYLoQueNoEsResiduoNoAbren()
        {
            IReadOnlyList<ObjetoEnFoto> objetos = AnalisisMultiobjeto.Analizar("plastic container, dog, blurry thing");
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(objetos, Estacion);
            Assert.AreEqual(0, plan.Aperturas.Count);
            Assert.AreEqual(3, plan.SinApertura.Count);
        }
    }

    /// <summary>Coordinador contra el ESP32 simulado: ninguna de estas pruebas toca puertos ni motores.</summary>
    [TestClass]
    public sealed class CoordinadorAperturasMultiplesTests
    {
        private static readonly IReadOnlyDictionary<int, DestinoResiduo> Tapas = new Dictionary<int, DestinoResiduo>
        {
            [1093] = DestinoResiduo.Blanca, [1401] = DestinoResiduo.Negra, [1400] = DestinoResiduo.Verde,
        };

        private static readonly IReadOnlyList<CanecaEstacion> Estacion = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, "esp32-blanca", null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, "esp32-negra", null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, "esp32-verde", null),
        };

        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 12, 7, 0, 0, TimeSpan.Zero));

        private sealed class EjecutorFisicoFalso : IEjecutorAperturas
        {
            public bool EsSimulado => false;
            public Task<ResultadoEjecucionApertura> AbrirAsync(ComandoApertura comando, System.Threading.CancellationToken ct = default) =>
                throw new AssertFailedException("Nunca debe llamarse.");
        }

        [TestMethod]
        public void RechazaCualquierEjecutorQueNoSeaSimulado() =>
            Assert.ThrowsExactly<InvalidOperationException>(() => new CoordinadorAperturasMultiples(new EjecutorFisicoFalso(), _reloj));

        // Las siete combinaciones de la fase 12.
        [TestMethod]
        [DataRow("cardboard box", "BLANCA")]
        [DataRow("used napkin", "NEGRA")]
        [DataRow("banana peel", "VERDE")]
        [DataRow("cardboard box, used napkin", "BLANCA+NEGRA")]
        [DataRow("cardboard box, banana peel", "BLANCA+VERDE")]
        [DataRow("used napkin, banana peel", "NEGRA+VERDE")]
        [DataRow("cardboard box, used napkin, banana peel", "BLANCA+NEGRA+VERDE")]
        public async Task CadaCombinacionAbreSoloSusCanecasEnOrden(string lista, string esperado)
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar(lista), Estacion);
            IReadOnlyList<ResultadoApertura> r = await coordinador.EjecutarAsync("op-" + esperado, plan);
            Assert.IsTrue(r.All(x => x.Completada));
            var abiertas = simulador.Bitacora.Where(b => b.Tipo == TipoEventoDispositivo.Apertura).Select(b => Tapas[b.IdContenedor]);
            Assert.AreEqual(esperado, string.Join("+", abiertas.Select(ResolutorDestino.Color)));
            Assert.AreEqual(1, simulador.MaximoTapasSimultaneas);
        }

        [TestMethod]
        public async Task LaNegraRecibeSuPropioContenedorYSuGpioConfigurado()
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("used napkin"), Estacion);
            ResultadoApertura negra = (await coordinador.EjecutarAsync("op-negra", plan)).Single();
            Assert.AreEqual(1401, negra.Apertura.Caneca.IdContenedor);
            Assert.AreEqual("op-negra:NEGRA", negra.ClaveIdempotencia);
            Assert.AreEqual(18, SimuladorEstacionTresCanecas.GpioConfigurado[DestinoResiduo.Negra]);
            CollectionAssert.AreEqual(new[] { TipoEventoDispositivo.Acuse, TipoEventoDispositivo.Apertura, TipoEventoDispositivo.CierreSinSensor },
                negra.Ejecucion.Eventos.ToArray());
            Assert.IsTrue(simulador.Bitacora.All(b => b.IdContenedor == 1401));
        }

        [TestMethod]
        public async Task UnFalloParcialNoCancelaLasDemasCanecas()
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            simulador.ProgramarFalla(1401, FallaSimulada.NoAbre);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("cardboard box, used napkin, banana peel"), Estacion);
            IReadOnlyList<ResultadoApertura> r = await coordinador.EjecutarAsync("op-parcial", plan);
            CollectionAssert.AreEqual(new[] { EstadoEjecucionApertura.Completada, EstadoEjecucionApertura.FalloApertura, EstadoEjecucionApertura.Completada },
                r.Select(x => x.Ejecucion.Estado).ToArray());
            StringAssert.Contains(CoordinadorAperturasMultiples.Resumen(r), "NEGRA: la tapa no abrió");
            StringAssert.Contains(CoordinadorAperturasMultiples.Resumen(r), "sin movimiento físico");
        }

        [TestMethod]
        [DataRow(FallaSimulada.NoCierra, EstadoEjecucionApertura.FalloCierre)]
        [DataRow(FallaSimulada.SinRespuesta, EstadoEjecucionApertura.SinRespuesta)]
        [DataRow(FallaSimulada.NoAbre, EstadoEjecucionApertura.FalloApertura)]
        public async Task CadaFallaQuedaRegistrada(FallaSimulada falla, EstadoEjecucionApertura esperado)
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            simulador.ProgramarFalla(1400, falla);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            var r = await coordinador.EjecutarAsync("op-falla", CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("banana peel"), Estacion));
            Assert.AreEqual(esperado, r.Single().Ejecucion.Estado);
        }

        [TestMethod]
        public async Task UnaOperacionRepetidaNoAbreDosVecesNiTrasReiniciar()
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("cardboard box, used napkin"), Estacion);
            await coordinador.EjecutarAsync("op-dup", plan);
            simulador.Reiniciar();
            IReadOnlyList<ResultadoApertura> segunda = await coordinador.EjecutarAsync("op-dup", plan);
            Assert.IsTrue(segunda.All(x => x.Ejecucion.Estado == EstadoEjecucionApertura.Duplicada));
            Assert.AreEqual(2, simulador.Bitacora.Count(b => b.Tipo == TipoEventoDispositivo.Apertura));
            Assert.AreEqual(1, simulador.Reinicios);
        }

        [TestMethod]
        public async Task DesconectadaNoAbreYAlReconectarSi()
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar("cardboard box"), Estacion);
            simulador.Desconectar();
            Assert.AreEqual(EstadoEjecucionApertura.Desconectada, (await coordinador.EjecutarAsync("op-a", plan)).Single().Ejecucion.Estado);
            simulador.Reconectar();
            Assert.AreEqual(EstadoEjecucionApertura.Completada, (await coordinador.EjecutarAsync("op-b", plan)).Single().Ejecucion.Estado);
        }

        [TestMethod]
        public async Task OrdenVencidaODeOtraEstacionSeRechaza()
        {
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var vencida = new ComandoApertura(1, 1093, "x", _reloj.Ahora.UtcDateTime.AddSeconds(-1));
            var ajena = new ComandoApertura(2, 9999, "y", _reloj.Ahora.UtcDateTime.AddMinutes(1));
            Assert.AreEqual(EstadoEjecucionApertura.Rechazada, (await simulador.AbrirAsync(vencida)).Estado);
            Assert.AreEqual(EstadoEjecucionApertura.Rechazada, (await simulador.AbrirAsync(ajena)).Estado);
            Assert.AreEqual(0, simulador.Bitacora.Count);
        }

        [TestMethod]
        public async Task CientoVeinteOperacionesConFallasYConcurrenciaNuncaMuevenDosTapasALaVez()
        {
            string[] listas =
            {
                "cardboard box", "used napkin", "banana peel", "cardboard box, used napkin", "cardboard box, banana peel",
                "used napkin, banana peel", "cardboard box, used napkin, banana peel", "plastic water bottle, battery",
            };
            var simulador = new SimuladorEstacionTresCanecas(Tapas, _reloj);
            var coordinador = new CoordinadorAperturasMultiples(simulador, _reloj);
            var azar = new Random(20261112);
            var tareas = new List<Task<IReadOnlyList<ResultadoApertura>>>();
            for (int i = 0; i < 120; i++)
            {
                if (i % 10 == 3) simulador.ProgramarFalla(Tapas.Keys.ElementAt(azar.Next(3)), (FallaSimulada)azar.Next(1, 4));
                PlanAperturas plan = CoordinadorAperturasMultiples.Planificar(AnalisisMultiobjeto.Analizar(listas[i % listas.Length]), Estacion);
                string operacion = "carga-" + i, anterior = "carga-" + (i - 1);
                tareas.Add(Task.Run(() => coordinador.EjecutarAsync(operacion, plan)));
                if (i % 20 == 19) tareas.Add(Task.Run(() => coordinador.EjecutarAsync(anterior, plan))); // mensaje duplicado
            }
            IReadOnlyList<ResultadoApertura>[] todos = await Task.WhenAll(tareas);
            var resultados = todos.SelectMany(r => r).ToList();
            Assert.AreEqual(1, simulador.MaximoTapasSimultaneas);
            int completas = resultados.Count(r => r.Completada);
            int aperturas = simulador.Bitacora.Count(b => b.Tipo == TipoEventoDispositivo.Apertura);
            int fallasCierre = resultados.Count(r => r.Ejecucion.Estado == EstadoEjecucionApertura.FalloCierre);
            Assert.AreEqual(completas + fallasCierre, aperturas, "Cada apertura del simulador corresponde a un resultado.");
            Assert.AreEqual(resultados.Select(r => r.ClaveIdempotencia).Distinct().Count(),
                resultados.Count(r => r.Ejecucion.Estado != EstadoEjecucionApertura.Duplicada));
            Assert.IsTrue(completas >= 150, $"Completadas: {completas}");
        }
    }
}
