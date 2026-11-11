using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Fases 5 y 6: pesos estimados versionados y matriz de ecopuntos propuesta.</summary>
    [TestClass]
    public sealed class PesosYEcopuntosTests
    {
        private static Material M(string codigo, CategoriaMaterial categoria = CategoriaMaterial.Aprovechable) =>
            new(1, codigo, "Material " + codigo, categoria, null, true);

        [TestMethod]
        public void ElCatalogoDePesosTieneVersionYValoresPositivosConTresDecimales()
        {
            StringAssert.Matches(CatalogoPesosEstimados.Version, new System.Text.RegularExpressions.Regex(@"^\d{4}\.\d{2}\.\d{2}-\d+$"));
            Assert.IsNotEmpty(CatalogoPesosEstimados.Todos);
            foreach (var p in CatalogoPesosEstimados.Todos)
            {
                Assert.IsGreaterThan(0m, p.KgPorUnidad, p.CodigoMaterial);
                Assert.AreEqual(decimal.Round(p.KgPorUnidad, 3), p.KgPorUnidad, p.CodigoMaterial);
                Assert.IsFalse(string.IsNullOrWhiteSpace(p.UnidadTipica));
            }
        }

        [TestMethod]
        [DataRow("PET", null, "0.025")]
        [DataRow("PET", 4, "0.100")]
        [DataRow("LATA_ALUMINIO", 3, "0.045")]
        [DataRow("VIDRIO", 2, "0.500")]
        public void EstimaPorUnidadDeclarada(string codigo, int? unidades, string esperado) =>
            Assert.AreEqual(decimal.Parse(esperado, System.Globalization.CultureInfo.InvariantCulture), CatalogoPesosEstimados.Estimar(codigo, unidades));

        [TestMethod]
        [DataRow("DESCONOCIDO", 1)]
        [DataRow(null, 1)]
        [DataRow("PET", 0)]
        [DataRow("PET", 51)]
        public void SinReferenciaOConCantidadIncreibleNoInventaPeso(string? codigo, int unidades) =>
            Assert.IsNull(CatalogoPesosEstimados.Estimar(codigo, unidades));

        [TestMethod]
        [DataRow("LATA_ALUMINIO", 5)]
        [DataRow("PET", 4)]
        [DataRow("CARTON", 3)]
        [DataRow("VIDRIO", 3)]
        [DataRow("METAL", 3)]
        [DataRow("PAPEL", 2)]
        [DataRow("MULTICAPA", 2)]
        public void LaMatrizPropuestaDaLosPuntosPorUnidadDeLaMision(string codigo, int puntos) =>
            Assert.AreEqual(puntos, MatrizEcopuntosPropuesta.PorUnidad(M(codigo)));

        [TestMethod]
        public void FlexiblesIcoporOrganicosSanitariosYPeligrososDanCero()
        {
            foreach (string codigo in MatrizEcopuntosPropuesta.SinPuntos)
                Assert.AreEqual(0, MatrizEcopuntosPropuesta.PorUnidad(M(codigo)), codigo);
            Assert.AreEqual(0, MatrizEcopuntosPropuesta.PorUnidad(M("PET", CategoriaMaterial.Peligroso)), "La categoría manda sobre el código.");
            Assert.AreEqual(0, MatrizEcopuntosPropuesta.PorUnidad(M("ORGANICO", CategoriaMaterial.OrganicoAprovechable)));
            Assert.IsFalse(MatrizEcopuntosPropuesta.ConPuntos.Any(p => MatrizEcopuntosPropuesta.SinPuntos.Contains(p.Codigo)));
        }

        [TestMethod]
        public void PotencialesMultiplicanUnidadesConElMismoTope()
        {
            Assert.AreEqual(20, MatrizEcopuntosPropuesta.Potenciales(M("PET"), 5));
            Assert.AreEqual(5, MatrizEcopuntosPropuesta.Potenciales(M("LATA_ALUMINIO"), null));
            Assert.AreEqual(0, MatrizEcopuntosPropuesta.Potenciales(M("PET"), 51));
        }

        [TestMethod]
        public void EstadoDeEcopuntos_ProvisionalConfirmadoYAnulado()
        {
            var fecha = new DateTime(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);
            var sinVerificar = new Deposito(1, 10, 1, 7, null, null, null, null, 0.025m, fecha, sinSensor: true);
            var verificado = new Deposito(2, 11, 2, 7, 7, 99, fecha.AddHours(1), null, 0.025m, fecha);

            Assert.AreEqual(EstadoEcopuntos.Provisional, MatrizEcopuntosPropuesta.Estado(sinVerificar, null));
            Assert.AreEqual(EstadoEcopuntos.Confirmado, MatrizEcopuntosPropuesta.Estado(verificado, 4));
            Assert.AreEqual(EstadoEcopuntos.Anulado, MatrizEcopuntosPropuesta.Estado(verificado, 0));
            Assert.AreEqual(EstadoRegistroDeposito.Verificada, verificado.EstadoRegistro);
        }
    }
}
