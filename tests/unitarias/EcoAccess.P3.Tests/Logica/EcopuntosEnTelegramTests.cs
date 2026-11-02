using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private sealed class ConsultaPuntos : IRepositorioEcopuntos
        {
            private readonly IReadOnlyList<ReglaEcopuntos>? _reglas;

            public ConsultaPuntos(IReadOnlyList<ReglaEcopuntos>? reglas = null)
            {
                _reglas = reglas;
            }

            public int? Consultado;
            public Task<ResumenEcopuntos> ConsultarAsync(int usuario, CancellationToken ct = default)
            {
                Consultado = usuario;
                return Task.FromResult(new ResumenEcopuntos(5, new MovimientoEcopuntos[]
    {
        new MovimientoEcopuntos(1,1,2,usuario,4,
                    TipoMovimientoEcopuntos.Credito,5,null,null,9,Inicio.UtcDateTime,"Prueba ficticia")
    }, 5, 0));
            }
            private static Task<T> Prohibido<T>() => throw new InvalidOperationException("Esta prueba no permite acceder a otras operaciones del repositorio.");
            public Task<int> AgregarReglaAsync(ReglaEcopuntos r, CancellationToken ct = default) => Prohibido<int>();
            public Task<ReglaEcopuntos?> ObtenerReglaAsync(int id, CancellationToken ct = default) => Prohibido<ReglaEcopuntos?>();
            public Task<IReadOnlyList<ReglaEcopuntos>> ListarReglasAsync(CancellationToken ct = default) => _reglas is null ? Prohibido<IReadOnlyList<ReglaEcopuntos>>() : Task.FromResult(_reglas);
            public Task<MovimientoEcopuntos?> ObtenerMovimientoAsync(int id, CancellationToken ct = default) => Prohibido<MovimientoEcopuntos?>();
            public Task<OperacionEcopuntos?> ObtenerOperacionAsync(string c, CancellationToken ct = default) => Prohibido<OperacionEcopuntos?>();
            public Task<IReadOnlyList<MovimientoEcopuntos>> AplicarAsync(SolicitudEcopuntos s, CancellationToken ct = default) => Prohibido<IReadOnlyList<MovimientoEcopuntos>>();
        }
        [TestMethod]
        public async Task TelegramConsultaExclusivamenteSaldoDelCiudadanoVinculado()
        {
            var repo = new ConsultaPuntos();
            var puntos = new ServicioEcopuntos(_usuarios, _interacciones, _materiales, repo, _reloj);
            var servicio = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new Dobles.GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, ecopuntos: puntos);
            string texto = Unica(await EnviarConAsync(servicio, "/ecopuntos")).Texto;
            Assert.AreEqual(Ciudadano, repo.Consultado);
            StringAssert.Contains(texto, "Saldo: 5 ecopuntos");
            StringAssert.Contains(texto, "Acreditados: 5; revertidos: 0");
            StringAssert.Contains(texto, "depósito 2, regla 4");
            repo.Consultado = null;
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await EnviarConAsync(servicio, "/ecopuntos", cuenta: Invitado)).Texto);
            Assert.IsNull(repo.Consultado);
        }
        [TestMethod]
        public async Task CiudadanoNoPuedeEscribirLibroAunqueConozcaLosIdentificadores()
        {
            var puntos = new ServicioEcopuntos(_usuarios, _interacciones, _materiales, new ConsultaPuntos(), _reloj);
            Assert.IsFalse((await puntos.AcreditarAsync(Ciudadano, 1, 1, "conocida")).Exitoso);
            Assert.IsFalse((await puntos.RevertirAsync(Ciudadano, 1, "conocida", "Intento propio")).Exitoso);
            Assert.IsFalse((await puntos.RegistrarReglaAsync(Ciudadano, 1, 10, "Tarifa", Inicio.UtcDateTime)).Exitoso);
        }
    }
}
