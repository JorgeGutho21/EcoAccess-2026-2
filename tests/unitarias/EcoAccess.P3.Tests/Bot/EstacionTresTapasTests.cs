using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Bot.Dispositivos;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Bot
{
    /// <summary>
    /// Estación de UN ESP32 con TRES tapas, simulada con las mismas reglas que
    /// hardware/estacion_tres_canecas: tapa elegida por la identidad del sobre firmado, caneca de la orden igual a la de
    /// esa tapa, desafío de un solo uso, reserva global de órdenes, ejecución pendiente por tapa y puerto COM exclusivo.
    /// Son pruebas de SOFTWARE: no demuestran movimientos físicos, corriente ni posiciones reales.
    /// </summary>
    [TestClass]
    public sealed class EstacionTresTapasTests
    {
        private const int Blanca = 1093, Verde = 1400, Negra = 1401;
        private static readonly TimeSpan Ventana = TimeSpan.FromMilliseconds(150);
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 11, 20, 0, 0, TimeSpan.Zero));
        private readonly List<CredencialDispositivo> _credenciales = new();
        private int _orden = 100;

        [TestCleanup]
        public void Limpiar() { foreach (var c in _credenciales) c.Dispose(); }

        private CredencialDispositivo Credencial(string dispositivo, int caneca, byte semilla)
        {
            var c = new CredencialDispositivo(dispositivo, 1, caneca, Enumerable.Range(0, 32).Select(i => (byte)(i * 7 + semilla)).ToArray());
            _credenciales.Add(c);
            return c;
        }

        private (EstacionSimulada Estacion, Dictionary<int, CredencialDispositivo> Bridges) Montar(params int[] habilitadas)
        {
            var blanca = Credencial("esp32-piloto-blanca", Blanca, 1);
            var verde = Credencial("esp32-piloto-verde", Verde, 2);
            var negra = Credencial("esp32-piloto-negra", Negra, 3);
            // Mapeo real confirmado por el propietario (11/11/2026): un servo por tapa, blanca 19, verde 23, negra 18.
            var estacion = new EstacionSimulada(_reloj, Ventana,
                new EstacionSimulada.Tapa(blanca, new[] { 19 }, habilitadas.Length == 0 || habilitadas.Contains(Blanca)),
                new EstacionSimulada.Tapa(verde, new[] { 23 }, habilitadas.Length == 0 || habilitadas.Contains(Verde)),
                new EstacionSimulada.Tapa(negra, new[] { 18 }, habilitadas.Length == 0 || habilitadas.Contains(Negra)));
            return (estacion, new() { [Blanca] = blanca, [Verde] = verde, [Negra] = negra });
        }

        private ActuacionSerial Bridge(EstacionSimulada estacion, CredencialDispositivo credencial, Func<bool>? autorizado = null)
        {
            var protocolo = new ProtocoloDispositivoHmac(_reloj, (id, v) => id == credencial.Dispositivo && v == credencial.VersionClave ? credencial : null);
            return new ActuacionSerial(credencial, protocolo, estacion.Abrir, (_, _) => Task.FromResult(autorizado?.Invoke() ?? true), _reloj,
                TimeSpan.FromMilliseconds(10));
        }

        private ComandoApertura Orden(int caneca, double segundos = 120) =>
            new(++_orden, caneca, "orden-" + _orden, _reloj.GetUtcNow().UtcDateTime.AddSeconds(segundos));

        [TestMethod]
        [DataRow(Blanca, new[] { 19 })]
        [DataRow(Verde, new[] { 23 })]
        [DataRow(Negra, new[] { 18 })]
        public async Task CadaCanecaAbreSoloSuServo_YCierraTrasLaVentana(int caneca, int[] pines)
        {
            var (estacion, bridges) = Montar();
            int aperturas = 0;
            await Bridge(estacion, bridges[caneca]).EjecutarAsync(Orden(caneca), _ => { aperturas++; return Task.CompletedTask; }, default);

            Assert.AreEqual(1, aperturas);
            CollectionAssert.AreEquivalent(pines, estacion.Escrituras.Select(e => e.Pin).Distinct().ToArray());
            Assert.AreEqual(2, estacion.Escrituras.Count, "un servo abre y el mismo cierra");
            CollectionAssert.AreEqual(new[] { "abierto", "cerrado" }, estacion.Escrituras.Select(e => e.Posicion).ToArray());
            Assert.IsTrue(estacion.UltimaVentanaReal >= Ventana, $"cerró a los {estacion.UltimaVentanaReal.TotalMilliseconds} ms");
        }

        [TestMethod]
        public async Task OrdenParaOtraCaneca_ElBridgeNoLaEnvia_YLaEstacionLaRechazaSiLlega()
        {
            var (estacion, bridges) = Montar();
            // El bridge de la blanca no acepta una orden de la verde.
            await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
                Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Verde), _ => Task.CompletedTask, default));
            // Aunque alguien firme con la clave de la blanca una orden para la verde, la estación no mueve nada.
            string respuesta = await estacion.EnviarCrudoAsync(bridges[Blanca], Orden(Verde));
            Assert.IsFalse(EstacionSimulada.Aceptado(respuesta, bridges[Blanca], _reloj));
            Assert.AreEqual(0, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task DispositivoDesconocidoOFirmaFalsa_NoMuevenNada()
        {
            var (estacion, _) = Montar();
            var gris = Credencial("esp32-piloto-gris", Blanca, 9);              // identidad que la estación no tiene
            var impostora = Credencial("esp32-piloto-blanca", Blanca, 77);       // identidad real, clave distinta
            Assert.AreEqual("{\"error\":\"rechazado\"}", await estacion.EnviarCrudoAsync(gris, Orden(Blanca)));
            Assert.AreEqual("{\"error\":\"rechazado\"}", await estacion.EnviarCrudoAsync(impostora, Orden(Blanca)));
            Assert.AreEqual(0, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task OrdenCaducada_NoSeEnvia_YValidezCortaSeRechaza()
        {
            var (estacion, bridges) = Montar();
            await Assert.ThrowsExactlyAsync<InvalidDataException>(() =>
                Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca, -1), _ => Task.CompletedTask, default));
            string respuesta = await estacion.EnviarCrudoAsync(bridges[Blanca], Orden(Blanca), validezMs: 50);
            Assert.IsFalse(EstacionSimulada.Aceptado(respuesta, bridges[Blanca], _reloj));
            Assert.AreEqual(0, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task OrdenDuplicada_EnCualquierTapa_NoReabre()
        {
            var (estacion, bridges) = Montar();
            var orden = Orden(Blanca);
            await Bridge(estacion, bridges[Blanca]).EjecutarAsync(orden, _ => Task.CompletedTask, default);
            int antes = estacion.Escrituras.Count;
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Blanca]).EjecutarAsync(orden, _ => Task.CompletedTask, default));
            // La misma orden y clave firmadas por otra tapa tampoco pasan: la reserva es global.
            string otra = await estacion.EnviarCrudoAsync(bridges[Verde], orden with { IdContenedor = Verde });
            Assert.IsFalse(EstacionSimulada.Aceptado(otra, bridges[Verde], _reloj));
            Assert.AreEqual(antes, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task DesafioRepetido_SeConsumeAunqueLaOrdenSeaNueva()
        {
            var (estacion, bridges) = Montar();
            var (primera, desafio) = await estacion.EnviarCrudoConDesafioAsync(bridges[Blanca], Orden(Blanca));
            Assert.IsTrue(EstacionSimulada.Aceptado(primera, bridges[Blanca], _reloj));
            int antes = estacion.Escrituras.Count;
            string repetida = await estacion.EnviarCrudoAsync(bridges[Blanca], Orden(Blanca), desafioForzado: desafio);
            Assert.IsFalse(EstacionSimulada.Aceptado(repetida, bridges[Blanca], _reloj));
            Assert.AreEqual(antes, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task ReinicioDuranteLaApertura_BloqueaSoloEsaTapaHastaRecuperacionManual()
        {
            var (estacion, bridges) = Montar();
            estacion.ReiniciarDuranteCiclo = true;
            int aperturas = 0;
            await Assert.ThrowsExactlyAsync<IOException>(() =>
                Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => { aperturas++; return Task.CompletedTask; }, default));
            Assert.AreEqual(1, aperturas, "la apertura se avisó; el cierre no se confirmó y no hay reintento");
            estacion.ReiniciarDuranteCiclo = false;
            estacion.Reiniciar();
            Assert.AreEqual(0, estacion.PwmConectado.Count, "al encender no hay PWM en ningún pin");

            int antes = estacion.Escrituras.Count;
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => Task.CompletedTask, default));
            Assert.AreEqual(antes, estacion.Escrituras.Count, "la blanca queda bloqueada");
            await Bridge(estacion, bridges[Negra]).EjecutarAsync(Orden(Negra), _ => Task.CompletedTask, default);   // las otras siguen

            estacion.RecuperacionManual(Blanca);
            await Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => Task.CompletedTask, default);
        }

        [TestMethod]
        public async Task DesconexionAntesDelCierre_FallaSinReintentar()
        {
            var (estacion, bridges) = Montar();
            estacion.DesconectarAntesDelCierre = true;
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Verde]).EjecutarAsync(Orden(Verde), _ => Task.CompletedTask, default));
            Assert.AreEqual(1, estacion.Aperturas, "una sola apertura: el bridge no repite la orden");
        }

        [TestMethod]
        public async Task FalloDeServo_NoAvisaApertura_YDejaLaTapaFueraDeServicio()
        {
            var (estacion, bridges) = Montar();
            estacion.FallarServo = true;
            int aperturas = 0;
            await Assert.ThrowsExactlyAsync<IOException>(() =>
                Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => { aperturas++; return Task.CompletedTask; }, default));
            Assert.AreEqual(0, aperturas);
            estacion.FallarServo = false;
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => Task.CompletedTask, default));
        }

        [TestMethod]
        public async Task TapaFueraDeServicio_NoSeMueve_YLasDemasSi()
        {
            var (estacion, bridges) = Montar(Blanca);   // verde y negra sin pines confirmados o sin calibrar
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Verde]).EjecutarAsync(Orden(Verde), _ => Task.CompletedTask, default));
            await Assert.ThrowsExactlyAsync<IOException>(() => Bridge(estacion, bridges[Negra]).EjecutarAsync(Orden(Negra), _ => Task.CompletedTask, default));
            Assert.AreEqual(0, estacion.Escrituras.Count);
            await Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca), _ => Task.CompletedTask, default);
            CollectionAssert.AreEquivalent(new[] { 19 }, estacion.Escrituras.Select(e => e.Pin).Distinct().ToArray());
        }

        [TestMethod]
        public async Task TresBridgesALaVez_ComparteElPuerto_YNuncaSeMuevenDosTapas()
        {
            var (estacion, bridges) = Montar();
            var tareas = new[] { Blanca, Verde, Negra }.Select(c =>
                Task.Run(() => Bridge(estacion, bridges[c]).EjecutarAsync(Orden(c), _ => Task.CompletedTask, default))).ToArray();
            await Task.WhenAll(tareas);
            Assert.AreEqual(3, estacion.Aperturas);
            Assert.AreEqual(1, estacion.MaximoTapasEnMovimiento, "el puerto exclusivo serializa las tapas");
            Assert.AreEqual(6, estacion.Escrituras.Count, "tres tapas de un servo: abrir y cerrar cada una");
            foreach (var grupo in estacion.Escrituras.GroupBy(e => e.Ciclo))
                Assert.AreEqual(1, grupo.Select(e => e.Caneca).Distinct().Count(), "un ciclo toca una sola tapa");
        }

        [TestMethod]
        public async Task PuertoOcupadoHastaCasiVencer_NoEsperaMasAllaDeLaOrden()
        {
            var (estacion, bridges) = Montar();
            using var ocupado = estacion.Abrir();
            var reloj = Stopwatch.StartNew();
            await Assert.ThrowsExactlyAsync<UnauthorizedAccessException>(() =>
                Bridge(estacion, bridges[Blanca]).EjecutarAsync(Orden(Blanca, 1.5), _ => Task.CompletedTask, default));
            Assert.IsTrue(reloj.Elapsed < TimeSpan.FromSeconds(1), "con menos de 2 s de vigencia no se queda esperando");
            Assert.AreEqual(0, estacion.Escrituras.Count);
        }

        [TestMethod]
        public async Task PuertoOcupado_EsperaTurno_YRevalidaLaOrdenAlObtenerlo()
        {
            var (estacion, bridges) = Montar();
            var ocupado = estacion.Abrir();
            bool revocada = false;
            var tarea = Bridge(estacion, bridges[Blanca], () => !revocada).EjecutarAsync(Orden(Blanca), _ => Task.CompletedTask, default);
            await Task.Delay(100);
            Assert.IsFalse(tarea.IsCompleted, "espera el turno del puerto");
            revocada = true;              // la orden se anuló mientras esperaba
            ocupado.Dispose();
            await Assert.ThrowsExactlyAsync<IOException>(() => tarea);
            Assert.AreEqual(0, estacion.Escrituras.Count);
        }

        [TestMethod]
        public void LaVentanaDelBridgeCubreLosDiezSegundosDelFirmware()
        {
            Assert.AreEqual(10, EcoAccess.P3.Logica.Servicios.MensajesTelegram.VentanaDepositoSegundos);
            Assert.IsTrue(ActuacionSerial.MargenEsperaPuerto < EcoAccess.P3.Logica.Servicios.ServicioInteracciones.VigenciaOrden);
        }
    }

    /// <summary>Modelo en C# del firmware de tres tapas, con un COM exclusivo como el de Windows.</summary>
    internal sealed class EstacionSimulada
    {
        internal sealed record Tapa(CredencialDispositivo Credencial, int[] Pines, bool Habilitada);
        internal sealed record Escritura(int Ciclo, int Caneca, int Pin, string Posicion);

        private readonly Tapa[] _tapas;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _ventana;
        private readonly ProtocoloDispositivoHmac _protocolo;
        private readonly HashSet<int> _ordenes = new();
        private readonly HashSet<string> _claves = new(StringComparer.Ordinal);
        private readonly HashSet<int> _pendientes = new();
        private readonly HashSet<int> _inhabilitadas = new();
        private readonly object _bloqueo = new();
        private bool _puertoAbierto;
        private string? _desafio;
        private int _ciclo, _enMovimiento;

        internal List<Escritura> Escrituras { get; } = new();
        internal HashSet<int> PwmConectado { get; } = new();
        internal int Aperturas, MaximoTapasEnMovimiento;
        internal TimeSpan UltimaVentanaReal;
        internal bool ReiniciarDuranteCiclo, DesconectarAntesDelCierre, FallarServo;

        internal EstacionSimulada(TimeProvider reloj, TimeSpan ventana, params Tapa[] tapas)
        {
            _reloj = reloj; _ventana = ventana; _tapas = tapas;
            _protocolo = new ProtocoloDispositivoHmac(reloj, (id, v) => tapas.FirstOrDefault(t => t.Credencial.Dispositivo == id && t.Credencial.VersionClave == v)?.Credencial);
        }

        internal IEnlaceActuadorSerial Abrir()
        {
            lock (_bloqueo)
            {
                if (_puertoAbierto) throw new UnauthorizedAccessException("Acceso denegado al puerto COM.");
                _puertoAbierto = true;
            }
            return new Enlace(this);
        }

        internal void Reiniciar() { lock (_bloqueo) { _desafio = null; PwmConectado.Clear(); _inhabilitadas.Clear(); } }
        internal void RecuperacionManual(int caneca) { lock (_bloqueo) _pendientes.Remove(caneca); }

        internal static bool Aceptado(string respuesta, CredencialDispositivo credencial, TimeProvider reloj) =>
            new ProtocoloDispositivoHmac(reloj, (_, _) => credencial).Leer<ReciboActuacionSerial>(Encoding.UTF8.GetBytes(respuesta), "recepcion-serial")
                is { } r && r.Cuerpo.Aceptado;

        /// <summary>Envía un permiso firmado sin pasar por el bridge (para atacar la estación directamente).</summary>
        internal async Task<string> EnviarCrudoAsync(CredencialDispositivo firmante, ComandoApertura comando, int validezMs = 2000, string? desafioForzado = null) =>
            (await EnviarCrudoConDesafioAsync(firmante, comando, validezMs, desafioForzado)).Respuesta;

        internal async Task<(string Respuesta, string Desafio)> EnviarCrudoConDesafioAsync(CredencialDispositivo firmante, ComandoApertura comando,
            int validezMs = 2000, string? desafioForzado = null)
        {
            using var enlace = Abrir();
            string linea = await enlace.IntercambiarAsync("?", default);
            string nonce = desafioForzado ?? linea.Substring(linea.IndexOf(':') + 2, 32);
            string paquete = Encoding.UTF8.GetString(ProtocoloDispositivoHmac.CrearSobre(firmante, "actuacion-serial",
                new PermisoActuacionSerial(comando, nonce, validezMs)));
            string respuesta = await enlace.IntercambiarAsync(paquete, default);
            if (respuesta.Contains("ecoaccess") || Aceptado(respuesta, firmante, _reloj)) { try { await enlace.LeerLineaAsync(TimeSpan.FromSeconds(2), default); } catch (Exception) { } }
            return (respuesta, nonce);
        }

        private string Desafio()
        {
            lock (_bloqueo)
            {
                _desafio = Guid.NewGuid().ToString("N");
                return "{\"ecoaccessDesafio\":\"" + _desafio + "\"}";
            }
        }

        private string Recibo(Tapa t, string original, bool aceptado, string estado, string nonce) =>
            Encoding.UTF8.GetString(ProtocoloDispositivoHmac.CrearSobre(t.Credencial, "recepcion-serial",
                new ReciboActuacionSerial(nonce, Compat.ToHexString(Compat.Sha256(Encoding.UTF8.GetBytes(original))), aceptado, estado)));

        /// <summary>Devuelve la respuesta inmediata y, si se aplicó la apertura, la acción que produce el cierre.</summary>
        private (string Respuesta, Func<string>? Cierre) Procesar(string linea)
        {
            lock (_bloqueo)
            {
                var leido = _protocolo.Leer<PermisoActuacionSerial>(Encoding.UTF8.GetBytes(linea), "actuacion-serial");
                Tapa? tapa = leido is { } l ? _tapas.First(t => t.Credencial == l.Credencial) : null;
                var permiso = leido?.Cuerpo;
                string nonce = permiso?.Desafio ?? "";
                int caneca = tapa?.Credencial.IdContenedor ?? 0;
                bool valido = tapa is not null && permiso is not null && permiso.Comando is not null
                    && permiso.ValidezMs is >= 100 and <= 5000 && permiso.Comando.IdOrden > 0
                    && permiso.Comando.IdContenedor == caneca
                    && !Compat.IsNullOrWhiteSpace(permiso.Comando.ClaveIdempotencia) && permiso.Comando.ClaveIdempotencia.Length <= 80
                    && _desafio is { Length: 32 } && nonce == _desafio;
                _desafio = null; // consumir incluso ante rechazo
                if (!valido || !tapa!.Habilitada || _inhabilitadas.Contains(caneca) || _pendientes.Contains(caneca)
                    || _ordenes.Contains(permiso!.Comando.IdOrden) || _claves.Contains(permiso.Comando.ClaveIdempotencia))
                    return (tapa is null ? "{\"error\":\"rechazado\"}" : Recibo(tapa, linea, false, "rechazado", nonce), null);
                _ordenes.Add(permiso.Comando.IdOrden); _claves.Add(permiso.Comando.ClaveIdempotencia); _pendientes.Add(caneca);

                int ciclo = ++_ciclo;
                if (++_enMovimiento > MaximoTapasEnMovimiento) MaximoTapasEnMovimiento = _enMovimiento;
                foreach (int p in tapa.Pines) PwmConectado.Add(p);
                if (FallarServo)
                {
                    _enMovimiento--; _inhabilitadas.Add(caneca);
                    return (Recibo(tapa, linea, false, "incierto", nonce), null);
                }
                foreach (int p in tapa.Pines) Escrituras.Add(new(ciclo, caneca, p, "abierto"));
                Aperturas++;
                var abierto = Stopwatch.StartNew();
                Func<string> cierre = () =>
                {
                    Thread.Sleep(_ventana);   // ABIERTO_MS del firmware, acortado en la prueba
                    lock (_bloqueo)
                    {
                        _enMovimiento--;
                        if (ReiniciarDuranteCiclo || DesconectarAntesDelCierre)
                        {
                            PwmConectado.Clear();
                            throw new IOException("Se desconectó el actuador.");  // queda pendiente: no se libera
                        }
                        foreach (int p in tapa.Pines) Escrituras.Add(new(ciclo, caneca, p, "cerrado"));
                        UltimaVentanaReal = abierto.Elapsed;
                        _pendientes.Remove(caneca);
                        return Recibo(tapa, linea, true, ActuacionSerial.EstadoCierre, nonce);
                    }
                };
                return (Recibo(tapa, linea, true, ActuacionSerial.EstadoApertura, nonce), cierre);
            }
        }

        private void Cerrar() { lock (_bloqueo) _puertoAbierto = false; }

        private sealed class Enlace : IEnlaceActuadorSerial
        {
            private readonly EstacionSimulada _e;
            private Func<string>? _cierre;
            private bool _cerrado;
            internal Enlace(EstacionSimulada e) => _e = e;

            public Task<string> IntercambiarAsync(string linea, CancellationToken ct)
            {
                ct.ThrowIfCancellationRequested();
                if (linea == "?") return Task.FromResult(_e.Desafio());
                var (respuesta, cierre) = _e.Procesar(linea);
                _cierre = cierre;
                return Task.FromResult(respuesta);
            }

            public Task<string> LeerLineaAsync(TimeSpan limite, CancellationToken ct) => Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var c = _cierre ?? throw new TimeoutException("El actuador no confirmó a tiempo.");
                _cierre = null;
                return c();
            }, ct);

            public void Dispose() { if (!_cerrado) { _cerrado = true; _e.Cerrar(); } }
        }
    }
}
