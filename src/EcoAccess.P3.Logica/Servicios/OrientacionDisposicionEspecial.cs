using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Qué tan comprobado está un punto. Solo <see cref="Confirmado"/> se presenta sin advertencia.</summary>
    public enum EstadoVerificacionPunto
    {
        /// <summary>Una persona del equipo confirmó en el lugar o con el operador que funciona y recibe esas categorías.</summary>
        Confirmado = 1,

        /// <summary>Aparece en la fuente oficial del programa u operador, sin confirmar vigencia ni el objeto concreto.</summary>
        ListadoSinConfirmar = 2,

        /// <summary>Programa nacional sin dirección local: se nombra el programa, nunca un lugar.</summary>
        SoloPrograma = 3,
    }

    /// <summary>Un lugar o programa de entrega en Valledupar (tabla PUNTO_DISPOSICION, V026). Sin dirección = solo programa.</summary>
    public sealed record PuntoDisposicion(string Codigo, string Nombre, string? Direccion, string Municipio, IReadOnlyList<RutaDisposicion> Rutas,
        string Recibe, EstadoVerificacionPunto Estado, string Fuente, DateTime FechaVerificacion, string? Horario = null)
    {
        /// <summary>Búsqueda en Google Maps con el nombre y la dirección publicados: no inventa coordenadas.</summary>
        public string? EnlaceMapa => Direccion is null ? null
            : "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString($"{Nombre}, {Direccion}, {Municipio}");
    }

    /// <summary>Origen de los puntos: Oracle (V026) o, si la tabla no existe todavía, la semilla verificada en código.</summary>
    public interface IRepositorioPuntosDisposicion
    {
        Task<IReadOnlyList<PuntoDisposicion>> ListarAsync(string municipio, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Semilla verificada el 12/11/2026 con fuentes públicas. Es la misma que inserta V026. No hay direcciones inventadas: lo que la
    /// fuente no publica queda sin dirección y se presenta como programa.
    /// </summary>
    public static class CatalogoPuntosValledupar
    {
        public const string Municipio = "Valledupar";
        private static readonly DateTime Verificacion = new(2026, 11, 12, 0, 0, 0, DateTimeKind.Utc);

        public static IReadOnlyList<PuntoDisposicion> Semilla { get; } = new[]
        {
            new PuntoDisposicion("VUP-UNICENTRO-LITO", "Centro Comercial Unicentro Valledupar", "Cra. 9 #6-42", Municipio,
                new[] { RutaDisposicion.Raee, RutaDisposicion.PilasBaterias, RutaDisposicion.Bombillos },
                "Aparece en la lista de puntos de recolección de la Fundación Puntos Verdes Lito (RAEE, pilas y lámparas)",
                EstadoVerificacionPunto.ListadoSinConfirmar, "https://fundacionpuntosverdes.com/puntos-de-recoleccion/", Verificacion),
            new PuntoDisposicion("VUP-CORPOCESAR-AMARILLO", "CORPOCESAR (punto amarillo «Cierra el ciclo»)",
                "Km 2 vía La Paz, Lote 1 U.I.C. Casa e' Campo, frente a la feria ganadera", Municipio,
                new[] { RutaDisposicion.Peligroso },
                "Envases y empaques de insecticidas domésticos (programa posconsumo de plaguicidas)",
                EstadoVerificacionPunto.ListadoSinConfirmar, "https://www.corpocesar.gov.co/residuos-y-consumo-sostenible/programas-de-posconsumo", Verificacion),
            new PuntoDisposicion("PROG-PUNTO-AZUL", "Punto Azul (medicamentos posconsumo)", null, Municipio,
                new[] { RutaDisposicion.Medicamentos },
                "Medicamentos vencidos o parcialmente consumidos y sus empaques, en contenedores de droguerías y supermercados",
                EstadoVerificacionPunto.SoloPrograma, "https://www.corpocesar.gov.co/residuos-y-consumo-sostenible/programas-de-posconsumo", Verificacion),
            new PuntoDisposicion("PROG-PILAS-AMBIENTE", "Pilas con el Ambiente", null, Municipio,
                new[] { RutaDisposicion.PilasBaterias },
                "Pilas de uso doméstico (programa posconsumo nacional)",
                EstadoVerificacionPunto.SoloPrograma, "https://www.eltiempo.com/vida/educacion/como-deben-botarse-residuos-como-medicamentos-computadores-bombillos-y-pilas-36663", Verificacion),
            new PuntoDisposicion("PROG-LUMINA", "Lúmina (bombillos posconsumo)", null, Municipio,
                new[] { RutaDisposicion.Bombillos },
                "Bombillos fluorescentes y ahorradores (programa posconsumo nacional)",
                EstadoVerificacionPunto.SoloPrograma, "https://www.cvc.gov.co/documentos/residuos-posconsumo/puntos-de-recoleccion-posconsumo", Verificacion),
            new PuntoDisposicion("PROG-RED-POSCONSUMO", "Aplicación Red Posconsumo (Minambiente)", null, Municipio,
                new[] { RutaDisposicion.Raee, RutaDisposicion.PilasBaterias, RutaDisposicion.Medicamentos, RutaDisposicion.Bombillos, RutaDisposicion.Peligroso },
                "Buscador oficial de contenedores posconsumo cercanos",
                EstadoVerificacionPunto.SoloPrograma, "https://www.eltiempo.com/vida/medio-ambiente/donde-se-puede-botar-basura-peligrosa-en-colombia-88872", Verificacion),
        };
    }

    /// <summary>
    /// Fase 7 de la misión final: cuando Eco reconoce algo que NO va en las canecas, dice adónde llevarlo en Valledupar. Nunca
    /// abre nada. Un punto sin confirmar se muestra con la advertencia de confirmar antes de desplazarse; si no hay punto con
    /// dirección, se nombra el programa y se dice que la ubicación está pendiente de verificación.
    /// </summary>
    public sealed class ServicioOrientacionEspecial
    {
        private readonly IRepositorioPuntosDisposicion? _repositorio;

        public ServicioOrientacionEspecial(IRepositorioPuntosDisposicion? repositorio = null) { _repositorio = repositorio; }

        public async Task<IReadOnlyList<PuntoDisposicion>> PuntosAsync(RutaDisposicion ruta, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PuntoDisposicion> todos = CatalogoPuntosValledupar.Semilla;
            if (_repositorio is not null)
            {
                try
                {
                    IReadOnlyList<PuntoDisposicion> oracle = await _repositorio.ListarAsync(CatalogoPuntosValledupar.Municipio, cancellationToken).ConfigureAwait(false);
                    if (oracle.Count > 0) todos = oracle;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch (Exception) { /* Oracle no disponible o sin V026: se usa la semilla verificada. */ }
            }
            return todos.Where(p => p.Rutas.Contains(ruta))
                .OrderBy(p => p.Estado).ThenBy(p => p.Nombre, StringComparer.Ordinal).ToList();
        }

        /// <summary>Líneas para añadir a la respuesta de Eco, o null si la ruta es una caneca o no aplica.</summary>
        public async Task<string?> TextoAsync(RutaDisposicion ruta, string objeto, CancellationToken cancellationToken = default)
        {
            if (MatrizDecisionResiduos.EsCaneca(ruta) || ruta is RutaDisposicion.NoEsResiduo or RutaDisposicion.Evaluar) return null;
            IReadOnlyList<PuntoDisposicion> puntos = await PuntosAsync(ruta, cancellationToken).ConfigureAwait(false);
            return Texto(ruta, objeto, puntos);
        }

        internal static string Texto(RutaDisposicion ruta, string objeto, IReadOnlyList<PuntoDisposicion> puntos)
        {
            var lineas = new List<string>();
            PuntoDisposicion? lugar = puntos.FirstOrDefault(p => p.Direccion is not null);
            if (lugar is not null)
            {
                lineas.Add($"📍 Lugar: {lugar.Nombre}");
                lineas.Add($"🏠 Dirección: {lugar.Direccion}, {lugar.Municipio}");
                lineas.Add($"🗺️ Ubicación: {lugar.EnlaceMapa}");
                if (lugar.Horario is { } horario) lineas.Add($"🕒 Horario: {horario}");
                lineas.Add(lugar.Estado == EstadoVerificacionPunto.Confirmado
                    ? $"Antes de desplazarte, confirma que reciben {objeto}."
                    : $"⚠️ Aparece en la lista oficial del programa, pero no hemos confirmado que siga activo ni que reciba {objeto}: confírmalo antes de desplazarte.");
            }
            else
            {
                lineas.Add(ruta == RutaDisposicion.ReutilizacionDonacion
                    ? "📍 Aún no tenemos un punto de donación verificado en Valledupar: pendiente de verificación."
                    : "📍 Ubicación en Valledupar pendiente de verificación.");
            }
            foreach (PuntoDisposicion programa in puntos.Where(p => p.Direccion is null).Take(2))
                lineas.Add($"ℹ️ Programa: {programa.Nombre}.");
            return string.Join("\n", lineas);
        }
    }
}
