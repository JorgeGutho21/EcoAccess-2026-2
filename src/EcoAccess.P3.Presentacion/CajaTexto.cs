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

        /// <summary>Pone el texto de ayuda en un TextBox cualquiera y lo repone si Windows vuelve a crear el control.</summary>
        internal static void Sugerir(TextBox caja, string texto)
        {
            void Aplicar() { if (caja.IsHandleCreated) SendMessage(caja.Handle, EmSetCueBanner, new IntPtr(1), texto); }
            caja.HandleCreated += (_, _) => Aplicar();
            Aplicar();
        }

        private void AplicarTextoAyuda()
        {
            if (IsHandleCreated) SendMessage(Handle, EmSetCueBanner, new IntPtr(1), _textoAyuda);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr ventana, int mensaje, IntPtr parametro, string texto);
    }
}
