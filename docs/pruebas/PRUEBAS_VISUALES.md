# Pruebas visuales

Herramienta: `tests/visual/ArnesVisual`. Abre los módulos reales contra una **clon temporal** de la base, en el puerto 1523, con un administrador y un ciudadano tomados de esa clon. La base real no se usa.

Condiciones: 1366×768, 96 ppp, con la ventana fuera del área visible. **No se probaron 125 %, 150 % ni 1920×1080.**

| Módulo | Abre | Interacción probada | Excepciones de interfaz |
|---|---|---|---|
| Marco principal, administrador | sí | los 13 botones de navegación probados llevan a su módulo | 0 |
| Dashboard institucional | sí | alcance, material, categoría, caneca, Limpiar | 0 |
| Estadísticas | sí | consulta inicial; canal manual corregido | 0 |
| Estadísticas personales | sí | carga inicial | 0 |
| Depósitos, ecopuntos de administrador, diagnóstico, materiales, puntos, canecas, usuarios | sí | carga inicial | 0 en la corrida base |
| Opiniones | sí | cierre sin la excepción anterior | 0 |
| Registro manual | sí | Enter en 9 búsquedas, flecha abajo, habilita Solicitar apertura | 0 |
| Próximamente | sí | carga; tres pestañas creadas | 0 |
| Mis ecopuntos | sí | carga y gráfico en la clon; sin Oracle, con el código final, muestra el fallo y botones completos | 0 |
| Explorar, invitado | sí | Enter, vacío, catálogo, Siguiente, Anterior | 0 |
| Explorar, ciudadano | sí | además, Actualizar mi estado | sin repetir tras corregir el arnés |

**Lección registrada:** dos fallos aparentes —«Error creating window handle» en Explorar y un tiempo agotado en el dashboard— venían del arnés. `Application.DoEvents` retiraba el contexto de Windows Forms y las continuaciones de `await` salían del hilo de la interfaz. La aplicación usa `Application.Run`. Tras corregir el arnés desaparecieron en Explorar, dashboard y registro manual, los tres módulos repetidos.

**No probado:** clic físico, ratón real, teclado con Tab en todos los módulos, inicio y cierre de sesión de punta a punta, lector de pantalla, medición de parpadeo.

Capturas en `docs/pruebas/capturas/`, elegidas porque no muestran datos personales. Son de la clon: los valores son de ensayo. La de Explorar es anterior al ajuste del botón Buscar. `mis_ecopuntos_sin_oracle.png` se tomó sin ninguna base, a propósito: muestra el estado de fallo con el código final, no un saldo.
