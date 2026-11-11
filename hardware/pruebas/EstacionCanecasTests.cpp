// Banco nativo de la lógica de la estación de tres tapas. Sin Arduino: g++ -std=c++17.
// Devuelve 0 si todo pasa; otro número identifica la comprobación que falló.
#include "../estacion_tres_canecas/EstacionCanecas.h"
#include <vector>
#include <utility>
using namespace ecoaccess;

static std::vector<std::pair<int, int>> escrituras;  // (pin, ángulo)
static std::vector<int> conexiones;
static bool mover(int pin, int angulo) { escrituras.push_back({pin, angulo}); return true; }
static bool adjuntar(int pin) { for (int p : conexiones) if (p == pin) return false; conexiones.push_back(pin); return true; }
static bool soloPines(int a, int b) { for (auto& e : escrituras) if (e.first != a && e.first != b) return false; return !escrituras.empty(); }

static Tapa T(const char* d, int caneca, int p0, int p1, bool calibrada = true) {
  return Tapa{d, caneca, 1, "", {p0, p1}, {0, 0}, {90, 90}, calibrada};
}

int main() {
  // Pares de ejemplo SOLO para la prueba. El mapeo real se confirma en el banco físico.
  Tapa est[3] = {T("esp32-piloto-blanca", 1093, 17, 16), T("esp32-piloto-verde", 1400, 21, 23), T("esp32-piloto-negra", 1401, 18, 19)};
  if (Validar(est, 3, 10000) != Configuracion::Valida) return 1;

  // Selección por identidad firmada y caneca de la orden.
  if (BuscarTapa(est, 3, "esp32-piloto-blanca") != 0 || BuscarTapa(est, 3, "esp32-piloto-verde") != 1
      || BuscarTapa(est, 3, "esp32-piloto-negra") != 2) return 2;
  if (BuscarTapa(est, 3, "esp32-piloto-gris") != -1 || BuscarTapa(est, 3, "") != -1 || BuscarTapa(est, 3, nullptr) != -1) return 3;
  if (!OrdenParaTapa(est[0], 1093) || OrdenParaTapa(est[0], 1400) || OrdenParaTapa(est[0], 1401) || OrdenParaTapa(est[0], 0)) return 4;

  // Apertura exclusiva: cada tapa escribe solo sus dos canales, y en ambos sentidos.
  for (int t = 0; t < 3; t++) {
    escrituras.clear();
    ServosEstacion s;
    if (!s.Preparar(est[t], t, adjuntar)) return 10 + t;
    if (!ServosEstacion::Posicionar(est[t], est[t].abierto, mover)) return 13 + t;
    if (!ServosEstacion::Posicionar(est[t], est[t].cerrado, mover)) return 16 + t;
    if (!soloPines(est[t].pin[0], est[t].pin[1]) || escrituras.size() != 4) return 19 + t;
  }

  // PWM perezoso: preparar dos veces no reconecta (como LEDC real, que falla en la segunda conexión).
  conexiones.clear();
  ServosEstacion perezoso;
  if (!perezoso.Preparar(est[0], 0, adjuntar) || !perezoso.Preparar(est[0], 0, adjuntar) || conexiones.size() != 2) return 25;
  if (!perezoso.Preparar(est[2], 2, adjuntar) || conexiones.size() != 4) return 26;  // la verde sigue sin PWM
  for (int p : conexiones) if (p == 21 || p == 23) return 27;

  // Configuración incoherente: invalida la estación completa.
  Tapa mal[3] = {est[0], est[1], est[2]};
  mal[1].pin[0] = 17;                                   // pin compartido con la blanca
  if (Validar(mal, 3, 10000) != Configuracion::PinRepetido) return 30;
  mal[1] = est[1]; mal[1].idCaneca = 1093;              // dos tapas para la misma caneca
  if (Validar(mal, 3, 10000) != Configuracion::CanecaRepetida) return 31;
  mal[1] = est[1]; mal[1].dispositivo = "esp32-piloto-blanca";
  if (Validar(mal, 3, 10000) != Configuracion::IdentidadRepetida) return 32;
  mal[1] = est[1]; mal[1].pin[1] = 2;                   // pin de arranque
  if (Validar(mal, 3, 10000) != Configuracion::Pin) return 33;
  mal[1] = est[1]; mal[1].pin[1] = 21;                  // los dos servos en el mismo pin
  if (Validar(mal, 3, 10000) != Configuracion::PinRepetido) return 34;
  mal[1] = est[1]; mal[1].abierto[0] = 200;
  if (Validar(mal, 3, 10000) != Configuracion::Angulo) return 35;
  mal[1] = est[1]; mal[1].dispositivo = "pendiente";
  if (Validar(mal, 3, 10000) != Configuracion::Identidad) return 36;
  if (Validar(est, 3, 999) != Configuracion::Ventana || Validar(est, 3, 30001) != Configuracion::Ventana) return 37;
  if (Validar(est, 0, 10000) != Configuracion::CantidadTapas || Validar(est, 4, 10000) != Configuracion::CantidadTapas) return 38;
  mal[1] = est[1]; mal[1].pin[1] = SinPin;              // media tapa
  if (Validar(mal, 3, 10000) != Configuracion::Pin) return 39;

  // Tapa sin pines confirmados o sin calibrar: tabla válida, tapa fuera de servicio y sin movimiento.
  Tapa pendientes[3] = {est[0], T("esp32-piloto-verde", 1400, SinPin, SinPin), T("esp32-piloto-negra", 1401, 18, 19, false)};
  if (Validar(pendientes, 3, 10000) != Configuracion::Valida) return 40;
  if (!Habilitada(pendientes[0]) || Habilitada(pendientes[1]) || Habilitada(pendientes[2])) return 41;
  escrituras.clear();
  ServosEstacion sinPines;
  if (sinPines.Preparar(pendientes[1], 1, adjuntar) || ServosEstacion::Posicionar(pendientes[1], pendientes[1].abierto, mover)) return 42;
  if (!escrituras.empty()) return 43;

  // Ventana del desafío: wrap del reloj, consumo y vencimiento exacto.
  VentanaDesafio v;
  v.Iniciar(0xfffffff0U);
  if (!v.Vigente(0x00000010U, 1000)) return 50;
  v.Actualizar(0x00002000U);
  if (v.Vigente(0x00002000U, 5000) || v.Vigente(0x00000010U, 5000)) return 51;
  v.Iniciar(100); v.Consumir();
  if (v.Vigente(101, 5000)) return 52;
  v.Iniciar(100);
  if (v.Vigente(5100, 5000) || v.Vigente(200, 5001)) return 53;

  char k[4]; ClaveNvs(k, 'p', 2);
  if (k[0] != 'p' || k[1] != '2' || k[2] != 0) return 60;
  return 0;
}
