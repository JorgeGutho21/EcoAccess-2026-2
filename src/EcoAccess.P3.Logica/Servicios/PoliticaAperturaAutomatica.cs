using System.Linq;
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
        public static bool Permite(ClasificacionImagen clasificacion) => PermiteAprovechable(clasificacion) || PermiteEnEstacion(clasificacion);

        /// <summary>
        /// Verde y negra de una estación de tres canecas: orgánicos y no aprovechables identificados por el objeto y admitidos
        /// por ESA caneca en Oracle. No exige segunda lectura corroborada (el modelo no la ofrece para comida) ni estado limpio
        /// (lo sanitario se ve sucio por definición); sí exige un par objeto-material verificable. El peor error posible es una
        /// caneca de color equivocado, nunca la blanca con un residuo contaminado.
        /// </summary>
        public static bool PermiteEnEstacion(ClasificacionImagen clasificacion) =>
            clasificacion.Resultado == ResultadoClasificacion.Identificado
            // Desde las órdenes 1685 y 1686 (bolsa de agua leída como empaque metalizado, abrió la NEGRA): también se exige que una
            // segunda lectura independiente coincida. Antes bastaba con un solo nombre.
            && clasificacion.Corroborada
            && clasificacion.Consulta is { Admision: AdmisionMaterial.Admitido } consulta
            && consulta.Material.Categoria is CategoriaMaterial.OrganicoAprovechable or CategoriaMaterial.NoAprovechable
            && consulta.Guia.ColorCaneca is not null
            && clasificacion.Objeto is { } objeto
            && CoincidenEnEstacion(consulta.Material.Codigo, objeto);

        /// <summary>Pares objeto-material de la verde y la negra que se aceptan sin intervención.</summary>
        public static bool CoincidenEnEstacion(string codigoMaterial, ObjetoVisible objeto) => codigoMaterial switch
        {
            "ORGANICO" or "RESTOS_ALIMENTOS" => objeto == ObjetoVisible.ResiduoOrganico,
            "SERVILLETA_USADA" or "PANAL" or "CONDON" or "OTRO_RESIDUO" => objeto == ObjetoVisible.ArticuloSanitario,
            "OBJETO_COMPUESTO" => objeto == ObjetoVisible.EmpaqueMetalizado,
            _ => false,
        };

        /// <summary>
        /// Caneca VERDE o NEGRA que esta clasificación abriría sola en una estación, suponiendo que Oracle admite el material allí;
        /// null si no abriría ninguna. Es la misma regla que aplican ServicioClasificacionImagen.ParaCanecaDeEstacionAsync y
        /// <see cref="PermiteEnEstacion"/>, sin consultar Oracle: sirve para medir falsas aperturas en las evaluaciones.
        /// </summary>
        public static DestinoResiduo? CanecaEstacionPrevista(ClasificacionImagen c)
        {
            if (c is null || c.Resultado != ResultadoClasificacion.ObjetoReconocido || c.Reconocido is not { } visto) return null;
            if (CatalogoObjetos.MaterialEnEstacion(visto) is not { } codigo || !c.Corroborada || c.Confianza is < ServicioClasificacionImagen.UmbralConfianza)
                return null;
            if (ServicioClasificacionImagen.MensajesObjetosConfundibles.ContainsKey(visto.Clave)) return null;
            ObjetoVisible? visible = visto.Objeto ?? (codigo == CatalogoObjetos.MaterialAlimentos ? ObjetoVisible.ResiduoOrganico : null);
            if (visible is not { } v || !CoincidenEnEstacion(codigo, v)) return null;
            return CatalogoObjetos.AlimentosPorConfirmar.Contains(visto.Clave) ? DestinoResiduo.Verde : ResolutorDestino.PorContexto(visto.Contexto);
        }

        private static bool PermiteAprovechable(ClasificacionImagen clasificacion) =>
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
