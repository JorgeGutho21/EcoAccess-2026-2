using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class RepositoriosCatalogoOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task Material_AgregarObtenerDeshabilitar_ConservaLosDatos()
        {
            var repositorio = new RepositorioMaterialesOracle(Fabrica);
            string codigo = "IT_" + Sufijo();

            int id = RegistrarMaterial(await repositorio.AgregarAsync(
                new Material(codigo, "Material de integración", CategoriaMaterial.Aprovechable, "Descripción con tilde")));
            await repositorio.DeshabilitarAsync(id);
            Material releido = (await repositorio.ObtenerPorIdAsync(id))!;

            Assert.AreEqual(codigo, releido.Codigo);
            Assert.AreEqual("Descripción con tilde", releido.Descripcion);
            Assert.AreEqual(CategoriaMaterial.Aprovechable, releido.Categoria);
            Assert.IsFalse(releido.Habilitado);
            Assert.IsTrue(await repositorio.ExisteCodigoAsync(codigo));
            Assert.IsFalse((await repositorio.ListarAsync(soloHabilitados: true)).Any(m => m.Id == id));

            await repositorio.HabilitarAsync(id);
            Assert.IsTrue((await repositorio.ObtenerPorIdAsync(id))!.Habilitado);
        }

        [TestMethod]
        public async Task Material_CodigoRepetido_LanzaConflictoConNombreDeRestriccion()
        {
            var repositorio = new RepositorioMaterialesOracle(Fabrica);
            string codigo = "IT_" + Sufijo();
            RegistrarMaterial(await repositorio.AgregarAsync(new Material(codigo, "Primero", CategoriaMaterial.Aprovechable, null)));

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() =>
                repositorio.AgregarAsync(new Material(codigo, "Segundo", CategoriaMaterial.Aprovechable, null)));

            StringAssert.Contains(error.Message, "uq_material_codigo");
        }

        [TestMethod]
        public async Task Punto_AgregarYDesactivar_ConservaLosDatos()
        {
            var repositorio = new RepositorioPuntosInstalacionOracle(Fabrica);
            string nombre = "Punto IT " + Sufijo();

            int id = RegistrarPunto(await repositorio.AgregarAsync(new PuntoInstalacion(nombre, "Pasillo norte")));
            await repositorio.CambiarActivoAsync(id, false);
            PuntoInstalacion releido = (await repositorio.ObtenerPorIdAsync(id))!;

            Assert.AreEqual(nombre, releido.Nombre);
            Assert.AreEqual("Pasillo norte", releido.ReferenciaUbicacion);
            Assert.IsFalse(releido.Activo);
            Assert.IsTrue(await repositorio.ExisteNombreAsync(nombre));
        }

        [TestMethod]
        public async Task Contenedor_EscriturasPuntuales_GuardanMaterialesYEstado()
        {
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            string sufijo = Sufijo();
            int a = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_A" + sufijo, "Material IT A", CategoriaMaterial.Aprovechable, null)));
            int b = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_B" + sufijo, "Material IT B", CategoriaMaterial.Aprovechable, null)));
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("Punto IT " + sufijo, null)));
            var fecha = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
            var nueva = new Contenedor(punto, "Caneca IT", new GeneradorIdentificadorQrAleatorio().Generar(), fecha);
            nueva.AdmitirMaterial(a);

            int id = RegistrarContenedor(await contenedores.AgregarAsync(nueva));
            await contenedores.AdmitirMaterialAsync(id, b);
            await contenedores.AdmitirMaterialAsync(id, b);
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio);
            await contenedores.QuitarMaterialAsync(id, a);
            Contenedor releida = (await contenedores.ObtenerPorIdAsync(id))!;

            Assert.AreEqual(EstadoContenedor.EnServicio, releida.Estado);
            CollectionAssert.AreEquivalent(new[] { b }, releida.MaterialesAdmitidos.ToArray());
            Assert.AreEqual(fecha, releida.FechaAltaUtc);
            Assert.AreEqual(DateTimeKind.Utc, releida.FechaAltaUtc.Kind);
            Assert.IsTrue((await contenedores.ListarPorMaterialAsync(b)).Any(c => c.Id == id));
            Assert.IsFalse((await contenedores.ListarPorMaterialAsync(a)).Any(c => c.Id == id));
            Assert.HasCount(1, await contenedores.ListarPorPuntoAsync(punto));
        }

        [TestMethod]
        public async Task Contenedor_QrRepetido_LanzaConflictoConNombreDeRestriccion()
        {
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("Punto IT " + Sufijo(), null)));
            string qr = new GeneradorIdentificadorQrAleatorio().Generar();
            var fecha = new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc);
            RegistrarContenedor(await contenedores.AgregarAsync(new Contenedor(punto, "Caneca 1", qr, fecha)));

            var error = await Assert.ThrowsExactlyAsync<ConflictoUnicidadException>(() =>
                contenedores.AgregarAsync(new Contenedor(punto, "Caneca 2", qr, fecha)));

            StringAssert.Contains(error.Message, "uq_contenedor_qr");
        }

        [TestMethod]
        public async Task Contenedor_ObtenerPorIdentificadorQr_DevuelveLaCanecaConSusMateriales()
        {
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            string sufijo = Sufijo();
            int a = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_Q" + sufijo, "Material IT Q", CategoriaMaterial.Aprovechable, null)));
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("Punto IT " + sufijo, null)));
            string qr = new GeneradorIdentificadorQrAleatorio().Generar();
            var nueva = new Contenedor(punto, "Caneca QR", qr, new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc));
            nueva.AdmitirMaterial(a);
            int id = RegistrarContenedor(await contenedores.AgregarAsync(nueva));

            Contenedor encontrada = (await contenedores.ObtenerPorIdentificadorQrAsync(qr))!;

            Assert.AreEqual(id, encontrada.Id);
            CollectionAssert.AreEqual(new[] { a }, encontrada.MaterialesAdmitidos.ToArray());
            Assert.IsNull(await contenedores.ObtenerPorIdentificadorQrAsync(new GeneradorIdentificadorQrAleatorio().Generar()));
        }

        /// <summary>Hallazgo del PR #1: dos deshabilitaciones no pueden dejar una caneca en servicio sin material.</summary>
        [TestMethod]
        public async Task Material_DeshabilitarUnicoHabilitadoDeCanecaEnServicio_RevierteYLanza()
        {
            (RepositorioMaterialesOracle materiales, _, int a, int b, int id) = await CanecaEnServicioConDosMaterialesAsync();
            await materiales.DeshabilitarAsync(a);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => materiales.DeshabilitarAsync(b));

            StringAssert.Contains(error.Message, "Caneca IT");
            Assert.IsTrue((await materiales.ObtenerPorIdAsync(b))!.Habilitado, "La transacción debió revertirse.");
        }

        /// <summary>
        /// Otra sesión retiene la caneca a medio deshabilitar B: deshabilitar A espera el bloqueo y, cuando la otra
        /// confirma, ve B deshabilitado y se rechaza. Determinista: no depende de que dos tareas coincidan por azar.
        /// </summary>
        [TestMethod]
        public async Task Material_DeshabilitarMientrasOtraSesionDeshabilitaElOtro_EsperaYRechaza()
        {
            (RepositorioMaterialesOracle materiales, _, int a, int b, int id) = await CanecaEnServicioConDosMaterialesAsync();
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_contenedor FROM contenedor WHERE id_contenedor = :id FOR UPDATE", id);
            await otra.EjecutarAsync("UPDATE material SET habilitado = 0 WHERE id_material = :id", b);

            Task operacion = materiales.DeshabilitarAsync(a);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(operacion);
            await otra.ConfirmarAsync();

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => operacion);
            Assert.IsTrue((await materiales.ObtenerPorIdAsync(a))!.Habilitado, "Debe quedar un material habilitado.");
        }

        /// <summary>Si la otra sesión no suelta el material en 10 s, el error de espera (ORA-00054 en 26ai) se traduce y nada cambia.</summary>
        [TestMethod]
        public async Task Material_HabilitarConFilaRetenidaMasDeDiezSegundos_LanzaCambioConcurrente()
        {
            var repositorio = new RepositorioMaterialesOracle(Fabrica);
            int id = RegistrarMaterial(await repositorio.AgregarAsync(
                new Material("IT_" + Sufijo(), "Material IT", CategoriaMaterial.Aprovechable, null)));
            await repositorio.DeshabilitarAsync(id);
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("SELECT id_material FROM material WHERE id_material = :id FOR UPDATE", id);

            var error = await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => repositorio.HabilitarAsync(id));

            Assert.AreEqual(TraductorErroresOracle.MensajeEsperaBloqueo, error.Message);
            otra.Dispose();
            Assert.IsFalse((await repositorio.ObtenerPorIdAsync(id))!.Habilitado);
        }

        [TestMethod]
        public async Task Contenedor_QuitarUltimoHabilitadoEnServicio_RevierteYLanza()
        {
            (RepositorioMaterialesOracle materiales, RepositorioContenedoresOracle contenedores, int a, int b, int id) =
                await CanecaEnServicioConDosMaterialesAsync();
            await materiales.DeshabilitarAsync(a);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => contenedores.QuitarMaterialAsync(id, b));

            CollectionAssert.AreEquivalent(new[] { a, b }, (await contenedores.ObtenerPorIdAsync(id))!.MaterialesAdmitidos.ToArray());
        }

        [TestMethod]
        public async Task Contenedor_PonerEnServicioSinHabilitados_LanzaYNoCambiaEstado()
        {
            (RepositorioMaterialesOracle materiales, RepositorioContenedoresOracle contenedores, int a, int b, int id) =
                await CanecaEnServicioConDosMaterialesAsync();
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.FueraDeServicio);
            await materiales.DeshabilitarAsync(a);
            await materiales.DeshabilitarAsync(b);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => contenedores.AdmitirMaterialAsync(id, a));

            Assert.AreEqual(EstadoContenedor.FueraDeServicio, (await contenedores.ObtenerPorIdAsync(id))!.Estado);
        }

        [TestMethod]
        public async Task Contenedor_DadaDeBaja_NoVuelveNiAdmiteCambios()
        {
            (_, RepositorioContenedoresOracle contenedores, int a, _, int id) = await CanecaEnServicioConDosMaterialesAsync();
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.DadoDeBaja);
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.DadoDeBaja);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                contenedores.CambiarEstadoAsync(id, EstadoContenedor.FueraDeServicio));
            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => contenedores.QuitarMaterialAsync(id, a));

            Assert.AreEqual(EstadoContenedor.DadoDeBaja, (await contenedores.ObtenerPorIdAsync(id))!.Estado);
        }

        [TestMethod]
        public async Task Punto_DesactivarConCanecaEnServicio_RevierteYLanza()
        {
            (_, RepositorioContenedoresOracle contenedores, _, _, int id) = await CanecaEnServicioConDosMaterialesAsync();
            int punto = (await contenedores.ObtenerPorIdAsync(id))!.IdPuntoInstalacion;
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => puntos.CambiarActivoAsync(punto, false));

            Assert.IsTrue((await puntos.ObtenerPorIdAsync(punto))!.Activo, "La transacción debió revertirse.");
        }

        [TestMethod]
        public async Task Contenedor_PonerEnServicioConPuntoInactivo_LanzaYNoCambiaEstado()
        {
            (_, RepositorioContenedoresOracle contenedores, _, _, int id) = await CanecaEnServicioConDosMaterialesAsync();
            int punto = (await contenedores.ObtenerPorIdAsync(id))!.IdPuntoInstalacion;
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.FueraDeServicio);
            await puntos.CambiarActivoAsync(punto, false);

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() =>
                contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio));

            Assert.AreEqual(EstadoContenedor.FueraDeServicio, (await contenedores.ObtenerPorIdAsync(id))!.Estado);
        }

        /// <summary>
        /// Otra sesión desactiva el punto sin confirmar: poner la caneca en servicio espera el bloqueo del punto y,
        /// tras la confirmación, lo ve inactivo y se rechaza.
        /// </summary>
        [TestMethod]
        public async Task Contenedor_PonerEnServicioMientrasOtraSesionDesactivaElPunto_EsperaYRechaza()
        {
            (_, RepositorioContenedoresOracle contenedores, _, _, int id) = await CanecaEnServicioConDosMaterialesAsync();
            int punto = (await contenedores.ObtenerPorIdAsync(id))!.IdPuntoInstalacion;
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.FueraDeServicio);
            using SesionAjenaOracle otra = await SesionAjenaOracle.AbrirAsync(Fabrica);
            await otra.EjecutarAsync("UPDATE punto_instalacion SET activo = 0 WHERE id_punto_instalacion = :id", punto);

            Task operacion = contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio);
            await SesionAjenaOracle.AfirmarQueEsperaAsync(operacion);
            await otra.ConfirmarAsync();

            await Assert.ThrowsExactlyAsync<CambioConcurrenteException>(() => operacion);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, (await contenedores.ObtenerPorIdAsync(id))!.Estado);
        }

        private async Task<(RepositorioMaterialesOracle Materiales, RepositorioContenedoresOracle Contenedores, int A, int B, int Id)>
            CanecaEnServicioConDosMaterialesAsync()
        {
            var materiales = new RepositorioMaterialesOracle(Fabrica);
            var puntos = new RepositorioPuntosInstalacionOracle(Fabrica);
            var contenedores = new RepositorioContenedoresOracle(Fabrica);
            string sufijo = Sufijo();
            int a = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_A" + sufijo, "Material IT A", CategoriaMaterial.Aprovechable, null)));
            int b = RegistrarMaterial(await materiales.AgregarAsync(new Material("IT_B" + sufijo, "Material IT B", CategoriaMaterial.Aprovechable, null)));
            int punto = RegistrarPunto(await puntos.AgregarAsync(new PuntoInstalacion("Punto IT " + sufijo, null)));
            var nueva = new Contenedor(punto, "Caneca IT " + sufijo, new GeneradorIdentificadorQrAleatorio().Generar(),
                new DateTime(2026, 3, 1, 8, 30, 0, DateTimeKind.Utc));
            nueva.AdmitirMaterial(a);
            nueva.AdmitirMaterial(b);
            int id = RegistrarContenedor(await contenedores.AgregarAsync(nueva));
            await contenedores.CambiarEstadoAsync(id, EstadoContenedor.EnServicio);
            return (materiales, contenedores, a, b, id);
        }
    }
}
