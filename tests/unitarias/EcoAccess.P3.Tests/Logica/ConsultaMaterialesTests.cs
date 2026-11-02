using System;
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
    public sealed class ConsultaMaterialesTests
    {
        [TestMethod]
        public async Task ConsultaAdministrativaRevalidaPermisoSinRecortarCatalogoPublico()
        {
            var usuarios = new RepositorioUsuariosEnMemoria();
            usuarios.Agregar(1, true, CodigoRol.Administrador, CodigoRol.Ciudadano);
            var materiales = new RepositorioMaterialesEnMemoria();
            for (int i = 0; i < 31; i++) materiales.Sembrar("MAT" + i.ToString("D2"));
            var servicio = new ServicioMateriales(usuarios, materiales, new RepositorioContenedoresEnMemoria());
            var pagina = await servicio.ConsultarPaginaAsync(1, new());
            Assert.HasCount(25, pagina.Valor.Filas);
            Assert.IsTrue(pagina.Valor.HaySiguiente);
            Assert.HasCount(31, await servicio.ListarAsync(true), "El bot y las reglas de admisión necesitan el catálogo completo.");
            usuarios.CambiarGuardado(1, u => u.QuitarRol(CodigoRol.Administrador));
            Assert.AreEqual(MotivoRechazo.SinPermiso, (await servicio.ConsultarPaginaAsync(1, new())).Motivo);
            Assert.AreEqual(1, materiales.ConsultasPagina);
            Assert.HasCount(31, await servicio.ListarAsync(true), "La orientación pública sigue disponible.");
        }

        [TestMethod]
        public async Task CancelacionPreviaNoConsultaRepositorio()
        {
            var materiales = new RepositorioMaterialesEnMemoria();
            var servicio = new ServicioMateriales(new RepositorioUsuariosEnMemoria(), materiales, new RepositorioContenedoresEnMemoria());
            using var cancelar = new CancellationTokenSource();
            cancelar.Cancel();
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => servicio.ConsultarPaginaAsync(1, new(), cancelar.Token));
            Assert.AreEqual(0, materiales.ConsultasPagina);
        }

        [TestMethod]
        public void FiltroValidaLimitesCategoriaYDesplazamientoSinDesbordar()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroMateriales(pagina: 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroMateriales(tamanoPagina: 0));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new FiltroMateriales(tamanoPagina: 101));
            Assert.ThrowsExactly<ArgumentException>(() => new FiltroMateriales(categoria: (CategoriaMaterial)9));
            Assert.ThrowsExactly<ArgumentException>(() => new FiltroMateriales(new string('x', 101)));
            var filtro = new FiltroMateriales("  PET_%  ", pagina: int.MaxValue, tamanoPagina: 100);
            Assert.AreEqual("pet_%", filtro.Texto);
            Assert.AreEqual(214748364600L, filtro.Desplazamiento);
        }
    }
}
