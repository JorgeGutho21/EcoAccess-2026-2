"""Separa conjuntos de ajuste y validación que no comparten fotos entre sí ni con el conjunto final de 198.

Uso: python preparar_ajuste.py <candidatas-commons> <dataset-resized.zip> <destino>

Las etiquetas de Commons vienen de una revisión visual (lista REVISADAS): se descartaron fotos con personas, bebés,
paisajes y objetos ajenos. Hay que ajustar solo con «ajuste»; «validacion» se usa una vez por decisión.
"""
import hashlib, json, os, random, sys, zipfile

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
RESULTADOS = os.path.join(RAIZ, 'docs', 'vision', 'resultados')
SEMILLA = 20261102
POR_CLASE_TRASHNET = 10
PLASTICOS = ['PET', 'PLASTICO_SIN_ID', 'BOLSA_PLASTICA']
METALES = ['METAL', 'LATA_ALUMINIO']
MIXTO = ['ObjetoDesconocido', 'OBJETO_COMPUESTO', 'SERVILLETA_USADA', 'BOLSA_PLASTICA', 'ORGANICO', 'TOALLA_HIGIENICA', 'CONDON', 'ENVASE_CONTAMINADO']
SUCIO = ['ENVASE_CONTAMINADO', 'ObjetoDesconocido', 'ORGANICO', 'OBJETO_COMPUESTO', 'ICOPOR_CONTAMINADO']
SANITARIO = ['TOALLA_HIGIENICA', 'PANAL', 'TAMPON']
REVISADAS = {
    'metalizado': (['01', '02', '05', '06', '07', '08', '10', '21', '22', '23'], 'OBJETO_COMPUESTO', None),
    'condon': (['01', '03', '04', '06', '10', '12'], 'CONDON', None),
    'panal': (['04', '05', '09', '12', '13', '14'], 'TOALLA_HIGIENICA', SANITARIO),
    'servilleta': (['01', '10'], 'SERVILLETA_USADA', None),
    'jeringa': (['01', '03', '04', '06', '08', '09', '10', '12', '13', '14', '21'], 'JERINGA', None),
    'sucio': (['13', '14', '22', '23'], 'ENVASE_CONTAMINADO', SUCIO),
    'multicapa': (['05', '06', '07', '08', '09', '10', '11', '13', '27'], 'MULTICAPA', None),
    'mixto': (['03', '06', '09'], 'ObjetoDesconocido', MIXTO),
}
CLASES_TRASHNET = {
    'cardboard': ('CARTON', None), 'glass': ('VIDRIO', None), 'paper': ('PAPEL', None),
    'plastic': ('PLASTICO_SIN_ID', PLASTICOS), 'metal': ('METAL', METALES), 'trash': ('ObjetoDesconocido', MIXTO),
}


def fila(nombre, contenido, esperada, aceptadas):
    f = {'Archivo': nombre, 'Sha256': hashlib.sha256(contenido).hexdigest().upper(), 'Esperada': esperada}
    if aceptadas:
        f['Aceptadas'] = aceptadas
    return f


def main():
    candidatas, zip_trashnet, destino = sys.argv[1:4]
    azar = random.Random(SEMILLA)
    os.makedirs(destino, exist_ok=True)
    conjuntos = {'ajuste': [], 'validacion': []}
    usadas = {x['Sha256'] for x in json.load(open(os.path.join(RESULTADOS, 'manifiesto_comparacion_198.json'), encoding='utf-8'))['Muestras']}
    fuentes = {x['archivo']: x for x in json.load(open(os.path.join(candidatas, 'candidatas.json'), encoding='utf-8'))}
    citas = []
    for clase, (numeros, esperada, aceptadas) in REVISADAS.items():
        orden = list(numeros)
        azar.shuffle(orden)
        for i, numero in enumerate(orden):
            nombre = f'{clase}-{numero}.jpg'
            contenido = open(os.path.join(candidatas, nombre), 'rb').read()
            with open(os.path.join(destino, nombre), 'wb') as f:
                f.write(contenido)
            conjunto = 'ajuste' if i % 2 == 0 else 'validacion'
            conjuntos[conjunto].append(fila(nombre, contenido, esperada, aceptadas))
            citas.append({**fuentes[nombre], 'conjunto': conjunto, 'etiqueta': esperada})
    excluidas = {d['nom'] for d in json.load(open(os.path.join(RESULTADOS, 'm512_aj.json'), encoding='utf-8'))['detalle']}
    with zipfile.ZipFile(zip_trashnet) as z:
        for clase, (esperada, aceptadas) in sorted(CLASES_TRASHNET.items()):
            nombres = sorted(n for n in z.namelist() if f'/{clase}/' in n and n.endswith('.jpg'))
            azar.shuffle(nombres)
            elegidas = 0
            for n in nombres:
                contenido = z.read(n)
                if hashlib.sha256(contenido).hexdigest().upper() in usadas or f'{clase}_{os.path.basename(n)}' in excluidas:
                    continue
                nombre = f'trashnet_{clase}_{os.path.basename(n)}'
                with open(os.path.join(destino, nombre), 'wb') as f:
                    f.write(contenido)
                conjuntos['ajuste' if elegidas < POR_CLASE_TRASHNET else 'validacion'].append(fila(nombre, contenido, esperada, aceptadas))
                elegidas += 1
                if elegidas == 2 * POR_CLASE_TRASHNET:
                    break
    for nombre, muestras in conjuntos.items():
        with open(os.path.join(destino, f'manifiesto_{nombre}.json'), 'w', encoding='utf-8') as f:
            json.dump({'Nombre': f'ecoaccess-{nombre}-{len(muestras)}', 'Licencia': 'TrashNet MIT; Wikimedia Commons según fotos_ampliadas.json', 'Muestras': muestras}, f, ensure_ascii=False)
        print(nombre, len(muestras))
    with open(os.path.join(RESULTADOS, 'fotos_ampliadas.json'), 'w', encoding='utf-8') as f:
        json.dump(citas, f, ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()
