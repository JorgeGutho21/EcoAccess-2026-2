using System;
using System.IO;
using System.Text;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Presentacion
{
    /// <summary>
    /// Guarda las preferencias de apariencia en %LOCALAPPDATA%\EcoAccessP3\apariencia.txt. Si el archivo falta, está dañado o
    /// no se puede leer, se usan los diseños predeterminados: la apariencia nunca impide abrir EcoAccess.
    /// </summary>
    public static class AlmacenApariencia
    {
        public static string Ruta { get; set; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EcoAccessP3", "apariencia.txt");

        public static PreferenciasApariencia Leer()
        {
            try { return PreferenciasApariencia.Leer(File.Exists(Ruta) ? File.ReadAllText(Ruta, Encoding.UTF8) : null); }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return new PreferenciasApariencia(); }
        }

        /// <summary>Devuelve false si no se pudo guardar; el diseño se aplica igual en esta sesión.</summary>
        public static bool Guardar(PreferenciasApariencia preferencias)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Ruta)!);
                File.WriteAllText(Ruta, preferencias.Escribir(), new UTF8Encoding(false));
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
        }
    }

    /// <summary>
    /// Contrato común de las ventanas de inicio de sesión (Moderno y Clásico): ambas usan
    /// <see cref="ServicioAutenticacion.IniciarSesionPublicaAsync"/> y <see cref="AreasAcceso.Elegir"/>, así que el área
    /// siempre la valida Oracle y no el diseño.
    /// </summary>
    public interface IVentanaAcceso : IDisposable
    {
        SesionActiva? Sesion { get; }
        bool AccesoInvitado { get; }
        AreaAcceso AreaElegida { get; }
        System.Windows.Forms.DialogResult ShowDialog();
    }

    /// <summary>Lo implementan los paneles que permiten abrir Configuración → Apariencia y volver con otro diseño.</summary>
    public interface IPanelConApariencia
    {
        /// <summary>El usuario aplicó otro diseño: el programa reabre el panel con la misma sesión, sin pedir la contraseña.</summary>
        bool CambioAparienciaSolicitado { get; }
    }
}
