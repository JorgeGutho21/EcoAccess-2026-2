using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Instalador
{
    /// <summary>
    /// Consultas de diagnóstico de Oracle Cloud que solo LEEN: identidad de la base, objetos, privilegios y cuentas.
    /// Nunca imprimen contraseñas, cadenas de conexión ni el contenido del wallet.
    /// </summary>
    internal static class Auditoria
    {
        /// <summary>Lo que ve cualquier cuenta: base, servicio, versión y quién soy.</summary>
        internal static readonly (string Titulo, string Sql)[] Identidad =
        {
            ("Base de datos y sesión",
             "SELECT SYS_CONTEXT('USERENV','DB_NAME') base, SYS_CONTEXT('USERENV','SERVICE_NAME') servicio, " +
             "SYS_CONTEXT('USERENV','CON_NAME') contenedor, SYS_CONTEXT('USERENV','CLOUD_SERVICE') tipo_autonomous, " +
             "SYS_CONTEXT('USERENV','SESSION_USER') usuario_sesion, SYS_CONTEXT('USERENV','CURRENT_SCHEMA') esquema_actual, " +
             "SYS_CONTEXT('USERENV','NETWORK_PROTOCOL') protocolo FROM dual"),
            ("Versión", "SELECT banner_full FROM v$version WHERE ROWNUM = 1"),
            ("Mis privilegios de sistema", "SELECT privilege FROM user_sys_privs ORDER BY privilege"),
            ("Mis roles", "SELECT granted_role, default_role FROM user_role_privs ORDER BY granted_role"),
        };

        /// <summary>Lo que solo ve el dueño del esquema (ECOACCESS_P3_APP).</summary>
        internal static readonly (string Titulo, string Sql)[] Esquema =
        {
            ("Objetos del esquema por tipo", "SELECT object_type tipo, COUNT(*) cantidad, SUM(CASE WHEN status <> 'VALID' THEN 1 ELSE 0 END) invalidos FROM user_objects WHERE object_name NOT LIKE 'BIN$%' GROUP BY object_type ORDER BY object_type"),
            ("Tablas del esquema", "SELECT table_name FROM user_tables ORDER BY table_name"),
            ("Cuota en tablespaces", "SELECT tablespace_name, ROUND(bytes/1048576) usado_mb, CASE max_bytes WHEN -1 THEN 'ilimitada' ELSE TO_CHAR(ROUND(max_bytes/1048576)) END cuota_mb FROM user_ts_quotas"),
            ("Permisos concedidos sobre mis objetos (por destinatario)", "SELECT grantee, privilege, COUNT(*) objetos FROM user_tab_privs_made GROUP BY grantee, privilege ORDER BY grantee, privilege"),
            ("Últimas migraciones aplicadas", "SELECT * FROM (SELECT * FROM eco_migracion_aplicada ORDER BY 1 DESC) WHERE ROWNUM <= 5"),
            ("Cuentas de la base que empiezan por ECO (solo nombre y fecha)", "SELECT username, TO_CHAR(created,'YYYY-MM-DD') creada FROM all_users WHERE username LIKE 'ECO%' ORDER BY username"),
        };

        /// <summary>Lo que solo ve ADMIN: estado y permisos de las cuentas del proyecto.</summary>
        internal static readonly (string Titulo, string Sql)[] Administrador =
        {
            ("Cuentas del proyecto", "SELECT username, account_status, TO_CHAR(expiry_date,'YYYY-MM-DD') expira, profile, TO_CHAR(created,'YYYY-MM-DD') creada FROM dba_users WHERE username LIKE 'ECO%' ORDER BY username"),
            ("Roles concedidos", "SELECT grantee, granted_role, admin_option FROM dba_role_privs WHERE grantee LIKE 'ECO%' ORDER BY grantee, granted_role"),
            ("Privilegios de sistema", "SELECT grantee, privilege, admin_option FROM dba_sys_privs WHERE grantee LIKE 'ECO%' ORDER BY grantee, privilege"),
            ("Privilegios peligrosos (debe estar vacío)", "SELECT grantee, privilege FROM dba_sys_privs WHERE grantee LIKE 'ECO%' AND (privilege LIKE '% ANY %' OR privilege IN ('GRANT ANY ROLE','GRANT ANY PRIVILEGE','ALTER USER','CREATE USER','DROP USER')) UNION ALL SELECT grantee, granted_role FROM dba_role_privs WHERE grantee LIKE 'ECO%' AND granted_role IN ('DBA','PDB_DBA','CONSOLE_ADMIN')"),
            ("EXECUTE sobre DBMS_CRYPTO (la aplicación no lo usa)", "SELECT grantee, privilege FROM dba_tab_privs WHERE table_name = 'DBMS_CRYPTO' AND grantee LIKE 'ECO%'"),
            ("Perfil de contraseñas de las cuentas ECO", "SELECT p.profile, p.resource_name, p.limit FROM dba_profiles p WHERE p.profile IN (SELECT profile FROM dba_users WHERE username LIKE 'ECO%') AND p.resource_name IN ('FAILED_LOGIN_ATTEMPTS','PASSWORD_LIFE_TIME','PASSWORD_LOCK_TIME') ORDER BY p.profile, p.resource_name"),
        };

        internal static async Task MostrarAsync(OracleConnection conexion, IEnumerable<(string Titulo, string Sql)> consultas, TextWriter salida, CancellationToken ct)
        {
            foreach (var (titulo, sql) in consultas)
            {
                salida.WriteLine();
                salida.WriteLine("== " + titulo);
                try
                {
                    using OracleCommand comando = conexion.CreateCommand();
                    comando.CommandText = sql;
                    using var lector = await comando.ExecuteReaderAsync(ct);
                    string[] columnas = Enumerable.Range(0, lector.FieldCount).Select(lector.GetName).ToArray();
                    var filas = new List<string[]>();
                    while (await lector.ReadAsync(ct))
                        filas.Add(Enumerable.Range(0, lector.FieldCount).Select(i => lector.IsDBNull(i) ? "" : Convert.ToString(lector.GetValue(i)) ?? "").ToArray());
                    if (filas.Count == 0) { salida.WriteLine("   (sin filas)"); continue; }
                    int[] anchos = columnas.Select((c, i) => Math.Min(48, Math.Max(c.Length, filas.Max(f => f[i].Length)))).ToArray();
                    salida.WriteLine("   " + string.Join("  ", columnas.Select((c, i) => c.PadRight(anchos[i]))));
                    foreach (string[] f in filas)
                        salida.WriteLine("   " + string.Join("  ", f.Select((v, i) => (v.Length > anchos[i] ? v.Substring(0, anchos[i]) : v).PadRight(anchos[i]))));
                }
                catch (OracleException error)
                {
                    salida.WriteLine($"   No disponible para esta cuenta (ORA-{error.Number:00000}).");
                }
            }
        }

        /// <summary>
        /// Pruebas de una cuenta de integrante: lo autorizado funciona y lo administrativo se rechaza. Devuelve cuántas no
        /// dieron lo esperado. No modifica datos: las sentencias que deben rechazarse no afectan filas aunque se aceptaran.
        /// </summary>
        internal static async Task<int> ComprobarIntegranteAsync(OracleConnection conexion, TextWriter salida, CancellationToken ct)
        {
            int fallas = 0;
            async Task Probar(string nombre, string sql, bool debeFuncionar)
            {
                string resultado;
                bool funciono;
                try
                {
                    using OracleCommand comando = conexion.CreateCommand();
                    comando.CommandText = sql;
                    object? valor = await comando.ExecuteScalarAsync(ct);
                    funciono = true;
                    resultado = valor is null || valor is DBNull ? "aceptado" : "aceptado (" + valor + ")";
                }
                catch (OracleException error)
                {
                    funciono = false;
                    resultado = $"rechazado ORA-{error.Number:00000}";
                }
                bool esperado = funciono == debeFuncionar;
                if (!esperado) fallas++;
                salida.WriteLine($"   [{(esperado ? "OK" : "REVISAR")}] {nombre}: {resultado}{(debeFuncionar ? "" : " (debe rechazarse)")}");
            }

            await Probar("Cambiar al esquema compartido", "ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP", true);
            await Probar("Leer usuarios de la aplicación", "SELECT COUNT(*) FROM usuario", true);
            await Probar("Leer catálogo de materiales", "SELECT COUNT(*) FROM material", true);
            await Probar("Leer canecas", "SELECT COUNT(*) FROM contenedor", true);
            await Probar("Borrar datos (DELETE)", "DELETE FROM material WHERE 1 = 0", false);
            await Probar("Crear tablas en el esquema compartido", "CREATE TABLE ecoaccess_p3_app.prueba_no_autorizada (x NUMBER)", false);
            await Probar("Ver todas las cuentas (DBA_USERS)", "SELECT COUNT(*) FROM dba_users", false);
            await Probar("Conceder privilegios", "GRANT CREATE SESSION TO PUBLIC", false);
            return fallas;
        }
    }
}
