# Guía de pruebas supervisadas tras la misión nocturna (13/11/2026)

> **Esta noche no hubo ningún movimiento físico.** No se movió ningún servo, no se cargó firmware, no se reinició el ESP32 y
> no se inició ningún bridge. Todo lo nuevo se probó con software y simuladores.
>
> Esta guía es para el propietario, **presente junto a la estación**. No la ejecutes a distancia.

## Antes de empezar

| Comprobación | Cómo | Debe dar |
|---|---|---|
| Rama | `git log --oneline -1` en `EcoAccess-P3` después de `git pull` | el último commit de `desarrollo/telegram-doble-ia` |
| Nada de la noche quedó en la estación | Las aperturas múltiples **no están conectadas** al hardware: `CoordinadorAperturasMultiples` rechaza cualquier ejecutor que no sea el simulador | se mantiene igual que ayer |
| Guardia de hardware | En una consola **sin** `ECOACCESS_SIN_HARDWARE`: `echo $env:ECOACCESS_SIN_HARDWARE` | vacío. Si vale `1`, ni el bot ni los bridges podrán abrir |
| Compilación | `dotnet build EcoAccess.P3.sln -c Release` con el bot detenido | 0 errores |

## Paso 1. Estado de la estación

1. Conecta el ESP32 por USB y comprueba que aparece el COM (`Get-PnpDevice -PresentOnly | ? FriendlyName -match 'COM\d'`).
2. Fuente de los servos encendida, tapas despejadas, nadie con las manos dentro.
3. `pwsh -File scripts\desarrollo\Iniciar-Estacion-Fisica.ps1`. El script comprueba el COM libre y la huella del firmware
   (commit `edcbc13`), y deja la sesión en `EcoAccessP3Framework\Desarrollo\sesion.json`.
4. Lista para la prueba si: los tres bridges escuchan, el bot dice «modo físico» y Oracle tiene 1093, 1400 y 1401 en servicio.

## Paso 2. Calibración de 90° (pendiente de tu confirmación)

El ángulo de 90° **no está confirmado**: el firmware conserva la calibración validada (0°/60°) mientras no la cambies tú.

1. Solo con la estación detenida (`scripts\desarrollo\Detener-Demostracion.ps1`), carga tú el banco
   `hardware/calibracion_seis_servos` (reemplaza temporalmente el firmware de la estación; la orden 1683 falló justo por
   dejarlo cargado) y mueve **una tapa a la vez** con `ARMAR` y `S <gpio> <grados>`: blanca 19, negra 18
   y verde 23. Sube de 60° a 90° de 10° en 10°, mirando que nada roce.
2. Anota por tapa el ángulo real alcanzado y cualquier ruido o esfuerzo del servo. `PARAR` al terminar.
3. Vuelve a cargar `hardware/estacion_tres_canecas`. Si decides adoptar 90°, cambia antes `abierto` en su configuración
   privada. Después repite el paso 1: el arranque detecta si quedó el firmware de calibración.

## Paso 3. Validación individual (blanca, negra y verde)

`pwsh -File scripts\pruebas\Probar-Apertura-Firmada.ps1` envía una orden firmada por tapa. Por cada una comprueba:
- que abre **solo** la tapa pedida;
- que cierra a los 10 s;
- y que repetir la misma orden no vuelve a abrir.

Presta especial atención a la **negra (GPIO 18, contenedor 1401)**.

## Paso 4. Foto individual por Telegram

Escanea el QR `EcoAccessCanecaPiloto1` y envía una botella PET. Debe:
- decir «Identifiqué una botella plástica…» (ahora con la descripción concreta cuando el modelo la nombra);
- abrir solo la BLANCA.

Repite con una cáscara de fruta (VERDE) y una servilleta usada (NEGRA).

**Comportamiento nuevo:** un vaso o bandeja de **icopor**, o una **colilla**, ahora se reconocen como no aprovechables. Con el
QR vigente pueden abrir la NEGRA, si Oracle los admite en 1401. Antes el icopor solo orientaba. Pruébalo.

## Paso 5. Foto con dos residuos

Envía una foto con cartón y una servilleta. **Comportamiento esperado de esta versión:** Eco responde un renglón por
residuo («Una caja de cartón → ⬜ BLANCA», «Una servilleta usada → ⬛ NEGRA») y pide depositarlos de uno en uno.
**No abre ninguna tapa por esa foto.** La apertura de dos o tres canecas desde una foto sigue en simulación. Activarla
exige un cambio de código consciente, después de estas pruebas.

## Paso 6. Foto con tres residuos

Repite con cartón, servilleta y cáscara: se esperan las tres líneas BLANCA, NEGRA y VERDE, sin aperturas. Con tres botellas
se espera «Identifiqué 3 residuos» y un solo destino.

## Paso 7. Registros reales en Oracle

Solo lectura, en SQL Developer: `ORDEN_APERTURA` y `EVENTO_DISPOSITIVO` de los pasos 3 y 4, con su contenedor correcto
(1093, 1401 o 1400). Las fotos del paso 5 y 6 **no deben** tener órdenes.

## Paso 8. Usuario nuevo

Desde un Telegram que nunca haya hablado con Eco: `/start`. Debe aparecer:
- «🌱 ¡Bienvenido a EcoAccess!»;
- «✨ Crear mi cuenta», «🔗 Ya tengo cuenta» y «🌱 Continuar como invitado».

Al tocar «Crear mi cuenta»:
- llega el enlace «Paso 2 de 3»;
- los datos se escriben solo en la página HTTPS;
- al volver, Eco confirma la cuenta.

## Paso 9. Usuario existente

Con la cuenta vinculada, `/start` **no** vuelve a ofrecer el registro: saluda y muestra el menú de 8 opciones
(Clasificar residuo, Escanear estación, Mis depósitos, Mis ecopuntos, Mi cuenta, Materiales y reciclaje, Ayuda, Acerca).

## Paso 10. Antes de dejarlo en producción

- [ ] Los pasos 3 a 9 dieron lo esperado.
- [ ] Decidiste el ángulo (60° o 90°) y quedó anotado por tapa.
- [ ] Revisaste el PR #11 y los commits de la misión nocturna.
- [ ] Reiniciaste el bot que corre en `EcoAccess-P3` (el que estaba activo anoche es una compilación anterior; los cambios
      de la noche no están en él hasta recompilar).
