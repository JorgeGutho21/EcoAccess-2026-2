using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EcoAccess.P3.Instalador;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Datos.Tests.Instalador
{
    /// <summary>El instalador sin Docker debe leer los scripts reales igual que SQL*Plus / SQL Developer (F5).</summary>
    [TestClass]
    public sealed class LectorScriptSqlTests
    {
        [TestMethod]
        public void SeparaSqlPlSqlConsultasYDirectivas()
        {
            string script = string.Join("\n",
                "-- comentario",
                "SET SERVEROUTPUT ON",
                "WHENEVER SQLERROR EXIT SQL.SQLCODE",
                "DECLARE",
                "    n NUMBER;",
                "BEGIN",
                "    SELECT COUNT(*) INTO n FROM dual;",
                "END;",
                "/",
                "CREATE TABLE t (",
                "    id NUMBER(10), -- columna",
                "    texto VARCHAR2(20 CHAR) DEFAULT 'a;b'",
                ");",
                "CREATE INDEX ix_t ON t(id); -- comentario al final",
                "PROMPT LISTO",
                "WHENEVER SQLERROR CONTINUE",
                "SELECT id",
                "FROM t;");

            IReadOnlyList<SentenciaScript> s = LectorScriptSql.Leer(script);

            CollectionAssert.AreEqual(new[]
            {
                TipoSentencia.DetenerseAnteErrores, TipoSentencia.PlSql, TipoSentencia.Sql, TipoSentencia.Sql,
                TipoSentencia.Mensaje, TipoSentencia.ContinuarAnteErrores, TipoSentencia.Consulta,
            }, s.Select(x => x.Tipo).ToArray());
            StringAssert.EndsWith(s[1].Texto, "END;");
            StringAssert.Contains(s[2].Texto, "'a;b'");
            Assert.IsFalse(s[2].Texto.TrimEnd().EndsWith(";", StringComparison.Ordinal), "El «;» final no se envía a ODP.NET.");
            Assert.AreEqual(10, s[2].Linea);
            Assert.AreEqual("LISTO", s[4].Texto);
        }

        [TestMethod]
        public void ExecSeConvierteEnBloque()
        {
            var s = LectorScriptSql.Leer("EXEC DBMS_OUTPUT.PUT_LINE('x');");
            Assert.AreEqual(TipoSentencia.PlSql, s.Single().Tipo);
            Assert.AreEqual("BEGIN DBMS_OUTPUT.PUT_LINE('x'); END;", s.Single().Texto);
        }

        [TestMethod]
        [DataRow("@@otro.sql")]
        [DataRow("DEFINE clave = x")]
        [DataRow("ACCEPT clave PROMPT 'x' HIDE")]
        [DataRow("INSERT INTO t VALUES ('&valor');\nUPDATE t SET a = &&x;")]
        public void RechazaFuncionesNoAdmitidas(string script) =>
            Assert.ThrowsExactly<FormatException>(() => LectorScriptSql.Leer(script));

        [TestMethod]
        public void SentenciaSinTerminar_Lanza() =>
            Assert.ThrowsExactly<FormatException>(() => LectorScriptSql.Leer("CREATE TABLE t (id NUMBER)"));

        [TestMethod]
        public void VariablesDeEnlace_IgnoraAsignacionTextosYComentarios()
        {
            const string bloque = "DECLARE v VARCHAR2(9) := :clave_esquema; h VARCHAR2(5) := 'HH:MI'; BEGIN NULL; -- :nada\n END;";
            CollectionAssert.AreEqual(new[] { "clave_esquema" }, LectorScriptSql.VariablesDeEnlace(bloque).ToArray());
        }

        [TestMethod]
        public void TriggerConNewYOld_NoTieneVariablesDeEnlace()
        {
            string trigger = string.Join("\n",
                "CREATE OR REPLACE TRIGGER tr_x BEFORE UPDATE ON t FOR EACH ROW",
                "BEGIN",
                "    IF :NEW.valor <> :OLD.valor THEN :NEW.cambio := 1; END IF;",
                "END;",
                "/",
                "SHOW ERRORS TRIGGER tr_x");
            SentenciaScript bloque = LectorScriptSql.Leer(trigger).Single();
            Assert.AreEqual(TipoSentencia.PlSql, bloque.Tipo);
            Assert.AreEqual(0, LectorScriptSql.VariablesDeEnlace(bloque.Texto).Count);
        }

        /// <summary>Todos los scripts del plan real (V001–V020 y catálogos) se leen sin error y sin variables de enlace.</summary>
        [TestMethod]
        public void ScriptsDelPlanSeLeenCompletos()
        {
            string raiz = Raiz();
            foreach (PasoInstalacion paso in PlanInstalacion.Pasos)
            {
                IReadOnlyList<SentenciaScript> sentencias = LectorScriptSql.Leer(File.ReadAllText(Path.Combine(raiz, paso.Archivo)));
                Assert.IsTrue(sentencias.Any(x => x.Tipo is TipoSentencia.Sql or TipoSentencia.PlSql), paso.Archivo);
                foreach (SentenciaScript s in sentencias)
                    Assert.AreEqual(0, LectorScriptSql.VariablesDeEnlace(s.Texto).Count, $"{paso.Archivo}:{s.Linea} tiene variables de enlace");
                Assert.AreEqual(64, paso.Huella(raiz).Length);
            }
        }

        [TestMethod]
        [DataRow("database/00_administracion/03_registro_migraciones.sql", "")]
        [DataRow("database/00_administracion/04_crear_esquema_oracle_cloud.sql", "clave_esquema")]
        [DataRow("database/00_administracion/05_conceder_permisos_roles.sql", "")]
        [DataRow("database/00_administracion/06_crear_cuenta_integrante.sql", "usuario_integrante,clave_integrante,rol_integrante")]
        [DataRow("database/00_administracion/07_prueba_colaborativa.sql", "")]
        public void ScriptsAdministrativosDeclaranSoloSusVariables(string archivo, string esperadas)
        {
            var sentencias = LectorScriptSql.Leer(File.ReadAllText(Path.Combine(Raiz(), archivo)));
            string[] variables = sentencias.SelectMany(s => LectorScriptSql.VariablesDeEnlace(s.Texto)).Distinct().ToArray();
            CollectionAssert.AreEquivalent(esperadas.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), variables);
        }

        [TestMethod]
        public void Plan_PendientesEnOrdenYDemoOpcional()
        {
            var aplicadas = new HashSet<string> { "V001", "C001", "V002", "V003", "V004", "V005", "V006", "V007", "V008" };
            string[] pendientes = PlanInstalacion.Pendientes(aplicadas, conDemo: false).Select(p => p.Version).ToArray();
            Assert.AreEqual("V009", pendientes.First());
            Assert.AreEqual("V033", pendientes.Last());
            Assert.AreEqual(27, pendientes.Length);
            Assert.AreEqual("D001", PlanInstalacion.Pendientes(aplicadas, conDemo: true).Last().Version);
            Assert.IsNull(PlanInstalacion.ValidarOrden(aplicadas));
            Assert.IsNotNull(PlanInstalacion.ValidarOrden(new HashSet<string> { "V001", "V003" }));
        }

        /// <summary>Ninguna migración numerada del repositorio queda fuera del plan.</summary>
        [TestMethod]
        public void TodasLasMigracionesDelRepositorioEstanEnElPlan()
        {
            string[] enDisco = Directory.GetFiles(Path.Combine(Raiz(), "database", "06_migraciones"), "V*.sql")
                .Select(r => "database/06_migraciones/" + Path.GetFileName(r)).ToArray();
            CollectionAssert.IsSubsetOf(enDisco, PlanInstalacion.Pasos.Select(p => p.Archivo).ToArray());
        }

        private static string Raiz()
        {
            for (DirectoryInfo? d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); d is not null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "EcoAccess.P3.sln"))) return d.FullName;
            throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }
    }
}
