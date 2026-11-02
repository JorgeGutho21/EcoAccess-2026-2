using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>
    /// Un solo clasificador para el bot con dos motores intercambiables. Telegram, Oracle y el ESP32 no saben cuál respondió.
    /// En modo automático el externo solo actúa si el local falló de verdad o si la persona pidió una segunda opinión:
    /// una respuesta válida pero incierta del modelo local no es un fallo y no envía la foto a ninguna parte.
    /// </summary>
    public sealed class SelectorVision : IClasificadorMaterial, IDisposable
    {
        private readonly ModoVision _modo;
        private readonly IClasificadorMaterial? _local;
        private readonly IClasificadorMaterial? _externo;
        private readonly Action<string> _registrar;

        public SelectorVision(ModoVision modo, IClasificadorMaterial? local, IClasificadorMaterial? externo, TimeSpan tiempoMaximo, Action<string> registrar)
        {
            _modo = modo;
            _local = local;
            _externo = externo;
            TiempoMaximo = tiempoMaximo;
            _registrar = registrar ?? throw new ArgumentNullException(nameof(registrar));
        }

        public TimeSpan TiempoMaximo { get; }

        public string Proveedor => (_modo == ModoVision.Groq ? _externo : _local ?? _externo)?.Proveedor ?? "sin-proveedor";

        /// <summary>Con qué papel debe tratar la conversación al servicio externo.</summary>
        public PoliticaVisionExterna Politica => _externo is null ? PoliticaVisionExterna.Ninguna : _modo switch
        {
            ModoVision.Groq => PoliticaVisionExterna.Principal,
            ModoVision.Automatico => PoliticaVisionExterna.Respaldo,
            _ => PoliticaVisionExterna.Ninguna,
        };

        public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
        {
            switch (_modo)
            {
                case ModoVision.Groq:
                    return await PedirAsync(_externo, imagen, catalogo, ct).ConfigureAwait(false);
                case ModoVision.Automatico:
                    break;
                default:
                    return await PedirAsync(_local, imagen, catalogo, ct).ConfigureAwait(false);
            }

            if (imagen.Envio == EnvioExterno.SegundaOpinion && _externo is not null)
                return await PedirAsync(_externo, imagen, catalogo, ct).ConfigureAwait(false);
            if (_local is not null)
            {
                try { return await PedirAsync(_local, imagen, catalogo, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception error) when (_externo is not null)
                {
                    _registrar($"El reconocimiento local falló ({error.GetType().Name}); se evalúa el respaldo externo.");
                }
            }
            if (_externo is null) throw new InvalidOperationException("No hay un motor de reconocimiento disponible.");
            if (imagen.Envio == EnvioExterno.Prohibido) throw new VisionExternaNoAutorizadaException();
            return await PedirAsync(_externo, imagen, catalogo, ct).ConfigureAwait(false);
        }

        private static async Task<PropuestaVision> PedirAsync(IClasificadorMaterial? motor, ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct)
        {
            if (motor is null) throw new InvalidOperationException("El motor de reconocimiento elegido no está configurado.");
            PropuestaVision propuesta = await motor.ClasificarAsync(imagen, catalogo, ct).ConfigureAwait(false);
            return propuesta with { Proveedor = motor.Proveedor };
        }

        public void Dispose()
        {
            (_local as IDisposable)?.Dispose();
            (_externo as IDisposable)?.Dispose();
        }
    }
}
