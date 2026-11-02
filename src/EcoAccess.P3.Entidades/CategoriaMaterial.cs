namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Grupo del material según el código de colores para la separación en la fuente
    /// (Resolución 2184 de 2019, Colombia): blanco, verde y negro. Peligroso se agrega aparte porque
    /// esos residuos no van en ninguna de las tres canecas y requieren una ruta especial.
    /// Los valores son explícitos porque se guardarán como número en Oracle.
    /// </summary>
    public enum CategoriaMaterial
    {
        /// <summary>Caneca blanca: plástico, papel, cartón, vidrio y metales limpios y secos.</summary>
        Aprovechable = 1,

        /// <summary>Caneca verde: restos de comida y residuos de jardín.</summary>
        OrganicoAprovechable = 2,

        /// <summary>Caneca negra: papel higiénico, servilletas, empaques contaminados.</summary>
        NoAprovechable = 3,

        /// <summary>Pilas, medicamentos, químicos, aparatos eléctricos: punto de recolección especial.</summary>
        Peligroso = 4
    }
}
