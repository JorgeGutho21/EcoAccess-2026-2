using System;
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
    public sealed class ServicioContenedoresTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private static readonly DateTimeOffset Ahora = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioPuntosEnMemoria _puntos = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly GeneradorQrSecuencial _generadorQr = new();
        private readonly ServicioContenedores _servicio;

        private readonly PuntoInstalacion _puntoActivo;
        private readonly Material _pet;

        public ServicioContenedoresTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _contenedores.Puntos = _puntos;
            _puntos.Contenedores = _contenedores;
            _puntoActivo = _puntos.Sembrar("Bloque A");
            _pet = _materiales.Sembrar("PET");
            _servicio = new ServicioContenedores(_usuarios, _contenedores, _puntos, _materiales, _generadorQr, new RelojFijo(Ahora));
        }

        // Registro

        [TestMethod]
        public async Task RegistrarAsync_PuntoActivo_GuardaCanecaFueraDeServicioConQrYFecha()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, _puntoActivo.Id, "Caneca entrada");

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Contenedor guardada = _contenedores.Leer(resultado.Valor)!;
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, guardada.Estado);
            Assert.AreEqual(_generadorQr.Generados.Single(), guardada.IdentificadorQr);
            Assert.AreEqual(Ahora.UtcDateTime, guardada.FechaAltaUtc);
            Assert.AreEqual(DateTimeKind.Utc, guardada.FechaAltaUtc.Kind);
        }

        [TestMethod]
        public async Task RegistrarAsync_Operario_RechazaSinPermiso()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Operario, _puntoActivo.Id, "Caneca entrada");

            Assert.AreEqual(MotivoRechazo.SinPermiso, resultado.Motivo);
            Assert.AreEqual(0, _contenedores.IntentosAgregar);
        }

        [TestMethod]
        public async Task RegistrarAsync_PuntoInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.RegistrarAsync(Admin, 99, "Caneca")).Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_PuntoInactivo_RechazaEstadoNoPermitido()
        {
            PuntoInstalacion inactivo = _puntos.Sembrar("Bloque B", activo: false);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.RegistrarAsync(Admin, inactivo.Id, "Caneca")).Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_NombreInvalido_RechazaDatosInvalidos()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, _puntoActivo.Id, "x");

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, resultado.Motivo);
            Assert.StartsWith("nombre: ", resultado.Mensaje);
        }

        [TestMethod]
        public async Task RegistrarAsync_QrRepetidoUnaVez_ReintentaConOtroQr()
        {
            _contenedores.ConflictosQrPendientes = 1;

            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, _puntoActivo.Id, "Caneca entrada");

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(2, _contenedores.IntentosAgregar);
            Assert.AreEqual(_generadorQr.Generados[1], _contenedores.Leer(resultado.Valor)!.IdentificadorQr);
        }

        [TestMethod]
        public async Task RegistrarAsync_QrRepetidoEnTodosLosIntentos_RechazaDuplicado()
        {
            _contenedores.ConflictosQrPendientes = ServicioContenedoresIntentos;

            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, _puntoActivo.Id, "Caneca entrada");

            Assert.AreEqual(MotivoRechazo.Duplicado, resultado.Motivo);
            Assert.AreEqual(ServicioContenedoresIntentos, _contenedores.IntentosAgregar);
        }

        private const int ServicioContenedoresIntentos = 3;

        // Consulta

        [TestMethod]
        public async Task ObtenerAsync_Inexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.ObtenerAsync(5)).Motivo);
        }

        [TestMethod]
        public async Task ObtenerAsync_Existente_DevuelveCanecaConMateriales()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);

            Resultado<Contenedor> resultado = await _servicio.ObtenerAsync(caneca.Id);

            Assert.IsTrue(resultado.Exitoso);
            Assert.IsTrue(resultado.Valor.AdmiteMaterial(_pet.Id));
        }

        // Materiales admitidos

        [TestMethod]
        public async Task AdmitirMaterialAsync_MaterialHabilitado_LoAgregaYGuarda()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Resultado resultado = await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, _pet.Id);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsTrue(_contenedores.Leer(caneca.Id)!.AdmiteMaterial(_pet.Id));
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_MaterialDeshabilitado_RechazaEstadoNoPermitido()
        {
            Material vidrio = _materiales.Sembrar("VID", habilitado: false);
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, vidrio.Id)).Motivo);
            Assert.AreEqual(0, _contenedores.Actualizaciones);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_MaterialInexistente_RechazaNoEncontrado()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, 77)).Motivo);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_YaAdmitido_RechazaDuplicado()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);

            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_CanecaDadaDeBaja_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.DadoDeBaja);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_CanecaInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.AdmitirMaterialAsync(Admin, 50, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_Operario_RechazaSinPermiso()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.AdmitirMaterialAsync(Operario, caneca.Id, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_CanecaFueraDeServicio_QuitaElUltimo()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);

            Assert.IsTrue((await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Exitoso);
            Assert.IsEmpty(_contenedores.Leer(caneca.Id)!.MaterialesAdmitidos);
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_NoAdmitido_RechazaNoEncontrado()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_UnicoDeCanecaEnServicio_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
            Assert.IsTrue(_contenedores.Leer(caneca.Id)!.AdmiteMaterial(_pet.Id));
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_EnServicioYRestanteDeshabilitado_RechazaEstadoNoPermitido()
        {
            Material vidrio = _materiales.Sembrar("VID", habilitado: false);
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id, vidrio.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_EnServicioYRestanteHabilitado_LoQuita()
        {
            Material vidrio = _materiales.Sembrar("VID");
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id, vidrio.Id);

            Assert.IsTrue((await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Exitoso);
            Assert.IsFalse(_contenedores.Leer(caneca.Id)!.AdmiteMaterial(_pet.Id));
        }

        [TestMethod]
        public async Task QuitarMaterialAsync_CanecaDadaDeBaja_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.DadoDeBaja, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id)).Motivo);
        }

        // Estados

        [TestMethod]
        public async Task PonerEnServicioAsync_PuntoActivoYMaterialHabilitado_LaPoneEnServicio()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);

            Resultado resultado = await _servicio.PonerEnServicioAsync(Admin, caneca.Id);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.AreEqual(EstadoContenedor.EnServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_SinMateriales_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PonerEnServicioAsync(Admin, caneca.Id)).Motivo);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_SoloMaterialesDeshabilitados_RechazaEstadoNoPermitido()
        {
            Material vidrio = _materiales.Sembrar("VID", habilitado: false);
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, vidrio.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PonerEnServicioAsync(Admin, caneca.Id)).Motivo);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_PuntoInactivo_RechazaEstadoNoPermitido()
        {
            PuntoInstalacion inactivo = _puntos.Sembrar("Bloque B", activo: false);
            Contenedor caneca = _contenedores.Sembrar(inactivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PonerEnServicioAsync(Admin, caneca.Id)).Motivo);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_DadaDeBaja_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.DadoDeBaja, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PonerEnServicioAsync(Admin, caneca.Id)).Motivo);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_YaEnServicio_EsExitosoSinGuardar()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);

            Assert.IsTrue((await _servicio.PonerEnServicioAsync(Admin, caneca.Id)).Exitoso);
            Assert.AreEqual(0, _contenedores.Actualizaciones);
        }

        [TestMethod]
        public async Task PonerFueraDeServicioAsync_EnServicio_LaPoneFueraDeServicio()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);

            Assert.IsTrue((await _servicio.PonerFueraDeServicioAsync(Admin, caneca.Id)).Exitoso);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task PonerFueraDeServicioAsync_YaFueraDeServicio_EsExitosoSinGuardar()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);

            Assert.IsTrue((await _servicio.PonerFueraDeServicioAsync(Admin, caneca.Id)).Exitoso);
            Assert.AreEqual(0, _contenedores.Actualizaciones);
        }

        [TestMethod]
        public async Task PonerFueraDeServicioAsync_DadaDeBaja_RechazaEstadoNoPermitido()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.DadoDeBaja);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, (await _servicio.PonerFueraDeServicioAsync(Admin, caneca.Id)).Motivo);
        }

        [TestMethod]
        public async Task DarDeBajaAsync_EnServicio_LaDaDeBaja()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);

            Assert.IsTrue((await _servicio.DarDeBajaAsync(Admin, caneca.Id)).Exitoso);
            Assert.AreEqual(EstadoContenedor.DadoDeBaja, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task DarDeBajaAsync_YaDadaDeBaja_EsExitosoSinGuardar()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.DadoDeBaja);

            Assert.IsTrue((await _servicio.DarDeBajaAsync(Admin, caneca.Id)).Exitoso);
            Assert.AreEqual(0, _contenedores.Actualizaciones);
        }

        [TestMethod]
        public async Task DarDeBajaAsync_Operario_RechazaSinPermiso()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);

            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.DarDeBajaAsync(Operario, caneca.Id)).Motivo);
            Assert.AreEqual(EstadoContenedor.EnServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }

        // Concurrencia (hallazgo del PR #1): AlEscribir simula a otra persona que escribe entre la lectura y la escritura.

        [TestMethod]
        public async Task QuitarMaterialAsync_RestanteDeshabilitadoALaVez_RechazaYConservaMateriales()
        {
            Material vidrio = _materiales.Sembrar("VID");
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id, vidrio.Id);
            _contenedores.AlEscribir = () => _materiales.DeshabilitarAsync(vidrio.Id).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.QuitarMaterialAsync(Admin, caneca.Id, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            CollectionAssert.AreEquivalent(new[] { _pet.Id, vidrio.Id }, _contenedores.Leer(caneca.Id)!.MaterialesAdmitidos.ToArray());
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_UnicoMaterialDeshabilitadoALaVez_RechazaYSigueFuera()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);
            _contenedores.AlEscribir = () => _materiales.DeshabilitarAsync(_pet.Id).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.PonerEnServicioAsync(Admin, caneca.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task AdmitirMaterialAsync_MaterialDeshabilitadoALaVez_RechazaSinAgregarlo()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio);
            _contenedores.AlEscribir = () => _materiales.DeshabilitarAsync(_pet.Id).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.AdmitirMaterialAsync(Admin, caneca.Id, _pet.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsEmpty(_contenedores.Leer(caneca.Id)!.MaterialesAdmitidos);
        }

        [TestMethod]
        public async Task PonerFueraDeServicioAsync_DadaDeBajaALaVez_NoLaDevuelveAOtroEstado()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.EnServicio, _pet.Id);
            _contenedores.AlEscribir = () =>
                _contenedores.CambiarEstadoAsync(caneca.Id, EstadoContenedor.DadoDeBaja).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.PonerFueraDeServicioAsync(Admin, caneca.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual(EstadoContenedor.DadoDeBaja, _contenedores.Leer(caneca.Id)!.Estado);
        }

        [TestMethod]
        public async Task PonerEnServicioAsync_PuntoDesactivadoALaVez_RechazaYQuedaFueraDeServicio()
        {
            Contenedor caneca = _contenedores.Sembrar(_puntoActivo.Id, EstadoContenedor.FueraDeServicio, _pet.Id);
            _contenedores.AlEscribir = () => _puntos.CambiarActivoAsync(_puntoActivo.Id, false).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.PonerEnServicioAsync(Admin, caneca.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual(MensajesCatalogo.PuntoDeCanecaInactivo, resultado.Mensaje);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, _contenedores.Leer(caneca.Id)!.Estado);
        }
    }
}
