using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class RolTests
    {
        [TestMethod]
        public void Constructor_DatosValidos_CreaRolConDescripcionRecortada()
        {
            var rol = new Rol(3, CodigoRol.Administrador, "  Configura catálogos y usuarios  ");

            Assert.AreEqual(3, rol.Id);
            Assert.AreEqual(CodigoRol.Administrador, rol.Codigo);
            Assert.AreEqual("Configura catálogos y usuarios", rol.Descripcion);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-5)]
        public void Constructor_IdNoPositivo_LanzaArgumentOutOfRangeException(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rol(id, CodigoRol.Ciudadano, "Ciudadano"));
        }

        [TestMethod]
        public void Constructor_CodigoNoDefinido_LanzaArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rol(1, (CodigoRol)0, "Sin rol"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void Constructor_DescripcionVacia_LanzaArgumentException(string? descripcion)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Rol(1, CodigoRol.Ciudadano, descripcion!));
        }

        [TestMethod]
        public void Constructor_DescripcionMuyLarga_LanzaArgumentException()
        {
            string larga = new('x', Rol.LongitudMaximaDescripcion + 1);

            Assert.ThrowsExactly<ArgumentException>(() => new Rol(1, CodigoRol.Ciudadano, larga));
        }

        [TestMethod]
        public void CodigoRol_ValoresNumericos_SonLosAprobados()
        {
            // Los valores deben coincidir con la columna CODIGO del catálogo ROL. Se leen con Enum.Parse
            // en tiempo de ejecución: comparar constantes como (int)CodigoRol.Operario no probaría nada (aviso MSTEST0032).
            Assert.AreEqual(1, (int)Compat.ParseEnum<CodigoRol>("Ciudadano"));
            Assert.AreEqual(2, (int)Compat.ParseEnum<CodigoRol>("Operario"));
            Assert.AreEqual(3, (int)Compat.ParseEnum<CodigoRol>("Administrador"));
            Assert.HasCount(3, Compat.GetNames<CodigoRol>());
        }
    }
}
