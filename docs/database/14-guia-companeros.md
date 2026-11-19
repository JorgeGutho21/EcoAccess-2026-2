# 14 · Guía para compañeros: de cero a la base funcionando

Para alguien que nunca configuró EcoAccess. Todas las rutas son relativas a la carpeta del repositorio. **No necesita llamar a nadie**, salvo para recibir su cuenta y el wallet (paso 7).

## 1. Requisitos previos

- Windows 10 u 11 y conexión a Internet. Si va a usar Oracle Cloud, el puerto 1522 de salida debe estar abierto; en redes universitarias a veces está bloqueado.
- Una cuenta de GitHub con acceso al repositorio `JorgeGutho21/EcoAccess-2026-2`.

## 2. Herramientas y versiones compatibles

| Herramienta | Versión | Obligatoria |
|---|---|---|
| Git | cualquiera reciente | Sí |
| Oracle SQL Developer | 23.x o superior (probado con 26.2) | Sí |
| PowerShell 7 (`pwsh`) | 7.4 o superior | Para los scripts |
| SQL*Plus o SQLcl | el de Oracle Instant Client 19+ o el de Oracle Database Free | Para los scripts |
| Visual Studio 2022 | con «Desarrollo de escritorio .NET» y .NET Framework 4.8 | Solo para la aplicación de Programación III |
| Oracle Database Free | 23ai o 26ai | Solo en el Escenario B |

## 3. Descargar el repositorio

```powershell
git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git
cd EcoAccess-2026-2
```

## 4. Abrir el proyecto

- **Visual Studio:** abra `EcoAccess.P3.sln`. Es la aplicación de Programación III; para la base no hace falta compilarla.
- **Base de datos:** todo está en dos carpetas:
  - [`database/`](../../database/): scripts, el esquema real en `database/schema`, migraciones y pruebas;
  - [`docs/database/`](README.md): esta documentación.

## 5. Instalar SQL Developer

Descárguelo de <https://www.oracle.com/database/sqldeveloper/> (versión *Windows 64-bit with JDK included*), descomprímalo y ejecute `sqldeveloper.exe`. No necesita instalación.

## 6. ¿Oracle Cloud o una base local?

| Si quiere… | Use | Escenario |
|---|---|---|
| Ver y consultar **los datos reales** del proyecto, igual que la aplicación | **Oracle Cloud** | **A** |
| Practicar, probar cambios, ejecutar pruebas que escriben o instalar desde cero | **Oracle local**, en un esquema propio | **B** |

Las dos bases **no se sincronizan solas** (ver [01-arquitectura.md](01-arquitectura.md)).

## 7. Conseguir acceso

- **Escenario A:** pida al administrador de Oracle Cloud (Jorge) que cree su cuenta `ECO_DEV_<SUNOMBRE>` con [`06_crear_cuenta_integrante.sql`](../../database/00_administracion/06_crear_cuenta_integrante.sql) y que le entregue el **wallet** (`Wallet_EcoAccess.zip`) **en persona o por un canal privado**. Recibirá una contraseña temporal, que deberá cambiar al primer inicio.
- **Escenario B:** instale Oracle Database Free. Después cree su esquema con `scripts\pruebas\Crear-Esquema-Aislado-Local.ps1` (ver [02-conexion-local.md](02-conexion-local.md)); el script le pedirá **su propia** contraseña de SYSTEM.

**Nunca** pida ni comparta por chat, GitHub o correo la contraseña de ADMIN, la de `ECOACCESS_P3_APP` ni el wallet.

## 8. Crear la conexión

- **A, Oracle Cloud:** SQL Developer → **+** → *Connection Type* **Cloud Wallet** → elija el zip → *Service* **`ecoaccess_tp`** → su usuario `ECO_DEV_…`. Detalle en [03-oracle-cloud.md](03-oracle-cloud.md).
- **B, local:** *Connection Type* **Basic**, `localhost`, `1521`, *Service name* `FREEPDB1`, su esquema. Detalle en [02-conexion-local.md](02-conexion-local.md).

Ponga a la conexión un nombre que diga qué base es, por ejemplo `EcoAccess CLOUD - ECO_DEV_ANA`.

## 9. Probar la conexión

Botón **Test** → `Status: Success`. Si falla, vea la tabla de errores del paso 19.

## 10. Identificar su esquema

En la hoja de trabajo (**Ctrl+Enter** ejecuta la línea):
```sql
SELECT USER, SYS_CONTEXT('USERENV','CURRENT_SCHEMA') AS esquema, SYS_CONTEXT('USERENV','SERVICE_NAME') AS servicio FROM dual;
```

**Esperado:**
- **A:** `ECO_DEV_…`, el mismo esquema y un servicio que termina en `_ECOACCESS_tp…`. Después ejecute `ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;` para leer las tablas del proyecto.
- **B:** su esquema y `freepdb1`.

## 11. Comprobar las tablas

- **Árbol:** en A, *Otros usuarios → ECOACCESS_P3_APP → Tablas*; en B, *Tablas*. Deben ser **33**.
- **Consulta:** `SELECT table_name FROM all_tables WHERE owner = 'ECOACCESS_P3_APP' ORDER BY 1;` (en B use `user_tables`).
- **Qué es cada columna:** [05-tablas-relaciones.md](05-tablas-relaciones.md).

## 12. Ver las vistas

*Vistas* → por ejemplo `VW_CANECA_ESTACION` → pestaña **Datos**. O bien:
```sql
SELECT estacion, caneca, color, estado_texto FROM vw_caneca_estacion WHERE es_sintetico = 0 AND estado = 1;
```

**Esperado en Oracle Cloud:** las tres canecas de la estación piloto, BLANCA, VERDE y NEGRA, EN SERVICIO. Sin `AND estado = 1` aparecen además dos canecas DEMO fuera de servicio (hallazgo H-17). Explicación de las nueve vistas: [06-vistas.md](06-vistas.md).

## 13. Encontrar los paquetes

*Paquetes* → `PKG_CONTEXTO_APP`, `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PKG_ESTADISTICAS` y `PKG_ECOACCESS_STATS`. Al expandir cada uno aparece el *Cuerpo*. Explicación: [07-paquetes.md](07-paquetes.md).

## 14. Revisar los procedimientos

*Procedimientos* → `PR_CAMBIAR_ESTADO_CONTENEDOR` y `PR_CONSULTAR_ESTADISTICAS`. Explicación: [08-procedimientos.md](08-procedimientos.md).

## 15. Consultar las funciones

*Funciones* → `FN_RESOLVER_CANECA_DISPONIBLE`, `FN_PESO_ESTIMADO_KG` y `FN_DEPOSITOS_CIUDADANO`. Pruebe:
```sql
SELECT descripcion_objeto, fn_peso_estimado_kg(id_peso_referencial, 2) AS kg FROM peso_referencial;
```
Explicación: [09-funciones.md](09-funciones.md).

## 16. Revisar los disparadores

*Disparadores* → los 5 `TRG_*` y `TR_ECOP_UNIDADES`. Explicación: [10-triggers-secuencias.md](10-triggers-secuencias.md).

## 17. ¿Cuándo instalar el esquema y cuándo no?

| Situación | ¿Instalar? |
|---|---|
| Se conecta a **Oracle Cloud** (A) | **NO.** Los objetos ya existen. No ejecute nada de `database/schema`, `database/02_tablas` ni las migraciones |
| Tiene un esquema local **vacío** (B) | **Sí**, con `database/schema/00_instalar_esquema_completo.sql` (ver [04-instalacion.md](04-instalacion.md)) |
| Su esquema local ya tiene objetos | No reinstale encima. Pida un esquema nuevo o aplique solo las migraciones que falten |
| Quiere **cambiar** la estructura | Escriba una migración nueva `V0NN__…sql` y súbala en un pull request para revisión. Nunca edite objetos a mano en la base compartida |

## 18. Ejecutar las pruebas

- **A, cuenta de integrante:** abra [`database/tests/A2_verificacion_integrante.sql`](../../database/tests/A2_verificacion_integrante.sql) y pulse **F5**. Debe terminar en `RESULTADO INTEGRANTE: 7 correctas, 0 fallidas.`
- **B o cuenta dueña:**
  ```powershell
  pwsh -File database\scripts\Ejecutar-PruebasBD.ps1 -Secreto <su id de User Secrets> -IncluirEscritura
  ```
  La evidencia queda en `docs/database/evidencias/`. Detalle en [13-pruebas.md](13-pruebas.md).

## 19. Errores comunes

| Mensaje | Causa probable | Solución |
|---|---|---|
| `ORA-01017: credenciales no válidas` | Usuario o contraseña incorrectos, o el `TNS_ADMIN` apunta a otra base | Revise usuario y contraseña. En local, el `TNS_ADMIN` no debe apuntar al wallet de la nube |
| `ORA-28001: la contraseña ha caducado` | Es la temporal | SQL Developer le pedirá una nueva: elíjala |
| `ORA-12506` / `ORA-12541` / no conecta a la nube | Base detenida, puerto 1522 bloqueado o wallet equivocado | Pida que revisen que la base esté *Available*. Pruebe otra red. Use el zip correcto y el servicio `ecoaccess_tp` |
| `ORA-12514: listener does not currently know of service` | Servicio mal escrito | En local: `FREEPDB1` como *Service name*, no SID |
| `ORA-00942: la tabla o vista no existe` | Está en su propio esquema | `ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;` o use `ECOACCESS_P3_APP.tabla` |
| `ORA-01031: privilegios insuficientes` | Su rol es CONSULTA e intentó escribir | Es lo esperado. Pida EDICION solo si lo necesita |
| `ORA-20991` al instalar | El esquema no está vacío | No reinstale encima: use un esquema nuevo |
| `ORA-201xx` / `ORA-208xx` | Una regla de EcoAccess rechazó la operación | El mensaje dice cuál; ver la tabla de códigos en [07-paquetes.md](07-paquetes.md) |

## 20. ¿Cómo sé que todo quedó bien?

- [ ] **Test** de la conexión: *Success*.
- [ ] El paso 10 muestra el usuario y el servicio esperados.
- [ ] Ve 33 tablas, 21 vistas, 5 paquetes, 2 procedimientos, 3 funciones y 5 disparadores.
- [ ] `VW_CANECA_ESTACION` muestra las tres canecas (en Oracle Cloud).
- [ ] Las pruebas terminan sin `FALLO`: `A2` en la nube, o `Ejecutar-PruebasBD.ps1` en local.
- [ ] No guardó la contraseña ni el wallet dentro de la carpeta del repositorio. `git status` no muestra ningún archivo del wallet.
