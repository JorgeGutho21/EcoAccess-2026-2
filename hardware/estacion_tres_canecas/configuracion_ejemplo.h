#pragma once
// Copiar como configuracion_privada.h (excluido de Git) y completar con los datos de los TRES archivos
// privados del bridge (uno por caneca). No inventar claves ni pines.
//
// PINES: el archivo del compañero usa 17, 16, 21, 23, 18 y 19, pero NO dice qué par mueve qué tapa.
// Mientras el cableado real no se compruebe tapa por tapa (ver hardware/calibracion_seis_servos),
// los pines quedan en SinPin y esa tapa permanece fuera de servicio: el firmware no la mueve.
//
// Canecas registradas en Oracle Cloud (11/11/2026): blanca 1093 en servicio; verde 1400 y negra 1401
// fuera de servicio hasta la verificación física y la autorización del propietario.
static const ecoaccess::Tapa TAPAS[] = {
  // dispositivo,            caneca, versión, clave,  pines,                                cerrado,  abierto,   calibrada
  {"esp32-piloto-blanca",    1093,   1,       "",     {ecoaccess::SinPin, ecoaccess::SinPin}, {0, 0},   {90, 90},  false},
  {"esp32-piloto-verde",     1400,   1,       "",     {ecoaccess::SinPin, ecoaccess::SinPin}, {0, 0},   {90, 90},  false},
  {"esp32-piloto-negra",     1401,   1,       "",     {ecoaccess::SinPin, ecoaccess::SinPin}, {0, 0},   {90, 90},  false},
};
static const unsigned ABIERTO_MS = 10000; // Ventana de depósito: 10 segundos desde la apertura aplicada.
// Solo tras revisar físicamente una ejecución interrumpida de ESA tapa; volver a false tras recuperar.
// No borra las reservas de órdenes ni permite repetirlas.
static const bool RECUPERACION_MANUAL_VERIFICADA[] = {false, false, false};
