using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    public sealed partial class BridgeOracleTests
    {
        private int _interaccionAnalisis;
        private sealed class VisionFallida : IClasificadorMaterial
        {
            public string Proveedor => "doble:sin-red";
            public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken = default) =>
                Task.FromException<PropuestaVision>(new IOException("Fallo sintético, sin API"));
        }

        [TestMethod]
        public async Task RecorridoErrorIa_ConsentimientoTrazaOracleSeleccionManualYDepositoUnico()
        {
            var e = await PrepararAsync(); await VincularTelegramAsync(e);
            var usuarios = new RepositorioUsuariosOracle(Fabrica); var materiales = new RepositorioMaterialesOracle(Fabrica);
            var canecas = new RepositorioContenedoresOracle(Fabrica); var trazas = new RepositorioAnalisisImagenOracle(Fabrica);
            int descargas = 0;
            var bot = new ServicioConversacionTelegram(new RepositorioConversacionesTelegramOracle(Fabrica), canecas, materiales,
                new ServicioVinculacionTelegram(usuarios, new RepositorioVinculosTelegramOracle(Fabrica), new GeneradorTokensAleatorio(), TimeProvider.System),
                e.Servicio, new LimitadorUsoEnMemoria(100, TimeSpan.FromMinutes(1)), null, TimeProvider.System,
                vision: new ServicioClasificacionImagen(new VisionFallida(), materiales, new ServicioCatalogoAmbiental(materiales, canecas)),
                descargarFoto: (_, _) => { descargas++; return Task.FromResult(new ImagenResiduo(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a, 0, 0, 0, 0 }, "image/png")); },
                requiereConsentimientoFoto: true, analisis: trazas);
            Hijo bridge = Iniciar(e, "--bridge"), sim = Iniciar(e, "--dispositivo-simulado");
            await ListoAsync(bridge, e.Config.Eventos); await ListoAsync(sim, e.Config.Comandos);
            await EnviarAsync(bot, "/start " + (await canecas.ObtenerPorIdAsync(e.Config.IdContenedor))!.IdentificadorQr);
            _interaccionAnalisis = (await new RepositorioConversacionesTelegramOracle(Fabrica).ObtenerAsync(_telegram))!.IdInteraccion!.Value;
            long updateFoto = NuevoUpdate();
            var aviso = await bot.ProcesarAsync(new(updateFoto, _telegram, _telegram, true, null, null, "foto-ficticia-no-descargada-de-telegram"));
            Assert.AreEqual(0, descargas);
            string nonce = aviso.SelectMany(r => r.Botones).Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoConsentirFoto, StringComparison.Ordinal)).Dato;
            var resultado = await EnviarAsync(bot, boton: nonce);
            Assert.AreEqual(1, descargas);
            Assert.IsFalse(resultado.SelectMany(r => r.Botones).Any(b => b.Dato == ServicioConversacionTelegram.ConfirmarSi));
            var traza = await trazas.ObtenerUltimoAsync(_interaccionAnalisis);
            Assert.IsNotNull(traza);
            Assert.AreEqual((int)ResultadoClasificacion.ProveedorNoDisponible, traza.Estado);
            Assert.AreEqual("doble", traza.Proveedor); Assert.AreEqual("sin-red", traza.Modelo);
            Assert.IsNull(traza.IdMaterialSugerido); Assert.IsNull(traza.IdMaterialConfirmado);
            await EnviarAsync(bot, "/manual");
            await EnviarAsync(bot, boton: ServicioConversacionTelegram.PrefijoMaterial + e.Material);
            await Task.WhenAll(EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi), EnviarAsync(bot, boton: ServicioConversacionTelegram.ConfirmarSi));
            int orden = (await OrdenesDeAsync(e.Config.IdContenedor)).Single();
            await EsperarAsync(async () => (await e.Repo.ObtenerOrdenAsync(orden))?.Estado == EstadoOrdenApertura.Cerrada, bridge, sim);
            _deposito = (await e.Repo.ObtenerDepositoPorOrdenAsync(orden))!.Id;
            traza = await trazas.ObtenerUltimoAsync(_interaccionAnalisis);
            Assert.AreEqual(e.Material, traza!.IdMaterialConfirmado);
            Assert.AreEqual(1, await CantidadDepositosAsync(orden));
            Assert.AreEqual(1, await ContarDeAsync("SELECT COUNT(*) FROM analisis_imagen WHERE id_interaccion=:id", _interaccionAnalisis));
            RegistrarEvidencia("fallo-ia-traza", $"interaccion={_interaccionAnalisis} estado=ProveedorNoDisponible proveedor=doble modelo=sin-red descargas=1 material_manual={e.Material} orden={orden} deposito={_deposito} trazas=1");
        }

        [TestMethod]
        public async Task TrazaOracle_OrdenaVersionNumericaYNoReescribeConfirmacion()
        {
            var e = await PrepararAsync();
            _interaccionAnalisis = (await e.Servicio.IniciarAsync(e.Ciudadano, e.Config.IdContenedor, CanalInteraccion.Telegram)).Valor;
            var repo = new RepositorioAnalisisImagenOracle(Fabrica); var fecha = DateTime.UtcNow;
            await repo.GuardarAsync(new($"telegram:{_interaccionAnalisis}:foto:9", _interaccionAnalisis, "doble", "prueba", "no-publicada", 7, null, fecha));
            await repo.GuardarAsync(new($"telegram:{_interaccionAnalisis}:foto:11", _interaccionAnalisis, "doble", "prueba", "no-publicada", 7, null, fecha));
            Assert.AreEqual($"telegram:{_interaccionAnalisis}:foto:11", (await repo.ObtenerUltimoAsync(_interaccionAnalisis))!.Clave);
            await repo.ConfirmarAsync(_interaccionAnalisis, e.Material, fecha.AddSeconds(1));
            await repo.ConfirmarAsync(_interaccionAnalisis, e.Material, fecha.AddSeconds(2));
            var ultima = (await repo.ObtenerUltimoAsync(_interaccionAnalisis))!;
            Assert.IsLessThan(1.0, Math.Abs((ultima.FechaConfirmacionUtc!.Value - fecha.AddSeconds(1)).TotalMilliseconds), "Oracle TIMESTAMP(6) redondea fracciones submicrosegundo; debe conservar la primera confirmación, no la segunda un segundo después.");
            Assert.AreEqual(e.Material, ultima.IdMaterialConfirmado);
            using var c = await Fabrica.AbrirAsync(CancellationToken.None);
            using var cmd = ComandosOracle.Crear(c, "SELECT COUNT(*) FROM analisis_imagen WHERE id_interaccion=:id AND id_material_confirmado IS NOT NULL");
            cmd.Agregar("id", OracleDbType.Int32, _interaccionAnalisis);
            Assert.AreEqual(1, Convert.ToInt32(await cmd.ExecuteScalarAsync()));
        }

        private async Task LimpiarAnalisisPropioAsync(OracleConnection c)
        {
            if (_interaccionAnalisis <= 0) return;
            using var cmd = ComandosOracle.Crear(c, "DELETE FROM analisis_imagen WHERE id_interaccion=:id");
            cmd.Agregar("id", OracleDbType.Int32, _interaccionAnalisis); await cmd.ExecuteNonQueryAsync();
        }
    }
}
