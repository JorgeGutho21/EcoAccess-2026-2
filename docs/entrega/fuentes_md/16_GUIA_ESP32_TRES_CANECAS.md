---
archivo: 16_GUIA_ESP32_TRES_CANECAS
titulo: Guía de integración ESP32 de las tres canecas
subtitulo: Identificadores, dispositivos, configuración del Bridge, protocolo firmado, ventana de diez segundos, eventos, errores y seguridad
codigo: 16
---

# Advertencia

Esta guía sale del código del repositorio (firmware `hardware/servosecoacces`, Bridge en `src/EcoAccess.P3.Bot/Dispositivos`)
y de Oracle Cloud (migraciones V021 y V024). **No se inventaron pines ni conexiones eléctricas.** Tampoco se energizó ningún
motor: la apertura física de las tres tapas sigue **pendiente**, y el equipo debe hacerla presencialmente.

# Integración final: un ESP32 con seis servos y el QR compartido

{{incluir:docs/hardware/INTEGRACION_FINAL_TRES_CANECAS_QR.md}}

# Variante con un ESP32 por caneca

{{incluir:docs/hardware/GUIA_INTEGRACION_ESP32_TRES_CANECAS.md}}
