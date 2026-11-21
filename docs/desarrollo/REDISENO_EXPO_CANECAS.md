# Rediseño ExpoSoftware: inicio, acceso y catálogo de 15 residuos

Guía para los integrantes del equipo. Explica qué cambió en la aplicación de escritorio, cómo actualizar su copia y cómo verificarla.
Plataforma sin cambios: **.NET Framework 4.8**, Windows Forms, Oracle Cloud (proveedor ODP.NET; no se reemplazó por SQL Server).

## 1. Actualizar tu copia

```powershell
git switch main
git pull
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
```

Si programas en Visual Studio basta con abrir `EcoAccess.P3.sln`, compilar y ejecutar `EcoAccess.P3.Presentacion`.
**Todas las imágenes están en el repositorio** (`assets/imagenes/`, unos 2,9 MB) y se incrustan en el ejecutable como
recursos: no dependen de OneDrive ni de rutas de tu equipo. No hay contraseñas ni tokens nuevos; la conexión a Oracle sigue
igual (wallet fuera del repositorio y contraseña guardada en tu perfil de Windows).

## 2. Qué cambió

| Pantalla | Cambio |
|---|---|
| Acceso | Barra verde «Universidad Popular del Cesar · EcoAccess · Sistema de Gestión de Residuos»; a la izquierda ECO en la UPC y, tras un borde curvo, un panel verde con el logotipo de EcoAccess, el lema «Reciclar hoy, construye un mejor mañana.» y las tres canecas (imágenes en `assets/imagenes/eco/`); tarjeta blanca con *Tipo de acceso* (Ciudadano, Operario, Supervisor), correo y contraseña con texto de ayuda, **Iniciar sesión**, **Continuar como invitado** y **¿Aún no tienes una cuenta? Regístrate**. Franja inferior con cuatro beneficios cuando hay alto suficiente. La tarjeta se ajusta a su contenido y en 1366×768 y 1600×900 se compacta para que «Regístrate» se vea sin desplazar. |
| Inicio | Barra superior blanca (logo, rol, nombre, **Cerrar sesión**), portada verde con «¡Hola, nombre!», ECO con la botella y la foto del campus, cuatro indicadores reales (ecopuntos, depósitos, última actividad, participación), fila de estado con la hora de la última actualización, panel **¿Dónde va tu residuo?** con buscador, las tres canecas seleccionables, el resultado de la búsqueda y «Así de fácil» en tres pasos. |
| Canecas | Cada tarjeta (verde, blanca, negra) se elige con clic o teclado y abre la lista de sus residuos con foto. |
| Depósito | Asistente de cuatro pasos: Residuo → Caneca → Depositar → Resultado. |
| Mis ecopuntos | Indicadores de ecopuntos verificados, puntos obtenidos, depósitos y pendientes; estado vacío con ECO; actualización automática. |

Usa todo el ancho de la ventana. A 1920×1080 el contenido esencial cabe sin barra de desplazamiento vertical; a 1366×768
la página se desplaza en vertical, pero nunca en horizontal.

## 3. Catálogo del prototipo (15 residuos, 3 canecas)

Definido en `src/EcoAccess.P3.Logica/Servicios/CatalogoPrototipo.cs`. Cada residuo tiene id, nombre, caneca, descripción,
instrucciones, imagen, código de material Oracle, sinónimos y disponibilidad.

| Caneca | Residuos | Material Oracle |
|---|---|---|
| Verde (orgánicos, 1400) | cáscara de banano, cáscara de naranja, hojas secas, medio kiwi, cáscara de mandarina, medio limón | `ORGANICO` |
| Blanca (aprovechables, 1093) | botella pequeña de agua, botella pequeña de gaseosa (`PET`), hoja de cuaderno arrugada (`PAPEL`), caja de jugo Hit (`MULTICAPA`), vasos plásticos limpios (`PLASTICO_RIGIDO`) | según residuo |
| Negra (no aprovechables, 1401) | papel higiénico usado, servilletas usadas (`SERVILLETA_USADA`), vaso desechable contaminado (`PLASTICO_SIN_ID`), plato de icopor con comida (`ICOPOR_CONTAMINADO`) | según residuo |

- La búsqueda no distingue mayúsculas ni tildes («limon» encuentra «Medio limón») y reconoce sinónimos.
- No hay una cuarta categoría: pilas, medicamentos, aceite y similares muestran una advertencia y **no se asignan a ninguna
  caneca**.
- Todos los materiales ya están admitidos en la estación, así que **no hace falta migración de base de datos**.
- `ServicioCatalogoAmbiental.ResolverPrototipoAsync` comprueba en Oracle que la caneca de la estación esté en servicio y
  admita el material; si no, rechaza el depósito y nunca lo envía a otra caneca.

## 4. Depósito y tiempos reales

1. **Residuo:** se elige (o llega preseleccionado desde la búsqueda).
2. **Caneca:** se muestra la caneca correcta, sus instrucciones y su estado en Oracle.
3. **Depositar:** se crea la orden de apertura con la misma clave idempotente de antes. Los hitos son *Solicitud enviada*,
   *Preparando apertura*, *Caneca disponible para depósito*, *Depósito registrado* y *Cierre confirmado*. El estado viene de
   Oracle y del acuse del dispositivo, no de una animación simulada.
4. **Resultado:** la cuenta regresiva usa **10 segundos** (`ABIERTO_MS` del firmware, `MensajesTelegram.VentanaDepositoSegundos`)
   y empieza cuando Oracle registra la apertura. **No se otorgan ecopuntos sin verificación.**

## 5. Actualización automática

`PoliticaSincronizacion` (Lógica): cada 30 s; tras fallos espera 60 s y luego 120 s como máximo. No consulta con la ventana
minimizada y nunca lanza dos consultas a la vez. También actualiza al volver a la ventana y al cerrar el asistente de
depósito. El botón «Actualizar mi estado» es opcional.

## 6. Roles

Sin cambios en las reglas: el *Tipo de acceso* elegido se contrasta con el rol real de la cuenta al iniciar sesión. El invitado
puede orientarse y depositar, pero no acumula ecopuntos ni entra a la administración.

## 7. Pruebas

```powershell
dotnet build EcoAccess.P3.sln -c Release
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug
```

Resultado en la rama del rediseño: **2.217 correctas, 0 con error, 1 omitida** (la prueba de enlace simbólico necesita
privilegios de Windows). Pruebas nuevas: `CatalogoPrototipoTests` y `DestinoPrototipoTests`. La integración contra el
esquema aislado `ECOACCESS_P3_PRUEBAS` dio 236 correctas, 7 omitidas y 0 con error.

### Capturas automáticas

`tests/visual/CapturasExpo` abre los formularios reales a 1366×768, 1600×900 y 1920×1080 con una conexión Oracle
inalcanzable a propósito (no usa contraseñas) y revisa que ningún control se salga de su contenedor:

```powershell
dotnet build tests\visual\CapturasExpo -c Debug -o "$env:TEMP\capturas_expo_bin"
& "$env:TEMP\capturas_expo_bin\CapturasExpo.exe" "$env:TEMP\capturas_expo"
```

Las capturas de esta entrega están en `docs/evidencias/rediseno_expo/` con su `informe_capturas.txt`.

## 8. Regenerar las imágenes (solo si cambian los originales)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\documentacion\Preparar-ImagenesEco.ps1 -Origen "<carpeta con las imágenes originales>"
```

Recorta, reduce y quita el fondo blanco de ECO y genera las 15 fotos de residuos (420 px) en `assets/imagenes/`. Los
originales no están en el repositorio; las versiones reducidas sí.

## 9. Errores frecuentes

| Síntoma | Solución |
|---|---|
| `No se puede copiar ... EcoAccess.P3.Presentacion.exe porque lo usa otro proceso` | Cierra la aplicación o el bot que esté abierto, o compila a otra carpeta con `-o`. |
| No aparecen las imágenes | Compila de nuevo: se incrustan desde `assets/imagenes/` (comprueba que la carpeta llegó con `git pull`). |
| «Sin conexión con Oracle» en el inicio | Revisa el wallet y la contraseña (guía de instalación). La aplicación reintenta sola cada 30–120 s. |
| La caneca aparece «fuera de servicio» | Es el estado real en Oracle; un operario debe habilitarla. El sistema no envía el residuo a otra caneca. |

## 10. Limitaciones conocidas

- La aplicación se ejecuta con DPI 96 aunque Windows esté al 125 %: las medidas se probaron así en 1366, 1600 y 1920.
- El estado de la caneca se infiere de Oracle (`ORDEN_APERTURA`, `EVENTO_DISPOSITIVO`) y del acuse del dispositivo; no hay un
  sensor de tapa.
- Las pruebas físicas con las tres canecas (ESP32, servos) **no se hicieron en esta entrega**: quedan pendientes con la
  estación conectada.
