// EcoAccess P3: UN ESP32, TRES tapas, dos servomotores por tapa. Transporte USB serie.
// Mismo protocolo que hardware/servosecoacces (sobre HMAC-SHA256, desafío del microcontrolador,
// reserva en NVS antes de mover, ventana de 7 s, recibos firmados) con una tapa por identidad.
// Sin Oracle, WiFi, contraseñas ni sensores inventados.
#include <Arduino.h>
#include <Preferences.h>
#include <cJSON.h>
#include <mbedtls/base64.h>
#include <mbedtls/md.h>
#include <esp_random.h>
#include <bootloader_random.h>
#include <memory>
#include "EstacionCanecas.h"
#if __has_include("configuracion_privada.h")
#include "configuracion_privada.h"
#else
#include "configuracion_ejemplo.h"
#endif

using namespace ecoaccess;
using Json = std::unique_ptr<cJSON, decltype(&cJSON_Delete)>;
static const int CANTIDAD_TAPAS = sizeof(TAPAS) / sizeof(TAPAS[0]);
static_assert(CANTIDAD_TAPAS >= 1 && CANTIDAD_TAPAS <= MaximoTapas, "Entre una y tres tapas");
static_assert(sizeof(RECUPERACION_MANUAL_VERIFICADA) / sizeof(RECUPERACION_MANUAL_VERIFICADA[0]) == CANTIDAD_TAPAS,
              "Una marca de recuperación por tapa");

static Preferences diario;
static uint8_t clave[MaximoTapas][128];
static size_t largoClave[MaximoTapas] = {};
static bool estacionLista = false, tapaLista[MaximoTapas] = {}, excedido = false;
static String entrada, desafio;
static uint32_t inicioDesafio;
static ServosEstacion servos;
static VentanaDesafio ventanaDesafio;

static const char* texto(cJSON* j, const char* nombre) {
  cJSON* v = cJSON_GetObjectItemCaseSensitive(j, nombre);
  return cJSON_IsString(v) && v->valuestring ? v->valuestring : "";
}
static bool entero(cJSON* j, const char* nombre, int& resultado) {
  cJSON* v = cJSON_GetObjectItemCaseSensitive(j, nombre);
  if (!cJSON_IsNumber(v) || !isfinite(v->valuedouble) || v->valuedouble != v->valueint) return false;
  resultado = v->valueint; return true;
}
// Limitar profundidad antes de cJSON; después rechazar campos duplicados y desconocidos.
static bool profundidadSegura(const String& s) {
  int nivel = 0; bool cadena = false, escape = false;
  for (unsigned i = 0; i < s.length(); i++) {
    char c = s[i];
    if (cadena) { if (escape) escape = false; else if (c == '\\') escape = true; else if (c == '"') cadena = false; }
    else if (c == '"') cadena = true;
    else if (c == '{' || c == '[') { if (++nivel > 12) return false; }
    else if (c == '}' || c == ']') { if (--nivel < 0) return false; }
  }
  return !cadena && nivel == 0;
}
static bool campos(cJSON* j, const char* nombres, int cantidad) {
  if (!cJSON_IsObject(j) || cJSON_GetArraySize(j) != cantidad) return false;
  for (cJSON* a = j->child; a; a = a->next) {
    if (!a->string || !strstr(nombres, (String("|") + a->string + "|").c_str())) return false;
    for (cJSON* b = a->next; b; b = b->next) if (b->string && !strcmp(a->string, b->string)) return false;
  }
  return true;
}
static String base64(const uint8_t* bytes, size_t largo) {
  char salida[4097]; size_t escritos = 0;
  if (mbedtls_base64_encode((uint8_t*)salida, sizeof(salida)-1, &escritos, bytes, largo)) return "";
  salida[escritos] = 0; return String(salida);
}
static String hashHex(const String& valor) {
  uint8_t h[32]; char salida[65];
  if (mbedtls_md(mbedtls_md_info_from_type(MBEDTLS_MD_SHA256), (const uint8_t*)valor.c_str(), valor.length(), h)) return "";
  for (int i = 0; i < 32; i++) sprintf(salida+2*i, "%02X", h[i]);
  salida[64] = 0; return String(salida);
}
// Mismo mensaje autenticado que el firmware de una tapa, con la identidad y la clave de ESA tapa.
static bool mac(int t, const String& tipo, const String& datos, uint8_t* salida) {
  String m = String("ecoaccess-p3/v1\n")+TAPAS[t].dispositivo+"\n"+String(TAPAS[t].versionClave)+"\n"+tipo+"\n"+datos;
  return !mbedtls_md_hmac(mbedtls_md_info_from_type(MBEDTLS_MD_SHA256), clave[t], largoClave[t],
      (const uint8_t*)m.c_str(), m.length(), salida);
}
static String serializar(cJSON* j) {
  char* p = cJSON_PrintUnformatted(j); if (!p) return "";
  String s(p); cJSON_free(p); return s;
}
static void responder(int t, const String& original, bool aceptado, const char* estado, const String& nonce) {
  Json cuerpo(cJSON_CreateObject(), cJSON_Delete);
  cJSON_AddStringToObject(cuerpo.get(), "desafio", nonce.c_str());
  cJSON_AddStringToObject(cuerpo.get(), "sha256Paquete", hashHex(original).c_str());
  cJSON_AddBoolToObject(cuerpo.get(), "aceptado", aceptado);
  cJSON_AddStringToObject(cuerpo.get(), "estado", estado);
  String json = serializar(cuerpo.get());
  String datos = base64((const uint8_t*)json.c_str(), json.length());
  uint8_t firma[32]; if (!mac(t, "recepcion-serial", datos, firma)) return;
  Json sobre(cJSON_CreateObject(), cJSON_Delete);
  cJSON_AddNumberToObject(sobre.get(), "version", 1);
  cJSON_AddStringToObject(sobre.get(), "dispositivo", TAPAS[t].dispositivo);
  cJSON_AddNumberToObject(sobre.get(), "versionClave", TAPAS[t].versionClave);
  cJSON_AddStringToObject(sobre.get(), "tipo", "recepcion-serial");
  cJSON_AddStringToObject(sobre.get(), "datos", datos.c_str());
  cJSON_AddStringToObject(sobre.get(), "firma", base64(firma, 32).c_str());
  Serial.println(serializar(sobre.get()));
}
// Devuelve el cuerpo autenticado y la tapa cuya clave lo firmó. Identidad desconocida o firma ajena: nada.
static Json autenticar(const String& linea, int& t) {
  Json vacio(nullptr, cJSON_Delete); t = -1;
  if (!profundidadSegura(linea)) return vacio;
  Json s(cJSON_ParseWithOpts(linea.c_str(), nullptr, true), cJSON_Delete);
  int version, vc;
  if (!s || !campos(s.get(), "|version|dispositivo|versionClave|tipo|datos|firma|", 6)
      || !entero(s.get(), "version", version) || version != 1
      || strcmp(texto(s.get(), "tipo"), "actuacion-serial")) return vacio;
  int candidata = BuscarTapa(TAPAS, CANTIDAD_TAPAS, texto(s.get(), "dispositivo"));
  if (candidata < 0 || !tapaLista[candidata]
      || !entero(s.get(), "versionClave", vc) || vc != TAPAS[candidata].versionClave) return vacio;
  String datos(texto(s.get(), "datos")); uint8_t recibida[32], calculada[32]; size_t n = 0;
  const char* f = texto(s.get(), "firma");
  if (mbedtls_base64_decode(recibida, 32, &n, (const uint8_t*)f, strlen(f)) || n != 32
      || !mac(candidata, "actuacion-serial", datos, calculada)) return vacio;
  uint8_t diferente = 0; for (int i = 0; i < 32; i++) diferente |= recibida[i] ^ calculada[i];
  if (diferente) return vacio;
  uint8_t decodificado[3073];
  if (mbedtls_base64_decode(decodificado, 3072, &n, (const uint8_t*)datos.c_str(), datos.length())) return vacio;
  decodificado[n] = 0; String cuerpo((const char*)decodificado);
  if (cuerpo.length() != n || !profundidadSegura(cuerpo)) return vacio;
  t = candidata;
  return Json(cJSON_ParseWithOpts(cuerpo.c_str(), nullptr, true), cJSON_Delete);
}
static bool pendiente(int t) { char k[4]; ClaveNvs(k, 'p', t); return diario.getBool(k, false); }
static bool marcarPendiente(int t, bool valor) { char k[4]; ClaveNvs(k, 'p', t); return diario.putBool(k, valor) == 1; }
// Reserva global: un id de orden o un nonce usados en cualquier tapa no se aceptan en ninguna otra.
static bool reservar(int t, int orden, const String& nonce) {
  String o = "o"+hashHex(String(orden)).substring(0,14), n = "n"+hashHex(nonce).substring(0,14);
  if (diario.isKey(o.c_str()) || diario.isKey(n.c_str()) || diario.getUInt("cantidad",0) >= 512) return false;
  // Una escritura parcial conserva una reserva o consume capacidad; jamás implica permiso para reabrir.
  if (diario.putUInt("cantidad", diario.getUInt("cantidad",0)+1) != 4) return false;
  if (diario.putBool(o.c_str(),true) != 1 || diario.putBool(n.c_str(),true) != 1) return false;
  return marcarPendiente(t, true);
}
static bool mover(int pin, int angulo) {
  int pulso = map(angulo,0,180,500,2500);
  return ledcWrite(pin,(uint32_t)((uint64_t)pulso*65535/20000));
}
static bool adjuntarServo(int pin) { return ledcAttach(pin,50,16); }
static void procesar(const String& linea) {
  if (linea == "?") {
    uint8_t bytes[16]; char nonce[33];
    bootloader_random_enable(); esp_fill_random(bytes,16); bootloader_random_disable();
    for (int i = 0; i < 16; i++) sprintf(nonce+2*i,"%02x",bytes[i]); nonce[32] = 0;
    desafio = nonce; inicioDesafio = millis(); ventanaDesafio.Iniciar(inicioDesafio);
    Serial.println(String("{\"ecoaccessDesafio\":\"")+desafio+"\"}"); return;
  }
  int t = -1;
  Json cuerpo = estacionLista ? autenticar(linea, t) : Json(nullptr, cJSON_Delete);
  String nonce = cuerpo ? texto(cuerpo.get(),"desafio") : "";
  int validez = 0, orden = 0, caneca = 0;
  cJSON* comando = cuerpo ? cJSON_GetObjectItemCaseSensitive(cuerpo.get(),"comando") : nullptr;
  bool valido = cuerpo && t >= 0 && campos(cuerpo.get(),"|comando|desafio|validezMs|",3)
      && campos(comando,"|idOrden|idContenedor|claveIdempotencia|venceUtc|",4)
      && entero(cuerpo.get(),"validezMs",validez) && validez >= 100 && validez <= 5000
      && entero(comando,"idOrden",orden) && orden > 0
      && entero(comando,"idContenedor",caneca) && OrdenParaTapa(TAPAS[t], caneca)
      && strlen(texto(comando,"claveIdempotencia")) > 0 && strlen(texto(comando,"claveIdempotencia")) <= 80
      && desafio.length() == 32 && nonce == desafio && ventanaDesafio.Vigente(millis(),validez);
  desafio = ""; ventanaDesafio.Consumir(); // Consumir incluso ante rechazo: no reutilizar el permiso.
  if (!valido || pendiente(t) || !reservar(t, orden, texto(comando,"claveIdempotencia"))) {
    if (cuerpo && t >= 0) responder(t,linea,false,"rechazado",nonce); else Serial.println("{\"error\":\"rechazado\"}");
    return;
  }
  // La escritura flash también consume vigencia. No actuar si la reserva terminó fuera de plazo.
  if ((uint32_t)(millis()-inicioDesafio) >= (unsigned)validez) { tapaLista[t] = false; responder(t,linea,false,"incierto",nonce); return; }
  const Tapa& tapa = TAPAS[t];
  bool ok = servos.Preparar(tapa, t, adjuntarServo);
  if (ok) {
    // Solo los servos asignados a esta tapa (uno en el prototipo). Las otras tapas no reciben ninguna escritura PWM.
    ok = ServosEstacion::Posicionar(tapa, tapa.abierto, mover);
    if (ok) responder(t,linea,true,"pwm-apertura-aplicada",nonce);
    delay(ABIERTO_MS);
    ok = ServosEstacion::Posicionar(tapa, tapa.cerrado, mover) && ok;
    delay(500);
  }
  // Mantener PWM de cerrado tras finalizar. No informa posiciones físicas, apertura ni depósito.
  if (!ok || !marcarPendiente(t, false)) { tapaLista[t] = false; responder(t,linea,false,"incierto",nonce); return; }
  responder(t,linea,true,"pwm-completado-sin-sensor",nonce);
}
void setup() {
  Serial.begin(115200); entrada.reserve(4096);
  estacionLista = Validar(TAPAS, CANTIDAD_TAPAS, ABIERTO_MS) == Configuracion::Valida;
  if (!estacionLista) return; // Tabla incoherente: ninguna tapa se habilita y nada se escribe en flash.
  if (!diario.begin("ecoaccess-est3",false)) { estacionLista = false; return; }
  for (int t = 0; t < CANTIDAD_TAPAS; t++) {
    const Tapa& tapa = TAPAS[t];
    largoClave[t] = 0;
    bool claveOk = tapa.claveBase64 && !mbedtls_base64_decode(clave[t],sizeof(clave[t]),&largoClave[t],
        (const uint8_t*)tapa.claveBase64,strlen(tapa.claveBase64)) && largoClave[t] >= 32 && largoClave[t] <= 128;
    // Fuera de servicio (sin pines, sin calibrar o sin clave): no se escribe binding ni se conecta PWM.
    if (!claveOk || !Habilitada(tapa)) continue;
    char kb[4]; ClaveNvs(kb, 'b', t);
    String binding = String(tapa.dispositivo)+"|"+String(tapa.idCaneca)+"|"+String(tapa.versionClave);
    bool ok = true;
    if (!diario.isKey(kb)) ok = diario.putString(kb,binding) > 0;
    else ok = diario.getString(kb) == binding;
    if (ok && RECUPERACION_MANUAL_VERIFICADA[t]) ok = marcarPendiente(t, false);
    tapaLista[t] = ok && !pendiente(t);
  }
  // No conecta PWM ni mueve motores al encender o reiniciar.
}
void loop() {
  ventanaDesafio.Actualizar(millis()); // Un desafío viejo no revive tras un ciclo completo de millis().
  while (Serial.available()) {
    char c = (char)Serial.read();
    if (c == '\n') { if (!excedido) procesar(entrada); entrada = ""; excedido = false; }
    else if (c != '\r') { if (entrada.length() >= 4096) { excedido = true; desafio = ""; ventanaDesafio.Consumir(); } else if (!excedido) entrada += c; }
  }
  delay(2);
}
