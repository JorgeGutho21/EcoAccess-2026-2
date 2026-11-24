using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Oracle
{
    /// <summary>
    /// Wallet de Oracle Autonomous Database (mTLS). La carpeta se descarga desde la consola de Oracle Cloud
    /// («Database connection» → «Download wallet»), se descomprime FUERA del repositorio y contiene, entre otros,
    /// <c>tnsnames.ora</c>, <c>sqlnet.ora</c> y <c>cwallet.sso</c>.
    /// </summary>
    /// <remarks>
    /// Esta clase no abre conexiones ni lee contraseñas: comprueba que la carpeta sirve, informa qué alias
    /// (por ejemplo <c>ecoaccess_tp</c>) y qué host publica, y arma la cadena que usa ODP.NET. Así la presentación,
    /// el bot y el instalador no repiten estas reglas (DRY) y se pueden probar sin Oracle.
    /// </remarks>
    public sealed class WalletOracleNube
    {
        /// <summary>Archivos sin los que ODP.NET no puede negociar mTLS con Autonomous Database.</summary>
        public static readonly IReadOnlyList<string> ArchivosRequeridos = new[] { "tnsnames.ora", "sqlnet.ora", "cwallet.sso" };

        /// <summary>Alias recomendado para EcoAccess: «_tp» es el servicio de transacciones cortas (OLTP).</summary>
        public const string AliasPredeterminado = "ecoaccess_tp";

        private static readonly Regex EntradaTns = new Regex(
            @"^\s*(?<alias>[A-Za-z0-9_.\-]+)\s*=\s*(?<cuerpo>\(.*?)(?=^\s*[A-Za-z0-9_.\-]+\s*=|\z)",
            RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex HostTns = new Regex(@"\(\s*host\s*=\s*([^)\s]+)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PuertoTns = new Regex(@"\(\s*port\s*=\s*(\d+)\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly Dictionary<string, (string Host, int Puerto)> _alias;

        private WalletOracleNube(string carpeta, Dictionary<string, (string Host, int Puerto)> alias)
        {
            Carpeta = carpeta;
            _alias = alias;
        }

        /// <summary>Carpeta privada sugerida: dentro del perfil de Windows de cada integrante, nunca en el repositorio.</summary>
        public static string CarpetaPredeterminada => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oracle", "wallets", "EcoAccess");

        /// <summary>Ruta absoluta de la carpeta del wallet.</summary>
        public string Carpeta { get; }

        /// <summary>Alias definidos en tnsnames.ora, en minúsculas.</summary>
        public IReadOnlyCollection<string> Alias => _alias.Keys;

        /// <summary>
        /// Comprueba la carpeta y lee los alias. Devuelve null y un motivo legible si falta algo; nunca lanza por una
        /// carpeta incompleta, porque es un error de configuración normal en un equipo nuevo.
        /// </summary>
        public static WalletOracleNube? Abrir(string? carpeta, out string motivo)
        {
            if (string.IsNullOrWhiteSpace(carpeta))
            {
                motivo = "No se indicó la carpeta del wallet.";
                return null;
            }

            string ruta = Path.GetFullPath(carpeta);
            if (!Directory.Exists(ruta))
            {
                motivo = "No existe la carpeta del wallet: " + ruta;
                return null;
            }

            string[] faltantes = ArchivosRequeridos.Where(a => !File.Exists(Path.Combine(ruta, a))).ToArray();
            if (faltantes.Length > 0)
            {
                motivo = "Al wallet le faltan archivos: " + string.Join(", ", faltantes) + ". Descomprime el ZIP completo.";
                return null;
            }

            Dictionary<string, (string Host, int Puerto)> alias = LeerAlias(File.ReadAllText(Path.Combine(ruta, "tnsnames.ora")));
            if (alias.Count == 0)
            {
                motivo = "tnsnames.ora no contiene alias con host y puerto.";
                return null;
            }

            motivo = "";
            return new WalletOracleNube(ruta, alias);
        }

        /// <summary>Host y puerto que publica un alias, para comprobar la red antes de autenticar.</summary>
        public bool TryObtenerDestino(string alias, out string host, out int puerto)
        {
            if (_alias.TryGetValue(alias.Trim().ToLowerInvariant(), out var destino))
            {
                host = destino.Host;
                puerto = destino.Puerto;
                return true;
            }

            host = "";
            puerto = 0;
            return false;
        }

        /// <summary>
        /// Cadena de ODP.NET para Autonomous Database con mTLS. <c>Tns_Admin</c> indica dónde está tnsnames.ora y
        /// <c>Wallet_Location</c> dónde está cwallet.sso; ambos apuntan a la misma carpeta privada.
        /// </summary>
        public string CrearCadena(string alias, string usuario, string contrasena)
        {
            if (!_alias.ContainsKey(alias.Trim().ToLowerInvariant()))
                throw new ArgumentException("El alias " + alias + " no está en tnsnames.ora.", nameof(alias));
            if (string.IsNullOrWhiteSpace(usuario)) throw new ArgumentException("Falta el usuario.", nameof(usuario));

            var cadena = new OracleConnectionStringBuilder
            {
                UserID = usuario.Trim(),
                Password = contrasena,
                DataSource = alias.Trim().ToLowerInvariant(),
                ConnectionTimeout = 30,
            };
            cadena["Tns_Admin"] = Carpeta;
            cadena["Wallet_Location"] = Carpeta;
            return cadena.ConnectionString;
        }

        /// <summary>
        /// Completa una cadena guardada que solo nombra el alias (por ejemplo la que deja
        /// <c>Configurar-Credenciales.ps1</c>, pensada para usarse con <c>TNS_ADMIN</c>) con la carpeta de este wallet.
        /// Devuelve la cadena sin cambios si ya declara su wallet, y null si está vacía o su Data Source no es un alias
        /// de este wallet (por ejemplo una base local con host:puerto/servicio). No lee ni muestra la contraseña.
        /// </summary>
        public string? CompletarCadena(string? cadena)
        {
            if (string.IsNullOrWhiteSpace(cadena)) return null;
            if (UsaWallet(cadena)) return cadena;
            try
            {
                var partes = new OracleConnectionStringBuilder(cadena);
                string destino = (partes.DataSource ?? "").Trim().ToLowerInvariant();
                if (!_alias.ContainsKey(destino)) return null;
                partes["Tns_Admin"] = Carpeta;
                partes["Wallet_Location"] = Carpeta;
                return partes.ConnectionString;
            }
            catch (ArgumentException) { return null; }
        }

        /// <summary>True si la cadena usa un wallet (Oracle Cloud) en lugar de host:puerto/servicio local.</summary>
        public static bool UsaWallet(string? cadena) => !string.IsNullOrWhiteSpace(CarpetaDe(cadena));

        /// <summary>Carpeta del wallet que declara una cadena, o null si no declara ninguna.</summary>
        public static string? CarpetaDe(string? cadena)
        {
            if (string.IsNullOrWhiteSpace(cadena)) return null;
            try
            {
                var partes = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cadena };
                return partes.TryGetValue("Wallet_Location", out object? valor) ? valor as string : null;
            }
            catch (ArgumentException) { return null; }
        }

        /// <summary>Alias (Data Source) y usuario de una cadena, sin exponer la contraseña.</summary>
        public static (string? Alias, string? Usuario) Describir(string? cadena)
        {
            if (string.IsNullOrWhiteSpace(cadena)) return (null, null);
            try
            {
                var partes = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = cadena };
                partes.TryGetValue("Data Source", out object? alias);
                partes.TryGetValue("User Id", out object? usuario);
                return (alias as string, usuario as string);
            }
            catch (ArgumentException) { return (null, null); }
        }

        /// <summary>
        /// Lee «alias = (description= ... (host=...)(port=...) ...)». El formato del wallet de Autonomous Database es
        /// estable; no se interpreta todo el lenguaje de tnsnames, solo lo necesario para EcoAccess.
        /// </summary>
        internal static Dictionary<string, (string Host, int Puerto)> LeerAlias(string tnsnames)
        {
            var resultado = new Dictionary<string, (string Host, int Puerto)>(StringComparer.OrdinalIgnoreCase);
            foreach (Match entrada in EntradaTns.Matches(tnsnames))
            {
                string cuerpo = entrada.Groups["cuerpo"].Value;
                Match host = HostTns.Match(cuerpo);
                Match puerto = PuertoTns.Match(cuerpo);
                if (host.Success && puerto.Success && int.TryParse(puerto.Groups[1].Value, out int numero))
                    resultado[entrada.Groups["alias"].Value.ToLowerInvariant()] = (host.Groups[1].Value, numero);
            }

            return resultado;
        }
    }
}
