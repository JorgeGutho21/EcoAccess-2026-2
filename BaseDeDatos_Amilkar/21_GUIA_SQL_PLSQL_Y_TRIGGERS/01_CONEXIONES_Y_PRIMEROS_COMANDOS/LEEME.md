# 01 · Conexiones y primeros comandos

## 1. Qué significa conectarse

Oracle es el **servidor** que guarda los datos. SQL*Plus (consola) y SQL Developer (ventanas) son **clientes**: programas que envían órdenes al servidor. Conectarse es decirle a Oracle *quién eres* (usuario y contraseña) y *a qué base vas* (servidor, puerto y servicio).

| Dato | Valor en EcoAccess |
|---|---|
| Servidor (host) | `localhost` |
| Puerto | `1521` |
| Servicio | `FREEPDB1` |
| Usuario del proyecto | `ECOACCESS_BD` |
| Usuario de las prácticas del profesor | `ECOACCESS_PRACTICAS` |

## 2. Para qué sirve y 3. por qué se usa

Sin conexión no se puede consultar nada. Se usa un usuario por propósito (proyecto, prácticas y solo lectura) para que un error en una práctica no dañe el modelo de EcoAccess. Esto se llama **mínimo privilegio**.

## 4. Cuándo se usa

Al empezar cada sesión de trabajo, antes de ejecutar cualquier script.

## 5. Estructura: conexión desde la consola (formato de clase)

1. Abre **Símbolo del sistema** (CMD).
2. Escribe:

```text
sqlplus /nolog
```

3. Dentro de SQL*Plus escribe:

```text
SQL> CONN ECOACCESS_BD@localhost:1521/FREEPDB1
```

4. Oracle pide `Introduzca la contraseña:`. **Escríbela tú**; no se ve mientras la escribes, y es normal.
5. Debe aparecer `Conectado.`

| Línea | Qué hace |
|---|---|
| `sqlplus /nolog` | Abre SQL*Plus **sin** conectarse todavía. |
| `CONN usuario@host:puerto/servicio` | Se conecta a ese usuario en esa base. La contraseña se pide aparte, para que no quede escrita en pantalla ni en el historial. |
| `SHOW USER` | Muestra con qué usuario estás conectado. |
| `EXIT` | Cierra la sesión. |

> Nunca escribas la contraseña dentro de un script, un README o una captura.

## 5b. Estructura: conexión desde SQL Developer

1. Abre **Oracle SQL Developer**.
2. En el panel **Conexiones**, a la izquierda, haz clic en el **+** de **EcoAccess BD LOCAL**.
3. Si pide contraseña, escríbela en «Contraseña» y pulsa **Aceptar**. Si marcas «Guardar contraseña», SQL Developer no la volverá a pedir en este PC.
4. Al conectarse, debajo aparecen las carpetas **Tablas, Vistas, Índices, Paquetes, Procedimientos, Funciones, Disparadores…**

Si la conexión no existe, créala con el **+ verde** → Nombre `EcoAccess BD LOCAL`, Usuario `ECOACCESS_BD`, Nombre del host `localhost`, Puerto `1521`, **Nombre del servicio** `FREEPDB1` → **Probar** (debe decir «Estado: Correcto») → **Guardar** → **Conectar**.

### Hoja de trabajo, Ctrl+Enter y F5

| Acción | Cómo |
|---|---|
| Abrir una hoja SQL | Clic derecho en la conexión → **Abrir Hoja de Trabajo SQL**, o el botón «SQL» de la barra. |
| Elegir la conexión correcta | Lista desplegable de la esquina superior derecha de la hoja: debe decir **EcoAccess BD LOCAL**. |
| Ejecutar UNA sentencia | Pon el cursor sobre ella y pulsa **Ctrl+Enter** (triángulo verde). El resultado sale en la pestaña **Resultado de la Consulta**, como tabla. |
| Ejecutar un script completo | **F5** («Ejecutar script»). El resultado sale como texto en **Salida de Script**. Necesario para `PROMPT`, `DEFINE`, `SET SERVEROUTPUT ON` y `PRINT`. |
| Abrir un archivo `.sql` | **Archivo → Abrir** → elige el archivo → selecciona la conexión en la lista de la derecha. |
| Guardar un archivo `.sql` | **Archivo → Guardar** (Ctrl+S). |
| Ver tablas, vistas, paquetes, procedimientos, funciones y disparadores | Expande la conexión; cada tipo de objeto tiene su carpeta. Doble clic en un objeto abre sus columnas, sus datos y su código. |
| Ver los datos de una tabla | Doble clic en la tabla → pestaña **Datos**. |
| Ver mensajes de `DBMS_OUTPUT` | **Ver → Salida de DBMS**, pulsa el **+** y elige la conexión. Con F5 basta `SET SERVEROUTPUT ON`. |

## 6. Ejemplo básico y 7. ejemplo de EcoAccess

Archivo: [`01_primeros_comandos.sql`](01_primeros_comandos.sql). Ábrelo y ejecútalo con F5.

## 8. Explicación línea por línea

| Sentencia | Qué hace |
|---|---|
| `SELECT USER FROM dual;` | Muestra el usuario conectado. `dual` es una tabla de Oracle de una sola fila, que sirve para calcular cosas sin leer una tabla real. |
| `SELECT SYS_CONTEXT('USERENV','CON_NAME') FROM dual;` | Muestra en qué base (contenedor) estás: debe decir `FREEPDB1`. |
| `SELECT COUNT(*) FROM user_tables;` | Cuenta las tablas de tu usuario: deben ser 19. |
| `SELECT object_type, COUNT(*) … GROUP BY object_type;` | Cuenta los objetos por tipo: tablas, vistas, paquetes, disparadores… |
| `SELECT … FROM user_objects WHERE status <> 'VALID';` | Lista los objetos inválidos: no debe salir ninguno. |

## 9. Resultado esperado

`ECOACCESS_BD`, `FREEPDB1`, `19` tablas, 4 `TRIGGER`, 9 `VIEW` y **0 objetos inválidos**. La salida real está en `../15_RESULTADOS_Y_CAPTURAS`.

## 11. Errores frecuentes

| Error | Causa | Solución |
|---|---|---|
| ORA-01017 | Usuario o contraseña incorrectos. | Escribe de nuevo la contraseña; revisa las mayúsculas. |
| ORA-12541 / ORA-12514 | Oracle o su servicio no están encendidos, o el nombre del servicio está mal. | Usa `FREEPDB1` como **Nombre del servicio**, no como SID; revisa que Oracle esté iniciado. |
| SP2-0640 «No conectado» | Escribiste SQL antes de `CONN`. | Conéctate primero. |
| La hoja no muestra nada con F5 | La salida está en otra pestaña. | Mira **Salida de Script**. |

## 12. Preguntas que podría hacer el profesor

- ¿Qué diferencia hay entre `sqlplus /nolog` y `sqlplus usuario/clave`? *La primera no expone la contraseña en la línea de comandos.*
- ¿Qué es `dual`? *Una tabla de una fila que Oracle trae para hacer cálculos.*
- ¿Por qué hay tres usuarios? *Mínimo privilegio: proyecto, prácticas y solo lectura.*
- ¿Qué diferencia hay entre Ctrl+Enter y F5? *Ctrl+Enter ejecuta una sentencia y muestra una tabla; F5 ejecuta todo como script y muestra texto.*
