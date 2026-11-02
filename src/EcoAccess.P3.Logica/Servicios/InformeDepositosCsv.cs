using System;
using System.Linq;
using System.Globalization;
using System.Text;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Representación acotada de una página ya autorizada; nunca consulta datos ni calcula reciclaje.</summary>
    public static class InformeDepositosCsv
    {
        public static string Generar(FiltroEstadisticas filtro, PaginaDepositos pagina)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            Compat.ThrowIfNull(pagina, nameof(pagina));
            var salida = new StringBuilder("desde_utc;hasta_utc;id_caneca_filtro;id_material_filtro;id_usuario_filtro;verificacion_filtro;criterio_material;pagina;tamano_pagina;total_resultados;id_deposito;detectado_utc;id_caneca;caneca;id_usuario;material_declarado;material_verificado;peso_medido_kg;peso_estimado_kg\r\n");
            foreach (var fila in pagina.Filas)
            {
                string?[] campos = new string?[]
    {
        filtro.DesdeUtc.ToString("O"),
        filtro.HastaUtc.ToString("O"),
        filtro.IdContenedor?.ToString(CultureInfo.InvariantCulture),
        filtro.IdMaterial?.ToString(CultureInfo.InvariantCulture),
        filtro.IdUsuario?.ToString(CultureInfo.InvariantCulture),
        filtro.Verificacion.ToString(),
        filtro.Criterio.ToString(),
        Numero(pagina.Pagina),
        Numero(pagina.TamanoPagina),
        Numero(pagina.Total),
        Numero(fila.IdDeposito),
        fila.FechaDeteccionUtc.ToString("O"),
        Numero(fila.IdContenedor),
        fila.Contenedor,
        fila.IdUsuario is { } usuario ? Numero(usuario) : null,
        fila.MaterialDeclarado,
        fila.MaterialVerificado,
        fila.PesoMedidoKg?.ToString(CultureInfo.InvariantCulture),
        fila.PesoEstimadoKg?.ToString(CultureInfo.InvariantCulture)
    };
                salida.AppendJoin(';', campos.Select(Celda)).Append("\r\n");
            }
            return salida.ToString();
        }

        private static string Numero(int valor) => valor.ToString(CultureInfo.InvariantCulture);
        private static string Celda(string? valor)
        {
            string texto = valor ?? string.Empty;
            // Las comillas CSV por sí solas no impiden que una hoja de cálculo interprete una fórmula.
            int inicio = 0;
            while (inicio < texto.Length && (char.IsWhiteSpace(texto[inicio]) || char.IsControl(texto[inicio])
                || texto[inicio] is '\uFEFF' or '\u200B')) inicio++;
            if (inicio < texto.Length && texto[inicio] is '=' or '+' or '-' or '@') texto = "'" + texto;
            return "\"" + texto.Replace("\"", "\"\"") + "\"";
        }
    }
}
