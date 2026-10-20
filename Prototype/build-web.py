"""Build a self-contained JavaScript game from the saved Unity scene assets."""
from pathlib import Path
import re, math, json, base64, struct, hashlib
ROOT=Path(__file__).resolve().parents[1]
ASSETS=ROOT/'Assets'
GUIDS={}
for meta in ASSETS.rglob('*.meta'):
    if meta.name.startswith('._'):continue
    match=re.search(r'^guid: (\w+)',meta.read_text(errors='ignore'),re.M)
    if match:GUIDS[match[1]]=Path(str(meta)[:-5])
def guid(name):
    return re.search(r'^guid: (\w+)',(ASSETS/'RecoilPrototype'/f'{name}.cs.meta').read_text(),re.M)[1]
def raw(b,k,default=None):
    m=re.search(r'^  '+re.escape(k)+r':(?: (.*))?$',b,re.M)
    return m[1] if m and m[1] is not None else default
def num(b,k,d=0):
    try:return float(raw(b,k,d))
    except (ValueError,TypeError):return d
def string(b,k,d=''):
    value=raw(b,k,d)
    if value.startswith('"'):
        try:return json.loads(value)
        except ValueError:pass
    return value

def ref(b,k):
    m=re.search(r'^  '+re.escape(k)+r': \{fileID: (\d+)',b,re.M)
    return m[1] if m else '0'
def vec(b,k,d=None):
    m=re.search(r'^  '+re.escape(k)+r': \{([^}]+)\}',b,re.M)
    return {k:float(v) for k,v in re.findall(r'(\w+): ([^,}]+)',m[1])} if m else (d or {'x':0,'y':0})
def params(b,defaults):return {k:num(b,k,v) for k,v in defaults.items()}
class Scene:
    def __init__(self,path):
        self.path=path;self.source=path.read_text()
        entries=list(re.finditer(r'^--- !u!(\d+) &(-?\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',self.source,re.M))
        ids=[m[2] for m in entries]
        if len(ids)!=len(set(ids)) or any(not -(2**63)<=int(i)<2**63 for i in ids):
            raise ValueError(f'{path}: duplicate or out-of-range Unity scene identifiers')
        self.docs={m[2]:(int(m[1]),m[3]) for m in entries}
        self.transforms={ref(b,'m_GameObject'):i for i,(typ,b) in self.docs.items() if typ==4};self.cache={}
    def b(self,i):return self.docs.get(i,(0,''))[1]
    def tid(self,b):return self.transforms.get(ref(b,'m_GameObject'),'0')
    def matrix(self,i):
        if i=='0':return [1,0,0,1,0,0]
        if i in self.cache:return self.cache[i]
        b=self.b(i);p=vec(b,'m_LocalPosition');s=vec(b,'m_LocalScale',{'x':1,'y':1});q=vec(b,'m_LocalRotation',{'x':0,'y':0,'z':0,'w':1})
        ang=math.atan2(2*(q['w']*q['z']+q['x']*q['y']),1-2*(q['y']**2+q['z']**2));co,si=math.cos(ang),math.sin(ang)
        a,bb,c,d=co*s['x'],si*s['x'],-si*s['y'],co*s['y'];A,B,C,D,X,Y=self.matrix(ref(b,'m_Father'))
        result=[A*a+C*bb,B*a+D*bb,A*c+C*d,B*c+D*d,A*p['x']+C*p['y']+X,B*p['x']+D*p['y']+Y];self.cache[i]=result;return result
    def pos(self,i):
        m=self.matrix(i);return {'x':m[4],'y':m[5]}
    def active(self,i):
        if i=='0':return True
        b=self.b(i);return bool(num(self.b(ref(b,'m_GameObject')),'m_IsActive',1)) and self.active(ref(b,'m_Father'))
    def under(self,i,parent):
        while i!='0':
            if i==parent:return True
            i=ref(self.b(i),'m_Father')
        return False
    def components(self,name,enabled=True):
        g=guid(name)
        return [(i,b) for i,(typ,b) in self.docs.items() if typ==114 and 'guid: '+g in b and (not enabled or (num(b,'m_Enabled',1) and self.active(self.tid(b))))]
    def on(self,name,tid):return next((b for i,b in self.components(name) if self.tid(b)==tid),'')
    def bounds(self,i,size,offset=None):
        a,b,c,d,x,y=self.matrix(i);o=offset or {'x':0,'y':0};x+=a*o['x']+c*o['y'];y+=b*o['x']+d*o['y'];w=abs(a)*size['x']+abs(c)*size['y'];h=abs(b)*size['x']+abs(d)*size['y'];return {'x':x-w/2,'y':y-h/2,'w':w,'h':h}
    def export(self):
        world=self.components('RecoilPrototype')[0][1];player=self.b(ref(world,'player'));playerTid=self.tid(player)
        spawn=ref(world,'spawnPoint');spawns={string(self.b(ref(b,'m_GameObject')),'m_Name'):self.pos(i) for i,(t,b) in self.docs.items() if t==4 and self.active(i)}
        cfg={'id':self.path.stem,'path':str(self.path.relative_to(ROOT)),'sha256':hashlib.sha256(self.path.read_bytes()).hexdigest(),'label':string(world,'locationLabel',self.path.stem),'spawn':self.pos(spawn if spawn!='0' else playerTid),'spawns':spawns,'objectiveWithoutCard':string(world,'objectiveWithoutCard'),'objectiveWithCard':string(world,'objectiveWithCard'),'startWithWaterGun':bool(num(world,'startWithWaterGun',1)),'enableCheckpoint':bool(num(world,'enableCheckpoint',1)),'legacyEvidenceEnabled':bool(num(world,'legacyEvidenceEnabled',1)),'checkpoint':self.pos(ref(world,'checkpointPoint')),'blocks':[],'sprites':[],'enemies':[],'doors':[],'items':[],'readables':[]}
        for i,(typ,b) in self.docs.items():
            if typ==61 and num(b,'m_Enabled',1) and not num(b,'m_IsTrigger',0) and self.active(self.tid(b)) and not self.on('SoftRoofPlatform',self.tid(b)) and not self.on('ClimbableLadder',self.tid(b)):
                block=self.bounds(self.tid(b),vec(b,'m_Size',{'x':1,'y':1}),vec(b,'m_Offset'));block['blocksSight']=not self.on('RecoilSurfaceVisibility',self.tid(b)) or bool(num(self.on('RecoilSurfaceVisibility',self.tid(b)),'blocksSight',0));block['disabledByItemId']=next((string(d,'openStateItemId') for _,d in self.components('RecoilDoor') if ref(d,'openingBarrier')==i),'');cfg['blocks'].append(block)
        for component,key in [('ClimbableLadder','ladders'),('SoftRoofPlatform','softRoofs')]:
            cfg[key]=[]
            for _,b in self.components(component):
                tid=self.tid(b);collider=next((c for typ,c in self.docs.values() if typ==61 and self.tid(c)==tid),'')
                if collider and num(collider,'m_Enabled',1):
                    cfg[key].append({**self.bounds(tid,vec(collider,'m_Size',{'x':1,'y':1}),vec(collider,'m_Offset')),**params(b,{'climbSpeed':2.4})} if component=='ClimbableLadder' else self.bounds(tid,vec(collider,'m_Size',{'x':1,'y':1}),vec(collider,'m_Offset')))
        cfg['dialogueTriggers']=[]
        cfg['chains']=[{**self.pos(self.tid(b)),**params(b,{'length':3.8,'climbSpeed':2.4,'grabRadius':.22,'gravity':9.81,'damping':.18,'inputAcceleration':7,'shotImpulse':4,'maximumAngle':80,'initialAngle':5,'linkSpacing':.18})} for _,b in self.components('SwingingChain')]
        cfg['stairways']=[]
        for _,b in self.components('WalkableStairway'):
            points=re.findall(r'^  - \{fileID: (\d+)\}',b,re.M)
            cfg['stairways'].append({'points':[self.pos(p) for p in points],**params(b,{'travelSpeed':3.6,'entryRadius':1.25,'entryHeightTolerance':.3,'catchRadius':.55})})
        cfg['handholds']=[]
        for _,b in self.components('EquipmentHandhold'):
            tid=self.tid(b);collider=next((c for typ,c in self.docs.values() if typ==61 and self.tid(c)==tid),'')
            cfg['handholds'].append({**self.bounds(tid,vec(collider,'m_Size'),vec(collider,'m_Offset')),'position':self.pos(ref(b,'holdPoint') if ref(b,'holdPoint')!='0' else tid),'releaseCooldown':num(b,'releaseCooldown',.5)})
        for _,b in self.components('PlayerDialogueTrigger'):
            tid=self.tid(b);collider=next((c for typ,c in self.docs.values() if typ==61 and self.tid(c)==tid),'')
            if collider and num(collider,'m_Enabled',1):
                cfg['dialogueTriggers'].append({**self.bounds(tid,vec(collider,'m_Size',{'x':1,'y':1}),vec(collider,'m_Offset')),'message':string(b,'message'),'displayDuration':num(b,'displayDuration',5),'triggerOnce':bool(num(b,'triggerOnce',1))})
        cfg['player']=params(player,{'walkSpeed':3.6,'sneakSpeed':1.65,'hoverSpeed':2.2,'verticalHoverSpeed':1.5,'gravity':14,'jumpSpeed':5,'maxHealth':3,'damageSlowDuration':1,'healthRecoveryInterval':1,'shotInterval':1,'shotRange':6,'recoilDecayDuration':.75,'flightDuration':1.25,'ammoCapacity':3,'groundRefillInterval':1})
        noise=self.on('PlayerNoiseEmitter',playerTid);cfg['noise']=params(noise,{'footstepRadius':3.2,'footstepInterval':.45,'propagationSpeed':10})
        cfg['player']['automaticRecoil']=bool(num(player,'automaticRecoil',1));cfg['player']['postRecoilFlightDuration']=num(player,'postRecoilFlightDuration',.5)
        for cid,controller in self.components('RampagerController'):
            tid=self.tid(controller);p=self.pos(tid);motor=self.on('RampagerMovement',tid);senses=self.on('RampagerPerception',tid);attack=self.on('RampagerAttack',tid);route=self.on('RampagerPatrolRoute',tid)
            e={'name':string(self.b(ref(controller,'m_GameObject')),'m_Name'),**p,'initialState':{0:'roaming',2:'attack',4:'sleeping'}.get(int(num(controller,'initialState',0)),'roaming'),'spawnFacing':1 if num(controller,'spawnFacing',0)==1 else -1,'returnToSleepAfterSound':bool(num(controller,'returnToSleepAfterSound',1))}
            e.update(params(controller,{'soundInspectionDuration':1.5,'sleepDelay':10,'wakeDuration':1,'roamingSightPitch':-25,'waveSightPitch':15,'nearDetectionDistance':1.5,'nearAttackTime':.3,'farAttackTime':3,'attackLostTime':20,'reportRadius':10,'reportFreshness':3,'searchTime':6,'attackSpeedMultiplier':1.5,'highFlightStopBodies':1.5,'stoppedTolerance':.5,'reverseTolerance':.5}))
            e['movement']=params(motor,{'leftOffset':-2,'rightOffset':2,'patrolSpeed':.9,'investigateSpeed':1.25,'chaseSpeed':2.6,'swarmAttackSpeedMultiplier':1.4,'jumpHeightInBodies':.7,'jumpCooldown':5,'jumpGravity':14,'jumpTriggerDistance':2.5})
            e['perception']=params(senses,{'idealSightRange':10.666667,'fieldOfView':100,'minimumExposure':.34});e['attack']=params(attack,{'attackInterval':1.5,'contactRadius':.78});e['sleepPosition']=self.pos(ref(controller,'sleepPoint')) if ref(controller,'sleepPoint')!='0' else p
            e['patrol']=None
            if route:
                points=[{**self.pos(self.tid(b)),**params(b,{'order':0,'duration':2,'lookInterval':.75}),'action':['stay','lookAround','lookLeft','lookRight'][int(num(b,'action',0))]} for _,b in self.components('RampagerPatrolPoint') if self.under(self.tid(b),tid)]
                e['patrol']={'points':points,'loop':bool(num(route,'loop',1))}
            cfg['enemies'].append(e)
        dynamicPropVisuals=[]
        exclusions=[playerTid]+[self.tid(b) for _,b in self.components('RampagerController')]
        cfg['towerTriggers']=[]
        for _,b in self.components('TowerSirenTrigger'):
            platform=ref(b,'highestPlatform');origin=ref(b,'waveOrigin');beacon=self.tid(self.b(ref(b,'beacon')))
            collider=next((c for typ,c in self.docs.values() if typ==61 and self.tid(c)==platform),'')
            bounds=self.bounds(platform,vec(collider,'m_Size',{'x':1,'y':1}),vec(collider,'m_Offset'))
            cfg['towerTriggers'].append({'x':self.pos(platform)['x'],'top':bounds['y']+bounds['h'],'source':self.pos(origin),'horizontalRange':num(b,'horizontalRange',3.5),'blinkPeriod':num(b,'blinkPeriod',.65),'progressId':string(b,'progressId','station_tower_siren_triggered'),'beacon':self.bounds(beacon,{'x':1,'y':1}),'restColor':vec(self.b(ref(b,'beacon')),'m_Color',{'r':.4,'g':0,'b':0,'a':1})})
            exclusions.append(beacon)
        for _,b in self.components('RecoilDoor'):
            tid=self.tid(b);exclusions.append(tid);cfg['doors'].append({**self.pos(tid),**params(b,{'interactionRange':1.7,'autoExitHalfWidth':1,'autoExitHeightOffset':0}),'interactionOffset':vec(b,'interactionOffset',{'x':0,'y':-.25}),'requiresClue':bool(num(b,'requiresClue',1)),'requiredItemId':string(b,'requiredItemId'),'completesLevel':bool(num(b,'completesLevel',1)),**{k:string(b,k) for k in ['destinationScene','destinationSpawn','interactionPrompt','lockedMessage','openedMessage','blockedByItemId','blockedMessage','rememberOnTravel','openStateItemId','openPrompt','travelPrompt','firstOpenMessage']},'permanentlyBlocked':bool(num(b,'permanentlyBlocked',0)),'requiresFlyingToOpen':bool(num(b,'requiresFlyingToOpen',0)),'autoTravelAboveOpening':bool(num(b,'autoTravelAboveOpening',0))})
        for door,(_,b) in zip(cfg['doors'],self.components('RecoilDoor')):
            door['kind']='office' if door['openStateItemId'].startswith('factory_office_door_') else 'standard'
            tid=self.tid(b)
            for _,(typ,sr) in self.docs.items():
                stid=self.tid(sr)
                if typ!=212 or not self.under(stid,tid):continue
                match=re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)',sr);asset=GUIDS.get(match[1]) if match else None
                if asset and asset.name=='SteelFireExit.png':
                    data=asset.read_bytes();w,h=struct.unpack('>II',data[16:24]);meta=Path(str(asset)+'.meta').read_text();ppu=float(re.search(r'spritePixelsToUnits: ([\d.]+)',meta)[1]);door['artwork']={'matrix':self.matrix(stid),'size':[w/ppu,h/ppu],'color':{'r':1,'g':1,'b':1,'a':1},'image':'data:image/png;base64,'+base64.b64encode(data).decode()}
        for name in ['Backpack','ContinuousInteractionItem','FuelPumpInteraction']:
            for _,b in self.components(name):
                tid=self.tid(b);exclusions.append(tid);cfg['items'].append({**self.pos(tid),'id':string(b,'itemId','backpack'),'name':string(b,'displayName','Backpack'),'description':string(b,'description'),'duration':num(b,'interactionDuration',1.5),'range':num(b,'interactionRange',1.5),'resultId':string(b,'resultId','employee_access_card'),'resultMessage':string(b,'resultMessage','Employee access card acquired.')})
        for _,b in self.components('FuelPumpInteraction'):
            tid=self.tid(b)
            # Keep the pump body in the scenery; its screen and nozzle are dynamic.
            if tid in exclusions:exclusions.remove(tid)
            display=self.transforms.get(ref(b,'serviceDisplay'),'0');fault=self.transforms.get(ref(b,'outOfOrderDisplay'),'0');nozzle=ref(b,'nozzle')
            dynamicPropVisuals.extend([display,fault,nozzle]);exclusions.extend([display,fault,nozzle])
            item=next(i for i in cfg['items'] if i['id']==string(b,'itemId'))
            item.update({'kind':'fuelPump','stationId':ref(self.b(tid),'m_Father'),'screen':self.bounds(display,{'x':1,'y':1}),'nozzle':self.pos(nozzle),'pickupPoseDuration':num(b,'pickupPoseDuration',.8)})
        for _,b in self.components('ReadableProp'):
            tid=self.tid(b);m=re.search(r'pageImage: \{fileID: \d+, guid: (\w+)',b);asset=GUIDS.get(m[1]) if m else None
            image='data:image/png;base64,'+base64.b64encode(asset.read_bytes()).decode() if asset else None
            cfg['readables'].append({**self.pos(tid),'name':string(b,'displayName','Poster'),'prompt':string(b,'interactionPrompt','E / READ'),'range':num(b,'interactionRange',1.5),'image':image,'text':string(b,'pageText')})
        lighting=self.components('RecoilLighting2D')[0][1];area=ref(lighting,'factoryArea');bounds=self.bounds(area,{'x':1,'y':1});cfg['lighting']=params(lighting,{'outdoorAmbient':.28,'unlitAmbientScale':.2,'playerLightRadius':5.4,'playerLightIntensity':.48,'swarmFlickerIntensity':.45,'swarmFlickerPeriod':1.4,'swarmFlashDuration':.45})
        cfg['lighting']['factoryArea']={'enabled':area!='0' and self.active(area),'minX':bounds['x'],'maxX':bounds['x']+bounds['w'],'minY':bounds['y'],'maxY':bounds['y']+bounds['h']}
        cfg['lighting']['fullyLitBrightness']=num(self.on('PlayerLightExposure',playerTid),'fullyLitBrightness',.45);cfg['lighting']['fixedLights']=[]
        for _,b in self.components('RecoilLight2D'):
            tid=self.tid(b);cfg['lighting']['fixedLights'].append({**self.pos(tid),**params(b,{'radius':2,'intensity':.25}),'enabled':True})
        cfg['lighting']['streetLamps']=[]
        for _,b in self.components('RecoilStreetLamp'):
            tid=self.tid(b);head=ref(b,'lampHead');head=head if head!='0' else tid;a,bb,c,d,x,y=self.matrix(head);norm=math.hypot(c,d)
            cfg['lighting']['streetLamps'].append({'x':x,'y':y,'direction':{'x':-c/norm,'y':-d/norm},**params(b,{'coneLength':4.8,'coneHalfWidth':2,'interactionRange':1.2}),'initiallyOn':bool(num(b,'initiallyOn',1)),'unstableStreetLamp':bool(num(b,'unstableStreetLamp',0)),'brightDuration':vec(b,'brightDuration',{'x':.5,'y':1.5}),'darkDuration':vec(b,'darkDuration',{'x':2.5,'y':3.5})})
        sw=self.components('SoundWaveManager',False)[0][1];cfg['sound']=params(sw,{'rounds':3,'maxRounds':4,'initialDelay':12,'interval':15,'warningDuration':3,'moveDelay':.1,'holdDuration':1.5,'commandDuration':1,'recoveryDuration':1.25});cfg['sound']['buildingBroadcastsEnabled']=bool(num(sw,'buildingBroadcastsEnabled',1) and num(sw,'m_Enabled',1) and self.active(self.tid(sw)));cfg['sound']['source']=self.pos(ref(sw,'emitter'));cfg['sound']['area']=self.bounds(ref(sw,'controlArea'),{'x':1,'y':1})
        camera=self.components('RecoilCameraController');cfg['camera']=params(camera[0][1] if camera else '',{'viewSize':6.7,'followSpeed':4,'maximumMouseOffset':2.5,'maximumDownwardMouseOffset':.5,'mouseLookSensitivity':.25,'minimumY':4.8,'maximumY':7});cfg['camera']['followOffset']=vec(camera[0][1] if camera else '', 'followOffset',{'x':2,'y':2.8});cfg['camera']['mouseLookEnabled']=bool(num(camera[0][1] if camera else '', 'mouseLookEnabled',1));cfg['camera']['limitDownwardMouseOffset']=bool(num(camera[0][1] if camera else '', 'limitDownwardMouseOffset',0));cfg['camera']['limitVertical']=bool(num(camera[0][1] if camera else '','limitVertical',1))
        darkness=self.tid(self.b(ref(lighting,'darkness')))
        for _,(typ,b) in self.docs.items():
            if typ!=212 or not num(b,'m_Enabled',1):continue
            tid=self.tid(b)
            if not self.active(tid) or tid==darkness or any(self.under(tid,parent) for parent in exclusions):continue
            color=vec(b,'m_Color',{'r':1,'g':1,'b':1,'a':1});match=re.search(r'm_Sprite: \{fileID: \d+, guid: (\w+)',b);asset=GUIDS.get(match[1]) if match else None
            size=[1,1];image=None
            if asset and asset.suffix.lower()=='.png':
                data=asset.read_bytes();w,h=struct.unpack('>II',data[16:24]);meta=Path(str(asset)+'.meta').read_text();ppm=re.search(r'spritePixelsToUnits: ([\d.]+)',meta);ppu=float(ppm[1]) if ppm else 100;size=[w/ppu,h/ppu]
                if asset.name!='PrototypeSquare.png':image='data:image/png;base64,'+base64.b64encode(data).decode()
            cfg['sprites'].append({'matrix':self.matrix(tid),'size':size,'color':color,'order':num(b,'m_SortingOrder',0),'image':image})
        cfg['texts']=[{**self.pos(self.tid(b)),'text':string(b,'m_Text'),'size':num(b,'m_CharacterSize',.1)} for _,(typ,b) in self.docs.items() if typ==102 and self.active(self.tid(b)) and not any(self.under(self.tid(b),parent) for parent in dynamicPropVisuals)]
        cfg['sprites'].sort(key=lambda s:s['order']);return cfg
if __name__=='__main__':
    scene_paths=[ASSETS/'Scenes'/name for name in ['RecoilPrototype.unity','RecoilFactoryInterior.unity','RecoilServiceStore.unity','RecoilFactoryWarehouse.unity','RecoilFactorySewer.unity','RecoilWarehouseStoreroom.unity']]
    scene_paths+=sorted((ASSETS/'Scenes'/'FactoryRooms').glob('*.unity'))
    scenes=[Scene(path).export() for path in scene_paths]
    config={'initialScene':scenes[0]['id'],'scenes':{s['id']:s for s in scenes}}
    output=ROOT/'Prototype/scene-config.js';output.write_text('// Generated from saved Unity scenes. Rebuild with Prototype/build-web.py.\n(function(root){const config='+json.dumps(config,ensure_ascii=False,separators=(',',':'))+';root.RecoilSceneConfig=config;if(typeof module!=="undefined")module.exports=config;})(globalThis);\n')
    html=(ROOT/'Prototype/index.html').read_text()
    html=re.sub(r'<script src="([^"?]+)(?:\?[^\"]*)?"></script>',lambda m:'<script>\n'+(ROOT/'Prototype'/m[1]).read_text().replace('</script','<\\/script')+'\n</script>',html)
    folder=ROOT/'Builds/Recoil-JavaScript';folder.mkdir(parents=True,exist_ok=True);(folder/'index.html').write_text(html)
    print(json.dumps({'output':str(folder/'index.html'),'scenes':[{'scene':s['id'],'enemies':len(s['enemies']),'colliders':len(s['blocks']),'sprites':len(s['sprites']),'items':len(s['items']),'doors':len(s['doors']),'broadcasts':s['sound']['buildingBroadcastsEnabled']} for s in scenes]},ensure_ascii=False,indent=2))
