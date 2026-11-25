using System;
using System.Linq;
using EcoAccess.P3.Logica.Vision35.Expo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica.Vision35
{
    /// <summary>Estadísticas de la estación (V031): invitados, detalle por residuo y filtros, sin datos ficticios.</summary>
    [TestClass]
    public sealed class EstadisticasEstacionTests
    {
        private static FilaEstadisticaEstacion F(string clave, string canal, string usuario, string? metodo, string? cod, string? nombre, string? caneca,
            string hw = "PWM_CONFIRMADO", string estado = "REGISTRADA", bool declarada = false, int hora = 10) =>
            new(clave, new DateTime(2026, 11, 25, hora, 0, 0), canal, usuario, metodo, "RECONOCIMIENTO_AUTOMATICO", cod, nombre, caneca, 1, caneca, 14000, hw, estado, declarada, false);

        [TestMethod]
        public void Calcular_CuentaOperacionesDistintas_InvitadosYResiduos()
        {
            var filas = new[]
            {
                F("web:1", "WEB", "INVITADO", "VOZ", "V31", "Limón", "VERDE"),
                F("web:1", "WEB", "INVITADO", "VOZ", "B30", "Papel de aluminio limpio", "BLANCA"),
                F("tg:2", "TELEGRAM", "REGISTRADO", "FOTO", "M01", "papa", "VERDE", declarada: true, hora: 11),
                F("tg:3", "TELEGRAM", "REGISTRADO", "FOTO", "M11", "servilleta usada", "NEGRA", hw: "RECHAZADO", estado: "ERROR", hora: 11),
            };
            var r = EstadisticasEstacion.Calcular(filas);
            Assert.AreEqual(3, r.Operaciones, "la operación web con dos residuos cuenta una vez");
            Assert.AreEqual(1, r.Invitados);
            Assert.AreEqual(2, r.Registrados);
            Assert.AreEqual(4, r.Residuos);
            Assert.AreEqual(2, r.AperturasEjecutadas);
            Assert.AreEqual(1, r.AperturasFallidas);
            Assert.AreEqual(1, r.DisposicionesDeclaradas);
            Assert.AreEqual(0, r.DisposicionesVerificadas, "sin sensores no hay verificadas");
            Assert.AreEqual(2, r.PorCaneca.Single(x => x.Clave == "VERDE").Cantidad);
            Assert.AreEqual(2, r.PorCanal.Single(x => x.Clave == "TELEGRAM").Cantidad);
            CollectionAssert.AreEqual(new[] { "10:00", "11:00" }, r.PorHora.Select(x => x.Clave).ToArray());
        }

        [TestMethod]
        public void Filtrar_PorCanecaCanalUsuarioYMaterial()
        {
            var filas = new[]
            {
                F("web:1", "WEB", "INVITADO", "TEXTO", "V31", "Limón", "VERDE"),
                F("web:1", "WEB", "INVITADO", "TEXTO", "B30", "Papel de aluminio limpio", "BLANCA"),
                F("tg:2", "TELEGRAM", "REGISTRADO", "FOTO", "M01", "papa", "VERDE"),
            };
            Assert.AreEqual(2, EstadisticasEstacion.Filtrar(filas, new FiltroEstadisticasEstacion(Caneca: "VERDE")).Count);
            Assert.AreEqual(2, EstadisticasEstacion.Filtrar(filas, new FiltroEstadisticasEstacion(Canal: "WEB")).Count);
            Assert.AreEqual(1, EstadisticasEstacion.Filtrar(filas, new FiltroEstadisticasEstacion(TipoUsuario: "REGISTRADO")).Count);
            Assert.AreEqual(1, EstadisticasEstacion.Filtrar(filas, new FiltroEstadisticasEstacion(Material: "Limón")).Count);
            Assert.AreEqual(0, EstadisticasEstacion.Calcular(Array.Empty<FilaEstadisticaEstacion>()).Operaciones);
        }

        [TestMethod]
        public void BitacoraWeb_InvitadoConDosResiduos_DetalleMetodoYSesionAnonima()
        {
            string linea = "{\"idOperacion\":\"web:abcdefghijklmnop0042\",\"fechaUtc\":\"2026-11-25T15:00:00Z\",\"sesion\":\"S1\",\"canal\":\"WEB\",\"tipo\":\"OPERACION_AUTOMATICA\"," +
                "\"consulta\":\"tengo un limon y papel aluminio\",\"materiales\":[{\"id\":\"V31\",\"oracle\":\"ORGANICO\",\"nombre\":\"Limón\",\"caneca\":\"verde\"},{\"id\":\"B30\",\"oracle\":\"LATA_ALUMINIO\",\"nombre\":\"Papel de aluminio limpio\",\"caneca\":\"blanca\"}]," +
                "\"canecas\":\"VB\",\"estadoApertura\":\"COMANDADA\",\"modo\":\"grupo\",\"tiempoProgramadoMs\":14000,\"idUsuario\":0,\"metodo\":\"VOZ\",\"invitado\":\"3f2a9c1e-0b7d-4e55-9a1c-2d6f7b8a9e10\"}";
            var op = RegistroOperacionesConCola.DesdeCiudadano(linea)!;
            Assert.AreEqual("INVITADO", op.TipoUsuario);
            Assert.AreEqual("VOZ", op.Metodo);
            Assert.AreEqual("3f2a9c1e-0b7d-4e55-9a1c-2d6f7b8a9e10", op.SesionAnonima);
            Assert.AreEqual(14000, op.DuracionMs);
            Assert.HasCount(2, op.Detalles!);
            Assert.AreEqual("VERDE", op.Detalles![0].Caneca);
            Assert.AreEqual("BLANCA", op.Detalles[1].Caneca);
            Assert.AreEqual("Papel de aluminio limpio", op.Detalles[1].Nombre);
            Assert.IsTrue(op.Detalles.All(d => d.Metodo == "VOZ"));
            var sinInvitado = RegistroOperacionesConCola.DesdeCiudadano(linea.Replace("3f2a9c1e-0b7d-4e55-9a1c-2d6f7b8a9e10", "<script>"))!;
            Assert.IsNull(sinInvitado.SesionAnonima, "un identificador con caracteres inválidos no se guarda");
        }
    }
}
