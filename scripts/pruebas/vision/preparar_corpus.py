"""Reconstruye el corpus de evaluación visual fuera del repositorio y genera su manifiesto con hashes.

Uso: python preparar_corpus.py <carpeta-destino> [--por-clase 27]

Descarga las fotos de Wikimedia Commons listadas en docs/vision/resultados/fuentes_fotos.json y, de TrashNet (MIT), una
muestra fija por clase que excluye las fotos usadas para ajustar el vocabulario. Registra lo que no pudo recuperar: nunca
inventa una foto ni una etiqueta. Las imágenes no se suben al repositorio.
"""
import argparse, hashlib, io, json, os, random, sys, time, urllib.parse, urllib.request, zipfile

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
RESULTADOS = os.path.join(RAIZ, 'docs', 'vision', 'resultados')
AGENTE = {'User-Agent': 'EcoAccessP3-evaluacion-academica/1.0 (proyecto universitario)'}
TRASHNET = 'https://github.com/garythung/trashnet/raw/master/data/dataset-resized.zip'
SEMILLA = 2026

PLASTICOS = ['PET', 'PLASTICO_SIN_ID', 'BOLSA_PLASTICA']
METALES = ['METAL', 'LATA_ALUMINIO']
SIN_MATERIAL = ['ObjetoDesconocido', 'OBJETO_COMPUESTO', 'SERVILLETA_USADA', 'BOLSA_PLASTICA', 'ORGANICO', 'TOALLA_HIGIENICA', 'CONDON']
CLASES_COMMONS = {
    'carton': ('CARTON', None), 'lata': ('LATA_ALUMINIO', METALES), 'pet': ('PET', PLASTICOS), 'vidrio': ('VIDRIO', None),
    'papel': ('PAPEL', None), 'multicapa': ('MULTICAPA', None), 'pila': ('PILA', None), 'medicamento': ('MEDICAMENTO', None),
    'bombillo': ('BOMBILLO', None), 'electronico': ('RAEE', None), 'organico': ('ORGANICO', None), 'noresiduo': ('SinResiduo', None),
}
# TrashNet solo conoce seis clases: «plastic» y «metal» admiten cualquier código de su familia y «trash» es residuo mixto.
CLASES_TRASHNET = {
    'cardboard': ('CARTON', None), 'glass': ('VIDRIO', None), 'paper': ('PAPEL', None),
    'plastic': ('PLASTICO_SIN_ID', PLASTICOS), 'metal': ('METAL', METALES), 'trash': ('ObjetoDesconocido', SIN_MATERIAL),
}


def bajar(url, intentos=4):
    for intento in range(intentos):
        try:
            with urllib.request.urlopen(urllib.request.Request(url, headers=AGENTE), timeout=120) as r:
                return r.read()
        except urllib.error.HTTPError as e:
            if e.code != 429 or intento == intentos - 1:
                raise
            time.sleep(int(e.headers.get('Retry-After') or 20))
    raise RuntimeError('sin respuesta')


def url_commons(titulo):
    consulta = urllib.parse.urlencode({'action': 'query', 'titles': titulo, 'prop': 'imageinfo', 'iiprop': 'url',
                                       'iiurlwidth': 1024, 'format': 'json'})
    datos = json.loads(bajar('https://commons.wikimedia.org/w/api.php?' + consulta))
    pagina = next(iter(datos['query']['pages'].values()))
    return pagina['imageinfo'][0]['thumburl']


def entrada(nombre, contenido, esperada, aceptadas):
    fila = {'Archivo': nombre, 'Sha256': hashlib.sha256(contenido).hexdigest().upper(), 'Esperada': esperada}
    if aceptadas:
        fila['Aceptadas'] = aceptadas
    return fila


def commons(destino, muestras, faltantes):
    fuentes = json.load(open(os.path.join(RESULTADOS, 'fuentes_fotos.json'), encoding='utf-8-sig'))
    for fuente in fuentes:
        nombre = fuente['archivo']
        clase = nombre.split('_', 1)[1].rsplit('-', 1)[0]
        ruta = os.path.join(destino, nombre)
        try:
            if not os.path.exists(ruta):
                contenido = bajar(url_commons(fuente['titulo']))
                with open(ruta, 'wb') as f:
                    f.write(contenido)
                time.sleep(1.5)
            esperada, aceptadas = CLASES_COMMONS[clase]
            muestras.append(entrada(nombre, open(ruta, 'rb').read(), esperada, aceptadas))
        except Exception as e:
            faltantes.append({'archivo': nombre, 'motivo': type(e).__name__})


def trashnet(destino, por_clase, muestras, faltantes):
    excluidas = set()
    for informe in ('m512_aj.json',):
        excluidas |= {d['nom'] for d in json.load(open(os.path.join(RESULTADOS, informe), encoding='utf-8'))['detalle']}
    archivo = os.path.join(destino, 'dataset-resized.zip')
    try:
        if not os.path.exists(archivo):
            with open(archivo, 'wb') as f:
                f.write(bajar(TRASHNET))
        with zipfile.ZipFile(archivo) as z:
            azar = random.Random(SEMILLA)
            for clase, (esperada, aceptadas) in sorted(CLASES_TRASHNET.items()):
                nombres = sorted(n for n in z.namelist() if f'/{clase}/' in n and n.endswith('.jpg'))
                nombres = [n for n in nombres if f'{clase}_{os.path.basename(n)}' not in excluidas]
                for n in azar.sample(nombres, min(por_clase, len(nombres))):
                    nombre = f'trashnet_{clase}_{os.path.basename(n)}'
                    contenido = z.read(n)
                    with open(os.path.join(destino, nombre), 'wb') as f:
                        f.write(contenido)
                    muestras.append(entrada(nombre, contenido, esperada, aceptadas))
    except Exception as e:
        faltantes.append({'archivo': 'TrashNet', 'motivo': type(e).__name__})


def main():
    argumentos = argparse.ArgumentParser()
    argumentos.add_argument('destino')
    argumentos.add_argument('--por-clase', type=int, default=27)
    a = argumentos.parse_args()
    os.makedirs(a.destino, exist_ok=True)
    muestras, faltantes = [], []
    commons(a.destino, muestras, faltantes)
    trashnet(a.destino, a.por_clase, muestras, faltantes)
    manifiesto = {'Nombre': f'ecoaccess-comparacion-{len(muestras)}', 'Licencia': 'TrashNet MIT; Wikimedia Commons según fuentes_fotos.json', 'Muestras': muestras}
    with open(os.path.join(a.destino, 'manifiesto.json'), 'w', encoding='utf-8') as f:
        json.dump(manifiesto, f, ensure_ascii=False)
    print(json.dumps({'muestras': len(muestras), 'faltantes': faltantes}, ensure_ascii=False))
    return 0 if muestras else 1


if __name__ == '__main__':
    sys.exit(main())
