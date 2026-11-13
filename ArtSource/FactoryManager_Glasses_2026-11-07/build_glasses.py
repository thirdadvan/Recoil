import math,json,os
verts=[];tris=[]
def v(p):verts.append(list(p));return len(verts)-1
def face(a,b,c,d):tris.extend([a,b,c,a,c,d])
def rim(cx):
 w=.064;h=.034;t=.0048;z=1.5565
 def corners(w,h,r):return [(-w/2+r,-h/2),(w/2-r,-h/2),(w/2,-h/2+r),(w/2,h/2-r),(w/2-r,h/2),(-w/2+r,h/2),(-w/2,h/2-r),(-w/2,-h/2+r)]
 ids=[]
 for depth in [-.0032,.0032]:
  for pts in [corners(w,h,.004),corners(w-2*t,h-2*t,.002)]:
   ids.append([v((cx+x,-.143+abs(cx+x)*.37+depth,z+zz)) for x,zz in pts])
 of,inf,ob,inb=ids
 for i in range(8):
  j=(i+1)%8
  face(of[i],of[j],inf[j],inf[i]);face(ob[j],ob[i],inb[i],inb[j]);face(of[j],of[i],ob[i],ob[j]);face(inf[i],inf[j],inb[j],inb[i])
def sub(a,b):return tuple(x-y for x,y in zip(a,b))
def norm(a):l=math.sqrt(sum(x*x for x in a));return tuple(x/l for x in a)
def cross(a,b):return (a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0])
def bar(a,b,thick):
 axis=norm(sub(b,a));u=norm(cross(axis,(0,0,1) if abs(axis[2])<.9 else (0,1,0)));q=cross(axis,u);ids=[]
 for p in [a,b]:
  ids.append([v(tuple(p[k]+thick*.5*(s*u[k]+t*q[k]) for k in range(3))) for s,t in [(-1,-1),(1,-1),(1,1),(-1,1)]])
 x,y=ids;face(x[3],x[2],x[1],x[0]);face(y[0],y[1],y[2],y[3])
 for i in range(4):j=(i+1)%4;face(x[i],x[j],y[j],y[i])
for sign in [-1,1]:
 rim(sign*.037)
 pts=[(sign*.069,-.1175,1.563),(sign*.077,-.074,1.563),(sign*.076,-.015,1.558),(sign*.071,.004,1.541)]
 for a,b in zip(pts,pts[1:]):bar(a,b,.006)
bar((-.007,-.141,1.5625),(.007,-.141,1.5625),.0055)
base=os.path.dirname(os.path.abspath(__file__))
path=base+'/GlassesGeometry.json'
previous=json.load(open(path)) if os.path.exists(path) else {}
data={'vertices':[dict(zip(('x','y','z'),v)) for v in verts],'triangles':tris}
if 'originalFrameVertices' in previous:data['originalFrameVertices']=previous['originalFrameVertices']
json.dump(data,open(path,'w'))
print(len(tris)//3,'triangles')
