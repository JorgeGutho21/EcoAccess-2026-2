using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// Estación de tres canecas contra Oracle real (V021): guarda la configuración, respeta el dispositivo único,
    /// renombra y resuelve el destino de cada material. Datos ficticios con sufijo único, borrados al terminar.
    /// No prueba hardware físico.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    public sealed class TresCanecasOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task EstacionConTresCanecas_ConfiguraResuelveYRechazaEnOracle()
        {
            if (await ContarAsync("SELECT COUNT(*) FROM user_tables WHERE table_name = 'CONFIGURACION_CANECA'") == 0)
                Assert.Inconclusive("Este esquema no tiene V021: prueba de tres canecas no ejecutada.");

            string s = Sufijo();
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            var configuraciones = new RepositorioConfiguracionCanecasOracle(Fabrica);
            DateTime ahora = DateTime.UtcNow;

            int admin = RegistrarUsuario(await usuarios.AgregarAsync(new Usuario("Admin tres canecas", $"it.tres.{s.ToLowerInvariant()}@example.invalid", CodigoRol.Administrador, ahora)));
            int aprovechable = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_BLA_" + s, "Material IT blanca", CategoriaMaterial.Aprovechable, null)));
            int organico = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_VER_" + s, "Material IT verde", CategoriaMaterial.OrganicoAprovechable, null)));
            int noAprovechable = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_NEG_" + s, "Material IT negra", CategoriaMaterial.NoAprovechable, null)));
            int peligroso = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_PEL_" + s, "Material IT peligroso", CategoriaMaterial.Peligroso, null)));
            int estacion = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("IT estación " + s, "Prueba de integración")));

            async Task<int> CanecaAsync(string color, int material)
            {
                int id = RegistrarContenedor(await contenedores.AgregarAsync(new Contenedor(estacion, $"IT {color} {s}", $"IT_{color}_{s}_QR0001", ahora)));
                await contenedores.AdmitirMaterialAsync(id, material);
                await contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio);
                return id;
            }

            int blanca = await CanecaAsync("BLANCA", aprovechable);
            int verde = await CanecaAsync("VERDE", organico);
            int negra = await CanecaAsync("NEGRA", noAprovechable);
            var servicio = new ServicioTresCanecas(usuarios, contenedores, materiales, configuraciones, TimeProvider.System);
            try
            {
                Assert.IsTrue((await servicio.ConfigurarAsync(admin, blanca, CategoriaMaterial.Aprovechable, "it-bla-" + s, "izquierda")).Exitoso);
                Assert.IsTrue((await servicio.ConfigurarAsync(admin, verde, CategoriaMaterial.OrganicoAprovechable, "it-ver-" + s, "centro")).Exitoso);
                Assert.IsTrue((await servicio.ConfigurarAsync(admin, negra, CategoriaMaterial.NoAprovechable, "it-neg-" + s, "derecha")).Exitoso);

                // Persistencia: se lee con una conexión nueva.
                IReadOnlyList<ConfiguracionCaneca> leidas = await configuraciones.ListarPorPuntoAsync(estacion);
                CollectionAssert.AreEquivalent(new[] { ColorCaneca.Blanca, ColorCaneca.Verde, ColorCaneca.Negra }, leidas.Select(c => c.Color).ToArray());

                // Cada material va a su caneca, identificada por id_contenedor y dispositivo.
                Assert.AreEqual(blanca, (await servicio.ResolverDestinoAsync(estacion, aprovechable)).Valor.IdContenedor);
                Assert.AreEqual(verde, (await servicio.ResolverDestinoAsync(estacion, organico)).Valor.IdContenedor);
                Resultado<DestinoCaneca> destinoNegra = await servicio.ResolverDestinoAsync(estacion, noAprovechable);
                Assert.AreEqual(negra, destinoNegra.Valor.IdContenedor);
                Assert.AreEqual("it-neg-" + s, destinoNegra.Valor.IdentificadorDispositivo);
                Assert.IsFalse((await servicio.ResolverDestinoAsync(estacion, peligroso)).Exitoso, "Un peligroso no va a ninguna caneca.");

                // Restricciones reales de Oracle: dispositivo único y categoría válida.
                Assert.AreEqual(MotivoRechazo.Duplicado,
                    (await servicio.ConfigurarAsync(admin, verde, CategoriaMaterial.OrganicoAprovechable, "it-bla-" + s, null)).Motivo);
                using (OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None))
                using (OracleCommand comando = ComandosOracle.Crear(conexion, "UPDATE configuracion_caneca SET categoria = 4 WHERE id_contenedor = :id"))
                {
                    comando.Agregar("id", OracleDbType.Int32, negra);
                    OracleException error = await Assert.ThrowsExactlyAsync<OracleException>(() => comando.ExecuteNonQueryAsync());
                    Assert.AreEqual(2290, error.Number, "CK_CONFIGURACION_CATEGORIA rechaza la categoría peligrosa.");
                }

                // Renombrar actualiza CONTENEDOR.nombre.
                Assert.IsTrue((await servicio.RenombrarAsync(admin, negra, $"IT negra renombrada {s}")).Exitoso);
                Assert.AreEqual($"IT negra renombrada {s}", (await contenedores.ObtenerPorIdAsync(negra))!.Nombre);
            }
            finally
            {
                using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
                foreach (int id in new[] { blanca, verde, negra })
                {
                    using OracleCommand borrar = ComandosOracle.Crear(conexion, "DELETE FROM configuracion_caneca WHERE id_contenedor = :id");
                    borrar.Agregar("id", OracleDbType.Int32, id);
                    await borrar.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
