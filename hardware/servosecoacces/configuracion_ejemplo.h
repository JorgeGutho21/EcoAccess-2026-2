#pragma once
// Copiar como configuracion_privada.h: ese archivo está excluido de Git.
// Usar la identidad, caneca, versión y clave del bridge privado; no crear otro sistema de claves.
static const char* DISPOSITIVO = "pendiente";
static const int ID_CANECA = 0;
static const int VERSION_CLAVE = 1;
static const char* CLAVE_BASE64 = "";
// Confirmar físicamente posiciones, corriente y orientación ANTES de habilitar.
static const bool CALIBRACION_VERIFICADA = false;
static const int CERRADO_1 = 0, CERRADO_2 = 0;
static const int ABIERTO_1 = 90, ABIERTO_2 = 90;
static const unsigned ABIERTO_MS = 10000; // Ventana de depósito: 10 segundos desde la apertura confirmada.
// Solo tras revisar físicamente una ejecución interrumpida; volver a false tras recuperar.
// No borra las reservas de órdenes ni permite repetirlas.
static const bool RECUPERACION_MANUAL_VERIFICADA = false;
