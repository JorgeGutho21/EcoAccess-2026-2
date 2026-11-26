# Entrega para Claude en el PC de Jorge

Instrucciones para el agente (Claude u otro) que continúe en el **computador principal de Jorge**, donde están la instalación
operativa, el ESP32, el bot de Telegram, el Wallet y las credenciales de demostración. Léelas completas antes de actuar.

## Estado de partida (2026-11-26 07:55 UTC)

- Lote `SIM-FERIA-2026` **COMPLETO en Oracle Cloud**, generado desde ese PC (ver `ESTADO_ACTUAL.md`). 46 comprobaciones: 0 fallas.
- Rama publicada por Heyder: **`feature/ecoaccess-datos-feria-2026`** (el SHA está en `CHANGELOG_FERIA.md`). Solo añade
  documentación, scripts de solo lectura, T004 (sin ejecutar) y `.gitignore`. **No cambia código C#, vistas ni interfaces.**
- Pendientes que solo se pueden hacer en ese PC: P3 (publicar el generador) y P4 (credenciales). Ver `PENDIENTES.md`.

## Reglas

- No ejecutar otra vez el generador sobre `SIM-FERIA-2026`; no regenerar contraseñas de cuentas existentes.
- No escribir en Oracle Cloud sin autorización expresa del responsable (incluye T004 y cualquier migración V036).
- No mover el hardware: nada de comandos al ESP32 ni cambios de firmware, calibración, puertos o reconocimiento visual.
- No reinstalar servicios, no cambiar puertos, no duplicar tareas programadas ni tocar el arranque de Telegram/EcoAccess.
- No publicar Wallet, contraseñas, tokens, cadenas de conexión ni el TXT de credenciales.

## Pasos

1. **Repositorio.** Localizar el clon (`git rev-parse --show-toplevel` desde la carpeta de Visual Studio). `git status` y
   `git stash list`: anotar cambios locales y **no descartarlos**.
2. **Publicar el generador (P3).** Si `tests/herramientas/EcoAccess.P3.DatosFeria` existe sin versionar: revisar que no contenga
   contraseñas ni cadena de conexión (`git diff`, búsqueda de `Password=`, `User Id=`), hacer commit en una rama nueva a partir de
   `origin/feature/ecoaccess-datos-feria-2026` y push. Abrir PR hacia `main` sin fusionarlo.
3. **Traer la rama de Heyder.** `git fetch origin` · `git switch feature/ecoaccess-datos-feria-2026` (o fusionarla en la rama de
   trabajo de Jorge con `git merge --no-ff`; si hay conflictos, resolverlos conservando ambos cambios).
4. **Compilar y probar.** `powershell -ExecutionPolicy Bypass -File scripts\feria\Compilar-Y-Probar.ps1`. Compila en `bin\` del
   repositorio; **no** sobrescribe `%LOCALAPPDATA%\Programs\EcoAccess P3`. Esperado: 0 errores; 2649 + 65 pruebas superadas.
5. **Wallet y conexión.** `scripts\instalacion\Verificar-Equipo-EcoAccess.ps1` (o «Sistema, Oracle y Telegram» en la app) sin mostrar su contenido:
   solo comprobar que existe la carpeta del Wallet y que la conexión abre.
6. **Verificar el lote.** `scripts\feria\Verificar-LoteFeria.ps1`. Debe dar 0 fallas; si la cifra de depósitos ya no es 650, alguien
   lo cambió: detenerse y avisar.
7. **Credenciales (P4).** Comprobar que `ECOACCESS_CREDENCIALES_FERIA_2026.txt` está en el escritorio de Jorge con las 64 cuentas.
   Verificar cada una con `ServicioAutenticacion.IniciarSesionPublicaAsync` desde una herramienta local (no imprimir contraseñas
   en la consola ni en registros); anotar en `RESULTADOS_PRUEBAS.md` cuántas inician sesión. Si el archivo no existe, **no**
   regenerar contraseñas: registrar la necesidad de una recuperación autorizada.
8. **Guía.** `scripts\feria\Generar-GuiaDemostracion.ps1` escribe `ECOACCESS_GUIA_DEMOSTRACION_FERIA.txt` en el escritorio.
9. **Decisión P1/P2.** Presentar a Jorge las opciones (a) V036 para separar el lote, (b) advertirlo al presentar o (c) revertir.
   No aplicar ninguna sin su autorización.
10. **Instalación principal.** Solo si Jorge lo pide: actualizar con el instalador habitual del proyecto. Esta rama no cambia la
    aplicación, así que normalmente **no hace falta**.
11. **Comprobaciones sin movimiento físico.** Acceso directo del escritorio, Visual Studio abre la solución, bot de Telegram
    responde, EcoAccess Web y Dos Clics cargan, servicios en segundo plano en ejecución, ESP32 visible en su puerto (solo
    estado: no enviar aperturas).
12. **Gráficos.** Entrar con las cuentas de la guía: Mis ecopuntos (ciudadano), Depósitos/Ecopuntos (operario), Dashboard
    institucional en ambos alcances (supervisor). Comparar con el informe de `Verificar-LoteFeria.ps1`.
13. **Informe final.** Actualizar `ESTADO_ACTUAL.md`, `RESULTADOS_PRUEBAS.md`, `PENDIENTES.md` y `CHANGELOG_FERIA.md` con lo que se
    comprobó de verdad (incluidas pruebas fallidas u omitidas), commit y push de la rama.
