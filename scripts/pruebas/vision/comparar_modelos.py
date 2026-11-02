import json,sys,time,base64,urllib.request,statistics,os
V=sys.argv[1]; modelo=sys.argv[2]; lado=sys.argv[3]; grupo=sys.argv[4]; prompt=sys.argv[5]; etiqueta=sys.argv[6]
ctx=int(sys.argv[7]) if len(sys.argv)>7 else 2048
esq=json.load(open(os.path.join(V,'prompts.json'),encoding='utf-8'))[prompt]
datos=json.load(open(os.path.join(V,'conjunto.json'),encoding='utf-8-sig'))[grupo]
MAP=esq['mapa']; ADM=set(['PET','CARTON','PAPEL','LATA','VIDRIO','MULTICAPA','PLASTICO','METAL'])
def llamar(c):
    r=urllib.request.Request('http://127.0.0.1:11434/api/chat',data=json.dumps(c).encode(),headers={'Content-Type':'application/json'})
    return json.load(urllib.request.urlopen(r,timeout=180))
res=[];
for x in datos:
    img=open(os.path.join(V,'px'+lado,x['nom']),'rb').read()
    c={'model':modelo,'stream':False,'think':False,'keep_alive':'15m','options':{'num_ctx':ctx,'num_predict':esq.get('max',30),'temperature':0},
       'messages':[{'role':'system','content':esq['sistema']},{'role':'user','content':esq['usuario'],'images':[base64.b64encode(img).decode()]}]}
    if esq.get('json'): c['format']=esq['json']
    if modelo.startswith('moondream') or modelo.startswith('qwen'): c.pop('think')
    t=time.perf_counter()
    try:
        r=llamar(c); total=time.perf_counter()-t; txt=r['message']['content'].strip()
        carga=r.get('load_duration',0)/1e9; pe=r.get('prompt_eval_duration',0)/1e9; ev=r.get('eval_duration',0)/1e9
        o=txt
        if esq.get('json'):
            try:
                jj=json.loads(txt); o=jj.get('objeto','?')
                if jj.get('certeza')=='low': o='lowconf'
            except Exception:o='?'
        o=o.strip().lower().strip('."\' ').split('\n')[0]
        obt=MAP.get(o)
        if obt is None:
            for k,v2 in MAP.items():
                if k in o: obt=v2;break
        obt=obt or 'ABST'
    except Exception as e:
        total=time.perf_counter()-t; txt='ERR '+str(e)[:60]; carga=pe=ev=0; obt='ERROR'
    esp=x['e']; acierto=obt==esp or (esp=='PLASTICO' and obt in('PET','PLASTICO')) or (esp=='METAL' and obt in('LATA','METAL')) or (esp=='NOAPROV' and obt in('NOAPROV','ABST','ORGANICO')) or (esp=='NINGUNO' and obt=='ABST')
    indebida=(esp not in ADM) and (obt in ADM)
    confusion=(esp in ADM) and (obt in ADM) and not acierto
    res.append(dict(nom=x['nom'],g=x['g'],esp=esp,obt=obt,ok=acierto,indebida=indebida,conf=confusion,t=round(total,2),carga=round(carga,2),img=round(pe,2),gen=round(ev,2),txt=txt[:70]))
    print(f"{x['nom'][:32]:32} esp={esp:11} obt={obt:11} {'OK ' if acierto else 'MAL'} {total:5.1f}s c={carga:4.1f} i={pe:4.1f} g={ev:4.1f} | {' '.join(txt.split())[:40]}",flush=True)
ts=[r['t'] for r in res][1:] or [r['t'] for r in res]
ts2=sorted(ts); p95=ts2[min(len(ts2)-1,int(len(ts2)*0.95))]
crit=[r for r in res if r['g']=='cotidiana']
res_={'etiqueta':etiqueta,'modelo':modelo,'lado':lado,'ctx':ctx,'n':len(res),'aciertos':sum(r['ok'] for r in res),'cotidiana':f"{sum(r['ok'] for r in crit)}/{len(crit)}",'indebidas':sum(r['indebida'] for r in res),'confusiones':sum(r['conf'] for r in res),'abstenciones':sum(r['obt']=='ABST' for r in res),'errores':sum(r['obt']=='ERROR' for r in res),'media_s':round(statistics.mean(ts),2),'mediana_s':round(statistics.median(ts),2),'p95_s':round(p95,2)}
print('RESUMEN '+json.dumps(res_,ensure_ascii=False))
json.dump({'resumen':res_,'detalle':res},open(os.path.join(V,'r_'+etiqueta+'.json'),'w',encoding='utf-8'),ensure_ascii=False,indent=0)
