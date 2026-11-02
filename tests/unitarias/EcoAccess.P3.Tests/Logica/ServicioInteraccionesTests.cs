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
    public sealed partial class ServicioInteraccionesTests
    {
        private const int Admin = 1;
        private const int Operario = 2;
        private const int Ciudadano = 3;
        private const int OtroCiudadano = 4;
        private const int SinConsentimiento = 5;
        private const int OperarioCiudadano = 6;
        private const string Clave = "clave-0001";
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _repositorio = new();
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioInteracciones _servicio;

        private readonly Material _pet;
        private readonly Material _vidrio;
        private readonly Contenedor _caneca;

        public ServicioInteraccionesTests()
        {
            _usuarios.Agregar(Admin, activo: true, CodigoRol.Administrador);
            _usuarios.Agregar(Operario, activo: true, CodigoRol.Operario);
            _usuarios.AgregarCiudadano(Ciudadano);
            _usuarios.AgregarCiudadano(OtroCiudadano);
            _usuarios.AgregarCiudadano(SinConsentimiento, consentimiento: false);
            _usuarios.AgregarCiudadano(OperarioCiudadano, otrosRoles: CodigoRol.Operario);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _vidrio = _materiales.Sembrar("VID");
            _caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _repositorio.Contenedores = _contenedores;
            _repositorio.Materiales = _materiales;
            _servicio = new ServicioInteracciones(_usuarios, _repositorio, _contenedores, _materiales, _reloj);
        }

        private DateTime AhoraUtc => _reloj.Ahora.UtcDateTime;

        private async Task<int> IniciarAsync(int actor = Ciudadano)
        {
            var r = await _servicio.IniciarAsync(actor, _caneca.Id, CanalInteraccion.Simulador);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return r.Valor;
        }

        private async Task<(int IdInteraccion, int IdOrden)> AutorizarAsync(int actor = Ciudadano, string clave = Clave)
        {
            int id = await IniciarAsync(actor);
            var r = await _servicio.AutorizarAperturaAsync(actor, id, _pet.Id, clave);
            Assert.IsTrue(r.Exitoso, r.ToString());
            return (id, r.Valor);
        }

        private Task<Resultado<EventoRegistrado>> EventoAsync(
            string idExterno, TipoEventoDispositivo tipo, int? idOrden, decimal? peso = null, int? idContenedor = null) =>
            _servicio.RegistrarEventoAsync(new EventoRecibido(idContenedor ?? _caneca.Id, idExterno, tipo, idOrden, null, peso));

        private async Task<int> DepositoCompletoAsync(int actor = Ciudadano)
        {
            var (_, idOrden) = await AutorizarAsync(actor);
            Assert.IsTrue((await _servicio.MarcarEnviadaAsync(idOrden)).Exitoso);
            Assert.AreEqual(ResultadoEvento.Aplicado, (await EventoAsync("e-1", TipoEventoDispositivo.Acuse, idOrden)).Valor.Resultado);
            Assert.AreEqual(ResultadoEvento.Aplicado, (await EventoAsync("e-2", TipoEventoDispositivo.Apertura, idOrden)).Valor.Resultado);
            var deteccion = await EventoAsync("e-3", TipoEventoDispositivo.Deteccion, idOrden, peso: 0.250m);
            Assert.AreEqual(ResultadoEvento.Aplicado, deteccion.Valor.Resultado, deteccion.Valor.MotivoRevision);
            return _repositorio.Depositos.Single().Id;
        }

        // Inicio

        [TestMethod]
        public async Task Iniciar_CiudadanoConConsentimiento_CreaInteraccionIniciada()
        {
            int id = await IniciarAsync();

            Interaccion guardada = _repositorio.LeerInteraccion(id)!;
            Assert.AreEqual(EstadoInteraccion.Iniciada, guardada.Estado);
            Assert.AreEqual(Ciudadano, guardada.IdUsuario);
            Assert.AreEqual(AhoraUtc, guardada.FechaInicioUtc);
        }

        [TestMethod]
        public async Task Iniciar_SinConsentimiento_RechazaSinPermiso()
        {
            var r = await _servicio.IniciarAsync(SinConsentimiento, _caneca.Id, CanalInteraccion.Telegram);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
            StringAssert.Contains(r.Mensaje, "tratamiento de datos");
        }

        [TestMethod]
        public async Task Iniciar_OperarioSinRolCiudadano_RechazaSinPermiso()
        {
            var r = await _servicio.IniciarAsync(Operario, _caneca.Id, CanalInteraccion.Telegram);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task Iniciar_CanecaFueraDeServicio_RechazaEstadoNoPermitido()
        {
            Contenedor fuera = _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, _pet.Id);

            var r = await _servicio.IniciarAsync(Ciudadano, fuera.Id, CanalInteraccion.Telegram);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.CanecaNoEnServicio, r.Mensaje);
        }

        [TestMethod]
        public async Task Iniciar_CanecaInexistente_RechazaNoEncontrado()
        {
            var r = await _servicio.IniciarAsync(Ciudadano, 999, CanalInteraccion.Telegram);

            Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo);
        }

        [TestMethod]
        public async Task Iniciar_CanalManual_RechazaDatosInvalidos()
        {
            var r = await _servicio.IniciarAsync(Ciudadano, _caneca.Id, CanalInteraccion.Manual);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        // Sugerencia y rechazo

        [TestMethod]
        public async Task RegistrarSugerencia_Propia_GuardaMaterialYConfianza()
        {
            int id = await IniciarAsync();

            var r = await _servicio.RegistrarSugerenciaAsync(Ciudadano, id, _vidrio.Id, 0.875m);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Interaccion guardada = _repositorio.LeerInteraccion(id)!;
            Assert.AreEqual(_vidrio.Id, guardada.IdMaterialSugerido);
            Assert.AreEqual(0.875m, guardada.ConfianzaSugerida);
        }

        [TestMethod]
        public async Task RegistrarSugerencia_InteraccionAjena_RechazaSinPermiso()
        {
            int id = await IniciarAsync();

            var r = await _servicio.RegistrarSugerenciaAsync(OtroCiudadano, id, _pet.Id, 0.5m);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task RegistrarSugerencia_ConfianzaFueraDeRango_RechazaDatosInvalidos()
        {
            int id = await IniciarAsync();

            var r = await _servicio.RegistrarSugerenciaAsync(Ciudadano, id, _pet.Id, 1.5m);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        [TestMethod]
        public async Task Rechazar_Iniciada_CierraConMotivo()
        {
            int id = await IniciarAsync();

            var r = await _servicio.RechazarAsync(Ciudadano, id, "Material no reconocido");

            Assert.IsTrue(r.Exitoso, r.ToString());
            Interaccion guardada = _repositorio.LeerInteraccion(id)!;
            Assert.AreEqual(EstadoInteraccion.Rechazada, guardada.Estado);
            Assert.AreEqual("Material no reconocido", guardada.Motivo);
        }

        [TestMethod]
        public async Task Rechazar_YaAutorizada_RechazaEstadoNoPermitido()
        {
            var (id, _) = await AutorizarAsync();

            var r = await _servicio.RechazarAsync(Ciudadano, id, "Tarde");

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
        }

        // Autorización

        [TestMethod]
        public async Task Autorizar_MaterialAdmitido_EmiteOrdenConVencimiento()
        {
            var (id, idOrden) = await AutorizarAsync();

            OrdenApertura orden = _repositorio.LeerOrden(idOrden)!;
            Assert.AreEqual(EstadoOrdenApertura.Emitida, orden.Estado);
            Assert.AreEqual(id, orden.IdInteraccion);
            Assert.AreEqual(AhoraUtc + ServicioInteracciones.VigenciaOrden, orden.FechaVencimientoUtc);
            Interaccion guardada = _repositorio.LeerInteraccion(id)!;
            Assert.AreEqual(EstadoInteraccion.Autorizada, guardada.Estado);
            Assert.AreEqual(_pet.Id, guardada.IdMaterialConfirmado);
        }

        [TestMethod]
        public async Task Autorizar_MismaClaveConOrdenVencida_RechazaSinEmitirOtra()
        {
            var (id, _) = await AutorizarAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, Clave);

            Assert.IsFalse(r.Exitoso);
            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.OrdenNoVigente, r.Mensaje);
            Assert.HasCount(1, _repositorio.Ordenes);
        }

        [TestMethod]
        public async Task Autorizar_MismaClaveRepetida_DevuelveLaMismaOrdenSinEmitirOtra()
        {
            var (id, idOrden) = await AutorizarAsync();

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, Clave);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(idOrden, r.Valor);
            Assert.HasCount(1, _repositorio.Ordenes);
            Assert.AreEqual(1, _repositorio.EmisionesIntentadas);
        }

        [TestMethod]
        public async Task Autorizar_ClaveDeOtraInteraccion_RechazaDuplicado()
        {
            await AutorizarAsync();
            Contenedor otra = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            int id = (await _servicio.IniciarAsync(OtroCiudadano, otra.Id, CanalInteraccion.Telegram)).Valor;

            var r = await _servicio.AutorizarAperturaAsync(OtroCiudadano, id, _pet.Id, Clave);

            Assert.AreEqual(MotivoRechazo.Duplicado, r.Motivo);
        }

        [TestMethod]
        public async Task Autorizar_MaterialNoAdmitido_RechazaSinEmitir()
        {
            int id = await IniciarAsync();

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _vidrio.Id, Clave);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.CanecaNoAdmiteMaterial, r.Mensaje);
            Assert.AreEqual(0, _repositorio.EmisionesIntentadas);
            Assert.AreEqual(EstadoInteraccion.Iniciada, _repositorio.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task Autorizar_MaterialDeshabilitado_RechazaEstadoNoPermitido()
        {
            Material deshabilitado = _materiales.Sembrar("CAR", habilitado: false);
            Contenedor caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id, deshabilitado.Id);
            int id = (await _servicio.IniciarAsync(Ciudadano, caneca.Id, CanalInteraccion.Telegram)).Valor;

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, id, deshabilitado.Id, Clave);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesCatalogo.MaterialDeshabilitado(deshabilitado.Nombre), r.Mensaje);
        }

        [TestMethod]
        public async Task Autorizar_CanecaConOrdenVigente_RechazaCanecaOcupada()
        {
            await AutorizarAsync();
            int id = await IniciarAsync(OtroCiudadano);

            var r = await _servicio.AutorizarAperturaAsync(OtroCiudadano, id, _pet.Id, "clave-0002");

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.CanecaOcupada, r.Mensaje);
        }

        [TestMethod]
        public async Task Autorizar_OtraSesionEmiteAntes_RechazaCanecaOcupadaSinCambiarLaInteraccion()
        {
            int primera = await IniciarAsync();
            int segunda = await IniciarAsync(OtroCiudadano);
            _repositorio.AlEscribir = () => _servicio.AutorizarAperturaAsync(OtroCiudadano, segunda, _pet.Id, "clave-0002").GetAwaiter().GetResult();

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, primera, _pet.Id, Clave);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.CanecaOcupada, r.Mensaje);
            Assert.AreEqual(EstadoInteraccion.Iniciada, _repositorio.LeerInteraccion(primera)!.Estado);
            Assert.HasCount(1, _repositorio.Ordenes);
        }

        [TestMethod]
        public async Task Autorizar_ClaveVacia_RechazaDatosInvalidos()
        {
            int id = await IniciarAsync();

            var r = await _servicio.AutorizarAperturaAsync(Ciudadano, id, _pet.Id, "  ");

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        // Envío

        [TestMethod]
        public async Task MarcarEnviada_DosVeces_EsIdempotente()
        {
            var (_, idOrden) = await AutorizarAsync();

            Assert.IsTrue((await _servicio.MarcarEnviadaAsync(idOrden)).Exitoso);
            var r = await _servicio.MarcarEnviadaAsync(idOrden);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(EstadoOrdenApertura.Enviada, _repositorio.LeerOrden(idOrden)!.Estado);
        }

        [TestMethod]
        public async Task MarcarEnviada_OrdenVencida_NoSeReenvia()
        {
            var (_, idOrden) = await AutorizarAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);

            var r = await _servicio.MarcarEnviadaAsync(idOrden);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, _repositorio.LeerOrden(idOrden)!.Estado);
        }

        // Eventos del dispositivo

        [TestMethod]
        public async Task FlujoCompleto_DeteccionYCierre_CreaUnDepositoYCierraTodo()
        {
            await DepositoCompletoAsync();
            int idOrden = _repositorio.Ordenes.Single().Id;

            var cierre = await EventoAsync("e-4", TipoEventoDispositivo.Cierre, idOrden);

            Assert.AreEqual(ResultadoEvento.Aplicado, cierre.Valor.Resultado);
            OrdenApertura orden = _repositorio.LeerOrden(idOrden)!;
            Assert.AreEqual(EstadoOrdenApertura.Cerrada, orden.Estado);
            Interaccion interaccion = _repositorio.LeerInteraccion(orden.IdInteraccion)!;
            Assert.AreEqual(EstadoInteraccion.ConDeposito, interaccion.Estado);
            Deposito deposito = _repositorio.Depositos.Single();
            Assert.AreEqual(_pet.Id, deposito.IdMaterialDeclarado);
            Assert.AreEqual(0.250m, deposito.PesoMedidoKg);
            Assert.IsNull(deposito.PesoEstimadoKg);
            Assert.AreEqual(_repositorio.Eventos.Single(e => e.IdExterno == "e-3").Id, deposito.IdEvento);
            Assert.HasCount(4, _repositorio.Eventos);
        }

        [TestMethod]
        public async Task RegistrarEvento_IdExternoRepetido_NoSeProcesaDosVeces()
        {
            await DepositoCompletoAsync();
            int idOrden = _repositorio.Ordenes.Single().Id;

            var r = await EventoAsync("e-3", TipoEventoDispositivo.Deteccion, idOrden, peso: 0.250m);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.IsTrue(r.Valor.Repetido);
            Assert.HasCount(1, _repositorio.Depositos);
            Assert.HasCount(3, _repositorio.Eventos);
        }

        [TestMethod]
        public async Task RegistrarEvento_ConflictoAlGuardar_DevuelveElEventoGuardadoComoRepetido()
        {
            var (_, idOrden) = await AutorizarAsync();
            _repositorio.AlEscribir = () => EventoAsync("e-1", TipoEventoDispositivo.Acuse, idOrden).GetAwaiter().GetResult();

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Acuse, idOrden);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.IsTrue(r.Valor.Repetido);
            Assert.HasCount(1, _repositorio.Eventos);
        }

        [TestMethod]
        public async Task RegistrarEvento_SinOrdenVigente_QuedaEnRevision()
        {
            var r = await EventoAsync("e-1", TipoEventoDispositivo.Deteccion, null, peso: 0.1m);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.IsNotNull(r.Valor.MotivoRevision);
            Assert.IsEmpty(_repositorio.Depositos);
            Assert.HasCount(1, (await _servicio.ListarEventosEnRevisionAsync(Operario)).Valor);
        }

        [TestMethod]
        public async Task RegistrarEvento_SinIdOrden_UsaLaOrdenVigenteDeLaCaneca()
        {
            var (_, idOrden) = await AutorizarAsync();

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Acuse, null);

            Assert.AreEqual(ResultadoEvento.Aplicado, r.Valor.Resultado);
            Assert.AreEqual(idOrden, _repositorio.Eventos.Single().IdOrden);
            Assert.AreEqual(EstadoOrdenApertura.Acusada, _repositorio.LeerOrden(idOrden)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_OrdenDeOtraCaneca_QuedaEnRevisionSinOrden()
        {
            var (_, idOrden) = await AutorizarAsync();
            Contenedor otra = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Apertura, idOrden, idContenedor: otra.Id);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.IsNull(_repositorio.Eventos.Single().IdOrden);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, _repositorio.LeerOrden(idOrden)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_DeteccionFueraDeVentana_QuedaEnRevisionSinDeposito()
        {
            var (_, idOrden) = await AutorizarAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden + TimeSpan.FromSeconds(1));

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Deteccion, idOrden, peso: 0.1m);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.IsEmpty(_repositorio.Depositos);
        }

        [TestMethod]
        public async Task RegistrarEvento_RetrocesoDeEstado_QuedaEnRevision()
        {
            var (_, idOrden) = await AutorizarAsync();
            await EventoAsync("e-1", TipoEventoDispositivo.Apertura, idOrden);

            var r = await EventoAsync("e-2", TipoEventoDispositivo.Acuse, idOrden);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.AreEqual(EstadoOrdenApertura.Abierta, _repositorio.LeerOrden(idOrden)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_SegundaDeteccion_NoCreaOtroDeposito()
        {
            await DepositoCompletoAsync();
            int idOrden = _repositorio.Ordenes.Single().Id;

            var r = await EventoAsync("e-9", TipoEventoDispositivo.Deteccion, idOrden, peso: 0.3m);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.HasCount(1, _repositorio.Depositos);
        }

        [TestMethod]
        public async Task RegistrarEvento_FalloAntesDeDetectar_CierraSinDeposito()
        {
            var (id, idOrden) = await AutorizarAsync();

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Fallo, idOrden);

            Assert.AreEqual(ResultadoEvento.Aplicado, r.Valor.Resultado);
            Assert.AreEqual(EstadoOrdenApertura.Fallida, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.AreEqual(EstadoInteraccion.SinDeposito, _repositorio.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_PesoInvalido_QuedaEnRevisionSinDeposito()
        {
            var (id, idOrden) = await AutorizarAsync();
            await _servicio.MarcarEnviadaAsync(idOrden);
            await EventoAsync("e-1", TipoEventoDispositivo.Acuse, idOrden);
            await EventoAsync("e-2", TipoEventoDispositivo.Apertura, idOrden);

            var r = await EventoAsync("e-3", TipoEventoDispositivo.Deteccion, idOrden, peso: 1000m);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.AreEqual("El peso informado no es válido.", r.Valor.MotivoRevision);
            Assert.IsEmpty(_repositorio.Depositos);
            Assert.AreEqual(EstadoOrdenApertura.Abierta, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.AreEqual(EstadoInteraccion.Autorizada, _repositorio.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_DeteccionSinApertura_QuedaEnRevisionSinDeposito()
        {
            var (id, idOrden) = await AutorizarAsync();

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Deteccion, idOrden, peso: 0.2m);

            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.IsEmpty(_repositorio.Depositos);
            Assert.AreEqual(EstadoOrdenApertura.Emitida, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.AreEqual(EstadoInteraccion.Autorizada, _repositorio.LeerInteraccion(id)!.Estado);
        }

        [TestMethod]
        public async Task RegistrarEvento_FechaNoUtc_RechazaDatosInvalidosSinGuardar()
        {
            var (_, idOrden) = await AutorizarAsync();
            var local = new DateTime(2026, 3, 10, 8, 31, 0, DateTimeKind.Local);

            var r = await _servicio.RegistrarEventoAsync(
                new EventoRecibido(_caneca.Id, "e-1", TipoEventoDispositivo.Acuse, idOrden, local, null));

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
            Assert.IsEmpty(_repositorio.Eventos);
        }

        [TestMethod]
        public async Task RegistrarEvento_IdExternoDemasiadoLargo_RechazaDatosInvalidos()
        {
            var r = await EventoAsync(new string('x', EventoDispositivo.LongitudMaximaIdExterno + 1), TipoEventoDispositivo.Acuse, null);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        [TestMethod]
        public async Task RegistrarEvento_TipoNoDefinido_RechazaDatosInvalidos()
        {
            var r = await EventoAsync("e-1", (TipoEventoDispositivo)99, null);

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        [TestMethod]
        public async Task RegistrarEvento_OrdenCambiaMientrasSeProcesa_VuelveADecidirConElEstadoNuevo()
        {
            var (_, idOrden) = await AutorizarAsync();
            _repositorio.AlEscribir = () => _repositorio.CambiarOrdenGuardada(idOrden, o => o.MarcarEnviada());

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Apertura, idOrden);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(ResultadoEvento.Aplicado, r.Valor.Resultado);
            Assert.AreEqual(EstadoOrdenApertura.Abierta, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.HasCount(1, _repositorio.Eventos);
        }

        [TestMethod]
        public async Task RegistrarEvento_OrdenCambiaDosVeces_GuardaElEventoEnRevision()
        {
            var (_, idOrden) = await AutorizarAsync();
            _repositorio.AlEscribir = () =>
            {
                _repositorio.CambiarOrdenGuardada(idOrden, o => o.MarcarEnviada());
                _repositorio.AlEscribir = () => _repositorio.CambiarOrdenGuardada(idOrden, o => o.RegistrarAcuse());
            };

            var r = await EventoAsync("e-1", TipoEventoDispositivo.Apertura, idOrden);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Assert.AreEqual(ResultadoEvento.Revision, r.Valor.Resultado);
            Assert.AreEqual(EstadoOrdenApertura.Acusada, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.HasCount(1, _repositorio.Eventos);
        }

        // Vencimiento

        [TestMethod]
        public async Task VencerOrdenes_OrdenSinDeteccion_VenceYCierraSinDeposito()
        {
            var (id, idOrden) = await AutorizarAsync();
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);

            int vencidas = await _servicio.VencerOrdenesAsync();

            Assert.AreEqual(1, vencidas);
            Assert.AreEqual(EstadoOrdenApertura.Vencida, _repositorio.LeerOrden(idOrden)!.Estado);
            Assert.AreEqual(EstadoInteraccion.SinDeposito, _repositorio.LeerInteraccion(id)!.Estado);
            int nueva = await IniciarAsync(OtroCiudadano);
            Assert.IsTrue((await _servicio.AutorizarAperturaAsync(OtroCiudadano, nueva, _pet.Id, "clave-0002")).Exitoso);
        }

        [TestMethod]
        public async Task VencerOrdenes_OrdenDetectada_VenceSinTocarLaInteraccionConDeposito()
        {
            await DepositoCompletoAsync();
            int idOrden = _repositorio.Ordenes.Single().Id;
            _reloj.Avanzar(ServicioInteracciones.VigenciaOrden);

            Assert.AreEqual(1, await _servicio.VencerOrdenesAsync());

            OrdenApertura orden = _repositorio.LeerOrden(idOrden)!;
            Assert.AreEqual(EstadoOrdenApertura.Vencida, orden.Estado);
            Assert.AreEqual(EstadoInteraccion.ConDeposito, _repositorio.LeerInteraccion(orden.IdInteraccion)!.Estado);
        }

        [TestMethod]
        public async Task VencerOrdenes_AntesDelVencimiento_NoCambiaNada()
        {
            await AutorizarAsync();

            Assert.AreEqual(0, await _servicio.VencerOrdenesAsync());
        }

        // Apertura manual

        [TestMethod]
        public async Task AperturaManual_Operario_RegistraCerradaSinDeposito()
        {
            var r = await _servicio.RegistrarAperturaManualAsync(Operario, _caneca.Id, "Atasco del sensor");

            Assert.IsTrue(r.Exitoso, r.ToString());
            Interaccion guardada = _repositorio.LeerInteraccion(r.Valor)!;
            Assert.AreEqual(CanalInteraccion.Manual, guardada.Canal);
            Assert.AreEqual(EstadoInteraccion.SinDeposito, guardada.Estado);
            Assert.AreEqual(Operario, guardada.IdOperario);
            Assert.IsNull(guardada.IdUsuario);
        }

        [TestMethod]
        public async Task AperturaManual_Ciudadano_RechazaSinPermiso()
        {
            var r = await _servicio.RegistrarAperturaManualAsync(Ciudadano, _caneca.Id, "Quiero abrir");

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task AperturaManual_ConOrdenVigente_RechazaCanecaOcupada()
        {
            await AutorizarAsync();

            var r = await _servicio.RegistrarAperturaManualAsync(Operario, _caneca.Id, "Revisión");

            Assert.AreEqual(MensajesInteracciones.CanecaOcupada, r.Mensaje);
        }

        [TestMethod]
        public async Task AperturaManual_MotivoVacio_RechazaDatosInvalidos()
        {
            var r = await _servicio.RegistrarAperturaManualAsync(Operario, _caneca.Id, " ");

            Assert.AreEqual(MotivoRechazo.DatosInvalidos, r.Motivo);
        }

        // Verificación

        [TestMethod]
        public async Task VerificarDeposito_Operario_FijaMaterialVerificado()
        {
            int idDeposito = await DepositoCompletoAsync();

            var r = await _servicio.VerificarDepositoAsync(Operario, idDeposito, _pet.Id);

            Assert.IsTrue(r.Exitoso, r.ToString());
            Deposito deposito = _repositorio.Depositos.Single();
            Assert.AreEqual(_pet.Id, deposito.IdMaterialVerificado);
            Assert.AreEqual(Operario, deposito.IdVerificador);
            Assert.IsEmpty((await _servicio.ListarDepositosSinVerificarAsync(Operario)).Valor);
        }

        [TestMethod]
        public async Task VerificarDeposito_QuienDeposito_RechazaSinPermiso()
        {
            int idDeposito = await DepositoCompletoAsync(OperarioCiudadano);

            var r = await _servicio.VerificarDepositoAsync(OperarioCiudadano, idDeposito, _pet.Id);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
            Assert.IsFalse(_repositorio.Depositos.Single().EstaVerificado);
        }

        [TestMethod]
        public async Task VerificarDeposito_YaVerificado_RechazaEstadoNoPermitido()
        {
            int idDeposito = await DepositoCompletoAsync();
            await _servicio.VerificarDepositoAsync(Operario, idDeposito, _pet.Id);

            var r = await _servicio.VerificarDepositoAsync(Admin, idDeposito, _vidrio.Id);

            Assert.AreEqual(MotivoRechazo.EstadoNoPermitido, r.Motivo);
            Assert.AreEqual(MensajesInteracciones.DepositoYaVerificado, r.Mensaje);
        }

        [TestMethod]
        public async Task VerificarDeposito_Ciudadano_RechazaSinPermiso()
        {
            int idDeposito = await DepositoCompletoAsync();

            var r = await _servicio.VerificarDepositoAsync(OtroCiudadano, idDeposito, _pet.Id);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        // Consultas

        [TestMethod]
        public async Task Obtener_CiudadanoAjeno_RechazaSinPermiso()
        {
            int id = await IniciarAsync();

            var r = await _servicio.ObtenerAsync(OtroCiudadano, id);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task Obtener_OperarioOPropietario_DevuelveLaInteraccion()
        {
            int id = await IniciarAsync();

            Assert.IsTrue((await _servicio.ObtenerAsync(Operario, id)).Exitoso);
            Assert.IsTrue((await _servicio.ObtenerAsync(Ciudadano, id)).Exitoso);
        }

        [TestMethod]
        public async Task ListarMias_DevuelveSoloLasPropias()
        {
            await IniciarAsync();
            await IniciarAsync(OtroCiudadano);

            var r = await _servicio.ListarMiasAsync(Ciudadano);

            Assert.HasCount(1, r.Valor);
            Assert.AreEqual(Ciudadano, r.Valor[0].IdUsuario);
        }

        [TestMethod]
        public async Task ListarMisDepositos_SoloDevuelveLosDelPropioCiudadano()
        {
            int idDeposito = await DepositoCompletoAsync();

            var propios = await _servicio.ListarMisDepositosAsync(Ciudadano);
            var ajenos = await _servicio.ListarMisDepositosAsync(OtroCiudadano);

            Assert.HasCount(1, propios.Valor);
            Assert.AreEqual(idDeposito, propios.Valor[0].Id);
            Assert.IsEmpty(ajenos.Valor);
        }

        [TestMethod]
        public async Task ListarMisDepositos_Operario_RechazaSinPermiso()
        {
            var r = await _servicio.ListarMisDepositosAsync(Operario);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }

        [TestMethod]
        public async Task ListarPorContenedor_Ciudadano_RechazaSinPermiso()
        {
            var r = await _servicio.ListarPorContenedorAsync(Ciudadano, _caneca.Id);

            Assert.AreEqual(MotivoRechazo.SinPermiso, r.Motivo);
        }
    }
}
