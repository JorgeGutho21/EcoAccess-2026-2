using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    public sealed record CotizacionEcopuntos(int CantidadDeclarada, int Tarifa, bool PorUnidad, int PuntosPotenciales);

    /// <summary>Créditos solo con verificación; correcciones mediante reverso y sucesor, sin borrar historia.</summary>
    public sealed class ServicioEcopuntos
    {
        private readonly IRepositorioUsuarios _usuarios;
        private readonly IRepositorioInteracciones _interacciones;
        private readonly IRepositorioMateriales _materiales;
        private readonly IRepositorioEcopuntos _repositorio;
        private readonly TimeProvider _reloj;

        public ServicioEcopuntos(IRepositorioUsuarios usuarios, IRepositorioInteracciones interacciones, IRepositorioMateriales materiales, IRepositorioEcopuntos repositorio, TimeProvider reloj)
        {
            _usuarios = usuarios;
            _interacciones = interacciones;
            _materiales = materiales;
            _repositorio = repositorio;
            _reloj = reloj;
            _acceso = new(usuarios);
        }

        private readonly ControlAcceso _acceso;

        public async Task<Resultado<IReadOnlyList<ReglaEcopuntos>>> ListarReglasAsync(int actor, CancellationToken ct = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(actor, ct, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
                return Resultado<IReadOnlyList<ReglaEcopuntos>>.DesdeRechazo(rechazo);
            return Resultado<IReadOnlyList<ReglaEcopuntos>>.Exito(await _repositorio.ListarReglasAsync(ct));
        }

        public async Task<Resultado<int>> RegistrarReglaAsync(int actor, int material, int puntos, string descripcion,
            DateTime desdeUtc, DateTime? hastaUtc = null, CancellationToken ct = default, bool porUnidad = false)
        {
            if (await _acceso.ExigirRolAsync(actor, CodigoRol.Administrador, ct) is { } rechazo)
                return Resultado<int>.DesdeRechazo(rechazo);
            if (await _materiales.ObtenerPorIdAsync(material, ct) is not { Habilitado: true })
                return Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, "El material no existe o está deshabilitado.");
            try
            {
                var regla = new ReglaEcopuntos(0, material, puntos, descripcion, desdeUtc, hastaUtc, actor, porUnidad);
                return Resultado<int>.Exito(await _repositorio.AgregarReglaAsync(regla, ct));
            }
            catch (ArgumentException ex) { return MensajesValidacion.Rechazo<int>(ex); }
        }

        /// <summary>Estimación informativa; no reserva una tarifa ni escribe créditos. La acreditación usa cantidad verificada.</summary>
        public async Task<Resultado<CotizacionEcopuntos>> ConsultarPotencialesAsync(int actor, int material, int cantidadDeclarada, CancellationToken ct = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(actor, ct) is { } rechazo)
                return Resultado<CotizacionEcopuntos>.DesdeRechazo(rechazo);
            if (cantidadDeclarada is < 1 or > 1000)
                return Resultado<CotizacionEcopuntos>.Rechazo(MotivoRechazo.DatosInvalidos, "Declara entre 1 y 1000 unidades.");
            if (await _materiales.ObtenerPorIdAsync(material, ct) is not { Habilitado: true })
                return Resultado<CotizacionEcopuntos>.Rechazo(MotivoRechazo.NoEncontrado, "El material no está disponible.");
            var regla = (await _repositorio.ListarReglasAsync(ct)).Where(r => r.IdMaterial == material && r.EstaVigente(_reloj.GetUtcNow().UtcDateTime))
                .OrderByDescending(r => r.DesdeUtc).ThenByDescending(r => r.Id).FirstOrDefault();
            if (regla is null) return Resultado<CotizacionEcopuntos>.Rechazo(MotivoRechazo.NoEncontrado, "No hay una tarifa vigente para estimar puntos.");
            try { return Resultado<CotizacionEcopuntos>.Exito(new(cantidadDeclarada, regla.Puntos, regla.PorUnidad, regla.CalcularPuntos(cantidadDeclarada))); }
            catch (ArgumentException error) { return MensajesValidacion.Rechazo<CotizacionEcopuntos>(error); }
        }

        public Task<Resultado<IReadOnlyList<MovimientoEcopuntos>>> AcreditarAsync(int actor, int deposito, int regla,
            string clave, CancellationToken ct = default) => AplicarAsync(actor, deposito, null, regla,
                TipoOperacionEcopuntos.Acreditar, clave, "Crédito por depósito con material verificado.", ct);

        public Task<Resultado<IReadOnlyList<MovimientoEcopuntos>>> RevertirAsync(int actor, int credito,
            string clave, string motivo, CancellationToken ct = default) => DesdeCreditoAsync(actor, credito, null,
                TipoOperacionEcopuntos.Revertir, clave, motivo, ct);

        public Task<Resultado<IReadOnlyList<MovimientoEcopuntos>>> CorregirAsync(int actor, int credito, int regla,
            string clave, string motivo, CancellationToken ct = default) => DesdeCreditoAsync(actor, credito, regla,
                TipoOperacionEcopuntos.Corregir, clave, motivo, ct);

        private async Task<Resultado<IReadOnlyList<MovimientoEcopuntos>>> DesdeCreditoAsync(int actor, int credito,
            int? regla, TipoOperacionEcopuntos tipo, string clave, string motivo, CancellationToken ct)
        {
            if (await _acceso.ExigirRolAsync(actor, CodigoRol.Administrador, ct) is { } rechazo)
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.DesdeRechazo(rechazo);
            MovimientoEcopuntos? original = await _repositorio.ObtenerMovimientoAsync(credito, ct);
            if (original is not { Tipo: TipoMovimientoEcopuntos.Credito })
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese crédito.");
            return await AplicarAsync(actor, original.IdDeposito, credito, regla, tipo, clave, motivo, ct);
        }

        private async Task<Resultado<IReadOnlyList<MovimientoEcopuntos>>> AplicarAsync(int actor, int idDeposito,
            int? credito, int? idRegla, TipoOperacionEcopuntos tipo, string clave, string motivo, CancellationToken ct)
        {
            if (await _acceso.ExigirAlgunRolAsync(actor, ct, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.DesdeRechazo(rechazo);
            if (Compat.IsNullOrWhiteSpace(clave) || clave.Trim().Length > 80
                || Compat.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 300)
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.DatosInvalidos, "Clave (máximo 80) y motivo (máximo 300) son obligatorios.");

            var solicitud = new SolicitudEcopuntos(tipo, idDeposito, credito, idRegla, actor, clave.Trim(), motivo.Trim(), _reloj.GetUtcNow().UtcDateTime);
            if (await _repositorio.ObtenerOperacionAsync(solicitud.Clave, ct) is { } anterior)
            {
                bool misma = anterior.Solicitud with { FechaUtc = solicitud.FechaUtc } == solicitud;
                return misma ? Resultado<IReadOnlyList<MovimientoEcopuntos>>.Exito(anterior.Movimientos)
                    : Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.Duplicado, "La clave ya identifica otra operación.");
            }

            Deposito? deposito = await _interacciones.ObtenerDepositoAsync(idDeposito, ct);
            if (deposito is not { EstaVerificado: true })
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.EstadoNoPermitido, "Se requiere un depósito con material verificado.");
            OrdenApertura? orden = await _interacciones.ObtenerOrdenAsync(deposito.IdOrden, ct);
            Interaccion? interaccion = orden is null ? null : await _interacciones.ObtenerInteraccionAsync(orden.IdInteraccion, ct);
            if (interaccion?.IdUsuario is not { } ciudadano || ciudadano == actor || deposito.IdVerificador == ciudadano)
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.SinPermiso, "El depósito debe tener titular y verificación independiente; no se acreditan operaciones propias.");
            if (idRegla is { } id)
            {
                ReglaEcopuntos? regla = await _repositorio.ObtenerReglaAsync(id, ct);
                if (regla is null || regla.IdMaterial != deposito.IdMaterialVerificado
                    || !regla.EstaVigente(_reloj.GetUtcNow().UtcDateTime))
                    return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.EstadoNoPermitido, "No hay una regla vigente para el material verificado.");
                try { _ = regla.CalcularPuntos(deposito.CantidadVerificada); }
                catch (Exception error) when (error is ArgumentException or InvalidOperationException)
                { return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.EstadoNoPermitido, error.Message); }
            }
            try
            {
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Exito(await _repositorio.AplicarAsync(solicitud, ct));
            }
            catch (CambioConcurrenteException ex)
            {
                return Resultado<IReadOnlyList<MovimientoEcopuntos>>.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
            }
        }

        public async Task<Resultado<ResumenEcopuntos>> ConsultarMiosAsync(int actor, CancellationToken ct = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(actor, ct) is { } rechazo)
                return Resultado<ResumenEcopuntos>.DesdeRechazo(rechazo);
            return Resultado<ResumenEcopuntos>.Exito(await _repositorio.ConsultarAsync(actor, ct));
        }

        public async Task<Resultado<ResumenEcopuntos>> ConsultarAsync(int actor, int ciudadano, CancellationToken ct = default)
        {
            if (await _acceso.ExigirAlgunRolAsync(actor, ct, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
                return Resultado<ResumenEcopuntos>.DesdeRechazo(rechazo);
            if (ciudadano <= 0) return Resultado<ResumenEcopuntos>.Rechazo(MotivoRechazo.DatosInvalidos, "Indica un ciudadano válido.");
            if (await _usuarios.ObtenerPorIdAsync(ciudadano, ct) is not { } titular || !titular.TieneRol(CodigoRol.Ciudadano))
                return Resultado<ResumenEcopuntos>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa cuenta de ciudadano.");
            return Resultado<ResumenEcopuntos>.Exito(await _repositorio.ConsultarAsync(ciudadano, ct));
        }
    }
}
