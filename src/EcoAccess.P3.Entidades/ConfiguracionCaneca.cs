using System;
using System.Linq;

namespace EcoAccess.P3.Entidades
{
    /// <summary>Color de la caneca según el código nacional (Resolución 2184 de 2019).</summary>
    public enum ColorCaneca
    {
        /// <summary>Residuos aprovechables: plástico, papel, cartón, vidrio y metales limpios y secos.</summary>
        Blanca = 1,

        /// <summary>Residuos orgánicos aprovechables: restos de comida y de jardín.</summary>
        Verde = 2,

        /// <summary>Residuos no aprovechables: papel higiénico, servilletas, empaques contaminados.</summary>
        Negra = 3,
    }

    /// <summary>
    /// Configuración de una caneca dentro de una estación de tres canecas: su categoría (de la que sale el color), el
    /// dispositivo que mueve su tapa y su ubicación. Relación 1:1 con <see cref="Contenedor"/>.
    /// </summary>
    /// <remarks>
    /// El color no se guarda aparte: se deduce de la categoría, para que no puedan contradecirse (DRY). Los residuos
    /// peligrosos no tienen caneca: la clase rechaza esa categoría.
    /// </remarks>
    public sealed class ConfiguracionCaneca
    {
        public const int LongitudMinimaDispositivo = 3;
        public const int LongitudMaximaDispositivo = 40;
        public const int LongitudMaximaUbicacion = 60;

        public ConfiguracionCaneca(int idContenedor, CategoriaMaterial categoria, string? identificadorDispositivo,
            string? ubicacionEnEstacion, DateTime fechaActualizacionUtc)
        {
            if (idContenedor <= 0) throw new ArgumentOutOfRangeException(nameof(idContenedor), "La caneca no es válida.");
            if (categoria is not (CategoriaMaterial.Aprovechable or CategoriaMaterial.OrganicoAprovechable or CategoriaMaterial.NoAprovechable))
                throw new ArgumentException("Una caneca solo puede ser blanca, verde o negra; los residuos peligrosos van a un punto posconsumo.", nameof(categoria));
            if (fechaActualizacionUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("La fecha debe estar en UTC.", nameof(fechaActualizacionUtc));

            IdContenedor = idContenedor;
            Categoria = categoria;
            IdentificadorDispositivo = ValidarDispositivo(identificadorDispositivo);
            UbicacionEnEstacion = ValidarUbicacion(ubicacionEnEstacion);
            FechaActualizacionUtc = fechaActualizacionUtc;
        }

        public int IdContenedor { get; }

        /// <summary>Categoría de residuos que recibe: Aprovechable, Orgánico aprovechable o No aprovechable.</summary>
        public CategoriaMaterial Categoria { get; }

        /// <summary>Identificador del ESP32 de esta caneca (igual al DISPOSITIVO del firmware), o null si aún no tiene.</summary>
        public string? IdentificadorDispositivo { get; }

        public string? UbicacionEnEstacion { get; }

        public DateTime FechaActualizacionUtc { get; }

        /// <summary>Color que corresponde a la categoría.</summary>
        public ColorCaneca Color => ColorPara(Categoria)!.Value;

        /// <summary>True si la caneca tiene un dispositivo que puede abrir su tapa físicamente.</summary>
        public bool TieneDispositivo => IdentificadorDispositivo is not null;

        /// <summary>Color de caneca de una categoría, o null para los peligrosos (no tienen caneca).</summary>
        public static ColorCaneca? ColorPara(CategoriaMaterial categoria) => categoria switch
        {
            CategoriaMaterial.Aprovechable => ColorCaneca.Blanca,
            CategoriaMaterial.OrganicoAprovechable => ColorCaneca.Verde,
            CategoriaMaterial.NoAprovechable => ColorCaneca.Negra,
            _ => (ColorCaneca?)null,
        };

        private static string? ValidarDispositivo(string? dispositivo)
        {
            if (dispositivo is null) return null;
            string limpio = dispositivo.Trim();
            if (limpio.Length is < LongitudMinimaDispositivo or > LongitudMaximaDispositivo
                || !limpio.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_' || c == '-'))
                throw new ArgumentException("El identificador del dispositivo debe tener de 3 a 40 letras, números, «_» o «-».", nameof(dispositivo));
            return limpio;
        }

        private static string? ValidarUbicacion(string? ubicacion)
        {
            if (ubicacion is null || ubicacion.Trim().Length == 0) return null;
            string limpia = ubicacion.Trim();
            if (limpia.Length > LongitudMaximaUbicacion)
                throw new ArgumentException("La ubicación admite como máximo 60 caracteres.", nameof(ubicacion));
            return limpia;
        }
    }
}
