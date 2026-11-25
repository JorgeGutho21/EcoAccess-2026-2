using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Vision35;

namespace EcoAccess.P3.Logica.Servicios
{
    public sealed partial class ServicioConversacionTelegram
    {
        /// <summary>
        /// Coordinador unificado compartido con la estación Windows para comandos /canecas, /modo, /historial y
        /// apertura autorizada de 1, 2 o 3 canecas sin competir por el controlador serial.
        /// </summary>
        public CoordinadorEstacionUnificado? CoordinadorEstacion { get; set; }

        private string DescribirModoOperacion()
        {
            if (CoordinadorEstacion is not null)
                return "⚙️ Estado de la estación EcoAccess\n" + CoordinadorEstacion.DescribirModoActual();

            if (SinAperturaFisica)
                return "⚙️ Modo software (solo orientación): sin dispositivo físico ni simulador conectado. Eco orienta el destino sin abrir canecas.";

            return "⚙️ Estación enlazada mediante bridges locales: las aperturas se ejecutan por órdenes confirmadas.";
        }

        private async Task<RespuestaBot> ConsultarCanecasAsync(MensajeEntrante mensaje, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.AppendLine("🗑️ Estado de las canecas EcoAccess:");
            sb.AppendLine(CoordinadorEstacion is not null
                ? $"• {CoordinadorEstacion.DescribirModoActual()}"
                : $"• {(SinAperturaFisica ? "Modo software (sin apertura física)." : "Estación con bridges configurados.")}");

            try
            {
                var lista = await _contenedores.ListarAsync(ct);
                var relevantes = lista
                    .Where(c => c.Id is 1093 or 1400 or 1401 || c.EstaEnServicio)
                    .Take(6)
                    .ToList();
                if (relevantes.Count == 0)
                {
                    sb.AppendLine("• Verde (Orgánicos — ID 1400), Blanca (Aprovechables — ID 1093), Negra (No aprovechables — ID 1401).");
                }
                else
                {
                    foreach (var c in relevantes)
                    {
                        string estado = c.EstaEnServicio ? "EN SERVICIO" : "FUERA DE SERVICIO";
                        sb.AppendLine($"• #{c.Id} {c.Nombre}: {estado}");
                    }
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception)
            {
                sb.AppendLine("• No fue posible consultar la lista en Oracle ahora; la estación local sigue operativa.");
            }

            sb.Append("Comandos autorizados: /verde, /blanca, /negra, /abrir verde blanca, /abrir verde negra, /abrir blanca negra, /abrir verde blanca negra.");
            return Responder(mensaje, sb.ToString());
        }

        private async Task<RespuestaBot> HistorialEstacionAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken ct)
        {
            var sb = new StringBuilder();
            if (CoordinadorEstacion is not null && CoordinadorEstacion.EsChatAutorizado(mensaje.ChatId, mensaje.TelegramUserId))
            {
                var recientes = CoordinadorEstacion.Historial.Reverse().Take(5).ToList();
                sb.AppendLine("📋 Historial reciente de la estación (PC + Telegram):");
                if (recientes.Count == 0)
                {
                    sb.AppendLine("• Sin operaciones coordinadas en esta sesión.");
                }
                else
                {
                    foreach (var op in recientes)
                    {
                        string est = op.Exitosa ? "OK" : (op.Autorizada ? "INCIDENCIA" : "BLOQUEADA");
                        sb.AppendLine($"• [{op.FechaUtc:HH:mm:ss}Z] {op.Origen} | {op.Combinacion} | {op.ModoEjecucion} | {est}");
                    }
                }
            }

            if (idUsuario is not null)
            {
                RespuestaBot dep = await DepositosAsync(mensaje, idUsuario, ct);
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(dep.Texto);
                return Responder(mensaje, sb.ToString());
            }

            if (sb.Length == 0)
                return Responder(mensaje, MensajesTelegram.SinVinculoConsulta);

            return Responder(mensaje, sb.ToString());
        }

        private async Task<RespuestaBot> EjecutarComandoCanecasAsync(MensajeEntrante mensaje, int? idUsuario, CancellationToken ct)
        {
            if (!CoordinadorEstacionUnificado.IntentarParsearComandoCanecas(mensaje.Texto, out IReadOnlyList<CategoriaMaterial> categorias))
            {
                return Responder(mensaje,
                    "Formato no reconocido. Usa:\n" +
                    "• /verde, /blanca o /negra\n" +
                    "• /abrir verde blanca\n" +
                    "• /abrir verde negra\n" +
                    "• /abrir blanca negra\n" +
                    "• /abrir verde blanca negra");
            }

            if (EstacionExpo is not null) return await ComandoCanecasExpoAsync(mensaje, idUsuario, categorias, ct);

            if (CoordinadorEstacion is null)
            {
                return Responder(mensaje,
                    "El control directo de combinaciones de canecas no está habilitado en esta instancia. " +
                    "Usa el flujo habitual enviando una foto o escaneando el QR, o inicia el Modo ExpoSoftware.");
            }

            bool autorizado = CoordinadorEstacion.EsChatAutorizado(mensaje.ChatId, mensaje.TelegramUserId);
            bool solicitarFisico = CoordinadorEstacion.ModoActual == ModoOperacionHardware.HardwareFisicoSupervisado;
            string claveIdempotencia = $"tg:{mensaje.ChatId.ToString(CultureInfo.InvariantCulture)}:{mensaje.UpdateId.ToString(CultureInfo.InvariantCulture)}";
            string solicitante = idUsuario is int uid
                ? $"telegram:{mensaje.TelegramUserId} (usuario:{uid})"
                : $"telegram:{mensaje.TelegramUserId}";

            RegistroOperacionEstacion res = await CoordinadorEstacion.SolicitarAperturaAsync(
                claveIdempotencia,
                OrigenSolicitudEstacion.Telegram,
                solicitante,
                categorias,
                solicitarModoFisico: solicitarFisico,
                usuarioAutorizado: autorizado,
                ct: ct).ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine(res.Mensaje);
            foreach (var r in res.ResultadosCanecas)
            {
                string nombre = MotorReglasMultirresiduo.NombreCortoCaneca(r.Caneca);
                string icono = r.AperturaConfirmada ? "✅" : "⚠️";
                sb.AppendLine($"{icono} {nombre} (ID {r.IdContenedor}): {r.Mensaje}");
            }
            return Responder(mensaje, sb.ToString().TrimEnd());
        }
    }
}
