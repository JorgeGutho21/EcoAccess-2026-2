using System;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Restablecimiento local de la contraseña del personal (12/11/2026: el titular del único Supervisor olvidó su contraseña y
    /// la aplicación no tenía cómo recuperarla). Solo Operario y Supervisor activos; política del personal; desbloquea; la
    /// contraseña anterior deja de servir. Los ciudadanos no usan esta vía.
    /// </summary>
    [TestClass]
    public sealed class RestablecimientoContrasenaTests
    {
        private const int Supervisor = 1, Operario = 2, Ciudadano = 3, Inactivo = 4, SinCredencial = 5;
        private const string Anterior = "clave-olvidada-1";
        private const string Nueva = "clave-nueva-larga-2026";

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioCredencialesEnMemoria _credenciales;
        private readonly HasherFalso _hasher = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 12, 5, 0, 0, TimeSpan.Zero));
        private readonly ServicioAutenticacion _servicio;

        public RestablecimientoContrasenaTests()
        {
            _usuarios.Agregar(Supervisor, activo: true, CodigoRol.Administrador, CodigoRol.Operario);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.Agregar(Ciudadano, activo: true, CodigoRol.Ciudadano);
            _usuarios.Agregar(Inactivo, activo: false, CodigoRol.Administrador);
            _usuarios.Agregar(SinCredencial, activo: true, CodigoRol.Operario);
            _credenciales = new RepositorioCredencialesEnMemoria(_usuarios);
            foreach (int id in new[] { Supervisor, Operario, Ciudadano, Inactivo })
            {
                var h = _hasher.Calcular(Anterior);
                _credenciales.Guardar(new CredencialAcceso(id, h.Hash, h.Sal, h.Iteraciones, Fechas.Base));
            }
            _servicio = new ServicioAutenticacion(_credenciales, _usuarios, _hasher, _reloj);
        }

        private static string Correo(int id) => $"usuario{id}@ejemplo.com";

        [TestMethod]
        public async Task Supervisor_DefineContrasenaNueva_LaAnteriorDejaDeServir()
        {
            Resultado<int> r = await _servicio.RestablecerContrasenaPersonalLocalAsync("  " + Correo(Supervisor).ToUpperInvariant() + " ", Nueva);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(Supervisor, r.Valor);
            Assert.IsTrue((await _servicio.IniciarSesionAsync(Correo(Supervisor), Nueva)).Exitoso);
            Assert.IsFalse((await _servicio.IniciarSesionAsync(Correo(Supervisor), Anterior)).Exitoso);
        }

        [TestMethod]
        public async Task CuentaBloqueadaPorIntentos_QuedaDesbloqueada()
        {
            for (int i = 0; i < ServicioAutenticacion.MaximoIntentos; i++)
                await _servicio.IniciarSesionAsync(Correo(Supervisor), "equivocada-123456");
            Assert.IsFalse((await _servicio.IniciarSesionAsync(Correo(Supervisor), Anterior)).Exitoso, "Bloqueada.");

            Assert.IsTrue((await _servicio.RestablecerContrasenaPersonalLocalAsync(Correo(Supervisor), Nueva)).Exitoso);

            Assert.IsTrue((await _servicio.IniciarSesionAsync(Correo(Supervisor), Nueva)).Exitoso);
        }

        [TestMethod]
        public async Task OperarioSinCredencial_RecibeLaPrimera()
        {
            Assert.IsTrue((await _servicio.RestablecerContrasenaPersonalLocalAsync(Correo(SinCredencial), Nueva)).Exitoso);
            Assert.IsTrue((await _servicio.IniciarSesionAsync(Correo(SinCredencial), Nueva)).Exitoso);
        }

        [TestMethod]
        [DataRow(Ciudadano)]
        [DataRow(Inactivo)]
        public async Task CiudadanoOCuentaInactiva_NoSeRestablecen(int id)
        {
            Resultado<int> r = await _servicio.RestablecerContrasenaPersonalLocalAsync(Correo(id), Nueva);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo);
            Assert.IsFalse((await _servicio.IniciarSesionPublicaAsync(Correo(id), Nueva)).Exitoso);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("noexiste@ejemplo.com")]
        public async Task CorreoInexistente_NoCambiaNada(string? correo)
        {
            Resultado<int> r = await _servicio.RestablecerContrasenaPersonalLocalAsync(correo, Nueva);

            Assert.IsFalse(r.Exitoso);
            Assert.IsTrue((await _servicio.IniciarSesionAsync(Correo(Supervisor), Anterior)).Exitoso, "Nada cambió.");
        }

        [TestMethod]
        [DataRow("corta-11ch")]
        [DataRow(null)]
        public async Task ContrasenaFueraDeLaPoliticaDelPersonal_SeRechaza(string? nueva)
        {
            Resultado<int> r = await _servicio.RestablecerContrasenaPersonalLocalAsync(Correo(Supervisor), nueva);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
            Assert.IsTrue((await _servicio.IniciarSesionAsync(Correo(Supervisor), Anterior)).Exitoso, "La anterior sigue valiendo.");
        }

        [TestMethod]
        public async Task ContrasenaDemasiadoLarga_SeRechaza()
        {
            Resultado<int> r = await _servicio.RestablecerContrasenaPersonalLocalAsync(Correo(Supervisor), new string('x', CredencialAcceso.LongitudMaximaContrasena + 1));
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }
    }
}
