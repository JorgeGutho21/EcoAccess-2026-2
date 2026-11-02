using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class EcopuntosOracleTests
    {
        [TestMethod]
        [DataRow("activo")]
        [DataRow("rol")]
        [DataRow("consentimiento")]
        public async Task AutorizacionRevalidaCiudadanoRevocadoMientrasEsperaMaterial(string revocacion)
        {
            var e = await PrepararAsync();
            var deposito = (await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!;
            var previa = (await e.RepoInteracciones.ObtenerOrdenAsync(deposito.IdOrden))!;
            int interaccion = (await e.Interacciones.IniciarAsync(e.Ciudadano, previa.IdContenedor, CanalInteraccion.Telegram)).Valor;
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            if (revocacion == "rol") await usuarios.AgregarRolAsync(e.Ciudadano, CodigoRol.Operario);
            using var retenida = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await retenida.EjecutarAsync("SELECT id_material FROM material WHERE id_material=:id FOR UPDATE", e.Material);
            string clave = "autorizar-revocado-" + Sufijo();
            var pendiente = e.Interacciones.AutorizarAperturaAsync(e.Ciudadano, interaccion, e.Material, clave);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(pendiente);
            if (revocacion == "activo") await usuarios.CambiarActivoAsync(e.Ciudadano, false);
            else if (revocacion == "rol") await usuarios.QuitarRolAsync(e.Ciudadano, CodigoRol.Ciudadano);
            else await usuarios.RevocarConsentimientoAsync(e.Ciudadano);
            await retenida.ConfirmarAsync();
            Assert.IsFalse((await pendiente).Exitoso, "La emisión no debe conservar la autorización anterior a la espera.");
            Assert.IsNull(await e.RepoInteracciones.ObtenerOrdenPorClaveAsync(clave));
            Assert.AreEqual(EstadoInteraccion.Iniciada, (await e.RepoInteracciones.ObtenerInteraccionAsync(interaccion))!.Estado);
        }

        [TestMethod]
        [DataRow(3)]
        [DataRow(15)]
        public async Task EmisionQueCruzaVencimientoMientrasEsperaNoCreaOrden(int minutos)
        {
            var e = await PrepararAsync();
            var previa = (await e.RepoInteracciones.ObtenerOrdenAsync((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.IdOrden))!;
            int id = (await e.Interacciones.IniciarAsync(e.Ciudadano, previa.IdContenedor, CanalInteraccion.Telegram)).Valor;
            using var retenida = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await retenida.EjecutarAsync("SELECT id_material FROM material WHERE id_material=:id FOR UPDATE", e.Material);
            string clave = "ttl-espera-" + Sufijo();
            var pendiente = e.Interacciones.AutorizarAperturaAsync(e.Ciudadano, id, e.Material, clave);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(pendiente);
            e.Reloj.Ahora = e.Reloj.Ahora.AddMinutes(minutos);
            await retenida.ConfirmarAsync();
            var resultado = await pendiente;
            Assert.IsFalse(resultado.Exitoso);
            Assert.AreEqual(minutos == 15 ? EcoAccess.P3.Logica.Contratos.MensajesInteracciones.SolicitudVencida
                : EcoAccess.P3.Logica.Contratos.MensajesInteracciones.OrdenNoVigente, resultado.Mensaje);
            Assert.IsNull(await e.RepoInteracciones.ObtenerOrdenPorClaveAsync(clave));
            Assert.AreEqual(EstadoInteraccion.Iniciada, (await e.RepoInteracciones.ObtenerInteraccionAsync(id))!.Estado);
        }

        [TestMethod]
        public async Task RepositorioNoConfiaEnTitularDistintoDentroDeObjetoAutorizado()
        {
            var e = await PrepararAsync();
            var previa = (await e.RepoInteracciones.ObtenerOrdenAsync((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.IdOrden))!;
            int id = (await e.Interacciones.IniciarAsync(e.Ciudadano, previa.IdContenedor, CanalInteraccion.Telegram)).Valor;
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            await usuarios.AgregarRolAsync(e.Operario, CodigoRol.Ciudadano);
            await usuarios.RegistrarConsentimientoAsync(e.Operario, "prueba", Inicio);
            var falsa = new Interaccion(id, previa.IdContenedor, e.Operario, CanalInteraccion.Telegram,
                EstadoInteraccion.Autorizada, null, null, e.Material, null, null, Inicio, null);
            string clave = "titular-falso-" + Sufijo();
            var orden = new OrdenApertura(id, previa.IdContenedor, clave, Inicio, Inicio.AddMinutes(2));
            await Assert.ThrowsExactlyAsync<EcoAccess.P3.Logica.Contratos.CambioConcurrenteException>(() =>
                e.RepoInteracciones.EmitirOrdenAsync(falsa, orden));
            Assert.IsNull(await e.RepoInteracciones.ObtenerOrdenPorClaveAsync(clave));
        }
    }
}
