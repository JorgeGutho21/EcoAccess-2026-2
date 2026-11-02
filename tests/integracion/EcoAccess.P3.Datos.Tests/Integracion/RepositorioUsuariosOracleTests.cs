using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositorioUsuariosOracleTests : BaseIntegracionOracle
    {
        private const string SqlAdministradoresActivos =
            "SELECT COUNT(*) FROM usuario u JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario " +
            "JOIN rol r ON r.id_rol = ur.id_rol WHERE r.codigo = 3 AND u.activo = 1";

        private static readonly DateTime Fecha = new(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

        private static Usuario Nuevo(string sufijo, CodigoRol rol) =>
            new("Persona de integración", $"it.{sufijo.ToLowerInvariant()}@ejemplo.com", rol, Fecha);

        [TestMethod]
        public async Task Usuario_RolesYConsentimiento_SeGuardanYRecuperan()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            string sufijo = Sufijo();

            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(sufijo, CodigoRol.Ciudadano)));
            await repositorio.AgregarRolAsync(id, CodigoRol.Operario);
            await repositorio.AgregarRolAsync(id, CodigoRol.Operario);
            await repositorio.QuitarRolAsync(id, CodigoRol.Ciudadano);
            Assert.IsTrue(await repositorio.RegistrarConsentimientoAsync(id, "v1", Fecha.AddDays(1)));
            Usuario releido = (await repositorio.ObtenerPorIdAsync(id))!;

            CollectionAssert.AreEquivalent(new[] { CodigoRol.Operario }, releido.Roles.ToArray());
            Assert.AreEqual("v1", releido.VersionConsentimiento);
            Assert.AreEqual(Fecha.AddDays(1), releido.FechaConsentimientoUtc);
            Assert.AreEqual(Fecha, releido.FechaRegistroUtc);
            Assert.IsTrue(await repositorio.ExisteCorreoAsync(releido.CorreoElectronico));
        }

        [TestMethod]
        public async Task Usuario_CorreoRepetido_LanzaConflictoConNombreDeRestriccion()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            string sufijo = Sufijo();
            RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(sufijo, CodigoRol.Ciudadano)));

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() =>
                repositorio.AgregarAsync(Nuevo(sufijo, CodigoRol.Ciudadano)));

            StringAssert.Contains(error.Message, "uq_usuario_correo");
        }

        [TestMethod]
        public async Task Usuario_UltimoAdministradorDesactivado_RevierteYLanzaRn23()
        {
            if (await ContarAsync(SqlAdministradoresActivos) > 0)
            {
                Assert.Inconclusive("La base ya tiene administradores activos; esta prueba solo aplica a una base sin ellos.");
            }

            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Administrador)));

            await Assert.ThrowsExactlyAsync<UltimoAdministradorException>(() => repositorio.CambiarActivoAsync(id, false));

            Assert.IsTrue((await repositorio.ObtenerPorIdAsync(id))!.Activo, "La transacción debió revertirse.");
        }

        [TestMethod]
        public async Task Usuario_AdministradorDesactivadoConOtroActivo_SeGuarda()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Administrador)));
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Administrador)));

            await repositorio.CambiarActivoAsync(id, false);

            Assert.IsFalse((await repositorio.ObtenerPorIdAsync(id))!.Activo);
        }

        [TestMethod]
        public async Task Usuario_QuitarUnicoRol_RevierteYLanzaRn01()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Ciudadano)));

            await Assert.ThrowsExactlyAsync<UltimoRolException>(() => repositorio.QuitarRolAsync(id, CodigoRol.Ciudadano));

            CollectionAssert.AreEquivalent(new[] { CodigoRol.Ciudadano }, (await repositorio.ObtenerPorIdAsync(id))!.Roles.ToArray());
        }

        [TestMethod]
        public async Task Usuario_ConsentimientoEnCuentaInactiva_NoSeGuardaNiLaReactiva()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Ciudadano)));
            await repositorio.CambiarActivoAsync(id, false);

            Assert.IsFalse(await repositorio.RegistrarConsentimientoAsync(id, "v1", Fecha.AddDays(1)));

            Usuario releido = (await repositorio.ObtenerPorIdAsync(id))!;
            Assert.IsFalse(releido.Activo);
            Assert.IsFalse(releido.TieneConsentimiento);
        }

        [TestMethod]
        public async Task Usuario_RevocarConsentimiento_NoTocaEstadoNiRoles()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Ciudadano)));
            await repositorio.RegistrarConsentimientoAsync(id, "v1", Fecha.AddDays(1));
            await repositorio.AgregarRolAsync(id, CodigoRol.Operario);
            await repositorio.CambiarActivoAsync(id, false);

            await repositorio.RevocarConsentimientoAsync(id);

            Usuario releido = (await repositorio.ObtenerPorIdAsync(id))!;
            Assert.IsFalse(releido.TieneConsentimiento);
            Assert.IsFalse(releido.Activo);
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Ciudadano, CodigoRol.Operario }, releido.Roles.ToArray());
        }

        /// <summary>
        /// Hallazgo del PR #1: otra sesión desactiva la cuenta sin confirmar. El consentimiento espera el bloqueo,
        /// ve la cuenta inactiva tras la confirmación y no se guarda ni la reactiva.
        /// </summary>
        [TestMethod]
        public async Task Usuario_ConsentimientoMientrasOtraSesionDesactiva_EsperaYNoSeGuarda()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Ciudadano)));
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("UPDATE usuario SET activo = 0 WHERE id_usuario = :id", id);

            Task<bool> operacion = repositorio.RegistrarConsentimientoAsync(id, "v1", Fecha.AddDays(1));
            await SesionAjenaOracle.AfirmarQueEsperaAsync(operacion);
            await otra.ConfirmarAsync();

            Assert.IsFalse(await operacion);
            Usuario releido = (await repositorio.ObtenerPorIdAsync(id))!;
            Assert.IsFalse(releido.Activo);
            Assert.IsFalse(releido.TieneConsentimiento);
        }

        /// <summary>Si otra sesión retiene la cuenta más de 10 s, el error de espera (ORA-00054 en 26ai) se traduce y el consentimiento no cambia.</summary>
        [TestMethod]
        public async Task Usuario_RevocarConFilaRetenidaMasDeDiezSegundos_LanzaCambioConcurrente()
        {
            var repositorio = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await repositorio.AgregarAsync(Nuevo(Sufijo(), CodigoRol.Ciudadano)));
            await repositorio.RegistrarConsentimientoAsync(id, "v1", Fecha.AddDays(1));
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_usuario FROM usuario WHERE id_usuario = :id FOR UPDATE", id);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => repositorio.RevocarConsentimientoAsync(id));

            otra.Dispose();
            Assert.IsTrue((await repositorio.ObtenerPorIdAsync(id))!.TieneConsentimiento);
        }
    }
}
