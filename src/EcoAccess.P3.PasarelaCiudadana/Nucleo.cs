// Núcleo de la pasarela ciudadana: reglas puras (sin red ni hardware), cubiertas por pruebas unitarias.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EcoAccess.P3.PasarelaCiudadana
{
    /// <summary>
    /// Permiso presencial: un secreto aleatorio que solo existe en el QR mostrado en la estación. Se guarda únicamente su
    /// huella SHA-256; caduca con la sesión y se invalida al generar otro o al terminar la sesión.
    /// </summary>
    public sealed class PermisoPresencial
    {
        private byte[]? _huella;
        public DateTime ExpiraUtc { get; private set; } = DateTime.MinValue;
        public string? IdSesion { get; private set; }

        public string Emitir(TimeSpan duracion, DateTime ahoraUtc)
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            string permiso = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            _huella = Huella(permiso);
            ExpiraUtc = ahoraUtc + duracion;
            IdSesion = "S" + ahoraUtc.ToString("yyyyMMddHHmmss") + "-" + BitConverter.ToString(_huella, 0, 3).Replace("-", "");
            return permiso;
        }

        public void Revocar() { _huella = null; ExpiraUtc = DateTime.MinValue; IdSesion = null; }
        public bool Activo(DateTime ahoraUtc) => _huella is not null && ahoraUtc < ExpiraUtc;

        /// <summary>Valida el encabezado «Authorization: Bearer …» en tiempo constante.</summary>
        public bool Valido(string? autorizacion, DateTime ahoraUtc)
        {
            if (!Activo(ahoraUtc) || autorizacion is null || !autorizacion.StartsWith("Bearer ", StringComparison.Ordinal)) return false;
            string permiso = autorizacion.Substring(7).Trim();
            if (permiso.Length < 20 || permiso.Length > 200) return false;
            byte[] h = Huella(permiso), esperado = _huella!;
            int dif = 0; for (int i = 0; i < h.Length; i++) dif |= h[i] ^ esperado[i];
            return dif == 0;
        }

        private static byte[] Huella(string s) { using var sha = SHA256.Create(); return sha.ComputeHash(Encoding.UTF8.GetBytes(s)); }
    }

    /// <summary>Límite de aperturas: separación mínima entre órdenes y máximo por ventana deslizante.</summary>
    public sealed class Limitador
    {
        private readonly Queue<DateTime> _recientes = new();
        private readonly TimeSpan _separacion, _ventana; private readonly int _maximo;
        public Limitador(TimeSpan separacion, int maximo, TimeSpan ventana) { _separacion = separacion; _maximo = maximo; _ventana = ventana; }

        /// <summary>Devuelve null si se permite (y lo anota) o el mensaje de rechazo.</summary>
        public string? Intentar(DateTime ahoraUtc)
        {
            lock (_recientes)
            {
                while (_recientes.Count > 0 && ahoraUtc - _recientes.Peek() > _ventana) _recientes.Dequeue();
                if (_recientes.Count > 0 && ahoraUtc - _recientes.Last() < _separacion) return "Espera unos segundos antes de otra apertura.";
                if (_recientes.Count >= _maximo) return "Se alcanzó el número máximo de aperturas por ahora. Intenta en unos minutos.";
                _recientes.Enqueue(ahoraUtc); return null;
            }
        }
    }

    /// <summary>Registro de claves ya ejecutadas: un reintento o doble clic devuelve el resultado original sin repetir la orden.</summary>
    public sealed class Idempotencia<T> where T : class
    {
        private readonly Dictionary<string, T> _hechas = new(StringComparer.Ordinal);
        private readonly Queue<string> _orden = new(); private readonly int _capacidad;
        public Idempotencia(int capacidad = 5000) { _capacidad = capacidad; }
        public T? Buscar(string clave) { lock (_hechas) return _hechas.TryGetValue(clave, out var v) ? v : null; }
        public void Guardar(string clave, T valor)
        {
            lock (_hechas)
            {
                if (_hechas.ContainsKey(clave)) return;
                _hechas[clave] = valor; _orden.Enqueue(clave);
                while (_orden.Count > _capacidad) _hechas.Remove(_orden.Dequeue());
            }
        }
    }

    public sealed class Tramo { public string Caneca = ""; public int InicioMs; public int AbiertaMs; }

    public sealed class PlanApertura
    {
        public string Modo = "";          // "grupo" (firmware v2: política 5/7/10 s) o "ciclo" (firmware v1: tiempo calibrado)
        public string Orden = "";         // ruta del panel que se usará
        public string Canecas = "";
        public int MovimientoMs;
        public List<Tramo> Tramos = new();
        public int TotalMs => Tramos.Count == 0 ? 0 : Tramos.Max(t => t.InicioMs + MovimientoMs + t.AbiertaMs + MovimientoMs);
    }

    public static class Reglas
    {
        public const int MovimientoServoMs = 700;     // recorrido estimado del servo (sin sensor de posición)
        public const int CierreFirmwareMs = 1000;     // TIEMPO_CIERRE_MS del firmware de calibración
        public const int EscalonGrupoMs = 250;        // arranque escalonado del panel v2 (pico de corriente)
        private static readonly Regex Clave = new("^[A-Za-z0-9_-]{16,64}$", RegexOptions.Compiled);

        public static int DuracionPolitica(int canecas) => canecas <= 1 ? 7000 : canecas == 2 ? 14000 : 21000;

        /// <summary>Normaliza «VBN»: solo letras válidas, sin repetir, en orden V, B, N.</summary>
        public static string NormalizarCanecas(string? s) => new string("VBN".Where(c => (s ?? "").ToUpperInvariant().IndexOf(c) >= 0).ToArray());

        public static bool ClaveValida(string? c) => c is not null && Clave.IsMatch(c);

        /// <summary>Una solicitud creada hace más de 60 s (o con el reloj muy adelantado) no se ejecuta: evita reenvíos antiguos.</summary>
        public static bool Reciente(string? creadoEn, DateTime ahoraUtc)
        {
            if (!DateTime.TryParse(creadoEn, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var t)) return false;
            double s = (ahoraUtc - t).TotalSeconds;
            return s > -60 && s < 60;
        }

        /// <summary>
        /// Plan de apertura según lo que el panel anuncia. Con «grupo» (panel y firmware v2) se aplica la política 5/7/10 s.
        /// Con firmware v1 no existe apertura temporizada: una caneca usa CICLO y varias SEC (una tras otra), cada una con su
        /// tiempo calibrado en el ESP32. No se cambia ninguna calibración para forzar la política.
        /// </summary>
        public static PlanApertura Planificar(string canecas, bool grupoDisponible, IDictionary<string, int> msCalibrados)
        {
            var plan = new PlanApertura { Canecas = canecas, MovimientoMs = MovimientoServoMs };
            if (grupoDisponible)
            {
                plan.Modo = "grupo"; plan.Orden = "/api/estacion/abrir-grupo";
                int dur = DuracionPolitica(canecas.Length);
                for (int i = 0; i < canecas.Length; i++) plan.Tramos.Add(new Tramo { Caneca = canecas[i].ToString(), InicioMs = i * EscalonGrupoMs, AbiertaMs = dur });
                return plan;
            }
            plan.Modo = "ciclo";
            plan.Orden = canecas.Length == 1 ? $"/api/canecas/{canecas}/abrir" : "/api/pruebas/secuencial";
            int inicio = 0;
            foreach (char c in canecas)
            {
                int ms = msCalibrados.TryGetValue(c.ToString(), out var v) && v >= 1000 ? v : 2000;
                // El firmware cuenta el tiempo desde que aplica el PWM de apertura: la ventana útil descuenta el recorrido.
                plan.Tramos.Add(new Tramo { Caneca = c.ToString(), InicioMs = inicio, AbiertaMs = Math.Max(500, ms - MovimientoServoMs) });
                inicio += ms + CierreFirmwareMs + 150;
            }
            return plan;
        }
    }
}
