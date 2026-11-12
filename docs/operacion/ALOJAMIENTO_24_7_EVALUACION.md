# Alojamiento 24/7: dependencias, alternativas y estado real (11/11/2026)

**Estado: BLOQUEADO.** EcoAccess sigue dependiendo del PC del propietario. No se desplegó nada en la nube, no se contrató
nada y no se simula disponibilidad. Este documento amplía `ALOJAMIENTO_PERMANENTE.md` con lo medido hoy.

## Dependencias de cada servicio

| # | Dependencia | Estado actual (medido) | Implicación para un servidor |
|---|---|---|---|
| 1 | Runtime del bot | .NET Framework 4.8 (`EcoAccess.P3.Bot.exe`) | solo Windows; en Linux hay que portarlo a .NET 8/10 |
| 2 | Windows / Linux | Windows 11 | Linux exige portar Entidades, Lógica, Datos, Compatibilidad y Bot |
| 3 | Telegram Bot API | long polling, solo conexiones salientes | no hacen falta puertos de entrada |
| 4 | Oracle Cloud | Autonomous Database, ya en la nube | cualquier servidor con el wallet se conecta |
| 5 | Wallet mTLS | carpeta de `TNS_ADMIN` | copiarlo con permisos 600 o una ACL solo del servicio |
| 6 | Ollama + LFM3-VL | modelo de 1,5 GB, Ollama 0.40.16 | sin él, Eco pide la categoría manualmente |
| 7 | GPU / CPU | GTX 1650 de 4 GB en el PC; mediana de 2,5 s por foto | en CPU ARM no está medido; podría ser varias veces más lento |
| 8 | Memoria | bot ≈ 35–55 MB; Ollama con el modelo cargado ≈ 2 GB | una VM de 1 GB no alcanza para la visión; Ampere A1 (hasta 24 GB Always Free) sí |
| 9 | Almacenamiento | binarios ≈ 60 MB + modelo 1,5 GB | menos de 5 GB |
| 10 | HTTPS del registro | Tailscale Funnel desde el PC (`/registro`, puerto 8081) | instalar Tailscale en el servidor o usar un proxy con certificado |
| 11 | Certificados | los gestiona Tailscale Funnel | igual en el servidor |
| 12 | Secretos | User Secrets de Windows (`ecoaccess-p3`) | variables `ECOACCESS_*` en el servicio, con permisos solo del usuario del servicio |
| 13 | Arranque automático | tarea «EcoAccess P3 - vigilante del bot» cada 5 min | `systemd` con `Restart=always` (Linux) o un servicio de Windows |
| 14 | Monitoreo | `vigilante.log` y `--comprobar-*` | `journalctl`, más una alerta por Telegram al propietario |
| 15 | Recuperación | el vigilante relanza el bot (verificado 15:19:23) | `systemd` relanza en segundos |
| 16 | Backups | Oracle Autonomous los hace solo; el código está en GitHub | el wallet y los secretos se guardan aparte |
| 17 | Costo | US$ 0 (PC propio) | Always Free = US$ 0; una VM Windows es de pago |
| 18 | Usuarios concurrentes | 30 simultáneos verificados en integración | lo limita Oracle y la CPU para PBKDF2, no la VM |

## Alternativas

| Opción | Disponible | Memoria/CPU | Latencia visión | Seguridad | Complejidad | Costo | Límite |
|---|---|---|---|---|---|---|---|
| A. PC + vigilante (hoy) | sí | sobra | 2,5 s (GPU) | buena | ninguna | US$ 0 | no es 24/7 |
| B. OCI Always Free Ampere A1 Linux + bot portado a .NET | depende de capacidad | hasta 4 OCPU / 24 GB | no medida en ARM | buena | media-alta (portar 5 proyectos) | US$ 0 | Oracle puede recuperar instancias ociosas |
| B'. Igual que B, con la visión en el PC | sí, si existe B | baja | 2,5 s cuando el PC está encendido | buena (Ollama no se expone) | media | US$ 0 | sin PC no hay visión: Eco pide la categoría |
| C. VM Windows (OCI/Azure) | sí | según plan | según plan | buena | baja (mismo .exe) | de pago | requiere autorización de gasto |
| D. Mono en Linux con el .exe | no recomendado | — | — | ODP.NET, SkiaSharp y TLS sin validar | incierta | US$ 0 | no se usa |

**No se ejecuta un .NET Framework 4.8 en Linux como si fuera multiplataforma** (opción D descartada). Windows Forms se queda
en Windows; solo se portaría el bot (opción B).

## Qué se intentó hoy y qué bloquea

1. **OCI:** no hay CLI de OCI ni archivo `~/.oci/config` en el equipo. Existe la clave SSH `~/.ssh/ecoaccess_oci` (ed25519,
   creada el 09/11), pero no hay registro de ninguna instancia (sin `known_hosts` ni `config`).
2. Se abrió la consola de Oracle Cloud en el navegador integrado: pide **Cloud Account Name** e inicio de sesión. Las
   credenciales del propietario no se escriben por él; **bloqueo: inicio de sesión del propietario**.
3. Aunque se inicie sesión, crear la VM A1 depende de la capacidad de la región. Ya falló antes por falta de capacidad
   («Out of host capacity»), según `PANEL_INTEGRACIONES_DISPONIBILIDAD.md` (región Bogotá, forma A1).
4. Portar el bot a .NET 8/10 no se hizo en esta sesión. Es un cambio de varios proyectos que necesita su propia rama y
   sus propias pruebas, y sin la VM no tendría dónde desplegarse.

## Intervención mínima del propietario

1. Iniciar sesión en Oracle Cloud (pestaña ya abierta), ir a *Compute > Instances > Create instance*, elegir la forma
   **VM.Standard.A1.Flex** (Always Free) con Ubuntu y pegar `~/.ssh/ecoaccess_oci.pub`.
2. Si la región responde «Out of host capacity», reintentar en otro dominio de disponibilidad u otro horario. **No** pase la
   cuenta a Pay As You Go sin decidirlo antes.
3. Con la IP de la VM, una sesión nueva puede portar el bot, crear el servicio `systemd`, copiar el wallet y los secretos,
   detener el bot del PC (un solo consumidor de Telegram) y probar el servicio de extremo a extremo con el PC apagado.
