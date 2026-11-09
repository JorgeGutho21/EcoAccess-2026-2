# Historial de cambios implementados — recuperación con Oracle Cloud

Rama `recovery/ecoaccess-oracle-cloud` de `JorgeGutho21/EcoAccess-P3`. Hashes reales: `git log --oneline recovery/ecoaccess-oracle-cloud`. Autor de los commits: Jorge Gutierrez Thomas (correo noreply de la cuenta GitHub `JorgeGutho21`); el trabajo se hizo con asistencia de IA (Claude Code), registrada aquí y no en los mensajes de commit.

| Fecha | Commit | Cambio | Verificación |
|---|---|---|---|
| 2026-11-09 | `afe9c41` | Oracle Cloud con wallet mTLS (`WalletOracleNube`), configuración inicial sin Docker, `EcoAccess.P3.Instalador`, scripts de esquema/roles/cuentas para Autonomous Database | V009–V020, C002, C003 aplicadas en Oracle Cloud; 225 pruebas de integración superadas |
| 2026-11-09 | `eeaf6b5` | Tres canecas: V021 `CONFIGURACION_CANECA`, `ConfiguracionCaneca`, `ServicioTresCanecas`, repositorio Oracle; evidencia LFM3-VL 60 fotos | 19 pruebas de lógica/simulación + 1 de integración en Oracle Cloud |
| 2026-11-09 | (este commit) | Colores centralizados en `TemaEcoAccess`; documentación de recuperación, Oracle Cloud, visión, Telegram, hardware, equipo; generador DOCX y documentos de entrega | compilación y 1 638 pruebas; DOCX abiertos con Word |

## Trabajo previo en la línea EcoAccess_P3 (otro repositorio)

En `JorgeGutho21/EcoAccess_P3`, rama del mismo nombre (commits `06ddfac` … `3085264`): primera versión del wallet, del instalador y de la configuración inicial; instalación de V001–V008 en Oracle Cloud; prueba de persistencia (usuario 244); colores y accesibilidad de botones Krypton (`BotonEco`); verificación del bot con la cuenta real del propietario. Se conserva como respaldo.

## Configuración del equipo (fuera del repositorio)

* Instalados con WinGet: Git, GitHub CLI, SQL Developer, PowerShell 7, Ollama.
* Variables de usuario: `TNS_ADMIN` (wallet) y `OLLAMA_NO_CLOUD=1`.
* User Secrets `ecoaccess-p3-framework`: conexión Oracle, Telegram, visión local.
* Corpus TrashNet en `C:\ProgramData\EcoAccessP3Framework\Modelos\trashnet-6fa2b87`.
