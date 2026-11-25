"""Prepara las imágenes optimizadas de EcoAccess Challenge a partir de los originales del proyecto (que no se modifican).

Uso: python scripts/web/preparar_imagenes_juego.py "<carpeta 01_EcoAccess en OneDrive>" web/juego/img
- Eco: las tres figuras originales de cuerpo entero con chaqueta de cada caneca (PNG transparente) → WebP.
- Preguntas: fotografías reales del proyecto (WhatsApp de ExpoSoftware) e ilustraciones del catálogo, recortadas al objeto.
"""
import sys
from pathlib import Path
from PIL import Image, ImageOps

raiz, destino = Path(sys.argv[1]), Path(sys.argv[2])
p3 = raiz / "01_ProyectoDeAula_EcoAccess" / "EcoAccess-P3"
expo = raiz / "02_Imagenes_Reales_EcoAcess" / "IMAGENES QUE SE VAN A UTILIZAR EN EXPO"
reales = raiz / "02_Imagenes_Reales_EcoAcess"
eco = raiz / "05_Imagenes_EcoAccess"


def abrir(ruta):
    return ImageOps.exif_transpose(Image.open(ruta))


def cuadrado(im, cx=0.5, cy=0.5, lado=0.8, salida=640):
    w, h = im.size
    l = int(min(w, h) * lado)
    x = int(min(max(cx * w - l / 2, 0), w - l)); y = int(min(max(cy * h - l / 2, 0), h - l))
    return im.crop((x, y, x + l, y + l)).resize((salida, salida), Image.LANCZOS)


def guardar(im, nombre, calidad=82):
    ruta = destino / nombre
    ruta.parent.mkdir(parents=True, exist_ok=True)
    im.save(ruta, "WEBP", quality=calidad, method=6)
    print(f"{nombre}: {im.size[0]}x{im.size[1]}, {ruta.stat().st_size // 1024} KB")


def buscar(carpeta, nombre):
    r = list(carpeta.rglob(nombre))
    if not r:
        raise SystemExit(f"No se encontró {nombre} en {carpeta}")
    return r[0]


# Eco de cuerpo entero (transparente): recorte al contenido y alto de 720 px.
for color in ("BLANCO", "NEGRO", "VERDE"):
    im = abrir(buscar(eco, f"ECO {color} CANECA.png")).convert("RGBA")
    im = im.crop(im.getbbox())
    im = im.resize((round(im.width * 720 / im.height), 720), Image.LANCZOS)
    guardar(im, f"eco/eco-{color.lower()}.webp", 86)
im = abrir(p3 / "assets/imagenes/eco/eco_celebra.png").convert("RGBA")
guardar(im.resize((360, 360), Image.LANCZOS), "eco/eco-celebra.webp", 86)

# Pregunta 1: cáscara de banano del catálogo EcoAccess.
guardar(cuadrado(abrir(p3 / "assets/imagenes/residuos/verde/banano.jpg").convert("RGB"), lado=1.0), "preguntas/p1-banano.webp")
# Pregunta 2: envase rosado de sérum de vidrio con tapa blanca (fotografía real).
guardar(cuadrado(abrir(expo / "WhatsApp Image 2026-11-25 at 9.33.11 AM (6).jpeg").convert("RGB"), 0.5, 0.55, 0.46), "preguntas/p2-serum-vidrio.webp")
# Pregunta 3: cuatro residuos reales como opciones.
guardar(cuadrado(abrir(reales / "WhatsApp Image 2026-11-23 at 5.09.16 PM (1).jpeg").convert("RGB"), 0.5, 0.56, 0.7, 480), "preguntas/p3-servilleta.webp")
guardar(cuadrado(abrir(expo / "WhatsApp Image 2026-11-25 at 9.33.12 AM (5).jpeg").convert("RGB"), 0.5, 0.56, 0.62, 480), "preguntas/p3-cuaderno.webp")
aluminio = cuadrado(abrir(expo / "WhatsApp Image 2026-11-25 at 9.33.12 AM (6).jpeg").convert("RGB"), 0.5, 0.56, 0.6, 480)
guardar(aluminio, "preguntas/p3-aluminio.webp")
# Pregunta 4: Eco y el prototipo original de las tres canecas.
proto = abrir(p3 / "assets/imagenes/bot/eco_canecas.jpg").convert("RGB")
y0 = proto.height - proto.width - int(proto.height * 0.03)
guardar(proto.crop((0, y0, proto.width, y0 + proto.width)).resize((640, 640), Image.LANCZOS), "preguntas/p4-eco-prototipo.webp")
# Pregunta 5: limón y papel aluminio limpio (fotografías reales), lado a lado.
limon = cuadrado(abrir(expo / "WhatsApp Image 2026-11-25 at 9.33.10 AM (2).jpeg").convert("RGB"), 0.5, 0.56, 0.62, 480)
par = Image.new("RGB", (960, 480), "white"); par.paste(limon, (0, 0)); par.paste(aluminio, (480, 0))
guardar(par, "preguntas/p5-limon-aluminio.webp")
