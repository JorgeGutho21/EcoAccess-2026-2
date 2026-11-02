using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Texto visible de cada categoría, con el color de caneca de la Resolución 2184 de 2019.</summary>
    internal static class TextosCategoria
    {
        public static IReadOnlyList<OpcionCategoria> Opciones { get; } =
            Compat.GetValues<CategoriaMaterial>().Select(c => new OpcionCategoria(c, Texto(c))).ToList();

        public static string Texto(CategoriaMaterial categoria) => categoria switch
        {
            CategoriaMaterial.Aprovechable => "Aprovechable (blanca)",
            CategoriaMaterial.OrganicoAprovechable => "Orgánico aprovechable (verde)",
            CategoriaMaterial.NoAprovechable => "No aprovechable (negra)",
            CategoriaMaterial.Peligroso => "Peligroso (ruta especial)",
            _ => categoria.ToString()
        };
    }

    internal sealed record OpcionCategoria(CategoriaMaterial Valor, string Texto);
}
