using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Casos de uso de los lugares donde se instalan canecas: registrar, consultar, activar y desactivar.
    /// Modificarlos es solo para administradores (matriz de permisos).
    /// </summary>
    public sealed class ServicioPuntosInstalacion
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioPuntosInstalacion _puntos;
        private readonly IRepositorioContenedores _contenedores;

        public ServicioPuntosInstalacion(
            IRepositorioUsuarios usuarios,
            IRepositorioPuntosInstalacion puntos,
            IRepositorioContenedores contenedores)
        {
            _acceso = new ControlAcceso(usuarios);
            _puntos = puntos ?? throw new ArgumentNullException(nameof(puntos));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
        }

        public async Task<Resultado<int>> RegistrarAsync(
            int idActor,
            string nombre,
            string? referenciaUbicacion,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            PuntoInstalacion punto;
            try
            {
                punto = new PuntoInstalacion(nombre, referenciaUbicacion);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            if (await _puntos.ExisteNombreAsync(punto.Nombre, cancellationToken))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, $"Ya existe un punto llamado «{punto.Nombre}».");
            }

            try
            {
                return Resultado<int>.Exito(await _puntos.AgregarAsync(punto, cancellationToken));
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, $"Ya existe un punto llamado «{punto.Nombre}».");
            }
        }

        public Task<IReadOnlyList<PuntoInstalacion>> ListarAsync(CancellationToken cancellationToken = default) =>
            _puntos.ListarAsync(cancellationToken);

        public async Task<Resultado> ActivarAsync(int idActor, int idPunto, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            PuntoInstalacion? punto = await _puntos.ObtenerPorIdAsync(idPunto, cancellationToken);
            if (punto is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese punto de instalación.");
            }

            if (!punto.Activo)
            {
                try
                {
                    await _puntos.CambiarActivoAsync(idPunto, true, cancellationToken);
                }
                catch (CambioConcurrenteException ex)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
                }
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// Se rechaza mientras el punto tenga canecas en servicio: un punto inactivo no debería seguir
        /// recibiendo depósitos. Las canecas fuera de servicio o dadas de baja no lo impiden.
        /// </summary>
        /// <remarks>
        /// Otra sesión puede poner una caneca en servicio después de la lectura; el repositorio repite la regla con el
        /// punto bloqueado y lanza <see cref="CambioConcurrenteException"/>, que aquí se convierte en un rechazo.
        /// </remarks>
        public async Task<Resultado> DesactivarAsync(int idActor, int idPunto, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            PuntoInstalacion? punto = await _puntos.ObtenerPorIdAsync(idPunto, cancellationToken);
            if (punto is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese punto de instalación.");
            }

            if (!punto.Activo)
            {
                return Resultado.Exito();
            }

            IReadOnlyList<Contenedor> canecas = await _contenedores.ListarPorPuntoAsync(idPunto, cancellationToken);
            if (canecas.Any(c => c.EstaEnServicio))
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.PuntoConCanecasEnServicio);
            }

            try
            {
                await _puntos.CambiarActivoAsync(idPunto, false, cancellationToken);
            }
            catch (CambioConcurrenteException ex)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
            }

            return Resultado.Exito();
        }
    }
}
