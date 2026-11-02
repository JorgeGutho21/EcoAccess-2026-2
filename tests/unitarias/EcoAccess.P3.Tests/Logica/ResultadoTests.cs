using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Logica;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ResultadoTests
    {
        [TestMethod]
        public void Exito_SinValor_EsExitosoYSinMotivo()
        {
            Resultado resultado = Resultado.Exito();

            Assert.IsTrue(resultado.Exitoso);
            Assert.IsNull(resultado.Motivo);
            Assert.IsNull(resultado.Mensaje);
        }

        [TestMethod]
        public void Rechazo_ConMotivo_NoEsExitosoYConservaMensaje()
        {
            Resultado resultado = Resultado.Rechazo(MotivoRechazo.SinPermiso, "No puede.");

            Assert.IsFalse(resultado.Exitoso);
            Assert.AreEqual(MotivoRechazo.SinPermiso, resultado.Motivo);
            Assert.AreEqual("No puede.", resultado.Mensaje);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        public void Rechazo_MensajeVacio_LanzaArgumentException(string mensaje)
        {
            Assert.ThrowsExactly<ArgumentException>(() => Resultado.Rechazo(MotivoRechazo.Duplicado, mensaje));
        }

        [TestMethod]
        public void Valor_ResultadoExitoso_DevuelveValor()
        {
            Assert.AreEqual(7, Resultado<int>.Exito(7).Valor);
        }

        [TestMethod]
        public void Valor_ResultadoRechazado_LanzaInvalidOperationException()
        {
            Resultado<int> resultado = Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, "No existe.");

            Assert.ThrowsExactly<InvalidOperationException>(() => _ = resultado.Valor);
        }

        [TestMethod]
        public void DesdeRechazo_RechazoSinValor_ConservaMotivoYMensaje()
        {
            Resultado<int> resultado = Resultado<int>.DesdeRechazo(Resultado.Rechazo(MotivoRechazo.ActorInactivo, "Inactiva."));

            Assert.AreEqual(MotivoRechazo.ActorInactivo, resultado.Motivo);
            Assert.AreEqual("Inactiva.", resultado.Mensaje);
        }

        [TestMethod]
        public void DesdeRechazo_ResultadoExitoso_LanzaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(() => Resultado<int>.DesdeRechazo(Resultado.Exito()));
        }
    }
}
