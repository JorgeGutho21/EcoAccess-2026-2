using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Textos de pantalla de los estados de caneca; el número guardado en Oracle no se muestra.</summary>
    internal static class TextosContenedor
    {
        public static string Estado(EstadoContenedor estado) => estado switch
        {
            EstadoContenedor.EnServicio => "En servicio",
            EstadoContenedor.FueraDeServicio => "Fuera de servicio",
            EstadoContenedor.DadoDeBaja => "Dada de baja",
            _ => estado.ToString(),
        };
    }
}
