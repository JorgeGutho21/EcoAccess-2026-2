using System;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Ventana provisional del piloto: una solicitud abandonada no mantiene autorización indefinida.</summary>
    public static class PoliticaVigenciaSolicitud
    {
        public static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(15);

        public static bool EstaVigente(DateTime inicioUtc, DateTime ahoraUtc) =>
            inicioUtc.Kind == DateTimeKind.Utc && ahoraUtc.Kind == DateTimeKind.Utc
            && inicioUtc <= ahoraUtc && ahoraUtc - inicioUtc < Vigencia;
    }
}
