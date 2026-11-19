using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Depósitos de un día para la gráfica de la semana.</summary>
    public sealed record DepositosDia(DateOnly Dia, int Depositos);

    /// <summary>
    /// Todo lo que muestra el panel del ciudadano, con datos reales de Oracle. No incluye porcentajes de llenado: las
    /// canecas no tienen sensor de nivel, así que solo se informa si están en servicio.
    /// </summary>
    /// <param name="TelegramVinculado">null si no se pudo consultar; el panel lo dice en vez de suponer.</param>
    public sealed record PanelCiudadano(
        int Depositos,
        int Verificados,
        long Saldo,
        int MaterialesDistintos,
        DateTime? UltimoDepositoUtc,
        IReadOnlyList<DepositosDia> Semana,
        IReadOnlyList<FilaDeposito> Recientes,
        IReadOnlyList<CanecaEstacion> Canecas,
        bool? TelegramVinculado);

    /// <summary>
    /// Fachada de lectura del panel del ciudadano. No tiene SQL propio: reutiliza <see cref="ServicioEstadisticas"/> y
    /// <see cref="ServicioEcopuntos"/> (que exigen que el actor sea un ciudadano habilitado y solo devuelven sus datos) y
    /// los repositorios de canecas y vínculos. Cualquier diseño de interfaz usa este mismo servicio.
    /// </summary>
    public sealed class ServicioPanelCiudadano
    {
        /// <summary>Período del resumen; es el máximo que admite <see cref="ServicioEstadisticas.ObtenerMiasAsync"/>.</summary>
        public const int DiasResumen = 90;

        private readonly ServicioEstadisticas _estadisticas;
        private readonly ServicioEcopuntos _ecopuntos;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioEstaciones _estaciones;
        private readonly IRepositorioVinculosTelegram _vinculos;
        private readonly TimeProvider _reloj;

        public ServicioPanelCiudadano(ServicioEstadisticas estadisticas, ServicioEcopuntos ecopuntos, IRepositorioContenedores contenedores,
            IRepositorioEstaciones estaciones, IRepositorioVinculosTelegram vinculos, TimeProvider reloj)
        {
            _estadisticas = estadisticas ?? throw new ArgumentNullException(nameof(estadisticas));
            _ecopuntos = ecopuntos ?? throw new ArgumentNullException(nameof(ecopuntos));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _estaciones = estaciones ?? throw new ArgumentNullException(nameof(estaciones));
            _vinculos = vinculos ?? throw new ArgumentNullException(nameof(vinculos));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public async Task<Resultado<PanelCiudadano>> ObtenerAsync(int idActor, CancellationToken ct = default)
        {
            DateTime hasta = _reloj.GetUtcNow().UtcDateTime;
            DateTime desde = hasta.AddDays(-DiasResumen);

            // Los dos servicios validan que el actor sea un ciudadano habilitado; si no lo es, el panel no recibe nada.
            Resultado<EstadisticasPersonales> estadisticas = await _estadisticas.ObtenerMiasAsync(idActor, desde, hasta, ct);
            if (!estadisticas.Exitoso) return Resultado<PanelCiudadano>.Rechazo(estadisticas.Motivo!.Value, estadisticas.Mensaje!);
            Resultado<ResumenEcopuntos> ecopuntos = await _ecopuntos.ConsultarMiosAsync(idActor, ct);
            if (!ecopuntos.Exitoso) return Resultado<PanelCiudadano>.Rechazo(ecopuntos.Motivo!.Value, ecopuntos.Mensaje!);
            Resultado<PaginaDepositos> recientes = await _estadisticas.ListarMiosAsync(idActor, desde, hasta, 1, 5, ct);
            if (!recientes.Exitoso) return Resultado<PanelCiudadano>.Rechazo(recientes.Motivo!.Value, recientes.Mensaje!);

            EstadisticasPersonales datos = estadisticas.Valor;
            List<FilaDeposito> filas = recientes.Valor.Filas.OrderByDescending(f => f.FechaDeteccionUtc).ToList();
            DateOnly hoy = DateOnly.FromDateTime(hasta + HoraColombia.Desfase);
            var porDia = datos.Evolucion.Where(f => f.Dia is not null).ToDictionary(f => f.Dia!.Value, f => f.Depositos);
            List<DepositosDia> semana = Enumerable.Range(0, 7)
                .Select(i => hoy.AddDays(i - 6))
                .Select(dia => new DepositosDia(dia, porDia.TryGetValue(dia, out int n) ? n : 0))
                .ToList();

            bool? vinculado;
            try { vinculado = await _vinculos.ObtenerVinculoActivoPorUsuarioAsync(idActor, ct) is not null; }
            catch (Exception e) when (e is not OperationCanceledException) { vinculado = null; }

            return Resultado<PanelCiudadano>.Exito(new PanelCiudadano(
                datos.Actual.Depositos.Total,
                datos.Actual.Depositos.Verificados,
                ecopuntos.Valor.Saldo,
                datos.Materiales.Count(m => m.Depositos > 0),
                filas.FirstOrDefault()?.FechaDeteccionUtc,
                semana,
                filas,
                await CanecasEstacionAsync(ct),
                vinculado));
        }

        /// <summary>Las tres canecas de la estación piloto tal como están configuradas en Oracle (público: no tiene datos personales).</summary>
        public async Task<IReadOnlyList<CanecaEstacion>> CanecasEstacionAsync(CancellationToken ct = default)
        {
            var piloto = (await _contenedores.ListarAsync(ct)).FirstOrDefault(c => c.Nombre == ServicioCatalogoAmbiental.NombreCanecaPiloto);
            if (piloto is null) return Array.Empty<CanecaEstacion>();
            return (await _estaciones.ListarEstacionDeAsync(piloto.Id, ct)).OrderBy(c => c.Categoria).ToList();
        }
    }
}
