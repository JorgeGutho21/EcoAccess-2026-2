using System.Collections.Generic;
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
    public sealed class ServicioMaterialesTests
    {
        private const int Admin = 1;
        private const int Ciudadano = 2;
        private const int AdminInactivo = 3;

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly ServicioMateriales _servicio;

        public ServicioMaterialesTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Ciudadano, activo: true, CodigoRol.Ciudadano);
            _usuarios.Agregar(AdminInactivo, activo: false, CodigoRol.Administrador);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _servicio = new ServicioMateriales(_usuarios, _materiales, _contenedores);
        }

        [TestMethod]
        public async Task RegistrarAsync_DatosValidos_DevuelveIdYGuardaMaterial()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "PET", "Botella PET", CategoriaMaterial.Aprovechable, null);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Material? guardado = _materiales.Leer(resultado.Valor);
            Assert.IsNotNull(guardado);
            Assert.AreEqual("PET", guardado.Codigo);
        }

        [TestMethod]
        [DataRow(0, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(99, MotivoRechazo.ActorNoEncontrado)]
        [DataRow(AdminInactivo, MotivoRechazo.ActorInactivo)]
        [DataRow(Ciudadano, MotivoRechazo.SinPermiso)]
        public async Task RegistrarAsync_ActorNoAutorizado_RechazaSinGuardar(int idActor, MotivoRechazo esperado)
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(idActor, "PET", "Botella PET", CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual(esperado, resultado.Motivo);
            Assert.IsEmpty(await _materiales.ListarAsync(soloHabilitados: false));
        }

        [TestMethod]
        public async Task RegistrarAsync_NombreVacio_RechazaDatosInvalidosConParametro()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "PET", "  ", CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.StartsWith("nombre: ", resultado.Mensaje);
            Assert.DoesNotContain("(Parameter", resultado.Mensaje);
        }

        [TestMethod]
        public async Task RegistrarAsync_CodigoExistente_RechazaDuplicado()
        {
            _materiales.Sembrar("PET");

            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "PET", "Otra botella", CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual(MotivoRechazo.Duplicado, resultado.Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_ConflictoAlGuardar_RechazaDuplicado()
        {
            _materiales.SimularConflictoAlAgregar = true;

            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "PET", "Botella PET", CategoriaMaterial.Aprovechable, null);

            Assert.AreEqual(MotivoRechazo.Duplicado, resultado.Motivo);
        }

        [TestMethod]
        public async Task ListarAsync_SoloHabilitados_OmiteDeshabilitados()
        {
            _materiales.Sembrar("PET");
            _materiales.Sembrar("VID", habilitado: false);

            IReadOnlyList<Material> lista = await _servicio.ListarAsync(soloHabilitados: true);

            Assert.HasCount(1, lista);
            Assert.AreEqual("PET", lista[0].Codigo);
        }

        [TestMethod]
        public async Task HabilitarAsync_MaterialDeshabilitado_LoHabilitaYGuarda()
        {
            Material material = _materiales.Sembrar("PET", habilitado: false);

            Resultado resultado = await _servicio.HabilitarAsync(Admin, material.Id);

            Assert.IsTrue(resultado.Exitoso);
            Assert.IsTrue(_materiales.Leer(material.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task HabilitarAsync_YaHabilitado_EsExitosoSinGuardar()
        {
            Material material = _materiales.Sembrar("PET");

            Resultado resultado = await _servicio.HabilitarAsync(Admin, material.Id);

            Assert.IsTrue(resultado.Exitoso);
            Assert.AreEqual(0, _materiales.Actualizaciones);
        }

        [TestMethod]
        public async Task HabilitarAsync_MaterialInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.HabilitarAsync(Admin, 42)).Motivo);
        }

        [TestMethod]
        public async Task HabilitarAsync_Ciudadano_RechazaSinPermiso()
        {
            Material material = _materiales.Sembrar("PET", habilitado: false);

            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.HabilitarAsync(Ciudadano, material.Id)).Motivo);
            Assert.IsFalse(_materiales.Leer(material.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_SinCanecasEnServicio_LoDeshabilita()
        {
            Material material = _materiales.Sembrar("PET");
            _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, material.Id);

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, material.Id);

            Assert.IsTrue(resultado.Exitoso);
            Assert.IsFalse(_materiales.Leer(material.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_UnicoMaterialDeCanecaEnServicio_RechazaEstadoNoPermitido()
        {
            Material material = _materiales.Sembrar("PET");
            _contenedores.Sembrar(1, EstadoContenedor.EnServicio, material.Id);

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, material.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_materiales.Leer(material.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_OtroMaterialAdmitidoYaDeshabilitado_RechazaEstadoNoPermitido()
        {
            Material pet = _materiales.Sembrar("PET");
            Material vidrio = _materiales.Sembrar("VID", habilitado: false);
            _contenedores.Sembrar(1, EstadoContenedor.EnServicio, pet.Id, vidrio.Id);

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_CanecaEnServicioConOtroHabilitado_LoDeshabilita()
        {
            Material pet = _materiales.Sembrar("PET");
            Material vidrio = _materiales.Sembrar("VID");
            _contenedores.Sembrar(1, EstadoContenedor.EnServicio, pet.Id, vidrio.Id);

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, pet.Id);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsFalse(_materiales.Leer(pet.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_YaDeshabilitado_EsExitosoSinGuardar()
        {
            Material material = _materiales.Sembrar("PET", habilitado: false);

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, material.Id);

            Assert.IsTrue(resultado.Exitoso);
            Assert.AreEqual(0, _materiales.Actualizaciones);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_MaterialInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.DeshabilitarAsync(Admin, 42)).Motivo);
        }

        // Concurrencia (hallazgo del PR #1): AlEscribir simula a otra persona que escribe entre la lectura y la escritura.

        [TestMethod]
        public async Task DeshabilitarAsync_OtroAdminDeshabilitaElOtroMaterialALaVez_RechazaYQuedaUnoHabilitado()
        {
            Material pet = _materiales.Sembrar("PET");
            Material vidrio = _materiales.Sembrar("VID");
            _contenedores.Sembrar(1, EstadoContenedor.EnServicio, pet.Id, vidrio.Id);
            _materiales.AlEscribir = () => _materiales.DeshabilitarAsync(vidrio.Id).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_materiales.Leer(pet.Id)!.Habilitado);
            Assert.IsFalse(_materiales.Leer(vidrio.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_CanecaPuestaEnServicioALaVez_RechazaEstadoNoPermitido()
        {
            Material pet = _materiales.Sembrar("PET");
            Contenedor caneca = _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, pet.Id);
            _materiales.AlEscribir = () =>
                _contenedores.CambiarEstadoAsync(caneca.Id, EstadoContenedor.EnServicio).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_materiales.Leer(pet.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task HabilitarAsync_EsperaDeBloqueoAgotada_RechazaEstadoNoPermitido()
        {
            Material material = _materiales.Sembrar("PET", habilitado: false);
            _materiales.AlEscribir = () => throw new CambioConcurrenteException("Otra operación está modificando esos datos.");

            Resultado resultado = await _servicio.HabilitarAsync(Admin, material.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual("Otra operación está modificando esos datos.", resultado.Mensaje);
            Assert.IsFalse(_materiales.Leer(material.Id)!.Habilitado);
        }

        [TestMethod]
        public async Task DeshabilitarAsync_EsperaDeBloqueoAgotada_RechazaEstadoNoPermitido()
        {
            Material material = _materiales.Sembrar("PET");
            _materiales.AlEscribir = () => throw new CambioConcurrenteException("Otra operación está modificando esos datos.");

            Resultado resultado = await _servicio.DeshabilitarAsync(Admin, material.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_materiales.Leer(material.Id)!.Habilitado);
        }
    }
}
