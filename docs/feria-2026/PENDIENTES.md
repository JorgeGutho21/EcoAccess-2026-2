# Pendientes del lote de feria

Ordenados por importancia. Ninguno se ejecutó desde el PC de Heyder: los que escriben en Oracle requieren autorización del
responsable de la base de datos.

## P1 · Separar el lote de los «Datos operativos» del Dashboard institucional (decisión del equipo)

**Hecho:** las vistas `EA_R_BASE_DEPOSITOS`, `EA_R_BASE_PUNTOS` y `EA_R_SOLICITUDES` calculan `es_ensayo` solo con
`canal IN (3,4)`, nombres `%SIMULACI%`/`%FICTIC%` o códigos `BR_/IT_/TEST_/PRUEBA_`. No cruzan `REGISTRO_SIMULACION`. El conteo de
ciudadanos de `RepositorioInformeInstitucionalOracle.cs:55` usa la misma regla.
**Efecto medido:** últimos 100 días, alcance «Datos operativos» = 631 depósitos, de los que **620 son del lote**
(canales Telegram, Manual y Asistido). Solo 30 depósitos del lote (canal Simulador) quedan en «Demostración y simulaciones».
**Opciones:**
- (a) Migración V036 no destructiva (`CREATE OR REPLACE VIEW`) que añada `OR EXISTS (SELECT 1 FROM registro_simulacion …)` a
  `es_ensayo` en esas tres vistas, y el mismo criterio en `RepositorioInformeInstitucionalOracle`. El lote se vería entero en
  «Demostración y simulaciones» y los datos operativos volverían a ser solo los reales. Requiere probarla en el esquema aislado
  y autorización para aplicarla en la nube.
- (b) Dejarlo así y advertirlo al presentar (la guía de demostración ya lo dice).
- (c) Revertir el lote (`PROCEDIMIENTO_REVERSION.md`) y regenerarlo solo con canales 3/4.

## P2 · Aperturas sintéticas en las canecas reales

**Hecho:** cada depósito del lote tiene acuse, **apertura** y cierre sin sensor en `EVENTO_DISPOSITIVO` (1950 eventos, `id_externo`
`sim-…`) sobre las canecas 1093/1400/1401. En 90 días: 650 aperturas del lote frente a 40 reales.
**Efecto:** `RepositorioEstadisticasOracle.cs:60-100` (aperturas, eventos e interacciones por canal) los cuenta sin cruzar
`REGISTRO_SIMULACION`; `canal = 1` se presenta como Telegram (`i_telegram`) aunque sea sintético.
**Opción:** el mismo criterio que P1 en esas consultas (cambio de código C#, sin tocar interfaces), o la opción (c) de P1.
`RepositorioNotificacionesTelegramOracle.cs:29-34` también clasifica por canal; las cuentas del lote no tienen Telegram
vinculado (0 vínculos), así que no se envían mensajes.

## P3 · Publicar el generador del PC de Jorge

`tests/herramientas/EcoAccess.P3.DatosFeria` no está en ninguna rama de GitHub (`git fetch --all` del 2026-11-26). Desde el PC de
Jorge: `git status`, crear rama a partir de `feature/ecoaccess-datos-feria-2026`, revisar que no incluya contraseñas ni la
cadena de conexión y hacer push. Sin eso no se puede reproducir ni revisar cómo se generó el lote.

## P4 · Credenciales de las 64 cuentas

Se generaron en el PC de Jorge. No deben regenerarse desde otro equipo (cambiaría contraseñas de cuentas existentes).
`ECOACCESS_CREDENCIALES_FERIA_2026.txt` debe salir de ese PC. Para pasarlo a otro integrante: archivo comprimido con cifrado
AES-256 (p. ej. 7-Zip) y la contraseña del archivo por un canal distinto (en persona o llamada); nunca por GitHub, el chat del
grupo ni en informes. Pendiente comprobar: que el archivo existe en el escritorio de Jorge y que cada cuenta inicia sesión con
`ServicioAutenticacion.IniciarSesionPublicaAsync` (prueba que debe hacerse en ese PC, donde están las contraseñas).

## P5 · Ensayo aislado no realizado

El lote se cargó directamente en la nube. No hay evidencia en el repositorio de un ensayo previo en `ECOACCESS_P3_PRUEBAS`
(Oracle FREE del PC de Jorge). En el PC de Heyder Oracle XE está detenido y no hay credenciales del esquema aislado.
Si se regenera el lote, ensayarlo antes allí: generar, repetir (sin duplicados), revertir con T004 y comprobar que lo previo queda.

## P6 · Fechas de los Ecopuntos

`PKG_ECOPUNTOS` fecha cada operación con la hora del servidor; `ServicioEcopuntos` usa el reloj inyectado. Comprobar en el PC de
Jorge qué ruta usó el generador: si fue el paquete, los 173 movimientos tienen fecha de hoy y la «Evolución» de Mis ecopuntos los
mostrará todos en noviembre 26.
