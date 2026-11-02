using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Administración: filtra períodos comparables y revalida al actor en cada actualización.</summary>
    public sealed class ServicioInformeInstitucional
    {
        private readonly IRepositorioInformeInstitucional _repositorio;

        public ServicioInformeInstitucional(IRepositorioUsuarios usuarios, IRepositorioInformeInstitucional repositorio)
        {
            _repositorio = repositorio;
            _acceso = new(usuarios);
        }

        private readonly ControlAcceso _acceso;
        public async Task<Resultado<InformeInstitucional>> ConsultarAsync(int actor, FiltroInforme filtro, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (await _acceso.ExigirRolAsync(actor, CodigoRol.Administrador, ct) is { } rechazo)
                return Resultado<InformeInstitucional>.DesdeRechazo(rechazo);
            TimeSpan duracion = filtro.HastaUtc - filtro.DesdeUtc;
            if (filtro.DesdeUtc.Kind != DateTimeKind.Utc || filtro.HastaUtc.Kind != DateTimeKind.Utc
                || duracion <= TimeSpan.Zero || duracion > TimeSpan.FromDays(366)
                || filtro.DesdeUtc.TimeOfDay != TimeSpan.FromHours(5) || filtro.HastaUtc.TimeOfDay != TimeSpan.FromHours(5)
                || filtro.DesdeUtc.Ticks < duracion.Ticks || filtro.IdContenedor is <= 0 || filtro.IdUbicacion is <= 0)
                return Resultado<InformeInstitucional>.Rechazo(MotivoRechazo.DatosInvalidos, "Consulta un período UTC de hasta 366 días y filtros válidos.");
            var actual = await _repositorio.ConsultarAsync(filtro, ct);
            var anterior = await _repositorio.ConsultarAsync(filtro with { DesdeUtc = filtro.DesdeUtc - duracion, HastaUtc = filtro.DesdeUtc }, ct);
            var dias = await _repositorio.AgruparAsync(filtro, AgrupacionDepositos.Dia, ct);
            var materiales = await _repositorio.AgruparAsync(filtro, AgrupacionDepositos.Material, ct);
            var canecas = await _repositorio.AgruparAsync(filtro, AgrupacionDepositos.Contenedor, ct);
            var ubicaciones = await _repositorio.AgruparAsync(filtro, AgrupacionDepositos.Punto, ct);
            return Resultado<InformeInstitucional>.Exito(new(actual, anterior, dias, materiales, canecas, ubicaciones));
        }
    }
}
