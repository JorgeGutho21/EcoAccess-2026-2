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
    /// <summary>
    /// Fase 1: los tres roles contra los servicios reales. Para cada operación de <see cref="MatrizPermisos"/> y cada
    /// rol se llama al servicio y se observa si el control de acceso dejó pasar la petición. Un permiso cambiado en un
    /// servicio sin cambiar la matriz (o al revés) hace fallar la prueba.
    /// </summary>
    [TestClass]
    public sealed class MatrizPermisosTests
    {
        private const int IdCiudadano = 1;
        private const int IdOperario = 2;
        private const int IdAdministrador = 3;
        private const int IdDesactivado = 4;
        private const int IdObjetivo = 50;

        private static readonly DateTime Desde = new(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Hasta = new(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc);

        /// <summary>Lo lanzan los repositorios de esta prueba: si se llega a ellos, el control de acceso ya dejó pasar.</summary>
        private sealed class AccesoConcedido : Exception { }

        private sealed class EcopuntosVacio : IRepositorioEcopuntos
        {
            public Task<int> AgregarReglaAsync(ReglaEcopuntos regla, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<ReglaEcopuntos?> ObtenerReglaAsync(int id, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<IReadOnlyList<ReglaEcopuntos>> ListarReglasAsync(CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<MovimientoEcopuntos?> ObtenerMovimientoAsync(int id, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<OperacionEcopuntos?> ObtenerOperacionAsync(string clave, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<IReadOnlyList<MovimientoEcopuntos>> AplicarAsync(SolicitudEcopuntos solicitud, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<ResumenEcopuntos> ConsultarAsync(int idUsuario, CancellationToken ct = default) => throw new AccesoConcedido();
        }

        private sealed class InformeVacio : IRepositorioInformeInstitucional
        {
            public Task<IndicadoresInforme> ConsultarAsync(FiltroInforme filtro, CancellationToken ct = default) => throw new AccesoConcedido();
            public Task<IReadOnlyList<FilaAgrupada>> AgruparAsync(FiltroInforme filtro, AgrupacionDepositos agrupacion, CancellationToken ct = default) => throw new AccesoConcedido();
        }

        private sealed class DiagnosticoVacio : IRepositorioDiagnosticoOracle
        {
            public Task<EstadoEsquemaOracle> ConsultarAsync(CancellationToken cancellationToken = default) => throw new AccesoConcedido();
        }

        private RepositorioUsuariosEnMemoria _usuarios = null!;
        private Dictionary<OperacionProtegida, Func<int, Task<Resultado>>> _llamadas = null!;

        [TestInitialize]
        public void Preparar()
        {
            _usuarios = new RepositorioUsuariosEnMemoria();
            _usuarios.AgregarCiudadano(IdCiudadano);
            _usuarios.Agregar(IdOperario, true, CodigoRol.Operario);
            _usuarios.Agregar(IdAdministrador, true, CodigoRol.Administrador);
            _usuarios.Agregar(IdDesactivado, false, CodigoRol.Administrador);
            _usuarios.AgregarCiudadano(IdObjetivo);

            var reloj = TimeProvider.System;
            var materiales = new RepositorioMaterialesEnMemoria();
            var contenedores = new RepositorioContenedoresEnMemoria();
            var ecopuntos = new ServicioEcopuntos(_usuarios, new RepositorioInteraccionesEnMemoria(), materiales, new EcopuntosVacio(), reloj);
            var estadisticas = new ServicioEstadisticas(_usuarios, new RepositorioEstadisticasEnMemoria());
            var cuentas = new ServicioUsuarios(_usuarios, reloj);
            var catalogo = new ServicioMateriales(_usuarios, materiales, contenedores);
            var informe = new ServicioInformeInstitucional(_usuarios, new InformeVacio());
            var diagnostico = new ServicioDiagnosticoOracle(_usuarios, new DiagnosticoVacio(), reloj);
            var filtro = new FiltroEstadisticas(Desde, Hasta);
            var filtroInforme = new FiltroInforme(Desde, Hasta, null, null, false);

            _llamadas = new Dictionary<OperacionProtegida, Func<int, Task<Resultado>>>
            {
                [OperacionProtegida.ConsultarMisEcopuntos] = async a => await ecopuntos.ConsultarMiosAsync(a),
                [OperacionProtegida.ConsultarMisEstadisticas] = async a => await estadisticas.ObtenerMiasAsync(a, Desde, Hasta),
                [OperacionProtegida.ConsultarEcopuntosDeOtro] = async a => await ecopuntos.ConsultarAsync(a, IdObjetivo),
                [OperacionProtegida.ListarReglasEcopuntos] = async a => await ecopuntos.ListarReglasAsync(a),
                [OperacionProtegida.RegistrarReglaEcopuntos] = async a => await ecopuntos.RegistrarReglaAsync(a, 1, 5, "Lata", Desde),
                [OperacionProtegida.BuscarCiudadanos] = async a => await cuentas.BuscarCiudadanosAsync(a, "Usuario"),
                [OperacionProtegida.ConsultarEstadisticasGenerales] = async a => await estadisticas.ObtenerResumenAsync(a, filtro),
                [OperacionProtegida.ListarUsuarios] = async a => await cuentas.ListarAsync(a),
                [OperacionProtegida.AsignarRol] = a => cuentas.AsignarRolAsync(a, IdObjetivo, CodigoRol.Operario),
                [OperacionProtegida.GestionarMateriales] = a => catalogo.DeshabilitarAsync(a, 999),
                [OperacionProtegida.ConsultarInformeInstitucional] = async a => await informe.ConsultarAsync(a, filtroInforme),
                [OperacionProtegida.DiagnosticarOracle] = async a => await diagnostico.ConsultarAsync(a),
            };
        }

        /// <summary>true si el control de acceso dejó pasar al actor (el resultado puede ser otro rechazo de negocio).</summary>
        private async Task<bool> PasaControlAsync(OperacionProtegida operacion, int actor)
        {
            try
            {
                Resultado r = await _llamadas[operacion](actor);
                return r.Motivo is not (MotivoRechazo.SinPermiso or MotivoRechazo.ActorInactivo or MotivoRechazo.ActorNoEncontrado);
            }
            catch (AccesoConcedido)
            {
                return true;
            }
        }

        [TestMethod]
        public void CadaOperacionTieneAlMenosUnRolYUnaLlamadaReal()
        {
            foreach (var operacion in MatrizPermisos.Operaciones)
            {
                Assert.IsNotEmpty(MatrizPermisos.RolesPermitidos(operacion), operacion.ToString());
                Assert.IsTrue(_llamadas.ContainsKey(operacion), $"La operación {operacion} no tiene prueba.");
            }
        }

        [TestMethod]
        [DataRow(CodigoRol.Ciudadano, IdCiudadano)]
        [DataRow(CodigoRol.Operario, IdOperario)]
        [DataRow(CodigoRol.Administrador, IdAdministrador)]
        public async Task LosServiciosCumplenLaMatrizParaCadaRol(CodigoRol rol, int actor)
        {
            var diferencias = new List<string>();
            foreach (var operacion in MatrizPermisos.Operaciones)
            {
                bool esperado = MatrizPermisos.Permite(rol, operacion);
                bool real = await PasaControlAsync(operacion, actor);
                if (esperado != real)
                    diferencias.Add($"{operacion}: matriz={(esperado ? "permite" : "niega")}, servicio={(real ? "permite" : "niega")}");
            }

            Assert.IsEmpty(diferencias, string.Join("; ", diferencias));
        }

        [TestMethod]
        public async Task UnaCuentaDesactivadaNoPasaNingunControlAunqueSeaAdministrador()
        {
            foreach (var operacion in MatrizPermisos.Operaciones)
                Assert.IsFalse(await PasaControlAsync(operacion, IdDesactivado), operacion.ToString());
        }

        [TestMethod]
        public async Task UnActorInexistenteNoPasaNingunControl()
        {
            foreach (var operacion in MatrizPermisos.Operaciones)
                Assert.IsFalse(await PasaControlAsync(operacion, 9999), operacion.ToString());
        }

        [TestMethod]
        public void ElCiudadanoNoTieneNingunaOperacionDeGestion()
        {
            var deGestion = MatrizPermisos.Operaciones.Where(o => MatrizPermisos.Permite(CodigoRol.Ciudadano, o)).ToList();
            CollectionAssert.AreEquivalent(
                new[] { OperacionProtegida.ConsultarMisEcopuntos, OperacionProtegida.ConsultarMisEstadisticas }, deGestion);
        }

        [TestMethod]
        public async Task ElAdministradorNoPuedeQuitarseASiMismoElRolAdministrador()
        {
            var cuentas = new ServicioUsuarios(_usuarios, TimeProvider.System);
            Resultado r = await cuentas.QuitarRolAsync(IdAdministrador, IdAdministrador, CodigoRol.Administrador);
            Assert.IsFalse(r.Exitoso);
            Assert.IsTrue(_usuarios.Leer(IdAdministrador)!.TieneRol(CodigoRol.Administrador));
        }
    }
}
