# 08 · Paquetes PL/SQL

## 1. Qué significa: procedimiento, función y paquete

| Concepto | Qué es | Ejemplo de EcoAccess |
|---|---|---|
| **Procedimiento** | Realiza una operación. | `PKG_DEPOSITO.verificar` marca un depósito como verificado. |
| **Función** | Devuelve un valor o un resultado. | `PKG_ECOPUNTOS.saldo` devuelve los puntos de una persona. |
| **Paquete** | Agrupa funciones, procedimientos, tipos y variables relacionados bajo un nombre. | `PKG_ECOPUNTOS` agrupa todo lo de los EcoPuntos. |

Un paquete tiene dos partes:

- La **especificación** (`PACKAGE`): la «carta del restaurante», lo que se puede llamar desde afuera.
- El **cuerpo** (`PACKAGE BODY`): la «cocina», el código y lo que es privado.

## 2. Para qué sirve, 3. por qué y 4. cuándo se usa

Sirve para ordenar el código por tema y esconder los detalles internos. Además, Oracle carga el paquete completo de una vez. Se usa cuando hay varias operaciones del mismo asunto: depósitos, EcoPuntos, estadísticas o contexto del usuario.

## 5. Estructura

```sql
CREATE OR REPLACE PACKAGE pkg_ejemplo AS             -- especificación (pública)
    FUNCTION  calcular(p IN NUMBER) RETURN NUMBER;
    PROCEDURE registrar(p IN NUMBER);
END pkg_ejemplo;
/
CREATE OR REPLACE PACKAGE BODY pkg_ejemplo AS        -- cuerpo (implementación)
    FUNCTION calcular(p IN NUMBER) RETURN NUMBER IS BEGIN RETURN p * 2; END;
    PROCEDURE registrar(p IN NUMBER) IS BEGIN NULL; END;
END pkg_ejemplo;
/
-- Llamada:  pkg_ejemplo.calcular(5)
```

## 6. Ejemplo básico

Bloques A y B de [`08_paquetes.sql`](08_paquetes.sql): consultan qué paquetes existen y qué subprogramas públicos tiene cada uno.

## 7. Los 4 paquetes reales (extraídos de `04_SCRIPTS_ORACLE/07_procedimientos_funciones_paquetes.sql`)

### PKG_CONTEXTO_APP — quién está actuando

| Subprograma | Tipo | Parámetros | Qué hace |
|---|---|---|---|
| `fijar_actor` | Procedimiento | `p_id_usuario`, `p_motivo` (opcional) | Guarda en la sesión quién actúa y por qué. Los disparadores lo leen para auditar. |
| `limpiar` | Procedimiento | — | Borra ese contexto. |
| `actor` / `motivo` | Funciones | — | Devuelven el actor y el motivo guardados. |
| `tiene_rol` | Función → NUMBER | `p_id_usuario`, `p_codigo` (1 Ciudadano, 2 Operario, 3 Administrador) | 1 si la persona está activa y tiene ese rol; 0 si no. |
| `exigir_rol` | Procedimiento | `p_id_usuario`, `p_codigos` (por ejemplo `'2,3'`) | Lanza ORA-20120 si la persona no tiene ninguno de esos roles (ORA-20100 si no existe o está inactiva). |

### PKG_ECOPUNTOS — el libro de puntos

| Subprograma | Tipo | Qué hace y reglas |
|---|---|---|
| `regla_vigente(p_id_material, p_fecha_utc)` | Función → id_regla | Busca la tarifa vigente en esa fecha (ORA-20203 si hubiera dos). |
| `registrar_regla(…)` | Procedimiento | Crea una versión nueva de tarifa (puntos ≥ 1; ORA-20206 y ORA-20207). **Modifica datos.** |
| `acreditar(p_id_deposito, p_id_actor, p_id_movimiento OUT)` | Procedimiento | Solo acredita depósitos VERIFICADOS (ORA-20201). Si el depósito ya tiene un crédito sin revertir, devuelve ese mismo (idempotencia); la clave única es `CRED-<depósito>-<n>`. Si el material no tiene tarifa vigente, no acredita. **Modifica datos.** |
| `revertir(p_id_deposito, p_id_actor, p_motivo)` | Procedimiento | Agrega un movimiento negativo; exige motivo (ORA-20112) y un crédito vigente (ORA-20205). **Modifica datos.** |
| `saldo(p_id_usuario)` | Función → NUMBER | Suma de los movimientos de la persona. |

### PKG_DEPOSITO — la verificación

| Subprograma | Qué hace | Reglas |
|---|---|---|
| `verificar(p_id_deposito, p_id_verificador, p_id_material, p_cantidad, [peso, instrumento, precisión, motivo])` | Pasa el depósito de 0 a 1 y llama a `PKG_ECOPUNTOS.acreditar`. | El depósito debe estar pendiente (ORA-20111). El disparador 2 valida quién verifica. **Modifica datos.** |
| `rechazar(p_id_deposito, p_id_verificador, p_motivo)` | Pasa de 0 a 2. | Exige motivo (ORA-20112). **Modifica datos.** |
| `corregir_verificacion(…)` | Corrige un depósito ya cerrado: reabrir (0), verificar con otros datos (1) o rechazar (2). | Exige motivo; activa `correccion_en_curso` para que el disparador 4 lo deje pasar. **Modifica datos.** |
| `correccion_en_curso` | Función → BOOLEAN | TRUE solo mientras se ejecuta una corrección autorizada. |

### PKG_ESTADISTICAS — los reportes

| Subprograma | Qué hace |
|---|---|
| `consultar_resumen(desde, hasta, punto, categoria, p_resultado OUT SYS_REFCURSOR)` | Resumen del periodo (ORA-20801, 20802 y 20803 si los filtros son inválidos). |
| `ranking_verificados(desde, hasta, top)` → SYS_REFCURSOR | Las personas con más depósitos verificados en el periodo. |
| `a_utc(fecha_colombia)` → TIMESTAMP | Convierte una fecha de Colombia a UTC (la base guarda en UTC). |

## 8. Explicación línea por línea (bloque D)

| Línea | Qué hace |
|---|---|
| `SUM(mv.puntos) AS suma_movimientos` | Suma los movimientos directamente con SQL. |
| `pkg_ecopuntos.saldo(mv.id_usuario) AS saldo_paquete` | Pide el saldo al paquete: se escribe `paquete.función`. |
| resultado | Las dos columnas deben ser **iguales**: así se comprueba que el saldo es la suma de los movimientos. |

## 9. Resultado esperado

- A: 8 filas, 4 `PACKAGE` y 4 `PACKAGE BODY`, todas `VALID`.
- C: 3 personas del personal (C-0001 y C-0002 operarios, C-0003 administrador).
- D: suma_movimientos = saldo_paquete.
- E: solo 7 materiales tienen tarifa vigente (CARTON, LATA_ALUMINIO, PAPEL, PET, RESTO_FRUTA_VERDURA, TETRAPAK y VIDRIO). Los demás devuelven NULL: no dan EcoPuntos.
- F: el ranking de verificados del periodo.

La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 10. Cómo ejecutarlo

**F5**. Para ver el código: **Conexiones → EcoAccess BD LOCAL → Paquetes** → expande el paquete → doble clic en la especificación o en el cuerpo.

## 11. Errores frecuentes

| Error | Causa |
|---|---|
| ORA-04063 «el cuerpo del paquete tiene errores» | El `PACKAGE BODY` no compiló. Revisa `SHOW ERRORS PACKAGE BODY nombre`. |
| PLS-00302 «el componente debe ser declarado» | Se llamó a algo que no está en la especificación (es privado) o el nombre está mal escrito. |
| ORA-06508 | El paquete no se encuentra o está inválido. |

## 12. Preguntas del profesor

- ¿Qué va en la especificación y qué en el cuerpo? *En la especificación, lo público; en el cuerpo, el código.*
- ¿Por qué el trigger no acredita los EcoPuntos? *Para no esconder lógica de negocio: solo `PKG_ECOPUNTOS.acreditar` acredita, con idempotencia.*
- ¿Cómo sabe el disparador quién actuó? *Por `PKG_CONTEXTO_APP.actor`, que fijó el procedimiento antes del UPDATE.*
