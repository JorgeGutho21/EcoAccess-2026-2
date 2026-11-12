using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Registro público mínimo, separado de la administración de usuarios y roles.</summary>
    public sealed class ServicioRegistroCiudadanos
    {
        public const string VersionConsentimiento = "escritorio-2026-10";
        private readonly IRepositorioRegistroCiudadanos _repositorio;
        private readonly IHasherContrasenas _hasher;
        private readonly TimeProvider _reloj;

        public ServicioRegistroCiudadanos(IRepositorioRegistroCiudadanos repositorio, IHasherContrasenas hasher, TimeProvider reloj)
        {
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _hasher = hasher ?? throw new ArgumentNullException(nameof(hasher));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public async Task<Resultado<int>> RegistrarAsync(string nombre, string correo, string? contrasena,
            string? confirmacion, bool aceptaTratamiento, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (!aceptaTratamiento)
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, "Debes aceptar el tratamiento de nombre y correo para crear tu cuenta.");
            // Política propia de ciudadanos (6 a 20); la del personal sigue en CredencialAcceso (12 a 128).
            if (PoliticaContrasenaCiudadano.Validar(contrasena) is { } problema)
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, problema);
            if (!string.Equals(contrasena, confirmacion, StringComparison.Ordinal))
                return Resultado<int>.Rechazo(MotivoRechazo.DatosInvalidos, "Las contraseñas no coinciden.");

            Usuario ciudadano;
            DateTime ahora = _reloj.GetUtcNow().UtcDateTime;
            try
            {
                ciudadano = new Usuario(nombre, correo, CodigoRol.Ciudadano, ahora);
                ciudadano.RegistrarConsentimiento(VersionConsentimiento, ahora);
            }
            catch (ArgumentException error) { return MensajesValidacion.Rechazo<int>(error); }

            // La GUI nunca recibe ni almacena este hash; Datos nunca recibe el texto de la contraseña.
            HashContrasena hash = _hasher.Calcular(contrasena);
            ct.ThrowIfCancellationRequested();
            try
            {
                int id = await _repositorio.RegistrarAsync(new(ciudadano.Nombre, ciudadano.CorreoElectronico,
                    ahora, VersionConsentimiento, hash), ct);
                return Resultado<int>.Exito(id);
            }
            catch (ConflictoUnicidadException)
            {
                return Resultado<int>.Rechazo(MotivoRechazo.Duplicado, "No se pudo crear la cuenta con ese correo. Si ya tienes una cuenta, inicia sesión.");
            }
        }
    }
}
