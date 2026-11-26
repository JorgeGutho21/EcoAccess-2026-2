# Inspección de Oracle y servicios para el lote de feria

Fuente: código del repositorio (`8f00efb`) y Oracle Cloud `ECOACCESS_P3_APP` (Oracle 19c, servicio `…_ecoaccess_tp`), consultado
en solo lectura el 2026-11-26. Nada de esto se inventó: cada nombre sale del DDL exportado en `database/schema/` o de una consulta.

## Roles (tabla `ROL`, enum `CodigoRol`)

| Código | Rol | Cuentas en la base | Del lote |
|---|---|---|---|
| 1 | Ciudadano | 89 | 50 |
| 2 | Operario | 17 | 8 |
| 3 | Administrador (la interfaz lo muestra como «ÁREA SUPERVISOR») | 14 | 6 (4 `sup-`, 2 `adm-`) |

No existe un cuarto rol: «supervisor» y «administrador» son el mismo rol 3.

## Estación y canecas (solo una estación)

| Contenedor | Caneca | Categoría (`CONFIGURACION_CANECA`) | Ubicación | Materiales admitidos |
|---|---|---|---|---|
| 1093 | EcoAccess — Caneca blanca | 1 | izquierda | 11 |
| 1400 | EcoAccess — Caneca verde | 2 | centro | 2 |
| 1401 | EcoAccess — Caneca negra | 3 | derecha | 14 |

Estación 1015 «Estación Oficial EcoAccess». El lote no creó estaciones ni canecas (`REGISTRO_SIMULACION` no tiene filas
`PUNTO_INSTALACION`/`CONTENEDOR` para él).

## Tarifas vigentes (`REGLA_ECOPUNTOS`, todas por unidad)

| Material | Categoría | Puntos | Desde |
|---|---|---|---|
| CARTON | 1 | 3 | 2026-11-11 |
| LATA_ALUMINIO | 1 | 5 | 2026-11-11 |
| METAL | 1 | 3 | 2026-11-11 |
| MULTICAPA | 1 | 2 | 2026-11-11 |
| PAPEL | 1 | 2 | 2026-11-11 |
| PET | 1 | 4 | 2026-11-11 |
| VIDRIO | 1 | 3 | 2026-11-11 |
| CORTOPUNZANTE | 4 | 4 | 2026-11-23 |

Siete materiales de la caneca blanca y uno de categoría 4. Los orgánicos y no aprovechables no tienen tarifa: sus depósitos
verificados dan 0 puntos (por eso hay ciudadanos con depósitos y saldo 0).

## Canales (`CanalInteraccion`)

1 Telegram · 2 Manual · 3 Simulador · 4 Telegram simulado · 5 Asistido. Las vistas de informes (`EA_R_*`) y
`RepositorioInformeInstitucionalOracle` consideran **ensayo** solo los canales 3 y 4; `VISTA_DEPOSITOS_ESTADO` además cruza
`REGISTRO_SIMULACION`.

## Paquetes y firmas usadas por el flujo oficial

| Objeto | Firma real | Notas |
|---|---|---|
| `PKG_ECOPUNTOS.acreditar` | `(p_id_deposito, p_id_actor, p_id_movimiento OUT)` | Roles 2 o 3; exige depósito verificado, titular con cuenta y actor ≠ titular ≠ verificador; idempotente; sin tarifa vigente no acredita. **Fecha = hora del servidor** (`SYS_EXTRACT_UTC(SYSTIMESTAMP)`). |
| `PKG_ECOPUNTOS.revertir` | `(p_id_deposito, p_id_actor, p_motivo)` | Solo rol 3; exige motivo. |
| `PKG_ECOPUNTOS.saldo` | `(p_id_usuario) RETURN NUMBER` | Suma de `MOVIMIENTO_ECOPUNTOS.puntos`. |
| `PKG_DEPOSITO.verificar` | `(p_id_deposito, p_id_verificador, p_id_material, p_cantidad, p_peso_medido_kg, p_motivo)` | |
| `PKG_DEPOSITO.rechazar` | `(p_id_deposito, p_id_verificador, p_motivo)` | Roles 2 o 3; deja `AUDITORIA_DEPOSITO` `RECHAZO` (estado 2). |
| `PKG_DEPOSITO.estado` | `(p_id_deposito) RETURN NUMBER` | 0 pendiente · 1 verificado · 2 rechazado. |
| `ServicioEcopuntos.AcreditarAsync` | `(actor, deposito, regla, clave, ct)` | Ruta C#: usa el reloj inyectado (`TimeProvider`), así una acreditación puede llevar fecha histórica. |
| `ServicioRegistroCiudadanos.RegistrarAsync` | `(nombre, correo, contrasena, confirmacion, aceptaTratamiento, ct)` | Política de ciudadanos: 6 a 20 caracteres, sin contraseñas comunes. Guarda hash PBKDF2 con sal (`IHasherContrasenas`). |
| `ServicioUsuarios.RegistrarAsync` | `(idActor, nombre, correo, rolInicial, ct)` | Exige actor administrador. |
| `ServicioAutenticacion.EstablecerContrasenaInicialAsync` | `(idActor, idUsuario, contrasena, ct)` | Personal (roles 2/3): 12 a 128 caracteres; solo si la cuenta aún no tiene contraseña. |
| `ServicioAutenticacion.IniciarSesionPublicaAsync` | `(correo, contrasena, ct)` | Verificación oficial de una cuenta de demostración. |
| `ServicioInteracciones.IniciarConCantidadAsync` / `AutorizarAperturaAsync` / `RegistrarEventoAsync` / `VerificarDepositoAsync` / `RegistrarAsistidoAsync` | ver `src/EcoAccess.P3.Logica/Servicios/ServicioInteracciones.cs` | **Un depósito solo nace de eventos de caneca** (acuse → apertura → detección o cierre sin sensor). `CanecaSimulada` produce esos eventos sin mover hardware. |

## Trazabilidad de lotes (V025)

`LOTE_SIMULACION(id_lote, descripcion, semilla, fecha_creacion_utc, ciudadanos, registros, estado)` y
`REGISTRO_SIMULACION(id_lote, tabla, id_registro, clave)`; `tabla ∈ {USUARIO, PUNTO_INSTALACION, CONTENEDOR, INTERACCION, DEPOSITO}`.
Claves del lote de feria: `ciu-NN`, `op-NN`, `sup-NN`, `adm-NN`, `int-NNN`, `dep-NNN`.

## Dependencias (FK) de lo que el lote creó

Hijos de `DEPOSITO`: `AUDITORIA_DEPOSITO` (CASCADE), `MOVIMIENTO_ECOPUNTOS`, `OPERACION_ECOPUNTOS`. `DEPOSITO` → `EVENTO_DISPOSITIVO`
→ `ORDEN_APERTURA` → `INTERACCION`. `MOVIMIENTO_ECOPUNTOS` ↔ `OPERACION_ECOPUNTOS` se referencian en ciclo (reverso → operación →
crédito); ninguna restricción es diferible. Hijos de `USUARIO`: `CREDENCIAL_ACCESO`, `USUARIO_ROL`, `INTERACCION`, `DEPOSITO`
(verificador), `MOVIMIENTO_ECOPUNTOS`, `OPERACION_ECOPUNTOS`, `REGLA_ECOPUNTOS`, `VINCULO_TELEGRAM`, `OPINION`,
`COMENTARIO_CIUDADANO`, `TOKEN_VINCULACION`, `VINCULACION_PENDIENTE`; con SET NULL: `AUDITORIA_DEPOSITO`, `CONSULTA_VISION`,
`HISTORIAL_ESTADO_CONTENEDOR`, `OPERACION_ESTACION`.

Triggers: `TRG_DEPOSITO_AUD_VERIFICACION`, `TRG_DEPOSITO_BLOQUEO_CERRADO`, `TRG_DEPOSITO_COHERENCIA` (DEPOSITO),
`TR_ECOP_UNIDADES` (MOVIMIENTO_ECOPUNTOS), `TRG_CONTENEDOR_AUD_ESTADO`. Ninguno actúa en DELETE.

## Migraciones aplicadas en la nube

Última: **V033** (invitados y detalle de operaciones de la estación). V034/V035 (bancos del Challenge) existen en el repositorio
pero no están aplicadas.
