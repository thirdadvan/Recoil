"""Export the saved Unity scene's environmental lighting for the browser prototype."""
from pathlib import Path
import json, re, math, sys
scene=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parents[1]/'Assets/Scenes/RecoilPrototype.unity'
source=scene.read_text()
blocks={m[2]:m[3] for m in re.finditer(r'^--- !u!(\d+) &(\d+)\n([\s\S]*?)(?=^--- !u!|\Z)',source,re.M)}
def scalar(block,key,default):
 m=re.search(r'^  '+re.escape(key)+r': ([^\n]+)',block,re.M)
 return float(m[1]) if m else default
def ref(block,key):
 m=re.search(r'^  '+re.escape(key)+r': \{fileID: (\d+)\}',block,re.M)
 return m[1] if m else '0'
def vector(block,key,default):
 m=re.search(r'^  '+re.escape(key)+r': \{([^}]+)\}',block,re.M)
 return {k:float(v) for k,v in re.findall(r'(\w+): ([^,}]+)',m[1])} if m else default
cache={}
def matrix(tid):
 if tid=='0':return (1,0,0,1,0,0)
 if tid in cache:return cache[tid]
 b=blocks[tid];p=vector(b,'m_LocalPosition',{'x':0,'y':0});s=vector(b,'m_LocalScale',{'x':1,'y':1});q=vector(b,'m_LocalRotation',{'x':0,'y':0,'z':0,'w':1})
 angle=math.atan2(2*(q['w']*q['z']+q['x']*q['y']),1-2*(q['y']**2+q['z']**2));c,sn=math.cos(angle),math.sin(angle)
 a,b0,c0,d,tx,ty=c*s['x'],sn*s['x'],-sn*s['y'],c*s['y'],p['x'],p['y']
 A,B,C,D,X,Y=matrix(ref(b,'m_Father'))
 result=(A*a+C*b0,B*a+D*b0,A*c0+C*d,B*c0+D*d,A*tx+C*ty+X,B*tx+D*ty+Y);cache[tid]=result;return result
def active(tid):
 if tid=='0':return True
 b=blocks[tid];go=blocks[ref(b,'m_GameObject')]
 return bool(scalar(go,'m_IsActive',1)) and active(ref(b,'m_Father'))
meta=scene.parents[1]/'RecoilPrototype/RecoilLighting2D.cs.meta';guid=re.search(r'guid: (\w+)',meta.read_text())[1]
lighting=next(b for b in blocks.values() if 'guid: '+guid in b)
areaId=ref(lighting,'factoryArea')
area={'enabled':False,'minX':0,'maxX':0,'minY':0,'maxY':0}
if areaId!='0':
 a,b,c,d,x,y=matrix(areaId);w,h=abs(a)+abs(c),abs(b)+abs(d)
 area={'enabled':active(areaId),'minX':x-w/2,'maxX':x+w/2,'minY':y-h/2,'maxY':y+h/2}
fixed=[]
section=re.search(r'  fixedLights:\n((?:  - .*\n)*)',lighting)
if section:
 for cid in re.findall(r'fileID: (\d+)',section[1]):
  if cid=='0':continue
  lamp=blocks[cid];go=blocks[ref(lamp,'m_GameObject')];tid=re.search(r'- component: \{fileID: (\d+)\}',go)[1];*_,x,y=matrix(tid)
  fixed.append({'x':x,'y':y,'radius':scalar(lamp,'radius',2),'intensity':scalar(lamp,'intensity',.25),'enabled':bool(scalar(lamp,'m_Enabled',1)) and active(tid)})
streetGuid=re.search(r'guid: (\w+)',(scene.parents[1]/'RecoilPrototype/RecoilStreetLamp.cs.meta').read_text())[1]
streetLamps=[]
for lamp in blocks.values():
 if 'guid: '+streetGuid not in lamp:continue
 go=blocks[ref(lamp,'m_GameObject')];tid=re.search(r'- component: \{fileID: (\d+)\}',go)[1]
 if not active(tid) or not scalar(lamp,'m_Enabled',1):continue
 head=ref(lamp,'lampHead');*_,x,y=matrix(head if head!='0' else tid)
 streetLamps.append({'x':x,'y':y,'coneLength':scalar(lamp,'coneLength',4.8),'coneHalfWidth':scalar(lamp,'coneHalfWidth',2),'interactionRange':scalar(lamp,'interactionRange',1.2),'initiallyOn':bool(scalar(lamp,'initiallyOn',1))})
config={'streetLamps':streetLamps,'outdoorAmbient':scalar(lighting,'outdoorAmbient',.28),'factoryArea':area,'fixedLights':fixed}
for key,default in [('playerLightRadius',5.4),('playerLightIntensity',.48),('swarmFlickerIntensity',.45),('swarmFlickerPeriod',1.4),('swarmFlashDuration',.45)]:config[key]=scalar(lighting,key,default)
exposureGuid=re.search(r'guid: (\w+)',(scene.parents[1]/'RecoilPrototype/PlayerLightExposure.cs.meta').read_text())[1]
exposure=next((b for b in blocks.values() if 'guid: '+exposureGuid in b),'')
config['fullyLitBrightness']=scalar(exposure,'fullyLitBrightness',.45)
out=Path(__file__).with_name('scene-lighting-config.js');out.write_text('// Generated from '+scene.name+' by export-scene-lighting.py.\n(function(root){\n  const config='+json.dumps(config,ensure_ascii=False,indent=2)+';\n  root.RecoilSceneLightingConfig=config;if(typeof module!==\'undefined\')module.exports=config;\n})(globalThis);\n')
print('Exported saved scene lighting:', 'indoor enabled='+str(area['enabled']), 'outdoor ambient='+str(config['outdoorAmbient']))
