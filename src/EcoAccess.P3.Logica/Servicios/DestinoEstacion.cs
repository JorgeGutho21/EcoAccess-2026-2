using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Adónde va un residuo según la Resolución 2184 de 2019. Posconsumo y NoEsResiduo no tienen caneca.</summary>
    public enum DestinoResiduo
    {
        Indeterminado = 0,
        Blanca = 1,
        Verde = 2,
        Negra = 3,
        Posconsumo = 4,
        NoEsResiduo = 5,
    }

    /// <summary>Una caneca de una estación con su color (categoría), su ESP32 y su ubicación (tabla CONFIGURACION_CANECA, V021).</summary>
    public sealed record CanecaEstacion(int IdContenedor, string Nombre, CategoriaMaterial Categoria, bool EnServicio,
        string? IdentificadorDispositivo, string? Ubicacion);

    /// <summary>Lo que la persona debe saber: el destino y, si la estación la tiene, la caneca concreta.</summary>
    public sealed record DecisionDestino(DestinoResiduo Destino, CanecaEstacion? Caneca)
    {
        public bool TieneCaneca => Caneca is not null;
    }

    /// <summary>
    /// Fase 2: un solo QR oficial por estación y tres canecas (blanca, verde, negra). El QR identifica una caneca; de ella
    /// se toma la estación y aquí se elige la caneca del color que corresponde. Es determinista y no abre nada: la apertura
    /// sigue pasando por <see cref="ServicioInteracciones"/> y por la política de apertura.
    /// </summary>
    public static class ResolutorDestino
    {
        public static DestinoResiduo PorCategoria(CategoriaMaterial categoria) => categoria switch
        {
            CategoriaMaterial.Aprovechable => DestinoResiduo.Blanca,
            CategoriaMaterial.OrganicoAprovechable => DestinoResiduo.Verde,
            CategoriaMaterial.NoAprovechable => DestinoResiduo.Negra,
            CategoriaMaterial.Peligroso => DestinoResiduo.Posconsumo,
            _ => DestinoResiduo.Indeterminado,
        };

        /// <summary>Destino de un objeto reconocido en la foto. Lo sanitario va en la negra; lo electrónico y especial, a posconsumo.</summary>
        public static DestinoResiduo PorContexto(ContextoObjeto contexto) => contexto switch
        {
            ContextoObjeto.ResiduoAprovechable => DestinoResiduo.Blanca,
            ContextoObjeto.ResiduoOrganico => DestinoResiduo.Verde,
            ContextoObjeto.ResiduoNoAprovechable or ContextoObjeto.ResiduoSanitario => DestinoResiduo.Negra,
            ContextoObjeto.ResiduoElectronico or ContextoObjeto.ResiduoEspecial => DestinoResiduo.Posconsumo,
            ContextoObjeto.ObjetoCotidiano or ContextoObjeto.AnimalVivo or ContextoObjeto.Persona or ContextoObjeto.PlantaViva => DestinoResiduo.NoEsResiduo,
            _ => DestinoResiduo.Indeterminado,
        };

        /// <summary>
        /// La caneca de la estación para ese destino. Si hay varias del mismo color, la primera en servicio y, si
        /// ninguna lo está, la de menor id (para orientar sin abrir). Posconsumo, NoEsResiduo e Indeterminado nunca
        /// tienen caneca.
        /// </summary>
        public static DecisionDestino Resolver(DestinoResiduo destino, IReadOnlyList<CanecaEstacion> estacion)
        {
            Compat.ThrowIfNull(estacion, nameof(estacion));
            if (destino is not (DestinoResiduo.Blanca or DestinoResiduo.Verde or DestinoResiduo.Negra)) return new DecisionDestino(destino, null);
            var categoria = (CategoriaMaterial)(int)destino;
            CanecaEstacion? caneca = estacion.Where(c => c.Categoria == categoria)
                .OrderByDescending(c => c.EnServicio).ThenBy(c => c.IdContenedor).FirstOrDefault();
            return new DecisionDestino(destino, caneca);
        }

        public static string Color(DestinoResiduo destino) => destino switch
        {
            DestinoResiduo.Blanca => "BLANCA",
            DestinoResiduo.Verde => "VERDE",
            DestinoResiduo.Negra => "NEGRA",
            _ => "SIN CANECA",
        };

        /// <summary>Una línea para la persona: color, para qué es y dónde está. Sin identificadores internos.</summary>
        public static string Texto(DecisionDestino decision)
        {
            Compat.ThrowIfNull(decision, nameof(decision));
            string donde = decision.Caneca?.Ubicacion is { } u ? $", a la {u} de la estación" : "";
            string fuera = decision.Caneca is { EnServicio: false } ? " (fuera de servicio por ahora)" : "";
            return decision.Destino switch
            {
                DestinoResiduo.Blanca => $"⬜ Destino: caneca BLANCA, aprovechables limpios y secos{donde}{fuera}.",
                DestinoResiduo.Verde => $"🟩 Destino: caneca VERDE, orgánicos aprovechables{donde}{fuera}.",
                DestinoResiduo.Negra => $"⬛ Destino: caneca NEGRA, no aprovechables{donde}{fuera}.",
                DestinoResiduo.Posconsumo => "⚠️ No va en ninguna caneca de colores: llévalo a un punto posconsumo o gestor autorizado.",
                DestinoResiduo.NoEsResiduo => "🌱 No parece un residuo: no hace falta depositarlo.",
                _ => "🤔 No pude determinar el destino. Elige el material de la lista.",
            };
        }
    }
}
