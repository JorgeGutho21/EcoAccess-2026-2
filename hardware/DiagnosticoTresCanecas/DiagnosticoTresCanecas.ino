// EcoAccess P3 · DIAGNÓSTICO TEMPORAL de los tres servos de la estación (no es el firmware de producción).
// Sin HMAC, sin Oracle, sin red: solo para aislar señal, alimentación y mecánica. Al terminar se vuelve a cargar
// hardware/estacion_tres_canecas. No mueve nada al encender.
//
// Monitor serie a 115200 baudios. Comandos (una letra + Enter):
//   V / B / N  abre 90° la verde (GPIO 23) / blanca (GPIO 19) / negra (GPIO 18) durante 10 s y la cierra a 0°
//   T          las tres, una después de otra
//   1 / 2 / 3  igual que V / B / N pero en barrido lento (pide menos corriente de golpe: separa alimentación de señal)
//   X          cancela: cierra de inmediato la que esté abierta
//   E          estado: pines, canales PWM conectados y motivo del último reinicio
//   ?          ayuda
// Tras cada prueba se suelta el PWM (ledcDetach): ningún servo queda consumiendo corriente en reposo.
#include <Arduino.h>
#include <esp_system.h>

struct Servo1 { const char* nombre; char letra; int pin; int cerrado; int abierto; };
static const Servo1 SERVOS[] = {
  {"VERDE",  'V', 23, 0, 90},
  {"BLANCA", 'B', 19, 0, 90},
  {"NEGRA",  'N', 18, 0, 90},
};
static const unsigned long ABIERTO_MS = 10000;
static const int FRECUENCIA = 50, RESOLUCION = 16;
static bool conectado[3] = {false, false, false};
static bool cancelado = false;

static uint32_t duty(int angulo) {               // mismo mapeo que el firmware oficial: 0°=500 µs, 180°=2500 µs
  int pulso = map(angulo, 0, 180, 500, 2500);
  return (uint32_t)((uint64_t)pulso * 65535 / 20000);
}
static const char* motivoReinicio() {
  switch (esp_reset_reason()) {
    case ESP_RST_POWERON: return "ENCENDIDO";
    case ESP_RST_BROWNOUT: return "BROWNOUT (cayo el voltaje: alimentacion insuficiente)";
    case ESP_RST_SW: return "SOFTWARE";
    case ESP_RST_PANIC: return "PANICO/EXCEPCION";
    case ESP_RST_INT_WDT: case ESP_RST_TASK_WDT: case ESP_RST_WDT: return "WATCHDOG";
    case ESP_RST_EXT: return "EXTERNO (boton EN o DTR)";
    default: return "OTRO";
  }
}
static void ayuda() {
  Serial.println(F("DIAG EcoAccess: V=verde(23) B=blanca(19) N=negra(18) T=las tres 1/2/3=barrido lento X=cancelar E=estado ?=ayuda"));
}
static bool conectar(int i) {
  if (conectado[i]) return true;
  conectado[i] = ledcAttach(SERVOS[i].pin, FRECUENCIA, RESOLUCION);
  Serial.printf("[%lu] %s: ledcAttach(GPIO %d, %d Hz, %d bits) = %s\n", millis(), SERVOS[i].nombre, SERVOS[i].pin,
                FRECUENCIA, RESOLUCION, conectado[i] ? "OK" : "ERROR");
  return conectado[i];
}
static void soltar(int i) {
  if (!conectado[i]) return;
  bool ok = ledcDetach(SERVOS[i].pin);
  conectado[i] = false;
  pinMode(SERVOS[i].pin, OUTPUT); digitalWrite(SERVOS[i].pin, LOW);  // linea de senal en bajo, sin pulsos
  Serial.printf("[%lu] %s: PWM liberado (%s)\n", millis(), SERVOS[i].nombre, ok ? "OK" : "ERROR");
}
static bool escribir(int i, int angulo) {
  bool ok = ledcWrite(SERVOS[i].pin, duty(angulo));
  Serial.printf("[%lu] %s: %d grados -> ledcWrite=%s, ledcRead=%lu (esperado %lu)\n", millis(), SERVOS[i].nombre, angulo,
                ok ? "OK" : "ERROR", (unsigned long)ledcRead(SERVOS[i].pin), (unsigned long)duty(angulo));
  return ok;
}
// Espera revisando el puerto: X cancela.
static void esperar(unsigned long ms) {
  unsigned long inicio = millis();
  while (millis() - inicio < ms) {
    while (Serial.available()) { char c = toupper(Serial.read()); if (c == 'X') { cancelado = true; Serial.println(F("CANCELADO: cerrando")); } }
    if (cancelado) return;
    delay(10);
  }
}
static void probar(int i, bool lento) {
  cancelado = false;
  const Servo1& s = SERVOS[i];
  Serial.printf("[%lu] === %s (GPIO %d) %s ===\n", millis(), s.nombre, s.pin, lento ? "barrido lento" : "movimiento directo");
  if (!conectar(i)) { Serial.println(F("FALLO: no se pudo conectar PWM a ese pin")); return; }
  escribir(i, s.cerrado); esperar(600);
  if (!cancelado) {
    if (lento) { for (int a = s.cerrado; a <= s.abierto && !cancelado; a += 5) { ledcWrite(s.pin, duty(a)); esperar(40); } escribir(i, s.abierto); }
    else escribir(i, s.abierto);
    Serial.printf("[%lu] %s ABIERTA %lu ms (X para cancelar)\n", millis(), s.nombre, ABIERTO_MS);
    esperar(ABIERTO_MS);
  }
  if (lento && !cancelado) { for (int a = s.abierto; a >= s.cerrado; a -= 5) { ledcWrite(s.pin, duty(a)); delay(40); } }
  escribir(i, s.cerrado);
  delay(700);  // dar tiempo a llegar a 0 grados antes de soltar
  soltar(i);
  Serial.printf("[%lu] === %s terminado%s ===\n", millis(), s.nombre, cancelado ? " (cancelado)" : "");
}
static void estado() {
  Serial.printf("Reinicio: %s | libre heap %u\n", motivoReinicio(), (unsigned)ESP.getFreeHeap());
  for (int i = 0; i < 3; i++) Serial.printf("  %s GPIO %d PWM %s\n", SERVOS[i].nombre, SERVOS[i].pin, conectado[i] ? "conectado" : "libre");
}
void setup() {
  Serial.begin(115200);
  delay(300);
  for (int i = 0; i < 3; i++) { pinMode(SERVOS[i].pin, OUTPUT); digitalWrite(SERVOS[i].pin, LOW); }
  Serial.println();
  Serial.println(F("DIAG-ECOACCESS-TRES-CANECAS v1 (temporal, no es el firmware oficial)"));
  Serial.printf("Motivo del ultimo reinicio: %s\n", motivoReinicio());
  ayuda();
}
void loop() {
  if (!Serial.available()) { delay(5); return; }
  char c = toupper(Serial.read());
  switch (c) {
    case 'V': probar(0, false); break;
    case 'B': probar(1, false); break;
    case 'N': probar(2, false); break;
    case '1': probar(0, true); break;
    case '2': probar(1, true); break;
    case '3': probar(2, true); break;
    case 'T': for (int i = 0; i < 3 && !cancelado; i++) { probar(i, false); if (!cancelado) delay(1000); } cancelado = false; break;
    case 'E': estado(); break;
    case '?': ayuda(); break;
    case 'X': Serial.println(F("Nada en curso.")); break;
    default: break;
  }
}
