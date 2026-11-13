using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// An authored, one-time scene pass. No per-frame decoration scripts or extra lights.
public static class FactoryConflictRemnantsPass
{
 const string Folder="Assets/Recoil3D/ConflictRemnants",Marker="1978 outdoor repaired breach and cleared barricades - editable";
 static Material steel,rust,concrete,brick,ink,whitewash,sand,sack;
 static Transform G(Transform p,string n,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.position=at;return t;}
 static Transform B(Transform p,string n,Vector3 at,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Cube){var t=GameObject.CreatePrimitive(type).transform;t.name=n;Object.DestroyImmediate(t.GetComponent<Collider>());t.SetParent(p,false);t.position=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
 static Transform Rod(Transform p,string n,Vector3 a,Vector3 b,float r,Material m){var t=B(p,n,(a+b)*.5f,new Vector3(r*2,(a-b).magnitude*.5f,r*2),m,PrimitiveType.Cylinder);t.rotation=Quaternion.FromToRotation(Vector3.up,b-a);return t;}
 static Material Mat(string n,Color c){string path=Folder+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.enableInstancing=true;m.SetFloat("_Glossiness",.08f);m.SetFloat("_Weathering",.85f);EditorUtility.SetDirty(m);return m;}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static float R(System.Random r,float a,float b){return a+(b-a)*(float)r.NextDouble();}

 static void Pipe(Transform p,Vector3 at,float yaw,int seed)
 {
  var root=G(p,"Crushed ventilation duct dragged off roadway",at);root.rotation=Quaternion.Euler(0,yaw,7);
  int rings=9,sides=18;var vs=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();
  // Outer and inner shells with a rolled lip; genuinely open, not a solid cylinder.
  for(int skin=0;skin<2;skin++)for(int j=0;j<rings;j++)for(int i=0;i<sides;i++){
   float a=i*2*Mathf.PI/sides,z=j*2.7f/(rings-1)-1.35f;
   float dent=1-.20f*Mathf.Exp(-Mathf.Pow((z-.2f)*2,2))*Mathf.Pow(Mathf.Max(0,Mathf.Cos(a-.5f)),4);
   float rr=(.59f-(skin==1?.035f:0))*dent;
   vs.Add(new Vector3(Mathf.Cos(a)*rr,Mathf.Sin(a)*rr*.76f,z));uv.Add(new Vector2(i/(float)sides,j/(float)rings));
  }
  for(int k=0;k<2;k++)for(int j=0;j<rings-1;j++)for(int i=0;i<sides;i++){int a=k*rings*sides+j*sides+i,b=k*rings*sides+j*sides+(i+1)%sides,c=a+sides,d=b+sides;if(k==0)tris.AddRange(new[]{a,c,b,b,c,d});else tris.AddRange(new[]{a,b,c,b,d,c});}
  foreach(int j in new[]{0,rings-1})for(int i=0;i<sides;i++){int a=j*sides+i,b=j*sides+(i+1)%sides,c=a+rings*sides,d=b+rings*sides;if(j==0)tris.AddRange(new[]{a,b,c,b,d,c});else tris.AddRange(new[]{a,c,b,b,c,d});}
  string path=Folder+"/Dented hollow duct "+seed+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}mesh.Clear();mesh.SetVertices(vs);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);root.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;root.gameObject.AddComponent<MeshRenderer>().sharedMaterial=rust;
  foreach(float z in new[]{-1.1f,.65f}){for(int i=0;i<18;i++){float a=i*Mathf.PI/9,b=(i+1)*Mathf.PI/9;Rod(root,"Old duct reinforcing hoop",root.TransformPoint(new Vector3(Mathf.Cos(a)*.61f,Mathf.Sin(a)*.47f,z)),root.TransformPoint(new Vector3(Mathf.Cos(b)*.61f,Mathf.Sin(b)*.47f,z)),.018f,steel);}}
 }
 static void Trolley(Transform p,Vector3 at,float yaw)
 {
  var root=G(p,"Overturned mine trolley - cleared occupation barricade",at);root.rotation=Quaternion.Euler(0,yaw,67);
  foreach(float x in new[]{-.67f,.67f})B(root,"Mine tub long steel wall",root.TransformPoint(new Vector3(x,.6f,0)),new Vector3(.08f,.85f,1.65f),rust).rotation=root.rotation;
  foreach(float z in new[]{-.79f,.79f})B(root,"Mine tub end plate",root.TransformPoint(new Vector3(0,.6f,z)),new Vector3(1.35f,.85f,.07f),rust).rotation=root.rotation;
  B(root,"Mine trolley floor",root.TransformPoint(new Vector3(0,.18f,0)),new Vector3(1.4f,.1f,1.7f),steel).rotation=root.rotation;
  foreach(float z in new[]{-.53f,.53f}){Rod(root,"Old trolley axle",root.TransformPoint(new Vector3(-.85f,0,z)),root.TransformPoint(new Vector3(.85f,0,z)),.05f,steel);foreach(float x in new[]{-.78f,.78f}){var t=B(root,"Flanged cast iron wheel",root.TransformPoint(new Vector3(x,0,z)),new Vector3(.42f,.07f,.42f),steel,PrimitiveType.Cylinder);t.rotation=root.rotation*Quaternion.Euler(0,0,90);}}
 }
 static void Repair(Transform p,float x,float z)
 {
  var g=G(p,"Old forced breach - brick and concrete infill",new Vector3(x,0,z));
  for(int row=0;row<9;row++){float width=2.05f+(row%3)*.13f;B(g,"Uneven later cement infill",new Vector3(x-.385f,.23f+row*.33f,z),new Vector3(.09f,.32f,width),concrete);}
  for(int row=0;row<10;row++)foreach(int side in new[]{-1,1})for(int j=0;j<2;j++){var t=B(g,"Exposed mismatched repair brick",new Vector3(x-.443f,.18f+row*.27f,z+side*(1.03f+j*.33f+(row%2)*.12f)),new Vector3(.10f,.24f,.30f),brick);t.rotation=Quaternion.Euler(row%3-1,0,row%2==0?2:-2);}
  foreach(float y in new[]{.8f,2.0f}){B(g,"Rusted reinforcement plate across old break",new Vector3(x-.51f,y,z-.6f),new Vector3(.055f,.12f,.95f),rust);foreach(float zz in new[]{-.95f,-.25f})B(g,"Rust stained anchor",new Vector3(x-.55f,y,z+zz),new Vector3(.04f,.08f,.08f),steel,PrimitiveType.Sphere);}
 }
 static void Slogan(Transform p,Vector3 at,string words,int seed)
 {
  var g=G(p,"Worker slogan partly covered after reopening",at);var tm=g.gameObject.AddComponent<TextMesh>();tm.text=words;tm.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");tm.fontSize=80;tm.characterSize=.07f;tm.anchor=TextAnchor.MiddleCenter;tm.color=new Color(.27f,.14f,.09f);tm.font.RequestCharactersInTexture(words,80,FontStyle.Normal);
  string matPath=Folder+"/Faded lit wall lettering.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!material){material=new Material(Shader.Find("Recoil/3D/Weathered Wall Lettering"));AssetDatabase.CreateAsset(material,matPath);}tm.color=Color.white;g.GetComponent<Renderer>().sharedMaterial=material;g.gameObject.AddComponent<Recoil3DDepthText>();
  var rng=new System.Random(seed);for(int i=0;i<8;i++){var t=B(g,"Uneven whitewash obscuring words",at+new Vector3(R(rng,-1.65f,1.65f),R(rng,-.27f,.27f),-.025f-i*.002f),new Vector3(R(rng,.35f,.85f),R(rng,.07f,.17f),.012f),whitewash);t.rotation=Quaternion.Euler(0,0,R(rng,-5,5));}
 }
 static int ScatterBags(Transform world,Transform added)
 {
  var bags=world.GetComponentsInChildren<Transform>(true).Where(t=>t.GetComponent<MeshRenderer>()&&(t.name=="Old sand-filled flood sack"||t.name=="Slumped old sandbag"||t.name=="Split collapsed old sandbag")).ToArray();
  var rng=new System.Random(19781028);int count=0;
  foreach(var b in bags){int sibling=b.GetSiblingIndex();var parent=b.parent;Transform tie=null;if(sibling+1<parent.childCount){var candidate=parent.GetChild(sibling+1);if(candidate.name=="Twisted sack tie"||candidate.name=="Loose sack tie")tie=candidate;}
   Vector3 pos=b.position;bool fallen=count%5==1||count%5==2||count%7==0;bool split=count%9==0;
   pos.x+=R(rng,-.23f,.23f)+(fallen?R(rng,-.55f,.55f):0);float sign=pos.z<0?-1:1;
   pos.z+=sign*(fallen?R(rng,.05f,1.0f):R(rng,-.16f,.16f));pos.z=sign*Mathf.Max(2.05f,Mathf.Abs(pos.z));
   var scale=b.localScale;scale.x*=split?.52f:R(rng,.78f,1.04f);scale.y*=R(rng,.85f,1.1f);b.localScale=scale;b.rotation=Quaternion.Euler(R(rng,-12,12),R(rng,-65,65),90+R(rng,-17,17));b.position=pos;
   var mr=b.GetComponent<Renderer>();if(fallen||split){pos.y-=mr.bounds.min.y-.018f;b.position=pos;}else if(mr.bounds.min.y<.01f){pos.y+=.01f-mr.bounds.min.y;b.position=pos;}
   if(tie){tie.position=b.position+b.up*(scale.y*.93f);tie.rotation=b.rotation;}
   if(split){var spill=B(added,"Sand leaked from torn old barricade sack",new Vector3(pos.x+.26f,.025f,pos.z+sign*.12f),new Vector3(R(rng,.42f,.85f),.04f,R(rng,.35f,.65f)),sand,PrimitiveType.Sphere);for(int i=0;i<2;i++){var strip=B(added,"Frayed empty sack fold",pos+new Vector3(R(rng,-.23f,.23f),-.03f,R(rng,-.12f,.12f)),new Vector3(.24f,.018f,.11f),sack);strip.rotation=Quaternion.Euler(0,R(rng,0,180),9);}}
   count++;
  }return count;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var original=SceneManager.GetActiveScene();const string path="Assets/Scenes/3D/RecoilPrototype.unity";var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  try{var world=All<RecoilPrototype>(scene).Single().transform;if(world.Find(Marker))return "Already applied";
   string backup="Backups/FactoryConflictRemnants/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(Path.GetDirectoryName(backup));EditorSceneManager.SaveScene(scene,backup,true);Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
   steel=Mat("Pitted dark iron",new Color(.15f,.17f,.16f));rust=Mat("Old duct rust and abandoned mine tub",new Color(.26f,.145f,.075f));concrete=Mat("Later mismatched concrete repair",new Color(.32f,.33f,.29f));brick=Mat("Exposed repair bricks",new Color(.31f,.21f,.14f));ink=Mat("Faded strike paint",new Color(.28f,.12f,.07f));whitewash=Mat("Uneven official whitewash",new Color(.28f,.30f,.26f));sand=Mat("Spilled weathered barricade sand",new Color(.28f,.25f,.17f));sack=Mat("Frayed sack cloth",new Color(.31f,.29f,.21f));
   var doors=All<RecoilDoor>(scene).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();var colliders=All<Collider2D>(scene).ToDictionary(c=>c.GetInstanceID(),c=>c.bounds);var root=G(world,Marker,Vector3.zero);
   Repair(root,15.71f,5.6f);var repair=root.Find("Old forced breach - brick and concrete infill");var inner=Object.Instantiate(repair.gameObject,root).transform;inner.name="Old repaired breach - inner face";inner.localScale=new Vector3(-1,1,1);Pipe(root,new Vector3(23.5f,.49f,4.0f),-58,1);Pipe(root,new Vector3(28.3f,.49f,4.7f),-12,2);Trolley(root,new Vector3(33.5f,.72f,5.1f),-23);
   Pipe(root,new Vector3(102.1f,.49f,-4.8f),62,3);Trolley(root,new Vector3(105.5f,.72f,-4.7f),12);
   Slogan(root,new Vector3(52.6f,1.92f,2.70f),"NO MORE CUTS",1);Slogan(root,new Vector3(71.4f,1.70f,2.70f),"WE STAY HERE",2);
   // Old skids/scrapes remain beside the cleared route, not on the player's movement axis.
   for(int bank=0;bank<2;bank++)for(int i=0;i<5;i++){var t=B(root,"Old barricade skid and rust stain",new Vector3(99+i*.75f,.012f,bank==0?-2.65f:3.25f),new Vector3(1.1f,.008f,.04f),rust);t.rotation=Quaternion.Euler(0,13+i*4,0);}
   int bags=ScatterBags(world,root);
   var gate=All<Transform>(scene).Single(t=>t.name=="Continuous iron gate - locked across yard depth");var wall=All<Transform>(scene).Single(t=>t.name=="Factory right perimeter wall");float dx=gate.position.x-wall.position.x;
   var gateColliders=wall.GetComponentsInChildren<Collider2D>(true);wall.position+=Vector3.right*dx;wall.SetParent(gate,true);foreach(var box in gateColliders.OfType<BoxCollider2D>()){var size=box.size;size.x=(box.name=="Wall cap"?.22f:.20f)/Mathf.Abs(box.transform.lossyScale.x);box.size=size;}
   // This legacy concrete wall supplied the gate collision; its art must not render a second barrier.
   foreach(var visual in wall.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){visual.suppressed=true;visual.Sync();EditorUtility.SetDirty(visual);}foreach(var renderer in wall.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
   Physics2D.SyncTransforms();foreach(var c in gateColliders)if(Mathf.Abs(c.bounds.center.x-gate.position.x)>.01f)throw new Exception("Gate collision alignment failed");
   var changed=new HashSet<int>(gateColliders.Select(c=>c.GetInstanceID()));foreach(var c in All<Collider2D>(scene))if(!changed.Contains(c.GetInstanceID())&&colliders[c.GetInstanceID()]!=c.bounds)throw new Exception("Unrelated collision changed");
   if(root.GetComponentsInChildren<Collider>(true).Length+root.GetComponentsInChildren<Collider2D>(true).Length!=0)throw new Exception("Decoration added blockers");
   if(!doors.SequenceEqual(All<RecoilDoor>(scene).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position)))throw new Exception("Door routes changed");
   foreach(var b in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Old sand-filled flood sack"||t.name=="Slumped old sandbag"||t.name=="Split collapsed old sandbag")){var r=b.GetComponent<Renderer>();if(r&&r.bounds.min.z<1.3f&&r.bounds.max.z> -1.3f)throw new Exception("Sandbag enters walking corridor");}
   EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");Directory.CreateDirectory("output/factory-conflict-remnants");string report="PASS: "+bags+" sacks irregularly settled with local collapses and torn fabric; no added collision or route changes.\nMain gate collision moved "+dx+"m, aligned at X="+gate.position.x+" and parented to visible gate.\nOne repaired breach, 3 hollow damaged ducts, 2 overturned mine trolleys, 2 whitewashed slogans.\nBackup: "+backup;File.WriteAllText("output/factory-conflict-remnants/verification.txt",report);return report;
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();}
 }
}
