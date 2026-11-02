using System;
using System.Linq;

namespace EcoAccess.P3.Entidades
{

    /// <summary>Valoración voluntaria. La clave del participante es seudónima, no su correo ni el id de Telegram.</summary>
    public sealed record Opinion
    {
        public int? IdUsuario { get; }
        public string Participante { get; }
        public int Estrellas { get; }
        public string? Comentario { get; }
        public DateTime FechaUtc { get; }

        public Opinion(int? idUsuario, string participante, int estrellas, string? comentario, DateTime fechaUtc)
        {
            if (idUsuario is <= 0) throw new ArgumentOutOfRangeException(nameof(idUsuario));
            if (participante is null || participante.Length != 64 || !participante.All(Compat.IsAsciiHexDigit))
                throw new ArgumentException("El identificador de participación no es válido.", nameof(participante));
            if (estrellas is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(estrellas), "Selecciona entre 1 y 5 estrellas.");
            Comentario = Validacion.TextoOpcional(comentario, nameof(comentario), 500);
            if (Comentario?.Any(char.IsControl) == true)
                throw new ArgumentException("Escribe el comentario en una sola línea, sin caracteres de control.", nameof(comentario));
            if (idUsuario is null && Comentario is not null)
                throw new ArgumentException("Como invitado puedes valorar sin escribir comentarios.", nameof(comentario));
            IdUsuario = idUsuario;
            Participante = participante.ToLowerInvariant();
            Estrellas = estrellas;
            FechaUtc = Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));
        }
    }
}
