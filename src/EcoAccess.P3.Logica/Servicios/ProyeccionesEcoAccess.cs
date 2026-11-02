using System;
using System.Collections.Generic;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Contenido educativo compartido. Describe planes; no registra canecas, ubicaciones ni dispositivos activos.</summary>
    public static class ProyeccionesEcoAccess
    {
        public static IReadOnlyList<(string Titulo, string Descripcion)> Todas { get; } = Array.AsReadOnly(new[]
        {
            ("Separación inteligente","Proyección: canecas blancas para aprovechables, negras para no aprovechables y verdes para orgánicos aprovechables. El piloto actual tiene un solo compartimiento."),
            ("EcoAccess en toda la UPC","Expansión planificada a bloques A, B, C, H, I, P y al comedor universitario. Estas ubicaciones futuras no se presentan como instalaciones operativas."),
            ("Reconocimiento inteligente","Mejorar la orientación por fotografía con evaluaciones autorizadas y confirmación del ciudadano. Hoy siempre está disponible la selección manual."),
            ("Trazabilidad ambiental","Seguimiento desde el depósito hasta su gestión posterior cuando exista información verificable; una solicitud no demuestra reciclaje."),
            ("Estadísticas avanzadas","Ampliar consultas por día, semana, mes, material, bloque y contenedor a medida que existan datos reales."),
            ("EcoAccess en crecimiento","Participación ciudadana, nuevas ubicaciones y mejora de la gestión ambiental universitaria. Los planes no generan depósitos ni ecopuntos.")
        });
    }
}
