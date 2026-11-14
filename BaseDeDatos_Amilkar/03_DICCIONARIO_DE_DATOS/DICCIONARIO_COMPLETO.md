# Diccionario de datos COMPLETO — una ficha por atributo

Generado el 2026-11-11 20:23 desde Oracle (metadatos reales) y el código del proyecto. 173 atributos en 19 tablas.

## Módulo: M1 · Catálogos base

### MATERIAL (6 atributos)

Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.

#### 1. `MATERIAL.ID_MATERIAL`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** ID_MATERIAL
- **3. Qué significa:** Número del material.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de MATERIAL, de modo que otras tablas puedan apuntarle. Se usa en: FK en todo el flujo.
- **5. Por qué existe:** Identificar el material.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY BY DEFAULT
- **9. ¿Es PK o FK? (restricciones):** PK; UNIQUE: UQ_MATERIAL_CATEGORIA; IDENTITY BY DEFAULT
- **10. Con qué tabla se relaciona:** Es referenciada por: CONSULTA_VISION, CONTENEDOR_MATERIAL, DEPOSITO, INTERACCION, PESO_REFERENCIAL, REGLA_ECOPUNTOS
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Catálogo. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_BI_ECOPUNTOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de MATERIAL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, TRG_DEPOSITO_COHERENCIA, VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_BI_ECOPUNTOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS, VW_BI_CONSULTAS_VISION, VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_MATERIAL es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM MATERIAL WHERE ID_MATERIAL = (SELECT MIN(ID_MATERIAL) FROM MATERIAL);
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. ID_MATERIAL es la casilla que guarda: Número del material. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.
- **Explicación ampliada:** Es el número de cada material del catálogo (botella PET, cáscara de fruta, papel…). La IA, el ciudadano y el operario siempre hablan de materiales con este número: así no hay nombres escritos de formas distintas. Aparece como FK en INTERACCION (sugerido y confirmado), CONSULTA_VISION, CONTENEDOR_MATERIAL, PESO_REFERENCIAL, REGLA_ECOPUNTOS y DEPOSITO (declarado y verificado).

#### 2. `MATERIAL.CODIGO`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** CODIGO
- **3. Qué significa:** Código corto en MAYÚSCULAS.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MATERIAL: código corto en MAYÚSCULAS. Se usa en: Lo usa la IA y los reportes.
- **5. Por qué existe:** UNIQUE y formato fijo: la IA no puede inventar materiales fuera del catálogo.
- **6. Tipo de dato:** VARCHAR2(30 CHAR) (hasta 30 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_MATERIAL_CODIGO; CHECK CK_MATERIAL_CODIGO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_BI_ECOPUNTOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** PET
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_MATERIAL_CODIGO (condición completa en el anexo de CHECK) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_MATERIAL_CODIGO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MATERIAL_CODIGO · Unicidad: UQ_MATERIAL_CODIGO
- **17. Qué pasaría si no existiera:** La tabla MATERIAL no podría guardar este dato (código corto en MAYÚSCULAS). UNIQUE y formato fijo: la IA no puede inventar materiales fuera del catálogo.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_BI_ECOPUNTOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS, VW_BI_CONSULTAS_VISION, VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MATERIAL_CODIGO y por qué esos y no otros? · ¿Por qué CODIGO no puede repetirse? (UNIQUE UQ_MATERIAL_CODIGO) · ¿Por qué CODIGO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CODIGO, COUNT(*) AS filas FROM MATERIAL GROUP BY CODIGO ORDER BY CODIGO;
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. CODIGO es la casilla que guarda: Código corto en MAYÚSCULAS. Nunca puede quedar vacía.

#### 3. `MATERIAL.NOMBRE`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** NOMBRE
- **3. Qué significa:** Nombre para personas.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MATERIAL: nombre para personas. Se usa en: Mensajes del bot y reportes.
- **5. Por qué existe:** Lectura humana.
- **6. Tipo de dato:** VARCHAR2(100 CHAR) (hasta 100 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_MATERIAL_NOMBRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** Botella plástica PET
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_MATERIAL_NOMBRE: TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2 (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 100 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MATERIAL_NOMBRE
- **17. Qué pasaría si no existiera:** La tabla MATERIAL no podría guardar este dato (nombre para personas). Lectura humana.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MATERIAL_NOMBRE y por qué esos y no otros? · ¿Por qué NOMBRE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT NOMBRE, COUNT(*) AS filas FROM MATERIAL GROUP BY NOMBRE ORDER BY NOMBRE;
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. NOMBRE es la casilla que guarda: Nombre para personas. Nunca puede quedar vacía.

#### 4. `MATERIAL.CATEGORIA`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** CATEGORIA
- **3. Qué significa:** 1 aprovechable, 2 orgánico, 3 no aprovechable, 4 peligroso (ruta especial).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MATERIAL: 1 aprovechable, 2 orgánico, 3 no aprovechable, 4 peligroso (ruta especial). Se usa en: Resolver caneca, FK compuesta.
- **5. Por qué existe:** Decide el color de caneca según la Resolución 2184 de 2019.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_MATERIAL_CATEGORIA; CHECK CK_MATERIAL_CATEGORIA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_MATERIAL_CATEGORIA: categoria IN (1, 2, 3, 4) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_MATERIAL_CATEGORIA) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MATERIAL_CATEGORIA · Unicidad: UQ_MATERIAL_CATEGORIA · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla MATERIAL no podría guardar este dato (1 aprovechable, 2 orgánico, 3 no aprovechable, 4 peligroso (ruta especial)). Decide el color de caneca según la Resolución 2184 de 2019.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, TRG_DEPOSITO_COHERENCIA, VW_BI_CONSULTAS_VISION, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS, VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MATERIAL_CATEGORIA y por qué esos y no otros? · ¿Por qué CATEGORIA no puede repetirse? (UNIQUE UQ_MATERIAL_CATEGORIA) · ¿Por qué CATEGORIA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CATEGORIA, COUNT(*) AS filas FROM MATERIAL GROUP BY CATEGORIA ORDER BY CATEGORIA;
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. CATEGORIA es la casilla que guarda: 1 aprovechable, 2 orgánico, 3 no aprovechable, 4 peligroso (ruta especial). Nunca puede quedar vacía.

#### 5. `MATERIAL.DESCRIPCION`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** DESCRIPCION
- **3. Qué significa:** Instrucción de separación.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MATERIAL: instrucción de separación. Se usa en: Orientación del bot.
- **5. Por qué existe:** Educar al ciudadano.
- **6. Tipo de dato:** VARCHAR2(500 CHAR) (hasta 500 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = sin descripción.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Botellas de bebidas marcadas con el número 1.
- **15. Ejemplo de valor incorrecto:** Un texto de más de 500 caracteres (ORA-12899)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla MATERIAL no podría guardar este dato (instrucción de separación). Educar al ciudadano.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que DESCRIPCION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(DESCRIPCION) AS con_valor FROM MATERIAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. DESCRIPCION es la casilla que guarda: Instrucción de separación. Puede quedar vacía: NULL = sin descripción.

#### 6. `MATERIAL.HABILITADO`

- **1. Tabla:** MATERIAL — Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.
- **2. Atributo:** HABILITADO
- **3. Qué significa:** 1 si se puede usar.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MATERIAL: 1 si se puede usar. Se usa en: Resolver caneca.
- **5. Por qué existe:** Retirar un material sin borrar su historia.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 1
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_MATERIAL_HABILITADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Clasificar el residuo (IA, ciudadano y operario)
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Todos los roles y la IA. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_MATERIAL_HABILITADO: habilitado IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MATERIAL_HABILITADO
- **17. Qué pasaría si no existiera:** La tabla MATERIAL no podría guardar este dato (1 si se puede usar). Retirar un material sin borrar su historia.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (27 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** La IA responde con un código de este catálogo.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MATERIAL_HABILITADO y por qué esos y no otros? · ¿Por qué HABILITADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT HABILITADO, COUNT(*) AS filas FROM MATERIAL GROUP BY HABILITADO ORDER BY HABILITADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en MATERIAL como una hoja de cálculo donde cada fila es un tipo de material. HABILITADO es la casilla que guarda: 1 si se puede usar. Nunca puede quedar vacía.

### PESO_REFERENCIAL (9 atributos)

Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.

#### 7. `PESO_REFERENCIAL.ID_PESO_REFERENCIAL`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** ID_PESO_REFERENCIAL
- **3. Qué significa:** Número de la fila del catálogo de pesos.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de PESO_REFERENCIAL, de modo que otras tablas puedan apuntarle. Se usa en: FK desde CONSULTA_VISION y DEPOSITO.
- **5. Por qué existe:** Saber de dónde salió cada estimación.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY BY DEFAULT
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY BY DEFAULT
- **10. Con qué tabla se relaciona:** Es referenciada por: CONSULTA_VISION, DEPOSITO
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Catálogo. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de PESO_REFERENCIAL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_PESO_ESTIMADO_KG, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_PESO_REFERENCIAL es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM PESO_REFERENCIAL WHERE ID_PESO_REFERENCIAL = (SELECT MIN(ID_PESO_REFERENCIAL) FROM PESO_REFERENCIAL);
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. ID_PESO_REFERENCIAL es la casilla que guarda: Número de la fila del catálogo de pesos. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 8. `PESO_REFERENCIAL.ID_MATERIAL`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** ID_MATERIAL
- **3. Qué significa:** Material del objeto.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de PESO_REFERENCIAL y poder unirlas con JOIN. Se usa en: Agrupar pesos por material.
- **5. Por qué existe:** Relación 1:N material-objetos.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_PESO_MATERIAL->MATERIAL
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_PESO_MATERIAL)
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con MATERIAL · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre PESO_REFERENCIAL y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Por qué ID_MATERIAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL, COUNT(*) AS filas FROM PESO_REFERENCIAL t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL GROUP BY t.ID_MATERIAL ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. ID_MATERIAL es la casilla que guarda: Material del objeto. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Nunca puede quedar vacía.

#### 9. `PESO_REFERENCIAL.CLAVE_OBJETO`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** CLAVE_OBJETO
- **3. Qué significa:** Objeto que reconoce la IA.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: objeto que reconoce la IA. Se usa en: Unir la respuesta de la IA con el peso.
- **5. Por qué existe:** Un material (PET) tiene objetos de pesos muy distintos.
- **6. Tipo de dato:** VARCHAR2(40 CHAR) (hasta 40 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_PESO_OBJETO_ESTADO; CHECK CK_PESO_CLAVE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** BOTELLA_PET_600ML
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_CLAVE: LENGTH(clave_objeto) >= 2 AND TRANSLATE(clave_objeto, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_PESO_OBJETO_ESTADO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_CLAVE · Unicidad: UQ_PESO_OBJETO_ESTADO
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (objeto que reconoce la IA). Un material (PET) tiene objetos de pesos muy distintos.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_CLAVE y por qué esos y no otros? · ¿Por qué CLAVE_OBJETO no puede repetirse? (UNIQUE UQ_PESO_OBJETO_ESTADO) · ¿Por qué CLAVE_OBJETO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CLAVE_OBJETO, COUNT(*) AS filas FROM PESO_REFERENCIAL GROUP BY CLAVE_OBJETO ORDER BY CLAVE_OBJETO;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. CLAVE_OBJETO es la casilla que guarda: Objeto que reconoce la IA. Nunca puede quedar vacía.

#### 10. `PESO_REFERENCIAL.DESCRIPCION_OBJETO`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** DESCRIPCION_OBJETO
- **3. Qué significa:** Nombre legible del objeto.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: nombre legible del objeto. Se usa en: Reportes.
- **5. Por qué existe:** Lectura humana.
- **6. Tipo de dato:** VARCHAR2(150 CHAR) (hasta 150 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PESO_DESCRIPCION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Botella PET de 600 ml
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_DESCRIPCION: TRIM(descripcion_objeto) IS NOT NULL AND descripcion_objeto = TRIM(descripcion_objeto) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 150 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_DESCRIPCION
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (nombre legible del objeto). Lectura humana.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_DESCRIPCION y por qué esos y no otros? · ¿Por qué DESCRIPCION_OBJETO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT DESCRIPCION_OBJETO, COUNT(*) AS filas FROM PESO_REFERENCIAL GROUP BY DESCRIPCION_OBJETO ORDER BY DESCRIPCION_OBJETO;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. DESCRIPCION_OBJETO es la casilla que guarda: Nombre legible del objeto. Nunca puede quedar vacía.

#### 11. `PESO_REFERENCIAL.ESTADO_VISUAL`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** ESTADO_VISUAL
- **3. Qué significa:** 0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: 0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno. Se usa en: Estimación según lo que se ve en la foto.
- **5. Por qué existe:** Una botella llena pesa 30 veces más que una vacía.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_PESO_OBJETO_ESTADO; CHECK CK_PESO_ESTADO_VISUAL
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_ESTADO_VISUAL: estado_visual IN (0, 1, 2, 3) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_PESO_OBJETO_ESTADO)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_ESTADO_VISUAL · Unicidad: UQ_PESO_OBJETO_ESTADO
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno). Una botella llena pesa 30 veces más que una vacía.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_ESTADO_VISUAL y por qué esos y no otros? · ¿Por qué ESTADO_VISUAL no puede repetirse? (UNIQUE UQ_PESO_OBJETO_ESTADO) · ¿Por qué ESTADO_VISUAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_VISUAL, COUNT(*) AS filas FROM PESO_REFERENCIAL GROUP BY ESTADO_VISUAL ORDER BY ESTADO_VISUAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. ESTADO_VISUAL es la casilla que guarda: 0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno. Nunca puede quedar vacía.

#### 12. `PESO_REFERENCIAL.GRAMOS_MINIMO`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** GRAMOS_MINIMO
- **3. Qué significa:** Peso mínimo esperado de una unidad.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: peso mínimo esperado de una unidad. Se usa en: Validar estimaciones (trigger 2).
- **5. Por qué existe:** Define el rango de incertidumbre.
- **6. Tipo de dato:** NUMBER(8,1) (8 dígitos en total, 1 decimales)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PESO_ORDEN
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 18
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_ORDEN: gramos_minimo > 0 AND gramos_minimo <= gramos_tipico AND gramos_tipico <= gramos_maximo (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_ORDEN · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (peso mínimo esperado de una unidad). Define el rango de incertidumbre.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_ORDEN y por qué esos y no otros? · ¿Por qué GRAMOS_MINIMO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT COUNT(GRAMOS_MINIMO) AS con_valor, MIN(GRAMOS_MINIMO) AS minimo, MAX(GRAMOS_MINIMO) AS maximo, SUM(GRAMOS_MINIMO) AS total FROM PESO_REFERENCIAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. GRAMOS_MINIMO es la casilla que guarda: Peso mínimo esperado de una unidad. Nunca puede quedar vacía.

#### 13. `PESO_REFERENCIAL.GRAMOS_TIPICO`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** GRAMOS_TIPICO
- **3. Qué significa:** Peso más habitual de una unidad.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: peso más habitual de una unidad. Se usa en: fn_peso_estimado_kg.
- **5. Por qué existe:** Base de la estimación.
- **6. Tipo de dato:** NUMBER(8,1) (8 dígitos en total, 1 decimales)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PESO_ORDEN
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 22
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_ORDEN: gramos_minimo > 0 AND gramos_minimo <= gramos_tipico AND gramos_tipico <= gramos_maximo (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_ORDEN
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (peso más habitual de una unidad). Base de la estimación.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_PESO_ESTIMADO_KG
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_ORDEN y por qué esos y no otros? · ¿Por qué GRAMOS_TIPICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT COUNT(GRAMOS_TIPICO) AS con_valor, MIN(GRAMOS_TIPICO) AS minimo, MAX(GRAMOS_TIPICO) AS maximo, SUM(GRAMOS_TIPICO) AS total FROM PESO_REFERENCIAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. GRAMOS_TIPICO es la casilla que guarda: Peso más habitual de una unidad. Nunca puede quedar vacía.

#### 14. `PESO_REFERENCIAL.GRAMOS_MAXIMO`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** GRAMOS_MAXIMO
- **3. Qué significa:** Peso máximo esperado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: peso máximo esperado. Se usa en: Validar estimaciones.
- **5. Por qué existe:** CHECK mínimo <= típico <= máximo.
- **6. Tipo de dato:** NUMBER(8,1) (8 dígitos en total, 1 decimales)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PESO_ORDEN
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 28
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_ORDEN: gramos_minimo > 0 AND gramos_minimo <= gramos_tipico AND gramos_tipico <= gramos_maximo (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_ORDEN · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (peso máximo esperado). CHECK mínimo <= típico <= máximo.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_ORDEN y por qué esos y no otros? · ¿Por qué GRAMOS_MAXIMO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT COUNT(GRAMOS_MAXIMO) AS con_valor, MIN(GRAMOS_MAXIMO) AS minimo, MAX(GRAMOS_MAXIMO) AS maximo, SUM(GRAMOS_MAXIMO) AS total FROM PESO_REFERENCIAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. GRAMOS_MAXIMO es la casilla que guarda: Peso máximo esperado. Nunca puede quedar vacía.

#### 15. `PESO_REFERENCIAL.FUENTE`

- **1. Tabla:** PESO_REFERENCIAL — Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.
- **2. Atributo:** FUENTE
- **3. Qué significa:** De dónde sale el dato.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PESO_REFERENCIAL: de dónde sale el dato. Se usa en: Defensa del dato ante el profesor.
- **5. Por qué existe:** Obliga a declarar el origen: no hay pesos «mágicos».
- **6. Tipo de dato:** VARCHAR2(200 CHAR) (hasta 200 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PESO_FUENTE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Estimar el peso a partir de una foto
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PESO_REFERENCIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot y el servicio de depósitos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Rango típico de referencia académica; pendiente de medición propia
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PESO_FUENTE: TRIM(fuente) IS NOT NULL AND fuente = TRIM(fuente) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 200 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PESO_FUENTE
- **17. Qué pasaría si no existiera:** La tabla PESO_REFERENCIAL no podría guardar este dato (de dónde sale el dato). Obliga a declarar el origen: no hay pesos «mágicos».
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite mostrar un peso ESTIMADO en la respuesta.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PESO_FUENTE y por qué esos y no otros? · ¿Por qué FUENTE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT FUENTE, COUNT(*) AS filas FROM PESO_REFERENCIAL GROUP BY FUENTE ORDER BY FUENTE;
- **24. Explicación sencilla (para quien empieza):** Piensa en PESO_REFERENCIAL como una hoja de cálculo donde cada fila es el peso típico de un objeto. FUENTE es la casilla que guarda: De dónde sale el dato. Nunca puede quedar vacía.

## Módulo: M2 · Usuarios e identidad

### ROL (3 atributos)

Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.

#### 16. `ROL.ID_ROL`

- **1. Tabla:** ROL — Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.
- **2. Atributo:** ID_ROL
- **3. Qué significa:** Número interno que identifica cada rol.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de ROL, de modo que otras tablas puedan apuntarle. Se usa en: FK en USUARIO_ROL.
- **5. Por qué existe:** Permite relacionar roles sin repetir su texto.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY BY DEFAULT
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY BY DEFAULT
- **10. Con qué tabla se relaciona:** Es referenciada por: USUARIO_ROL
- **11. Qué proceso lo usa:** Control de acceso: decide qué puede hacer cada persona
- **12. Quién lo llena:** Catálogo (10_catalogos.sql). INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT en las vistas VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de ROL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_CONTEXTO_APP, VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (15 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot muestra comandos distintos según el rol.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_ROL es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM ROL WHERE ID_ROL = (SELECT MIN(ID_ROL) FROM ROL);
- **24. Explicación sencilla (para quien empieza):** Piensa en ROL como una hoja de cálculo donde cada fila es un rol. ID_ROL es la casilla que guarda: Número interno que identifica cada rol. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 17. `ROL.CODIGO`

- **1. Tabla:** ROL — Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.
- **2. Atributo:** CODIGO
- **3. Qué significa:** Código de negocio del rol, igual al enum CodigoRol de C#: 1 Ciudadano, 2 Operario, 3 Administrador.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ROL: código de negocio del rol, igual al enum CodigoRol de C#: 1 Ciudadano, 2 Operario, 3 Administrador. Se usa en: Permisos en PKG_CONTEXTO_APP y MatrizPermisos de P3.
- **5. Por qué existe:** Evita inventar un cuarto rol (CHECK 1-3) y mantiene el mismo número que el código C#.
- **6. Tipo de dato:** NUMBER(2) (entero de hasta 2 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_ROL_CODIGO; CHECK CK_ROL_CODIGO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Control de acceso: decide qué puede hacer cada persona
- **12. Quién lo llena:** Catálogo. INSERT/UPDATE directo sobre ROL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT en las vistas VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_ROL_CODIGO: codigo IN (1, 2, 3) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_ROL_CODIGO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_ROL_CODIGO · Unicidad: UQ_ROL_CODIGO
- **17. Qué pasaría si no existiera:** La tabla ROL no podría guardar este dato (código de negocio del rol, igual al enum CodigoRol de C#: 1 Ciudadano, 2 Operario, 3 Administrador). Evita inventar un cuarto rol (CHECK 1-3) y mantiene el mismo número que el código C#.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_CONTEXTO_APP, VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (15 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot muestra comandos distintos según el rol.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ROL_CODIGO y por qué esos y no otros? · ¿Por qué CODIGO no puede repetirse? (UNIQUE UQ_ROL_CODIGO) · ¿Por qué CODIGO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CODIGO, COUNT(*) AS filas FROM ROL GROUP BY CODIGO ORDER BY CODIGO;
- **24. Explicación sencilla (para quien empieza):** Piensa en ROL como una hoja de cálculo donde cada fila es un rol. CODIGO es la casilla que guarda: Código de negocio del rol, igual al enum CodigoRol de C#: 1 Ciudadano, 2 Operario, 3 Administrador. Nunca puede quedar vacía.

#### 18. `ROL.DESCRIPCION`

- **1. Tabla:** ROL — Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.
- **2. Atributo:** DESCRIPCION
- **3. Qué significa:** Nombre legible del rol.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ROL: nombre legible del rol. Se usa en: Pantallas y reportes.
- **5. Por qué existe:** Mostrar el rol con palabras sin depender del número.
- **6. Tipo de dato:** VARCHAR2(200 CHAR) (hasta 200 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_ROL_DESCRIPCION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Control de acceso: decide qué puede hacer cada persona
- **12. Quién lo llena:** Catálogo. INSERT/UPDATE directo sobre ROL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Operario
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_ROL_DESCRIPCION: TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 200 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_ROL_DESCRIPCION
- **17. Qué pasaría si no existiera:** La tabla ROL no podría guardar este dato (nombre legible del rol). Mostrar el rol con palabras sin depender del número.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (15 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot muestra comandos distintos según el rol.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ROL_DESCRIPCION y por qué esos y no otros? · ¿Por qué DESCRIPCION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT DESCRIPCION, COUNT(*) AS filas FROM ROL GROUP BY DESCRIPCION ORDER BY DESCRIPCION;
- **24. Explicación sencilla (para quien empieza):** Piensa en ROL como una hoja de cálculo donde cada fila es un rol. DESCRIPCION es la casilla que guarda: Nombre legible del rol. Nunca puede quedar vacía.

### USUARIO (8 atributos)

Cada persona registrada. No guarda contraseñas.

#### 19. `USUARIO.ID_USUARIO`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Número único de cada persona registrada (IDENTITY ALWAYS: nadie lo escribe).
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de USUARIO, de modo que otras tablas puedan apuntarle. Se usa en: FK en casi todo el modelo; seudónimo C-0017 en Power BI.
- **5. Por qué existe:** Identifica a la persona aunque cambie su nombre o correo.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: CONSULTA_VISION, CREDENCIAL_ACCESO, DEPOSITO, HISTORIAL_ESTADO_CONTENEDOR, INTERACCION, MOVIMIENTO_ECOPUNTOS, REGLA_ECOPUNTOS, USUARIO_ROL, VINCULO_TELEGRAM
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Oracle al insertar. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT en las vistas VW_BI_CIUDADANOS, VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 17
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de USUARIO, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_DEPOSITOS_CIUDADANO, PKG_CONTEXTO_APP, PKG_DEPOSITO, PKG_ECOPUNTOS, PKG_ESTADISTICAS, PR_CAMBIAR_ESTADO_CONTENEDOR, TRG_DEPOSITO_COHERENCIA, VW_BI_CIUDADANOS, VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_USUARIO es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM USUARIO WHERE ID_USUARIO = (SELECT MIN(ID_USUARIO) FROM USUARIO);
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. ID_USUARIO es la casilla que guarda: Número único de cada persona registrada (IDENTITY ALWAYS: nadie lo escribe). Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.
- **Explicación ampliada:** Es la «cédula interna» de cada persona dentro de EcoAccess. Oracle la pone sola (IDENTITY ALWAYS), así nadie puede inventarla ni repetirla. Casi todas las tablas guardan una copia de este número para decir «esto lo hizo esta persona»: la sesión QR (INTERACCION), la foto (CONSULTA_VISION), los EcoPuntos (MOVIMIENTO_ECOPUNTOS), la verificación (DEPOSITO.ID_VERIFICADOR) y los roles (USUARIO_ROL). Si mañana la persona cambia su nombre o su correo, el número sigue igual y nada se rompe. En Power BI no se muestra el número real: se muestra un seudónimo como C-0017.

#### 20. `USUARIO.NOMBRE`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** NOMBRE
- **3. Qué significa:** Nombre de la persona, sin espacios sobrantes.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: nombre de la persona, sin espacios sobrantes. Se usa en: Listados del operario, auditoría.
- **5. Por qué existe:** CHECK impide nombres vacíos o de 1 letra.
- **6. Tipo de dato:** VARCHAR2(100 CHAR) (hasta 100 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_USUARIO_NOMBRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Registro (formulario o bot). INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT en las vistas VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** María Gómez
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_USUARIO_NOMBRE: TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2 (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 100 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_USUARIO_NOMBRE
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (nombre de la persona, sin espacios sobrantes). CHECK impide nombres vacíos o de 1 letra.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_NOMBRE y por qué esos y no otros? · ¿Por qué NOMBRE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT NOMBRE, COUNT(*) AS filas FROM USUARIO GROUP BY NOMBRE ORDER BY NOMBRE;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. NOMBRE es la casilla que guarda: Nombre de la persona, sin espacios sobrantes. Nunca puede quedar vacía.

#### 21. `USUARIO.CORREO_ELECTRONICO`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** CORREO_ELECTRONICO
- **3. Qué significa:** Correo en minúsculas; es único.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: correo en minúsculas; es único. Se usa en: Inicio de sesión de escritorio, búsqueda del operario.
- **5. Por qué existe:** UNIQUE + minúsculas impide dos cuentas con el mismo correo escrito distinto.
- **6. Tipo de dato:** VARCHAR2(254 CHAR) (hasta 254 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_USUARIO_CORREO; CHECK CK_USUARIO_CORREO_FORMA; CHECK CK_USUARIO_CORREO_MIN
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Registro. INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** maria@ejemplo.test
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_USUARIO_CORREO_FORMA (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_USUARIO_CORREO_MIN: correo_electronico = LOWER(correo_electronico) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_USUARIO_CORREO)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_USUARIO_CORREO_FORMA, CK_USUARIO_CORREO_MIN · Unicidad: UQ_USUARIO_CORREO
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (correo en minúsculas; es único). UNIQUE + minúsculas impide dos cuentas con el mismo correo escrito distinto.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_CORREO_FORMA y por qué esos y no otros? · ¿Por qué CORREO_ELECTRONICO no puede repetirse? (UNIQUE UQ_USUARIO_CORREO) · ¿Por qué CORREO_ELECTRONICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CORREO_ELECTRONICO, COUNT(*) AS filas FROM USUARIO GROUP BY CORREO_ELECTRONICO ORDER BY CORREO_ELECTRONICO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. CORREO_ELECTRONICO es la casilla que guarda: Correo en minúsculas; es único. Nunca puede quedar vacía.

#### 22. `USUARIO.FECHA_REGISTRO_UTC`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** FECHA_REGISTRO_UTC
- **3. Qué significa:** Momento (UTC) en que se creó la cuenta.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: momento (UTC) en que se creó la cuenta. Se usa en: Reportes de altas.
- **5. Por qué existe:** Guardar en UTC evita confusiones de zona horaria; se muestra en hora Colombia.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Oracle (DEFAULT). INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT en las vistas VW_BI_CIUDADANOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-09-10 14:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (momento (UTC) en que se creó la cuenta). Guardar en UTC evita confusiones de zona horaria; se muestra en hora Colombia.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CIUDADANOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_REGISTRO_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_REGISTRO_UTC) AS primera, MAX(FECHA_REGISTRO_UTC) AS ultima, COUNT(FECHA_REGISTRO_UTC) AS con_valor FROM USUARIO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. FECHA_REGISTRO_UTC es la casilla que guarda: Momento (UTC) en que se creó la cuenta. Nunca puede quedar vacía.
- **Explicación ampliada:** Guarda el momento exacto en que se creó la cuenta. Se guarda en UTC (hora universal) para que no haya errores con los cambios de zona horaria; las vistas la convierten a hora de Colombia (UTC−5) cuando se muestran. Sirve para reportes como «cuántas personas se registraron por mes». Oracle la llena sola con un DEFAULT, por eso nunca queda vacía. Si el profesor pregunta por «FECHA_REGISTRO», es esta columna.

#### 23. `USUARIO.ACTIVO`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** ACTIVO
- **3. Qué significa:** 1 si la cuenta puede usarse, 0 si está desactivada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: 1 si la cuenta puede usarse, 0 si está desactivada. Se usa en: tiene_rol() rechaza cuentas inactivas.
- **5. Por qué existe:** Desactivar sin borrar: se conserva la historia de depósitos.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 1).
- **8. Valor por defecto:** 1
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_USUARIO_ACTIVO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT en las vistas VW_BI_CIUDADANOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_USUARIO_ACTIVO: activo IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_USUARIO_ACTIVO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (1 si la cuenta puede usarse, 0 si está desactivada). Desactivar sin borrar: se conserva la historia de depósitos.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_CONTEXTO_APP, TRG_DEPOSITO_COHERENCIA, VW_BI_CIUDADANOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_ACTIVO y por qué esos y no otros? · ¿Por qué ACTIVO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ACTIVO, COUNT(*) AS filas FROM USUARIO GROUP BY ACTIVO ORDER BY ACTIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. ACTIVO es la casilla que guarda: 1 si la cuenta puede usarse, 0 si está desactivada. Nunca puede quedar vacía.

#### 24. `USUARIO.VERSION_CONSENTIMIENTO`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** VERSION_CONSENTIMIENTO
- **3. Qué significa:** Versión del aviso de tratamiento de datos que aceptó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: versión del aviso de tratamiento de datos que aceptó. Se usa en: Cumplimiento de protección de datos.
- **5. Por qué existe:** Saber qué texto aceptó la persona (Ley 1581 de 2012).
- **6. Tipo de dato:** VARCHAR2(20 CHAR) (hasta 20 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = aún no aceptó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_USUARIO_CONSENTIMIENTO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Registro. INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** v1-2026
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_USUARIO_CONSENTIMIENTO (condición completa en el anexo de CHECK) (ORA-02290) · Un texto de más de 20 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_USUARIO_CONSENTIMIENTO
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (versión del aviso de tratamiento de datos que aceptó). Saber qué texto aceptó la persona (Ley 1581 de 2012).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_CONSENTIMIENTO y por qué esos y no otros? · ¿Qué significa que VERSION_CONSENTIMIENTO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT VERSION_CONSENTIMIENTO, COUNT(*) AS filas FROM USUARIO GROUP BY VERSION_CONSENTIMIENTO ORDER BY VERSION_CONSENTIMIENTO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. VERSION_CONSENTIMIENTO es la casilla que guarda: Versión del aviso de tratamiento de datos que aceptó. Puede quedar vacía: NULL = aún no aceptó.

#### 25. `USUARIO.FECHA_CONSENTIMIENTO_UTC`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** FECHA_CONSENTIMIENTO_UTC
- **3. Qué significa:** Cuándo aceptó ese aviso.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: cuándo aceptó ese aviso. Se usa en: Cumplimiento.
- **5. Por qué existe:** CHECK: versión y fecha van juntas o no van.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL junto con la versión.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_USUARIO_CONSENTIMIENTO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Registro. INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-09-10 14:00
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_USUARIO_CONSENTIMIENTO (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_USUARIO_CONSENTIMIENTO
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (cuándo aceptó ese aviso). CHECK: versión y fecha van juntas o no van.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_CONSENTIMIENTO y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_CONSENTIMIENTO_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_CONSENTIMIENTO_UTC) AS primera, MAX(FECHA_CONSENTIMIENTO_UTC) AS ultima, COUNT(FECHA_CONSENTIMIENTO_UTC) AS con_valor FROM USUARIO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. FECHA_CONSENTIMIENTO_UTC es la casilla que guarda: Cuándo aceptó ese aviso. Puede quedar vacía: NULL junto con la versión.

#### 26. `USUARIO.ES_SINTETICO`

- **1. Tabla:** USUARIO — Cada persona registrada. No guarda contraseñas.
- **2. Atributo:** ES_SINTETICO
- **3. Qué significa:** 1 si la cuenta es de datos ficticios de prueba.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO: 1 si la cuenta es de datos ficticios de prueba. Se usa en: Filtro «DATOS SINTÉTICOS» en vistas BI.
- **5. Por qué existe:** Impide confundir datos de prueba con personas reales.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_USUARIO_SINTETICO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro de personas; identifica al actor en todos los procesos
- **12. Quién lo llena:** Script 11. INSERT/UPDATE directo sobre USUARIO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador; cada persona ve sus propios datos. SELECT en las vistas VW_BI_CIUDADANOS, VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_USUARIO_SINTETICO: es_sintetico IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); mezclar datos de demostración con datos del sistema. Reglas CHECK: CK_USUARIO_SINTETICO
- **17. Qué pasaría si no existiera:** La tabla USUARIO no podría guardar este dato (1 si la cuenta es de datos ficticios de prueba). Impide confundir datos de prueba con personas reales.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CIUDADANOS, VW_DEPOSITO_DETALLE, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (20 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se crea al registrarse con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_USUARIO_SINTETICO y por qué esos y no otros? · ¿Por qué ES_SINTETICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ES_SINTETICO, COUNT(*) AS filas FROM USUARIO GROUP BY ES_SINTETICO ORDER BY ES_SINTETICO;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO como una hoja de cálculo donde cada fila es una persona registrada. ES_SINTETICO es la casilla que guarda: 1 si la cuenta es de datos ficticios de prueba. Nunca puede quedar vacía.
- **Explicación ampliada:** Es una «etiqueta de prueba». Vale 1 cuando la fila fue creada por el script de datos de demostración (11_datos_prueba.sql) y 0 cuando es un dato del sistema. Así nunca se confunde una persona inventada para la demo con una persona real. Power BI la usa para mostrar el aviso DATOS SINTÉTICOS. Las tablas de operación tienen la misma bandera (INTERACCION, CONSULTA_VISION, PUNTO_INSTALACION, REGLA_ECOPUNTOS.ES_SINTETICA).

### USUARIO_ROL (3 atributos)

Relación muchos a muchos: una persona puede tener varios roles.

#### 27. `USUARIO_ROL.ID_USUARIO`

- **1. Tabla:** USUARIO_ROL — Relación muchos a muchos: una persona puede tener varios roles.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Persona que tiene el rol.
- **4. Para qué sirve:** Sirve a la vez para identificar cada fila de USUARIO_ROL y para atarla a su fila de USUARIO (relación 1:1 o tabla puente). Se usa en: Control de permisos.
- **5. Por qué existe:** Parte de la relación N:M usuario-rol.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** PK; FK: FK_USUARIO_ROL_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_USUARIO_ROL_USUARIO) · Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Control de acceso por rol
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre USUARIO_ROL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y la lógica de permisos. SELECT en las vistas VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir; registros huérfanos (no puede apuntar a un(a) USUARIO que no existe). Identidad: cada fila se distingue de las demás · Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de USUARIO_ROL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_CONTEXTO_APP, VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (14 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide qué comandos acepta el bot.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_USUARIO es un número subrogado y no un dato del negocio que podría cambiar? · ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291)
- **23. SELECT para verlo:** SELECT * FROM USUARIO_ROL WHERE ID_USUARIO = (SELECT MIN(ID_USUARIO) FROM USUARIO_ROL);
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO_ROL como una hoja de cálculo donde cada fila es la asignación de un rol a una persona. ID_USUARIO es la casilla que guarda: Persona que tiene el rol. Funciona como la «cédula» de la fila: no se repite y no cambia. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 28. `USUARIO_ROL.ID_ROL`

- **1. Tabla:** USUARIO_ROL — Relación muchos a muchos: una persona puede tener varios roles.
- **2. Atributo:** ID_ROL
- **3. Qué significa:** Rol que tiene esa persona.
- **4. Para qué sirve:** Sirve a la vez para identificar cada fila de USUARIO_ROL y para atarla a su fila de ROL (relación 1:1 o tabla puente). Se usa en: Control de permisos.
- **5. Por qué existe:** Una persona puede tener varios roles (operario que también es ciudadano).
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** PK; FK: FK_USUARIO_ROL_ROL->ROL
- **10. Con qué tabla se relaciona:** Apunta a ROL (FK FK_USUARIO_ROL_ROL) · Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Control de acceso por rol
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre USUARIO_ROL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y la lógica de permisos. SELECT en las vistas VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un número que no exista en ROL (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir; registros huérfanos (no puede apuntar a un(a) ROL que no existe). Identidad: cada fila se distingue de las demás · Integridad referencial con ROL
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de USUARIO_ROL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_CONTEXTO_APP, VW_BI_CIUDADANOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (14 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide qué comandos acepta el bot.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CIUDADANOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_ROL es un número subrogado y no un dato del negocio que podría cambiar? · ¿Qué pasa si intento guardar un ID_ROL que no existe en ROL? (respuesta: ORA-02291)
- **23. SELECT para verlo:** SELECT * FROM USUARIO_ROL WHERE ID_ROL = (SELECT MIN(ID_ROL) FROM USUARIO_ROL);
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO_ROL como una hoja de cálculo donde cada fila es la asignación de un rol a una persona. ID_ROL es la casilla que guarda: Rol que tiene esa persona. Funciona como la «cédula» de la fila: no se repite y no cambia. Es un «puntero»: copia el número de una fila de ROL para unir las dos tablas. Nunca puede quedar vacía.

#### 29. `USUARIO_ROL.FECHA_ASIGNACION_UTC`

- **1. Tabla:** USUARIO_ROL — Relación muchos a muchos: una persona puede tener varios roles.
- **2. Atributo:** FECHA_ASIGNACION_UTC
- **3. Qué significa:** Cuándo se asignó el rol.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de USUARIO_ROL: cuándo se asignó el rol. Se usa en: Consulta administrativa.
- **5. Por qué existe:** Metadato técnico, igual que en P3.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Control de acceso por rol
- **12. Quién lo llena:** Oracle (DEFAULT). INSERT/UPDATE directo sobre USUARIO_ROL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y la lógica de permisos. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-08-01 10:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla USUARIO_ROL no podría guardar este dato (cuándo se asignó el rol). Metadato técnico, igual que en P3.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (14 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide qué comandos acepta el bot.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_ASIGNACION_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_ASIGNACION_UTC) AS primera, MAX(FECHA_ASIGNACION_UTC) AS ultima, COUNT(FECHA_ASIGNACION_UTC) AS con_valor FROM USUARIO_ROL;
- **24. Explicación sencilla (para quien empieza):** Piensa en USUARIO_ROL como una hoja de cálculo donde cada fila es la asignación de un rol a una persona. FECHA_ASIGNACION_UTC es la casilla que guarda: Cuándo se asignó el rol. Nunca puede quedar vacía.

### CREDENCIAL_ACCESO (7 atributos)

Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).

#### 30. `CREDENCIAL_ACCESO.ID_USUARIO`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Cuenta dueña de la contraseña (relación 1:1 con USUARIO).
- **4. Para qué sirve:** Sirve a la vez para identificar cada fila de CREDENCIAL_ACCESO y para atarla a su fila de USUARIO (relación 1:1 o tabla puente). Se usa en: Inicio de sesión de la app de escritorio.
- **5. Por qué existe:** Separar la contraseña de los datos personales: nadie consulta USUARIO y ve hashes.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK y FK).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** PK; FK: FK_CREDENCIAL_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_CREDENCIAL_USUARIO) · Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Administrador / registro. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir; registros huérfanos (no puede apuntar a un(a) USUARIO que no existe). Identidad: cada fila se distingue de las demás · Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de CREDENCIAL_ACCESO, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_USUARIO es un número subrogado y no un dato del negocio que podría cambiar? · ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291)
- **23. SELECT para verlo:** SELECT * FROM CREDENCIAL_ACCESO WHERE ID_USUARIO = (SELECT MIN(ID_USUARIO) FROM CREDENCIAL_ACCESO);
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. ID_USUARIO es la casilla que guarda: Cuenta dueña de la contraseña (relación 1:1 con USUARIO). Funciona como la «cédula» de la fila: no se repite y no cambia. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 31. `CREDENCIAL_ACCESO.HASH_CONTRASENA`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** HASH_CONTRASENA
- **3. Qué significa:** Resultado PBKDF2-SHA256 (32 bytes) de la contraseña; nunca la contraseña.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: resultado PBKDF2-SHA256 (32 bytes) de la contraseña; nunca la contraseña. Se usa en: Verificar la contraseña al iniciar sesión.
- **5. Por qué existe:** Aunque roben la tabla, no se conocen las contraseñas.
- **6. Tipo de dato:** RAW(32) (32 bytes binarios)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CREDENCIAL_HASH
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** (32 bytes binarios)
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CREDENCIAL_HASH: LENGTH(RAWTOHEX(hash_contrasena)) = 64 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); guardar la contraseña en texto legible. Reglas CHECK: CK_CREDENCIAL_HASH
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (resultado PBKDF2-SHA256 (32 bytes) de la contraseña; nunca la contraseña). Aunque roben la tabla, no se conocen las contraseñas.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CREDENCIAL_HASH y por qué esos y no otros? · ¿Por qué HASH_CONTRASENA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(HASH_CONTRASENA) AS con_valor FROM CREDENCIAL_ACCESO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. HASH_CONTRASENA es la casilla que guarda: Resultado PBKDF2-SHA256 (32 bytes) de la contraseña; nunca la contraseña. Nunca puede quedar vacía.

#### 32. `CREDENCIAL_ACCESO.SAL`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** SAL
- **3. Qué significa:** 16 bytes aleatorios propios de cada contraseña.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: 16 bytes aleatorios propios de cada contraseña. Se usa en: Cálculo del hash.
- **5. Por qué existe:** Dos personas con la misma contraseña tienen hashes distintos.
- **6. Tipo de dato:** RAW(16) (16 bytes binarios)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CREDENCIAL_SAL
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** (16 bytes)
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CREDENCIAL_SAL: LENGTH(RAWTOHEX(sal)) = 32 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); guardar la contraseña en texto legible. Reglas CHECK: CK_CREDENCIAL_SAL
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (16 bytes aleatorios propios de cada contraseña). Dos personas con la misma contraseña tienen hashes distintos.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CREDENCIAL_SAL y por qué esos y no otros? · ¿Por qué SAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(SAL) AS con_valor FROM CREDENCIAL_ACCESO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. SAL es la casilla que guarda: 16 bytes aleatorios propios de cada contraseña. Nunca puede quedar vacía.

#### 33. `CREDENCIAL_ACCESO.ITERACIONES`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** ITERACIONES
- **3. Qué significa:** Veces que se repitió el cálculo PBKDF2.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: veces que se repitió el cálculo PBKDF2. Se usa en: Cálculo del hash.
- **5. Por qué existe:** CHECK >= 100000: hace lento adivinar contraseñas por fuerza bruta.
- **6. Tipo de dato:** NUMBER(7) (entero de hasta 7 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CREDENCIAL_ITER
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 210000
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CREDENCIAL_ITER: iteraciones >= 100000 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CREDENCIAL_ITER
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (veces que se repitió el cálculo PBKDF2). CHECK >= 100000: hace lento adivinar contraseñas por fuerza bruta.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CREDENCIAL_ITER y por qué esos y no otros? · ¿Por qué ITERACIONES es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ITERACIONES, COUNT(*) AS filas FROM CREDENCIAL_ACCESO GROUP BY ITERACIONES ORDER BY ITERACIONES;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. ITERACIONES es la casilla que guarda: Veces que se repitió el cálculo PBKDF2. Nunca puede quedar vacía.

#### 34. `CREDENCIAL_ACCESO.INTENTOS_FALLIDOS`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** INTENTOS_FALLIDOS
- **3. Qué significa:** Errores seguidos al iniciar sesión.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: errores seguidos al iniciar sesión. Se usa en: Bloqueo temporal.
- **5. Por qué existe:** Frena ataques de adivinación.
- **6. Tipo de dato:** NUMBER(3) (entero de hasta 3 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CREDENCIAL_INTENTOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 0
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CREDENCIAL_INTENTOS: intentos_fallidos BETWEEN 0 AND 100 (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CREDENCIAL_INTENTOS
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (errores seguidos al iniciar sesión). Frena ataques de adivinación.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CREDENCIAL_INTENTOS y por qué esos y no otros? · ¿Por qué INTENTOS_FALLIDOS es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT INTENTOS_FALLIDOS, COUNT(*) AS filas FROM CREDENCIAL_ACCESO GROUP BY INTENTOS_FALLIDOS ORDER BY INTENTOS_FALLIDOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. INTENTOS_FALLIDOS es la casilla que guarda: Errores seguidos al iniciar sesión. Nunca puede quedar vacía.

#### 35. `CREDENCIAL_ACCESO.BLOQUEADA_HASTA_UTC`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** BLOQUEADA_HASTA_UTC
- **3. Qué significa:** Hasta cuándo no se permite entrar.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: hasta cuándo no se permite entrar. Se usa en: Inicio de sesión.
- **5. Por qué existe:** Bloqueo temporal sin borrar la cuenta.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = no está bloqueada.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-10 15:30
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (hasta cuándo no se permite entrar). Bloqueo temporal sin borrar la cuenta.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que BLOQUEADA_HASTA_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(BLOQUEADA_HASTA_UTC) AS primera, MAX(BLOQUEADA_HASTA_UTC) AS ultima, COUNT(BLOQUEADA_HASTA_UTC) AS con_valor FROM CREDENCIAL_ACCESO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. BLOQUEADA_HASTA_UTC es la casilla que guarda: Hasta cuándo no se permite entrar. Puede quedar vacía: NULL = no está bloqueada.

#### 36. `CREDENCIAL_ACCESO.FECHA_CAMBIO_UTC`

- **1. Tabla:** CREDENCIAL_ACCESO — Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).
- **2. Atributo:** FECHA_CAMBIO_UTC
- **3. Qué significa:** Último cambio de contraseña.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CREDENCIAL_ACCESO: último cambio de contraseña. Se usa en: Política de cambio.
- **5. Por qué existe:** Auditar renovaciones de contraseña.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Inicio de sesión de la aplicación de escritorio
- **12. Quién lo llena:** Servicio C#. INSERT/UPDATE directo sobre CREDENCIAL_ACCESO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Solo el proceso de inicio de sesión (nadie la lee en claro). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-10-01 09:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla CREDENCIAL_ACCESO no podría guardar este dato (último cambio de contraseña). Auditar renovaciones de contraseña.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (7 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa: el bot no pide contraseña.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_CAMBIO_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_CAMBIO_UTC) AS primera, MAX(FECHA_CAMBIO_UTC) AS ultima, COUNT(FECHA_CAMBIO_UTC) AS con_valor FROM CREDENCIAL_ACCESO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CREDENCIAL_ACCESO como una hoja de cálculo donde cada fila es la contraseña (en hash) de una persona. FECHA_CAMBIO_UTC es la casilla que guarda: Último cambio de contraseña. Nunca puede quedar vacía.

### VINCULO_TELEGRAM (5 atributos)

Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.

#### 37. `VINCULO_TELEGRAM.ID_VINCULO`

- **1. Tabla:** VINCULO_TELEGRAM — Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.
- **2. Atributo:** ID_VINCULO
- **3. Qué significa:** Número del vínculo.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de VINCULO_TELEGRAM, de modo que otras tablas puedan apuntarle. Se usa en: Historial de vínculos.
- **5. Por qué existe:** Permite guardar varios vínculos (bajas) de la misma persona.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Reconocer a la persona cuando escribe al bot
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot de Telegram. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 5
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de VINCULO_TELEGRAM, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (5 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Identifica el chat de la persona en cada mensaje.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_VINCULO es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM VINCULO_TELEGRAM WHERE ID_VINCULO = (SELECT MIN(ID_VINCULO) FROM VINCULO_TELEGRAM);
- **24. Explicación sencilla (para quien empieza):** Piensa en VINCULO_TELEGRAM como una hoja de cálculo donde cada fila es la unión de una cuenta con un chat de Telegram. ID_VINCULO es la casilla que guarda: Número del vínculo. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 38. `VINCULO_TELEGRAM.ID_USUARIO`

- **1. Tabla:** VINCULO_TELEGRAM — Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Cuenta EcoAccess vinculada.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de VINCULO_TELEGRAM y poder unirlas con JOIN. Se usa en: Identificar al ciudadano que escribe en Telegram.
- **5. Por qué existe:** Une el chat con la persona.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_VINCULO_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_VINCULO_USUARIO)
- **11. Qué proceso lo usa:** Reconocer a la persona cuando escribe al bot
- **12. Quién lo llena:** Bot Eco al registrar. INSERT/UPDATE directo sobre VINCULO_TELEGRAM (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot de Telegram. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 17
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre VINCULO_TELEGRAM y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (5 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Identifica el chat de la persona en cada mensaje.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291) · ¿Por qué ID_USUARIO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_USUARIO, COUNT(*) AS filas FROM VINCULO_TELEGRAM t JOIN USUARIO r ON r.ID_USUARIO = t.ID_USUARIO GROUP BY t.ID_USUARIO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en VINCULO_TELEGRAM como una hoja de cálculo donde cada fila es la unión de una cuenta con un chat de Telegram. ID_USUARIO es la casilla que guarda: Cuenta EcoAccess vinculada. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 39. `VINCULO_TELEGRAM.TELEGRAM_USER_ID`

- **1. Tabla:** VINCULO_TELEGRAM — Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.
- **2. Atributo:** TELEGRAM_USER_ID
- **3. Qué significa:** Número de la cuenta de Telegram (from.id).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de VINCULO_TELEGRAM: número de la cuenta de Telegram (from.id). Se usa en: Reconocer a quien envía la foto.
- **5. Por qué existe:** Índice único: una cuenta de Telegram activa no puede pertenecer a dos personas.
- **6. Tipo de dato:** NUMBER(19) (entero de hasta 19 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_VINCULO_TELEGRAM_ID
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Reconocer a la persona cuando escribe al bot
- **12. Quién lo llena:** Bot Eco. INSERT/UPDATE directo sobre VINCULO_TELEGRAM (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot de Telegram. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 900000017
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_VINCULO_TELEGRAM_ID: telegram_user_id > 0 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_VINCULO_TELEGRAM_ID
- **17. Qué pasaría si no existiera:** La tabla VINCULO_TELEGRAM no podría guardar este dato (número de la cuenta de Telegram (from.id)). Índice único: una cuenta de Telegram activa no puede pertenecer a dos personas.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (5 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Identifica el chat de la persona en cada mensaje.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_VINCULO_TELEGRAM_ID y por qué esos y no otros? · ¿Por qué TELEGRAM_USER_ID es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT TELEGRAM_USER_ID, COUNT(*) AS filas FROM VINCULO_TELEGRAM GROUP BY TELEGRAM_USER_ID ORDER BY TELEGRAM_USER_ID;
- **24. Explicación sencilla (para quien empieza):** Piensa en VINCULO_TELEGRAM como una hoja de cálculo donde cada fila es la unión de una cuenta con un chat de Telegram. TELEGRAM_USER_ID es la casilla que guarda: Número de la cuenta de Telegram (from.id). Nunca puede quedar vacía.

#### 40. `VINCULO_TELEGRAM.FECHA_VINCULO_UTC`

- **1. Tabla:** VINCULO_TELEGRAM — Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.
- **2. Atributo:** FECHA_VINCULO_UTC
- **3. Qué significa:** Cuándo se vinculó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de VINCULO_TELEGRAM: cuándo se vinculó. Se usa en: Trazabilidad.
- **5. Por qué existe:** Saber desde cuándo vale el vínculo.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_VINCULO_BAJA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Reconocer a la persona cuando escribe al bot
- **12. Quién lo llena:** Bot Eco. INSERT/UPDATE directo sobre VINCULO_TELEGRAM (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot de Telegram. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-09-26 08:00
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_VINCULO_BAJA: fecha_baja_utc IS NULL OR fecha_baja_utc >= fecha_vinculo_utc (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_VINCULO_BAJA
- **17. Qué pasaría si no existiera:** La tabla VINCULO_TELEGRAM no podría guardar este dato (cuándo se vinculó). Saber desde cuándo vale el vínculo.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (5 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Identifica el chat de la persona en cada mensaje.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_VINCULO_BAJA y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_VINCULO_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_VINCULO_UTC) AS primera, MAX(FECHA_VINCULO_UTC) AS ultima, COUNT(FECHA_VINCULO_UTC) AS con_valor FROM VINCULO_TELEGRAM;
- **24. Explicación sencilla (para quien empieza):** Piensa en VINCULO_TELEGRAM como una hoja de cálculo donde cada fila es la unión de una cuenta con un chat de Telegram. FECHA_VINCULO_UTC es la casilla que guarda: Cuándo se vinculó. Nunca puede quedar vacía.

#### 41. `VINCULO_TELEGRAM.FECHA_BAJA_UTC`

- **1. Tabla:** VINCULO_TELEGRAM — Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.
- **2. Atributo:** FECHA_BAJA_UTC
- **3. Qué significa:** Cuándo se desvinculó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de VINCULO_TELEGRAM: cuándo se desvinculó. Se usa en: Vínculo activo = fecha NULL.
- **5. Por qué existe:** Se conserva la historia en vez de borrar.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = vínculo vigente.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_VINCULO_BAJA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Reconocer a la persona cuando escribe al bot
- **12. Quién lo llena:** Bot / administrador. INSERT/UPDATE directo sobre VINCULO_TELEGRAM (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** El bot de Telegram. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-01 08:00
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_VINCULO_BAJA: fecha_baja_utc IS NULL OR fecha_baja_utc >= fecha_vinculo_utc (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_VINCULO_BAJA
- **17. Qué pasaría si no existiera:** La tabla VINCULO_TELEGRAM no podría guardar este dato (cuándo se desvinculó). Se conserva la historia en vez de borrar.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (5 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Identifica el chat de la persona en cada mensaje.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_VINCULO_BAJA y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_BAJA_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_BAJA_UTC) AS primera, MAX(FECHA_BAJA_UTC) AS ultima, COUNT(FECHA_BAJA_UTC) AS con_valor FROM VINCULO_TELEGRAM;
- **24. Explicación sencilla (para quien empieza):** Piensa en VINCULO_TELEGRAM como una hoja de cálculo donde cada fila es la unión de una cuenta con un chat de Telegram. FECHA_BAJA_UTC es la casilla que guarda: Cuándo se desvinculó. Puede quedar vacía: NULL = vínculo vigente.

## Módulo: M3 · Infraestructura y QR

### PUNTO_INSTALACION (6 atributos)

Estación física con un QR compartido para sus tres canecas.

#### 42. `PUNTO_INSTALACION.ID_PUNTO_INSTALACION`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** ID_PUNTO_INSTALACION
- **3. Qué significa:** Número de la estación.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de PUNTO_INSTALACION, de modo que otras tablas puedan apuntarle. Se usa en: FK de CONTENEDOR e INTERACCION.
- **5. Por qué existe:** Distinguir la estación (lugar) de la caneca (recipiente).
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY BY DEFAULT
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY BY DEFAULT
- **10. Con qué tabla se relaciona:** Es referenciada por: CONTENEDOR, INTERACCION
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Catálogo. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de PUNTO_INSTALACION, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_PUNTO_INSTALACION es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM PUNTO_INSTALACION WHERE ID_PUNTO_INSTALACION = (SELECT MIN(ID_PUNTO_INSTALACION) FROM PUNTO_INSTALACION);
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. ID_PUNTO_INSTALACION es la casilla que guarda: Número de la estación. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 43. `PUNTO_INSTALACION.NOMBRE`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** NOMBRE
- **3. Qué significa:** Nombre único de la estación.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PUNTO_INSTALACION: nombre único de la estación. Se usa en: Pantallas y reportes.
- **5. Por qué existe:** UNIQUE evita dos estaciones con el mismo nombre.
- **6. Tipo de dato:** VARCHAR2(100 CHAR) (hasta 100 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_PUNTO_NOMBRE; CHECK CK_PUNTO_NOMBRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PUNTO_INSTALACION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** Estación UPC Sabanas
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PUNTO_NOMBRE: TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2 (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_PUNTO_NOMBRE) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PUNTO_NOMBRE · Unicidad: UQ_PUNTO_NOMBRE
- **17. Qué pasaría si no existiera:** La tabla PUNTO_INSTALACION no podría guardar este dato (nombre único de la estación). UNIQUE evita dos estaciones con el mismo nombre.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PUNTO_NOMBRE y por qué esos y no otros? · ¿Por qué NOMBRE no puede repetirse? (UNIQUE UQ_PUNTO_NOMBRE) · ¿Por qué NOMBRE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT NOMBRE, COUNT(*) AS filas FROM PUNTO_INSTALACION GROUP BY NOMBRE ORDER BY NOMBRE;
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. NOMBRE es la casilla que guarda: Nombre único de la estación. Nunca puede quedar vacía.

#### 44. `PUNTO_INSTALACION.REFERENCIA_UBICACION`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** REFERENCIA_UBICACION
- **3. Qué significa:** Descripción del lugar.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PUNTO_INSTALACION: descripción del lugar. Se usa en: Orientar al ciudadano.
- **5. Por qué existe:** Reemplaza las tablas COMUNA y BARRIO de V3 que P3 no usa.
- **6. Tipo de dato:** VARCHAR2(200 CHAR) (hasta 200 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = sin descripción.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PUNTO_INSTALACION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Pasillo principal, primer piso
- **15. Ejemplo de valor incorrecto:** Un texto de más de 200 caracteres (ORA-12899)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla PUNTO_INSTALACION no podría guardar este dato (descripción del lugar). Reemplaza las tablas COMUNA y BARRIO de V3 que P3 no usa.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que REFERENCIA_UBICACION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(REFERENCIA_UBICACION) AS con_valor FROM PUNTO_INSTALACION;
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. REFERENCIA_UBICACION es la casilla que guarda: Descripción del lugar. Puede quedar vacía: NULL = sin descripción.

#### 45. `PUNTO_INSTALACION.CODIGO_QR_ESTACION`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** CODIGO_QR_ESTACION
- **3. Qué significa:** Texto del QR compartido por las tres canecas de la estación (parámetro start de Telegram).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PUNTO_INSTALACION: texto del QR compartido por las tres canecas de la estación (parámetro start de Telegram). Se usa en: Inicio de sesión QR en el bot.
- **5. Por qué existe:** Un solo QR para tres canecas; la caneca la decide el material.
- **6. Tipo de dato:** VARCHAR2(64 CHAR) (hasta 64 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = estación sin QR de estación (modelo legado).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_PUNTO_QR_ESTACION; CHECK CK_PUNTO_QR_ESTACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PUNTO_INSTALACION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_CANECA_ESTACION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** EcoAccessEstacionUPC01
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_PUNTO_QR_ESTACION (condición completa en el anexo de CHECK) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_PUNTO_QR_ESTACION) · Un texto de más de 64 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas. Reglas CHECK: CK_PUNTO_QR_ESTACION · Unicidad: UQ_PUNTO_QR_ESTACION
- **17. Qué pasaría si no existiera:** La tabla PUNTO_INSTALACION no podría guardar este dato (texto del QR compartido por las tres canecas de la estación (parámetro start de Telegram)). Un solo QR para tres canecas; la caneca la decide el material.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_CANECA_ESTACION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PUNTO_QR_ESTACION y por qué esos y no otros? · ¿Por qué CODIGO_QR_ESTACION no puede repetirse? (UNIQUE UQ_PUNTO_QR_ESTACION) · ¿Qué significa que CODIGO_QR_ESTACION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT CODIGO_QR_ESTACION, COUNT(*) AS filas FROM PUNTO_INSTALACION GROUP BY CODIGO_QR_ESTACION ORDER BY CODIGO_QR_ESTACION;
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. CODIGO_QR_ESTACION es la casilla que guarda: Texto del QR compartido por las tres canecas de la estación (parámetro start de Telegram). Puede quedar vacía: NULL = estación sin QR de estación (modelo legado).
- **Explicación ampliada:** Es el texto que lleva el código QR pegado en la estación. Cuando la persona lo escanea, Telegram abre el bot con ese texto y así el sistema sabe en qué estación está. Hay un solo QR para las tres canecas de la estación: la caneca correcta la elige el sistema según el material. Es UNIQUE: dos estaciones no pueden tener el mismo QR, porque el sistema no sabría a cuál abrirle la caneca.

#### 46. `PUNTO_INSTALACION.ACTIVO`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** ACTIVO
- **3. Qué significa:** 1 si la estación funciona.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PUNTO_INSTALACION: 1 si la estación funciona. Se usa en: fn_resolver_caneca_disponible.
- **5. Por qué existe:** Desactivar una estación sin borrarla.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 1
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PUNTO_ACTIVO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre PUNTO_INSTALACION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PUNTO_ACTIVO: activo IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_PUNTO_ACTIVO
- **17. Qué pasaría si no existiera:** La tabla PUNTO_INSTALACION no podría guardar este dato (1 si la estación funciona). Desactivar una estación sin borrarla.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PUNTO_ACTIVO y por qué esos y no otros? · ¿Por qué ACTIVO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ACTIVO, COUNT(*) AS filas FROM PUNTO_INSTALACION GROUP BY ACTIVO ORDER BY ACTIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. ACTIVO es la casilla que guarda: 1 si la estación funciona. Nunca puede quedar vacía.

#### 47. `PUNTO_INSTALACION.ES_SINTETICO`

- **1. Tabla:** PUNTO_INSTALACION — Estación física con un QR compartido para sus tres canecas.
- **2. Atributo:** ES_SINTETICO
- **3. Qué significa:** 1 si la estación es de simulación.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de PUNTO_INSTALACION: 1 si la estación es de simulación. Se usa en: Reportes.
- **5. Por qué existe:** Separar simulación de instalación real.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_PUNTO_SINTETICO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Lectura del QR de la estación e inicio de la sesión
- **12. Quién lo llena:** Script 10. INSERT/UPDATE directo sobre PUNTO_INSTALACION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_PUNTO_SINTETICO: es_sintetico IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); mezclar datos de demostración con datos del sistema. Reglas CHECK: CK_PUNTO_SINTETICO
- **17. Qué pasaría si no existiera:** La tabla PUNTO_INSTALACION no podría guardar este dato (1 si la estación es de simulación). Separar simulación de instalación real.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (8 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El QR de la estación llega al bot con /start.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_PUNTO_SINTETICO y por qué esos y no otros? · ¿Por qué ES_SINTETICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ES_SINTETICO, COUNT(*) AS filas FROM PUNTO_INSTALACION GROUP BY ES_SINTETICO ORDER BY ES_SINTETICO;
- **24. Explicación sencilla (para quien empieza):** Piensa en PUNTO_INSTALACION como una hoja de cálculo donde cada fila es una estación de reciclaje. ES_SINTETICO es la casilla que guarda: 1 si la estación es de simulación. Nunca puede quedar vacía.

### CONTENEDOR (8 atributos)

Caneca de un color (blanca, verde o negra) dentro de una estación.

#### 48. `CONTENEDOR.ID_CONTENEDOR`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Número de la caneca.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de CONTENEDOR, de modo que otras tablas puedan apuntarle. Se usa en: FK de órdenes, eventos y CONTENEDOR_MATERIAL.
- **5. Por qué existe:** Identificar cada recipiente físico.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY BY DEFAULT
- **9. ¿Es PK o FK? (restricciones):** PK; UNIQUE: UQ_CONTENEDOR_CATEGORIA, UQ_CONTENEDOR_PUNTO; IDENTITY BY DEFAULT
- **10. Con qué tabla se relaciona:** Es referenciada por: CONSULTA_VISION, CONTENEDOR_MATERIAL, EVENTO_DISPOSITIVO, HISTORIAL_ESTADO_CONTENEDOR, INTERACCION, ORDEN_APERTURA
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Catálogo. INSERT (Oracle asigna el número; no se actualiza) · INSERT/UPDATE a través de: PR_CAMBIAR_ESTADO_CONTENEDOR · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 12
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de CONTENEDOR, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, PKG_ESTADISTICAS, PR_CAMBIAR_ESTADO_CONTENEDOR, TRG_CONTENEDOR_AUD_ESTADO, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_CONTENEDOR es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM CONTENEDOR WHERE ID_CONTENEDOR = (SELECT MIN(ID_CONTENEDOR) FROM CONTENEDOR);
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. ID_CONTENEDOR es la casilla que guarda: Número de la caneca. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.
- **Explicación ampliada:** Es el número de cada caneca física. Con él se sabe a qué caneca se le dio la orden de abrir (ORDEN_APERTURA), qué caneca informó el evento (EVENTO_DISPOSITIVO), qué materiales recibe (CONTENEDOR_MATERIAL) y cuál cambió de estado (HISTORIAL_ESTADO_CONTENEDOR). Es la clave primaria de CONTENEDOR.

#### 49. `CONTENEDOR.ID_PUNTO_INSTALACION`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** ID_PUNTO_INSTALACION
- **3. Qué significa:** Estación donde está la caneca.
- **4. Para qué sirve:** Sirve para saber a qué fila de PUNTO_INSTALACION pertenece cada fila de CONTENEDOR y poder unirlas con JOIN. Se usa en: Agrupar canecas por estación.
- **5. Por qué existe:** Relación 1:N estación-canecas.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONTENEDOR_PUNTO->PUNTO_INSTALACION; UNIQUE: UQ_CONTENEDOR_PUNTO
- **10. Con qué tabla se relaciona:** Apunta a PUNTO_INSTALACION (FK FK_CONTENEDOR_PUNTO)
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en PUNTO_INSTALACION (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_CONTENEDOR_PUNTO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) PUNTO_INSTALACION que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con PUNTO_INSTALACION · Unicidad: UQ_CONTENEDOR_PUNTO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONTENEDOR y PUNTO_INSTALACION: no sabríamos a qué PUNTO_INSTALACION pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, PKG_ESTADISTICAS, VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_PUNTO_INSTALACION que no existe en PUNTO_INSTALACION? (respuesta: ORA-02291) · ¿Por qué ID_PUNTO_INSTALACION no puede repetirse? (UNIQUE UQ_CONTENEDOR_PUNTO) · ¿Por qué ID_PUNTO_INSTALACION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_PUNTO_INSTALACION, COUNT(*) AS filas FROM CONTENEDOR t JOIN PUNTO_INSTALACION r ON r.ID_PUNTO_INSTALACION = t.ID_PUNTO_INSTALACION GROUP BY t.ID_PUNTO_INSTALACION ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. ID_PUNTO_INSTALACION es la casilla que guarda: Estación donde está la caneca. Es un «puntero»: copia el número de una fila de PUNTO_INSTALACION para unir las dos tablas. Nunca puede quedar vacía.

#### 50. `CONTENEDOR.NOMBRE`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** NOMBRE
- **3. Qué significa:** Nombre de la caneca.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: nombre de la caneca. Se usa en: Pantallas.
- **5. Por qué existe:** Reconocerla en pantalla.
- **6. Tipo de dato:** VARCHAR2(100 CHAR) (hasta 100 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONTENEDOR_NOMBRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** UPC01 - Caneca verde
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONTENEDOR_NOMBRE: TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2 (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 100 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONTENEDOR_NOMBRE
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (nombre de la caneca). Reconocerla en pantalla.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** Nunca se expone: es dato personal o secreto.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONTENEDOR_NOMBRE y por qué esos y no otros? · ¿Por qué NOMBRE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT NOMBRE, COUNT(*) AS filas FROM CONTENEDOR GROUP BY NOMBRE ORDER BY NOMBRE;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. NOMBRE es la casilla que guarda: Nombre de la caneca. Nunca puede quedar vacía.

#### 51. `CONTENEDOR.CATEGORIA`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** CATEGORIA
- **3. Qué significa:** Color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables. Se usa en: FK compuesta en CONTENEDOR_MATERIAL; resolver caneca.
- **5. Por qué existe:** El color se DERIVA en vistas; no se guarda texto repetido.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_CONTENEDOR_CATEGORIA; CHECK CK_CONTENEDOR_CATEGORIA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONTENEDOR_CATEGORIA: categoria IN (1, 2, 3) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_CONTENEDOR_CATEGORIA) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONTENEDOR_CATEGORIA · Unicidad: UQ_CONTENEDOR_CATEGORIA · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables). El color se DERIVA en vistas; no se guarda texto repetido.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_CANECA_ESTACION, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONTENEDOR_CATEGORIA y por qué esos y no otros? · ¿Por qué CATEGORIA no puede repetirse? (UNIQUE UQ_CONTENEDOR_CATEGORIA) · ¿Por qué CATEGORIA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CATEGORIA, COUNT(*) AS filas FROM CONTENEDOR GROUP BY CATEGORIA ORDER BY CATEGORIA;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. CATEGORIA es la casilla que guarda: Color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables. Nunca puede quedar vacía.
- **Explicación ampliada:** Es el color de la caneca expresado como número: 1 blanca (aprovechables), 2 verde (orgánicos), 3 negra (no aprovechables). Va dentro de la FK compuesta de CONTENEDOR_MATERIAL, así Oracle impide que, por ejemplo, la caneca verde acepte plástico.

#### 52. `CONTENEDOR.IDENTIFICADOR_QR`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** IDENTIFICADOR_QR
- **3. Qué significa:** QR propio de la caneca (modelo legado de P3: un QR por caneca).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: qR propio de la caneca (modelo legado de P3: un QR por caneca). Se usa en: Compatibilidad con el QR piloto de P3.
- **5. Por qué existe:** No rompe el QR antiguo de P3.
- **6. Tipo de dato:** VARCHAR2(64 CHAR) (hasta 64 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = la caneca usa el QR de la estación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_CONTENEDOR_QR; CHECK CK_CONTENEDOR_QR
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_CANECA_ESTACION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** EcoAccessCanecaLegado01
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_CONTENEDOR_QR (condición completa en el anexo de CHECK) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_CONTENEDOR_QR) · Un texto de más de 64 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas. Reglas CHECK: CK_CONTENEDOR_QR · Unicidad: UQ_CONTENEDOR_QR
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (qR propio de la caneca (modelo legado de P3: un QR por caneca)). No rompe el QR antiguo de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_CANECA_ESTACION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONTENEDOR_QR y por qué esos y no otros? · ¿Por qué IDENTIFICADOR_QR no puede repetirse? (UNIQUE UQ_CONTENEDOR_QR) · ¿Qué significa que IDENTIFICADOR_QR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT IDENTIFICADOR_QR, COUNT(*) AS filas FROM CONTENEDOR GROUP BY IDENTIFICADOR_QR ORDER BY IDENTIFICADOR_QR;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. IDENTIFICADOR_QR es la casilla que guarda: QR propio de la caneca (modelo legado de P3: un QR por caneca). Puede quedar vacía: NULL = la caneca usa el QR de la estación.
- **Explicación ampliada:** Es el QR propio de una caneca en el modelo antiguo de Programación III, donde cada caneca tenía su QR. Hoy el QR principal es el de la estación (PUNTO_INSTALACION.CODIGO_QR_ESTACION); esta columna se conserva para no romper el piloto de P3. Si está vacía, la caneca usa el QR de su estación.

#### 53. `CONTENEDOR.IDENTIFICADOR_DISPOSITIVO`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** IDENTIFICADOR_DISPOSITIVO
- **3. Qué significa:** Nombre del ESP32 que abre la tapa.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: nombre del ESP32 que abre la tapa. Se usa en: Enviar la orden al dispositivo correcto.
- **5. Por qué existe:** UNIQUE: un ESP32 no puede controlar dos canecas.
- **6. Tipo de dato:** VARCHAR2(40 CHAR) (hasta 40 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = sin hardware instalado.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_CONTENEDOR_DISPOSITIVO; CHECK CK_CONTENEDOR_DISPOSITIVO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_CANECA_ESTACION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** esp32-upc01-verde
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_CONTENEDOR_DISPOSITIVO (condición completa en el anexo de CHECK) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_CONTENEDOR_DISPOSITIVO) · Un texto de más de 40 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas. Reglas CHECK: CK_CONTENEDOR_DISPOSITIVO · Unicidad: UQ_CONTENEDOR_DISPOSITIVO
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (nombre del ESP32 que abre la tapa). UNIQUE: un ESP32 no puede controlar dos canecas.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_CANECA_ESTACION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONTENEDOR_DISPOSITIVO y por qué esos y no otros? · ¿Por qué IDENTIFICADOR_DISPOSITIVO no puede repetirse? (UNIQUE UQ_CONTENEDOR_DISPOSITIVO) · ¿Qué significa que IDENTIFICADOR_DISPOSITIVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT IDENTIFICADOR_DISPOSITIVO, COUNT(*) AS filas FROM CONTENEDOR GROUP BY IDENTIFICADOR_DISPOSITIVO ORDER BY IDENTIFICADOR_DISPOSITIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. IDENTIFICADOR_DISPOSITIVO es la casilla que guarda: Nombre del ESP32 que abre la tapa. Puede quedar vacía: NULL = sin hardware instalado.

#### 54. `CONTENEDOR.FECHA_ALTA_UTC`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** FECHA_ALTA_UTC
- **3. Qué significa:** Cuándo se registró la caneca.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: cuándo se registró la caneca. Se usa en: Inventario.
- **5. Por qué existe:** Trazabilidad del inventario.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Oracle (DEFAULT). INSERT/UPDATE directo sobre CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-01 00:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (cuándo se registró la caneca). Trazabilidad del inventario.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_ALTA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_ALTA_UTC) AS primera, MAX(FECHA_ALTA_UTC) AS ultima, COUNT(FECHA_ALTA_UTC) AS con_valor FROM CONTENEDOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. FECHA_ALTA_UTC es la casilla que guarda: Cuándo se registró la caneca. Nunca puede quedar vacía.

#### 55. `CONTENEDOR.ESTADO`

- **1. Tabla:** CONTENEDOR — Caneca de un color (blanca, verde o negra) dentro de una estación.
- **2. Atributo:** ESTADO
- **3. Qué significa:** 1 En servicio, 2 Fuera de servicio, 3 Dada de baja.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONTENEDOR: 1 En servicio, 2 Fuera de servicio, 3 Dada de baja. Se usa en: Trigger 1 y resolver caneca.
- **5. Por qué existe:** Nace fuera de servicio para no recibir órdenes antes de configurarse.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 2).
- **8. Valor por defecto:** 2
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONTENEDOR_ESTADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Elegir, abrir y controlar la disponibilidad de la caneca
- **12. Quién lo llena:** Administrador (PR_CAMBIAR_ESTADO_CONTENEDOR). INSERT/UPDATE a través de: PR_CAMBIAR_ESTADO_CONTENEDOR · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (bot), operario y administrador. SELECT en las vistas VW_BI_DEPOSITOS, VW_CANECA_ESTACION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONTENEDOR_ESTADO: estado IN (1, 2, 3) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONTENEDOR_ESTADO · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla CONTENEDOR no podría guardar este dato (1 En servicio, 2 Fuera de servicio, 3 Dada de baja). Nace fuera de servicio para no recibir órdenes antes de configurarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, PR_CAMBIAR_ESTADO_CONTENEDOR, TRG_CONTENEDOR_AUD_ESTADO, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_CANECA_ESTACION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (18 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot dice el color de la caneca.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONTENEDOR_ESTADO y por qué esos y no otros? · ¿Por qué ESTADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO, COUNT(*) AS filas FROM CONTENEDOR GROUP BY ESTADO ORDER BY ESTADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR como una hoja de cálculo donde cada fila es una caneca. ESTADO es la casilla que guarda: 1 En servicio, 2 Fuera de servicio, 3 Dada de baja. Nunca puede quedar vacía.
- **Explicación ampliada:** Dice si la caneca puede usarse: 1 En servicio, 2 Fuera de servicio, 3 Dada de baja. Una caneca nueva nace en 2 (fuera de servicio) para que nadie la use antes de configurarla. Cada vez que cambia, el disparador 1 (TRG_CONTENEDOR_AUD_ESTADO) guarda el antes y el después en HISTORIAL_ESTADO_CONTENEDOR. Si el profesor dice «ESTADO_CONTENEDOR», se refiere a esta columna.

### CONTENEDOR_MATERIAL (3 atributos)

Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.

#### 56. `CONTENEDOR_MATERIAL.ID_CONTENEDOR`

- **1. Tabla:** CONTENEDOR_MATERIAL — Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Caneca.
- **4. Para qué sirve:** Sirve a la vez para identificar cada fila de CONTENEDOR_MATERIAL y para atarla a su fila de CONTENEDOR (relación 1:1 o tabla puente). Se usa en: Saber qué recibe.
- **5. Por qué existe:** Relación N:M caneca-material.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** PK; FK: FK_CONT_MAT_CONTENEDOR->CONTENEDOR
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_CONT_MAT_CONTENEDOR) · Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Decidir a qué caneca va cada material
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR_MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** La función que resuelve la caneca. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 11
- **15. Ejemplo de valor incorrecto:** Un número que no exista en CONTENEDOR (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir; registros huérfanos (no puede apuntar a un(a) CONTENEDOR que no existe). Identidad: cada fila se distingue de las demás · Integridad referencial con CONTENEDOR · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de CONTENEDOR_MATERIAL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (11 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide a qué caneca manda el bot cada material.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_CONTENEDOR es un número subrogado y no un dato del negocio que podría cambiar? · ¿Qué pasa si intento guardar un ID_CONTENEDOR que no existe en CONTENEDOR? (respuesta: ORA-02291)
- **23. SELECT para verlo:** SELECT * FROM CONTENEDOR_MATERIAL WHERE ID_CONTENEDOR = (SELECT MIN(ID_CONTENEDOR) FROM CONTENEDOR_MATERIAL);
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR_MATERIAL como una hoja de cálculo donde cada fila es un material que una caneca acepta. ID_CONTENEDOR es la casilla que guarda: Caneca. Funciona como la «cédula» de la fila: no se repite y no cambia. Es un «puntero»: copia el número de una fila de CONTENEDOR para unir las dos tablas. Nunca puede quedar vacía.

#### 57. `CONTENEDOR_MATERIAL.ID_MATERIAL`

- **1. Tabla:** CONTENEDOR_MATERIAL — Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.
- **2. Atributo:** ID_MATERIAL
- **3. Qué significa:** Material que esa caneca recibe.
- **4. Para qué sirve:** Sirve a la vez para identificar cada fila de CONTENEDOR_MATERIAL y para atarla a su fila de MATERIAL (relación 1:1 o tabla puente). Se usa en: Validar el depósito (trigger 2).
- **5. Por qué existe:** Lista blanca de materiales por caneca.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** PK; FK: FK_CONT_MAT_MATERIAL->MATERIAL
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_CONT_MAT_MATERIAL) · Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Decidir a qué caneca va cada material
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR_MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** La función que resuelve la caneca. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir; registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe). Identidad: cada fila se distingue de las demás · Integridad referencial con MATERIAL · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de CONTENEDOR_MATERIAL, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (11 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide a qué caneca manda el bot cada material.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_MATERIAL es un número subrogado y no un dato del negocio que podría cambiar? · ¿Qué pasa si intento guardar un ID_MATERIAL que no existe en MATERIAL? (respuesta: ORA-02291)
- **23. SELECT para verlo:** SELECT * FROM CONTENEDOR_MATERIAL WHERE ID_MATERIAL = (SELECT MIN(ID_MATERIAL) FROM CONTENEDOR_MATERIAL);
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR_MATERIAL como una hoja de cálculo donde cada fila es un material que una caneca acepta. ID_MATERIAL es la casilla que guarda: Material que esa caneca recibe. Funciona como la «cédula» de la fila: no se repite y no cambia. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Nunca puede quedar vacía.

#### 58. `CONTENEDOR_MATERIAL.CATEGORIA`

- **1. Tabla:** CONTENEDOR_MATERIAL — Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.
- **2. Atributo:** CATEGORIA
- **3. Qué significa:** Color compartido por la caneca y el material.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de CONTENEDOR_MATERIAL y poder unirlas con JOIN. Se usa en: Dos FK compuestas.
- **5. Por qué existe:** Redundancia CONTROLADA: Oracle impide poner vidrio en la caneca verde sin necesidad de un trigger.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONT_MAT_CONTENEDOR->CONTENEDOR, FK_CONT_MAT_MATERIAL->MATERIAL; CHECK CK_CONT_MAT_CATEGORIA
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_CONT_MAT_CONTENEDOR) · Apunta a MATERIAL (FK FK_CONT_MAT_MATERIAL)
- **11. Qué proceso lo usa:** Decidir a qué caneca va cada material
- **12. Quién lo llena:** Administrador. INSERT/UPDATE directo sobre CONTENEDOR_MATERIAL (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** La función que resuelve la caneca. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Un valor que rompa CK_CONT_MAT_CATEGORIA: categoria IN (1, 2, 3) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Integridad referencial con MATERIAL · Reglas CHECK: CK_CONT_MAT_CATEGORIA · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONTENEDOR_MATERIAL y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** FN_RESOLVER_CANECA_DISPONIBLE, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (11 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Decide a qué caneca manda el bot cada material.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un CATEGORIA que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_CONT_MAT_CATEGORIA y por qué esos y no otros? · ¿Por qué CATEGORIA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.CATEGORIA, COUNT(*) AS filas FROM CONTENEDOR_MATERIAL t JOIN MATERIAL r ON r.ID_MATERIAL = t.CATEGORIA GROUP BY t.CATEGORIA ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONTENEDOR_MATERIAL como una hoja de cálculo donde cada fila es un material que una caneca acepta. CATEGORIA es la casilla que guarda: Color compartido por la caneca y el material. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Nunca puede quedar vacía.

## Módulo: M4 · Operación (QR → foto → orden → evento → depósito)

### INTERACCION (15 atributos)

Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.

#### 59. `INTERACCION.ID_INTERACCION`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_INTERACCION
- **3. Qué significa:** Número de la sesión.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de INTERACCION, de modo que otras tablas puedan apuntarle. Se usa en: Une fotos, orden y depósito.
- **5. Por qué existe:** Cada escaneo de QR es una sesión.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; UNIQUE: UQ_INTERACCION_CONTENEDOR; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: CONSULTA_VISION, ORDEN_APERTURA
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 57
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de INTERACCION, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_INTERACCION es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM INTERACCION WHERE ID_INTERACCION = (SELECT MIN(ID_INTERACCION) FROM INTERACCION);
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_INTERACCION es la casilla que guarda: Número de la sesión. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 60. `INTERACCION.ID_PUNTO_INSTALACION`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_PUNTO_INSTALACION
- **3. Qué significa:** Estación donde se escaneó el QR.
- **4. Para qué sirve:** Sirve para saber a qué fila de PUNTO_INSTALACION pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Resolver caneca.
- **5. Por qué existe:** Con QR de estación se sabe el lugar antes que la caneca.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_CONTENEDOR->CONTENEDOR, FK_INTERACCION_PUNTO->PUNTO_INSTALACION
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_INTERACCION_CONTENEDOR) · Apunta a PUNTO_INSTALACION (FK FK_INTERACCION_PUNTO)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot Eco. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en PUNTO_INSTALACION (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) PUNTO_INSTALACION que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con PUNTO_INSTALACION
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y PUNTO_INSTALACION: no sabríamos a qué PUNTO_INSTALACION pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_PUNTO_INSTALACION que no existe en PUNTO_INSTALACION? (respuesta: ORA-02291) · ¿Por qué ID_PUNTO_INSTALACION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_PUNTO_INSTALACION, COUNT(*) AS filas FROM INTERACCION t JOIN PUNTO_INSTALACION r ON r.ID_PUNTO_INSTALACION = t.ID_PUNTO_INSTALACION GROUP BY t.ID_PUNTO_INSTALACION ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_PUNTO_INSTALACION es la casilla que guarda: Estación donde se escaneó el QR. Es un «puntero»: copia el número de una fila de PUNTO_INSTALACION para unir las dos tablas. Nunca puede quedar vacía.

#### 61. `INTERACCION.ID_CONTENEDOR`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Caneca elegida para el material.
- **4. Para qué sirve:** Sirve para saber a qué fila de CONTENEDOR pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Orden de apertura.
- **5. Por qué existe:** FK compuesta: la caneca debe ser de esa misma estación.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = todavía no se sabe qué caneca.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_CONTENEDOR->CONTENEDOR; UNIQUE: UQ_INTERACCION_CONTENEDOR; CHECK CK_INTERACCION_AUTORIZADA
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_INTERACCION_CONTENEDOR)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot (fn_resolver_caneca). INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 12
- **15. Ejemplo de valor incorrecto:** Un número que no exista en CONTENEDOR (ORA-02291) · Un valor que rompa CK_INTERACCION_AUTORIZADA: estado NOT IN (2, 3) OR (id_material_confirmado IS NOT NULL AND id_contenedor IS NOT NULL) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_INTERACCION_CONTENEDOR)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) CONTENEDOR que no existe); valores duplicados; valores fuera de las opciones permitidas. Integridad referencial con CONTENEDOR · Reglas CHECK: CK_INTERACCION_AUTORIZADA · Unicidad: UQ_INTERACCION_CONTENEDOR · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y CONTENEDOR: no sabríamos a qué CONTENEDOR pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CONTENEDOR que no existe en CONTENEDOR? (respuesta: ORA-02291) · ¿Qué valores permite CK_INTERACCION_AUTORIZADA y por qué esos y no otros? · ¿Por qué ID_CONTENEDOR no puede repetirse? (UNIQUE UQ_INTERACCION_CONTENEDOR)
- **23. SELECT para verlo:** SELECT t.ID_CONTENEDOR, COUNT(*) AS filas FROM INTERACCION t JOIN CONTENEDOR r ON r.ID_CONTENEDOR = t.ID_CONTENEDOR GROUP BY t.ID_CONTENEDOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_CONTENEDOR es la casilla que guarda: Caneca elegida para el material. Es un «puntero»: copia el número de una fila de CONTENEDOR para unir las dos tablas. Puede quedar vacía: NULL = todavía no se sabe qué caneca.

#### 62. `INTERACCION.ID_USUARIO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Ciudadano de la sesión.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Historial personal.
- **5. Por qué existe:** Permite atender visitantes sin dar puntos.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = visitante sin cuenta.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_INTERACCION_USUARIO)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot Eco. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 17
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe). Integridad referencial con USUARIO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291) · ¿Qué significa que ID_USUARIO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_USUARIO, COUNT(*) AS filas FROM INTERACCION t JOIN USUARIO r ON r.ID_USUARIO = t.ID_USUARIO GROUP BY t.ID_USUARIO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_USUARIO es la casilla que guarda: Ciudadano de la sesión. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Puede quedar vacía: NULL = visitante sin cuenta.

#### 63. `INTERACCION.CANAL`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** CANAL
- **3. Qué significa:** 1 Telegram, 2 Manual, 3 Simulador, 4 Telegram simulado, 5 Asistido.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: 1 Telegram, 2 Manual, 3 Simulador, 4 Telegram simulado, 5 Asistido. Se usa en: Separar pruebas de uso real.
- **5. Por qué existe:** Mismos valores que CanalInteraccion de P3.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_CANAL; CHECK CK_INTERACCION_MANUAL
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot / escritorio. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 4
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_INTERACCION_CANAL: canal IN (1, 2, 3, 4, 5) (ORA-02290) · Una combinación que rompa CK_INTERACCION_MANUAL (condición completa en el anexo de CHECK) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_INTERACCION_CANAL, CK_INTERACCION_MANUAL
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (1 Telegram, 2 Manual, 3 Simulador, 4 Telegram simulado, 5 Asistido). Mismos valores que CanalInteraccion de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_CANAL y por qué esos y no otros? · ¿Por qué CANAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CANAL, COUNT(*) AS filas FROM INTERACCION GROUP BY CANAL ORDER BY CANAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. CANAL es la casilla que guarda: 1 Telegram, 2 Manual, 3 Simulador, 4 Telegram simulado, 5 Asistido. Nunca puede quedar vacía.

#### 64. `INTERACCION.ESTADO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ESTADO
- **3. Qué significa:** 1 Iniciada, 2 Autorizada, 3 Con depósito, 4 Sin depósito, 5 Rechazada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: 1 Iniciada, 2 Autorizada, 3 Con depósito, 4 Sin depósito, 5 Rechazada. Se usa en: Seguimiento del flujo.
- **5. Por qué existe:** Sabe en qué paso quedó cada sesión.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 1).
- **8. Valor por defecto:** 1
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_AUTORIZADA; CHECK CK_INTERACCION_CIERRE; CHECK CK_INTERACCION_ESTADO; CHECK CK_INTERACCION_RECHAZO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot / servicios. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_INTERACCION_AUTORIZADA: estado NOT IN (2, 3) OR (id_material_confirmado IS NOT NULL AND id_contenedor IS NOT NULL) (ORA-02290) · Una combinación que rompa CK_INTERACCION_CIERRE (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_INTERACCION_ESTADO: estado IN (1, 2, 3, 4, 5) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_INTERACCION_AUTORIZADA, CK_INTERACCION_CIERRE, CK_INTERACCION_ESTADO, CK_INTERACCION_RECHAZO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (1 Iniciada, 2 Autorizada, 3 Con depósito, 4 Sin depósito, 5 Rechazada). Sabe en qué paso quedó cada sesión.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_AUTORIZADA y por qué esos y no otros? · ¿Por qué ESTADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO, COUNT(*) AS filas FROM INTERACCION GROUP BY ESTADO ORDER BY ESTADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ESTADO es la casilla que guarda: 1 Iniciada, 2 Autorizada, 3 Con depósito, 4 Sin depósito, 5 Rechazada. Nunca puede quedar vacía.

#### 65. `INTERACCION.ID_MATERIAL_SUGERIDO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_MATERIAL_SUGERIDO
- **3. Qué significa:** Material que propuso la IA.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Comparar IA vs realidad.
- **5. Por qué existe:** La sugerencia NO es verificación.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = la IA no propuso.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_MAT_SUG->MATERIAL; CHECK CK_INTERACCION_MANUAL; CHECK CK_INTERACCION_SUGERENCIA
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_INTERACCION_MAT_SUG)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot (visión). INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Una combinación que rompa CK_INTERACCION_MANUAL (condición completa en el anexo de CHECK) (ORA-02290) · Una combinación que rompa CK_INTERACCION_SUGERENCIA (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con MATERIAL · Reglas CHECK: CK_INTERACCION_MANUAL, CK_INTERACCION_SUGERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL_SUGERIDO que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_INTERACCION_MANUAL y por qué esos y no otros? · ¿Qué significa que ID_MATERIAL_SUGERIDO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL_SUGERIDO, COUNT(*) AS filas FROM INTERACCION t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL_SUGERIDO GROUP BY t.ID_MATERIAL_SUGERIDO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_MATERIAL_SUGERIDO es la casilla que guarda: Material que propuso la IA. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Puede quedar vacía: NULL = la IA no propuso.

#### 66. `INTERACCION.CONFIANZA_SUGERIDA`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** CONFIANZA_SUGERIDA
- **3. Qué significa:** Confianza de la IA entre 0 y 1.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: confianza de la IA entre 0 y 1. Se usa en: Calidad del reconocimiento.
- **5. Por qué existe:** CHECK: sugerencia y confianza van juntas.
- **6. Tipo de dato:** NUMBER(4,3) (4 dígitos en total, 3 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL junto con el material sugerido.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_CONFIANZA; CHECK CK_INTERACCION_SUGERENCIA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot (visión). INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 0,873
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_INTERACCION_CONFIANZA: confianza_sugerida BETWEEN 0 AND 1 (ORA-02290) · Una combinación que rompa CK_INTERACCION_SUGERENCIA (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_INTERACCION_CONFIANZA, CK_INTERACCION_SUGERENCIA
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (confianza de la IA entre 0 y 1). CHECK: sugerencia y confianza van juntas.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_CONFIANZA y por qué esos y no otros? · ¿Qué significa que CONFIANZA_SUGERIDA esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(CONFIANZA_SUGERIDA) AS con_valor, MIN(CONFIANZA_SUGERIDA) AS minimo, MAX(CONFIANZA_SUGERIDA) AS maximo, SUM(CONFIANZA_SUGERIDA) AS total FROM INTERACCION;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. CONFIANZA_SUGERIDA es la casilla que guarda: Confianza de la IA entre 0 y 1. Puede quedar vacía: NULL junto con el material sugerido.

#### 67. `INTERACCION.ID_MATERIAL_CONFIRMADO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_MATERIAL_CONFIRMADO
- **3. Qué significa:** Material que la persona confirmó.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Orden y depósito declarado.
- **5. Por qué existe:** Obligatorio desde el estado Autorizada (CHECK).
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = aún no confirmó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_MAT_CONF->MATERIAL; CHECK CK_INTERACCION_AUTORIZADA
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_INTERACCION_MAT_CONF)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Un valor que rompa CK_INTERACCION_AUTORIZADA: estado NOT IN (2, 3) OR (id_material_confirmado IS NOT NULL AND id_contenedor IS NOT NULL) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con MATERIAL · Reglas CHECK: CK_INTERACCION_AUTORIZADA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL_CONFIRMADO que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_INTERACCION_AUTORIZADA y por qué esos y no otros? · ¿Qué significa que ID_MATERIAL_CONFIRMADO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL_CONFIRMADO, COUNT(*) AS filas FROM INTERACCION t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL_CONFIRMADO GROUP BY t.ID_MATERIAL_CONFIRMADO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_MATERIAL_CONFIRMADO es la casilla que guarda: Material que la persona confirmó. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Puede quedar vacía: NULL = aún no confirmó.

#### 68. `INTERACCION.CANTIDAD_DECLARADA`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** CANTIDAD_DECLARADA
- **3. Qué significa:** Unidades que la persona dice depositar.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: unidades que la persona dice depositar. Se usa en: Estimación de peso.
- **5. Por qué existe:** Nunca se toma como conteo verificado.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no se preguntó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_CANTIDAD
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_INTERACCION_CANTIDAD: cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000 (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_INTERACCION_CANTIDAD · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (unidades que la persona dice depositar). Nunca se toma como conteo verificado.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_CANTIDAD y por qué esos y no otros? · ¿Qué significa que CANTIDAD_DECLARADA esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT CANTIDAD_DECLARADA, COUNT(*) AS filas FROM INTERACCION GROUP BY CANTIDAD_DECLARADA ORDER BY CANTIDAD_DECLARADA;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. CANTIDAD_DECLARADA es la casilla que guarda: Unidades que la persona dice depositar. Puede quedar vacía: NULL = no se preguntó.

#### 69. `INTERACCION.ID_OPERARIO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ID_OPERARIO
- **3. Qué significa:** Operario que hizo una apertura manual.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de INTERACCION y poder unirlas con JOIN. Se usa en: Auditoría de aperturas manuales.
- **5. Por qué existe:** CHECK: solo canal 2 lleva operario.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no fue manual.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_INTERACCION_OPERARIO->USUARIO; CHECK CK_INTERACCION_MANUAL
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_INTERACCION_OPERARIO)
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Escritorio (canal 2). INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Una combinación que rompa CK_INTERACCION_MANUAL (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); valores fuera de las opciones permitidas. Integridad referencial con USUARIO · Reglas CHECK: CK_INTERACCION_MANUAL
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre INTERACCION y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_OPERARIO que no existe en USUARIO? (respuesta: ORA-02291) · ¿Qué valores permite CK_INTERACCION_MANUAL y por qué esos y no otros? · ¿Qué significa que ID_OPERARIO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_OPERARIO, COUNT(*) AS filas FROM INTERACCION t JOIN USUARIO r ON r.ID_USUARIO = t.ID_OPERARIO GROUP BY t.ID_OPERARIO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ID_OPERARIO es la casilla que guarda: Operario que hizo una apertura manual. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Puede quedar vacía: NULL = no fue manual.

#### 70. `INTERACCION.MOTIVO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** MOTIVO
- **3. Qué significa:** Razón de una apertura manual, rechazo o cierre sin depósito.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: razón de una apertura manual, rechazo o cierre sin depósito. Se usa en: Reportes.
- **5. Por qué existe:** Obligatorio en rechazo y apertura manual.
- **6. Tipo de dato:** VARCHAR2(300 CHAR) (hasta 300 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = sin motivo.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_MANUAL; CHECK CK_INTERACCION_MOTIVO; CHECK CK_INTERACCION_RECHAZO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot / operario. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Residuo peligroso: se orientó al punto posconsumo
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_INTERACCION_MANUAL (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_INTERACCION_MOTIVO: motivo IS NULL OR (TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo)) (ORA-02290) · Un valor que rompa CK_INTERACCION_RECHAZO: estado <> 5 OR motivo IS NOT NULL (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_INTERACCION_MANUAL, CK_INTERACCION_MOTIVO, CK_INTERACCION_RECHAZO · Vigilada por: TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (razón de una apertura manual, rechazo o cierre sin depósito). Obligatorio en rechazo y apertura manual.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_MANUAL y por qué esos y no otros? · ¿Qué significa que MOTIVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MOTIVO, COUNT(*) AS filas FROM INTERACCION GROUP BY MOTIVO ORDER BY MOTIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. MOTIVO es la casilla que guarda: Razón de una apertura manual, rechazo o cierre sin depósito. Puede quedar vacía: NULL = sin motivo.

#### 71. `INTERACCION.FECHA_INICIO_UTC`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** FECHA_INICIO_UTC
- **3. Qué significa:** Cuándo empezó la sesión.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: cuándo empezó la sesión. Se usa en: Filtros por fecha.
- **5. Por qué existe:** Base de los reportes de uso.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_CIERRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:20
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_INTERACCION_CIERRE (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_INTERACCION_CIERRE
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (cuándo empezó la sesión). Base de los reportes de uso.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_CIERRE y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_INICIO_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_INICIO_UTC) AS primera, MAX(FECHA_INICIO_UTC) AS ultima, COUNT(FECHA_INICIO_UTC) AS con_valor FROM INTERACCION;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. FECHA_INICIO_UTC es la casilla que guarda: Cuándo empezó la sesión. Nunca puede quedar vacía.

#### 72. `INTERACCION.FECHA_CIERRE_UTC`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** FECHA_CIERRE_UTC
- **3. Qué significa:** Cuándo terminó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: cuándo terminó. Se usa en: Duración.
- **5. Por qué existe:** CHECK coherente con el estado.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = sesión abierta (estados 1-2).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_CIERRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:21
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_INTERACCION_CIERRE (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_INTERACCION_CIERRE
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (cuándo terminó). CHECK coherente con el estado.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_CIERRE y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_CIERRE_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_CIERRE_UTC) AS primera, MAX(FECHA_CIERRE_UTC) AS ultima, COUNT(FECHA_CIERRE_UTC) AS con_valor FROM INTERACCION;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. FECHA_CIERRE_UTC es la casilla que guarda: Cuándo terminó. Puede quedar vacía: NULL = sesión abierta (estados 1-2).

#### 73. `INTERACCION.ES_SINTETICO`

- **1. Tabla:** INTERACCION — Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.
- **2. Atributo:** ES_SINTETICO
- **3. Qué significa:** 1 si la sesión es simulada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de INTERACCION: 1 si la sesión es simulada. Se usa en: Rótulo DATOS SINTÉTICOS.
- **5. Por qué existe:** No confundir simulación con uso real.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_INTERACCION_SINTETICO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Sesión QR, desde el escaneo hasta el cierre
- **12. Quién lo llena:** Script 11. INSERT/UPDATE directo sobre INTERACCION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario, administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_INTERACCION_SINTETICO: es_sintetico IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); mezclar datos de demostración con datos del sistema. Reglas CHECK: CK_INTERACCION_SINTETICO
- **17. Qué pasaría si no existiera:** La tabla INTERACCION no podría guardar este dato (1 si la sesión es simulada). No confundir simulación con uso real.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (30 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada /start con QR abre una sesión.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_INTERACCION_SINTETICO y por qué esos y no otros? · ¿Por qué ES_SINTETICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ES_SINTETICO, COUNT(*) AS filas FROM INTERACCION GROUP BY ES_SINTETICO ORDER BY ES_SINTETICO;
- **24. Explicación sencilla (para quien empieza):** Piensa en INTERACCION como una hoja de cálculo donde cada fila es una sesión iniciada con el QR. ES_SINTETICO es la casilla que guarda: 1 si la sesión es simulada. Nunca puede quedar vacía.

### CONSULTA_VISION (20 atributos)

Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.

#### 74. `CONSULTA_VISION.ID_CONSULTA`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_CONSULTA
- **3. Qué significa:** Número de la foto analizada.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de CONSULTA_VISION, de modo que otras tablas puedan apuntarle. Se usa en: Reportes de visión.
- **5. Por qué existe:** Cada foto atendida es un registro.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 120
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de CONSULTA_VISION, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_CONSULTA es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM CONSULTA_VISION WHERE ID_CONSULTA = (SELECT MIN(ID_CONSULTA) FROM CONSULTA_VISION);
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_CONSULTA es la casilla que guarda: Número de la foto analizada. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 75. `CONSULTA_VISION.CLAVE`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** CLAVE
- **3. Qué significa:** Clave única del análisis (idempotencia).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: clave única del análisis (idempotencia). Se usa en: Evitar registrar dos veces la misma foto.
- **5. Por qué existe:** UNIQUE: un reintento de Telegram no duplica la consulta.
- **6. Tipo de dato:** VARCHAR2(80 CHAR) (hasta 80 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_CONSULTA_CLAVE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** SIM-CV-57-B
- **15. Ejemplo de valor incorrecto:** Repetir un valor ya usado (ORA-00001 por UQ_CONSULTA_CLAVE) · Dejarlo vacío (ORA-01400) · Un texto de más de 80 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores duplicados; filas incompletas (no se puede guardar sin este dato). Unicidad: UQ_CONSULTA_CLAVE
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (clave única del análisis (idempotencia)). UNIQUE: un reintento de Telegram no duplica la consulta.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué CLAVE no puede repetirse? (UNIQUE UQ_CONSULTA_CLAVE) · ¿Por qué CLAVE es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CLAVE FROM CONSULTA_VISION FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. CLAVE es la casilla que guarda: Clave única del análisis (idempotencia). Nunca puede quedar vacía.

#### 76. `CONSULTA_VISION.ID_USUARIO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Persona que envió la foto.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de CONSULTA_VISION y poder unirlas con JOIN. Se usa en: Historial personal.
- **5. Por qué existe:** Consultar sin cuenta está permitido.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = visitante.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONSULTA_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_CONSULTA_USUARIO)
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 17
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe). Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONSULTA_VISION y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291) · ¿Qué significa que ID_USUARIO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_USUARIO, COUNT(*) AS filas FROM CONSULTA_VISION t JOIN USUARIO r ON r.ID_USUARIO = t.ID_USUARIO GROUP BY t.ID_USUARIO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_USUARIO es la casilla que guarda: Persona que envió la foto. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Puede quedar vacía: NULL = visitante.

#### 77. `CONSULTA_VISION.ID_INTERACCION`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_INTERACCION
- **3. Qué significa:** Sesión QR en la que se envió.
- **4. Para qué sirve:** Sirve para saber a qué fila de INTERACCION pertenece cada fila de CONSULTA_VISION y poder unirlas con JOIN. Se usa en: Recorrido completo.
- **5. Por qué existe:** Distinguir consultas sueltas de sesiones de depósito.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = foto sin QR (solo orientación).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONSULTA_INTERACCION->INTERACCION
- **10. Con qué tabla se relaciona:** Apunta a INTERACCION (FK FK_CONSULTA_INTERACCION)
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 57
- **15. Ejemplo de valor incorrecto:** Un número que no exista en INTERACCION (ORA-02291)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) INTERACCION que no existe). Integridad referencial con INTERACCION
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONSULTA_VISION y INTERACCION: no sabríamos a qué INTERACCION pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_INTERACCION que no existe en INTERACCION? (respuesta: ORA-02291) · ¿Qué significa que ID_INTERACCION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_INTERACCION, COUNT(*) AS filas FROM CONSULTA_VISION t JOIN INTERACCION r ON r.ID_INTERACCION = t.ID_INTERACCION GROUP BY t.ID_INTERACCION ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_INTERACCION es la casilla que guarda: Sesión QR en la que se envió. Es un «puntero»: copia el número de una fila de INTERACCION para unir las dos tablas. Puede quedar vacía: NULL = foto sin QR (solo orientación).

#### 78. `CONSULTA_VISION.CANAL`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** CANAL
- **3. Qué significa:** 1 Telegram, 2 Escritorio, 3 Evaluación.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: 1 Telegram, 2 Escritorio, 3 Evaluación. Se usa en: Reportes por canal.
- **5. Por qué existe:** Igual que CanalConsultaVision de P3.
- **6. Tipo de dato:** NUMBER(2) (entero de hasta 2 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_CANAL
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_CANAL: canal IN (1, 2, 3) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONSULTA_CANAL
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (1 Telegram, 2 Escritorio, 3 Evaluación). Igual que CanalConsultaVision de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_CANAL y por qué esos y no otros? · ¿Por qué CANAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CANAL, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY CANAL ORDER BY CANAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. CANAL es la casilla que guarda: 1 Telegram, 2 Escritorio, 3 Evaluación. Nunca puede quedar vacía.

#### 79. `CONSULTA_VISION.MODO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** MODO
- **3. Qué significa:** 1 Software (sin ESP32), 2 Con dispositivo.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: 1 Software (sin ESP32), 2 Con dispositivo. Se usa en: Saber si hubo hardware.
- **5. Por qué existe:** En modo software no se inventan aperturas.
- **6. Tipo de dato:** NUMBER(2) (entero de hasta 2 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_MODO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_MODO: modo IN (1, 2) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONSULTA_MODO
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (1 Software (sin ESP32), 2 Con dispositivo). En modo software no se inventan aperturas.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_MODO y por qué esos y no otros? · ¿Por qué MODO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MODO, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY MODO ORDER BY MODO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. MODO es la casilla que guarda: 1 Software (sin ESP32), 2 Con dispositivo. Nunca puede quedar vacía.

#### 80. `CONSULTA_VISION.PROVEEDOR`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** PROVEEDOR
- **3. Qué significa:** Motor de visión usado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: motor de visión usado. Se usa en: Comparar motores.
- **5. Por qué existe:** Trazabilidad del resultado de IA.
- **6. Tipo de dato:** VARCHAR2(80 CHAR) (hasta 80 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_PROVEEDOR
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** SIMULADOR
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_PROVEEDOR: TRIM(proveedor) IS NOT NULL AND proveedor = TRIM(proveedor) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 80 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONSULTA_PROVEEDOR
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (motor de visión usado). Trazabilidad del resultado de IA.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_PROVEEDOR y por qué esos y no otros? · ¿Por qué PROVEEDOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT PROVEEDOR, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY PROVEEDOR ORDER BY PROVEEDOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. PROVEEDOR es la casilla que guarda: Motor de visión usado. Nunca puede quedar vacía.

#### 81. `CONSULTA_VISION.MODELO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** MODELO
- **3. Qué significa:** Versión del modelo de IA.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: versión del modelo de IA. Se usa en: Comparar versiones.
- **5. Por qué existe:** Absorbe la tabla ANALISIS_IMAGEN de P3.
- **6. Tipo de dato:** VARCHAR2(80 CHAR) (hasta 80 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL = no informado.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** sin-modelo
- **15. Ejemplo de valor incorrecto:** Un texto de más de 80 caracteres (ORA-12899)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (versión del modelo de IA). Absorbe la tabla ANALISIS_IMAGEN de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que MODELO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(MODELO) AS con_valor FROM CONSULTA_VISION;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. MODELO es la casilla que guarda: Versión del modelo de IA. Puede quedar vacía: NULL = no informado.

#### 82. `CONSULTA_VISION.RESULTADO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** RESULTADO
- **3. Qué significa:** 1 Identificado, 2 Pidió otra foto, 3 No es residuo, 4 Error.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: 1 Identificado, 2 Pidió otra foto, 3 No es residuo, 4 Error. Se usa en: Calidad del servicio.
- **5. Por qué existe:** Simplificación académica del código 1-10 de P3.
- **6. Tipo de dato:** NUMBER(2) (entero de hasta 2 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_IDENTIFICADO; CHECK CK_CONSULTA_RESULTADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_IDENTIFICADO: resultado <> 1 OR (clave_objeto IS NOT NULL AND id_material IS NOT NULL) (ORA-02290) · Un valor que rompa CK_CONSULTA_RESULTADO: resultado IN (1, 2, 3, 4) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONSULTA_IDENTIFICADO, CK_CONSULTA_RESULTADO
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (1 Identificado, 2 Pidió otra foto, 3 No es residuo, 4 Error). Simplificación académica del código 1-10 de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_IDENTIFICADO y por qué esos y no otros? · ¿Por qué RESULTADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT RESULTADO, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY RESULTADO ORDER BY RESULTADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. RESULTADO es la casilla que guarda: 1 Identificado, 2 Pidió otra foto, 3 No es residuo, 4 Error. Nunca puede quedar vacía.

#### 83. `CONSULTA_VISION.CLAVE_OBJETO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** CLAVE_OBJETO
- **3. Qué significa:** Objeto reconocido.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: objeto reconocido. Se usa en: Buscar el peso de referencia.
- **5. Por qué existe:** CHECK: resultado 1 exige objeto y material.
- **6. Tipo de dato:** VARCHAR2(40 CHAR) (hasta 40 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL si no se identificó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_IDENTIFICADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot (IA). INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** BOTELLA_PET_600ML
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_IDENTIFICADO: resultado <> 1 OR (clave_objeto IS NOT NULL AND id_material IS NOT NULL) (ORA-02290) · Un texto de más de 40 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_CONSULTA_IDENTIFICADO
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (objeto reconocido). CHECK: resultado 1 exige objeto y material.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_IDENTIFICADO y por qué esos y no otros? · ¿Qué significa que CLAVE_OBJETO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT CLAVE_OBJETO, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY CLAVE_OBJETO ORDER BY CLAVE_OBJETO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. CLAVE_OBJETO es la casilla que guarda: Objeto reconocido. Puede quedar vacía: NULL si no se identificó.

#### 84. `CONSULTA_VISION.ID_MATERIAL`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_MATERIAL
- **3. Qué significa:** Material del objeto.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de CONSULTA_VISION y poder unirlas con JOIN. Se usa en: Caneca indicada.
- **5. Por qué existe:** La IA solo puede apuntar a materiales del catálogo.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si no se identificó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONSULTA_MATERIAL->MATERIAL; CHECK CK_CONSULTA_IDENTIFICADO
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_CONSULTA_MATERIAL)
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot (IA validada con catálogo). INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Un valor que rompa CK_CONSULTA_IDENTIFICADO: resultado <> 1 OR (clave_objeto IS NOT NULL AND id_material IS NOT NULL) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con MATERIAL · Reglas CHECK: CK_CONSULTA_IDENTIFICADO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONSULTA_VISION y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_CONSULTA_IDENTIFICADO y por qué esos y no otros? · ¿Qué significa que ID_MATERIAL esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL, COUNT(*) AS filas FROM CONSULTA_VISION t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL GROUP BY t.ID_MATERIAL ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_MATERIAL es la casilla que guarda: Material del objeto. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Puede quedar vacía: NULL si no se identificó.

#### 85. `CONSULTA_VISION.UNIDADES_ESTIMADAS`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** UNIDADES_ESTIMADAS
- **3. Qué significa:** Cuántos objetos se ven en la foto.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: cuántos objetos se ven en la foto. Se usa en: Estimación de peso.
- **5. Por qué existe:** Una foto no equivale siempre a una unidad.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no contó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_PESO; CHECK CK_CONSULTA_UNIDADES
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot (IA). INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_CONSULTA_PESO (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_CONSULTA_UNIDADES: unidades_estimadas IS NULL OR unidades_estimadas BETWEEN 1 AND 1000 (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_CONSULTA_PESO, CK_CONSULTA_UNIDADES
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (cuántos objetos se ven en la foto). Una foto no equivale siempre a una unidad.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_PESO y por qué esos y no otros? · ¿Qué significa que UNIDADES_ESTIMADAS esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT UNIDADES_ESTIMADAS, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY UNIDADES_ESTIMADAS ORDER BY UNIDADES_ESTIMADAS;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. UNIDADES_ESTIMADAS es la casilla que guarda: Cuántos objetos se ven en la foto. Puede quedar vacía: NULL = no contó.

#### 86. `CONSULTA_VISION.ESTADO_VISUAL`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ESTADO_VISUAL
- **3. Qué significa:** Vacío, parcial o lleno según la foto.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: vacío, parcial o lleno según la foto. Se usa en: Elegir el peso de referencia.
- **5. Por qué existe:** El contenido cambia mucho el peso.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** Sí. NULL = no aplica.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_ESTADO_VIS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot (IA). INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_ESTADO_VIS: estado_visual IS NULL OR estado_visual IN (0, 1, 2, 3) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_CONSULTA_ESTADO_VIS
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (vacío, parcial o lleno según la foto). El contenido cambia mucho el peso.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_ESTADO_VIS y por qué esos y no otros? · ¿Qué significa que ESTADO_VISUAL esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_VISUAL, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY ESTADO_VISUAL ORDER BY ESTADO_VISUAL;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ESTADO_VISUAL es la casilla que guarda: Vacío, parcial o lleno según la foto. Puede quedar vacía: NULL = no aplica.

#### 87. `CONSULTA_VISION.ID_PESO_REFERENCIAL`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_PESO_REFERENCIAL
- **3. Qué significa:** Fila del catálogo usada para estimar.
- **4. Para qué sirve:** Sirve para saber a qué fila de PESO_REFERENCIAL pertenece cada fila de CONSULTA_VISION y poder unirlas con JOIN. Se usa en: Explicar la estimación.
- **5. Por qué existe:** CHECK: no hay peso estimado sin catálogo.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = sin estimación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONSULTA_PESO->PESO_REFERENCIAL; CHECK CK_CONSULTA_PESO
- **10. Con qué tabla se relaciona:** Apunta a PESO_REFERENCIAL (FK FK_CONSULTA_PESO)
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en PESO_REFERENCIAL (ORA-02291) · Una combinación que rompa CK_CONSULTA_PESO (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) PESO_REFERENCIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con PESO_REFERENCIAL · Reglas CHECK: CK_CONSULTA_PESO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONSULTA_VISION y PESO_REFERENCIAL: no sabríamos a qué PESO_REFERENCIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_PESO_REFERENCIAL que no existe en PESO_REFERENCIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_CONSULTA_PESO y por qué esos y no otros? · ¿Qué significa que ID_PESO_REFERENCIAL esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_PESO_REFERENCIAL, COUNT(*) AS filas FROM CONSULTA_VISION t JOIN PESO_REFERENCIAL r ON r.ID_PESO_REFERENCIAL = t.ID_PESO_REFERENCIAL GROUP BY t.ID_PESO_REFERENCIAL ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_PESO_REFERENCIAL es la casilla que guarda: Fila del catálogo usada para estimar. Es un «puntero»: copia el número de una fila de PESO_REFERENCIAL para unir las dos tablas. Puede quedar vacía: NULL = sin estimación.

#### 88. `CONSULTA_VISION.PESO_ESTIMADO_KG`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** PESO_ESTIMADO_KG
- **3. Qué significa:** Peso ESTIMADO (no medido) en kg.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: peso ESTIMADO (no medido) en kg. Se usa en: Dashboard «kg estimados».
- **5. Por qué existe:** Nunca se suma como reciclaje real.
- **6. Tipo de dato:** NUMBER(9,4) (9 dígitos en total, 4 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL = sin estimación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_PESO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot (fn_peso_estimado_kg). INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 0,066
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_CONSULTA_PESO (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; presentar una estimación como si fuera una medición. Reglas CHECK: CK_CONSULTA_PESO
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (peso ESTIMADO (no medido) en kg). Nunca se suma como reciclaje real.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_PESO y por qué esos y no otros? · ¿En qué se diferencia un peso estimado de uno medido? · ¿Qué significa que PESO_ESTIMADO_KG esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PESO_ESTIMADO_KG) AS con_valor, MIN(PESO_ESTIMADO_KG) AS minimo, MAX(PESO_ESTIMADO_KG) AS maximo, SUM(PESO_ESTIMADO_KG) AS total FROM CONSULTA_VISION;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. PESO_ESTIMADO_KG es la casilla que guarda: Peso ESTIMADO (no medido) en kg. Puede quedar vacía: NULL = sin estimación.
- **Explicación ampliada:** Es el peso que la IA estima al mirar la foto. Es solo orientación: una foto consultada NO es un depósito y este peso nunca se suma como reciclaje real.

#### 89. `CONSULTA_VISION.ADMISION`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ADMISION
- **3. Qué significa:** 1 Sin caneca, 2 Admitido, 3 No admitido, 4 Caneca no disponible.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: 1 Sin caneca, 2 Admitido, 3 No admitido, 4 Caneca no disponible. Se usa en: Orientación.
- **5. Por qué existe:** Igual que AdmisionMaterial de P3.
- **6. Tipo de dato:** NUMBER(2) (entero de hasta 2 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no se evaluó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_ADMISION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_ADMISION: admision IS NULL OR admision IN (1, 2, 3, 4) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_CONSULTA_ADMISION
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (1 Sin caneca, 2 Admitido, 3 No admitido, 4 Caneca no disponible). Igual que AdmisionMaterial de P3.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_ADMISION y por qué esos y no otros? · ¿Qué significa que ADMISION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT ADMISION, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY ADMISION ORDER BY ADMISION;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ADMISION es la casilla que guarda: 1 Sin caneca, 2 Admitido, 3 No admitido, 4 Caneca no disponible. Puede quedar vacía: NULL = no se evaluó.

#### 90. `CONSULTA_VISION.ID_CONTENEDOR_SUGERIDO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ID_CONTENEDOR_SUGERIDO
- **3. Qué significa:** Caneca que se le indicó al ciudadano.
- **4. Para qué sirve:** Sirve para saber a qué fila de CONTENEDOR pertenece cada fila de CONSULTA_VISION y poder unirlas con JOIN. Se usa en: Reportes de orientación.
- **5. Por qué existe:** Saber a qué caneca se envió a la persona.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = ninguna (peligroso o no identificado).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_CONSULTA_CONTENEDOR->CONTENEDOR
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_CONSULTA_CONTENEDOR)
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 11
- **15. Ejemplo de valor incorrecto:** Un número que no exista en CONTENEDOR (ORA-02291)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) CONTENEDOR que no existe). Integridad referencial con CONTENEDOR
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre CONSULTA_VISION y CONTENEDOR: no sabríamos a qué CONTENEDOR pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CONTENEDOR_SUGERIDO que no existe en CONTENEDOR? (respuesta: ORA-02291) · ¿Qué significa que ID_CONTENEDOR_SUGERIDO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_CONTENEDOR_SUGERIDO, COUNT(*) AS filas FROM CONSULTA_VISION t JOIN CONTENEDOR r ON r.ID_CONTENEDOR = t.ID_CONTENEDOR_SUGERIDO GROUP BY t.ID_CONTENEDOR_SUGERIDO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ID_CONTENEDOR_SUGERIDO es la casilla que guarda: Caneca que se le indicó al ciudadano. Es un «puntero»: copia el número de una fila de CONTENEDOR para unir las dos tablas. Puede quedar vacía: NULL = ninguna (peligroso o no identificado).

#### 91. `CONSULTA_VISION.LATENCIA_MS`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** LATENCIA_MS
- **3. Qué significa:** Milisegundos que tardó el análisis.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: milisegundos que tardó el análisis. Se usa en: Rendimiento.
- **5. Por qué existe:** Medir la experiencia del usuario.
- **6. Tipo de dato:** NUMBER(7) (entero de hasta 7 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_LATENCIA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1850
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_LATENCIA: latencia_ms >= 0 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_CONSULTA_LATENCIA
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (milisegundos que tardó el análisis). Medir la experiencia del usuario.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_LATENCIA y por qué esos y no otros? · ¿Por qué LATENCIA_MS es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT LATENCIA_MS, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY LATENCIA_MS ORDER BY LATENCIA_MS;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. LATENCIA_MS es la casilla que guarda: Milisegundos que tardó el análisis. Nunca puede quedar vacía.

#### 92. `CONSULTA_VISION.FECHA_UTC`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** FECHA_UTC
- **3. Qué significa:** Cuándo se analizó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: cuándo se analizó. Se usa en: Filtros por fecha.
- **5. Por qué existe:** Reportes diarios.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Bot. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:20
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (cuándo se analizó). Reportes diarios.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_UTC) AS primera, MAX(FECHA_UTC) AS ultima, COUNT(FECHA_UTC) AS con_valor FROM CONSULTA_VISION;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. FECHA_UTC es la casilla que guarda: Cuándo se analizó. Nunca puede quedar vacía.

#### 93. `CONSULTA_VISION.ES_SINTETICO`

- **1. Tabla:** CONSULTA_VISION — Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.
- **2. Atributo:** ES_SINTETICO
- **3. Qué significa:** 1 si es simulada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de CONSULTA_VISION: 1 si es simulada. Se usa en: Rótulo DATOS SINTÉTICOS.
- **5. Por qué existe:** No confundir simulación.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_CONSULTA_SINTETICO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Análisis de la foto por la IA Eco (solo orientación)
- **12. Quién lo llena:** Script 11. INSERT/UPDATE directo sobre CONSULTA_VISION (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y Power BI. SELECT en las vistas VW_BI_CONSULTAS_VISION, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_CONSULTA_SINTETICO: es_sintetico IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); mezclar datos de demostración con datos del sistema. Reglas CHECK: CK_CONSULTA_SINTETICO
- **17. Qué pasaría si no existiera:** La tabla CONSULTA_VISION no podría guardar este dato (1 si es simulada). No confundir simulación.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_CONSULTAS_VISION, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Cada foto enviada a Eco queda aquí.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_CONSULTAS_VISION (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_CONSULTA_SINTETICO y por qué esos y no otros? · ¿Por qué ES_SINTETICO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ES_SINTETICO, COUNT(*) AS filas FROM CONSULTA_VISION GROUP BY ES_SINTETICO ORDER BY ES_SINTETICO;
- **24. Explicación sencilla (para quien empieza):** Piensa en CONSULTA_VISION como una hoja de cálculo donde cada fila es una foto analizada por la IA. ES_SINTETICO es la casilla que guarda: 1 si es simulada. Nunca puede quedar vacía.

### ORDEN_APERTURA (8 atributos)

Orden idempotente de abrir una caneca; máximo una vigente por caneca.

#### 94. `ORDEN_APERTURA.ID_ORDEN`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** ID_ORDEN
- **3. Qué significa:** Número de la orden de abrir.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de ORDEN_APERTURA, de modo que otras tablas puedan apuntarle. Se usa en: Eventos y depósito.
- **5. Por qué existe:** Abrir es un hecho distinto de depositar.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; UNIQUE: UQ_ORDEN_CONTENEDOR; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: EVENTO_DISPOSITIVO
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 40
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de ORDEN_APERTURA, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_ORDEN es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM ORDEN_APERTURA WHERE ID_ORDEN = (SELECT MIN(ID_ORDEN) FROM ORDEN_APERTURA);
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. ID_ORDEN es la casilla que guarda: Número de la orden de abrir. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 95. `ORDEN_APERTURA.ID_INTERACCION`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** ID_INTERACCION
- **3. Qué significa:** Sesión que pidió abrir.
- **4. Para qué sirve:** Sirve para saber a qué fila de INTERACCION pertenece cada fila de ORDEN_APERTURA y poder unirlas con JOIN. Se usa en: Trazabilidad.
- **5. Por qué existe:** Una orden por sesión.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL; UNIQUE.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_ORDEN_INTERACCION->INTERACCION; UNIQUE: UQ_ORDEN_INTERACCION
- **10. Con qué tabla se relaciona:** Apunta a INTERACCION (FK FK_ORDEN_INTERACCION)
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 57
- **15. Ejemplo de valor incorrecto:** Un número que no exista en INTERACCION (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_ORDEN_INTERACCION) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) INTERACCION que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con INTERACCION · Unicidad: UQ_ORDEN_INTERACCION · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre ORDEN_APERTURA y INTERACCION: no sabríamos a qué INTERACCION pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_INTERACCION que no existe en INTERACCION? (respuesta: ORA-02291) · ¿Por qué ID_INTERACCION no puede repetirse? (UNIQUE UQ_ORDEN_INTERACCION) · ¿Por qué ID_INTERACCION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_INTERACCION, COUNT(*) AS filas FROM ORDEN_APERTURA t JOIN INTERACCION r ON r.ID_INTERACCION = t.ID_INTERACCION GROUP BY t.ID_INTERACCION ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. ID_INTERACCION es la casilla que guarda: Sesión que pidió abrir. Es un «puntero»: copia el número de una fila de INTERACCION para unir las dos tablas. Nunca puede quedar vacía.

#### 96. `ORDEN_APERTURA.ID_CONTENEDOR`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Caneca que debe abrirse.
- **4. Para qué sirve:** Sirve para saber a qué fila de INTERACCION pertenece cada fila de ORDEN_APERTURA y poder unirlas con JOIN. Se usa en: Índice de orden vigente.
- **5. Por qué existe:** FK compuesta: es la misma caneca de la sesión.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_ORDEN_CONTENEDOR->CONTENEDOR, FK_ORDEN_INTERACCION->INTERACCION; UNIQUE: UQ_ORDEN_CONTENEDOR
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_ORDEN_CONTENEDOR) · Apunta a INTERACCION (FK FK_ORDEN_INTERACCION)
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 11
- **15. Ejemplo de valor incorrecto:** Un número que no exista en INTERACCION (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_ORDEN_CONTENEDOR) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) INTERACCION que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con INTERACCION · Unicidad: UQ_ORDEN_CONTENEDOR · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre ORDEN_APERTURA y INTERACCION: no sabríamos a qué INTERACCION pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CONTENEDOR que no existe en INTERACCION? (respuesta: ORA-02291) · ¿Por qué ID_CONTENEDOR no puede repetirse? (UNIQUE UQ_ORDEN_CONTENEDOR) · ¿Por qué ID_CONTENEDOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_CONTENEDOR, COUNT(*) AS filas FROM ORDEN_APERTURA t JOIN INTERACCION r ON r.ID_INTERACCION = t.ID_CONTENEDOR GROUP BY t.ID_CONTENEDOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. ID_CONTENEDOR es la casilla que guarda: Caneca que debe abrirse. Es un «puntero»: copia el número de una fila de INTERACCION para unir las dos tablas. Nunca puede quedar vacía.

#### 97. `ORDEN_APERTURA.CLAVE_IDEMPOTENCIA`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** CLAVE_IDEMPOTENCIA
- **3. Qué significa:** Clave del pedido; repetirla devuelve la misma orden.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ORDEN_APERTURA: clave del pedido; repetirla devuelve la misma orden. Se usa en: Evitar dobles aperturas.
- **5. Por qué existe:** Un reintento de red no abre dos veces.
- **6. Tipo de dato:** VARCHAR2(64 CHAR) (hasta 64 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL; UNIQUE.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_ORDEN_CLAVE; CHECK CK_ORDEN_CLAVE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** SIM-ORD-0057
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_ORDEN_CLAVE: LENGTH(clave_idempotencia) >= 8 AND clave_idempotencia = TRIM(clave_idempotencia) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_ORDEN_CLAVE) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); procesar dos veces la misma operación. Reglas CHECK: CK_ORDEN_CLAVE · Unicidad: UQ_ORDEN_CLAVE
- **17. Qué pasaría si no existiera:** La tabla ORDEN_APERTURA no podría guardar este dato (clave del pedido; repetirla devuelve la misma orden). Un reintento de red no abre dos veces.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ORDEN_CLAVE y por qué esos y no otros? · ¿Por qué CLAVE_IDEMPOTENCIA no puede repetirse? (UNIQUE UQ_ORDEN_CLAVE) · ¿Por qué CLAVE_IDEMPOTENCIA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CLAVE_IDEMPOTENCIA, COUNT(*) AS filas FROM ORDEN_APERTURA GROUP BY CLAVE_IDEMPOTENCIA ORDER BY CLAVE_IDEMPOTENCIA;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. CLAVE_IDEMPOTENCIA es la casilla que guarda: Clave del pedido; repetirla devuelve la misma orden. Nunca puede quedar vacía.

#### 98. `ORDEN_APERTURA.ESTADO`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** ESTADO
- **3. Qué significa:** 1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ORDEN_APERTURA: 1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida. Se usa en: Índice único de orden vigente.
- **5. Por qué existe:** Solo UNA orden vigente por caneca (concurrencia).
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 1).
- **8. Valor por defecto:** 1
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_ORDEN_CIERRE; CHECK CK_ORDEN_ESTADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio / dispositivo. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT en las vistas VW_BI_DEPOSITOS, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 6
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_ORDEN_CIERRE (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_ORDEN_ESTADO: estado BETWEEN 1 AND 8 (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_ORDEN_CIERRE, CK_ORDEN_ESTADO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla ORDEN_APERTURA no podría guardar este dato (1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida). Solo UNA orden vigente por caneca (concurrencia).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ORDEN_CIERRE y por qué esos y no otros? · ¿Por qué ESTADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO, COUNT(*) AS filas FROM ORDEN_APERTURA GROUP BY ESTADO ORDER BY ESTADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. ESTADO es la casilla que guarda: 1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida. Nunca puede quedar vacía.

#### 99. `ORDEN_APERTURA.FECHA_EMISION_UTC`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** FECHA_EMISION_UTC
- **3. Qué significa:** Cuándo se emitió.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ORDEN_APERTURA: cuándo se emitió. Se usa en: Ventana de 10 s.
- **5. Por qué existe:** Medir tiempos.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_ORDEN_CIERRE; CHECK CK_ORDEN_VENCIMIENTO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:20:30
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_ORDEN_CIERRE (condición completa en el anexo de CHECK) (ORA-02290) · Un valor que rompa CK_ORDEN_VENCIMIENTO: fecha_vencimiento_utc > fecha_emision_utc (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_ORDEN_CIERRE, CK_ORDEN_VENCIMIENTO
- **17. Qué pasaría si no existiera:** La tabla ORDEN_APERTURA no podría guardar este dato (cuándo se emitió). Medir tiempos.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ORDEN_CIERRE y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_EMISION_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_EMISION_UTC) AS primera, MAX(FECHA_EMISION_UTC) AS ultima, COUNT(FECHA_EMISION_UTC) AS con_valor FROM ORDEN_APERTURA;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. FECHA_EMISION_UTC es la casilla que guarda: Cuándo se emitió. Nunca puede quedar vacía.

#### 100. `ORDEN_APERTURA.FECHA_VENCIMIENTO_UTC`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** FECHA_VENCIMIENTO_UTC
- **3. Qué significa:** Después de esta hora la orden ya no vale.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ORDEN_APERTURA: después de esta hora la orden ya no vale. Se usa en: Vencer órdenes.
- **5. Por qué existe:** CHECK: vence después de emitirse.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_ORDEN_VENCIMIENTO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:22:30
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_ORDEN_VENCIMIENTO: fecha_vencimiento_utc > fecha_emision_utc (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_ORDEN_VENCIMIENTO
- **17. Qué pasaría si no existiera:** La tabla ORDEN_APERTURA no podría guardar este dato (después de esta hora la orden ya no vale). CHECK: vence después de emitirse.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ORDEN_VENCIMIENTO y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_VENCIMIENTO_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_VENCIMIENTO_UTC) AS primera, MAX(FECHA_VENCIMIENTO_UTC) AS ultima, COUNT(FECHA_VENCIMIENTO_UTC) AS con_valor FROM ORDEN_APERTURA;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. FECHA_VENCIMIENTO_UTC es la casilla que guarda: Después de esta hora la orden ya no vale. Nunca puede quedar vacía.

#### 101. `ORDEN_APERTURA.FECHA_CIERRE_UTC`

- **1. Tabla:** ORDEN_APERTURA — Orden idempotente de abrir una caneca; máximo una vigente por caneca.
- **2. Atributo:** FECHA_CIERRE_UTC
- **3. Qué significa:** Cuándo terminó la orden.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de ORDEN_APERTURA: cuándo terminó la orden. Se usa en: Duración.
- **5. Por qué existe:** CHECK coherente con el estado.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = sigue vigente.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_ORDEN_CIERRE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Orden de abrir la caneca
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre ORDEN_APERTURA (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Servicio de la caneca y operario. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:21:14
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_ORDEN_CIERRE (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_ORDEN_CIERRE
- **17. Qué pasaría si no existiera:** La tabla ORDEN_APERTURA no podría guardar este dato (cuándo terminó la orden). CHECK coherente con el estado.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (25 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** Se emite cuando la persona confirma «abrir caneca».
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_ORDEN_CIERRE y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_CIERRE_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_CIERRE_UTC) AS primera, MAX(FECHA_CIERRE_UTC) AS ultima, COUNT(FECHA_CIERRE_UTC) AS con_valor FROM ORDEN_APERTURA;
- **24. Explicación sencilla (para quien empieza):** Piensa en ORDEN_APERTURA como una hoja de cálculo donde cada fila es una orden de abrir una caneca. FECHA_CIERRE_UTC es la casilla que guarda: Cuándo terminó la orden. Puede quedar vacía: NULL = sigue vigente.

### EVENTO_DISPOSITIVO (8 atributos)

Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).

#### 102. `EVENTO_DISPOSITIVO.ID_EVENTO`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** ID_EVENTO
- **3. Qué significa:** Número del evento.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de EVENTO_DISPOSITIVO, de modo que otras tablas puedan apuntarle. Se usa en: Depósito.
- **5. Por qué existe:** Cada mensaje de la caneca se guarda.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; UNIQUE: UQ_EVENTO_ORDEN; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: DEPOSITO
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 300
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de EVENTO_DISPOSITIVO, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_EVENTO es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM EVENTO_DISPOSITIVO WHERE ID_EVENTO = (SELECT MIN(ID_EVENTO) FROM EVENTO_DISPOSITIVO);
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. ID_EVENTO es la casilla que guarda: Número del evento. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 103. `EVENTO_DISPOSITIVO.ID_CONTENEDOR`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Caneca que informó.
- **4. Para qué sirve:** Sirve para saber a qué fila de ORDEN_APERTURA pertenece cada fila de EVENTO_DISPOSITIVO y poder unirlas con JOIN. Se usa en: Trazabilidad.
- **5. Por qué existe:** Saber qué caneca habló.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_EVENTO_CONTENEDOR->CONTENEDOR, FK_EVENTO_ORDEN->ORDEN_APERTURA; UNIQUE: UQ_EVENTO_EXTERNO
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_EVENTO_CONTENEDOR) · Apunta a ORDEN_APERTURA (FK FK_EVENTO_ORDEN)
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** Adaptador del ESP32. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 11
- **15. Ejemplo de valor incorrecto:** Un número que no exista en ORDEN_APERTURA (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_EVENTO_EXTERNO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) ORDEN_APERTURA que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con ORDEN_APERTURA · Unicidad: UQ_EVENTO_EXTERNO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre EVENTO_DISPOSITIVO y ORDEN_APERTURA: no sabríamos a qué ORDEN_APERTURA pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CONTENEDOR que no existe en ORDEN_APERTURA? (respuesta: ORA-02291) · ¿Por qué ID_CONTENEDOR no puede repetirse? (UNIQUE UQ_EVENTO_EXTERNO) · ¿Por qué ID_CONTENEDOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_CONTENEDOR, COUNT(*) AS filas FROM EVENTO_DISPOSITIVO t JOIN ORDEN_APERTURA r ON r.ID_ORDEN = t.ID_CONTENEDOR GROUP BY t.ID_CONTENEDOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. ID_CONTENEDOR es la casilla que guarda: Caneca que informó. Es un «puntero»: copia el número de una fila de ORDEN_APERTURA para unir las dos tablas. Nunca puede quedar vacía.

#### 104. `EVENTO_DISPOSITIVO.ID_EXTERNO`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** ID_EXTERNO
- **3. Qué significa:** Id que pone el dispositivo.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de EVENTO_DISPOSITIVO: id que pone el dispositivo. Se usa en: Detectar retransmisiones.
- **5. Por qué existe:** UNIQUE por caneca: un mensaje repetido no se cuenta dos veces.
- **6. Tipo de dato:** VARCHAR2(64 CHAR) (hasta 64 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_EVENTO_EXTERNO; CHECK CK_EVENTO_EXTERNO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** ESP32 / simulador. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** SIM-57-DETECCION
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_EVENTO_EXTERNO: TRIM(id_externo) IS NOT NULL AND id_externo = TRIM(id_externo) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_EVENTO_EXTERNO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores duplicados; valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_EVENTO_EXTERNO · Unicidad: UQ_EVENTO_EXTERNO
- **17. Qué pasaría si no existiera:** La tabla EVENTO_DISPOSITIVO no podría guardar este dato (id que pone el dispositivo). UNIQUE por caneca: un mensaje repetido no se cuenta dos veces.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_EVENTO_EXTERNO y por qué esos y no otros? · ¿Por qué ID_EXTERNO no puede repetirse? (UNIQUE UQ_EVENTO_EXTERNO) · ¿Por qué ID_EXTERNO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ID_EXTERNO, COUNT(*) AS filas FROM EVENTO_DISPOSITIVO GROUP BY ID_EXTERNO ORDER BY ID_EXTERNO;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. ID_EXTERNO es la casilla que guarda: Id que pone el dispositivo. Nunca puede quedar vacía.

#### 105. `EVENTO_DISPOSITIVO.TIPO`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** TIPO
- **3. Qué significa:** 1 Acuse, 2 Apertura, 3 Detección, 4 Cierre, 5 Fallo, 6 Cierre sin sensor.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de EVENTO_DISPOSITIVO: 1 Acuse, 2 Apertura, 3 Detección, 4 Cierre, 5 Fallo, 6 Cierre sin sensor. Se usa en: Evidencia física.
- **5. Por qué existe:** Separa «se abrió» de «algo entró».
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_EVENTO_TIPO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** ESP32 / simulador. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT en las vistas VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_EVENTO_TIPO: tipo BETWEEN 1 AND 6 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_EVENTO_TIPO · Vigilada por: TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla EVENTO_DISPOSITIVO no podría guardar este dato (1 Acuse, 2 Apertura, 3 Detección, 4 Cierre, 5 Fallo, 6 Cierre sin sensor). Separa «se abrió» de «algo entró».
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_EVENTO_TIPO y por qué esos y no otros? · ¿Por qué TIPO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT TIPO, COUNT(*) AS filas FROM EVENTO_DISPOSITIVO GROUP BY TIPO ORDER BY TIPO;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. TIPO es la casilla que guarda: 1 Acuse, 2 Apertura, 3 Detección, 4 Cierre, 5 Fallo, 6 Cierre sin sensor. Nunca puede quedar vacía.

#### 106. `EVENTO_DISPOSITIVO.ID_ORDEN`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** ID_ORDEN
- **3. Qué significa:** Orden a la que pertenece.
- **4. Para qué sirve:** Sirve para saber a qué fila de ORDEN_APERTURA pertenece cada fila de EVENTO_DISPOSITIVO y poder unirlas con JOIN. Se usa en: Depósito.
- **5. Por qué existe:** CHECK: un evento aplicado siempre tiene orden.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = evento en revisión sin orden.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_EVENTO_ORDEN->ORDEN_APERTURA; UNIQUE: UQ_EVENTO_ORDEN; CHECK CK_EVENTO_APLICADO
- **10. Con qué tabla se relaciona:** Apunta a ORDEN_APERTURA (FK FK_EVENTO_ORDEN)
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** Adaptador. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT en las vistas VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 40
- **15. Ejemplo de valor incorrecto:** Un número que no exista en ORDEN_APERTURA (ORA-02291) · Un valor que rompa CK_EVENTO_APLICADO: resultado = 2 OR id_orden IS NOT NULL (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_EVENTO_ORDEN)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) ORDEN_APERTURA que no existe); valores duplicados; valores fuera de las opciones permitidas. Integridad referencial con ORDEN_APERTURA · Reglas CHECK: CK_EVENTO_APLICADO · Unicidad: UQ_EVENTO_ORDEN · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre EVENTO_DISPOSITIVO y ORDEN_APERTURA: no sabríamos a qué ORDEN_APERTURA pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_ORDEN que no existe en ORDEN_APERTURA? (respuesta: ORA-02291) · ¿Qué valores permite CK_EVENTO_APLICADO y por qué esos y no otros? · ¿Por qué ID_ORDEN no puede repetirse? (UNIQUE UQ_EVENTO_ORDEN)
- **23. SELECT para verlo:** SELECT t.ID_ORDEN, COUNT(*) AS filas FROM EVENTO_DISPOSITIVO t JOIN ORDEN_APERTURA r ON r.ID_ORDEN = t.ID_ORDEN GROUP BY t.ID_ORDEN ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. ID_ORDEN es la casilla que guarda: Orden a la que pertenece. Es un «puntero»: copia el número de una fila de ORDEN_APERTURA para unir las dos tablas. Puede quedar vacía: NULL = evento en revisión sin orden.

#### 107. `EVENTO_DISPOSITIVO.RESULTADO`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** RESULTADO
- **3. Qué significa:** 1 Aplicado, 2 Revisión (tardío o sin orden).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de EVENTO_DISPOSITIVO: 1 Aplicado, 2 Revisión (tardío o sin orden). Se usa en: Diagnóstico.
- **5. Por qué existe:** Los eventos dudosos no crean depósitos.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_EVENTO_APLICADO; CHECK CK_EVENTO_RESULTADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** Adaptador. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_EVENTO_APLICADO: resultado = 2 OR id_orden IS NOT NULL (ORA-02290) · Un valor que rompa CK_EVENTO_RESULTADO: resultado IN (1, 2) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_EVENTO_APLICADO, CK_EVENTO_RESULTADO
- **17. Qué pasaría si no existiera:** La tabla EVENTO_DISPOSITIVO no podría guardar este dato (1 Aplicado, 2 Revisión (tardío o sin orden)). Los eventos dudosos no crean depósitos.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_EVENTO_APLICADO y por qué esos y no otros? · ¿Por qué RESULTADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT RESULTADO, COUNT(*) AS filas FROM EVENTO_DISPOSITIVO GROUP BY RESULTADO ORDER BY RESULTADO;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. RESULTADO es la casilla que guarda: 1 Aplicado, 2 Revisión (tardío o sin orden). Nunca puede quedar vacía.

#### 108. `EVENTO_DISPOSITIVO.FECHA_DISPOSITIVO_UTC`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** FECHA_DISPOSITIVO_UTC
- **3. Qué significa:** Hora según el reloj de la caneca.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de EVENTO_DISPOSITIVO: hora según el reloj de la caneca. Se usa en: Diagnóstico de reloj.
- **5. Por qué existe:** El reloj del ESP32 puede no ser fiable.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = el dispositivo no la envió.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** ESP32. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:21:10
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla EVENTO_DISPOSITIVO no podría guardar este dato (hora según el reloj de la caneca). El reloj del ESP32 puede no ser fiable.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_DISPOSITIVO_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_DISPOSITIVO_UTC) AS primera, MAX(FECHA_DISPOSITIVO_UTC) AS ultima, COUNT(FECHA_DISPOSITIVO_UTC) AS con_valor FROM EVENTO_DISPOSITIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. FECHA_DISPOSITIVO_UTC es la casilla que guarda: Hora según el reloj de la caneca. Puede quedar vacía: NULL = el dispositivo no la envió.

#### 109. `EVENTO_DISPOSITIVO.FECHA_RECEPCION_UTC`

- **1. Tabla:** EVENTO_DISPOSITIVO — Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).
- **2. Atributo:** FECHA_RECEPCION_UTC
- **3. Qué significa:** Hora del servidor al recibir.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de EVENTO_DISPOSITIVO: hora del servidor al recibir. Se usa en: Ventana de la orden.
- **5. Por qué existe:** La hora que cuenta es la del servidor.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Avisos de la caneca: abrió, detectó, cerró, falló
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre EVENTO_DISPOSITIVO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario y administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:21:10
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla EVENTO_DISPOSITIVO no podría guardar este dato (hora del servidor al recibir). La hora que cuenta es la del servidor.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (13 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot avisa apertura y cierre de la caneca.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_RECEPCION_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_RECEPCION_UTC) AS primera, MAX(FECHA_RECEPCION_UTC) AS ultima, COUNT(FECHA_RECEPCION_UTC) AS con_valor FROM EVENTO_DISPOSITIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en EVENTO_DISPOSITIVO como una hoja de cálculo donde cada fila es un aviso enviado por la caneca. FECHA_RECEPCION_UTC es la casilla que guarda: Hora del servidor al recibir. Nunca puede quedar vacía.

### DEPOSITO (18 atributos)

Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.

#### 110. `DEPOSITO.ID_DEPOSITO`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_DEPOSITO
- **3. Qué significa:** Número del depósito.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de DEPOSITO, de modo que otras tablas puedan apuntarle. Se usa en: Verificación, puntos, auditoría.
- **5. Por qué existe:** Identificar cada residuo depositado.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: AUDITORIA_DEPOSITO, MOVIMIENTO_ECOPUNTOS
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 25
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_BLOQUEO_CERRADO
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de DEPOSITO, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_BLOQUEO_CERRADO, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_DEPOSITO es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM DEPOSITO WHERE ID_DEPOSITO = (SELECT MIN(ID_DEPOSITO) FROM DEPOSITO);
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_DEPOSITO es la casilla que guarda: Número del depósito. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.
- **Explicación ampliada:** Es el número de cada residuo que entró (o se declaró) en una caneca. Es el centro de la auditoría y de los EcoPuntos: AUDITORIA_DEPOSITO y MOVIMIENTO_ECOPUNTOS apuntan a este número.

#### 111. `DEPOSITO.ID_ORDEN`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_ORDEN
- **3. Qué significa:** Orden que lo originó.
- **4. Para qué sirve:** Sirve para saber a qué fila de EVENTO_DISPOSITIVO pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Titular y caneca (por la orden).
- **5. Por qué existe:** Un depósito por orden.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL; UNIQUE.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_EVENTO->EVENTO_DISPOSITIVO; UNIQUE: UQ_DEPOSITO_ORDEN
- **10. Con qué tabla se relaciona:** Apunta a EVENTO_DISPOSITIVO (FK FK_DEPOSITO_EVENTO)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 40
- **15. Ejemplo de valor incorrecto:** Un número que no exista en EVENTO_DISPOSITIVO (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_DEPOSITO_ORDEN) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) EVENTO_DISPOSITIVO que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con EVENTO_DISPOSITIVO · Unicidad: UQ_DEPOSITO_ORDEN · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y EVENTO_DISPOSITIVO: no sabríamos a qué EVENTO_DISPOSITIVO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_ORDEN que no existe en EVENTO_DISPOSITIVO? (respuesta: ORA-02291) · ¿Por qué ID_ORDEN no puede repetirse? (UNIQUE UQ_DEPOSITO_ORDEN) · ¿Por qué ID_ORDEN es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_ORDEN, COUNT(*) AS filas FROM DEPOSITO t JOIN EVENTO_DISPOSITIVO r ON r.ID_EVENTO = t.ID_ORDEN GROUP BY t.ID_ORDEN ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_ORDEN es la casilla que guarda: Orden que lo originó. Es un «puntero»: copia el número de una fila de EVENTO_DISPOSITIVO para unir las dos tablas. Nunca puede quedar vacía.

#### 112. `DEPOSITO.ID_EVENTO`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_EVENTO
- **3. Qué significa:** Evento de detección o cierre sin sensor.
- **4. Para qué sirve:** Sirve para saber a qué fila de EVENTO_DISPOSITIVO pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Evidencia física.
- **5. Por qué existe:** FK compuesta: el evento es de ESA orden.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL; UNIQUE.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_EVENTO->EVENTO_DISPOSITIVO; UNIQUE: UQ_DEPOSITO_EVENTO
- **10. Con qué tabla se relaciona:** Apunta a EVENTO_DISPOSITIVO (FK FK_DEPOSITO_EVENTO)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 300
- **15. Ejemplo de valor incorrecto:** Un número que no exista en EVENTO_DISPOSITIVO (ORA-02291) · Repetir un valor ya usado (ORA-00001 por UQ_DEPOSITO_EVENTO) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) EVENTO_DISPOSITIVO que no existe); valores duplicados; filas incompletas (no se puede guardar sin este dato). Integridad referencial con EVENTO_DISPOSITIVO · Unicidad: UQ_DEPOSITO_EVENTO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y EVENTO_DISPOSITIVO: no sabríamos a qué EVENTO_DISPOSITIVO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_EVENTO que no existe en EVENTO_DISPOSITIVO? (respuesta: ORA-02291) · ¿Por qué ID_EVENTO no puede repetirse? (UNIQUE UQ_DEPOSITO_EVENTO) · ¿Por qué ID_EVENTO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_EVENTO, COUNT(*) AS filas FROM DEPOSITO t JOIN EVENTO_DISPOSITIVO r ON r.ID_EVENTO = t.ID_EVENTO GROUP BY t.ID_EVENTO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_EVENTO es la casilla que guarda: Evento de detección o cierre sin sensor. Es un «puntero»: copia el número de una fila de EVENTO_DISPOSITIVO para unir las dos tablas. Nunca puede quedar vacía.

#### 113. `DEPOSITO.SIN_SENSOR`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** SIN_SENSOR
- **3. Qué significa:** 1 si no hubo sensor (depósito DECLARADO); 0 si el sensor lo detectó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: 1 si no hubo sensor (depósito DECLARADO); 0 si el sensor lo detectó. Se usa en: Separar declarado de detectado.
- **5. Por qué existe:** Igual que P3 V016: QR + foto + 10 s no prueban el ingreso.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_SIN_SENSOR
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_DEPOSITO_SIN_SENSOR: sin_sensor IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_DEPOSITO_SIN_SENSOR · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (1 si no hubo sensor (depósito DECLARADO); 0 si el sensor lo detectó). Igual que P3 V016: QR + foto + 10 s no prueban el ingreso.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_SIN_SENSOR y por qué esos y no otros? · ¿Por qué SIN_SENSOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT SIN_SENSOR, COUNT(*) AS filas FROM DEPOSITO GROUP BY SIN_SENSOR ORDER BY SIN_SENSOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. SIN_SENSOR es la casilla que guarda: 1 si no hubo sensor (depósito DECLARADO); 0 si el sensor lo detectó. Nunca puede quedar vacía.
- **Explicación ampliada:** Vale 1 cuando la caneca no tenía sensor y el depósito fue DECLARADO por la persona; 0 cuando el sensor lo detectó. Sirve para no tratar igual un depósito comprobado por hardware y uno solo declarado.

#### 114. `DEPOSITO.ID_MATERIAL_DECLARADO`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_MATERIAL_DECLARADO
- **3. Qué significa:** Material confirmado por la persona.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Comparar con lo verificado.
- **5. Por qué existe:** Trigger 2: la caneca debe admitirlo.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_MAT_DECL->MATERIAL
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_DEPOSITO_MAT_DECL)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con MATERIAL · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL_DECLARADO que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Por qué ID_MATERIAL_DECLARADO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL_DECLARADO, COUNT(*) AS filas FROM DEPOSITO t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL_DECLARADO GROUP BY t.ID_MATERIAL_DECLARADO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_MATERIAL_DECLARADO es la casilla que guarda: Material confirmado por la persona. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Nunca puede quedar vacía.

#### 115. `DEPOSITO.CANTIDAD_DECLARADA`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** CANTIDAD_DECLARADA
- **3. Qué significa:** Unidades declaradas.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: unidades declaradas. Se usa en: Estimación.
- **5. Por qué existe:** No es conteo verificado.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no declaradas.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_CANT_DECL
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_DEPOSITO_CANT_DECL: cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000 (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_DEPOSITO_CANT_DECL · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (unidades declaradas). No es conteo verificado.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_CANT_DECL y por qué esos y no otros? · ¿Qué significa que CANTIDAD_DECLARADA esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT CANTIDAD_DECLARADA, COUNT(*) AS filas FROM DEPOSITO GROUP BY CANTIDAD_DECLARADA ORDER BY CANTIDAD_DECLARADA;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. CANTIDAD_DECLARADA es la casilla que guarda: Unidades declaradas. Puede quedar vacía: NULL = no declaradas.

#### 116. `DEPOSITO.PESO_ESTIMADO_KG`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** PESO_ESTIMADO_KG
- **3. Qué significa:** Peso estimado con el catálogo.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: peso estimado con el catálogo. Se usa en: Dashboard kg estimados.
- **5. Por qué existe:** Trigger 2: debe caber en el rango del catálogo.
- **6. Tipo de dato:** NUMBER(9,4) (9 dígitos en total, 4 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL = sin estimación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_ESTIMADO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 0,044
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_DEPOSITO_ESTIMADO: peso_estimado_kg IS NULL OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; presentar una estimación como si fuera una medición. Reglas CHECK: CK_DEPOSITO_ESTIMADO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (peso estimado con el catálogo). Trigger 2: debe caber en el rango del catálogo.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ESTADISTICAS, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_ESTIMADO y por qué esos y no otros? · ¿En qué se diferencia un peso estimado de uno medido? · ¿Qué significa que PESO_ESTIMADO_KG esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PESO_ESTIMADO_KG) AS con_valor, MIN(PESO_ESTIMADO_KG) AS minimo, MAX(PESO_ESTIMADO_KG) AS maximo, SUM(PESO_ESTIMADO_KG) AS total FROM DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. PESO_ESTIMADO_KG es la casilla que guarda: Peso estimado con el catálogo. Puede quedar vacía: NULL = sin estimación.
- **Explicación ampliada:** Es un peso APROXIMADO, calculado con la tabla PESO_REFERENCIAL (pesos típicos por objeto). No viene de una báscula. El disparador 2 revisa que quede dentro del rango del catálogo. En Power BI se muestra aparte como «kg estimados» y nunca se suma con los kilos medidos.

#### 117. `DEPOSITO.ID_PESO_REFERENCIAL`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_PESO_REFERENCIAL
- **3. Qué significa:** Fila del catálogo usada.
- **4. Para qué sirve:** Sirve para saber a qué fila de PESO_REFERENCIAL pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Explicar la estimación.
- **5. Por qué existe:** CHECK: no hay estimado sin catálogo.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = sin estimación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_PESO->PESO_REFERENCIAL; CHECK CK_DEPOSITO_ESTIMADO
- **10. Con qué tabla se relaciona:** Apunta a PESO_REFERENCIAL (FK FK_DEPOSITO_PESO)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio. INSERT/UPDATE directo sobre DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un número que no exista en PESO_REFERENCIAL (ORA-02291) · Un valor que rompa CK_DEPOSITO_ESTIMADO: peso_estimado_kg IS NULL OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) PESO_REFERENCIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con PESO_REFERENCIAL · Reglas CHECK: CK_DEPOSITO_ESTIMADO · Vigilada por: TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y PESO_REFERENCIAL: no sabríamos a qué PESO_REFERENCIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_PESO_REFERENCIAL que no existe en PESO_REFERENCIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_DEPOSITO_ESTIMADO y por qué esos y no otros? · ¿Qué significa que ID_PESO_REFERENCIAL esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_PESO_REFERENCIAL, COUNT(*) AS filas FROM DEPOSITO t JOIN PESO_REFERENCIAL r ON r.ID_PESO_REFERENCIAL = t.ID_PESO_REFERENCIAL GROUP BY t.ID_PESO_REFERENCIAL ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_PESO_REFERENCIAL es la casilla que guarda: Fila del catálogo usada. Es un «puntero»: copia el número de una fila de PESO_REFERENCIAL para unir las dos tablas. Puede quedar vacía: NULL = sin estimación.

#### 118. `DEPOSITO.ESTADO_VERIFICACION`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ESTADO_VERIFICACION
- **3. Qué significa:** 0 Pendiente, 1 Verificado, 2 Rechazado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: 0 Pendiente, 1 Verificado, 2 Rechazado. Se usa en: Puntos y reportes.
- **5. Por qué existe:** Solo los verificados cuentan como reciclaje y dan puntos.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_ESTADO; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario/administrador vía PKG_DEPOSITO. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_DEPOSITO_ESTADO: estado_verificacion IN (0, 1, 2) (ORA-02290) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_DEPOSITO_ESTADO, CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (0 Pendiente, 1 Verificado, 2 Rechazado). Solo los verificados cuentan como reciclaje y dan puntos.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE, VW_RECORRIDO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_ESTADO y por qué esos y no otros? · ¿Por qué ESTADO_VERIFICACION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_VERIFICACION, COUNT(*) AS filas FROM DEPOSITO GROUP BY ESTADO_VERIFICACION ORDER BY ESTADO_VERIFICACION;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ESTADO_VERIFICACION es la casilla que guarda: 0 Pendiente, 1 Verificado, 2 Rechazado. Nunca puede quedar vacía.
- **Explicación ampliada:** Dice en qué va la revisión del depósito: 0 Pendiente, 1 Verificado, 2 Rechazado. Nace en 0. Solo un operario o un administrador lo cambia, por medio del paquete PKG_DEPOSITO. Solo los depósitos en 1 cuentan como reciclaje y dan EcoPuntos. Cada cambio queda guardado por el disparador 3 en AUDITORIA_DEPOSITO con el valor anterior (:OLD) y el nuevo (:NEW).

#### 119. `DEPOSITO.ID_MATERIAL_VERIFICADO`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_MATERIAL_VERIFICADO
- **3. Qué significa:** Material que comprobó el operario.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Reportes verificados.
- **5. Por qué existe:** Puede diferir del declarado (mal separado).
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si pendiente o rechazado.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_MAT_VERIF->MATERIAL; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_DEPOSITO_MAT_VERIF)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); valores fuera de las opciones permitidas. Integridad referencial con MATERIAL · Reglas CHECK: CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL_VERIFICADO que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Qué valores permite CK_DEPOSITO_VERIFICACION y por qué esos y no otros? · ¿Qué significa que ID_MATERIAL_VERIFICADO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL_VERIFICADO, COUNT(*) AS filas FROM DEPOSITO t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL_VERIFICADO GROUP BY t.ID_MATERIAL_VERIFICADO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_MATERIAL_VERIFICADO es la casilla que guarda: Material que comprobó el operario. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Puede quedar vacía: NULL si pendiente o rechazado.

#### 120. `DEPOSITO.CANTIDAD_VERIFICADA`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** CANTIDAD_VERIFICADA
- **3. Qué significa:** Unidades contadas por el operario.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: unidades contadas por el operario. Se usa en: Puntos por unidad.
- **5. Por qué existe:** Los puntos por unidad usan este conteo.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = no contó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_CANT_VERIF; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_DEPOSITO_CANT_VERIF: cantidad_verificada IS NULL OR cantidad_verificada BETWEEN 1 AND 1000 (ORA-02290) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_DEPOSITO_CANT_VERIF, CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (unidades contadas por el operario). Los puntos por unidad usan este conteo.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_CANT_VERIF y por qué esos y no otros? · ¿Qué significa que CANTIDAD_VERIFICADA esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT CANTIDAD_VERIFICADA, COUNT(*) AS filas FROM DEPOSITO GROUP BY CANTIDAD_VERIFICADA ORDER BY CANTIDAD_VERIFICADA;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. CANTIDAD_VERIFICADA es la casilla que guarda: Unidades contadas por el operario. Puede quedar vacía: NULL = no contó.

#### 121. `DEPOSITO.ID_VERIFICADOR`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** ID_VERIFICADOR
- **3. Qué significa:** Quién verificó o rechazó.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de DEPOSITO y poder unirlas con JOIN. Se usa en: Auditoría.
- **5. Por qué existe:** Trigger 2: nunca el propio titular, siempre rol 2 o 3.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si pendiente.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_DEPOSITO_VERIFICADOR->USUARIO; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_DEPOSITO_VERIFICADOR)
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); valores fuera de las opciones permitidas. Integridad referencial con USUARIO · Reglas CHECK: CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre DEPOSITO y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_VERIFICADOR que no existe en USUARIO? (respuesta: ORA-02291) · ¿Qué valores permite CK_DEPOSITO_VERIFICACION y por qué esos y no otros? · ¿Qué significa que ID_VERIFICADOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_VERIFICADOR, COUNT(*) AS filas FROM DEPOSITO t JOIN USUARIO r ON r.ID_USUARIO = t.ID_VERIFICADOR GROUP BY t.ID_VERIFICADOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. ID_VERIFICADOR es la casilla que guarda: Quién verificó o rechazó. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Puede quedar vacía: NULL si pendiente.
- **Explicación ampliada:** Es el número (ID_USUARIO) de la persona que verificó o rechazó el depósito. Está vacío mientras el depósito está pendiente. El disparador 2 exige que sea un operario o un administrador y que no sea el mismo ciudadano dueño del depósito (nadie se aprueba a sí mismo). Si el profesor dice «USUARIO_VERIFICADOR», es esta columna.

#### 122. `DEPOSITO.FECHA_VERIFICACION_UTC`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** FECHA_VERIFICACION_UTC
- **3. Qué significa:** Cuándo se verificó.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: cuándo se verificó. Se usa en: Tiempos de respuesta.
- **5. Por qué existe:** CHECK: no antes de la detección.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL si pendiente.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** PKG_DEPOSITO. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-06 09:00
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_DEPOSITO_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (cuándo se verificó). CHECK: no antes de la detección.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_VERIFICACION y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que FECHA_VERIFICACION_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_VERIFICACION_UTC) AS primera, MAX(FECHA_VERIFICACION_UTC) AS ultima, COUNT(FECHA_VERIFICACION_UTC) AS con_valor FROM DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. FECHA_VERIFICACION_UTC es la casilla que guarda: Cuándo se verificó. Puede quedar vacía: NULL si pendiente.

#### 123. `DEPOSITO.MOTIVO_VERIFICACION`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** MOTIVO_VERIFICACION
- **3. Qué significa:** Explicación (rechazo, residuo mal separado, corrección).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: explicación (rechazo, residuo mal separado, corrección). Se usa en: Auditoría.
- **5. Por qué existe:** Obligatorio al rechazar y al verificar otro color.
- **6. Tipo de dato:** VARCHAR2(300 CHAR) (hasta 300 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL si no hace falta.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_MOTIVO; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Se encontró papel sucio en la caneca blanca
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_MOTIVO (condición completa en el anexo de CHECK) (ORA-02290) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290) · Un texto de más de 300 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_DEPOSITO_MOTIVO, CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (explicación (rechazo, residuo mal separado, corrección)). Obligatorio al rechazar y al verificar otro color.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, TRG_DEPOSITO_AUD_VERIFICACION, TRG_DEPOSITO_COHERENCIA
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_MOTIVO y por qué esos y no otros? · ¿Qué significa que MOTIVO_VERIFICACION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MOTIVO_VERIFICACION, COUNT(*) AS filas FROM DEPOSITO GROUP BY MOTIVO_VERIFICACION ORDER BY MOTIVO_VERIFICACION;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. MOTIVO_VERIFICACION es la casilla que guarda: Explicación (rechazo, residuo mal separado, corrección). Puede quedar vacía: NULL si no hace falta.

#### 124. `DEPOSITO.PESO_MEDIDO_KG`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** PESO_MEDIDO_KG
- **3. Qué significa:** Peso REAL medido con báscula.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: peso REAL medido con báscula. Se usa en: Dashboard kg medidos.
- **5. Por qué existe:** Nunca se mezcla con el estimado.
- **6. Tipo de dato:** NUMBER(9,4) (9 dígitos en total, 4 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL = no se midió.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_MEDIDO; CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 0,045
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_MEDIDO (condición completa en el anexo de CHECK) (ORA-02290) · Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; inventar pesos cuando no hubo báscula. Reglas CHECK: CK_DEPOSITO_MEDIDO, CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (peso REAL medido con báscula). Nunca se mezcla con el estimado.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_MEDIDO y por qué esos y no otros? · ¿Por qué no se inventa el peso cuando no hay báscula? (queda NULL) · ¿Qué significa que PESO_MEDIDO_KG esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PESO_MEDIDO_KG) AS con_valor, MIN(PESO_MEDIDO_KG) AS minimo, MAX(PESO_MEDIDO_KG) AS maximo, SUM(PESO_MEDIDO_KG) AS total FROM DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. PESO_MEDIDO_KG es la casilla que guarda: Peso REAL medido con báscula. Puede quedar vacía: NULL = no se midió.
- **Explicación ampliada:** Es el peso REAL, medido con una báscula por el operario. Si no hubo báscula, queda vacío (NULL) a propósito: el sistema nunca inventa un peso. Separar estimado y medido evita reportar más reciclaje del que realmente hubo. Si el profesor pregunta por «PESO_KG», hay que aclarar que el modelo tiene dos pesos distintos: este (medido) y PESO_ESTIMADO_KG.

#### 125. `DEPOSITO.INSTRUMENTO_MEDICION`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** INSTRUMENTO_MEDICION
- **3. Qué significa:** Báscula usada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: báscula usada. Se usa en: Credibilidad del dato.
- **5. Por qué existe:** CHECK: no hay peso medido sin instrumento.
- **6. Tipo de dato:** VARCHAR2(60 CHAR) (hasta 60 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL si no hubo medición.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_MEDIDO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** Báscula digital (SIMULADA)
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_MEDIDO (condición completa en el anexo de CHECK) (ORA-02290) · Un texto de más de 60 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_DEPOSITO_MEDIDO
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (báscula usada). CHECK: no hay peso medido sin instrumento.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_MEDIDO y por qué esos y no otros? · ¿Qué significa que INSTRUMENTO_MEDICION esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT INSTRUMENTO_MEDICION, COUNT(*) AS filas FROM DEPOSITO GROUP BY INSTRUMENTO_MEDICION ORDER BY INSTRUMENTO_MEDICION;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. INSTRUMENTO_MEDICION es la casilla que guarda: Báscula usada. Puede quedar vacía: NULL si no hubo medición.

#### 126. `DEPOSITO.PRECISION_MEDICION_G`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** PRECISION_MEDICION_G
- **3. Qué significa:** Precisión de la báscula en gramos.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: precisión de la báscula en gramos. Se usa en: Límite de exactitud.
- **5. Por qué existe:** Saber cuánto se puede confiar en la medida.
- **6. Tipo de dato:** NUMBER(6,1) (6 dígitos en total, 1 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL si no hubo medición.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_MEDIDO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Operario. INSERT/UPDATE a través de: PKG_DEPOSITO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_MEDIDO (condición completa en el anexo de CHECK) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas. Reglas CHECK: CK_DEPOSITO_MEDIDO
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (precisión de la báscula en gramos). Saber cuánto se puede confiar en la medida.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_MEDIDO y por qué esos y no otros? · ¿Qué significa que PRECISION_MEDICION_G esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PRECISION_MEDICION_G) AS con_valor, MIN(PRECISION_MEDICION_G) AS minimo, MAX(PRECISION_MEDICION_G) AS maximo, SUM(PRECISION_MEDICION_G) AS total FROM DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. PRECISION_MEDICION_G es la casilla que guarda: Precisión de la báscula en gramos. Puede quedar vacía: NULL si no hubo medición.

#### 127. `DEPOSITO.FECHA_DETECCION_UTC`

- **1. Tabla:** DEPOSITO — Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.
- **2. Atributo:** FECHA_DETECCION_UTC
- **3. Qué significa:** Cuándo ocurrió el depósito.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de DEPOSITO: cuándo ocurrió el depósito. Se usa en: Ventanas de 7 días, rankings.
- **5. Por qué existe:** Base de todos los reportes por fecha.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_DEPOSITO_VERIFICACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Registro del depósito y su verificación
- **12. Quién lo llena:** Servicio o trigger 2 (si llega NULL). INSERT/UPDATE a través de: TRG_DEPOSITO_COHERENCIA · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Operario (verifica), administrador y Power BI. SELECT en las vistas VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-11-05 15:21:10
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_DEPOSITO_VERIFICACION (condición completa en el anexo de CHECK) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_DEPOSITO_VERIFICACION · Vigilada por: TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA
- **17. Qué pasaría si no existiera:** La tabla DEPOSITO no podría guardar este dato (cuándo ocurrió el depósito). Base de todos los reportes por fecha.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PKG_ESTADISTICAS, TRG_DEPOSITO_BLOQUEO_CERRADO, TRG_DEPOSITO_COHERENCIA, VW_BI_DEPOSITOS, VW_DEPOSITO_DETALLE
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (22 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot confirma el registro del depósito (pendiente).
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_DEPOSITOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_DEPOSITO_VERIFICACION y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_DETECCION_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_DETECCION_UTC) AS primera, MAX(FECHA_DETECCION_UTC) AS ultima, COUNT(FECHA_DETECCION_UTC) AS con_valor FROM DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en DEPOSITO como una hoja de cálculo donde cada fila es un residuo depositado. FECHA_DETECCION_UTC es la casilla que guarda: Cuándo ocurrió el depósito. Nunca puede quedar vacía.

## Módulo: M5 · EcoPuntos y auditoría

### REGLA_ECOPUNTOS (10 atributos)

Tarifa de EcoPuntos por material, versionada en el tiempo.

#### 128. `REGLA_ECOPUNTOS.ID_REGLA`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** ID_REGLA
- **3. Qué significa:** Número de la versión de tarifa.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de REGLA_ECOPUNTOS, de modo que otras tablas puedan apuntarle. Se usa en: Movimientos.
- **5. Por qué existe:** Cada movimiento sabe qué tarifa usó.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: MOVIMIENTO_ECOPUNTOS
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de REGLA_ECOPUNTOS, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_REGLA es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM REGLA_ECOPUNTOS WHERE ID_REGLA = (SELECT MIN(ID_REGLA) FROM REGLA_ECOPUNTOS);
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. ID_REGLA es la casilla que guarda: Número de la versión de tarifa. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 129. `REGLA_ECOPUNTOS.ID_MATERIAL`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** ID_MATERIAL
- **3. Qué significa:** Material al que aplica.
- **4. Para qué sirve:** Sirve para saber a qué fila de MATERIAL pertenece cada fila de REGLA_ECOPUNTOS y poder unirlas con JOIN. Se usa en: regla_vigente().
- **5. Por qué existe:** Tarifas distintas por material.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_REGLA_MATERIAL->MATERIAL
- **10. Con qué tabla se relaciona:** Apunta a MATERIAL (FK FK_REGLA_MATERIAL)
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MATERIAL (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MATERIAL que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con MATERIAL
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre REGLA_ECOPUNTOS y MATERIAL: no sabríamos a qué MATERIAL pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MATERIAL que no existe en MATERIAL? (respuesta: ORA-02291) · ¿Por qué ID_MATERIAL es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_MATERIAL, COUNT(*) AS filas FROM REGLA_ECOPUNTOS t JOIN MATERIAL r ON r.ID_MATERIAL = t.ID_MATERIAL GROUP BY t.ID_MATERIAL ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. ID_MATERIAL es la casilla que guarda: Material al que aplica. Es un «puntero»: copia el número de una fila de MATERIAL para unir las dos tablas. Nunca puede quedar vacía.

#### 130. `REGLA_ECOPUNTOS.PUNTOS`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** PUNTOS
- **3. Qué significa:** Puntos que se dan.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: puntos que se dan. Se usa en: Cálculo del crédito.
- **5. Por qué existe:** CHECK 1..100000.
- **6. Tipo de dato:** NUMBER(7) (entero de hasta 7 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_PUNTOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 12
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_PUNTOS: puntos BETWEEN 1 AND 100000 (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_REGLA_PUNTOS
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (puntos que se dan). CHECK 1..100000.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_PUNTOS y por qué esos y no otros? · ¿Por qué PUNTOS es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT PUNTOS, COUNT(*) AS filas FROM REGLA_ECOPUNTOS GROUP BY PUNTOS ORDER BY PUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. PUNTOS es la casilla que guarda: Puntos que se dan. Nunca puede quedar vacía.
- **Explicación ampliada:** Es la tarifa: cuántos EcoPuntos se dan por un material. Si POR_UNIDAD = 1, se multiplica por las unidades verificadas. Las reglas tienen vigencia (DESDE_UTC, HASTA_UTC), así un cambio de tarifa no altera los puntos ya entregados.

#### 131. `REGLA_ECOPUNTOS.POR_UNIDAD`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** POR_UNIDAD
- **3. Qué significa:** 1 = puntos × unidades verificadas; 0 = puntos por depósito.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: 1 = puntos × unidades verificadas; 0 = puntos por depósito. Se usa en: Cálculo del crédito.
- **5. Por qué existe:** Algunos residuos se premian por unidad.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (DEFAULT 0).
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_POR_UNIDAD
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_POR_UNIDAD: por_unidad IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_REGLA_POR_UNIDAD
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (1 = puntos × unidades verificadas; 0 = puntos por depósito). Algunos residuos se premian por unidad.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_POR_UNIDAD y por qué esos y no otros? · ¿Por qué POR_UNIDAD es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT POR_UNIDAD, COUNT(*) AS filas FROM REGLA_ECOPUNTOS GROUP BY POR_UNIDAD ORDER BY POR_UNIDAD;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. POR_UNIDAD es la casilla que guarda: 1 = puntos × unidades verificadas; 0 = puntos por depósito. Nunca puede quedar vacía.

#### 132. `REGLA_ECOPUNTOS.DESCRIPCION`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** DESCRIPCION
- **3. Qué significa:** Explicación de la tarifa.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: explicación de la tarifa. Se usa en: Reportes.
- **5. Por qué existe:** Transparencia.
- **6. Tipo de dato:** VARCHAR2(200 CHAR) (hasta 200 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_DESCRIPCION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Tarifa sintética v2: 12 puntos por botella
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_DESCRIPCION: TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 200 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_REGLA_DESCRIPCION
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (explicación de la tarifa). Transparencia.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_DESCRIPCION y por qué esos y no otros? · ¿Por qué DESCRIPCION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT DESCRIPCION, COUNT(*) AS filas FROM REGLA_ECOPUNTOS GROUP BY DESCRIPCION ORDER BY DESCRIPCION;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. DESCRIPCION es la casilla que guarda: Explicación de la tarifa. Nunca puede quedar vacía.

#### 133. `REGLA_ECOPUNTOS.DESDE_UTC`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** DESDE_UTC
- **3. Qué significa:** Inicio de la vigencia.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: inicio de la vigencia. Se usa en: regla_vigente().
- **5. Por qué existe:** Versionado: una tarifa usada nunca se edita.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_INTERVALO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Administrador. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-10-22 00:00
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_INTERVALO: hasta_utc IS NULL OR hasta_utc > desde_utc (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Reglas CHECK: CK_REGLA_INTERVALO
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (inicio de la vigencia). Versionado: una tarifa usada nunca se edita.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_INTERVALO y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué DESDE_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(DESDE_UTC) AS primera, MAX(DESDE_UTC) AS ultima, COUNT(DESDE_UTC) AS con_valor FROM REGLA_ECOPUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. DESDE_UTC es la casilla que guarda: Inicio de la vigencia. Nunca puede quedar vacía.

#### 134. `REGLA_ECOPUNTOS.HASTA_UTC`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** HASTA_UTC
- **3. Qué significa:** Fin de la vigencia.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: fin de la vigencia. Se usa en: regla_vigente().
- **5. Por qué existe:** CHECK: fin después del inicio.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** Sí. NULL = versión vigente.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_INTERVALO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** PKG_ECOPUNTOS al crear otra versión. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-10-22 00:00
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_INTERVALO: hasta_utc IS NULL OR hasta_utc > desde_utc (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; errores de hora (se guarda en UTC). Reglas CHECK: CK_REGLA_INTERVALO
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (fin de la vigencia). CHECK: fin después del inicio.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_INTERVALO y por qué esos y no otros? · ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Qué significa que HASTA_UTC esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT MIN(HASTA_UTC) AS primera, MAX(HASTA_UTC) AS ultima, COUNT(HASTA_UTC) AS con_valor FROM REGLA_ECOPUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. HASTA_UTC es la casilla que guarda: Fin de la vigencia. Puede quedar vacía: NULL = versión vigente.

#### 135. `REGLA_ECOPUNTOS.ID_CREADOR`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** ID_CREADOR
- **3. Qué significa:** Administrador que la creó.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de REGLA_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Auditoría.
- **5. Por qué existe:** Solo el rol 3 crea tarifas.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_REGLA_CREADOR->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_REGLA_CREADOR)
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre REGLA_ECOPUNTOS y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CREADOR que no existe en USUARIO? (respuesta: ORA-02291) · ¿Por qué ID_CREADOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_CREADOR, COUNT(*) AS filas FROM REGLA_ECOPUNTOS t JOIN USUARIO r ON r.ID_USUARIO = t.ID_CREADOR GROUP BY t.ID_CREADOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. ID_CREADOR es la casilla que guarda: Administrador que la creó. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 136. `REGLA_ECOPUNTOS.CREADA_UTC`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** CREADA_UTC
- **3. Qué significa:** Cuándo se registró.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: cuándo se registró. Se usa en: Auditoría.
- **5. Por qué existe:** Trazabilidad.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre REGLA_ECOPUNTOS (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-07-13 00:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (cuándo se registró). Trazabilidad.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué CREADA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(CREADA_UTC) AS primera, MAX(CREADA_UTC) AS ultima, COUNT(CREADA_UTC) AS con_valor FROM REGLA_ECOPUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. CREADA_UTC es la casilla que guarda: Cuándo se registró. Nunca puede quedar vacía.

#### 137. `REGLA_ECOPUNTOS.ES_SINTETICA`

- **1. Tabla:** REGLA_ECOPUNTOS — Tarifa de EcoPuntos por material, versionada en el tiempo.
- **2. Atributo:** ES_SINTETICA
- **3. Qué significa:** 1 si la tarifa es de prueba (no aprobada).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de REGLA_ECOPUNTOS: 1 si la tarifa es de prueba (no aprobada). Se usa en: Rótulo TARIFA SINTÉTICA.
- **5. Por qué existe:** Sin tarifa aprobada el saldo real no aumenta.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** 0
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_REGLA_SINTETICA
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Cálculo de EcoPuntos
- **12. Quién lo llena:** Script 11. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y PKG_ECOPUNTOS. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_REGLA_SINTETICA: es_sintetica IN (0, 1) (ORA-02290)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato); mezclar datos de demostración con datos del sistema. Reglas CHECK: CK_REGLA_SINTETICA
- **17. Qué pasaría si no existiera:** La tabla REGLA_ECOPUNTOS no podría guardar este dato (1 si la tarifa es de prueba (no aprobada)). Sin tarifa aprobada el saldo real no aumenta.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (3 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** No se usa directamente.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_REGLA_SINTETICA y por qué esos y no otros? · ¿Por qué ES_SINTETICA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ES_SINTETICA, COUNT(*) AS filas FROM REGLA_ECOPUNTOS GROUP BY ES_SINTETICA ORDER BY ES_SINTETICA;
- **24. Explicación sencilla (para quien empieza):** Piensa en REGLA_ECOPUNTOS como una hoja de cálculo donde cada fila es una tarifa de EcoPuntos. ES_SINTETICA es la casilla que guarda: 1 si la tarifa es de prueba (no aprobada). Nunca puede quedar vacía.

### MOVIMIENTO_ECOPUNTOS (11 atributos)

Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.

#### 138. `MOVIMIENTO_ECOPUNTOS.ID_MOVIMIENTO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_MOVIMIENTO
- **3. Qué significa:** Número del movimiento.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de MOVIMIENTO_ECOPUNTOS, de modo que otras tablas puedan apuntarle. Se usa en: Saldo.
- **5. Por qué existe:** Libro contable: nunca se borra.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Es referenciada por: MOVIMIENTO_ECOPUNTOS
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** Oracle. INSERT (Oracle asigna el número; no se actualiza) · INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 10
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de MOVIMIENTO_ECOPUNTOS, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_MOVIMIENTO es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM MOVIMIENTO_ECOPUNTOS WHERE ID_MOVIMIENTO = (SELECT MIN(ID_MOVIMIENTO) FROM MOVIMIENTO_ECOPUNTOS);
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_MOVIMIENTO es la casilla que guarda: Número del movimiento. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 139. `MOVIMIENTO_ECOPUNTOS.CLAVE_IDEMPOTENCIA`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** CLAVE_IDEMPOTENCIA
- **3. Qué significa:** Clave única de la operación.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MOVIMIENTO_ECOPUNTOS: clave única de la operación. Se usa en: Evitar doble crédito.
- **5. Por qué existe:** Repetir «acreditar» no duplica puntos.
- **6. Tipo de dato:** VARCHAR2(80 CHAR) (hasta 80 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL; UNIQUE.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** UNIQUE: UQ_MOV_CLAVE
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** CRED-25-1
- **15. Ejemplo de valor incorrecto:** Repetir un valor ya usado (ORA-00001 por UQ_MOV_CLAVE) · Dejarlo vacío (ORA-01400) · Un texto de más de 80 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores duplicados; filas incompletas (no se puede guardar sin este dato); procesar dos veces la misma operación. Unicidad: UQ_MOV_CLAVE
- **17. Qué pasaría si no existiera:** La tabla MOVIMIENTO_ECOPUNTOS no podría guardar este dato (clave única de la operación). Repetir «acreditar» no duplica puntos.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué CLAVE_IDEMPOTENCIA no puede repetirse? (UNIQUE UQ_MOV_CLAVE) · ¿Por qué CLAVE_IDEMPOTENCIA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT CLAVE_IDEMPOTENCIA FROM MOVIMIENTO_ECOPUNTOS FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. CLAVE_IDEMPOTENCIA es la casilla que guarda: Clave única de la operación. Nunca puede quedar vacía.
- **Explicación ampliada:** Es una clave única por operación (por ejemplo CRED-25-1). Si el sistema intenta acreditar dos veces el mismo depósito, Oracle rechaza la segunda por UNIQUE y la persona no recibe puntos dobles.

#### 140. `MOVIMIENTO_ECOPUNTOS.TIPO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** TIPO
- **3. Qué significa:** 1 Crédito, 2 Reverso.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MOVIMIENTO_ECOPUNTOS: 1 Crédito, 2 Reverso. Se usa en: Saldo.
- **5. Por qué existe:** Las correcciones se hacen con reversos.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_MOV_TIPO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_MOV_TIPO (condición completa en el anexo de CHECK) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MOV_TIPO
- **17. Qué pasaría si no existiera:** La tabla MOVIMIENTO_ECOPUNTOS no podría guardar este dato (1 Crédito, 2 Reverso). Las correcciones se hacen con reversos.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MOV_TIPO y por qué esos y no otros? · ¿Por qué TIPO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT TIPO, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS GROUP BY TIPO ORDER BY TIPO;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. TIPO es la casilla que guarda: 1 Crédito, 2 Reverso. Nunca puede quedar vacía.

#### 141. `MOVIMIENTO_ECOPUNTOS.ID_DEPOSITO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_DEPOSITO
- **3. Qué significa:** Depósito que originó los puntos.
- **4. Para qué sirve:** Sirve para saber a qué fila de DEPOSITO pertenece cada fila de MOVIMIENTO_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Trazabilidad.
- **5. Por qué existe:** Todo punto tiene un depósito detrás.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_MOV_DEPOSITO->DEPOSITO
- **10. Con qué tabla se relaciona:** Apunta a DEPOSITO (FK FK_MOV_DEPOSITO)
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 25
- **15. Ejemplo de valor incorrecto:** Un número que no exista en DEPOSITO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) DEPOSITO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con DEPOSITO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre MOVIMIENTO_ECOPUNTOS y DEPOSITO: no sabríamos a qué DEPOSITO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_DEPOSITO que no existe en DEPOSITO? (respuesta: ORA-02291) · ¿Por qué ID_DEPOSITO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_DEPOSITO, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS t JOIN DEPOSITO r ON r.ID_DEPOSITO = t.ID_DEPOSITO GROUP BY t.ID_DEPOSITO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_DEPOSITO es la casilla que guarda: Depósito que originó los puntos. Es un «puntero»: copia el número de una fila de DEPOSITO para unir las dos tablas. Nunca puede quedar vacía.

#### 142. `MOVIMIENTO_ECOPUNTOS.ID_USUARIO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_USUARIO
- **3. Qué significa:** Ciudadano titular.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de MOVIMIENTO_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Saldo.
- **5. Por qué existe:** A quién pertenecen los puntos.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_MOV_USUARIO->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_MOV_USUARIO)
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 17
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre MOVIMIENTO_ECOPUNTOS y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_USUARIO que no existe en USUARIO? (respuesta: ORA-02291) · ¿Por qué ID_USUARIO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_USUARIO, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS t JOIN USUARIO r ON r.ID_USUARIO = t.ID_USUARIO GROUP BY t.ID_USUARIO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_USUARIO es la casilla que guarda: Ciudadano titular. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 143. `MOVIMIENTO_ECOPUNTOS.ID_REGLA`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_REGLA
- **3. Qué significa:** Tarifa aplicada.
- **4. Para qué sirve:** Sirve para saber a qué fila de REGLA_ECOPUNTOS pertenece cada fila de MOVIMIENTO_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Explicación del cálculo.
- **5. Por qué existe:** Se sabe con qué tarifa se calculó.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_MOV_REGLA->REGLA_ECOPUNTOS
- **10. Con qué tabla se relaciona:** Apunta a REGLA_ECOPUNTOS (FK FK_MOV_REGLA)
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un número que no exista en REGLA_ECOPUNTOS (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) REGLA_ECOPUNTOS que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con REGLA_ECOPUNTOS
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre MOVIMIENTO_ECOPUNTOS y REGLA_ECOPUNTOS: no sabríamos a qué REGLA_ECOPUNTOS pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_REGLA que no existe en REGLA_ECOPUNTOS? (respuesta: ORA-02291) · ¿Por qué ID_REGLA es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_REGLA, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS t JOIN REGLA_ECOPUNTOS r ON r.ID_REGLA = t.ID_REGLA GROUP BY t.ID_REGLA ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_REGLA es la casilla que guarda: Tarifa aplicada. Es un «puntero»: copia el número de una fila de REGLA_ECOPUNTOS para unir las dos tablas. Nunca puede quedar vacía.

#### 144. `MOVIMIENTO_ECOPUNTOS.PUNTOS`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** PUNTOS
- **3. Qué significa:** Puntos (+ crédito, − reverso).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MOVIMIENTO_ECOPUNTOS: puntos (+ crédito, − reverso). Se usa en: Saldo = SUMA.
- **5. Por qué existe:** CHECK de signo según el tipo.
- **6. Tipo de dato:** NUMBER(8) (entero de hasta 8 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_MOV_TIPO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 24
- **15. Ejemplo de valor incorrecto:** Una combinación que rompa CK_MOV_TIPO (condición completa en el anexo de CHECK) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MOV_TIPO
- **17. Qué pasaría si no existiera:** La tabla MOVIMIENTO_ECOPUNTOS no podría guardar este dato (puntos (+ crédito, − reverso)). CHECK de signo según el tipo.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS, PR_CAMBIAR_ESTADO_CONTENEDOR, VW_BI_ECOPUNTOS, VW_SALDO_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MOV_TIPO y por qué esos y no otros? · ¿Por qué PUNTOS es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT PUNTOS, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS GROUP BY PUNTOS ORDER BY PUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. PUNTOS es la casilla que guarda: Puntos (+ crédito, − reverso). Nunca puede quedar vacía.
- **Explicación ampliada:** Es la cantidad de EcoPuntos de cada movimiento: positiva en un crédito y negativa en un reverso. El saldo de una persona NO se guarda en ninguna columna: se calcula sumando sus movimientos (vista VW_SALDO_ECOPUNTOS). Por eso, si el profesor pregunta por «SALDO_PUNTOS», la respuesta es que el saldo es un dato derivado: SUM(PUNTOS) por persona. Guardarlo aparte sería redundante y podría quedar desactualizado.

#### 145. `MOVIMIENTO_ECOPUNTOS.ID_MOVIMIENTO_REVERTIDO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_MOVIMIENTO_REVERTIDO
- **3. Qué significa:** Crédito que este reverso anula.
- **4. Para qué sirve:** Sirve para saber a qué fila de MOVIMIENTO_ECOPUNTOS pertenece cada fila de MOVIMIENTO_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Correcciones.
- **5. Por qué existe:** UNIQUE: un crédito se revierte una sola vez.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL en créditos.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_MOV_REVERTIDO->MOVIMIENTO_ECOPUNTOS; UNIQUE: UQ_MOV_REVERTIDO; CHECK CK_MOV_TIPO
- **10. Con qué tabla se relaciona:** Apunta a MOVIMIENTO_ECOPUNTOS (FK FK_MOV_REVERTIDO)
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 10
- **15. Ejemplo de valor incorrecto:** Un número que no exista en MOVIMIENTO_ECOPUNTOS (ORA-02291) · Una combinación que rompa CK_MOV_TIPO (condición completa en el anexo de CHECK) (ORA-02290) · Repetir un valor ya usado (ORA-00001 por UQ_MOV_REVERTIDO)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) MOVIMIENTO_ECOPUNTOS que no existe); valores duplicados; valores fuera de las opciones permitidas. Integridad referencial con MOVIMIENTO_ECOPUNTOS · Reglas CHECK: CK_MOV_TIPO · Unicidad: UQ_MOV_REVERTIDO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre MOVIMIENTO_ECOPUNTOS y MOVIMIENTO_ECOPUNTOS: no sabríamos a qué MOVIMIENTO_ECOPUNTOS pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_MOVIMIENTO_REVERTIDO que no existe en MOVIMIENTO_ECOPUNTOS? (respuesta: ORA-02291) · ¿Qué valores permite CK_MOV_TIPO y por qué esos y no otros? · ¿Por qué ID_MOVIMIENTO_REVERTIDO no puede repetirse? (UNIQUE UQ_MOV_REVERTIDO)
- **23. SELECT para verlo:** SELECT t.ID_MOVIMIENTO_REVERTIDO, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS t JOIN MOVIMIENTO_ECOPUNTOS r ON r.ID_MOVIMIENTO = t.ID_MOVIMIENTO_REVERTIDO GROUP BY t.ID_MOVIMIENTO_REVERTIDO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_MOVIMIENTO_REVERTIDO es la casilla que guarda: Crédito que este reverso anula. Es un «puntero»: copia el número de una fila de MOVIMIENTO_ECOPUNTOS para unir las dos tablas. Puede quedar vacía: NULL en créditos.
- **Explicación ampliada:** En un reverso, apunta al crédito que se anula (es una FK a la misma tabla). Es UNIQUE: un crédito solo se puede revertir una vez. Así las correcciones no borran nada: se agrega un movimiento negativo que deja la historia completa.

#### 146. `MOVIMIENTO_ECOPUNTOS.ID_ACTOR`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** ID_ACTOR
- **3. Qué significa:** Operario o administrador que ejecutó.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de MOVIMIENTO_ECOPUNTOS y poder unirlas con JOIN. Se usa en: Auditoría.
- **5. Por qué existe:** Rendición de cuentas.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_MOV_ACTOR->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_MOV_ACTOR)
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con USUARIO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre MOVIMIENTO_ECOPUNTOS y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_ACTOR que no existe en USUARIO? (respuesta: ORA-02291) · ¿Por qué ID_ACTOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_ACTOR, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS t JOIN USUARIO r ON r.ID_USUARIO = t.ID_ACTOR GROUP BY t.ID_ACTOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. ID_ACTOR es la casilla que guarda: Operario o administrador que ejecutó. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Nunca puede quedar vacía.

#### 147. `MOVIMIENTO_ECOPUNTOS.MOTIVO`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** MOTIVO
- **3. Qué significa:** Razón del movimiento.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MOVIMIENTO_ECOPUNTOS: razón del movimiento. Se usa en: Auditoría.
- **5. Por qué existe:** Explicar cada cambio de saldo.
- **6. Tipo de dato:** VARCHAR2(300 CHAR) (hasta 300 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_MOV_MOTIVO
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** PKG_ECOPUNTOS. INSERT/UPDATE a través de: PKG_ECOPUNTOS · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Crédito por depósito verificado
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_MOV_MOTIVO: TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo) (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 300 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_MOV_MOTIVO
- **17. Qué pasaría si no existiera:** La tabla MOVIMIENTO_ECOPUNTOS no podría guardar este dato (razón del movimiento). Explicar cada cambio de saldo.
- **18. Consultas, procedimientos y triggers que lo usan:** PKG_DEPOSITO, PKG_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_MOV_MOTIVO y por qué esos y no otros? · ¿Por qué MOTIVO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MOTIVO, COUNT(*) AS filas FROM MOVIMIENTO_ECOPUNTOS GROUP BY MOTIVO ORDER BY MOTIVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. MOTIVO es la casilla que guarda: Razón del movimiento. Nunca puede quedar vacía.

#### 148. `MOVIMIENTO_ECOPUNTOS.FECHA_UTC`

- **1. Tabla:** MOVIMIENTO_ECOPUNTOS — Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.
- **2. Atributo:** FECHA_UTC
- **3. Qué significa:** Cuándo se registró.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de MOVIMIENTO_ECOPUNTOS: cuándo se registró. Se usa en: Reportes.
- **5. Por qué existe:** Línea de tiempo del saldo.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Acreditar y revertir EcoPuntos (el saldo es la suma)
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre MOVIMIENTO_ECOPUNTOS (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Ciudadano (su saldo), administrador y Power BI. SELECT en las vistas VW_BI_ECOPUNTOS y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-11-06 09:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla MOVIMIENTO_ECOPUNTOS no podría guardar este dato (cuándo se registró). Línea de tiempo del saldo.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_ECOPUNTOS
- **19. ¿Se usa en C#?:** En P3 la tabla la usa la capa EcoAccess.P3.Datos (9 sentencias SQL). La app académica de C# solo lee vistas.
- **20. ¿Se usa en Telegram?:** El bot responde el saldo de EcoPuntos.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_ECOPUNTOS (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_UTC) AS primera, MAX(FECHA_UTC) AS ultima, COUNT(FECHA_UTC) AS con_valor FROM MOVIMIENTO_ECOPUNTOS;
- **24. Explicación sencilla (para quien empieza):** Piensa en MOVIMIENTO_ECOPUNTOS como una hoja de cálculo donde cada fila es un crédito o reverso de EcoPuntos. FECHA_UTC es la casilla que guarda: Cuándo se registró. Nunca puede quedar vacía.

### AUDITORIA_DEPOSITO (17 atributos)

Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).

#### 149. `AUDITORIA_DEPOSITO.ID_AUDITORIA`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_AUDITORIA
- **3. Qué significa:** Número de la fila de auditoría.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de AUDITORIA_DEPOSITO, de modo que otras tablas puedan apuntarle. Se usa en: Consulta de auditoría.
- **5. Por qué existe:** Historia completa de verificaciones.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de AUDITORIA_DEPOSITO, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_AUDITORIA es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM AUDITORIA_DEPOSITO WHERE ID_AUDITORIA = (SELECT MIN(ID_AUDITORIA) FROM AUDITORIA_DEPOSITO);
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_AUDITORIA es la casilla que guarda: Número de la fila de auditoría. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 150. `AUDITORIA_DEPOSITO.ID_DEPOSITO`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_DEPOSITO
- **3. Qué significa:** Depósito afectado.
- **4. Para qué sirve:** Sirve para saber a qué fila de DEPOSITO pertenece cada fila de AUDITORIA_DEPOSITO y poder unirlas con JOIN. Se usa en: Consulta por depósito.
- **5. Por qué existe:** Saber qué depósito cambió.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_AUD_DEP_DEPOSITO->DEPOSITO
- **10. Con qué tabla se relaciona:** Apunta a DEPOSITO (FK FK_AUD_DEP_DEPOSITO)
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 25
- **15. Ejemplo de valor incorrecto:** Un número que no exista en DEPOSITO (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) DEPOSITO que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con DEPOSITO · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre AUDITORIA_DEPOSITO y DEPOSITO: no sabríamos a qué DEPOSITO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_DEPOSITO que no existe en DEPOSITO? (respuesta: ORA-02291) · ¿Por qué ID_DEPOSITO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_DEPOSITO, COUNT(*) AS filas FROM AUDITORIA_DEPOSITO t JOIN DEPOSITO r ON r.ID_DEPOSITO = t.ID_DEPOSITO GROUP BY t.ID_DEPOSITO ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_DEPOSITO es la casilla que guarda: Depósito afectado. Es un «puntero»: copia el número de una fila de DEPOSITO para unir las dos tablas. Nunca puede quedar vacía.

#### 151. `AUDITORIA_DEPOSITO.OPERACION`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** OPERACION
- **3. Qué significa:** VERIFICACION, RECHAZO, CORRECCION o REAPERTURA.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: vERIFICACION, RECHAZO, CORRECCION o REAPERTURA. Se usa en: Filtros de auditoría.
- **5. Por qué existe:** Clasifica el cambio.
- **6. Tipo de dato:** VARCHAR2(12 CHAR) (hasta 12 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_AUD_DEP_OPERACION
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** VERIFICACION
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_AUD_DEP_OPERACION: operacion IN ('VERIFICACION', 'RECHAZO', 'CORRECCION', 'REAPERTURA') (ORA-02290) · Dejarlo vacío (ORA-01400) · Un texto de más de 12 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_AUD_DEP_OPERACION · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (vERIFICACION, RECHAZO, CORRECCION o REAPERTURA). Clasifica el cambio.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_AUD_DEP_OPERACION y por qué esos y no otros? · ¿Por qué OPERACION es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT OPERACION, COUNT(*) AS filas FROM AUDITORIA_DEPOSITO GROUP BY OPERACION ORDER BY OPERACION;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. OPERACION es la casilla que guarda: VERIFICACION, RECHAZO, CORRECCION o REAPERTURA. Nunca puede quedar vacía.

#### 152. `AUDITORIA_DEPOSITO.ESTADO_ANTERIOR`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ESTADO_ANTERIOR
- **3. Qué significa:** :OLD.estado_verificacion.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :OLD.estado_verificacion. Se usa en: Antes/después.
- **5. Por qué existe:** Valor previo, que no cambia ni en BEFORE ni en AFTER.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_AUD_DEP_ESTADOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 0
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_AUD_DEP_ESTADOS: estado_anterior IN (0, 1, 2) AND estado_nuevo IN (0, 1, 2) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_AUD_DEP_ESTADOS · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:OLD.estado_verificacion). Valor previo, que no cambia ni en BEFORE ni en AFTER.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_AUD_DEP_ESTADOS y por qué esos y no otros? · ¿Por qué ESTADO_ANTERIOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_ANTERIOR, COUNT(*) AS filas FROM AUDITORIA_DEPOSITO GROUP BY ESTADO_ANTERIOR ORDER BY ESTADO_ANTERIOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ESTADO_ANTERIOR es la casilla que guarda: :OLD.estado_verificacion. Nunca puede quedar vacía.

#### 153. `AUDITORIA_DEPOSITO.ESTADO_NUEVO`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ESTADO_NUEVO
- **3. Qué significa:** :NEW.estado_verificacion.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :NEW.estado_verificacion. Se usa en: Antes/después.
- **5. Por qué existe:** Valor final guardado.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_AUD_DEP_ESTADOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_AUD_DEP_ESTADOS: estado_anterior IN (0, 1, 2) AND estado_nuevo IN (0, 1, 2) (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_AUD_DEP_ESTADOS · Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:NEW.estado_verificacion). Valor final guardado.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_AUD_DEP_ESTADOS y por qué esos y no otros? · ¿Por qué ESTADO_NUEVO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_NUEVO, COUNT(*) AS filas FROM AUDITORIA_DEPOSITO GROUP BY ESTADO_NUEVO ORDER BY ESTADO_NUEVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ESTADO_NUEVO es la casilla que guarda: :NEW.estado_verificacion. Nunca puede quedar vacía.

#### 154. `AUDITORIA_DEPOSITO.ID_MATERIAL_VERIFICADO_ANTERIOR`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_MATERIAL_VERIFICADO_ANTERIOR
- **3. Qué significa:** :OLD del material verificado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :OLD del material verificado. Se usa en: Antes/después.
- **5. Por qué existe:** Detectar correcciones de material.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si no había.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** NULL
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:OLD del material verificado). Detectar correcciones de material.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que ID_MATERIAL_VERIFICADO_ANTERIOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(ID_MATERIAL_VERIFICADO_ANTERIOR) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_MATERIAL_VERIFICADO_ANTERIOR es la casilla que guarda: :OLD del material verificado. Puede quedar vacía: NULL si no había.

#### 155. `AUDITORIA_DEPOSITO.ID_MATERIAL_VERIFICADO_NUEVO`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_MATERIAL_VERIFICADO_NUEVO
- **3. Qué significa:** :NEW del material verificado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :NEW del material verificado. Se usa en: Antes/después.
- **5. Por qué existe:** Idem.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL en rechazo o reapertura.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 101
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:NEW del material verificado). Idem.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que ID_MATERIAL_VERIFICADO_NUEVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(ID_MATERIAL_VERIFICADO_NUEVO) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_MATERIAL_VERIFICADO_NUEVO es la casilla que guarda: :NEW del material verificado. Puede quedar vacía: NULL en rechazo o reapertura.

#### 156. `AUDITORIA_DEPOSITO.CANTIDAD_ANTERIOR`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** CANTIDAD_ANTERIOR
- **3. Qué significa:** :OLD de la cantidad verificada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :OLD de la cantidad verificada. Se usa en: Antes/después.
- **5. Por qué existe:** Detectar correcciones de conteo.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si no había.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:OLD de la cantidad verificada). Detectar correcciones de conteo.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que CANTIDAD_ANTERIOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(CANTIDAD_ANTERIOR) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. CANTIDAD_ANTERIOR es la casilla que guarda: :OLD de la cantidad verificada. Puede quedar vacía: NULL si no había.

#### 157. `AUDITORIA_DEPOSITO.CANTIDAD_NUEVA`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** CANTIDAD_NUEVA
- **3. Qué significa:** :NEW de la cantidad verificada.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :NEW de la cantidad verificada. Se usa en: Antes/después.
- **5. Por qué existe:** Idem.
- **6. Tipo de dato:** NUMBER(4) (entero de hasta 4 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si se quitó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:NEW de la cantidad verificada). Idem.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que CANTIDAD_NUEVA esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(CANTIDAD_NUEVA) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. CANTIDAD_NUEVA es la casilla que guarda: :NEW de la cantidad verificada. Puede quedar vacía: NULL si se quitó.

#### 158. `AUDITORIA_DEPOSITO.PESO_MEDIDO_ANTERIOR_KG`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** PESO_MEDIDO_ANTERIOR_KG
- **3. Qué significa:** :OLD del peso medido.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :OLD del peso medido. Se usa en: Antes/después.
- **5. Por qué existe:** Detectar cambios de medición.
- **6. Tipo de dato:** NUMBER(9,4) (9 dígitos en total, 4 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL si no había.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** NULL
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:OLD del peso medido). Detectar cambios de medición.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué no se inventa el peso cuando no hay báscula? (queda NULL) · ¿Qué significa que PESO_MEDIDO_ANTERIOR_KG esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PESO_MEDIDO_ANTERIOR_KG) AS con_valor, MIN(PESO_MEDIDO_ANTERIOR_KG) AS minimo, MAX(PESO_MEDIDO_ANTERIOR_KG) AS maximo, SUM(PESO_MEDIDO_ANTERIOR_KG) AS total FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. PESO_MEDIDO_ANTERIOR_KG es la casilla que guarda: :OLD del peso medido. Puede quedar vacía: NULL si no había.

#### 159. `AUDITORIA_DEPOSITO.PESO_MEDIDO_NUEVO_KG`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** PESO_MEDIDO_NUEVO_KG
- **3. Qué significa:** :NEW del peso medido.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :NEW del peso medido. Se usa en: Antes/después.
- **5. Por qué existe:** Idem.
- **6. Tipo de dato:** NUMBER(9,4) (9 dígitos en total, 4 decimales)
- **7. ¿Acepta NULL?:** Sí. NULL si no hay medición.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 0,045
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:NEW del peso medido). Idem.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué no se inventa el peso cuando no hay báscula? (queda NULL) · ¿Qué significa que PESO_MEDIDO_NUEVO_KG esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(PESO_MEDIDO_NUEVO_KG) AS con_valor, MIN(PESO_MEDIDO_NUEVO_KG) AS minimo, MAX(PESO_MEDIDO_NUEVO_KG) AS maximo, SUM(PESO_MEDIDO_NUEVO_KG) AS total FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. PESO_MEDIDO_NUEVO_KG es la casilla que guarda: :NEW del peso medido. Puede quedar vacía: NULL si no hay medición.

#### 160. `AUDITORIA_DEPOSITO.ID_VERIFICADOR_ANTERIOR`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_VERIFICADOR_ANTERIOR
- **3. Qué significa:** :OLD del verificador.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :OLD del verificador. Se usa en: Antes/después.
- **5. Por qué existe:** Quién había decidido antes.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL si nadie había verificado.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** NULL
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:OLD del verificador). Quién había decidido antes.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que ID_VERIFICADOR_ANTERIOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(ID_VERIFICADOR_ANTERIOR) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_VERIFICADOR_ANTERIOR es la casilla que guarda: :OLD del verificador. Puede quedar vacía: NULL si nadie había verificado.

#### 161. `AUDITORIA_DEPOSITO.ID_VERIFICADOR_NUEVO`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_VERIFICADOR_NUEVO
- **3. Qué significa:** :NEW del verificador.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: :NEW del verificador. Se usa en: Antes/después.
- **5. Por qué existe:** Quién decide ahora.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL en reapertura.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (:NEW del verificador). Quién decide ahora.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que ID_VERIFICADOR_NUEVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(ID_VERIFICADOR_NUEVO) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_VERIFICADOR_NUEVO es la casilla que guarda: :NEW del verificador. Puede quedar vacía: NULL en reapertura.

#### 162. `AUDITORIA_DEPOSITO.ID_ACTOR`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** ID_ACTOR
- **3. Qué significa:** Persona de la aplicación que actuó (PKG_CONTEXTO_APP).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: persona de la aplicación que actuó (PKG_CONTEXTO_APP). Se usa en: Rendición de cuentas.
- **5. Por qué existe:** USER es la cuenta Oracle compartida; el actor es la persona.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = cambio sin actor de aplicación (SQL directo).
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (persona de la aplicación que actuó (PKG_CONTEXTO_APP)). USER es la cuenta Oracle compartida; el actor es la persona.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que ID_ACTOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(ID_ACTOR) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. ID_ACTOR es la casilla que guarda: Persona de la aplicación que actuó (PKG_CONTEXTO_APP). Puede quedar vacía: NULL = cambio sin actor de aplicación (SQL directo).

#### 163. `AUDITORIA_DEPOSITO.USUARIO_BD`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** USUARIO_BD
- **3. Qué significa:** Cuenta Oracle de la sesión (USER).
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: cuenta Oracle de la sesión (USER). Se usa en: Diagnóstico técnico.
- **5. Por qué existe:** Saber desde qué cuenta técnica se hizo.
- **6. Tipo de dato:** VARCHAR2(128 CHAR) (hasta 128 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** ECOACCESS_BD
- **15. Ejemplo de valor incorrecto:** Dejarlo vacío (ORA-01400) · Un texto de más de 128 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato). Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (cuenta Oracle de la sesión (USER)). Saber desde qué cuenta técnica se hizo.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué USUARIO_BD es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT USUARIO_BD FROM AUDITORIA_DEPOSITO FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. USUARIO_BD es la casilla que guarda: Cuenta Oracle de la sesión (USER). Nunca puede quedar vacía.

#### 164. `AUDITORIA_DEPOSITO.MOTIVO`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** MOTIVO
- **3. Qué significa:** Razón del cambio.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: razón del cambio. Se usa en: Auditoría.
- **5. Por qué existe:** Justificar correcciones.
- **6. Tipo de dato:** VARCHAR2(300 CHAR) (hasta 300 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL en verificaciones normales sin motivo.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Trigger 3. INSERT/UPDATE a través de: TRG_DEPOSITO_AUD_VERIFICACION · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** Revisión de cámara: solo 1 unidad
- **15. Ejemplo de valor incorrecto:** Un texto de más de 300 caracteres (ORA-12899)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_DEPOSITO_AUD_VERIFICACION
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (razón del cambio). Justificar correcciones.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_DEPOSITO_AUD_VERIFICACION, VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que MOTIVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(MOTIVO) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. MOTIVO es la casilla que guarda: Razón del cambio. Puede quedar vacía: NULL en verificaciones normales sin motivo.

#### 165. `AUDITORIA_DEPOSITO.FECHA_UTC`

- **1. Tabla:** AUDITORIA_DEPOSITO — Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).
- **2. Atributo:** FECHA_UTC
- **3. Qué significa:** Cuándo ocurrió.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de AUDITORIA_DEPOSITO: cuándo ocurrió. Se usa en: Línea de tiempo.
- **5. Por qué existe:** Orden de los hechos.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Auditoría de verificaciones (disparador 3)
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre AUDITORIA_DEPOSITO (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador y auditor (Power BI). SELECT en las vistas VW_BI_AUDITORIA y en las consultas de exposición (13_consultas_exposicion.sql)
- **14. Ejemplo de valor correcto:** 2026-11-06 09:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla AUDITORIA_DEPOSITO no podría guardar este dato (cuándo ocurrió). Orden de los hechos.
- **18. Consultas, procedimientos y triggers que lo usan:** VW_BI_AUDITORIA
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** No se usa.
- **21. ¿Se usa en Power BI?:** Llega a Power BI por VW_BI_AUDITORIA (solo lectura, con seudónimos).
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_UTC) AS primera, MAX(FECHA_UTC) AS ultima, COUNT(FECHA_UTC) AS con_valor FROM AUDITORIA_DEPOSITO;
- **24. Explicación sencilla (para quien empieza):** Piensa en AUDITORIA_DEPOSITO como una hoja de cálculo donde cada fila es un cambio auditado de un depósito. FECHA_UTC es la casilla que guarda: Cuándo ocurrió. Nunca puede quedar vacía.

### HISTORIAL_ESTADO_CONTENEDOR (8 atributos)

Cada cambio de disponibilidad de una caneca (trigger 1).

#### 166. `HISTORIAL_ESTADO_CONTENEDOR.ID_HISTORIAL`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** ID_HISTORIAL
- **3. Qué significa:** Número del cambio.
- **4. Para qué sirve:** Sirve para identificar sin ambigüedad cada fila de HISTORIAL_ESTADO_CONTENEDOR, de modo que otras tablas puedan apuntarle. Se usa en: Consulta.
- **5. Por qué existe:** Historia de disponibilidad.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL (PK).
- **8. Valor por defecto:** IDENTITY ALWAYS
- **9. ¿Es PK o FK? (restricciones):** PK; IDENTITY ALWAYS
- **10. Con qué tabla se relaciona:** Ninguna tabla la referencia
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT (Oracle asigna el número; no se actualiza) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Escribir el número a mano en un INSERT (ORA-32795: la identidad la asigna Oracle)
- **16. Qué problema evita:** Evita filas repetidas o imposibles de distinguir. Identidad: cada fila se distingue de las demás
- **17. Qué pasaría si no existiera:** No habría forma segura de señalar una fila concreta de HISTORIAL_ESTADO_CONTENEDOR, y las tablas que dependen de ella no podrían relacionarse.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué ID_HISTORIAL es un número subrogado y no un dato del negocio que podría cambiar?
- **23. SELECT para verlo:** SELECT * FROM HISTORIAL_ESTADO_CONTENEDOR WHERE ID_HISTORIAL = (SELECT MIN(ID_HISTORIAL) FROM HISTORIAL_ESTADO_CONTENEDOR);
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. ID_HISTORIAL es la casilla que guarda: Número del cambio. Funciona como la «cédula» de la fila: no se repite y no cambia. Nunca puede quedar vacía.

#### 167. `HISTORIAL_ESTADO_CONTENEDOR.ID_CONTENEDOR`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** ID_CONTENEDOR
- **3. Qué significa:** Caneca que cambió.
- **4. Para qué sirve:** Sirve para saber a qué fila de CONTENEDOR pertenece cada fila de HISTORIAL_ESTADO_CONTENEDOR y poder unirlas con JOIN. Se usa en: Consulta por caneca.
- **5. Por qué existe:** Saber qué caneca.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_HIST_CONTENEDOR->CONTENEDOR
- **10. Con qué tabla se relaciona:** Apunta a CONTENEDOR (FK FK_HIST_CONTENEDOR)
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 13
- **15. Ejemplo de valor incorrecto:** Un número que no exista en CONTENEDOR (ORA-02291) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) CONTENEDOR que no existe); filas incompletas (no se puede guardar sin este dato). Integridad referencial con CONTENEDOR · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre HISTORIAL_ESTADO_CONTENEDOR y CONTENEDOR: no sabríamos a qué CONTENEDOR pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_CONTENEDOR que no existe en CONTENEDOR? (respuesta: ORA-02291) · ¿Por qué ID_CONTENEDOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT t.ID_CONTENEDOR, COUNT(*) AS filas FROM HISTORIAL_ESTADO_CONTENEDOR t JOIN CONTENEDOR r ON r.ID_CONTENEDOR = t.ID_CONTENEDOR GROUP BY t.ID_CONTENEDOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. ID_CONTENEDOR es la casilla que guarda: Caneca que cambió. Es un «puntero»: copia el número de una fila de CONTENEDOR para unir las dos tablas. Nunca puede quedar vacía.

#### 168. `HISTORIAL_ESTADO_CONTENEDOR.ESTADO_ANTERIOR`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** ESTADO_ANTERIOR
- **3. Qué significa:** :OLD.estado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de HISTORIAL_ESTADO_CONTENEDOR: :OLD.estado. Se usa en: Antes/después.
- **5. Por qué existe:** CHECK: distinto del nuevo.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_HIST_ESTADOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 1
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_HIST_ESTADOS: estado_anterior IN (1, 2, 3) AND estado_nuevo IN (1, 2, 3) AND estado_anterior <> estado_nuevo (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_HIST_ESTADOS · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** La tabla HISTORIAL_ESTADO_CONTENEDOR no podría guardar este dato (:OLD.estado). CHECK: distinto del nuevo.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_HIST_ESTADOS y por qué esos y no otros? · ¿Por qué ESTADO_ANTERIOR es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_ANTERIOR, COUNT(*) AS filas FROM HISTORIAL_ESTADO_CONTENEDOR GROUP BY ESTADO_ANTERIOR ORDER BY ESTADO_ANTERIOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. ESTADO_ANTERIOR es la casilla que guarda: :OLD.estado. Nunca puede quedar vacía.

#### 169. `HISTORIAL_ESTADO_CONTENEDOR.ESTADO_NUEVO`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** ESTADO_NUEVO
- **3. Qué significa:** :NEW.estado.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de HISTORIAL_ESTADO_CONTENEDOR: :NEW.estado. Se usa en: Antes/después.
- **5. Por qué existe:** Idem.
- **6. Tipo de dato:** NUMBER(1) (entero de 1 dígito)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** CHECK CK_HIST_ESTADOS
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2
- **15. Ejemplo de valor incorrecto:** Un valor que rompa CK_HIST_ESTADOS: estado_anterior IN (1, 2, 3) AND estado_nuevo IN (1, 2, 3) AND estado_anterior <> estado_nuevo (ORA-02290) · Dejarlo vacío (ORA-01400)
- **16. Qué problema evita:** Evita valores fuera de las opciones permitidas; filas incompletas (no se puede guardar sin este dato). Reglas CHECK: CK_HIST_ESTADOS · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** La tabla HISTORIAL_ESTADO_CONTENEDOR no podría guardar este dato (:NEW.estado). Idem.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué valores permite CK_HIST_ESTADOS y por qué esos y no otros? · ¿Por qué ESTADO_NUEVO es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT ESTADO_NUEVO, COUNT(*) AS filas FROM HISTORIAL_ESTADO_CONTENEDOR GROUP BY ESTADO_NUEVO ORDER BY ESTADO_NUEVO;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. ESTADO_NUEVO es la casilla que guarda: :NEW.estado. Nunca puede quedar vacía.

#### 170. `HISTORIAL_ESTADO_CONTENEDOR.ID_ACTOR`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** ID_ACTOR
- **3. Qué significa:** Administrador que lo cambió.
- **4. Para qué sirve:** Sirve para saber a qué fila de USUARIO pertenece cada fila de HISTORIAL_ESTADO_CONTENEDOR y poder unirlas con JOIN. Se usa en: Rendición de cuentas.
- **5. Por qué existe:** Igual que en la auditoría de depósitos.
- **6. Tipo de dato:** NUMBER(10) (entero de hasta 10 dígitos)
- **7. ¿Acepta NULL?:** Sí. NULL = cambio sin actor de aplicación.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** FK: FK_HIST_ACTOR->USUARIO
- **10. Con qué tabla se relaciona:** Apunta a USUARIO (FK FK_HIST_ACTOR)
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 3
- **15. Ejemplo de valor incorrecto:** Un número que no exista en USUARIO (ORA-02291)
- **16. Qué problema evita:** Evita registros huérfanos (no puede apuntar a un(a) USUARIO que no existe). Integridad referencial con USUARIO · Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** Se perdería el vínculo entre HISTORIAL_ESTADO_CONTENEDOR y USUARIO: no sabríamos a qué USUARIO pertenece cada fila y no se podrían cruzar los datos (ni en SQL ni en Power BI).
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué pasa si intento guardar un ID_ACTOR que no existe en USUARIO? (respuesta: ORA-02291) · ¿Qué significa que ID_ACTOR esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT t.ID_ACTOR, COUNT(*) AS filas FROM HISTORIAL_ESTADO_CONTENEDOR t JOIN USUARIO r ON r.ID_USUARIO = t.ID_ACTOR GROUP BY t.ID_ACTOR ORDER BY 2 DESC FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. ID_ACTOR es la casilla que guarda: Administrador que lo cambió. Es un «puntero»: copia el número de una fila de USUARIO para unir las dos tablas. Puede quedar vacía: NULL = cambio sin actor de aplicación.

#### 171. `HISTORIAL_ESTADO_CONTENEDOR.USUARIO_BD`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** USUARIO_BD
- **3. Qué significa:** Cuenta Oracle.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de HISTORIAL_ESTADO_CONTENEDOR: cuenta Oracle. Se usa en: Diagnóstico.
- **5. Por qué existe:** Idem.
- **6. Tipo de dato:** VARCHAR2(128 CHAR) (hasta 128 caracteres)
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1. INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** ECOACCESS_BD
- **15. Ejemplo de valor incorrecto:** Dejarlo vacío (ORA-01400) · Un texto de más de 128 caracteres (ORA-12899)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato). Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** La tabla HISTORIAL_ESTADO_CONTENEDOR no podría guardar este dato (cuenta Oracle). Idem.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué USUARIO_BD es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT USUARIO_BD FROM HISTORIAL_ESTADO_CONTENEDOR FETCH FIRST 5 ROWS ONLY;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. USUARIO_BD es la casilla que guarda: Cuenta Oracle. Nunca puede quedar vacía.

#### 172. `HISTORIAL_ESTADO_CONTENEDOR.MOTIVO`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** MOTIVO
- **3. Qué significa:** Razón del cambio.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de HISTORIAL_ESTADO_CONTENEDOR: razón del cambio. Se usa en: Reportes de mantenimiento.
- **5. Por qué existe:** Explicar paradas de servicio.
- **6. Tipo de dato:** VARCHAR2(300 CHAR) (hasta 300 caracteres)
- **7. ¿Acepta NULL?:** Sí. NULL si no se informó.
- **8. Valor por defecto:** No tiene
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Trigger 1 (desde el procedimiento). INSERT/UPDATE a través de: TRG_CONTENEDOR_AUD_ESTADO · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** Mantenimiento de la tapa
- **15. Ejemplo de valor incorrecto:** Un texto de más de 300 caracteres (ORA-12899)
- **16. Qué problema evita:** Perder información descriptiva; su tipo y su longitud impiden guardar datos deformes. Vigilada por: TRG_CONTENEDOR_AUD_ESTADO
- **17. Qué pasaría si no existiera:** La tabla HISTORIAL_ESTADO_CONTENEDOR no podría guardar este dato (razón del cambio). Explicar paradas de servicio.
- **18. Consultas, procedimientos y triggers que lo usan:** TRG_CONTENEDOR_AUD_ESTADO
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Qué significa que MOTIVO esté vacío (NULL)?
- **23. SELECT para verlo:** SELECT COUNT(*) AS filas, COUNT(MOTIVO) AS con_valor FROM HISTORIAL_ESTADO_CONTENEDOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. MOTIVO es la casilla que guarda: Razón del cambio. Puede quedar vacía: NULL si no se informó.

#### 173. `HISTORIAL_ESTADO_CONTENEDOR.FECHA_UTC`

- **1. Tabla:** HISTORIAL_ESTADO_CONTENEDOR — Cada cambio de disponibilidad de una caneca (trigger 1).
- **2. Atributo:** FECHA_UTC
- **3. Qué significa:** Cuándo cambió.
- **4. Para qué sirve:** Sirve para que EcoAccess sepa, en cada fila de HISTORIAL_ESTADO_CONTENEDOR: cuándo cambió. Se usa en: Línea de tiempo.
- **5. Por qué existe:** Medir disponibilidad.
- **6. Tipo de dato:** TIMESTAMP(6) (fecha y hora con 6 decimales de segundo (UTC))
- **7. ¿Acepta NULL?:** No (obligatoria). Nunca es NULL.
- **8. Valor por defecto:** SYS_EXTRACT_UTC(SYSTIMESTAMP)
- **9. ¿Es PK o FK? (restricciones):** Ni PK ni FK; sin restricciones adicionales
- **10. Con qué tabla se relaciona:** No es clave: no relaciona tablas
- **11. Qué proceso lo usa:** Historial de disponibilidad de canecas (disparador 1)
- **12. Quién lo llena:** Oracle. INSERT/UPDATE directo sobre HISTORIAL_ESTADO_CONTENEDOR (catálogo o datos de prueba) · Nunca DELETE desde la aplicación (se conserva la historia)
- **13. Quién lo consulta:** Administrador. SELECT directo sobre la tabla (consultas de exposición y SQL Developer)
- **14. Ejemplo de valor correcto:** 2026-11-10 08:00
- **15. Ejemplo de valor incorrecto:** Un valor de otro tipo de dato (ORA-01722 o ORA-01843)
- **16. Qué problema evita:** Evita filas incompletas (no se puede guardar sin este dato); errores de hora (se guarda en UTC). Dato descriptivo: no tiene regla adicional; lo protegen su tipo y su longitud
- **17. Qué pasaría si no existiera:** La tabla HISTORIAL_ESTADO_CONTENEDOR no podría guardar este dato (cuándo cambió). Medir disponibilidad.
- **18. Consultas, procedimientos y triggers que lo usan:** Ningún paquete ni disparador la nombra; se consulta con SELECT directo
- **19. ¿Se usa en C#?:** Tabla nueva del modelo académico (no existe en P3); C# la consulta por vistas o paquetes.
- **20. ¿Se usa en Telegram?:** Permite explicar por qué una caneca está fuera de servicio.
- **21. ¿Se usa en Power BI?:** No se expone en Power BI.
- **22. Cómo podría preguntarlo el profesor:** ¿Por qué la fecha se guarda en UTC y se muestra en hora de Colombia? · ¿Por qué FECHA_UTC es obligatorio (NOT NULL)?
- **23. SELECT para verlo:** SELECT MIN(FECHA_UTC) AS primera, MAX(FECHA_UTC) AS ultima, COUNT(FECHA_UTC) AS con_valor FROM HISTORIAL_ESTADO_CONTENEDOR;
- **24. Explicación sencilla (para quien empieza):** Piensa en HISTORIAL_ESTADO_CONTENEDOR como una hoja de cálculo donde cada fila es un cambio de estado de una caneca. FECHA_UTC es la casilla que guarda: Cuándo cambió. Nunca puede quedar vacía.

