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
    /// Casos de uso de las canecas: registro con su QR, materiales admitidos y cambios de estado.
    /// El prototipo trabaja con una caneca de un compartimento; la lista de materiales indica qué puede
    /// depositarse en ella. Modificarlas es solo para administradores (matriz de permisos).
    /// </summary>
    /// <remarks>
    /// Las reglas que dependen de otras tablas (punto activo, material habilitado) se comprueban aquí
    /// antes de llamar a la entidad, para devolver un <see cref="Resultado"/> claro en lugar de una excepción.
    /// </remarks>
    public sealed class ServicioContenedores
    {
        /// <summary>Intentos de generar un QR único. Con 128 bits aleatorios, un choque es casi imposible.</summary>
        internal const int IntentosGenerarQr = 3;

        private readonly ControlAcceso _acceso;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioPuntosInstalacion _puntos;
        private readonly IRepositorioMateriales _materiales;
        private readonly IGeneradorIdentificadorQr _generadorQr;
        private readonly TimeProvider _reloj;

        public ServicioContenedores(
            IRepositorioUsuarios usuarios,
            IRepositorioContenedores contenedores,
            IRepositorioPuntosInstalacion puntos,
            IRepositorioMateriales materiales,
            IGeneradorIdentificadorQr generadorQr,
            TimeProvider reloj)
        {
            _acceso = new ControlAcceso(usuarios);
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _puntos = puntos ?? throw new ArgumentNullException(nameof(puntos));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _generadorQr = generadorQr ?? throw new ArgumentNullException(nameof(generadorQr));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        /// <summary>Registra una caneca fuera de servicio en un punto activo y le asigna un QR nuevo.</summary>
        public async Task<Resultado<int>> RegistrarAsync(
            int idActor,
            int idPunto,
            string nombre,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            PuntoInstalacion? punto = await _puntos.ObtenerPorIdAsync(idPunto, cancellationToken);
            if (punto is null)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese punto de instalación.");
            }

            if (!punto.Activo)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.EstadoNoPermitido, "El punto de instalación está inactivo.");
            }

            DateTime fechaAltaUtc = _reloj.GetUtcNow().UtcDateTime;
            for (int intento = 1; intento <= IntentosGenerarQr; intento++)
            {
                Contenedor caneca;
                try
                {
                    caneca = new Contenedor(idPunto, nombre, _generadorQr.Generar(), fechaAltaUtc);
                }
                catch (ArgumentException error)
                {
                    return MensajesValidacion.Rechazo<int>(error);
                }

                try
                {
                    return Resultado<int>.Exito(await _contenedores.AgregarAsync(caneca, cancellationToken));
                }
                catch (ConflictoUnicidadException) when (intento < IntentosGenerarQr)
                {
                    // El QR generado ya existía: se intenta con otro.
                }
                catch (ConflictoUnicidadException)
                {
                    return Resultado<int>.Rechazo(
                        MotivoRechazo.Duplicado,
                        "No se pudo generar un identificador QR único; intente de nuevo.");
                }
            }

            throw new InvalidOperationException("Inalcanzable: el bucle siempre devuelve un resultado.");
        }

        public async Task<Resultado<Contenedor>> ObtenerAsync(int idContenedor, CancellationToken cancellationToken = default)
        {
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            return caneca is null
                ? Resultado<Contenedor>.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.")
                : Resultado<Contenedor>.Exito(caneca);
        }

        public Task<IReadOnlyList<Contenedor>> ListarAsync(CancellationToken cancellationToken = default) =>
            _contenedores.ListarAsync(cancellationToken);

        public Task<Resultado> AdmitirMaterialAsync(
            int idActor,
            int idContenedor,
            int idMaterial,
            CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idContenedor, async caneca =>
            {
                if (caneca.Estado == EstadoContenedor.DadoDeBaja)
                {
                    return RechazoDadaDeBaja();
                }

                Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
                if (material is null)
                {
                    return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
                }

                if (!material.Habilitado)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.MaterialDeshabilitado(material.Nombre));
                }

                if (caneca.AdmiteMaterial(idMaterial))
                {
                    return Resultado.Rechazo(MotivoRechazo.Duplicado, $"La caneca ya admite «{material.Nombre}».");
                }

                caneca.AdmitirMaterial(idMaterial);
                return null;
            }, () => _contenedores.AdmitirMaterialAsync(idContenedor, idMaterial, cancellationToken), cancellationToken);

        /// <summary>
        /// En una caneca en servicio, se rechaza si después no quedaría ningún material habilitado,
        /// la misma regla que impide deshabilitar el único material de una caneca en servicio.
        /// </summary>
        public Task<Resultado> QuitarMaterialAsync(
            int idActor,
            int idContenedor,
            int idMaterial,
            CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idContenedor, async caneca =>
            {
                if (caneca.Estado == EstadoContenedor.DadoDeBaja)
                {
                    return RechazoDadaDeBaja();
                }

                if (!caneca.AdmiteMaterial(idMaterial))
                {
                    return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "La caneca no admite ese material.");
                }

                if (caneca.EstaEnServicio)
                {
                    var restantes = caneca.MaterialesAdmitidos.Where(id => id != idMaterial).ToList();
                    if (!await TieneMaterialHabilitadoAsync(restantes, cancellationToken))
                    {
                        return Resultado.Rechazo(
                            MotivoRechazo.EstadoNoPermitido,
                            MensajesCatalogo.CanecaQuedariaSinMateriales);
                    }
                }

                caneca.QuitarMaterialAdmitido(idMaterial);
                return null;
            }, () => _contenedores.QuitarMaterialAsync(idContenedor, idMaterial, cancellationToken), cancellationToken);

        /// <summary>Exige un punto activo y al menos un material habilitado admitido. Repetirlo no es un error.</summary>
        public Task<Resultado> PonerEnServicioAsync(int idActor, int idContenedor, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idContenedor, async caneca =>
            {
                switch (caneca.Estado)
                {
                    case EstadoContenedor.EnServicio:
                        return Resultado.Exito();
                    case EstadoContenedor.DadoDeBaja:
                        return RechazoDadaDeBaja();
                }

                PuntoInstalacion? punto = await _puntos.ObtenerPorIdAsync(caneca.IdPuntoInstalacion, cancellationToken);
                if (punto is not { Activo: true })
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.PuntoDeCanecaInactivo);
                }

                if (!await TieneMaterialHabilitadoAsync(caneca.MaterialesAdmitidos, cancellationToken))
                {
                    return Resultado.Rechazo(
                        MotivoRechazo.EstadoNoPermitido,
                        MensajesCatalogo.CanecaSinMaterialParaServicio);
                }

                caneca.PonerEnServicio();
                return null;
            }, () => _contenedores.CambiarEstadoAsync(idContenedor, EstadoContenedor.EnServicio, cancellationToken), cancellationToken);

        public Task<Resultado> PonerFueraDeServicioAsync(int idActor, int idContenedor, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idContenedor, caneca => Task.FromResult(caneca.Estado switch
            {
                EstadoContenedor.FueraDeServicio => Resultado.Exito(),
                EstadoContenedor.DadoDeBaja => RechazoDadaDeBaja(),
                _ => Aplicar(caneca.PonerFueraDeServicio),
            }), () => _contenedores.CambiarEstadoAsync(idContenedor, EstadoContenedor.FueraDeServicio, cancellationToken), cancellationToken);

        /// <summary>Baja definitiva: la caneca ya no puede volver a servicio. Repetirlo no es un error.</summary>
        public Task<Resultado> DarDeBajaAsync(int idActor, int idContenedor, CancellationToken cancellationToken = default) =>
            ModificarAsync(idActor, idContenedor, caneca => Task.FromResult(caneca.Estado == EstadoContenedor.DadoDeBaja
                ? Resultado.Exito()
                : Aplicar(caneca.DarDeBaja)),
                () => _contenedores.CambiarEstadoAsync(idContenedor, EstadoContenedor.DadoDeBaja, cancellationToken), cancellationToken);

        /// <summary>
        /// Plantilla común: exige administrador, carga la caneca y valida el cambio sobre la entidad. Si el cambio
        /// devuelve null, se ejecuta <paramref name="guardar"/>, la escritura puntual que toca solo lo cambiado;
        /// si devuelve un resultado, se responde sin escribir.
        /// </summary>
        /// <remarks>
        /// La lectura puede haber envejecido cuando se escribe: otra sesión pudo deshabilitar un material, dar de baja
        /// la caneca o desactivar su punto. El repositorio repite las reglas con la fila bloqueada y, si ya no se cumplen, lanza
        /// <see cref="CambioConcurrenteException"/>, que aquí se convierte en un rechazo.
        /// </remarks>
        private async Task<Resultado> ModificarAsync(
            int idActor,
            int idContenedor,
            Func<Contenedor, Task<Resultado?>> cambio,
            Func<Task> guardar,
            CancellationToken cancellationToken)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            if (caneca is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");
            }

            if (await cambio(caneca) is { } respuesta)
            {
                return respuesta;
            }

            try
            {
                await guardar();
            }
            catch (CambioConcurrenteException ex)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
            }

            return Resultado.Exito();
        }

        private static Resultado? Aplicar(Action accion)
        {
            accion();
            return null;
        }

        private static Resultado RechazoDadaDeBaja() =>
            Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, MensajesCatalogo.CanecaDadaDeBaja);

        private async Task<bool> TieneMaterialHabilitadoAsync(IEnumerable<int> idsMateriales, CancellationToken cancellationToken)
        {
            foreach (int idMaterial in idsMateriales)
            {
                Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
                if (material is { Habilitado: true })
                {
                    return true;
                }
            }

            return false;
        }
    }
}
