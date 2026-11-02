using System;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>No admite roles ni contraseña en texto: el único rol posible es Ciudadano.</summary>
    public sealed record AltaCiudadano(string Nombre, string CorreoElectronico, DateTime FechaUtc,
        string VersionConsentimiento, HashContrasena Credencial);

    public interface IRepositorioRegistroCiudadanos
    {
        /// <summary>Confirma cuenta, rol ciudadano, consentimiento y hash juntos; un fallo revierte todo.</summary>
        Task<int> RegistrarAsync(AltaCiudadano alta, CancellationToken cancellationToken = default);
    }
}
