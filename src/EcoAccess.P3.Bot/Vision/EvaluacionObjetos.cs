using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Vision
{

    /// <param name="Aceptadas">Claves de CatalogoObjetos correctas para esta foto.</param>
    /// <param name="Contexto">Nombre de <see cref="ContextoObjeto"/> esperado.</param>
    /// <param name="Admisible">Si es un aprovechable que la caneca piloto debería recibir.</param>
    public sealed record MuestraObjeto(string Archivo, string Sha256, string Clase, string[] Aceptadas, string Contexto, bool Admisible);

    public sealed record ManifiestoObjetos(string Nombre, string Licencia, string Conjunto, MuestraObjeto[] Muestras);

    /// <summary>Qué respondió el motor a una foto y qué habría hecho Eco, sin imagen ni texto libre del modelo.</summary>
    public sealed record ResultadoObjeto(string Archivo, string Clase, string? Clave, string? Contexto, string Resultado, string? Material, string? Admision,
        bool Abriria, bool OfreceToque, bool Correcto, bool ContextoCorrecto, long Milisegundos, string? CanecaEstacion = null);

    public sealed record MetricaObjetos(string Grupo, int Muestras, int Correctas, int ContextoCorrecto);

    public sealed record InformeObjetos(string Conjunto, string Licencia, string Proveedor, DateTime FechaUtc, int Muestras, int Correctas, decimal Precision,
        int ContextoCorrecto, IReadOnlyList<MetricaObjetos> PorContexto, IReadOnlyList<MetricaObjetos> PorClase,
        int AjenasAlCatalogo, int AjenasReconocidas, int FalsasAdmisiones, int OfertasIndebidas, int FalsosRechazos, int AperturasCorrectas, int Abstenciones,
        int FallosTecnicos, long MedianaMs, long P95Ms, IReadOnlyList<ResultadoObjeto> Detalle);

    /// <summary>
    /// Mide un corpus etiquetado con el motor real y la admisión de la caneca indicada. Solo lee: no crea solicitudes, órdenes
    /// ni depósitos. Falsa admisión es que Eco abriría sola para algo que no debe recibir; es el error que más importa.
    /// </summary>
    public static class EvaluacionObjetos
    {
        public const int MaximoMuestras = 400;

        public static async Task<InformeObjetos> EjecutarAsync(string manifiesto, ServicioClasificacionImagen servicio, string proveedor, int? idCaneca,
            EnvioExterno envio, TimeSpan pausa, string archivoProgreso, CancellationToken ct = default)
        {
            var archivo = new FileInfo(Path.GetFullPath(manifiesto));
            if (!archivo.Exists || archivo.Length > 512 * 1024) throw new ArgumentException("El manifiesto debe existir y no superar 512 KiB.");
            ManifiestoObjetos documento = JsonSerializer.Deserialize<ManifiestoObjetos>(File.ReadAllText(archivo.FullName))
                ?? throw new ArgumentException("Manifiesto vacío.");
            if (documento.Muestras is not { Length: > 0 and <= MaximoMuestras } muestras) throw new ArgumentException($"El manifiesto debe tener de 1 a {MaximoMuestras} muestras.");
            foreach (MuestraObjeto muestra in muestras)
                if (muestra.Aceptadas.Length == 0 || muestra.Aceptadas.Any(c => CatalogoObjetos.Buscar(c) is null) || !Enum.TryParse(muestra.Contexto, out ContextoObjeto _))
                    throw new ArgumentException("Cada muestra necesita claves y contexto del catálogo de objetos.");

            // Lo ya medido no se repite: una interrupción de un proveedor de pago no obliga a enviar las fotos otra vez.
            var hechos = new Dictionary<string, ResultadoObjeto>(StringComparer.Ordinal);
            if (File.Exists(archivoProgreso))
                foreach (string linea in File.ReadAllLines(archivoProgreso))
                    if (JsonSerializer.Deserialize<ResultadoObjeto>(linea) is { } previo) hechos[previo.Archivo] = previo;

            string raiz = archivo.DirectoryName!;
            var detalle = new List<ResultadoObjeto>();
            for (int i = 0; i < muestras.Length; i++)
            {
                MuestraObjeto muestra = muestras[i];
                if (hechos.TryGetValue(muestra.Archivo, out ResultadoObjeto? hecho)) { detalle.Add(hecho); continue; }
                string ruta = Path.GetFullPath(Path.Combine(raiz, muestra.Archivo));
                if (!ruta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("La muestra sale del corpus.");
                byte[] bytes = File.ReadAllBytes(ruta);
                if (!string.Equals(Compat.ToHexString(Compat.Sha256(bytes)), muestra.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Una muestra no coincide con su hash.");
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                var r = await servicio.ClasificarAsync(new ImagenResiduo(bytes, null, envio), idCaneca, ct);
                reloj.Stop();
                if (!r.Exitoso) throw new InvalidOperationException("Una foto no pudo evaluarse.");
                ResultadoObjeto medido = Medir(muestra, r.Valor, reloj.ElapsedMilliseconds);
                detalle.Add(medido);
                // Un límite del proveedor no mide al modelo: esa foto se vuelve a intentar en la siguiente corrida.
                if (r.Valor.Resultado != ResultadoClasificacion.ProveedorNoDisponible)
                    File.AppendAllText(archivoProgreso, JsonSerializer.Serialize(medido) + Environment.NewLine);
                if (pausa > TimeSpan.Zero && i < muestras.Length - 1) await Task.Delay(pausa, ct);
            }
            return Resumir(documento, proveedor, muestras, detalle);
        }

        private static ResultadoObjeto Medir(MuestraObjeto muestra, ClasificacionImagen c, long ms)
        {
            string? clave = c.Reconocido?.Clave;
            if (clave is null && c.Resultado == ResultadoClasificacion.ImagenBorrosa) clave = CatalogoObjetos.NoDistinguible;
            if (clave is null && c.Resultado == ResultadoClasificacion.VariosObjetos) clave = CatalogoObjetos.VariosObjetos;
            string? contexto = CatalogoObjetos.Buscar(clave)?.Contexto.ToString();
            bool abriria = PoliticaAperturaAutomatica.Permite(c);
            bool toque = !abriria && c.Resultado == ResultadoClasificacion.Identificado && c.Consulta?.Admision == AdmisionMaterial.Admitido;
            return new ResultadoObjeto(muestra.Archivo, muestra.Clase, clave, contexto, c.Resultado.ToString(), c.Consulta?.Material.Codigo, c.Consulta?.Admision.ToString(),
                abriria, toque, clave is not null && muestra.Aceptadas.Contains(clave), contexto == muestra.Contexto, ms,
                // Verde o negra que se abriría sola en la estación (Abriria solo mide la blanca de la caneca evaluada).
                PoliticaAperturaAutomatica.CanecaEstacionPrevista(c)?.ToString());
        }

        private static InformeObjetos Resumir(ManifiestoObjetos documento, string proveedor, MuestraObjeto[] muestras, IReadOnlyList<ResultadoObjeto> detalle)
        {
            var parejas = muestras.Zip(detalle, (m, d) => (Muestra: m, Dato: d)).ToArray();
            IReadOnlyList<MetricaObjetos> Agrupar(Func<MuestraObjeto, string> llave) => parejas.GroupBy(p => llave(p.Muestra)).OrderBy(g => g.Key, StringComparer.Ordinal)
                .Select(g => new MetricaObjetos(g.Key, g.Count(), g.Count(p => p.Dato.Correcto), g.Count(p => p.Dato.ContextoCorrecto))).ToArray();
            string[] ajenos = { nameof(ContextoObjeto.AnimalVivo), nameof(ContextoObjeto.Persona), nameof(ContextoObjeto.PlantaViva), nameof(ContextoObjeto.ResiduoElectronico), nameof(ContextoObjeto.ObjetoCotidiano) };
            var ajenas = parejas.Where(p => ajenos.Contains(p.Muestra.Contexto)).ToArray();
            string[] sinDecision = { nameof(ResultadoClasificacion.Incierta), nameof(ResultadoClasificacion.ImagenBorrosa), nameof(ResultadoClasificacion.VariosObjetos), nameof(ResultadoClasificacion.ObjetoDesconocido) };
            bool EsperaAbstencion(MuestraObjeto m) => m.Aceptadas.Contains(CatalogoObjetos.NoDistinguible) || m.Aceptadas.Contains(CatalogoObjetos.VariosObjetos);
            int tecnicos = detalle.Count(d => d.Resultado is nameof(ResultadoClasificacion.ProveedorNoDisponible) or nameof(ResultadoClasificacion.CuotaGratuitaAgotada) or nameof(ResultadoClasificacion.AutorizacionExternaRequerida));
            long[] tiempos = detalle.Where(d => d.Milisegundos > 0).Select(d => d.Milisegundos).OrderBy(t => t).ToArray();
            int correctas = detalle.Count(d => d.Correcto);
            return new InformeObjetos(documento.Conjunto, documento.Licencia, proveedor, DateTime.UtcNow, parejas.Length, correctas,
                decimal.Round((decimal)correctas / parejas.Length, 4, MidpointRounding.AwayFromZero), detalle.Count(d => d.ContextoCorrecto),
                Agrupar(m => m.Contexto), Agrupar(m => m.Clase), ajenas.Length, ajenas.Count(p => p.Dato.ContextoCorrecto),
                parejas.Count(p => p.Dato.Abriria && (!p.Muestra.Admisible || !p.Dato.Correcto)),
                parejas.Count(p => p.Dato.OfreceToque && !p.Muestra.Admisible),
                parejas.Count(p => p.Muestra.Admisible && !p.Dato.Abriria && !p.Dato.OfreceToque),
                parejas.Count(p => p.Dato.Abriria && p.Muestra.Admisible && p.Dato.Correcto),
                parejas.Count(p => sinDecision.Contains(p.Dato.Resultado) && !EsperaAbstencion(p.Muestra)), tecnicos,
                tiempos.Length == 0 ? 0 : MetricasVision.Percentil(tiempos, 0.50), tiempos.Length == 0 ? 0 : MetricasVision.Percentil(tiempos, 0.95), detalle);
        }
    }
}
