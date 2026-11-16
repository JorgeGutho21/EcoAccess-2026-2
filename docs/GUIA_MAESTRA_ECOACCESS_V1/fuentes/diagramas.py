"""Genera los cinco diagramas del manual con Pillow en Windows."""
from pathlib import Path
import math
import os

from PIL import Image, ImageDraw, ImageFont

DESTINO = Path(__file__).resolve().parents[1] / "diagramas"
FONTS = Path(os.environ["WINDIR"]) / "Fonts"
TINTA = "#152B26"
VERDE = "#22584B"
CLARO = "#EBF3EF"
GRIS = "#536861"


def fuente(tam, negrita=False):
    return ImageFont.truetype(str(FONTS / ("segoeuib.ttf" if negrita else "segoeui.ttf")), tam)


def base(alto, titulo):
    imagen = Image.new("RGB", (1480, alto), "#F7F9F8")
    dibujo = ImageDraw.Draw(imagen)
    dibujo.text((40, 28), titulo, font=fuente(45, True), fill=TINTA)
    return imagen, dibujo


def caja(dibujo, area, titulo, detalle="", fondo="#FFFFFF"):
    x1, y1, x2, y2 = area
    dibujo.rounded_rectangle(area, radius=15, fill=fondo, outline="#A9C2B7", width=3)
    estilo = fuente(34, True)
    if dibujo.textlength(titulo, font=estilo) > x2 - x1 - 26:
        estilo = fuente(28, True)
    x = (x1 + x2) / 2
    if detalle:
        dibujo.text((x, (y1 + y2) / 2 - 20), titulo, font=estilo, fill=TINTA, anchor="mm")
        dibujo.text((x, (y1 + y2) / 2 + 23), detalle, font=fuente(27), fill=GRIS, anchor="mm")
    else:
        dibujo.text((x, (y1 + y2) / 2), titulo, font=estilo, fill=TINTA, anchor="mm")


def flecha(dibujo, inicio, fin, color=VERDE):
    dibujo.line((inicio, fin), fill=color, width=5)
    angulo = math.atan2(fin[1] - inicio[1], fin[0] - inicio[0])
    puntos = [fin]
    for delta in (-0.45, 0.45):
        puntos.append((fin[0] - 21 * math.cos(angulo + delta), fin[1] - 21 * math.sin(angulo + delta)))
    dibujo.polygon(puntos, fill=color)


def guardar(imagen, nombre):
    destino = DESTINO / nombre
    destino.parent.mkdir(parents=True, exist_ok=True)
    imagen.save(destino, optimize=True)


def recorrido(titulo, pasos, nombre):
    imagen, d = base(127 + 112 * len(pasos), titulo)
    for i, (accion, detalle) in enumerate(pasos):
        y = 105 + i * 112
        d.ellipse((105, y + 12, 165, y + 72), fill=VERDE)
        d.text((135, y + 42), str(i + 1), font=fuente(31, True), fill="white", anchor="mm")
        d.text((210, y + 5), accion, font=fuente(37, True), fill=TINTA)
        d.text((210, y + 51), detalle, font=fuente(30), fill=GRIS)
        if i < len(pasos) - 1:
            flecha(d, (135, y + 74), (135, y + 120))
    guardar(imagen, nombre)


def capas():
    imagen, d = base(620, "Capas y conexiones de EcoAccess")
    caja(d, (50, 122, 690, 225), "Windows Forms", "Interfaz de escritorio")
    caja(d, (790, 122, 1430, 225), "EcoAccess.P3.Bot", "Telegram · visión · dispositivo")
    caja(d, (50, 315, 690, 415), "Lógica", "Casos de uso y contratos", CLARO)
    caja(d, (790, 315, 1430, 415), "Datos", "Implementa repositorios Oracle", CLARO)
    caja(d, (50, 495, 690, 587), "Entidades", "Datos e invariantes")
    caja(d, (790, 495, 1430, 587), "Oracle", "Persistencia y consultas")
    for inicio, fin in [((360,225),(360,315)), ((1110,225),(1110,315)), ((850,225),(630,315)),
                        ((600,225),(820,315)), ((790,365),(690,365)), ((360,415),(360,495)),
                        ((1110,415),(1110,495))]:
        flecha(d, inicio, fin)
    guardar(imagen, "arquitectura/capas.png")


def relaciones():
    imagen, d = base(670, "Relaciones principales de Oracle")
    grupos = [
        ("USUARIO", "1 → N", "USUARIO_ROL", "N ← 1", "ROL"),
        ("PUNTO_INSTALACION", "1 → N", "CONTENEDOR", "1 → N", "INTERACCION"),
        ("MATERIAL", "1 → N", "CONTENEDOR_MATERIAL", "N ← 1", "CONTENEDOR"),
        ("INTERACCION", "1 → 0..1", "ORDEN_APERTURA", "1 → 0..1", "DEPOSITO"),
    ]
    for i, (origen, arista1, centro, arista2, destino) in enumerate(grupos):
        y = 112 + i * 112
        caja(d, (44, y, 408, y + 86), origen)
        caja(d, (543, y, 940, y + 86), centro, fondo=CLARO)
        caja(d, (1070, y, 1436, y + 86), destino)
        for x, arista in ((475, arista1), (1006, arista2)):
            d.text((x, y + 39), arista, font=fuente(27, True), fill=VERDE, anchor="mm")
    d.text((47, 577), "Una orden puede tener varios eventos. Un depósito habilita la auditoría de sus puntos.", font=fuente(29), fill=GRIS)
    guardar(imagen, "base_datos/relaciones.png")


def conexiones():
    imagen, d = base(680, "Estación física con un ESP32")
    caja(d, (45, 132, 445, 227), "PC Windows", "Bridge y adaptadores")
    caja(d, (540, 132, 1430, 227), "ESP32", "USB de datos · COM · 115200 baudios", CLARO)
    flecha(d, (446, 180), (540, 180))
    servos = [(550, "Servo blanca", "GPIO 19"),
              (852, "Servo verde", "GPIO 23"),
              (1154, "Servo negra", "GPIO 18")]
    for x, titulo, pin in servos:
        medio = x + 135
        flecha(d, (medio, 228), (medio, 312))
        caja(d, (x, 313, x + 270, 415), titulo, pin)
    caja(d, (45, 360, 445, 461), "Fuente externa", "Tensión según el servo")
    d.line(((445, 442), (1289, 442)), fill="#9B5C28", width=6)
    for x, _, _ in servos:
        flecha(d, (x + 135, 443), (x + 135, 416), "#9B5C28")
    d.text((45, 496), "Alimentación externa a los tres servos. GPIO únicamente para señal.", font=fuente(32, True), fill=TINTA)
    d.rounded_rectangle((45, 562, 1430, 636), 13, fill=CLARO, outline="#A9C2B7", width=3)
    d.text((737, 598), "Tierra común entre GND del ESP32, la fuente y los tres servos.", font=fuente(30), fill=TINTA, anchor="mm")
    guardar(imagen, "hardware/conexiones.png")


if __name__ == "__main__":
    capas()
    relaciones()
    conexiones()
    recorrido("De una fotografía PET al depósito", [
        ("Telegram recibe la foto", "QR vigente y sesión con el contexto de la estación"),
        ("Ollama ejecuta LFM3-VL", "Forma y material se corroboran en el flujo C#"),
        ("Lógica y Oracle autorizan", "Admisión, disponibilidad e idempotencia para blanca 1093"),
        ("El bridge entrega el permiso", "Desafío y HMAC por el puerto serie compartido"),
        ("El ESP32 actúa en GPIO 19", "PWM de apertura y cierre al terminar la ventana"),
        ("Oracle conserva el resultado", "Cierre reportado y depósito declarado sin sensor"),
    ], "telegram/pet.png")
    recorrido("Visión y política de apertura", [
        ("Inferencia local", "Ollama + LFM3-VL describen el objeto de la imagen"),
        ("Interpretación", "C# reduce la salida al catálogo de objetos y materiales"),
        ("Corroboración", "Se comprueba objeto, condición, material y contexto"),
        ("Decisión", "Abstención, orientación o validación contra la caneca"),
    ], "inteligencia_artificial/vision.png")
