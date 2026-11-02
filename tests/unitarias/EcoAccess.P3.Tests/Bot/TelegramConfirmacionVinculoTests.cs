using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        [TestMethod]
        public async Task TelegramConfirmar_CompletaSegundoCanalSinOrdenYRechazaRepeticion()
        {
            _usuarios.AgregarCiudadano(4);
            var vincular = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
            string token = (await vincular.SolicitarAsync(Invitado, Invitado)).Valor!.Token;
            string codigo = (await vincular.PrepararVinculacionCuentaExistenteAsync(token, 4)).Valor!.Codigo;
            Assert.IsNull(await vincular.ObtenerUsuarioVinculadoAsync(Invitado));
            string mensaje = Unica(await TextoAsync("/confirmar " + codigo, Invitado)).Texto;
            StringAssert.Contains(mensaje, "quedó vinculado");
            Assert.AreEqual(4, await vincular.ObtenerUsuarioVinculadoAsync(Invitado));
            string repetido = Unica(await TextoAsync("/confirmar " + codigo, Invitado)).Texto;
            Assert.DoesNotContain("quedó vinculado", repetido);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task TelegramConfirmar_SinCodigoExplicaElPasoSinPedirContrasena()
        {
            var respuesta = Unica(await TextoAsync("/confirmar", Invitado));
            StringAssert.Contains(respuesta.Texto, "página HTTPS");
            StringAssert.Contains(respuesta.Texto, "No envíes tu contraseña");
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
