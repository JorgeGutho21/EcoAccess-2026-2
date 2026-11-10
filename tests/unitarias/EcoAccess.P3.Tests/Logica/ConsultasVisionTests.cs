using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Cada foto atendida deja una consulta (no un depósito): en la fase de software era el único rastro posible y no existía.
    /// </summary>
    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class ConsultasVisionEnMemoria : IRepositorioConsultasVision
        {
            public List<ConsultaVisionRegistrada> Guardadas { get; } = new();
            public bool Fallar { get; set; }

            public Task GuardarAsync(ConsultaVisionRegistrada consulta, CancellationToken ct = default)
            {
                if (Fallar) throw new InvalidOperationException("Oracle no disponible (simulado).");
                if (Guardadas.All(c => c.Clave != consulta.Clave)) Guardadas.Add(consulta);
                return Task.CompletedTask;
            }

            public Task<ResumenConsultasVision> ResumirDeUsuarioAsync(int idUsuario, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            {
                var propias = Guardadas.Where(c => c.IdUsuario == idUsuario && c.FechaUtc >= desdeUtc && c.FechaUtc < hastaUtc).ToList();
                return Task.FromResult(new ResumenConsultasVision(propias.Count,
                    propias.Count(c => c.Resultado is ResultadoClasificacion.Identificado or ResultadoClasificacion.ObjetoReconocido),
                    propias.Count(c => c.Resultado is ResultadoClasificacion.Incierta or ResultadoClasificacion.ImagenBorrosa or ResultadoClasificacion.VariosObjetos)));
            }

            public Task<ResumenPeriodoConsultasVision?> ResumirPeriodoAsync(DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
            {
                var periodo = Guardadas.Where(c => c.FechaUtc >= desdeUtc && c.FechaUtc < hastaUtc).ToList();
                return Task.FromResult<ResumenPeriodoConsultasVision?>(new(periodo.Count,
                    periodo.Count(c => c.Resultado is ResultadoClasificacion.Identificado or ResultadoClasificacion.ObjetoReconocido),
                    periodo.Count(c => c.Resultado is ResultadoClasificacion.Incierta or ResultadoClasificacion.ImagenBorrosa or ResultadoClasificacion.VariosObjetos),
                    periodo.Where(c => c.IdUsuario is not null).Select(c => c.IdUsuario).Distinct().Count(),
                    periodo.Count == 0 ? 0 : (int)Math.Round(periodo.Average(c => c.LatenciaMs))));
            }
        }

        private ServicioConversacionTelegram ModoSoftwareConConsultas(ConsultasVisionEnMemoria consultas)
        {
            var servicio = ConVision(TipoPropuestaVision.MaterialIdentificado, (_, _) => Task.FromResult(new ImagenResiduo(FotoDelDoble, "image/png")), flujoRapido: true);
            servicio.SinAperturaFisica = true;
            servicio.ConsultasVision = consultas;
            return servicio;
        }

        [TestMethod]
        public async Task ModoSoftware_LaFotoQuedaAnotadaComoConsultaSinCrearSolicitudNiDeposito()
        {
            var consultas = new ConsultasVisionEnMemoria();
            var servicio = ModoSoftwareConConsultas(consultas);

            var respuesta = Unica(await EnviarFotoAsync(servicio, update: 9_001));

            StringAssert.Contains(respuesta.Texto, MensajesTelegram.AperturaPendiente);
            var c = consultas.Guardadas.Single();
            Assert.AreEqual("telegram:9001", c.Clave);
            Assert.AreEqual(Ciudadano, c.IdUsuario, "La consulta es de la persona vinculada.");
            Assert.AreEqual(ModoConsultaVision.Software, c.Modo);
            Assert.AreEqual(_pet.Id, c.IdMaterial);
            Assert.IsGreaterThanOrEqualTo(0, c.LatenciaMs);
            Assert.AreEqual(DateTimeKind.Utc, c.FechaUtc.Kind);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsEmpty(_interacciones.Depositos);
        }

        [TestMethod]
        public async Task LaMismaActualizacionRepetidaNoCuentaDosConsultas()
        {
            var consultas = new ConsultasVisionEnMemoria();
            var servicio = ModoSoftwareConConsultas(consultas);
            await EnviarFotoAsync(servicio, update: 9_002);
            await EnviarFotoAsync(servicio, update: 9_002);
            Assert.HasCount(1, consultas.Guardadas);
        }

        [TestMethod]
        public async Task SiOracleFallaAlAnotarLaPersonaRecibeIgualSuDestino()
        {
            var consultas = new ConsultasVisionEnMemoria { Fallar = true };
            var servicio = ModoSoftwareConConsultas(consultas);
            var avisos = new List<string>();
            servicio.RegistrarReconocimiento = avisos.Add;

            var respuesta = Unica(await EnviarFotoAsync(servicio, update: 9_003));

            StringAssert.Contains(respuesta.Texto, MensajesTelegram.AperturaPendiente);
            Assert.IsTrue(avisos.Any(a => a.Contains("no se pudo anotar la consulta")), "El fallo queda en el registro del bot.");
        }

        [TestMethod]
        public async Task MiActividadMuestraLasFotosConsultadasAparteDeLosDepositos()
        {
            var consultas = new ConsultasVisionEnMemoria();
            var estadisticas = new RepositorioEstadisticasEnMemoria();
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj,
                estadisticas: new ServicioEstadisticas(_usuarios, estadisticas)) { ConsultasVision = consultas };
            consultas.Guardadas.Add(new ConsultaVisionRegistrada("telegram:1", Ciudadano, CanalConsultaVision.Telegram, ModoConsultaVision.Software,
                "prueba", "plastic_bottle", ContextoObjeto.ResiduoAprovechable, ResultadoClasificacion.Identificado, _pet.Id, AdmisionMaterial.Admitido, null, 3000,
                _reloj.GetUtcNow().UtcDateTime.AddDays(-1)));
            consultas.Guardadas.Add(new ConsultaVisionRegistrada("telegram:2", Ciudadano, CanalConsultaVision.Telegram, ModoConsultaVision.Software,
                "prueba", null, null, ResultadoClasificacion.Incierta, null, null, null, 3000, _reloj.GetUtcNow().UtcDateTime.AddDays(-2)));
            consultas.Guardadas.Add(new ConsultaVisionRegistrada("telegram:3", Ciudadano + 1, CanalConsultaVision.Telegram, ModoConsultaVision.Software,
                "prueba", null, null, ResultadoClasificacion.Incierta, null, null, null, 3000, _reloj.GetUtcNow().UtcDateTime.AddDays(-2)));

            var r = Unica(await servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, "/estadisticas", null)));

            StringAssert.Contains(r.Texto, "Fotos consultadas a Echo: 2 (con destino: 1; pidió otra foto: 1)");
            StringAssert.Contains(r.Texto, "Depósitos:", "Los depósitos se siguen informando aparte.");
        }
    }
}
