using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Servicios de la lógica que Program compone y entrega al menú. Agruparlos evita que el constructor de
    /// <see cref="FormPrincipal"/> crezca con cada pantalla nueva; los formularios reciben solo los que usan.
    /// <see cref="NombreBotTelegram"/> es la configuración <c>Telegram:NombreBot</c>, usada para el QR de las canecas;
    /// es nula si no está configurada y entonces la pantalla de canecas avisa en vez de imprimir un QR inservible.
    /// </summary>
    public sealed record ServiciosAplicacion(
        ServicioMateriales Materiales,
        ServicioAutenticacion Autenticacion,
        ServicioPuntosInstalacion Puntos,
        ServicioContenedores Contenedores,
        ServicioUsuarios Usuarios,
        ServicioInteracciones Interacciones,
        ServicioEstadisticas Estadisticas,
        string? NombreBotTelegram,
        ServicioEcopuntos Ecopuntos,
        ServicioDiagnosticoOracle DiagnosticoOracle,
        ServicioPanelAdministrativo PanelAdministrativo,
        ServicioRegistroCiudadanos RegistroCiudadanos,
        ServicioCatalogoAmbiental CatalogoAmbiental,
        ServicioOpiniones? Opiniones = null,
        ServicioInformeInstitucional? InformeInstitucional = null,
        ServicioOrientacionEspecial? Orientacion = null)
    {
        /// <summary>
        /// Ejecuta en la caneca física las órdenes de apertura de Oracle (registro manual y depósitos de la app) a través del
        /// servicio central de hardware (panel ECOACCESS ESP32, local o del anfitrión). Se crea en cada uso para ver el
        /// estado actual de la estación.
        /// </summary>
        public System.Func<System.Threading.Tasks.Task<EjecutorOrdenesEstacion>>? EjecutorEstacion { get; init; }
    }
}
