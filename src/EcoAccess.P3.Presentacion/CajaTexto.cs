using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>TextBox con texto de ayuda nativo de Windows (la propiedad no existe en .NET Framework 4.8).</summary>
    internal sealed class CajaTexto : TextBox
    {
        private const int EmSetCueBanner = 0x1501;
        private string _textoAyuda = string.Empty;

        public string PlaceholderText
        {
            get => _textoAyuda;
            set
            {
                _textoAyuda = value ?? string.Empty;
                AplicarTextoAyuda();
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AplicarTextoAyuda();
        }

        private void AplicarTextoAyuda()
        {
            if (IsHandleCreated) SendMessage(Handle, EmSetCueBanner, new IntPtr(1), _textoAyuda);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr parametro, string texto);
    }
}
