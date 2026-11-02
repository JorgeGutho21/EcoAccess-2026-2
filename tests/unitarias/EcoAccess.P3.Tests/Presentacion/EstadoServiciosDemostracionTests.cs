using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;
using EcoAccess.P3.Presentacion;

namespace EcoAccess.P3.Tests.Presentacion
{

    [TestClass]
    public sealed class EstadoServiciosDemostracionTests
    {
        private string _directorio = "";

        [TestInitialize]
        public void Preparar() => _directorio = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "estado-servicios-" + Guid.NewGuid().ToString("N"))).FullName;

        [TestCleanup]
        public void Limpiar() => Directory.Delete(_directorio, true);

        [TestMethod]
        public void SinManifiestoNadaApareceActivo()
        {
            var resumen = EstadoServiciosDemostracion.Consultar(_directorio, Path.Combine(_directorio, "no-existe.json"));
            Assert.IsTrue(resumen.Servicios.All(s => !s.Activo), "Sin supervisor no se muestran estados falsos.");
            Assert.IsNull(resumen.UltimaOperacion);
        }

        [TestMethod]
        public void ProcesoVigenteActivoYPidReutilizadoDetenido()
        {
            using var actual = Process.GetCurrentProcess();
            string sesion = Path.Combine(_directorio, "sesion-1");
            Directory.CreateDirectory(sesion);
            File.WriteAllText(Path.Combine(sesion, "Bridge.salida.log"), "[t1] orden 10 recibida\n[t2] orden 10 aplicada\n");
            File.WriteAllText(Path.Combine(sesion, "Telegram.error.log"), "[t] Bot en polling. Ctrl+C para detener.\n[t] Update 5: falló la atención (OracleException).\n");
            long ticks = actual.StartTime.ToUniversalTime().Ticks;
            File.WriteAllText(Path.Combine(_directorio, "sesion.json"), $@"{{""Version"":1,""Directorio"":{System.Text.Json.JsonSerializer.Serialize(sesion)},""Procesos"":[
 {{""Componente"":""Telegram"",""IdProceso"":{actual.Id},""InicioTicks"":{ticks}}},
 {{""Componente"":""Bridge"",""IdProceso"":{actual.Id},""InicioTicks"":{ticks + 1}}}]}}");

            var resumen = EstadoServiciosDemostracion.Consultar(_directorio, Path.Combine(_directorio, "no-existe.json"));

            Assert.IsTrue(resumen.Servicios.Single(s => s.Nombre == "Telegram").Activo);
            Assert.IsFalse(resumen.Servicios.Single(s => s.Nombre == "Bridge").Activo, "Un PID con otra hora de inicio es otro proceso.");
            Assert.IsFalse(resumen.Servicios.Single(s => s.Nombre == "Simulador").Activo);
            Assert.AreEqual("[t2] orden 10 aplicada", resumen.UltimaOperacion);
            Assert.HasCount(1, resumen.Errores, "Las líneas informativas no se muestran como errores.");
        }

    }
}
