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
    public sealed class ServicioPuntosInstalacionTests
    {
        private const int Admin = 1;
        private const int Operario = 2;

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioPuntosEnMemoria _puntos = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly ServicioPuntosInstalacion _servicio;

        public ServicioPuntosInstalacionTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _puntos.Contenedores = _contenedores;
            _contenedores.Puntos = _puntos;
            _servicio = new ServicioPuntosInstalacion(_usuarios, _puntos, _contenedores);
        }

        [TestMethod]
        public async Task RegistrarAsync_DatosValidos_DevuelveIdYGuardaPuntoActivo()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "Bloque A", "Entrada principal");

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            PuntoInstalacion? guardado = _puntos.Leer(resultado.Valor);
            Assert.IsNotNull(guardado);
            Assert.IsTrue(guardado.Activo);
        }

        [TestMethod]
        public async Task RegistrarAsync_Operario_RechazaSinPermiso()
        {
            Resultado<int> resultado = await _servicio.RegistrarAsync(Operario, "Bloque A", null);

            Assert.AreEqual(MotivoRechazo.SinPermiso, resultado.Motivo);
            Assert.IsEmpty(await _puntos.ListarAsync());
        }

        [TestMethod]
        public async Task RegistrarAsync_NombreDeUnCaracter_RechazaDatosInvalidos()
        {
            Assert.AreEqual(MotivoRechazo.DatosInvalidos, (await _servicio.RegistrarAsync(Admin, "A", null)).Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_NombreExistenteConEspacios_RechazaDuplicado()
        {
            _puntos.Sembrar("Bloque A");

            Resultado<int> resultado = await _servicio.RegistrarAsync(Admin, "  Bloque A ", null);

            Assert.AreEqual(MotivoRechazo.Duplicado, resultado.Motivo);
        }

        [TestMethod]
        public async Task RegistrarAsync_ConflictoAlGuardar_RechazaDuplicado()
        {
            _puntos.SimularConflictoAlAgregar = true;

            Assert.AreEqual(MotivoRechazo.Duplicado, (await _servicio.RegistrarAsync(Admin, "Bloque A", null)).Motivo);
        }

        [TestMethod]
        public async Task ActivarAsync_PuntoInactivo_LoActiva()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A", activo: false);

            Assert.IsTrue((await _servicio.ActivarAsync(Admin, punto.Id)).Exitoso);
            Assert.IsTrue(_puntos.Leer(punto.Id)!.Activo);
        }

        [TestMethod]
        public async Task ActivarAsync_YaActivo_EsExitosoSinGuardar()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A");

            Assert.IsTrue((await _servicio.ActivarAsync(Admin, punto.Id)).Exitoso);
            Assert.AreEqual(0, _puntos.Actualizaciones);
        }

        [TestMethod]
        public async Task ActivarAsync_PuntoInexistente_RechazaNoEncontrado()
        {
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.ActivarAsync(Admin, 9)).Motivo);
        }

        [TestMethod]
        public async Task DesactivarAsync_SinCanecasEnServicio_LoDesactiva()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A");
            _contenedores.Sembrar(punto.Id, EstadoContenedor.FueraDeServicio, 1);
            _contenedores.Sembrar(punto.Id, EstadoContenedor.DadoDeBaja);

            Resultado resultado = await _servicio.DesactivarAsync(Admin, punto.Id);

            Assert.IsTrue(resultado.Exitoso, resultado.ToString());
            Assert.IsFalse(_puntos.Leer(punto.Id)!.Activo);
        }

        [TestMethod]
        public async Task DesactivarAsync_ConCanecaEnServicio_RechazaEstadoNoPermitido()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A");
            _contenedores.Sembrar(punto.Id, EstadoContenedor.EnServicio, 1);

            Resultado resultado = await _servicio.DesactivarAsync(Admin, punto.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.IsTrue(_puntos.Leer(punto.Id)!.Activo);
        }

        [TestMethod]
        public async Task DesactivarAsync_YaInactivo_EsExitosoSinGuardar()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A", activo: false);

            Assert.IsTrue((await _servicio.DesactivarAsync(Admin, punto.Id)).Exitoso);
            Assert.AreEqual(0, _puntos.Actualizaciones);
        }

        // Concurrencia: AlEscribir simula a otra persona que escribe entre la lectura y la escritura.

        [TestMethod]
        public async Task DesactivarAsync_CanecaPuestaEnServicioALaVez_RechazaYElPuntoSigueActivo()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A");
            Contenedor caneca = _contenedores.Sembrar(punto.Id, EstadoContenedor.FueraDeServicio, 1);
            _puntos.AlEscribir = () =>
                _contenedores.CambiarEstadoAsync(caneca.Id, EstadoContenedor.EnServicio).GetAwaiter().GetResult();

            Resultado resultado = await _servicio.DesactivarAsync(Admin, punto.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual(MensajesCatalogo.PuntoConCanecasEnServicio, resultado.Mensaje);
            Assert.IsTrue(_puntos.Leer(punto.Id)!.Activo);
        }

        [TestMethod]
        public async Task ActivarAsync_EsperaDeBloqueoAgotada_RechazaEstadoNoPermitido()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A", activo: false);
            _puntos.AlEscribir = () => throw new CambioConcurrenteException("Otra operación está modificando esos datos.");

            Resultado resultado = await _servicio.ActivarAsync(Admin, punto.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, resultado.Motivo);
            Assert.AreEqual("Otra operación está modificando esos datos.", resultado.Mensaje);
            Assert.IsFalse(_puntos.Leer(punto.Id)!.Activo);
        }

        [TestMethod]
        public async Task DesactivarAsync_Operario_RechazaSinPermiso()
        {
            PuntoInstalacion punto = _puntos.Sembrar("Bloque A");

            Assert.AreEqual(MotivoRechazo.SinPermiso, (await _servicio.DesactivarAsync(Operario, punto.Id)).Motivo);
        }
    }
}
