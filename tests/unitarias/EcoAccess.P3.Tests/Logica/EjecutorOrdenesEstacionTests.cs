using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Logica.Vision35.Expo;
using EcoAccess.P3.Tests.Logica.Dobles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EcoAccess.P3.Tests.Logica
{
    /// <summary>
    /// Causa raíz del fallo del operario: las órdenes de Registro manual y del asistente de depósito (ciudadano e invitado)
    /// solo se insertaban en Oracle y nadie las llevaba al ESP32. EjecutorOrdenesEstacion las entrega al servicio central
    /// (panel) y solo registra apertura y depósito si el controlador confirma. Lógica con dobles: no prueba el hardware real.
    /// </summary>
    [TestClass]
    public sealed class EjecutorOrdenesEstacionTests
    {
        private const int Operario = 2, Ciudadano = 3;
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(new DateTimeOffset(2026, 11, 25, 20, 0, 0, TimeSpan.Zero));
        private readonly ServicioInteracciones _servicio;
        private readonly Material _organico;
        private readonly Contenedor _verde, _blanca, _negra, _demo;

        public EjecutorOrdenesEstacionTests()
        {
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.AgregarCiudadano(Ciudadano);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _organico = _materiales.Sembrar("ORG");
            _verde = _contenedores.SembrarCon("EcoAccess — Caneca verde", CanecasEstacionOficial.QrVerde, EstadoContenedor.EnServicio, _organico.Id);
            _blanca = _contenedores.SembrarCon("EcoAccess — Caneca blanca", CanecasEstacionOficial.QrBlanca, EstadoContenedor.EnServicio, _organico.Id);
            _negra = _contenedores.SembrarCon("EcoAccess — Caneca negra", CanecasEstacionOficial.QrNegra, EstadoContenedor.EnServicio, _organico.Id);
            _demo = _contenedores.SembrarCon("DEMO - Caneca verde", "DemoCanecaVerde00001", EstadoContenedor.EnServicio, _organico.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _servicio = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
        }

        private sealed class EstacionFalsa : IEstacionCanecasExpo
        {
            public bool Simulada { get; set; }
            public bool Acepta { get; set; } = true;
            public string Rechazo { get; set; } = "La estación está en pausa manual (mantenimiento).";
            public List<(string Clave, string Letras)> Pedidas { get; } = new();
            public bool EsSimulada => Simulada;
            public Task<EstadoEstacionExpo> EstadoAsync(CancellationToken ct = default) =>
                Task.FromResult(new EstadoEstacionExpo(true, true, true, "", "COM9", "fw"));
            public Task<ResultadoAperturaEstacion> AbrirAsync(string clave, IReadOnlyList<CategoriaMaterial> canecas, string modalidad, string origen, string solicitante, string? materiales, CancellationToken ct = default)
            {
                string letras = ClienteEstacionPanel.Letras(canecas);
                Pedidas.Add((clave, letras));
                return Task.FromResult(Acepta
                    ? new ResultadoAperturaEstacion(true, false, false, letras, 7000, modalidad, 7000, "ok")
                    : new ResultadoAperturaEstacion(false, false, false, letras, 0, modalidad, 0, Rechazo));
            }
        }

        private EjecutorOrdenesEstacion Ejecutor(EstacionFalsa e) =>
            new(_repositorio, _contenedores, _servicio, e, _reloj, (_, _) => Task.CompletedTask);

        private async Task<int> OrdenOperarioAsync(Contenedor caneca)
        {
            var r = await _servicio.RegistrarAsistidoAsync(Operario, Ciudadano, caneca.Id, _organico.Id);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return r.Valor;
        }

        [DataTestMethod]
        [DataRow("verde", "V")]
        [DataRow("blanca", "B")]
        [DataRow("negra", "N")]
        public async Task Operario_RegistroManual_LlegaAlEsp32_YDejaDepositoPendiente(string color, string letra)
        {
            var caneca = color switch { "verde" => _verde, "blanca" => _blanca, _ => _negra };
            int idOrden = await OrdenOperarioAsync(caneca);
            var estacion = new EstacionFalsa();

            var (r, cierre) = await Ejecutor(estacion).EjecutarAsync(idOrden, "operario", "op", "ORG");
            await cierre;

            Assert.IsTrue(r.ComandoConfirmado, r.Mensaje);
            Assert.AreEqual(letra, estacion.Pedidas.Single().Letras, "Cada caneca se mapea a su servo.");
            StringAssert.StartsWith(r.Mensaje, "Caneca abierta. Puedes desechar tu residuo.");
            var op = await _servicio.ConsultarOperacionAsync(Operario, idOrden);
            Assert.AreEqual(EstadoOrdenApertura.Cerrada, op.Valor.Orden.Estado);
            Assert.IsNotNull(op.Valor.Deposito, "Al cerrar queda el depósito pendiente de verificación.");
            Assert.IsFalse(op.Valor.Deposito!.EstaVerificado);
        }

        [TestMethod]
        public async Task EstacionRechaza_NoRegistraAperturaNiDeposito()
        {
            int idOrden = await OrdenOperarioAsync(_verde);
            var estacion = new EstacionFalsa { Acepta = false };

            var (r, _) = await Ejecutor(estacion).EjecutarAsync(idOrden, "operario", "op", "ORG");

            Assert.IsFalse(r.ComandoConfirmado);
            StringAssert.Contains(r.Mensaje, "pausa");
            var op = await _servicio.ConsultarOperacionAsync(Operario, idOrden);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, op.Valor.Orden.Estado, "Sin confirmación del ESP32 la orden no avanza.");
            Assert.IsNull(op.Valor.Deposito);
        }

        [TestMethod]
        public async Task SinEstacionFisica_NoSimulaUnaApertura()
        {
            int idOrden = await OrdenOperarioAsync(_negra);
            var estacion = new EstacionFalsa { Simulada = true };

            var (r, _) = await Ejecutor(estacion).EjecutarAsync(idOrden, "operario", "op", "ORG");

            Assert.IsFalse(r.ComandoConfirmado);
            Assert.AreEqual(0, estacion.Pedidas.Count);
        }

        [TestMethod]
        public async Task CanecaDemo_NuncaSeEnviaAlEsp32()
        {
            int idOrden = await OrdenOperarioAsync(_demo);
            var estacion = new EstacionFalsa();

            var (r, _) = await Ejecutor(estacion).EjecutarAsync(idOrden, "operario", "op", "ORG");

            Assert.IsFalse(r.ComandoConfirmado);
            Assert.AreEqual(0, estacion.Pedidas.Count);
        }

        [TestMethod]
        public async Task MismaOrdenDosVeces_NoAbreDosVeces()
        {
            int idOrden = await OrdenOperarioAsync(_blanca);
            var estacion = new EstacionFalsa();
            var ejecutor = Ejecutor(estacion);

            var (primera, cierre) = await ejecutor.EjecutarAsync(idOrden, "operario", "op", "ORG");
            await cierre;
            var (segunda, _) = await ejecutor.EjecutarAsync(idOrden, "operario", "op", "ORG");

            Assert.IsTrue(primera.ComandoConfirmado);
            Assert.IsFalse(segunda.ComandoConfirmado);
            Assert.AreEqual(1, estacion.Pedidas.Count);
            StringAssert.StartsWith(estacion.Pedidas[0].Clave, "orden-", "La clave de idempotencia de Oracle viaja al panel.");
        }

        [TestMethod]
        public void Mapeo_PorQrYNombre_IgnoraDemoYSimulacion()
        {
            Assert.AreEqual('V', CanecasEstacionOficial.Letra(_verde));
            Assert.AreEqual('B', CanecasEstacionOficial.Letra(_blanca));
            Assert.AreEqual('N', CanecasEstacionOficial.Letra(_negra));
            Assert.IsNull(CanecasEstacionOficial.Letra(_demo));
        }
    }
}
