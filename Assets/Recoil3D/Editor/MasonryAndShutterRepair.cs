using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class MasonryAndShutterRepair
{
 const string ScenePath="Assets/Scenes/3D/RecoilPrototype.unity",Folder="Assets/Recoil3D/MasonryAndShutter";
 const string Marker="Warehouse closed rolling slat door - masonry repair";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(Transform parent,string name,Vector3 local){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
 static Transform B(Transform p,string name,Vector3 at,Vector3 size,Material m,PrimitiveType type=PrimitiveType.Cube){var t=GameObject.CreatePrimitive(type).transform;t.name=name;Object.DestroyImmediate(t.GetComponent<Collider>());t.SetParent(p,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
 static void Beam(Transform p,string name,Vector3 a,Vector3 b,float width,Material mat){var t=B(p,name,(a+b)*.5f,new Vector3(width,width,(b-a).magnitude),mat);t.localRotation=Quaternion.LookRotation(b-a);}
 static Material Surface(string name,Color tint,int kind,float metallic){var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Metric Architectural Surface"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_Color",Color.white);m.SetColor("_BaseTint",tint);m.SetFloat("_Kind",kind);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.16f);EditorUtility.SetDirty(m);return m;}
 static Mesh Save(Mesh mesh,string name){mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Models/"+name+".asset"));return mesh;}
 static void Suppress(Transform t){if(t.GetComponentsInChildren<Collider2D>(true).Length+t.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Decorative object unexpectedly has collision: "+t.name);foreach(var v in t.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}foreach(var r in t.GetComponentsInChildren<Renderer>(true)){r.enabled=false;EditorUtility.SetDirty(r);}t.gameObject.SetActive(false);}
 static void Curtain(Transform p,Material material)
 {
  var v=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();const int rows=62;float bottom=.12f,top=4.78f,pitch=(top-bottom)/rows;material.SetFloat("_Pitch",pitch);material.SetFloat("_Bottom",bottom);EditorUtility.SetDirty(material);
  float[] fy={0,.08f,.17f,.82f,.94f,1};float[] fz={-.105f,-.108f,-.133f,-.133f,-.108f,-.105f};
  for(int row=0;row<rows;row++)for(int k=0;k<fy.Length-1;k++){int n=v.Count;float y0=bottom+(row+fy[k])*pitch,y1=bottom+(row+fy[k+1])*pitch;v.Add(new Vector3(-2.50f,y0,fz[k]));v.Add(new Vector3(2.50f,y0,fz[k]));v.Add(new Vector3(-2.50f,y1,fz[k+1]));v.Add(new Vector3(2.50f,y1,fz[k+1]));uv.Add(new Vector2(0,y0));uv.Add(new Vector2(5,y0));uv.Add(new Vector2(0,y1));uv.Add(new Vector2(5,y1));tris.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var t=G(p,"62 continuous interlocking steel slats - no vertical panel seam",Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Continuous profiled rolling slat curtain");t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
 }
 static void Hood(Transform p,Material material)
 {
  var profile=new[]{new Vector2(-.30f,-.015f),new Vector2(-.30f,-.48f),new Vector2(-.18f,-.63f),new Vector2(.19f,-.63f),new Vector2(.30f,-.48f),new Vector2(.30f,-.015f)};var v=new List<Vector3>();var tr=new List<int>();
  for(int i=0;i<profile.Length;i++){int j=(i+1)%profile.Length,k=v.Count;v.Add(new Vector3(-2.72f,5.04f+profile[i].x,profile[i].y));v.Add(new Vector3(2.72f,5.04f+profile[i].x,profile[i].y));v.Add(new Vector3(-2.72f,5.04f+profile[j].x,profile[j].y));v.Add(new Vector3(2.72f,5.04f+profile[j].x,profile[j].y));tr.AddRange(new[]{k,k+2,k+1,k+1,k+2,k+3});}
  for(int side=0;side<2;side++){int k=v.Count;for(int i=0;i<profile.Length;i++)v.Add(new Vector3(side==0?-2.72f:2.72f,5.04f+profile[i].x,profile[i].y));for(int j=1;j<profile.Length-1;j++)tr.AddRange(side==0?new[]{k,k+j+1,k+j}:new[]{k,k+j,k+j+1});}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var t=G(p,"Folded sheet metal coil hood with angled shoulders",Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Folded rolling shutter hood");t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
 }
 static void Build(Transform p)
 {
  var steel=Surface("Old shutter guides and bottom angle",new Color(.20f,.235f,.21f),3,.46f);var hood=Surface("Chipped zinc grey coil housing",new Color(.31f,.34f,.30f),3,.38f);var dark=Surface("Deep shutter track and rubber seal",new Color(.035f,.043f,.037f),0,0);var rust=Surface("Rusty fasteners and lock",new Color(.20f,.10f,.045f),3,.3f);
  var curtain=new Material(Shader.Find("Recoil/3D/Aged Rolling Shutter"));curtain.name="Worn galvanised interlocking shutter slats";AssetDatabase.CreateAsset(curtain,Folder+"/Worn galvanised interlocking shutter slats.mat");
  B(p,"Dark opening reveal behind closed slats",new Vector3(0,2.43f,-.033f),new Vector3(5.18f,4.86f,.065f),dark);Curtain(p,curtain);Hood(p,hood);
  foreach(float side in new[]{-1f,1f}){
   B(p,"Channel guide outer leg",new Vector3(side*2.56f,2.42f,-.15f),new Vector3(.14f,4.84f,.25f),steel);B(p,"Guide slot visible dark channel",new Vector3(side*2.484f,2.43f,-.18f),new Vector3(.025f,4.74f,.035f),dark);
   B(p,"Guide flared bellmouth below coil",new Vector3(side*2.56f,4.77f,-.14f),new Vector3(.19f,.18f,.28f),steel);
   foreach(float y in new[]{.3f,1.25f,2.2f,3.15f,4.1f}){B(p,"Guide wall fixing lug",new Vector3(side*2.66f,y,-.055f),new Vector3(.16f,.065f,.10f),steel);var bolt=B(p,"Old wall anchor bolt head",new Vector3(side*2.69f,y,-.115f),new Vector3(.035f,.015f,.035f),rust,PrimitiveType.Cylinder);bolt.localRotation=Quaternion.Euler(90,0,0);}
   var cap=B(p,"Coil hood removable end plate",new Vector3(side*2.735f,5.04f,-.28f),new Vector3(.025f,.52f,.55f),steel);
  }
  B(p,"Continuous bottom angle bar",new Vector3(0,.115f,-.145f),new Vector3(5.03f,.125f,.11f),steel);B(p,"Compressed rubber threshold seal",new Vector3(0,.043f,-.115f),new Vector3(5.0f,.027f,.085f),dark);
  foreach(float x in new[]{-1.12f,1.12f}){foreach(float dx in new[]{-.13f,.13f})Beam(p,"Handle stand-off",new Vector3(x+dx,.43f,-.14f),new Vector3(x+dx,.43f,-.235f),.024f,steel);Beam(p,"Worn bottom lift grip",new Vector3(x-.13f,.43f,-.235f),new Vector3(x+.13f,.43f,-.235f),.029f,steel);}
  B(p,"Bottom central slide lock",new Vector3(0,.22f,-.21f),new Vector3(.24f,.065f,.035f),steel);B(p,"Old padlock body",new Vector3(.05f,.17f,-.24f),new Vector3(.065f,.075f,.035f),rust);
  B(p,"Manual chain gear case beside right guide",new Vector3(2.90f,4.58f,-.16f),new Vector3(.26f,.40f,.28f),steel);var wheel=B(p,"Chain hoist sprocket cover",new Vector3(2.9f,4.48f,-.33f),new Vector3(.22f,.04f,.22f),steel,PrimitiveType.Cylinder);wheel.localRotation=Quaternion.Euler(90,0,0);
  var points=new List<Vector3>();for(int i=0;i<=12;i++){float angle=Mathf.PI*i/12;points.Add(new Vector3(2.9f+.075f*Mathf.Cos(angle),4.45f+.075f*Mathf.Sin(angle),-.36f));}points.Add(new Vector3(2.825f,.88f,-.36f));for(int i=1;i<=12;i++){float angle=Mathf.PI+Mathf.PI*i/12;points.Add(new Vector3(2.9f+.075f*Mathf.Cos(angle),.88f+.075f*Mathf.Sin(angle),-.36f));}points.Add(points[0]);for(int i=1;i<points.Count;i++)Beam(p,"Hanging manual hand chain",points[i-1],points[i],.009f,steel);
  Batch(p);
 }
 static void Batch(Transform root){var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();foreach(var mat in rs.Select(r=>r.sharedMaterial).Distinct()){var members=rs.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(members.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);var t=G(root,"Combined rolling door detail - "+mat.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=Save(mesh,"Combined "+mat.name);t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;foreach(var r in members)r.enabled=false;}}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play before repairing saved scenery");Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory("Backups/MasonryAndShutter");Directory.CreateDirectory("output/masonry-and-shutter");var prev=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  try{var all=All<Transform>(scene);if(all.Any(t=>t.name==Marker))return "Already repaired";var backup="Backups/MasonryAndShutter/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(scene,backup,true);
   var collision=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy).ToArray();var doors=All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();var spawns=all.Where(t=>t.name.Contains("Spawn")).Select(t=>t.GetInstanceID()+"|"+t.position).ToArray();
   var factory=all.Single(t=>t.name=="Factory Building - four storeys");var warehouse=all.Single(t=>t.name=="Factory Warehouse - exterior");int factoryCount=0,warehouseCount=0;
   foreach(var t in all.Where(t=>t.IsChildOf(factory)&&(t.name=="Storey belt course"||t.name=="Facade concrete pier"))){Suppress(t);factoryCount++;}
   foreach(var t in all.Where(t=>t.IsChildOf(warehouse)&&(t.name=="Facade steel rib"||t.name=="Side wall vertical pier"))){Suppress(t);warehouseCount++;}
   var old=all.Single(t=>t.name=="Warehouse rolling shutter - mounted to front wall");float x=all.Single(t=>t.name=="Loading shutter").position.x;Suppress(old);var wall=warehouse.GetComponentsInChildren<MeshRenderer>(true).Single(r=>r.enabled&&r.sharedMaterial&&r.sharedMaterial.name=="Warehouse aligned brick and worn plaster"&&r.bounds.size.x>20&&r.bounds.size.z<1&&r.bounds.center.z<5).bounds.min.z;
   var root=G(warehouse,Marker,Vector3.zero);root.position=new Vector3(x,0,wall);Build(root);
   if(!collision.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy)))throw new Exception("Collision changed");if(!doors.SequenceEqual(All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Door routes changed");if(!spawns.SequenceEqual(All<Transform>(scene).Where(t=>t.name.Contains("Spawn")).Select(t=>t.GetInstanceID()+"|"+t.position)))throw new Exception("Spawn changed");if(root.GetComponentsInChildren<Collider>(true).Length+root.GetComponentsInChildren<Collider2D>(true).Length!=0)throw new Exception("Unexpected new collision");
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();string result="PASS removed "+factoryCount+" mismatched factory concrete grid strips and "+warehouseCount+" warehouse facade/side ribs. Existing brick material and moss decals retained; base moss now unobstructed. Replaced flat plate and 13 sparse bars with 62 continuous profiled horizontal slats, guide channels and anchors, folded coil hood, bottom bar/seal/grips/lock and manual hand chain. Opening remains 5 x 4.8m at X="+x+", front Z="+wall+". All Collider2D, original doors and spawns unchanged. Backup="+backup;File.WriteAllText("output/masonry-and-shutter/verification.txt",result);return result;
  }finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prev);var w=All<Recoil3DWorld>(prev).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(prev).FirstOrDefault();if(look)look.ApplyLook();}
 }
 public static string Capture()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; capture deferred";var prev=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);var others=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).ToArray();var rs=others.SelectMany(s=>All<Renderer>(s)).Where(r=>r.enabled).ToArray();var ls=others.SelectMany(s=>All<Light>(s)).Where(l=>l.enabled).ToArray();GameObject cg=null,lg=null;
  try{foreach(var r in rs)r.enabled=false;foreach(var l in ls)l.enabled=false;SceneManager.SetActiveScene(scene);All<Recoil3DWorld>(scene).First().ApplyAtmosphere();cg=new GameObject("Temporary masonry repair inspection camera");var camera=cg.AddComponent<Camera>();camera.fieldOfView=45;camera.nearClipPlane=.1f;camera.farClipPlane=200;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.035f);var shot=typeof(FactoryRoofStairHeadPass).GetMethod("Shot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   camera.transform.position=new Vector3(101,3.2f,-8);camera.transform.LookAt(new Vector3(100.5f,.6f,2.9f));shot.Invoke(null,new object[]{camera,"FactoryMasonryAndMossRepaired"});camera.transform.position=new Vector3(128.3f,3.7f,-9);camera.transform.LookAt(new Vector3(128.3f,2.5f,2.8f));shot.Invoke(null,new object[]{camera,"WarehouseRollingShutterNight"});
   lg=new GameObject("Temporary rolling shutter inspection fill");var li=lg.AddComponent<Light>();li.type=LightType.Directional;li.color=new Color(1,.92f,.82f);li.intensity=.9f;lg.transform.rotation=Quaternion.Euler(42,-25,0);shot.Invoke(null,new object[]{camera,"WarehouseRollingShutterInspection"});camera.fieldOfView=48;camera.transform.position=new Vector3(112,10,-15);camera.transform.LookAt(new Vector3(124,3.0f,8));shot.Invoke(null,new object[]{camera,"WarehouseFacadeWithoutGrid"});return "Four actual scene screenshots captured";
  }finally{if(cg)Object.DestroyImmediate(cg);if(lg)Object.DestroyImmediate(lg);foreach(var r in rs)if(r)r.enabled=true;foreach(var l in ls)if(l)l.enabled=true;if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prev);var w=All<Recoil3DWorld>(prev).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(prev).FirstOrDefault();if(look)look.ApplyLook();}
 }
}
