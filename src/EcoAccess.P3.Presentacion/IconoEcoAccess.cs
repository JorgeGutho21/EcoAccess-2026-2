using System.Drawing;
using FontAwesome.Sharp;

namespace EcoAccess.P3.Presentacion
{

    /// <summary>Icono con propiedad explícita de su bitmap; evita retenerlo al cerrar módulos.</summary>
    public sealed class IconoEcoAccess : IconPictureBox
    {
        public IconoEcoAccess() => UseIconCache = false;

        protected override void Dispose(bool disposing)
        {
            // La biblioteca borra Image en su evento Disposed; hay que conservarla antes de llamar a base.
            Image? propia = disposing && !UseIconCache ? Image : null;
            try { base.Dispose(disposing); }
            finally { propia?.Dispose(); }
        }
    }
}
