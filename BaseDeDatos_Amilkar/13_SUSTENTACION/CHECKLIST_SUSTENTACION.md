# Checklist para la sustentación

## Antes del día

| ✔ | Qué revisar | Cómo |
|---|---|---|
| ☐ | Todo funciona | `pwsh -File .\INSTALAR_Y_PROBAR.ps1` debe terminar en verde: «28 de 28 comprobaciones PASAN» |
| ☐ | Oracle está encendido | Consola: `lsnrctl status` muestra el servicio `freepdb1` |
| ☐ | La instalación está sana | En SQL Developer, conexión ECOACCESS_BD, F5 a `14_verificar_invalidos.sql`: debe dar 0 inválidos |
| ☐ | Las pruebas pasan | F5 a `12_pruebas.sql`: debe terminar en «TOTAL: 163 · PASAN: 163 · FALLAN: 0» |
| ☐ | El tablero abre | Abrir `09_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix`, recorrer las 5 páginas y cambiar un filtro. Para Oracle directo, abrir el `.pbip`, escribir la contraseña del lector (Transformar datos → Editar credenciales) y pulsar Actualizar (Guía de Power BI) |
| ☐ | El diagrama .dmd abre | SQL Developer → Archivo → Data Modeler → Abrir → `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EcoAccess_BD.dmd` (la carpeta `EcoAccess_BD/` va al lado). Mostrar Relational_1 y las subvistas M1–M5 |
| ☐ | La galería de modelos abre | Doble clic en `19_MODELOS_VISUALES/00_ABRIR_DIAGRAMAS.html`; tener abierto `ATLAS_VISUAL_ECOACCESS.pdf` como plan B |
| ☐ | El informe que se entrega está listo | `01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.pdf` (40 páginas, APA 7) |
| ☐ | Ninguna contraseña en lo que se entrega | La carpeta `_LOCAL_NO_PUBLICAR/` NO se entrega ni se sube a Git |

## Guion sugerido (15 minutos)

| Min. | Qué mostrar | Archivo o consulta |
|---|---|---|
| 0–2 | El problema: una foto no es un depósito; un peso estimado no es un peso medido | Informe, cap. 2 |
| 2–4 | Los 5 puntos del avance: objetivos, MER y modelo relacional | `MER_conceptual.png`, `MR_00_general_por_modulos.png` |
| 4–6 | Diccionario: una columna explicada (DEPOSITO.estado_verificacion) | SQL Developer → DEPOSITO → pestaña Columnas |
| 6–9 | Trigger 2 ★: un ciudadano intenta verificar su propio depósito → ORA-20101 | Ejercicios del tema 18 o P094 en `12_pruebas.sql` |
| 9–11 | Trigger 3 ★: verificar y ver la fila :OLD/:NEW en AUDITORIA_DEPOSITO | Q18 |
| 11–13 | Consulta estrella: quiénes depositaron más residuos VERIFICADOS en 7 días | Q01 (y Q02 con SYS_REFCURSOR) |
| 13–14 | Power BI: Inicio → Ranking y pesos (estimado vs medido) | `.pbip` |
| 14–15 | Práctica de clase: precio 1000 → 1200 y nota inválida ORA-20001 | `P02`, `P03` |

## Preguntas probables y respuesta corta

- **¿Por qué 19 tablas y no 27?** Porque solo quedan las del dominio que se defiende. Las del bot (tokens, conversaciones, updates) son infraestructura de P3, y RFID, organizaciones y supervisor ya no existen.
- **¿Por qué no hay MEER?** Porque no hay especialización con atributos propios: los roles son N:M (USUARIO_ROL) y una persona puede tener varios.
- **¿Por qué el trigger no da los EcoPuntos?** Para no duplicar la lógica: solo `PKG_ECOPUNTOS.acreditar` acredita, de forma idempotente y dentro de la transacción de quien llama.
- **¿Qué pasa si se cae la red y llega dos veces la misma orden?** La clave de idempotencia es UNIQUE, y `acreditar` devuelve el mismo movimiento sin duplicarlo (P136).
- **¿Cómo se evita que dos personas abran la misma caneca a la vez?** Con el índice único funcional `UQ_ORDEN_VIGENTE`: la segunda orden vigente recibe ORA-00001 (P084).
- **¿Por qué TIMESTAMP en UTC?** Para que la hora no dependa del servidor; se convierte a `America/Bogota` solo al consultar.
- **¿Los datos son reales?** No. Son sintéticos (semilla 20261110), y están marcados en las tablas y en el tablero.
