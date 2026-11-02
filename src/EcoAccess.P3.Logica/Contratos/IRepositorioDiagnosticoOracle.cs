using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Inventario de solo lectura del esquema conectado, sin SQL aportado por el usuario.</summary>
    public interface IRepositorioDiagnosticoOracle
    {
        Task<EstadoEsquemaOracle> ConsultarAsync(CancellationToken cancellationToken = default);
    }

    public sealed record EstadoEsquemaOracle(
        string Esquema,
        string Contenedor,
        double LatenciaMilisegundos,
        IReadOnlyList<string> Tablas,
        int RestriccionesProblematicas,
        int ObjetosInvalidos,
        int IndicesInvalidos,
        IReadOnlyList<string> RestriccionesValidadas);

    /// <summary>La presencia de tablas es evidencia estructural, no un historial de ejecución de DDL.</summary>
    public sealed record EstadoMigracionOracle(string Version, string Estado, string ElementosFaltantes);

    public sealed record DiagnosticoOracle(
        DateTimeOffset VerificadoUtc,
        EstadoEsquemaOracle Esquema,
        IReadOnlyList<EstadoMigracionOracle> Migraciones)
    {
        public bool EstructuraCompleta => Migraciones.All(m => m.Estado == "Estructura presente");
        public bool SinIncidencias => EstructuraCompleta
            && Esquema.RestriccionesProblematicas == 0
            && Esquema.ObjetosInvalidos == 0
            && Esquema.IndicesInvalidos == 0;
    }
}
