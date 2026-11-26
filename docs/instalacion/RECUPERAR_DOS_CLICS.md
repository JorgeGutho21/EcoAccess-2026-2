# Recuperar la versión final de DOS CLICS (PC anfitrión)

Rama: `integracion/tres-canecas-fisicas`. Solo cambian archivos de la web (`web/ciudadano/dos-clics/`, `web/ciudadano/assets/estacion/`) y un script de vista local; no hay cambios de base de datos, del panel, del ESP32 ni del bot, así que **no hay que recompilar ni reiniciar servicios**.

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

GitHub → **Actions** → **web-ciudadana** → **Run workflow** → rama `integracion/tres-canecas-fisicas` → **Run workflow**. Tarda 1–2 minutos.

## 3. Comprobar

1. Abre https://jorgegutho21.github.io/EcoAccess-2026-2/version.json y verifica que `commit` sea el último de la rama (`git log --oneline -1`).
2. Abre https://jorgegutho21.github.io/EcoAccess-2026-2/dos-clics/ y recarga con **Ctrl+F5** una sola vez.
3. Debe verse: hojas en las esquinas, Eco, título verde con barra, buscador con lupa, flecha verde y micrófono, tres canecas con su pastilla y el pie blanco. Un toque en una caneca la abre.
4. El zoom del navegador debe estar en 100 % (**Ctrl+0**).

Desde esta versión la página pide sus estilos con `?v=…`, así que los navegadores ya no mezclan el HTML nuevo con estilos viejos en caché.

## Vista local sin publicar (opcional)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Abrir-DosClics-Local.ps1
```

Sirve la copia local en http://localhost:5173/dos-clics/ (sin Python). Para detenerla: añade `-Detener`.
