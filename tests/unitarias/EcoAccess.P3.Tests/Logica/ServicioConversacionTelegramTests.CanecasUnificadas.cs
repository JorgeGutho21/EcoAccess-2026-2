using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    public sealed partial class ServicioConversacionTelegramTests
    {
        private static readonly IReadOnlyList<CanecaEstacion> EstacionTresCanecas = new[]
        {
            new CanecaEstacion(1093, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(1400, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
            new CanecaEstacion(1401, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
        };

        private CoordinadorEstacionUnificado CrearCoordinadorDemo(params long[] chatsAutorizados)
        {
            var sim = new SimuladorEstacionTresCanecas(
                new Dictionary<int, DestinoResiduo>
                {
                    [1093] = DestinoResiduo.Blanca,
                    [1400] = DestinoResiduo.Verde,
                    [1401] = DestinoResiduo.Negra,
                },
                _reloj,
                TimeSpan.Zero);
            return new CoordinadorEstacionUnificado(sim, EstacionTresCanecas, ejecutorFisico: null, chatsTelegramAutorizados: chatsAutorizados, reloj: _reloj);
        }

        [TestMethod]
        public async Task ComandoMiId_DevuelveSoloElIdPropio_EIndicaSiEstaAutorizado()
        {
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var r = Unica(await EnviarConAsync(servicio, texto: "/miid"));
            StringAssert.Contains(r.Texto, "Tu ID numérico de Telegram es:");
            StringAssert.Contains(r.Texto, "supervisor");
            servicio.CoordinadorEstacion = CrearCoordinadorDemo(Vinculado);
            var autorizado = Unica(await EnviarConAsync(servicio, texto: "/miid"));
            StringAssert.Contains(autorizado.Texto, "ya está autorizado");
        }

        [TestMethod]
        public async Task ComandosCanecasYModo_InformanEstadoHonestoYSoportanLas7CombinacionesParaChatAutorizado()
        {
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var coord = CrearCoordinadorDemo(Vinculado);
            servicio.CoordinadorEstacion = coord;

            var rModo = Unica(await EnviarConAsync(servicio, texto: "/modo"));
            StringAssert.Contains(rModo.Texto, "MODO DEMO");

            var rCanecas = Unica(await EnviarConAsync(servicio, texto: "/canecas"));
            StringAssert.Contains(rCanecas.Texto, "/abrir verde blanca negra");

            string[] comandos =
            {
                "/verde",
                "/blanca",
                "/negra",
                "/abrir verde blanca",
                "/abrir verde negra",
                "/abrir blanca negra",
                "/abrir verde blanca negra",
            };

            foreach (string cmd in comandos)
            {
                var resp = Unica(await EnviarConAsync(servicio, texto: cmd));
                StringAssert.Contains(resp.Texto, "MODO DEMO", $"Comando {cmd}");
                StringAssert.Contains(resp.Texto, "Secuencia completada", $"Comando {cmd}");
            }

            Assert.HasCount(7, coord.Historial);
            Assert.IsTrue(coord.Historial.All(h => h.Autorizada && h.Exitosa && h.Origen == OrigenSolicitudEstacion.Telegram));

            var rHist = Unica(await EnviarConAsync(servicio, texto: "/historial"));
            StringAssert.Contains(rHist.Texto, "Historial reciente de la estación");
        }

        [TestMethod]
        public async Task ComandosCanecas_RechazanChatNoAutorizadoYFormatoInvalido()
        {
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var coord = CrearCoordinadorDemo(99999999L);
            servicio.CoordinadorEstacion = coord;

            var rDenegado = Unica(await EnviarConAsync(servicio, texto: "/abrir verde blanca"));
            StringAssert.Contains(rDenegado.Texto, "denegada");
            Assert.HasCount(1, coord.Historial);
            Assert.IsFalse(coord.Historial[0].Autorizada);

            var rInvalido = Unica(await EnviarConAsync(servicio, texto: "/abrir amarilla"));
            StringAssert.Contains(rInvalido.Texto, "Formato no reconocido");
        }
    }
}
