using System;
using System.Collections.Generic;
using System.Linq;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Caneca física de un compartimiento. Su QR fijo y público contiene solo el
    /// <see cref="IdentificadorQr"/>: identifica la caneca e inicia el bot, pero no autentica a nadie ni
    /// autoriza abrir (RN-04). La entidad protege su estado y su lista de materiales admitidos;
    /// decidir si una persona concreta puede abrirla, y si un material sigue habilitado en el catálogo,
    /// es trabajo de la capa lógica, que consulta la base de datos en el momento de la operación.
    /// </summary>
    public class Contenedor
    {
        public const int LongitudMinimaNombre = 2;
        public const int LongitudMaximaNombre = 100;

        /// <summary>
        /// Mínimo para que el identificador sea opaco si se genera al azar: 16 caracteres de un alfabeto de 64
        /// símbolos dan 2^96 combinaciones. La entidad solo valida el formato; generar un valor aleatorio de verdad
        /// es responsabilidad de la lógica.
        /// </summary>
        public const int LongitudMinimaIdentificadorQr = 16;

        /// <summary>Límite del parámetro start de los enlaces de Telegram (core.telegram.org/bots/features).</summary>
        public const int LongitudMaximaIdentificadorQr = 64;

        private readonly List<int> _materialesAdmitidos = new();

        /// <summary>0 mientras la caneca no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public int IdPuntoInstalacion { get; }
        public string Nombre { get; }

        /// <summary>Texto opaco que va en el enlace t.me/bot?start=...; distingue mayúsculas de minúsculas.</summary>
        public string IdentificadorQr { get; }
        public DateTime FechaAltaUtc { get; }
        public EstadoContenedor Estado { get; private set; }

        /// <summary>Identificadores de los materiales que esta caneca recibe; vista de solo lectura.</summary>
        public IReadOnlyCollection<int> MaterialesAdmitidos { get; }

        public bool EsNuevo => Id == 0;
        public bool EstaEnServicio => Estado == EstadoContenedor.EnServicio;

        /// <summary>
        /// Registra una caneca nueva. Empieza fuera de servicio (seguridad por defecto): primero se configuran
        /// sus materiales y después se pone en servicio de forma explícita.
        /// </summary>
        public Contenedor(int idPuntoInstalacion, string nombre, string identificadorQr, DateTime fechaAltaUtc)
        {
            IdPuntoInstalacion = Validacion.IdPositivo(idPuntoInstalacion, nameof(idPuntoInstalacion));
            Nombre = Validacion.TextoRequerido(nombre, nameof(nombre), LongitudMaximaNombre, LongitudMinimaNombre);
            IdentificadorQr = ValidarIdentificadorQr(identificadorQr, nameof(identificadorQr));
            FechaAltaUtc = Validacion.FechaUtc(fechaAltaUtc, nameof(fechaAltaUtc));
            Estado = EstadoContenedor.FueraDeServicio;
            MaterialesAdmitidos = _materialesAdmitidos.AsReadOnly();
        }

        /// <summary>
        /// Reconstruye una caneca guardada. Rechaza combinaciones que la propia clase nunca permitiría,
        /// como una caneca en servicio sin materiales admitidos. Copia la lista recibida, así cambios
        /// posteriores en esa lista no alteran la caneca.
        /// </summary>
        public Contenedor(
            int id,
            int idPuntoInstalacion,
            string nombre,
            string identificadorQr,
            DateTime fechaAltaUtc,
            EstadoContenedor estado,
            IEnumerable<int> materialesAdmitidos)
            : this(idPuntoInstalacion, nombre, identificadorQr, fechaAltaUtc)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            Estado = Validacion.EnumDefinido(estado, nameof(estado));
            _materialesAdmitidos.AddRange(ValidarMaterialesGuardados(materialesAdmitidos));

            if (Estado == EstadoContenedor.EnServicio && _materialesAdmitidos.Count == 0)
            {
                throw new ArgumentException("Una caneca en servicio debe admitir al menos un material.", nameof(materialesAdmitidos));
            }
        }

        public bool AdmiteMaterial(int idMaterial) => _materialesAdmitidos.Contains(idMaterial);

        /// <summary>
        /// Agrega un material guardado a la lista de admitidos. Antes de llamarlo, el servicio de la lógica
        /// comprueba con el repositorio que el material exista y esté habilitado.
        /// </summary>
        public void AdmitirMaterial(int idMaterial)
        {
            Validacion.IdPositivo(idMaterial, nameof(idMaterial));
            ExigirQueNoEsteDadoDeBaja();

            if (AdmiteMaterial(idMaterial))
            {
                throw new InvalidOperationException("La caneca ya admite ese material.");
            }

            _materialesAdmitidos.Add(idMaterial);
        }

        public void QuitarMaterialAdmitido(int idMaterial)
        {
            ExigirQueNoEsteDadoDeBaja();

            if (!AdmiteMaterial(idMaterial))
            {
                throw new InvalidOperationException("La caneca no admite ese material.");
            }

            if (EstaEnServicio && _materialesAdmitidos.Count == 1)
            {
                throw new InvalidOperationException(
                    "No se puede quitar el último material de una caneca en servicio; póngala fuera de servicio primero.");
            }

            _materialesAdmitidos.Remove(idMaterial);
        }

        public void PonerEnServicio()
        {
            ExigirQueNoEsteDadoDeBaja();

            if (_materialesAdmitidos.Count == 0)
            {
                throw new InvalidOperationException("La caneca no admite ningún material todavía; no puede ponerse en servicio.");
            }

            Estado = EstadoContenedor.EnServicio;
        }

        public void PonerFueraDeServicio()
        {
            ExigirQueNoEsteDadoDeBaja();
            Estado = EstadoContenedor.FueraDeServicio;
        }

        /// <summary>Retira la caneca de forma definitiva. Repetirlo no es un error.</summary>
        public void DarDeBaja() => Estado = EstadoContenedor.DadoDeBaja;

        private void ExigirQueNoEsteDadoDeBaja()
        {
            if (Estado == EstadoContenedor.DadoDeBaja)
            {
                throw new InvalidOperationException("La caneca fue dada de baja; ya no se puede modificar.");
            }
        }

        private static List<int> ValidarMaterialesGuardados(IEnumerable<int> materialesAdmitidos)
        {
            Compat.ThrowIfNull(materialesAdmitidos, nameof(materialesAdmitidos));

            var lista = new List<int>(materialesAdmitidos);
            foreach (int idMaterial in lista)
            {
                Validacion.IdPositivo(idMaterial, nameof(materialesAdmitidos));
            }

            if (lista.Distinct().Count() != lista.Count)
            {
                throw new ArgumentException("La lista de materiales admitidos tiene valores repetidos.", nameof(materialesAdmitidos));
            }

            return lista;
        }

        private static string ValidarIdentificadorQr(string? identificador, string nombreParametro)
        {
            // Sin Trim: el identificador es un dato técnico y un espacio indicaría que se copió mal.
            bool valido = identificador is not null
                && identificador.Length >= LongitudMinimaIdentificadorQr
                && identificador.Length <= LongitudMaximaIdentificadorQr
                && identificador.All(EsCaracterPermitidoPorTelegram);

            if (!valido)
            {
                throw new ArgumentException(
                    $"El identificador del QR debe tener entre {LongitudMinimaIdentificadorQr} y {LongitudMaximaIdentificadorQr} " +
                    "caracteres y solo letras sin tilde, números, guion o guion bajo (formato de Telegram).", nombreParametro);
            }

            return identificador!;
        }

        // Se comparan rangos ASCII explícitos: char.IsLetterOrDigit aceptaría «ñ» o dígitos árabes, que Telegram no admite.
        private static bool EsCaracterPermitidoPorTelegram(char c) =>
            c is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-';
    }
}
