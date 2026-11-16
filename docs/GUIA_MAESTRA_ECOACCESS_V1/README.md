# Guía maestra EcoAccess P3 V1

Universidad Popular del Cesar · Programación de Computadores III.

Instalación, arquitectura y operación del código integrado en [PR #11](https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/11). Empieza por **Guía rápida para el equipo**. Para operar el ESP32 abre el capítulo 07 y sigue el encendido supervisado.

| Recurso | Archivo |
| --- | --- |
| Word con índice navegable | [EcoAccess_V1_GUIA_MAESTRA.docx](EcoAccess_V1_GUIA_MAESTRA.docx) |
| PDF con marcadores | [EcoAccess_V1_GUIA_MAESTRA.pdf](EcoAccess_V1_GUIA_MAESTRA.pdf) |
| Fuente única editable | [GUIA_MAESTRA.md](fuentes/GUIA_MAESTRA.md) |
| Pruebas y pendientes | [verificaciones.md](evidencias/verificaciones.md) |
| Código y versiones | [rutas_y_dependencias.md](evidencias/rutas_y_dependencias.md) |

## Regenerar

En Windows con Microsoft Word de escritorio y Python 3.10 o posterior, instala `python-docx` y `Pillow` en tu entorno de documentación. Cierra el Word o PDF de la guía si está abierto. Desde esta carpeta ejecuta:

```powershell
python -m pip install python-docx==1.2.0 Pillow==13.0.0
pwsh -NoProfile -File fuentes/Regenerar.ps1
```

Si usas un entorno virtual, pasa su ejecutable con `-Python`. El proceso lee el Markdown y recursos versionados, genera las figuras, produce el Word, actualiza su tabla de contenido y exporta el PDF con Word. Las versiones de Word y las fuentes instaladas pueden alterar la paginación. La portada utiliza el logo del repositorio. Las capturas históricas proceden de las rutas en el inventario de evidencias.

Para validar una edición, abre los dos archivos y verifica la portada, el índice, los números de página, el texto visible y los enlaces. Los marcadores del PDF siguen los encabezados. El apéndice Oracle conserva inventario histórico y DDL V026 con alcance documentado.
