using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class EcopuntosOracleTests
    {
        [TestMethod]
        public async Task TarifaUnitariaPersiste_DeclaraTresVerificaDos_CreditoCuatroConcurrenteIdempotente()
        {
            Caso e = await PrepararAsync(cantidadDeclarada: 3, cantidadVerificada: 2);
            int regla = (await e.Servicio.RegistrarReglaAsync(e.Administrador, e.Material, 2, "Tarifa unitaria ficticia", Inicio, porUnidad: true)).Valor;
            _reglas.Add(regla);
            Assert.IsTrue((await e.Repo.ObtenerReglaAsync(regla))!.PorUnidad);
            var potencial = await e.Servicio.ConsultarPotencialesAsync(e.Ciudadano, e.Material, 3);
            Assert.AreEqual(6, potencial.Valor.PuntosPotenciales);
            Assert.AreEqual(0L, (await e.Servicio.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
            Assert.AreEqual(2, (await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.CantidadVerificada);
            var orden = (await e.RepoInteracciones.ObtenerOrdenAsync((await e.RepoInteracciones.ObtenerDepositoAsync(e.Deposito))!.IdOrden))!;
            Assert.AreEqual(3, (await e.RepoInteracciones.ObtenerInteraccionAsync(orden.IdInteraccion))!.CantidadDeclarada);
            string clave = "unidades-" + Guid.NewGuid().ToString("N");
            var simultaneos = await Task.WhenAll(e.Servicio.AcreditarAsync(e.Administrador, e.Deposito, regla, clave),
                e.Servicio.AcreditarAsync(e.Administrador, e.Deposito, regla, clave));
            Assert.IsTrue(simultaneos.All(r => r.Exitoso));
            Assert.IsTrue(simultaneos.All(r => r.Valor.Single().Puntos == 4));
            Assert.AreEqual(4L, (await e.Servicio.ConsultarMiosAsync(e.Ciudadano)).Valor.Saldo);
        }

        [TestMethod]
        public async Task SinConteo_VerificacionMaterialNoBasta_YTriggerProtegeInclusoInsercionAntigua()
        {
            Caso e = await PrepararAsync(cantidadDeclarada: 3);
            int regla = (await e.Servicio.RegistrarReglaAsync(e.Administrador, e.Material, 2, "Unitaria sin conteo", Inicio, porUnidad: true)).Valor;
            _reglas.Add(regla);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido,
                (await e.Servicio.AcreditarAsync(e.Administrador, e.Deposito, regla, "sin-conteo-" + Sufijo())).Motivo);
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => e.Repo.AplicarAsync(new SolicitudEcopuntos(
                TipoOperacionEcopuntos.Acreditar, e.Deposito, null, regla, e.Administrador, "directo-" + Sufijo(), "Ensayo", Inicio)));
            using var conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync();
            using var operacion = ComandosOracle.Crear(conexion,
                "INSERT INTO operacion_ecopuntos(clave,tipo,id_deposito,id_actor,id_regla,motivo,fecha_utc) " +
                "VALUES(:clave,1,:deposito,:actor,:regla,'Ensayo cliente anterior',:fecha) RETURNING id_operacion INTO :id", transaccion);
            operacion.Agregar("clave", OracleDbType.Varchar2, "antiguo-" + Sufijo());
            operacion.Agregar("deposito", OracleDbType.Int32, e.Deposito);
            operacion.Agregar("actor", OracleDbType.Int32, e.Administrador);
            operacion.Agregar("regla", OracleDbType.Int32, regla);
            operacion.Agregar("fecha", OracleDbType.TimeStamp, Inicio);
            var id = operacion.AgregarIdSalida();
            await operacion.ExecuteNonQueryAsync();
            using var credito = ComandosOracle.Crear(conexion,
                "INSERT INTO movimiento_ecopuntos(id_operacion,id_deposito,id_usuario,id_regla,tipo,puntos) VALUES(:op,:dep,:usuario,:regla,1,6)", transaccion);
            credito.Agregar("op", OracleDbType.Int32, ComandosOracle.LeerId(id));
            credito.Agregar("dep", OracleDbType.Int32, e.Deposito);
            credito.Agregar("usuario", OracleDbType.Int32, e.Ciudadano);
            credito.Agregar("regla", OracleDbType.Int32, regla);
            var fallo = await Assert.ThrowsExactlyAsync<OracleException>(() => credito.ExecuteNonQueryAsync());
            Assert.AreEqual(20113, fallo.Number);
            await transaccion.RollbackAsync();
        }
    }
}
