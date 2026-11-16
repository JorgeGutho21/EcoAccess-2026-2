using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// V023 contra Oracle real (el esquema aislado ECOACCESS_P3_PRUEBAS, o la base que tenga la migración):
    /// una consulta se guarda una sola vez aunque Telegram reintente, no crea solicitudes, depósitos, órdenes ni
    /// ecopuntos, aparece en los agregados y, si se borra la persona, queda anónima en vez de bloquear el borrado.
    /// En una base sin V023 la prueba queda inconclusa: no se declara aprobada sin la tabla.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    public sealed class ConsultasVisionOracleTests : BaseIntegracionOracle
    {
        private async Task<int> ContarClaveAsync(string clave)
        {
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand comando = ComandosOracle.Crear(conexion, "SELECT COUNT(*) FROM consulta_vision WHERE clave=:clave");
            comando.Agregar("clave", OracleDbType.Varchar2, clave);
            return ConversionOracle.Entero(await comando.ExecuteScalarAsync() ?? 0);
        }

        private async Task<object> UsuarioDeClaveAsync(string clave)
        {
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand comando = ComandosOracle.Crear(conexion, "SELECT id_usuario FROM consulta_vision WHERE clave=:clave");
            comando.Agregar("clave", OracleDbType.Varchar2, clave);
            return await comando.ExecuteScalarAsync() ?? DBNull.Value;
        }

        private async Task EjecutarAsync(string sql, string nombre, object valor)
        {
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            comando.Agregar(nombre, valor is int ? OracleDbType.Int32 : OracleDbType.Varchar2, valor);
            await comando.ExecuteNonQueryAsync();
        }

        [TestMethod]
        public async Task ConsultaSeGuardaUnaVez_NoEsDeposito_ApareceEnAgregados_YQuedaAnonimaAlBorrarLaPersona()
        {
            var repositorio = new RepositorioConsultasVisionOracle(Fabrica);
            if (!await repositorio.TablaDisponibleAsync()) Assert.Inconclusive("Esta base no tiene V023 (CONSULTA_VISION): prueba no ejecutada.");

            string sufijo = Sufijo();
            int usuario = RegistrarUsuario(await new RepositorioUsuariosOracle(Fabrica).AgregarAsync(
                new Usuario("Ciudadano ficticio de consultas", $"it.vision.{sufijo.ToLowerInvariant()}@example.invalid", CodigoRol.Ciudadano, DateTime.UtcNow)));
            const string Operativas = "SELECT (SELECT COUNT(*) FROM interaccion)+(SELECT COUNT(*) FROM deposito)+(SELECT COUNT(*) FROM orden_apertura)+(SELECT COUNT(*) FROM movimiento_ecopuntos) FROM dual";
            int antes = await ContarAsync(Operativas);
            DateTime ahora = DateTime.UtcNow;
            string clave = "it:" + sufijo;
            try
            {
                var consulta = new ConsultaVisionRegistrada(clave, usuario, CanalConsultaVision.Telegram, ModoConsultaVision.Software,
                    "ollama-local:prueba", "can", ContextoObjeto.ResiduoAprovechable, ResultadoClasificacion.Identificado,
                    null, AdmisionMaterial.Admitido, null, 1234, ahora);
                await repositorio.GuardarAsync(consulta);
                await repositorio.GuardarAsync(consulta); // Telegram reintenta la misma actualización.
                Assert.AreEqual(1, await ContarClaveAsync(clave), "Una actualización reintentada no debe crear otra fila.");
                Assert.AreEqual(antes, await ContarAsync(Operativas), "Una consulta no crea solicitudes, depósitos, órdenes ni ecopuntos.");

                var propia = await repositorio.ResumirDeUsuarioAsync(usuario, ahora.AddMinutes(-1), ahora.AddMinutes(1));
                Assert.AreEqual(1, propia.Total);
                Assert.AreEqual(1, propia.ConDestino);
                var periodo = await repositorio.ResumirPeriodoAsync(ahora.AddMinutes(-1), ahora.AddMinutes(1));
                Assert.IsNotNull(periodo);
                Assert.IsTrue(periodo!.Total >= 1 && periodo.Ciudadanos >= 1);
                Assert.IsTrue(await ContarAsync("SELECT COUNT(*) FROM ea_r_consultas_vision") >= 1, "La vista agregada debe ver la consulta.");

                // Borrar a la persona no queda bloqueado por la consulta: la fila se conserva sin usuario.
                await EjecutarAsync("DELETE FROM usuario_rol WHERE id_usuario=:id", "id", usuario);
                await EjecutarAsync("DELETE FROM usuario WHERE id_usuario=:id", "id", usuario);
                Assert.AreEqual(DBNull.Value, await UsuarioDeClaveAsync(clave));
            }
            finally
            {
                await EjecutarAsync("DELETE FROM consulta_vision WHERE clave=:clave", "clave", clave);
            }
        }
    }
}
