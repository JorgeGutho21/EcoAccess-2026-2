using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass, TestCategory("VisionLocalReal"), DoNotParallelize]
    public sealed class QwenAmplioRealTests
    {
        private sealed record Muestra(string Archivo, string? Esperado, string Clase, string Sha);
        private sealed record Particion(Muestra[] Muestras);
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public async Task ParticionFijada24FotosPublicas_ProveedorRealConMetricasSinOracle()
        {
            if (Environment.GetEnvironmentVariable("ECOACCESS_PROBAR_QWEN_AMPLIO") != "1")
                Assert.Inconclusive("Evaluación local costosa en tiempo; requiere selección explícita, corpus MIT y modelo instalado.");
            string? manifiesto = Environment.GetEnvironmentVariable("ECOACCESS_PARTICION_QWEN");
            Assert.IsFalse(Compat.IsNullOrWhiteSpace(manifiesto));
            var particion = JsonSerializer.Deserialize<Particion>(await Compat.ReadAllTextAsync(manifiesto!))!;
            Assert.HasCount(24, particion.Muestras);
            Assert.AreEqual(24, particion.Muestras.Select(m => m.Sha).Distinct().Count());
            using var salud = new DiagnosticoOllamaLocal();
            Assert.IsTrue((await salud.ComprobarAsync()).ListoParaInferencia);
            string raiz = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            string carpeta = Path.Combine(raiz, "Vision", "Evaluaciones"); Directory.CreateDirectory(carpeta);
            string marca = Guid.NewGuid().ToString("N");
            string anterior = EstadoVisionLocal.Archivo, origen = EstadoVisionLocal.Origen;
            IReadOnlyList<Material> catalogo = new Material[]
    {
        new(1,"PAPEL","Papel",CategoriaMaterial.Aprovechable,null,true),
        new(2,"VIDRIO","Vidrio",CategoriaMaterial.Aprovechable,null,true),
        new(3,"CARTON","Cartón",CategoriaMaterial.Aprovechable,null,true),
        new(4,"PET","Botella PET",CategoriaMaterial.Aprovechable,null,true)
    };
            var filas = new List<object>(); int fallos = 0;
            try
            {
                EstadoVisionLocal.Archivo = Path.Combine(carpeta, "estado-amplio-" + marca + ".json"); EstadoVisionLocal.Origen = "evaluacion-controlada";
                using var qwen = new ClasificadorOllamaLocal();
                foreach (var m in particion.Muestras)
                {
                    byte[] bytes = await Compat.ReadAllBytesAsync(Path.Combine(raiz, "Modelos", "trashnet-6fa2b87", m.Archivo));
                    Assert.AreEqual(m.Sha, Compat.ToHexString(Compat.Sha256(bytes)));
                    var tiempo = Stopwatch.StartNew();
                    try
                    {
                        var p = await qwen.ClasificarAsync(new(bytes, "image/jpeg"), catalogo);
                        bool propuesta = p.Tipo == TipoPropuestaVision.MaterialIdentificado && p.CodigoMaterial is not null;
                        filas.Add(new
                        {
                            m.Archivo,
                            m.Sha,
                            m.Clase,
                            m.Esperado,
                            Tipo = p.Tipo.ToString(),
                            p.CodigoMaterial,
                            Objeto = p.Objeto?.ToString(),
                            p.ConfianzaDeclarada,
                            Ms = tiempo.ElapsedMilliseconds,
                            Abstencion = !propuesta,
                            AciertoMaterial = m.Esperado is not null && propuesta && p.CodigoMaterial == m.Esperado,
                            ControlFueraCatalogo = m.Esperado is null,
                            FalsoMaterialEnControl = m.Esperado is null && propuesta,
                            MemoriaHostBytes = Process.GetCurrentProcess().WorkingSet64
                        });
                    }
                    catch (Exception e) { fallos++; filas.Add(new { m.Archivo, m.Clase, Fallo = e.GetType().Name, Ms = tiempo.ElapsedMilliseconds }); }
                    await Compat.WriteAllTextAsync(Path.Combine(carpeta, "avance-amplio-" + marca + ".json"),
                        JsonSerializer.Serialize(new { Procesadas = filas.Count, Total = 24, Fallos = fallos }));
                }
            }
            finally { EstadoVisionLocal.Archivo = anterior; EstadoVisionLocal.Origen = origen; }
            string evidencia = Path.Combine(carpeta, "qwen-amplio-" + marca + ".json");
            await Compat.WriteAllTextAsync(evidencia, JsonSerializer.Serialize(new
            {
                FechaUtc = DateTime.UtcNow,
                Modelo = ClasificadorOllamaLocal.Modelo,
                ClasificadorEnsambladoSha256 = Compat.ToHexString(Compat.Sha256(await Compat.ReadAllBytesAsync(typeof(ClasificadorOllamaLocal).Assembly.Location))),
                ParticionSha256 = Compat.ToHexString(Compat.Sha256(await Compat.ReadAllBytesAsync(manifiesto!))),
                Fuente = "TrashNet MIT revisión 6fa2b878c6c1b4304b91109070ce0edf9279bb31",
                Alcance = "24 fotografías, seis categorías, catálogo controlado; plástico genérico no prueba PET, metal no prueba aluminio; sin Oracle, Telegram ni autorización",
                Fallos = fallos,
                Resultados = filas
            }, new JsonSerializerOptions { WriteIndented = true }));
            TestContext.WriteLine("Resultados sin fotos ni respuesta libre: " + evidencia);
            Assert.AreEqual(0, fallos, "Revisar fallos técnicos; los errores de material se informan separadamente.");
        }
    }
}
