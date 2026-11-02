using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Estadísticas de uso para operarios y administradores (CU-15). Solo lee: comprueba el permiso, valida
    /// el filtro y delega la agregación en el repositorio, que la hace en la base de datos para no traer filas sueltas.
    /// </summary>
    /// <remarks>
    /// Cada categoría se cuenta por separado: una interacción no es una orden, una orden no es una apertura confirmada
    /// y una apertura no es un depósito. Los kilogramos medidos y estimados nunca se suman (RN-12).
    /// </remarks>
    public sealed class ServicioEstadisticas
    {
        /// <summary>Máximo de filas por página del detalle; protege a la GUI y a la base de datos.</summary>
        public const int TamanoPaginaMaximo = 100;

        private readonly ControlAcceso _acceso;
        private readonly IRepositorioEstadisticas _estadisticas;

        public ServicioEstadisticas(IRepositorioUsuarios usuarios, IRepositorioEstadisticas estadisticas)
        {
            _acceso = new ControlAcceso(usuarios);
            _estadisticas = estadisticas ?? throw new ArgumentNullException(nameof(estadisticas));
        }

        /// <summary>Solo la actividad del ciudadano autenticado, comparada con un período anterior de igual duración.
        /// No admite un identificador de tercero ni habilita las consultas administrativas.</summary>
        public async Task<Resultado<EstadisticasPersonales>> ObtenerMiasAsync(
            int idActor, DateTime desdeUtc, DateTime hastaUtc, CancellationToken cancellationToken = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(idActor, cancellationToken) is { } rechazo)
                return Resultado<EstadisticasPersonales>.DesdeRechazo(rechazo);

            var actual = new FiltroEstadisticas(desdeUtc, hastaUtc, IdUsuario: idActor);
            string? error = ErrorDelFiltro(actual);
            if (error is not null)
                return Resultado<EstadisticasPersonales>.Rechazo(MotivoRechazo.DatosInvalidos, error);
            TimeSpan duracion = hastaUtc - desdeUtc;
            if (duracion > TimeSpan.FromDays(90) || desdeUtc.Ticks < duracion.Ticks)
                return Resultado<EstadisticasPersonales>.Rechazo(MotivoRechazo.DatosInvalidos,
                    "Consulta un período de hasta 90 días que permita comparar con el período anterior.");

            var anterior = actual with { DesdeUtc = desdeUtc - duracion, HastaUtc = desdeUtc };
            // Secuencial: no comparte una conexión en paralelo y conserva el filtro de identidad en todas las consultas.
            var resumen = await _estadisticas.ObtenerResumenAsync(actual, cancellationToken);
            var previo = await _estadisticas.ObtenerResumenAsync(anterior, cancellationToken);
            var materiales = await _estadisticas.AgruparDepositosAsync(actual, AgrupacionDepositos.Material, cancellationToken);
            var evolucion = await _estadisticas.AgruparDepositosAsync(actual, AgrupacionDepositos.Dia, cancellationToken);
            return Resultado<EstadisticasPersonales>.Exito(new(actual, anterior, resumen, previo, materiales, evolucion));
        }

        /// <summary>Conteos del período: interacciones, órdenes, eventos y depósitos, cada uno aparte.</summary>
        public async Task<Resultado<ResumenActividad>> ObtenerResumenAsync(
            int idActor,
            FiltroEstadisticas filtro,
            CancellationToken cancellationToken = default)
        {
            if (await ValidarAsync(idActor, filtro, cancellationToken) is { } rechazo)
            {
                return Resultado<ResumenActividad>.DesdeRechazo(rechazo);
            }

            return Resultado<ResumenActividad>.Exito(await _estadisticas.ObtenerResumenAsync(filtro, cancellationToken));
        }

        /// <summary>Depósitos agrupados por material, caneca, punto, día o usuario.</summary>
        public async Task<Resultado<IReadOnlyList<FilaAgrupada>>> AgruparDepositosAsync(
            int idActor,
            FiltroEstadisticas filtro,
            AgrupacionDepositos agrupacion,
            CancellationToken cancellationToken = default)
        {
            if (await ValidarAsync(idActor, filtro, cancellationToken) is { } rechazo)
            {
                return Resultado<IReadOnlyList<FilaAgrupada>>.DesdeRechazo(rechazo);
            }

            if (!Compat.IsDefined(agrupacion))
            {
                return Resultado<IReadOnlyList<FilaAgrupada>>.Rechazo(MotivoRechazo.DatosInvalidos, "La agrupación no es válida.");
            }

            return Resultado<IReadOnlyList<FilaAgrupada>>.Exito(
                await _estadisticas.AgruparDepositosAsync(filtro, agrupacion, cancellationToken));
        }

        /// <summary>Canecas con fallos, eventos en revisión u órdenes fallidas o vencidas en el período.</summary>
        public async Task<Resultado<IReadOnlyList<FilaFallosContenedor>>> ListarFallosPorContenedorAsync(
            int idActor,
            FiltroEstadisticas filtro,
            CancellationToken cancellationToken = default)
        {
            if (await ValidarAsync(idActor, filtro, cancellationToken) is { } rechazo)
            {
                return Resultado<IReadOnlyList<FilaFallosContenedor>>.DesdeRechazo(rechazo);
            }

            return Resultado<IReadOnlyList<FilaFallosContenedor>>.Exito(
                await _estadisticas.ListarFallosPorContenedorAsync(filtro, cancellationToken));
        }

        /// <summary>Detalle paginado de depósitos; la página empieza en 1.</summary>
        public async Task<Resultado<PaginaDepositos>> ListarDepositosAsync(
            int idActor,
            FiltroEstadisticas filtro,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
        {
            if (await ValidarAsync(idActor, filtro, cancellationToken) is { } rechazo)
            {
                return Resultado<PaginaDepositos>.DesdeRechazo(rechazo);
            }

            if (pagina < 1)
            {
                return Resultado<PaginaDepositos>.Rechazo(MotivoRechazo.DatosInvalidos, "La página empieza en 1.");
            }

            if (tamanoPagina is < 1 or > TamanoPaginaMaximo)
            {
                return Resultado<PaginaDepositos>.Rechazo(
                    MotivoRechazo.DatosInvalidos,
                    $"El tamaño de página debe estar entre 1 y {TamanoPaginaMaximo}.");
            }

            return Resultado<PaginaDepositos>.Exito(
                await _estadisticas.ListarDepositosAsync(filtro, pagina, tamanoPagina, cancellationToken));
        }

        /// <summary>Detalle privado paginado: el llamador no puede elegir otro titular ni acceder al informe administrativo.</summary>
        public async Task<Resultado<PaginaDepositos>> ListarMiosAsync(int actor, DateTime desdeUtc, DateTime hastaUtc,
            int pagina = 1, int tamanoPagina = 20, CancellationToken ct = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(actor, ct) is { } rechazo)
                return Resultado<PaginaDepositos>.DesdeRechazo(rechazo);
            var filtro = new FiltroEstadisticas(desdeUtc, hastaUtc, IdUsuario: actor);
            string? error = ErrorDelFiltro(filtro);
            if (error is not null || hastaUtc - desdeUtc > TimeSpan.FromDays(90) || pagina < 1 || tamanoPagina is < 1 or > TamanoPaginaMaximo)
                return Resultado<PaginaDepositos>.Rechazo(MotivoRechazo.DatosInvalidos, error ?? "Consulta hasta 90 días, una página positiva y entre 1 y 100 filas.");
            return Resultado<PaginaDepositos>.Exito(await _estadisticas.ListarDepositosAsync(filtro, pagina, tamanoPagina, ct));
        }

        // Primero el permiso y luego el filtro: quien no puede consultar no recibe pistas sobre qué filtro es válido.
        private async Task<Resultado?> ValidarAsync(int idActor, FiltroEstadisticas? filtro, CancellationToken cancellationToken)
        {
            if (await _acceso.ExigirAlgunRolAsync(idActor, cancellationToken, CodigoRol.Operario, CodigoRol.Administrador) is { } rechazo)
            {
                return rechazo;
            }

            string? error = ErrorDelFiltro(filtro);
            return error is null ? null : Resultado.Rechazo(MotivoRechazo.DatosInvalidos, error);
        }

        // Devuelve el primer problema del filtro, o null si es válido.
        private static string? ErrorDelFiltro(FiltroEstadisticas? filtro)
        {
            if (filtro is null)
            {
                return "Falta el filtro de la consulta.";
            }

            if (filtro.DesdeUtc.Kind != DateTimeKind.Utc || filtro.HastaUtc.Kind != DateTimeKind.Utc)
            {
                return "Las fechas del período deben estar en UTC.";
            }

            if (filtro.DesdeUtc >= filtro.HastaUtc)
            {
                return "La fecha inicial debe ser anterior a la final.";
            }

            if (filtro.IdContenedor is <= 0 || filtro.IdMaterial is <= 0 || filtro.IdUsuario is <= 0)
            {
                return "Los identificadores del filtro deben ser positivos.";
            }

            if (!Compat.IsDefined(filtro.Verificacion) || !Compat.IsDefined(filtro.Criterio))
            {
                return "El estado de verificación o el criterio de material no es válido.";
            }

            // El material verificado solo existe en depósitos verificados: pedir los sin verificar daría siempre cero.
            if (filtro.Criterio == CriterioMaterial.Verificado && filtro.Verificacion == EstadoVerificacion.SinVerificar)
            {
                return "Un depósito sin verificar no tiene material verificado; cambie el criterio a material declarado.";
            }

            return null;
        }
    }
}
