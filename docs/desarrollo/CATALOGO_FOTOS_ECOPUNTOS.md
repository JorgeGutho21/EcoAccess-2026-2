# Catálogo con fotografías y matriz de ecopuntos 2026.11.21-1

Guía para el equipo: qué se integró de las carpetas **CANECA BLANCA**, **CANECA VERDE** y **CANECA NEGRA**, cómo se
recortaron las láminas con varios residuos, cómo se ve en la aplicación, qué quedó pendiente y cómo se aprueba la matriz
de ecopuntos. Plataforma sin cambios: .NET Framework 4.8, Windows Forms y Oracle Cloud.

## 1. Inventario de imágenes

| Caneca | Archivos originales | Láminas | Paneles detectados | Recortes publicados | Fotos individuales | Duplicados excluidos | Inválidas | Pendientes | Residuos únicos | Visibles (con imagen) | Sin imagen |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Blanca | 28 | 1 | 10 | 10 | 22 | 1 | 0 | 4 | 19 | 18 | 1 |
| Verde | 33 | 2 | 17 | 17 | 31 | 0 | 0 | 0 | 22 | 21 | 1 |
| Negra | 40 | 0 | 0 | 0 | 32 | 0 | 0 | 8 | 13 | 10 | 3 |
| **Total** | **101** | **3** | **27** | **27** | **85** | **1** | **0** | **12** | **54** | **49** | **5** |

- **112 fotos** quedan asociadas a un residuo: 85 individuales y 27 paneles recortados de las láminas. Con las 15
  ilustraciones del stand, las galerías suman 127 imágenes.
- Las fotos se revisaron **una por una**: la clasificación sale de lo que muestra cada imagen (o de la etiqueta del panel),
  no del nombre del archivo.
- Los originales no se tocan y **no están en el repositorio**. Las versiones cuadradas de 360 px (JPEG) están en
  `assets/imagenes/residuos/<caneca>/fotos/` y se incrustan en el ejecutable: no dependen de rutas de ningún equipo.
- Archivos de control (en `docs/evidencias/catalogo_fotos/`):
  - [`inventario_fotos.csv`](../evidencias/catalogo_fotos/inventario_fotos.csv): carpeta, archivo, tamaño, SHA-1, estado, residuo y nota de cada original.
  - [`recortes_laminas.csv`](../evidencias/catalogo_fotos/recortes_laminas.csv): lámina, número de panel, residuo, coordenadas `x0, y0, x1, y1` del recorte en el original y archivo generado.
  - `recortes/<caneca>_lamina_<n>.jpg`: cada lámina con el rectángulo y el residuo de cada panel, para revisión visual.
- Los contadores de la aplicación salen del catálogo real (`CatalogoFotos.Contar`, `CatalogoFotos.ContarTodo`), no de
  números escritos en los formularios. Si se agregan residuos o fotos, se actualizan solos.

### Láminas recortadas panel por panel

Ninguna lámina («Guía visual…») aparece completa en la aplicación: cada producto es una foto individual de su residuo.

| Lámina | Cuadrícula | Paneles → residuo |
|---|---|---|
| Guía visual de reciclaje de papel y cartón (blanca) | 2 × 5 | periódico, revista, folletos, cartulina, caja de cartón corrugado, caja de cereal, cubeta de huevos, tubo de cartón, caja de jugo, caja de leche |
| Manos ECO con residuos compostables (verde) | 6 arriba + 5 abajo | cáscara de limón, de aguacate, fruta dañada, trozos de fruta, cáscara de papa, de yuca, de plátano verde, zanahoria, tomate, lechuga, tallos y hojas |
| Residuos orgánicos en manos robóticas (verde) | 3 × 2 | trozos de fruta, cáscara de plátano verde, zanahoria, tomate, lechuga, tallos y hojas |

Cómo lo hace `scripts/documentacion/Integrar-FotosResiduos.py`:

1. **Detecta la cuadrícula real** con las franjas sin contenido: primero las filas y luego las columnas de cada fila (la lámina verde tiene 6 paneles arriba y 5 abajo). Si dos paneles se tocan sin franja blanca, el tramo que mide el doble se parte por la columna de menor contenido.
2. **Quita la etiqueta de texto** de cada panel (la franja vacía entre el dibujo y el rótulo), sin cortar la mano de ECO ni el residuo.
3. **Recorta el fondo sobrante** (margen del 4 %) y centra el contenido en un cuadrado del color del fondo del panel: no se estira ni se deforma.
4. **Valida**: si los paneles detectados no coinciden con los declarados en `COMPOSICIONES`, la lámina se rechaza y no se publica ningún recorte; si un recorte todavía se divide en dos, se marca «rechazado». Las coordenadas se pueden fijar a mano (`'manual'`).
5. **Deduplica**: exacto por SHA-1 entre archivos y perceptual (dHash + color medio) entre las fotos de un mismo residuo. Dos residuos distintos con fotos parecidas **no se fusionan**.

Los residuos nuevos que salieron de los paneles usan códigos que ya existen en Oracle (sin migración): periódico,
revista y folletos → `PAPEL`; cartulina, caja de cereal, cubeta de huevos y tubo de cartón → `CARTON`; las nueve cáscaras y
restos de la lámina verde → `ORGANICO`. En Oracle Cloud se verificó (solo lectura) que esos cuatro materiales existen y
están habilitados.

### Pendientes (no aparecen en el catálogo visual ni se depositan)

| Carpeta | Qué muestra | Por qué queda pendiente |
|---|---|---|
| Blanca | Bolsa plástica | Oracle tiene `BOLSA_PLASTICA`, pero el catálogo del piloto no la incluye |
| Blanca | Bolsa blanca | Material ambiguo (papel, tela o plástico) |
| Blanca | Lata de maíz | Lata de conserva (acero) y llena; el catálogo solo tiene lata de aluminio |
| Blanca | Tapa metálica | Oracle tiene `METAL`, pero el catálogo del piloto no la incluye |
| Negra | Palillo de madera usado | No hay residuo de madera en el catálogo |
| Negra | Papel aluminio con comida | Sin residuo equivalente |
| Negra | Vinipel sucio | Sin residuo equivalente |
| Negra | Plástico o cinta sucia | Material no identificable |
| Negra | Plato desechable sucio | Material ambiguo (icopor, papel o plástico) |
| Negra | Bolsa negra de basura (3 fotos) | Muestran una mezcla, no un material |

**Residuos sin recurso gráfico** (siguen en Oracle, en la búsqueda y en el catálogo de datos; no tienen tarjeta en el
catálogo visual): semillas y tusas, botella PET de otras bebidas, toalla higiénica, tampón y pañal. No se generaron
imágenes artificiales para ellos.

### Regenerar (solo si cambian las fotos)

```powershell
python -m pip install pillow numpy
python scripts\documentacion\Integrar-FotosResiduos.py "<carpeta que contiene CANECA BLANCA, CANECA VERDE y CANECA NEGRA>"
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Release
```

`CatalogoFotosTests` comprueba, entre otras cosas: que cada foto pertenezca a un residuo de **su misma caneca**, que las
27 celdas de las láminas existan con sus coordenadas y sin superponerse, que ninguna «Guía visual» llegue a la galería,
que las imágenes sean cuadradas de 360 px, que los contadores coincidan con las tarjetas y que no haya rutas del equipo.

## 2. En la aplicación

- **Residuos de una caneca** (tocar una caneca en el inicio y «Ver los residuos que recibe»): **una tarjeta por residuo
  único con imagen**, ordenadas por nombre, con foto, nombre completo (dos o tres líneas si hace falta), caneca y
  ecopuntos por unidad. Encabezado «Caneca verde — Orgánicos aprovechables» y el contador «21 residuos disponibles»
  (blanca 18, negra 10). Buscador dentro de la caneca cuando hay más de seis.
- **Sin espacios en blanco:** la cuadrícula calcula sus columnas según el ancho y el número de tarjetas
  (`DistribucionRejilla`); nunca reserva casillas vacías. La ventana toma el alto exacto de su contenido hasta el 88 % de
  la pantalla y, si hay más, se desplaza verticalmente. Las tarjetas de una fila tienen el mismo alto. Una caneca sin
  residuos con imagen muestra un aviso corto en lugar de una cuadrícula vacía.
- Las tres canecas usan la **misma ventana y la misma lógica**: solo cambia el contenido.
- **Catálogo** (botón de la barra superior): los 49 residuos con imagen, buscador sin tildes con sinónimos, filtros
  *Todas*, *Blanca*, *Verde* y *Negra* y, en el encabezado, el resumen por caneca y los que esperan imagen.
- **Detalle de un residuo:** foto grande y **galería interna** con todas sus fotos (por ejemplo, el panel recortado y las
  fotos individuales del mismo material), caneca, condiciones y ecopuntos.
- **Ver cómo depositarlo:** vuelve al inicio con el destino del residuo y sigue el flujo de siempre (Oracle comprueba la
  caneca y se abre el asistente de depósito). Consultar o elegir un residuo **no otorga puntos**.

Pruebas visuales reproducibles con las ventanas reales (Oracle desconectado a propósito):

```powershell
dotnet build tests\visual\CapturasExpo -c Release -o $env:TEMP\capturas_bin
& "$env:TEMP\capturas_bin\CapturasExpo.exe" "$env:TEMP\capturas_catalogo" catalogo
```

Genera las capturas de las tres canecas y de listas de 0, 4, 7, 10, 13 y 20 residuos, nombres largos, búsqueda y ventana
maximizada, e `informe_catalogo.txt` con tarjetas = contador, tarjetas superpuestas y el espacio vacío bajo la última fila.
Resultado del 21/11/2026: 13 de 13 casos correctos.

## 3. Matriz de ecopuntos 2026.11.21-1 (aprobada el 21/11/2026)

| Caneca | Material Oracle | Puntos por unidad |
|---|---|---:|
| Blanca | `LATA_ALUMINIO` | 10 |
| Blanca | `PET` | 8 |
| Blanca | `CARTON` | 7 |
| Blanca | `VIDRIO` | 7 |
| Blanca | `PAPEL` | 5 |
| Blanca | `METAL` | 3 (de la versión 2026.11.10-1, por reevaluar) |
| Blanca | `MULTICAPA` | 2 (de la versión 2026.11.10-1, por reevaluar) |
| Verde | `ORGANICO` | 2 |
| Negra | todos | 0 |

- **No se usa peso.**
- Los puntos se acreditan solo con una cuenta, después de un depósito confirmado y verificado. Cada crédito queda en
  `movimiento_ecopuntos` con su regla, así que cambiar la tarifa no altera el historial.
- **Verde:** Oracle tiene un solo código `ORGANICO`. Para distinguir frutas (4), café (3) y hojas (2) habría que dividir
  ese material en una migración. Mientras tanto se usa el valor menor (2), para no dar más de lo propuesto.
- **En la aplicación:** `MatrizEcopuntosPropuesta` (Lógica) muestra el valor propuesto en el catálogo.
- **En Oracle:** quien acredita es la regla vigente. La matriz ya está aprobada; para aplicarla, un **administrador real** ejecuta,
  en una copia, `database/07_operacion/T002__tarifas_ecopuntos_2026_11_21.sql`, reemplazando `<<ID_ADMINISTRADOR>>` por
  su id. El script:
  - cierra la regla vigente de cada material si tiene otro valor y crea la nueva;
  - es idempotente;
  - se niega si el id no es un administrador activo.
- **Esta entrega no lo ejecutó.** Hasta que se ejecute, Oracle sigue acreditando con las reglas anteriores.

### Ejecutar T002 en SQL Developer (administrador)

1. Abre la conexión del esquema de la aplicación con la cuenta autorizada (wallet `ecoaccess_tp`, guía de instalación).
2. Busca tu id: `SELECT u.id_usuario, u.correo_electronico FROM usuario u JOIN usuario_rol r ON r.id_usuario = u.id_usuario JOIN rol ro ON ro.id_rol = r.id_rol WHERE ro.codigo = 3 AND u.activo = 1;`
3. Abre una **copia** de `T002__tarifas_ecopuntos_2026_11_21.sql`, cambia `<<ID_ADMINISTRADOR>>` por tu id y ejecútala como script (F5). No guardes la copia en el repositorio.
4. Comprueba: `SELECT m.codigo, r.puntos, r.desde_utc FROM regla_ecopuntos r JOIN material m ON m.id_material = r.id_material WHERE r.hasta_utc IS NULL ORDER BY r.puntos DESC;` debe mostrar lata 10, PET 8, cartón 7, vidrio 7, papel 5, metal 3, multicapa 2 y orgánico 2.
5. Reversión: cerrar esas reglas (`hasta_utc`) y volver a ejecutar T001.

## 4. Actualizar la aplicación instalada y el acceso directo

```powershell
git pull
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1
```

El instalador hace esto:
1. Compila en Release.
2. Copia la aplicación a `%LOCALAPPDATA%\Programs\EcoAccess P3`.
3. Deja un único acceso directo «EcoAccess P3» en el escritorio.

Antes de reinstalar, cierra EcoAccess. Si quieres conservar la versión anterior, copia esa carpeta, por ejemplo a
`%LOCALAPPDATA%\EcoAccessP3\respaldo_version_<fecha>`.

Si el bot de Telegram está abierto desde `src\EcoAccess.P3.Bot\bin\Release`, bloquea esa carpeta: ciérralo antes de
compilar la solución completa en Release, y vuelve a abrirlo para que use el catálogo nuevo.
