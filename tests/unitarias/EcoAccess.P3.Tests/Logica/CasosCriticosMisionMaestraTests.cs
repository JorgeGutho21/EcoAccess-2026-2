using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Casos críticos de la misión maestra (14/11/2026, sección 1.6), a nivel de reglas: el nombre que da el modelo → clave → ruta →
    /// ¿podría abrir una caneca sola? Se escribieron a partir de la lista de la misión, no del conjunto de prueba. La parte que
    /// depende del modelo (¿tiene algo dentro?) está en VisionLocalTests.
    /// </summary>
    [TestClass]
    public sealed class CasosCriticosMisionMaestraTests
    {
        /// <summary>True si el objeto, con la lectura que lo corrobore, podría abrir la blanca, la verde o la negra sin intervención.</summary>
        private static bool PodriaAbrirSolo(string clave)
        {
            ObjetoConocido o = CatalogoObjetos.Buscar(clave)!;
            if (o.CodigoMaterial is not { } codigo) return false;
            ObjetoVisible? visible = o.Objeto ?? (codigo == CatalogoObjetos.MaterialAlimentos ? ObjetoVisible.ResiduoOrganico : null);
            return visible is { } v && (PoliticaAperturaAutomatica.Coinciden(codigo, v) || PoliticaAperturaAutomatica.CoincidenEnEstacion(codigo, v));
        }

        private static (string Clave, RutaDisposicion Ruta) Leer(string nombre)
        {
            string? clave = InterpretadorNombreObjeto.Clave(nombre, out _);
            Assert.IsNotNull(clave, nombre);
            return (clave, MatrizDecisionResiduos.Ruta(CatalogoObjetos.Buscar(clave)!));
        }

        // Ninguno de estos puede abrir una caneca por sí solo: o tienen contenido, o son especiales, o dependen de algo que la foto no dice.
        [TestMethod]
        [DataRow("Bag of drinking water", "water_bag")]
        [DataRow("Plastic bag of water", "water_bag")]
        [DataRow("Clear plastic water bag", "water_bag")]
        [DataRow("Egg carton with eggs", "egg_carton_with_eggs")]
        [DataRow("Egg tray with eggs", "egg_carton_with_eggs")]
        [DataRow("Carton of eggs", "egg_carton_with_eggs")]
        [DataRow("Eggs in a cardboard tray", "egg_carton_with_eggs")]
        [DataRow("Half full water bottle", "container_with_content")]
        [DataRow("Plastic bottle with liquid", "container_with_content")]
        [DataRow("Full soda bottle", "container_with_content")]
        [DataRow("Sealed beer can", "container_with_content")]
        [DataRow("Glass of water", "container_with_content")]
        [DataRow("Cup of coffee", "container_with_content")]
        [DataRow("Cleaning product bottle", "chemical_container")]
        [DataRow("Disinfectant bottle", "chemical_container")]
        [DataRow("Soap bottle", "liquid_soap")]
        [DataRow("Toilet paper roll", "toilet_paper_roll")]
        [DataRow("Button cell battery", "battery")]
        [DataRow("Coin cell battery", "battery")]
        [DataRow("Pill blister pack", "medicine")]
        [DataRow("Aluminum blister", "medicine")]
        [DataRow("Person holding a speaker", "electronic_other")]
        [DataRow("Smartphone in a case", "phone")]
        [DataRow("Razor blade", "disposable_razor")]
        [DataRow("Scalpel", "scalpel")]
        [DataRow("Insecticide spray", "aerosol")]
        [DataRow("Full bleach bottle", "chemical_container")]
        public void NoAbreNingunaCanecaPorSiSolo(string nombre, string claveEsperada)
        {
            (string clave, RutaDisposicion ruta) = Leer(nombre);
            Assert.AreEqual(claveEsperada, clave, nombre);
            Assert.IsFalse(PodriaAbrirSolo(clave), $"{nombre} → {clave} ({ruta}) no debe abrir ninguna caneca sin intervención.");
        }

        // La ruta que se le dice a la persona.
        [TestMethod]
        [DataRow("Razor blade", RutaDisposicion.Cortopunzante)]
        [DataRow("Utility knife blade", RutaDisposicion.Cortopunzante)]
        [DataRow("Doritos bag", RutaDisposicion.Negra)]
        [DataRow("Chicharron bag", RutaDisposicion.Negra)]
        [DataRow("Cookie wrapper", RutaDisposicion.Negra)]
        [DataRow("Watermelon rind", RutaDisposicion.Verde)]
        [DataRow("Grass clippings", RutaDisposicion.Verde)]
        [DataRow("Empanada", RutaDisposicion.Evaluar)]
        [DataRow("Salchipapa", RutaDisposicion.Evaluar)]
        [DataRow("Egg carton with eggs", RutaDisposicion.Evaluar)]
        [DataRow("Glass of water", RutaDisposicion.Evaluar)]
        [DataRow("Bag of drinking water", RutaDisposicion.Blanca)]
        public void RutaDeCasosCriticos(string nombre, RutaDisposicion esperada) => Assert.AreEqual(esperada, Leer(nombre).Ruta, nombre);

        // Lo vacío sigue igual: los cambios no quitan aperturas medidas a envases sin contenido.
        [TestMethod]
        [DataRow("Empty egg carton", "cardboard")]
        [DataRow("Plastic water bottle", "plastic_bottle")]
        [DataRow("Crushed plastic bottle", "plastic_bottle")]
        [DataRow("Soda can", "can")]
        [DataRow("Bottle of orange juice", "plastic_bottle")]
        [DataRow("Cardboard box", "cardboard")]
        public void LosEnvasesSinContenidoConservanSuClave(string nombre, string esperada) => Assert.AreEqual(esperada, Leer(nombre).Clave, nombre);

        // Una marca no basta: «Doritos» solo, sin el empaque, no se reconoce como residuo.
        [TestMethod]
        public void UnaMarcaSolaNoEsUnObjeto() => Assert.IsNull(InterpretadorNombreObjeto.Clave("Doritos", out _));
    }
}
