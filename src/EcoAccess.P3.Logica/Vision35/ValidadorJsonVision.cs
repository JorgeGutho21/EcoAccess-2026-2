using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>
    /// Valida formato de imagen y valida estrictamente el JSON emitido por Ollama (Qwen-VL / LFM3-VL).
    /// Cualquier JSON mal formado, truncado, con enumeraciones desconocidas o fuera de rango se rechaza de forma segura
    /// (T027) y jamás se convierte en una orden de apertura.
    /// </summary>
    public static class ValidadorJsonVision
    {
        public const int MaximoBytesJson = 32 * 1024;
        public const int MaximoObjetosPorFoto = 12;

        private static readonly Dictionary<string, EstadoObservableObjeto> MapaEstados = new(StringComparer.OrdinalIgnoreCase)
        {
            ["desconocido"] = EstadoObservableObjeto.Desconocido,
            ["unknown"] = EstadoObservableObjeto.Desconocido,
            ["limpio_y_vacio"] = EstadoObservableObjeto.LimpioYVacio,
            ["limpio"] = EstadoObservableObjeto.LimpioYVacio,
            ["clean"] = EstadoObservableObjeto.LimpioYVacio,
            ["clean_empty"] = EstadoObservableObjeto.LimpioYVacio,
            ["empty"] = EstadoObservableObjeto.LimpioYVacio,
            ["contaminado"] = EstadoObservableObjeto.Contaminado,
            ["sucio"] = EstadoObservableObjeto.Contaminado,
            ["usado"] = EstadoObservableObjeto.Contaminado,
            ["con_grasa"] = EstadoObservableObjeto.Contaminado,
            ["dirty"] = EstadoObservableObjeto.Contaminado,
            ["contaminated"] = EstadoObservableObjeto.Contaminado,
            ["greasy"] = EstadoObservableObjeto.Contaminado,
            ["used"] = EstadoObservableObjeto.Contaminado,
            ["con_liquido"] = EstadoObservableObjeto.ConLiquido,
            ["lleno"] = EstadoObservableObjeto.ConLiquido,
            ["liquid"] = EstadoObservableObjeto.ConLiquido,
            ["with_liquid"] = EstadoObservableObjeto.ConLiquido,
            ["full"] = EstadoObservableObjeto.ConLiquido,
            ["danado_o_roto"] = EstadoObservableObjeto.DanadoORoto,
            ["roto"] = EstadoObservableObjeto.DanadoORoto,
            ["broken"] = EstadoObservableObjeto.DanadoORoto,
            ["reutilizable"] = EstadoObservableObjeto.Reutilizable,
            ["reusable"] = EstadoObservableObjeto.Reutilizable,
            ["nuevo_sin_uso"] = EstadoObservableObjeto.NuevoSinUso,
            ["nuevo"] = EstadoObservableObjeto.NuevoSinUso,
            ["new"] = EstadoObservableObjeto.NuevoSinUso,
        };

        private static readonly Dictionary<string, CalidadImagenVision> MapaCalidad = new(StringComparer.OrdinalIgnoreCase)
        {
            ["aceptable"] = CalidadImagenVision.Aceptable,
            ["ok"] = CalidadImagenVision.Aceptable,
            ["good"] = CalidadImagenVision.Aceptable,
            ["clear"] = CalidadImagenVision.Aceptable,
            ["borrosa"] = CalidadImagenVision.Borrosa,
            ["blurry"] = CalidadImagenVision.Borrosa,
            ["oscura"] = CalidadImagenVision.Oscura,
            ["dark"] = CalidadImagenVision.Oscura,
            ["vacia"] = CalidadImagenVision.Vacia,
            ["empty"] = CalidadImagenVision.Vacia,
        };

        /// <summary>Verifica que los bytes correspondan realmente a JPEG, PNG o WebP dentro del tamaño seguro.</summary>
        public static bool ValidarFormatoImagen(byte[]? contenido, out string? tipoMime, out string? error)
        {
            tipoMime = ServicioClasificacionImagen.TipoMimeReal(contenido);
            if (tipoMime is null)
            {
                error = "La imagen está vacía, corrupta, supera el tamaño máximo (5 MB) o no es JPEG, PNG ni WebP.";
                return false;
            }
            error = null;
            return true;
        }

        /// <summary>
        /// Valida y deserializa estrictamente la respuesta JSON de un modelo multimodal.
        /// Nunca inventa coordenadas ni completa campos obligatorios ausentes.
        /// </summary>
        public static RespuestaVisionEstructurada ValidarRespuestaJson(string? textoJson, string modeloUtilizado, long latenciaMs)
        {
            if (string.IsNullOrWhiteSpace(textoJson))
            {
                return Invalido(modeloUtilizado, latenciaMs, "Respuesta JSON vacía del modelo visual.");
            }

            string limpio = ExtraerBloqueJsonSeguro(textoJson!);
            if (limpio.Length == 0 || limpio.Length > MaximoBytesJson)
            {
                return Invalido(modeloUtilizado, latenciaMs, "Respuesta JSON ausente o excede el tamaño máximo permitido.");
            }

            try
            {
                using var doc = JsonDocument.Parse(limpio, new JsonDocumentOptions { MaxDepth = 6, AllowTrailingCommas = false });
                JsonElement raiz = doc.RootElement;
                if (raiz.ValueKind != JsonValueKind.Object)
                {
                    return Invalido(modeloUtilizado, latenciaMs, "La raíz del JSON debe ser un objeto.");
                }

                if (!raiz.TryGetProperty("hay_residuos", out JsonElement hayProp) ||
                    hayProp.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                {
                    return Invalido(modeloUtilizado, latenciaMs, "Falta el campo booleano obligatorio 'hay_residuos'.");
                }

                bool hayResiduos = hayProp.GetBoolean();
                CalidadImagenVision calidad = CalidadImagenVision.Aceptable;
                if (raiz.TryGetProperty("calidad_imagen", out JsonElement calProp))
                {
                    if (calProp.ValueKind != JsonValueKind.String ||
                        !MapaCalidad.TryGetValue(calProp.GetString() ?? "", out calidad))
                    {
                        return Invalido(modeloUtilizado, latenciaMs, "Valor inválido en 'calidad_imagen'.");
                    }
                }

                if (!raiz.TryGetProperty("objetos", out JsonElement objetosProp) || objetosProp.ValueKind != JsonValueKind.Array)
                {
                    return Invalido(modeloUtilizado, latenciaMs, "Falta el arreglo obligatorio 'objetos'.");
                }

                var objetos = new List<ObservacionObjetoVisual>();
                foreach (JsonElement item in objetosProp.EnumerateArray())
                {
                    if (objetos.Count >= MaximoObjetosPorFoto)
                        break;
                    if (item.ValueKind != JsonValueKind.Object)
                        return Invalido(modeloUtilizado, latenciaMs, "Cada elemento de 'objetos' debe ser un objeto JSON.");

                    if (!item.TryGetProperty("nombre", out JsonElement nombreProp) || nombreProp.ValueKind != JsonValueKind.String)
                        return Invalido(modeloUtilizado, latenciaMs, "Falta el campo de texto 'nombre' en un objeto.");

                    string nombre = (nombreProp.GetString() ?? string.Empty).Trim();
                    if (nombre.Length == 0 || nombre.Length > 120)
                        return Invalido(modeloUtilizado, latenciaMs, "El campo 'nombre' está vacío o excede 120 caracteres.");

                    string material = item.TryGetProperty("material", out JsonElement matProp) && matProp.ValueKind == JsonValueKind.String
                        ? (matProp.GetString() ?? "desconocido").Trim()
                        : "desconocido";

                    EstadoObservableObjeto estado = EstadoObservableObjeto.Desconocido;
                    if (item.TryGetProperty("estado", out JsonElement estProp))
                    {
                        if (estProp.ValueKind != JsonValueKind.String || !MapaEstados.TryGetValue(estProp.GetString() ?? "", out estado))
                            return Invalido(modeloUtilizado, latenciaMs, "Valor inválido en 'estado' del objeto.");
                    }

                    int cantidad = 1;
                    if (item.TryGetProperty("cantidad", out JsonElement cantProp))
                    {
                        if (cantProp.ValueKind != JsonValueKind.Number || !cantProp.TryGetInt32(out cantidad) || cantidad < 1 || cantidad > 50)
                            return Invalido(modeloUtilizado, latenciaMs, "El campo 'cantidad' debe ser un entero entre 1 y 50.");
                    }

                    decimal confianza = 0.70m;
                    if (item.TryGetProperty("confianza", out JsonElement confProp))
                    {
                        if (confProp.ValueKind != JsonValueKind.Number || !confProp.TryGetDecimal(out confianza) || confianza < 0m || confianza > 1m)
                            return Invalido(modeloUtilizado, latenciaMs, "El campo 'confianza' debe estar entre 0 y 1.");
                    }

                    bool esEtiqueta = LeerBooleanoOpcional(item, "es_etiqueta_impresa", out bool ok1);
                    bool esPantalla = LeerBooleanoOpcional(item, "es_pantalla", out bool ok2);
                    bool esReutilizable = LeerBooleanoOpcional(item, "es_reutilizable", out bool ok3);
                    bool requiereAclaracion = LeerBooleanoOpcional(item, "requiere_aclaracion", out bool ok4);
                    if (!ok1 || !ok2 || !ok3 || !ok4)
                        return Invalido(modeloUtilizado, latenciaMs, "Uno de los indicadores booleanos del objeto tiene un tipo inválido.");

                    // Nunca inventar coordenadas: solo se leen si vienen explícitamente y son válidas en [0, 1].
                    CajaDelimitadoraNormalizada? caja = null;
                    if (item.TryGetProperty("caja", out JsonElement cajaProp) && cajaProp.ValueKind == JsonValueKind.Object)
                    {
                        if (TryLeerCaja(cajaProp, out CajaDelimitadoraNormalizada? cajaLeida))
                            caja = cajaLeida;
                    }

                    objetos.Add(new ObservacionObjetoVisual(
                        nombre, material, estado, cantidad, decimal.Round(confianza, 3),
                        esEtiqueta, esPantalla, esReutilizable, requiereAclaracion, caja));
                }

                return new RespuestaVisionEstructurada(
                    JsonValido: true,
                    HayResiduos: objetos.Count > 0 || hayResiduos,
                    CalidadImagen: calidad,
                    Objetos: objetos,
                    ModeloUtilizado: modeloUtilizado,
                    LatenciaMs: latenciaMs,
                    ErrorValidacion: null,
                    JsonOriginalSanitizado: limpio);
            }
            catch (JsonException ex)
            {
                return Invalido(modeloUtilizado, latenciaMs, "JSON mal formado: " + ex.Message);
            }
        }

        private static RespuestaVisionEstructurada Invalido(string modelo, long latenciaMs, string motivo) =>
            new(false, false, CalidadImagenVision.Aceptable, Array.Empty<ObservacionObjetoVisual>(), modelo, latenciaMs, motivo, null);

        private static bool LeerBooleanoOpcional(JsonElement item, string propiedad, out bool valido)
        {
            valido = true;
            if (!item.TryGetProperty(propiedad, out JsonElement prop))
                return false;
            if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return prop.GetBoolean();
            valido = false;
            return false;
        }

        private static bool TryLeerCaja(JsonElement caja, out CajaDelimitadoraNormalizada? resultado)
        {
            resultado = null;
            if (!caja.TryGetProperty("x", out var px) || !px.TryGetDouble(out double x) ||
                !caja.TryGetProperty("y", out var py) || !py.TryGetDouble(out double y) ||
                !caja.TryGetProperty("ancho", out var pw) || !pw.TryGetDouble(out double w) ||
                !caja.TryGetProperty("alto", out var ph) || !ph.TryGetDouble(out double h))
                return false;

            if (x < 0 || y < 0 || w <= 0 || h <= 0 || x + w > 1.001 || y + h > 1.001)
                return false;

            bool verificada = caja.TryGetProperty("verificada", out var pv) && pv.ValueKind == JsonValueKind.True;
            resultado = new CajaDelimitadoraNormalizada(x, y, w, h, verificada);
            return true;
        }

        private static string ExtraerBloqueJsonSeguro(string texto)
        {
            string s = texto.Trim();
            if (s.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(7).Trim();
            else if (s.StartsWith("```", StringComparison.Ordinal))
                s = s.Substring(3).Trim();
            if (s.EndsWith("```", StringComparison.Ordinal))
                s = s.Substring(0, s.Length - 3).Trim();
            return s;
        }
    }
}
