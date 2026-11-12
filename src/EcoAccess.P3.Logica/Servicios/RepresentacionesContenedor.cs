using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    public enum ModoContenedor
    {
        /// <summary>Caneca física de la estación (ESP32, GPIO y Oracle). Solo se mueve en una prueba supervisada.</summary>
        Real = 1,

        /// <summary>La misma caneca en <see cref="SimuladorEstacionTresCanecas"/>: sin ESP32, sin Oracle y sin depósitos.</summary>
        Virtual = 2,
    }

    /// <param name="IdContenedor">Contenedor de Oracle al que corresponde. La virtual comparte el id solo para consultar la admisión;
    /// nunca crea órdenes, eventos ni depósitos en Oracle.</param>
    /// <param name="Gpio">GPIO del servo en la estación física; null en la virtual.</param>
    public sealed record RepresentacionContenedor(string IdLogico, ModoContenedor Modo, DestinoResiduo Color, int IdContenedor, int? Gpio);

    /// <summary>
    /// Fase 1 de la misión final: exactamente seis representaciones, tres REALES y tres VIRTUALES (una por color). No crea
    /// contenedores en Oracle, no cambia identificadores ni GPIO, y comparte el único QR oficial de la estación.
    /// Ids de Oracle y GPIO verificados físicamente (commits 1fc2443 y c7eea5b): blanca 1093/19, negra 1401/18, verde 1400/23.
    /// </summary>
    public static class RepresentacionesContenedor
    {
        public static IReadOnlyList<RepresentacionContenedor> Todas { get; } = new[]
        {
            new RepresentacionContenedor("REAL-BLANCA", ModoContenedor.Real, DestinoResiduo.Blanca, 1093, 19),
            new RepresentacionContenedor("REAL-NEGRA", ModoContenedor.Real, DestinoResiduo.Negra, 1401, 18),
            new RepresentacionContenedor("REAL-VERDE", ModoContenedor.Real, DestinoResiduo.Verde, 1400, 23),
            new RepresentacionContenedor("VIRTUAL-BLANCA", ModoContenedor.Virtual, DestinoResiduo.Blanca, 1093, null),
            new RepresentacionContenedor("VIRTUAL-NEGRA", ModoContenedor.Virtual, DestinoResiduo.Negra, 1401, null),
            new RepresentacionContenedor("VIRTUAL-VERDE", ModoContenedor.Virtual, DestinoResiduo.Verde, 1400, null),
        };

        public static IReadOnlyList<RepresentacionContenedor> DelModo(ModoContenedor modo) => Todas.Where(r => r.Modo == modo).ToList();

        public static RepresentacionContenedor Buscar(string idLogico) =>
            Todas.FirstOrDefault(r => r.IdLogico == idLogico) ?? throw new ArgumentException("Representación desconocida: " + idLogico, nameof(idLogico));

        /// <summary>La estación virtual con las tres VIRTUALES: el único destino de las aperturas múltiples mientras no haya prueba física.</summary>
        public static SimuladorEstacionTresCanecas CrearEstacionVirtual(TimeProvider reloj, TimeSpan? ventana = null) =>
            new(DelModo(ModoContenedor.Virtual).ToDictionary(r => r.IdContenedor, r => r.Color), reloj, ventana);

        /// <summary>Las canecas virtuales con su color, para planificar con el mismo motor que las reales.</summary>
        public static IReadOnlyList<CanecaEstacion> CanecasVirtuales() => DelModo(ModoContenedor.Virtual)
            .Select(r => new CanecaEstacion(r.IdContenedor, r.IdLogico, (CategoriaMaterial)(int)r.Color, true, null, null)).ToList();
    }
}
