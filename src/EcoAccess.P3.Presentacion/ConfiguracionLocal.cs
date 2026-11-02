using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>
    /// Guarda valores en los User Secrets del perfil de Windows (fuera del repositorio), el mismo almacén que lee
    /// Program. Solo agrega o reemplaza las claves indicadas: Telegram, visión y el resto se conservan intactos.
    /// </summary>
    internal static class ConfiguracionLocal
    {
        // La misma ruta que usa .NET para leerlos (respeta APPDATA), así lo guardado es exactamente lo que se lee.
        internal static string Ruta(string idSecretos) =>
            Microsoft.Extensions.Configuration.UserSecrets.PathHelper.GetSecretsPathFromSecretsId(idSecretos);

        internal static void Guardar(string idSecretos, IReadOnlyDictionary<string, string> valores)
        {
            string ruta = Ruta(idSecretos);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta)!);
            JsonObject raiz = File.Exists(ruta) && JsonNode.Parse(File.ReadAllText(ruta)) is JsonObject existente ? existente : new JsonObject();
            foreach (var (clave, valor) in valores)
            {
                // Una clave anidada («ConnectionStrings» → «OracleP3») se reemplaza por la plana para no dejar dos valores.
                string[] partes = clave.Split(':');
                if (partes.Length == 2 && raiz[partes[0]] is JsonObject seccion)
                {
                    seccion.Remove(partes[1]);
                    if (seccion.Count == 0) raiz.Remove(partes[0]);
                }
                raiz[clave] = valor;
            }
            string temporal = ruta + ".tmp";
            File.WriteAllText(temporal, raiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            Compat.MoverSobrescribiendo(temporal, ruta);
        }
    }
}
