using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Consultas agregadas de solo lectura sobre interacciones, órdenes, eventos y depósitos (CU-15).
    /// Va separada de <see cref="IRepositorioInteracciones"/> (ISP): quien registra eventos no necesita estadísticas
    /// y quien las consulta no puede escribir nada. El servicio valida el filtro antes de llamar.
    /// </summary>
    public interface IRepositorioEstadisticas
    {
        /// <summary>Conteos del período, cada uno en su propia categoría: nunca se suman entre sí.</summary>
        Task<ResumenActividad> ObtenerResumenAsync(FiltroEstadisticas filtro, CancellationToken cancellationToken = default);

        /// <summary>Depósitos agrupados, ordenados de más a menos depósitos y luego por etiqueta (o por día ascendente).</summary>
        Task<IReadOnlyList<FilaAgrupada>> AgruparDepositosAsync(
            FiltroEstadisticas filtro,
            AgrupacionDepositos agrupacion,
            CancellationToken cancellationToken = default);

        /// <summary>Canecas con algún fallo, revisión o vencimiento en el período, de más a menos incidencias.</summary>
        Task<IReadOnlyList<FilaFallosContenedor>> ListarFallosPorContenedorAsync(
            FiltroEstadisticas filtro,
            CancellationToken cancellationToken = default);

        /// <summary>Una página del detalle de depósitos, del más reciente al más antiguo, con el total sin paginar.</summary>
        Task<PaginaDepositos> ListarDepositosAsync(
            FiltroEstadisticas filtro,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default);
    }

    /// <summary>Qué depósitos cuentan según su verificación por un operario (RN-11).</summary>
    public enum EstadoVerificacion
    {
        Todos = 0,
        SinVerificar = 1,
        Verificado = 2
    }

    /// <summary>
    /// Material que se usa para filtrar y agrupar depósitos. Declarado es el que confirmó el ciudadano; Verificado es
    /// el que fijó un operario y deja fuera los depósitos sin verificar. Nunca se usa la sugerencia del clasificador.
    /// </summary>
    public enum CriterioMaterial
    {
        Declarado = 1,
        Verificado = 2
    }

    /// <summary>
    /// Agrupaciones del detalle de depósitos. Punto es la ubicación de la caneca, nunca la residencia del ciudadano
    /// (RN-13). Día es el día civil de Colombia (UTC−5, sin horario de verano).
    /// </summary>
    public enum AgrupacionDepositos
    {
        Material = 1,
        Contenedor = 2,
        Punto = 3,
        Dia = 4,
        Usuario = 5
    }

    /// <summary>
    /// Filtro común de todas las consultas. El período es semiabierto [DesdeUtc, HastaUtc) y se aplica a la fecha
    /// propia de cada registro: inicio de la interacción, emisión de la orden, recepción del evento y detección del
    /// depósito. Caneca y usuario filtran todo; material y verificación solo filtran depósitos, porque una
    /// interacción o una orden no prueban que el residuo haya entrado.
    /// </summary>
    /// <param name="IdUsuario">Ciudadano de la interacción; los eventos sin orden quedan fuera cuando se indica.</param>
    public sealed record FiltroEstadisticas(
        DateTime DesdeUtc,
        DateTime HastaUtc,
        int? IdContenedor = null,
        int? IdMaterial = null,
        int? IdUsuario = null,
        EstadoVerificacion Verificacion = EstadoVerificacion.Todos,
        CriterioMaterial Criterio = CriterioMaterial.Declarado);

    /// <summary>Solicitudes de apertura: muestran interés, no reciclaje.</summary>
    public sealed record ConteoInteracciones(
        int Total,
        int Iniciadas,
        int Autorizadas,
        int ConDeposito,
        int SinDeposito,
        int Rechazadas,
        int PorTelegram,
        int Manuales,
        int PorSimulador);

    /// <summary>
    /// Órdenes de apertura emitidas (aperturas autorizadas). ConAperturaConfirmada cuenta las que tienen un evento de
    /// apertura aplicado, es decir, que el sensor confirmó; las demás se autorizaron pero no consta que abrieran.
    /// </summary>
    public sealed record ConteoOrdenes(
        int Total,
        int Vigentes,
        int Cerradas,
        int Vencidas,
        int Fallidas,
        int ConAperturaConfirmada,
        int ConDeposito)
    {
        public int SinAperturaConfirmada => Total - ConAperturaConfirmada;
    }

    /// <summary>
    /// Mensajes recibidos de las canecas. Aplicados + EnRevision = Total: EnRevision son los que no se pudieron aplicar
    /// (RN-20). Fallos cuenta por tipo de evento y se cruza con lo anterior: un fallo puede estar aplicado o en revisión.
    /// </summary>
    public sealed record ConteoEventos(int Total, int Aplicados, int EnRevision, int Fallos);

    /// <summary>
    /// Depósitos detectados por el sensor. Los kilogramos medidos y estimados van en campos separados y nunca se
    /// suman (RN-12): un depósito sin báscula no aporta kilogramos reales.
    /// </summary>
    /// <param name="Coincidentes">Verificados cuyo material verificado es el declarado.</param>
    public sealed record ResumenDepositos(
        int Total,
        int Verificados,
        int Coincidentes,
        int ConPesoMedido,
        decimal KgMedidos,
        int ConPesoEstimado,
        decimal KgEstimados)
    {
        public int SinVerificar => Total - Verificados;
        public int Discrepantes => Verificados - Coincidentes;
    }

    public sealed record ResumenActividad(
        ConteoInteracciones Interacciones,
        ConteoOrdenes Ordenes,
        ConteoEventos Eventos,
        ResumenDepositos Depositos);

    /// <summary>Una fila de depósitos agrupados. Id es null en la agrupación por día; Dia es null en las demás.</summary>
    public sealed record FilaAgrupada(
        int? Id,
        DateOnly? Dia,
        string Etiqueta,
        int Depositos,
        int Verificados,
        int ConPesoMedido,
        decimal KgMedidos,
        int ConPesoEstimado,
        decimal KgEstimados);

    /// <summary>
    /// Incidencias de una caneca: cada columna es un hecho distinto y se muestra por separado. EventosEnRevision excluye
    /// los fallos, que ya tienen su columna, para que Incidencias cuente cada evento una sola vez (a diferencia de
    /// <see cref="ConteoEventos.EnRevision"/>, que sí los incluye).
    /// </summary>
    public sealed record FilaFallosContenedor(
        int IdContenedor,
        string Contenedor,
        int EventosFallo,
        int EventosEnRevision,
        int OrdenesFallidas,
        int OrdenesVencidas)
    {
        public int Incidencias => EventosFallo + EventosEnRevision + OrdenesFallidas + OrdenesVencidas;
    }

    /// <summary>Un depósito con los nombres ya resueltos. MaterialVerificado es null mientras no se verifique.</summary>
    /// <param name="IdUsuario">Ciudadano de la interacción; null en una apertura manual sin ciudadano.</param>
    public sealed record FilaDeposito(
        int IdDeposito,
        DateTime FechaDeteccionUtc,
        int IdContenedor,
        string Contenedor,
        int? IdUsuario,
        string MaterialDeclarado,
        string? MaterialVerificado,
        decimal? PesoMedidoKg,
        decimal? PesoEstimadoKg);

    public sealed record PaginaDepositos(IReadOnlyList<FilaDeposito> Filas, int Total, int Pagina, int TamanoPagina)
    {
        public int TotalPaginas => Total == 0 ? 0 : (Total + TamanoPagina - 1) / TamanoPagina;
    }
}
