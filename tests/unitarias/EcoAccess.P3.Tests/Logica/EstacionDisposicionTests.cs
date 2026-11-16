using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Flujo automático de disposición en la estación de tres canecas (pruebas reales del 12/11/2026: una manzana y un rollo
    /// de papel higiénico terminaban en «¿Deseas desechar…?» o en «fuera de servicio» sin abrir nada). Con el QR de la estación
    /// vigente, la foto es la intención de depositar: va a la caneca del color que corresponde, sin botones de confirmación,
    /// solo si Oracle la tiene en servicio y admite el material. Todo lo dudoso sigue sin abrir.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private static PropuestaVision VistoConCerteza(string clave, decimal certeza) =>
            new(TipoPropuestaVision.SinResiduo, null, certeza, ClaveObjeto: clave);

        /// <summary>Blanca = la caneca del QR (_caneca); verde con ORGANICO; negra con SERVILLETA_USADA.</summary>
        private (ServicioConversacionTelegram Eco, Contenedor Verde, Contenedor Negra) Estacion(
            System.Func<PropuestaVision> propuesta, bool verdeEnServicio = true, bool verdeAdmiteOrganico = true, bool conEstacion = true)
        {
            Material organico = _materiales.Sembrar("ORGANICO", "Restos de comida", CategoriaMaterial.OrganicoAprovechable);
            Material servilleta = _materiales.Sembrar("SERVILLETA_USADA", "Servilleta o papel higiénico usado", CategoriaMaterial.NoAprovechable);
            Material otro = _materiales.Sembrar("MADERA", "Madera", CategoriaMaterial.NoAprovechable);
            Contenedor verde = _contenedores.Sembrar(1, verdeEnServicio ? EstadoContenedor.EnServicio : EstadoContenedor.FueraDeServicio,
                verdeAdmiteOrganico ? organico.Id : otro.Id);
            Contenedor negra = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, servilleta.Id);
            var eco = Eco(propuesta);
            if (conEstacion)
                eco.Estaciones = new EstacionesEnMemoria
                {
                    Canecas = new[]
                    {
                        new CanecaEstacion(_caneca.Id, "Blanca", CategoriaMaterial.Aprovechable, true, "esp32-piloto-blanca", "izquierda"),
                        new CanecaEstacion(verde.Id, "Verde", CategoriaMaterial.OrganicoAprovechable, verdeEnServicio, "esp32-piloto-verde", "centro"),
                        new CanecaEstacion(negra.Id, "Negra", CategoriaMaterial.NoAprovechable, true, "esp32-piloto-negra", "derecha"),
                    },
                };
            return (eco, verde, negra);
        }

        private int[] CanecasConOrden() => _interacciones.Ordenes.Select(o => o.IdContenedor).ToArray();

        private static void SinBotonDeConfirmar(IReadOnlyList<RespuestaBot> respuestas)
        {
            foreach (RespuestaBot r in respuestas)
            {
                Assert.DoesNotContain("¿Deseas desechar", r.Texto);
                Assert.IsFalse(r.Botones.Any(b => b.Texto.Contains("Sí, desechar") || b.Texto.Contains("Sí, continuar")), "La foto ya es la solicitud.");
            }
        }

        [TestMethod]
        [DataRow("whole_produce")]
        [DataRow("food_scraps")]
        [DataRow("served_food")]
        public async Task Disposicion_ComidaAbreSoloLaVerde_SinPreguntar(string clave)
        {
            // Con la segunda lectura que corrobora (orgánico): sin ella ya no abre (órdenes 1685 y 1686).
            var (eco, verde, _) = Estacion(() => Visto(clave, "ORGANICO"));

            IReadOnlyList<RespuestaBot> respuestas = await EnviarFotoAsync(eco);

            SinBotonDeConfirmar(respuestas);
            CollectionAssert.AreEqual(new[] { verde.Id }, CanecasConOrden(), "Una sola orden y solo para la verde. Respuesta: " + string.Join(" || ", respuestas.Select(x => x.Texto)));
        }

        [TestMethod]
        public async Task Disposicion_PapelHigienicoUsadoAbreSoloLaNegra()
        {
            var (eco, _, negra) = Estacion(() => Visto("used_tissue", "SERVILLETA_USADA"));

            IReadOnlyList<RespuestaBot> respuestas = await EnviarFotoAsync(eco);

            SinBotonDeConfirmar(respuestas);
            CollectionAssert.AreEqual(new[] { negra.Id }, CanecasConOrden(), "Respuesta: " + string.Join(" || ", respuestas.Select(x => x.Texto)));
        }

        /// <summary>
        /// Regresión de las órdenes reales 1685 y 1686 (12/11/2026): una bolsa de agua se nombró como empaque metalizado y abrió la
        /// NEGRA. Sin una segunda lectura que confirme el material, ni la negra ni la verde se abren: Eco solo orienta.
        /// </summary>
        [TestMethod]
        [DataRow("snack_wrapper", null)]
        [DataRow("snack_wrapper", "BOLSA_PLASTICA")]
        [DataRow("used_tissue", "PAPEL")]
        [DataRow("food_scraps", null)]
        public async Task Disposicion_SinSegundaLecturaQueCorroboreNoAbreNingunaCaneca(string clave, string? lectura)
        {
            var (eco, _, _) = Estacion(() => Visto(clave, lectura));

            IReadOnlyList<RespuestaBot> respuestas = await EnviarFotoAsync(eco);

            Assert.IsEmpty(CanecasConOrden(), "Respuesta: " + string.Join(" || ", respuestas.Select(x => x.Texto)));
        }

        [TestMethod]
        public void LaBolsaDeAguaNoEsUnEmpaqueMetalizado()
        {
            foreach (string nombre in new[] { "Clear plastic water bag", "Ice bag", "Plastic packet", "Transparent plastic package", "Water sachet" })
                Assert.AreNotEqual("snack_wrapper", InterpretadorNombreObjeto.Clave(nombre, out _), nombre);
            Assert.AreEqual("snack_wrapper", InterpretadorNombreObjeto.Clave("Chip packet", out _));
            Assert.AreEqual("snack_wrapper", InterpretadorNombreObjeto.Clave("Shiny candy wrapper", out _));
        }

        [TestMethod]
        public async Task Disposicion_BotellaPetSigueAbriendoLaBlanca()
        {
            var (eco, _, _) = Estacion(BotellaEnPet);

            await EnviarFotoAsync(eco);

            CollectionAssert.AreEqual(new[] { _caneca.Id }, CanecasConOrden());
        }

        [TestMethod]
        public async Task Disposicion_VerdeFueraDeServicioEnOracle_AvisaYNoAbreNada()
        {
            var (eco, _, _) = Estacion(() => Visto("whole_produce"), verdeEnServicio: false);

            RespuestaBot r = Unica(await EnviarFotoAsync(eco));

            StringAssert.Contains(r.Texto, "fuera de servicio");
            StringAssert.Contains(r.Texto, "No se abrió ninguna tapa.");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task Disposicion_VerdeQueNoAdmiteElMaterial_NoAbre()
        {
            var (eco, _, _) = Estacion(() => Visto("whole_produce"), verdeAdmiteOrganico: false);

            await EnviarFotoAsync(eco);

            Assert.IsEmpty(_interacciones.Ordenes, "Oracle decide qué admite cada caneca.");
        }

        [TestMethod]
        [DataRow("toilet_paper_roll")]
        [DataRow("plastic_container")]
        [DataRow("battery")]
        [DataRow("people")]
        [DataRow("dog")]
        public async Task Disposicion_ObjetosDudososOEspeciales_NoAbrenNinguna(string clave)
        {
            var (eco, _, _) = Estacion(() => Visto(clave));

            await EnviarFotoAsync(eco);

            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task Disposicion_CertezaDeclaradaBaja_NoAbre()
        {
            var (eco, _, _) = Estacion(() => VistoConCerteza("used_tissue", 0.4m));

            await EnviarFotoAsync(eco);

            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task SinEstacionConfigurada_LaComidaSoloOrienta()
        {
            var (eco, _, _) = Estacion(() => Visto("whole_produce"), conEstacion: false);

            RespuestaBot r = Unica(await EnviarFotoAsync(eco));

            StringAssert.Contains(r.Texto, "VERDE");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public void Politica_VerdeYNegraSoloConParesVerificables()
        {
            Assert.IsTrue(PoliticaAperturaAutomatica.CoincidenEnEstacion("ORGANICO", ObjetoVisible.ResiduoOrganico));
            Assert.IsTrue(PoliticaAperturaAutomatica.CoincidenEnEstacion("SERVILLETA_USADA", ObjetoVisible.ArticuloSanitario));
            Assert.IsFalse(PoliticaAperturaAutomatica.CoincidenEnEstacion("ORGANICO", ObjetoVisible.ArticuloSanitario));
            Assert.IsFalse(PoliticaAperturaAutomatica.CoincidenEnEstacion("PILA", ObjetoVisible.PilaOBateria), "Lo especial nunca va a una caneca.");
        }
    }

    [TestClass]
    public sealed class InterpretadorDisposicionTests
    {
        [TestMethod]
        [DataRow("Green apple", "whole_produce")]
        [DataRow("A red apple on a table", "whole_produce")]
        [DataRow("Bunch of bananas", "whole_produce")]
        [DataRow("Tomatoes", "whole_produce")]
        [DataRow("Fruit salad in a bowl", "served_food")]
        [DataRow("Apple pie", "served_food")]
        [DataRow("Banana peel", "food_scraps")]
        [DataRow("Apple core", "food_scraps")]
        [DataRow("Rotten apple", "food_scraps")]
        [DataRow("Plastic bottle of orange juice", "plastic_bottle")]
        [DataRow("Canned fruit syrup.", "can")]
        [DataRow("Toilet paper roll", "toilet_paper_roll")]
        [DataRow("A roll of toilet paper", "toilet_paper_roll")]
        [DataRow("Used toilet paper", "used_tissue")]
        [DataRow("Crumpled toilet paper", "used_tissue")]
        [DataRow("Empty toilet paper roll", "cardboard")]
        [DataRow("Toilet paper tube", "cardboard")]
        [DataRow("Tissue paper", "used_tissue")]
        public void FrutaYPapelHigienicoSeDistinguen(string nombre, string esperada) =>
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
    }
}
