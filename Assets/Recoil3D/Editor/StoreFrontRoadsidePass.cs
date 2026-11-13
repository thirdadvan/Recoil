using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

public static class StoreFrontRoadsidePass
{
 const string Folder="Assets/Recoil3D/StoreFrontRoadside",Marker="Store-front roadside - bus stop bicycles and drainage";
 static Material steel,rust,timber,concrete,asphalt,rubber,grass,paper;
 static int serial;
 static Transform Group(Transform parent,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=at;return t;}
 static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material)
 {
  var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;t.SetParent(parent,false);t.localPosition=at;t.localScale=size;Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=material;return t;
 }
 static void Rod(Transform parent,string name,Vector3 a,Vector3 b,float radius,Material material)
 {
  var t=GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;t.name=name;t.SetParent(parent,false);t.localPosition=(a+b)*.5f;t.localScale=new Vector3(radius*2,(a-b).magnitude*.5f,radius*2);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);Object.DestroyImmediate(t.GetComponent<Collider>());t.GetComponent<Renderer>().sharedMaterial=material;
 }
 static Material Mat(string name,Color colour)
 {
  string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=colour;m.enableInstancing=true;
  if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);if(m.HasProperty("_Weathering"))m.SetFloat("_Weathering",.75f);EditorUtility.SetDirty(m);return m;
 }
 static Mesh Save(string name,Mesh mesh){mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Meshes/"+(serial++).ToString("000")+" "+name+".asset"));return mesh;}
 static void Batch(Transform root)
 {
  var fs=root.GetComponentsInChildren<MeshFilter>().ToArray();
  foreach(var group in fs.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
  {
   var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
   var part=Group(root,"Combined detail - "+group.Key.name,root.position);part.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(root.name+" "+group.Key.name,mesh);part.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
  }
  foreach(var f in fs)Object.DestroyImmediate(f.gameObject);
 }
 static void Shelter(Transform parent)
 {
  var g=Group(parent,"Abandoned worker bus stop - open frame",new Vector3(-5,0,-9.3f));
  Box(g,"Old shallow boarding pad",new Vector3(0,.045f,0),new Vector3(5.7f,.09f,2.25f),concrete);
  foreach(float x in new[]{-2.45f,2.45f})foreach(float z in new[]{-.72f,.68f})
  {Rod(g,"Thin weathered shelter post",new Vector3(x,.09f,z),new Vector3(x,3.0f,z),.035f,steel);Box(g,"Post base plate",new Vector3(x,.12f,z),new Vector3(.16f,.055f,.16f),rust);}
  for(int i=0;i<19;i++)
  {
   var roof=Box(g,"Weathered corrugated shelter roof",new Vector3(-2.58f+i*.285f,3.04f,0),new Vector3(.27f,.045f,1.9f),i%6==1?rust:steel);roof.localRotation=Quaternion.Euler(-4,0,(i%3-1)*.4f);
   if(i%4==0)Rod(g,"Roof corrugation raised fold",new Vector3(-2.58f+i*.285f,3.06f,-.94f),new Vector3(-2.58f+i*.285f,3.06f,.94f),.012f,steel);
  }
  Rod(g,"Rear upper cross rail",new Vector3(-2.45f,2.90f,-.72f),new Vector3(2.45f,2.90f,-.72f),.025f,steel);
  // Open rear preserves the player silhouette when viewed from the foreground.
  foreach(float x in new[]{-1.32f,1.32f})
  {Rod(g,"Bench leg",new Vector3(x,.10f,-.25f),new Vector3(x,.49f,-.25f),.026f,steel);Rod(g,"Bench leg",new Vector3(x,.10f,.14f),new Vector3(x,.49f,.14f),.026f,steel);}
  for(int i=0;i<3;i++)Box(g,"Old uneven bench plank",new Vector3(0,.51f,-.28f+i*.16f),new Vector3(3.5f-i*.09f,.06f,.14f),timber).localRotation=Quaternion.Euler(0,i*.6f,0);
  for(int i=0;i<2;i++)Box(g,"Bench back board",new Vector3(.08f,.77f+i*.19f,-.38f),new Vector3(3.45f,.12f,.05f),timber);
  Rod(g,"Old bus stop sign post",new Vector3(3,.06f,.72f),new Vector3(3,2.7f,.72f),.032f,steel);
  Box(g,"Faded blank bus-stop plate",new Vector3(3,2.42f,.72f),new Vector3(.43f,.54f,.045f),concrete);
  Box(g,"Empty timetable case",new Vector3(3,1.45f,.72f),new Vector3(.30f,.47f,.05f),steel);
  foreach(int i in new[]{0,1,2})Box(g,"Weathered paper remnants without text",new Vector3(2.98f+(i%2)*.035f,1.36f+i*.10f,.751f),new Vector3(.19f-i*.025f,.075f,.008f),paper).localRotation=Quaternion.Euler(0,0,3-i*7);
  Box(g,"Loose broken bench board",new Vector3(-1.4f,.13f,.60f),new Vector3(1.03f,.055f,.12f),timber).localRotation=Quaternion.Euler(0,23,3);
  Batch(g);
 }
 static void Wheel(Transform parent,Vector3 centre,float radius,bool bent)
 {
  var v=new List<Vector3>();var triangles=new List<int>();const int around=24,tube=6;
  for(int i=0;i<around;i++)for(int j=0;j<tube;j++)
  {
   float a=i*Mathf.PI*2/around,b=j*Mathf.PI*2/tube,r=radius+.022f*Mathf.Cos(b);
   v.Add(centre+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,.022f*Mathf.Sin(b)+(bent?.025f*Mathf.Sin(a*3):0)));
   int n=i*tube+j,ni=((i+1)%around)*tube+j,nj=i*tube+(j+1)%tube,nij=((i+1)%around)*tube+(j+1)%tube;triangles.AddRange(new[]{n,ni,nij,n,nij,nj});
  }
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
  var t=Group(parent,"Perished bicycle tyre",parent.position);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=rubber;
  for(int i=0;i<10;i++){float a=i*Mathf.PI*.2f;Rod(parent,"Thin wheel spoke",centre,centre+new Vector3(Mathf.Cos(a)*radius*.93f,Mathf.Sin(a)*radius*.93f,0),.004f,steel);}
  Rod(parent,"Axle",centre+Vector3.back*.055f,centre+Vector3.forward*.055f,.025f,rust);
 }
 static void Bicycle(Transform parent,int index,Vector3 at,float yaw,float lean)
 {
  var g=Group(parent,"Abandoned bicycle "+(index+1)+" - independently movable",at);
  var back=new Vector3(-.66f,.34f,0);var front=new Vector3(.68f,.34f,0);Wheel(g,back,.32f,index==2);Wheel(g,front,.32f,index==2);
  var crank=new Vector3(-.10f,.34f,0);var seat=new Vector3(-.28f,.88f,0);var neck=new Vector3(.38f,.87f,0);
  foreach(var pair in new[]{new[]{back,seat},new[]{seat,crank},new[]{crank,back},new[]{seat,neck},new[]{neck,crank},new[]{neck,front}})Rod(g,"Rusting bicycle frame",pair[0],pair[1],.019f,index%2==0?rust:steel);
  Rod(g,"Saddle stem",seat,seat+Vector3.up*.10f,.015f,steel);Box(g,"Worn saddle",seat+Vector3.up*.13f,new Vector3(.24f,.055f,.15f),rubber);
  Rod(g,"Handlebar stem",neck,neck+new Vector3(.01f,.18f,0),.014f,steel);Rod(g,"Handlebars",neck+new Vector3(.01f,.18f,-.24f),neck+new Vector3(.01f,.18f,.24f),.014f,steel);
  Rod(g,"Pedal crank",crank+new Vector3(-.11f,.07f,-.06f),crank+new Vector3(.11f,-.07f,.06f),.014f,steel);
  foreach(float side in new[]{-1f,1f})Box(g,"Old pedal",crank+new Vector3(side*.11f,-side*.07f,side*.11f),new Vector3(.10f,.04f,.15f),rubber);
  if(index!=2){Rod(g,"Rear luggage rack",new Vector3(-.81f,.72f,0),new Vector3(-.40f,.72f,0),.013f,steel);Rod(g,"Rear rack support",back,new Vector3(-.77f,.72f,0),.009f,steel);}
  Batch(g);g.rotation=Quaternion.Euler(lean,yaw,0);
  float bottom=g.GetComponentsInChildren<Renderer>().Min(r=>r.bounds.min.y);g.position+=Vector3.up*(.10f-bottom);
 }
 static void Bicycles(Transform parent)
 {
  var group=Group(parent,"Worker bicycle rack and three abandoned bicycles",new Vector3(19,0,-9.6f));
  var rack=Group(group,"Bent steel bicycle rack",group.position);
  foreach(float z in new[]{-.55f,.42f})Rod(rack,"Rack ground rail",new Vector3(-2.1f,.13f,z),new Vector3(2.1f,.13f,z),.025f,steel);
  for(int i=0;i<7;i++)
  {
   float x=-1.9f+i*.62f;Rod(rack,"Wheel cradle",new Vector3(x,.12f,-.55f),new Vector3(x,.60f,-.38f),.018f,steel);
   Rod(rack,"Wheel cradle",new Vector3(x,.60f,-.38f),new Vector3(x,.60f,.13f),.018f,steel);Rod(rack,"Wheel cradle",new Vector3(x,.60f,.13f),new Vector3(x,.12f,.42f),.018f,steel);
  }
  Batch(rack);Bicycle(group,0,new Vector3(17.4f,0,-9.4f),55,-8);Bicycle(group,1,new Vector3(19.1f,0,-9.3f),66,11);Bicycle(group,2,new Vector3(21,0,-10.1f),31,77);
 }
 static void Drainage(Transform parent)
 {
  var g=Group(parent,"Roadside drainage - broken concrete channel and covers",Vector3.zero);var v=new List<Vector3>();var triangles=new List<int>();
  foreach(var interval in new[]{new Vector2(-25,-10),new Vector2(-8,6),new Vector2(8,23),new Vector2(25,38)})
  {
   float[] zs={-8.02f,-7.94f,-7.78f,-7.45f,-7.29f,-7.21f};float[] ys={.14f,.14f,.035f,.035f,.14f,.14f};int offset=v.Count;
   foreach(float x in new[]{interval.x,interval.y})for(int k=0;k<zs.Length;k++)v.Add(new Vector3(x,ys[k],zs[k]));
   for(int k=0;k<zs.Length-1;k++){int a=offset+k,b=a+zs.Length;triangles.AddRange(new[]{a,a+1,b+1,a,b+1,b});}
   for(float x=interval.x+.5f;x<interval.y;x+=2.4f)
   {
    if(((int)x)%3!=0)Box(g,"Cracked displaced drainage cover",new Vector3(x,.16f,-7.62f),new Vector3(.82f,.065f,.72f),concrete).localRotation=Quaternion.Euler(0,((int)x%5)*2,((int)x%3)*1.5f);
    Box(g,"Dark silt and standing water",new Vector3(x,.040f,-7.62f),new Vector3(1.1f,.009f,.26f),asphalt);
    for(int k=0;k<3;k++)Rod(g,"Weeds in channel joint",new Vector3(x+k*.07f,.14f,-8.04f),new Vector3(x+k*.09f,.26f+k*.04f,-8.10f),.009f,grass);
   }
  }
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var channel=Group(g,"Concrete U-section drainage channel",Vector3.zero);channel.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;channel.gameObject.AddComponent<MeshRenderer>().sharedMaterial=concrete;Batch(g);
 }
 static void Segment(Transform t,float near,float far)
 {var p=t.position;p.z=(near+far)*.5f;t.position=p;var scale=t.localScale;scale.z=(far-near)/Mathf.Abs(t.parent.lossyScale.z);t.localScale=scale;}
 static void AlignGate(Transform root,float z)
 {
  var piers=root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Masonry rear gate pier").ToArray();float old=piers.Average(t=>t.position.z),delta=z-old;
  var parts=root.Cast<Transform>().Where(t=>t.name!="Perimeter continues behind camera"&&t.name!="Foreground cutaway wall footing"&&t.name!="Foreground perimeter upper wall - shadow only"&&t.name!="Continuous wall beside rear gate"&&t.name!="Weathered wall coping"&&!t.name.StartsWith("Weathered barricade remnant")).ToArray();
  var assembly=Group(root,"Rear gate - aligned with store-front road",new Vector3(piers[0].position.x,0,old));foreach(var part in parts)part.SetParent(assembly,true);assembly.position+=Vector3.forward*delta;
  Segment(root.Find("Foreground cutaway wall footing"),-27,z-3.2f);Segment(root.Find("Foreground perimeter upper wall - shadow only"),-27,z-3.2f);
  float far=root.Find("Continuous wall beside rear gate").GetComponent<Renderer>().bounds.max.z;Segment(root.Find("Continuous wall beside rear gate"),z+3.2f,far);Segment(root.Find("Weathered wall coping"),z+3.2f,far);
  foreach(var cluster in root.Cast<Transform>().Where(t=>t.name.StartsWith("Weathered barricade remnant")))
  {var p=cluster.position;p.z=p.z<old?p.z+delta:Mathf.Max(2.3f,p.z+delta);cluster.position=p;}
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var all=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Single(t=>t.GetComponent<RecoilPrototype>());
  if(world.Find(Marker))return "Roadside set already exists; placements retained";
  var before=all.SelectMany(t=>t.GetComponents<Collider2D>()).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  Directory.CreateDirectory("Backups/StoreFrontRoadside");var backup="Backups/StoreFrontRoadside/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);
  Directory.CreateDirectory(Folder+"/Meshes");AssetDatabase.Refresh();serial=0;
  steel=Mat("Faded roadside green grey steel",new Color(.19f,.23f,.21f));rust=Mat("Old roadside rust",new Color(.22f,.12f,.065f));timber=Mat("Weathered bench timber",new Color(.26f,.22f,.16f));concrete=Mat("Worn roadside concrete",new Color(.27f,.28f,.25f));asphalt=Mat("Drain dark silt and water",new Color(.035f,.052f,.048f));rubber=Mat("Old bicycle rubber and saddles",new Color(.035f,.04f,.035f));grass=Mat("Dry road shoulder weeds",new Color(.12f,.15f,.075f));paper=Mat("Faded timetable paper",new Color(.32f,.32f,.25f));
  AlignGate(all.Single(t=>t.name=="Factory rear gate and old sandbags - editable"),-3.8f);
  string roadReport=FactoryForecourtRoadPass.Apply();
  var root=Group(world,Marker,Vector3.zero);Shelter(root);Bicycles(root);Drainage(root);
  if(root.GetComponentsInChildren<Collider>(true).Length+root.GetComponentsInChildren<Collider2D>(true).Length!=0)throw new Exception("Unintended new blockers");
  if(root.GetComponentsInChildren<Renderer>().Any(r=>r.bounds.max.z> -7))throw new Exception("Set dressing intrudes into road/player corridor");
  var after=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Collider2D>(true)).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();if(!before.SequenceEqual(after))throw new Exception("Gameplay collision changed");
  AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  string report="PASS rear gate centred at Z=-3.8 with repaired adjoining wall segments; road straight past store and in front of tower, bends only near petrol forecourt. Open-frame bus shelter, bench, blank stop plate, bicycle rack with 3 individually movable old bicycles, segmented U-section drainage with displaced covers/weeds. No new lights/collision; all existing gameplay collision retained. Backup: "+backup+"\n"+roadReport;
  Directory.CreateDirectory("output/store-front-roadside");File.WriteAllText("output/store-front-roadside/verification.txt",report);return report;
 }
}
