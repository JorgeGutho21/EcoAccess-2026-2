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
    public sealed class RepositorioCredencialesOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);

        private static CredencialAcceso Credencial(int idUsuario, byte relleno, DateTime fecha) =>
            new(idUsuario,
                Enumerable.Repeat(relleno, CredencialAcceso.LongitudHash).ToArray(),
                Enumerable.Repeat((byte)(relleno + 1), CredencialAcceso.LongitudSal).ToArray(),
                CredencialAcceso.IteracionesMinimas, fecha);

        private async Task<(int Id, string Correo)> CrearCuentaAsync()
        {
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            string correo = $"it.cred.{Sufijo().ToLowerInvariant()}@ejemplo.com";
            int id = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Persona de integración", correo, CodigoRol.Ciudadano, Fecha)));
            return (id, correo);
        }

        [TestMethod]
        public async Task Credencial_Guardada_SeRecuperaPorCorreoYPorUsuario()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, string correo) = await CrearCuentaAsync();

            await repositorio.AgregarAsync(Credencial(id, 7, Fecha));
            CredencialAcceso porCorreo = (await repositorio.ObtenerPorCorreoAsync(correo))!;
            CredencialAcceso porUsuario = (await repositorio.ObtenerPorUsuarioAsync(id))!;

            CollectionAssert.AreEqual(Enumerable.Repeat((byte)7, 32).ToArray(), porCorreo.Hash);
            CollectionAssert.AreEqual(Enumerable.Repeat((byte)8, 16).ToArray(), porCorreo.Sal);
            Assert.AreEqual(CredencialAcceso.IteracionesMinimas, porCorreo.Iteraciones);
            Assert.AreEqual(Fecha, porCorreo.FechaCambioUtc);
            Assert.AreEqual(DateTimeKind.Utc, porCorreo.FechaCambioUtc.Kind);
            Assert.AreEqual(0, porCorreo.IntentosFallidos);
            Assert.IsNull(porCorreo.BloqueadaHastaUtc);
            Assert.AreEqual(id, porUsuario.IdUsuario);
            Assert.IsNull(await repositorio.ObtenerPorCorreoAsync("nadie." + correo));
        }

        [TestMethod]
        public async Task Credencial_Duplicada_LanzaConflictoConNombreDeRestriccion()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();
            await repositorio.AgregarAsync(Credencial(id, 1, Fecha));

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() =>
                repositorio.AgregarAsync(Credencial(id, 2, Fecha)));

            StringAssert.Contains(error.Message, "pk_credencial");
        }

        [TestMethod]
        public async Task Credencial_Reemplazada_CambiaSecretoYReiniciaBloqueo()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();
            await repositorio.AgregarAsync(Credencial(id, 1, Fecha));
            await repositorio.RegistrarFalloAsync(id, 1, Fecha.AddMinutes(15));

            await repositorio.ReemplazarSecretoAsync(Credencial(id, 9, Fecha.AddDays(1)));
            CredencialAcceso releida = (await repositorio.ObtenerPorUsuarioAsync(id))!;

            CollectionAssert.AreEqual(Enumerable.Repeat((byte)9, 32).ToArray(), releida.Hash);
            Assert.AreEqual(Fecha.AddDays(1), releida.FechaCambioUtc);
            Assert.AreEqual(0, releida.IntentosFallidos);
            Assert.IsNull(releida.BloqueadaHastaUtc);
        }

        [TestMethod]
        public async Task Credencial_ReemplazoSinCredencial_LanzaInvalidOperation()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
                repositorio.ReemplazarSecretoAsync(Credencial(id, 1, Fecha)));
        }

        [TestMethod]
        public async Task Credencial_FallosHastaElMaximo_BloqueaYReiniciaContador()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();
            await repositorio.AgregarAsync(Credencial(id, 1, Fecha));
            DateTime hasta = Fecha.AddMinutes(15);

            await repositorio.RegistrarFalloAsync(id, 3, hasta);
            await repositorio.RegistrarFalloAsync(id, 3, hasta);
            CredencialAcceso antes = (await repositorio.ObtenerPorUsuarioAsync(id))!;
            await repositorio.RegistrarFalloAsync(id, 3, hasta);
            CredencialAcceso despues = (await repositorio.ObtenerPorUsuarioAsync(id))!;
            await repositorio.ReiniciarIntentosAsync(id);
            CredencialAcceso reiniciada = (await repositorio.ObtenerPorUsuarioAsync(id))!;

            Assert.AreEqual(2, antes.IntentosFallidos);
            Assert.IsNull(antes.BloqueadaHastaUtc);
            Assert.AreEqual(0, despues.IntentosFallidos);
            Assert.AreEqual(hasta, despues.BloqueadaHastaUtc);
            Assert.AreEqual(0, reiniciada.IntentosFallidos);
            Assert.IsNull(reiniciada.BloqueadaHastaUtc);
        }

        [TestMethod]
        public async Task Credencial_FalloSinCredencial_NoHaceNada()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();

            await repositorio.RegistrarFalloAsync(id, 3, Fecha);

            Assert.IsNull(await repositorio.ObtenerPorUsuarioAsync(id));
        }

        // Una sesión ajena retiene la fila; mientras tanto se lanzan dos fallos. Ninguno puede leer un
        // contador viejo: al soltar la fila, Oracle reevalúa cada UPDATE sobre el valor ya confirmado.
        [TestMethod]
        public async Task Credencial_FallosSimultaneos_NoPierdenIncrementos()
        {
            var repositorio = new RepositorioCredencialesOracle(Fabrica);
            (int id, _) = await CrearCuentaAsync();
            await repositorio.AgregarAsync(Credencial(id, 1, Fecha));

            using (SesionAjenaOracle ajena = await SesionAjenaOracle.AbrirAsync(Fabrica))
            {
                await ajena.EjecutarAsync("UPDATE credencial_acceso SET intentos_fallidos = 1 WHERE id_usuario = :id", id);
                Task primero = repositorio.RegistrarFalloAsync(id, 5, Fecha.AddMinutes(15));
                Task segundo = repositorio.RegistrarFalloAsync(id, 5, Fecha.AddMinutes(15));
                await Task.Delay(500);
                Assert.IsFalse(primero.IsCompleted || segundo.IsCompleted, "Los fallos debieron esperar la fila retenida.");
                await ajena.ConfirmarAsync();
                await Task.WhenAll(primero, segundo);
            }

            Assert.AreEqual(3, (await repositorio.ObtenerPorUsuarioAsync(id))!.IntentosFallidos);
        }
    }
}
