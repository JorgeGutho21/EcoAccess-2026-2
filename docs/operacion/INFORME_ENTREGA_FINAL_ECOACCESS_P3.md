# Informe de entrega final — EcoAccess P3 (11/11/2026, 21:10)

**Equipo:** Jorge Gutiérrez Thomas, Heyder Canchila y Saray Cadena.
**Repositorio:** `JorgeGutho21/EcoAccess-P3`, rama `desarrollo/telegram-doble-ia`, PR #11.

El informe anterior (cierre técnico, commit `dd7f12c`) queda en el historial de Git.

Estados que se usan en este informe:

- VERIFICADO REAL
- VERIFICADO EN SIMULACIÓN
- IMPLEMENTADO SIN VALIDACIÓN EXTERNA
- BLOQUEADO
- PENDIENTE

**No se declara «100 % funcional».** Siguen abiertos:
- un fallo intermitente de concurrencia (408 con 30 personas en la serie paralela);
- las pruebas físicas;
- las pruebas desde el teléfono real.

Detalle en la sección 24.

## 1. Estado de Git

- La rama del PR integra el trabajo que estaba sin publicar en `EcoAccess-P3-auditoria`: 4 commits llevados a la rama del
  PR.
- Antes de tocar nada se respaldó en `_respaldos/respaldo_integracion_20261111_1957`, fuera del repositorio:
  - los parches de los dos árboles de trabajo;
  - el bundle de los commits;
  - los archivos sin seguimiento.
- Además queda un `stash` con los cambios previos.
- `scripts/docker/` y `scripts/pruebas/Ejecutar-Integracion-Temporal.ps1` **no se publicaron**: son archivos locales de
  otra persona. En el árbol de auditoría siguen dentro de su commit local `26ffdba`, que no se subió.
- Sin `reset --hard`, sin limpieza destructiva y sin `force push`.

## 2. Estado del PR #11

Abierto y listo para revisión (no es borrador). CI verde en `a1af3e6` (compilar y hardware). **No se fusiona**: el 408 de
concurrencia sigue abierto.

## 3. Cambios publicados en esta sesión

| Commit | Contenido |
|---|---|
| bce0fde, 3cf9c97, 99032df, 1066d91 | regla de objetos confundibles, ronda 6, fuentes 08/11/16, guía de tres canecas, guías de Visual Studio y de alojamiento (traídos de auditoría) |
| 19f8aff | validación del sujeto vivo (persona o animal) antes de ofrecer depositar |
| 344afe1 | formulario de registro móvil y política ciudadana de 6 a 20 caracteres |
| 1fa52ac | pescado cocinado, huevos, carne cruda y pregunta «¿Deseas desechar estos alimentos?» |
| c843ec1 | contraseñas ficticias de integración adaptadas a la política nueva |
| 1f8c30b | diagnóstico del 408 (servidor y cliente de prueba) |
| commit de documentación | ronda 7, regresión, PROGRESO, este informe y fuentes 08 y 11 |

## 4. Pruebas unitarias

1 864 superadas, 0 fallidas, 1 omitida (crear un enlace simbólico requiere permisos de Windows). La cifra de 1 798
reportada antes se superó al agregar las pruebas nuevas.

## 5. Integración con Oracle

Esquema aislado `ECOACCESS_P3_PRUEBAS`, 3 pasadas paralelas completas:

| Pasada | Superadas | Fallidas | Omitidas |
|---|---|---|---|
| 1 | 212 | 0 | 7 |
| 2 | 211 | 1 | 7 |
| 3 | 211 | 1 | 7 |

Las 7 omitidas requieren el modelo real o datos que la prueba no crea; no cuentan como aprobadas. La integración completa
contra Oracle Cloud no se repitió en esta sesión.

## 6. Concurrencia

- 1, 10 y 20 personas: superadas en las 3 pasadas.
- 30 personas: **fallan en 2 de 3 pasadas con 408**.
- Descartados con medición:
  - hilos de trabajo agotados;
  - cola del servidor;
  - `Expect: 100-continue`;
  - Nagle;
  - pausas del GC.
- Hipótesis principal: la concurrencia del puerto IOCP se satura con trabajo de CPU.
  - Datos: 8 hilos de E/S ocupados, igual a los 8 núcleos, y la CPU entre el 63 % y el 75 %.
  - Confirmarla requiere ETW con permisos de administrador.
- Evidencia: `docs/pruebas/resultados_cierre/diagnostico_408`.
- **PENDIENTE.**

## 7. Registro móvil

- Una columna, etiquetas encima y los 4 campos del mismo ancho (303 px a 375 px), sin desplazamiento horizontal.
- Errores junto a su campo; el botón «Mostrar» va dentro del campo.
- En producción desde las 20:40, servido por el bot.
- Probado en una emulación de teléfono. **Falta el iPhone real del propietario.**

## 8. Política de contraseñas

- Ciudadanos nuevos: de 6 a 20 caracteres, símbolos opcionales y rechazo de las muy comunes.
- Personal sin cambios: de 12 a 128 caracteres.
- Riesgo aceptado y documentado en `PoliticaContrasenaCiudadano`.

## 9. Autenticación

- PBKDF2 con sal propia; bloqueo de 15 minutos tras 5 fallos; respuesta idéntica ante cualquier fallo.
- Probado: una cuenta antigua con contraseña de 25 caracteres sigue entrando, y un usuario bloqueado no entra.
- No se inició sesión con cuentas reales.

## 10. Persistencia de cuentas

- El registro crea solo ciudadanos, nunca cuentas privilegiadas.
- Un correo existente no crea otra cuenta, y un envío duplicado crea una sola.
- Verificado contra el Oracle aislado: 26 pruebas de registro, carga y vinculación.

## 11. Vinculación con Telegram

- La página prepara el código y solo el bot confirma el segundo canal.
- Una cuenta existente recupera su identidad y su historial, sin crear otra.
- Verificado en simulación y contra el Oracle aislado.

## 12. Reconocimiento de alimentos

- Pescado cocinado: 3 de 3 leídos como comida y ninguno como animal (ronda 7).
- Huevos y comida servida reconocidos.
- Eco pregunta si se van a desechar; con «Sí», orienta a la caneca verde con 0 ecopuntos.

## 13. Protección ante personas y animales

- Validación independiente del sujeto vivo antes de cualquier oferta.
- Ronda 7: 0 ofertas y 0 aperturas para 2 personas y 2 animales.
- Costo: 1 falso rechazo, una mano sosteniendo PET.

## 14. Reconocimiento de PET

- Ronda 7: 1 de 2 correctos (abriría la blanca); el otro, sostenido con la mano, fue un falso rechazo seguro.
- Botella Cielo del propietario: reconocida como PET y caneca blanca, según su informe.

## 15. Clasificación ambiental

- Blanca para aprovechables, verde para orgánicos y negra para no aprovechables (`ResolutorDestino`).
- Icopor y metalizados solo reciben orientación.
- Peligrosos y RAEE reciben su propia orientación.

## 16. QR compartido

`EcoAccessCanecaPiloto1`, sin cambios.

## 17. Ventana de 10 segundos

- Implementada y probada con el simulador y el bridge:
  - `VentanaDepositoSegundos`;
  - `DECLARADA_SIN_SENSOR`;
  - idempotencia (`EstacionSimuladaTests`).
- El bot en vivo corre en **modo software** (sin dispositivo): responde «Modo demostración: apertura física pendiente» y
  no abre la sesión.
- Pendiente:
  - la ventana lógica de 10 s sin dispositivo;
  - el aviso de fin sin mensaje del usuario;
  - la persistencia que sobreviva a un reinicio en ese modo.

## 18. Registro en Oracle

- Depósito declarado sin sensor (V016), consultas de visión (V023) y lote experimental de 100 sin duplicar.
- Verificado en simulación y con las consultas SQL del cierre.

## 19. Ecopuntos

- 7 tarifas vigentes en Oracle Cloud.
- Los orgánicos no dan puntos; la acreditación exige verificación y es idempotente.
- Nadie ha acreditado puntos reales.

## 20. Power BI

`reportes/powerbi/EcoAccess.pbix`, de 5 páginas, validado con SQL (commit 8ff0be6). No se rehízo.

## 21. Windows Forms

- Accesos Ciudadano, Operario y Supervisor (8603578).
- El registro de escritorio usa la política ciudadana nueva.
- La aplicación compila y abre la pantalla de inicio.

## 22. ESP32

- Firmware compilado en CI: una tapa, tres tapas y calibración.
- Simulación probada.
- **No se cargó en un dispositivo ni se movió ningún motor.**

## 23. Documentación en Word y PDF

- Fuentes actualizadas: 00, 08, 11, 12, 14 y 15, más las rondas 6 y 7, la regresión y PROGRESO.
- La regeneración de los Word y PDF afectados se registra en el commit de documentación.

## 24. Bloqueos pendientes

| Pendiente | Quién | Cómo |
|---|---|---|
| 408 con 30 personas en la serie paralela | desarrollo | trazas ETW (permisos de administrador) para confirmar la hipótesis IOCP; luego mover el hash fuera de las continuaciones de E/S y medir otra vez |
| Ventana de 10 s en el bot en vivo (modo software) | desarrollo | estado de ventana persistido en Oracle y aviso programado al vencer |
| Prueba del formulario en el iPhone | propietario | abrir `/vincular` en Telegram y registrarse con la versión publicada |
| Foto real por Telegram (pescado, huevos, PET) | propietario | enviar las fotos desde el teléfono al bot en vivo |
| Cuentas de Heyder y Saray | administrador 4521 | `FormUsuarios` con sus correos reales (no se inventan) |
| Visual Studio (`0x80070005` en `C:\ProgramData\Microsoft\VisualStudio`) | propietario | Instalador > Reparar con permisos de administrador (`docs/operacion/GUIA_VISUAL_STUDIO.md`) |
| Alojamiento 24/7 | propietario | iniciar sesión en OCI y conseguir capacidad A1 (`ALOJAMIENTO_24_7_EVALUACION.md`) |
| Apertura física de las tres tapas | equipo presente | guía ESP32; no hay pruebas desatendidas |
| Groq | propietario | clave propia |

## Matriz de requisitos

| REQUISITO | IMPLEMENTADO | PROBADO | RESULTADO | EVIDENCIA | COMMIT | ESTADO |
|---|---|---|---|---|---|---|
| Trabajo pendiente recuperado sin pérdidas | sí | comparación de árboles | árbol del PR = árbol de auditoría (sin docker) | `_respaldos/respaldo_integracion_20261111_1957` | 1066d91 | VERIFICADO REAL |
| Persona o animal no ofrece depositar | sí | unitarias y ronda 7 | 0 ofertas en 4 fotos de personas y animales | EVALUACION_RONDA7 | 19f8aff | VERIFICADO EN SIMULACIÓN |
| Pescado cocinado no es animal | sí | unitarias y ronda 7 | 3/3 como comida | EVALUACION_RONDA7 | 1fa52ac | VERIFICADO EN SIMULACIÓN |
| Huevos y comida con pregunta | sí | unitarias y ronda 7 | reconocidos; pregunta y 0 ecopuntos | AlimentosContextoTests | 1fa52ac | VERIFICADO EN SIMULACIÓN |
| Formulario móvil | sí | unitarias y emulación a 375 px | una columna; mismo ancho | docs/evidencias/registro_movil | 344afe1 | IMPLEMENTADO SIN VALIDACIÓN EXTERNA |
| Contraseñas ciudadanas de 6 a 20 | sí | unitarias e integración | 6/20 aceptadas; 5/21/comunes rechazadas | RegistroMovilTests | 344afe1, c843ec1 | VERIFICADO EN SIMULACIÓN |
| Cuentas antiguas siguen entrando | sí | unitaria | 25 caracteres entra | RegistroMovilTests | 344afe1 | VERIFICADO EN SIMULACIÓN |
| Persistencia y vinculación de ciudadanos | sí | Oracle aislado | 26/0/1 | REGRESION_ENTREGA_FINAL | c843ec1 | VERIFICADO EN SIMULACIÓN |
| HTTP 408 con 30 personas | diagnóstico | 3 pasadas completas | falla en 2/3 | diagnostico_408 | 1f8c30b | PENDIENTE |
| Ventana de 10 s con dispositivo | sí | simulador | 10 s, declarada sin sensor, idempotente | EstacionSimuladaTests | anteriores | VERIFICADO EN SIMULACIÓN |
| Ventana de 10 s en modo software | no | — | «Modo demostración» | — | — | PENDIENTE |
| Ecopuntos (7 tarifas, orgánicos 0) | sí | Oracle Cloud y unitarias | sin duplicados | t001_oracle_cloud.txt | 1fd5b7d | VERIFICADO REAL |
| Power BI de cinco páginas | sí | cruce SQL | coincide | docs/evidencias/powerbi_p3 | 8ff0be6 | VERIFICADO REAL |
| Accesos Ciudadano, Operario y Supervisor | sí | unitarias | permisos por servicio | AreasAcceso | 8603578 | VERIFICADO EN SIMULACIÓN |
| Cuenta de Jorge con dos roles | sí | consulta en Oracle | roles 2 y 3 (14:50) | roles_equipo_oracle_cloud.txt | 1fd5b7d | VERIFICADO REAL |
| Cuentas de Heyder y Saray | no | — | faltan sus correos | — | — | BLOQUEADO |
| Visual Studio carga los 9 proyectos | no | acceso a ProgramData | sin acceso | GUIA_VISUAL_STUDIO.md | 1066d91 | BLOQUEADO |
| Firmware ESP32 compilado | sí | GitHub Actions | 3 programas compilan | CI hardware | a1af3e6 | VERIFICADO REAL |
| Apertura física | firmware | no | — | — | — | PENDIENTE |
| Bot en vivo con la versión nueva | sí | GET local de `/registro` | plantilla y CSP nuevas | consola | 1fa52ac | VERIFICADO REAL |
| Prueba desde el iPhone | — | no | — | — | — | PENDIENTE |
| Alojamiento 24/7 | no | — | sin VM | ALOJAMIENTO_24_7_EVALUACION.md | 1066d91 | BLOQUEADO |
| Documentación actualizada | sí | revisión | fuentes y evidencias | docs/ | commit de documentación | IMPLEMENTADO SIN VALIDACIÓN EXTERNA |
