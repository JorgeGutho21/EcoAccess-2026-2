using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Datos.Oracle;
using Oracle.ManagedDataAccess.Client;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    /// <summary>
    /// Prueba las consultas agregadas contra Oracle real con un escenario propio. Todas las consultas filtran por la
    /// caneca del escenario, así que los datos de otras pruebas o de la demostración no alteran los conteos.
    /// </summary>
    /// <remarks>
    /// Escenario (fechas UTC; día de Colombia = UTC−5):
    /// orden 1 (día 1): apertura aplicada, detección con depósito de A con 0,125 kg medidos, cerrada y verificada como B;
    /// orden 2 (día 2): detección con depósito de B con 0,050 kg estimados, cerrada y sin verificar;
    /// orden 3 (día 3): vencida sin depósito; además una interacción del simulador rechazada, un evento huérfano en revisión
    /// y un fallo de la caneca.
    /// </remarks>
    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositorioEstadisticasOracleTests : BaseIntegracionOracle
    {
        private static readonly DateTime Fecha = new(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime Desde = Fecha.AddDays(-1);
        private static readonly DateTime Hasta = Fecha.AddDays(5);

        [TestMethod]
        public async Task Resumen_CuentaCadaCategoriaPorSeparado()
        {
            Escenario e = await PrepararAsync();

            ResumenActividad r = await e.Estadisticas.ObtenerResumenAsync(Filtro(e));

            Assert.AreEqual(new ConteoInteracciones(4, 0, 0, 2, 1, 1, 3, 0, 1), r.Interacciones);
            Assert.AreEqual(new ConteoOrdenes(3, 0, 2, 1, 0, 1, 2), r.Ordenes);
            Assert.AreEqual(2, r.Ordenes.SinAperturaConfirmada);
            Assert.AreEqual(new ConteoEventos(5, 3, 2, 1), r.Eventos);
            Assert.AreEqual(new ResumenDepositos(2, 1, 0, 1, 0.125m, 1, 0.050m), r.Depositos);
            Assert.AreEqual(1, r.Depositos.Discrepantes);
        }

        [TestMethod]
        public async Task Resumen_PeriodoParcial_FiltraCadaTablaPorSuFecha()
        {
            Escenario e = await PrepararAsync();

            ResumenActividad r = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { DesdeUtc = Fecha.AddDays(1) });

            Assert.AreEqual(3, r.Interacciones.Total);
            Assert.AreEqual(2, r.Ordenes.Total);
            Assert.AreEqual(1, r.Depositos.Total);
            Assert.AreEqual(0m, r.Depositos.KgMedidos);
            Assert.AreEqual(0.050m, r.Depositos.KgEstimados);
        }

        [TestMethod]
        public async Task Resumen_FiltrosDeVerificacionYMaterial_SoloAfectanALosDepositos()
        {
            Escenario e = await PrepararAsync();

            ResumenActividad sinVerificar = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { Verificacion = EstadoVerificacion.SinVerificar });
            ResumenActividad verificados = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { Verificacion = EstadoVerificacion.Verificado });
            ResumenActividad declaradoA = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { IdMaterial = e.MaterialA });
            ResumenActividad verificadoA = await e.Estadisticas.ObtenerResumenAsync(
                Filtro(e) with { IdMaterial = e.MaterialA, Criterio = CriterioMaterial.Verificado });

            Assert.AreEqual(1, sinVerificar.Depositos.Total);
            Assert.AreEqual(0.050m, sinVerificar.Depositos.KgEstimados);
            Assert.AreEqual(1, verificados.Depositos.Total);
            Assert.AreEqual(0.125m, verificados.Depositos.KgMedidos);
            Assert.AreEqual(1, declaradoA.Depositos.Total);
            Assert.AreEqual(0, verificadoA.Depositos.Total);
            Assert.AreEqual(4, declaradoA.Interacciones.Total);
            Assert.AreEqual(3, declaradoA.Ordenes.Total);
        }

        [TestMethod]
        public async Task Resumen_FiltroPorUsuario_DejaFueraLosEventosSinOrden()
        {
            Escenario e = await PrepararAsync();

            ResumenActividad r = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { IdUsuario = e.Ciudadano });

            Assert.AreEqual(4, r.Interacciones.Total);
            Assert.AreEqual(3, r.Eventos.Total);
            Assert.AreEqual(0, r.Eventos.EnRevision);
            Assert.AreEqual(2, r.Depositos.Total);
        }

        [TestMethod]
        public async Task Agrupar_PorMaterial_SegunElCriterio()
        {
            Escenario e = await PrepararAsync();

            IReadOnlyList<FilaAgrupada> declarado = await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Material);
            IReadOnlyList<FilaAgrupada> verificado = await e.Estadisticas.AgruparDepositosAsync(
                Filtro(e) with { Criterio = CriterioMaterial.Verificado }, AgrupacionDepositos.Material);

            Assert.HasCount(2, declarado);
            Assert.AreEqual(new FilaAgrupada(e.MaterialA, null, "Material IT A", 1, 1, 1, 0.125m, 0, 0m), declarado[0]);
            Assert.AreEqual(new FilaAgrupada(e.MaterialB, null, "Material IT B", 1, 0, 0, 0m, 1, 0.050m), declarado[1]);
            Assert.HasCount(1, verificado);
            Assert.AreEqual(e.MaterialB, verificado[0].Id);
            Assert.AreEqual(0.125m, verificado[0].KgMedidos);
        }

        [TestMethod]
        public async Task Agrupar_PorContenedorPuntoYUsuario_UnaFilaConTodo()
        {
            Escenario e = await PrepararAsync();

            FilaAgrupada caneca = (await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Contenedor)).Single();
            FilaAgrupada punto = (await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Punto)).Single();
            FilaAgrupada usuario = (await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Usuario)).Single();

            Assert.AreEqual(new FilaAgrupada(e.Caneca, null, e.NombreCaneca, 2, 1, 1, 0.125m, 1, 0.050m), caneca);
            Assert.AreEqual(new FilaAgrupada(e.Punto, null, e.NombrePunto, 2, 1, 1, 0.125m, 1, 0.050m), punto);
            Assert.AreEqual(e.Ciudadano, usuario.Id);
            Assert.AreEqual($"Usuario #{e.Ciudadano}", usuario.Etiqueta);
            Assert.AreEqual(2, usuario.Depositos);
        }

        [TestMethod]
        public async Task Agrupar_PorDia_UsaElDiaDeColombiaEnOrdenAscendente()
        {
            Escenario e = await PrepararAsync();

            IReadOnlyList<FilaAgrupada> dias = await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Dia);

            Assert.HasCount(2, dias);
            Assert.AreEqual(new DateOnly(2026, 3, 1), dias[0].Dia);
            Assert.AreEqual("2026-03-01", dias[0].Etiqueta);
            Assert.IsNull(dias[0].Id);
            Assert.AreEqual(new DateOnly(2026, 3, 2), dias[1].Dia);
        }

        [TestMethod]
        public async Task Agrupar_DepositoA_LasCuatroYMediaDeLaMadrugadaUtc_CuentaParaElDiaAnteriorEnColombia()
        {
            Escenario e = await PrepararAsync();
            // 2026-03-04 04:30 UTC es 2026-03-03 23:30 en Colombia.
            await DepositarAsync(e, new DateTime(2026, 3, 4, 4, 28, 0, DateTimeKind.Utc), e.MaterialA, null, 0.010m);

            IReadOnlyList<FilaAgrupada> dias = await e.Estadisticas.AgruparDepositosAsync(Filtro(e), AgrupacionDepositos.Dia);

            Assert.HasCount(3, dias);
            Assert.AreEqual(new DateOnly(2026, 3, 3), dias[2].Dia);
        }

        [TestMethod]
        public async Task Fallos_CuentaCadaIncidenciaDeLaCanecaAparte()
        {
            Escenario e = await PrepararAsync();

            IReadOnlyList<FilaFallosContenedor> filas = await e.Estadisticas.ListarFallosPorContenedorAsync(Filtro(e));

            Assert.HasCount(1, filas);
            Assert.AreEqual(new FilaFallosContenedor(e.Caneca, e.NombreCaneca, 1, 1, 0, 1), filas[0]);
            Assert.AreEqual(3, filas[0].Incidencias);
        }

        [TestMethod]
        public async Task Fallos_PeriodoSinIncidencias_NoDevuelveLaCaneca()
        {
            Escenario e = await PrepararAsync();

            IReadOnlyList<FilaFallosContenedor> filas = await e.Estadisticas.ListarFallosPorContenedorAsync(
                Filtro(e) with { DesdeUtc = Fecha.AddDays(10), HastaUtc = Fecha.AddDays(11) });

            Assert.IsEmpty(filas);
        }

        [TestMethod]
        public async Task Detalle_PaginaDelMasRecienteAlMasAntiguoConNombres()
        {
            Escenario e = await PrepararAsync();

            PaginaDepositos primera = await e.Estadisticas.ListarDepositosAsync(Filtro(e), 1, 1);
            PaginaDepositos segunda = await e.Estadisticas.ListarDepositosAsync(Filtro(e), 2, 1);
            PaginaDepositos tercera = await e.Estadisticas.ListarDepositosAsync(Filtro(e), 3, 1);

            Assert.AreEqual(2, primera.Total);
            Assert.AreEqual(2, primera.TotalPaginas);
            FilaDeposito reciente = primera.Filas.Single();
            Assert.AreEqual(Fecha.AddDays(1).AddMinutes(2), reciente.FechaDeteccionUtc);
            Assert.AreEqual(DateTimeKind.Utc, reciente.FechaDeteccionUtc.Kind);
            Assert.AreEqual(e.NombreCaneca, reciente.Contenedor);
            Assert.AreEqual(e.Ciudadano, reciente.IdUsuario);
            Assert.AreEqual("Material IT B", reciente.MaterialDeclarado);
            Assert.IsNull(reciente.MaterialVerificado);
            Assert.IsNull(reciente.PesoMedidoKg);
            Assert.AreEqual(0.050m, reciente.PesoEstimadoKg);
            FilaDeposito antiguo = segunda.Filas.Single();
            Assert.AreEqual("Material IT A", antiguo.MaterialDeclarado);
            Assert.AreEqual("Material IT B", antiguo.MaterialVerificado);
            Assert.AreEqual(0.125m, antiguo.PesoMedidoKg);
            Assert.IsEmpty(tercera.Filas);
            Assert.AreEqual(2, tercera.Total);
        }

        [TestMethod]
        public async Task Detalle_FiltroSinVerificar_SoloDevuelveLosPendientes()
        {
            Escenario e = await PrepararAsync();

            PaginaDepositos pagina = await e.Estadisticas.ListarDepositosAsync(
                Filtro(e) with { Verificacion = EstadoVerificacion.SinVerificar }, 1, ServicioEstadisticas.TamanoPaginaMaximo);

            Assert.AreEqual(1, pagina.Total);
            Assert.IsNull(pagina.Filas.Single().MaterialVerificado);
        }

        [TestMethod]
        public async Task Detalle_PaginaMuyAlta_DevuelveVaciaSinDesbordarElSalto()
        {
            Escenario e = await PrepararAsync();

            // (int.MaxValue − 1) × 100 no cabe en un int: el salto debe calcularse en long.
            PaginaDepositos pagina = await e.Estadisticas.ListarDepositosAsync(Filtro(e), int.MaxValue, ServicioEstadisticas.TamanoPaginaMaximo);

            Assert.IsEmpty(pagina.Filas);
            Assert.AreEqual(2, pagina.Total);
        }

        [TestMethod]
        public async Task Detalle_FiltroPorUsuario_SoloDevuelveLosDepositosDeEseCiudadano()
        {
            Escenario e = await PrepararAsync();

            PaginaDepositos delCiudadano = await e.Estadisticas.ListarDepositosAsync(Filtro(e) with { IdUsuario = e.Ciudadano }, 1, 10);
            PaginaDepositos delOperario = await e.Estadisticas.ListarDepositosAsync(Filtro(e) with { IdUsuario = e.Operario }, 1, 10);

            Assert.AreEqual(2, delCiudadano.Total);
            Assert.IsTrue(delCiudadano.Filas.All(f => f.IdUsuario == e.Ciudadano));
            Assert.AreEqual(0, delOperario.Total);
        }

        [TestMethod]
        public async Task Resumen_HastaExacto_QuedaFueraDelPeriodo()
        {
            Escenario e = await PrepararAsync();
            DateTime segundaDeteccion = Fecha.AddDays(1).AddMinutes(2);

            ResumenActividad hastaExacto = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { HastaUtc = segundaDeteccion });
            ResumenActividad unTickDespues = await e.Estadisticas.ObtenerResumenAsync(Filtro(e) with { HastaUtc = segundaDeteccion.AddTicks(1) });

            Assert.AreEqual(1, hastaExacto.Depositos.Total);
            Assert.AreEqual(2, unTickDespues.Depositos.Total);
        }

        [TestMethod]
        public async Task Resumen_OrdenConDosAperturasAplicadas_CuentaUnaSolaVez()
        {
            Escenario e = await PrepararAsync();
            PaginaDepositos detalle = await e.Estadisticas.ListarDepositosAsync(Filtro(e), 2, 1);
            int idOrden = (await e.Interacciones.ObtenerDepositoAsync(detalle.Filas.Single().IdDeposito))!.IdOrden;

            // Una segunda apertura aplicada de la misma orden no debe duplicar la orden ni su depósito en el resumen.
            await e.Interacciones.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, "ape2-" + Sufijo(), TipoEventoDispositivo.Apertura, idOrden, ResultadoEvento.Aplicado, null, Fecha.AddMinutes(3)),
                null, null);
            ResumenActividad r = await e.Estadisticas.ObtenerResumenAsync(Filtro(e));

            Assert.AreEqual(3, r.Ordenes.Total);
            Assert.AreEqual(1, r.Ordenes.ConAperturaConfirmada);
            Assert.AreEqual(2, r.Ordenes.ConDeposito);
            Assert.AreEqual(6, r.Eventos.Total);
        }

        private static FiltroEstadisticas Filtro(Escenario e) => new(Desde, Hasta, IdContenedor: e.Caneca);

        [TestMethod]
        public async Task InformeInstitucional_CanecasHomonimasConservanSuIdentidadYConteosPositivos()
        {
            if (ConservarDemo || PdbEsperado != "PRUEBAP3")
                Assert.Inconclusive("Ensayo temporal exclusivo de PRUEBAP3, sin conservar fixtures de informe.");
            Escenario a = await PrepararAsync();
            Escenario b = await PrepararAsync();
            string nombre = "Caneca de informe " + Sufijo();
            using (var c = await Fabrica.AbrirAsync(CancellationToken.None))
            {
                // Solo IDs propios del ensayo; BaseIntegracionOracle los elimina al finalizar.
                foreach (int id in new[] { a.MaterialA, a.MaterialB, b.MaterialA, b.MaterialB })
                {
                    using var cmd = ComandosOracle.Crear(c, "UPDATE material SET codigo=:codigo WHERE id_material=:id");
                    cmd.Agregar("codigo", OracleDbType.Varchar2, "RPT_" + id);
                    cmd.Agregar("id", OracleDbType.Int32, id);
                    await cmd.ExecuteNonQueryAsync();
                }
                foreach (int id in new[] { a.Caneca, b.Caneca })
                {
                    using var cmd = ComandosOracle.Crear(c, "UPDATE contenedor SET nombre=:nombre WHERE id_contenedor=:id");
                    cmd.Agregar("nombre", OracleDbType.Varchar2, nombre);
                    cmd.Agregar("id", OracleDbType.Int32, id);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            var repo = new RepositorioInformeInstitucionalOracle(Fabrica);
            var filtro = new FiltroInforme(HoraColombia.InicioDelDiaUtc(new(2026, 2, 28)), HoraColombia.InicioDelDiaUtc(new(2026, 3, 7)));
            var filas = (await repo.AgruparAsync(filtro, AgrupacionDepositos.Contenedor)).Where(f => f.Etiqueta == nombre).ToArray();
            Assert.HasCount(2, filas);
            CollectionAssert.AreEquivalent(new int?[] { a.Caneca, b.Caneca }, filas.Select(f => f.Id).ToArray());
            Assert.IsTrue(filas.All(f => f.Depositos == 2 && f.Verificados == 1));
            var indicadores = await repo.ConsultarAsync(filtro with { IdContenedor = a.Caneca });
            Assert.AreEqual(2L, indicadores.Depositos); Assert.AreEqual(1L, indicadores.Verificados);
            Assert.AreEqual(1L, indicadores.Ciudadanos);
            var ubicacion = (await repo.AgruparAsync(filtro with { IdContenedor = a.Caneca }, AgrupacionDepositos.Punto)).Single();
            Assert.AreEqual(a.Punto, ubicacion.Id); Assert.AreEqual(2, ubicacion.Depositos);
        }

        private sealed record Escenario(
            RepositorioEstadisticasOracle Estadisticas,
            RepositorioInteraccionesOracle Interacciones,
            int Caneca,
            string NombreCaneca,
            int Punto,
            string NombrePunto,
            int MaterialA,
            int MaterialB,
            int Ciudadano,
            int Operario, RelojOracleAjustable Reloj);

        private async Task<Escenario> PrepararAsync()
        {
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            var usuarios = new RepositorioUsuariosOracle(Fabrica);
            string sufijo = Sufijo();
            int a = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_A" + sufijo, "Material IT A", CategoriaMaterial.Aprovechable, null)));
            int b = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_B" + sufijo, "Material IT B", CategoriaMaterial.Aprovechable, null)));
            string nombrePunto = "Punto IT " + sufijo;
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion(nombrePunto, null)));
            string nombreCaneca = "Caneca IT " + sufijo;
            var nueva = new Contenedor(punto, nombreCaneca, new GeneradorIdentificadorQrAleatorio().Generar(), Fecha);
            nueva.AdmitirMaterial(a);
            nueva.AdmitirMaterial(b);
            int caneca = RegistrarContenedor(await contenedores.AgregarAsync(nueva));
            await contenedores.CambiarEstadoAsync(caneca, EstadoContenedor.EnServicio);
            var cuenta = new Usuario("Ciudadano IT", $"it.c{sufijo.ToLowerInvariant()}@ejemplo.com", CodigoRol.Ciudadano, Fecha);
            cuenta.RegistrarConsentimiento("prueba-integracion", Fecha);
            int ciudadano = RegistrarUsuario(await usuarios.AgregarAsync(cuenta));
            int operario = RegistrarUsuario(await usuarios.AgregarAsync(
                new Usuario("Operario IT", $"it.o{sufijo.ToLowerInvariant()}@ejemplo.com", CodigoRol.Operario, Fecha)));
            var reloj = new RelojOracleAjustable(Fecha);
            var e = new Escenario(new RepositorioEstadisticasOracle(Fabrica), new RepositorioInteraccionesOracle(Fabrica, reloj),
                caneca, nombreCaneca, punto, nombrePunto, a, b, ciudadano, operario, reloj);

            // Orden 1: apertura confirmada por el sensor, depósito pesado y verificado con otro material.
            Deposito primero = await DepositarAsync(e, Fecha, a, 0.125m, null, conApertura: true);
            primero.Verificar(b, operario, Fecha.AddMinutes(10));
            await e.Interacciones.VerificarDepositoAsync(primero);

            // Orden 2: depósito sin báscula (peso estimado), sin verificar.
            await DepositarAsync(e, Fecha.AddDays(1), b, null, 0.050m);

            // Orden 3: vence sin depósito.
            DateTime dia3 = Fecha.AddDays(2);
            OrdenApertura vencida = await EmitirAsync(e, dia3, a);
            Interaccion sinDeposito = (await e.Interacciones.ObtenerInteraccionAsync(vencida.IdInteraccion))!;
            vencida.Vencer(vencida.FechaVencimientoUtc);
            sinDeposito.CerrarSinDeposito(vencida.FechaVencimientoUtc);
            await e.Interacciones.ActualizarOrdenAsync(new CambioOrden(vencida, EstadoOrdenApertura.Emitida, sinDeposito));

            // Interacción del simulador rechazada: no llega a orden.
            int idRechazada = await e.Interacciones.AgregarInteraccionAsync(new Interaccion(caneca, ciudadano, CanalInteraccion.Simulador, dia3.AddMinutes(10)));
            Interaccion rechazada = (await e.Interacciones.ObtenerInteraccionAsync(idRechazada))!;
            rechazada.Rechazar("Material no admitido", dia3.AddMinutes(11));
            await e.Interacciones.ActualizarInteraccionAsync(rechazada, EstadoInteraccion.Iniciada);

            // Evento sin orden que queda en revisión y fallo reportado por la caneca.
            await e.Interacciones.RegistrarEventoAsync(
                new EventoDispositivo(caneca, "huerfano-" + Sufijo(), TipoEventoDispositivo.Deteccion, null, ResultadoEvento.Revision, null, dia3.AddMinutes(20)),
                null, null);
            await e.Interacciones.RegistrarEventoAsync(
                new EventoDispositivo(caneca, "fallo-" + Sufijo(), TipoEventoDispositivo.Fallo, null, ResultadoEvento.Revision, null, dia3.AddMinutes(21)),
                null, null);
            return e;
        }

        /// <summary>Emite una orden, registra la detección con su depósito y cierra la orden; devuelve el depósito guardado.</summary>
        private static async Task<Deposito> DepositarAsync(
            Escenario e, DateTime inicio, int idMaterial, decimal? pesoMedido, decimal? pesoEstimado, bool conApertura = false)
        {
            OrdenApertura orden = await EmitirAsync(e, inicio, idMaterial);
            if (conApertura)
            {
                orden.RegistrarApertura();
                await e.Interacciones.RegistrarEventoAsync(
                    new EventoDispositivo(e.Caneca, "ape-" + Sufijo(), TipoEventoDispositivo.Apertura, orden.Id, ResultadoEvento.Aplicado, null, inicio.AddMinutes(1)),
                    new CambioOrden(orden, EstadoOrdenApertura.Emitida), null);
            }

            EstadoOrdenApertura anterior = orden.Estado;
            DateTime deteccion = inicio.AddMinutes(2);
            orden.RegistrarDeteccion();
            await e.Interacciones.RegistrarEventoAsync(
                new EventoDispositivo(e.Caneca, "det-" + Sufijo(), TipoEventoDispositivo.Deteccion, orden.Id, ResultadoEvento.Aplicado, deteccion, deteccion),
                new CambioOrden(orden, anterior),
                new Deposito(orden.Id, idMaterial, pesoMedido, pesoEstimado, deteccion));

            Interaccion interaccion = (await e.Interacciones.ObtenerInteraccionAsync(orden.IdInteraccion))!;
            orden.Cerrar(deteccion);
            interaccion.CerrarConDeposito(deteccion);
            await e.Interacciones.ActualizarOrdenAsync(new CambioOrden(orden, EstadoOrdenApertura.Detectada, interaccion));
            return (await e.Interacciones.ObtenerDepositoPorOrdenAsync(orden.Id))!;
        }

        private static async Task<OrdenApertura> EmitirAsync(Escenario e, DateTime inicio, int idMaterial)
        {
            e.Reloj.Ahora = inicio.AddSeconds(30);
            int id = await e.Interacciones.AgregarInteraccionAsync(new Interaccion(e.Caneca, e.Ciudadano, CanalInteraccion.Telegram, inicio));
            Interaccion interaccion = (await e.Interacciones.ObtenerInteraccionAsync(id))!;
            interaccion.Autorizar(idMaterial);
            var orden = new OrdenApertura(id, e.Caneca, "it-" + Guid.NewGuid().ToString("N"), inicio.AddSeconds(30), inicio.AddMinutes(3));
            int idOrden = await e.Interacciones.EmitirOrdenAsync(interaccion, orden);
            return (await e.Interacciones.ObtenerOrdenAsync(idOrden))!;
        }
    }
}
