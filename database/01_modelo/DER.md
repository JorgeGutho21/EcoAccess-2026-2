# Diagrama entidad-relación — EcoAccess P3 (versión 001 y migración V003)

Corresponde a `database/02_tablas/01_crear_tablas_iniciales.sql`. Detalle de columnas y reglas en `DICCIONARIO_DATOS.md`. GitHub y VS Code muestran el diagrama Mermaid.

```mermaid
erDiagram
    ROL ||--o{ USUARIO_ROL : "se asigna en"
    USUARIO ||--|{ USUARIO_ROL : "tiene"
    PUNTO_INSTALACION ||--o{ CONTENEDOR : "aloja"
    CONTENEDOR ||--o{ CONTENEDOR_MATERIAL : "admite"
    MATERIAL ||--o{ CONTENEDOR_MATERIAL : "es admitido en"

    ROL {
        NUMBER id_rol PK
        NUMBER codigo UK "1 Ciudadano, 2 Operario, 3 Administrador"
        VARCHAR2 descripcion
    }
    USUARIO {
        NUMBER id_usuario PK
        VARCHAR2 nombre
        VARCHAR2 correo_electronico UK "en minúsculas"
        TIMESTAMP fecha_registro_utc
        NUMBER activo "0 o 1"
        VARCHAR2 version_consentimiento "nulo junto con la fecha"
        TIMESTAMP fecha_consentimiento_utc
    }
    USUARIO_ROL {
        NUMBER id_usuario PK, FK
        NUMBER id_rol PK, FK
        TIMESTAMP fecha_asignacion_utc "metadato técnico"
    }
    MATERIAL {
        NUMBER id_material PK
        VARCHAR2 codigo UK "A-Z, 0-9, _"
        VARCHAR2 nombre
        NUMBER categoria "1 a 4"
        VARCHAR2 descripcion
        NUMBER habilitado "0 o 1"
    }
    PUNTO_INSTALACION {
        NUMBER id_punto_instalacion PK
        VARCHAR2 nombre UK
        VARCHAR2 referencia_ubicacion
        NUMBER activo "0 o 1"
    }
    CONTENEDOR {
        NUMBER id_contenedor PK
        NUMBER id_punto_instalacion FK
        VARCHAR2 nombre
        VARCHAR2 identificador_qr UK "16 a 64, base64url"
        TIMESTAMP fecha_alta_utc
        NUMBER estado "1 EnServicio, 2 FueraDeServicio, 3 DadoDeBaja"
    }
    CONTENEDOR_MATERIAL {
        NUMBER id_contenedor PK, FK
        NUMBER id_material PK, FK
    }
```

## Migración V003 — interacciones y depósitos

```mermaid
erDiagram
    CONTENEDOR ||--o{ INTERACCION : "recibe"
    USUARIO |o--o{ INTERACCION : "realiza"
    INTERACCION ||--o| ORDEN_APERTURA : "autoriza"
    ORDEN_APERTURA ||--o{ EVENTO_DISPOSITIVO : "recibe"
    CONTENEDOR ||--o{ EVENTO_DISPOSITIVO : "informa"
    ORDEN_APERTURA ||--o| DEPOSITO : "produce"
    EVENTO_DISPOSITIVO ||--o| DEPOSITO : "detecta"
    MATERIAL ||--o{ DEPOSITO : "declarado o verificado"

    INTERACCION {
        NUMBER id_interaccion PK
        NUMBER id_contenedor FK
        NUMBER id_usuario FK "nulo: visitante o manual"
        NUMBER canal "1 Telegram, 2 Manual, 3 Simulador"
        NUMBER estado "1 a 5"
        NUMBER id_material_confirmado FK
    }
    ORDEN_APERTURA {
        NUMBER id_orden PK
        NUMBER id_interaccion FK, UK
        NUMBER id_contenedor FK "una vigente por caneca"
        VARCHAR2 clave_idempotencia UK
        NUMBER estado "1 a 5 vigentes, 6 a 8 terminales"
        TIMESTAMP fecha_vencimiento_utc
    }
    EVENTO_DISPOSITIVO {
        NUMBER id_evento PK
        NUMBER id_contenedor FK
        VARCHAR2 id_externo "UK con la caneca"
        NUMBER tipo "1 a 5"
        NUMBER id_orden FK "nula en revisión"
        NUMBER resultado "1 Aplicado, 2 Revision"
    }
    DEPOSITO {
        NUMBER id_deposito PK
        NUMBER id_orden FK, UK
        NUMBER id_evento FK, UK
        NUMBER id_material_declarado FK
        NUMBER id_material_verificado FK "nulo sin verificar"
        NUMBER peso_medido_kg "nulo si no se midió"
    }
```

## Lectura del diagrama
- `USUARIO ||--|{ USUARIO_ROL`: un usuario tiene **uno o más** roles; Oracle solo garantiza la relación, el «al menos uno» lo garantiza la lógica.
- `CONTENEDOR ||--o{ CONTENEDOR_MATERIAL`: una caneca recién creada puede no admitir nada (nace fuera de servicio); para ponerla en servicio necesita al menos un material.
- Ninguna relación borra en cascada: para retirar algo se usan `activo`, `habilitado` o el estado `DadoDeBaja`.
- Los nombres y descripciones obligatorios no admiten espacios al inicio ni al final, ni texto formado solo por espacios; el detalle de cada `CHECK` está en `DICCIONARIO_DATOS.md`.
- V003: las claves foráneas compuestas (orden → interacción con su caneca, evento → orden con su caneca, depósito → evento con su orden) impiden ligar un depósito o un evento a la orden de otra caneca.


## Extensión V005 — Ecopuntos

```mermaid
erDiagram
    MATERIAL ||--o{ REGLA_ECOPUNTOS : tarifa_versionada
    DEPOSITO ||--o{ OPERACION_ECOPUNTOS : origina
    OPERACION_ECOPUNTOS ||--|{ MOVIMIENTO_ECOPUNTOS : confirma_atomicamente
    REGLA_ECOPUNTOS ||--o{ MOVIMIENTO_ECOPUNTOS : respalda_cantidad
    USUARIO ||--o{ MOVIMIENTO_ECOPUNTOS : titular
    MOVIMIENTO_ECOPUNTOS ||--o| MOVIMIENTO_ECOPUNTOS : reverso_o_sucesor
```

Un crédito inicial por depósito; un reverso y un sucesor como máximo por crédito. La corrección confirma reverso y sucesor juntos. El saldo se calcula del libro. Ver `docs/modulos/16_ECOPUNTOS.md`.
