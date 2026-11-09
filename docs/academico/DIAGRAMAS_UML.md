# Diagramas UML del tercer corte — EcoAccess P3

Fuente reproducible: Mermaid (GitHub y VS Code lo dibujan sin instalar nada). Cada diagrama nombra clases, interfaces y
métodos que existen en la rama `desarrollo/auditoria-tercer-corte`; se simplifican parámetros y se omiten miembros que
no aportan a la explicación. El modelo entidad-relación detallado (columnas) sigue en `database/01_modelo/DER.md`.

Versiones exportadas: `docs/academico/diagramas/01_clases.svg` … `10_conexion_oracle.svg`, generadas con Mermaid 10.9.1
(las 10 fuentes de este archivo se dibujaron sin errores el 09/11/2026). Si se cambia un diagrama aquí, se regenera su SVG.

## 1. Diagrama de clases (equivalente EcoAccess del patrón DAO + Repository del curso)

En la guía del curso la cadena es `MainWindow → ProductoService → IProductoRepository → ProductoRepository → IProductoDAO
→ ProductoDAO → Conexion → SQL Server`. En EcoAccess el ejemplo equivalente es el catálogo de materiales:

```mermaid
classDiagram
    direction LR
    class FormMateriales {
        «view» Presentacion
        -ServicioMateriales _servicio
    }
    class ServicioMateriales {
        «service» Logica
        +RegistrarAsync(idActor, codigo, nombre, categoria, ...) Resultado~int~
        +ListarAsync(soloHabilitados) IReadOnlyList~Material~
        +HabilitarAsync(idActor, idMaterial) Resultado
        +DeshabilitarAsync(idActor, idMaterial) Resultado
    }
    class IRepositorioMateriales {
        <<interface>>
        «Logica/Contratos»
        +ObtenerPorIdAsync(id) Material
        +ExisteCodigoAsync(codigo) bool
        +ListarAsync(soloHabilitados) IReadOnlyList~Material~
        +AgregarAsync(material) int
        +HabilitarAsync(id)
        +DeshabilitarAsync(id)
    }
    class IRepositorioUsuarios {
        <<interface>>
    }
    class RepositorioMaterialesOracle {
        «Datos/Repositorios»
        -FabricaConexionesOracle _fabrica
        -Leer(DbDataReader) Material
    }
    class FabricaConexionesOracle {
        «Datos/Oracle»
        -string _cadenaConexion
        ~AbrirAsync(ct) OracleConnection
    }
    class ComandosOracle {
        <<static>>
        +Crear(conexion, sql, transaccion) OracleCommand
        +Agregar(comando, nombre, OracleDbType, valor) OracleParameter
    }
    class CadenaConexionOracle {
        <<static>>
        +Resolver(privada, deAppConfig) string
        +TieneContrasena(cadena) bool
    }
    class Material {
        «entity» Entidades
        +int Id
        +string Codigo
        +string Nombre
        +CategoriaMaterial Categoria
        +bool Habilitado
        +Habilitar()
        +Deshabilitar()
    }
    FormMateriales --> ServicioMateriales : usa
    ServicioMateriales --> IRepositorioMateriales : usa (inyección)
    ServicioMateriales --> IRepositorioUsuarios : usa (permisos)
    RepositorioMaterialesOracle ..|> IRepositorioMateriales : realiza
    RepositorioMaterialesOracle --> FabricaConexionesOracle : abre conexiones
    RepositorioMaterialesOracle ..> ComandosOracle : crea comandos
    FabricaConexionesOracle ..> CadenaConexionOracle : cadena resuelta en Program
    ServicioMateriales ..> Material
    RepositorioMaterialesOracle ..> Material : mapea filas
```

Diferencia con el curso (justificada en la matriz, criterio C-05): EcoAccess no tiene una clase DAO separada del
Repository. `RepositorioMaterialesOracle` cumple las dos responsabilidades de la guía: ejecuta el SQL con ADO.NET
(papel del DAO) y expone una colección orientada al dominio con interfaz propia (papel del Repository). La interfaz vive en
Lógica y la implementación en Datos (inversión de dependencias), de modo que la Lógica no conoce Oracle.

## 2. Diagrama de paquetes (proyectos y referencias reales de los .csproj)

```mermaid
flowchart TB
    subgraph Raices["Raíces de composición"]
        P["EcoAccess.P3.Presentacion<br/>WinForms · Program.cs · App.config"]
        B["EcoAccess.P3.Bot<br/>Telegram · bridge ESP32 · Program.cs"]
    end
    L["EcoAccess.P3.Logica<br/>Servicios · Contratos (IRepositorio*)"]
    D["EcoAccess.P3.Datos<br/>Oracle · Repositorios (ADO.NET / ODP.NET)"]
    E["EcoAccess.P3.Entidades<br/>POCO con invariantes"]
    C["EcoAccess.P3.Compatibilidad<br/>soporte de C# 9 en .NET Framework 4.8"]
    P --> L
    P --> D
    P --> E
    B --> L
    B --> D
    B --> E
    D --> L
    D --> E
    L --> E
    E -.-> C
    L -.-> C
    D -.-> C
```

`PruebasArquitectura.ReferenciasDeProyecto_CoincidenConElDiagramaDeDependencias` falla si un `.csproj` se aparta de este
diagrama.

## 3. Diagrama de componentes

```mermaid
flowchart LR
    subgraph Escritorio["Cliente Windows"]
        GUI["Formularios WinForms"]
    end
    subgraph Backend["Proceso EcoAccess.P3.Bot"]
        TG["Adaptador Telegram<br/>BuclePolling · ProcesadorUpdates"]
        VIS["Reconocimiento visual<br/>IClasificadorMaterial"]
        BR["Bridge del dispositivo<br/>AplicacionBridge · ProtocoloDispositivoHmac"]
    end
    LOG["Lógica de negocio<br/>Servicio* + Politica*"]
    DAT["Datos<br/>Repositorio*Oracle"]
    ORA[("Oracle<br/>esquema ECOACCESS_P3_APP")]
    ESP["ESP32<br/>firmware servosecoacces"]
    GUI --> LOG
    TG --> LOG
    VIS --> LOG
    BR --> LOG
    LOG --> DAT
    DAT --> ORA
    BR <-->|"serie / HTTP local, mensajes HMAC"| ESP
```

## 4. Diagrama de secuencia: registro y autorización de un depósito

```mermaid
sequenceDiagram
    autonumber
    actor C as Ciudadano
    participant T as Telegram
    participant PU as ProcesadorUpdates (Bot)
    participant SC as ServicioConversacionTelegram
    participant CI as ServicioClasificacionImagen
    participant PA as PoliticaAperturaAutomatica
    participant SI as ServicioInteracciones
    participant RI as RepositorioInteraccionesOracle
    participant O as Oracle
    participant BR as Bridge (ActuacionSerial)
    participant E as ESP32
    participant N as ServicioNotificacionesTelegram
    C->>T: escanea el QR de la caneca y envía la foto
    T->>PU: update (polling)
    PU->>O: UPDATE_TELEGRAM (update_id único: sin duplicados)
    PU->>SC: ProcesarAsync(mensaje)
    SC->>CI: ClasificarAsync(foto)
    CI-->>SC: Resultado<ClasificacionImagen>
    SC->>PA: Permite(clasificación, caneca)
    alt material admitido y evidencia suficiente
        SC->>SI: IniciarAsync(actor, caneca, canal, material)
        SI->>RI: EmitirOrdenAsync(interacción, orden)
        RI->>O: transacción: comprueba clave_idempotencia, actualiza interacción e INSERT orden_apertura
        O-->>RI: id de la orden
        BR->>SI: SigueAutorizadoComandoAsync(comando)
        BR->>E: comando de apertura firmado (HMAC, nonce, vigencia)
        E-->>BR: recibo «apertura aplicada»
        BR->>SI: RegistrarEventoAsync(Apertura)
        SI->>RI: RegistrarEventoAsync(evento, cambio de orden)
        RI->>O: INSERT evento_dispositivo + UPDATE orden_apertura
        N->>O: RevisarAsync lee el evento de apertura registrado
        N-->>T: «Caneca abierta. Tienes 10 segundos…» (MensajesTelegram.CanecaAbierta)
        Note over E: el firmware espera ABIERTO_MS = 10000 y cierra solo
        E-->>BR: recibo «cierre completado»
        BR->>SI: RegistrarEventoAsync(Cierre)
        SI->>RI: RegistrarEventoAsync(evento, cambio, depósito pendiente de verificación)
    else dudosa, no admitida o caneca ocupada
        SC-->>T: orientación breve, sin abrir
    end
```

El aviso de los 10 s solo sale después del evento de apertura registrado en Oracle; el cierre lo ejecuta el ESP32 sin
depender de Telegram.

## 5. Diagrama de casos de uso

```mermaid
flowchart LR
    Ciudadano([Ciudadano])
    Operario([Operario])
    Administrador([Administrador])
    ESP([ESP32])
    subgraph Sistema["EcoAccess P3"]
        UC1(Registrarse e iniciar sesión)
        UC2(Clasificar residuo con foto en Telegram)
        UC3(Depositar en la caneca autorizada)
        UC4(Consultar ecopuntos y estadísticas)
        UC5(Opinar o comentar)
        UC6(Registrar depósito asistido)
        UC7(Gestionar materiales, puntos y canecas)
        UC8(Gestionar usuarios y roles)
        UC9(Consultar tableros e informes)
        UC10(Confirmar apertura y cierre)
    end
    Ciudadano --- UC1
    Ciudadano --- UC2
    Ciudadano --- UC3
    Ciudadano --- UC4
    Ciudadano --- UC5
    Operario --- UC6
    Operario --- UC9
    Administrador --- UC7
    Administrador --- UC8
    Administrador --- UC9
    ESP --- UC10
    UC2 -. incluye .-> UC3
    UC3 -. incluye .-> UC10
```

## 6. Diagrama de despliegue

```mermaid
flowchart TB
    subgraph PC["Equipo Windows (.NET Framework 4.8)"]
        WF["EcoAccess.P3.Presentacion.exe<br/>+ .exe.config (App.config)"]
        BOT["EcoAccess.P3.Bot.exe<br/>polling Telegram + bridge"]
        SEC["User Secrets del perfil<br/>(cadena con contraseña, token)"]
        WAL["Wallet mTLS (TNS_ADMIN)"]
    end
    subgraph Nube["Servicios externos"]
        TGS["Telegram Bot API"]
        IA["Proveedor visual<br/>(LFM3-VL local o Groq con consentimiento)"]
        ADB[("Oracle Autonomous Database<br/>alias ecoaccess_tp")]
    end
    subgraph Estacion["Estación física"]
        ESP["ESP32 + servos de 3 canecas"]
        QR["QR impreso de la estación"]
    end
    WF -->|ODP.NET, TLS| ADB
    BOT -->|ODP.NET, TLS| ADB
    BOT -->|HTTPS| TGS
    BOT -->|HTTPS| IA
    BOT <-->|USB serie, HMAC| ESP
    WF -.-> SEC
    BOT -.-> SEC
    WF -.-> WAL
    BOT -.-> WAL
    QR -.->|enlace t.me| TGS
```

## 7. Diagrama entidad-relación (todas las tablas con clave foránea)

```mermaid
erDiagram
    ROL ||--o{ USUARIO_ROL : "asigna"
    USUARIO ||--o{ USUARIO_ROL : "tiene"
    USUARIO ||--o| CREDENCIAL_ACCESO : "autentica"
    PUNTO_INSTALACION ||--o{ CONTENEDOR : "aloja"
    CONTENEDOR ||--o{ CONTENEDOR_MATERIAL : "admite"
    MATERIAL ||--o{ CONTENEDOR_MATERIAL : "es admitido"
    USUARIO ||--o{ INTERACCION : "inicia"
    CONTENEDOR ||--o{ INTERACCION : "recibe"
    MATERIAL ||--o{ INTERACCION : "declara"
    INTERACCION ||--o{ ORDEN_APERTURA : "autoriza"
    ORDEN_APERTURA ||--o{ EVENTO_DISPOSITIVO : "produce"
    CONTENEDOR ||--o{ EVENTO_DISPOSITIVO : "informa"
    EVENTO_DISPOSITIVO ||--o| DEPOSITO : "origina"
    USUARIO ||--o{ DEPOSITO : "realiza"
    MATERIAL ||--o{ DEPOSITO : "contiene"
    INTERACCION ||--o{ ANALISIS_IMAGEN : "documenta"
    MATERIAL ||--o{ ANALISIS_IMAGEN : "propone"
    INTERACCION ||--o{ CONVERSACION_TELEGRAM : "sigue"
    CONTENEDOR ||--o{ CONVERSACION_TELEGRAM : "apunta"
    MATERIAL ||--o{ CONVERSACION_TELEGRAM : "recuerda"
    USUARIO ||--o{ VINCULO_TELEGRAM : "vincula"
    USUARIO ||--o{ TOKEN_VINCULACION : "solicita"
    TOKEN_VINCULACION ||--o{ VINCULACION_PENDIENTE : "confirma"
    USUARIO ||--o{ VINCULACION_PENDIENTE : "espera"
    MATERIAL ||--o{ REGLA_ECOPUNTOS : "valora"
    USUARIO ||--o{ REGLA_ECOPUNTOS : "define"
    DEPOSITO ||--o{ OPERACION_ECOPUNTOS : "liquida"
    REGLA_ECOPUNTOS ||--o{ OPERACION_ECOPUNTOS : "aplica"
    USUARIO ||--o{ OPERACION_ECOPUNTOS : "recibe"
    OPERACION_ECOPUNTOS ||--o{ MOVIMIENTO_ECOPUNTOS : "registra"
    DEPOSITO ||--o{ MOVIMIENTO_ECOPUNTOS : "soporta"
    USUARIO ||--o{ MOVIMIENTO_ECOPUNTOS : "acumula"
    USUARIO ||--o{ OPINION : "escribe"
    USUARIO ||--o{ COMENTARIO_CIUDADANO : "envía"
    INTERACCION ||--o{ COMENTARIO_CIUDADANO : "refiere"
```

Tablas sin clave foránea: `UPDATE_TELEGRAM` (deduplicación por `update_id`) y `CONSENTIMIENTO_VISION_EXTERNA`. Las
relaciones se extrajeron de los `REFERENCES` de `database/02_tablas/01_crear_tablas_iniciales.sql` y
`database/06_migraciones/V002…V020`.

## 8. Diagrama de actividad: inicio de la aplicación de escritorio

```mermaid
flowchart TD
    A([Inicio Main]) --> B["Leer App.config<br/>ConfigurationManager.ConnectionStrings['OracleP3']"]
    B --> C["Leer User Secrets / ECOACCESS_ConnectionStrings__OracleP3"]
    C --> D{"¿Hay cadena privada?"}
    D -- Sí --> F["VerificadorConexionOracle.ComprobarAsync"]
    D -- No --> E["Plantilla de App.config sin contraseña"]
    E --> G["FormActivacionOracle pide la contraseña"]
    F -- Correcta --> H["Composición: Repositorio*Oracle → Servicio*"]
    F -- Credenciales inválidas / sin servicio --> G
    G -- Oracle la acepta --> I["Guardar cadena en el perfil (User Secrets)"] --> H
    G -- Cancelar --> Z([Fin])
    H --> J["FormInicioSesion"] --> K{"Rol"}
    K -- Administrador / Operario --> L["FormPrincipal"]
    K -- Ciudadano / invitado --> M["FormExplorarEcoAccess"]
```

## 9. Flujo de clasificación y depósito

```mermaid
flowchart TD
    F["Foto recibida"] --> V{"¿JPEG/PNG/WebP ≤ límite?"}
    V -- No --> R1["Rechazo: formato"]
    V -- Sí --> M["Modelo visual describe el objeto"]
    M --> N["InterpretadorNombreObjeto + CatalogoObjetos"]
    N --> Q{"¿Material reconocido y corroborado?"}
    Q -- No --> R2["Pedir otra foto (no abre)"]
    Q -- Sí --> S{"¿La caneca del QR lo admite?"}
    S -- No --> R3["Orientar a la caneca o punto correcto"]
    S -- Sí --> P{"PoliticaAperturaAutomatica.Permite"}
    P -- No --> R2
    P -- Sí --> O["Orden de apertura en Oracle"] --> E["ESP32 confirma apertura"] --> T["10 s (ABIERTO_MS)"] --> X["Cierre y depósito pendiente de verificación"]
```

## 10. Flujo de conexión C# – Oracle (ADO.NET con ODP.NET)

```mermaid
sequenceDiagram
    participant S as Servicio (Logica)
    participant R as Repositorio*Oracle (Datos)
    participant F as FabricaConexionesOracle
    participant P as Pool de ODP.NET
    participant O as Oracle
    S->>R: ListarAsync(...)
    R->>F: AbrirAsync(ct)
    F->>P: new OracleConnection(cadena).OpenAsync()
    P->>O: reutiliza o crea sesión TLS
    R->>R: ComandosOracle.Crear(conexion, sql) · BindByName = true
    R->>R: comando.Agregar(":codigo", OracleDbType.Varchar2, valor)
    R->>O: ExecuteReaderAsync() devuelve DbDataReader (OracleDataReader)
    O-->>R: filas (OracleDataReader)
    R->>R: Leer(lector) → entidad
    R-->>S: IReadOnlyList<Entidad>
    Note over R,P: using cierra lector, comando y conexión, y la conexión vuelve al pool
```
