#include "../servosecoacces/EstadoActuador.h"
static int conexiones18 = 0, conexiones19 = 0;
static bool adjuntar(int pin) {
  int& cuenta = pin == 18 ? conexiones18 : conexiones19;
  return ++cuenta == 1; // Como LEDC real: una segunda conexión del mismo pin falla.
}
int main() {
  ConexionServos servos;
  if (!servos.Preparar(adjuntar) || !servos.Preparar(adjuntar)) return 1;
  if (conexiones18 != 1 || conexiones19 != 1) return 2;
  VentanaDesafio ventana;
  ventana.Iniciar(0xfffffff0U);
  if (!ventana.Vigente(0x00000010U,1000)) return 3; // Cruce normal de wrap, 32 ms.
  ventana.Actualizar(0x00002000U);
  if (ventana.Vigente(0x00002000U,5000)) return 4;
  if (ventana.Vigente(0x00000010U,5000)) return 5; // Otro ciclo del reloj no revive nonce invalidado.
  ventana.Iniciar(100); ventana.Consumir();
  if (ventana.Vigente(101,5000)) return 6;
  ventana.Iniciar(100);
  if (ventana.Vigente(5100,5000)) return 7; // Instante exacto vencido.
  return 0;
}
