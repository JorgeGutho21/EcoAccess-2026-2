using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Quién ve la interfaz. Sale del rol ya validado por Oracle (<see cref="AreasAcceso.Elegir"/>), nunca del diseño.</summary>
    public enum PerfilApariencia
    {
        Invitado = 0,
        Ciudadano = 1,
        Operario = 2,
        Supervisor = 3
    }

    /// <summary>
    /// Un diseño seleccionable. Solo cambia presentación y navegación: todos usan los mismos servicios, la misma base Oracle,
    /// el mismo bot de Telegram y las mismas reglas de ecopuntos.
    /// </summary>
    /// <param name="Id">Identificador estable que se guarda en la preferencia.</param>
    /// <param name="Perfiles">Perfiles que pueden elegirlo.</param>
    /// <param name="EsModoEstacion">El diseño táctil configura el equipo como estación pública; no es un diseño de panel personal.</param>
    public sealed record DisenoInterfaz(string Id, string Nombre, string Descripcion, IReadOnlyList<PerfilApariencia> Perfiles,
        bool EsPredeterminado = false, bool EsModoEstacion = false)
    {
        public bool PermitidoPara(PerfilApariencia perfil) => Perfiles.Contains(perfil);
    }

    /// <summary>
    /// Diseños disponibles y reglas para elegir uno. Si la preferencia guardada no existe, está dañada o no corresponde al
    /// perfil, se usa el predeterminado: una preferencia nunca impide abrir EcoAccess ni concede permisos.
    /// </summary>
    public static class CatalogoApariencias
    {
        public const string Moderno = "moderno";
        public const string Clasico = "clasico";
        public const string Tactil = "tactil";

        private static readonly PerfilApariencia[] TodosLosPerfiles =
            { PerfilApariencia.Invitado, PerfilApariencia.Ciudadano, PerfilApariencia.Operario, PerfilApariencia.Supervisor };

        public static IReadOnlyList<DisenoInterfaz> Todos { get; } = new[]
        {
            new DisenoInterfaz(Moderno, "EcoAccess Moderno",
                "Inicio de sesión con Eco a la izquierda y panel con menú lateral, tarjetas de resumen, historial y las tres canecas.",
                TodosLosPerfiles, EsPredeterminado: true),
            new DisenoInterfaz(Clasico, "EcoAccess Clásico",
                "El diseño anterior: inicio con tarjetas de acceso y la página de exploración con acciones rápidas.",
                TodosLosPerfiles),
            new DisenoInterfaz(Tactil, "EcoAccess Táctil",
                "Pantalla pública de la estación: un botón grande «Voy a botar algo» y la caneca correcta. Configura este equipo como estación.",
                new[] { PerfilApariencia.Supervisor }, EsModoEstacion: true),
        };

        public static DisenoInterfaz Predeterminado => Todos.First(d => d.EsPredeterminado);

        /// <summary>Diseños que el perfil puede ver en el selector.</summary>
        public static IReadOnlyList<DisenoInterfaz> DisponiblesPara(PerfilApariencia perfil) => Todos.Where(d => d.PermitidoPara(perfil)).ToList();

        /// <summary>
        /// Diseño de escritorio que se abre para el perfil. El táctil nunca se devuelve aquí: el modo estación se activa con
        /// <see cref="PreferenciasApariencia.EsEstacion"/>, no como panel personal.
        /// </summary>
        public static DisenoInterfaz Resolver(string? id, PerfilApariencia perfil)
        {
            DisenoInterfaz? elegido = Todos.FirstOrDefault(d => string.Equals(d.Id, id?.Trim(), StringComparison.OrdinalIgnoreCase));
            return elegido is not null && !elegido.EsModoEstacion && elegido.PermitidoPara(perfil) ? elegido : Predeterminado;
        }

        /// <summary>Perfil a partir del área que Oracle autorizó; sin sesión es invitado.</summary>
        public static PerfilApariencia PerfilDe(SesionActiva? sesion, AreaAcceso area) => sesion is null
            ? PerfilApariencia.Invitado
            : area switch
            {
                AreaAcceso.Supervisor => PerfilApariencia.Supervisor,
                AreaAcceso.Operario => PerfilApariencia.Operario,
                _ => PerfilApariencia.Ciudadano,
            };
    }

    /// <summary>
    /// Preferencias de apariencia de esta instalación. Se guardan en un archivo de texto del perfil de Windows
    /// (%LOCALAPPDATA%\EcoAccessP3\apariencia.txt), no en Oracle: es una preferencia estética y no justifica cambiar el
    /// esquema. Formato «clave=valor» por línea; lo que no se entiende se ignora.
    /// </summary>
    public sealed class PreferenciasApariencia
    {
        private readonly Dictionary<int, string> _porUsuario = new();

        /// <summary>Diseño del inicio de sesión de este equipo (antes de saber quién entra).</summary>
        public string? Equipo { get; private set; }

        /// <summary>Diseño elegido por un invitado en este equipo.</summary>
        public string? Invitado { get; private set; }

        /// <summary>Este equipo es la pantalla táctil de la estación: abre directamente el modo táctil. Solo lo cambia un supervisor.</summary>
        public bool EsEstacion { get; private set; }

        public IReadOnlyDictionary<int, string> PorUsuario => _porUsuario;

        public string? DeUsuario(int idUsuario) => _porUsuario.TryGetValue(idUsuario, out string? id) ? id : null;

        /// <summary>Preferencia del perfil: la de la cuenta, o la del invitado; el diseño se valida después con <see cref="CatalogoApariencias.Resolver"/>.</summary>
        public string? Para(SesionActiva? sesion) => sesion is null ? Invitado : DeUsuario(sesion.IdUsuario);

        /// <summary>Guarda el diseño elegido. También pasa a ser el del inicio de sesión de este equipo.</summary>
        public void Elegir(SesionActiva? sesion, string idDiseno)
        {
            if (Compat.IsNullOrWhiteSpace(idDiseno) || !EsIdValido(idDiseno)) throw new ArgumentException("Diseño no válido.", nameof(idDiseno));
            string id = idDiseno.Trim().ToLowerInvariant();
            if (sesion is null) Invitado = id; else _porUsuario[sesion.IdUsuario] = id;
            Equipo = id;
        }

        /// <summary>Vuelve al diseño predeterminado: quita la preferencia de la cuenta (o del invitado) y la del inicio de sesión.</summary>
        public void Restablecer(SesionActiva? sesion)
        {
            if (sesion is null) Invitado = null; else _porUsuario.Remove(sesion.IdUsuario);
            Equipo = null;
        }

        /// <summary>Activa o desactiva el modo estación. Solo el supervisor validado por Oracle puede hacerlo.</summary>
        public bool CambiarModoEstacion(PerfilApariencia perfil, bool activar)
        {
            if (perfil != PerfilApariencia.Supervisor) return false;
            EsEstacion = activar;
            return true;
        }

        public static PreferenciasApariencia Leer(string? texto)
        {
            var preferencias = new PreferenciasApariencia();
            if (texto is null) return preferencias;
            foreach (string linea in texto.Split('\n'))
            {
                int igual = linea.IndexOf('=');
                if (igual <= 0) continue;
                string clave = linea.Substring(0, igual).Trim().ToLowerInvariant();
                string valor = linea.Substring(igual + 1).Trim();
                if (valor.Length == 0) continue;
                if (clave == "equipo" && EsIdValido(valor)) preferencias.Equipo = valor.ToLowerInvariant();
                else if (clave == "invitado" && EsIdValido(valor)) preferencias.Invitado = valor.ToLowerInvariant();
                else if (clave == "estacion") preferencias.EsEstacion = valor == "1" || valor.Equals("si", StringComparison.OrdinalIgnoreCase);
                else if (clave.StartsWith("usuario.", StringComparison.Ordinal)
                    && int.TryParse(clave.Substring(8), NumberStyles.None, CultureInfo.InvariantCulture, out int id) && id > 0 && EsIdValido(valor))
                    preferencias._porUsuario[id] = valor.ToLowerInvariant();
            }
            return preferencias;
        }

        public string Escribir()
        {
            var texto = new StringBuilder("# Preferencias de apariencia de EcoAccess P3 (solo presentación; no contiene datos de Oracle).\n");
            if (Equipo is not null) texto.Append("equipo=").Append(Equipo).Append('\n');
            if (Invitado is not null) texto.Append("invitado=").Append(Invitado).Append('\n');
            texto.Append("estacion=").Append(EsEstacion ? "1" : "0").Append('\n');
            foreach (var par in _porUsuario.OrderBy(p => p.Key))
                texto.Append("usuario.").Append(par.Key.ToString(CultureInfo.InvariantCulture)).Append('=').Append(par.Value).Append('\n');
            return texto.ToString();
        }

        // Solo letras, dígitos y guiones: un archivo editado a mano no puede inyectar nada en la interfaz.
        private static bool EsIdValido(string valor) =>
            valor.Trim().Length is > 0 and <= 40 && valor.Trim().All(c => c < 128 && (char.IsLetterOrDigit(c) || c == '-' || c == '_'));
    }
}
