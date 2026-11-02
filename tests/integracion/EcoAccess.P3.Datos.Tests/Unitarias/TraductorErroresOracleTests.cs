using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;

namespace EcoAccess.P3.Datos.Tests.Unitarias
{

    [TestClass]
    public sealed class TraductorErroresOracleTests
    {
        [TestMethod]
        [DataRow("ORA-00001: unique constraint (ECOACCESS_P3.UQ_MATERIAL_CODIGO) violated", "uq_material_codigo")]
        [DataRow("ORA-00001: restricción única (ECOACCESS_P3.UQ_CONTENEDOR_QR) violada", "uq_contenedor_qr")]
        [DataRow("ORA-00001: unique constraint (UQ_USUARIO_CORREO) violated on table ECOACCESS_P3.USUARIO", "uq_usuario_correo")]
        public void ExtraerNombreRestriccion_MensajeOracle_DevuelveNombreEnMinusculas(string mensaje, string esperado)
        {
            Assert.AreEqual(esperado, TraductorErroresOracle.ExtraerNombreRestriccion(mensaje));
        }

        [TestMethod]
        [DataRow("ORA-02291: integrity constraint (ECOACCESS_P3.FK_X) violated - parent key not found")]
        [DataRow("mensaje sin formato")]
        public void ExtraerNombreRestriccion_OtroMensaje_DevuelveNull(string mensaje)
        {
            Assert.IsNull(TraductorErroresOracle.ExtraerNombreRestriccion(mensaje));
        }

        // ORA-00054 es lo que devuelve Oracle 26ai al vencer FOR UPDATE WAIT; se descubrió al ejecutar la integración real.
        [TestMethod]
        [DataRow(54)]
        [DataRow(30006)]
        [DataRow(60)]
        public void EsErrorDeEspera_CodigoDeBloqueo_DevuelveTrue(int numero)
        {
            Assert.IsTrue(TraductorErroresOracle.EsErrorDeEspera(numero));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2291)]
        [DataRow(1013)]
        public void EsErrorDeEspera_OtroCodigo_DevuelveFalse(int numero)
        {
            Assert.IsFalse(TraductorErroresOracle.EsErrorDeEspera(numero));
        }
    }
}
