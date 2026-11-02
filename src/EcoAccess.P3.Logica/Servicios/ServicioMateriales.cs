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
    /// Casos de uso del catálogo de materiales: registrar, consultar, habilitar y deshabilitar.
    /// Es la fachada del área (GRASP controlador): la GUI no decide reglas, solo llama aquí y muestra el resultado.
    /// Modificar el catálogo es solo para administradores; consultarlo no exige cuenta, porque el bot lo
    /// muestra a cualquier ciudadano y no contiene datos personales.
    /// </summary>
    public sealed class ServicioMateriales
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioMateriales _materiales;
        private readonly IRepositorioContenedores _contenedores;

        public ServicioMateriales(
            IRepositorioUsuarios usuarios,
            IRepositorioMateriales materiales,
            IRepositorioContenedores contenedores)
        {
            _acceso = new ControlAcceso(usuarios);
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
        }

        /// <summary>Registra un material habilitado y devuelve su identificador.</summary>
        public async Task<Resultado<int>> RegistrarAsync(
            int idActor,
            string codigo,
            string nombre,
            CategoriaMaterial categoria,
            string? descripcion,
            CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return Resultado<int>.DesdeRechazo(rechazo);
            }

            Material material;
            try
            {
                material = new Material(codigo, nombre, categoria, descripcion);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo<int>(error);
            }

            if (await _materiales.ExisteCodigoAsync(material.Codigo, cancellationToken))
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, $"Ya existe un material con el código {material.Codigo}.");
            }

            try
            {
                return Resultado<int>.Exito(await _materiales.AgregarAsync(material, cancellationToken));
            }
            catch (ConflictoUnicidadException)
            {
                // Otro administrador lo guardó entre la consulta y la inserción.
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, $"Ya existe un material con el código {material.Codigo}.");
            }
        }

        public Task<IReadOnlyList<Material>> ListarAsync(bool soloHabilitados, CancellationToken cancellationToken = default) =>
            _materiales.ListarAsync(soloHabilitados, cancellationToken);

        public async Task<Resultado<PaginaMateriales>> ConsultarPaginaAsync(int idActor, FiltroMateriales filtro,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            cancellationToken.ThrowIfCancellationRequested();
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
                return Resultado<PaginaMateriales>.DesdeRechazo(rechazo);
            return Resultado<PaginaMateriales>.Exito(await _materiales.ConsultarPaginaAsync(filtro, cancellationToken));
        }

        public async Task<Resultado> HabilitarAsync(int idActor, int idMaterial, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
            if (material is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
            }

            if (!material.Habilitado)
            {
                try
                {
                    await _materiales.HabilitarAsync(idMaterial, cancellationToken);
                }
                catch (CambioConcurrenteException ex)
                {
                    return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
                }
            }

            return Resultado.Exito();
        }

        /// <summary>
        /// Retira el material de nuevas clasificaciones y depósitos. Se rechaza si es el único material
        /// habilitado de una caneca en servicio: esa caneca quedaría abierta al público sin nada que pueda recibir.
        /// </summary>
        public async Task<Resultado> DeshabilitarAsync(int idActor, int idMaterial, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
            {
                return rechazo;
            }

            Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
            if (material is null)
            {
                return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material.");
            }

            if (!material.Habilitado)
            {
                return Resultado.Exito();
            }

            IReadOnlyList<Contenedor> canecas = await _contenedores.ListarPorMaterialAsync(idMaterial, cancellationToken);
            if (canecas.Any(c => c.EstaEnServicio))
            {
                var habilitados = (await _materiales.ListarAsync(soloHabilitados: true, cancellationToken))
                    .Select(m => m.Id)
                    .ToHashSet();

                Contenedor? bloqueada = canecas.FirstOrDefault(c =>
                    c.EstaEnServicio && !c.MaterialesAdmitidos.Any(id => id != idMaterial && habilitados.Contains(id)));

                if (bloqueada is not null)
                {
                    return Resultado.Rechazo(
                        MotivoRechazo.EstadoNoPermitido,
                        MensajesCatalogo.UnicoMaterialDeCanecaEnServicio(bloqueada.Nombre));
                }
            }

            // La comprobación anterior da un mensaje claro en el caso normal; el repositorio la repite con bloqueos
            // por si otra sesión deshabilitó o quitó otro material de la caneca entre esta lectura y la escritura.
            try
            {
                await _materiales.DeshabilitarAsync(idMaterial, cancellationToken);
            }
            catch (CambioConcurrenteException ex)
            {
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, ex.Message);
            }

            return Resultado.Exito();
        }
    }
}
