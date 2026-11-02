namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Lugar físico donde se instala una caneca (por ejemplo, un bloque del campus). Es una entidad aparte
    /// del contenedor pensando en que, si en el futuro una caneca se reubica, sus depósitos antiguos sigan
    /// asociados al lugar donde ocurrieron; la operación de reubicar todavía no existe (YAGNI). Más adelante
    /// se le asociará el sector geográfico (comuna, barrio), que hoy no se conoce con certeza para el piloto
    /// universitario (RN-13).
    /// </summary>
    public class PuntoInstalacion
    {
        public const int LongitudMinimaNombre = 2;
        public const int LongitudMaximaNombre = 100;
        public const int LongitudMaximaReferencia = 200;

        /// <summary>0 mientras el punto no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public string Nombre { get; }

        /// <summary>Indicación para encontrarlo, por ejemplo «Bloque A, primer piso, junto a la cafetería». Opcional.</summary>
        public string? ReferenciaUbicacion { get; }
        public bool Activo { get; private set; }

        public bool EsNuevo => Id == 0;

        /// <summary>Registra un punto nuevo, activo.</summary>
        public PuntoInstalacion(string nombre, string? referenciaUbicacion)
        {
            Nombre = Validacion.TextoRequerido(nombre, nameof(nombre), LongitudMaximaNombre, LongitudMinimaNombre);
            ReferenciaUbicacion = Validacion.TextoOpcional(referenciaUbicacion, nameof(referenciaUbicacion), LongitudMaximaReferencia);
            Activo = true;
        }

        /// <summary>Reconstruye un punto guardado con las mismas validaciones del registro.</summary>
        public PuntoInstalacion(int id, string nombre, string? referenciaUbicacion, bool activo)
            : this(nombre, referenciaUbicacion)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            Activo = activo;
        }

        public void Activar() => Activo = true;

        public void Desactivar() => Activo = false;
    }
}
