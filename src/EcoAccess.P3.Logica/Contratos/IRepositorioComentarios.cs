using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    public enum TipoComentario { Queja = 1, Sugerencia = 2, ProblemaTecnico = 3, Elogio = 4, Comentario = 5 }

    public enum CategoriaComentario { General = 1, Reconocimiento = 2, Demora = 3, Caneca = 4, Valoracion = 5, Otro = 6 }

    public enum EstadoComentario { Nuevo = 1, EnRevision = 2, Cerrado = 3 }

    /// <param name="Clave">Evita registrar dos veces la misma actualización de Telegram.</param>
    /// <param name="Participante">Hash de quien escribe: un invitado no aporta datos personales.</param>
    /// <param name="Comentario">Texto ya saneado, o null si solo se conserva la categoría.</param>
    public sealed record ComentarioCiudadano(string Clave, int? IdUsuario, string Participante, TipoComentario Tipo, CategoriaComentario Categoria,
        string? Comentario, int? IdInteraccion, DateTime FechaUtc);

    /// <summary>Quejas, sugerencias y problemas contados en el chat (COMENTARIO_CIUDADANO).</summary>
    public interface IRepositorioComentarios
    {
        /// <returns>False si esa clave ya estaba registrada.</returns>
        Task<bool> AgregarAsync(ComentarioCiudadano comentario, CancellationToken ct = default);
    }
}
