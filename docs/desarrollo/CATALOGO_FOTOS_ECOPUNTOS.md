# Catálogo con fotografías y matriz de ecopuntos 2026.11.21-1

Guía para el equipo: qué se integró de las carpetas **CANECA BLANCA**, **CANECA VERDE** y **CANECA NEGRA**, cómo se ve en
la aplicación, qué quedó pendiente y cómo se aprueba la nueva matriz de ecopuntos. Plataforma sin cambios: .NET Framework
4.8, Windows Forms y Oracle Cloud.

## 1. Fotografías integradas

| Carpeta | Archivos | Integradas | Guías visuales | Pendientes | Duplicadas | Dañadas |
|---|---:|---:|---:|---:|---:|---:|
| CANECA BLANCA | 28 | 22 | 1 | 4 | 1 | 0 |
| CANECA VERDE | 33 | 31 | 2 | 0 | 0 | 0 |
| CANECA NEGRA | 40 | 32 | 0 | 8 | 0 | 0 |
| **Total** | **101** | **85** | **3** | **12** | **1** | **0** |

- Revisé las fotos **una por una**: la clasificación sale de lo que muestra cada imagen, no del nombre del archivo (por
  ejemplo, «papel arrugado» de la carpeta negra se ve manchado y quedó como papel contaminado).
- Los originales no se tocan y **no están en el repositorio**. Las versiones de 360 px (JPEG) están en
  `assets/imagenes/residuos/<caneca>/fotos/` y las guías en `assets/imagenes/residuos/<caneca>/guias/` (3,5 MB en total).
  Se incrustan en el ejecutable, así que no dependen de rutas de ningún equipo.
- El inventario completo está en [`docs/evidencias/catalogo_fotos/inventario_fotos.csv`](../evidencias/catalogo_fotos/inventario_fotos.csv). Incluye
  carpeta, archivo, tamaño, huella SHA-1, estado, residuo asignado y nota.
- La relación foto → residuo vive en `scripts/documentacion/Integrar-FotosResiduos.py` (tabla `ASIGNACION`). El script genera
  `src/EcoAccess.P3.Logica/Servicios/CatalogoFotos.g.cs`; no lo edites a mano.

### Pendientes (no se muestran como material depositable)

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

Para integrarlas hay que decidir primero el residuo y su material de Oracle. Después se agrega la entrada en
`CatalogoPrototipo` y la foto en `ASIGNACION`, y se vuelve a ejecutar el script.

### Inconsistencias a revisar con el equipo

- Algunas fotos de «rollo usado» (negra) muestran también el **tubo de cartón**. El tubo limpio es aprovechable (blanca);
  el papel higiénico usado va a la negra.
- La foto de la lata de maíz está **llena**: un envase solo se recicla vacío y limpio.
- Seis residuos siguen sin foto y usan el icono: botella PET de otras bebidas, caja de leche, semillas y tusas, toalla
  higiénica, tampón y pañal.

### Regenerar (solo si cambian las fotos)

```powershell
python -m pip install pillow
python scripts\documentacion\Integrar-FotosResiduos.py "<carpeta que contiene CANECA BLANCA, CANECA VERDE y CANECA NEGRA>"
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug
```

`CatalogoFotosTests` comprueba que cada foto pertenezca a un residuo de **su misma caneca** y que cada archivo exista.

## 2. En la aplicación

- **Todas las fotos de cada caneca:** al tocar una caneca en el inicio y luego «Ver los N residuos que recibe», debajo de
  los residuos aparecen TODAS las fotos de su carpeta (blanca 27, verde 33, negra 40). Un clic abre cada foto en grande.
  Las guías salen en azul y las 12 sin residuo equivalente salen en ámbar como **«Por clasificar»**, con su razón; esas no
  se pueden depositar. En el catálogo, el enlace «Ver todas las fotos de esta caneca» abre la misma galería.

- **Catálogo** (botón en la barra superior del inicio): muestra los 38 residuos con su foto, un buscador sin tildes que
  reconoce sinónimos y los filtros *Todas*, *Blanca*, *Verde* y *Negra*.
- **Detalle de un residuo:** foto grande, miniaturas de todas sus fotos, caneca, descripción, condiciones y ecopuntos
  propuestos.
- **Guía visual:** el enlace aparece en el filtro de la caneca que tiene guías (blanca y verde).
- **Ver cómo depositarlo:** vuelve al inicio con el destino del residuo. Desde ahí sigue el flujo de siempre:
  1. Oracle comprueba que la caneca esté en servicio.
  2. Se abre el asistente de depósito, con la ventana real de 10 s.
- Los residuos sin ilustración propia toman su primera foto como imagen principal, también en «¿Dónde va tu residuo?» y en
  la lista de cada caneca. Los 15 del stand conservan su ilustración.
- Consultar o elegir un residuo **no otorga puntos**.

Capturas en `docs/evidencias/catalogo_fotos/` (Oracle desconectado a propósito, sin contraseñas).

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
