using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Bot.Vision
{
    /// <param name="Destinos">Destinos esperados, etiquetados a ojo: Blanca, Negra, Verde y/o Posconsumo.</param>
    /// <param name="Objetos">Residuos distintos visibles (aproximado).</param>
    public sealed record MuestraMultiobjeto(string Archivo, string Sha256, string[] Destinos, int Objetos, string Conjunto);

    public sealed record ManifiestoMultiobjeto(string Nombre, string Licencia, MuestraMultiobjeto[] Muestras);

    public sealed record ResultadoMultiobjeto(string Archivo, string Conjunto, string[] Esperados, string[] Predichos, string[] Canecas, string[] CanecasEsperadas,
        int ObjetosEsperados, int ObjetosPredichos, string[] Claves, bool CanecasExactas, long Milisegundos);

    public sealed record MetricaDestino(string Destino, int VerdaderosPositivos, int FalsosPositivos, int FalsosNegativos, decimal Precision, decimal Recall, decimal F1);

    public sealed record InformeMultiobjeto(string Proveedor, DateTime FechaUtc, int Muestras, int CanecasExactas, decimal ExactitudCanecas,
        int SinLista, IReadOnlyList<MetricaDestino> PorDestino, decimal ErrorMedioObjetos, long MedianaMs, long P95Ms, IReadOnlyList<ResultadoMultiobjeto> Detalle);

    /// <summary>
    /// Fotos con varios residuos: compara el conjunto de destinos y de canecas que Eco deduciría con lo etiquetado. Solo lee
    /// las fotos y llama al modelo local: no consulta Oracle, no crea solicitudes y no abre nada (el plan usa canecas ficticias).
    /// </summary>
    public static class EvaluacionMultiobjeto
    {
        private static readonly string[] Colores = { "Blanca", "Negra", "Verde" };

        private static readonly IReadOnlyList<CanecaEstacion> EstacionFicticia = new[]
        {
            new CanecaEstacion(1, "Blanca", CategoriaMaterial.Aprovechable, true, null, null),
            new CanecaEstacion(2, "Negra", CategoriaMaterial.NoAprovechable, true, null, null),
            new CanecaEstacion(3, "Verde", CategoriaMaterial.OrganicoAprovechable, true, null, null),
        };

        public static async Task<InformeMultiobjeto> EjecutarAsync(string manifiesto, ServicioClasificacionImagen servicio, string proveedor, CancellationToken ct = default)
        {
            var archivo = new FileInfo(Path.GetFullPath(manifiesto));
            if (!archivo.Exists || archivo.Length > 512 * 1024) throw new ArgumentException("El manifiesto debe existir y no superar 512 KiB.");
            ManifiestoMultiobjeto documento = JsonSerializer.Deserialize<ManifiestoMultiobjeto>(File.ReadAllText(archivo.FullName)) ?? throw new ArgumentException("Manifiesto vacío.");
            if (documento.Muestras is not { Length: > 0 and <= 400 } muestras) throw new ArgumentException("El manifiesto debe tener de 1 a 400 muestras.");
            string raiz = archivo.DirectoryName!;
            var detalle = new List<ResultadoMultiobjeto>();
            foreach (MuestraMultiobjeto muestra in muestras)
            {
                string ruta = Path.GetFullPath(Path.Combine(raiz, muestra.Archivo));
                if (!ruta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal)) throw new ArgumentException("La muestra sale del corpus.");
                byte[] bytes = File.ReadAllBytes(ruta);
                if (!string.Equals(Compat.ToHexString(Compat.Sha256(bytes)), muestra.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Una muestra no coincide con su hash.");
                var reloj = System.Diagnostics.Stopwatch.StartNew();
                IReadOnlyList<ObjetoEnFoto> objetos = await servicio.ListarObjetosAsync(new ImagenResiduo(bytes, null, EnvioExterno.Prohibido), ct);
                reloj.Stop();
                detalle.Add(Medir(muestra, objetos, reloj.ElapsedMilliseconds));
            }
            return Resumir(proveedor, detalle);
        }

        internal static ResultadoMultiobjeto Medir(MuestraMultiobjeto muestra, IReadOnlyList<ObjetoEnFoto> objetos, long ms)
        {
            string[] predichos = objetos.Where(o => o.Destino is DestinoResiduo.Blanca or DestinoResiduo.Negra or DestinoResiduo.Verde or DestinoResiduo.Posconsumo)
                .Select(o => o.Destino.ToString()).Distinct().OrderBy(d => d, StringComparer.Ordinal).ToArray();
            string[] canecas = CoordinadorAperturasMultiples.Planificar(objetos, EstacionFicticia).Aperturas
                .Select(a => a.Destino.ToString()).OrderBy(d => d, StringComparer.Ordinal).ToArray();
            string[] esperados = muestra.Destinos.OrderBy(d => d, StringComparer.Ordinal).ToArray();
            string[] canecasEsperadas = esperados.Where(Colores.Contains).ToArray();
            return new ResultadoMultiobjeto(muestra.Archivo, muestra.Conjunto, esperados, predichos, canecas, canecasEsperadas, muestra.Objetos,
                objetos.Count(o => o.Clave is not null), objetos.Select(o => o.Clave ?? "-").ToArray(), canecas.SequenceEqual(canecasEsperadas), ms);
        }

        internal static InformeMultiobjeto Resumir(string proveedor, IReadOnlyList<ResultadoMultiobjeto> detalle)
        {
            var porDestino = new List<MetricaDestino>();
            foreach (string destino in Colores.Append("Posconsumo"))
            {
                int vp = detalle.Count(d => d.Esperados.Contains(destino) && d.Predichos.Contains(destino));
                int fp = detalle.Count(d => !d.Esperados.Contains(destino) && d.Predichos.Contains(destino));
                int fn = detalle.Count(d => d.Esperados.Contains(destino) && !d.Predichos.Contains(destino));
                decimal p = vp + fp == 0 ? 0 : Math.Round((decimal)vp / (vp + fp), 3);
                decimal r = vp + fn == 0 ? 0 : Math.Round((decimal)vp / (vp + fn), 3);
                porDestino.Add(new MetricaDestino(destino, vp, fp, fn, p, r, p + r == 0 ? 0 : Math.Round(2 * p * r / (p + r), 3)));
            }
            long[] tiempos = detalle.Select(d => d.Milisegundos).OrderBy(t => t).ToArray();
            int exactas = detalle.Count(d => d.CanecasExactas);
            return new InformeMultiobjeto(proveedor, DateTime.UtcNow, detalle.Count, exactas, detalle.Count == 0 ? 0 : Math.Round((decimal)exactas / detalle.Count, 3),
                detalle.Count(d => d.ObjetosPredichos == 0), porDestino,
                detalle.Count == 0 ? 0 : Math.Round((decimal)detalle.Average(d => Math.Abs(d.ObjetosPredichos - d.ObjetosEsperados)), 2),
                tiempos.Length == 0 ? 0 : tiempos[tiempos.Length / 2], tiempos.Length == 0 ? 0 : tiempos[Math.Min(tiempos.Length - 1, (int)Math.Ceiling(tiempos.Length * 0.95) - 1)],
                detalle);
        }
    }
}
