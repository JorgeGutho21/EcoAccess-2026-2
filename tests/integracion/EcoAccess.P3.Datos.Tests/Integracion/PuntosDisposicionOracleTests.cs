using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{
    /// <summary>
    /// V026 en Oracle real, SOLO en el esquema aislado ECOACCESS_P3_PRUEBAS: aplica el bloque de la migración (idempotente),
    /// lee los puntos con el repositorio y comprueba que coinciden con la semilla en C# y que las restricciones rechazan
    /// una dirección inventada para un programa o una fuente que no sea https.
    /// </summary>
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class PuntosDisposicionOracleTests : BaseIntegracionOracle
    {
        private const string EsquemaAislado = "ECOACCESS_P3_PRUEBAS";

        private async Task<OracleConnection> AbrirAisladoAsync()
        {
            OracleConnection conexion = await Fabrica.AbrirAsync(System.Threading.CancellationToken.None);
            using OracleCommand esquema = new("SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM dual", conexion);
            if ((string)(await esquema.ExecuteScalarAsync())! != EsquemaAislado)
            {
                conexion.Dispose();
                Assert.Inconclusive($"V026 solo se aplica en {EsquemaAislado}; este secreto apunta a otro esquema y no se toca.");
            }
            return conexion;
        }

        private static string BloqueMigracion()
        {
            string raiz = AppDomain.CurrentDomain.BaseDirectory;
            while (!File.Exists(Path.Combine(raiz, "EcoAccess.P3.sln"))) raiz = Path.GetDirectoryName(raiz) ?? throw new InvalidOperationException("Sin raíz.");
            string sql = File.ReadAllText(Path.Combine(raiz, "database", "06_migraciones", "V026__puntos_disposicion_valledupar.sql"));
            int inicio = sql.IndexOf("-- BLOQUE-INICIO", StringComparison.Ordinal), fin = sql.IndexOf("-- BLOQUE-FIN", StringComparison.Ordinal);
            return sql.Substring(inicio, fin - inicio).Replace("-- BLOQUE-INICIO", "").Replace("\r", "").Trim();
        }

        [TestMethod]
        public async Task LaMigracionEsIdempotenteYElRepositorioDevuelveLaSemilla()
        {
            using (OracleConnection conexion = await AbrirAisladoAsync())
            {
                for (int vez = 0; vez < 2; vez++)
                {
                    using var bloque = new OracleCommand(BloqueMigracion(), conexion);
                    await bloque.ExecuteNonQueryAsync();
                }
            }
            var puntos = await new RepositorioPuntosDisposicionOracle(Fabrica).ListarAsync("Valledupar");
            CollectionAssert.IsSubsetOf(CatalogoPuntosValledupar.Semilla.Select(p => p.Codigo).ToArray(), puntos.Select(p => p.Codigo).ToArray());
            foreach (PuntoDisposicion semilla in CatalogoPuntosValledupar.Semilla)
            {
                PuntoDisposicion leido = puntos.Single(p => p.Codigo == semilla.Codigo);
                Assert.AreEqual(semilla.Direccion, leido.Direccion, semilla.Codigo);
                Assert.AreEqual(semilla.Estado, leido.Estado, semilla.Codigo);
                CollectionAssert.AreEquivalent(semilla.Rutas.ToArray(), leido.Rutas.ToArray(), semilla.Codigo);
            }
            string? texto = await new ServicioOrientacionEspecial(new RepositorioPuntosDisposicionOracle(Fabrica)).TextoAsync(RutaDisposicion.Raee, "un celular");
            StringAssert.Contains(texto, "Cra. 9 #6-42");
        }

        [TestMethod]
        public async Task LasRestriccionesRechazanDireccionesInventadasYFuentesSinHttps()
        {
            using OracleConnection conexion = await AbrirAisladoAsync();
            using (var bloque = new OracleCommand(BloqueMigracion(), conexion)) await bloque.ExecuteNonQueryAsync();
            async Task<bool> InsertaAsync(string? direccion, string estado, string fuente)
            {
                using var insertar = new OracleCommand("INSERT INTO punto_disposicion (codigo, nombre, direccion, municipio, rutas, recibe, estado, fuente, fecha_verificacion) " +
                    "VALUES ('PRUEBA-' || :s, 'Prueba', :d, 'Valledupar', 'Raee', 'prueba', :e, :f, DATE '2026-11-12')", conexion) { BindByName = true };
                insertar.Parameters.Add("s", Sufijo()); insertar.Parameters.Add("d", direccion); insertar.Parameters.Add("e", estado); insertar.Parameters.Add("f", fuente);
                try { await insertar.ExecuteNonQueryAsync(); using var deshacer = new OracleCommand("ROLLBACK", conexion); await deshacer.ExecuteNonQueryAsync(); return true; }
                catch (OracleException) { return false; }
            }
            Assert.IsFalse(await InsertaAsync("Calle inventada 1", "SOLO_PROGRAMA", "https://ejemplo.org"), "Un programa no lleva dirección.");
            Assert.IsFalse(await InsertaAsync(null, "LISTADO_SIN_CONFIRMAR", "https://ejemplo.org"), "Un lugar necesita dirección.");
            Assert.IsFalse(await InsertaAsync("Cra. 1", "LISTADO_SIN_CONFIRMAR", "http://sin-tls.org"), "La fuente debe ser https.");
        }
    }
}
