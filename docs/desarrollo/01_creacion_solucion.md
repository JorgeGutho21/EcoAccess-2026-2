# 01. Creación de la solución

## Punto de partida

EcoAccess P3 se reconstruyó en el repositorio `JorgeGutho21/EcoAccess-P3` a partir del proyecto anterior `JorgeGutho21/EcoAccess_P3`, escrito para .NET 10. El código **no se escribió desde cero**: se recuperó, se revisó y se adaptó. Como base se tomó el commit `e15fcf6`, de la rama de estabilización general, la más avanzada, que contenía a las demás ramas de trabajo. Su repositorio no se modificó.

## Auditoría inicial

| Aspecto | Hallazgo |
|---|---|
| Código anterior | 332 archivos C#, unas 24.000 líneas de producción y 16.000 de pruebas, en net10 |
| Entorno | Visual Studio 2026, Developer Pack 4.8, .NET SDK 10, Oracle 26ai Free en Docker, Ollama |
| Paquetes | todos con versión compatible con net48 (Krypton, SkiaSharp, LiveCharts, FontAwesome, MSTest, QRCoder) |
| Excluido | configuraciones de herramientas, transcripciones, evidencias antiguas y cachés |

## Decisiones

| Decisión | Motivo |
|---|---|
| **C# 9** con `LangVersion` fijo | compila desde Visual Studio 2019 16.8 y conserva `record` y `switch` de expresión; C# 7.3 habría exigido reescribir el dominio sin ganancia funcional |
| Proyecto `EcoAccess.P3.Compatibilidad` | reúne en un solo lugar las utilidades de .NET moderno ausentes en Framework (cadenas, LINQ, tareas, criptografía), sin condicionales en el resto del código |
| Formato de proyecto SDK | restauración NuGet simple y configuración común en `Directory.Build.props` |
| Retirar Gemini, OpenAI y ONNX | el alcance es reconocimiento local sin servicios de pago; ONNX nunca superó la fase experimental. Se eliminaron también sus pruebas |
| Perfil privado propio | User Secrets `ecoaccess-p3-framework`, esquema `ECOACCESS_P3_APP` y carpeta `ProgramData\EcoAccessP3Framework`, para no interferir con otras instalaciones |

## Migración

1. Se aplicó una herramienta de conversión de un solo uso, basada en Roslyn y externa al repositorio, que trasladó la sintaxis posterior a C# 9: cadenas literales sin formato, expresiones de colección, constructores primarios, `using` implícitos y `namespace` de archivo.
2. Las API inexistentes en Framework se sustituyeron por la clase `Compat` y por extensiones.
3. Se compiló capa por capa, en orden de dependencia, y se corrigió lo que requería criterio: transacciones Oracle, `HttpClientHandler`, `SerialPort`, DPI por manifiesto, texto de ayuda de las cajas de búsqueda.
4. Se ejecutaron las pruebas heredadas para confirmar que el comportamiento se conservaba.

## Organización

`EcoAccess.P3.sln` agrupa `src` y `tests`, y muestra `hardware` como carpeta de solución. La guía de carpetas del proyecto se adaptó: los formularios permanecen en la raíz de `EcoAccess.P3.Presentacion` para no alterar los espacios de nombres heredados del diseñador.

## Git

Trabajo por ramas `desarrollo/*` con pull request hacia `main`. Identidad local del repositorio: la cuenta de Jorge.