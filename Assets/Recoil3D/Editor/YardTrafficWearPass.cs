using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class YardTrafficWearPass
{
 const string ScenePath="Assets/Scenes/3D/RecoilPrototype.unity",Folder="Assets/Recoil3D/YardTrafficWear",Marker="Factory and warehouse - aged tyre arcs and dragging scuffs";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static Transform G(Transform p,string name){var t=new GameObject(name).transform;t.SetParent(p,false);return t;}
 static Material Mat(string name,Color tint,int type){var m=new Material(Shader.Find("Recoil/3D/Aged Yard Traffic Marks"));m.name=name;m.SetColor("_Tint",tint);m.SetFloat("_Type",type);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
 static Vector3 P(float x,float z){return new Vector3(x,.026f,z);}
 static Vector3 Bezier(Vector3 a,Vector3 b,Vector3 c,Vector3 d,float t){float u=1-t;return a*(u*u*u)+b*(3*u*u*t)+c*(3*u*t*t)+d*(t*t*t);}
 static void Track(Transform root,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float offset,float width,float strength,Material mat)
 {
  const int segments=64;var v=new List<Vector3>();var uv=new List<Vector2>();var colours=new List<Color>();var tr=new List<int>();float distance=0;Vector3 last=a;
  for(int i=0;i<=segments;i++){float t=i/(float)segments;var at=Bezier(a,b,c,d,t);var tangent=(Bezier(a,b,c,d,Mathf.Min(1,t+.002f))-Bezier(a,b,c,d,Mathf.Max(0,t-.002f))).normalized;var cross=new Vector3(-tangent.z,0,tangent.x);var centre=at+cross*offset;if(i>0)distance+=Vector3.Distance(last,centre);last=centre;float fade=Mathf.SmoothStep(0,1,t/.11f)*(1-Mathf.SmoothStep(0,1,(t-.84f)/.16f));float variation=.76f+.24f*Mathf.Sin(t*15+offset*2);
   v.Add(centre-cross*width*.5f);v.Add(centre+cross*width*.5f);uv.Add(new Vector2(0,distance));uv.Add(new Vector2(1,distance));var colour=new Color(1,1,1,fade*strength*variation);colours.Add(colour);colours.Add(colour);
   if(i>0){int k=(i-1)*2;bool up=Vector3.Cross(v[k+1]-v[k],v[k+2]-v[k]).y>0;tr.AddRange(up?new[]{k,k+1,k+2,k+1,k+3,k+2}:new[]{k,k+2,k+1,k+1,k+2,k+3});}}
  var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(colours);mesh.SetTriangles(tr,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Models/"+name+".asset"));var tform=G(root,name);tform.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=tform.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
 }
 static void Pair(Transform root,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d,float gauge,float width,float strength,Material mat){foreach(float side in new[]{-1f,1f})Track(root,name+(side<0?" - left wheel":" - right wheel"),a,b,c,d,side*gauge*.5f,width,strength,mat);}
 static void Build(Transform root)
 {
  var tyre=Mat("Old faded rubber wheel tracks",new Color(.025f,.026f,.022f,.56f),0);var brake=Mat("Distressed continuous rubber scuffs",new Color(.03f,.029f,.024f,.40f),2);var drag=Mat("Pallet dragging and scraped grime",new Color(.075f,.062f,.043f,.44f),1);
  Pair(root,"Truck turning from road toward warehouse shutter",P(120,-7.6f),P(124,-7.4f),P(128.2f,-3.0f),P(128.3f,2.28f),1.82f,.28f,.94f,tyre);
  Pair(root,"Older shallow reversing trace beside loading turn",P(131.5f,-6.8f),P(132.2f,-3.9f),P(129.8f,-1.5f),P(129.3f,1.9f),1.78f,.23f,.48f,tyre);
  Pair(root,"Factory cargo truck approach - faded old rolling marks",P(90,-6.7f),P(88.9f,-4.4f),P(86.9f,-1.9f),P(87.0f,1.8f),1.85f,.27f,.70f,tyre);
  Pair(root,"Old short braking marks along industrial road",P(100,-6.45f),P(104,-6.38f),P(108,-6.05f),P(111.4f,-5.95f),1.72f,.24f,.60f,brake);
  Pair(root,"Narrow warehouse hand trolley wheel traces",P(118.8f,-2.3f),P(118.6f,-1.4f),P(117.6f,.1f),P(117.5f,1.9f),.73f,.085f,.48f,tyre);
  Pair(root,"Parallel pallet runner drag scars",P(135.1f,1.8f),P(135.2f,.3f),P(134.6f,-1.1f),P(133.9f,-2.5f),.68f,.065f,.9f,drag);
  Track(root,"Short diagonal crate scrape",P(137.7f,-2.0f),P(137.4f,-1.4f),P(136.9f,-.7f),P(136.8f,.25f),0,.085f,.75f,drag);
  Track(root,"Thin dragged metal edge mark",P(136.5f,-2.1f),P(136.1f,-1.7f),P(135.8f,-.8f),P(135.6f,-.1f),0,.025f,.65f,brake);
  Track(root,"Interrupted scuff near factory loading steps",P(98.0f,-2.35f),P(98.7f,-1.7f),P(99.0f,-.7f),P(99.2f,.65f),0,.07f,.65f,drag);
  Track(root,"Parallel thin scrape near loading steps",P(98.4f,-2.4f),P(99.0f,-1.6f),P(99.3f,-.7f),P(99.5f,.7f),0,.036f,.55f,drag);
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; deferred";Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory("Backups/YardTrafficWear");Directory.CreateDirectory("output/yard-traffic-wear");var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
  try{if(All<Transform>(s).Any(t=>t.name==Marker))return "Traffic wear already applied";var backup="Backups/YardTrafficWear/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(s,backup,true);var collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();
   var pole=All<Transform>(s).Single(t=>t.name=="Yard street light 3 - failed");if(Mathf.Abs(pole.position.x-128.32f)>.1f||pole.GetComponentsInChildren<Collider2D>(true).Length+pole.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Unexpected shutter-front lamp");pole.gameObject.SetActive(false);var root=G(All<RecoilPrototype>(s).Single().transform,Marker);Build(root);
   if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy)))throw new Exception("Collision changed");if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Door state changed");EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();string result="PASS removed complete failed lamp at X128.32 Z-1.6, directly in front of warehouse shutter. Added 16 aged surface strips: paired warehouse turning/reversing tyre arcs, faded factory truck traces, road braking scuffs, narrow trolley marks, pallet runner drags and isolated crate/metal scrapes. Ground-only geometry with upward normals; no collision or door changes. Backup="+backup;File.WriteAllText("output/yard-traffic-wear/verification.txt",result);return result;
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
 }
 public static string Capture()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; deferred";var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);var other=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(scene=>scene!=s).ToArray();var rs=other.SelectMany(All<Renderer>).Where(r=>r.enabled).ToArray();var ls=other.SelectMany(All<Light>).Where(l=>l.enabled).ToArray();GameObject cg=null,lg=null;
  try{foreach(var r in rs)r.enabled=false;foreach(var l in ls)l.enabled=false;SceneManager.SetActiveScene(s);All<Recoil3DWorld>(s).First().ApplyAtmosphere();cg=new GameObject("Temporary traffic wear inspection camera");var camera=cg.AddComponent<Camera>();camera.fieldOfView=46;camera.nearClipPlane=.1f;camera.farClipPlane=220;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.035f);var shot=typeof(FactoryRoofStairHeadPass).GetMethod("Shot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   camera.transform.position=new Vector3(128.3f,3.7f,-9);camera.transform.LookAt(new Vector3(128.3f,2.1f,2.6f));shot.Invoke(null,new object[]{camera,"ShutterWithoutFrontLamp"});camera.fieldOfView=48;camera.transform.position=new Vector3(113,14,-19);camera.transform.LookAt(new Vector3(118,1,-.9f));shot.Invoke(null,new object[]{camera,"FactoryRoadTyreMarksNight"});
   lg=new GameObject("Temporary traffic inspection fill");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.0f;light.color=new Color(1,.91f,.8f);light.transform.rotation=Quaternion.Euler(48,-25,0);shot.Invoke(null,new object[]{camera,"FactoryRoadTyreMarksInspection"});camera.transform.position=new Vector3(130,7,-8);camera.transform.LookAt(new Vector3(129,.15f,-.4f));camera.fieldOfView=49;shot.Invoke(null,new object[]{camera,"WarehouseTyreAndDragDetail"});return "Four actual scene screenshots captured";
  }finally{if(cg)Object.DestroyImmediate(cg);if(lg)Object.DestroyImmediate(lg);foreach(var r in rs)if(r)r.enabled=true;foreach(var l in ls)if(l)l.enabled=true;if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
 }
}
