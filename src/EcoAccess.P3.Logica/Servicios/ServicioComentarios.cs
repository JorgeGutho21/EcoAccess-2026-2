using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Guarda lo que una persona cuenta en el chat. Nunca crea una valoración, ni estrellas, ni puntos. Oculta correos y
    /// números largos, y limita cuánto puede registrar cada participante al día para que el spam no llene la tabla.
    /// </summary>
    public sealed class ServicioComentarios
    {
        public const int MaximoCaracteres = 500;
        public const int MaximoPorDia = 20;
        private static readonly TimeSpan VentanaRepetidos = TimeSpan.FromMinutes(10);
        private static readonly Regex DatosPersonales = new(@"[\w.+-]+@[\w-]+\.[\w.-]+|\d[\d\s().-]{5,}\d", RegexOptions.CultureInvariant);

        private readonly IRepositorioComentarios _repositorio;
        private readonly TimeProvider _reloj;
        private readonly ILimitadorUso _porDia = new LimitadorUsoEnMemoria(MaximoPorDia, TimeSpan.FromDays(1));
        private readonly Dictionary<string, DateTime> _recientes = new(StringComparer.Ordinal);

        public ServicioComentarios(IRepositorioComentarios repositorio, TimeProvider reloj)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public static string Sanear(string texto)
        {
            string limpio = DatosPersonales.Replace(texto.Trim(), "[dato oculto]");
            return limpio.Length <= MaximoCaracteres ? limpio : limpio.Substring(0, MaximoCaracteres);
        }

        /// <param name="idActualizacion">Identificador de la actualización de Telegram: un reintento no registra otra fila.</param>
        /// <returns>True si quedó guardado; false si era repetido o se superó el límite. Nada de eso es un error para la persona.</returns>
        public async Task<bool> RegistrarAsync(long telegramUserId, long idActualizacion, int? idUsuario, TipoComentario tipo, CategoriaComentario categoria,
            string? texto, int? idInteraccion, CancellationToken ct = default)
        {
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            string participante = Compat.ToHexStringLower(Compat.Sha256(Encoding.UTF8.GetBytes(
                idUsuario is int cuenta ? "ciudadano:" + cuenta.ToString(CultureInfo.InvariantCulture) : "invitado:telegram:" + telegramUserId.ToString(CultureInfo.InvariantCulture))));
            string? comentario = Compat.IsNullOrWhiteSpace(texto) ? null : Sanear(texto);
            string huella = participante + "|" + (int)tipo + "|" + (int)categoria + "|" + comentario;
            lock (_recientes)
            {
                foreach (string vieja in _recientes.Where(r => ahora - r.Value >= VentanaRepetidos).Select(r => r.Key).ToArray()) _recientes.Remove(vieja);
                if (_recientes.ContainsKey(huella)) return false;
                if (!_porDia.Permitir(participante, ahora)) return false;
                _recientes[huella] = ahora;
            }
            return await _repositorio.AgregarAsync(new ComentarioCiudadano("telegram:" + idActualizacion.ToString(CultureInfo.InvariantCulture), idUsuario, participante,
                tipo, categoria, comentario, idInteraccion, ahora), ct);
        }
    }
}
