using System;
using System.Linq;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Entrada del catálogo validado de materiales. El clasificador de imágenes solo puede
    /// sugerir un <see cref="Codigo"/> que exista aquí; así la IA orienta, pero el vocabulario lo controla EcoAccess.
    /// El catálogo también contiene materiales que ninguna caneca admite, para poder explicar por qué no.
    /// </summary>
    public class Material
    {
        public const int LongitudMinimaCodigo = 2;
        public const int LongitudMaximaCodigo = 30;
        public const int LongitudMinimaNombre = 2;
        public const int LongitudMaximaNombre = 100;
        public const int LongitudMaximaDescripcion = 500;

        /// <summary>0 mientras el material no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }

        /// <summary>Código estable en mayúsculas, por ejemplo PET o CARTON. No cambia aunque cambie el nombre visible.</summary>
        public string Codigo { get; }
        public string Nombre { get; }
        public CategoriaMaterial Categoria { get; }

        /// <summary>Orientación para el ciudadano: cómo entregarlo (limpio, seco, aplastado...). Opcional.</summary>
        public string? Descripcion { get; }
        public string? Sinonimos { get; }
        public string? Precauciones { get; }
        public string? RutaSeparacion { get; }
        public bool Habilitado { get; private set; }

        public bool EsNuevo => Id == 0;

        /// <summary>Registra un material nuevo en el catálogo, habilitado.</summary>
        public Material(string codigo, string nombre, CategoriaMaterial categoria, string? descripcion,
            string? sinonimos = null, string? precauciones = null, string? rutaSeparacion = null)
        {
            Codigo = NormalizarCodigo(codigo, nameof(codigo));
            Nombre = Validacion.TextoRequerido(nombre, nameof(nombre), LongitudMaximaNombre, LongitudMinimaNombre);
            Categoria = Validacion.EnumDefinido(categoria, nameof(categoria));
            Descripcion = Validacion.TextoOpcional(descripcion, nameof(descripcion), LongitudMaximaDescripcion);
            Sinonimos = Validacion.TextoOpcional(sinonimos, nameof(sinonimos), 300);
            Precauciones = Validacion.TextoOpcional(precauciones, nameof(precauciones), 500);
            RutaSeparacion = Validacion.TextoOpcional(rutaSeparacion, nameof(rutaSeparacion), 150);
            Habilitado = true;
        }

        /// <summary>
        /// Reconstruye un material guardado. Primero ejecuta el constructor de registro (": this(...)"),
        /// así las validaciones de código, nombre y categoría están escritas una sola vez.
        /// </summary>
        public Material(int id, string codigo, string nombre, CategoriaMaterial categoria, string? descripcion, bool habilitado,
            string? sinonimos = null, string? precauciones = null, string? rutaSeparacion = null)
            : this(codigo, nombre, categoria, descripcion, sinonimos, precauciones, rutaSeparacion)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            Habilitado = habilitado;
        }

        // Deshabilitar retira el material de nuevas clasificaciones sin borrar los depósitos que ya lo usaron.
        public void Habilitar() => Habilitado = true;

        public void Deshabilitar() => Habilitado = false;

        private static string NormalizarCodigo(string? codigo, string nombreParametro)
        {
            string limpio = Validacion.TextoRequerido(codigo, nombreParametro, LongitudMaximaCodigo, LongitudMinimaCodigo);

            // Se valida antes de pasar a mayúsculas: ToUpperInvariant convierte letras como «ı» en «I»
            // y las dejaría pasar sin aviso.
            bool valido = EsLetraAscii(limpio[0]) && limpio.All(c => EsLetraAscii(c) || c is (>= '0' and <= '9') or '_');
            if (!valido)
            {
                throw new ArgumentException(
                    "El código solo admite letras sin tilde, números y guion bajo, y debe empezar por una letra.", nombreParametro);
            }

            return limpio.ToUpperInvariant();
        }

        private static bool EsLetraAscii(char c) => c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');
    }
}
