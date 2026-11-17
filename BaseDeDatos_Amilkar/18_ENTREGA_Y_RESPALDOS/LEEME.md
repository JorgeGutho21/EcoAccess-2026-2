# Entrega y respaldos

| Archivo o carpeta | Qué es |
|---|---|
| `MANIFIESTO_FINAL.md` | Inventario de todos los archivos de la entrega, con tamaño y SHA-256, y el estado del proyecto. Lo genera `herramientas/empaquetar.ps1`. |
| `HISTORIAL_DE_CAMBIOS_Y_COMMITS.md` | Cada commit de la publicación en GitHub: identificador, fecha real, archivos, qué cambió, por qué y cómo se comprobó. |
| `PLAN_DE_REVERSION.md` | Cómo deshacer la instalación del esquema académico y cómo restaurar el respaldo. |
| `registro_respaldo_data_pump/` | El registro de la exportación Data Pump. El archivo `.DMP` **no se publica**: se guarda solo en el PC. |
| `CORRESPONDENCIA_RUTAS_ANTIGUAS_NUEVAS.csv` | Para cada archivo: su ruta en la estructura anterior (`EcoAccessBD_Exposoftware`), su ruta nueva, su tamaño y su SHA-256. |
| `ARCHIVOS_EXCLUIDOS_DE_LA_ENTREGA.csv` | Lo que no pasó a la estructura nueva y por qué: versiones anteriores, duplicados, intermedios regenerables y archivos sensibles. Todo sigue en el respaldo local. |
| `herramientas/` | Scripts de apoyo: conexión a Oracle (`comun.ps1`, `ejecutar_sql.ps1`), generación de los documentos Word y PDF (`generar_todos.ps1`, `generar_documentos.ps1`), empaquetado (`empaquetar.ps1`), revisión de secretos antes de publicar (`revisar_secretos.ps1`), la prueba de concurrencia de tarifas con dos sesiones (`prueba_concurrencia_tarifas.ps1`) y la reorganización (`reorganizar_entrega.ps1`). |
| `fuentes_documentos/` | Las fuentes HTML de cada documento Word y PDF. |

## Respaldos (solo en el PC, nunca en GitHub)

- **Respaldo completo de la estructura anterior:** `RESPALDOS_LOCALES/RESPALDO_EcoAccessBD_Exposoftware_COMPLETO_20261114_0657.zip`, en la carpeta de la asignatura, junto a `BaseDeDatos_Amilkar`.
  - Contiene 987 archivos, verificados uno por uno con SHA-256 (0 diferencias).
  - SHA-256 del ZIP: `0A244A962E8FDBD90018A344906A5BF654D3E974D98909ED161E59CC0D21E3F9`.
- **Respaldo Data Pump:** `ECOACCESS_RESPALDO_20261111_1137.DMP`, de los esquemas `ECOACCESS_BD` y `ECOACCESS_PRACTICAS`, en `_LOCAL_NO_PUBLICAR/respaldo_data_pump/`. La exportación está verificada; la restauración no se ha probado.
- **Credenciales:** `_LOCAL_NO_PUBLICAR/` guarda las contraseñas locales. El `.gitignore` lo excluye y `empaquetar.ps1` comprueba que ninguna contraseña quede dentro del ZIP.

## Empaquetar la entrega

```
pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\empaquetar.ps1
```

Crea `BaseDeDatos_Amilkar_ENTREGA_<fecha>.zip` junto a la carpeta del proyecto, sin `_LOCAL_NO_PUBLICAR` ni respaldos. Después lo reabre y busca las contraseñas reales dentro de cada archivo de texto: si encuentra alguna, borra el ZIP y falla. El ZIP no se sube a GitHub.
