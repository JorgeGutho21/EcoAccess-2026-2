using System;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Conserva la fila elegida tras recargar una tabla, para que la siguiente acción no caiga en otra fila.</summary>
    internal static class SeleccionGrid
    {
        public static void Restaurar<TFila>(DataGridView tabla, Func<TFila, bool> esLaElegida, string columnaVisible)
        {
            foreach (DataGridViewRow fila in tabla.Rows)
            {
                if (fila.DataBoundItem is TFila f && esLaElegida(f))
                {
                    tabla.CurrentCell = fila.Cells[columnaVisible];
                    return;
                }
            }

            // Si la fila ya no está (o no había ninguna elegida), no se deja la primera seleccionada en su lugar:
            // la siguiente acción caería sobre otro registro sin que la persona lo eligiera.
            tabla.ClearSelection();
            tabla.CurrentCell = null;
        }
    }
}
