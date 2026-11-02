using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using Microsoft.Extensions.Configuration;

namespace EcoAccess.P3.Bot.Vision
{
    /// <summary>Activa el reconocimiento local con Qwen cuando la configuración lo permite; sin él, el bot ofrece selección manual.</summary>
    public sealed class SeleccionVisionLocal : IClasificadorMaterial, IDisposable
    {
        private readonly ClasificadorOllamaLocal _clasificador;

        private SeleccionVisionLocal(ClasificadorOllamaLocal clasificador)
        {
            _clasificador = clasificador;
        }

        public string Proveedor => _clasificador.Proveedor;

        public TimeSpan TiempoMaximo => ClasificadorOllamaLocal.TiempoMaximoInferencia + TimeSpan.FromSeconds(10);

        public static SeleccionVisionLocal? Crear(IConfiguration configuracion, Action<string> registrar)
        {
            var elegido = configuracion["Vision:Proveedor"];
            if (Compat.IsNullOrWhiteSpace(elegido)) elegido = "OllamaLocal";
            if (elegido == "Desactivada") return null;
            if (elegido == "OllamaLocal" && configuracion["Vision:Ollama:ServidorSinNubeVerificado"] == "true")
            {
                try
                {
                    return new SeleccionVisionLocal(new ClasificadorOllamaLocal(configuracion["Vision:Ollama:MantenerCargado"] ?? "15m"));
                }
                catch (ArgumentException)
                {
                    registrar("La configuración de Ollama es inválida. La selección manual sigue disponible.");
                    return null;
                }
            }
            registrar("Reconocimiento local no disponible o configuración no verificada. La selección manual sigue disponible.");
            return null;
        }

        public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<EcoAccess.P3.Entidades.Material> catalogo, CancellationToken ct = default) =>
            _clasificador.ClasificarAsync(imagen, catalogo, ct);

        public void Dispose() => _clasificador.Dispose();
    }
}
