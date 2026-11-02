using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography;
using System.Text.Json;
using EcoAccess.P3.Bot.Vision;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using SkiaSharp;

namespace EcoAccess.P3.Tests.Bot
{

    [TestClass]
    public sealed class EvaluacionVisionArchivosTests
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.I1)]
        private static extern bool CreateSymbolicLink(string enlace, string destino, int opciones);

        private readonly string _raiz = Path.Combine(Path.GetTempPath(), "ecoaccess-evaluacion-" + Guid.NewGuid().ToString("N"));
        private readonly ClasificadorMaterialSimulado _clasificador = new();
        private static readonly Material[] Catalogo = new Material[] { new Material(1, "PET", "Plástico PET", CategoriaMaterial.Aprovechable, null, true) };
        private ManifiestoVision _documento = null!;

        private async Task<string> PrepararAsync(int cantidad = 1)
        {
            Directory.CreateDirectory(_raiz);
            var muestras = new List<MuestraArchivoVision>();
            for (int i = 0; i < cantidad; i++)
            {
                byte[] imagen = VisionSeguraFixture.Imagen(16 + i, 10, SKEncodedImageFormat.Png);
                string archivo = $"muestra-{i}.png";
                await Compat.WriteAllBytesAsync(Path.Combine(_raiz, archivo), imagen);
                muestras.Add(new MuestraArchivoVision(archivo, Compat.ToHexString(Compat.Sha256(imagen)), "PET"));
                _clasificador.Preparar(imagen, new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, "PET", 0.99m));
            }
            _documento = new ManifiestoVision("Conjunto SINTÉTICO de pruebas, no mide calidad visual", "Imágenes sintéticas generadas en la prueba", muestras.ToArray());
            return await GuardarAsync();
        }
        private async Task<string> GuardarAsync()
        {
            string p = Path.Combine(_raiz, "manifiesto.json"); await Compat.WriteAllBytesAsync(p, JsonSerializer.SerializeToUtf8Bytes(_documento)); return p;
        }
        private Task<InformeVisionArchivos> EjecutarAsync(string p, int maximo = 10, IClasificadorMaterial? clasificador = null) =>
            EvaluacionVisionArchivos.EjecutarAsync(p, maximo, clasificador ?? _clasificador, Catalogo, new RepositorioContenedoresEnMemoria(), TimeProvider.System);
        [TestCleanup]
        public void Limpiar()
        {
            if (Directory.Exists(_raiz)) Directory.Delete(_raiz, true); // Solo ficheros sintéticos creados por esta prueba.
        }

        [TestMethod]
        public async Task InformeConservaHuellasModeloYConjuntoSinPublicarRutasDeImagenes()
        {
            string p = await PrepararAsync(2);
            var informe = await EjecutarAsync(p, 2);
            Assert.AreEqual("simulador", informe.Proveedor);
            Assert.AreEqual(2, informe.Metricas.Total);
            Assert.AreEqual(2, _clasificador.Llamadas);
            Assert.AreEqual(0, informe.FallosProveedor);
            Assert.AreEqual(64, informe.CatalogoSha256.Length);
            Assert.AreEqual("PET", informe.Muestras[0].Propuesta!.CodigoMaterial);
            Assert.IsNull(informe.Muestras[0].Incidencia);
            Assert.IsGreaterThanOrEqualTo(0L, informe.Muestras[0].DuracionMilisegundos);
            string json = JsonSerializer.Serialize(informe);
            Assert.DoesNotContain(_raiz, json);
            Assert.DoesNotContain("muestra-0.png", json);
            // No se presenta esta aritmética con un doble como precisión de un modelo real.
        }

        [TestMethod]
        public async Task CuotaAgotadaTrasPrimeraMuestraImpideDeclararEvaluacionCompleta()
        {
            string p = await PrepararAsync(2); var clasificador = new PrimeraLuegoCuota();
            await Assert.ThrowsExactlyAsync<CuotaVisionAgotadaException>(() => EjecutarAsync(p, 2, clasificador));
            Assert.AreEqual(2, clasificador.Llamadas, "No hay espera ni reintento para saltar la cuota.");
            Assert.AreEqual(1, EvaluadorClasificacion.Evaluar(new MuestraEvaluacion[] { new MuestraEvaluacion("PET", nameof(ResultadoClasificacion.CuotaGratuitaAgotada)) }).Abstenciones);
        }

        private sealed class PrimeraLuegoCuota : IClasificadorMaterial
        {
            public string Proveedor => "cuota-ficticia";
            public int Llamadas { get; private set; }
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            {
                Llamadas++;
                return Llamadas == 1 ? Task.FromResult(new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, "PET", 0.99m)) :
                    Task.FromException<PropuestaVision>(new CuotaVisionAgotadaException());
            }
        }

        [TestMethod]
        public async Task CuotaAgotadaNoEsEtiquetaDelContenidoDeUnaFotografia()
        {
            await PrepararAsync(); _documento.Muestras[0] = _documento.Muestras[0] with { Esperada = nameof(ResultadoClasificacion.CuotaGratuitaAgotada) };
            string p = await GuardarAsync(); await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        public async Task PresupuestoMenorQueCorpusNoLlamaAlProveedor()
        {
            string p = await PrepararAsync(2);
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p, 1));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        public async Task HashIncorrectoEnUltimaMuestraEvitaCobrarLasAnteriores()
        {
            await PrepararAsync(2);
            _documento.Muestras[1] = _documento.Muestras[1] with { Sha256 = new string('0', 64) };
            string p = await GuardarAsync();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        [DataRow("../privada.png")]
        [DataRow("/tmp/privada.png")]
        [DataRow("sub/../../privada.png")]
        public async Task CorpusNoPuedeReferenciarArchivosFueraDeSuDirectorio(string archivo)
        {
            await PrepararAsync(); _documento.Muestras[0] = _documento.Muestras[0] with { Archivo = archivo };
            string p = await GuardarAsync();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        public async Task EnlaceSimbolicoSeRechazaAntesDeClasificar()
        {
            await PrepararAsync();
            if (!CreateSymbolicLink(Path.Combine(_raiz, "enlace.png"), Path.Combine(_raiz, _documento.Muestras[0].Archivo), 2))
                Assert.Inconclusive("Windows no concede permiso para crear enlaces simbólicos en esta sesión.");
            _documento.Muestras[0] = _documento.Muestras[0] with { Archivo = "enlace.png" };
            string p = await GuardarAsync();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        public async Task SeExigeLicenciaDeclaradaYMuestrasNoRepetidas()
        {
            await PrepararAsync();
            var original = _documento;
            _documento = original with { Licencia = "" };
            string sinLicencia = await GuardarAsync();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(sinLicencia));
            _documento = original with { Muestras = new MuestraArchivoVision[] { original.Muestras[0], original.Muestras[0] } };
            string p = await GuardarAsync();
            await Assert.ThrowsExactlyAsync<ArgumentException>(() => EjecutarAsync(p));
            Assert.AreEqual(0, _clasificador.Llamadas);
        }

        [TestMethod]
        public async Task FallosDeProveedorQuedanSeparadosDeLasEtiquetasReconocidas()
        {
            string p = await PrepararAsync();
            var informe = await EjecutarAsync(p, clasificador: new ClasificadorCaido());
            Assert.AreEqual(1, informe.FallosProveedor);
            Assert.AreEqual(1, informe.Metricas.Abstenciones);
            Assert.AreEqual(nameof(ResultadoClasificacion.ProveedorNoDisponible), informe.Muestras.Single().Obtenida);
            Assert.AreEqual("fallo_o_tiempo_agotado", informe.Muestras.Single().Incidencia);
        }
        private sealed class ClasificadorCaido : IClasificadorMaterial
        {
            public string Proveedor => "doble sin red";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken = default) =>
                throw new HttpRequestException("Fallo externo simulado.");
        }

        [TestMethod]
        public async Task EvaluacionRespetaElTiempoDelProveedorConfigurado()
        {
            string p = await PrepararAsync();
            var informe = await EvaluacionVisionArchivos.EjecutarAsync(p, 1, new ClasificadorLento(), Catalogo,
                new RepositorioContenedoresEnMemoria(), TimeProvider.System, tiempoMaximo: TimeSpan.FromMilliseconds(20));
            Assert.AreEqual(1, informe.FallosProveedor);
            Assert.AreEqual(nameof(ResultadoClasificacion.ProveedorNoDisponible), informe.Muestras.Single().Obtenida);
        }

        private sealed class ClasificadorLento : IClasificadorMaterial
        {
            public string Proveedor => "doble lento sin red";
            public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150), ct);
                return new(TipoPropuestaVision.MaterialIdentificado, "PET", 0.99m);
            }
        }

        [TestMethod]
        public async Task InformeNoCopiaTextoLibreNiCodigosDesconocidosDelProveedor()
        {
            string p = await PrepararAsync();
            var informe = await EjecutarAsync(p, clasificador: new ClasificadorConTexto());
            var muestra = informe.Muestras.Single();
            Assert.AreEqual("codigo_fuera_catalogo", muestra.Incidencia);
            Assert.IsNull(muestra.Propuesta!.CodigoMaterial);
            Assert.IsNull(muestra.Propuesta.Explicacion);
            Assert.DoesNotContain("dato-no-publicar", JsonSerializer.Serialize(informe));
        }

        private sealed class ClasificadorConTexto : IClasificadorMaterial
        {
            public string Proveedor => "doble sin red";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default) =>
                Task.FromResult(new PropuestaVision(TipoPropuestaVision.MaterialIdentificado, "dato-no-publicar@ejemplo.com", 0.99m, "dato-no-publicar"));
        }
    }
}
