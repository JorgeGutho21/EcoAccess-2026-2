using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Seguridad visual tras la ronda 5: «envase plástico» puede ser icopor y «bolsa plástica» puede ser un empaque
    /// metalizado (no aprovechables). Aunque el material esté admitido en la caneca y la segunda lectura coincida, la foto
    /// solo orienta: no abre, no ofrece «Sí, continuar» y explica cómo distinguirlos.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        [DataRow("plastic_container", "PLASTICO_RIGIDO", "Recipiente plástico rígido", "icopor")]
        [DataRow("plastic_bag", "BOLSA_PLASTICA", "Bolsa plástica", "metalizada")]
        public async Task ObjetoConfundibleSoloOrienta_NoAbreNiOfreceDepositar(string clave, string codigo, string nombre, string pista)
        {
            var material = _materiales.Sembrar(codigo, nombre, CategoriaMaterial.Aprovechable);
            _caneca.AdmitirMaterial(material.Id);

            RespuestaBot r = Unica(await EnviarFotoAsync(Eco(() => Visto(clave, codigo))));

            Assert.IsEmpty(_interacciones.Ordenes, "La foto de un objeto confundible nunca abre la caneca.");
            Assert.IsFalse(r.Botones.Any(b => b.Texto.Contains("Sí, continuar")), "Tampoco ofrece depositar con un toque.");
            StringAssert.Contains(r.Texto, pista);
        }

        [TestMethod]
        public void LaListaDeObjetosConfundiblesEsExplicita()
        {
            CollectionAssert.AreEquivalent(new[] { "plastic_container", "plastic_bag" },
                ServicioClasificacionImagen.MensajesObjetosConfundibles.Keys.ToArray());
        }
    }
}
