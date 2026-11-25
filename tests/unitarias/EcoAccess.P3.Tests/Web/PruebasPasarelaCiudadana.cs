using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using EcoAccess.P3.PasarelaCiudadana;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Web
{
    /// <summary>Reglas de la pasarela ciudadana: nada se mueve sin permiso vigente, sin duplicados y con tiempos reales.</summary>
    [TestClass]
    public sealed class PruebasPasarelaCiudadana
    {
        private static readonly DateTime Ahora = new(2026, 11, 25, 12, 0, 0, DateTimeKind.Utc);

        [TestMethod]
        public void PermisoValidoSoloConElSecretoEmitidoYAntesDeExpirar()
        {
            var p = new PermisoPresencial();
            Assert.IsFalse(p.Valido("Bearer cualquiera-de-veinte-caracteres", Ahora), "sin emitir no hay permiso");
            string s = p.Emitir(TimeSpan.FromHours(2), Ahora);
            Assert.IsTrue(s.Length >= 40);
            Assert.IsTrue(p.Valido("Bearer " + s, Ahora));
            Assert.IsFalse(p.Valido(s, Ahora), "sin esquema Bearer");
            Assert.IsFalse(p.Valido("Bearer " + s + "x", Ahora));
            Assert.IsFalse(p.Valido("Bearer " + s, Ahora.AddHours(2).AddSeconds(1)), "expirado");
            string otro = p.Emitir(TimeSpan.FromHours(2), Ahora);
            Assert.IsFalse(p.Valido("Bearer " + s, Ahora), "un QR nuevo invalida el anterior");
            Assert.IsTrue(p.Valido("Bearer " + otro, Ahora));
            p.Revocar();
            Assert.IsFalse(p.Valido("Bearer " + otro, Ahora), "terminar la sesión invalida el QR");
        }

        [TestMethod]
        public void LimitadorExigeSeparacionYMaximoPorVentana()
        {
            var l = new Limitador(TimeSpan.FromSeconds(6), 3, TimeSpan.FromMinutes(10));
            Assert.IsNull(l.Intentar(Ahora));
            Assert.IsNotNull(l.Intentar(Ahora.AddSeconds(2)), "doble clic o ráfaga");
            Assert.IsNull(l.Intentar(Ahora.AddSeconds(7)));
            Assert.IsNull(l.Intentar(Ahora.AddSeconds(14)));
            Assert.IsNotNull(l.Intentar(Ahora.AddSeconds(30)), "máximo por ventana");
            Assert.IsNull(l.Intentar(Ahora.AddMinutes(11)), "la ventana se libera");
        }

        [TestMethod]
        public void IdempotenciaDevuelveElResultadoOriginalSinRepetir()
        {
            var i = new Idempotencia<string>(capacidad: 2);
            Assert.IsNull(i.Buscar("clave-aaaaaaaaaaaa"));
            i.Guardar("clave-aaaaaaaaaaaa", "primera");
            i.Guardar("clave-aaaaaaaaaaaa", "segunda");
            Assert.AreEqual("primera", i.Buscar("clave-aaaaaaaaaaaa"));
            i.Guardar("b", "x"); i.Guardar("c", "y");
            Assert.IsNull(i.Buscar("clave-aaaaaaaaaaaa"), "capacidad acotada");
        }

        [TestMethod]
        public void SolicitudesAntiguasOFuturasNoSeEjecutan()
        {
            Assert.IsTrue(Reglas.Reciente(Ahora.AddSeconds(-3).ToString("o"), Ahora));
            Assert.IsFalse(Reglas.Reciente(Ahora.AddMinutes(-2).ToString("o"), Ahora), "solicitud vieja (recarga, reenvío)");
            Assert.IsFalse(Reglas.Reciente(Ahora.AddMinutes(2).ToString("o"), Ahora));
            Assert.IsFalse(Reglas.Reciente("no-es-fecha", Ahora));
            Assert.IsFalse(Reglas.Reciente(null, Ahora));
        }

        [TestMethod]
        public void CanecasYClavesSeValidan()
        {
            Assert.AreEqual("VBN", Reglas.NormalizarCanecas("nbvVx"));
            Assert.AreEqual("B", Reglas.NormalizarCanecas("b"));
            Assert.AreEqual("", Reglas.NormalizarCanecas("xyz"));
            Assert.IsTrue(Reglas.ClaveValida("0123456789abcdef0123"));
            Assert.IsFalse(Reglas.ClaveValida("corta"));
            Assert.IsFalse(Reglas.ClaveValida("con espacios y símbolos !!!!"));
        }

        [TestMethod]
        public void PoliticaGrupoAplica7_14_21SegundosSegunCanecasDistintas()
        {
            foreach (var (c, ms) in new[] { ("B", 7000), ("VB", 14000), ("VBN", 21000) })
            {
                var plan = Reglas.Planificar(c, grupoDisponible: true, new Dictionary<string, int>());
                Assert.AreEqual("grupo", plan.Modo);
                Assert.AreEqual("/api/estacion/abrir-grupo", plan.Orden);
                Assert.AreEqual(c.Length, plan.Tramos.Count);
                Assert.IsTrue(plan.Tramos.All(t => t.AbiertaMs == ms), c);
                Assert.AreEqual(0, plan.Tramos[0].InicioMs);
            }
        }

        [TestMethod]
        public void FirmwareV1UsaElTiempoCalibradoSinModificarlo()
        {
            var calibrado = new Dictionary<string, int> { ["V"] = 2000, ["B"] = 2000, ["N"] = 3000 };
            var una = Reglas.Planificar("B", grupoDisponible: false, calibrado);
            Assert.AreEqual("ciclo", una.Modo);
            Assert.AreEqual("/api/canecas/B/abrir", una.Orden);
            Assert.AreEqual(2000 - Reglas.MovimientoServoMs, una.Tramos[0].AbiertaMs, "la ventana descuenta el recorrido del servo");
            var varias = Reglas.Planificar("BN", grupoDisponible: false, calibrado);
            Assert.AreEqual("/api/pruebas/secuencial", varias.Orden, "varias canecas en v1: SEC (una tras otra)");
            Assert.IsTrue(varias.Tramos[1].InicioMs >= 2000 + Reglas.CierreFirmwareMs, "la segunda abre cuando la primera cerró");
        }

        [TestMethod]
        public void CatalogoCiudadanoTiene90ResiduosConCodigosOracleExistentes()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            Assert.IsNotNull(dir);
            string raiz = dir!.FullName;
            var json = new JavaScriptSerializer { MaxJsonLength = 1 << 22 }.DeserializeObject(File.ReadAllText(Path.Combine(raiz, "web", "ciudadano", "datos", "catalogo-residuos.json"))) as Dictionary<string, object>;
            var residuos = ((object[])json!["residuos"]).Cast<Dictionary<string, object>>().ToList();
            Assert.AreEqual(90, residuos.Count);
            foreach (var c in new[] { "verde", "blanca", "negra" }) Assert.AreEqual(30, residuos.Count(r => (string)r["caneca"] == c), c);

            // Códigos de MATERIAL definidos en los scripts de la base (datos y migraciones): el catálogo web no inventa materiales.
            var sql = string.Join("\n", Directory.GetFiles(Path.Combine(raiz, "database"), "*.sql", SearchOption.AllDirectories).Select(File.ReadAllText));
            var codigos = new HashSet<string>(Regex.Matches(sql, @"'([A-Z][A-Z0-9_]{2,})'").Cast<Match>().Select(m => m.Groups[1].Value));
            var categoria = new Dictionary<string, int> { ["blanca"] = 1, ["verde"] = 2, ["negra"] = 3 };
            foreach (var r in residuos)
            {
                string codigo = (string)r["oracle"];
                Assert.IsTrue(codigos.Contains(codigo), $"{r["id"]}: el código {codigo} no existe en los scripts de Oracle");
            }
            // Cada código usado conserva la categoría con la que Oracle lo registró (ningún residuo cambia de caneca en la BD).
            foreach (Match m in Regex.Matches(sql, @"'([A-Z][A-Z0-9_]{2,})'\s*(?:AS\s+codigo\s*)?,\s*'[^']*'\s*(?:AS\s+nombre\s*)?,\s*([1-4])\b"))
            {
                var usados = residuos.Where(r => (string)r["oracle"] == m.Groups[1].Value).ToList();
                foreach (var r in usados) Assert.AreEqual(int.Parse(m.Groups[2].Value), categoria[(string)r["caneca"]], $"{r["id"]} usa {m.Groups[1].Value} con otra categoría");
            }
        }
    }
}
