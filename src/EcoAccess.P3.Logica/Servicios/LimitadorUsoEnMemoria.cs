using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Ventana fija por clave guardada en memoria del proceso. Basta para un solo bot; si el bot se reinicia, los
    /// contadores vuelven a cero (límite aceptado en P-10 hasta definir cupos definitivos). Las claves de la página
    /// de registro las elige el cliente, así que las ventanas vencidas se podan cuando hay demasiadas (11.4).
    /// </summary>
    public sealed class LimitadorUsoEnMemoria : ILimitadorUso
    {
        /// <summary>Por encima de este número de claves se descartan las ventanas ya vencidas.</summary>
        internal const int ClavesAntesDePodar = 10_000;

        private readonly ConcurrentDictionary<string, Ventana> _ventanas = new(StringComparer.Ordinal);
        private int _llamadas;
        private readonly int _maximo;
        private readonly TimeSpan _duracion;

        public LimitadorUsoEnMemoria(int maximoPorVentana, TimeSpan duracionVentana)
        {
            if (maximoPorVentana <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximoPorVentana), "El máximo debe ser positivo.");
            }

            if (duracionVentana <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(duracionVentana), "La ventana debe ser positiva.");
            }

            _maximo = maximoPorVentana;
            _duracion = duracionVentana;
        }

        public bool Permitir(string clave, DateTime ahoraUtc)
        {
            Compat.ThrowIfNullOrWhiteSpace(clave, nameof(clave));

            // Count bloquea todo el diccionario: se consulta solo una de cada 256 llamadas.
            if ((Interlocked.Increment(ref _llamadas) & 0xFF) == 0 && _ventanas.Count > ClavesAntesDePodar)
            {
                Podar(ahoraUtc);
            }

            var ventana = _ventanas.GetOrAdd(clave, _ => new Ventana(ahoraUtc));
            lock (ventana)
            {
                if (ahoraUtc - ventana.Inicio >= _duracion)
                {
                    ventana.Inicio = ahoraUtc;
                    ventana.Usos = 0;
                }

                if (ventana.Usos >= _maximo)
                {
                    return false;
                }

                ventana.Usos++;
                return true;
            }
        }

        internal int Claves => _ventanas.Count;

        private void Podar(DateTime ahoraUtc)
        {
            foreach (KeyValuePair<string, Ventana> par in _ventanas)
            {
                bool vencida;
                lock (par.Value)
                {
                    vencida = ahoraUtc - par.Value.Inicio >= _duracion;
                }

                // Una ventana vencida equivale a una nueva: quitarla no cambia ninguna decisión futura.
                if (vencida)
                {
                    _ventanas.TryRemove(par);
                }
            }
        }

        private sealed class Ventana
        {
            public Ventana(DateTime inicio)
            {
                Inicio = inicio;
            }

            public DateTime Inicio { get; set; }

            public int Usos { get; set; }
        }
    }
}
