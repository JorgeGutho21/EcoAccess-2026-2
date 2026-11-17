using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Mantiene estable el contenido de una ventana al minimizar, restaurar, maximizar o cambiar de tamaño (misión maestra,
    /// 14/11/2026). El arnés de ventanas reprodujo dos causas raíz en la ventana principal real:
    /// <list type="number">
    /// <item>Paneles con <c>AutoSize</c> en el modo por defecto <see cref="AutoSizeMode.GrowOnly"/>: al estrechar o minimizar la
    /// ventana el texto se reparte en más líneas y el panel crece, pero nunca vuelve a encoger. Quedaban huecos y el contenido
    /// corrido hacia abajo (dashboard: 2 093 → 2 569 px de alto tras minimizar y restaurar).</item>
    /// <item>Al restaurar, Windows devuelve el foco al último control y el contenedor desplazable salta hasta él (catálogo de
    /// materiales: el contenido quedaba 276 px más arriba).</item>
    /// </list>
    /// No cambia datos ni permisos: solo la geometría.
    /// </summary>
    internal sealed class EstabilizadorVentana
    {
        private readonly Form _ventana;
        private readonly Control _contenido;
        private readonly Dictionary<ScrollableControl, Point> _posiciones = new();
        private FormWindowState _anterior;

        internal EstabilizadorVentana(Form ventana, Control contenido)
        {
            _ventana = ventana ?? throw new ArgumentNullException(nameof(ventana));
            _contenido = contenido ?? throw new ArgumentNullException(nameof(contenido));
            _anterior = ventana.WindowState;
            ventana.Resize += (_, _) => AlCambiarEstado();
        }

        /// <summary>Se llama cada vez que se monta un módulo: los paneles auto-ajustables pasan a crecer y encoger.</summary>
        internal static int PermitirEncoger(Control raiz)
        {
            int cambiados = 0;
            foreach (Panel panel in Descendientes(raiz).OfType<Panel>())
                if (panel.AutoSize && panel.AutoSizeMode == AutoSizeMode.GrowOnly) { panel.AutoSizeMode = AutoSizeMode.GrowAndShrink; cambiados++; }
            return cambiados;
        }

        private void AlCambiarEstado()
        {
            FormWindowState actual = _ventana.WindowState;
            if (actual == _anterior) return;
            if (actual == FormWindowState.Minimized) Recordar();
            else if (_anterior == FormWindowState.Minimized)
                // Después de que Windows devuelva el foco y el diseño se recalcule.
                _ventana.BeginInvoke(new Action(Restablecer));
            _anterior = actual;
        }

        private void Recordar()
        {
            _posiciones.Clear();
            foreach (ScrollableControl c in Descendientes(_contenido).OfType<ScrollableControl>().Prepend(_contenido as ScrollableControl).OfType<ScrollableControl>())
                if (c.AutoScroll && !c.IsDisposed) _posiciones[c] = new Point(-c.AutoScrollPosition.X, -c.AutoScrollPosition.Y);
        }

        private void Restablecer()
        {
            if (_ventana.IsDisposed) return;
            foreach (var par in _posiciones)
                if (!par.Key.IsDisposed && par.Key.AutoScrollPosition != new Point(-par.Value.X, -par.Value.Y)) par.Key.AutoScrollPosition = par.Value;
            _posiciones.Clear();
        }

        internal static IEnumerable<Control> Descendientes(Control c)
        {
            foreach (Control h in c.Controls) { yield return h; foreach (Control n in Descendientes(h)) yield return n; }
        }

        [DllImport("user32.dll")] private static extern short GetKeyState(int tecla);

        /// <summary>
        /// Un contenedor desplazable solo sigue al foco cuando la persona navega con Tab (accesibilidad). Al hacer clic, cambiar de
        /// pestaña o restaurar la ventana, se queda donde estaba.
        /// </summary>
        internal static bool NavegandoConTeclado() => (GetKeyState(0x09) & 0x8000) != 0;
    }
}
