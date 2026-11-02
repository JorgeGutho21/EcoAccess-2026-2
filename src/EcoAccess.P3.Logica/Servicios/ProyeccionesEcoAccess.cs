using System;
using System.Collections.Generic;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Contenido educativo compartido. Describe planes; no registra canecas, ubicaciones ni dispositivos activos.</summary>
    public static class ProyeccionesEcoAccess
    {
        public const string MensajeInstitucional = "Hoy comenzamos con una caneca. Con tu participación, construiremos una UPC más inteligente y sostenible.";

        public static IReadOnlyList<(string Titulo, string Descripcion)> Todas { get; } = Array.AsReadOnly(new[]
        {
            ("Separación inteligente", "Canecas blancas para residuos aprovechables, negras para no aprovechables y verdes para orgánicos aprovechables, con gestión diferenciada de residuos especiales."),
            ("EcoAccess en la Universidad Popular del Cesar", "Expansión propuesta a los bloques A, B, C, H, I y P y al comedor universitario. Son propuestas: no hay instalaciones verificadas en esas ubicaciones."),
            ("Ecopuntos avanzados", "Recompensas, beneficios, clasificaciones, incentivos ambientales, retos y participación ciudadana."),
            ("Estadísticas", "Materiales registrados, actividad por fechas, participación ciudadana, actividad por contenedor e indicadores por ubicación."),
            ("Dashboard administrativo", "Indicadores ambientales, gráficos, filtros, historial, comparaciones y estado de los contenedores."),
            ("Power BI", "Análisis institucional, reportes, indicadores históricos, visualizaciones y seguimiento del crecimiento."),
            ("Trazabilidad ambiental", "Seguimiento de depósito, recolección, separación, transporte, gestor responsable y aprovechamiento o disposición final verificable. Un residuo no se considera reciclado solo por ingresar a EcoAccess.")
        });
    }
}
