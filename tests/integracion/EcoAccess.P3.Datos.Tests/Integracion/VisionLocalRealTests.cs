using System;
using System.Collections.Generic;
using System.IO;
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

    /// <summary>Proveedor local real independiente de Oracle. No es una prueba del bot, de admisión ni de hardware.</summary>
    [TestClass, TestCategory("VisionLocalReal"), DoNotParallelize]
    public sealed class VisionLocalRealTests
    {
        public TestContext TestContext { get; set; } = null!;

        [TestMethod]
        public async Task TresFotosPublicasNuevas_VisionRealSinOracleNiNube_DevuelvePropuestasValidas()
        {
            if (Environment.GetEnvironmentVariable("ECOACCESS_PROBAR_VISION_REAL") != "1")
                Assert.Inconclusive("Ensayo externo local: requiere autorización de ejecución explícita ECOACCESS_PROBAR_VISION_REAL=1 y el modelo instalado; no se cuenta aprobado sin inferir.");
            using var diagnostico = new DiagnosticoOllamaLocal();
            var salud = await diagnostico.ComprobarAsync();
            Assert.IsTrue(salud.ListoParaInferencia, "visión local local no está listo; no se descargó ni detuvo otro modelo.");
            var muestras = new[]
            {
                (Archivo:"paper/paper219.jpg",Codigo:"PAPEL",Sha:"0097B8F965F69B557DA82846482508E27A2B7C12F56BD3BDAAA43F69EE5C22FB"),
                (Archivo:"glass/glass465.jpg",Codigo:"VIDRIO",Sha:"00CBA166AE35F321E828C3A5BF23A3F626B027FFA14E43C11423994972F319A0"),
                (Archivo:"cardboard/cardboard308.jpg",Codigo:"CARTON",Sha:"00780D2F2ABC38C3F1F3C2A89C017702E890C652CFA8C6B70597278BB80D5AE0")
            };
            IReadOnlyList<Material> catalogo =
            new Material[]
    {
        new(1,"PAPEL","Papel",CategoriaMaterial.Aprovechable,null,true),
        new(2,"VIDRIO","Vidrio",CategoriaMaterial.Aprovechable,null,true),
        new(3,"CARTON","Cartón",CategoriaMaterial.Aprovechable,null,true),
        new(4,"PET","Botella PET",CategoriaMaterial.Aprovechable,null,true)
    };
            string raiz = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EcoAccessP3Framework");
            string carpeta = Path.Combine(raiz, "Vision", "Evaluaciones"); Directory.CreateDirectory(carpeta);
            string marca = Guid.NewGuid().ToString("N"); string archivoEstado = Path.Combine(carpeta, "estado-" + marca + ".json");
            string anterior = EstadoVisionLocal.Archivo; string origen = EstadoVisionLocal.Origen;
            var resultados = new List<object>(); int fallos = 0;
            try
            {
                EstadoVisionLocal.Archivo = archivoEstado; EstadoVisionLocal.Origen = "evaluacion-controlada";
                using var qwen = new ClasificadorOllamaLocal();
                foreach (var m in muestras)
                {
                    byte[] bytes = await Compat.ReadAllBytesAsync(Path.Combine(raiz, "Modelos", "trashnet-6fa2b87", m.Archivo));
                    Assert.AreEqual(m.Sha, Compat.ToHexString(Compat.Sha256(bytes)), "La foto no coincide con el corpus público MIT fijado.");
                    var reloj = Stopwatch.StartNew();
                    try
                    {
                        var p = await qwen.ClasificarAsync(new ImagenResiduo(bytes, "image/jpeg"), catalogo);
                        resultados.Add(new
                        {
                            m.Archivo,
                            m.Sha,
                            Esperado = m.Codigo,
                            Resultado = p.Tipo.ToString(),
                            p.CodigoMaterial,
                            Objeto = p.Objeto?.ToString(),
                            p.ConfianzaDeclarada,
                            Milisegundos = reloj.ElapsedMilliseconds,
                            Acierto = p.Tipo == TipoPropuestaVision.MaterialIdentificado && p.CodigoMaterial == m.Codigo
                        });
                    }
                    catch (Exception error)
                    { fallos++; resultados.Add(new { m.Archivo, m.Sha, Fallo = error.GetType().Name, Milisegundos = reloj.ElapsedMilliseconds }); }
                }
            }
            finally { EstadoVisionLocal.Archivo = anterior; EstadoVisionLocal.Origen = origen; }
            string evidencia = Path.Combine(carpeta, "vision-control-" + marca + ".json");
            await Compat.WriteAllTextAsync(evidencia, JsonSerializer.Serialize(new
            {
                FechaUtc = DateTime.UtcNow,
                Modelo = ClasificadorOllamaLocal.Modelo,
                Fuente = "TrashNet MIT, revisión6fa2b878c6c1b4304b91109070ce0edf9279bb31",
                Catalogo = "Ensayo de cuatro materiales, sin Oracle ni admisión de caneca",
                Limite = "Tres imágenes no presentes por nombre en evaluación47; sin ajuste en este ensayo, no es precisión poblacional ni prueba Telegram",
                Fallos = fallos,
                Resultados = resultados
            }, new JsonSerializerOptions { WriteIndented = true }));
            TestContext.WriteLine("Evidencia local sin imágenes ni respuestas libres: " + evidencia);
            Assert.AreEqual(0, fallos, "Hubo fallos de inferencia real; consultar la evidencia estructurada. Los errores de material se informan y no se ocultan.");
        }
    }
}
