using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Si una caneca concreta recibe el material consultado.</summary>
    public enum AdmisionMaterial
    {
        /// <summary>La consulta no se hizo frente a ninguna caneca.</summary>
        SinCaneca = 1,

        /// <summary>La caneca está en servicio y tiene el material en su lista.</summary>
        Admitido = 2,

        /// <summary>La caneca está en servicio pero no recibe ese material.</summary>
        NoAdmitido = 3,

        /// <summary>La caneca no existe o no está en servicio.</summary>
        CanecaNoDisponible = 4
    }

    /// <summary>
    /// Respuesta a «¿dónde va este material?». Separa dos preguntas distintas: <see cref="Guia"/> dice cómo se
    /// clasifica el material en general y <see cref="Admision"/> si una caneca concreta lo recibe. Ninguna de las dos
    /// dice si la persona puede pedir la apertura: eso lo decide <see cref="ServicioInteracciones"/>.
    /// </summary>
    /// <param name="Caneca">La caneca consultada, si existe; se devuelve para no leerla otra vez.</param>
    public sealed record ConsultaMaterial(Material Material, IndicacionSeparacion Guia, AdmisionMaterial Admision, Contenedor? Caneca = null);

    /// <summary>Caneca y materiales habilitados que recibe; si no está en servicio no se puede usar.</summary>
    public sealed record CanecaPublica(Contenedor Caneca, IReadOnlyList<Material> Admitidos);

    /// <summary>Caneca concreta de la estación y material de Oracle con que se registra un residuo del prototipo.</summary>
    public sealed record DestinoPrototipo(Contenedor Caneca, Material Material);

    /// <summary>
    /// Catálogo ambiental consultable por el ciudadano: busca materiales por texto y explica cómo se
    /// separan. Es determinista: compara texto normalizado contra el catálogo, sin IA, sin SQL armado a partir del
    /// mensaje y sin tocar la caneca. Solo muestra materiales habilitados y no exige cuenta (el catálogo no tiene
    /// datos personales).
    /// </summary>
    public sealed class ServicioCatalogoAmbiental
    {
        /// <summary>Máximo de materiales que devuelve una búsqueda.</summary>
        public const int MaximoResultados = 5;
        public const int TamanoPaginaPublica = 10;
        public const string NombreCanecaPiloto = "EcoAccess — Caneca piloto";

        /// <summary>Largo máximo del texto que se analiza; el resto se ignora.</summary>
        public const int LargoMaximoBusqueda = 100;

        /// <summary>Palabras de la búsqueda con menos letras que esto se ignoran (artículos, preposiciones).</summary>
        private const int LetrasMinimas = 3;

        private static readonly HashSet<string> PalabrasVacias =
            new(StringComparer.Ordinal) { "con", "del", "las", "los", "una", "uno", "unos", "unas", "para", "por", "que", "donde", "echo", "boto", "tiro", "van" };

        private readonly IRepositorioMateriales _materiales;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioEstaciones? _estaciones;

        /// <param name="estaciones">Configuración de la estación de tres canecas (V024). Sin ella solo existe la caneca piloto.</param>
        public ServicioCatalogoAmbiental(IRepositorioMateriales materiales, IRepositorioContenedores contenedores, IRepositorioEstaciones? estaciones = null)
        {
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _estaciones = estaciones;
        }

        /// <summary>
        /// Caneca de la estación del QR oficial que recibe ese residuo del prototipo y el material de Oracle con que se
        /// registra el depósito. Rechaza si la estación no tiene esa caneca, si está fuera de servicio en Oracle o si no
        /// admite el material: nunca se cambia a otra caneca para «hacer que funcione».
        /// </summary>
        public async Task<Resultado<DestinoPrototipo>> ResolverPrototipoAsync(ResiduoPrototipo residuo, CancellationToken cancellationToken = default)
        {
            if (residuo is null) throw new ArgumentNullException(nameof(residuo));
            Contenedor? piloto = (await _contenedores.ListarAsync(cancellationToken)).FirstOrDefault(c => c.Nombre == NombreCanecaPiloto);
            if (piloto is null)
                return Resultado<DestinoPrototipo>.Rechazo(MotivoRechazo.NoEncontrado, "La estación de EcoAccess no está configurada en la base conectada.");
            string nombreCaneca = CatalogoPrototipo.NombreCaneca(residuo.Caneca);
            Contenedor? destino = residuo.Caneca == CategoriaMaterial.Aprovechable ? piloto : null;
            if (_estaciones is not null)
            {
                IReadOnlyList<CanecaEstacion> estacion = await _estaciones.ListarEstacionDeAsync(piloto.Id, cancellationToken);
                CanecaEstacion? configurada = estacion.FirstOrDefault(c => c.Categoria == residuo.Caneca);
                if (configurada is not null) destino = await _contenedores.ObtenerPorIdAsync(configurada.IdContenedor, cancellationToken);
            }
            if (destino is null)
                return Resultado<DestinoPrototipo>.Rechazo(MotivoRechazo.NoEncontrado, $"La estación no tiene configurada la {nombreCaneca.ToLowerInvariant()}.");
            if (!destino.EstaEnServicio)
                return Resultado<DestinoPrototipo>.Rechazo(MotivoRechazo.EstadoNoPermitido, $"La {nombreCaneca.ToLowerInvariant()} está fuera de servicio en Oracle: no se puede abrir.");
            Material? material = (await ListarHabilitadosAsync(cancellationToken))
                .FirstOrDefault(m => m.Codigo == residuo.CodigoMaterial && destino.AdmiteMaterial(m.Id));
            if (material is null)
                return Resultado<DestinoPrototipo>.Rechazo(MotivoRechazo.EstadoNoPermitido, $"La {nombreCaneca.ToLowerInvariant()} no admite este residuo en la configuración de Oracle.");
            return Resultado<DestinoPrototipo>.Exito(new DestinoPrototipo(destino, material));
        }

        /// <summary>Catálogo público: materiales habilitados ordenados por categoría y nombre (comando /materiales).</summary>
        public async Task<IReadOnlyList<Material>> ListarHabilitadosAsync(CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Material> catalogo = await _materiales.ListarAsync(soloHabilitados: true, cancellationToken);
            return catalogo
                .Where(m => !FiltroMateriales.EsDatoDePrueba(m))
                .OrderBy(m => m.Categoria)
                .ThenBy(m => m.Nombre, StringComparer.CurrentCulture)
                .ToList();
        }

        /// <summary>
        /// Materiales habilitados que coinciden con el texto, de mayor a menor coincidencia. Coincide el código exacto
        /// (por ejemplo «pet» o «lata aluminio») o cada palabra de la búsqueda que empiece una palabra del nombre;
        /// se ignoran mayúsculas, tildes y plurales simples («botellas» encuentra «Botella»).
        /// </summary>
        public async Task<IReadOnlyList<Material>> BuscarAsync(string? texto, CancellationToken cancellationToken = default)
        {
            string normalizado = Normalizar(texto);
            string[] palabras = normalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => p.Length >= LetrasMinimas && !PalabrasVacias.Contains(p))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            string codigo = normalizado.Replace(' ', '_');
            if (palabras.Length == 0 && codigo.Length == 0)
            {
                return Array.Empty<Material>();
            }

            IReadOnlyList<Material> catalogo = await _materiales.ListarAsync(soloHabilitados: true, cancellationToken);
            return catalogo
                .Where(m => !FiltroMateriales.EsDatoDePrueba(m))
                .Select(m => (Material: m, Puntaje: Puntaje(m, codigo, palabras)))
                .Where(x => x.Puntaje > 0)
                .OrderByDescending(x => x.Puntaje)
                .ThenBy(x => x.Material.Nombre, StringComparer.Ordinal)
                .Take(MaximoResultados)
                .Select(x => x.Material)
                .ToList();
        }

        /// <summary>Una sola página pública. Oracle filtra y limita; la interfaz nunca recibe cientos de tarjetas.</summary>
        public async Task<Resultado<PaginaMateriales>> ConsultarPaginaPublicaAsync(string? texto, int pagina = 1,
            CategoriaMaterial? categoria = null, CancellationToken cancellationToken = default)
        {
            if (texto?.Length > LargoMaximoBusqueda || pagina < 1 || (categoria is not null && !Compat.IsDefined(categoria.Value)))
                return Resultado<PaginaMateriales>.Rechazo(MotivoRechazo.DatosInvalidos, "Revisa el texto, la categoría y la página de la búsqueda.");
            var filtro = new FiltroMateriales(Normalizar(texto), habilitado: true, categoria: categoria, pagina: pagina,
                tamanoPagina: TamanoPaginaPublica, soloPublicos: true);
            return Resultado<PaginaMateriales>.Exito(await _materiales.ConsultarPaginaAsync(filtro, cancellationToken));
        }

        /// <summary>
        /// Caneca identificada por su QR (dato público, va impreso en ella) con los materiales habilitados que recibe.
        /// La usa el asistente de depósito para mostrar la caneca DEMO tal como está en Oracle, sin inventar otras.
        /// </summary>
        public async Task<Resultado<CanecaPublica>> ConsultarCanecaAsync(string? identificadorQr, CancellationToken cancellationToken = default)
        {
            Contenedor? caneca = Compat.IsNullOrWhiteSpace(identificadorQr)
                ? null
                : await _contenedores.ObtenerPorIdentificadorQrAsync(identificadorQr.Trim(), cancellationToken);
            if (caneca is null)
            {
                return Resultado<CanecaPublica>.Rechazo(MotivoRechazo.NoEncontrado, "No hay una caneca con ese código.");
            }

            return Resultado<CanecaPublica>.Exito(await PublicarAsync(caneca, cancellationToken));
        }

        /// <summary>La caneca piloto en servicio, tal como está en Oracle; no depende de ningún dato guardado en el equipo.</summary>
        public async Task<Resultado<CanecaPublica>> ConsultarPilotoAsync(CancellationToken cancellationToken = default)
        {
            Contenedor? piloto = (await _contenedores.ListarAsync(cancellationToken))
                .FirstOrDefault(c => c.Nombre == NombreCanecaPiloto && c.EstaEnServicio);
            return piloto is null
                ? Resultado<CanecaPublica>.Rechazo(MotivoRechazo.NoEncontrado, "La caneca piloto no está en servicio.")
                : Resultado<CanecaPublica>.Exito(await PublicarAsync(piloto, cancellationToken));
        }

        private async Task<CanecaPublica> PublicarAsync(Contenedor caneca, CancellationToken cancellationToken) =>
            new CanecaPublica(caneca, (await ListarHabilitadosAsync(cancellationToken)).Where(m => caneca.AdmiteMaterial(m.Id)).ToList());

        /// <summary>
        /// Clasificación del material y, si se indica una caneca, si esa caneca lo recibe. Un material inexistente o
        /// deshabilitado se rechaza con <see cref="MotivoRechazo.NoEncontrado"/>.
        /// </summary>
        public async Task<Resultado<ConsultaMaterial>> ConsultarAsync(int idMaterial, int? idContenedor, CancellationToken cancellationToken = default)
        {
            Material? material = idMaterial > 0 ? await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken) : null;
            if (material is null || !material.Habilitado || FiltroMateriales.EsDatoDePrueba(material))
            {
                return Resultado<ConsultaMaterial>.Rechazo(MotivoRechazo.NoEncontrado, "El material no está en el catálogo.");
            }

            AdmisionMaterial admision = AdmisionMaterial.SinCaneca;
            Contenedor? caneca = null;
            if (idContenedor is { } id)
            {
                caneca = id > 0 ? await _contenedores.ObtenerPorIdAsync(id, cancellationToken) : null;
                admision = caneca is null || !caneca.EstaEnServicio
                    ? AdmisionMaterial.CanecaNoDisponible
                    : caneca.AdmiteMaterial(material.Id) ? AdmisionMaterial.Admitido : AdmisionMaterial.NoAdmitido;
            }

            return Resultado<ConsultaMaterial>.Exito(new ConsultaMaterial(material, GuiaSeparacion.Para(material.Categoria), admision, caneca));
        }

        /// <summary>
        /// Minúsculas, sin tildes (la ñ queda como n) y con todo lo que no sea letra o dígito ASCII convertido en un solo espacio.
        /// </summary>
        private static string Normalizar(string? texto, int maximo = LargoMaximoBusqueda)
        {
            if (Compat.IsNullOrWhiteSpace(texto))
            {
                return "";
            }

            string recortado = texto.Length > maximo ? texto[..maximo] : texto;
            var salida = new StringBuilder(recortado.Length);
            bool espacio = false;
            foreach (char c in recortado.Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (Compat.IsAsciiLetterOrDigit(c))
                {
                    if (espacio && salida.Length > 0)
                    {
                        salida.Append(' ');
                    }

                    salida.Append(char.ToLowerInvariant(c));
                    espacio = false;
                }
                else
                {
                    espacio = true;
                }
            }

            return salida.ToString();
        }

        private static int Puntaje(Material material, string codigo, string[] palabras)
        {
            if (string.Equals(material.Codigo, codigo, StringComparison.OrdinalIgnoreCase))
            {
                return int.MaxValue;
            }

            string[] delNombre = Normalizar(material.Nombre + " " + material.Sinonimos, 401).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return palabras.Count(p => delNombre.Any(n => Coincide(p, n)));
        }

        /// <summary>La palabra buscada empieza la del nombre («bot» → «botella») o es su plural en -s o -es («pilas» → «pila»).</summary>
        private static bool Coincide(string buscada, string delNombre) =>
            delNombre.StartsWith(buscada, StringComparison.Ordinal)
            || (delNombre.Length >= 4 && buscada.StartsWith(delNombre, StringComparison.Ordinal)
                && buscada[delNombre.Length..] is "s" or "es");
    }
}
