using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Fotos del propietario (11/11/2026): un plato con pescado cocinado y papas recibió «Identifiqué un animal», y unos
    /// huevos enteros se quedaron en «comida» sin continuar. La comida que todavía puede ser alimento no se supone residuo:
    /// Eco pregunta si se va a desechar y solo entonces orienta a la caneca verde, sin ecopuntos y sin abrir ninguna tapa.
    /// </summary>
    [TestClass]
    public class InterpretadorAlimentosTests
    {
        [TestMethod]
        [DataRow("Grilled fish with potatoes", "served_fish")]
        [DataRow("Fried fish on a plate", "served_fish")]
        [DataRow("Fish fillet with rice and salad", "served_fish")]
        [DataRow("Cooked salmon dish", "served_fish")]
        [DataRow("Fried tilapia with patacones", "served_fish")]
        [DataRow("Shrimp ceviche", "served_fish")]
        [DataRow("Roast chicken with fries", "served_food")]
        [DataRow("Steak and potatoes", "served_food")]
        [DataRow("Raw fish", "raw_food")]
        [DataRow("Raw chicken breast", "raw_food")]
        [DataRow("Raw beef", "raw_food")]
        [DataRow("Brown eggs", "eggs")]
        [DataRow("Eggs", "eggs")]
        [DataRow("Three white eggs on table", "eggs")]
        [DataRow("Fried egg on toast", "served_food")]
        [DataRow("Fish bones and leftovers", "food_scraps")]
        public void ComidaConAnimalesOHuevos_EsComidaNoUnAnimal(string nombre, string esperada)
        {
            Assert.AreEqual(esperada, InterpretadorNombreObjeto.Clave(nombre, out _));
        }

        /// <summary>Lo que no cambia: un pez vivo sigue siendo un animal; la caja de huevos sigue siendo cartón.</summary>
        [TestMethod]
        [DataRow("Goldfish in aquarium", null)]
        [DataRow("Fish swimming", "other_animal")]
        [DataRow("Chicken in a farm", "bird")]
        [DataRow("Egg carton.", "cardboard")]
        [DataRow("Broken eggshell.", "food_scraps")]
        [DataRow("Plastic water bottle", "plastic_bottle")]
        public void LoQueNoEsComida_ConservaSuClave(string nombre, string? esperada)
        {
            string? clave = InterpretadorNombreObjeto.Clave(nombre, out _);
            if (esperada is null) Assert.AreNotEqual("served_fish", clave);
            else Assert.AreEqual(esperada, clave);
        }
    }

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        [DataRow("served_fish", "🍽️ Identifiqué comida preparada, que incluye pescado y acompañamientos.")]
        [DataRow("served_food", "🍽️ Identifiqué comida preparada.")]
        [DataRow("eggs", "🥚 Identifiqué huevos.")]
        [DataRow("raw_food", "🥩 Identifiqué carne o pescado crudo.")]
        public async Task Alimentos_PreguntanSiSeDesechanSinAbrirNiCrearSolicitud(string clave, string inicio)
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => Visto(clave))));

            Assert.AreEqual(inicio + "\n" + MensajesTelegram.PreguntaDesecharAlimentos, r.Texto);
            CollectionAssert.AreEqual(new[] { "🗑️ Sí, desechar", "🔎 No, solo consultar" }, r.Botones.Select(b => b.Texto).ToArray());
            Assert.DoesNotContain("animal", r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsNull(_conversaciones.Leer(Vinculado)?.IdInteraccion, "Una consulta de comida no abre una solicitud.");
        }

        [TestMethod]
        public async Task SiDesechar_OrientaALaVerdeConCeroEcopuntosYNoAbre()
        {
            var eco = Eco(() => Visto("served_fish"));
            await EnviarFotoAsync(eco);

            RespuestaBot r = Unica(await eco.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, ServicioConversacionTelegram.PrefijoMenu + "desechar-comida")));

            StringAssert.Contains(r.Texto, "restos de comida");
            StringAssert.Contains(r.Texto, "verde");
            StringAssert.Contains(r.Texto, "Ecopuntos: 0, según la política actual para residuos orgánicos.");
            Assert.IsEmpty(_interacciones.Ordenes, "La foto de comida no abre ninguna tapa.");
        }

        [TestMethod]
        public async Task NoSoloConsultar_NoRegistraNada()
        {
            var eco = Eco(() => Visto("eggs"));
            await EnviarFotoAsync(eco);

            RespuestaBot r = Unica(await eco.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, null, ServicioConversacionTelegram.PrefijoMenu + "solo-consultar")));

            Assert.AreEqual(MensajesTelegram.SoloConsultaAlimentos, r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        /// <summary>Los restos evidentes (cáscaras, sobras) siguen yendo directo a su orientación, sin la pregunta.</summary>
        [TestMethod]
        public async Task RestosEvidentes_NoPreguntan()
        {
            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => Visto("food_scraps"))));

            Assert.DoesNotContain("¿Deseas desechar", r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
