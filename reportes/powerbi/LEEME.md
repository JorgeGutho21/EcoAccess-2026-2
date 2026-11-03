# Informe en Power BI

**Estado: el informe no está construido.** Aquí están los recursos para hacerlo en Power BI Desktop. No hay `.pbix` ni `.pbip`, porque Power BI Desktop no está instalado en el equipo donde se prepararon. El dashboard de EcoAccess no depende de nada de esto.

| Recurso | Qué es | Comprobado |
|---|---|---|
| `database/06_migraciones/V019__vistas_power_bi.sql` | vistas de materiales y de ciudadanos únicos por mes | aplicada en una clon temporal |
| `database/00_administracion/cuenta_informes_powerbi.sql` | cuenta de solo lectura | en la clon: lee las vistas, no lee tablas |
| `Validacion.sql` | totales de referencia | ejecutada en la clon |
| `ImportarOracle.pq`, `Consultas.pq` | consultas de Power Query | **sin ejecutar** |
| `Medidas.dax` | medidas | **sin ejecutar** |

## 1. Preparar Oracle

`Iniciar-Bot-Eco.ps1` o `Preparar-BaseDatos.ps1` aplican `V019`. Luego, como administrador de Oracle en el PDB del proyecto, ejecute `cuenta_informes_powerbi.sql`. Pide la contraseña y no la guarda. Power BI nunca debe usar la cuenta propietaria.

## 2. Instalar Power BI Desktop

Requiere su autorización. Es gratuito y no necesita Power BI Service ni publicación:

```powershell
winget install --id Microsoft.PowerBI
```

También necesita el cliente Oracle que indica el conector Oracle de Power BI.

## 3. Consultas

1. Cree los parámetros `Servidor`, `Esquema` y `SoloDemostracion` (ver `Consultas.pq`).
2. Cree una consulta en blanco llamada `ImportarOracle` y pegue `ImportarOracle.pq`.
3. Cree una consulta por cada bloque de `Consultas.pq`, con el nombre del bloque.
4. Al conectar, use `ECOACCESS_P3_INFORMES` en el diálogo del conector. No marque ninguna opción que guarde la contraseña en el archivo.

`SoloDemostracion` se aplica a todos los hechos a la vez: nunca mezcle datos operativos con simulaciones, y muestre el alcance en el título. Hoy los datos de la base están marcados como ensayo: con `false` el informe saldrá vacío, y es correcto.

## 4. Modelo

- Cree `Calendario` con la expresión final de `Medidas.dax` y márquela como tabla de fechas.
- Relaciones 1:* con dirección única:
  - `Calendario[Date]` → `DIA` de Depositos, Puntos, Solicitudes y Opiniones.
  - `Calendario[Date]` → `MES` de Participacion.
  - `DimCaneca[ID_CONTENEDOR]` → Depositos, Puntos y Solicitudes.
  - `DimUbicacion[ID_PUNTO_INSTALACION]` → Depositos, Puntos y Solicitudes.
  - `DimMaterial[MATERIAL]` → `Depositos[MATERIAL]`.
- No relacione hechos entre sí. Opiniones no se relaciona con caneca o ubicación: son globales. Puntos y Solicitudes no se reparten por material. `ExpansionUPC` queda sin relaciones.
- Oculte las columnas de identificador. Los hechos no contienen personas, fotos ni contraseñas.
- Incorpore las medidas de `Medidas.dax`. Ajuste los separadores si su configuración regional lo pide.

## 5. Páginas

| # | Página | Contenido |
|---|---|---|
| 1 | Resumen ejecutivo | tarjetas de solicitudes, depósitos, verificados y puntos netos; líneas por `Calendario[Date]`; segmentadores de fecha, caneca y ubicación |
| 2 | Residuos y materiales | barras por `DimMaterial[NOMBRE]`, anillo por `DimMaterial[CATEGORIA]`, tendencia y `Variación de depósitos`. Nada de «reciclado» ni kilogramos: son registros |
| 3 | Participación ciudadana | `Ciudadanos únicos del mes` por `Calendario[Mes]` y `Depósitos por ciudadano del mes`. Sin identidades |
| 4 | Ecopuntos | `Puntos acreditados`, `Puntos reversados`, `Puntos netos`, tendencia y `Variación de puntos netos` |
| 5 | Contenedores | actividad por `DimCaneca[CANECA]` y `DimUbicacion[UBICACION]`. «Habilitada» no demuestra conexión |
| 6 | Experiencia y opiniones | `Opiniones recibidas` y `Promedio de valoración` por fecha, con la nota «opiniones globales» |
| 7 | Expansión UPC | tabla de `ExpansionUPC` con el título **PROYECCIÓN FUTURA**. Sin medidas ni segmentadores de hechos |

Guarde el archivo como `reportes\powerbi\EcoAccess.pbix`: el botón «Abrir análisis en Power BI» de EcoAccess lo abre desde ahí. Si quiere control de versiones, guárdelo además como `.pbip`. No guarde credenciales.

## 6. Validar

1. Ejecute `Validacion.sql` y compare cada tarjeta con su fila, en los dos alcances.
2. `Control verificados` y `Control netos` deben dar 0.
3. Cambie fecha, caneca y ubicación, y compruebe que Opiniones no cambia con caneca ni ubicación.
4. Guarde capturas de las siete páginas antes de declararlo validado.

## Compañeros y seguridad

Use la dirección privada del servidor a través de VPN, no una IP pública. No exponga el puerto 1522 a Internet ni use «Publicar en la web». Power BI Embedded no se implementó: exige licencias, registro de aplicación y capacidad que no se han comprobado.

Los reversos llevan signo negativo. Las unidades verificadas excluyen conteos históricos nulos, que se cuentan aparte, y no son kilos. Solicitudes, depósitos y movimientos tienen fechas de hechos distintos: sus totales diarios no tienen por qué coincidir.
