using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RegistroCiudadanosOracleTests : BaseIntegracionOracle
    {
        private const string Clave = "ficticio-registro26"; // política ciudadana: de 6 a 20 caracteres
        private static string Correo() => "it.registro." + Sufijo().ToLowerInvariant() + "@example.invalid";
        private static AltaCiudadano Alta(string correo) => new("Ciudadano ficticio de registro", correo, DateTime.UtcNow,
            ServicioRegistroCiudadanos.VersionConsentimiento, new HasherPbkdf2().Calcular(Clave));

        [TestMethod]
        public async Task RegistroPersisteCiudadanoConsentimientoYHashUsableSinPermisosDePersonal()
        {
            string correo = Correo();
            var repositorio = new RepositorioRegistroCiudadanosOracle(Fabrica);
            var servicio = new ServicioRegistroCiudadanos(repositorio, new HasherPbkdf2(), TimeProvider.System);
            var resultado = await servicio.RegistrarAsync("Ciudadano de prueba", correo, Clave, Clave, true);
            Assert.IsTrue(resultado.Exitoso, resultado.Mensaje);
            int id = RegistrarUsuario(resultado.Valor);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var credenciales = new RepositorioCredencialesOracle(Fabrica);
            Usuario usuario = (await usuarios.ObtenerPorIdAsync(id))!;
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Ciudadano }, usuario.Roles.ToArray());
            Assert.IsTrue(usuario.TieneConsentimiento);
            Assert.AreEqual(ServicioRegistroCiudadanos.VersionConsentimiento, usuario.VersionConsentimiento);
            CredencialAcceso credencial = (await credenciales.ObtenerPorUsuarioAsync(id))!;
            Assert.IsTrue(new HasherPbkdf2().Verificar(Clave, credencial.Sal, credencial.Iteraciones, credencial.Hash));
            Assert.IsFalse(new HasherPbkdf2().Verificar("otra contraseña", credencial.Sal, credencial.Iteraciones, credencial.Hash));
            var autenticacion = new ServicioAutenticacion(credenciales, usuarios, new HasherPbkdf2(), TimeProvider.System);
            Assert.IsTrue((await autenticacion.IniciarSesionPublicaAsync(correo, Clave)).Exitoso);
            Assert.AreEqual(MotivoRechazo.CredencialesInvalidas, (await autenticacion.IniciarSesionAsync(correo, Clave)).Motivo);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await new ServicioUsuarios(usuarios, TimeProvider.System).ListarAsync(id)).Motivo);
        }

        [TestMethod]
        public async Task DosAltasConcurrentesDelMismoCorreoSoloConfirmanUnaCuenta()
        {
            string correo = Correo();
            AltaCiudadano alta = Alta(correo);
            var repositorio = new RepositorioRegistroCiudadanosOracle(Fabrica);
            async Task<int?> IntentarAsync()
            {
                try { return await repositorio.RegistrarAsync(alta); }
                catch (ConflictoUnicidadException) { return null; }
            }
            int?[] resultados = await Task.WhenAll(IntentarAsync(), IntentarAsync());
            foreach (int? id in resultados) if (id is { } creado) RegistrarUsuario(creado);
            Assert.AreEqual(1, resultados.Count(r => r is not null));
            Assert.HasCount(1, (await new RepositorioUsuariosOracle(Fabrica).ListarAsync()).Where(u => u.CorreoElectronico == correo).ToArray());
        }

        [TestMethod]
        public async Task FalloAlCrearCredencialRevierteUsuarioYRolInsertadosAntes()
        {
            string correo = Correo();
            AltaCiudadano alta = Alta(correo) with { Credencial = new HashContrasena(new byte[3], new byte[16], 600000) };
            await Assert.ThrowsAsync<ArgumentException>(() => new RepositorioRegistroCiudadanosOracle(Fabrica).RegistrarAsync(alta));
            var residuos = (await new RepositorioUsuariosOracle(Fabrica).ListarAsync()).Where(u => u.CorreoElectronico == correo).ToArray();
            foreach (Usuario residuo in residuos) RegistrarUsuario(residuo.Id);
            Assert.IsEmpty(residuos, "No debe quedar una cuenta ni roles después del fallo de credencial.");
        }

        [TestMethod]
        public async Task RegistroPublicoNoReemplazaCuentaAdministrativaExistente()
        {
            string correo = Correo();
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            int id = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Admin ficticio existente", correo, CodigoRol.Administrador, DateTime.UtcNow)));
            await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() => new RepositorioRegistroCiudadanosOracle(Fabrica).RegistrarAsync(Alta(correo)));
            Usuario anterior = (await usuarios.ObtenerPorIdAsync(id))!;
            CollectionAssert.AreEquivalent(new[] { CodigoRol.Administrador }, anterior.Roles.ToArray());
            Assert.IsNull(await new RepositorioCredencialesOracle(Fabrica).ObtenerPorUsuarioAsync(id));
        }

        [TestMethod]
        public async Task CancelacionPreviaNoCreaUsuario()
        {
            string correo = Correo();
            using var ct = new CancellationTokenSource(); ct.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => new RepositorioRegistroCiudadanosOracle(Fabrica).RegistrarAsync(Alta(correo), ct.Token));
            Assert.IsFalse(await new RepositorioUsuariosOracle(Fabrica).ExisteCorreoAsync(correo));
        }
    }
}
