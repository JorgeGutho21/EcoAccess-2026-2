// EcoAccess P3: diagnóstico del ESP32 SIN mover motores.
// Identifica el chip real, la versión del núcleo Arduino, la flash, el motivo del último reinicio y el estado
// eléctrico (en reposo) de los seis GPIO de los servos. No conecta PWM, no usa WiFi y no guarda nada en flash.
//
// Monitor serie a 115200 baudios, fin de línea "Nueva línea". Órdenes:
//   INFO     repite el informe
//   PINES    lee cada GPIO de servo como entrada con pull-down (1 = algo lo mantiene en alto)
//   ECO <t>  devuelve el texto: prueba la comunicación en ambos sentidos
// Al terminar, vuelve a cargar estacion_tres_canecas: este sketch no implementa el protocolo de EcoAccess.
#include <Arduino.h>
#include <esp_chip_info.h>
#include <esp_mac.h>
#include <esp_system.h>
#include <esp_flash.h>

static const int PINES_SERVOS[] = {17, 16, 21, 23, 18, 19};
static String linea;

static const char* motivoReinicio(esp_reset_reason_t r) {
  switch (r) {
    case ESP_RST_POWERON: return "encendido";
    case ESP_RST_EXT: return "pin externo";
    case ESP_RST_SW: return "software";
    case ESP_RST_PANIC: return "excepcion (panic)";
    case ESP_RST_INT_WDT: return "watchdog de interrupciones";
    case ESP_RST_TASK_WDT: return "watchdog de tareas";
    case ESP_RST_WDT: return "otro watchdog";
    case ESP_RST_DEEPSLEEP: return "salida de deep sleep";
    case ESP_RST_BROWNOUT: return "BROWNOUT: caida de tension (revisar alimentacion)";
    case ESP_RST_SDIO: return "SDIO";
    default: return "desconocido";
  }
}

static void informe() {
  esp_chip_info_t chip; esp_chip_info(&chip);
  uint8_t mac[6]; esp_read_mac(mac, ESP_MAC_WIFI_STA);
  uint32_t flash = 0; esp_flash_get_size(nullptr, &flash);
  Serial.println("=== EcoAccess diagnostico ESP32 ===");
  Serial.printf("modelo=%s revision=%d nucleos=%d\n", ESP.getChipModel(), ESP.getChipRevision(), chip.cores);
  Serial.printf("wifi=%s bt=%s ble=%s\n", (chip.features & CHIP_FEATURE_WIFI_BGN) ? "si" : "no",
                (chip.features & CHIP_FEATURE_BT) ? "si" : "no", (chip.features & CHIP_FEATURE_BLE) ? "si" : "no");
  Serial.printf("cpu_mhz=%lu flash_bytes=%lu psram_bytes=%lu heap_libre=%lu\n", (unsigned long)getCpuFrequencyMhz(),
                (unsigned long)flash, (unsigned long)ESP.getPsramSize(), (unsigned long)ESP.getFreeHeap());
  // Solo los tres últimos bytes de la MAC: bastan para distinguir placas sin publicar la dirección completa.
  Serial.printf("mac_sufijo=%02X:%02X:%02X\n", mac[3], mac[4], mac[5]);
  Serial.printf("arduino_core=%d.%d.%d idf=%s\n", ESP_ARDUINO_VERSION_MAJOR, ESP_ARDUINO_VERSION_MINOR,
                ESP_ARDUINO_VERSION_PATCH, esp_get_idf_version());
  Serial.printf("reinicio=%s\n", motivoReinicio(esp_reset_reason()));
  Serial.println("motores=sin PWM (este sketch nunca los mueve)");
}

static void pines() {
  for (int pin : PINES_SERVOS) {
    pinMode(pin, INPUT_PULLDOWN);
    delay(5);
    Serial.printf("GPIO %d = %d\n", pin, digitalRead(pin));
    pinMode(pin, INPUT); // alta impedancia: no entrega corriente al servo
  }
  Serial.println("Nota: 0 es lo esperado con un servo en reposo; 1 sugiere un corto o una senal ajena.");
}

void setup() {
  Serial.begin(115200);
  delay(300);
  linea.reserve(80);
  informe();
  Serial.println("Ordenes: INFO, PINES, ECO <texto>");
}

void loop() {
  while (Serial.available()) {
    char c = (char)Serial.read();
    if (c == '\n') {
      linea.trim();
      String orden = linea; orden.toUpperCase();
      if (orden == "INFO") informe();
      else if (orden == "PINES") pines();
      else if (orden.startsWith("ECO ")) Serial.println("ECO " + linea.substring(4));
      else if (linea.length()) Serial.println("Ordenes: INFO, PINES, ECO <texto>");
      linea = "";
    } else if (c != '\r' && linea.length() < 78) linea += c;
  }
  delay(2);
}
