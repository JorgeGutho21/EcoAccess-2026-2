using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Vision
{

    /// <summary>Compatibilidad de taxonomías para objetos inequívocos. No convierte plástico genérico en PET,
    /// metal en aluminio, ni respuestas borrosas, múltiples o inciertas en material identificado.</summary>
    internal static class MapeoObjetoCatalogoLocal
    {
        internal static PropuestaVision Normalizar(PropuestaVision propuesta, IReadOnlyList<Material> catalogo)
        {
            if (propuesta.Tipo != TipoPropuestaVision.ObjetoDesconocido || propuesta.CodigoMaterial is not null)
                return propuesta;
            string? codigo = propuesta.Objeto switch
            {
                ObjetoVisible.Papel => "PAPEL",
                ObjetoVisible.Carton => "CARTON",
                ObjetoVisible.EnvaseVidrio => "VIDRIO",
                _ => null
            };
            if (codigo is null || catalogo.Count(m => m.Habilitado && m.Codigo == codigo
                    && m.Categoria == CategoriaMaterial.Aprovechable) != 1)
                return propuesta;
            // Mantiene confianza autodeclarada: la BLL conserva su umbral y exige confirmación del ciudadano.
            return propuesta with { Tipo = TipoPropuestaVision.MaterialIdentificado, CodigoMaterial = codigo };
        }
    }
}
