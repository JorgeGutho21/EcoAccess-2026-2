"""Separa cada caneca original (PNG/WebP transparente) en tres capas alineadas: tapa, cuerpo y boca interior.

La unión tapa/cuerpo es un arco: se detecta columna a columna (línea oscura de la junta) y se suaviza con un polinomio.
Uso: python scripts/web/separar_capas_canecas.py estacion-web/dist/assets/canecas web/ciudadano/assets/canecas
La boca muestra el labio del cuerpo, la pared interna trasera y el fondo oscuro; la cara interior de la tapa lleva
luz desde la bisagra, labio perimetral y el hueco real de la tapa (región oscura conectada al centro).
"""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ANCHO_WEB = 540  # px; 2x para mostrarse a ~270 px


def separar(origen: Path, destino: Path, color: str) -> dict:
    im = Image.open(origen / f"{color}.webp").convert("RGBA")
    bbox = im.getbbox()
    im = im.crop(bbox)
    a = np.asarray(im).astype(float)
    h, w, _ = a.shape
    lum = a[:, :, :3].mean(axis=2)
    alfa = a[:, :, 3]
    cols = [x for x in range(w) if alfa[:, x].max() > 200]
    x0, x1 = cols[0], cols[-1]
    # Banda de búsqueda de la junta: entre el 22 % y el 36 % de la altura.
    y_a, y_b = int(h * 0.22), int(h * 0.36)
    xs, ys = [], []
    for x in range(x0 + int((x1 - x0) * 0.06), x1 - int((x1 - x0) * 0.06), 3):
        col = lum[y_a:y_b, max(0, x - 2):x + 3].mean(axis=1)
        col = np.convolve(col, np.ones(3) / 3, mode="same")
        xs.append(x); ys.append(y_a + int(np.argmin(col)))
    xs, ys = np.array(xs), np.array(ys)
    p = np.polyfit(xs, ys, 4)
    for _ in range(3):  # descarta valores atípicos (reflejos) y vuelve a ajustar
        r = np.abs(np.polyval(p, xs) - ys); m = r < max(4, np.percentile(r, 85))
        p = np.polyfit(xs[m], ys[m], 4)
    curva = np.clip(np.polyval(p, np.arange(w)), 0, h - 1)
    yy = np.arange(h)[:, None]
    solape = 3  # px de solape para que no aparezca una rendija entre capas
    mascara_tapa = yy <= (curva[None, :] + solape)
    tapa = a.copy(); tapa[:, :, 3] *= mascara_tapa
    cuerpo = a.copy(); cuerpo[:, :, 3] *= ~(yy < curva[None, :] - 1)
    alto_tapa = int(curva[x0:x1].max()) + solape + 1
    ancho = x1 - x0
    centro = (x0 + x1) / 2
    filas_tapa = [y for y in range(h) if alfa[y, x0:x1].max() > 200]
    tope = filas_tapa[0]
    junta_lados = curva[x0 + int(ancho * 0.08)]
    # Bisagra: borde trasero de la tapa, casi en su tope (la web la gira como un plano inclinado 54° y estirado,
    # de modo que la tapa abierta se levanta desde aquí y la boca va desde este borde hasta la junta frontal).
    bisagra = tope + (junta_lados - tope) * 0.12
    # Color del cuerpo justo bajo la junta (para el borde de la boca y la cara interior de la tapa).
    muestra = a[int(curva[int(centro)]) + 8:int(curva[int(centro)]) + 30, int(centro - ancho * .3):int(centro + ancho * .3), :3].reshape(-1, 3)
    tono = np.median(muestra, axis=0)
    # Boca: abertura completa del cuerpo, desde el borde trasero (bisagra) hasta la junta frontal, con esquinas redondeadas.
    boca = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(boca)
    m = int(ancho * 0.035)
    xi, xd = x0 + m, x1 - m
    r = ancho * 0.09
    contorno = []
    for i in range(41):  # esquina trasera izquierda → borde trasero → esquina trasera derecha
        t = i / 40
        x = xi + t * (xd - xi)
        dx = min(x - xi, xd - x)
        y = bisagra + (r - np.sqrt(max(0.0, r * r - (r - dx) ** 2)) if dx < r else 0)
        contorno.append((x, y))
    contorno += [(x, curva[int(x)] + 1) for x in np.linspace(xd, xi, 60)]
    # Borde del cuerpo (labio): el color real del cuerpo, un poco más claro en el borde trasero, que recibe la luz.
    gy = np.linspace(0, 1, h)[:, None]
    y_a, y_b = bisagra / h, junta_lados / h
    tt = np.clip((gy - y_a) / max(1e-3, (y_b - y_a)), 0, 1)[..., None]
    def lienzo(rgb):
        rgb = np.broadcast_to(rgb, (h, w, 3)) if rgb.ndim == 3 else np.broadcast_to(rgb[None, None, :], (h, w, 3))
        return Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), np.full((h, w, 1), 255)], axis=2).astype("uint8"), "RGBA")
    claro = np.clip(tono * 1.08 + 10, 0, 255)
    mascara_borde = Image.new("L", (w, h), 0); ImageDraw.Draw(mascara_borde).polygon(contorno, fill=255)
    boca.paste(lienzo(claro * (1 - tt * 0.5) + tono * (tt * 0.5)), (0, 0), mascara_borde)
    interior = []
    k = ancho * 0.03
    for x, y in contorno:
        cx_ = min(max(x, xi + k), xd - k)
        interior.append((cx_, y + k * (0.55 if y <= bisagra + r else -0.8)))
    mascara_int = Image.new("L", (w, h), 0); ImageDraw.Draw(mascara_int).polygon(interior, fill=255)
    # Interior visto desde arriba y de frente: arriba, la pared interna trasera (tono del cuerpo en sombra);
    # abajo, el fondo oscuro. Así la boca se lee como un hueco con profundidad y no como una placa negra.
    pared = np.clip(np.maximum(tono * 0.68 + 10, 58), 0, 255)
    fondo = np.array([7.0, 9.0, 8.0])
    u = np.clip(tt / 0.75, 0, 1)
    rgb = pared * (1 - u) ** 1.15 + fondo * (1 - (1 - u) ** 1.15)
    boca.paste(lienzo(rgb), (0, 0), mascara_int.filter(ImageFilter.GaussianBlur(0.6)))
    # Sombra de contacto bajo el labio frontal (el borde delantero tapa parte del interior).
    sombra = Image.new("L", (w, h), 0)
    ImageDraw.Draw(sombra).line([(x, curva[int(x)] - k * 0.4) for x in np.linspace(xi + k, xd - k, 50)], fill=170, width=max(2, int(k * 0.9)))
    sombra = sombra.filter(ImageFilter.GaussianBlur(k * 0.5))
    sombra = Image.fromarray((np.asarray(sombra).astype(float) * (np.asarray(mascara_int) / 255)).astype("uint8"), "L")
    boca.paste(Image.new("RGBA", (w, h), (0, 0, 0, 255)), (0, 0), sombra)
    boca = boca.filter(ImageFilter.GaussianBlur(0.5))
    # Cara interior de la tapa: silueta de la tapa en el color del cuerpo en sombra, con luz que entra por la
    # bisagra, un labio perimetral más claro y el panel rehundido alrededor del hueco de la tapa.
    alfa_tapa = (tapa[:, :, 3]).clip(0, 255).astype("uint8")
    sil = Image.fromarray(alfa_tapa, "L")
    filas = [y for y in range(h) if alfa_tapa[y].max() > 128]
    t0, t1 = filas[0], filas[-1]
    v = np.clip((np.arange(h) - t0) / max(1, t1 - t0), 0, 1)[:, None, None]
    base_rgb = np.maximum(tono * 0.56 + 14, 40) * (1.08 - 0.28 * v)
    interior_tapa = lienzo(base_rgb); interior_tapa.putalpha(sil)
    labio = sil.filter(ImageFilter.MinFilter(int(ancho * 0.03) | 1))
    anillo = Image.fromarray(np.clip(np.asarray(sil).astype(int) - np.asarray(labio).astype(int), 0, 255).astype("uint8"), "L").filter(ImageFilter.GaussianBlur(1.2))
    interior_tapa.alpha_composite(Image.composite(lienzo(np.clip(np.maximum(tono * 0.85 + 22, 66), 0, 255) * (1.05 - 0.2 * v)), Image.new("RGBA", (w, h), (0, 0, 0, 0)), anillo))
    panel = sil.filter(ImageFilter.MinFilter(int(ancho * 0.07) | 1)).filter(ImageFilter.GaussianBlur(4))
    capa_panel = lienzo(np.clip(np.maximum(tono * 0.40 + 8, 30), 0, 255) * (1.1 - 0.3 * v)); capa_panel.putalpha(Image.fromarray((np.asarray(panel) * 0.85).astype("uint8"), "L"))
    interior_tapa.alpha_composite(capa_panel)
    # Hueco de la tapa: solo la región oscura conectada al centro de la tapa (en la caneca negra hay sombras oscuras
    # en los bordes que no son el hueco).
    oscuro_bin = Image.fromarray(((lum < 28) & (alfa_tapa > 200)).astype("uint8") * 255, "L").copy()
    cx = int(centro)
    col = lum[t0:int(junta_lados), cx - 3:cx + 4].mean(axis=1)
    semilla = (cx, t0 + int(np.argmin(col)))
    if oscuro_bin.getpixel(semilla) == 255:
        ImageDraw.floodfill(oscuro_bin, semilla, 128)
        oscuro_bin = Image.fromarray((np.asarray(oscuro_bin) == 128).astype("uint8") * 255, "L")
    hueco = oscuro_bin.filter(ImageFilter.MaxFilter(3)).filter(ImageFilter.GaussianBlur(1))
    capa_hueco = Image.new("RGBA", (w, h), (8, 10, 9, 255)); capa_hueco.putalpha(hueco)
    interior_tapa.alpha_composite(capa_hueco)
    escala = ANCHO_WEB / w
    nuevo = (ANCHO_WEB, round(h * escala))
    def guardar(arr_o_img, nombre):
        img = arr_o_img if isinstance(arr_o_img, Image.Image) else Image.fromarray(arr_o_img.clip(0, 255).astype("uint8"), "RGBA")
        img.resize(nuevo, Image.LANCZOS).save(destino / f"{color}-{nombre}.webp", "WEBP", quality=88, method=6)
    guardar(tapa, "tapa"); guardar(cuerpo, "cuerpo"); guardar(boca, "boca"); guardar(interior_tapa, "tapa-interior")
    # Punto de giro: borde posterior de la tapa = parte superior de la junta en los costados.
    return {"color": color, "ancho": nuevo[0], "alto": nuevo[1], "junta_centro": round(curva[int(centro)] * escala),
            "junta_lados": round(junta_lados * escala), "alto_tapa": round(alto_tapa * escala), "bisagra_pct": round(100 * bisagra / h, 2)}


if __name__ == "__main__":
    o, dst = Path(sys.argv[1]), Path(sys.argv[2])
    dst.mkdir(parents=True, exist_ok=True)
    for c in ("verde", "blanca", "negra"):
        print(separar(o, dst, c))
