using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>Configuración → Apariencia desde el menú del operario y del supervisor.</summary>
    public partial class FormPrincipal : IPanelConApariencia
    {
        public bool CambioAparienciaSolicitado { get; private set; }

        private void AbrirApariencia()
        {
            PerfilApariencia perfil = CatalogoApariencias.PerfilDe(_sesion, _area);
            string actual = CatalogoApariencias.Resolver(AlmacenApariencia.Leer().Para(_sesion), perfil).Id;
            using var selector = new FormSeleccionInterfaz(_sesion, perfil, actual);
            if (selector.ShowDialog(this) == System.Windows.Forms.DialogResult.OK && selector.Aplicado && CerrarContenido())
            {
                CambioAparienciaSolicitado = true;
                Close();
            }
        }
    }
}
