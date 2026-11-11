#pragma once
// EcoAccess P3: lógica pura de una estación con UN ESP32 y TRES tapas (dos servos por tapa).
// No depende de Arduino: se compila igual en el ESP32 y en el banco nativo (hardware/pruebas).
// Cada tapa es un "dispositivo lógico" con identidad, caneca, versión y clave propias:
// el bridge de cada caneca habla con su tapa sin cambiar el sobre HMAC existente.
#include <stddef.h>
#include <string.h>
#include <ctype.h>

static_assert(sizeof(unsigned) == 4, "El reloj necesita enteros sin signo de 32 bits");

namespace ecoaccess {

const int MaximoTapas = 3;
const int ServosPorTapa = 2;
const int MaximoServos = MaximoTapas * ServosPorTapa;

// GPIO de salida del ESP32 clásico que usa el archivo de seis servos del compañero (17, 16, 21, 23, 18, 19).
// Se rechaza cualquier otro: evita pines de arranque (0, 2, 5, 12, 15), de flash (6-11) o solo entrada (34-39).
const int PinesPermitidos[MaximoServos] = {16, 17, 18, 19, 21, 23};
const int SinPin = -1;

struct Tapa {
  const char* dispositivo;      // identidad del bridge de esta caneca (Bridge:Dispositivo)
  int idCaneca;                 // ID_CONTENEDOR de Oracle (Bridge:IdContenedor)
  int versionClave;             // Bridge:VersionClave
  const char* claveBase64;      // clave HMAC propia de esta caneca, 32 a 128 bytes
  int pin[ServosPorTapa];       // GPIO de señal; SinPin mientras el cableado no esté confirmado
  int cerrado[ServosPorTapa];   // grados
  int abierto[ServosPorTapa];   // grados
  bool calibracionVerificada;   // solo tras calibrar ESTA tapa sin carga ni tapa acoplada
};

enum class Configuracion {
  Valida, CantidadTapas, Ventana, Identidad, IdentidadRepetida, Caneca, CanecaRepetida,
  Version, Pin, PinRepetido, Angulo
};

inline bool PinPermitido(int pin) {
  for (int p : PinesPermitidos) if (p == pin) return true;
  return false;
}

inline bool IdentidadValida(const char* d) {
  if (!d) return false;
  size_t n = strlen(d);
  if (n == 0 || n > 40 || strcmp(d, "pendiente") == 0) return false;
  for (size_t i = 0; i < n; i++)
    if (!(isalnum((unsigned char)d[i]) || d[i] == '_' || d[i] == '-')) return false;
  return true;
}

// Una tapa sin pines confirmados es válida en la tabla, pero queda fuera de servicio (ver Habilitada).
inline bool PinesAsignados(const Tapa& t) { return t.pin[0] != SinPin && t.pin[1] != SinPin; }

// Valida la tabla completa. Un error estructural invalida TODA la estación: no se adivina qué tapa es cuál.
inline Configuracion Validar(const Tapa* t, int n, unsigned abiertoMs) {
  if (!t || n < 1 || n > MaximoTapas) return Configuracion::CantidadTapas;
  if (abiertoMs < 1000 || abiertoMs > 30000) return Configuracion::Ventana;
  for (int i = 0; i < n; i++) {
    if (!IdentidadValida(t[i].dispositivo)) return Configuracion::Identidad;
    if (t[i].idCaneca <= 0) return Configuracion::Caneca;
    if (t[i].versionClave <= 0) return Configuracion::Version;
    for (int k = 0; k < ServosPorTapa; k++) {
      if (t[i].pin[k] != SinPin && !PinPermitido(t[i].pin[k])) return Configuracion::Pin;
      if (t[i].cerrado[k] < 0 || t[i].cerrado[k] > 180 || t[i].abierto[k] < 0 || t[i].abierto[k] > 180)
        return Configuracion::Angulo;
    }
    if ((t[i].pin[0] == SinPin) != (t[i].pin[1] == SinPin)) return Configuracion::Pin; // media tapa: no
    if (PinesAsignados(t[i]) && t[i].pin[0] == t[i].pin[1]) return Configuracion::PinRepetido;
    for (int j = 0; j < i; j++) {
      if (strcmp(t[i].dispositivo, t[j].dispositivo) == 0) return Configuracion::IdentidadRepetida;
      if (t[i].idCaneca == t[j].idCaneca) return Configuracion::CanecaRepetida;
      for (int a = 0; a < ServosPorTapa; a++)
        for (int b = 0; b < ServosPorTapa; b++)
          if (t[i].pin[a] != SinPin && t[i].pin[a] == t[j].pin[b]) return Configuracion::PinRepetido;
    }
  }
  return Configuracion::Valida;
}

// Habilitada = puede recibir órdenes. Requiere pines confirmados y calibración propia.
inline bool Habilitada(const Tapa& t) { return PinesAsignados(t) && t.calibracionVerificada; }

// Selecciona la tapa por la identidad del sobre firmado. -1 si no existe: nunca hay tapa "por defecto".
inline int BuscarTapa(const Tapa* t, int n, const char* dispositivo) {
  if (!t || !dispositivo) return -1;
  for (int i = 0; i < n; i++) if (strcmp(t[i].dispositivo, dispositivo) == 0) return i;
  return -1;
}

// La orden debe ir a la caneca de la tapa cuya clave la firmó. Una firma válida de la blanca no abre la verde.
inline bool OrdenParaTapa(const Tapa& t, int idContenedor) { return idContenedor > 0 && idContenedor == t.idCaneca; }

// Conecta PWM de forma perezosa y SOLO para la tapa que va a moverse. Al encender no se conecta nada.
class ServosEstacion {
  bool conectado[MaximoServos] = {};
public:
  bool Preparar(const Tapa& t, int indice, bool (*adjuntar)(int)) {
    if (indice < 0 || indice >= MaximoTapas || !PinesAsignados(t)) return false;
    bool ok = true;
    for (int k = 0; k < ServosPorTapa; k++) {
      bool& c = conectado[indice * ServosPorTapa + k];
      if (!c) c = adjuntar(t.pin[k]);
      ok = ok && c;
    }
    return ok;
  }
  // Escribe exactamente los dos servos de la tapa indicada y ningún otro canal.
  static bool Posicionar(const Tapa& t, const int (&angulo)[ServosPorTapa], bool (*mover)(int, int)) {
    if (!PinesAsignados(t)) return false;
    bool a = mover(t.pin[0], angulo[0]);
    bool b = mover(t.pin[1], angulo[1]);
    return a && b;
  }
};

// Ventana del desafío (nonce) con reloj de 32 bits que da la vuelta: igual que el firmware de una tapa.
class VentanaDesafio {
  bool activo = false;
  unsigned inicio = 0;
public:
  void Iniciar(unsigned ahora) { inicio = ahora; activo = true; }
  unsigned Edad(unsigned ahora) const { return ahora - inicio; }
  void Actualizar(unsigned ahora) { if (activo && Edad(ahora) >= 5000) activo = false; }
  bool Vigente(unsigned ahora, unsigned plazo) const { return activo && plazo <= 5000 && Edad(ahora) < plazo; }
  void Consumir() { activo = false; }
};

// Claves NVS por tapa: binding y ejecución pendiente. Las reservas de órdenes y nonces son globales
// (los id de orden de Oracle son únicos para las tres canecas).
inline void ClaveNvs(char (&salida)[4], char prefijo, int indice) {
  salida[0] = prefijo; salida[1] = (char)('0' + indice); salida[2] = 0; salida[3] = 0;
}

}  // namespace ecoaccess
