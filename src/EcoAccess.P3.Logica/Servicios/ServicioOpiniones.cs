using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Consentimiento, límites y reintentos de una valoración. Nunca concede puntos ni cambia permisos.</summary>
    public sealed class ServicioOpiniones
    {
        private readonly IRepositorioOpiniones _repositorio;
        private readonly TimeProvider _reloj;

        public ServicioOpiniones(IRepositorioUsuarios usuarios, IRepositorioOpiniones repositorio, TimeProvider reloj)
        {
            _repositorio = repositorio;
            _reloj = reloj;
            _acceso = new(usuarios);
        }

        private readonly ControlAcceso _acceso;
        private readonly ILimitadorUso _limiteInvitados = new LimitadorUsoEnMemoria(100, TimeSpan.FromDays(1));

        public async Task<Resultado> RegistrarCiudadanoAsync(int actor, int estrellas, string? comentario,
            bool consentimiento, CancellationToken ct = default)
        {
            if (await _acceso.ExigirCiudadanoHabilitadoAsync(actor, ct) is { } rechazo) return rechazo;
            return await RegistrarAsync(actor, "ciudadano:" + actor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                estrellas, comentario, consentimiento, ct);
        }

        /// <summary>La sesión opaca la aporta el adaptador de confianza, nunca un correo. Invitados: solo estrellas.</summary>
        public Task<Resultado> RegistrarInvitadoAsync(string sesion, int estrellas, bool consentimiento, CancellationToken ct = default)
        {
            if (Compat.IsNullOrWhiteSpace(sesion) || sesion.Length is < 16 or > 100)
                return Task.FromResult(Resultado.Rechazo(MotivoRechazo.DatosInvalidos, "La sesión de invitado no es válida."));
            return RegistrarAsync(null, "invitado:" + sesion, estrellas, null, consentimiento, ct);
        }

        private async Task<Resultado> RegistrarAsync(int? actor, string identidad, int estrellas, string? comentario,
            bool consentimiento, CancellationToken ct)
        {
            if (!consentimiento) return Resultado.Rechazo(MotivoRechazo.SinPermiso, "Acepta guardar tu valoración de forma voluntaria.");
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            Opinion opinion;
            try { opinion = new Opinion(actor, Compat.ToHexString(Compat.Sha256(Encoding.UTF8.GetBytes(identidad))), estrellas, comentario, ahora); }
            catch (ArgumentException error) { return MensajesValidacion.Rechazo(error); }
            if (await _repositorio.ObtenerAsync(opinion.Participante, ahora.Date, ct) is { } anterior)
                return Comparar(anterior, opinion);
            if (actor is null && !_limiteInvitados.Permitir("opiniones-invitados", ahora))
                return Resultado.Rechazo(MotivoRechazo.EstadoNoPermitido, "Se alcanzó el límite diario de valoraciones de invitados. Intenta mañana.");
            try { await _repositorio.AgregarAsync(opinion, ct); return Resultado.Exito(); }
            catch (ConflictoUnicidadException)
            {
                // Otro proceso pudo insertar tras la consulta: la restricción es la autoridad, no el estado de la GUI.
                var existente = await _repositorio.ObtenerAsync(opinion.Participante, ahora.Date, ct);
                return existente is null ? Resultado.Rechazo(MotivoRechazo.Duplicado, "La valoración ya fue enviada.") : Comparar(existente, opinion);
            }
        }

        private static Resultado Comparar(Opinion anterior, Opinion nueva) =>
            anterior.IdUsuario == nueva.IdUsuario && anterior.Estrellas == nueva.Estrellas && anterior.Comentario == nueva.Comentario
                ? Resultado.Exito()
                : Resultado.Rechazo(MotivoRechazo.Duplicado, "Ya recibimos tu valoración de hoy; puedes enviar otra mañana.");

        public async Task<Resultado<IReadOnlyList<TendenciaOpiniones>>> ConsultarTendenciasAsync(int actor,
            DateTime desdeUtc, DateTime hastaUtc, CancellationToken ct = default)
        {
            if (await _acceso.ExigirRolAsync(actor, CodigoRol.Administrador, ct) is { } rechazo)
                return Resultado<IReadOnlyList<TendenciaOpiniones>>.DesdeRechazo(rechazo);
            if (desdeUtc.Kind != DateTimeKind.Utc || hastaUtc.Kind != DateTimeKind.Utc || hastaUtc <= desdeUtc || hastaUtc - desdeUtc > TimeSpan.FromDays(366))
                return Resultado<IReadOnlyList<TendenciaOpiniones>>.Rechazo(MotivoRechazo.DatosInvalidos, "Consulta un período UTC de hasta 366 días.");
            return Resultado<IReadOnlyList<TendenciaOpiniones>>.Exito(await _repositorio.ResumirAsync(desdeUtc, hastaUtc, ct));
        }
    }
}
