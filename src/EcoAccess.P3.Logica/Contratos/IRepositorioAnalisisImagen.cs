using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    public interface IRepositorioAnalisisImagen
    {
        Task GuardarAsync(AnalisisImagen analisis, CancellationToken ct = default);
        Task<AnalisisImagen?> ObtenerUltimoAsync(int interaccion, CancellationToken ct = default);
        Task ConfirmarAsync(int interaccion, int material, DateTime fechaUtc, CancellationToken ct = default);
    }
}
