using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Reutiliza el fixture real y su limpieza de datos ficticios; no crea un segundo esquema.
    public sealed partial class EcopuntosOracleTests
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task VerificacionRevalidaActorRevocadoMientrasEsperaMaterial(bool desactivar)
        {
            var e = await PrepararAsync(verificar: false);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            await usuarios.AgregarRolAsync(e.Operario, CodigoRol.Ciudadano);
            using var retenida = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await retenida.EjecutarAsync("SELECT id_material FROM material WHERE id_material=:id FOR UPDATE", e.Material);
            var pendiente = e.Interacciones.VerificarDepositoAsync(e.Operario, e.Deposito, e.Material);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(pendiente);
            if (desactivar) await usuarios.CambiarActivoAsync(e.Operario, false);
            else await usuarios.QuitarRolAsync(e.Operario, CodigoRol.Operario);
            await retenida.ConfirmarAsync();
            var resultado = await pendiente;
            Assert.IsFalse(resultado.Exitoso, "La autorización anterior a la espera no debe bastar para verificar.");
            Assert.IsFalse((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.EstaVerificado);
        }

        [TestMethod]
        public async Task VerificacionHistoricaPermiteMaterialExistenteDeshabilitado()
        {
            var e = await PrepararAsync(verificar: false);
            var deposito = (await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!;
            var orden = (await e.RepoInteracciones.ObtenerOrdenAsync(deposito.IdOrden))!;
            await new RepositorioContenedoresOracle(Fabrica).CambiarEstadoAsync(orden.IdContenedor, EstadoContenedor.FueraDeServicio);
            await new RepositorioMaterialesOracle(Fabrica).DeshabilitarAsync(e.Material);
            var resultado = await e.Interacciones.VerificarDepositoAsync(e.Operario, e.Deposito, e.Material);
            Assert.IsTrue(resultado.Exitoso, resultado.Mensaje);
            Assert.AreEqual(e.Material, (await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.IdMaterialVerificado);
        }

        [TestMethod]
        public async Task RepositorioRechazaVerificacionDirectaDelTitularAunqueTengaRolOperario()
        {
            var e = await PrepararAsync(verificar: false);
            await new RepositorioUsuariosOracle(Fabrica).AgregarRolAsync(e.Ciudadano, CodigoRol.Operario);
            var deposito = (await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!;
            deposito.Verificar(e.Material, e.Ciudadano, Inicio.AddMinutes(1));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.RepoInteracciones.VerificarDepositoAsync(deposito));
            Assert.IsFalse((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.EstaVerificado);
        }

        [TestMethod]
        public async Task VerificacionesConcurrentesPersistenUnaSolaEvidenciaIndependiente()
        {
            var e = await PrepararAsync(verificar: false);
            using var retenida = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await retenida.EjecutarAsync("SELECT id_deposito FROM deposito WHERE id_deposito=:id FOR UPDATE", e.Deposito);
            var primera = e.Interacciones.VerificarDepositoAsync(e.Operario, e.Deposito, e.Material);
            var segunda = e.Interacciones.VerificarDepositoAsync(e.Administrador, e.Deposito, e.Material);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(primera);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(segunda);
            await retenida.ConfirmarAsync();
            var resultados = await Task.WhenAll(primera, segunda);
            Assert.AreEqual(1, resultados.Count(r => r.Exitoso));
            Assert.IsTrue((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.EstaVerificado);
        }

        [TestMethod]
        public async Task VerificacionConDepositoRetenidoAgotaEsperaAcotadaSinEscribir()
        {
            var e = await PrepararAsync(verificar: false);
            using var retenida = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await retenida.EjecutarAsync("SELECT id_deposito FROM deposito WHERE id_deposito=:id FOR UPDATE", e.Deposito);
            var pendiente = e.Interacciones.VerificarDepositoAsync(e.Operario, e.Deposito, e.Material);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(pendiente);
            var resultado = await pendiente.WaitAsync(TimeSpan.FromSeconds(20));
            Assert.IsFalse(resultado.Exitoso);
            Assert.AreEqual("Otra operación está modificando esos datos; inténtelo de nuevo.", resultado.Mensaje);
            Assert.IsFalse((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.EstaVerificado);
        }
    }
}
