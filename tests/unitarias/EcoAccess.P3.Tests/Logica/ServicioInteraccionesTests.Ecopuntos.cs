using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>
    /// Recorrido depósito → ecopuntos sin Oracle: usa el mismo escenario real de interacción, orden, eventos y
    /// depósito de esta clase, y un libro en memoria que respeta el contrato de <see cref="IRepositorioEcopuntos"/>
    /// (una clave = una operación, un solo crédito activo por depósito). La garantía transaccional definitiva la da
    /// el repositorio Oracle y se prueba en EcoAccess.P3.Datos.Tests.
    /// </summary>
    public sealed partial class ServicioInteraccionesTests
    {
        private const int Puntos = 10;

        private (ServicioEcopuntos Servicio, LibroEcopuntosEnMemoria Libro, int Regla) PrepararEcopuntos()
        {
            var libro = new LibroEcopuntosEnMemoria(_repositorio);
            int regla = libro.AgregarRegla(new ReglaEcopuntos(0, _pet.Id, Puntos, "Regla de prueba, no es tarifa definitiva",
                AhoraUtc.AddDays(-1), null, Admin));
            return (new ServicioEcopuntos(_usuarios, _repositorio, _materiales, libro, _reloj), libro, regla);
        }

        private async Task<int> DepositoVerificadoAsync()
        {
            int idDeposito = await DepositoCompletoAsync();
            Assert.IsTrue((await _servicio.VerificarDepositoAsync(Operario, idDeposito, _pet.Id)).Exitoso);
            return idDeposito;
        }

        [TestMethod]
        public async Task Ecopuntos_DepositoVerificado_AcreditaUnaVezAlTitular()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();

            var r = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1");

            Assert.IsTrue(r.Exitoso, r.ToString());
            MovimientoEcopuntos credito = r.Valor.Single();
            Assert.AreEqual(Ciudadano, credito.IdUsuario);
            Assert.AreEqual(Puntos, credito.Puntos);
            Assert.AreEqual(Puntos, (await puntos.ConsultarAsync(Operario, Ciudadano)).Valor.Saldo);
            Assert.HasCount(1, libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_DepositoSinVerificar_RechazaYNoEscribe()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoCompletoAsync();

            var r = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1");

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.IsEmpty(libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_AperturaSinDeteccion_NoTieneDepositoQueAcreditar()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            var (_, idOrden) = await AutorizarAsync();
            Assert.IsTrue((await _servicio.MarcarEnviadaAsync(idOrden)).Exitoso);
            Assert.AreEqual(ResultadoEvento.Aplicado, (await EventoAsync("e-1", TipoEventoDispositivo.Acuse, idOrden)).Valor.Resultado);
            Assert.AreEqual(ResultadoEvento.Aplicado, (await EventoAsync("e-2", TipoEventoDispositivo.Apertura, idOrden)).Valor.Resultado);

            Assert.IsEmpty(_repositorio.Depositos, "Una apertura sin evento del sensor no es un depósito.");
            var r = await puntos.AcreditarAsync(Admin, 999, regla, "credito-1");
            Assert.IsFalse(r.Exitoso);
            Assert.IsEmpty(libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_ReintentoConLaMismaClave_DevuelveElMismoCreditoSinDuplicar()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();

            var primero = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1");
            _reloj.Avanzar(TimeSpan.FromSeconds(30));
            var reintento = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1");

            Assert.IsTrue(reintento.Exitoso, reintento.ToString());
            Assert.AreEqual(primero.Valor.Single().Id, reintento.Valor.Single().Id);
            Assert.HasCount(1, libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_SegundoCreditoConOtraClave_SeRechazaSinDuplicar()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();
            Assert.IsTrue((await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1")).Exitoso);

            var duplicado = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-2");

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, duplicado.Motivo);
            Assert.AreEqual(Puntos, (await puntos.ConsultarAsync(Operario, Ciudadano)).Valor.Saldo);
        }

        [TestMethod]
        public async Task Ecopuntos_MismaClaveConOtroContenido_SeRechazaComoDuplicado()
        {
            var (puntos, _, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();
            Assert.IsTrue((await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1")).Exitoso);

            var otro = await puntos.RevertirAsync(Admin, 1, "credito-1", "Intento de reutilizar la clave");

            Assert.AreEqual(MotivoRechazo.Duplicado, otro.Motivo);
        }

        [TestMethod]
        public async Task Ecopuntos_ReversionYNuevoCredito_ConservanLaHistoriaYElSaldoCorrecto()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();
            int credito = (await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1")).Valor.Single().Id;

            var reverso = await puntos.RevertirAsync(Admin, credito, "reverso-1", "Material mal verificado");
            var corregido = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-2");

            Assert.IsTrue(reverso.Exitoso, reverso.ToString());
            Assert.IsTrue(corregido.Exitoso, corregido.ToString());
            Assert.HasCount(3, libro.Movimientos, "La corrección no borra: crédito, reverso y crédito nuevo.");
            Assert.AreEqual(Puntos, (await puntos.ConsultarAsync(Operario, Ciudadano)).Valor.Saldo);
        }

        [TestMethod]
        public async Task Ecopuntos_OperarioQueTambienEsTitular_NoPuedeAcreditarseASiMismo()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoCompletoAsync(OperarioCiudadano);
            Assert.IsTrue((await _servicio.VerificarDepositoAsync(Operario, idDeposito, _pet.Id)).Exitoso);

            var r = await puntos.AcreditarAsync(OperarioCiudadano, idDeposito, regla, "credito-1");

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
            Assert.IsEmpty(libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_CiudadanoNoPuedeAcreditar()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();

            var r = await puntos.AcreditarAsync(Ciudadano, idDeposito, regla, "credito-1");

            Assert.IsFalse(r.Exitoso);
            Assert.IsEmpty(libro.Movimientos);
        }

        [TestMethod]
        public async Task Ecopuntos_FalloDeOracleAlGuardar_PropagaElErrorSinMovimientosYElReintentoFunciona()
        {
            var (puntos, libro, regla) = PrepararEcopuntos();
            int idDeposito = await DepositoVerificadoAsync();
            libro.FallarProximaEscritura = true;

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1"));
            Assert.IsEmpty(libro.Movimientos, "Una transacción fallida no deja créditos a medias.");

            var reintento = await puntos.AcreditarAsync(Admin, idDeposito, regla, "credito-1");
            Assert.IsTrue(reintento.Exitoso, reintento.ToString());
            Assert.HasCount(1, libro.Movimientos);
        }

        /// <summary>
        /// Libro en memoria con las reglas del contrato: idempotencia por clave, un solo crédito activo por depósito,
        /// la corrección revierte sin borrar y el saldo es la suma del libro. Simula además un fallo de escritura.
        /// </summary>
        private sealed class LibroEcopuntosEnMemoria : IRepositorioEcopuntos
        {
            private readonly IRepositorioInteracciones _interacciones;

            public LibroEcopuntosEnMemoria(IRepositorioInteracciones interacciones)
            {
                _interacciones = interacciones;
            }

            private readonly List<ReglaEcopuntos> _reglas = new List<ReglaEcopuntos>();
            private readonly List<MovimientoEcopuntos> _movimientos = new List<MovimientoEcopuntos>();
            private readonly Dictionary<string, OperacionEcopuntos> _operaciones = new(StringComparer.Ordinal);

            public bool FallarProximaEscritura { get; set; }
            public IReadOnlyList<MovimientoEcopuntos> Movimientos => _movimientos;

            public int AgregarRegla(ReglaEcopuntos regla)
            {
                var guardada = new ReglaEcopuntos(_reglas.Count + 1, regla.IdMaterial, regla.Puntos, regla.Descripcion,
                    regla.DesdeUtc, regla.HastaUtc, regla.IdCreador, regla.PorUnidad);
                _reglas.Add(guardada);
                return guardada.Id;
            }

            public Task<int> AgregarReglaAsync(ReglaEcopuntos regla, CancellationToken ct = default) => Task.FromResult(AgregarRegla(regla));
            public Task<ReglaEcopuntos?> ObtenerReglaAsync(int id, CancellationToken ct = default) => Task.FromResult<ReglaEcopuntos?>(_reglas.FirstOrDefault(r => r.Id == id));
            public Task<IReadOnlyList<ReglaEcopuntos>> ListarReglasAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ReglaEcopuntos>>(_reglas);
            public Task<MovimientoEcopuntos?> ObtenerMovimientoAsync(int id, CancellationToken ct = default) => Task.FromResult<MovimientoEcopuntos?>(_movimientos.FirstOrDefault(m => m.Id == id));
            public Task<OperacionEcopuntos?> ObtenerOperacionAsync(string clave, CancellationToken ct = default) =>
                Task.FromResult(_operaciones.GetValueOrDefault(clave));

            public async Task<IReadOnlyList<MovimientoEcopuntos>> AplicarAsync(SolicitudEcopuntos s, CancellationToken ct = default)
            {
                if (FallarProximaEscritura)
                {
                    FallarProximaEscritura = false;
                    throw new InvalidOperationException("Simulación: Oracle no disponible al confirmar la transacción.");
                }

                Deposito deposito = (await _interacciones.ObtenerDepositoAsync(s.IdDeposito, ct))!;
                OrdenApertura orden = (await _interacciones.ObtenerOrdenAsync(deposito.IdOrden, ct))!;
                int titular = (await _interacciones.ObtenerInteraccionAsync(orden.IdInteraccion, ct))!.IdUsuario!.Value;
                int idOperacion = _operaciones.Count + 1;
                var nuevos = new List<MovimientoEcopuntos>();

                MovimientoEcopuntos? Activo() => _movimientos.LastOrDefault(m => m.IdDeposito == s.IdDeposito
                    && m.Tipo == TipoMovimientoEcopuntos.Credito && !_movimientos.Any(r => r.IdCreditoRevertido == m.Id));

                if (s.Tipo is TipoOperacionEcopuntos.Revertir or TipoOperacionEcopuntos.Corregir)
                {
                    MovimientoEcopuntos? credito = Activo();
                    if (credito is null || credito.Id != s.IdCredito)
                        throw new CambioConcurrenteException("El crédito ya no está activo.");
                    nuevos.Add(credito with
                    {
                        Id = _movimientos.Count + 1,
                        IdOperacion = idOperacion,
                        Tipo = TipoMovimientoEcopuntos.Reverso,
                        Puntos = -credito.Puntos,
                        IdCreditoRevertido = credito.Id,
                        IdActor = s.IdActor,
                        FechaUtc = s.FechaUtc,
                        Motivo = s.Motivo
                    });
                }

                if (s.Tipo is TipoOperacionEcopuntos.Acreditar or TipoOperacionEcopuntos.Corregir)
                {
                    if (s.Tipo == TipoOperacionEcopuntos.Acreditar && Activo() is not null)
                        throw new CambioConcurrenteException("El depósito ya tiene un crédito activo.");
                    ReglaEcopuntos regla = _reglas.Single(r => r.Id == s.IdRegla);
                    nuevos.Add(new MovimientoEcopuntos(_movimientos.Count + nuevos.Count + 1, idOperacion, s.IdDeposito, titular,
                        regla.Id, TipoMovimientoEcopuntos.Credito, regla.CalcularPuntos(deposito.CantidadVerificada), null, s.IdCredito, s.IdActor, s.FechaUtc, s.Motivo));
                }

                _movimientos.AddRange(nuevos);
                _operaciones[s.Clave] = new OperacionEcopuntos(s, nuevos);
                return nuevos;
            }

            public Task<ResumenEcopuntos> ConsultarAsync(int idUsuario, CancellationToken ct = default)
            {
                var propios = _movimientos.Where(m => m.IdUsuario == idUsuario).ToList();
                return Task.FromResult(new ResumenEcopuntos(propios.Sum(m => (long)m.Puntos), propios,
                    propios.Where(m => m.Puntos > 0).Sum(m => (long)m.Puntos), -propios.Where(m => m.Puntos < 0).Sum(m => (long)m.Puntos)));
            }
        }
    }
}
