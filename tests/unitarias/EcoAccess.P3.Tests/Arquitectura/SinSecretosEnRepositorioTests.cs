using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Arquitectura
{
    /// <summary>
    /// Fase 12 (seguridad): ningún archivo de texto del repositorio contiene una clave de Groq, un token de bot de
    /// Telegram ni una contraseña escrita en una cadena de conexión. Los secretos viven en User Secrets, fuera del repo.
    /// </summary>
    [TestClass]
    public sealed class SinSecretosEnRepositorioTests
    {
        private static readonly string[] Extensiones =
            { ".cs", ".sql", ".ps1", ".psm1", ".json", ".config", ".md", ".txt", ".xml", ".yml", ".yaml", ".sh", ".ino", ".h", ".csproj", ".props" };

        private static readonly string[] CarpetasIgnoradas = { "\\bin\\", "\\obj\\", "\\.git\\", "\\.vs\\", "\\packages\\", "\\node_modules\\", "\\TestResults\\" };

        private static readonly (string Nombre, Regex Patron)[] Patrones =
        {
            ("clave de Groq", new Regex(@"gsk_[A-Za-z0-9]{40,}", RegexOptions.CultureInvariant)),
            ("token de bot de Telegram", new Regex(@"\b\d{8,10}:AA[A-Za-z0-9_-]{30,}", RegexOptions.CultureInvariant)),
            ("contraseña en cadena de conexión", // Estilo cadena de conexión (clave pegada al signo igual, sin espacios): «password = variable» en C# no es un secreto.
            new Regex(@"(?i)\bpassword=(?!\*|<|\{|\$|""|'|;|\[|\(|%|tu_|su_|x+;|clave|contrase|valor-de|prueba|ejemplo|ficticia)[^;\s""'<>]{6,}", RegexOptions.CultureInvariant)),
        };

        private static string Raiz()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "EcoAccess.P3.sln"))) dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repositorio.");
        }

        [TestMethod]
        public void NingunArchivoDeTextoContieneSecretos()
        {
            string raiz = Raiz();
            var hallazgos = new List<string>();
            int revisados = 0;
            foreach (string archivo in Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories))
            {
                if (CarpetasIgnoradas.Any(c => archivo.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                if (!Extensiones.Contains(Path.GetExtension(archivo), StringComparer.OrdinalIgnoreCase)) continue;
                // Los modelos de Data Modeler de BaseDeDatos_Amilkar superan MAX_PATH (260) cuando el repositorio vive
                // en una carpeta profunda; el prefijo \\?\ permite leerlos en .NET Framework sin cambiar el registro.
                string lectura = archivo.Length >= 260 ? @"\\?\" + archivo : archivo;
                if (new FileInfo(lectura).Length > 5_000_000) continue;
                revisados++;
                string texto = File.ReadAllText(lectura);
                foreach (var (nombre, patron) in Patrones)
                    if (patron.IsMatch(texto))
                        hallazgos.Add($"{nombre} en {archivo.Substring(raiz.Length + 1)}");
            }

            Assert.IsGreaterThan(100, revisados, "La búsqueda debe recorrer el repositorio.");
            Assert.IsEmpty(hallazgos, string.Join("\n", hallazgos));
        }

        [TestMethod]
        [DataRow("gsk_" + "0123456789abcdefghijABCDEFGHIJ0123456789abcdefgh")]
        [DataRow("123456789:" + "AAEabcdefghijklmnopqrstuvwxyz012345")]
        [DataRow("User Id=APP;Password=" + "Secreto123;Data Source=x")]
        public void LosPatronesDetectanSecretosDeEjemplo(string muestra) =>
            Assert.IsTrue(Patrones.Any(p => p.Patron.IsMatch(muestra)), muestra);

        [TestMethod]
        [DataRow("Password=***;")]
        [DataRow("Password=<clave>;")]
        [DataRow("PASSWORD=\"$pw\"")]
        public void LosMarcadoresNoSonSecretos(string muestra) =>
            Assert.IsFalse(Patrones.Any(p => p.Patron.IsMatch(muestra)), muestra);
    }
}
