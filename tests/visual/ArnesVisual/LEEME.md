# Arnés visual

Abre cada módulo real de Windows Forms, ejecuta interacciones (Enter en las búsquedas, Anterior y Siguiente, filtros, Limpiar, navegación del menú) y registra excepciones, diálogos, tiempos de desplazamiento y capturas.

**No forma parte de la solución ni de `dotnet test`.** Acepta una clon temporal en loopback por el puerto 1523, o un esquema ECOACCESS_P3_PRUEBAS en Oracle FREE local por el puerto 1521. Usa datos ficticios.

## Uso

1. Prepara un esquema de pruebas local con datos ficticios o una clon temporal publicada en el puerto 1523. La prueba requiere un ciudadano, un operario y un supervisor activos en ese esquema.
2. Copia `src\EcoAccess.P3.Presentacion\bin\Release\net48` a una carpeta `lib` junto al proyecto.
3. Compila con `dotnet build arnes.csproj -c Release` y copia `lib` a `sal`, con `EcoAccess.P3.Presentacion.exe.config` como `arnes.exe.config`.
4. Define `ECOARNES_CADENA` y ejecuta `sal\arnes.exe <carpeta-salida> [modulos,separados,por,comas]`. Ejecuta un módulo por proceso si quieres aislar fallos.

Las capturas pueden mostrar nombres y correos de la base clonada: revísalas antes de publicarlas.

## Límites

- Muestra lo que pinta Windows a la escala del equipo: **no prueba 125 % ni 150 %**.
- Los tiempos de desplazamiento miden el repintado con la ventana fuera de la pantalla: sirven para comparar antes y después, no sustituyen la percepción real.
- Usa `Application.DoEvents` y restaura el contexto de Windows Forms en cada paso. Sin esa restauración, las continuaciones de `await` salen del hilo de la interfaz y aparecen errores falsos de creación de ventanas.
