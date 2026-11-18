using System;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Selección de interfaces: el diseño es solo presentación y nunca concede permisos ni impide abrir EcoAccess.</summary>
    [TestClass]
    public sealed class CatalogoAparienciasTests
    {
        private static SesionActiva Sesion(int id, params CodigoRol[] roles) => new(id, "Persona de prueba", roles, DateTime.UtcNow);

        [TestMethod]
        public void ElPredeterminadoEsElModerno()
        {
            Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Predeterminado.Id);
            Assert.AreEqual(1, CatalogoApariencias.Todos.Count(d => d.EsPredeterminado));
        }

        [TestMethod]
        public void SinPreferenciaSeUsaElPredeterminado()
        {
            foreach (PerfilApariencia perfil in Enum.GetValues(typeof(PerfilApariencia)))
                Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Resolver(null, perfil).Id);
        }

        [TestMethod]
        public void UnaPreferenciaInexistenteODanadaUsaElPredeterminado()
        {
            Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Resolver("no-existe", PerfilApariencia.Ciudadano).Id);
            Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Resolver("   ", PerfilApariencia.Ciudadano).Id);
        }

        [TestMethod]
        public void UnaPreferenciaValidaSeRespeta()
        {
            Assert.AreEqual(CatalogoApariencias.Clasico, CatalogoApariencias.Resolver("CLASICO", PerfilApariencia.Ciudadano).Id);
        }

        [TestMethod]
        public void ElTactilNuncaSeAbreComoPanelPersonal()
        {
            Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Resolver(CatalogoApariencias.Tactil, PerfilApariencia.Supervisor).Id);
        }

        [TestMethod]
        public void SoloElSupervisorVeElDisenoTactil()
        {
            Assert.IsTrue(CatalogoApariencias.DisponiblesPara(PerfilApariencia.Supervisor).Any(d => d.EsModoEstacion));
            foreach (var perfil in new[] { PerfilApariencia.Invitado, PerfilApariencia.Ciudadano, PerfilApariencia.Operario })
                Assert.IsFalse(CatalogoApariencias.DisponiblesPara(perfil).Any(d => d.EsModoEstacion), perfil.ToString());
        }

        [TestMethod]
        public void ElPerfilSaleDelAreaAutorizadaYNoDelDiseno()
        {
            Assert.AreEqual(PerfilApariencia.Invitado, CatalogoApariencias.PerfilDe(null, AreaAcceso.Supervisor), "Sin sesión no hay supervisor.");
            Assert.AreEqual(PerfilApariencia.Ciudadano, CatalogoApariencias.PerfilDe(Sesion(5, CodigoRol.Ciudadano), AreaAcceso.Ciudadano));
            Assert.AreEqual(PerfilApariencia.Supervisor, CatalogoApariencias.PerfilDe(Sesion(6, CodigoRol.Administrador), AreaAcceso.Supervisor));
        }

        [TestMethod]
        public void SoloUnSupervisorPuedeConvertirElEquipoEnEstacion()
        {
            var preferencias = new PreferenciasApariencia();
            Assert.IsFalse(preferencias.CambiarModoEstacion(PerfilApariencia.Ciudadano, true));
            Assert.IsFalse(preferencias.CambiarModoEstacion(PerfilApariencia.Operario, true));
            Assert.IsFalse(preferencias.EsEstacion);
            Assert.IsTrue(preferencias.CambiarModoEstacion(PerfilApariencia.Supervisor, true));
            Assert.IsTrue(preferencias.EsEstacion);
        }

        [TestMethod]
        public void LaPreferenciaSeConservaAlGuardarYLeer()
        {
            var preferencias = new PreferenciasApariencia();
            preferencias.Elegir(Sesion(42, CodigoRol.Ciudadano), CatalogoApariencias.Clasico);
            preferencias.Elegir(null, CatalogoApariencias.Moderno);
            preferencias.CambiarModoEstacion(PerfilApariencia.Supervisor, true);

            PreferenciasApariencia leidas = PreferenciasApariencia.Leer(preferencias.Escribir());
            Assert.AreEqual(CatalogoApariencias.Clasico, leidas.DeUsuario(42));
            Assert.AreEqual(CatalogoApariencias.Moderno, leidas.Invitado);
            Assert.AreEqual(CatalogoApariencias.Moderno, leidas.Equipo);
            Assert.IsTrue(leidas.EsEstacion);
        }

        [TestMethod]
        public void CadaCuentaTieneSuPropiaPreferencia()
        {
            var preferencias = new PreferenciasApariencia();
            preferencias.Elegir(Sesion(1, CodigoRol.Ciudadano), CatalogoApariencias.Clasico);
            Assert.AreEqual(CatalogoApariencias.Clasico, preferencias.Para(Sesion(1, CodigoRol.Ciudadano)));
            Assert.IsNull(preferencias.Para(Sesion(2, CodigoRol.Ciudadano)), "Otra cuenta sigue con el predeterminado.");
        }

        [TestMethod]
        public void RestablecerVuelveAlPredeterminado()
        {
            var preferencias = new PreferenciasApariencia();
            SesionActiva sesion = Sesion(7, CodigoRol.Ciudadano);
            preferencias.Elegir(sesion, CatalogoApariencias.Clasico);
            preferencias.Restablecer(sesion);
            Assert.IsNull(preferencias.Para(sesion));
            Assert.IsNull(preferencias.Equipo);
            Assert.AreEqual(CatalogoApariencias.Moderno, CatalogoApariencias.Resolver(preferencias.Para(sesion), PerfilApariencia.Ciudadano).Id);
        }

        [TestMethod]
        public void UnArchivoDanadoNoImpideIniciar()
        {
            PreferenciasApariencia leidas = PreferenciasApariencia.Leer("basura\n=sin clave\nequipo=<script>\nusuario.abc=moderno\nusuario.-3=clasico\nestacion=quizas\n\u0000\u0001");
            Assert.IsNull(leidas.Equipo, "Un valor con caracteres no permitidos se ignora.");
            Assert.AreEqual(0, leidas.PorUsuario.Count);
            Assert.IsFalse(leidas.EsEstacion);
            Assert.IsNotNull(PreferenciasApariencia.Leer(null));
        }

        [TestMethod]
        public void NoSePuedeElegirUnIdentificadorInvalido()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new PreferenciasApariencia().Elegir(null, "../otro"));
        }
    }
}
