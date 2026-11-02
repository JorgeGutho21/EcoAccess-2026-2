using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    public sealed partial class ServicioConversacionTelegram
    {
        private async Task<RespuestaBot> DeclararCantidadAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            if (usuario is null) return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            if (conversacion.IdInteraccion is not int solicitud || conversacion.Estado is not (EstadoConversacion.CanecaIdentificada or EstadoConversacion.EsperandoConfirmacion))
                return Responder(mensaje, "Identifica primero la caneca y una solicitud pendiente. No puedes cambiar la cantidad de una orden emitida.");
            string[] partes = (mensaje.Texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length != 2 || !int.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out int cantidad))
                return Responder(mensaje, "Escribe /cantidad 3 para declarar tres unidades, entre 1 y 1000. Esa declaración no es una verificación ni acredita puntos.");
            var r = await _interacciones.DeclararCantidadAsync(usuario.Value, solicitud, cantidad, ct);
            if (!r.Exitoso) return Responder(mensaje, r.Mensaje!);
            if (conversacion.IdMaterialPropuesto is int material && await _materiales.ObtenerPorIdAsync(material, ct) is { } elegido)
                return await MostrarDeclaracionAsync(mensaje, conversacion, usuario, elegido, ct);
            return Responder(mensaje, $"Cantidad declarada guardada: {cantidad} unidades. Elige el material con /manual; todavía no se acreditaron puntos.");
        }

        private async Task<RespuestaBot> PotencialesAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, CancellationToken ct)
        {
            if (usuario is null) return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            if (conversacion.Estado != EstadoConversacion.EsperandoConfirmacion || conversacion.IdMaterialPropuesto is not int material)
                return Responder(mensaje, "Elige el material de una solicitud pendiente. Si ya emitiste la orden, consulta /estado y /ecopuntos.");
            var elegido = await _materiales.ObtenerPorIdAsync(material, ct);
            return elegido is null ? Responder(mensaje, MensajesTelegram.MaterialNoValido)
                : await MostrarDeclaracionAsync(mensaje, conversacion, usuario, elegido, ct);
        }

        private async Task<RespuestaBot> MostrarDeclaracionAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, Material material, CancellationToken ct)
        {
            var botones = BotonesConfirmar(_vision is not null);
            if (usuario is null || _aperturaDirecta) return new(mensaje.ChatId, MensajesTelegram.Confirmar(material.Nombre), botones);
            var r = await _interacciones.ConsultarSolicitudPropiaAsync(usuario.Value, conversacion.IdInteraccion!.Value, ct);
            if (!r.Exitoso) return Responder(mensaje, r.Mensaje!);
            if (r.Valor.Estado != EstadoInteraccion.Iniciada)
                return Responder(mensaje, "La solicitud ya cambió de estado. Consulta /estado antes de continuar.");
            string texto = MensajesTelegram.Confirmar(material.Nombre);
            if (r.Valor.CantidadDeclarada is not int cantidad)
                texto += "\nCantidad sin declarar. Puedes indicar /cantidad 3 y consultar /potenciales antes de confirmar. No se estiman puntos sin una cantidad declarada.";
            else
            {
                botones[0] = new("✅ Confirmar esta declaración", $"{ConfirmarSi}:{conversacion.IdInteraccion}:{material.Id}:{cantidad}");
                texto += $"\nCantidad declarada: {cantidad} unidades.";
                if (_ecopuntos is null) texto += "\nLa consulta de tarifas no está disponible; no se inventan puntos potenciales.";
                else
                {
                    var cotizacion = await _ecopuntos.ConsultarPotencialesAsync(usuario.Value, material.Id, cantidad, ct);
                    texto += cotizacion.Exitoso
                        ? $"\nTarifa vigente: {cotizacion.Valor.Tarifa} puntos {(cotizacion.Valor.PorUnidad ? "por unidad verificada" : "por depósito verificado")}.\nPuntos potenciales: {cotizacion.Valor.PuntosPotenciales}."
                        : "\nNo se pudo estimar: " + cotizacion.Mensaje;
                }
                texto += "\nPuntos acreditados por esta solicitud: 0. La estimación reemplaza las anteriores, no reserva tarifa y requiere material y cantidad verificados. La acreditación utilizará la tarifa vigente entonces.";
            }
            botones.Insert(Math.Max(0, botones.Count - 1), new("✏️ Declarar o cambiar cantidad", PrefijoMenu + "/cantidad"));
            return new(mensaje.ChatId, texto, botones);
        }

        private async Task<RespuestaBot> ConfirmarDeclaracionAsync(MensajeEntrante mensaje, ConversacionTelegram conversacion, int? usuario, string dato, CancellationToken ct)
        {
            if (usuario is null) return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);
            string[] campos = dato.Split(':');
            if (campos.Length != 5 || !int.TryParse(campos[2], NumberStyles.None, CultureInfo.InvariantCulture, out int solicitud)
                || !int.TryParse(campos[3], NumberStyles.None, CultureInfo.InvariantCulture, out int material)
                || !int.TryParse(campos[4], NumberStyles.None, CultureInfo.InvariantCulture, out int cantidad)
                || cantidad is < 1 or > 1000 || solicitud != conversacion.IdInteraccion || material != conversacion.IdMaterialPropuesto)
                return Responder(mensaje, "Esta confirmación no corresponde a la solicitud actual. Consulta /potenciales.");
            var actual = await _interacciones.ConsultarSolicitudPropiaAsync(usuario.Value, solicitud, ct);
            if (!actual.Exitoso) return Responder(mensaje, actual.Mensaje!);
            if (actual.Valor.CantidadDeclarada != cantidad)
                return Responder(mensaje, "La cantidad cambió; consulta /potenciales y usa la confirmación actualizada.");
            return await ConfirmarAsync(mensaje, conversacion, usuario, ct, cantidad);
        }
    }
}
