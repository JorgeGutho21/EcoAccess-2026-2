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

        /// <summary>
        /// Destino usado si App.config no indica ninguno: el alias TNS de la base oficial en Oracle Cloud (Autonomous
        /// Database). El alias se resuelve con el tnsnames.ora del wallet, que nunca se versiona (ver <see cref="CarpetaWallet"/>).
        /// Oracle local (Docker PRUEBAP3 o FREEPDB1) queda solo como entorno de desarrollo y se elige en App.config o User Secrets.
        /// </summary>
        public const string DestinoPredeterminado = "ecoaccess_tp";

        /// <summary>
        /// Carpeta del wallet de Oracle Cloud, en este orden: TNS_ADMIN del proceso, TNS_ADMIN del usuario de Windows,
        /// %LOCALAPPDATA%\EcoAccessP3\wallet (la que usa el instalador) y %USERPROFILE%\.oracle\wallets\EcoAccess.
        /// Devuelve null si ninguna contiene tnsnames.ora. Nunca lee ni muestra el contenido del wallet.
        /// </summary>
        public static string? CarpetaWallet()
        {
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string perfil = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string?[] candidatas =
            {
                Environment.GetEnvironmentVariable("TNS_ADMIN"),
                Environment.GetEnvironmentVariable("TNS_ADMIN", EnvironmentVariableTarget.User),
                System.IO.Path.Combine(local, "EcoAccessP3", "wallet"),
                System.IO.Path.Combine(perfil, ".oracle", "wallets", "EcoAccess"),
            };
            foreach (string? carpeta in candidatas)
                if (!string.IsNullOrWhiteSpace(carpeta) && System.IO.File.Exists(System.IO.Path.Combine(carpeta!.Trim(), "tnsnames.ora")))
                    return carpeta.Trim();
            return null;
        }

        /// <summary>
        /// Si el destino es un alias TNS (sin «/» ni «:») y el proceso no tiene TNS_ADMIN, lo fija con
        /// <see cref="CarpetaWallet"/> para que ODP.NET encuentre tnsnames.ora y sqlnet.ora. Devuelve true si hay wallet.
        /// </summary>
        public static bool PrepararWallet(string? cadena)
        {
            string destino = Destino(cadena);
            if (destino.IndexOf('/') >= 0 || destino.IndexOf(':') >= 0) return true; // EZConnect: no usa wallet.
            string? carpeta = CarpetaWallet();
            if (carpeta is null) return false;
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TNS_ADMIN")))
                Environment.SetEnvironmentVariable("TNS_ADMIN", carpeta);
            return true;
        }

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
