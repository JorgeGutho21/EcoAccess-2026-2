using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Decide si una foto puede abrir la caneca sin el toque de confirmación. No usa la confianza declarada, que casi
    /// siempre sale alta: exige que objeto y material coincidan en los pares con evidencia medida (ver
    /// docs/telegram/FLUJO_ECO_DEFINITIVO.md). Todo lo demás pide un toque; esto no reemplaza la sesión, Oracle ni la orden.
    /// </summary>
    public static class PoliticaAperturaAutomatica
    {
        public static bool Permite(ClasificacionImagen clasificacion) =>
            clasificacion.Resultado == ResultadoClasificacion.Identificado
            && clasificacion.Corroborada
            && clasificacion.Estado != EstadoObservable.Contaminado
            && clasificacion.Consulta is { Admision: AdmisionMaterial.Admitido } consulta
            && consulta.Material.Categoria == CategoriaMaterial.Aprovechable
            && consulta.Guia.ColorCaneca is not null
            && clasificacion.Objeto is { } objeto
            && Coinciden(consulta.Material.Codigo, objeto);

        /// <summary>Pares objeto-material admitidos. Papel, multicapa, bolsas y orgánicos quedan fuera: su error medido es alto.</summary>
        public static bool Coinciden(string codigoMaterial, ObjetoVisible objeto) => codigoMaterial switch
        {
            "LATA_ALUMINIO" => objeto == ObjetoVisible.Lata,
            "VIDRIO" => objeto == ObjetoVisible.EnvaseVidrio,
            "CARTON" => objeto is ObjetoVisible.Caja or ObjetoVisible.Carton,
            "PET" or "PLASTICO_SIN_ID" => objeto == ObjetoVisible.BotellaPlastica,
            _ => false,
        };
    }
}
