using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    public sealed partial class ServicioConversacionTelegramTests
    {
        private Task<IReadOnlyList<RespuestaBot>> CantidadEnviarAsync(ServicioConversacionTelegram servicio, string? texto = null, string? boton = null) =>
            servicio.ProcesarAsync(new MensajeEntrante(++_update, Vinculado, Vinculado, true, texto, boton));

        [TestMethod]
        public async Task CantidadBot_PersisteEstimaYConfirmaUnaDeclaracionSinCrearCreditos()
        {
            var regla = new ReglaEcopuntos(1, _pet.Id, 2, "Tarifa sintética por unidad", Inicio.UtcDateTime.AddDays(-1), null, 99, true);
            var puntos = new ServicioEcopuntos(_usuarios, _interacciones, _materiales, new ConsultaPuntos(new ReglaEcopuntos[] { regla }), _reloj);
            var bot = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, ecopuntos: puntos);
            await CantidadEnviarAsync(bot, "/start " + _caneca.IdentificadorQr);
            await CantidadEnviarAsync(bot, boton: MaterialPet);
            var cotizacion = Unica(await CantidadEnviarAsync(bot, "/cantidad 3"));
            StringAssert.Contains(cotizacion.Texto, "Cantidad declarada: 3");
            StringAssert.Contains(cotizacion.Texto, "Puntos potenciales: 6");
            StringAssert.Contains(cotizacion.Texto, "Puntos acreditados por esta solicitud: 0");
            Assert.IsEmpty(_interacciones.Ordenes);
            string confirmar = cotizacion.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.ConfirmarSi + ":")).Dato;
            await CantidadEnviarAsync(bot, boton: confirmar); await CantidadEnviarAsync(bot, boton: confirmar);
            Assert.HasCount(1, _interacciones.Ordenes); Assert.IsEmpty(_interacciones.Depositos);
            Assert.AreEqual(3, _interacciones.LeerInteraccion(IdInteraccionActual)!.CantidadDeclarada);
        }

        [TestMethod]
        public async Task CantidadBot_BotonesAntiguosOManipuladosNoAutorizanYReinicioConservaCantidad()
        {
            await TextoAsync("/start " + _caneca.IdentificadorQr); await BotonAsync(MaterialPet);
            var anterior = Unica(await TextoAsync("/cantidad 3"));
            string boton = anterior.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.ConfirmarSi + ":")).Dato;
            await TextoAsync("/cantidad 4");
            await BotonAsync(boton); await BotonAsync(ServicioConversacionTelegram.ConfirmarSi);
            await BotonAsync(boton.Replace(":" + _pet.Id + ":", ":999999:"));
            Assert.IsEmpty(_interacciones.Ordenes);
            var nuevo = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj);
            var actual = Unica(await CantidadEnviarAsync(nuevo, "/potenciales"));
            StringAssert.Contains(actual.Texto, "Cantidad declarada: 4");
            await CantidadEnviarAsync(nuevo, boton: actual.Botones.Single(b => b.Dato.StartsWith(ServicioConversacionTelegram.ConfirmarSi + ":")).Dato);
            Assert.HasCount(1, _interacciones.Ordenes);
        }

        [TestMethod]
        public async Task CantidadBot_InvitadoYDatosInvalidosNoAlteranLaDeclaracion()
        {
            Assert.AreEqual(MensajesTelegram.SinVinculoConsulta, Unica(await TextoAsync("/cantidad 3", Invitado)).Texto);
            await TextoAsync("/start " + _caneca.IdentificadorQr); await BotonAsync(MaterialPet); await TextoAsync("/cantidad 3");
            foreach (string texto in new[] { "/cantidad", "/cantidad -1", "/cantidad 0", "/cantidad 1001", "/cantidad 3 4", "/cantidad texto" })
                await TextoAsync(texto);
            Assert.AreEqual(3, _interacciones.LeerInteraccion(IdInteraccionActual)!.CantidadDeclarada);
            Assert.IsEmpty(_interacciones.Ordenes);
        }
        [TestMethod]
        public async Task Invitado_QrDemoConservaCanalSimuladorYExigeSuSesionPrivada()
        {
            var bot = new ServicioConversacionTelegram(_conversaciones, _contenedores, _materiales,
                new ServicioVinculacionTelegram(_usuarios, _vinculos, new GeneradorTokensSecuencial(), _reloj),
                _servicioInteracciones, _limitador, PaginaRegistro, _reloj, qrDemo: _caneca.IdentificadorQr);
            Task<IReadOnlyList<RespuestaBot>> Enviar(string? texto = null, string? boton = null) =>
                bot.ProcesarAsync(new MensajeEntrante(++_update, Invitado, Invitado, true, texto, boton));
            await Enviar("/start " + _caneca.IdentificadorQr); await Enviar(boton: "menu:invitado");
            int id = _conversaciones.Leer(Invitado)!.IdInteraccion!.Value;
            Assert.AreEqual(CanalInteraccion.TelegramSimulado, _interacciones.LeerInteraccion(id)!.Canal);
            Assert.IsFalse((await _servicioInteracciones.AutorizarAperturaVisitanteAsync(id, _pet.Id, "intrusion-visitante")).Exitoso);
            Assert.IsFalse((await _servicioInteracciones.ConsultarOperacionVisitanteAsync(id, "intrusion-visitante")).Exitoso);
            var invitados = new ServicioInvitadosTelegram(_conversaciones, _servicioInteracciones, _reloj);
            Assert.IsFalse((await invitados.AutorizarAsync(Vinculado, id, _pet.Id, "ajena")).Exitoso);
            await Enviar(boton: MaterialPet); await Enviar(boton: ServicioConversacionTelegram.ConfirmarSi);
            Assert.HasCount(1, _interacciones.Ordenes);
            Assert.IsNull(_interacciones.LeerInteraccion(id)!.IdUsuario);
            Assert.IsTrue((await invitados.ConsultarAsync(Invitado, id, $"telegram:{id}:{_pet.Id}")).Exitoso);
        }

    }
}
