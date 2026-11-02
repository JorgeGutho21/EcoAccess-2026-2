using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Cuenta de una persona en EcoAccess. Protege sus invariantes (RN-01): nombre válido,
    /// correo normalizado y al menos un rol. La unicidad del correo no la puede garantizar un objeto
    /// aislado: la aseguran la lógica y la restricción UNIQUE de la base de datos. No guarda contraseñas
    /// ni decide permisos según quién la modifica: eso lo hace la capa lógica (RN-15).
    /// </summary>
    public class Usuario
    {
        public const int LongitudMinimaNombre = 2;
        public const int LongitudMaximaNombre = 100;
        public const int LongitudMaximaCorreo = 254;
        public const int LongitudMaximaParteLocalCorreo = 64;
        public const int LongitudMaximaVersionConsentimiento = 20;

        // Comillas, corchetes, paréntesis y similares son válidos en el estándar de correo, pero solo
        // aparecen en direcciones raras que el registro no podría verificar de forma confiable.
        private static readonly char[] CaracteresNoAdmitidosEnCorreo = { '"', '[', ']', '(', ')', '<', '>', ',', ';', ':', '\\' };

        private readonly List<CodigoRol> _roles = new();

        /// <summary>0 mientras el usuario no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public string Nombre { get; }
        public string CorreoElectronico { get; }
        public DateTime FechaRegistroUtc { get; }
        public bool Activo { get; private set; }
        public string? VersionConsentimiento { get; private set; }
        public DateTime? FechaConsentimientoUtc { get; private set; }

        /// <summary>Vista de solo lectura de los roles: quien la recibe no puede agregar ni quitar.</summary>
        public IReadOnlyCollection<CodigoRol> Roles { get; }
        public bool EsNuevo => Id == 0;
        public bool TieneConsentimiento => VersionConsentimiento is not null;

        /// <summary>Registra una cuenta nueva, activa y con un primer rol.</summary>
        public Usuario(string nombre, string correoElectronico, CodigoRol rolInicial, DateTime fechaRegistroUtc)
            : this(0, nombre, correoElectronico, fechaRegistroUtc, activo: true)
        {
            AsignarRol(rolInicial);
        }

        /// <summary>
        /// Reconstruye una cuenta ya guardada. Lo usa la capa de datos y aplica las mismas
        /// invariantes que el registro, para que una fila inconsistente no se convierta en un objeto válido.
        /// Como los datos vienen de afuera, todos sus errores se informan como ArgumentException.
        /// </summary>
        public Usuario(
            int id,
            string nombre,
            string correoElectronico,
            DateTime fechaRegistroUtc,
            bool activo,
            IEnumerable<CodigoRol> roles,
            string? versionConsentimiento,
            DateTime? fechaConsentimientoUtc)
            : this(id, nombre, correoElectronico, fechaRegistroUtc, activo)
        {
            Validacion.IdPositivo(id, nameof(id));

            _roles.AddRange(ValidarRolesGuardados(roles));

            if (versionConsentimiento is not null || fechaConsentimientoUtc is not null)
            {
                if (versionConsentimiento is null)
                {
                    throw new ArgumentException("Hay fecha de consentimiento sin versión.", nameof(versionConsentimiento));
                }

                if (fechaConsentimientoUtc is null)
                {
                    throw new ArgumentException("Hay versión de consentimiento sin fecha.", nameof(fechaConsentimientoUtc));
                }

                RegistrarConsentimiento(versionConsentimiento, fechaConsentimientoUtc.Value);
            }
        }

        private Usuario(int id, string nombre, string correoElectronico, DateTime fechaRegistroUtc, bool activo)
        {
            Id = id;
            Nombre = Validacion.TextoRequerido(nombre, nameof(nombre), LongitudMaximaNombre, LongitudMinimaNombre);
            CorreoElectronico = NormalizarCorreo(correoElectronico, nameof(correoElectronico));
            FechaRegistroUtc = Validacion.FechaUtc(fechaRegistroUtc, nameof(fechaRegistroUtc));
            Activo = activo;
            Roles = _roles.AsReadOnly();
        }

        public bool TieneRol(CodigoRol rol) => _roles.Contains(rol);

        public void AsignarRol(CodigoRol rol)
        {
            Validacion.EnumDefinido(rol, nameof(rol));

            if (TieneRol(rol))
            {
                throw new InvalidOperationException($"El usuario ya tiene el rol {rol}.");
            }

            _roles.Add(rol);
        }

        public void QuitarRol(CodigoRol rol)
        {
            if (!TieneRol(rol))
            {
                throw new InvalidOperationException($"El usuario no tiene el rol {rol}.");
            }

            if (_roles.Count == 1)
            {
                throw new InvalidOperationException("No se puede quitar el último rol de un usuario (RN-01).");
            }

            _roles.Remove(rol);
        }

        /// <summary>Guarda la versión de la política aceptada; reemplaza la anterior (el historial se guarda aparte).</summary>
        public void RegistrarConsentimiento(string version, DateTime fechaUtc)
        {
            // Se valida todo antes de asignar: si la fecha es inválida, la versión anterior queda intacta.
            string versionValidada = Validacion.TextoRequerido(version, nameof(version), LongitudMaximaVersionConsentimiento);
            DateTime fechaValidada = Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));

            VersionConsentimiento = versionValidada;
            FechaConsentimientoUtc = fechaValidada;
        }

        /// <summary>Retira el consentimiento vigente; desde ahí RN-02 bloquea las acciones personales.</summary>
        public void RevocarConsentimiento()
        {
            VersionConsentimiento = null;
            FechaConsentimientoUtc = null;
        }

        // Activar y Desactivar son idempotentes a propósito: pedir el estado que ya se tiene deja el mismo
        // resultado y no indica un error. Asignar un rol repetido, en cambio, sí revela un error de quien llama.
        public void Activar() => Activo = true;

        public void Desactivar() => Activo = false;

        private static List<CodigoRol> ValidarRolesGuardados(IEnumerable<CodigoRol> roles)
        {
            Compat.ThrowIfNull(roles, nameof(roles));

            var lista = new List<CodigoRol>(roles);
            if (lista.Count == 0)
            {
                throw new ArgumentException("Un usuario debe tener al menos un rol.", nameof(roles));
            }

            foreach (CodigoRol rol in lista)
            {
                Validacion.EnumDefinido(rol, nameof(roles));
            }

            if (lista.Distinct().Count() != lista.Count)
            {
                throw new ArgumentException("La lista de roles tiene valores repetidos.", nameof(roles));
            }

            return lista;
        }

        private static string NormalizarCorreo(string? correo, string nombreParametro)
        {
            string limpio = Validacion.TextoRequerido(correo, nombreParametro, LongitudMaximaCorreo).ToLowerInvariant();

            // MailAddress también acepta formatos como "Nombre <a@b.co>"; se exige que la dirección sea todo el texto,
            // que el dominio tenga un punto (rechaza "ana@localhost") y que no haya caracteres de direcciones especiales.
            bool valido = limpio.IndexOfAny(CaracteresNoAdmitidosEnCorreo) < 0
                && Compat.TryCreateMailAddress(limpio, out MailAddress? direccion)
                && direccion.Address == limpio
                && direccion.User.Length <= LongitudMaximaParteLocalCorreo
                && direccion.Host.Contains('.')
                && !direccion.Host.EndsWith('.');

            if (!valido)
            {
                throw new ArgumentException("El correo electrónico no tiene un formato válido.", nombreParametro);
            }

            return limpio;
        }
    }
}
