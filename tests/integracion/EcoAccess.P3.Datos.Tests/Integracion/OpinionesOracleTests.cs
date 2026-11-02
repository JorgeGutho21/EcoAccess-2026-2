using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class OpinionesOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task ReintentosConcurrentesDesdeDosServiciosGuardanUnaValoracionYAgreganEnOracle()
        {
            string sesion = "ensayo-opinion-" + Guid.NewGuid().ToString("N");
            string participante = Compat.ToHexString(Compat.Sha256(Encoding.UTF8.GetBytes("invitado:" + sesion))).ToLowerInvariant();
            var repoA = new RepositorioOpinionesOracle(Fabrica);
            var repoB = new RepositorioOpinionesOracle(Fabrica);
            var servicioA = new ServicioOpiniones(new RepositorioUsuariosOracle(Fabrica), repoA, TimeProvider.System);
            var servicioB = new ServicioOpiniones(new RepositorioUsuariosOracle(Fabrica), repoB, TimeProvider.System);
            try
            {
                var resultados = await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
                    (i % 2 == 0 ? servicioA : servicioB).RegistrarInvitadoAsync(sesion, 4, true)));
                Assert.IsTrue(resultados.All(r => r.Exitoso));
                DateTime dia = DateTime.UtcNow.Date;
                var leida = await repoB.ObtenerAsync(participante, dia);
                Assert.IsNotNull(leida); Assert.AreEqual(4, leida.Estrellas); Assert.IsNull(leida.IdUsuario);
                Assert.AreEqual(MotivoRechazo.Duplicado, (await servicioB.RegistrarInvitadoAsync(sesion, 5, true)).Motivo);
                var resumen = await repoB.ResumirAsync(dia, dia.AddDays(1));
                Assert.IsGreaterThanOrEqualTo(1, resumen.Single().Cantidad);
                Assert.IsTrue(resumen.Single().Promedio is >= 1m and <= 5m);
                using var conexion = await Fabrica.AbrirAsync(CancellationToken.None);
                using var contar = ComandosOracle.Crear(conexion, "SELECT COUNT(*) FROM opinion WHERE participante=:p");
                contar.Agregar("p", OracleDbType.Varchar2, participante);
                Assert.AreEqual(1, ConversionOracle.Entero((await contar.ExecuteScalarAsync())!));
                using var invalida = ComandosOracle.Crear(conexion,
                    "INSERT INTO opinion(participante,estrellas,comentario,fecha_utc) VALUES(:p,5,'comentario invitado no permitido',:fecha)");
                invalida.Agregar("p", OracleDbType.Varchar2, new string('a', 64));
                invalida.Agregar("fecha", OracleDbType.TimeStamp, DateTime.UtcNow);
                var error = await Assert.ThrowsExactlyAsync<OracleException>(() => invalida.ExecuteNonQueryAsync());
                Assert.AreEqual(2290, error.Number);
            }
            finally
            {
                // Únicamente la fila propia del ensayo; no se purgan opiniones ni datos preexistentes.
                using var conexion = await Fabrica.AbrirAsync(CancellationToken.None);
                using var limpiar = ComandosOracle.Crear(conexion, "DELETE FROM opinion WHERE participante=:p");
                limpiar.Agregar("p", OracleDbType.Varchar2, participante);
                await limpiar.ExecuteNonQueryAsync();
            }
        }
    }
}
