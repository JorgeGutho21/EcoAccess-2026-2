# Inicio del ciudadano, catálogo ampliado y depósitos

Guía para los integrantes del equipo (20/11/2026, rama `feature/rediseno-expo-canecas`, PR #19). Explica qué cambió, cómo
probarlo y qué queda pendiente. La plataforma no cambia: **.NET Framework 4.8**, Windows Forms, Oracle Cloud con ODP.NET,
el mismo bot de Telegram y el mismo firmware del ESP32.

## 1. Qué cambió

| Parte | Cambio | Archivos |
|---|---|---|
| Inicio del ciudadano | Rediseño completo con el diseño aprobado: barra con logo y sesión; portada con saludo, ECO con la botella, campus de la UPC y cuatro indicadores reales; «¿Dónde va tu residuo?» con buscador y sugerencias; las tres canecas oficiales pulsables; tarjeta de resultado; «Así de fácil» en cuatro pasos con el avance real. **Nunca necesita desplazamiento.** | `src/EcoAccess.P3.Presentacion/FormExplorarEcoAccess.cs`, `ComponentesInicioCiudadano.cs` |
| Recursos gráficos | Canecas, botella e iconos de pasos recortados de la hoja aprobada con fondo transparente real. | `assets/imagenes/inicio/` |
| Catálogo | Catálogo unificado de **337 residuos**: los del piloto (depositables) más la matriz que ya usa ECO en Telegram (orientación). | `src/EcoAccess.P3.Logica/Servicios/CatalogoResiduos.cs` |
| Piloto | De 15 a **38 residuos depositables** (13 verdes, 12 blancos, 13 negros), solo con materiales que ya existen en Oracle. | `CatalogoPrototipo.cs` |
| Búsqueda | Tildes, mayúsculas, plurales, diminutivos («botellita»), sinónimos y nombres populares («guineo», «chuspa»), coincidencias parciales, errores de escritura («servileta»), sugerencias y desambiguación («vaso»: limpio a la blanca, sucio a la negra). | `CatalogoResiduos.cs` |
| Residuos especiales | Pilas, RAEE, medicamentos, bombillos, peligrosos, cortopunzantes y aceite: se encuentran, **no abren ninguna caneca** y ofrecen **¿Dónde puedo llevarlo en Valledupar?** | `FormPuntosDisposicion` en `ComponentesInicioCiudadano.cs` |
| Actualización automática | Cada 15 s (configurable), al volver a la ventana y al terminar un depósito; datos viejos marcados; estado de Oracle y de la estación a la vista. | `PoliticaSincronizacion.cs`, `App.config` |
| Estación | La tarjeta de resultado consulta en Oracle si la caneca de la estación está en servicio y admite el residuo antes de ofrecer el depósito. Si en el equipo corre el simulador, el inicio muestra **Estación simulada (demo)**. | `FormExplorarEcoAccess.cs` |

Lo que **no** cambió: el asistente de depósito, las órdenes de apertura, el bridge HMAC, el firmware, el bot, la lógica de
ecopuntos y los permisos de cada rol (Ciudadano, Operario, Supervisor, invitado).

## 2. Descargar y compilar

```powershell
git fetch origin
git switch feature/rediseno-expo-canecas
git pull
```

En Visual Studio: abrir `EcoAccess.P3.sln`, compilar y ejecutar `EcoAccess.P3.Presentacion`. Desde la consola:

```powershell
dotnet build EcoAccess.P3.sln -c Release
```

Si la compilación dice que un archivo está «bloqueado por EcoAccess.P3.Bot» o por `EcoAccess.P3.Presentacion`, es porque el
bot o la aplicación están abiertos desde esa carpeta: ciérralos o compila en la otra configuración (Debug/Release).

## 3. Oracle sin compartir secretos

Nada nuevo: el wallet va fuera del repositorio y la contraseña del esquema se guarda solo en tu perfil de Windows (User
Secrets `ecoaccess-p3-framework`), como explica [INSTALACION_COMPANEROS.md](../instalacion/INSTALACION_COMPANEROS.md).
**Nunca** escribas contraseñas, el token del bot ni claves en `App.config` ni en el repositorio.

La única clave nueva es pública y opcional, en `src/EcoAccess.P3.Presentacion/App.config`:

```xml
<add key="EcoAccess.IntervaloActualizacionSegundos" value="15" />
```

Acepta de 10 a 120 s; un valor inválido deja 15 s.

## 4. Telegram, modo demo y estación física

Los tres scripts ya existían; no se creó otro bot.

| Quiero… | Comando (PowerShell, en tu propia ventana) |
|---|---|
| Bot de Telegram con caneca simulada | `scripts\desarrollo\Iniciar-Bot-Eco.ps1` |
| Demostración completa sin hardware (simulador + escritorio) | `scripts\desarrollo\Iniciar-Demostracion.ps1` (con `-ConBot` también el bot) |
| Estación física de tres canecas (ESP32 por USB) | `scripts\desarrollo\Iniciar-Estacion-Fisica.ps1` |
| Detener todo lo anterior | `scripts\desarrollo\Detener-Demostracion.ps1` |

**Modo demo.** El simulador (`SimuladorEstacionTresCanecas`) reproduce apertura, ventana de 10 s, cierre, fallos, falta de
respuesta, órdenes duplicadas y la regla de una sola tapa a la vez. Registra sus acuses en Oracle como cualquier caneca, pero
**no es una prueba física**: el inicio lo identifica con la pastilla **Estación simulada (demo)** y la tarjeta de resultado
avisa que la apertura será simulada. Nada en la aplicación inventa una confirmación: los pasos del asistente se marcan solo
cuando Oracle registra el evento correspondiente.

**Modo físico.** Antes de energizar los servos revisa [hardware/README.md](../../hardware/README.md) y la
[guía de integración de tres canecas](../hardware/GUIA_INTEGRACION_ESP32_TRES_CANECAS.md): alimentación, calibración y que
nadie tenga las manos en las tapas. Correspondencia real (no cambiarla sin revisar Oracle y el firmware):

| Caneca | Contenedor Oracle | QR / identificador | Dispositivo | GPIO |
|---|---|---|---|---|
| Blanca | 1093 | `EcoAccessCanecaPiloto1` (QR oficial impreso) | `esp32-piloto-blanca` | 19 |
| Verde | 1400 | `EcoAccessPilotoInternoVerde1` (interno) | `esp32-piloto-verde` | 23 |
| Negra | 1401 | `EcoAccessPilotoInternoNegra1` (interno) | `esp32-piloto-negra` | 18 |

## 5. Los dos métodos de depósito

**A. QR y Telegram.** Escanear el QR oficial → bot ECO → consulta escrita o foto → ECO dice la caneca → la persona confirma →
Oracle crea una orden única (clave de idempotencia) → el bridge la entrega al ESP32 → la tapa se abre 10 s → depósito
manual → cierre → Oracle registra los eventos → los ecopuntos se acreditan solo cuando un operario o supervisor verifica.

**B. Escritorio.** Buscar o elegir el residuo → la tarjeta muestra la caneca y su disponibilidad real en Oracle →
**Depositar este residuo** → el asistente revalida sesión, rol, caneca y material → orden única → seguimiento de cada hito
según Oracle (Emitida, Enviada, Acusada, Abierta, Detectada, Cerrada; o Vencida/Fallida) → el inicio se actualiza solo.

Ambos usan `ServicioInteracciones` y la misma resolución de la caneca (`ServicioCatalogoAmbiental.ResolverPrototipoAsync`), así
que una caneca fuera de servicio o que no admite el material **no recibe ninguna orden** por ningún canal.

### Cómo interpretar los estados

| Estado de la orden | Significa |
|---|---|
| Emitida / Enviada | Oracle registró la orden y se entregó al bridge. |
| Acusada | El ESP32 (o el simulador) confirmó que la recibió. |
| Abierta | La tapa abrió: hay 10 s para depositar. |
| Detectada | Se registró el depósito (pendiente de verificación). |
| Cerrada | La tapa se cerró. |
| Vencida | Nadie confirmó a tiempo: no hay depósito. |
| Fallida | La caneca informó un fallo: no hay depósito. |

El depósito queda «pendiente de verificación» hasta que un operario o supervisor lo verifica; solo entonces suma ecopuntos.

## 6. Matriz de los nueve escenarios

| Entorno | Verde | Blanca | Negra |
|---|---|---|---|
| Escritorio (lógica, catálogo y destino) | Verificado por pruebas | Verificado por pruebas y contra Oracle real | Verificado por pruebas |
| Telegram (misma tabla MATERIAL) | Coherencia verificada por prueba con los scripts de Oracle | Ídem | Ídem |
| Estación física | **Pendiente de prueba física** | **Pendiente de prueba física** | **Pendiente de prueba física** |

«Verificado contra Oracle real» significa que la app consultó la base en la nube y confirmó que la caneca blanca está en
servicio y admite la botella (captura `10_oracle_real_invitado_1920x1080_125pct.jpg`). No se emitió ninguna orden real en esta
entrega. Las pruebas físicas de apertura, cierre, desconexión, tiempo de espera, órdenes duplicadas y rechazo de órdenes no
autorizadas deben hacerse con la estación conectada y una persona responsable presente; el protocolo está en
[GUIA_VALIDACION_ESP32.md](../hardware/GUIA_VALIDACION_ESP32.md).

## 7. Cómo probar cada caneca desde el escritorio

1. Abre la aplicación e inicia sesión como ciudadano (o «Continuar como invitado»).
2. Busca un residuo de cada caneca: `cáscara de banano` (verde), `botellita` (blanca), `servilleta` (negra).
3. Comprueba que la caneca correcta se resalta, que la tarjeta dice la caneca y que la línea verde de disponibilidad aparece.
4. Pulsa **Depositar este residuo** y sigue los hitos del asistente. Con la estación simulada, verás la pastilla de demo.
5. Busca `pila`, `medicamento` o `aceite de cocina`: no debe aparecer ningún botón de depositar, sino **¿Dónde puedo llevarlo
   en Valledupar?**

## 8. Agregar residuos y sinónimos

- **Un residuo que se pueda depositar:** añade una línea `R(...)` en `CatalogoPrototipo.cs` con un código de material que ya
  exista en Oracle **con la categoría de su caneca** (1 blanca, 2 verde, 3 negra). La prueba
  `CadaResiduoDepositableCoincideConLaCategoriaDeSuMaterialEnLosScriptsDeOracle` lo comprueba contra `database/03_datos_prueba`.
  La imagen es opcional: si no hay, la interfaz usa el icono del material.
- **Un residuo solo de orientación:** añádelo a `CatalogoAmpliado.cs` (lo usa también ECO en Telegram) y, si su nombre está en
  inglés, su nombre y sinónimos en español en el diccionario `Espanol` de `CatalogoResiduos.cs`.
- **Sinónimos:** sin tildes y en minúsculas; el buscador ya resuelve plurales y diminutivos.
- No cambies la caneca de un residuo existente sin revisar la Resolución 2184 de 2019 y la configuración de Oracle.

## 9. Imágenes

- `assets/imagenes/inicio/`: `canecas_tres.png`, `caneca_verde|blanca|negra.png`, `botella_agua.png`, `paso_qr|lupa|caneca|listo.png`
  (PNG con transparencia, recortados de la hoja de recursos aprobada).
- `assets/imagenes/eco/`: ECO, campus de la UPC y logotipo.
- `assets/imagenes/residuos/<caneca>/`: ilustraciones de los 15 residuos originales.

Se incrustan en el ejecutable: no dependen de OneDrive ni de rutas del equipo.

## 10. Pruebas

```powershell
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug
dotnet test tests\integracion\EcoAccess.P3.Datos.Tests -c Debug   # requiere Oracle en tus User Secrets
dotnet build tests\visual\CapturasExpo -c Release
tests\visual\CapturasExpo\bin\Release\net48\CapturasExpo.exe docs\evidencias\rediseno_inicio_ciudadano
```

El arnés de capturas abre las ventanas reales a 1366×768, 1600×900 y 1920×1080 con Oracle inalcanzable a propósito (sin
credenciales) y escribe `informe.txt` con los controles fuera de su contenedor y las barras de desplazamiento.

## 11. Si algo no responde

| Síntoma | Qué hacer |
|---|---|
| Pastilla roja «Sin conexión con Oracle» | Revisa internet y el wallet (`TNS_ADMIN`). La app reintenta sola (15 s, 30 s, 60 s… hasta 2 min) y marca los datos viejos. |
| «No se pudo comprobar la estación» | Oracle no respondió; el asistente lo vuelve a intentar antes de emitir la orden. |
| La caneca «está fuera de servicio en Oracle» | Un supervisor debe habilitarla en **Canecas y QR**; mientras tanto no se envía ninguna orden. |
| El asistente dice «La caneca no respondió» | La orden venció sin acuse: revisa que el bridge o el simulador estén en marcha (`Detener-Demostracion.ps1` y volver a iniciar). No se registró depósito. |
| El ESP32 no abre | Revisa alimentación, COM3 y el registro del bridge según la guía de validación. No fuerces la tapa. |

## 12. Escalas de Windows

El inicio se compone en píxeles a partir del tamaño físico de la ventana, así que la escala de Windows no cambia la
composición. Comprobado: capturas automáticas a 1366×768, 1600×900 y 1920×1080 (arnés a 96 ppp) y la aplicación real en una
pantalla 1920×1080 al 125 %. La escala del 150 % no se pudo probar en este equipo (requiere cambiar la configuración de
pantalla de Windows); como la composición depende solo de los píxeles físicos, una pantalla 1920×1080 al 150 % debe verse
igual que al 100 % o al 125 %. Queda pendiente confirmarlo con una captura real.
