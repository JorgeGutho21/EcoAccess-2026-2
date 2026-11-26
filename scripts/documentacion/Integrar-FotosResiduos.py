"""
Integra las fotografías de las carpetas CANECA BLANCA, CANECA VERDE y CANECA NEGRA al catálogo de EcoAccess.

Uso (Python 3 con Pillow y NumPy):
    python scripts/documentacion/Integrar-FotosResiduos.py "<carpeta que contiene CANECA BLANCA, CANECA VERDE y CANECA NEGRA>"

Qué hace:
  * Recorre las tres carpetas (recursivamente), cuenta, mide y detecta duplicados exactos (SHA-1) y archivos dañados.
  * NO modifica los originales. Genera versiones cuadradas de 360 px (JPEG) en assets/imagenes/residuos/<caneca>/fotos/.
  * LÁMINAS (guías visuales con varios residuos): detecta su cuadrícula real (filas y, dentro de cada fila, columnas)
    por las franjas sin contenido, quita la etiqueta de texto de cada panel cuando la tiene, recorta el fondo sobrante y
    guarda CADA PANEL como una foto individual de su residuo. La lámina completa nunca se publica. Si la cuadrícula
    detectada no coincide con los paneles declarados en COMPOSICIONES, la lámina se rechaza (no se publican recortes
    dudosos); las coordenadas se pueden fijar a mano en 'manual'.
  * Deduplicación: exacta por SHA-1 entre archivos y perceptual (dHash de 64 bits) entre las fotos de un mismo residuo.
    Dos residuos distintos con fotos parecidas NO se fusionan: solo se anotan en el informe.
  * Cada foto se asocia a un residuo del catálogo (CatalogoPrototipo) según lo que muestra la imagen, revisada una por
    una el 21/11/2026; el nombre del archivo NO decide la clasificación. Lo que no tiene residuo equivalente queda
    «pendiente» con la razón: no aparece en el catálogo visual ni se puede depositar.
  * Escribe docs/evidencias/catalogo_fotos/inventario_fotos.csv, recortes_laminas.csv, una lámina de revisión por
    composición (recortes/*.jpg, con el rectángulo y el residuo de cada panel) y
    src/EcoAccess.P3.Logica/Servicios/CatalogoFotos.g.cs.

Si se agregan fotos nuevas, añádelas a ASIGNACION (archivo -> residuo) o a COMPOSICIONES después de mirarlas; las que
no estén en ninguna tabla se reportan como «sin revisar» y no se integran.
"""
import csv
import hashlib
import os
import sys
import unicodedata

import numpy as np
from PIL import Image, ImageDraw, ImageOps

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
CARPETAS = {'CANECA BLANCA': 'blanca', 'CANECA VERDE': 'verde', 'CANECA NEGRA': 'negra'}
LADO = 360
# Dos fotos del MISMO residuo son repetidas si su dHash difiere en 6 bits o menos de 64 Y su color medio (miniatura
# 32x32) difiere en 6 o menos (0-255). Solo el dHash no basta: las ilustraciones comparten la misma mano de ECO y dos
# residuos distintos (té y café) quedaban a 2 bits; entre fotos realmente distintas el color difiere 14 o más.
UMBRAL_PERCEPTUAL = 6
UMBRAL_COLOR = 6.0

# Láminas con varios residuos: cada panel, en orden de lectura (filas de arriba abajo, de izquierda a derecha), es
# el residuo que muestra según su propia etiqueta o su contenido. 'etiqueta': el panel tiene un rótulo de texto abajo
# que se excluye del recorte. 'manual' (opcional): lista de (x0, y0, x1, y1) en píxeles del original si la detección
# automática no sirve.
COMPOSICIONES = {
    'Guía visual de reciclaje de papel y cartón-2.png': {
        'etiqueta': True,
        'paneles': ['periodico-usado', 'revista-usada', 'folletos', 'cartulina-limpia', 'carton-limpio',
                    'caja-cereal', 'cubeta-huevos', 'tubo-carton', 'caja-jugo', 'caja-leche'],
    },
    'Manos ECO con residuos compostables-2.png': {
        'etiqueta': True,
        'paneles': ['cascara-limon', 'cascara-aguacate', 'fruta-danada', 'restos-fruta', 'cascara-papa', 'cascara-yuca',
                    'cascara-platano-verde', 'cascara-zanahoria', 'restos-tomate', 'hojas-lechuga', 'restos-verdura'],
    },
    'Residuos orgánicos en manos robóticasitsoq-11.png': {
        'etiqueta': False,
        'paneles': ['restos-fruta', 'cascara-platano-verde', 'cascara-zanahoria', 'restos-tomate', 'hojas-lechuga', 'restos-verdura'],
    },
}

# Archivo original -> id del residuo en CatalogoPrototipo, o ('pendiente', razón).
ASIGNACION = {
    # ---------------- CANECA BLANCA ----------------
    'Imagen de ChatGPT 21 nov 2026, 10_37_16 a.m-1.png': 'envase-plastico',      # tapa/tarro plástico blanco
    'Imagen de ChatGPT 21 nov 2026, 10_37_17 a.m-2.png': 'vasos-plasticos',      # vaso plástico transparente limpio
    'Imagen de ChatGPT 21 nov 2026, 10_37_17 a.m-3.png': 'envase-plastico',      # frasco dispensador (jabón/champú)
    'Imagen de ChatGPT 21 nov 2026, 10_37_18 a.m-4.png': 'envase-plastico',      # galón de detergente
    'Imagen de ChatGPT 21 nov 2026, 10_37_19 a.m-5.png': 'envase-plastico',      # tarro plástico blanco
    'Imagen de ChatGPT 21 nov 2026, 10_37_19 a.m-6.png': ('pendiente', 'Bolsa plástica: Oracle tiene BOLSA_PLASTICA, pero el catálogo de la Estación Oficial aún no la incluye.'),
    'Imagen de ChatGPT 21 nov 2026, 10_37_20 a.m-8.png': 'envase-plastico',      # recipiente plástico con tapa
    'Imagen de ChatGPT 21 nov 2026, 10_37_21 a.m-10.png': 'papel-limpio',        # resma de hojas
    'Imagen de ChatGPT 21 nov 2026, 10_37_21 a.m-9.png': ('pendiente', 'Bolsa blanca de material ambiguo (papel, tela o plástico): confirmar antes de clasificar.'),
    'Mano Eco sosteniendo caja de cartón-10.png': 'carton-limpio',
    'Mano robótica ecológica con periódico-3.png': 'periodico-usado',
    'Mano robótica ecológica con refresco frío-1.png': 'lata-aluminio',
    'Mano robótica ecológica sosteniendo frasco de vidrio-5.png': 'vidrio',
    'Mano robótica ecológica sosteniendo frasco de vidrio.png': 'vidrio',
    'Mano robótica sosteniendo botella de vidrio-4.png': 'vidrio',
    'Mano robótica sosteniendo caja de cereal-12.png': 'caja-cereal',
    'Mano robótica sosteniendo caja de cereal-13.png': 'caja-cereal',
    'Mano robótica sosteniendo caja ecológica-11.png': 'carton-limpio',
    'Mano robótica sosteniendo cartulina verde-8.png': 'cartulina-limpia',
    'Mano robótica sosteniendo cartulina verde-9.png': 'cartulina-limpia',
    'Mano robótica sosteniendo folletos ecológicos-7.png': 'folletos',
    'Mano robótica sosteniendo maíz enlatado-3.png': ('pendiente', 'Lata de conserva (acero) llena: el catálogo solo tiene lata de aluminio; además debe mostrarse vacía.'),
    'Mano robótica sosteniendo periódico reciclado-1.png': 'periodico-usado',
    'Mano robótica sosteniendo revista ecológica-4.png': 'revista-usada',
    'Mano robótica sosteniendo revista ecológica-5.png': 'revista-usada',
    'Mano robótica sosteniendo una tapa dorada-2.png': ('pendiente', 'Tapa metálica: Oracle tiene METAL, pero el catálogo de la Estación Oficial aún no la incluye.'),
    'Volantes promocionales en mano robótica ecológica-6.png': 'folletos',
    # ---------------- CANECA VERDE ----------------
    'Imagen de ChatGPT 21 nov 2026, 11_31_34 a.m-1.png': 'restos-comida',        # arroz
    'Imagen de ChatGPT 21 nov 2026, 11_31_35 a.m-2.png': 'cafe-te',              # hojas de té usadas
    'Imagen de ChatGPT 21 nov 2026, 11_31_36 a.m-3.png': 'restos-verdura',       # pepino
    'Imagen de ChatGPT 21 nov 2026, 11_31_37 a.m-4.png': 'restos-verdura',       # cáscaras de cebolla
    'Imagen de ChatGPT 21 nov 2026, 11_31_37 a.m-5.png': 'cafe-te',              # borra de café
    'Imagen de ChatGPT 21 nov 2026, 11_31_38 a.m-6.png': 'poda-jardin',          # pasto
    'Imagen de ChatGPT 21 nov 2026, 11_31_39 a.m-7.png': 'poda-jardin',          # flores marchitas
    'Imagen de ChatGPT 21 nov 2026, 11_31_39 a.m-8.png': 'poda-jardin',          # ramas pequeñas
    'Imagen de ChatGPT 21 nov 2026, 11_31_40 a.m-9.png': 'restos-verdura',       # verduras mixtas
    'Imagen de ChatGPT 21 nov 2026, 11_31_41 a.m-10.png': 'hojas-secas',
    'Mano robótica ECO con cáscaras de yuca-12.png': 'cascara-yuca',
    'Mano robótica ECO con fruta fresca-5.png': 'restos-fruta',
    'Mano robótica ECO y manzana podrida-6.png': 'fruta-danada',
    'Mano robótica con cáscara de mandarina-5.png': 'cascara-mandarina',
    'Mano robótica con frutas coloridas-7.png': 'restos-fruta',
    'Mano robótica con frutas frescas-3.png': 'restos-fruta',
    'Mano robótica con frutas frescas-9.png': 'restos-fruta',
    'Mano robótica ecológica con cáscara de banana-1.png': 'cascara-banano',
    'Mano robótica ecológica con cáscara de naranja-3.png': 'cascara-naranja',
    'Mano robótica sosteniendo cáscara de mango-7.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de mango-8.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de naranja-4.png': 'cascara-naranja',
    'Mano robótica sosteniendo cáscara de papaya-9.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo cáscara de piña-10.png': 'cascaras-alimentos',
    'Mano robótica sosteniendo espirales de mandarina-6.png': 'cascara-mandarina',
    'Mano robótica sosteniendo frutas frescas-13.png': 'restos-fruta',
    'Mano robótica sosteniendo restos de fruta-8.png': 'restos-fruta',
    'Mano robótica sosteniendo una cáscara de aguacate-4.png': 'cascara-aguacate',
    'Mano robótica sosteniendo una cáscara de banana-2.png': 'cascara-banano',
    'Mano robótica sosteniendo una cáscara de lima-1.png': 'cascara-limon',
    'Mano robótica sosteniendo una cáscara de papa-10.png': 'cascara-papa',
    # ---------------- CANECA NEGRA ----------------
    'Gemini_Generated_Image_a3r2spa3r2spa3r2.jpg': 'condon-usado',
    'Gemini_Generated_Image_w01sf3w01sf3w01s.jpg': 'colilla',
    'Imagen de ChatGPT 21 nov 2026, 12_05_00 p.m-1.png': 'icopor-sucio',         # bandeja desechable con comida
    'Imagen de ChatGPT 21 nov 2026, 12_05_01 p.m-2.png': 'cubiertos-desechables', # pitillo
    'Imagen de ChatGPT 21 nov 2026, 12_05_02 p.m-3.png': 'cubiertos-desechables', # cubiertos plásticos
    'Imagen de ChatGPT 21 nov 2026, 12_05_03 p.m-4.png': ('pendiente', 'Palillo de madera usado: no hay residuo de madera en el catálogo de la Estación Oficial.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_03 p.m-5.png': 'vaso-contaminado',     # vaso desechable sucio
    'Imagen de ChatGPT 21 nov 2026, 12_05_05 p.m-6.png': 'papel-contaminado',    # papel con grasa
    'Imagen de ChatGPT 21 nov 2026, 12_05_06 p.m-7.png': ('pendiente', 'Papel aluminio con comida: no hay un residuo equivalente en el catálogo de la Estación Oficial.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_07 p.m-8.png': ('pendiente', 'Película plástica (vinipel) sucia: no hay un residuo equivalente en el catálogo de la Estación Oficial.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_08 p.m-9.png': ('pendiente', 'Plástico/cinta sucia de material no identificable: confirmar antes de clasificar.'),
    'Imagen de ChatGPT 21 nov 2026, 12_05_09 p.m-10.png': ('pendiente', 'Plato desechable sucio de material ambiguo (icopor, papel o plástico): confirmar antes de clasificar.'),
    'Mano ECO sosteniendo servilleta usada-10.png': 'servilletas-usadas',
    'Mano Robótica Sosteniendo Caja de Pizza Usada-23.png': 'papel-contaminado',
    'Mano eco robótica con rollo usado-7.png': 'papel-higienico',
    'Mano robótica ECO con servilleta usada-9.png': 'servilletas-usadas',
    'Mano robótica ECO con toalla grasienta-20.png': 'papel-contaminado',
    'Mano robótica ECO sostiene rollo usado-6.png': 'papel-higienico',
    'Mano robótica Eco con cartón grasiento-22.png': 'papel-contaminado',
    'Mano robótica con bolsa de basura 白小姐-15.png': ('pendiente', 'Bolsa negra de basura con residuos: muestra una mezcla, no un material.'),
    'Mano robótica con envoltura de galletas-3.png': 'envolturas',
    'Mano robótica ecológica con bolsa de chips-26.png': 'envolturas',
    'Mano robótica ecológica con papel arrugado-5.png': 'papel-contaminado',     # el nombre dice «papel arrugado», pero se ve manchado
    'Mano robótica ecológica con papel usado-1.png': 'servilletas-usadas',
    'Mano robótica ecológica con papel usado-4.png': 'papel-higienico',
    'Mano robótica ecológica con pañuelo usado-16.png': 'servilletas-usadas',
    'Mano robótica ecológica con toalla grasienta-17.png': 'papel-contaminado',
    'Mano robótica sosteniendo bolsa crujiente-25.png': 'envolturas',
    'Mano robótica sosteniendo bolsa de papas-28.png': 'envolturas',
    'Mano robótica sosteniendo caja de pizza usada-24.png': 'papel-contaminado',
    'Mano robótica sosteniendo caja grasienta-21.png': 'papel-contaminado',
    'Mano robótica sosteniendo paño engrasado-19.png': 'papel-contaminado',
    'Mano robótica sosteniendo pañuelo usado-14.png': 'servilletas-usadas',
    'Mano robótica sosteniendo residuos-11.png': ('pendiente', 'Bolsa negra con una servilleta: muestra una mezcla, no un material.'),
    'Mano robótica sosteniendo residuos-27.png': ('pendiente', 'Bolsa negra con una envoltura: muestra una mezcla, no un material.'),
    'Mano robótica sosteniendo rollo usado-8.png': 'papel-higienico',
    'Mano robótica sosteniendo servilleta grasienta-18.png': 'servilletas-usadas',
    'Mano robótica sosteniendo servilleta manchada-12.png': 'servilletas-usadas',
    'Puño robótico exprimiendo pañuelo manchado-13.png': 'servilletas-usadas',
    'Robomano ecológica con servilleta manchada-2.png': 'servilletas-usadas',
}

# Caneca de cada residuo (debe coincidir con CatalogoPrototipo; una prueba unitaria lo comprueba).
CANECA_RESIDUO = {
    **{r: 'verde' for r in ['cascara-banano', 'cascara-naranja', 'hojas-secas', 'mitad-kiwi', 'cascara-mandarina', 'mitad-limon',
                             'restos-fruta', 'restos-verdura', 'cascaras-alimentos', 'restos-comida', 'poda-jardin', 'cafe-te', 'semillas-tusas',
                             'cascara-limon', 'cascara-aguacate', 'fruta-danada', 'cascara-papa', 'cascara-yuca', 'cascara-platano-verde',
                             'cascara-zanahoria', 'restos-tomate', 'hojas-lechuga']},
    **{r: 'blanca' for r in ['botella-agua', 'botella-gaseosa', 'papel-cuaderno', 'caja-jugo', 'vasos-plasticos', 'botella-pet',
                              'envase-plastico', 'carton-limpio', 'papel-limpio', 'vidrio', 'lata-aluminio', 'caja-leche',
                              'periodico-usado', 'revista-usada', 'folletos', 'cartulina-limpia', 'caja-cereal', 'cubeta-huevos', 'tubo-carton']},
    **{r: 'negra' for r in ['papel-higienico', 'servilletas-usadas', 'vaso-contaminado', 'plato-icopor', 'papel-contaminado', 'envolturas',
                             'cubiertos-desechables', 'icopor-sucio', 'condon-usado', 'toalla-higienica', 'tampon-usado', 'panal-usado', 'colilla']},
}


# ───────────────────────────── Análisis de láminas ─────────────────────────────

def mascara_contenido(imagen):
    """True donde hay «tinta»: píxel saturado u oscuro (el fondo de las láminas es blanco o casi blanco)."""
    a = np.asarray(imagen.convert('RGB')).astype(np.int16)
    mx, mn = a.max(axis=2), a.min(axis=2)
    return ((mx - mn) > 28) | (mx < 215)


def franjas_vacias(perfil, umbral, minimo):
    """Tramos [a, b) donde la fracción de contenido es menor que `umbral`, de al menos `minimo` px."""
    vacias, inicio = [], None
    for i, v in enumerate(list(perfil) + [1.0]):
        if v < umbral and inicio is None:
            inicio = i
        elif v >= umbral and inicio is not None:
            if i - inicio >= minimo:
                vacias.append((inicio, i))
            inicio = None
    return vacias


def segmentos(perfil, fraccion_minima=0.12, umbral=0.03):
    """
    Divide un eje en paneles por sus franjas vacías: se aceptan cortes (de la franja más ancha a la más estrecha) solo si
    todos los segmentos resultantes miden al menos `fraccion_minima` del eje; así una franja dentro de un panel (entre
    el dibujo y su rótulo) no lo parte en dos. Devuelve los segmentos sin los márgenes vacíos de los extremos.
    """
    n = len(perfil)
    minimo = max(4, n // 200)
    vacias = franjas_vacias(perfil, umbral, minimo)
    inicio = next((b for a, b in vacias if a == 0), 0)
    fin = next((a for a, b in vacias if b == n), n)
    internas = sorted((v for v in vacias if v[0] > inicio and v[1] < fin), key=lambda v: v[0] - v[1])
    cortes = []
    for a, b in internas:
        prueba = sorted(cortes + [(a, b)])
        limites = [inicio] + [x for c in prueba for x in c] + [fin]
        tramos = [(limites[i], limites[i + 1]) for i in range(0, len(limites), 2)]
        if all(t[1] - t[0] >= fraccion_minima * (fin - inicio) for t in tramos):
            cortes = prueba
    limites = [inicio] + [x for c in cortes for x in c] + [fin]
    return [(limites[i], limites[i + 1]) for i in range(0, len(limites), 2)]


def quitar_rotulo(mascara, y0, y1):
    """Fila donde empieza el rótulo de texto inferior de una fila de paneles (o y1 si no se encuentra uno)."""
    alto = y1 - y0
    perfil = mascara[y0:y1].mean(axis=1)
    candidatas = [(a, b) for a, b in franjas_vacias(perfil, 0.06, max(4, alto // 120))
                  if a >= alto * 0.60 and (alto - b) >= alto * 0.06]
    if not candidatas:
        return None
    return y0 + min(a for a, _ in candidatas)


def separar_pegados(tramos, perfil):
    """
    Dos paneles sin franja blanca entre ellos forman un tramo mucho más ancho que los demás de la fila: se parte en
    round(ancho / mediana) por las columnas de menor contenido cerca de cada división esperada.
    """
    if len(tramos) < 2:
        return tramos
    mediana = float(np.median([b - a for a, b in tramos]))
    suave = np.convolve(perfil, np.ones(9) / 9, mode='same')
    resultado = []
    for a, b in tramos:
        partes = int(round((b - a) / mediana))
        if (b - a) < 1.6 * mediana or partes < 2:
            resultado.append((a, b))
            continue
        cortes = []
        for k in range(1, partes):
            centro = a + (b - a) * k / partes
            v0, v1 = int(centro - 0.15 * mediana), int(centro + 0.15 * mediana)
            cortes.append(v0 + int(np.argmin(suave[v0:v1])))
        limites = [a] + cortes + [b]
        resultado.extend((limites[i], limites[i + 1]) for i in range(len(limites) - 1))
    return resultado


def paneles(imagen, etiqueta):
    """Rectángulos (x0, y0, x1, y1) de cada panel, en orden de lectura; filas primero, columnas dentro de cada fila."""
    m = mascara_contenido(imagen)
    resultado = []
    for y0, y1 in segmentos(m.mean(axis=1)):
        corte = quitar_rotulo(m, y0, y1) if etiqueta else y1
        if corte is None:
            raise ValueError(f'No se encontró el rótulo de la fila {y0}-{y1}')
        perfil = m[y0:corte].mean(axis=0)
        for x0, x1 in separar_pegados(segmentos(perfil), perfil):
            resultado.append((x0, y0, x1, corte))
    return resultado


def recortar_panel(imagen, caja):
    """Recorta el panel al contenido (más un margen del 4 %) y lo centra en un cuadrado del color de su fondo."""
    panel = imagen.crop(caja)
    m = mascara_contenido(panel)
    filas, columnas = np.where(m.mean(axis=1) > 0.01)[0], np.where(m.mean(axis=0) > 0.01)[0]
    if len(filas) == 0 or len(columnas) == 0:
        raise ValueError('Panel vacío')
    x0, x1, y0, y1 = columnas[0], columnas[-1] + 1, filas[0], filas[-1] + 1
    margen = int(0.04 * max(x1 - x0, y1 - y0))
    x0, y0 = max(0, x0 - margen), max(0, y0 - margen)
    x1, y1 = min(panel.width, x1 + margen), min(panel.height, y1 + margen)
    contenido = panel.crop((x0, y0, x1, y1))
    lado = max(contenido.size)
    a = np.asarray(panel.convert('RGB'))
    esquinas = np.concatenate([a[:4, :4].reshape(-1, 3), a[:4, -4:].reshape(-1, 3), a[-4:, :4].reshape(-1, 3), a[-4:, -4:].reshape(-1, 3)])
    fondo = tuple(int(v) for v in np.median(esquinas, axis=0))
    cuadro = Image.new('RGB', (lado, lado), fondo)
    cuadro.paste(contenido, ((lado - contenido.width) // 2, (lado - contenido.height) // 2))
    return cuadro.resize((LADO, LADO), Image.LANCZOS), (caja[0] + x0, caja[1] + y0, caja[0] + x1, caja[1] + y1)


def contiene_varios(cuadro):
    """True si el recorte todavía se divide en dos o más paneles: indicaría que juntó dos productos."""
    m = mascara_contenido(cuadro)
    return len(segmentos(m.mean(axis=0), 0.30)) > 1 or len(segmentos(m.mean(axis=1), 0.30)) > 1


def dhash(imagen):
    """Huella perceptual de 64 bits: compara el brillo de píxeles vecinos en una miniatura de 9x8."""
    g = np.asarray(imagen.convert('L').resize((9, 8), Image.LANCZOS)).astype(np.int16)
    bits = (g[:, 1:] > g[:, :-1]).flatten()
    return int(''.join('1' if b else '0' for b in bits), 2)


def distancia(a, b):
    return bin(a ^ b).count('1')


def miniatura_color(imagen):
    return np.asarray(ImageOps.fit(imagen, (32, 32), Image.LANCZOS)).astype(np.float32)


def repetidas(huella_a, color_a, huella_b, color_b):
    """Misma foto (recomprimida, reescalada o con otro nombre): dHash y color casi iguales."""
    return distancia(huella_a, huella_b) <= UMBRAL_PERCEPTUAL and float(np.abs(color_a - color_b).mean()) <= UMBRAL_COLOR


# ───────────────────────────── Integración ─────────────────────────────

def literal_cs(texto):
    """Texto seguro dentro de una cadena C# normal."""
    return texto.replace(chr(92), chr(92) * 2).replace('"', chr(92) + '"')


def nfc(texto):
    return unicodedata.normalize('NFC', texto)


class Integracion:
    def __init__(self):
        self.filas, self.recortes, self.vistos = [], [], {}
        self.generado, self.pendientes, self.contadores = [], [], {}
        self.huellas = {}  # residuo -> [(huella, recurso)]
        self.similares = []
        self.inventario = {c: dict(originales=0, laminas=0, paneles=0, recortes=0, individuales=0, duplicadas=0,
                                   invalidas=0, pendientes=0) for c in CARPETAS.values()}

    def guardar_foto(self, imagen, caneca, residuo, origen):
        """Guarda una foto de un residuo si no repite (perceptualmente) otra del mismo residuo. Devuelve (estado, recurso, nota)."""
        if CANECA_RESIDUO.get(residuo) != caneca:
            sys.exit(f'{origen}: el residuo {residuo} no pertenece a la caneca {caneca}')
        huella, color = dhash(imagen), miniatura_color(imagen)
        for otra, otro_color, recurso in self.huellas.get(residuo, []):
            if repetidas(huella, color, otra, otro_color):
                return 'duplicada', '', f'Casi idéntica a {recurso} (dHash a {distancia(huella, otra)} bits)'
        for r, lista in self.huellas.items():
            if r != residuo:
                for otra, otro_color, recurso in lista:
                    if repetidas(huella, color, otra, otro_color):
                        self.similares.append((origen, residuo, recurso, r))
        n = self.contadores[residuo] = self.contadores.get(residuo, 0) + 1
        relativa = f'residuos/{caneca}/fotos/{residuo}-{n}.jpg'
        salida = os.path.join(RAIZ, 'assets', 'imagenes', *relativa.split('/'))
        os.makedirs(os.path.dirname(salida), exist_ok=True)
        imagen.save(salida, 'JPEG', quality=86, optimize=True, progressive=True)
        self.huellas.setdefault(residuo, []).append((huella, color, 'assets/imagenes/' + relativa))
        self.generado.append((residuo, caneca, f'{caneca}.fotos.{residuo}-{n}'))
        return 'integrada', 'assets/imagenes/' + relativa, ''

    def lamina(self, nombre, imagen, caneca, config, fila):
        inv = self.inventario[caneca]
        inv['laminas'] += 1
        cajas = config.get('manual') or paneles(imagen, config['etiqueta'])
        esperados = config['paneles']
        inv['paneles'] += len(cajas)
        if len(cajas) != len(esperados):
            fila['estado'] = 'lámina rechazada'
            fila['nota'] = f'Se detectaron {len(cajas)} paneles y se esperaban {len(esperados)}: revisar o fijar «manual».'
            return
        revision = imagen.copy()
        dibujo = ImageDraw.Draw(revision)
        publicados = 0
        for i, (caja, residuo) in enumerate(zip(cajas, esperados), start=1):
            cuadro, ajustada = recortar_panel(imagen, caja)
            if contiene_varios(cuadro):
                estado, recurso, nota = 'rechazado', '', 'El recorte parece contener más de un producto.'
            else:
                estado, recurso, nota = self.guardar_foto(cuadro, caneca, residuo, f'{nombre} panel {i}')
            if estado == 'integrada':
                publicados += 1
            elif estado == 'duplicada':
                inv['duplicadas'] += 1
            self.recortes.append({'carpeta': fila['carpeta'], 'lamina': nombre, 'panel': i, 'residuo': residuo,
                                  'x0': ajustada[0], 'y0': ajustada[1], 'x1': ajustada[2], 'y1': ajustada[3],
                                  'estado': estado, 'recurso': recurso, 'nota': nota})
            color = (0, 150, 70) if estado == 'integrada' else (200, 80, 0)
            dibujo.rectangle(ajustada, outline=color, width=max(3, imagen.width // 300))
            dibujo.rectangle((ajustada[0], ajustada[1], ajustada[0] + 260, ajustada[1] + 26), fill=color)
            dibujo.text((ajustada[0] + 6, ajustada[1] + 6), f'{i}. {residuo}', fill=(255, 255, 255))
        inv['recortes'] += publicados
        carpeta = os.path.join(RAIZ, 'docs', 'evidencias', 'catalogo_fotos', 'recortes')
        os.makedirs(carpeta, exist_ok=True)
        revision.thumbnail((1400, 1400), Image.LANCZOS)
        revision.save(os.path.join(carpeta, f'{caneca}_lamina_{inv["laminas"]}.jpg'), 'JPEG', quality=82)
        fila['estado'] = 'lámina'
        fila['nota'] = f'{publicados} de {len(cajas)} paneles publicados como fotos individuales; la lámina completa no se muestra.'

    def ejecutar(self, origen):
        composiciones = {nfc(k): v for k, v in COMPOSICIONES.items()}
        asignacion = {nfc(k): v for k, v in ASIGNACION.items()}
        for carpeta, caneca in CARPETAS.items():
            ruta = os.path.join(origen, carpeta)
            if not os.path.isdir(ruta):
                sys.exit(f'No existe la carpeta {ruta}')
            inv = self.inventario[caneca]
            for archivo in sorted(os.path.join(d, f) for d, _, fs in os.walk(ruta) for f in fs):
                nombre = nfc(os.path.basename(archivo))
                inv['originales'] += 1
                fila = {'carpeta': carpeta, 'archivo': nombre, 'ancho': '', 'alto': '', 'sha1': '', 'estado': '', 'residuo': '', 'recurso': '', 'nota': ''}
                self.filas.append(fila)
                try:
                    with Image.open(archivo) as im:
                        im.load()
                        fila['ancho'], fila['alto'] = im.size
                        imagen = ImageOps.exif_transpose(im).convert('RGB')
                except Exception as error:  # archivo dañado o que no es imagen
                    fila['estado'], fila['nota'] = 'descartada', f'No se pudo leer: {type(error).__name__}'
                    inv['invalidas'] += 1
                    continue
                with open(archivo, 'rb') as f:
                    fila['sha1'] = hashlib.sha1(f.read()).hexdigest()
                if fila['sha1'] in self.vistos:
                    fila['estado'], fila['nota'] = 'duplicada', f'Mismo contenido que {self.vistos[fila["sha1"]]}'
                    inv['duplicadas'] += 1
                    continue
                self.vistos[fila['sha1']] = nombre
                if nombre in composiciones:
                    self.lamina(nombre, imagen, caneca, composiciones[nombre], fila)
                    continue
                destino = asignacion.get(nombre)
                if destino is None:
                    fila['estado'], fila['nota'] = 'sin revisar', 'Agregar a ASIGNACION después de mirar la foto.'
                    inv['invalidas'] += 1
                    continue
                if isinstance(destino, tuple):
                    # Se conserva para el panel administrativo; no aparece en el catálogo visual ni se deposita.
                    fila['estado'], fila['nota'] = destino
                    n = self.contadores[f'{caneca}-pendiente'] = self.contadores.get(f'{caneca}-pendiente', 0) + 1
                    relativa = f'residuos/{caneca}/pendientes/pendiente-{n}.jpg'
                    salida = os.path.join(RAIZ, 'assets', 'imagenes', *relativa.split('/'))
                    os.makedirs(os.path.dirname(salida), exist_ok=True)
                    ImageOps.fit(imagen, (LADO, LADO), Image.LANCZOS).save(salida, 'JPEG', quality=86, optimize=True, progressive=True)
                    fila['recurso'] = 'assets/imagenes/' + relativa
                    self.pendientes.append((caneca, f'{caneca}.pendientes.pendiente-{n}', destino[1]))
                    inv['pendientes'] += 1
                    continue
                cuadro = ImageOps.fit(imagen, (LADO, LADO), Image.LANCZOS, centering=(0.5, 0.5))
                estado, recurso, nota = self.guardar_foto(cuadro, caneca, destino, nombre)
                fila['estado'], fila['residuo'], fila['recurso'], fila['nota'] = estado, destino, recurso, nota
                if estado == 'integrada':
                    inv['individuales'] += 1
                else:
                    inv['duplicadas'] += 1
        self.escribir()

    def escribir(self):
        informe = os.path.join(RAIZ, 'docs', 'evidencias', 'catalogo_fotos')
        os.makedirs(informe, exist_ok=True)
        for nombre, filas in (('inventario_fotos.csv', self.filas), ('recortes_laminas.csv', self.recortes)):
            with open(os.path.join(informe, nombre), 'w', newline='', encoding='utf-8-sig') as f:
                w = csv.DictWriter(f, fieldnames=list(filas[0].keys()))
                w.writeheader()
                w.writerows(filas)
        orden = ['verde', 'blanca', 'negra']
        lineas = [
            '// <auto-generated>',
            '// Generado por scripts/documentacion/Integrar-FotosResiduos.py: no editar a mano.',
            '// </auto-generated>',
            'using System.Collections.Generic;',
            '',
            'namespace EcoAccess.P3.Logica.Servicios',
            '{',
            '    public static partial class CatalogoFotos',
            '    {',
            '        /// <summary>(residuo, caneca, clave de imagen) de cada foto integrada (individual o panel recortado de una lámina).</summary>',
            '        private static readonly (string Residuo, string Caneca, string Clave)[] Generadas =',
            '        {',
            *[f'            ("{r}", "{c}", "{k}"),' for r, c, k in self.generado],
            '        };',
            '',
            '        /// <summary>Fotos sin residuo equivalente en el catálogo: solo para el panel administrativo, nunca en el catálogo visual.</summary>',
            '        private static readonly (string Caneca, string Clave, string Nota)[] PendientesGeneradas =',
            '        {',
            *[f'            ("{c}", "{k}", "{literal_cs(n)}"),' for c, k, n in self.pendientes],
            '        };',
            '',
            '        /// <summary>Inventario de las carpetas originales por caneca (lo calcula el script al integrar).</summary>',
            '        private static readonly InventarioFotos[] InventarioGenerado =',
            '        {',
            *[f'            new("{c}", {i["originales"]}, {i["laminas"]}, {i["paneles"]}, {i["recortes"]}, {i["individuales"]}, {i["duplicadas"]}, {i["invalidas"]}, {i["pendientes"]}),'
              for c in orden for i in [self.inventario[c]]],
            '        };',
            '    }',
            '}',
            '',
        ]
        with open(os.path.join(RAIZ, 'src', 'EcoAccess.P3.Logica', 'Servicios', 'CatalogoFotos.g.cs'), 'w', newline='\r\n', encoding='utf-8-sig') as f:
            f.write('\n'.join(lineas))

        print(f'{"caneca":8} originales láminas paneles recortes individuales duplicadas inválidas pendientes fotos')
        for c in orden:
            i = self.inventario[c]
            fotos = sum(1 for _, cc, _ in self.generado if cc == c)
            print(f'{c:8} {i["originales"]:10} {i["laminas"]:7} {i["paneles"]:7} {i["recortes"]:8} {i["individuales"]:11} {i["duplicadas"]:10} {i["invalidas"]:9} {i["pendientes"]:10} {fotos:5}')
        for origen, residuo, recurso, otro in self.similares:
            print(f'Aviso: {origen} ({residuo}) se parece a {recurso} ({otro}); se conservan separados.')
        rechazados = [r for r in self.recortes if r['estado'] == 'rechazado'] + [f for f in self.filas if f['estado'] == 'lámina rechazada']
        for r in rechazados:
            print('REVISAR:', r.get('lamina', r.get('archivo')), r.get('panel', ''), r['nota'])


if __name__ == '__main__':
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    Integracion().ejecutar(sys.argv[1])
