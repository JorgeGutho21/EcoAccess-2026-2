using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text.Json;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Bot.Transportes;

namespace EcoAccess.P3.Bot.Vision
{

    /// <param name="Aceptadas">Otras etiquetas igual de correctas: una foto no distingue el polímero de un plástico cualquiera.</param>
    public sealed record MuestraArchivoVision(string Archivo, string Sha256, string Esperada, string[]? Aceptadas = null);
    public sealed record ManifiestoVision(string Nombre, string Licencia, MuestraArchivoVision[] Muestras);
    public sealed record DetalleEvaluacionVision(string Sha256, string Esperada, string Obtenida,
        long DuracionMilisegundos = 0, PropuestaVision? Propuesta = null, string? Incidencia = null);
    public sealed record InformeVisionArchivos(string Conjunto, string LicenciaDeclarada, string Proveedor,
        DateTime FechaUtc, string CatalogoSha256, decimal Umbral, int MaximoSolicitudes,
        IReadOnlyList<DetalleEvaluacionVision> Muestras, int FallosProveedor, InformeEvaluacion Metricas);

    /// <summary>Evalúa un corpus local explícito y acotado; no descarga datasets, entrena ni modifica el catálogo.</summary>
    public static class EvaluacionVisionArchivos
    {
        public const int MaximoSolicitudes = 200;

        public static async Task<InformeVisionArchivos> EjecutarAsync(string manifiesto, int maximoSolicitudes,
            IClasificadorMaterial clasificador, IReadOnlyList<Material> catalogo, IRepositorioContenedores contenedores,
            TimeProvider reloj, CancellationToken ct = default, TimeSpan? tiempoMaximo = null,
            EnvioExterno envio = EnvioExterno.Prohibido, TimeSpan? pausaEntreMuestras = null)
        {
            if (maximoSolicitudes is < 1 or > MaximoSolicitudes) throw new ArgumentException($"El presupuesto explícito debe ser de 1 a {MaximoSolicitudes} solicitudes.");
            var manifestFile = new FileInfo(Path.GetFullPath(manifiesto));
            if (!manifestFile.Exists || manifestFile.Length > 64 * 1024) throw new ArgumentException("El manifiesto debe existir y no superar 64 KiB.");
            using var manifiestoStream = File.OpenRead(manifestFile.FullName);
            var documento = JsonSerializer.Deserialize<ManifiestoVision>(await LecturaAcotada.DesdeStreamAsync(manifiestoStream, 64 * 1024, ct));
            if (documento is null || Compat.IsNullOrWhiteSpace(documento.Nombre) || Compat.IsNullOrWhiteSpace(documento.Licencia)
                || documento.Muestras is not { Length: > 0 } muestras || muestras.Length > maximoSolicitudes)
                throw new ArgumentException("Declara conjunto, licencia y muestras dentro del presupuesto explícito.");
            string raiz = manifestFile.DirectoryName!;
            var snapshot = new CatalogoInmutable(catalogo);
            var etiquetas = snapshot.Materiales.Select(m => m.Codigo).Concat(Compat.GetNames<ResultadoClasificacion>()
                .Where(n => n is not nameof(ResultadoClasificacion.Identificado) and not nameof(ResultadoClasificacion.ProveedorNoDisponible)
                    and not nameof(ResultadoClasificacion.CuotaGratuitaAgotada) and not nameof(ResultadoClasificacion.AutorizacionExternaRequerida))).ToHashSet(StringComparer.Ordinal);
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Validar todo el corpus antes de la primera solicitud, incluidas imágenes realmente decodificables.
            foreach (var muestra in muestras)
            {
                if (muestra is null || !etiquetas.Contains(muestra.Esperada) || muestra.Aceptadas?.All(etiquetas.Contains) == false || muestra.Sha256 is null || muestra.Sha256.Length != 64
                    || muestra.Sha256.Any(c => !Uri.IsHexDigit(c)) || !hashes.Add(muestra.Sha256))
                    throw new ArgumentException("Cada muestra necesita etiqueta válida y hash SHA-256 único.");
                string ruta = ResolverDentroDe(raiz, muestra.Archivo);
                byte[] contenido = await LeerMuestraAsync(ruta, muestra.Sha256, ct);
                PreparadorImagen.Preparar(new ImagenResiduo(contenido, null));
            }
            var detalles = new List<DetalleEvaluacionVision>();
            for (int i = 0; i < muestras.Length; i++)
            {
                // Revalidar hash y ruta al usar el archivo; no enviar contenido cambiado después de la inspección.
                string ruta = ResolverDentroDe(raiz, muestras[i].Archivo);
                byte[] bytes = await LeerMuestraAsync(ruta, muestras[i].Sha256, ct);
                var observado = new ClasificadorObservado(clasificador);
                var servicio = new ServicioClasificacionImagen(observado, snapshot, new ServicioCatalogoAmbiental(snapshot, contenedores), tiempoMaximo);
                var cronometro = System.Diagnostics.Stopwatch.StartNew();
                var resultado = await servicio.ClasificarAsync(new ImagenResiduo(bytes, null, envio), cancellationToken: ct);
                cronometro.Stop();
                // Respeta el límite por minuto del proveedor; este tiempo no se cuenta como inferencia.
                if (pausaEntreMuestras is { } pausa && i < muestras.Length - 1) await Task.Delay(pausa, ct);
                if (!resultado.Exitoso) throw new InvalidOperationException("Una muestra no pudo evaluarse; no se genera un informe completo.");
                if (resultado.Valor.Resultado == ResultadoClasificacion.CuotaGratuitaAgotada)
                    throw new CuotaVisionAgotadaException(); // Evaluación incompleta, sin métricas de calidad ni salida exitosa.
                string obtenida = EvaluadorClasificacion.Etiqueta(resultado.Valor);
                detalles.Add(new DetalleEvaluacionVision(muestras[i].Sha256.ToUpperInvariant(), muestras[i].Aceptadas?.Contains(obtenida) == true ? obtenida : muestras[i].Esperada,
                    obtenida, cronometro.ElapsedMilliseconds,
                    resultado.Valor.Resultado == ResultadoClasificacion.ProveedorNoDisponible ? null : observado.Propuesta,
                    resultado.Valor.Resultado == ResultadoClasificacion.ProveedorNoDisponible ? "fallo_o_tiempo_agotado" : observado.Incidencia));
            }
            string huella = Compat.ToHexString(Compat.Sha256(JsonSerializer.SerializeToUtf8Bytes(snapshot.Materiales
                .OrderBy(m => m.Codigo).Select(m => new { m.Codigo, m.Nombre, m.Categoria, m.Descripcion }))));
            return new InformeVisionArchivos(documento.Nombre, documento.Licencia, clasificador.Proveedor, reloj.GetUtcNow().UtcDateTime, huella,
                ServicioClasificacionImagen.UmbralConfianza, maximoSolicitudes, detalles,
                detalles.Count(m => m.Obtenida == nameof(ResultadoClasificacion.ProveedorNoDisponible)),
                EvaluadorClasificacion.Evaluar(detalles.Select(m => new MuestraEvaluacion(m.Esperada, m.Obtenida)).ToArray()));
        }

        // Observa el contrato existente para el informe; no autoriza ni registra operaciones de negocio.
        private sealed class ClasificadorObservado : IClasificadorMaterial
        {
            private readonly IClasificadorMaterial _proveedor;

            public ClasificadorObservado(IClasificadorMaterial proveedor)
            {
                _proveedor = proveedor;
            }

            public string Proveedor => _proveedor.Proveedor;
            public PropuestaVision? Propuesta { get; private set; }
            public string? Incidencia { get; private set; }
            public async Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken ct = default)
            {
                var propuesta = await _proveedor.ClasificarAsync(imagen, catalogo, ct);
                ct.ThrowIfCancellationRequested();
                string? codigo = propuesta.CodigoMaterial?.Trim().ToUpperInvariant();
                bool conocido = codigo is not null && catalogo.Any(m => m.Codigo == codigo);
                if (codigo is not null && !conocido) Incidencia = "codigo_fuera_catalogo";
                // El informe conserva solo códigos públicos conocidos, nunca texto libre del proveedor.
                Propuesta = propuesta with { CodigoMaterial = conocido ? codigo : null, Explicacion = null, Proveedor = null };
                return propuesta;
            }
        }

        private static string ResolverDentroDe(string raiz, string archivo)
        {
            if (Compat.IsNullOrWhiteSpace(archivo) || Path.IsPathRooted(archivo)
                || archivo.Split('/', '\\').Any(s => s.Length == 0 || s is "." or ".."))
                throw new ArgumentException("Las muestras deben ser rutas relativas dentro del corpus.");
            string ruta = Path.GetFullPath(Path.Combine(raiz, archivo));
            if (!ruta.StartsWith(raiz + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new ArgumentException("La muestra sale del directorio del corpus.");
            FileSystemInfo info = new FileInfo(ruta);
            while (info.FullName != raiz)
            {
                if (info.LinkTarget() is not null) throw new ArgumentException("No se admiten enlaces simbólicos en las muestras.");
                info = info is FileInfo f ? f.Directory! : ((DirectoryInfo)info).Parent!;
            }
            if (new DirectoryInfo(raiz).LinkTarget() is not null) throw new ArgumentException("El corpus debe tener un directorio físico.");
            return ruta;
        }
        private static async Task<byte[]> LeerMuestraAsync(string ruta, string hash, CancellationToken ct)
        {
            var f = new FileInfo(ruta);
            if (!f.Exists || f.Length < 12 || f.Length > ServicioClasificacionImagen.MaximoBytes)
                throw new ArgumentException("Una muestra no existe o supera los límites permitidos.");
            using var stream = File.OpenRead(ruta);
            byte[] bytes = await LecturaAcotada.DesdeStreamAsync(stream, ServicioClasificacionImagen.MaximoBytes, ct);
            if (!string.Equals(Compat.ToHexString(Compat.Sha256(bytes)), hash, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Una muestra no coincide con el hash declarado.");
            return bytes;
        }

        private sealed class CatalogoInmutable : IRepositorioMateriales
        {
            public CatalogoInmutable(IReadOnlyList<Material> materiales)
            {
                Materiales = materiales.Where(m => m.Habilitado)
                .Select(m => new Material(m.Id, m.Codigo, m.Nombre, m.Categoria, m.Descripcion, true)).ToArray();
            }

            internal IReadOnlyList<Material> Materiales { get; }
            public Task<Material?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult<Material?>(Materiales.FirstOrDefault(m => m.Id == id));
            public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default) => Task.FromResult(Materiales.Any(m => m.Codigo == codigo));
            public Task<IReadOnlyList<Material>> ListarAsync(bool soloHabilitados, CancellationToken cancellationToken = default) => Task.FromResult(Materiales);
            public Task<PaginaMateriales> ConsultarPaginaAsync(FiltroMateriales filtro, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException("El catálogo de evaluación no expone consultas administrativas.");
            public Task<int> AgregarAsync(Material m, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task HabilitarAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
            public Task DeshabilitarAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        }
    }
}
