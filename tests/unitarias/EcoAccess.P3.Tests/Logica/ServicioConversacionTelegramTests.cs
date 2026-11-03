using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    [TestClass]
    public sealed partial class ServicioConversacionTelegramTests
    {
        private const long Vinculado = 700_000_001;
        private const long Invitado = 700_000_002;
        private const int Ciudadano = 3;
        private static readonly DateTimeOffset Inicio = new(2026, 3, 10, 8, 30, 0, TimeSpan.Zero);
        private static readonly Uri PaginaRegistro = new("https://ecoaccess.example/registro");

        private readonly RepositorioUsuariosEnMemoria _usuarios = new();
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly RepositorioInteraccionesEnMemoria _interacciones = new();
        private readonly RepositorioConversacionesEnMemoria _conversaciones = new();
        private readonly RepositorioVinculosEnMemoria _vinculos;
        private readonly LimitadorManual _limitador = new();
        private readonly RelojAjustable _reloj = new(Inicio);
        private readonly ServicioConversacionTelegram _servicio;
        private readonly ServicioInteracciones _servicioInteracciones;

        private readonly Material _pet;
        private readonly Material _vidrio;
        private readonly Contenedor _caneca;
        private long _update;

        public ServicioConversacionTelegramTests()
        {
            _usuarios.AgregarCiudadano(Ciudadano);
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET");
            _vidrio = _materiales.Sembrar("VID");
            _caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);
            _interacciones.Contenedores = _contenedores;
            _interacciones.Materiales = _materiales;
            _vinculos = new RepositorioVinculosEnMemoria(_usuarios);
            _vinculos.Sembrar(Ciudadano, Vinculado);

            var vinculacion = new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj);
            _servicioInteracciones = new ServicioInteracciones(_usuarios, _interacciones, _contenedores, _materiales, _reloj);
            _servicio = new ServicioConversacionTelegram(
                _conversaciones, _contenedores, _materiales, vinculacion, _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
        }

        private Task<IReadOnlyList<RespuestaBot>> TextoAsync(string texto, long cuenta = Vinculado) =>
            _servicio.ProcesarAsync(new MensajeEntrante(++_update, cuenta, cuenta, true, texto, null));

        private Task<IReadOnlyList<RespuestaBot>> BotonAsync(string dato, long cuenta = Vinculado) =>
            _servicio.ProcesarAsync(new MensajeEntrante(++_update, cuenta, cuenta, true, null, dato));

        private static RespuestaBot Unica(IReadOnlyList<RespuestaBot> respuestas) => respuestas.Single();

        private string MaterialPet => ServicioConversacionTelegram.PrefijoMaterial + _pet.Id;

        private int IdInteraccionActual => _conversaciones.Leer(Vinculado)!.IdInteraccion!.Value;

        /// <summary>Recorre el chat hasta emitir la orden y simula los eventos del dispositivo hasta el depósito.</summary>
        private async Task DepositoPorChatAsync(decimal? pesoMedido)
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            int idOrden = _interacciones.Ordenes.Single().Id;
            Assert.IsTrue((await _servicioInteracciones.MarcarEnviadaAsync(idOrden)).Exitoso);
            foreach (var (id, tipo) in new[] { ("e-1", TipoEventoDispositivo.Acuse), ("e-2", TipoEventoDispositivo.Apertura), ("e-3", TipoEventoDispositivo.Deteccion) })
            {
                var r = await _servicioInteracciones.RegistrarEventoAsync(
                    new EventoRecibido(_caneca.Id, id, tipo, idOrden, null, tipo == TipoEventoDispositivo.Deteccion ? pesoMedido : null));
                Assert.AreEqual(ResultadoEvento.Aplicado, r.Valor.Resultado, r.Valor.MotivoRevision);
            }
        }

        [TestMethod]
        [DataRow("/operaciones")]
        [DataRow("/depositos")]
        [DataRow("/ecopuntos")]
        public async Task ProcesarAsync_ConsultaSinVinculo_InvitaAVincularSinConsultar(string comando)
        {
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await TextoAsync(comando, Invitado)).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_ConsultasSinHistorial_RespondenQueNoHayDatos()
        {
            Assert.AreEqual(MensajesTelegram.SinOperaciones, Unica(await TextoAsync("/operaciones")).Texto);
            Assert.AreEqual(MensajesTelegram.SinDepositos, Unica(await TextoAsync("/depositos")).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_Operaciones_MuestraLaInteraccionConSuMaterial()
        {
            await DepositoPorChatAsync(0.25m);

            string texto = Unica(await TextoAsync("/operaciones")).Texto;

            StringAssert.Contains(texto, "2026-03-10 08:30: con depósito (" + _pet.Nombre + ")");
        }

        [TestMethod]
        public async Task ProcesarAsync_Depositos_MuestraElPesoMedidoSinPresentarloComoEstimado()
        {
            await DepositoPorChatAsync(0.25m);

            string texto = Unica(await TextoAsync("/depositos")).Texto;

            StringAssert.Contains(texto, _pet.Nombre + ", pendiente de verificación, peso medido 0.25 kg");
            Assert.DoesNotContain("estimado", texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_DepositoSinPeso_LoDiceSinInventarlo()
        {
            await DepositoPorChatAsync(null);

            StringAssert.EndsWith(Unica(await TextoAsync("/depositos")).Texto, ", sin peso");
        }

        [TestMethod]
        public async Task ProcesarAsync_Ecopuntos_RespondeQueAunNoEstaDisponible()
        {
            Assert.AreEqual(MensajesTelegram.EcopuntosPendiente, Unica(await TextoAsync("/ecopuntos")).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_Ayuda_ListaLasConsultas()
        {
            string texto = Unica(await TextoAsync("/help")).Texto;

            StringAssert.Contains(texto, "/operaciones");
            StringAssert.Contains(texto, "/depositos");
            StringAssert.Contains(texto, "/ecopuntos");
            Assert.DoesNotContain("contraseña:", texto);
        }

        [TestMethod]
        public async Task FlujoCompleto_QrMaterialYConfirmacion_EmiteUnaSolaOrden()
        {
            var qr = Unica(await TextoAsync("/start " + _caneca.IdentificadorQr));
            Assert.AreEqual(Vinculado, qr.ChatId);
            StringAssert.EndsWith(qr.Texto, MensajesTelegram.ElegirMaterial);
            Assert.AreEqual(MaterialPet, qr.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoMaterial, StringComparison.Ordinal)).Dato);
            int idInteraccion = IdInteraccionActual;

            var material = Unica(await BotonAsync(MaterialPet));
            StringAssert.StartsWith(material.Texto, MensajesTelegram.Confirmar(_pet.Nombre));
            StringAssert.Contains(material.Texto, "/cantidad");
            CollectionAssert.AreEqual(
                new[] { ServicioConversacionTelegram.ConfirmarSi, ServicioConversacionTelegram.ConfirmarNo },
                material.Botones.Take(2).Select(b => b.Dato).ToArray());
            Assert.AreEqual(ServicioConversacionTelegram.PrefijoMenu + "/cancelar", material.Botones[^1].Dato);

            Assert.AreEqual(MensajesTelegram.OrdenEmitida, Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarSi)).Texto);
            Assert.AreEqual(MensajesTelegram.OrdenYaEmitida, Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarSi)).Texto);

            var orden = _interacciones.Ordenes.Single();
            Assert.AreEqual(idInteraccion, orden.IdInteraccion);
            Assert.AreEqual(EstadoInteraccion.Autorizada, _interacciones.LeerInteraccion(idInteraccion)!.Estado);
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_OtroMensajeGuardadoALaVez_RespondeMensajeSimultaneoSinEmitirOrden()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);
            _conversaciones.AlEscribir = () => _conversaciones.IncrementarVersion(Vinculado);

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarNo));

            Assert.AreEqual(MensajesTelegram.MensajeSimultaneo, r.Texto);
            Assert.AreEqual(EstadoConversacion.EsperandoConfirmacion, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task ProcesarAsync_Invitado_IniciaSinUsuarioYConfirmaUnaSolaOrden()
        {
            var r = Unica(await IniciarInvitadoPorQrAsync());
            StringAssert.Contains(r.Texto, _caneca.Nombre);
            var sesion = _conversaciones.Leer(Invitado)!;
            Assert.IsNotNull(sesion.IdInteraccion);
            Assert.IsNull(_interacciones.LeerInteraccion(sesion.IdInteraccion.Value)!.IdUsuario);
            Assert.AreEqual(CanalInteraccion.Telegram, _interacciones.LeerInteraccion(sesion.IdInteraccion.Value)!.Canal);
            await BotonAsync(MaterialPet, Invitado);
            await Task.WhenAll(BotonAsync(ServicioConversacionTelegram.ConfirmarSi, Invitado), BotonAsync(ServicioConversacionTelegram.ConfirmarSi, Invitado));
            Assert.AreEqual(EstadoConversacion.OrdenEmitida, _conversaciones.Leer(Invitado)!.Estado);
            var invitados = new ServicioInvitadosTelegram(_conversaciones, _servicioInteracciones, _reloj);
            var consulta = await invitados.ConsultarAsync(Invitado, sesion.IdInteraccion.Value, $"telegram:{sesion.IdInteraccion}:{_pet.Id}");
            Assert.IsTrue(consulta.Exitoso, consulta.Mensaje);
            Assert.IsNull(consulta.Valor.Deposito, "Una orden no es un depósito.");
            StringAssert.Contains(Unica(await TextoAsync("/estado", Invitado)).Texto, "sin ecopuntos");
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await TextoAsync("/ecopuntos", Invitado)).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_ChatDeGrupoOAjeno_SeDescartaSinResponder()
        {
            var grupo = await _servicio.ProcesarAsync(new MensajeEntrante(1, Vinculado, -100_200_300, false, "/ayuda", null));
            var ajeno = await _servicio.ProcesarAsync(new MensajeEntrante(2, Vinculado, Invitado, true, "/ayuda", null));

            Assert.IsEmpty(grupo);
            Assert.IsEmpty(ajeno);
            Assert.IsNull(_conversaciones.Leer(Vinculado));
        }

        [TestMethod]
        public async Task ProcesarAsync_UpdateRepetido_SeProcesaUnaSolaVez()
        {
            var mensaje = new MensajeEntrante(42, Vinculado, Vinculado, true, "/start " + _caneca.IdentificadorQr, null);

            Assert.HasCount(1, await _servicio.ProcesarAsync(mensaje));
            Assert.IsEmpty(await _servicio.ProcesarAsync(mensaje));
        }

        [TestMethod]
        public async Task ProcesarAsync_LimitadorCerrado_RespondeDemasiadosMensajesSinCambiarEstado()
        {
            _limitador.Permitido = false;

            var r = Unica(await TextoAsync("/start " + _caneca.IdentificadorQr));

            Assert.AreEqual(MensajesTelegram.DemasiadosMensajes, r.Texto);
            Assert.IsNull(_conversaciones.Leer(Vinculado));
        }

        [TestMethod]
        public async Task ProcesarAsync_BotonDespuesDeQuinceMinutos_VenceYRechazaLaInteraccion()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);
            int idInteraccion = IdInteraccionActual;
            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaConversacion + TimeSpan.FromSeconds(1));

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarSi));

            Assert.AreEqual(MensajesTelegram.ConversacionVencida, r.Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
            Assert.IsTrue(_interacciones.LeerInteraccion(idInteraccion)!.EstaCerrada);
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        [DataRow("mat:999")]
        [DataRow("mat:abc")]
        [DataRow("mat:-1")]
        public async Task ProcesarAsync_MaterialAlterado_RespondeMaterialNoValido(string dato)
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);

            Assert.AreEqual(MensajesTelegram.MaterialNoValido, Unica(await BotonAsync(dato)).Texto);
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_MaterialQueLaCanecaNoAdmite_RespondeMaterialNoValido()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoMaterial + _vidrio.Id));

            Assert.AreEqual(MensajesTelegram.MaterialNoValido, r.Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_ConfirmarSinMaterial_RespondeNadaQueConfirmar()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);

            Assert.AreEqual(MensajesTelegram.NadaQueConfirmar, Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarSi)).Texto);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task ProcesarAsync_NoCambiar_VuelveALosMateriales()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.ConfirmarNo));

            Assert.AreEqual(MensajesTelegram.ElegirMaterial, r.Texto);
            Assert.AreEqual(MaterialPet, r.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoMaterial, StringComparison.Ordinal)).Dato);
        }

        [TestMethod]
        public async Task ProcesarAsync_Cancelar_RechazaLaInteraccionYReinicia()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            int idInteraccion = IdInteraccionActual;

            Assert.AreEqual(MensajesTelegram.Cancelado, Unica(await TextoAsync("/cancelar")).Texto);
            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(idInteraccion)!.Estado);
            Assert.AreEqual(MensajesTelegram.NadaQueCancelar, Unica(await TextoAsync("/cancelar")).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_QrNuevo_ReemplazaLaSolicitudPendiente()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            int primera = IdInteraccionActual;

            await TextoAsync("/start " + _caneca.IdentificadorQr);

            Assert.AreEqual(EstadoInteraccion.Rechazada, _interacciones.LeerInteraccion(primera)!.Estado);
            Assert.AreNotEqual(primera, IdInteraccionActual);
        }

        [TestMethod]
        public async Task ProcesarAsync_Desvincular_CierraElVinculo()
        {
            Assert.AreEqual(MensajesTelegram.Desvinculado, Unica(await TextoAsync("/desvincular")).Texto);
            Assert.AreEqual(MensajesTelegram.SinVinculo, Unica(await TextoAsync("/desvincular")).Texto);
            Assert.IsFalse(_vinculos.Vinculos.Single().Activo);
        }

        [TestMethod]
        public async Task ProcesarAsync_VincularInvitado_EntregaEnlaceHttpsConElToken()
        {
            var r = Unica(await TextoAsync("/vincular", Invitado));

            var esperado = EnlaceTelegram.ParaRegistro(PaginaRegistro, "token-de-prueba-0001");
            Assert.AreEqual(MensajesTelegram.EnlaceRegistro(esperado), r.Texto);
            Assert.HasCount(1, _vinculos.Tokens);
        }

        [TestMethod]
        public async Task ProcesarAsync_VincularCuentaVinculada_RespondeYaVinculada()
        {
            Assert.AreEqual(MensajesTelegram.YaVinculada, Unica(await TextoAsync("/vincular")).Texto);
            Assert.IsEmpty(_vinculos.Tokens);
        }

        [TestMethod]
        public async Task ProcesarAsync_CanecaFueraDeServicio_NoIniciaInteraccion()
        {
            var averiada = _contenedores.Sembrar(1, EstadoContenedor.FueraDeServicio, _pet.Id);

            var r = Unica(await TextoAsync("/start " + averiada.IdentificadorQr));

            Assert.AreEqual(MensajesTelegram.CanecaNoDisponible, r.Texto);
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        [DataRow("/start QR-INEXISTENTE")]
        [DataRow("/start <script>")]
        public async Task ProcesarAsync_QrDesconocidoOMalFormado_NoIniciaInteraccion(string texto)
        {
            var r = Unica(await TextoAsync(texto));

            Assert.IsTrue(r.Texto is MensajesTelegram.QrDesconocido or MensajesTelegram.Bienvenida, r.Texto);
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Vinculado)!.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_ComandoDesconocido_RespondeNoEntendido()
        {
            Assert.AreEqual(MensajesTelegram.NoEntendido, Unica(await TextoAsync("/comando")).Texto);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("   ")]
        public async Task ProcesarAsync_MensajeSinTexto_RespondeNoEntendido(string? texto)
        {
            Assert.AreEqual(MensajesTelegram.NoEntendido, Unica(await TextoAsync(texto!)).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_InfoConOrdenYaEmitida_NoOfreceBotonDeMaterial()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            await BotonAsync(MaterialPet);
            await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            ConversacionTelegram antes = _conversaciones.Leer(Vinculado)!;

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + _pet.Id));

            StringAssert.Contains(r.Texto, "Sí lo recibe");
            Assert.IsEmpty(r.Botones);
            ConversacionTelegram despues = _conversaciones.Leer(Vinculado)!;
            Assert.AreEqual(antes.Estado, despues.Estado);
            Assert.AreEqual(antes.IdInteraccion, despues.IdInteraccion);
            Assert.AreEqual(antes.FechaActualizacionUtc, despues.FechaActualizacionUtc);
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task ProcesarAsync_BotonOfrecidoPorInfoConMaterialRetirado_NoAvanza()
        {
            Contenedor doble = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id, _vidrio.Id);
            await TextoAsync("/start " + doble.IdentificadorQr);
            string dato = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + _vidrio.Id)).Botones.Single().Dato;
            await _materiales.DeshabilitarAsync(_vidrio.Id);

            await BotonAsync(dato);

            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.IsEmpty(_interacciones.Ordenes);
        }

        [TestMethod]
        public async Task ProcesarAsync_TextoSinCoincidencias_RespondeMaterialNoEncontrado()
        {
            Assert.AreEqual(MensajesTelegram.MaterialNoEncontrado, Unica(await TextoAsync("xyzqw")).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_Separar_MuestraLosTresColoresYLaRutaDePeligrosos()
        {
            string texto = Unica(await TextoAsync("/separar", Invitado)).Texto;

            StringAssert.Contains(texto, "Resolución 2184 de 2019");
            StringAssert.Contains(texto, "Caneca blanca");
            StringAssert.Contains(texto, "Caneca verde");
            StringAssert.Contains(texto, "Caneca negra");
            StringAssert.Contains(texto, "Ruta especial (especial o peligroso)");
        }

        [TestMethod]
        public async Task ProcesarAsync_Categorias_EsPublicoYMuestraLaMismaGuiaQueSeparar()
        {
            string categorias = Unica(await TextoAsync("/categorias", Invitado)).Texto;

            Assert.AreEqual(MensajesTelegram.Separacion(GuiaSeparacion.Todas), categorias);
        }

        [TestMethod]
        public async Task ProcesarAsync_MaterialesSinCuenta_MuestraElCatalogoConBotonesInfoYNoCambiaElEstado()
        {
            var r = Unica(await TextoAsync("/materiales", Invitado));

            StringAssert.StartsWith(r.Texto, "Catálogo de materiales de EcoAccess.");
            CollectionAssert.IsSubsetOf(
                new[] { ServicioConversacionTelegram.PrefijoInfo + _pet.Id, ServicioConversacionTelegram.PrefijoInfo + _vidrio.Id },
                r.Botones.Select(b => b.Dato).ToArray());
            Assert.IsTrue(r.Botones.All(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoInfo, StringComparison.Ordinal)),
                "El catálogo público solo informa: ningún botón elige material para depositar.");
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Invitado)!.Estado);
        }

        [TestMethod]
        public async Task ProcesarAsync_Ayuda_AnunciaLosComandosPublicosDelCatalogo()
        {
            string ayuda = Unica(await TextoAsync("/ayuda", Invitado)).Texto;

            StringAssert.Contains(ayuda, "/materiales");
            StringAssert.Contains(ayuda, "/categorias");
            StringAssert.Contains(ayuda, "nunca te pedirá tu contraseña");
        }

        [TestMethod]
        public async Task ProcesarAsync_TextoConUnaCoincidenciaSinCaneca_ExplicaElMaterialSinBotones()
        {
            var r = Unica(await TextoAsync("pet", Invitado));

            StringAssert.StartsWith(r.Texto, _pet.Nombre + ": aprovechable, caneca blanca");
            StringAssert.Contains(r.Texto, "escanea su código QR");
            Assert.IsEmpty(r.Botones);
            Assert.AreEqual(EstadoConversacion.SinCaneca, _conversaciones.Leer(Invitado)!.Estado, "Consultar el catálogo no cambia el estado.");
        }

        [TestMethod]
        public async Task ProcesarAsync_TextoConVariasCoincidencias_OfreceBotonesInfo()
        {
            var r = Unica(await TextoAsync("material"));

            Assert.AreEqual(MensajesTelegram.VariosMateriales, r.Texto);
            CollectionAssert.AreEquivalent(
                new[] { ServicioConversacionTelegram.PrefijoInfo + _pet.Id, ServicioConversacionTelegram.PrefijoInfo + _vidrio.Id },
                r.Botones.Select(b => b.Dato).ToArray());
        }

        [TestMethod]
        public async Task ProcesarAsync_InfoConCanecaQueLoRecibe_OfreceElBotonDeMaterialQueValidaDeNuevo()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + _pet.Id));

            StringAssert.Contains(r.Texto, "Sí lo recibe la caneca «" + _caneca.Nombre + "».");
            Assert.AreEqual(MaterialPet, r.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.PrefijoMaterial, StringComparison.Ordinal)).Dato);
            Assert.AreEqual(EstadoConversacion.CanecaIdentificada, _conversaciones.Leer(Vinculado)!.Estado);
            Assert.IsEmpty(_interacciones.Ordenes, "Consultar no emite órdenes.");

            var confirmar = Unica(await BotonAsync(r.Botones.Single().Dato));
            Assert.AreEqual(EstadoConversacion.EsperandoConfirmacion, _conversaciones.Leer(Vinculado)!.Estado, confirmar.Texto);
            Assert.IsEmpty(_interacciones.Ordenes, "El botón lleva a la confirmación, no a la apertura.");
        }

        [TestMethod]
        public async Task ProcesarAsync_InfoConCanecaQueNoLoRecibe_LoDiceSinBotones()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + _vidrio.Id));

            StringAssert.StartsWith(r.Texto, _vidrio.Nombre + ": aprovechable, caneca blanca");
            StringAssert.Contains(r.Texto, "No lo recibe la caneca «" + _caneca.Nombre + "».");
            Assert.IsEmpty(r.Botones);
        }

        [TestMethod]
        public async Task ProcesarAsync_InvitadoConCanecaQueLoRecibe_PermiteElegirMaterial()
        {
            await IniciarInvitadoPorQrAsync();

            var r = Unica(await TextoAsync("PET", Invitado));

            StringAssert.Contains(r.Texto, "Sí lo recibe");
            Assert.IsTrue(r.Botones.Any(b => b.Dato == MaterialPet));
        }

        [TestMethod]
        public async Task ProcesarAsync_InfoConConversacionVencida_RespondeSinCanecaYSinCancelar()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr);
            int idInteraccion = IdInteraccionActual;
            _reloj.Avanzar(ServicioConversacionTelegram.VigenciaConversacion + TimeSpan.FromSeconds(1));

            var r = Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + _pet.Id));

            StringAssert.Contains(r.Texto, "escanea su código QR");
            Assert.IsEmpty(r.Botones);
            Assert.AreEqual(idInteraccion, IdInteraccionActual, "La consulta no toca la solicitud; la vigencia la resuelve el flujo normal.");
        }

        [TestMethod]
        [DataRow("info:")]
        [DataRow("info:abc")]
        [DataRow("info:-1")]
        [DataRow("info:999")]
        [DataRow("info:99999999999")]
        public async Task ProcesarAsync_InfoAlteradoOInexistente_RespondeMaterialNoEncontrado(string dato)
        {
            Assert.AreEqual(MensajesTelegram.MaterialNoEncontrado, Unica(await BotonAsync(dato)).Texto);
        }

        [TestMethod]
        public async Task ProcesarAsync_InfoDeMaterialDeshabilitado_RespondeMaterialNoEncontrado()
        {
            Material retirado = _materiales.Sembrar("RET", habilitado: false);

            Assert.AreEqual(MensajesTelegram.MaterialNoEncontrado,
                Unica(await BotonAsync(ServicioConversacionTelegram.PrefijoInfo + retirado.Id)).Texto);
        }
    }
}
