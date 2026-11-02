using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed partial class RepositorioMaterialesOracle
    {
        public async Task<PaginaMateriales> ConsultarPaginaAsync(FiltroMateriales filtro, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            // El servicio elige visibilidad pública; aquí solo se traduce el filtro a SQL parametrizado.
            var condiciones = new List<string>();
            var parametros = new List<(string Nombre, string Valor)>();
            if (filtro.SoloPublicos)
            {
                foreach (string prefijo in FiltroMateriales.PrefijosCodigoPrueba)
                {
                    string nombre = "pc" + parametros.Count;
                    condiciones.Add($"INSTR(LOWER(codigo),:{nombre})<>1"); parametros.Add((nombre, prefijo.ToLowerInvariant()));
                }
                foreach (string prefijo in FiltroMateriales.PrefijosNombrePrueba)
                {
                    string nombre = "pn" + parametros.Count;
                    condiciones.Add($"INSTR(LOWER(nombre),:{nombre})<>1"); parametros.Add((nombre, prefijo));
                }
                foreach (string palabra in filtro.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal))
                {
                    string nombre = "t" + parametros.Count;
                    condiciones.Add($"(INSTR(LOWER(codigo),:{nombre})>0 OR INSTR(TRANSLATE(LOWER(nombre),'áéíóúüñ','aeiouun'),:{nombre})>0 OR INSTR(TRANSLATE(LOWER(sinonimos),'áéíóúüñ','aeiouun'),:{nombre})>0)");
                    parametros.Add((nombre, palabra));
                }
            }
            string visible = condiciones.Count == 0 ? "1=1" : string.Join(" AND ", condiciones);
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion, $@"SELECT {Columnas} FROM material
WHERE (:texto IS NULL OR INSTR(LOWER(codigo), :texto)>0 OR INSTR(LOWER(nombre), :texto)>0)
  AND ({visible})
  AND (:habilitado IS NULL OR habilitado=:habilitado)
  AND (:categoria IS NULL OR categoria=:categoria)
ORDER BY NLSSORT(codigo, 'NLS_SORT=BINARY'), id_material
OFFSET :desplazamiento ROWS FETCH NEXT :limite ROWS ONLY");
            comando.Agregar("texto", OracleDbType.Varchar2, filtro.Texto.Length == 0 || filtro.SoloPublicos ? DBNull.Value : filtro.Texto);
            foreach (var p in parametros) comando.Agregar(p.Nombre, OracleDbType.Varchar2, p.Valor);
            comando.Agregar("habilitado", OracleDbType.Int32, filtro.Habilitado is bool habilitado ? (habilitado ? 1 : 0) : DBNull.Value);
            comando.Agregar("categoria", OracleDbType.Int32, filtro.Categoria is CategoriaMaterial categoria ? (int)categoria : DBNull.Value);
            comando.Agregar("desplazamiento", OracleDbType.Int64, filtro.Desplazamiento);
            comando.Agregar("limite", OracleDbType.Int32, filtro.TamanoPagina + 1);
            var filas = new List<Material>();
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false)) filas.Add(Leer(lector));
            bool siguiente = filas.Count > filtro.TamanoPagina;
            if (siguiente) filas.RemoveAt(filas.Count - 1);
            return new PaginaMateriales(filas, filtro.Pagina, filtro.TamanoPagina, siguiente);
        }
    }
}
