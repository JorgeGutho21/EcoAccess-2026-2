using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Contratos
{
    /// <summary>Por dónde llegó la foto.</summary>
    public enum CanalConsultaVision
    {
        Telegram = 1,
        Escritorio = 2,
        Evaluacion = 3
    }

    /// <summary>Si había un dispositivo capaz de abrir o solo se informó el destino.</summary>
    public enum ModoConsultaVision
    {
        Software = 1,
        ConDispositivo = 2
    }

    /// <summary>
    /// Una foto atendida por Echo: qué se reconoció y adónde va. No es un depósito ni una solicitud: no abre nada ni suma
    /// ecopuntos. No guarda la foto ni el identificador de Telegram.
    /// </summary>
    public sealed record ConsultaVisionRegistrada(
        string Clave,
        int? IdUsuario,
        CanalConsultaVision Canal,
        ModoConsultaVision Modo,
        string Proveedor,
        string? ClaveObjeto,
        ContextoObjeto? Contexto,
        ResultadoClasificacion Resultado,
        int? IdMaterial,
        AdmisionMaterial? Admision,
        int? IdContenedor,
        int LatenciaMs,
        DateTime FechaUtc);

    /// <summary>Lo que la persona consultó en un período.</summary>
    public sealed record ResumenConsultasVision(int Total, int ConDestino, int Abstenciones);

    public interface IRepositorioConsultasVision
    {
        /// <summary>Guarda la consulta; una clave repetida (la misma actualización de Telegram) no crea otra fila.</summary>
        Task GuardarAsync(ConsultaVisionRegistrada consulta, CancellationToken ct = default);

        Task<ResumenConsultasVision> ResumirDeUsuarioAsync(int idUsuario, DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default);
    }
}
