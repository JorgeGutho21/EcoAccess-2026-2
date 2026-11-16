using System;
using System.Collections.Generic;
using System.Data.Common;

namespace EcoAccess.P3.Datos.Oracle
{
    /// <summary>
    /// Origen de la cadena de conexión, como enseña el curso (App.config + ConfigurationManager) pero sin guardar la
    /// contraseña en el repositorio: App.config aporta la parte pública (destino y usuario del esquema) y la cadena
    /// completa, con contraseña, solo puede venir de los User Secrets del perfil de Windows o de una variable de entorno.
    /// </summary>
    public static class CadenaConexionOracle
    {
        /// <summary>Nombre de la entrada en <c>&lt;connectionStrings&gt;</c> de App.config y en los User Secrets.</summary>
        public const string Nombre = "OracleP3";

        /// <summary>Destino usado si App.config no indica ninguno (contenedor Oracle local de la guía de instalación).</summary>
        public const string DestinoPredeterminado = "localhost:1522/PRUEBAP3";

        /// <summary>
        /// La cadena privada (User Secrets o variable de entorno) tiene prioridad. Si no existe, se usa la plantilla
        /// pública de App.config; una plantilla que trae contraseña se rechaza (devuelve null) para que nadie la
        /// versione por error.
        /// </summary>
        public static string? Resolver(string? privada, string? deAppConfig)
        {
            if (!string.IsNullOrWhiteSpace(privada)) return privada;
            if (string.IsNullOrWhiteSpace(deAppConfig) || TieneContrasena(deAppConfig)) return null;
            return deAppConfig;
        }

        /// <summary>True si la cadena incluye una contraseña no vacía (claves «Password» o «PWD»).</summary>
        public static bool TieneContrasena(string? cadena) =>
            !string.IsNullOrEmpty(Valor(cadena, "Password")) || !string.IsNullOrEmpty(Valor(cadena, "PWD"));

        /// <summary>«Data Source» de la cadena o, si falta, el destino predeterminado.</summary>
        public static string Destino(string? cadena) => Valor(cadena, "Data Source") is { Length: > 0 } destino ? destino : DestinoPredeterminado;

        /// <summary>«User Id» de la cadena, o null.</summary>
        public static string? Usuario(string? cadena) => Valor(cadena, "User Id");

        private static string? Valor(string? cadena, string clave)
        {
            if (string.IsNullOrWhiteSpace(cadena)) return null;
            try
            {
                var partes = new DbConnectionStringBuilder { ConnectionString = cadena };
                return partes.TryGetValue(clave, out object? valor) ? (valor as string)?.Trim() : null;
            }
            catch (Exception e) when (e is ArgumentException or KeyNotFoundException)
            {
                return null;
            }
        }
    }
}
