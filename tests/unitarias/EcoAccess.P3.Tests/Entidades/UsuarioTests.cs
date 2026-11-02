using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class UsuarioTests
    {
        private static readonly DateTime FechaUtc = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        private static Usuario CrearCiudadano() =>
            new("Ana Pérez", "ana.perez@example.com", CodigoRol.Ciudadano, FechaUtc);

        // ---------- registro de un usuario nuevo ----------

        [TestMethod]
        public void Constructor_DatosValidos_CreaUsuarioNuevoActivoConUnRol()
        {
            Usuario usuario = CrearCiudadano();

            Assert.AreEqual(0, usuario.Id);
            Assert.IsTrue(usuario.EsNuevo);
            Assert.AreEqual("Ana Pérez", usuario.Nombre);
            Assert.IsTrue(usuario.Activo);
            Assert.AreEqual(FechaUtc, usuario.FechaRegistroUtc);
            CollectionAssert.AreEqual(new[] { CodigoRol.Ciudadano }, usuario.Roles.ToArray());
            Assert.IsFalse(usuario.TieneConsentimiento);
        }

        [TestMethod]
        public void Constructor_NombreConEspaciosAlrededor_GuardaNombreRecortado()
        {
            var usuario = new Usuario("  Ana Pérez  ", "ana@example.com", CodigoRol.Ciudadano, FechaUtc);

            Assert.AreEqual("Ana Pérez", usuario.Nombre);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("A")]
        public void Constructor_NombreVacioOMuyCorto_LanzaArgumentException(string? nombre)
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(nombre!, "ana@example.com", CodigoRol.Ciudadano, FechaUtc));
        }

        [TestMethod]
        [DataRow(Usuario.LongitudMinimaNombre)]
        [DataRow(Usuario.LongitudMaximaNombre)]
        public void Constructor_NombreEnLosLimitesDeLongitud_LoAcepta(int longitud)
        {
            string nombre = new('a', longitud);

            var usuario = new Usuario(nombre, "ana@example.com", CodigoRol.Ciudadano, FechaUtc);

            Assert.AreEqual(longitud, usuario.Nombre.Length);
        }

        [TestMethod]
        public void Constructor_NombreMasLargoQueElMaximo_LanzaArgumentException()
        {
            string nombreLargo = new('a', Usuario.LongitudMaximaNombre + 1);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(nombreLargo, "ana@example.com", CodigoRol.Ciudadano, FechaUtc));
        }

        [TestMethod]
        public void Constructor_CorreoConMayusculasYEspacios_LoNormaliza()
        {
            var usuario = new Usuario("Ana Pérez", "  Ana.Perez@Example.COM ", CodigoRol.Ciudadano, FechaUtc);

            Assert.AreEqual("ana.perez@example.com", usuario.CorreoElectronico);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("ana")]
        [DataRow("ana@")]
        [DataRow("@example.com")]
        [DataRow("ana@localhost")]
        [DataRow("ana perez@example.com")]
        [DataRow("Ana <ana@example.com>")]
        [DataRow("\"ana perez\"@example.com")]
        [DataRow("ana@[1.2.3.4]")]
        [DataRow("ana@example.")]
        [DataRow("ana@@example.com")]
        public void Constructor_CorreoInvalido_LanzaArgumentException(string? correo)
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario("Ana Pérez", correo!, CodigoRol.Ciudadano, FechaUtc));
        }

        [TestMethod]
        [DataRow("ana+eco@example.com")]
        [DataRow("ana.maria_p-2@sub.example.edu.co")]
        public void Constructor_CorreoValidoConCaracteresHabituales_LoAcepta(string correo)
        {
            var usuario = new Usuario("Ana Pérez", correo, CodigoRol.Ciudadano, FechaUtc);

            Assert.AreEqual(correo, usuario.CorreoElectronico);
        }

        [TestMethod]
        public void Constructor_ParteLocalDelCorreoEnElLimite_LaAceptaYUnoMasLaRechaza()
        {
            string limite = new string('a', Usuario.LongitudMaximaParteLocalCorreo) + "@example.com";
            string excedido = new string('a', Usuario.LongitudMaximaParteLocalCorreo + 1) + "@example.com";

            var usuario = new Usuario("Ana Pérez", limite, CodigoRol.Ciudadano, FechaUtc);

            Assert.AreEqual(limite, usuario.CorreoElectronico);
            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario("Ana Pérez", excedido, CodigoRol.Ciudadano, FechaUtc));
        }

        [TestMethod]
        [DataRow(DateTimeKind.Local)]
        [DataRow(DateTimeKind.Unspecified)]
        public void Constructor_FechaNoUtc_LanzaArgumentException(DateTimeKind tipo)
        {
            var fecha = DateTime.SpecifyKind(FechaUtc, tipo);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario("Ana Pérez", "ana@example.com", CodigoRol.Ciudadano, fecha));
        }

        [TestMethod]
        public void Constructor_RolNoDefinido_LanzaArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Usuario("Ana Pérez", "ana@example.com", (CodigoRol)99, FechaUtc));
        }

        // ---------- reconstrucción desde la base de datos ----------

        [TestMethod]
        public void ConstructorReconstruccion_DatosValidos_ConservaTodosLosValores()
        {
            var fechaConsentimiento = FechaUtc.AddMinutes(1);

            var usuario = new Usuario(
                7, "Luis Gómez", "luis@example.com", FechaUtc, activo: false,
                new[] { CodigoRol.Operario, CodigoRol.Ciudadano }, "2026-10", fechaConsentimiento);

            Assert.AreEqual(7, usuario.Id);
            Assert.IsFalse(usuario.EsNuevo);
            Assert.IsFalse(usuario.Activo);
            Assert.IsTrue(usuario.TieneRol(CodigoRol.Operario));
            Assert.IsTrue(usuario.TieneRol(CodigoRol.Ciudadano));
            Assert.AreEqual("2026-10", usuario.VersionConsentimiento);
            Assert.AreEqual(fechaConsentimiento, usuario.FechaConsentimientoUtc);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void ConstructorReconstruccion_IdNoPositivo_LanzaArgumentOutOfRangeException(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Usuario(id, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano }, null, null));
        }

        [TestMethod]
        public void ConstructorReconstruccion_SinRoles_LanzaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    Array.Empty<CodigoRol>(), null, null));
        }

        [TestMethod]
        public void ConstructorReconstruccion_CorreoConMayusculas_LoNormaliza()
        {
            var usuario = new Usuario(7, "Luis Gómez", " Luis@Example.com ", FechaUtc, true,
                new[] { CodigoRol.Ciudadano }, null, null);

            Assert.AreEqual("luis@example.com", usuario.CorreoElectronico);
        }

        [TestMethod]
        public void ConstructorReconstruccion_RolesNulos_LanzaArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    null!, null, null));
        }

        [TestMethod]
        public void ConstructorReconstruccion_RolNoDefinidoEnLaLista_LanzaArgumentOutOfRangeException()
        {
            var excepcion = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano, (CodigoRol)99 }, null, null));

            Assert.AreEqual("roles", excepcion.ParamName);
        }

        [TestMethod]
        public void ConstructorReconstruccion_RolesRepetidos_LanzaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano, CodigoRol.Ciudadano }, null, null));
        }

        [TestMethod]
        public void ConstructorReconstruccion_VersionSinFecha_LanzaArgumentException()
        {
            var excepcion = Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano }, "2026-10", null));

            Assert.AreEqual("fechaConsentimientoUtc", excepcion.ParamName);
        }

        [TestMethod]
        public void ConstructorReconstruccion_FechaSinVersion_LanzaArgumentException()
        {
            var excepcion = Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano }, null, FechaUtc));

            Assert.AreEqual("versionConsentimiento", excepcion.ParamName);
        }

        [TestMethod]
        public void ConstructorReconstruccion_FechaConsentimientoSinUtc_LanzaArgumentException()
        {
            var fechaSinTipo = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Unspecified);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", FechaUtc, true,
                    new[] { CodigoRol.Ciudadano }, "2026-10", fechaSinTipo));
        }

        [TestMethod]
        public void ConstructorReconstruccion_FechaLeidaSinUtc_LanzaArgumentException()
        {
            // Así llegaría una fecha de Oracle si la capa de datos olvida SpecifyKind.
            var fechaSinTipo = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Unspecified);

            Assert.ThrowsExactly<ArgumentException>(
                () => new Usuario(7, "Luis Gómez", "luis@example.com", fechaSinTipo, true,
                    new[] { CodigoRol.Ciudadano }, null, null));
        }

        // ---------- roles ----------

        [TestMethod]
        public void AsignarRol_RolNuevo_LoAgrega()
        {
            Usuario usuario = CrearCiudadano();

            usuario.AsignarRol(CodigoRol.Operario);

            Assert.IsTrue(usuario.TieneRol(CodigoRol.Operario));
            Assert.HasCount(2, usuario.Roles);
        }

        [TestMethod]
        public void AsignarRol_RolQueYaTiene_LanzaInvalidOperationException()
        {
            Usuario usuario = CrearCiudadano();

            Assert.ThrowsExactly<InvalidOperationException>(() => usuario.AsignarRol(CodigoRol.Ciudadano));
        }

        [TestMethod]
        public void QuitarRol_ConVariosRoles_QuitaSoloEse()
        {
            Usuario usuario = CrearCiudadano();
            usuario.AsignarRol(CodigoRol.Operario);

            usuario.QuitarRol(CodigoRol.Ciudadano);

            CollectionAssert.AreEqual(new[] { CodigoRol.Operario }, usuario.Roles.ToArray());
        }

        [TestMethod]
        public void QuitarRol_UltimoRol_LanzaInvalidOperationException()
        {
            Usuario usuario = CrearCiudadano();

            Assert.ThrowsExactly<InvalidOperationException>(() => usuario.QuitarRol(CodigoRol.Ciudadano));
            Assert.IsTrue(usuario.TieneRol(CodigoRol.Ciudadano));
        }

        [TestMethod]
        public void QuitarRol_RolQueNoTiene_LanzaInvalidOperationException()
        {
            Usuario usuario = CrearCiudadano();

            Assert.ThrowsExactly<InvalidOperationException>(() => usuario.QuitarRol(CodigoRol.Administrador));
        }

        [TestMethod]
        public void Roles_ColeccionExpuesta_NoPermiteModificarDesdeAfuera()
        {
            Usuario usuario = CrearCiudadano();

            var lista = (ICollection<CodigoRol>)usuario.Roles;

            Assert.ThrowsExactly<NotSupportedException>(() => lista.Add(CodigoRol.Administrador));
        }

        // ---------- consentimiento ----------

        [TestMethod]
        public void RegistrarConsentimiento_DatosValidos_QuedaVigente()
        {
            Usuario usuario = CrearCiudadano();

            usuario.RegistrarConsentimiento(" 2026-10 ", FechaUtc);

            Assert.IsTrue(usuario.TieneConsentimiento);
            Assert.AreEqual("2026-10", usuario.VersionConsentimiento);
            Assert.AreEqual(FechaUtc, usuario.FechaConsentimientoUtc);
        }

        [TestMethod]
        public void RegistrarConsentimiento_VersionVacia_LanzaArgumentExceptionYNoCambiaNada()
        {
            Usuario usuario = CrearCiudadano();

            Assert.ThrowsExactly<ArgumentException>(() => usuario.RegistrarConsentimiento("  ", FechaUtc));
            Assert.IsFalse(usuario.TieneConsentimiento);
        }

        [TestMethod]
        public void RegistrarConsentimiento_FechaNoUtcSinConsentimientoPrevio_LanzaArgumentExceptionYNoCambiaNada()
        {
            Usuario usuario = CrearCiudadano();
            var fechaLocal = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Local);

            Assert.ThrowsExactly<ArgumentException>(() => usuario.RegistrarConsentimiento("2026-10", fechaLocal));
            Assert.IsFalse(usuario.TieneConsentimiento);
            Assert.IsNull(usuario.VersionConsentimiento);
            Assert.IsNull(usuario.FechaConsentimientoUtc);
        }

        [TestMethod]
        public void RegistrarConsentimiento_FechaNoUtcConConsentimientoPrevio_ConservaElAnterior()
        {
            Usuario usuario = CrearCiudadano();
            usuario.RegistrarConsentimiento("2026-10", FechaUtc);
            var fechaLocal = DateTime.SpecifyKind(FechaUtc.AddDays(30), DateTimeKind.Local);

            Assert.ThrowsExactly<ArgumentException>(() => usuario.RegistrarConsentimiento("2026-11", fechaLocal));
            Assert.AreEqual("2026-10", usuario.VersionConsentimiento);
            Assert.AreEqual(FechaUtc, usuario.FechaConsentimientoUtc);
        }

        [TestMethod]
        public void RevocarConsentimiento_ConConsentimientoVigente_LoRetira()
        {
            Usuario usuario = CrearCiudadano();
            usuario.RegistrarConsentimiento("2026-10", FechaUtc);

            usuario.RevocarConsentimiento();

            Assert.IsFalse(usuario.TieneConsentimiento);
            Assert.IsNull(usuario.VersionConsentimiento);
            Assert.IsNull(usuario.FechaConsentimientoUtc);
        }

        // ---------- activación ----------

        [TestMethod]
        public void Desactivar_UsuarioActivo_QuedaInactivoYConservaSusDatos()
        {
            Usuario usuario = CrearCiudadano();

            usuario.Desactivar();

            Assert.IsFalse(usuario.Activo);
            Assert.AreEqual("ana.perez@example.com", usuario.CorreoElectronico);
            Assert.IsTrue(usuario.TieneRol(CodigoRol.Ciudadano));
        }

        [TestMethod]
        public void ActivarYDesactivar_RepetidosVariasVeces_SonIdempotentes()
        {
            Usuario usuario = CrearCiudadano();

            usuario.Desactivar();
            usuario.Desactivar();
            Assert.IsFalse(usuario.Activo);

            usuario.Activar();
            usuario.Activar();
            Assert.IsTrue(usuario.Activo);
        }
    }
}
