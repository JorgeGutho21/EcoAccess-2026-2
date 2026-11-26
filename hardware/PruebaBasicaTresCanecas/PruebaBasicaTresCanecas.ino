// ECOACCESS P3 · PRUEBA BÁSICA DE LAS TRES CANECAS
// Código sencillo para comprobar el hardware: ESP32 + tres servomotores. Sin Oracle, Telegram, HMAC, bridges ni C#.
// NO es el firmware oficial: después de las pruebas se vuelve a cargar hardware/estacion_tres_canecas.
//
// Uso: Arduino IDE → Herramientas → Placa «ESP32 Dev Module», Puerto COM3 → Subir.
//      Monitor Serie a 115200 baudios, «Nueva línea». Escribe una letra y Enter:
//        V = verde   B = blanca   N = negra   T = las tres, una después de otra   X = cancelar
//        1..4 = búsqueda de pin por grupos   P22 = probar un pin suelto (ver probarGrupo)
// Al encender no se mueve ningún motor. Cada motor: abre a ANGULO_ABIERTO, espera 10 s, vuelve a 0° y se apaga su PWM.

#include <Arduino.h>

// --- Configuración: lo ÚNICO que cambia entre canecas es el pin ---
const int PIN_VERDE  = 23;
const int PIN_BLANCA = 19;
const int PIN_NEGRA  = 18;

const int ANGULO_CERRADO = 0;
// 24/11/2026: los servos quedaban un poco adelantados; Jorge pidió restar 20° a TODAS las posiciones de las tres
// canecas. Cerrada 0° → -20° real (≈280 µs, bajo el rango típico del SG90: si un servo zumba, usar -10).
// 17:50: con -20° la verde solo zumbó (forzaba contra el tope interno). Se vuelve al mínimo seguro: 0° real (500 µs).
const int CORRECCION_GRADOS = 0;
// Paletas recortadas (24/11/2026): las tres abren con un recorrido de 100° desde la posición cerrada.
// Real: cerrada -20° (≈280 µs, el mínimo que ya se usaba; más abajo el servo no responde) y abierta 80° (≈1390 µs).
// Ajuste individual: cambiar el recorrido de una caneca aquí.
const int ABIERTO_VERDE  = 100;
const int ABIERTO_BLANCA = 90;
const int ABIERTO_NEGRA  = 100;
int ANGULO_ABIERTO = 50;                   // solo para la búsqueda de pin y «P»; «A30» lo cambia
const unsigned long TIEMPO_ABIERTO_MS = 10000;
const int FRECUENCIA_HZ = 50;              // servos SG90: 50 Hz (periodo de 20 ms)
const int RESOLUCION_BITS = 16;

bool cancelado = false;

// Ángulo → ancho de pulso (0° = 500 µs, 180° = 2500 µs) → valor PWM de 16 bits sobre un periodo de 20 000 µs.
uint32_t valorPwm(int angulo) {
  int microsegundos = map(angulo + CORRECCION_GRADOS, 0, 180, 500, 2500);
  return (uint32_t)((uint64_t)microsegundos * 65535 / 20000);
}

// Espera el tiempo indicado, pero atiende la X para cancelar.
void esperar(unsigned long ms) {
  unsigned long inicio = millis();
  while (millis() - inicio < ms) {
    while (Serial.available()) {
      char c = toupper(Serial.read());
      if (c == 'X') { cancelado = true; Serial.println("X recibido: cancelando y cerrando."); }
    }
    if (cancelado) return;
    delay(10);
  }
}

// La MISMA función para las tres canecas.
void moverCaneca(const char* nombre, int pin, int abierto = -1) {
  if (abierto < 0) abierto = ANGULO_ABIERTO;
  Serial.printf("\n[%s] GPIO %d: inicio\n", nombre, pin);
  if (!ledcAttach(pin, FRECUENCIA_HZ, RESOLUCION_BITS)) {
    Serial.printf("[%s] ERROR: no se pudo activar el PWM en GPIO %d\n", nombre, pin);
    return;
  }
  ledcWrite(pin, valorPwm(ANGULO_CERRADO));
  esperar(500);
  if (!cancelado) {
    ledcWrite(pin, valorPwm(abierto));
    Serial.printf("[%s] ABIERTA a %d grados por %lu s (X cancela)\n", nombre, abierto, TIEMPO_ABIERTO_MS / 1000);
    esperar(TIEMPO_ABIERTO_MS);
  }
  ledcWrite(pin, valorPwm(ANGULO_CERRADO));
  Serial.printf("[%s] cerrando a %d grados\n", nombre, ANGULO_CERRADO);
  delay(1000);                             // tiempo para llegar a 0° antes de apagar la señal
  ledcDetach(pin);                         // PWM apagado: el motor no queda consumiendo corriente
  pinMode(pin, OUTPUT);
  digitalWrite(pin, LOW);
  Serial.printf("[%s] CERRADA y PWM apagado\n", nombre);
}

// BÚSQUEDA DE PIN: manda la misma apertura a un grupo de pines a la vez. Si una tapa se mueve, su cable de señal
// está en ese grupo. Sirve para comprobar si un cable quedó en un pin vecino (p. ej. 22 en vez de 23).
// Grupos (sin 23, 19 ni 18, sin TX/RX ni el GPIO 0 de arranque):
//   1 = 22 21 17 16    2 = 4 5 2 15    3 = 13 12 14 27    4 = 26 25 33 32
const int GRUPOS[4][4] = { {22, 21, 17, 16}, {4, 5, 2, 15}, {13, 12, 14, 27}, {26, 25, 33, 32} };

void probarGrupo(int g) {
  Serial.printf("\n[GRUPO %d] pines %d %d %d %d: abriendo 4 s\n", g + 1, GRUPOS[g][0], GRUPOS[g][1], GRUPOS[g][2], GRUPOS[g][3]);
  for (int k = 0; k < 4; k++) if (!ledcAttach(GRUPOS[g][k], FRECUENCIA_HZ, RESOLUCION_BITS)) Serial.printf("  ERROR PWM en GPIO %d\n", GRUPOS[g][k]);
  for (int k = 0; k < 4; k++) ledcWrite(GRUPOS[g][k], valorPwm(ANGULO_CERRADO));
  esperar(500);
  for (int k = 0; k < 4; k++) ledcWrite(GRUPOS[g][k], valorPwm(ANGULO_ABIERTO));
  esperar(4000);
  for (int k = 0; k < 4; k++) ledcWrite(GRUPOS[g][k], valorPwm(ANGULO_CERRADO));
  delay(1000);
  for (int k = 0; k < 4; k++) { ledcDetach(GRUPOS[g][k]); pinMode(GRUPOS[g][k], INPUT); }
  Serial.printf("[GRUPO %d] terminado\n", g + 1);
}

// Prueba un pin suelto: «P22» + Enter.
void probarPin(int pin) {
  for (int g = 0; g < 4; g++) for (int k = 0; k < 4; k++)
    if (GRUPOS[g][k] == pin) { char nombre[12]; snprintf(nombre, sizeof(nombre), "GPIO%d", pin); moverCaneca(nombre, pin); pinMode(pin, INPUT); return; }
  Serial.printf("GPIO %d no esta en la lista de pines de busqueda.\n", pin);
}

void setup() {
  Serial.begin(115200);
  delay(300);
  // Los tres pines en bajo: ningún pulso, ningún movimiento al encender.
  pinMode(PIN_VERDE, OUTPUT);  digitalWrite(PIN_VERDE, LOW);
  pinMode(PIN_BLANCA, OUTPUT); digitalWrite(PIN_BLANCA, LOW);
  pinMode(PIN_NEGRA, OUTPUT);  digitalWrite(PIN_NEGRA, LOW);
  Serial.println("\nECOACCESS P3 - PRUEBA BASICA TRES CANECAS");
  Serial.printf("Verde GPIO %d | Blanca GPIO %d | Negra GPIO %d\n", PIN_VERDE, PIN_BLANCA, PIN_NEGRA);
  Serial.println("Comandos: V verde, B blanca, N negra, T las tres, X cancelar");
}

void loop() {
  if (!Serial.available()) { delay(5); return; }
  char c = toupper(Serial.read());
  cancelado = false;
  if (c == 'V') moverCaneca("VERDE", PIN_VERDE, ABIERTO_VERDE);
  else if (c == 'B') moverCaneca("BLANCA", PIN_BLANCA, ABIERTO_BLANCA);
  else if (c == 'N') moverCaneca("NEGRA", PIN_NEGRA, ABIERTO_NEGRA);
  else if (c == 'T') {
    moverCaneca("VERDE", PIN_VERDE, ABIERTO_VERDE);
    if (!cancelado) { delay(1000); moverCaneca("BLANCA", PIN_BLANCA, ABIERTO_BLANCA); }
    if (!cancelado) { delay(1000); moverCaneca("NEGRA", PIN_NEGRA, ABIERTO_NEGRA); }
  }
  else if (c == 'C') {                     // las tres a la posición inicial (0°), una por una, y PWM apagado
    const int pines[3] = {PIN_VERDE, PIN_BLANCA, PIN_NEGRA};
    for (int k = 0; k < 3; k++) {
      if (!ledcAttach(pines[k], FRECUENCIA_HZ, RESOLUCION_BITS)) { Serial.printf("ERROR PWM en GPIO %d\n", pines[k]); continue; }
      ledcWrite(pines[k], valorPwm(ANGULO_CERRADO));
      delay(1200);
      ledcDetach(pines[k]); pinMode(pines[k], OUTPUT); digitalWrite(pines[k], LOW);
      Serial.printf("GPIO %d en %d grados\n", pines[k], ANGULO_CERRADO);
    }
    Serial.println("Las tres en posicion inicial.");
  }
  else if (c >= '1' && c <= '4') probarGrupo(c - '1');
  else if (c == 'P') probarPin(Serial.parseInt());
  else if (c == 'A') {                     // «A30» + Enter: apertura a 30° (entre 10 y 90) hasta reiniciar
    int a = Serial.parseInt();
    if (a >= 10 && a <= 90) { ANGULO_ABIERTO = a; Serial.printf("Apertura ahora: %d grados\n", a); }
    else Serial.println("Angulo fuera de rango (10 a 90).");
  }
  else if (c == 'X') Serial.println("No hay ninguna prueba en curso.");
}
