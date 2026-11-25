using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Bot.Web;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>Sincronizador de la bitácora ciudadana hacia Oracle y vinculación web–Telegram (sin red ni hardware).</summary>
    [TestClass]
    public sealed class SincronizadorBitacoraWebTests
    {
        private sealed class OracleFalso : IRegistroOperacionesEstacion
        {
            public bool Caido;
            public readonly List<OperacionEstacion> Escrituras = new();
            public Task GuardarAsync(OperacionEstacion op, CancellationToken ct = default)
            {
                if (Caido) throw new InvalidOperationException("Oracle no disponible");
                Escrituras.Add(op); return Task.CompletedTask;
            }
            public Task<IReadOnlyList<OperacionEstacion>> RecientesAsync(int? idUsuario, int maximo, CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<OperacionEstacion>>(Escrituras.Where(e => idUsuario is null || e.IdUsuario == idUsuario).ToList());
        }

        private static string Apertura(string clave, string estado = "COMANDADA", int usuario = 0) =>
            "{\"idOperacion\":\"web:" + clave + "\",\"fechaUtc\":\"2026-11-25T15:00:00.0000000Z\",\"sesion\":\"S1\",\"canal\":\"WEB\",\"tipo\":\"OPERACION_AUTOMATICA\"," +
            "\"consulta\":\"tengo una botella\",\"materiales\":[{\"id\":\"B01\",\"oracle\":\"PET\"}],\"canecas\":\"B\",\"contenedores\":[1093],\"reconocimiento\":\"TEXTO_DETERMINISTA\"," +
            "\"estadoApertura\":\"" + estado + "\",\"modo\":\"grupo\",\"tiempoProgramadoMs\":5000,\"error\":\"\",\"verificacionFisica\":\"SIN_SENSOR\",\"estadoRegistro\":\"PENDIENTE_ORACLE\"," +
            "\"estacion\":\"estacion_tres_canecas\",\"idUsuario\":" + usuario + "}";

        private static string Confirmacion(string clave) =>
            "{\"idOperacion\":\"web:" + clave + ":confirmacion\",\"operacionOrigen\":\"web:" + clave + "\",\"fechaUtc\":\"2026-11-25T15:00:09Z\",\"sesion\":\"S1\",\"canal\":\"WEB\"," +
            "\"tipo\":\"DISPOSICION_CONFIRMADA_CIUDADANO\",\"verificacionFisica\":\"SIN_SENSOR\",\"estadoRegistro\":\"PENDIENTE_ORACLE\",\"estacion\":\"estacion_tres_canecas\"}";

        private static string Carpeta()
        {
            string c = Path.Combine(Path.GetTempPath(), "ecoaccess-sinc-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(c, "Ciudadano"));
            return c;
        }

        [TestMethod]
        public void DesdeCiudadano_MapeaColumnasRealesDeV030()
        {
            var op = RegistroOperacionesConCola.DesdeCiudadano(Apertura("abcdefghijklmnop0001", usuario: 7))!;
            Assert.AreEqual("web:abcdefghijklmnop0001", op.Clave);
            Assert.AreEqual(CanalEstacion.WEB, op.Canal);
            Assert.AreEqual(TipoOperacionEstacion.RECONOCIMIENTO_AUTOMATICO, op.Tipo);
            Assert.AreEqual(7, op.IdUsuario);
            Assert.AreEqual("BLANCA", op.Canecas);
            Assert.AreEqual("APROVECHABLE", op.Categorias);
            Assert.AreEqual(5000, op.DuracionMs);
            Assert.AreEqual("SIMULTANEA", op.Modalidad);
            Assert.AreEqual(EstadoHardwareEstacion.PWM_CONFIRMADO, op.EstadoHardware);
            Assert.IsFalse(op.DisposicionVerificada, "sin sensor nunca se marca como verificada");
            StringAssert.Contains(op.Materiales, "B01 PET");

            var rechazada = RegistroOperacionesConCola.DesdeCiudadano(Apertura("abcdefghijklmnop0002", "RECHAZADA"))!;
            Assert.AreEqual(EstadoOperacionEstacion.ERROR, rechazada.Estado);
            Assert.AreEqual(0, rechazada.DuracionMs);
            Assert.AreEqual("NINGUNA", rechazada.Modalidad);
            Assert.IsNull(RegistroOperacionesConCola.DesdeCiudadano(Apertura("abcdefghijklmnop0003"))!.IdUsuario, "sin cuenta vinculada no se inventa un usuario");

            var conf = RegistroOperacionesConCola.DesdeCiudadano(Confirmacion("abcdefghijklmnop0001"))!;
            Assert.AreEqual("web:abcdefghijklmnop0001", conf.Clave, "la confirmación actualiza la fila de su apertura");
            Assert.AreEqual(EstadoOperacionEstacion.DISPOSICION_CONFIRMADA, conf.Estado);
            Assert.IsTrue(conf.DisposicionDeclarada);
            Assert.IsFalse(conf.DisposicionVerificada);
        }

        [TestMethod]
        public async Task Sincronizador_Idempotente_NoReescribeLaBitacora_YReintentaTrasCaida()
        {
            string carpeta = Carpeta();
            string bitacora = Path.Combine(carpeta, "Ciudadano", "operaciones-ciudadanas.jsonl");
            string original = Confirmacion("abcdefghijklmnop0001") + "\n" + Apertura("abcdefghijklmnop0001") + "\n{no es json\n";
            File.WriteAllText(bitacora, original);
            var oracle = new OracleFalso { Caido = true };
            var reg = new RegistroOperacionesConCola(oracle, carpeta, subirOperacionesWeb: true);

            Assert.AreEqual(0, await reg.SincronizarAsync(), "Oracle caído: nada se marca como sincronizado");
            Assert.AreEqual(2, reg.PendientesWeb);

            oracle.Caido = false;
            int primera = await reg.SincronizarAsync();
            // La confirmación venía antes que su apertura: espera al siguiente ciclo y nunca se aplica sobre una fila inexistente.
            Assert.AreEqual(1, primera);
            Assert.AreEqual(1, await reg.SincronizarAsync());
            Assert.AreEqual(0, await reg.SincronizarAsync(), "reprocesar no vuelve a escribir");
            Assert.AreEqual(0, reg.PendientesWeb);
            CollectionAssert.AreEqual(new[] { EstadoOperacionEstacion.REGISTRADA, EstadoOperacionEstacion.DISPOSICION_CONFIRMADA }, oracle.Escrituras.Select(e => e.Estado).ToArray());
            Assert.AreEqual(original, File.ReadAllText(bitacora), "la bitácora de la pasarela no se modifica");

            // Un reinicio (nuevo objeto) recuerda lo sincronizado.
            var tras = new RegistroOperacionesConCola(oracle, carpeta, subirOperacionesWeb: true);
            File.AppendAllText(bitacora, Apertura("abcdefghijklmnop0009") + "\n");
            Assert.AreEqual(1, await tras.SincronizarAsync());
            Assert.AreEqual(3, oracle.Escrituras.Count);
            try { Directory.Delete(carpeta, true); } catch (IOException) { }
        }

        [TestMethod]
        public void CuentasWeb_CodigoUnSoloUso_ConfirmadoSoloPorCuentaVinculada_YVence()
        {
            var ahora = new DateTime(2026, 11, 25, 15, 0, 0, DateTimeKind.Utc);
            string carpeta = Carpeta();
            string dir = Path.Combine(carpeta, "Ciudadano");
            var s = new ServicioCuentasWeb(null, dir, () => ahora);
            string codigo = s.NuevoCodigo()!;
            Assert.AreEqual(10, codigo.Length);
            Assert.AreEqual("pendiente", s.Canjear(codigo).Estado, "sin confirmar en Telegram no entrega token");
            Assert.IsTrue(s.Confirmar(codigo, 42));
            Assert.IsFalse(s.Confirmar(codigo, 99), "otra cuenta no puede quedarse con el código");
            var (token, estado) = s.Canjear(codigo);
            Assert.AreEqual("vinculado", estado);
            Assert.AreEqual(42, s.Usuario(token));
            Assert.IsNull(s.Canjear(codigo).Token, "el código no se reutiliza");
            Assert.AreEqual(42, new ServicioCuentasWeb(null, dir, () => ahora).Usuario(token), "el token sobrevive a un reinicio del bot");
            Assert.IsFalse(File.ReadAllText(Path.Combine(carpeta, "Ciudadano", "cuentas-web.json")).Contains(token!), "solo se guarda la huella del token");

            string viejo = s.NuevoCodigo()!;
            ahora = ahora.AddMinutes(11);
            Assert.IsFalse(s.Confirmar(viejo, 42), "el código vence a los 10 minutos");
            Assert.IsTrue(s.Desvincular(token));
            Assert.IsNull(s.Usuario(token));
            try { Directory.Delete(carpeta, true); } catch (IOException) { }
        }
    }
}
