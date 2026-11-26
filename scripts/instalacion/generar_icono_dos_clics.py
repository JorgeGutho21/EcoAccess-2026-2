"""Genera assets/identidad/EcoAccess_DOS_CLICS.ico: icono del acceso «ECOACCESS DOS CLICS MANUAL».

Usa las capas originales de las tres canecas de ECOACCESS (web/ciudadano/assets/canecas: cuerpo + tapa, que comparten el
mismo lienzo) sobre el círculo verde institucional, con la verde al frente y un distintivo blanco con un puntero (los dos
clics: elegir la caneca y abrirla). Varias resoluciones (16-256 px).
Uso (desde la raíz del repositorio): python scripts/instalacion/generar_icono_dos_clics.py
"""
import os
from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
CANECAS = os.path.join(RAIZ, "web", "ciudadano", "assets", "canecas")
DESTINO = os.path.join(RAIZ, "assets", "identidad", "EcoAccess_DOS_CLICS.ico")
VERDE_OSC, VERDE, BLANCO = (16, 92, 59), (22, 138, 85), (255, 255, 255)


def caneca(color):
    cuerpo = Image.open(os.path.join(CANECAS, f"{color}-cuerpo.webp")).convert("RGBA")
    tapa = Image.open(os.path.join(CANECAS, f"{color}-tapa.webp")).convert("RGBA")
    im = Image.alpha_composite(cuerpo, tapa)
    return im.crop(im.getbbox())


def con_sombra(lienzo, im, x, y):
    sombra = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sombra.putalpha(im.getchannel("A").point(lambda a: int(a * 0.45)))
    sombra = sombra.filter(ImageFilter.GaussianBlur(max(2, im.width // 25)))
    lienzo.alpha_composite(sombra, (x + im.width // 30, y + im.width // 22))
    lienzo.alpha_composite(im, (x, y))


def dibujar(lado=1024):
    im = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    m = lado // 32
    ImageDraw.Draw(im).ellipse([m, m, lado - m, lado - m], fill=VERDE_OSC)
    # Brillo suave en la parte superior del círculo.
    brillo = Image.new("L", (lado, lado), 0)
    ImageDraw.Draw(brillo).ellipse([m * 3, m * 2, lado - m * 3, lado // 2], fill=38)
    im.paste(Image.new("RGBA", (lado, lado), BLANCO + (255,)), (0, 0), brillo.filter(ImageFilter.GaussianBlur(lado // 20)))

    def escalar(c, alto):
        return c.resize((max(1, int(c.width * alto / c.height)), alto), Image.LANCZOS)

    alto_lado, alto_centro = int(lado * 0.40), int(lado * 0.50)
    blanca, negra, verde = escalar(caneca("blanca"), alto_lado), escalar(caneca("negra"), alto_lado), escalar(caneca("verde"), alto_centro)
    base = int(lado * 0.77)
    con_sombra(im, blanca, int(lado * 0.17), base - blanca.height)
    con_sombra(im, negra, int(lado * 0.83) - negra.width, base - negra.height)
    con_sombra(im, verde, (lado - verde.width) // 2, base + lado // 40 - verde.height)

    # Distintivo: círculo blanco con un puntero verde (seleccionar y abrir).
    r = int(lado * 0.15)
    cx, cy = lado - m - r - lado // 60, lado - m - r - lado // 60
    d = ImageDraw.Draw(im)
    d.ellipse([cx - r - lado // 70, cy - r - lado // 70, cx + r + lado // 70, cy + r + lado // 70], fill=VERDE_OSC)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=BLANCO)
    s = r / 100
    flecha = [(-38, -52), (-38, 48), (-12, 24), (6, 62), (24, 54), (6, 16), (40, 16)]
    d.polygon([(cx + x * s, cy + y * s) for x, y in flecha], fill=VERDE)
    return im


if __name__ == "__main__":
    base = dibujar()
    base.save(DESTINO, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
    base.resize((256, 256), Image.LANCZOS).save(os.path.join(os.environ.get("TEMP", "."), "EcoAccess_DOS_CLICS_vista.png"))
    print("Generado", os.path.relpath(DESTINO, RAIZ))
