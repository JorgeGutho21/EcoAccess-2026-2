# Informe de pruebas del cierre (fase 12, 11/11/2026)

Rama `desarrollo/telegram-doble-ia` (PR #11). Compilación `dotnet build EcoAccess.P3.sln --no-incremental -c Debug`:
**0 errores**, 11 advertencias, todas de analizadores MSTest en pruebas anteriores.

| Suite | Dónde | Aprobadas | Fallidas | Omitidas | Notas |
|---|---|---|---|---|---|
| Unitarias (`tests/unitarias`) | local | **1760** | 0 | 1 | omitida `EnlaceSimbolicoSeRechazaAntesDeClasificar`: requiere privilegio para crear enlaces simbólicos |
| Integración (`tests/integracion`) | Oracle FREE local, esquema aislado `ECOACCESS_P3_PRUEBAS` | **211** | 0 | 7 | 3 min 53 s; las omitidas se detallan abajo |
| Integración de solo lectura | Oracle Cloud | 2 | 0 | 0 | `EstacionTresCanecasOracleTests` |
| E2E del lote de simulación | Oracle Cloud | — | — | — | el generador recorre los servicios reales en 100 depósitos: 0 rechazos; la repetición no crea nada |

**Las omitidas no se cuentan como aprobadas.** Las 7 de integración son inconclusas por diseño:
- 5 necesitan el modelo de visión local y un corpus, más una autorización explícita (`ECOACCESS_PROBAR_VISION_*`);
- 1 es exclusiva del PDB Docker `PRUEBAP3`, que ya no existe;
- 1 (`Usuario_UltimoAdministradorDesactivado…`) solo aplica a una base sin administradores, y el esquema aislado tiene uno.

## Pruebas nuevas de esta misión

| Fase | Prueba | Casos |
|---|---|---|
| 1 Roles | `MatrizPermisosTests` (y prueba de mutación manual) | 8 |
| 2 Tres canecas | `ResolutorDestinoTests`, flujo de foto con estación | 18 |
| 2 Tres canecas | `EstacionTresCanecasOracleTests` | 2 |
| 3 Flujo Eco | `EstacionSimuladaTests` | 6 |
| 5–6 Pesos y ecopuntos | `PesosYEcopuntosTests` | 25 |
| 8 Simulación | `LoteSimulacionOracleTests` | 1 |
| 12 Seguridad | `SinSecretosEnRepositorioTests` | 7 |

## Concurrencia (1, 10, 20 y 30 personas)

`CargaUsuariosOracleTests`, esquema aislado. Métricas en `docs/pruebas/resultados_cierre/*.json`.

| Escenario | Personas | Errores | Tiempo total |
|---|---|---|---|
| Altas nuevas simultáneas | 1 | 0 | 1,1 s |
| Altas nuevas simultáneas | 10 | 0 | 3,1 s |
| Altas nuevas simultáneas | 20 | 0 | 7,1 s |
| Altas nuevas simultáneas | 30 | 0 | 11,8 s |
| Cuentas existentes simultáneas | 10 / 20 / 30 | 0 | 2,4 / 3,7 / 7,2 s |
| Altas consecutivas | 30 | 0 | 32,3 s (≈1,06 s por alta, por PBKDF2) |

Cada caso verifica en Oracle que hay 1 usuario, 1 rol, 1 credencial y 1 vínculo por correo, sin duplicados.

## Seguridad

- **Permisos**: los tres roles contra los servicios reales; las cuentas desactivadas o inexistentes no pasan ningún control.
- **Secretos**: `SinSecretosEnRepositorioTests` recorre todos los archivos de texto del repositorio y no encuentra:
  - claves de Groq;
  - tokens de bot;
  - contraseñas en cadenas de conexión.

  Los propios patrones están probados con ejemplos positivos y negativos.
- **Credenciales**: PBKDF2 nativo (`HasherPbkdf2Tests`, `Pbkdf2NativoTests`). Ninguna pantalla ni consulta del panel lee hashes, tokens ni contraseñas.
- **Dispositivo**: protocolo HMAC (`ProtocoloDispositivoHmacTests`). Además, una orden para otra caneca no se entrega (`EstacionSimuladaTests`).
- **Inyección SQL**: los repositorios usan parámetros enlazados (`ComandosOracle.Crear` con `BindByName`). No se hizo un análisis automático aparte.
