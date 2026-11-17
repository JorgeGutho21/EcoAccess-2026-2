using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Una caneca que hay que abrir y los objetos de la foto que van en ella.</summary>
    public sealed record AperturaPlanificada(DestinoResiduo Destino, CanecaEstacion Caneca, IReadOnlyList<int> Objetos);

    /// <summary>Lo que se abriría (una vez por caneca, en orden fijo) y lo que no, con su motivo.</summary>
    public sealed record PlanAperturas(IReadOnlyList<AperturaPlanificada> Aperturas, IReadOnlyList<(ObjetoEnFoto Objeto, string Motivo)> SinApertura);

    /// <summary>Resultado de una caneca del plan.</summary>
    public sealed record ResultadoApertura(AperturaPlanificada Apertura, int IdOrden, string ClaveIdempotencia, ResultadoEjecucionApertura Ejecucion)
    {
        public bool Completada => Ejecucion.Estado == EstadoEjecucionApertura.Completada;
    }

    /// <summary>
    /// Coordinador de aperturas múltiples (fase 5 de la misión nocturna). Recibe los objetos de una foto, valida la admisión
    /// de cada uno, agrupa por caneca, elimina aperturas repetidas (tres botellas = una sola apertura de la blanca) y las
    /// ejecuta UNA DESPUÉS DE OTRA. Un fallo en una caneca no cancela las demás: cada una queda con su propio resultado.
    /// <para>
    /// Esta versión solo acepta un ejecutor simulado (<see cref="IEjecutorAperturas.EsSimulado"/>): la apertura física de dos
    /// o tres canecas queda pendiente de la prueba supervisada. El constructor lanza con cualquier otro ejecutor.
    /// </para>
    /// </summary>
    public sealed class CoordinadorAperturasMultiples
    {
        /// <summary>Orden fijo de apertura. La negra va antes que la verde para que lo sanitario no espere junto a la comida.</summary>
        public static readonly IReadOnlyList<DestinoResiduo> Orden = new[] { DestinoResiduo.Blanca, DestinoResiduo.Negra, DestinoResiduo.Verde };

        private readonly IEjecutorAperturas _ejecutor;
        private readonly TimeProvider _reloj;
        private readonly TimeSpan _vigencia;
        private int _siguienteOrden;

        public CoordinadorAperturasMultiples(IEjecutorAperturas ejecutor, TimeProvider reloj, TimeSpan? vigencia = null, int primeraOrden = 1)
        {
            _ejecutor = ejecutor ?? throw new ArgumentNullException(nameof(ejecutor));
            if (!ejecutor.EsSimulado)
                throw new InvalidOperationException("La apertura de varias canecas solo funciona con el simulador hasta la prueba física supervisada.");
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
            _vigencia = vigencia ?? ServicioInteracciones.VigenciaOrden;
            _siguienteOrden = primeraOrden - 1;
        }

        /// <summary>
        /// Arma el plan sin ejecutar nada. Solo entran objetos elegibles cuyo destino tiene una caneca EN SERVICIO en la
        /// estación; lo demás queda en <see cref="PlanAperturas.SinApertura"/> con una explicación.
        /// </summary>
        public static PlanAperturas Planificar(IReadOnlyList<ObjetoEnFoto> objetos, IReadOnlyList<CanecaEstacion> estacion)
        {
            Compat.ThrowIfNull(objetos, nameof(objetos));
            Compat.ThrowIfNull(estacion, nameof(estacion));
            var sin = new List<(ObjetoEnFoto, string)>();
            var porDestino = new Dictionary<DestinoResiduo, (CanecaEstacion Caneca, List<int> Objetos)>();
            foreach (ObjetoEnFoto objeto in objetos)
            {
                if (!objeto.ElegibleApertura) { sin.Add((objeto, objeto.Motivo ?? "No es elegible para abrir una caneca.")); continue; }
                DecisionDestino decision = ResolutorDestino.Resolver(objeto.Destino, estacion);
                if (decision.Caneca is not { } caneca) { sin.Add((objeto, $"La estación no tiene caneca {ResolutorDestino.Color(objeto.Destino)}.")); continue; }
                if (!caneca.EnServicio) { sin.Add((objeto, $"La caneca {ResolutorDestino.Color(objeto.Destino)} está fuera de servicio.")); continue; }
                if (porDestino.TryGetValue(objeto.Destino, out var grupo)) grupo.Objetos.Add(objeto.Ordinal);
                else porDestino[objeto.Destino] = (caneca, new List<int> { objeto.Ordinal });
            }
            var aperturas = Orden.Where(porDestino.ContainsKey)
                .Select(d => new AperturaPlanificada(d, porDestino[d].Caneca, porDestino[d].Objetos)).ToList();
            return new PlanAperturas(aperturas, sin);
        }

        /// <summary>
        /// Ejecuta el plan en orden, una caneca a la vez. La clave de idempotencia es «operación:color»: repetir la misma
        /// operación (mensaje duplicado de Telegram) no vuelve a abrir ninguna caneca.
        /// </summary>
        public async Task<IReadOnlyList<ResultadoApertura>> EjecutarAsync(string operacion, PlanAperturas plan, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(operacion) || operacion.Length > 60) throw new ArgumentException("Operación inválida.", nameof(operacion));
            Compat.ThrowIfNull(plan, nameof(plan));
            var resultados = new List<ResultadoApertura>(plan.Aperturas.Count);
            foreach (AperturaPlanificada apertura in plan.Aperturas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int idOrden = Interlocked.Increment(ref _siguienteOrden);
                string clave = $"{operacion}:{ResolutorDestino.Color(apertura.Destino)}";
                var comando = new ComandoApertura(idOrden, apertura.Caneca.IdContenedor, clave, _reloj.GetUtcNow().UtcDateTime.Add(_vigencia));
                ResultadoEjecucionApertura ejecucion;
                try { ejecucion = await _ejecutor.AbrirAsync(comando, cancellationToken).ConfigureAwait(false); }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception) { ejecucion = new ResultadoEjecucionApertura(EstadoEjecucionApertura.SinRespuesta, Array.Empty<Entidades.TipoEventoDispositivo>()); }
                resultados.Add(new ResultadoApertura(apertura, idOrden, clave, ejecucion));
            }
            return resultados;
        }

        /// <summary>Resumen honesto de la ejecución simulada. Nunca dice que una tapa real se abrió.</summary>
        public static string Resumen(IReadOnlyList<ResultadoApertura> resultados)
        {
            Compat.ThrowIfNull(resultados, nameof(resultados));
            if (resultados.Count == 0) return "🧪 Simulación: no había canecas que abrir.";
            var lineas = resultados.Select(r => $"{ResolutorDestino.Color(r.Apertura.Destino)}: {Texto(r.Ejecucion.Estado)}");
            return "🧪 Simulación (sin movimiento físico): " + string.Join(" · ", lineas) + ".";
        }

        private static string Texto(EstadoEjecucionApertura estado) => estado switch
        {
            EstadoEjecucionApertura.Completada => "abrió y cerró en el simulador",
            EstadoEjecucionApertura.Duplicada => "ya se había ejecutado, no se repite",
            EstadoEjecucionApertura.Desconectada => "estación desconectada",
            EstadoEjecucionApertura.FalloApertura => "la tapa no abrió",
            EstadoEjecucionApertura.FalloCierre => "la tapa no cerró",
            EstadoEjecucionApertura.SinRespuesta => "sin respuesta",
            _ => "orden rechazada",
        };
    }
}
