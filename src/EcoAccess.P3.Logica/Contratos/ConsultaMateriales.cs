using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Filtro administrativo acotado, independiente del catálogo completo del bot y del negocio.</summary>
    public sealed record FiltroMateriales
    {
        public string Texto { get; }
        public bool? Habilitado { get; }
        public CategoriaMaterial? Categoria { get; }
        public int Pagina { get; }
        public int TamanoPagina { get; }
        public bool SoloPublicos { get; }
        /// <summary>Convención reservada de fixtures, no categorías ambientales. No borra ni deshabilita filas.</summary>
        public static IReadOnlyList<string> PrefijosCodigoPrueba { get; } = Array.AsReadOnly(new[] { "BR_", "IT_", "TEST_", "PRUEBA_" });
        public static IReadOnlyList<string> PrefijosNombrePrueba { get; } = Array.AsReadOnly(new[] { "material ficticio", "material de prueba", "material temporal" });
        public static bool EsDatoDePrueba(Material material) =>
            PrefijosCodigoPrueba.Any(p => material.Codigo.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            || PrefijosNombrePrueba.Any(p => material.Nombre.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        public long Desplazamiento => ((long)Pagina - 1) * TamanoPagina;

        public FiltroMateriales(string? texto = null, bool? habilitado = null, CategoriaMaterial? categoria = null,
            int pagina = 1, int tamanoPagina = 25, bool soloPublicos = false)
        {
            Texto = (texto ?? "").Trim().ToLowerInvariant();
            if (Texto.Length > Material.LongitudMaximaNombre)
                throw new ArgumentException("La búsqueda no puede superar 100 caracteres.", nameof(texto));
            if (categoria is not null && !Compat.IsDefined(categoria.Value))
                throw new ArgumentException("Seleccione una categoría válida.", nameof(categoria));
            if (pagina < 1) throw new ArgumentOutOfRangeException(nameof(pagina), "La página debe ser positiva.");
            if (tamanoPagina is < 1 or > 100)
                throw new ArgumentOutOfRangeException(nameof(tamanoPagina), "La página debe contener entre 1 y 100 materiales.");
            Habilitado = habilitado;
            Categoria = categoria;
            Pagina = pagina;
            TamanoPagina = tamanoPagina;
            SoloPublicos = soloPublicos;
        }
    }

    public sealed record PaginaMateriales(IReadOnlyList<Material> Filas, int Pagina, int TamanoPagina, bool HaySiguiente);
}
