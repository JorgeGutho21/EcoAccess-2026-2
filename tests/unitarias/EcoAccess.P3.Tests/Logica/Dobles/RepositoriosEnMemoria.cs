using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    // Repositorios falsos escritos a mano. Guardan copias de las entidades, como haría una base de datos:
    // si un servicio cambia una entidad y no llama al método de escritura, el cambio no queda guardado y la
    // prueba lo detecta. No reemplazan las pruebas de integración contra Oracle (pendientes).

    internal sealed class RepositorioUsuariosEnMemoria : IRepositorioUsuarios
    {
        private readonly Dictionary<int, Usuario> _usuarios = new Dictionary<int, Usuario>();
        private int _siguienteId = 100;

        public int Actualizaciones { get; private set; }
        public bool SimularConflictoAlAgregar { get; set; }

        /// <summary>Si se asigna, CambiarActivoAsync y QuitarRolAsync lanzan RN-23: simula la carrera que detecta el repositorio Oracle.</summary>
        public bool SimularUltimoAdministrador { get; set; }

        /// <summary>
        /// Se ejecuta una vez justo antes de la próxima escritura: simula otra sesión que cambia la cuenta entre la
        /// lectura del servicio y su escritura.
        /// </summary>
        public Action? AlEscribir { get; set; }

        public Usuario Agregar(int id, bool activo = true, params CodigoRol[] roles)
        {
            var usuario = new Usuario(id, $"Usuario {id}", $"usuario{id}@ejemplo.com",
                Fechas.Base, activo, roles, versionConsentimiento: null, fechaConsentimientoUtc: null);
            _usuarios[id] = Copiar(usuario);
            return usuario;
        }

        /// <summary>Ciudadano que ya aceptó el tratamiento de datos, como exige solicitar aperturas (RN-02).</summary>
        public Usuario AgregarCiudadano(int id, bool consentimiento = true, bool activo = true, params CodigoRol[] otrosRoles)
        {
            var usuario = new Usuario(id, $"Usuario {id}", $"usuario{id}@ejemplo.com", Fechas.Base, activo, new CodigoRol[] { CodigoRol.Ciudadano }.Concat(otrosRoles).ToArray(),
                versionConsentimiento: consentimiento ? "v1" : null, fechaConsentimientoUtc: consentimiento ? Fechas.Base : null);
            _usuarios[id] = Copiar(usuario);
            return usuario;
        }

        public Usuario? Leer(int id) => _usuarios.TryGetValue(id, out var u) ? Copiar(u) : null;

        public Task<Usuario?> ObtenerPorIdAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(idUsuario));

        public Task<bool> ExisteCorreoAsync(string correoElectronico, CancellationToken cancellationToken = default) =>
            Task.FromResult(_usuarios.Values.Any(u => u.CorreoElectronico == correoElectronico));

        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>(_usuarios.Values
                .OrderBy(u => u.Nombre, StringComparer.Ordinal).Select(Copiar).ToList());

        public int ConsultasPagina { get; private set; }
        public Task<PaginaUsuarios> ConsultarPaginaAsync(FiltroUsuarios filtro, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConsultasPagina++;
            var consulta = _usuarios.Values.Where(u =>
                (u.Nombre.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase) || u.CorreoElectronico.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase))
                && (filtro.Activo is null || u.Activo == filtro.Activo)
                && (filtro.Rol is null || u.TieneRol(filtro.Rol.Value)))
                .OrderBy(u => u.Nombre, StringComparer.Ordinal).ThenBy(u => u.Id).ToArray();
            var filas = consulta.Skip((int)Math.Min(int.MaxValue, filtro.Desplazamiento)).Take(filtro.TamanoPagina).Select(Copiar).ToArray();
            return Task.FromResult(new PaginaUsuarios(filas, filtro.Pagina, filtro.TamanoPagina,
                filtro.Desplazamiento + filas.Length < consulta.Length));
        }

        public Task<int> AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default)
        {
            if (SimularConflictoAlAgregar)
            {
                throw new ConflictoUnicidadException("uq_usuario_correo");
            }

            int id = _siguienteId++;
            _usuarios[id] = new Usuario(id, usuario.Nombre, usuario.CorreoElectronico, usuario.FechaRegistroUtc, usuario.Activo,
                usuario.Roles, usuario.VersionConsentimiento, usuario.FechaConsentimientoUtc);
            return Task.FromResult(id);
        }

        public Task CambiarActivoAsync(int idUsuario, bool activo, CancellationToken cancellationToken = default)
        {
            Usuario guardado = AntesDeEscribir(idUsuario);
            bool perderiaAdministrador = guardado.Activo && !activo && guardado.TieneRol(CodigoRol.Administrador);
            if (SimularUltimoAdministrador || (perderiaAdministrador && AdministradoresActivos() == 1))
            {
                throw new UltimoAdministradorException();
            }

            if (activo) { guardado.Activar(); } else { guardado.Desactivar(); }
            Actualizaciones++;
            return Task.CompletedTask;
        }

        public Task AgregarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default)
        {
            Usuario guardado = AntesDeEscribir(idUsuario);
            if (!guardado.TieneRol(rol))
            {
                guardado.AsignarRol(rol);
                Actualizaciones++;
            }

            return Task.CompletedTask;
        }

        public Task QuitarRolAsync(int idUsuario, CodigoRol rol, CancellationToken cancellationToken = default)
        {
            Usuario guardado = AntesDeEscribir(idUsuario);
            if (!guardado.TieneRol(rol))
            {
                return Task.CompletedTask;
            }

            if (guardado.Roles.Count == 1)
            {
                throw new UltimoRolException();
            }

            bool perderiaAdministrador = rol == CodigoRol.Administrador && guardado.Activo;
            if (SimularUltimoAdministrador || (perderiaAdministrador && AdministradoresActivos() == 1))
            {
                throw new UltimoAdministradorException();
            }

            guardado.QuitarRol(rol);
            Actualizaciones++;
            return Task.CompletedTask;
        }

        public Task<bool> RegistrarConsentimientoAsync(int idUsuario, string version, DateTime fechaUtc,
            CancellationToken cancellationToken = default)
        {
            AlEscribir?.Invoke();
            AlEscribir = null;
            if (!_usuarios.TryGetValue(idUsuario, out Usuario? guardado) || !guardado.Activo)
            {
                return Task.FromResult(false);
            }

            guardado.RegistrarConsentimiento(version, fechaUtc);
            Actualizaciones++;
            return Task.FromResult(true);
        }

        public Task RevocarConsentimientoAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            AlEscribir?.Invoke();
            AlEscribir = null;
            if (_usuarios.TryGetValue(idUsuario, out Usuario? guardado))
            {
                guardado.RevocarConsentimiento();
                Actualizaciones++;
            }

            return Task.CompletedTask;
        }

        /// <summary>Cambia directamente la cuenta guardada, como haría otra sesión, sin contar como actualización.</summary>
        public void CambiarGuardado(int id, Action<Usuario> cambio) => cambio(_usuarios[id]);

        private Usuario AntesDeEscribir(int idUsuario)
        {
            AlEscribir?.Invoke();
            AlEscribir = null;
            return _usuarios.TryGetValue(idUsuario, out Usuario? guardado)
                ? guardado
                : throw new InvalidOperationException($"No existe cuenta con id {idUsuario}.");
        }

        private int AdministradoresActivos() =>
            _usuarios.Values.Count(u => u.Activo && u.TieneRol(CodigoRol.Administrador));

        private static Usuario Copiar(Usuario u) =>
            new(u.Id, u.Nombre, u.CorreoElectronico, u.FechaRegistroUtc, u.Activo, u.Roles,
                u.VersionConsentimiento, u.FechaConsentimientoUtc);
    }

    internal sealed class RepositorioMaterialesEnMemoria : IRepositorioMateriales
    {
        private readonly Dictionary<int, Material> _materiales = new Dictionary<int, Material>();
        private int _siguienteId = 1;

        public int Actualizaciones { get; private set; }

        public int ConsultasPagina { get; private set; }
        public Task<PaginaMateriales> ConsultarPaginaAsync(FiltroMateriales filtro, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ConsultasPagina++;
            bool Coincide(Material material)
            {
                if (!filtro.SoloPublicos) return material.Codigo.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase)
                    || material.Nombre.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase);
                string normalizado = string.Concat((material.Codigo + " " + material.Nombre + " " + material.Sinonimos)
                    .Normalize(System.Text.NormalizationForm.FormD)
                    .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)).ToLowerInvariant();
                return filtro.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(p => normalizado.Contains(p, StringComparison.Ordinal));
            }
            var consulta = _materiales.Values.Where(m => Coincide(m)
                && (filtro.Habilitado is null || m.Habilitado == filtro.Habilitado)
                && (filtro.Categoria is null || m.Categoria == filtro.Categoria)
                && (!filtro.SoloPublicos || !FiltroMateriales.EsDatoDePrueba(m)))
                .OrderBy(m => m.Codigo, StringComparer.Ordinal).ThenBy(m => m.Id).ToArray();
            var filas = consulta.Skip((int)Math.Min(int.MaxValue, filtro.Desplazamiento)).Take(filtro.TamanoPagina).Select(Copiar).ToArray();
            return Task.FromResult(new PaginaMateriales(filas, filtro.Pagina, filtro.TamanoPagina,
                filtro.Desplazamiento + filas.Length < consulta.Length));
        }

        /// <summary>Si se asigna, AgregarAsync lanza el conflicto: simula una carrera con otro usuario.</summary>
        public bool SimularConflictoAlAgregar { get; set; }

        public Material Sembrar(string codigo, bool habilitado = true) =>
            Sembrar(codigo, $"Material {codigo}", CategoriaMaterial.Aprovechable, habilitado);

        public Material Sembrar(string codigo, string nombre, CategoriaMaterial categoria, bool habilitado = true)
        {
            var material = new Material(_siguienteId++, codigo, nombre, categoria, null, habilitado);
            _materiales[material.Id] = Copiar(material);
            return material;
        }

        public Material? Leer(int id) => _materiales.TryGetValue(id, out var m) ? Copiar(m) : null;

        public Task<Material?> ObtenerPorIdAsync(int idMaterial, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(idMaterial));

        public Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default) =>
            Task.FromResult(_materiales.Values.Any(m => m.Codigo == codigo));

        public Task<IReadOnlyList<Material>> ListarAsync(bool soloHabilitados, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Material>>(_materiales.Values
                .Where(m => !soloHabilitados || m.Habilitado)
                .OrderBy(m => m.Codigo, StringComparer.Ordinal)
                .Select(Copiar)
                .ToList());

        public Task<int> AgregarAsync(Material material, CancellationToken cancellationToken = default)
        {
            if (SimularConflictoAlAgregar)
            {
                throw new ConflictoUnicidadException("uq_material_codigo");
            }

            var guardado = new Material(_siguienteId++, material.Codigo, material.Nombre, material.Categoria, material.Descripcion, material.Habilitado,
                material.Sinonimos, material.Precauciones, material.RutaSeparacion);
            _materiales[guardado.Id] = guardado;
            return Task.FromResult(guardado.Id);
        }

        /// <summary>Canecas con las que se comprueba la regla de DeshabilitarAsync, como hace Oracle dentro de su transacción.</summary>
        public RepositorioContenedoresEnMemoria? Contenedores { get; set; }

        /// <summary>Se ejecuta una vez justo antes de la próxima escritura: simula otra sesión entre la lectura y la escritura.</summary>
        public Action? AlEscribir { get; set; }

        internal bool EstaHabilitado(int idMaterial) => _materiales.TryGetValue(idMaterial, out Material? m) && m.Habilitado;

        internal string NombreDe(int idMaterial) => _materiales[idMaterial].Nombre;

        public Task HabilitarAsync(int idMaterial, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            Guardado(idMaterial).Habilitar();
            Actualizaciones++;
            return Task.CompletedTask;
        }

        public Task DeshabilitarAsync(int idMaterial, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            Material guardado = Guardado(idMaterial);
            if (Contenedores?.UnicaEnServicioCon(idMaterial, this) is { } bloqueada)
            {
                throw new CambioConcurrenteException(MensajesCatalogo.UnicoMaterialDeCanecaEnServicio(bloqueada.Nombre));
            }

            guardado.Deshabilitar();
            Actualizaciones++;
            return Task.CompletedTask;
        }

        private void AntesDeEscribir()
        {
            Action? accion = AlEscribir;
            AlEscribir = null;
            accion?.Invoke();
        }

        private Material Guardado(int idMaterial) => _materiales.TryGetValue(idMaterial, out Material? m)
            ? m
            : throw new InvalidOperationException($"No existe material con id {idMaterial}.");

        private static Material Copiar(Material m) =>
            new(m.Id, m.Codigo, m.Nombre, m.Categoria, m.Descripcion, m.Habilitado, m.Sinonimos, m.Precauciones, m.RutaSeparacion);
    }

    internal sealed class RepositorioPuntosEnMemoria : IRepositorioPuntosInstalacion
    {
        private readonly Dictionary<int, PuntoInstalacion> _puntos = new Dictionary<int, PuntoInstalacion>();
        private int _siguienteId = 1;

        public int Actualizaciones { get; private set; }
        public bool SimularConflictoAlAgregar { get; set; }

        public PuntoInstalacion Sembrar(string nombre, bool activo = true)
        {
            var punto = new PuntoInstalacion(_siguienteId++, nombre, null, activo);
            _puntos[punto.Id] = Copiar(punto);
            return punto;
        }

        public PuntoInstalacion? Leer(int id) => _puntos.TryGetValue(id, out var p) ? Copiar(p) : null;

        public Task<PuntoInstalacion?> ObtenerPorIdAsync(int idPunto, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(idPunto));

        public Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default) =>
            Task.FromResult(_puntos.Values.Any(p => p.Nombre == nombre));

        public Task<IReadOnlyList<PuntoInstalacion>> ListarAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PuntoInstalacion>>(_puntos.Values
                .OrderBy(p => p.Nombre, StringComparer.Ordinal).Select(Copiar).ToList());

        public Task<int> AgregarAsync(PuntoInstalacion punto, CancellationToken cancellationToken = default)
        {
            if (SimularConflictoAlAgregar)
            {
                throw new ConflictoUnicidadException("uq_punto_instalacion_nom");
            }

            var guardado = new PuntoInstalacion(_siguienteId++, punto.Nombre, punto.ReferenciaUbicacion, punto.Activo);
            _puntos[guardado.Id] = guardado;
            return Task.FromResult(guardado.Id);
        }

        /// <summary>Canecas con las que se repite la regla al desactivar, como hace Oracle con el punto bloqueado.</summary>
        public RepositorioContenedoresEnMemoria? Contenedores { get; set; }

        /// <summary>Se ejecuta una vez justo antes de la próxima escritura: simula otra sesión entre la lectura y la escritura.</summary>
        public Action? AlEscribir { get; set; }

        internal bool EstaActivo(int idPunto) => _puntos.TryGetValue(idPunto, out PuntoInstalacion? p) && p.Activo;

        public Task CambiarActivoAsync(int idPunto, bool activo, CancellationToken cancellationToken = default)
        {
            Action? accion = AlEscribir;
            AlEscribir = null;
            accion?.Invoke();

            if (!_puntos.TryGetValue(idPunto, out PuntoInstalacion? guardado))
            {
                throw new InvalidOperationException($"No existe punto de instalación con id {idPunto}.");
            }

            if (!activo && Contenedores is not null && Contenedores.HayEnServicioEn(idPunto))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.PuntoConCanecasEnServicio);
            }

            _puntos[idPunto] = new PuntoInstalacion(guardado.Id, guardado.Nombre, guardado.ReferenciaUbicacion, activo);
            Actualizaciones++;
            return Task.CompletedTask;
        }

        private static PuntoInstalacion Copiar(PuntoInstalacion p) => new(p.Id, p.Nombre, p.ReferenciaUbicacion, p.Activo);
    }

    internal sealed class RepositorioContenedoresEnMemoria : IRepositorioContenedores
    {
        private readonly Dictionary<int, Contenedor> _canecas = new Dictionary<int, Contenedor>();
        private int _siguienteId = 1;

        public int Actualizaciones { get; private set; }

        /// <summary>Cuántas veces seguidas AgregarAsync responde con un conflicto de QR antes de aceptar.</summary>
        public int ConflictosQrPendientes { get; set; }

        public int IntentosAgregar { get; private set; }

        public Contenedor Sembrar(int idPunto, EstadoContenedor estado, params int[] materiales)
        {
            int id = _siguienteId++;
            var caneca = new Contenedor(id, idPunto, $"Caneca {id}", $"QR-PRUEBA-{id:D10}", Fechas.Base, estado, materiales);
            _canecas[id] = Copiar(caneca);
            return caneca;
        }

        public Contenedor SembrarPiloto(EstadoContenedor estado, params int[] materiales)
        {
            int id = _siguienteId++;
            var caneca = new Contenedor(id, 1, EcoAccess.P3.Logica.Servicios.ServicioCatalogoAmbiental.NombreCanecaPiloto, $"QR-PILOTO-{id:D10}", Fechas.Base, estado, materiales);
            _canecas[id] = Copiar(caneca);
            return caneca;
        }

        public Contenedor? Leer(int id) => _canecas.TryGetValue(id, out var c) ? Copiar(c) : null;

        public Task<Contenedor?> ObtenerPorIdAsync(int idContenedor, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(idContenedor));

        public Task<Contenedor?> ObtenerPorIdentificadorQrAsync(string identificadorQr, CancellationToken cancellationToken = default) =>
            Task.FromResult<Contenedor?>(_canecas.Values.Where(c => c.IdentificadorQr == identificadorQr).Select(Copiar).FirstOrDefault());

        public Task<IReadOnlyList<Contenedor>> ListarAsync(CancellationToken cancellationToken = default) =>
            Listar(_ => true);

        public Task<IReadOnlyList<Contenedor>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default) =>
            Listar(c => c.IdPuntoInstalacion == idPunto);

        public Task<IReadOnlyList<Contenedor>> ListarPorMaterialAsync(int idMaterial, CancellationToken cancellationToken = default) =>
            Listar(c => c.AdmiteMaterial(idMaterial));

        public Task<int> AgregarAsync(Contenedor contenedor, CancellationToken cancellationToken = default)
        {
            IntentosAgregar++;
            if (ConflictosQrPendientes > 0)
            {
                ConflictosQrPendientes--;
                throw new ConflictoUnicidadException("uq_contenedor_qr");
            }

            int id = _siguienteId++;
            _canecas[id] = new Contenedor(id, contenedor.IdPuntoInstalacion, contenedor.Nombre, contenedor.IdentificadorQr,
                contenedor.FechaAltaUtc, contenedor.Estado, contenedor.MaterialesAdmitidos);
            return Task.FromResult(id);
        }

        /// <summary>Materiales con los que se comprueban las reglas de las escrituras puntuales, como hace Oracle.</summary>
        public RepositorioMaterialesEnMemoria? Materiales { get; set; }

        /// <summary>Puntos con los que se comprueba que el de la caneca siga activo al ponerla en servicio. Sin enlazar, no se comprueba.</summary>
        public RepositorioPuntosEnMemoria? Puntos { get; set; }

        /// <summary>Se ejecuta una vez justo antes de la próxima escritura: simula otra sesión entre la lectura y la escritura.</summary>
        public Action? AlEscribir { get; set; }

        internal bool HayEnServicioEn(int idPunto) =>
            _canecas.Values.Any(c => c.IdPuntoInstalacion == idPunto && c.EstaEnServicio);

        public Task AdmitirMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            Contenedor guardada = Guardada(idContenedor);
            ExigirNoDadaDeBaja(guardada);
            if (Materiales is not null && !Materiales.EstaHabilitado(idMaterial))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.MaterialDeshabilitado(Materiales.NombreDe(idMaterial)));
            }

            if (!guardada.AdmiteMaterial(idMaterial))
            {
                Reemplazar(guardada, guardada.Estado, guardada.MaterialesAdmitidos.Append(idMaterial));
            }

            return Task.CompletedTask;
        }

        public Task QuitarMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            Contenedor guardada = Guardada(idContenedor);
            ExigirNoDadaDeBaja(guardada);
            var restantes = guardada.MaterialesAdmitidos.Where(id => id != idMaterial).ToList();
            if (guardada.EstaEnServicio && !restantes.Any(EstaHabilitado))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.CanecaQuedariaSinMateriales);
            }

            Reemplazar(guardada, guardada.Estado, restantes);
            return Task.CompletedTask;
        }

        public Task CambiarEstadoAsync(int idContenedor, EstadoContenedor estado, CancellationToken cancellationToken = default)
        {
            AntesDeEscribir();
            Contenedor guardada = Guardada(idContenedor);
            if (guardada.Estado == EstadoContenedor.DadoDeBaja && estado != EstadoContenedor.DadoDeBaja)
            {
                throw new CambioConcurrenteException(MensajesCatalogo.CanecaDadaDeBaja);
            }

            if (estado == EstadoContenedor.EnServicio && Puntos is not null && !Puntos.EstaActivo(guardada.IdPuntoInstalacion))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.PuntoDeCanecaInactivo);
            }

            if (estado == EstadoContenedor.EnServicio && !guardada.MaterialesAdmitidos.Any(EstaHabilitado))
            {
                throw new CambioConcurrenteException(MensajesCatalogo.CanecaSinMaterialParaServicio);
            }

            Reemplazar(guardada, estado, guardada.MaterialesAdmitidos);
            return Task.CompletedTask;
        }

        /// <summary>Primera caneca en servicio cuyo único material habilitado es <paramref name="idMaterial"/>.</summary>
        internal Contenedor? UnicaEnServicioCon(int idMaterial, RepositorioMaterialesEnMemoria materiales) =>
            _canecas.Values.OrderBy(c => c.Id).FirstOrDefault(c => c.EstaEnServicio && c.AdmiteMaterial(idMaterial)
                && !c.MaterialesAdmitidos.Any(id => id != idMaterial && materiales.EstaHabilitado(id)));

        // Sin repositorio de materiales enlazado, todos cuentan como habilitados.
        private bool EstaHabilitado(int idMaterial) => Materiales?.EstaHabilitado(idMaterial) ?? true;

        private void AntesDeEscribir()
        {
            Action? accion = AlEscribir;
            AlEscribir = null;
            accion?.Invoke();
        }

        private Contenedor Guardada(int idContenedor) => _canecas.TryGetValue(idContenedor, out Contenedor? c)
            ? c
            : throw new InvalidOperationException($"No existe caneca con id {idContenedor}.");

        private static void ExigirNoDadaDeBaja(Contenedor caneca)
        {
            if (caneca.Estado == EstadoContenedor.DadoDeBaja)
            {
                throw new CambioConcurrenteException(MensajesCatalogo.CanecaDadaDeBaja);
            }
        }

        private void Reemplazar(Contenedor c, EstadoContenedor estado, IEnumerable<int> materiales)
        {
            _canecas[c.Id] = new Contenedor(c.Id, c.IdPuntoInstalacion, c.Nombre, c.IdentificadorQr, c.FechaAltaUtc, estado, materiales.ToList());
            Actualizaciones++;
        }

        private Task<IReadOnlyList<Contenedor>> Listar(Func<Contenedor, bool> filtro) =>
            Task.FromResult<IReadOnlyList<Contenedor>>(_canecas.Values
                .Where(filtro).OrderBy(c => c.Nombre, StringComparer.Ordinal).Select(Copiar).ToList());

        private static Contenedor Copiar(Contenedor c) =>
            new(c.Id, c.IdPuntoInstalacion, c.Nombre, c.IdentificadorQr, c.FechaAltaUtc, c.Estado, c.MaterialesAdmitidos);
    }

    internal sealed class GeneradorQrSecuencial : IGeneradorIdentificadorQr
    {
        private int _contador;

        public List<string> Generados { get; } = new List<string>();

        public string Generar()
        {
            string qr = $"QR-GENERADO-{++_contador:D6}";
            Generados.Add(qr);
            return qr;
        }
    }

    internal sealed class RelojFijo : TimeProvider
    {
        private readonly DateTimeOffset _ahora;

        public RelojFijo(DateTimeOffset ahora)
        {
            _ahora = ahora;
        }

        public override DateTimeOffset GetUtcNow() => _ahora;
    }

    internal static class Fechas
    {
        public static readonly DateTime Base = new(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    }
}
