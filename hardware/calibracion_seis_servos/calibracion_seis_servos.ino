// EcoAccess P3: banco de calibración SUPERVISADA para el ESP32 de seis servos (pines del compañero).
// Sustituye al bucle original que movía los seis motores a la vez sin parar. Úsalo SIN tapas acopladas,
// con fuente externa para los servos y tierra común, para descubrir qué GPIO mueve qué tapa.
//
// No mueve nada al encender. Órdenes por el Monitor Serie (115200, fin de línea "Nueva línea"):
//   ARMAR              habilita movimientos durante 60 s (se desarma solo)
//   S <gpio> <grados>  mueve UN servo, entre 0 y 120 grados, si está armado
//   PARAR              quita el PWM de todos los pines y desarma (los servos quedan libres)
//   ESTADO             informa armado, pines conectados y último ángulo
// No implementa el protocolo de EcoAccess: nunca debe quedar cargado en una estación en servicio.
#include <Arduino.h>

static const int PINES[] = {17, 16, 21, 23, 18, 19};
static const int CANTIDAD = sizeof(PINES) / sizeof(PINES[0]);
static const int MAXIMO_GRADOS = 120;          // límite de banco; el archivo original solo llegaba a 60
static const uint32_t DURACION_ARMADO_MS = 60000;
static bool conectado[CANTIDAD] = {};
static int ultimo[CANTIDAD];
static bool armado = false;
static uint32_t armadoDesde = 0;
static String linea;

static int indice(int pin) { for (int i = 0; i < CANTIDAD; i++) if (PINES[i] == pin) return i; return -1; }

static void parar() {
  for (int i = 0; i < CANTIDAD; i++) if (conectado[i]) { ledcDetach(PINES[i]); conectado[i] = false; }
  armado = false;
  Serial.println("PARADO: sin PWM en ningun pin");
}

static void mover(int pin, int grados) {
  int i = indice(pin);
  if (i < 0) { Serial.println("RECHAZADO: pin fuera de la lista 17,16,21,23,18,19"); return; }
  if (!armado) { Serial.println("RECHAZADO: escribe ARMAR primero"); return; }
  if (grados < 0 || grados > MAXIMO_GRADOS) { Serial.println("RECHAZADO: grados fuera de 0..120"); return; }
  if (!conectado[i] && !(conectado[i] = ledcAttach(pin, 50, 16))) { Serial.println("ERROR: no se pudo conectar PWM"); return; }
  int pulso = map(grados, 0, 180, 500, 2500);
  ledcWrite(pin, (uint32_t)((uint64_t)pulso * 65535 / 20000));
  ultimo[i] = grados;
  Serial.printf("MOVIDO: GPIO %d a %d grados (solo este servo)\n", pin, grados);
}

static void procesar(String s) {
  s.trim(); s.toUpperCase();
  if (s == "ARMAR") { armado = true; armadoDesde = millis(); Serial.println("ARMADO por 60 s"); }
  else if (s == "PARAR") parar();
  else if (s == "ESTADO") {
    Serial.printf("armado=%s\n", armado ? "si" : "no");
    for (int i = 0; i < CANTIDAD; i++)
      Serial.printf("GPIO %d: %s, ultimo=%d\n", PINES[i], conectado[i] ? "PWM" : "libre", conectado[i] ? ultimo[i] : -1);
  } else if (s.startsWith("S ")) {
    int pin = -1, grados = -1;
    if (sscanf(s.c_str() + 2, "%d %d", &pin, &grados) == 2) mover(pin, grados);
    else Serial.println("RECHAZADO: formato S <gpio> <grados>");
  } else if (s.length()) Serial.println("Ordenes: ARMAR, S <gpio> <grados>, PARAR, ESTADO");
}

void setup() {
  Serial.begin(115200);
  linea.reserve(64);
  Serial.println("Banco de calibracion EcoAccess: nada se mueve hasta ARMAR + S <gpio> <grados>");
}

void loop() {
  if (armado && (uint32_t)(millis() - armadoDesde) >= DURACION_ARMADO_MS) { armado = false; Serial.println("DESARMADO por tiempo"); }
  while (Serial.available()) {
    char c = (char)Serial.read();
    if (c == '\n') { procesar(linea); linea = ""; }
    else if (c != '\r' && linea.length() < 60) linea += c;
  }
  delay(2);
}
