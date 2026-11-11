using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Operaciones protegidas por rol. Cada una corresponde a un método de servicio que revalida al actor.</summary>
    public enum OperacionProtegida
    {
        ConsultarMisEcopuntos,
        ConsultarMisEstadisticas,
        ConsultarEcopuntosDeOtro,
        ListarReglasEcopuntos,
        RegistrarReglaEcopuntos,
        BuscarCiudadanos,
        ConsultarEstadisticasGenerales,
        ListarUsuarios,
        AsignarRol,
        GestionarMateriales,
        ConsultarInformeInstitucional,
        DiagnosticarOracle,
    }

    /// <summary>
    /// Matriz única y legible de qué rol puede hacer qué (RN-15). No sustituye a la revalidación de cada servicio:
    /// la documenta, y la prueba <c>MatrizPermisosTests</c> comprueba que los servicios reales la cumplen para los tres
    /// roles. Si alguien cambia un permiso en un servicio sin cambiarlo aquí, la prueba falla.
    /// </summary>
    public static class MatrizPermisos
    {
        private static readonly CodigoRol[] SoloCiudadano = { CodigoRol.Ciudadano };
        private static readonly CodigoRol[] Gestion = { CodigoRol.Operario, CodigoRol.Administrador };
        private static readonly CodigoRol[] SoloAdministrador = { CodigoRol.Administrador };

        private static readonly IReadOnlyDictionary<OperacionProtegida, CodigoRol[]> Roles =
            new Dictionary<OperacionProtegida, CodigoRol[]>
            {
                [OperacionProtegida.ConsultarMisEcopuntos] = SoloCiudadano,
                [OperacionProtegida.ConsultarMisEstadisticas] = SoloCiudadano,
                [OperacionProtegida.ConsultarEcopuntosDeOtro] = Gestion,
                [OperacionProtegida.ListarReglasEcopuntos] = Gestion,
                [OperacionProtegida.RegistrarReglaEcopuntos] = SoloAdministrador,
                [OperacionProtegida.BuscarCiudadanos] = Gestion,
                [OperacionProtegida.ConsultarEstadisticasGenerales] = Gestion,
                [OperacionProtegida.ListarUsuarios] = SoloAdministrador,
                [OperacionProtegida.AsignarRol] = SoloAdministrador,
                [OperacionProtegida.GestionarMateriales] = SoloAdministrador,
                [OperacionProtegida.ConsultarInformeInstitucional] = SoloAdministrador,
                [OperacionProtegida.DiagnosticarOracle] = SoloAdministrador,
            };

        public static IReadOnlyList<OperacionProtegida> Operaciones { get; } =
            Enum.GetValues(typeof(OperacionProtegida)).Cast<OperacionProtegida>().ToList();

        public static IReadOnlyList<CodigoRol> RolesPermitidos(OperacionProtegida operacion) =>
            Roles.TryGetValue(operacion, out var roles) ? roles : Array.Empty<CodigoRol>();

        /// <summary>Si una cuenta activa con exactamente ese rol puede ejecutar la operación.</summary>
        public static bool Permite(CodigoRol rol, OperacionProtegida operacion) => RolesPermitidos(operacion).Contains(rol);
    }
}
