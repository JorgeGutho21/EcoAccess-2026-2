using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica
{
    /// <summary>Las tres áreas visibles de EcoAccess. Supervisor es el nombre visible del rol técnico Administrador (3).</summary>
    public enum AreaAcceso
    {
        Ciudadano = 1,
        Operario = 2,
        Supervisor = 3,
    }

    /// <summary>
    /// Traduce roles de Oracle a áreas de la interfaz y decide si una cuenta puede entrar al área que eligió.
    /// Elegir un área en la pantalla de inicio NO concede nada: solo se entra si la cuenta tiene el rol real.
    /// Los servicios siguen revalidando el rol en cada operación (<see cref="Servicios.ControlAcceso"/>).
    /// </summary>
    public static class AreasAcceso
    {
        /// <summary>Nombre visible del rol. El código interno Administrador se muestra como Supervisor.</summary>
        public static string NombreRol(CodigoRol rol) => rol switch
        {
            CodigoRol.Ciudadano => "Ciudadano",
            CodigoRol.Operario => "Operario",
            CodigoRol.Administrador => "Supervisor",
            _ => "Rol desconocido",
        };

        /// <summary>Nombre con la aclaración técnica, para formularios de asignación de roles y manuales.</summary>
        public static string NombreRolCompleto(CodigoRol rol) =>
            rol == CodigoRol.Administrador ? "Supervisor (Administrador del sistema)" : NombreRol(rol);

        public static string NombreArea(AreaAcceso area) => NombreRol(RolDe(area));

        public static CodigoRol RolDe(AreaAcceso area) => area switch
        {
            AreaAcceso.Ciudadano => CodigoRol.Ciudadano,
            AreaAcceso.Operario => CodigoRol.Operario,
            AreaAcceso.Supervisor => CodigoRol.Administrador,
            _ => throw new ArgumentOutOfRangeException(nameof(area)),
        };

        /// <summary>Áreas a las que la cuenta puede entrar, en orden fijo: Ciudadano, Operario, Supervisor.</summary>
        public static IReadOnlyList<AreaAcceso> Disponibles(SesionActiva sesion)
        {
            if (sesion is null) throw new ArgumentNullException(nameof(sesion));
            return new[] { AreaAcceso.Ciudadano, AreaAcceso.Operario, AreaAcceso.Supervisor }
                .Where(a => sesion.TieneRol(RolDe(a))).ToArray();
        }

        /// <summary>Áreas de gestión (Windows Forms) de la cuenta: Operario y Supervisor.</summary>
        public static IReadOnlyList<AreaAcceso> DeGestion(SesionActiva sesion) =>
            Disponibles(sesion).Where(a => a != AreaAcceso.Ciudadano).ToArray();

        /// <summary>
        /// Autoriza la entrada al área solicitada. Si la cuenta no tiene ese rol, rechaza sin abrir ninguna otra área:
        /// la persona debe elegir el acceso que corresponde a su cuenta.
        /// </summary>
        public static Resultado<AreaAcceso> Elegir(SesionActiva sesion, AreaAcceso solicitada)
        {
            if (sesion is null) throw new ArgumentNullException(nameof(sesion));
            if (!Enum.IsDefined(typeof(AreaAcceso), solicitada))
                return Resultado<AreaAcceso>.Rechazo(MotivoRechazo.DatosInvalidos, "Elige un tipo de acceso válido.");
            if (Disponibles(sesion).Contains(solicitada)) return Resultado<AreaAcceso>.Exito(solicitada);
            string disponibles = string.Join(" o ", Disponibles(sesion).Select(NombreArea));
            return Resultado<AreaAcceso>.Rechazo(MotivoRechazo.SinPermiso,
                $"Tu cuenta no tiene acceso de {NombreArea(solicitada)}." +
                (disponibles.Length > 0 ? $" Puedes entrar como {disponibles}." : " Pide a un supervisor que revise tu cuenta."));
        }
    }
}
