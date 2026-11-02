using System;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    /// <summary>Reloj explícito para fixtures históricos; la producción conserva TimeProvider.System.</summary>
    internal sealed class RelojOracleAjustable : TimeProvider
    {
        public RelojOracleAjustable(DateTime inicio)
        {
            Ahora = inicio;
        }

        public DateTime Ahora { get; set; }
        public override DateTimeOffset GetUtcNow() => new(Ahora);
    }
}
