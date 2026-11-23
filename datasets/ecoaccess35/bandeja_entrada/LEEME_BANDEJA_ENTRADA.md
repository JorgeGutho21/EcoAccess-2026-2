# Bandeja de entrada de nuevas fotografías (`datasets/ecoaccess35/bandeja_entrada/`)

Copia aquí tus nuevas fotografías reales tomadas con la cámara del stand (JPG, PNG o WebP).
No necesitas recompilar Visual Studio.

1. Detectar archivos nuevos, verificar integridad, rechazar duplicados y generar sugerencias:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -SugerirConOllama
   ```
2. Revisa y aprueba las etiquetas en `datasets/ecoaccess35/anotaciones/pendientes_revision.json` cambiando `"revisado_humano": false` a `true`.
3. Incorpora solo las imágenes aprobadas y ejecuta la evaluación automática:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\pruebas\vision\Incorporar-NuevasFotos35.ps1 -AprobarRevisadas
   ```