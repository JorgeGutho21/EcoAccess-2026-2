// ECOACCESS P3 · FIRMWARE DE CALIBRACIÓN DE LAS TRES CANECAS (fase 1 del panel web)
// Controla los tres servos con UNA misma lógica; cada caneca tiene su propia calibración (cerrado, abierto, tiempo),
// guardada en la memoria del ESP32 (NVS): sobrevive a reinicios y desconexiones. Sin HMAC, Oracle ni Telegram:
// lo controla el panel web local (src/EcoAccess.P3.PanelCanecas). Al encender NO mueve ningún motor.
//
// Protocolo serie 115200, una orden por línea, respuesta en una línea JSON:
//   ESTADO                      estado y calibración de las tres
//   CAL <V|B|N> <cerrado> <abierto> <ms>   cambia la calibración en memoria (ángulos absolutos del servo 0..180)
//   GUARDAR [c]                 guarda en la memoria permanente las tres o solo la caneca c
//   DESCARTAR [c]               vuelve a la última calibración guardada (las tres o la caneca c)
//   ABRIR <c> | CERRAR <c>      abre (máx. 60 s, luego cierra sola) o cierra una caneca
//   CICLO <c>                   abre, espera el tiempo configurado y cierra
//   POS <c> <grados>            lleva el servo a un ángulo para calibrar (se suelta solo a los 60 s)
//   SEC <letras>                ciclos uno tras otro, p. ej. SEC VBN
//   DETENER                     vacía la cola, cierra lo que esté abierto y apaga el PWM
// Eventos espontáneos: {"evt":"...","c":"V"}. Ningún recibo de PWM prueba que la tapa se movió: no hay sensores.
#include <Arduino.h>
#include <Preferences.h>

struct Caneca {
  char letra; const char* nombre; int pin;
  int cerrado, abierto; unsigned long ms;     // calibración vigente
  int estado;                                 // 0 reposo, 1 abierta, 2 ciclo, 3 cerrando, 4 posicion
  bool pendiente;                             // calibración aplicada en memoria y aún no guardada
  unsigned long hasta;                        // fin del estado actual (millis)
  bool pwm;
};
enum { REPOSO = 0, ABIERTA = 1, CICLO = 2, CERRANDO = 3, POSICION = 4 };
static const char* NOMBRE_ESTADO[] = {"REPOSO", "ABIERTA", "CICLO", "CERRANDO", "POSICION"};
static Caneca C[3] = {
  {'V', "verde", 23, 0, 100, 10000, REPOSO, false, 0, false},
  {'B', "blanca", 19, 0, 90, 10000, REPOSO, false, 0, false},
  {'N', "negra", 18, 0, 100, 10000, REPOSO, false, 0, false},
};
static const unsigned long MAX_MANUAL_MS = 60000, TIEMPO_CIERRE_MS = 1000;
static Preferences nvs;
static String entrada, cola;
static bool sinGuardar() { for (auto& c : C) if (c.pendiente) return true; return false; }

static uint32_t duty(int angulo) {             // 0° = 500 µs, 180° = 2500 µs, periodo 20 ms, 16 bits
  int us = map(constrain(angulo, 0, 180), 0, 180, 500, 2500);
  return (uint32_t)((uint64_t)us * 65535 / 20000);
}
static Caneca* buscar(char l) { for (auto& c : C) if (c.letra == toupper(l)) return &c; return nullptr; }
static bool ocupado() { for (auto& c : C) if (c.estado != REPOSO) return true; return false; }
static void evento(const char* e, const Caneca& c) { Serial.printf("{\"evt\":\"%s\",\"c\":\"%c\"}\n", e, c.letra); }

static bool mover(Caneca& c, int angulo) {
  if (!c.pwm) { c.pwm = ledcAttach(c.pin, 50, 16); if (!c.pwm) return false; }
  return ledcWrite(c.pin, duty(angulo));
}
static void soltar(Caneca& c) {
  if (c.pwm) { ledcDetach(c.pin); c.pwm = false; }
  pinMode(c.pin, OUTPUT); digitalWrite(c.pin, LOW);
}
static void cerrar(Caneca& c) {
  if (c.estado == REPOSO && !c.pwm) return;
  mover(c, c.cerrado); c.estado = CERRANDO; c.hasta = millis() + TIEMPO_CIERRE_MS; evento("cerrando", c);
}

// solo == 0: las tres; si no, solo la caneca con esa letra.
static void cargar(char solo = 0) {
  nvs.begin("eco-cal", true);
  for (auto& c : C) {
    if (solo && c.letra != solo) continue;
    char k[3] = {c.letra, 'c', 0}; c.cerrado = nvs.getInt(k, c.cerrado);
    k[1] = 'a'; c.abierto = nvs.getInt(k, c.abierto);
    k[1] = 't'; c.ms = nvs.getULong(k, c.ms);
    c.pendiente = false;
  }
  nvs.end();
}
static bool guardar(char solo = 0) {
  if (!nvs.begin("eco-cal", false)) return false;
  bool ok = true;
  for (auto& c : C) {
    if (solo && c.letra != solo) continue;
    char k[3] = {c.letra, 'c', 0}; bool e = nvs.putInt(k, c.cerrado) == 4;
    k[1] = 'a'; e &= nvs.putInt(k, c.abierto) == 4;
    k[1] = 't'; e &= nvs.putULong(k, c.ms) == 4;
    if (e) c.pendiente = false; ok &= e;
  }
  nvs.end(); return ok;
}

static void responderEstado() {
  String s = "{\"ok\":true,\"firmware\":\"ecoaccess-calibracion-v1\",\"sinGuardar\":";
  s += sinGuardar() ? "true" : "false"; s += ",\"cola\":\""; s += cola; s += "\",\"canecas\":[";
  for (int i = 0; i < 3; i++) {
    Caneca& c = C[i];
    char b[240];
    long resta = c.estado == REPOSO ? 0 : (long)(c.hasta - millis()); if (resta < 0) resta = 0;
    snprintf(b, sizeof(b), "%s{\"c\":\"%c\",\"nombre\":\"%s\",\"pin\":%d,\"cerrado\":%d,\"abierto\":%d,\"ms\":%lu,\"estado\":\"%s\",\"pwm\":%s,\"pendiente\":%s,\"restanteMs\":%ld}",
             i ? "," : "", c.letra, c.nombre, c.pin, c.cerrado, c.abierto, c.ms, NOMBRE_ESTADO[c.estado], c.pwm ? "true" : "false", c.pendiente ? "true" : "false", resta);
    s += b;
  }
  s += "]}"; Serial.println(s);
}
static void ok(const char* m) { Serial.printf("{\"ok\":true,\"msg\":\"%s\"}\n", m); }
static void error(const char* m) { Serial.printf("{\"ok\":false,\"error\":\"%s\"}\n", m); }

static void procesar(String linea) {
  linea.trim();
  if (!linea.length()) return;
  char orden[12] = {0}, l = 0; long a = 0, b = 0, t = 0;
  int n = sscanf(linea.c_str(), "%11s %c %ld %ld %ld", orden, &l, &a, &b, &t);
  String o = String(orden); o.toUpperCase();
  Caneca* c = n >= 2 ? buscar(l) : nullptr;

  if (o == "ESTADO") return responderEstado();
  if (o == "DETENER") {
    if (n >= 2) {                                    // DETENER <c>: quitarla de la cola y cerrarla si está abierta
      Caneca* d = buscar(l); if (!d) return error("caneca invalida");
      cola.replace(String(d->letra), "");
      if (d->estado != REPOSO && d->estado != CERRANDO) cerrar(*d);
      return ok("caneca detenida: fuera de la cola y cerrando");
    }
    cola = ""; for (auto& x : C) if (x.estado != REPOSO && x.estado != CERRANDO) cerrar(x);
    return ok("detenido: cola vaciada y tapas cerrando");
  }
  if (o == "GUARDAR" || o == "DESCARTAR") {
    char solo = n >= 2 ? (char)toupper(l) : 0;
    if (solo && !buscar(solo)) return error("caneca invalida");
    if (o == "GUARDAR") return guardar(solo) ? ok("calibracion guardada") : error("no se pudo guardar");
    for (auto& x : C) if ((!solo || x.letra == solo) && x.estado != REPOSO) return error("ocupado");
    cargar(solo); return ok("calibracion guardada restaurada");
  }
  if (o == "SEC") {
    if (n < 2) return error("faltan canecas");
    String sec = linea.substring(4); sec.trim(); sec.toUpperCase();
    for (unsigned i = 0; i < sec.length(); i++) if (!buscar(sec[i])) return error("caneca invalida en la secuencia");
    if (ocupado() || cola.length()) return error("ocupado");
    cola = sec; return ok("secuencia en cola");
  }
  if (!c) return error("orden o caneca invalida");
  if (o == "CAL") {
    if (n < 5) return error("uso: CAL V cerrado abierto ms");
    if (a < 0 || a > 180 || b < 0 || b > 180) return error("angulos fuera de 0..180");
    if (labs(b - a) < 5) return error("el recorrido debe ser de al menos 5 grados");
    if (t < 1000 || t > 30000) return error("tiempo fuera de 1000..30000 ms");
    if (c->estado != REPOSO) return error("ocupado");
    c->cerrado = a; c->abierto = b; c->ms = t; c->pendiente = true; return ok("calibracion aplicada (sin guardar)");
  }
  if (o == "CERRAR") { cerrar(*c); return ok("cerrando"); }
  if (ocupado() || cola.length()) return error("ocupado");
  if (o == "ABRIR" || o == "CICLO") {
    if (!mover(*c, c->abierto)) return error("no se pudo activar PWM");
    c->estado = o == "ABRIR" ? ABIERTA : CICLO;
    c->hasta = millis() + (o == "ABRIR" ? MAX_MANUAL_MS : c->ms);
    evento("abierta", *c); return ok("pwm de apertura aplicado");
  }
  if (o == "POS") {
    if (n < 3 || a < 0 || a > 180) return error("angulo fuera de 0..180");
    if (!mover(*c, a)) return error("no se pudo activar PWM");
    c->estado = POSICION; c->hasta = millis() + MAX_MANUAL_MS; return ok("pwm de posicion aplicado");
  }
  error("orden desconocida");
}

void setup() {
  Serial.begin(115200);
  for (auto& c : C) { pinMode(c.pin, OUTPUT); digitalWrite(c.pin, LOW); }
  cargar();
  entrada.reserve(128);
  Serial.println("{\"evt\":\"inicio\",\"firmware\":\"ecoaccess-calibracion-v1\"}");
}

void loop() {
  while (Serial.available()) {
    char ch = (char)Serial.read();
    if (ch == '\n') { procesar(entrada); entrada = ""; }
    else if (ch != '\r' && entrada.length() < 120) entrada += ch;
  }
  unsigned long ahora = millis();
  for (auto& c : C) {
    if (c.estado == REPOSO || (long)(ahora - c.hasta) < 0) continue;
    if (c.estado == CERRANDO) { soltar(c); c.estado = REPOSO; evento("cerrada", c); }
    else cerrar(c);                                   // fin de ciclo, apertura manual o posición: volver a cerrado
  }
  if (cola.length() && !ocupado()) {                  // siguiente caneca de la secuencia, una a la vez
    Caneca* c = buscar(cola[0]); cola.remove(0, 1);
    if (c && mover(*c, c->abierto)) { c->estado = CICLO; c->hasta = millis() + c->ms; evento("abierta", *c); }
  }
  delay(2);
}
