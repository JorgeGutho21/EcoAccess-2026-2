using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioAutenticacionTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private const int Ciudadano = 3;
        private const int OtroAdmin = 4;
        private const int Inactivo = 5;
        private const int SinCredencial = 6;

        private const string Clave = "clave-correcta-1";
        private const string ClaveNueva = "clave-nueva-segura-2";
        private const string CorreoOperario = "usuario2@ejemplo.com";

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioCredencialesEnMemoria _credenciales;
        private readonly HasherFalso _hasher = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 3, 1, 9, 30, 0, TimeSpan.Zero));
        private readonly ServicioAutenticacion _servicio;

        public ServicioAutenticacionTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.Agregar(Ciudadano, activo: true, CodigoRol.Ciudadano);
            _usuarios.Agregar(OtroAdmin, activo: true, CodigoRol.Administrador, CodigoRol.Operario);
            _usuarios.Agregar(Inactivo, activo: false, CodigoRol.Operario);
            _usuarios.Agregar(SinCredencial, activo: true, CodigoRol.Operario);
            _credenciales = new RepositorioCredencialesEnMemoria(_usuarios);
            foreach (int id in new[] { Admin, Operario, Ciudadano, OtroAdmin, Inactivo })
            {
                DarCredencial(id, Clave);
            }

            _servicio = new ServicioAutenticacion(_credenciales, _usuarios, _hasher, _reloj);
        }

        private DateTime Ahora => _reloj.Ahora.UtcDateTime;

        private void DarCredencial(int id, string clave)
        {
            var calculado = _hasher.Calcular(clave);
            _credenciales.Guardar(new CredencialAcceso(id, calculado.Hash, calculado.Sal, calculado.Iteraciones, Fechas.Base));
        }

        private static void AssertFalloGenerico(Resultado resultado)
        {
            Assert.AreEqual(MotivoRechazo.CredencialesInvalidas, resultado.Motivo, resultado.ToString());
            Assert.AreEqual(ServicioAutenticacion.MensajeFallo, resultado.Mensaje);
        }

        private async Task FallarAsync(int veces, string correo = CorreoOperario)
        {
            for (int i = 0; i < veces; i++)
            {
                AssertFalloGenerico(await _servicio.IniciarSesionAsync(correo, "clave-equivocada"));
            }
        }

        [TestMethod]
        public void Constructor_SinDependencias_LanzaExcepcion()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioAutenticacion(null!, _usuarios, _hasher, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioAutenticacion(_credenciales, null!, _hasher, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioAutenticacion(_credenciales, _usuarios, null!, _reloj));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioAutenticacion(_credenciales, _usuarios, _hasher, null!));
        }

        // ---------- inicio de sesión ----------

        [TestMethod]
        public async Task BienvenidaPermiteCiudadanoSinOtorgarRolesAdministrativos()
        {
            var resultado = await _servicio.IniciarSesionPublicaAsync("usuario3@ejemplo.com", Clave);
            Assert.IsTrue(resultado.Exitoso);
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Ciudadano }, resultado.Valor.Roles.ToArray());
            AssertFalloGenerico(await _servicio.IniciarSesionAsync("usuario3@ejemplo.com", Clave));
        }

        [TestMethod]
        [DataRow(CorreoOperario)]
        [DataRow("usuario1@ejemplo.com")]
        public async Task BienvenidaConservaLoginDePersonal(string correo)
        {
            Assert.IsTrue((await _servicio.IniciarSesionPublicaAsync(correo, Clave)).Exitoso);
        }

        [TestMethod]
        public async Task BienvenidaConservaBloqueoDeCiudadanoYRechazoDeInactivos()
        {
            for (int i = 0; i < ServicioAutenticacion.MaximoIntentos; i++)
                AssertFalloGenerico(await _servicio.IniciarSesionPublicaAsync("usuario3@ejemplo.com", "incorrecta"));
            AssertFalloGenerico(await _servicio.IniciarSesionPublicaAsync("usuario3@ejemplo.com", Clave));
            AssertFalloGenerico(await _servicio.IniciarSesionPublicaAsync("usuario5@ejemplo.com", Clave));
        }

        [TestMethod]
        public async Task IniciarSesionAsync_CredencialCorrecta_DevuelveSesionConRolesYHora()
        {
            Resultado<SesionActiva> resultado = await _servicio.IniciarSesionAsync("  USUARIO4@Ejemplo.com ", Clave);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            SesionActiva sesion = resultado.Valor;
            Assert.AreEqual(OtroAdmin, sesion.IdUsuario);
            Assert.AreEqual("Usuario 4", sesion.Nombre);
            Assert.IsTrue(sesion.TieneRol(CodigoRol.Administrador));
            Assert.IsTrue(sesion.TieneRol(CodigoRol.Operario));
            Assert.IsFalse(sesion.TieneRol(CodigoRol.Ciudadano));
            Assert.AreEqual(Ahora, sesion.InicioUtc);
        }

        [TestMethod]
        [DataRow(null, Clave)]
        [DataRow("   ", Clave)]
        [DataRow(CorreoOperario, null)]
        [DataRow(CorreoOperario, "")]
        public async Task IniciarSesionAsync_CamposVacios_RechazaDatosInvalidos(string? correo, string? clave)
        {
            Resultado<SesionActiva> resultado = await _servicio.IniciarSesionAsync(correo, clave);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.AreEqual(0, _hasher.Verificaciones);
        }

        [TestMethod]
        [DataRow("noexiste@ejemplo.com", Clave, DisplayName = "correo inexistente")]
        [DataRow(CorreoOperario, "clave-equivocada", DisplayName = "contraseña errada")]
        [DataRow("usuario5@ejemplo.com", Clave, DisplayName = "cuenta inactiva")]
        [DataRow("usuario3@ejemplo.com", Clave, DisplayName = "ciudadano sin acceso a la GUI")]
        [DataRow("usuario6@ejemplo.com", Clave, DisplayName = "cuenta sin credencial")]
        public async Task IniciarSesionAsync_CualquierFallo_RespondeElMismoMensajeYSiempreVerifica(string correo, string clave)
        {
            AssertFalloGenerico(await _servicio.IniciarSesionAsync(correo, clave));
            Assert.IsGreaterThanOrEqualTo(1, _hasher.Verificaciones, "Sin credencial se verifica contra el hash de relleno.");
        }

        [TestMethod]
        public async Task IniciarSesionAsync_ContrasenaDemasiadoLarga_RechazaSinCalcular()
        {
            AssertFalloGenerico(await _servicio.IniciarSesionAsync(CorreoOperario, new string('x', CredencialAcceso.LongitudMaximaContrasena + 1)));
            Assert.AreEqual(0, _hasher.Verificaciones);
        }

        [TestMethod]
        public async Task IniciarSesionAsync_ContrasenaErrada_CuentaElIntento()
        {
            await FallarAsync(2);

            Assert.AreEqual(2, _credenciales.Leer(Operario)!.IntentosFallidos);
            Assert.IsNull(_credenciales.Leer(Operario)!.BloqueadaHastaUtc);
        }

        [TestMethod]
        public async Task IniciarSesionAsync_QuintoFallo_BloqueaQuinceMinutosAunConLaClaveCorrecta()
        {
            await FallarAsync(ServicioAutenticacion.MaximoIntentos);

            CredencialAcceso bloqueada = _credenciales.Leer(Operario)!;
            Assert.AreEqual(Ahora + ServicioAutenticacion.DuracionBloqueo, bloqueada.BloqueadaHastaUtc);
            AssertFalloGenerico(await _servicio.IniciarSesionAsync(CorreoOperario, Clave));

            _reloj.Avanzar(ServicioAutenticacion.DuracionBloqueo - TimeSpan.FromSeconds(1));
            AssertFalloGenerico(await _servicio.IniciarSesionAsync(CorreoOperario, Clave));
        }

        [TestMethod]
        public async Task IniciarSesionAsync_DuranteElBloqueo_NoCuentaNuevosIntentos()
        {
            await FallarAsync(ServicioAutenticacion.MaximoIntentos);
            CredencialAcceso antes = _credenciales.Leer(Operario)!;

            await FallarAsync(3);

            CredencialAcceso despues = _credenciales.Leer(Operario)!;
            Assert.AreEqual(antes.BloqueadaHastaUtc, despues.BloqueadaHastaUtc);
            Assert.AreEqual(antes.IntentosFallidos, despues.IntentosFallidos);
        }

        [TestMethod]
        public async Task IniciarSesionAsync_VencidoElBloqueo_EntraYReiniciaElEstado()
        {
            await FallarAsync(ServicioAutenticacion.MaximoIntentos);
            _reloj.Avanzar(ServicioAutenticacion.DuracionBloqueo);

            Resultado<SesionActiva> resultado = await _servicio.IniciarSesionAsync(CorreoOperario, Clave);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsNull(_credenciales.Leer(Operario)!.BloqueadaHastaUtc);
            Assert.AreEqual(1, _credenciales.Reinicios);
        }

        [TestMethod]
        public async Task IniciarSesionAsync_ExitoTrasFallos_ReiniciaIntentos()
        {
            await FallarAsync(3);

            Assert.IsTrue((await _servicio.IniciarSesionAsync(CorreoOperario, Clave)).Exitoso);
            Assert.AreEqual(0, _credenciales.Leer(Operario)!.IntentosFallidos);
        }

        [TestMethod]
        public async Task IniciarSesionAsync_ExitoSinFallosPrevios_NoEscribe()
        {
            Assert.IsTrue((await _servicio.IniciarSesionAsync(CorreoOperario, Clave)).Exitoso);
            Assert.AreEqual(0, _credenciales.Reinicios);
        }

        // ---------- cambio de contraseña ----------

        [TestMethod]
        public async Task CambiarContrasenaAsync_ActualCorrecta_ReemplazaYPermiteEntrarConLaNueva()
        {
            await FallarAsync(2);

            Resultado resultado = await _servicio.CambiarContrasenaAsync(Operario, Clave, ClaveNueva);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(0, _credenciales.Leer(Operario)!.IntentosFallidos);
            Assert.AreEqual(Ahora, _credenciales.Leer(Operario)!.FechaCambioUtc);
            Assert.IsTrue((await _servicio.IniciarSesionAsync(CorreoOperario, ClaveNueva)).Exitoso);
            AssertFalloGenerico(await _servicio.IniciarSesionAsync(CorreoOperario, Clave));
        }

        [TestMethod]
        public async Task CambiarContrasenaAsync_ActualErrada_RechazaYCuentaElIntento()
        {
            Resultado resultado = await _servicio.CambiarContrasenaAsync(Operario, "clave-equivocada", ClaveNueva);

            Assert.AreEqual(MotivoRechazo.CredencialesInvalidas, resultado.Motivo);
            Assert.AreEqual(1, _credenciales.Leer(Operario)!.IntentosFallidos);
        }

        [TestMethod]
        public async Task CambiarContrasenaAsync_Bloqueada_RechazaSinVerificar()
        {
            await FallarAsync(ServicioAutenticacion.MaximoIntentos);
            int verificaciones = _hasher.Verificaciones;

            Resultado resultado = await _servicio.CambiarContrasenaAsync(Operario, Clave, ClaveNueva);

            Assert.AreEqual(MotivoRechazo.CredencialesInvalidas, resultado.Motivo);
            Assert.AreEqual(verificaciones, _hasher.Verificaciones);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("corta")]
        [DataRow("            ")]
        public async Task CambiarContrasenaAsync_NuevaInvalida_RechazaDatosInvalidos(string? nueva)
        {
            Resultado resultado = await _servicio.CambiarContrasenaAsync(Operario, Clave, nueva);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.AreEqual(0, _hasher.Verificaciones);
        }

        [TestMethod]
        public async Task CambiarContrasenaAsync_NuevaDemasiadoLarga_RechazaDatosInvalidos()
        {
            Resultado resultado = await _servicio.CambiarContrasenaAsync(Operario, Clave, new string('x', CredencialAcceso.LongitudMaximaContrasena + 1));

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
        }

        [TestMethod]
        public async Task CambiarContrasenaAsync_NuevaIgualALaActual_RechazaDatosInvalidos()
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.CambiarContrasenaAsync(Operario, Clave, Clave)).Motivo);
        }

        [TestMethod]
        [DataRow(Ciudadano, MotivoRechazo.SinPermiso)]
        [DataRow(Inactivo, MotivoRechazo.ActorInactivo)]
        [DataRow(999, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(0, MotivoRechazo.ActorNoEncontrado)]
        public async Task CambiarContrasenaAsync_CuentaSinAccesoGui_Rechaza(int id, MotivoRechazo esperado)
        {
            Assert.AreEqual(esperado, (await _servicio.CambiarContrasenaAsync(id, Clave, ClaveNueva)).Motivo);
        }

        [TestMethod]
        public async Task CambiarContrasenaAsync_SinCredencial_RechazaEstadoNoPermitido()
        {
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.CambiarContrasenaAsync(SinCredencial, Clave, ClaveNueva)).Motivo);
        }

        // ---------- contraseña inicial ----------

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_Administrador_CreaCredencialUsable()
        {
            Resultado resultado = await _servicio.EstablecerContrasenaInicialAsync(Admin, SinCredencial, ClaveNueva);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(Ahora, _credenciales.Leer(SinCredencial)!.FechaCambioUtc);
            Assert.IsTrue((await _servicio.IniciarSesionAsync("usuario6@ejemplo.com", ClaveNueva)).Exitoso);
        }

        [TestMethod]
        [DataRow(Operario, MotivoRechazo.SinPermiso)]
        [DataRow(Inactivo, MotivoRechazo.ActorInactivo)]
        [DataRow(999, MotivoRechazo.ActorNoEncontrado)]
        public async Task EstablecerContrasenaInicialAsync_ActorSinPermiso_Rechaza(int actor, MotivoRechazo esperado)
        {
            Assert.AreEqual(esperado, (await _servicio.EstablecerContrasenaInicialAsync(actor, SinCredencial, ClaveNueva)).Motivo);
            Assert.IsNull(_credenciales.Leer(SinCredencial));
        }

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_CuentaInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.EstablecerContrasenaInicialAsync(Admin, 999, ClaveNueva)).Motivo);
        }

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_Ciudadano_RechazaEstadoNoPermitido()
        {
            _usuarios.Agregar(7, activo: true, CodigoRol.Ciudadano);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.EstablecerContrasenaInicialAsync(Admin, 7, ClaveNueva)).Motivo);
            Assert.IsNull(_credenciales.Leer(7));
        }

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_YaTieneCredencial_RechazaDuplicadoSinCambiarla()
        {
            byte[] antes = _credenciales.Leer(Operario)!.Hash;

            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.EstablecerContrasenaInicialAsync(Admin, Operario, ClaveNueva)).Motivo);
            CollectionAssert.AreEqual(antes, _credenciales.Leer(Operario)!.Hash);
        }

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_CarreraConOtroAdministrador_RechazaDuplicado()
        {
            _credenciales.SimularConflictoAlAgregar = true;

            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.EstablecerContrasenaInicialAsync(Admin, SinCredencial, ClaveNueva)).Motivo);
        }

        [TestMethod]
        public async Task EstablecerContrasenaInicialAsync_ContrasenaCorta_RechazaDatosInvalidos()
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.EstablecerContrasenaInicialAsync(Admin, SinCredencial, "corta")).Motivo);
        }

        // ---------- configuración inicial del primer administrador ----------

        private void QuitarCredencialesDeAdministradores()
        {
            _credenciales.Quitar(Admin);
            _credenciales.Quitar(OtroAdmin);
        }

        [TestMethod]
        public async Task RequiereConfiguracionInicialAsync_HayAdministradorConContrasena_DevuelveFalse()
        {
            Assert.IsFalse(await _servicio.RequiereConfiguracionInicialAsync());
        }

        [TestMethod]
        public async Task RequiereConfiguracionInicialAsync_SoloAdministradorInactivoTieneContrasena_DevuelveFalse()
        {
            // Desactivar al único administrador con contraseña no reabre el atajo del primer arranque.
            QuitarCredencialesDeAdministradores();
            _usuarios.Agregar(8, activo: false, CodigoRol.Administrador);
            DarCredencial(8, Clave);

            Assert.IsFalse(await _servicio.RequiereConfiguracionInicialAsync());
        }

        [TestMethod]
        public async Task RequiereConfiguracionInicialAsync_NingunAdministradorTieneContrasena_DevuelveTrue()
        {
            QuitarCredencialesDeAdministradores();

            Assert.IsTrue(await _servicio.RequiereConfiguracionInicialAsync());
        }

        [TestMethod]
        public async Task ConfigurarAdministradorInicialAsync_SinAdministradoresConContrasena_CreaCredencialYPermiteIniciarSesion()
        {
            QuitarCredencialesDeAdministradores();

            Resultado resultado = await _servicio.ConfigurarAdministradorInicialAsync(" USUARIO1@ejemplo.com ", ClaveNueva);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsFalse(await _servicio.RequiereConfiguracionInicialAsync());
            Resultado<SesionActiva> sesion = await _servicio.IniciarSesionAsync("usuario1@ejemplo.com", ClaveNueva);
            Assert.IsTrue(sesion.Exitoso, sesion.ToString());
            Assert.AreEqual(Admin, sesion.Valor.IdUsuario);
        }

        [TestMethod]
        public async Task ConfigurarAdministradorInicialAsync_YaConfigurado_RechazaSinTocarCredenciales()
        {
            _credenciales.Quitar(OtroAdmin);

            Resultado resultado = await _servicio.ConfigurarAdministradorInicialAsync("usuario4@ejemplo.com", ClaveNueva);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsNull(_credenciales.Leer(OtroAdmin));
        }

        [TestMethod]
        [DataRow("usuario2@ejemplo.com", DisplayName = "operario")]
        [DataRow("usuario9@ejemplo.com", DisplayName = "administrador inactivo")]
        [DataRow("noexiste@ejemplo.com", DisplayName = "correo inexistente")]
        [DataRow(null, DisplayName = "sin correo")]
        public async Task ConfigurarAdministradorInicialAsync_CuentaNoValida_RechazaNoEncontrado(string? correo)
        {
            QuitarCredencialesDeAdministradores();
            _usuarios.Agregar(9, activo: false, CodigoRol.Administrador);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.ConfigurarAdministradorInicialAsync(correo, ClaveNueva)).Motivo);
            Assert.IsNull(_credenciales.Leer(9));
        }

        [TestMethod]
        public async Task ConfigurarAdministradorInicialAsync_ContrasenaCorta_RechazaDatosInvalidos()
        {
            QuitarCredencialesDeAdministradores();

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.ConfigurarAdministradorInicialAsync("usuario1@ejemplo.com", "corta")).Motivo);
            Assert.IsNull(_credenciales.Leer(Admin));
        }

        [TestMethod]
        public async Task ConfigurarAdministradorInicialAsync_CarreraConOtraConfiguracion_RechazaDuplicado()
        {
            QuitarCredencialesDeAdministradores();
            _credenciales.SimularConflictoAlAgregar = true;

            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.ConfigurarAdministradorInicialAsync("usuario1@ejemplo.com", ClaveNueva)).Motivo);
        }
    }
}
