using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioUsuariosTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private const int Ciudadano = 3;
        private const int OtroAdmin = 4;
        private const int Inactivo = 5;

        private static readonly DateTimeOffset Ahora = new(2026, 3, 1, 9, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly ServicioUsuarios _servicio;

        public ServicioUsuariosTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.Agregar(Ciudadano, activo: true, CodigoRol.Ciudadano);
            _usuarios.Agregar(OtroAdmin, activo: true, CodigoRol.Administrador, CodigoRol.Operario);
            _usuarios.Agregar(Inactivo, activo: false, CodigoRol.Ciudadano);
            _servicio = new ServicioUsuarios(_usuarios, new RelojFijo(Ahora));
        }

        [TestMethod]
        public async Task BuscarCiudadanos_OperarioPuede_CiudadanoNo()
        {
            Assert.IsTrue((await _servicio.BuscarCiudadanosAsync(Operario, null)).Exitoso);
            Assert.IsFalse((await _servicio.BuscarCiudadanosAsync(Ciudadano, null)).Exitoso);
        }


        [TestMethod]
        public void Constructor_SinReloj_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioUsuarios(_usuarios, null!));
        }

        [TestMethod]
        public async Task RegistrarAsync_DatosValidos_GuardaCuentaActivaConCorreoNormalizadoYFechaDelReloj()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "Ana Pérez", "  Ana.Perez@Ejemplo.COM ", CodigoRol.Operario);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Usuario guardado = _usuarios.Leer(resultado.Valor)!;
            Assert.AreEqual("ana.perez@ejemplo.com", guardado.CorreoElectronico);
            Assert.AreEqual(Ahora.UtcDateTime, guardado.FechaRegistroUtc);
            Assert.IsTrue(guardado.Activo);
            Assert.IsTrue(guardado.TieneRol(CodigoRol.Operario));
            Assert.IsFalse(guardado.TieneConsentimiento);
        }

        [TestMethod]
        public async Task RegistrarAsync_Operario_RechazaSinPermiso()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Operario, "Ana Pérez", "ana@ejemplo.com", CodigoRol.Ciudadano);

            Assert.AreEqual(MotivoRechazo.SinPermiso, resultado.Motivo);
            Assert.IsFalse(await _usuarios.ExisteCorreoAsync("ana@ejemplo.com"));
        }

        [TestMethod]
        public async Task RegistrarAsync_AdminInactivo_RechazaActorInactivo()
        {
            _usuarios.Agregar(9, activo: false, CodigoRol.Administrador);

            Assert.AreEqual(MotivoRechazo.ActorInactivo,
                (await _servicio.RegistrarAsync(9, "Ana Pérez", "ana@ejemplo.com", CodigoRol.Ciudadano)).Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_CorreoInvalido_RechazaDatosInvalidos()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "Ana Pérez", "sin-arroba", CodigoRol.Ciudadano);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.StartsWith("correoElectronico:", resultado.Mensaje);
        }

        [TestMethod]
        public async Task RegistrarAsync_CorreoExistenteConOtrasMayusculas_RechazaDuplicado()
        {
            Assert.AreEqual(MotivoRechazo.Duplicado,
                (await _servicio.RegistrarAsync(Admin, "Otra persona", "USUARIO3@ejemplo.com", CodigoRol.Ciudadano)).Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_ConflictoAlGuardar_RechazaDuplicado()
        {
            _usuarios.SimularConflictoAlAgregar = true;

            Assert.AreEqual(MotivoRechazo.Duplicado,
                (await _servicio.RegistrarAsync(Admin, "Ana Pérez", "ana@ejemplo.com", CodigoRol.Ciudadano)).Motivo);
        }

        [TestMethod]
        public async Task ObtenerAsync_Admin_DevuelveCuenta()
        {
            Resultado<Usuario> resultado = await _servicio.ObtenerAsync(Admin, Ciudadano);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(Ciudadano, resultado.Valor.Id);
        }

        [TestMethod]
        public async Task ObtenerAsync_CuentaInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.ObtenerAsync(Admin, 99)).Motivo);
        }

        [TestMethod]
        public async Task ObtenerAsync_Ciudadano_RechazaSinPermiso()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.ObtenerAsync(Ciudadano, Operario)).Motivo);
        }

        [TestMethod]
        public async Task ListarAsync_Admin_DevuelveTodasLasCuentas()
        {
            Resultado<IReadOnlyList<Usuario>> resultado = await _servicio.ListarAsync(Admin);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.HasCount(5, resultado.Valor);
        }

        [TestMethod]
        public async Task ListarAsync_Operario_RechazaSinPermiso()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.ListarAsync(Operario)).Motivo);
        }

        [TestMethod]
        public async Task AsignarRolAsync_RolNuevo_LoGuarda()
        {
            Resultado resultado = await _servicio.AsignarRolAsync(Admin, Ciudadano, CodigoRol.Operario);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsTrue(_usuarios.Leer(Ciudadano)!.TieneRol(CodigoRol.Operario));
            Assert.AreEqual(1, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task AsignarRolAsync_RolQueYaTiene_EsExitosoSinGuardar()
        {
            Assert.IsTrue((await _servicio.AsignarRolAsync(Admin, Operario, CodigoRol.Operario)).Exitoso);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task AsignarRolAsync_RolNoDefinido_RechazaDatosInvalidos()
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.AsignarRolAsync(Admin, Ciudadano, (CodigoRol)99)).Motivo);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task AsignarRolAsync_CuentaInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.AsignarRolAsync(Admin, 99, CodigoRol.Operario)).Motivo);
        }

        [TestMethod]
        public async Task AsignarRolAsync_Operario_RechazaSinPermiso()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.AsignarRolAsync(Operario, Operario, CodigoRol.Administrador)).Motivo);
            Assert.IsFalse(_usuarios.Leer(Operario)!.TieneRol(CodigoRol.Administrador));
        }

        [TestMethod]
        public async Task QuitarRolAsync_UnoDeVariosRoles_LoQuita()
        {
            Resultado resultado = await _servicio.QuitarRolAsync(Admin, OtroAdmin, CodigoRol.Administrador);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsFalse(_usuarios.Leer(OtroAdmin)!.TieneRol(CodigoRol.Administrador));
        }

        [TestMethod]
        public async Task QuitarRolAsync_UltimoRol_RechazaEstadoNoPermitido()
        {
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.QuitarRolAsync(Admin, Ciudadano, CodigoRol.Ciudadano)).Motivo);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task QuitarRolAsync_AdminSobreSiMismo_RechazaEstadoNoPermitido()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador, CodigoRol.Operario);

            Resultado resultado = await _servicio.QuitarRolAsync(Admin, Admin, CodigoRol.Administrador);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_usuarios.Leer(Admin)!.TieneRol(CodigoRol.Administrador));
        }

        [TestMethod]
        public async Task QuitarRolAsync_AdminQuitaseOtroRolPropio_LoQuita()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador, CodigoRol.Operario);

            Assert.IsTrue((await _servicio.QuitarRolAsync(Admin, Admin, CodigoRol.Operario)).Exitoso);
            Assert.IsFalse(_usuarios.Leer(Admin)!.TieneRol(CodigoRol.Operario));
        }

        [TestMethod]
        public async Task QuitarRolAsync_RolQueNoTiene_EsExitosoSinGuardar()
        {
            Assert.IsTrue((await _servicio.QuitarRolAsync(Admin, Ciudadano, CodigoRol.Operario)).Exitoso);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task ActivarAsync_CuentaInactiva_LaActiva()
        {
            Assert.IsTrue((await _servicio.ActivarAsync(Admin, Inactivo)).Exitoso);
            Assert.IsTrue(_usuarios.Leer(Inactivo)!.Activo);
        }

        [TestMethod]
        public async Task ActivarAsync_YaActiva_EsExitosoSinGuardar()
        {
            Assert.IsTrue((await _servicio.ActivarAsync(Admin, Ciudadano)).Exitoso);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task DesactivarAsync_OtraCuenta_LaDesactivaYConservaRoles()
        {
            Assert.IsTrue((await _servicio.DesactivarAsync(Admin, OtroAdmin)).Exitoso);

            Usuario guardado = _usuarios.Leer(OtroAdmin)!;
            Assert.IsFalse(guardado.Activo);
            Assert.IsTrue(guardado.TieneRol(CodigoRol.Administrador));
        }

        [TestMethod]
        public async Task DesactivarAsync_SiMismo_RechazaEstadoNoPermitido()
        {
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.DesactivarAsync(Admin, Admin)).Motivo);
            Assert.IsTrue(_usuarios.Leer(Admin)!.Activo);
        }

        [TestMethod]
        public async Task DesactivarAsync_RepositorioDetectaUltimoAdministrador_RechazaEstadoNoPermitido()
        {
            // Carrera RN-23: la comprobación previa pasa, pero el repositorio ve que no quedaría ningún administrador activo.
            _usuarios.SimularUltimoAdministrador = true;

            Resultado resultado = await _servicio.DesactivarAsync(Admin, OtroAdmin);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            StringAssert.Contains(resultado.Mensaje, "RN-23");
            Assert.IsTrue(_usuarios.Leer(OtroAdmin)!.Activo);
        }

        [TestMethod]
        public async Task DesactivarAsync_YaInactiva_EsExitosoSinGuardar()
        {
            Assert.IsTrue((await _servicio.DesactivarAsync(Admin, Inactivo)).Exitoso);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task DesactivarAsync_Operario_RechazaSinPermiso()
        {
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.DesactivarAsync(Operario, Ciudadano)).Motivo);
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_CuentaActiva_GuardaVersionYFechaDelReloj()
        {
            Resultado resultado = await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0");

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Usuario guardado = _usuarios.Leer(Ciudadano)!;
            Assert.AreEqual("v1.0", guardado.VersionConsentimiento);
            Assert.AreEqual(Ahora.UtcDateTime, guardado.FechaConsentimientoUtc);
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_VersionVacia_RechazaDatosInvalidosSinGuardar()
        {
            Resultado resultado = await _servicio.RegistrarConsentimientoAsync(Ciudadano, "  ");

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.StartsWith("version:", resultado.Mensaje);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_CuentaInactiva_RechazaActorInactivo()
        {
            Assert.AreEqual(MotivoRechazo.ActorInactivo, (await _servicio.RegistrarConsentimientoAsync(Inactivo, "v1.0")).Motivo);
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_CuentaInexistente_RechazaActorNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.ActorNoEncontrado, (await _servicio.RegistrarConsentimientoAsync(0, "v1.0")).Motivo);
        }

        [TestMethod]
        public async Task RevocarConsentimientoAsync_CuentaInactivaConConsentimiento_LoRetira()
        {
            Assert.IsTrue((await _servicio.ActivarAsync(Admin, Inactivo)).Exitoso);
            Assert.IsTrue((await _servicio.RegistrarConsentimientoAsync(Inactivo, "v1.0")).Exitoso);
            Assert.IsTrue((await _servicio.DesactivarAsync(Admin, Inactivo)).Exitoso);
            Assert.IsTrue(_usuarios.Leer(Inactivo)!.TieneConsentimiento, "Desactivar no debe retirar el consentimiento.");
            Assert.IsFalse(_usuarios.Leer(Inactivo)!.Activo);

            Assert.IsTrue((await _servicio.RevocarConsentimientoAsync(Inactivo)).Exitoso);
            Assert.IsFalse(_usuarios.Leer(Inactivo)!.TieneConsentimiento);
        }

        [TestMethod]
        public async Task RevocarConsentimientoAsync_SinConsentimiento_EsExitosoSinGuardar()
        {
            Assert.IsTrue((await _servicio.RevocarConsentimientoAsync(Ciudadano)).Exitoso);
            Assert.AreEqual(0, _usuarios.Actualizaciones);
        }

        [TestMethod]
        public async Task RevocarConsentimientoAsync_CuentaInexistente_RechazaActorNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.ActorNoEncontrado, (await _servicio.RevocarConsentimientoAsync(99)).Motivo);
        }

        // Escrituras concurrentes (hallazgo de revisión del PR #1): cada operación guarda solo lo suyo,
        // así que no devuelve a su valor anterior lo que otra sesión cambió entre la lectura y la escritura.

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_DesactivadaEntreLecturaYEscritura_RechazaYNoLaReactiva()
        {
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(Ciudadano, u => u.Desactivar());

            Resultado resultado = await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0");

            Assert.AreEqual(MotivoRechazo.ActorInactivo, resultado.Motivo);
            Usuario guardado = _usuarios.Leer(Ciudadano)!;
            Assert.IsFalse(guardado.Activo);
            Assert.IsFalse(guardado.TieneConsentimiento);
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_RolQuitadoEntreLecturaYEscritura_NoLoRestaura()
        {
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(OtroAdmin, u => u.QuitarRol(CodigoRol.Operario));

            Assert.IsTrue((await _servicio.RegistrarConsentimientoAsync(OtroAdmin, "v1.0")).Exitoso);

            Usuario guardado = _usuarios.Leer(OtroAdmin)!;
            Assert.IsFalse(guardado.TieneRol(CodigoRol.Operario));
            Assert.AreEqual("v1.0", guardado.VersionConsentimiento);
        }

        [TestMethod]
        public async Task RevocarConsentimientoAsync_RolAgregadoEntreLecturaYEscritura_ConservaElRol()
        {
            await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0");
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(Ciudadano, u => u.AsignarRol(CodigoRol.Operario));

            Assert.IsTrue((await _servicio.RevocarConsentimientoAsync(Ciudadano)).Exitoso);

            Usuario guardado = _usuarios.Leer(Ciudadano)!;
            Assert.IsTrue(guardado.TieneRol(CodigoRol.Operario));
            Assert.IsFalse(guardado.TieneConsentimiento);
        }

        [TestMethod]
        public async Task DesactivarAsync_ConsentimientoRevocadoEntreLecturaYEscritura_NoLoRestaura()
        {
            await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0");
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(Ciudadano, u => u.RevocarConsentimiento());

            Assert.IsTrue((await _servicio.DesactivarAsync(Admin, Ciudadano)).Exitoso);

            Usuario guardado = _usuarios.Leer(Ciudadano)!;
            Assert.IsFalse(guardado.Activo);
            Assert.IsFalse(guardado.TieneConsentimiento);
        }

        [TestMethod]
        public async Task AsignarRolAsync_DesactivadaEntreLecturaYEscritura_NoLaReactiva()
        {
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(Ciudadano, u => u.Desactivar());

            Assert.IsTrue((await _servicio.AsignarRolAsync(Admin, Ciudadano, CodigoRol.Operario)).Exitoso);

            Usuario guardado = _usuarios.Leer(Ciudadano)!;
            Assert.IsFalse(guardado.Activo);
            Assert.IsTrue(guardado.TieneRol(CodigoRol.Operario));
        }

        [TestMethod]
        public async Task QuitarRolAsync_OtroRolQuitadoEntreLecturaYEscritura_RechazaUltimoRol()
        {
            // Carrera RN-01: dos administradores quitan a la vez roles distintos de una cuenta con dos roles.
            _usuarios.AlEscribir = () => _usuarios.CambiarGuardado(OtroAdmin, u => u.QuitarRol(CodigoRol.Administrador));

            Resultado resultado = await _servicio.QuitarRolAsync(Admin, OtroAdmin, CodigoRol.Operario);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            StringAssert.Contains(resultado.Mensaje, "RN-01");
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Operario }, _usuarios.Leer(OtroAdmin)!.Roles.ToArray());
        }

        [TestMethod]
        public async Task RegistrarConsentimientoAsync_EsperaDeBloqueoAgotada_RechazaEstadoNoPermitidoSinGuardar()
        {
            _usuarios.AlEscribir = () => throw new CambioConcurrenteException("Otra operación está modificando esos datos.");

            Resultado resultado = await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0");

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual("Otra operación está modificando esos datos.", resultado.Mensaje);
            Assert.IsFalse(_usuarios.Leer(Ciudadano)!.TieneConsentimiento);
        }

        [TestMethod]
        public async Task RevocarConsentimientoAsync_EsperaDeBloqueoAgotada_RechazaEstadoNoPermitidoYLoConserva()
        {
            Assert.IsTrue((await _servicio.RegistrarConsentimientoAsync(Ciudadano, "v1.0")).Exitoso);
            _usuarios.AlEscribir = () => throw new CambioConcurrenteException("Otra operación está modificando esos datos.");

            Resultado resultado = await _servicio.RevocarConsentimientoAsync(Ciudadano);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_usuarios.Leer(Ciudadano)!.TieneConsentimiento);
        }
    }
}
