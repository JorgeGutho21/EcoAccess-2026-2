using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class MaterialTests
    {
        private static Material CrearPet() =>
            new("PET", "Botella plástica PET", CategoriaMaterial.Aprovechable, "Vacía, limpia y aplastada");

        [TestMethod]
        public void Constructor_DatosValidos_CreaMaterialNuevoHabilitado()
        {
            Material material = CrearPet();

            Assert.AreEqual(0, material.Id);
            Assert.IsTrue(material.EsNuevo);
            Assert.AreEqual("PET", material.Codigo);
            Assert.AreEqual("Botella plástica PET", material.Nombre);
            Assert.AreEqual(CategoriaMaterial.Aprovechable, material.Categoria);
            Assert.AreEqual("Vacía, limpia y aplastada", material.Descripcion);
            Assert.IsTrue(material.Habilitado);
        }

        [TestMethod]
        public void Constructor_CodigoEnMinusculasConEspacios_LoNormalizaEnMayusculas()
        {
            var material = new Material("  carton_corrugado ", "Cartón corrugado", CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual("CARTON_CORRUGADO", material.Codigo);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("P")]
        [DataRow("1PET")]
        [DataRow("_PET")]
        [DataRow("CARTÓN")]
        [DataRow("PET-1")]
        [DataRow("PET 1")]
        [DataRow("ınk")]       // «ı» sin punto: ToUpperInvariant la convertiría en «I» si se validara después
        [DataRow("ſodio")]     // «ſ» (s larga): mismo caso
        public void Constructor_CodigoInvalido_LanzaArgumentException(string? codigo)
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Material(codigo!, "Botella", CategoriaMaterial.Aprovechable, null));
        }

        [TestMethod]
        public void Constructor_CodigoYNombreEnElMaximo_LosAcepta()
        {
            string codigo = new('A', Material.LongitudMaximaCodigo);
            string nombre = new('n', Material.LongitudMaximaNombre);

            var material = new Material(codigo, nombre, CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual(codigo, material.Codigo);
            Assert.AreEqual(nombre, material.Nombre);
        }

        [TestMethod]
        public void Constructor_CodigoMasLargoQueElMaximo_LanzaArgumentException()
        {
            string codigo = new('A', Material.LongitudMaximaCodigo + 1);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Material(codigo, "Botella", CategoriaMaterial.Aprovechable, null));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("B")]
        public void Constructor_NombreVacioOMuyCorto_LanzaArgumentException(string? nombre)
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Material("PET", nombre!, CategoriaMaterial.Aprovechable, null));
        }

        [TestMethod]
        public void Constructor_CategoriaNoDefinida_LanzaArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Material("PET", "Botella", (CategoriaMaterial)0, null));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("    ")]
        public void Constructor_DescripcionVacia_LaGuardaComoNull(string? descripcion)
        {
            var material = new Material("PET", "Botella", CategoriaMaterial.Aprovechable, descripcion);

            Assert.IsNull(material.Descripcion);
        }

        [TestMethod]
        public void Constructor_DescripcionMasLargaQueElMaximo_LanzaArgumentException()
        {
            string larga = new('x', Material.LongitudMaximaDescripcion + 1);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Material("PET", "Botella", CategoriaMaterial.Aprovechable, larga));
        }

        [TestMethod]
        public void ConstructorReconstruccion_DatosValidos_ConservaIdYEstado()
        {
            var material = new Material(4, "PILA", "Pila o batería", CategoriaMaterial.Peligroso, null, habilitado: false);

            Assert.AreEqual(4, material.Id);
            Assert.IsFalse(material.EsNuevo);
            Assert.AreEqual(CategoriaMaterial.Peligroso, material.Categoria);
            Assert.IsFalse(material.Habilitado);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-3)]
        public void ConstructorReconstruccion_IdNoPositivo_LanzaArgumentOutOfRangeException(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Material(id, "PET", "Botella", CategoriaMaterial.Aprovechable, null, true));
        }

        [TestMethod]
        public void ConstructorReconstruccion_CodigoInvalido_LanzaArgumentException()
        {
            // La reconstrucción reutiliza el constructor de registro, así que valida lo mismo.
            Assert.ThrowsExactly<ArgumentException>(
                () => new Material(4, "pet!", "Botella", CategoriaMaterial.Aprovechable, null, true));
        }

        [TestMethod]
        public void DeshabilitarYHabilitar_CambianElEstadoYSonIdempotentes()
        {
            Material material = CrearPet();

            material.Deshabilitar();
            material.Deshabilitar();
            Assert.IsFalse(material.Habilitado);

            material.Habilitar();
            Assert.IsTrue(material.Habilitado);
        }

        [TestMethod]
        public void CategoriaMaterial_ValoresNumericos_SonLosAprobados()
        {
            // Los números se guardarán en Oracle; si alguien los cambia, los datos guardados cambiarían de significado.
            Assert.AreEqual(1, (int)Compat.ParseEnum<CategoriaMaterial>("Aprovechable"));
            Assert.AreEqual(2, (int)Compat.ParseEnum<CategoriaMaterial>("OrganicoAprovechable"));
            Assert.AreEqual(3, (int)Compat.ParseEnum<CategoriaMaterial>("NoAprovechable"));
            Assert.AreEqual(4, (int)Compat.ParseEnum<CategoriaMaterial>("Peligroso"));
            Assert.HasCount(4, Compat.GetNames<CategoriaMaterial>());
        }
    }
}
