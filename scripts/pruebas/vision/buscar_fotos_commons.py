"""Busca en Wikimedia Commons candidatas con licencia libre para las clases que el corpus no cubre.

Uso: python buscar_fotos_commons.py <carpeta-destino>

Solo descarga candidatas y su licencia: una persona debe revisarlas antes de etiquetarlas (fotos_ampliadas.json).
"""
import json, os, re, sys, time, urllib.parse, urllib.request

AGENTE = {'User-Agent': 'EcoAccessP3-evaluacion-academica/1.0 (proyecto universitario)'}
BUSQUEDAS = {
    'metalizado': ['potato chips bag', 'crisps packet', 'snack foil bag', 'chips bag litter'],
    'condon': ['condom wrapper', 'condom packet', 'used condom litter'],
    'panal': ['disposable diaper', 'sanitary pad', 'nappy litter'],
    'servilleta': ['used tissue paper', 'crumpled paper napkin', 'used toilet paper litter'],
    'jeringa': ['syringe litter', 'disposable syringe', 'discarded needle'],
    'sucio': ['greasy pizza box', 'dirty food container', 'food waste packaging litter', 'used takeaway container'],
    'multicapa': ['milk carton', 'juice carton', 'tetra pak carton'],
    'mixto': ['household garbage pile', 'mixed waste trash bag open', 'litter pile rubbish'],
}
LIBRES = re.compile(r'^(cc0|cc[- ]by|cc[- ]by[- ]sa|public domain|pd)', re.I)


def consultar(parametros):
    url = 'https://commons.wikimedia.org/w/api.php?' + urllib.parse.urlencode(parametros)
    with urllib.request.urlopen(urllib.request.Request(url, headers=AGENTE), timeout=60) as r:
        return json.load(r)


def main():
    destino = sys.argv[1]
    os.makedirs(destino, exist_ok=True)
    fuentes = []
    for clase, terminos in BUSQUEDAS.items():
        vistos = set()
        for termino in terminos:
            datos = consultar({'action': 'query', 'generator': 'search', 'gsrsearch': termino + ' filetype:bitmap', 'gsrnamespace': 6, 'gsrlimit': 12,
                               'prop': 'imageinfo', 'iiprop': 'url|extmetadata|mime', 'iiurlwidth': 800, 'format': 'json'})
            for pagina in sorted(datos.get('query', {}).get('pages', {}).values(), key=lambda p: p.get('index', 0)):
                info = pagina['imageinfo'][0]
                licencia = info.get('extmetadata', {}).get('LicenseShortName', {}).get('value', '')
                if pagina['title'] in vistos or info.get('mime') != 'image/jpeg' or not LIBRES.match(licencia):
                    continue
                vistos.add(pagina['title'])
                nombre = f"{clase}-{len(vistos):02d}.jpg"
                try:
                    with urllib.request.urlopen(urllib.request.Request(info['thumburl'], headers=AGENTE), timeout=60) as r:
                        contenido = r.read()
                except Exception as e:
                    print('omitida', pagina['title'], type(e).__name__)
                    continue
                with open(os.path.join(destino, nombre), 'wb') as f:
                    f.write(contenido)
                fuentes.append({'archivo': nombre, 'clase': clase, 'titulo': pagina['title'], 'licencia': licencia, 'url': info['descriptionurl']})
                time.sleep(0.8)
            time.sleep(1)
        print(clase, len(vistos))
    with open(os.path.join(destino, 'candidatas.json'), 'w', encoding='utf-8') as f:
        json.dump(fuentes, f, ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
