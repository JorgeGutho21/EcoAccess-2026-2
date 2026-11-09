# Matriz de validación final (2026-11-09)

Solo resultados obtenidos. «No aplica» cuando el componente no se compila o no tiene simulación. Una simulación nunca reemplaza una prueba real.

| Componente | Compila | Prueba simulada | Prueba real | Estado |
|---|---|---|---|---|
| Oracle Cloud | N/A | No aplica | **Sí**: 19c EE, PDB `G5AB3C4722E812D_ECOACCESS`, consultas reales, 25 versiones aplicadas | **Funcional** |
| SQL Developer | N/A | No aplica | Instalado (26.2); conexión documentada; **la comprobación visual la hace el propietario** | Instalado, sin verificar en pantalla |
| Windows Forms | Sí (0 errores) | No aplica | Abre «Bienvenida» contra Oracle Cloud sin Docker; formularios de administrador no probados en pantalla (falta primer administrador) | Parcial |
| Registro | Sí | Pruebas de lógica | Usuario 244 registrado desde C# en Oracle Cloud; correo repetido rechazado; inicio de sesión correcto | **Funcional** |
| Telegram | Sí | Pruebas de lógica y bridge | Identidad y polling reales; 6 mensajes reales con la línea anterior sobre la misma base; bot P3 en polling sin mensajes de teléfono todavía; registro/vinculación sin página HTTPS | Parcial |
| LFM3-VL | Sí | Clasificador simulado en pruebas | 3/3 fotos fijadas; 60 fotos con catálogo real: 0 fallos, P95 3,27 s, 28/60 categorías correctas, 19 al Plan B | **Funcional con confirmación** |
| QR | Sí | Pruebas de `CodigoQrCaneca`/`EnlaceTelegram` | QR DEMO configurado; no escaneado con teléfono en esta recuperación | Sin verificar en real |
| Caneca blanca | Sí | **Sí** (orden propia aceptada; la de otras rechazada) | Configuración y destino en Oracle Cloud; **sin hardware** | Lógica y datos funcionales; física pendiente |
| Caneca verde | Sí | **Sí** | Ídem | Ídem |
| Caneca negra | Sí | **Sí** | Ídem | Ídem |
| Estadísticas | Sí | Pruebas de lógica | Pruebas de integración de estadísticas e informes contra Oracle Cloud superadas; dashboard no abierto con administrador | Datos funcionales; pantalla sin verificar |
| Concurrencia | Sí | Pruebas de concurrencia del proyecto | 10 sesiones Oracle simultáneas; 5 fotos simultáneas en cola (16 s la última); `CargaUsuariosOracleTests` superada contra Oracle Cloud | **Funcional** (visión en serie) |

## Impedimentos que requieren al propietario

1. **Primer administrador**: Bienvenida → «Configuración inicial: contraseña del primer administrador».
2. **Prueba del bot P3 desde el teléfono**: escribir a @EcoAccessP3_JG_bot mientras el bot está en polling; enviar una foto.
3. **Hardware**: tres ESP32, servos, fuentes y calibración (ver `docs/hardware/`).
4. **HTTPS público** para registro/vinculación y despliegue sin el computador personal.
5. **Cuentas `ECO_DEV_*`** de los compañeros (`crear-integrante`) y entrega privada del wallet.
