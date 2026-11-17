using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>Fase 2: tres canecas (Resolución 2184 de 2019) detrás de un solo QR oficial.</summary>
    [TestClass]
    public sealed class ResolutorDestinoTests
    {
        internal static readonly IReadOnlyList<CanecaEstacion> Estacion = new[]
        {
            new CanecaEstacion(1093, "EcoAccess · Caneca piloto", CategoriaMaterial.Aprovechable, true, "esp32-piloto-blanca", "izquierda"),
            new CanecaEstacion(2001, "EcoAccess · Caneca verde", CategoriaMaterial.OrganicoAprovechable, false, "esp32-piloto-verde", "centro"),
            new CanecaEstacion(2002, "EcoAccess · Caneca negra", CategoriaMaterial.NoAprovechable, false, "esp32-piloto-negra", "derecha"),
        };

        [TestMethod]
        [DataRow(CategoriaMaterial.Aprovechable, DestinoResiduo.Blanca, 1093)]
        [DataRow(CategoriaMaterial.OrganicoAprovechable, DestinoResiduo.Verde, 2001)]
        [DataRow(CategoriaMaterial.NoAprovechable, DestinoResiduo.Negra, 2002)]
        public void CadaCategoriaVaASuCanecaDeColor(CategoriaMaterial categoria, DestinoResiduo esperado, int caneca)
        {
            var d = ResolutorDestino.Resolver(ResolutorDestino.PorCategoria(categoria), Estacion);
            Assert.AreEqual(esperado, d.Destino);
            Assert.AreEqual(caneca, d.Caneca!.IdContenedor);
        }

        [TestMethod]
        public void PeligrosoVaAPosconsumoYNuncaAUnaCaneca()
        {
            var d = ResolutorDestino.Resolver(ResolutorDestino.PorCategoria(CategoriaMaterial.Peligroso), Estacion);
            Assert.AreEqual(DestinoResiduo.Posconsumo, d.Destino);
            Assert.IsNull(d.Caneca);
            StringAssert.Contains(ResolutorDestino.Texto(d), "posconsumo");
        }

        [TestMethod]
        [DataRow(ContextoObjeto.ResiduoAprovechable, DestinoResiduo.Blanca)]
        [DataRow(ContextoObjeto.ResiduoOrganico, DestinoResiduo.Verde)]
        [DataRow(ContextoObjeto.ResiduoNoAprovechable, DestinoResiduo.Negra)]
        [DataRow(ContextoObjeto.ResiduoSanitario, DestinoResiduo.Negra)]
        [DataRow(ContextoObjeto.ResiduoElectronico, DestinoResiduo.Posconsumo)]
        [DataRow(ContextoObjeto.ResiduoEspecial, DestinoResiduo.Posconsumo)]
        [DataRow(ContextoObjeto.Persona, DestinoResiduo.NoEsResiduo)]
        [DataRow(ContextoObjeto.AnimalVivo, DestinoResiduo.NoEsResiduo)]
        [DataRow(ContextoObjeto.Desconocido, DestinoResiduo.Indeterminado)]
        public void ElContextoDelObjetoDecideElDestino(ContextoObjeto contexto, DestinoResiduo esperado) =>
            Assert.AreEqual(esperado, ResolutorDestino.PorContexto(contexto));

        [TestMethod]
        public void SinCanecaDeEseColorNoSeInventaUna()
        {
            var soloBlanca = Estacion.Take(1).ToList();
            Assert.IsNull(ResolutorDestino.Resolver(DestinoResiduo.Verde, soloBlanca).Caneca);
        }

        [TestMethod]
        public void ConDosDelMismoColorGanaLaQueEstaEnServicio()
        {
            var dos = new[]
            {
                new CanecaEstacion(5, "A", CategoriaMaterial.NoAprovechable, false, null, null),
                new CanecaEstacion(9, "B", CategoriaMaterial.NoAprovechable, true, null, null),
            };
            Assert.AreEqual(9, ResolutorDestino.Resolver(DestinoResiduo.Negra, dos).Caneca!.IdContenedor);
        }

        [TestMethod]
        public void ElTextoDiceColorUbicacionYSiEstaFueraDeServicioSinIdentificadoresInternos()
        {
            string texto = ResolutorDestino.Texto(ResolutorDestino.Resolver(DestinoResiduo.Verde, Estacion));
            StringAssert.Contains(texto, "VERDE");
            StringAssert.Contains(texto, "centro");
            StringAssert.Contains(texto, "fuera de servicio");
            Assert.DoesNotContain("esp32", texto, "El ESP32 no se muestra a la persona.");
            Assert.DoesNotContain("2001", texto);
        }
    }

    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class EstacionesEnMemoria : IRepositorioEstaciones
        {
            public IReadOnlyList<CanecaEstacion> Canecas { get; set; } = Array.Empty<CanecaEstacion>();
            public bool Fallar { get; set; }

            public Task<IReadOnlyList<CanecaEstacion>> ListarEstacionDeAsync(int idContenedor, CancellationToken ct = default) =>
                Fallar ? throw new InvalidOperationException("Oracle no disponible (simulado).") : Task.FromResult(Canecas);
        }

        [TestMethod]
        public async Task Estacion_LosRestosDeComidaIndicanLaCanecaVerdeYNoAbrenNada()
        {
            var eco = Eco(() => Visto("food_scraps"));
            eco.Estaciones = new EstacionesEnMemoria { Canecas = ResolutorDestinoTests.Estacion };

            RespuestaBot r = Unica(await EnviarFotoAsync(eco));

            StringAssert.StartsWith(r.Texto, "🍃 Identifiqué restos de comida.");
            StringAssert.Contains(r.Texto, "caneca VERDE");
            Assert.DoesNotContain("todavía no dispone", r.Texto, "Con caneca verde en la estación ese aviso ya no es cierto.");
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task Estacion_SinConfiguracionOConFalloDeOracleLaRespuestaQuedaComoAntes()
        {
            RespuestaBot sinEstacion = Unica(await EnviarFotoAsync(Eco(() => Visto("food_scraps"))));
            var vacia = Eco(() => Visto("food_scraps"));
            vacia.Estaciones = new EstacionesEnMemoria();
            var fallida = Eco(() => Visto("food_scraps"));
            fallida.Estaciones = new EstacionesEnMemoria { Fallar = true, Canecas = ResolutorDestinoTests.Estacion };

            Assert.AreEqual(sinEstacion.Texto, Unica(await EnviarFotoAsync(vacia)).Texto);
            Assert.AreEqual(sinEstacion.Texto, Unica(await EnviarFotoAsync(fallida)).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
    }
}
