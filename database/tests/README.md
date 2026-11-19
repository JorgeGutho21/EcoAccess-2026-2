# database/tests · pruebas de la base de datos

| Archivo | Prueba | Escribe | Dónde |
|---|---|---|---|
| `A_conexion.sql` | A · conexión | No | Cualquier esquema dueño |
| `A2_verificacion_integrante.sql` | Verificación con una cuenta `ECO_DEV_*` | No | Oracle Cloud, en SQL Developer (F5) |
| `B_estructura.sql` | B · estructura | No | Cualquier esquema dueño |
| `C_compilacion.sql` | C · compilación | No | Cualquier esquema dueño |
| `D_consultas_vistas.sql` | D · vistas | No | Cualquier esquema dueño |
| `E_plsql_sin_escritura.sql` | E · lógica PL/SQL | No (termina con `ROLLBACK`) | Cualquier esquema dueño |
| [`../05_pruebas/V027_pruebas_capa_academica.sql`](../05_pruebas/V027_pruebas_capa_academica.sql) | F · flujo de depósito y ecopuntos | **Sí**, con `ROLLBACK` | Solo el esquema aislado |
| `ejercicios_academicos.sql` | Ejercicios E1–E10 | No | Cualquiera |
| `ejercicios_academicos_escritura.sql` | Ejercicios E11–E14 | **Sí**, con `ROLLBACK` | Solo el esquema aislado (se niega en `ECOACCESS_P3_APP`) |

Se ejecutan todas con:
```powershell
pwsh -File database\scripts\Ejecutar-PruebasBD.ps1                                            # Oracle Cloud: A–E e I
pwsh -File database\scripts\Ejecutar-PruebasBD.ps1 -Secreto ecoaccess-p3-pruebas -IncluirEscritura   # esquema aislado: A–F e I
```

Resultados y evidencias: [`docs/database/13-pruebas.md`](../../docs/database/13-pruebas.md).
