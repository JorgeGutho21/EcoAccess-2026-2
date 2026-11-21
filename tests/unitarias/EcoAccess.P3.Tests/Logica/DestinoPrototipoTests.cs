using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Depósito simplificado: cada residuo del prototipo va a su caneca de la estación (blanca, verde o negra) y se registra
    /// con un material que esa caneca admite. Si no se puede, se rechaza sin cambiar de caneca.
    /// </summary>
    [TestClass]
    public sealed class DestinoPrototipoTests
    {
        private sealed class Estacion : IRepositorioEstaciones
        {
            public IReadOnlyList<CanecaEstacion> Canecas { get; set; } = Array.Empty<CanecaEstacion>();
            public Task<IReadOnlyList<CanecaEstacion>> ListarEstacionDeAsync(int idContenedor, CancellationToken ct = default) => Task.FromResult(Canecas);
        }

        private RepositorioMaterialesEnMemoria _materiales = null!;
        private RepositorioContenedoresEnMemoria _contenedores = null!;
        private Estacion _estacion = null!;
        private Contenedor _blanca = null!, _verde = null!, _negra = null!;

        [TestInitialize]
        public void Preparar()
        {
            _materiales = new RepositorioMaterialesEnMemoria();
            _contenedores = new RepositorioContenedoresEnMemoria();
            Material pet = _materiales.Sembrar("PET", "Botella plástica PET", CategoriaMaterial.Aprovechable);
            Material multicapa = _materiales.Sembrar("MULTICAPA", "Envase multicapa", CategoriaMaterial.Aprovechable);
            Material organico = _materiales.Sembrar("ORGANICO", "Restos de comida", CategoriaMaterial.OrganicoAprovechable);
            Material servilleta = _materiales.Sembrar("SERVILLETA_USADA", "Servilleta o papel higiénico usado", CategoriaMaterial.NoAprovechable);
            Material icopor = _materiales.Sembrar("ICOPOR_CONTAMINADO", "Icopor con restos de comida", CategoriaMaterial.NoAprovechable);
            _blanca = _contenedores.SembrarPiloto(EstadoContenedor.EnServicio, pet.Id, multicapa.Id);
            _verde = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, organico.Id);
            _negra = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, servilleta.Id, icopor.Id);
            _estacion = new Estacion
            {
                Canecas = new[]
                {
                    new CanecaEstacion(_blanca.Id, "Blanca", CategoriaMaterial.Aprovechable, true, "esp32-piloto-blanca", "izquierda"),
                    new CanecaEstacion(_verde.Id, "Verde", CategoriaMaterial.OrganicoAprovechable, true, "esp32-piloto-verde", "centro"),
                    new CanecaEstacion(_negra.Id, "Negra", CategoriaMaterial.NoAprovechable, true, "esp32-piloto-negra", "derecha"),
                },
            };
        }

        private ServicioCatalogoAmbiental Servicio(bool conEstacion = true) => new(_materiales, _contenedores, conEstacion ? _estacion : null);

        [TestMethod]
        [DataRow("cascara-banano", "verde", "ORGANICO")]
        [DataRow("botella-agua", "blanca", "PET")]
        [DataRow("caja-jugo", "blanca", "MULTICAPA")]
        [DataRow("servilletas-usadas", "negra", "SERVILLETA_USADA")]
        [DataRow("plato-icopor", "negra", "ICOPOR_CONTAMINADO")]
        public async Task CadaResiduoVaSoloASuCanecaConSuMaterial(string id, string caneca, string codigo)
        {
            var r = await Servicio().ResolverPrototipoAsync(CatalogoPrototipo.Buscar(id)!);

            Assert.IsTrue(r.Exitoso, r.Mensaje);
            int esperada = caneca switch { "verde" => _verde.Id, "negra" => _negra.Id, _ => _blanca.Id };
            Assert.AreEqual(esperada, r.Valor.Caneca.Id);
            Assert.AreEqual(codigo, r.Valor.Material.Codigo);
        }

        [TestMethod]
        public async Task CanecaFueraDeServicio_SeRechazaSinCambiarDeCaneca()
        {
            _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio); // otra caneca cualquiera no cambia nada
            var verdeApagada = _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, _materiales.Sembrar("RESTOS", "Restos", CategoriaMaterial.OrganicoAprovechable).Id);
            _estacion.Canecas = new[] { _estacion.Canecas[0], new CanecaEstacion(verdeApagada.Id, "Verde", CategoriaMaterial.OrganicoAprovechable, false, null, null), _estacion.Canecas[2] };

            var r = await Servicio().ResolverPrototipoAsync(CatalogoPrototipo.Buscar("cascara-naranja")!);

            Assert.IsFalse(r.Exitoso);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            StringAssert.Contains(r.Mensaje, "fuera de servicio");
        }

        [TestMethod]
        public async Task MaterialNoAdmitidoPorLaCaneca_NoSeDeposita()
        {
            // La negra de esta estación no admite «envase plástico sin identificar» (vaso contaminado).
            var r = await Servicio().ResolverPrototipoAsync(CatalogoPrototipo.Buscar("vaso-contaminado")!);

            Assert.IsFalse(r.Exitoso);
            StringAssert.Contains(r.Mensaje, "no admite");
        }

        [TestMethod]
        public async Task SinConfiguracionDeEstacion_SoloLaBlancaPilotoRecibe()
        {
            Assert.IsTrue((await Servicio(conEstacion: false).ResolverPrototipoAsync(CatalogoPrototipo.Buscar("botella-agua")!)).Exitoso);
            var verde = await Servicio(conEstacion: false).ResolverPrototipoAsync(CatalogoPrototipo.Buscar("cascara-banano")!);
            Assert.IsFalse(verde.Exitoso);
            Assert.AreEqual(MotivoRechazo.NoEncontrado, verde.Motivo);
        }

        [TestMethod]
        [DoNotParallelize]
        public void LaSincronizacionSeEspaciaTrasCadaFalloSinPasarDeDosMinutos()
        {
            PoliticaSincronizacion.Configurar("15");
            Assert.AreEqual(15, PoliticaSincronizacion.EsperaTrasFallos(0).TotalSeconds);
            Assert.AreEqual(30, PoliticaSincronizacion.EsperaTrasFallos(1).TotalSeconds);
            Assert.AreEqual(60, PoliticaSincronizacion.EsperaTrasFallos(2).TotalSeconds);
            Assert.AreEqual(120, PoliticaSincronizacion.EsperaTrasFallos(3).TotalSeconds);
            Assert.AreEqual(120, PoliticaSincronizacion.EsperaTrasFallos(10).TotalSeconds);
        }

        [TestMethod]
        [DoNotParallelize]
        public void ElIntervaloSeConfiguraDentroDeLimitesRazonables()
        {
            try
            {
                Assert.AreEqual(20, PoliticaSincronizacion.Configurar("20").TotalSeconds);
                Assert.AreEqual(10, PoliticaSincronizacion.Configurar("1").TotalSeconds, "Menos de 10 s satura Oracle.");
                Assert.AreEqual(120, PoliticaSincronizacion.Configurar("9999").TotalSeconds);
                Assert.AreEqual(120, PoliticaSincronizacion.Configurar("abc").TotalSeconds, "Un valor inválido conserva el anterior.");
                Assert.AreEqual(120, PoliticaSincronizacion.Configurar(null).TotalSeconds);
            }
            finally { PoliticaSincronizacion.Configurar(PoliticaSincronizacion.SegundosPorDefecto.ToString()); }
        }

        [TestMethod]
        [DoNotParallelize]
        public void LosDatosViejosSeMarcanComoDesactualizados()
        {
            PoliticaSincronizacion.Configurar("15");
            var ahora = new DateTime(2026, 11, 20, 23, 0, 0, DateTimeKind.Utc);
            Assert.IsTrue(PoliticaSincronizacion.EstaDesactualizado(DateTime.MinValue, ahora), "Nunca leídos.");
            Assert.IsFalse(PoliticaSincronizacion.EstaDesactualizado(ahora.AddSeconds(-20), ahora));
            Assert.IsTrue(PoliticaSincronizacion.EstaDesactualizado(ahora.AddSeconds(-40), ahora));
        }
    }
}
