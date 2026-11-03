using System;
using System.Collections.Generic;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    public enum EstadoProyeccion { Actual, EnDesarrollo, Futura }

    public sealed record ElementoEvolucion(string Titulo, string Descripcion, EstadoProyeccion Estado);

    public sealed record CanecaProyectada(string Nombre, CategoriaMaterial Categoria, string MaterialesOrientativos, string Descripcion, string Funcionalidad);

    public sealed record BloqueProyectado(string Nombre, bool EsComedor, string Objetivo, string Integracion);

    /// <summary>Contenido educativo compartido. Describe planes; no registra canecas, ubicaciones, cantidades, fechas ni dispositivos activos.</summary>
    public static class ProyeccionesEcoAccess
    {
        public const string MensajeInstitucional = "Hoy comenzamos con una caneca. Con tu participación, construiremos una UPC más inteligente y sostenible.";

        public const string AvisoBloques = "Proyección futura: estas ubicaciones no tienen canecas de EcoAccess. Los nombres los aporta el proyecto y están por confirmar con la UPC. Las ilustraciones son conceptuales: no muestran fachadas, planos ni posiciones reales.";

        public const string CantidadCanecas = "Cantidad de canecas: pendiente de planificación.";

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

        public static IReadOnlyList<ElementoEvolucion> Evolucion { get; } = Array.AsReadOnly(new[]
        {
            new ElementoEvolucion("Caneca piloto y registro", "Una caneca piloto registrada en Oracle, con solicitud por QR, Telegram o registro manual y validación antes de abrir.", EstadoProyeccion.Actual),
            new ElementoEvolucion("Reconocimiento visual", "El modelo propone un material y las reglas de seguridad deciden si abre o pide un toque. La admisión la decide el catálogo, nunca el modelo.", EstadoProyeccion.Actual),
            new ElementoEvolucion("Ecopuntos verificados", "Los puntos se acreditan después de verificar un depósito, según la tarifa vigente.", EstadoProyeccion.Actual),
            new ElementoEvolucion("Dashboard en EcoAccess", "Indicadores, gráficos, filtros y comparaciones con datos de Oracle, sin licencias adicionales.", EstadoProyeccion.Actual),
            new ElementoEvolucion("Apertura física con ESP32", "El firmware y el protocolo existen. Falta comprobar alimentación, calibración y los 10 segundos con hardware real.", EstadoProyeccion.EnDesarrollo),
            new ElementoEvolucion("Informes en Power BI", "Consultas, modelo y medidas preparados. El informe se construye en Power BI Desktop.", EstadoProyeccion.EnDesarrollo),
            new ElementoEvolucion("Recompensas y retos", "Beneficios, clasificaciones e incentivos ambientales. Hoy no hay nada que se pueda reclamar.", EstadoProyeccion.Futura),
            new ElementoEvolucion("Trazabilidad ambiental", "Seguimiento de recolección, transporte, gestor responsable y aprovechamiento o disposición final verificable.", EstadoProyeccion.Futura),
        });

        public static IReadOnlyList<CanecaProyectada> CanecasFuturas { get; } = Array.AsReadOnly(new[]
        {
            new CanecaProyectada("Caneca blanca", CategoriaMaterial.Aprovechable, "papel y cartón limpios, plástico, vidrio y metales", "Residuos aprovechables limpios y secos.", "Proponer el material con reconocimiento visual y abrir solo si la persona confirma."),
            new CanecaProyectada("Caneca negra", CategoriaMaterial.NoAprovechable, "servilletas usadas, papeles sucios y empaques no aprovechables", "Residuos no aprovechables.", "Orientar a la persona cuando un residuo no se puede aprovechar."),
            new CanecaProyectada("Caneca verde", CategoriaMaterial.OrganicoAprovechable, "restos de comida, cáscaras y residuos de jardín", "Residuos orgánicos aprovechables.", "Separar lo orgánico del resto para facilitar su aprovechamiento."),
            new CanecaProyectada("Residuos especiales", CategoriaMaterial.Peligroso, "pilas, medicamentos, bombillos, aparatos y cortopunzantes", "No son residuos ordinarios: necesitan una gestión diferenciada.", "Indicar el punto de entrega adecuado en lugar de ofrecer una caneca común."),
        });

        public static IReadOnlyList<BloqueProyectado> Bloques { get; } = Array.AsReadOnly(new[]
        {
            Bloque("Bloque A"), Bloque("Bloque B"), Bloque("Bloque C"), Bloque("Bloque H"), Bloque("Bloque I"), Bloque("Bloque P"),
            new BloqueProyectado("Comedor universitario", true, "Propuesta para separar residuos de comida y empaques donde más se generan.", "Se integraría con QR, Telegram, reconocimiento visual, Oracle, registro de depósitos y estadísticas."),
        });

        private static BloqueProyectado Bloque(string nombre) =>
            new(nombre, false, "Propuesta de punto inteligente de disposición de residuos.", "Se integraría con QR, Telegram, reconocimiento visual, Oracle, registro de depósitos y estadísticas.");
    }
}
