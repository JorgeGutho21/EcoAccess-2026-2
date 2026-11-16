using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Caso real del 12/11/2026: tras abrirse la NEGRA con una bolsa de agua, la persona escribió que era plástico y el bot lo
    /// guardó como comentario genérico. Una corrección o un problema de caneca debe quedar como INCIDENCIA verificable.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        [DataRow("Era plástico y se abrió la caneca negra", CategoriaComentario.Caneca)]
        [DataRow("Abrió la caneca equivocada", CategoriaComentario.Caneca)]
        [DataRow("Es una bolsa plástica, no un empaque multicapa", CategoriaComentario.Reconocimiento)]
        [DataRow("No es multicapa, es plástico", CategoriaComentario.Reconocimiento)]
        public async Task UnaCorreccionQuedaComoIncidenciaVerificable(string texto, CategoriaComentario categoria)
        {
            await EnviarConAsync(EcoConversador(), texto);

            ComentarioCiudadano fila = _comentarios.Filas.Single();
            Assert.AreEqual(TipoComentario.ProblemaTecnico, fila.Tipo);
            Assert.AreEqual(categoria, fila.Categoria);
            Assert.IsNotNull(fila.Comentario);
        }

        [TestMethod]
        public async Task UnaCorreccionEscritaCuandoSeEsperabaUnComentarioTambienEsIncidencia()
        {
            var eco = EcoConversador();
            await EnviarConAsync(eco, boton: "menu:queja:otro");
            RespuestaBot r = Unica(await EnviarConAsync(eco, "No era esa caneca, era plástico"));

            ComentarioCiudadano fila = _comentarios.Filas.Single();
            Assert.AreEqual(TipoComentario.ProblemaTecnico, fila.Tipo);
            Assert.AreEqual(CategoriaComentario.Caneca, fila.Categoria);
            Assert.AreEqual(MensajesConversacion.IncidenciaRegistrada, r.Texto);
        }

        [TestMethod]
        public async Task UnComentarioNormalSigueSiendoComentario()
        {
            var eco = EcoConversador();
            await EnviarConAsync(eco, boton: "menu:queja:otro");
            await EnviarConAsync(eco, "Me gustaría que hubiera más canecas en la universidad");

            ComentarioCiudadano fila = _comentarios.Filas.Single();
            Assert.AreEqual(TipoComentario.Comentario, fila.Tipo);
            Assert.AreEqual(CategoriaComentario.Otro, fila.Categoria);
        }
    }
}
