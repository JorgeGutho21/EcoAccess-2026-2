using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Hecho ya persistido que se avisa al ciudadano por Telegram.</summary>
    public enum TipoHechoNotificable
    {
        Apertura = 1,
        FalloCaneca = 2,
        CierreSinDeposito = 3,
        DepositoRegistrado = 4,
        DepositoVerificado = 5,
        EcopuntosAcreditados = 6,
        EcopuntosRevertidos = 7,
    }

    /// <summary>
    /// Un hecho de Oracle y el Telegram vinculado (vigente) del dueño de la orden. <see cref="Clave"/> es única por
    /// hecho y evita avisar dos veces; los datos vienen de las tablas, nunca se infieren en el bot.
    /// </summary>
    public sealed record HechoNotificable(
        string Clave,
        TipoHechoNotificable Tipo,
        long TelegramUserId,
        int IdOrden,
        int? IdDeposito,
        string? Material,
        int? Puntos,
        int? Saldo,
        DateTime FechaUtc,
        bool Simulacion = false);

    /// <summary>Consulta de solo lectura sobre eventos, depósitos y movimientos recientes de cuentas vinculadas.</summary>
    public interface IRepositorioNotificacionesTelegram
    {
        Task<IReadOnlyList<HechoNotificable>> ListarDesdeAsync(DateTime desdeUtc, CancellationToken cancellationToken = default);
    }
}
