using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;

namespace EcoAccess.P3.Tests.Logica
{
    [TestClass]
    public sealed class AreasAccesoTests
    {
        private static SesionActiva Sesion(params CodigoRol[] roles) => new(1, "Prueba", roles, DateTime.UtcNow);

        [TestMethod]
        public void ElRolAdministradorSeMuestraComoSupervisor_SinCambiarSuCodigo()
        {
            Assert.AreEqual(3, (int)CodigoRol.Administrador);
            Assert.AreEqual("Supervisor", AreasAcceso.NombreRol(CodigoRol.Administrador));
            Assert.AreEqual("Supervisor (Administrador del sistema)", AreasAcceso.NombreRolCompleto(CodigoRol.Administrador));
            Assert.AreEqual("Operario", AreasAcceso.NombreRolCompleto(CodigoRol.Operario));
            Assert.AreEqual(CodigoRol.Administrador, AreasAcceso.RolDe(AreaAcceso.Supervisor));
            Assert.AreEqual((int)CodigoRol.Administrador, (int)AreaAcceso.Supervisor);
        }

        [TestMethod]
        [DataRow(AreaAcceso.Operario)]
        [DataRow(AreaAcceso.Supervisor)]
        public void UnCiudadanoNoEntraAGestionPorElegirla(AreaAcceso area)
        {
            var r = AreasAcceso.Elegir(Sesion(CodigoRol.Ciudadano), area);
            Assert.IsFalse(r.Exitoso);
            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
            StringAssert.Contains(r.Mensaje, "Puedes entrar como Ciudadano");
        }

        [TestMethod]
        public void UnOperarioNoEntraComoSupervisor()
        {
            var operario = Sesion(CodigoRol.Operario);
            Assert.IsTrue(AreasAcceso.Elegir(operario, AreaAcceso.Operario).Exitoso);
            Assert.IsFalse(AreasAcceso.Elegir(operario, AreaAcceso.Supervisor).Exitoso);
            Assert.IsFalse(AreasAcceso.Elegir(operario, AreaAcceso.Ciudadano).Exitoso);
        }

        [TestMethod]
        public void UnSupervisorSoloNoEntraComoOperarioSinEseRol()
        {
            var supervisor = Sesion(CodigoRol.Administrador);
            Assert.IsTrue(AreasAcceso.Elegir(supervisor, AreaAcceso.Supervisor).Exitoso);
            Assert.IsFalse(AreasAcceso.Elegir(supervisor, AreaAcceso.Operario).Exitoso);
        }

        [TestMethod]
        public void CuentaConAmbosRoles_PuedeCambiarEntreLasDosAreasDeGestion()
        {
            var equipo = Sesion(CodigoRol.Operario, CodigoRol.Administrador);
            CollectionAssert.AreEqual(new[] { AreaAcceso.Operario, AreaAcceso.Supervisor }, (System.Collections.ICollection)AreasAcceso.DeGestion(equipo));
            Assert.IsTrue(AreasAcceso.Elegir(equipo, AreaAcceso.Operario).Exitoso);
            Assert.IsTrue(AreasAcceso.Elegir(equipo, AreaAcceso.Supervisor).Exitoso);
            Assert.IsFalse(AreasAcceso.Elegir(equipo, AreaAcceso.Ciudadano).Exitoso);
        }

        [TestMethod]
        public void SinRolesOAreaInvalida_SeRechaza()
        {
            var sinRoles = Sesion();
            Assert.AreEqual(0, AreasAcceso.Disponibles(sinRoles).Count);
            StringAssert.Contains(AreasAcceso.Elegir(sinRoles, AreaAcceso.Ciudadano).Mensaje, "supervisor");
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, AreasAcceso.Elegir(Sesion(CodigoRol.Ciudadano), (AreaAcceso)99).Motivo);
            Assert.ThrowsExactly<ArgumentNullException>(() => AreasAcceso.Elegir(null!, AreaAcceso.Ciudadano));
        }
    }
}
