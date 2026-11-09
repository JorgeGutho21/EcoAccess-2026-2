using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Estación con tres canecas (fase VIII). Son pruebas de lógica y de SIMULACIÓN: no mueven ningún servomotor real y
    /// no prueban hardware físico.
    /// </summary>
    [TestClass]
    public sealed class ServicioTresCanecasTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private static readonly DateTimeOffset Ahora = new(2026, 11, 9, 12, 0, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioPuntosEnMemoria _puntos = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly ConfiguracionesEnMemoria _configuraciones;
        private readonly ServicioTresCanecas _servicio;
        private readonly PuntoInstalacion _estacion;
        private readonly Material _pet, _carton, _cascaras, _servilleta, _pila;
        private readonly Contenedor _blanca, _verde, _negra;

        public ServicioTresCanecasTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _contenedores.Puntos = _puntos;
            _puntos.Contenedores = _contenedores;
            _estacion = _puntos.Sembrar("Estación Bloque A");
            _pet = _materiales.Sembrar("PET", "Botella PET", CategoriaMaterial.Aprovechable);
            _carton = _materiales.Sembrar("CARTON", "Cartón", CategoriaMaterial.Aprovechable);
            _cascaras = _materiales.Sembrar("CASCARAS", "Cáscaras de fruta", CategoriaMaterial.OrganicoAprovechable);
            _servilleta = _materiales.Sembrar("SERVILLETA", "Servilleta usada", CategoriaMaterial.NoAprovechable);
            _pila = _materiales.Sembrar("PILA", "Pila", CategoriaMaterial.Peligroso);
            _blanca = _contenedores.Sembrar(_estacion.Id, EstadoContenedor.EnServicio, _pet.Id, _carton.Id);
            _verde = _contenedores.Sembrar(_estacion.Id, EstadoContenedor.EnServicio, _cascaras.Id);
            _negra = _contenedores.Sembrar(_estacion.Id, EstadoContenedor.EnServicio, _servilleta.Id);
            _configuraciones = new ConfiguracionesEnMemoria(_contenedores);
            _servicio = new ServicioTresCanecas(_usuarios, _contenedores, _materiales, _configuraciones, new RelojFijo(Ahora));
        }

        private async Task ConfigurarLasTresAsync()
        {
            Assert.IsTrue((await _servicio.ConfigurarAsync(Admin, _blanca.Id, CategoriaMaterial.Aprovechable, "eco-blanca-01", "izquierda")).Exitoso);
            Assert.IsTrue((await _servicio.ConfigurarAsync(Admin, _verde.Id, CategoriaMaterial.OrganicoAprovechable, "eco-verde-01", "centro")).Exitoso);
            Assert.IsTrue((await _servicio.ConfigurarAsync(Admin, _negra.Id, CategoriaMaterial.NoAprovechable, "eco-negra-01", "derecha")).Exitoso);
        }

        // Selección de la caneca correcta

        [TestMethod]
        [DataRow("PET", ColorCaneca.Blanca, "eco-blanca-01")]
        [DataRow("CARTON", ColorCaneca.Blanca, "eco-blanca-01")]
        [DataRow("CASCARAS", ColorCaneca.Verde, "eco-verde-01")]
        [DataRow("SERVILLETA", ColorCaneca.Negra, "eco-negra-01")]
        public async Task ResolverDestino_CadaMaterialVaASuCanecaYSuDispositivo(string codigo, ColorCaneca color, string dispositivo)
        {
            await ConfigurarLasTresAsync();
            Material material = new[] { _pet, _carton, _cascaras, _servilleta }.Single(m => m.Codigo == codigo);

            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, material.Id);

            Assert.IsTrue(destino.Exitoso, destino.Mensaje);
            Assert.AreEqual(color, destino.Valor.Color);
            Assert.AreEqual(dispositivo, destino.Valor.IdentificadorDispositivo);
            int esperado = color switch { ColorCaneca.Blanca => _blanca.Id, ColorCaneca.Verde => _verde.Id, _ => _negra.Id };
            Assert.AreEqual(esperado, destino.Valor.IdContenedor, "La orden se crea para el identificador de la caneca, no por color.");
        }

        [TestMethod]
        public async Task ResolverDestino_Peligroso_NoVaANingunaCanecaNiALaNegra()
        {
            await ConfigurarLasTresAsync();
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, _pila.Id);
            Assert.IsFalse(destino.Exitoso);
            StringAssert.Contains(destino.Mensaje, "posconsumo");
        }

        [TestMethod]
        public async Task ResolverDestino_MaterialQueLaCanecaNoAdmite_Rechaza()
        {
            await ConfigurarLasTresAsync();
            Material vidrio = _materiales.Sembrar("VIDRIO", "Botella de vidrio", CategoriaMaterial.Aprovechable);
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, vidrio.Id);
            Assert.IsFalse(destino.Exitoso);
            StringAssert.Contains(destino.Mensaje, "no recibe");
        }

        [TestMethod]
        public async Task ResolverDestino_CanecaFueraDeServicio_Rechaza()
        {
            await ConfigurarLasTresAsync();
            await _contenedores.CambiarEstadoAsync(_verde.Id, EstadoContenedor.FueraDeServicio);
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, _cascaras.Id);
            Assert.IsFalse(destino.Exitoso);
            StringAssert.Contains(destino.Mensaje, "fuera de servicio");
        }

        [TestMethod]
        public async Task ResolverDestino_EstacionSinEseColor_Rechaza()
        {
            Assert.IsTrue((await _servicio.ConfigurarAsync(Admin, _blanca.Id, CategoriaMaterial.Aprovechable, null, null)).Exitoso);
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, _servilleta.Id);
            Assert.IsFalse(destino.Exitoso);
            StringAssert.Contains(destino.Mensaje, "negra");
        }

        [TestMethod]
        public async Task ResolverDestino_DosCanecasDelMismoColor_RechazaPorAmbiguedad()
        {
            await ConfigurarLasTresAsync();
            Contenedor otraBlanca = _contenedores.Sembrar(_estacion.Id, EstadoContenedor.EnServicio, _pet.Id);
            // Una configuración heredada inconsistente (escrita sin pasar por el servicio) no debe abrir ninguna.
            await _configuraciones.GuardarAsync(new ConfiguracionCaneca(otraBlanca.Id, CategoriaMaterial.Aprovechable, null, null, Ahora.UtcDateTime));
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, _pet.Id);
            Assert.IsFalse(destino.Exitoso);
            StringAssert.Contains(destino.Mensaje, "más de una");
        }

        [TestMethod]
        public async Task ResolverDestino_SinDispositivo_DevuelveDestinoQueSoloPuedeSimularse()
        {
            Assert.IsTrue((await _servicio.ConfigurarAsync(Admin, _blanca.Id, CategoriaMaterial.Aprovechable, null, null)).Exitoso);
            Resultado<DestinoCaneca> destino = await _servicio.ResolverDestinoAsync(_estacion.Id, _pet.Id);
            Assert.IsTrue(destino.Exitoso);
            Assert.IsFalse(destino.Valor.PuedeAbrirFisicamente);
        }

        // Configuración

        [TestMethod]
        public async Task Configurar_CanecaQueAdmiteMaterialDeOtraCategoria_Rechaza()
        {
            Resultado r = await _servicio.ConfigurarAsync(Admin, _blanca.Id, CategoriaMaterial.OrganicoAprovechable, null, null);
            Assert.IsFalse(r.Exitoso);
            StringAssert.Contains(r.Mensaje, "Botella PET");
        }

        [TestMethod]
        public async Task Configurar_SegundaCanecaDelMismoColorEnLaEstacion_Rechaza()
        {
            await ConfigurarLasTresAsync();
            Contenedor otra = _contenedores.Sembrar(_estacion.Id, EstadoContenedor.FueraDeServicio);
            Resultado r = await _servicio.ConfigurarAsync(Admin, otra.Id, CategoriaMaterial.NoAprovechable, null, null);
            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
        }

        [TestMethod]
        public async Task Configurar_DispositivoYaVinculado_Rechaza()
        {
            await ConfigurarLasTresAsync();
            Contenedor otra = _contenedores.Sembrar(_puntos.Sembrar("Estación Bloque B").Id, EstadoContenedor.FueraDeServicio);
            Resultado r = await _servicio.ConfigurarAsync(Admin, otra.Id, CategoriaMaterial.Aprovechable, "eco-blanca-01", null);
            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
        }

        [TestMethod]
        public async Task Configurar_PeligrosoODispositivoInvalido_Rechaza()
        {
            Assert.IsFalse((await _servicio.ConfigurarAsync(Admin, _negra.Id, CategoriaMaterial.Peligroso, null, null)).Exitoso);
            Assert.IsFalse((await _servicio.ConfigurarAsync(Admin, _negra.Id, CategoriaMaterial.NoAprovechable, "con espacio", null)).Exitoso);
        }

        [TestMethod]
        public async Task Configurar_Operario_NoTienePermiso()
        {
            Resultado r = await _servicio.ConfigurarAsync(Operario, _blanca.Id, CategoriaMaterial.Aprovechable, null, null);
            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task Renombrar_ValidaLongitudYGuardaNombreLimpio()
        {
            Assert.IsFalse((await _servicio.RenombrarAsync(Admin, _blanca.Id, " x ")).Exitoso);
            Assert.IsTrue((await _servicio.RenombrarAsync(Admin, _blanca.Id, "  Caneca blanca Bloque A  ")).Exitoso);
            Assert.AreEqual("Caneca blanca Bloque A", _configuraciones.Nombres[_blanca.Id]);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.RenombrarAsync(Operario, _blanca.Id, "Otra")).Motivo);
        }

        [TestMethod]
        public void ColorPara_DeduceElColorDeLaCategoria()
        {
            Assert.AreEqual(ColorCaneca.Blanca, ConfiguracionCaneca.ColorPara(CategoriaMaterial.Aprovechable));
            Assert.AreEqual(ColorCaneca.Verde, ConfiguracionCaneca.ColorPara(CategoriaMaterial.OrganicoAprovechable));
            Assert.AreEqual(ColorCaneca.Negra, ConfiguracionCaneca.ColorPara(CategoriaMaterial.NoAprovechable));
            Assert.IsNull(ConfiguracionCaneca.ColorPara(CategoriaMaterial.Peligroso));
        }

        // Simulación de las tres tapas: cada caneca simulada solo acepta órdenes con SU identificador (misma regla que el firmware).

        [TestMethod]
        public async Task Simulacion_OrdenParaLaBlanca_NoLaAceptanLaVerdeNiLaNegra()
        {
            var reloj = new RelojFijo(Ahora);
            var eventos = new List<EventoRecibido>();
            Task<Resultado<EventoRegistrado>> Registrar(EventoRecibido e, CancellationToken _) { eventos.Add(e); return Task.FromResult(Resultado<EventoRegistrado>.Exito(new EventoRegistrado(eventos.Count, default, false))); }
            var simuladas = new Dictionary<int, CanecaSimulada>
            {
                [_blanca.Id] = new CanecaSimulada(_blanca.Id, Registrar, reloj),
                [_verde.Id] = new CanecaSimulada(_verde.Id, Registrar, reloj),
                [_negra.Id] = new CanecaSimulada(_negra.Id, Registrar, reloj),
            };
            var orden = new ComandoApertura(501, _blanca.Id, "orden-501-blanca", Ahora.UtcDateTime.AddMinutes(2));

            Assert.IsFalse(await simuladas[_verde.Id].EnviarAperturaAsync(orden), "La verde no debe aceptar una orden de la blanca.");
            Assert.IsFalse(await simuladas[_negra.Id].EnviarAperturaAsync(orden), "La negra no debe aceptar una orden de la blanca.");
            Assert.IsTrue(await simuladas[_blanca.Id].EnviarAperturaAsync(orden));

            Assert.AreEqual(0, (await simuladas[_verde.Id].EjecutarAsync(501)).Count);
            Assert.AreEqual(0, (await simuladas[_negra.Id].EjecutarAsync(501)).Count);
            Assert.IsTrue((await simuladas[_blanca.Id].EjecutarAsync(501)).Count > 0, "La blanca simulada sí ejecuta su orden.");
            Assert.IsTrue(eventos.All(e => e.IdContenedor == _blanca.Id), "Todos los eventos pertenecen a la caneca blanca.");
        }

        [TestMethod]
        public async Task Simulacion_OrdenVencidaORepetida_NoAbreDeNuevo()
        {
            var reloj = new RelojFijo(Ahora);
            int eventos = 0;
            Task<Resultado<EventoRegistrado>> Registrar(EventoRecibido e, CancellationToken _) { eventos++; return Task.FromResult(Resultado<EventoRegistrado>.Exito(new EventoRegistrado(eventos, default, false))); }
            var negra = new CanecaSimulada(_negra.Id, Registrar, reloj);

            Assert.IsFalse(await negra.EnviarAperturaAsync(new ComandoApertura(601, _negra.Id, "orden-601", Ahora.UtcDateTime.AddSeconds(-1))), "Orden vencida rechazada.");

            Assert.IsTrue(await negra.EnviarAperturaAsync(new ComandoApertura(602, _negra.Id, "orden-602", Ahora.UtcDateTime.AddMinutes(2))));
            int primera = (await negra.EjecutarAsync(602)).Count;
            int repetida = (await negra.EjecutarAsync(602)).Count;
            Assert.IsTrue(primera > 0);
            Assert.AreEqual(0, repetida, "La misma clave de idempotencia no abre dos veces.");
        }

        /// <summary>Doble en memoria de CONFIGURACION_CANECA con la misma restricción de dispositivo único.</summary>
        private sealed class ConfiguracionesEnMemoria : IRepositorioConfiguracionCanecas
        {
            private readonly RepositorioContenedoresEnMemoria _contenedores;
            private readonly Dictionary<int, ConfiguracionCaneca> _filas = new();
            public Dictionary<int, string> Nombres { get; } = new();

            public ConfiguracionesEnMemoria(RepositorioContenedoresEnMemoria contenedores) => _contenedores = contenedores;

            public Task<ConfiguracionCaneca?> ObtenerAsync(int idContenedor, CancellationToken cancellationToken = default) =>
                Task.FromResult(_filas.TryGetValue(idContenedor, out var c) ? c : null);

            public async Task<IReadOnlyList<ConfiguracionCaneca>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default)
            {
                var resultado = new List<ConfiguracionCaneca>();
                foreach (ConfiguracionCaneca c in _filas.Values)
                    if ((await _contenedores.ObtenerPorIdAsync(c.IdContenedor))?.IdPuntoInstalacion == idPunto) resultado.Add(c);
                return resultado;
            }

            public Task GuardarAsync(ConfiguracionCaneca configuracion, CancellationToken cancellationToken = default)
            {
                if (configuracion.IdentificadorDispositivo is not null && _filas.Values.Any(c =>
                        c.IdContenedor != configuracion.IdContenedor && c.IdentificadorDispositivo == configuracion.IdentificadorDispositivo))
                    throw new ConflictoUnicidadException("uq_configuracion_dispositivo");
                _filas[configuracion.IdContenedor] = configuracion;
                return Task.CompletedTask;
            }

            public Task RenombrarAsync(int idContenedor, string nombre, CancellationToken cancellationToken = default)
            {
                Nombres[idContenedor] = nombre;
                return Task.CompletedTask;
            }
        }
    }
}
