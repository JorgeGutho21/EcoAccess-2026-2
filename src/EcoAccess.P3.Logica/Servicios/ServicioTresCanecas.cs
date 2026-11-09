using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Caneca elegida para un material: la orden de apertura se crea para ESTE identificador, nunca por color.</summary>
    public sealed record DestinoCaneca(int IdContenedor, string Nombre, ColorCaneca Color, string? IdentificadorDispositivo)
    {
        /// <summary>Sin dispositivo la apertura solo puede simularse; una apertura física exige dispositivo vinculado.</summary>
        public bool PuedeAbrirFisicamente => IdentificadorDispositivo is not null;
    }

    /// <summary>
    /// Estación con tres canecas (blanca, verde y negra): configura cada caneca y decide a cuál va un material.
    /// </summary>
    /// <remarks>
    /// Reglas (las pregunta el profesor):
    /// 1. La caneca se identifica por su <c>id_contenedor</c> y su configuración, no por el color: si una estación tuviera
    ///    dos canecas del mismo color, el destino sería ambiguo y se rechaza.
    /// 2. El material confirmado se busca en el catálogo de Oracle; su categoría decide el color.
    /// 3. Los residuos peligrosos no tienen caneca: se orienta al punto posconsumo (nunca a la negra por defecto).
    /// 4. La caneca debe estar en servicio y admitir explícitamente ese material.
    /// 5. Solo un administrador configura o renombra canecas.
    /// La apertura física la protege además el firmware: cada ESP32 rechaza órdenes de otra caneca.
    /// </remarks>
    public sealed class ServicioTresCanecas
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioContenedores _contenedores;
        private readonly IRepositorioMateriales _materiales;
        private readonly IRepositorioConfiguracionCanecas _configuraciones;
        private readonly TimeProvider _reloj;

        public ServicioTresCanecas(IRepositorioUsuarios usuarios, IRepositorioContenedores contenedores,
            IRepositorioMateriales materiales, IRepositorioConfiguracionCanecas configuraciones, TimeProvider reloj)
        {
            _acceso = new ControlAcceso(usuarios);
            _contenedores = contenedores ?? throw new ArgumentNullException(nameof(contenedores));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _configuraciones = configuraciones ?? throw new ArgumentNullException(nameof(configuraciones));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        /// <summary>Texto para la persona: «blanca», «verde» o «negra».</summary>
        public static string Texto(ColorCaneca color) => color switch
        {
            ColorCaneca.Blanca => "blanca",
            ColorCaneca.Verde => "verde",
            _ => "negra",
        };

        /// <summary>
        /// Asigna categoría (color), dispositivo y ubicación a una caneca. Rechaza si ya admite materiales de otra
        /// categoría o si otra caneca de la misma estación ya tiene ese color.
        /// </summary>
        public async Task<Resultado> ConfigurarAsync(int idActor, int idContenedor, CategoriaMaterial categoria,
            string? identificadorDispositivo, string? ubicacion, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo) return rechazo;

            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            if (caneca is null) return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");
            if (caneca.Estado == EstadoContenedor.DadoDeBaja)
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "La caneca está dada de baja.");

            ConfiguracionCaneca nueva;
            try
            {
                nueva = new ConfiguracionCaneca(idContenedor, categoria, identificadorDispositivo, ubicacion, _reloj.GetUtcNow().UtcDateTime);
            }
            catch (ArgumentException error)
            {
                return MensajesValidacion.Rechazo(error);
            }

            // Una caneca blanca no puede seguir admitiendo, por ejemplo, restos de comida.
            var incompatibles = new List<string>();
            foreach (int idMaterial in caneca.MaterialesAdmitidos)
            {
                Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
                if (material is not null && material.Categoria != categoria) incompatibles.Add(material.Nombre);
            }

            if (incompatibles.Count > 0)
                return Resultado.Rechazo(MotivoRechazo.DatosInvalidos,
                    $"La caneca {Texto(nueva.Color)} no puede admitir: {string.Join(", ", incompatibles)}. Quítalos primero.");

            IReadOnlyList<ConfiguracionCaneca> deLaEstacion = await _configuraciones.ListarPorPuntoAsync(caneca.IdPuntoInstalacion, cancellationToken);
            if (deLaEstacion.Any(c => c.IdContenedor != idContenedor && c.Categoria == categoria))
                return Resultado.Rechazo(MotivoRechazo.Duplicado, $"Esta estación ya tiene una caneca {Texto(nueva.Color)}.");

            try
            {
                await _configuraciones.GuardarAsync(nueva, cancellationToken);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado.Rechazo(MotivoRechazo.Duplicado, "Ese dispositivo ya está vinculado a otra caneca.");
            }

            return Resultado.Exito();
        }

        /// <summary>Cambia el nombre visible de una caneca, con las mismas reglas que al registrarla.</summary>
        public async Task<Resultado> RenombrarAsync(int idActor, int idContenedor, string nombre, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo) return rechazo;
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(idContenedor, cancellationToken);
            if (caneca is null) return Resultado.Rechazo(MotivoRechazo.NoEncontrado, "No existe esa caneca.");

            string limpio = (nombre ?? "").Trim();
            if (limpio.Length < Contenedor.LongitudMinimaNombre || limpio.Length > Contenedor.LongitudMaximaNombre)
                return Resultado.Rechazo(MotivoRechazo.DatosInvalidos,
                    $"El nombre debe tener entre {Contenedor.LongitudMinimaNombre} y {Contenedor.LongitudMaximaNombre} caracteres.");

            await _configuraciones.RenombrarAsync(idContenedor, limpio, cancellationToken);
            return Resultado.Exito();
        }

        /// <summary>
        /// Decide la caneca de una estación para un material confirmado. Devuelve el identificador del contenedor
        /// (para crear la orden de apertura) o un rechazo con la explicación para el ciudadano.
        /// </summary>
        public async Task<Resultado<DestinoCaneca>> ResolverDestinoAsync(int idPunto, int idMaterial, CancellationToken cancellationToken = default)
        {
            Material? material = await _materiales.ObtenerPorIdAsync(idMaterial, cancellationToken);
            if (material is null) return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.NoEncontrado, "No existe ese material en el catálogo.");
            if (!material.Habilitado) return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.EstadoNoPermitido, $"{material.Nombre} no está habilitado.");

            ColorCaneca? color = ConfiguracionCaneca.ColorPara(material.Categoria);
            if (color is null)
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    $"{material.Nombre} es un residuo peligroso: no va en ninguna caneca. Llévalo a un punto posconsumo autorizado.");

            IReadOnlyList<ConfiguracionCaneca> configuraciones = await _configuraciones.ListarPorPuntoAsync(idPunto, cancellationToken);
            List<ConfiguracionCaneca> candidatas = configuraciones.Where(c => c.Categoria == material.Categoria).ToList();
            string nombreColor = Texto(color.Value);
            if (candidatas.Count == 0)
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.NoEncontrado, $"Esta estación no tiene caneca {nombreColor} configurada.");
            if (candidatas.Count > 1)
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    $"La estación tiene más de una caneca {nombreColor}; un administrador debe corregir la configuración antes de abrir.");

            ConfiguracionCaneca elegida = candidatas[0];
            Contenedor? caneca = await _contenedores.ObtenerPorIdAsync(elegida.IdContenedor, cancellationToken);
            if (caneca is null || caneca.IdPuntoInstalacion != idPunto)
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.NoEncontrado, $"La caneca {nombreColor} no pertenece a esta estación.");
            if (!caneca.EstaEnServicio)
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.EstadoNoPermitido, $"La caneca {nombreColor} está fuera de servicio.");
            if (!caneca.AdmiteMaterial(material.Id))
                return Resultado<DestinoCaneca>.Rechazo(MotivoRechazo.EstadoNoPermitido,
                    $"La caneca {nombreColor} de esta estación no recibe {material.Nombre}.");

            return Resultado<DestinoCaneca>.Exito(new DestinoCaneca(caneca.Id, caneca.Nombre, color.Value, elegida.IdentificadorDispositivo));
        }
    }
}
