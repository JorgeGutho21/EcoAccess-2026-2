-- EcoAccess Challenge: bancos de preguntas por competencia y ronda de DIEZ preguntas. Requiere V031 (y V032 aplicada).
-- Aditiva y repetible. No modifica la ronda oficial abierta ni sus 7 preguntas, opciones o respuestas:
--   - ECO_JUEGO_BANCOS / ECO_JUEGO_BANCO_PREGUNTAS: cada banco fija qué preguntas se juegan y en qué orden (1..N).
--       banco 1 «CLASICO-7»   = las 7 preguntas de V031 (ids 1..7, orden 1..7), sin cambios.
--       banco 2 «EXPO2026-10» = 10 preguntas nuevas (ids 101..110; opciones 1011..1104).
--   - ECO_JUEGO_COMPETENCIAS.BANCO_ID (nulo = banco 1): las competencias existentes, incluida la oficial abierta, quedan
--     con BANCO_ID nulo y siguen jugando exactamente las mismas 7 preguntas.
--   - Una competencia oficial nueva PREPARADA «RONDA-10-EXPO2026» con el banco de 10 (solo se abre desde el panel, y
--     el panel no abre una oficial mientras otra esté en curso).
--   - Los CHECK de ECO_JUEGO_INTENTOS admiten hasta 10 preguntas (puntaje <= 1000, aciertos + errores <= 10,
--     pregunta_actual <= 11). Se crean los nuevos antes de retirar los antiguos: nunca hay un instante sin control.
--   - Disparadores de integridad: el banco de una competencia no cambia fuera de PREPARADA, y el contenido de un banco no
--     cambia mientras alguna competencia que lo usa ya se abrió.
-- La explicación educativa usa la columna EXPLICACION que ya existe. ES_CORRECTA queda en 'N': la clave de esta ronda se
-- carga con un script privado fuera del repositorio (el repositorio es público).
-- Las 10 preguntas se juegan solo con el paquete de V035; aplicar V035 inmediatamente después de V034.
-- Reversión: database/06_migraciones/reversion/R034__juego_bancos_ronda10.sql (después de R035).
WHENEVER SQLERROR EXIT SQL.SQLCODE
SET SERVEROUTPUT ON
DECLARE
    n NUMBER;
    PROCEDURE crear_si_falta(p_tipo VARCHAR2, p_nombre VARCHAR2, p_ddl VARCHAR2) IS
        existe NUMBER;
    BEGIN
        SELECT COUNT(*) INTO existe FROM user_objects WHERE object_type = p_tipo AND object_name = p_nombre;
        IF existe = 0 THEN
            EXECUTE IMMEDIATE p_ddl;
            DBMS_OUTPUT.PUT_LINE('V034: creado ' || p_tipo || ' ' || p_nombre);
        ELSE
            DBMS_OUTPUT.PUT_LINE('V034: ya existía ' || p_tipo || ' ' || p_nombre);
        END IF;
    END;
    -- Agrega una restricción si no existe (por nombre).
    PROCEDURE restriccion(p_tabla VARCHAR2, p_nombre VARCHAR2, p_ddl VARCHAR2) IS
        existe NUMBER;
    BEGIN
        SELECT COUNT(*) INTO existe FROM user_constraints WHERE table_name = p_tabla AND constraint_name = p_nombre;
        IF existe = 0 THEN
            EXECUTE IMMEDIATE 'ALTER TABLE ' || p_tabla || ' ADD CONSTRAINT ' || p_nombre || ' ' || p_ddl;
            DBMS_OUTPUT.PUT_LINE('V034: restricción ' || p_nombre || ' creada');
        END IF;
    END;
    PROCEDURE retirar(p_tabla VARCHAR2, p_nombre VARCHAR2) IS
        existe NUMBER;
    BEGIN
        SELECT COUNT(*) INTO existe FROM user_constraints WHERE table_name = p_tabla AND constraint_name = p_nombre;
        IF existe = 1 THEN
            EXECUTE IMMEDIATE 'ALTER TABLE ' || p_tabla || ' DROP CONSTRAINT ' || p_nombre;
            DBMS_OUTPUT.PUT_LINE('V034: restricción ' || p_nombre || ' retirada');
        END IF;
    END;
BEGIN
    -- Si hay jugadores en ese instante, el DDL espera su turno en lugar de fallar con ORA-00054.
    EXECUTE IMMEDIATE 'ALTER SESSION SET ddl_lock_timeout = 30';

    crear_si_falta('TABLE', 'ECO_JUEGO_BANCOS', q'[
        CREATE TABLE eco_juego_bancos (
            id          NUMBER(4)          NOT NULL,
            codigo      VARCHAR2(40 CHAR)  NOT NULL,
            nombre      VARCHAR2(120 CHAR) NOT NULL,
            creado_en   TIMESTAMP WITH TIME ZONE DEFAULT SYSTIMESTAMP NOT NULL,
            CONSTRAINT pk_eco_juego_bancos PRIMARY KEY (id),
            CONSTRAINT uq_eco_juego_bancos_codigo UNIQUE (codigo)
        )]');

    crear_si_falta('TABLE', 'ECO_JUEGO_BANCO_PREGUNTAS', q'[
        CREATE TABLE eco_juego_banco_preguntas (
            banco_id     NUMBER(4)  NOT NULL,
            orden        NUMBER(2)  NOT NULL,
            pregunta_id  NUMBER(3)  NOT NULL,
            CONSTRAINT pk_eco_juego_banco_preg PRIMARY KEY (banco_id, orden),
            CONSTRAINT uq_eco_juego_banco_preg UNIQUE (banco_id, pregunta_id),
            CONSTRAINT fk_eco_juego_bp_banco FOREIGN KEY (banco_id) REFERENCES eco_juego_bancos (id),
            CONSTRAINT fk_eco_juego_bp_pregunta FOREIGN KEY (pregunta_id) REFERENCES eco_juego_preguntas (id),
            CONSTRAINT ck_eco_juego_bp_orden CHECK (orden BETWEEN 1 AND 10)
        )]');
    crear_si_falta('INDEX', 'IX_ECO_JUEGO_BP_PREGUNTA', 'CREATE INDEX ix_eco_juego_bp_pregunta ON eco_juego_banco_preguntas (pregunta_id)');

    SELECT COUNT(*) INTO n FROM user_tab_columns WHERE table_name = 'ECO_JUEGO_COMPETENCIAS' AND column_name = 'BANCO_ID';
    IF n = 0 THEN
        -- Columna nula sin valor por defecto: no reescribe filas y deja a las competencias existentes en el banco 1.
        EXECUTE IMMEDIATE 'ALTER TABLE eco_juego_competencias ADD (banco_id NUMBER(4))';
        DBMS_OUTPUT.PUT_LINE('V034: columna ECO_JUEGO_COMPETENCIAS.BANCO_ID creada');
    END IF;
    restriccion('ECO_JUEGO_COMPETENCIAS', 'FK_ECO_JUEGO_COMP_BANCO', 'FOREIGN KEY (banco_id) REFERENCES eco_juego_bancos (id)');
    crear_si_falta('INDEX', 'IX_ECO_JUEGO_COMP_BANCO', 'CREATE INDEX ix_eco_juego_comp_banco ON eco_juego_competencias (banco_id)');

    -- Límites de un intento: hasta 10 preguntas. Primero los nuevos (más amplios), luego se retiran los de 7.
    restriccion('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_PUNTAJE10', 'CHECK (puntaje BETWEEN 0 AND 1000 AND MOD(puntaje, 100) = 0)');
    restriccion('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_CONTEO10', 'CHECK (aciertos + errores <= 10 AND aciertos >= 0 AND errores >= 0)');
    restriccion('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_PREGUNTA10', 'CHECK (pregunta_actual BETWEEN 1 AND 11)');
    retirar('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_PUNTAJE');
    retirar('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_CONTEO');
    retirar('ECO_JUEGO_INTENTOS', 'CK_ECO_JUEGO_INT_PREGUNTA');
END;
/

MERGE INTO eco_juego_bancos d
USING (
    SELECT 1 id, 'CLASICO-7' codigo, 'Siete preguntas oficiales (V031)' nombre FROM dual UNION ALL
    SELECT 2, 'EXPO2026-10', 'Diez preguntas: EcoAccess, residuos, ESP32, servos, SOLID, grafos, Oracle, Wallet, arquitectura y aperturas' FROM dual
) s ON (d.id = s.id)
WHEN MATCHED THEN UPDATE SET d.nombre = s.nombre
WHEN NOT MATCHED THEN INSERT (id, codigo, nombre) VALUES (s.id, s.codigo, s.nombre)
/

-- Preguntas nuevas. ORDEN es la columna histórica de V031 (única en la tabla): 11..20 para no chocar con las 7
-- oficiales. El orden que ve el participante (1..10) lo define ECO_JUEGO_BANCO_PREGUNTAS.
MERGE INTO eco_juego_preguntas d
USING (
    SELECT 101 id, 11 orden, 'BASICA' nivel, 'EcoAccess' tema,
           '¿Cuál es el propósito de EcoAccess?' enunciado,
           'EcoAccess es un prototipo para orientar y registrar la disposición de residuos aprovechables en Valledupar: indica la caneca que corresponde, abre su tapa y deja el registro de la operación. No tritura, no fabrica productos ni reemplaza la recolección de la ciudad.' explicacion FROM dual UNION ALL
    SELECT 102, 12, 'BASICA', 'Separación',
           'Una botella plástica pequeña, vacía y limpia, ¿en qué caneca va?',
           'Va en la caneca blanca, la de los residuos aprovechables limpios y secos (plástico, vidrio, papel, cartón y metal). La verde recibe orgánicos aprovechables, como la cáscara de banano, y la negra lo no aprovechable, como la servilleta usada.' FROM dual UNION ALL
    SELECT 103, 13, 'INTERMEDIA', 'ESP32',
           'En la estación de EcoAccess, ¿qué pin del ESP32 envía la señal al servomotor de la caneca verde?',
           'El firmware ecoaccess-calibracion-v2 asigna un pin por tapa: verde en GPIO 23, blanca en GPIO 19 y negra en GPIO 18. El ESP32 genera la señal PWM que mueve cada servo; las órdenes le llegan por USB desde el panel local.' FROM dual UNION ALL
    SELECT 104, 14, 'INTERMEDIA', 'Servomotores',
           'Según la calibración guardada en el ESP32, ¿entre qué ángulos se mueve la tapa de la caneca blanca (cerrada → abierta)?',
           'Cada tapa guarda su propia calibración en la memoria NVS del ESP32: verde de 0° a 50°, blanca de 0° a 70° y negra de 0° a 40°. Los servos no tienen sensores de posición: el firmware sabe qué ángulo ordenó, pero no puede comprobar que la tapa se movió.' FROM dual UNION ALL
    SELECT 105, 15, 'INTERMEDIA', 'SOLID',
           'En EcoAccess, un nuevo proveedor de reconocimiento visual implementa IClasificadorMaterial sin modificar ServicioClasificacionImagen. ¿Qué principio SOLID se aplica?',
           'Es el principio abierto/cerrado: el sistema se extiende con una clase nueva que implementa la interfaz, sin modificar el servicio que ya funciona. Responsabilidad única sería que cada clase tenga un solo motivo de cambio; inversión de dependencias, depender de interfaces y no de clases concretas.' FROM dual UNION ALL
    SELECT 106, 16, 'AVANZADA', 'Grafos',
           'Si los puntos de disposición de Valledupar fueran nodos y las calles aristas con su distancia en metros, ¿qué algoritmo calcula la ruta más corta desde la estación hasta un punto?',
           'El algoritmo de Dijkstra encuentra el camino de menor costo en un grafo con pesos no negativos, como las distancias. La búsqueda en anchura solo minimiza el número de aristas, y ordenar o buscar en una lista no recorre un grafo.' FROM dual UNION ALL
    SELECT 107, 17, 'AVANZADA', 'Oracle Cloud',
           '¿Dónde se guardan las operaciones de la estación y los resultados de este concurso?',
           'En Oracle Autonomous Database, en Oracle Cloud. Allí quedan las operaciones de la estación, los usuarios y este concurso; el puntaje y el tiempo de cada participante los calcula PL/SQL en el servidor, no el navegador.' FROM dual UNION ALL
    SELECT 108, 18, 'AVANZADA', 'Wallet',
           '¿Para qué usa EcoAccess el Wallet de Oracle?',
           'El Wallet contiene los certificados y la configuración de red para conectarse a Oracle Autonomous con TLS mutuo (mTLS): el cliente y la base se autentican entre sí y el tráfico viaja cifrado. Por eso es un archivo privado que nunca se publica en el repositorio.' FROM dual UNION ALL
    SELECT 109, 19, 'AVANZADA', 'Arquitectura web',
           'Cuando alguien abre una caneca desde la página pública de EcoAccess, ¿qué camino sigue la orden?',
           'GitHub Pages solo sirve archivos estáticos: la orden viaja por un túnel HTTPS a la pasarela local, que la entrega al panel local (el único proceso dueño del puerto del ESP32), y el panel se la envía al ESP32. Además exige el QR presencial y ACTIVAR CONTROL en el panel.' FROM dual UNION ALL
    SELECT 110, 20, 'AVANZADA', 'Política de apertura',
           'Si una persona va a depositar residuos en dos canecas distintas a la vez, ¿cuánto tiempo permanecen abiertas según la política de EcoAccess?',
           'La política suma 7 segundos por cada caneca distinta: una caneca 7 s, dos canecas 14 s y tres canecas 21 s. Es la misma regla en C#, en el panel, en la pasarela y en la web.' FROM dual
) s ON (d.id = s.id)
WHEN MATCHED THEN UPDATE SET d.orden = s.orden, d.nivel = s.nivel, d.tema = s.tema, d.enunciado = s.enunciado,
                             d.imagen = NULL, d.opciones_con_imagen = 'N', d.explicacion = s.explicacion
WHEN NOT MATCHED THEN INSERT (id, orden, nivel, tema, enunciado, imagen, opciones_con_imagen, explicacion)
                      VALUES (s.id, s.orden, s.nivel, s.tema, s.enunciado, NULL, 'N', s.explicacion)
/

-- Opciones (4 por pregunta). ES_CORRECTA no se toca aquí: se insertan con 'N'.
MERGE INTO eco_juego_opciones d
USING (
    SELECT 1011 id, 101 pregunta_id, 'A' letra, 'Orientar a la persona, abrir la caneca que corresponde y registrar la disposición de residuos aprovechables' texto FROM dual UNION ALL
    SELECT 1012, 101, 'B', 'Vender canecas inteligentes para los hogares' FROM dual UNION ALL
    SELECT 1013, 101, 'C', 'Triturar y compactar los residuos dentro de la caneca' FROM dual UNION ALL
    SELECT 1014, 101, 'D', 'Reemplazar la recolección de residuos de la ciudad' FROM dual UNION ALL
    SELECT 1021, 102, 'A', 'Caneca verde' FROM dual UNION ALL
    SELECT 1022, 102, 'B', 'Caneca negra' FROM dual UNION ALL
    SELECT 1023, 102, 'C', 'Caneca blanca' FROM dual UNION ALL
    SELECT 1024, 102, 'D', 'En cualquiera, porque el plástico no se aprovecha' FROM dual UNION ALL
    SELECT 1031, 103, 'A', 'GPIO 18' FROM dual UNION ALL
    SELECT 1032, 103, 'B', 'GPIO 19' FROM dual UNION ALL
    SELECT 1033, 103, 'C', 'GPIO 23' FROM dual UNION ALL
    SELECT 1034, 103, 'D', 'GPIO 2' FROM dual UNION ALL
    SELECT 1041, 104, 'A', 'De 0° a 50°' FROM dual UNION ALL
    SELECT 1042, 104, 'B', 'De 0° a 70°' FROM dual UNION ALL
    SELECT 1043, 104, 'C', 'De 0° a 40°' FROM dual UNION ALL
    SELECT 1044, 104, 'D', 'De 90° a 180°' FROM dual UNION ALL
    SELECT 1051, 105, 'A', 'Responsabilidad única' FROM dual UNION ALL
    SELECT 1052, 105, 'B', 'Abierto/cerrado' FROM dual UNION ALL
    SELECT 1053, 105, 'C', 'Segregación de interfaces' FROM dual UNION ALL
    SELECT 1054, 105, 'D', 'Inversión de dependencias' FROM dual UNION ALL
    SELECT 1061, 106, 'A', 'Algoritmo de Dijkstra' FROM dual UNION ALL
    SELECT 1062, 106, 'B', 'Ordenamiento burbuja' FROM dual UNION ALL
    SELECT 1063, 106, 'C', 'Búsqueda binaria' FROM dual UNION ALL
    SELECT 1064, 106, 'D', 'Búsqueda en anchura, que ignora las distancias' FROM dual UNION ALL
    SELECT 1071, 107, 'A', 'En una hoja de cálculo del portátil de la estación' FROM dual UNION ALL
    SELECT 1072, 107, 'B', 'En Oracle Autonomous Database, en Oracle Cloud' FROM dual UNION ALL
    SELECT 1073, 107, 'C', 'En la memoria del ESP32' FROM dual UNION ALL
    SELECT 1074, 107, 'D', 'En el historial del chat de Telegram' FROM dual UNION ALL
    SELECT 1081, 108, 'A', 'Para pagar los ecopuntos de los usuarios' FROM dual UNION ALL
    SELECT 1082, 108, 'B', 'Para conectarse a Oracle Autonomous con TLS mutuo (mTLS) mediante certificados' FROM dual UNION ALL
    SELECT 1083, 108, 'C', 'Para guardar las fotografías de los residuos' FROM dual UNION ALL
    SELECT 1084, 108, 'D', 'Para calibrar los ángulos de los servomotores' FROM dual UNION ALL
    SELECT 1091, 109, 'A', 'GitHub Pages → pasarela HTTPS → panel local → ESP32' FROM dual UNION ALL
    SELECT 1092, 109, 'B', 'GitHub Pages → ESP32, directamente por wifi' FROM dual UNION ALL
    SELECT 1093, 109, 'C', 'Telegram → Oracle Cloud → servomotor' FROM dual UNION ALL
    SELECT 1094, 109, 'D', 'ESP32 → GitHub Pages → panel local' FROM dual UNION ALL
    SELECT 1101, 110, 'A', '7 segundos' FROM dual UNION ALL
    SELECT 1102, 110, 'B', '14 segundos' FROM dual UNION ALL
    SELECT 1103, 110, 'C', '21 segundos' FROM dual UNION ALL
    SELECT 1104, 110, 'D', '60 segundos' FROM dual
) s ON (d.id = s.id)
WHEN MATCHED THEN UPDATE SET d.pregunta_id = s.pregunta_id, d.letra = s.letra, d.texto = s.texto, d.imagen = NULL
WHEN NOT MATCHED THEN INSERT (id, pregunta_id, letra, texto, imagen, es_correcta)
                      VALUES (s.id, s.pregunta_id, s.letra, s.texto, NULL, 'N')
/

-- Composición de los bancos. Solo inserta lo que falta: un banco en uso nunca se reescribe (ver disparador abajo).
MERGE INTO eco_juego_banco_preguntas d
USING (
    SELECT 1 banco_id, LEVEL orden, LEVEL pregunta_id FROM dual CONNECT BY LEVEL <= 7
    UNION ALL
    SELECT 2, LEVEL, 100 + LEVEL FROM dual CONNECT BY LEVEL <= 10
) s ON (d.banco_id = s.banco_id AND d.orden = s.orden)
WHEN NOT MATCHED THEN INSERT (banco_id, orden, pregunta_id) VALUES (s.banco_id, s.orden, s.pregunta_id)
/

-- Competencia oficial PREPARADA con el banco de 10. Solo se crea si no existe; nunca se toca después.
INSERT INTO eco_juego_competencias (codigo, nombre, estado, es_prueba, banco_id)
SELECT 'RONDA-10-EXPO2026', 'EcoAccess Challenge · ExpoSoftware 2026 · Ronda de 10', 'PREPARADA', 'N', 2 FROM dual
 WHERE NOT EXISTS (SELECT 1 FROM eco_juego_competencias WHERE codigo = 'RONDA-10-EXPO2026')
/
COMMIT
/

-- El banco de una competencia solo se elige mientras está PREPARADA.
CREATE OR REPLACE TRIGGER tr_eco_juego_comp_banco
BEFORE UPDATE OF banco_id ON eco_juego_competencias
FOR EACH ROW
BEGIN
    IF NVL(:OLD.banco_id, 1) <> NVL(:NEW.banco_id, 1) AND :OLD.estado <> 'PREPARADA' THEN
        RAISE_APPLICATION_ERROR(-20431, 'El banco de preguntas de una competencia ya abierta no se puede cambiar.');
    END IF;
END;
/

-- Un banco que ya usa una competencia abierta (o cerrada) no cambia: las respuestas guardadas dependen de él.
CREATE OR REPLACE TRIGGER tr_eco_juego_banco_inmutable
BEFORE INSERT OR UPDATE OR DELETE ON eco_juego_banco_preguntas
FOR EACH ROW
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM eco_juego_competencias
     WHERE NVL(banco_id, 1) IN (:OLD.banco_id, :NEW.banco_id) AND estado <> 'PREPARADA';
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20432, 'Ese banco de preguntas ya está en uso por una competencia abierta o finalizada.');
    END IF;
END;
/
PROMPT V034_JUEGO_BANCOS_RONDA10_APLICADA
