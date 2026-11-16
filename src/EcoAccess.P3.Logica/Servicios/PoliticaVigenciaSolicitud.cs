using System;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Ventana provisional del piloto: una solicitud abandonada no mantiene autorización indefinida.</summary>
    public static class PoliticaVigenciaSolicitud
    {
        public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Oracle guarda TIMESTAMP con microsegundos y redondea: una hora recién guardada puede volver hasta medio
        /// microsegundo «en el futuro». Con el reloj de .NET Framework (saltos de hasta 15,6 ms) el «ahora» siguiente
        /// puede ser el mismo valor, y la solicitud recién creada se declaraba vencida (caso real del 10/11/2026 contra
        /// un Oracle local rápido). Se tolera exactamente esa precisión de almacenamiento, nada más.
        /// </summary>
        public static readonly TimeSpan PrecisionAlmacenamiento = TimeSpan.FromTicks(10);

        public static bool EstaVigente(DateTime inicioUtc, DateTime ahoraUtc) =>
            inicioUtc.Kind == DateTimeKind.Utc && ahoraUtc.Kind == DateTimeKind.Utc
            && inicioUtc <= ahoraUtc + PrecisionAlmacenamiento && ahoraUtc - inicioUtc < Vigencia;
    }
}
