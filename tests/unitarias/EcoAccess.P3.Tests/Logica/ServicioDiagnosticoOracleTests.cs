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
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed class ServicioDiagnosticoOracleTests
    {
        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly Inventario _inventario = new();

        public ServicioDiagnosticoOracleTests()
        {
            _usuarios.Agregar(1, true, CodigoRol.Administrador);
            _usuarios.Agregar(2, true, CodigoRol.Operario);
            _usuarios.Agregar(3, true, CodigoRol.Ciudadano);
            _usuarios.Agregar(4, false, CodigoRol.Administrador);
        }

        private ServicioDiagnosticoOracle Servicio() => new(_usuarios, _inventario, TimeProvider.System);

        [TestMethod]
        [DataRow(0, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(99, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(2, MotivoRechazo.SinPermiso)]
        [DataRow(3, MotivoRechazo.SinPermiso)]
        [DataRow(4, MotivoRechazo.ActorInactivo)]
        public async Task SinPermisoNoConsultaInventario(int id, MotivoRechazo motivo)
        {
            var resultado = await Servicio().ConsultarAsync(id);
            Assert.AreEqual(motivo, resultado.Motivo);
            Assert.AreEqual(0, _inventario.Llamadas);
        }

        [TestMethod]
        public async Task TablasAusentesNoSePresentanComoMigracionesAplicadas()
        {
            var resultado = await Servicio().ConsultarAsync(1);
            Assert.IsTrue(resultado.Exitoso);
            Assert.IsFalse(resultado.Valor.EstructuraCompleta);
            Assert.IsTrue(resultado.Valor.Migraciones.All(m => m.Estado == "Ausente"));
            Assert.HasCount(6, resultado.Valor.Migraciones);
        }

        [TestMethod]
        public async Task MigracionParcialNoSeConfundeConAusenteOPresente()
        {
            _inventario.Tablas = new string[] { "ROL", "CREDENCIAL_ACCESO", "TABLA_AJENA" };
            var resultado = (await Servicio().ConsultarAsync(1)).Valor;
            Assert.AreEqual("Parcial", resultado.Migraciones[0].Estado);
            StringAssert.Contains(resultado.Migraciones[0].ElementosFaltantes, "USUARIO");
            Assert.AreEqual("Estructura presente", resultado.Migraciones[1].Estado);
            Assert.AreEqual("Ausente", resultado.Migraciones[2].Estado);
            Assert.IsFalse(resultado.SinIncidencias);
        }

        [TestMethod]
        public async Task RevocacionDeRolSeRevalidaEnCadaConsulta()
        {
            var servicio = Servicio();
            Assert.IsTrue((await servicio.ConsultarAsync(1)).Exitoso);
            _usuarios.Agregar(1, true, CodigoRol.Operario);
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.ConsultarAsync(1)).Motivo);
            Assert.AreEqual(1, _inventario.Llamadas);
        }

        [TestMethod]
        public async Task IdentificadoresEntreComillasConOtraCapitalizacionNoAcreditanEstructura()
        {
            _inventario.Tablas = new string[] { "rol", "credencial_acceso" };
            _inventario.Restricciones = new string[] { "rol:ck_v006_rol_descripcion" };
            var resultado = (await Servicio().ConsultarAsync(1)).Valor;
            Assert.IsTrue(resultado.Migraciones.All(m => m.Estado == "Ausente"));
            Assert.IsFalse(resultado.SinIncidencias);
        }

        [TestMethod]
        public async Task CancelacionPreviaNoConsultaOracle()
        {
            using var cancelacion = new CancellationTokenSource();
            cancelacion.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => Servicio().ConsultarAsync(1, cancelacion.Token));
            Assert.AreEqual(0, _inventario.Llamadas);
        }

        [TestMethod]
        public async Task ErrorDeInfraestructuraNoSeDeclaraDiagnosticoCorrecto()
        {
            _inventario.Fallar = true;
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => Servicio().ConsultarAsync(1));
        }

        [TestMethod]
        [DataRow(0, 0, 0, true)]
        [DataRow(1, 0, 0, false)]
        [DataRow(0, 1, 0, false)]
        [DataRow(0, 0, 1, false)]
        public async Task EstructuraPresenteNoOcultaRestriccionesObjetosOIndicesInvalidos(int restricciones, int objetos, int indices, bool esperado)
        {
            _inventario.Tablas = new string[]
    {
        "ROL",
        "USUARIO",
        "USUARIO_ROL",
        "MATERIAL",
        "PUNTO_INSTALACION",
        "CONTENEDOR",
        "CONTENEDOR_MATERIAL",
        "CREDENCIAL_ACCESO",
        "INTERACCION",
        "ORDEN_APERTURA",
        "EVENTO_DISPOSITIVO",
        "DEPOSITO",
        "VINCULO_TELEGRAM",
        "TOKEN_VINCULACION",
        "CONVERSACION_TELEGRAM",
        "UPDATE_TELEGRAM",
        "REGLA_ECOPUNTOS",
        "OPERACION_ECOPUNTOS",
        "MOVIMIENTO_ECOPUNTOS"
    };
            _inventario.Restricciones = new string[]
    {
        "ROL:CK_V006_ROL_DESCRIPCION",
        "USUARIO:CK_V006_USUARIO_NOMBRE",
        "MATERIAL:CK_V006_MATERIAL_NOMBRE",
        "PUNTO_INSTALACION:CK_V006_PUNTO_NOMBRE",
        "CONTENEDOR:CK_V006_CONTENEDOR_NOMBRE",
        "MATERIAL:CK_V006_MATERIAL_CODIGO"
    };
            _inventario.Incidencias = (restricciones, objetos, indices);
            var resultado = (await Servicio().ConsultarAsync(1)).Valor;
            Assert.IsTrue(resultado.EstructuraCompleta);
            Assert.AreEqual(esperado, resultado.SinIncidencias);
            _inventario.Restricciones = new string[] { "TABLA_AJENA:CK_V006_ROL_DESCRIPCION" };
            Assert.IsFalse((await Servicio().ConsultarAsync(1)).Valor.EstructuraCompleta);
        }

        private sealed class Inventario : IRepositorioDiagnosticoOracle
        {
            public int Llamadas { get; private set; }
            public bool Fallar { get; set; }
            public IReadOnlyList<string> Tablas { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> Restricciones { get; set; } = Array.Empty<string>();
            public (int Restricciones, int Objetos, int Indices) Incidencias { get; set; }
            public Task<EstadoEsquemaOracle> ConsultarAsync(CancellationToken cancellationToken = default)
            {
                Llamadas++;
                cancellationToken.ThrowIfCancellationRequested();
                if (Fallar) throw new InvalidOperationException("Fallo simulado");
                return Task.FromResult(new EstadoEsquemaOracle("ECOACCESS_P3", "FREEPDB1", 1, Tablas,
                    Incidencias.Restricciones, Incidencias.Objetos, Incidencias.Indices, Restricciones));
            }
        }
    }
}
