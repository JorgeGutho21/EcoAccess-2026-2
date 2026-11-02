using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Xml.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Arquitectura
{

    /// <summary>
    /// Comprueba la dirección de las dependencias de dos maneras:
    /// leyendo los ProjectReference de cada .csproj (lo que se declaró) y las referencias que quedaron dentro
    /// del ensamblado compilado (lo que el código usa de verdad, incluidos paquetes como Oracle).
    /// La regla «Presentacion usa Datos solo en Program.cs» no se puede comprobar así.
    /// </summary>
    [TestClass]
    public class PruebasArquitectura
    {
        // Referencias de proyecto permitidas para cada proyecto, según el diagrama de dependencias.
        private static readonly Dictionary<string, string[]> ReferenciasPermitidas = new()
        {
            ["src/EcoAccess.P3.Entidades/EcoAccess.P3.Entidades.csproj"] = Array.Empty<string>(),
            ["src/EcoAccess.P3.Logica/EcoAccess.P3.Logica.csproj"] = new[] { "EcoAccess.P3.Entidades" },
            ["src/EcoAccess.P3.Datos/EcoAccess.P3.Datos.csproj"] = new[] { "EcoAccess.P3.Logica", "EcoAccess.P3.Entidades" },
            ["src/EcoAccess.P3.Presentacion/EcoAccess.P3.Presentacion.csproj"] =
                new[] { "EcoAccess.P3.Logica", "EcoAccess.P3.Entidades", "EcoAccess.P3.Datos" },
            // El bot es una segunda raíz de composición, como Presentacion: solo él junta Logica con Datos.
            ["src/EcoAccess.P3.Bot/EcoAccess.P3.Bot.csproj"] = new[] { "EcoAccess.P3.Logica", "EcoAccess.P3.Entidades", "EcoAccess.P3.Datos" },
            ["tests/unitarias/EcoAccess.P3.Tests/EcoAccess.P3.Tests.csproj"] = new[] { "EcoAccess.P3.Entidades", "EcoAccess.P3.Logica", "EcoAccess.P3.Bot" },
            // El banco Oracle también inicia el ejecutable Bot para verificar el bridge entre procesos.
            // Esta referencia pertenece a pruebas; no cambia las dependencias de las cuatro capas productivas.
            ["tests/integracion/EcoAccess.P3.Datos.Tests/EcoAccess.P3.Datos.Tests.csproj"] = new[] { "EcoAccess.P3.Entidades", "EcoAccess.P3.Logica", "EcoAccess.P3.Datos", "EcoAccess.P3.Bot" },
        };

        [TestMethod]
        public void ReferenciasDeProyecto_CoincidenConElDiagramaDeDependencias()
        {
            string raiz = BuscarRaizDeLaSolucion();

            foreach ((string rutaProyecto, string[] permitidas) in ReferenciasPermitidas)
            {
                string[] declaradas = LeerReferenciasDeProyecto(Path.Combine(raiz, rutaProyecto));
                string[] noPermitidas = declaradas.Except(permitidas).ToArray();

                Assert.IsEmpty(noPermitidas, $"{rutaProyecto} referencia sin permiso: {string.Join(", ", noPermitidas)}");
            }
        }

        [TestMethod]
        public void Entidades_NoUsaOtrosProyectosOracleNiWindowsForms()
        {
            Assembly entidades = typeof(Usuario).Assembly;

            // Control de cordura: si la lista viniera vacía, la prueba pasaría sin comprobar nada.
            Assert.IsNotEmpty(entidades.GetReferencedAssemblies());

            string[] prohibidas = ReferenciasQueEmpiezanPor(entidades, "EcoAccess.P3.", "Oracle.", "System.Windows.Forms");

            Assert.IsEmpty(prohibidas, $"Entidades referencia: {string.Join(", ", prohibidas)}");
        }

        [TestMethod]
        public void Logica_NoUsaDatosPresentacionOracleNiWindowsForms()
        {
            // Mientras Logica no tenga clases, su ensamblado no referencia nada y esta prueba pasa de forma trivial;
            // la prueba de los .csproj sí vigila desde ya que nadie le agregue una referencia indebida.
            Assembly logica = Assembly.Load("EcoAccess.P3.Logica");

            string[] prohibidas = ReferenciasQueEmpiezanPor(
                logica, "EcoAccess.P3.Datos", "EcoAccess.P3.Presentacion", "Oracle.", "System.Windows.Forms");

            Assert.IsEmpty(prohibidas, $"Logica referencia: {string.Join(", ", prohibidas)}");
        }

        [TestMethod]
        public void Bot_UsaDatosSoloEnProgram()
        {
            // Los adaptadores de Telegram no tocan Oracle: solo la raíz de composición crea los repositorios.
            string carpeta = Path.Combine(BuscarRaizDeLaSolucion(), "src", "EcoAccess.P3.Bot");
            string[] archivos = Directory.GetFiles(carpeta, "*.cs", SearchOption.AllDirectories)
                .Where(ruta => !ruta.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                               && !ruta.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                .ToArray();
            Assert.IsNotEmpty(archivos);

            string[] conDatos = archivos
                .Where(ruta => Path.GetFileName(ruta) != "Program.cs")
                .Where(ruta => File.ReadAllText(ruta).Contains("EcoAccess.P3.Datos", StringComparison.Ordinal)
                               || File.ReadAllText(ruta).Contains("Oracle.ManagedDataAccess", StringComparison.Ordinal))
                .Select(Path.GetFileName)
                .ToArray()!;

            Assert.IsEmpty(conDatos, $"Usan Datos fuera de Program.cs: {string.Join(", ", conDatos)}");
        }

        private static string[] ReferenciasQueEmpiezanPor(Assembly ensamblado, params string[] prefijos)
        {
            return ensamblado.GetReferencedAssemblies()
                .Select(referencia => referencia.Name ?? string.Empty)
                .Where(nombre => nombre != "EcoAccess.P3.Compatibilidad")
                .Where(nombre => prefijos.Any(prefijo => nombre.StartsWith(prefijo, StringComparison.Ordinal)))
                .ToArray();
        }

        private static string[] LeerReferenciasDeProyecto(string rutaCsproj)
        {
            Assert.IsTrue(File.Exists(rutaCsproj), $"No se encontró {rutaCsproj}");

            return XDocument.Load(rutaCsproj)
                .Descendants("ProjectReference")
                // Los .csproj usan «\» como separador; en Linux Path no lo reconoce, por eso se normaliza antes.
                .Select(referencia => ((string?)referencia.Attribute("Include") ?? string.Empty).Replace('\\', '/'))
                .Select(ruta => Path.GetFileNameWithoutExtension(ruta))
                .ToArray();
        }

        /// <summary>Sube desde la carpeta bin de las pruebas hasta encontrar EcoAccess.P3.sln.</summary>
        private static string BuscarRaizDeLaSolucion()
        {
            DirectoryInfo? carpeta = new(AppContext.BaseDirectory);
            while (carpeta is not null && !File.Exists(Path.Combine(carpeta.FullName, "EcoAccess.P3.sln")))
            {
                carpeta = carpeta.Parent;
            }

            Assert.IsNotNull(carpeta, "No se encontró EcoAccess.P3.sln por encima de la carpeta de pruebas.");
            return carpeta.FullName;
        }
    }
}
