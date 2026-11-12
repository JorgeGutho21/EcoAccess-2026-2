# Guía manual completa de EcoAccess P3, paso a paso y en orden

Esta guía permite montar, probar y operar EcoAccess P3 desde cero en un PC con Windows. Cada paso dice qué hacer, con qué
comando y qué debe verse para continuar. Si un paso no da lo esperado, **no sigas**: ve a «Problemas frecuentes» al final.

> **Seguridad**
> - Los servos solo se mueven cuando alguien envía una foto con el QR vigente, o en el banco de calibración.
> - Ten despejadas las tapas y no metas las manos mientras la estación esté encendida.
> - Las contraseñas, claves y tokens se escriben solo en los asistentes indicados: nunca en el chat ni en Git.

## 0. Requisitos

| Necesitas | Para qué | Cómo comprobarlo |
|---|---|---|
| Windows 10/11, PowerShell 7 (`pwsh`) y Git | Ejecutar los scripts | `pwsh -v`, `git --version` |
| .NET SDK (compila .NET Framework 4.8) | Compilar | `dotnet --version` |
| Wallet de Oracle Autonomous Database | Conectarse a Oracle Cloud | variable `TNS_ADMIN` del usuario |
| Ollama con `lfm3-vl:latest` | Reconocimiento visual local | `ollama list` |
| Arduino IDE 2 con el núcleo ESP32 3.0.7 | Cargar el firmware | Arduino IDE → Placas → esp32 |
| ESP32 Dev Module conectado por USB (CP210x) | Estación física | aparece «Silicon Labs CP210x (COM3)» en el Administrador de dispositivos |
| Token del bot `@EcoAccessP3_JG_bot` | Telegram | lo guarda el paso 2 |

Comprobación automática de todo lo anterior:

```bash
pwsh -File scripts/instalacion/Validar-Entorno.ps1
```

## 1. Código

```bash
git clone https://github.com/JorgeGutho21/EcoAccess-P3.git
```

```bash
git switch desarrollo/telegram-doble-ia
```

## 2. Credenciales, una sola vez y en el perfil privado de Windows

```bash
pwsh -File scripts/instalacion/Configurar-Credenciales.ps1
```

```bash
pwsh -File scripts/instalacion/Configurar-Telegram.ps1
```

El primero guarda la cadena de Oracle y el motor visual; el segundo comprueba con Telegram que el token es del bot oficial.

## 3. Base de datos

```bash
pwsh -File scripts/instalacion/Preparar-BaseDatos.ps1
```

Aplica en orden las migraciones pendientes de `database/06_migraciones`.

**Debe verse:** «sin errores» y la última migración aplicada.

V026 (puntos de entrega de Valledupar) es aditiva. Si todavía no se ha aplicado en producción, el bot usa la misma
información desde el código.

## 4. Compilar y pruebas automáticas

```bash
dotnet build EcoAccess.P3.sln -c Release
```

```bash
dotnet test tests/unitarias/EcoAccess.P3.Tests
```

**Debe verse:** 0 errores de compilación y «Correctas! Con error: 0» (al 12/11/2026: 2083 superadas, 1 omitida).

Las pruebas no pueden tocar el hardware: `GuardiaHardware` bloquea los COM y los comandos a bridges en cualquier proceso de
pruebas.

Integración y carga en el esquema aislado (opcional, unos 4 minutos):

```bash
pwsh -File scripts/pruebas/Ejecutar-Integracion-Esquema.ps1
```

## 5. Visión local

```bash
ollama pull lfm3-vl:latest
```

```bash
src/EcoAccess.P3.Bot/bin/Release/net48/EcoAccess.P3.Bot.exe --estado-vision-local
```

**Debe verse:** `"ListoParaInferencia": true`.

## 6. Firmware de la estación (ESP32)

1. Solo la primera vez, genera claves, bridges y la cabecera privada del firmware:

   ```bash
   pwsh -File scripts/instalacion/Preparar-Estacion-Fisica.ps1
   ```

   Crea `hardware/estacion_tres_canecas/configuracion_privada.h`, que Git ignora, y
   `C:\ProgramData\EcoAccessP3Framework\Bridge\bridge-{blanca,negra,verde}.json`.

2. Revisa en `configuracion_privada.h` la línea de cada tapa: `{pin}, {cerrado}, {abierto}`.

   | Caneca | Contenedor | GPIO | Cerrado | Abierto (12/11/2026) |
   |---|---|---|---|---|
   | Blanca | 1093 | 19 | 0° | **90°** (respaldo de 60° en `C:\ProgramData\EcoAccessP3Framework\Bridge\configuracion_privada_60grados.h.bak`) |
   | Verde | 1400 | 23 | 0° | 90° |
   | Negra | 1401 | 18 | 0° | 90° |

3. **Cierra el Monitor Serie de Arduino IDE** y carga el firmware. Estos son los mismos comandos que se usaron el 12/11/2026:

   ```bash
   "%LOCALAPPDATA%\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe" --config-file "%USERPROFILE%\.arduinoIDE\arduino-cli.yaml" compile --fqbn esp32:esp32:esp32 hardware/estacion_tres_canecas
   ```

   ```bash
   "%LOCALAPPDATA%\Programs\Arduino IDE\resources\app\lib\backend\resources\arduino-cli.exe" --config-file "%USERPROFILE%\.arduinoIDE\arduino-cli.yaml" upload --fqbn esp32:esp32:esp32 -p COM3 hardware/estacion_tres_canecas
   ```

   **Debe verse:** «Hash of data verified». Al arrancar, el firmware no mueve ningún servo.

4. Para calibrar ángulos se usa otro firmware, el banco `hardware/calibracion_seis_servos`:
   - `ARMAR`, `S <gpio> <grados>`, `PARAR`;
   - una tapa a la vez;
   - **al terminar, vuelve a cargar `estacion_tres_canecas`**: si queda el banco cargado, la estación no arranca (orden 1683).

## 7. Encender la estación

```bash
pwsh -File scripts/desarrollo/Iniciar-Estacion-Fisica.ps1
```

El script hace todo en orden:
1. comprueba que COM3 esté libre;
2. comprueba que el ESP32 responde como estación;
3. detiene el bot anterior de esta copia;
4. compila;
5. arranca 3 adaptadores USB, 3 bridges y el bot en modo físico.

**Debe verse:**
- «Responde con desafíos: estacion_tres_canecas cargado»;
- los seis enlaces de 8182 a 8187 en «escucha»;
- «Identidad comprobada: @EcoAccessP3_JG_bot».

Los registros quedan en `C:\ProgramData\EcoAccessP3Framework\Sesiones\estacion-<fecha>`.

Para apagar todo:

```bash
pwsh -File scripts/desarrollo/Detener-Demostracion.ps1
```

**Comprueba que las tres canecas estén EN SERVICIO** en la aplicación de escritorio (área Supervisor → Canecas y QR). Una
caneca fuera de servicio orienta, pero nunca abre.

## 8. Prueba física de las tapas, sin Telegram

Con el propietario presente:

```bash
pwsh -File scripts/pruebas/Probar-Apertura-Firmada.ps1
```

**Debe verse en cada caneca:**
- abre solo la tapa pedida;
- cierra a los 10 s;
- la orden repetida se rechaza.

## 9. Prueba con Telegram, en este orden

| # | Acción | Resultado esperado |
|---|---|---|
| 1 | Desde un Telegram nuevo, `/start` | «🌱 ¡Bienvenido a EcoAccess!» con «✨ Crear mi cuenta», «🔗 Ya tengo cuenta» y «🌱 Continuar como invitado» |
| 2 | «Crear mi cuenta» | Enlace «Paso 2 de 3». Los datos se escriben en la página HTTPS y al volver Eco confirma la cuenta |
| 3 | `/start` otra vez, ya vinculado | Saludo y menú de 8 opciones; **no** vuelve a pedir el registro |
| 4 | Escanear el QR `EcoAccessCanecaPiloto1` | Eco reconoce la estación |
| 5 | Foto de una botella PET limpia | «Identifiqué una botella plástica…» y se abre **solo la BLANCA** 10 s |
| 6 | Foto de una cáscara de fruta | Se abre **solo la VERDE** |
| 7 | Foto de una servilleta usada | Se abre **solo la NEGRA** |
| 8 | Foto con cartón, servilleta y cáscara juntos | Una línea por residuo con su caneca; **no abre nada** y pide uno por uno |
| 9 | Foto de un celular, pilas o zapatos | Ruta (RAEE, pilas, donar) y punto en Valledupar con su advertencia; no abre |
| 10 | Foto de un envase de cloro o de un medicamento | Ruta de residuo peligroso o de medicamentos; **no abre** (comprobación de contenido) |

## 10. Comprobar en Oracle

En SQL Developer, solo lectura:
- `ORDEN_APERTURA` y `EVENTO_DISPOSITIVO` de los pasos 5 a 7, con su contenedor (1093, 1400 o 1401);
- los pasos 8 a 10 **no deben** tener órdenes.

## 11. Repetir la evaluación visual

Así se repite la medición con cualquier banco de fotos:

1. Descargar las candidatas con licencia libre:

   ```bash
   pwsh -File scripts/pruebas/vision/Buscar-FotosCommons.ps1 -Destino <banco>/fotos -Busquedas <busquedas.json> -Excluidos <excluidos.txt>
   ```

2. Hacer las hojas de contacto y etiquetar **a ojo antes de ejecutar el modelo** en `<banco>/etiquetas.txt` (formato en
   `docs/vision/resultados/banco_noche/etiquetas.txt`):

   ```bash
   pwsh -File scripts/pruebas/vision/Crear-HojasContacto.ps1 -Fotos <banco>/fotos -Destino <banco>/hojas
   ```

3. Generar los manifiestos (entrenamiento, validación y prueba, separados por autor):

   ```bash
   pwsh -File scripts/pruebas/vision/Preparar-ManifiestosBanco.ps1 -Banco <banco>
   ```

4. Evaluar cada conjunto. La evaluación solo lee el catálogo: no crea órdenes ni abre canecas.

   ```bash
   src/EcoAccess.P3.Bot/bin/Release/net48/EcoAccess.P3.Bot.exe --evaluar-objetos <banco>/fotos/manifiesto_prueba.json local
   ```

5. Analizar los resultados (matriz de confusión, precisión, recall, F1 y falsas admisiones):

   ```bash
   pwsh -File scripts/pruebas/vision/Analizar-ResultadosBanco.ps1 -Informe <resultado.json> -Manifiesto <manifiesto.json> -Rutas <rutas.csv>
   ```

**Regla:** solo se corrige con el conjunto de entrenamiento. La prueba se mide al final y no se usa para ajustar.

## Problemas frecuentes

| Síntoma | Causa | Solución |
|---|---|---|
| «COM3 está ocupado» | Monitor Serie de Arduino IDE abierto u otra prueba | Ciérralo y repite el paso 7 |
| «El ESP32 no tiene el firmware de estación (responde como: calibracion)» | Quedó el banco de calibración cargado | Paso 6.3 |
| La foto se reconoce pero no abre | Caneca fuera de servicio en Oracle, QR vencido (más de unos minutos) u objeto que solo orienta | Paso 7 (en servicio); vuelve a escanear el QR; revisa la ruta del objeto en `docs/vision/CATALOGO_OBJETOS_COLOMBIA.md` |
| Una tapa roza o el servo hace fuerza a 90° | Ángulo demasiado grande para esa tapa | Detén la estación, restaura el respaldo de 60° o ajusta esa tapa y repite el paso 6.3 |
| «El reconocimiento de fotos no está disponible» | Ollama detenido | Abre Ollama y repite el paso 5 |
| La página de registro tarda con muchas personas a la vez | Carga de CPU | Ya está acotada (como mucho la mitad de los núcleos); espera unos segundos |
| Una prueba falla con «HardwareBloqueadoException» | Una prueba intentó llegar al hardware | Es la protección funcionando: la prueba debe usar el simulador |

## Qué no garantiza esta guía

- **El reconocimiento visual no es perfecto.** Las métricas reales están en `docs/vision/EVALUACION_BANCO_MISION_NOCTURNA.md` y
  en `docs/vision/EVALUACION_BANCO_COLOMBIA.md`.
- Por eso las reglas de seguridad deciden cuándo NO abrir: ante la duda, Eco orienta y no abre.
