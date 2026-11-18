using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EcoAccess.P3.Instalador
{

    /// <summary>Tipo de cada pieza que se obtiene de un script .sql.</summary>
    public enum TipoSentencia
    {
        /// <summary>DDL o DML que termina en «;» (CREATE TABLE, MERGE, ALTER…).</summary>
        Sql = 1,

        /// <summary>SELECT o WITH: se ejecuta y se imprimen sus filas como evidencia.</summary>
        Consulta = 2,

        /// <summary>Bloque PL/SQL (DECLARE/BEGIN, CREATE PROCEDURE/TRIGGER…) que termina con «/» en su propia línea.</summary>
        PlSql = 3,

        /// <summary>PROMPT de SQL*Plus: solo se muestra el texto.</summary>
        Mensaje = 4,

        /// <summary>WHENEVER SQLERROR CONTINUE: los errores siguientes se informan pero no detienen el script.</summary>
        ContinuarAnteErrores = 5,

        /// <summary>WHENEVER SQLERROR EXIT: el primer error detiene el script (comportamiento por defecto).</summary>
        DetenerseAnteErrores = 6,
}

    /// <summary>Una sentencia lista para ODP.NET, con la línea del archivo donde empieza (para los mensajes de error).</summary>
    public sealed record SentenciaScript(TipoSentencia Tipo, string Texto, int Linea);

    /// <summary>
    /// Convierte un script escrito para SQL*Plus / SQL Developer (F5) en sentencias que ODP.NET puede ejecutar una a una.
    /// </summary>
    /// <remarks>
    /// Solo admite el subconjunto que usan los scripts de EcoAccess: comentarios «--» y «/* */», SET, WHENEVER, PROMPT,
    /// EXEC, sentencias SQL terminadas en «;» y bloques PL/SQL terminados en «/». Las variables de sustitución («&») y
    /// la inclusión de otros archivos («@») se rechazan con un error claro, porque ejecutarlas a medias sería peor.
    /// </remarks>
    public static class LectorScriptSql
    {
        private static readonly HashSet<string> DirectivasIgnoradas = new(StringComparer.OrdinalIgnoreCase)
        {
            "SET", "SPOOL", "COLUMN", "COL", "TTITLE", "BTITLE", "BREAK", "COMPUTE", "CLEAR", "PAUSE", "SHOW", "REM", "REMARK",
        };

        private static readonly string[] InicioBloqueCreate =
            { "PROCEDURE", "FUNCTION", "PACKAGE", "TRIGGER", "TYPE" };

        public static IReadOnlyList<SentenciaScript> Leer(string script)
        {
            var resultado = new List<SentenciaScript>();
            string[] lineas = script.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var actual = new StringBuilder();
            TipoSentencia? tipoActual = null;
            int lineaInicio = 0;
            bool enComentarioBloque = false;

            for (int i = 0; i < lineas.Length; i++)
            {
                string linea = lineas[i];
                string recortada = linea.Trim();
                int numero = i + 1;

                if (tipoActual == TipoSentencia.PlSql)
                {
                    if (recortada == "/")
                    {
                        resultado.Add(new(TipoSentencia.PlSql, actual.ToString().TrimEnd(), lineaInicio));
                        actual.Clear();
                        tipoActual = null;
                    }
                    else actual.AppendLine(linea);
                    continue;
                }

                if (tipoActual is TipoSentencia.Sql or TipoSentencia.Consulta)
                {
                    if (recortada == "/")
                    {
                        Cerrar(resultado, actual, tipoActual.Value, lineaInicio);
                        tipoActual = null;
                        continue;
                    }

                    if (TerminaSentencia(linea, out string sinTerminador))
                    {
                        actual.AppendLine(sinTerminador);
                        Cerrar(resultado, actual, tipoActual.Value, lineaInicio);
                        tipoActual = null;
                    }
                    else actual.AppendLine(linea);
                    continue;
                }

                // Fuera de cualquier sentencia.
                if (enComentarioBloque)
                {
                    if (recortada.IndexOf("*/", StringComparison.Ordinal) >= 0) enComentarioBloque = false;
                    continue;
                }

                if (recortada.Length == 0 || recortada.StartsWith("--", StringComparison.Ordinal) || recortada == "/") continue;
                if (recortada.StartsWith("/*", StringComparison.Ordinal))
                {
                    enComentarioBloque = recortada.IndexOf("*/", StringComparison.Ordinal) < 0;
                    continue;
                }

                string primera = PrimeraPalabra(recortada);
                if (recortada.StartsWith("@", StringComparison.Ordinal))
                    throw new FormatException($"Línea {numero}: «@» (incluir otro script) no está admitido por el instalador.");
                if (primera.Equals("PROMPT", StringComparison.OrdinalIgnoreCase))
                {
                    resultado.Add(new(TipoSentencia.Mensaje, recortada.Length > 6 ? recortada.Substring(6).Trim() : "", numero));
                    continue;
                }

                if (primera.Equals("WHENEVER", StringComparison.OrdinalIgnoreCase))
                {
                    if (recortada.IndexOf("OSERROR", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    bool continuar = recortada.IndexOf("CONTINUE", StringComparison.OrdinalIgnoreCase) >= 0;
                    resultado.Add(new(continuar ? TipoSentencia.ContinuarAnteErrores : TipoSentencia.DetenerseAnteErrores, recortada, numero));
                    continue;
                }

                if (primera is "DEFINE" or "UNDEFINE" or "ACCEPT" or "VARIABLE" or "PRINT" or "EXIT" or "QUIT")
                    throw new FormatException($"Línea {numero}: la directiva {primera} de SQL*Plus no está admitida por el instalador.");
                if (DirectivasIgnoradas.Contains(primera)) continue;

                if (primera.Equals("EXEC", StringComparison.OrdinalIgnoreCase) || primera.Equals("EXECUTE", StringComparison.OrdinalIgnoreCase))
                {
                    string cuerpo = recortada.Substring(primera.Length).Trim().TrimEnd(';');
                    resultado.Add(new(TipoSentencia.PlSql, $"BEGIN {cuerpo}; END;", numero));
                    continue;
                }

                lineaInicio = numero;
                actual.Clear();
                if (EsInicioPlSql(recortada))
                {
                    tipoActual = TipoSentencia.PlSql;
                    actual.AppendLine(linea);
                    continue;
                }

                tipoActual = primera.Equals("SELECT", StringComparison.OrdinalIgnoreCase) || primera.Equals("WITH", StringComparison.OrdinalIgnoreCase)
                    ? TipoSentencia.Consulta
                    : TipoSentencia.Sql;
                if (TerminaSentencia(linea, out string unaLinea))
                {
                    actual.AppendLine(unaLinea);
                    Cerrar(resultado, actual, tipoActual.Value, lineaInicio);
                    tipoActual = null;
                }
                else actual.AppendLine(linea);
            }

            if (tipoActual is not null && actual.ToString().Trim().Length > 0)
                throw new FormatException($"Línea {lineaInicio}: la última sentencia no termina con «;» o «/».");

            foreach (SentenciaScript s in resultado)
                if (s.Tipo is TipoSentencia.Sql or TipoSentencia.Consulta && ContieneSustitucion(s.Texto))
                    throw new FormatException($"Línea {s.Linea}: usa variables de sustitución «&», que el instalador no admite.");
            return resultado;
        }

        private static void Cerrar(List<SentenciaScript> resultado, StringBuilder actual, TipoSentencia tipo, int linea)
        {
            string texto = actual.ToString().Trim();
            if (texto.Length > 0) resultado.Add(new(tipo, texto, linea));
            actual.Clear();
        }

        private static string PrimeraPalabra(string texto)
        {
            int fin = 0;
            while (fin < texto.Length && !char.IsWhiteSpace(texto[fin]) && texto[fin] != '(' && texto[fin] != ';') fin++;
            return texto.Substring(0, fin).ToUpperInvariant();
        }

        /// <summary>DECLARE, BEGIN o CREATE [OR REPLACE] [EDITIONABLE] PROCEDURE/FUNCTION/PACKAGE/TRIGGER/TYPE.</summary>
        internal static bool EsInicioPlSql(string recortada)
        {
            string[] palabras = recortada.ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (palabras.Length == 0) return false;
            if (palabras[0] is "DECLARE" or "BEGIN" || palabras[0].StartsWith("DECLARE", StringComparison.Ordinal)) return true;
            if (palabras[0] != "CREATE") return false;
            int i = 1;
            if (i + 1 < palabras.Length && palabras[i] == "OR" && palabras[i + 1] == "REPLACE") i += 2;
            if (i < palabras.Length && palabras[i] is "EDITIONABLE" or "NONEDITIONABLE") i++;
            return i < palabras.Length && InicioBloqueCreate.Contains(palabras[i]);
        }

        /// <summary>
        /// True si la línea termina la sentencia con «;» fuera de comillas (se admite un comentario «--» después).
        /// Devuelve la línea sin el «;» final ni el comentario. Las comillas que empiezan en líneas anteriores no se
        /// siguen: los scripts de EcoAccess no tienen literales de varias líneas fuera de bloques PL/SQL.
        /// </summary>
        internal static bool TerminaSentencia(string linea, out string sinTerminador)
        {
            bool enTexto = false;
            int ultimoPuntoComa = -1;
            int finUtil = linea.Length;
            for (int i = 0; i < linea.Length; i++)
            {
                char c = linea[i];
                if (enTexto)
                {
                    if (c == '\'') enTexto = false;
                    continue;
                }

                if (c == '\'') { enTexto = true; continue; }
                if (c == '-' && i + 1 < linea.Length && linea[i + 1] == '-') { finUtil = i; break; }
                if (c == ';') ultimoPuntoComa = i;
                else if (!char.IsWhiteSpace(c)) ultimoPuntoComa = -1;
            }

            if (ultimoPuntoComa >= 0 && ultimoPuntoComa < finUtil)
            {
                sinTerminador = linea.Substring(0, ultimoPuntoComa);
                return true;
            }

            sinTerminador = linea;
            return false;
        }

        /// <summary>
        /// Variables de enlace («:nombre») fuera de literales y comentarios, en minúsculas y sin repetir. No confunde
        /// la asignación de PL/SQL («:=») porque exige una letra después de los dos puntos, y omite <c>:NEW</c>,
        /// <c>:OLD</c> y <c>:PARENT</c>, que en un trigger son pseudorregistros de la fila, no variables.
        /// </summary>
        public static IReadOnlyList<string> VariablesDeEnlace(string texto)
        {
            var pseudorregistros = new HashSet<string>(StringComparer.Ordinal) { "new", "old", "parent" };
            var nombres = new List<string>();
            bool enTexto = false, enComentario = false;
            for (int i = 0; i < texto.Length; i++)
            {
                char c = texto[i];
                if (enComentario) { if (c == '\n') enComentario = false; continue; }
                if (c == '\'') { enTexto = !enTexto; continue; }
                if (enTexto) continue;
                if (c == '-' && i + 1 < texto.Length && texto[i + 1] == '-') { enComentario = true; continue; }
                if (c != ':' || i + 1 >= texto.Length || !char.IsLetter(texto[i + 1])) continue;
                if (i > 0 && (char.IsLetterOrDigit(texto[i - 1]) || texto[i - 1] == '_')) continue;
                int fin = i + 1;
                while (fin < texto.Length && (char.IsLetterOrDigit(texto[fin]) || texto[fin] == '_')) fin++;
                string nombre = texto.Substring(i + 1, fin - i - 1).ToLowerInvariant();
                if (!pseudorregistros.Contains(nombre) && !nombres.Contains(nombre)) nombres.Add(nombre);
                i = fin - 1;
            }

            return nombres;
        }

        private static bool ContieneSustitucion(string texto)
        {
            bool enTexto = false;
            for (int i = 0; i < texto.Length; i++)
            {
                if (texto[i] == '\'') enTexto = !enTexto;
                else if (!enTexto && texto[i] == '&' && i + 1 < texto.Length && char.IsLetter(texto[i + 1])) return true;
            }

            return false;
        }
}
}
