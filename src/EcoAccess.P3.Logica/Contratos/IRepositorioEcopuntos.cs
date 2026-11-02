using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    public enum TipoOperacionEcopuntos { Acreditar = 1, Revertir = 2, Corregir = 3 }

    /// <summary>Solicitud normalizada; la misma clave solo puede representar el mismo contenido.</summary>
    public sealed record SolicitudEcopuntos(TipoOperacionEcopuntos Tipo, int IdDeposito, int? IdCredito,
        int? IdRegla, int IdActor, string Clave, string Motivo, DateTime FechaUtc);

    public sealed record ResumenEcopuntos(long Saldo, IReadOnlyList<MovimientoEcopuntos> Movimientos,
        long TotalAcreditado = 0, long TotalRevertido = 0);
    public sealed record OperacionEcopuntos(SolicitudEcopuntos Solicitud, IReadOnlyList<MovimientoEcopuntos> Movimientos);

    /// <summary>
    /// Guarda cada operación y sus movimientos en una transacción. Bloquea el depósito, revalida evidencia,
    /// titularidad, versión y crédito activo. Una corrección revierte y acredita el sucesor juntos.
    /// Reintentos idénticos devuelven el resultado persistido; payload diferente o carrera incompatible se rechaza.
    /// </summary>
    public interface IRepositorioEcopuntos
    {
        Task<int> AgregarReglaAsync(ReglaEcopuntos regla, CancellationToken ct = default);
        Task<ReglaEcopuntos?> ObtenerReglaAsync(int id, CancellationToken ct = default);
        Task<IReadOnlyList<ReglaEcopuntos>> ListarReglasAsync(CancellationToken ct = default);
        Task<MovimientoEcopuntos?> ObtenerMovimientoAsync(int id, CancellationToken ct = default);
        Task<OperacionEcopuntos?> ObtenerOperacionAsync(string clave, CancellationToken ct = default);
        Task<IReadOnlyList<MovimientoEcopuntos>> AplicarAsync(SolicitudEcopuntos solicitud, CancellationToken ct = default);
        /// <summary>Saldo sobre todo el libro y como máximo los últimos 50 movimientos.</summary>
        Task<ResumenEcopuntos> ConsultarAsync(int idUsuario, CancellationToken ct = default);
    }
}
