using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoAccess.P3.Logica.Vision35.Expo
{
    /// <summary>Una fila de EA_R_ESTADISTICAS_ESTACION: un residuo de una operación (sin datos personales).</summary>
    public sealed record FilaEstadisticaEstacion(
        string Clave, DateTime FechaLocal, string Canal, string TipoUsuario, string? Metodo, string Tipo, string? CodigoResiduo,
        string? NombreResiduo, string? Caneca, int Cantidad, string? CanecasOperacion, int DuracionMs, string EstadoHardware,
        string EstadoOperacion, bool DisposicionDeclarada, bool DisposicionVerificada);

    /// <summary>Filtros de la pantalla de estadísticas (null = todos).</summary>
    public sealed record FiltroEstadisticasEstacion(string? Caneca = null, string? Canal = null, string? TipoUsuario = null, string? Material = null);

    /// <summary>Totales calculados a partir de filas reales de Oracle (nunca datos ficticios).</summary>
    public sealed record ResumenEstadisticasEstacion(
        int Operaciones, int Invitados, int Registrados, int AperturasEjecutadas, int AperturasFallidas, int DisposicionesDeclaradas,
        int DisposicionesVerificadas, int Residuos,
        IReadOnlyList<(string Clave, int Cantidad)> PorCanal, IReadOnlyList<(string Clave, int Cantidad)> PorCaneca,
        IReadOnlyList<(string Clave, int Cantidad)> Materiales, IReadOnlyList<(string Clave, int Cantidad)> PorMetodo,
        IReadOnlyList<(string Clave, int Cantidad)> PorDia, IReadOnlyList<(string Clave, int Cantidad)> PorHora);

    /// <summary>Cálculo determinista de las estadísticas de la estación (operaciones distintas por clave).</summary>
    public static class EstadisticasEstacion
    {
        private static readonly string[] Ejecutadas = { "PWM_CONFIRMADO", "SIMULADO" };
        private static readonly string[] Fallidas = { "RECHAZADO", "SIN_RESPUESTA" };

        public static IReadOnlyList<FilaEstadisticaEstacion> Filtrar(IEnumerable<FilaEstadisticaEstacion> filas, FiltroEstadisticasEstacion f) =>
            filas.Where(x => (f.Caneca is null || string.Equals(x.Caneca, f.Caneca, StringComparison.OrdinalIgnoreCase) || (x.Caneca ?? "").Split(',').Contains(f.Caneca, StringComparer.OrdinalIgnoreCase))
                          && (f.Canal is null || x.Canal == f.Canal)
                          && (f.TipoUsuario is null || x.TipoUsuario == f.TipoUsuario)
                          && (f.Material is null || string.Equals(x.NombreResiduo, f.Material, StringComparison.OrdinalIgnoreCase))).ToList();

        public static ResumenEstadisticasEstacion Calcular(IEnumerable<FilaEstadisticaEstacion> filasFiltradas)
        {
            var filas = filasFiltradas.ToList();
            var ops = filas.GroupBy(x => x.Clave).Select(g => g.First()).ToList();
            static IReadOnlyList<(string, int)> Contar(IEnumerable<string?> claves) =>
                claves.Where(c => !string.IsNullOrWhiteSpace(c)).GroupBy(c => c!).Select(g => (g.Key, g.Count())).OrderByDescending(t => t.Item2).ThenBy(t => t.Item1).ToList();
            var conResiduo = filas.Where(x => x.NombreResiduo is not null && x.CodigoResiduo is not null).ToList();
            return new ResumenEstadisticasEstacion(
                ops.Count, ops.Count(o => o.TipoUsuario == "INVITADO"), ops.Count(o => o.TipoUsuario == "REGISTRADO"),
                ops.Count(o => Ejecutadas.Contains(o.EstadoHardware) && o.EstadoOperacion != "ERROR"),
                ops.Count(o => Fallidas.Contains(o.EstadoHardware) || o.EstadoOperacion == "ERROR"),
                ops.Count(o => o.DisposicionDeclarada), ops.Count(o => o.DisposicionVerificada),
                conResiduo.Sum(x => Math.Max(1, x.Cantidad)),
                Contar(ops.Select(o => o.Canal)),
                Contar(conResiduo.Count > 0 ? conResiduo.Select(x => x.Caneca) : ops.SelectMany(o => (o.CanecasOperacion ?? "").Split(','))),
                Contar(conResiduo.Select(x => x.NombreResiduo)).Take(15).ToList(),
                Contar(ops.Select(o => o.Metodo ?? "Sin dato")),
                Contar(ops.Select(o => o.FechaLocal.ToString("yyyy-MM-dd"))).OrderBy(t => t.Item1).ToList(),
                Contar(ops.Select(o => o.FechaLocal.ToString("HH") + ":00")).OrderBy(t => t.Item1).ToList());
        }
    }
}
