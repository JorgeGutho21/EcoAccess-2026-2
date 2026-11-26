# Recuperar la versión final de DOS CLICS (PC anfitrión)

Rama: `integracion/tres-canecas-fisicas`. Cambian la web (`web/ciudadano/`: DOS CLICS, sus imágenes y el *service worker* `sw.js`) y la pantalla de inicio de sesión de la app de escritorio (las canecas abren el sitio web). No hay cambios de base de datos, del panel, del ESP32 ni del bot.

## 1. Traer los archivos (PowerShell, en la carpeta del repositorio)

```powershell
git status --short
```

Si aparece algún archivo modificado (líneas con `M`), avisa antes de seguir: no uses `reset`, `stash` ni `--force`.

```powershell
git fetch origin
git checkout integracion/tres-canecas-fisicas
git pull --ff-only origin integracion/tres-canecas-fisicas
```

## 2. Publicar la página pública

GitHub Pages se publica **desde `main`** (cada push a `main` vuelve a publicar). Para que la página pública quede con esta versión:

1. GitHub → **Pull requests** → **New pull request** → base `main` ← compare `integracion/tres-canecas-fisicas` → **Create** → **Merge**.
2. Espera 1–2 minutos a que termine **Actions → web-ciudadana**.

(Ejecutar el workflow a mano desde la rama también publica, pero el siguiente push a `main` lo vuelve a reemplazar.)

## 2.1 App de escritorio (para ver las canecas clicables del inicio de sesión)

Reinstala la app con el mismo perfil que usas siempre en ese equipo, por ejemplo:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal
```

En la pantalla de inicio de sesión, al pasar el mouse sobre las tres canecas aparece la mano y el aviso «Abrir el sitio web de EcoAccess»; un clic abre https://jorgegutho21.github.io/EcoAccess-2026-2/.

## 3. Comprobar

1. Abre https://jorgegutho21.github.io/EcoAccess-2026-2/version.json y verifica que `commit` sea el último de la rama (`git log --oneline -1`).
2. Abre https://jorgegutho21.github.io/EcoAccess-2026-2/dos-clics/ y recarga con **Ctrl+F5** una sola vez.
3. Debe verse: hojas en las esquinas, Eco, título verde con barra, buscador con lupa, flecha verde y micrófono, tres canecas con su pastilla y el pie blanco. Un toque en una caneca la abre.
4. El zoom del navegador debe estar en 100 % (**Ctrl+0**).

**Por qué antes se veía mal:** el *service worker* del sitio guardaba los estilos y el código «caché primero» y su versión no cambiaba, así que los navegadores que ya habían visitado el sitio seguían usando la hoja de estilos vieja con el HTML nuevo. Ahora HTML, CSS y JS van «red primero», la caché pasó a `ecoaccess-ciudadano-v8` (borra la vieja) y DOS CLICS pide sus estilos con `?v=…`. Tras publicar basta **una** recarga.

## Vista local sin publicar (opcional)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-DosClics-Local.ps1
```

Sirve la copia local en http://localhost:5173/dos-clics/ (sin Python). Para detenerla: añade `-Detener`.
