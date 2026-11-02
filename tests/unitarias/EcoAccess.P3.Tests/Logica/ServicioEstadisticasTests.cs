using System;
using System.Collections.Generic;
using System.Linq;
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
    public sealed class ServicioEstadisticasTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private const int Ciudadano = 3;
        private const int OperarioInactivo = 4;

        private static readonly DateTime Desde = new(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Hasta = new(2026, 11, 1, 5, 0, 0, DateTimeKind.Utc);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioEstadisticasEnMemoria _estadisticas = new();
        private readonly ServicioEstadisticas _servicio;

        public ServicioEstadisticasTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.Agregar(Ciudadano, activo: true, CodigoRol.Ciudadano);
            _usuarios.Agregar(OperarioInactivo, activo: false, CodigoRol.Operario);
            _servicio = new ServicioEstadisticas(_usuarios, _estadisticas);
        }

        private static FiltroEstadisticas Filtro() => new(Desde, Hasta);

        [TestMethod]
        public void Constructor_RepositorioNulo_Lanza()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioEstadisticas(_usuarios, null!));
        }

        [TestMethod]
        [DataRow(Admin)]
        [DataRow(Operario)]
        public async Task ObtenerResumenAsync_OperarioOAdministrador_DevuelveResumenDelRepositorio(int idActor)
        {
            FiltroEstadisticas filtro = Filtro() with { IdContenedor = 7, IdUsuario = 9 };

            Resultado<ResumenActividad> resultado = await _servicio.ObtenerResumenAsync(idActor, filtro);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreSame(RepositorioEstadisticasEnMemoria.ResumenFijo, resultado.Valor);
            Assert.AreEqual(filtro, _estadisticas.UltimoFiltro);
        }

        [TestMethod]
        [DataRow(0, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(99, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(OperarioInactivo, MotivoRechazo.ActorInactivo)]
        [DataRow(Ciudadano, MotivoRechazo.SinPermiso)]
        public async Task Consultas_ActorNoAutorizado_RechazanSinConsultar(int idActor, MotivoRechazo esperado)
        {
            Assert.AreEqual(esperado, (await _servicio.ObtenerResumenAsync(idActor, Filtro())).Motivo);
            Assert.AreEqual(esperado, (await _servicio.AgruparDepositosAsync(idActor, Filtro(), AgrupacionDepositos.Material)).Motivo);
            Assert.AreEqual(esperado, (await _servicio.ListarFallosPorContenedorAsync(idActor, Filtro())).Motivo);
            Assert.AreEqual(esperado, (await _servicio.ListarDepositosAsync(idActor, Filtro(), 1, 20)).Motivo);
            Assert.AreEqual(0, _estadisticas.Llamadas);
        }

        [TestMethod]
        public async Task Consultas_CiudadanoConFiltroInvalido_RechazaPorPermisoAntesQuePorFiltro()
        {
            Resultado<ResumenActividad> resultado = await _servicio.ObtenerResumenAsync(Ciudadano, null!);

            Assert.AreEqual(MotivoRechazo.SinPermiso, resultado.Motivo);
        }

        public static IEnumerable<object[]> FiltrosInvalidos()
        {
            FiltroEstadisticas valido = new(Desde, Hasta);
            yield return new object[] { null!, "Falta el filtro" };
            yield return new object[] { valido with { DesdeUtc = DateTime.SpecifyKind(Desde, DateTimeKind.Local) }, "UTC" };
            yield return new object[] { valido with { HastaUtc = DateTime.SpecifyKind(Hasta, DateTimeKind.Unspecified) }, "UTC" };
            yield return new object[] { valido with { HastaUtc = Desde }, "anterior a la final" };
            yield return new object[] { valido with { DesdeUtc = Hasta, HastaUtc = Desde }, "anterior a la final" };
            yield return new object[] { valido with { IdContenedor = 0 }, "positivos" };
            yield return new object[] { valido with { IdMaterial = -1 }, "positivos" };
            yield return new object[] { valido with { IdUsuario = 0 }, "positivos" };
            yield return new object[] { valido with { Verificacion = (EstadoVerificacion)9 }, "no es válido" };
            yield return new object[] { valido with { Criterio = (CriterioMaterial)0 }, "no es válido" };
            yield return new object[] { valido with { Criterio = CriterioMaterial.Verificado, Verificacion = EstadoVerificacion.SinVerificar }, "material declarado" };
        }

        [TestMethod]
        [DynamicData(nameof(FiltrosInvalidos))]
        public async Task Consultas_FiltroInvalido_RechazanDatosInvalidosSinConsultar(FiltroEstadisticas filtro, string fragmento)
        {
            Resultado[] resultados =
            new Resultado[]
    {
        await _servicio.ObtenerResumenAsync(Operario, filtro),
        await _servicio.AgruparDepositosAsync(Operario, filtro, AgrupacionDepositos.Dia),
        await _servicio.ListarFallosPorContenedorAsync(Operario, filtro),
        await _servicio.ListarDepositosAsync(Operario, filtro, 1, 20)
    };

            foreach (Resultado resultado in resultados)
            {
                Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
                StringAssert.Contains(resultado.Mensaje, fragmento);
            }

            Assert.AreEqual(0, _estadisticas.Llamadas);
        }

        [TestMethod]
        public async Task ObtenerResumenAsync_CriterioVerificadoConTodos_EsValido()
        {
            FiltroEstadisticas filtro = Filtro() with { Criterio = CriterioMaterial.Verificado, IdMaterial = 3 };

            Resultado<ResumenActividad> resultado = await _servicio.ObtenerResumenAsync(Admin, filtro);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
        }

        [TestMethod]
        [DataRow(AgrupacionDepositos.Material)]
        [DataRow(AgrupacionDepositos.Contenedor)]
        [DataRow(AgrupacionDepositos.Punto)]
        [DataRow(AgrupacionDepositos.Dia)]
        [DataRow(AgrupacionDepositos.Usuario)]
        public async Task AgruparDepositosAsync_AgrupacionValida_DelegaConLaMismaAgrupacion(AgrupacionDepositos agrupacion)
        {
            Resultado<IReadOnlyList<FilaAgrupada>> resultado = await _servicio.AgruparDepositosAsync(Operario, Filtro(), agrupacion);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.HasCount(1, resultado.Valor);
            Assert.AreEqual(agrupacion, _estadisticas.UltimaAgrupacion);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(6)]
        public async Task AgruparDepositosAsync_AgrupacionIndefinida_Rechaza(int valor)
        {
            Resultado<IReadOnlyList<FilaAgrupada>> resultado =
                await _servicio.AgruparDepositosAsync(Operario, Filtro(), (AgrupacionDepositos)valor);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            StringAssert.Contains(resultado.Mensaje, "agrupación");
            Assert.AreEqual(0, _estadisticas.Llamadas);
        }

        [TestMethod]
        public async Task ListarFallosPorContenedorAsync_Valido_DevuelveFilas()
        {
            Resultado<IReadOnlyList<FilaFallosContenedor>> resultado = await _servicio.ListarFallosPorContenedorAsync(Admin, Filtro());

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(4, resultado.Valor[0].Incidencias);
        }

        [TestMethod]
        [DataRow(1, 1)]
        [DataRow(3, ServicioEstadisticas.TamanoPaginaMaximo)]
        public async Task ListarDepositosAsync_PaginaValida_DelegaPaginaYTamano(int pagina, int tamano)
        {
            Resultado<PaginaDepositos> resultado = await _servicio.ListarDepositosAsync(Operario, Filtro(), pagina, tamano);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual((pagina, tamano), _estadisticas.UltimaPagina);
        }

        [TestMethod]
        public async Task ListarDepositosAsync_PaginaMayorQueElTotal_DevuelveFilasVaciasConElTotal()
        {
            for (int i = 1; i <= 5; i++)
            {
                _estadisticas.Depositos.Add(new FilaDeposito(i, Desde.AddHours(i), 7, "Caneca 7", Ciudadano, "PET", null, null, 0.1m));
            }

            Resultado<PaginaDepositos> ultima = await _servicio.ListarDepositosAsync(Operario, Filtro(), 3, 2);
            Resultado<PaginaDepositos> fuera = await _servicio.ListarDepositosAsync(Operario, Filtro(), 5, 2);

            Assert.AreEqual(5, ultima.Valor!.Filas.Single().IdDeposito);
            Assert.AreEqual(3, ultima.Valor.TotalPaginas);
            Assert.IsEmpty(fuera.Valor!.Filas);
            Assert.AreEqual(5, fuera.Valor.Total);
        }

        [TestMethod]
        public async Task ListarDepositosAsync_PaginaMaxima_SeAceptaSinLimiteSuperior()
        {
            Resultado<PaginaDepositos> resultado = await _servicio.ListarDepositosAsync(
                Operario, Filtro(), int.MaxValue, ServicioEstadisticas.TamanoPaginaMaximo);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsEmpty(resultado.Valor!.Filas);
        }

        [TestMethod]
        [DataRow(0, 20, "empieza en 1")]
        [DataRow(-2, 20, "empieza en 1")]
        [DataRow(1, 0, "entre 1 y 100")]
        [DataRow(1, 101, "entre 1 y 100")]
        public async Task ListarDepositosAsync_PaginaInvalida_Rechaza(int pagina, int tamano, string fragmento)
        {
            Resultado<PaginaDepositos> resultado = await _servicio.ListarDepositosAsync(Operario, Filtro(), pagina, tamano);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            StringAssert.Contains(resultado.Mensaje, fragmento);
            Assert.AreEqual(0, _estadisticas.Llamadas);
        }

        [TestMethod]
        public void ResumenDepositos_CalculaSinVerificarYDiscrepantes()
        {
            var resumen = new ResumenDepositos(10, 6, 4, 3, 1.2m, 5, 2.5m);

            Assert.AreEqual(4, resumen.SinVerificar);
            Assert.AreEqual(2, resumen.Discrepantes);
        }

        [TestMethod]
        public void ConteoOrdenes_CalculaSinAperturaConfirmada()
        {
            Assert.AreEqual(3, new ConteoOrdenes(8, 1, 4, 2, 1, 5, 4).SinAperturaConfirmada);
        }

        [TestMethod]
        [DataRow(0, 20, 0)]
        [DataRow(1, 20, 1)]
        [DataRow(20, 20, 1)]
        [DataRow(21, 20, 2)]
        public void PaginaDepositos_CalculaTotalPaginas(int total, int tamano, int esperado)
        {
            Assert.AreEqual(esperado, new PaginaDepositos(Array.Empty<FilaDeposito>(), total, 1, tamano).TotalPaginas);
        }
    }
}
