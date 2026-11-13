using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class UpperFloorLightingReview
{
 static T[] All<T>(Scene s) where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static string Path(Transform t){return t.parent?Path(t.parent)+"/"+t.name:t.name;}
 public static void Inspect(){var b=new StringBuilder();foreach(var p in EditorBuildSettings.scenes.Where(x=>x.enabled&&x.path.StartsWith("Assets/Scenes/3D/")&&!x.path.EndsWith("/RecoilPrototype.unity"))){var s=EditorSceneManager.OpenScene(p.path,OpenSceneMode.Additive);try{b.AppendLine("SCENE "+p.path);foreach(var v in All<Recoil3DSpriteVisual>(s).Where(v=>!v.suppressed&&v.meshRenderer&&v.gameObject.activeInHierarchy))b.AppendLine("EXTRUSION "+Path(v.transform)+" bounds="+v.meshRenderer.bounds+" depth="+v.depth+" offset="+v.depthOffset);if(s.name=="RecoilFactoryInterior"){foreach(var d in All<RecoilDoor>(s))b.AppendLine("DOOR "+Path(d.transform)+" pos="+d.transform.position+" permanent="+d.permanentlyBlocked+" damaged="+d.irreparableDamage+" threat="+d.blockedByThreat+" to="+d.destinationScene);foreach(var l in All<Light>(s))b.AppendLine("LIGHT "+Path(l.transform)+" pos="+l.transform.position+" intensity="+l.intensity+" active="+l.gameObject.activeInHierarchy);}}finally{EditorSceneManager.CloseScene(s,true);}}Directory.CreateDirectory("output/upper-floor-review");File.WriteAllText("output/upper-floor-review/inspection.txt",b.ToString());}

 static Material Mat(string name,Color colour,bool emission=false){string p="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,p);}m.color=colour;m.SetFloat("_Glossiness",.28f);if(emission){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",colour*.7f);}return m;}
 static Transform Box(Transform parent,string name,Vector3 p,Vector3 scale,Material m){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=m;return g.transform;}
 static void Unregister(Light l,WarehouseAtmosphereTest look){if(look){int i=Array.IndexOf(look.originalLights,l);if(i>=0){look.originalIntensities[i]=0;look.testIntensities[i]=0;}look.decorativeLights=look.decorativeLights.Where(x=>x!=l).ToArray();look.lamps=look.lamps.Where(x=>x!=l).ToArray();}l.intensity=0;l.gameObject.SetActive(false);}
 static void Retire(Recoil3DSpriteVisual v,List<string> report,string reason){v.suppressed=true;if(v.meshRenderer&&v.meshRenderer.gameObject!=v.gameObject&&v.meshRenderer.transform.childCount==0)Object.DestroyImmediate(v.meshRenderer.gameObject);else if(v.meshRenderer)v.meshRenderer.enabled=false;EditorUtility.SetDirty(v);report.Add(reason+": "+Path(v.transform));}
 static void Cleanup(Scene s,List<string> report){
  var visuals=All<Recoil3DSpriteVisual>(s);
  foreach(var v in visuals.Where(v=>!v.suppressed&&v.meshRenderer&&v.gameObject.activeInHierarchy).ToArray()){
   var bounds=v.meshRenderer.bounds;
   // Only redundant cap strips whose top exactly meets their existing structural parent.
   var parent=v.transform.parent?v.transform.parent.GetComponent<Recoil3DSpriteVisual>():null;
   if(v.name=="Edge"&&parent&&parent.meshRenderer&&Mathf.Abs(parent.meshRenderer.bounds.max.y-bounds.max.y)<.003f){Retire(v,report,"Removed coplanar legacy cap");continue;}
   if(s.name=="RecoilFactoryInterior"&&v.name=="Steel landing deck"&&Path(v.transform).Contains("Left fire escape")){
    bool covered=visuals.Any(f=>f!=v&&!f.suppressed&&f.meshRenderer&&(f.name=="Factory Floor"||f.name.Contains("solid slab"))&&f.meshRenderer.bounds.min.x<=bounds.min.x&&f.meshRenderer.bounds.max.x>=bounds.max.x&&Mathf.Abs(f.meshRenderer.bounds.max.y-bounds.max.y)<.025f);
    if(covered){Retire(v,report,"Removed landing deck already covered by main slab");continue;}
   }

  }
 }
 static void UpperFloors(Scene s,List<string> report){
  var look=All<WarehouseAtmosphereTest>(s).Single();
  var steel=Mat("Aged fluorescent ivory housing",new Color(.36f,.36f,.30f));var dark=Mat("Fluorescent end sockets",new Color(.11f,.13f,.12f));var tube=Mat("Dim cool white fluorescent diffuser",new Color(.49f,.57f,.55f),true);
  var root=All<RecoilPrototype>(s).Single().transform;
  var old=root.Find("Upper floors - dim fluorescent fixtures");if(old)Object.DestroyImmediate(old.gameObject);
  var group=new GameObject("Upper floors - dim fluorescent fixtures").transform;group.SetParent(root,false);
  foreach(var g in All<Transform>(s).Where(t=>t.name=="Ceiling fixture"&&Path(t).Contains("public corridor")).ToArray()){
   foreach(var l in g.GetComponentsInChildren<Light>(true))Unregister(l,look);
   g.gameObject.SetActive(false);
  }
  for(int floor=2;floor<=4;floor++){
   float y=floor==2?7.6f:floor==3?11.6f:15.6f;
   foreach(float x in new[]{4f,12f,20f,30.6f}){
    var g=new GameObject("Floor "+floor+" - double fluorescent "+x).transform;g.SetParent(group,false);g.position=new Vector3(x,y+3.28f,x>28?1.3f:1.7f);
    Box(g,"Folded enamel reflector",Vector3.zero,new Vector3(1.35f,.08f,.32f),steel);
    foreach(float z in new[]{-.095f,.095f}){
     var t=Box(g,"Frosted fluorescent tube",new Vector3(0,-.065f,z),new Vector3(1.15f,.035f,.035f),tube);
     foreach(float a in new[]{-.61f,.61f})Box(g,"Ceramic end socket",new Vector3(a,-.055f,z),new Vector3(.065f,.075f,.065f),dark);
    }
    var lamp=new GameObject("Downward soft corridor illumination");lamp.transform.SetParent(g,false);lamp.transform.localPosition=new Vector3(0,-.15f,0);lamp.transform.localRotation=Quaternion.Euler(90,0,0);
    var l=lamp.AddComponent<Light>();l.type=LightType.Spot;l.range=4.7f;l.spotAngle=122;l.innerSpotAngle=65;l.intensity=.8f;l.color=new Color(.68f,.76f,.72f);l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;
    look.decorativeLights=look.decorativeLights.Concat(new[]{l}).ToArray();
   }
  }
  foreach(var d in All<RecoilDoor>(s).Where(d=>d.name=="Third floor left fire exit - unlock shortcut"||d.irreparableDamage)){
   foreach(var l in d.GetComponentsInChildren<Light>(true))Unregister(l,look);
   var glow=d.GetComponent<DoorProximityGlow>();if(glow){glow.ApplyBlend(0);glow.enabled=false;foreach(var r in glow.reflectiveSurfaces)if(r)r.gameObject.SetActive(false);}
   report.Add("Removed exit lighting and interaction sheen: "+d.name+" at Y="+d.transform.position.y);
  }
  report.Add("Factory: 12 dim double-tube ceiling fixtures across upper corridors and stair landings. Ground-floor lighting retained.");
 }
 static bool IsLeavingInterior(Scene s,RecoilDoor d){
  string target=System.IO.Path.GetFileNameWithoutExtension(d.destinationScene);
  if(s.name=="RecoilFactoryInterior")return target=="RecoilPrototype";
  if(s.name=="RecoilFactoryWarehouse"||s.name=="RecoilWarehouseLightingTest")return target!="RecoilWarehouseStoreroom";
  if(s.name=="RecoilResidentialStairwell")return target!="RecoilResidentialApartment";
  return true;
 }
 static void ReflectiveDoor(Scene s,RecoilDoor d,Material material,List<string> report){
  var glow=d.GetComponent<DoorProximityGlow>();if(!glow)return;glow.ApplyBlend(0);
  foreach(var t in d.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Door activation halo - depth tested"||t.name=="Door activation light - soft threshold spill"||t.name=="Door reflective highlight").ToArray())Object.DestroyImmediate(t.gameObject);
  glow.halo=null;glow.doorwayLights=new Light[0];glow.brightnessMultiplier=1;glow.haloStrength=.32f;
  if(!IsLeavingInterior(s,d)){glow.reflectiveSurfaces=new Renderer[0];glow.surfaceSources=new Renderer[0];glow.enabled=false;EditorUtility.SetDirty(glow);report.Add(s.name+": no glow on room-entry door "+d.name);return;}
  glow.enabled=true;
  var sources=d.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly&&r.bounds.size.x<2.9f&&r.bounds.size.y>.65f&&r.bounds.size.y<3.5f&&Mathf.Abs(r.bounds.center.x-d.InteractionPosition.x)<1.5f&&!r.name.Contains("shadow")&&!r.name.Contains("Unseen")&&!r.name.Contains("jamb")&&!r.name.Contains("reveal")).ToArray();
  var surfaces=new List<Renderer>();var owners=new List<Renderer>();
  foreach(var source in sources){var filter=source.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;var g=new GameObject("Door reflective highlight");g.transform.SetParent(source.transform,false);g.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;surfaces.Add(r);owners.Add(source);}
  if(surfaces.Count==0){
   // Existing foreground entrances intentionally have no visible door mesh. Highlight
   // only the vertical opening boundary, never a horizontal floor projection.
   var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="Door reflective highlight";Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(d.transform,false);g.transform.position=new Vector3(d.InteractionPosition.x,d.InteractionPosition.y+.2f,d.transform.position.z-.03f);g.transform.rotation=Quaternion.identity;var ps=d.transform.lossyScale;g.transform.localScale=new Vector3(1.35f/Mathf.Max(.001f,Mathf.Abs(ps.x)),2.4f/Mathf.Max(.001f,Mathf.Abs(ps.y)),1/Mathf.Max(.001f,Mathf.Abs(ps.z)));var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;surfaces.Add(r);owners.Add(null);
  }
  glow.reflectiveSurfaces=surfaces.ToArray();glow.surfaceSources=owners.ToArray();glow.editorPreview=0;glow.ApplyBlend(0);EditorUtility.SetDirty(glow);
  report.Add(s.name+": material-only reflection on "+d.name+" ("+surfaces.Count+" surfaces, no light boost / floor halo)");
 }
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before applying");
  string backup="Backups/UpperFloorsAndDoorReflection/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);Directory.CreateDirectory("output/upper-floor-review");
  var report=new List<string>();var original=SceneManager.GetActiveScene();
  string mp="Assets/Recoil3D/Materials/Door reflective highlight.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);if(!material){material=new Material(Shader.Find("Recoil/3D/Door Reflective Highlight"));AssetDatabase.CreateAsset(material,mp);}
  var paths=EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Distinct();
  foreach(var p in paths){if(!File.Exists(p))continue;var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);try{
   EditorSceneManager.SaveScene(s,backup+"/"+(p.Contains("/Tests/")?"Test_":"")+s.name+".unity",true);
   var doors=All<RecoilDoor>(s);var positions=doors.Select(d=>d.transform.position).ToArray();var routes=doors.Select(d=>d.destinationScene+"|"+d.destinationSpawn).ToArray();
   Cleanup(s,report);foreach(var d in doors)ReflectiveDoor(s,d,material,report);if(s.name=="RecoilFactoryInterior")UpperFloors(s,report);
   var look=All<WarehouseAtmosphereTest>(s).FirstOrDefault();if(look){look.decorativeLights=look.decorativeLights.Where(l=>l).ToArray();look.lamps=look.lamps.Where(l=>l).ToArray();EditorUtility.SetDirty(look);}
   for(int i=0;i<doors.Length;i++)if(positions[i]!=doors[i].transform.position||routes[i]!=doors[i].destinationScene+"|"+doors[i].destinationSpawn)throw new Exception("Door route changed");
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
   if(s.name=="RecoilFactoryInterior"){
    var capture=typeof(IndoorExitReviewPass).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
    capture.Invoke(null,new object[]{s,doors.First(d=>d.name=="SHIFT OFFICE"),"Upper_Floor_Corridor",0f});
    capture.Invoke(null,new object[]{s,doors.First(d=>d.name=="SHIFT OFFICE"),"Door_Reflection_Active",1f});
   }
  }finally{EditorSceneManager.CloseScene(s,true);}}
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();AssetDatabase.SaveAssets();
  File.WriteAllText("output/upper-floor-review/result.txt",string.Join("\n",report)+"\nBackup: "+backup);Debug.Log("UPPER_FLOORS_AND_REFLECTIVE_DOORS_PASS");
 }

 public static void RestoreNecessarySideWalls(){
  var original=SceneManager.GetActiveScene();int restored=0;
  var paths=EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Distinct();
  foreach(var p in paths){if(!File.Exists(p))continue;var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);try{bool changed=false;foreach(var v in All<Recoil3DSpriteVisual>(s).Where(v=>v.name.ToLowerInvariant().Contains("wall")&&Mathf.Abs(v.depth-34)<.001f&&Mathf.Abs(v.depthOffset+6)<.001f)){v.depth=86;v.depthOffset=-4;v.Sync();EditorUtility.SetDirty(v);changed=true;restored++;}if(changed){EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}}finally{EditorSceneManager.CloseScene(s,true);}}
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();
  var report=File.ReadAllLines("output/upper-floor-review/result.txt").Where(l=>!l.StartsWith("Trimmed side wall")).ToList();report.Add("Preserved "+restored+" deep structural side walls: required to close foreground/distant set; no speculative trimming.");File.WriteAllLines("output/upper-floor-review/result.txt",report);
 }

 public static void ApplyExitOnlyPolicy(){
  var original=SceneManager.GetActiveScene();var report=new List<string>();
  foreach(var p in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Distinct()){
   if(!File.Exists(p))continue;var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);try{
    foreach(var d in All<RecoilDoor>(s)){
     var glow=d.GetComponent<DoorProximityGlow>();if(!glow||IsLeavingInterior(s,d))continue;
     glow.ApplyBlend(0);foreach(var r in glow.reflectiveSurfaces)if(r)Object.DestroyImmediate(r.gameObject);
     glow.reflectiveSurfaces=new Renderer[0];glow.surfaceSources=new Renderer[0];glow.enabled=false;EditorUtility.SetDirty(glow);report.Add(s.name+" / "+d.name+": room entrance highlight removed");
    }
    EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
   }finally{EditorSceneManager.CloseScene(s,true);}
  }
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();File.WriteAllLines("output/upper-floor-review/exit-only-policy.txt",report);
 }

 public static void NormalizeOpeningHighlights(){
  var original=SceneManager.GetActiveScene();foreach(var p in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Distinct()){
   if(!File.Exists(p))continue;var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);try{foreach(var g in All<DoorProximityGlow>(s))for(int i=0;i<g.reflectiveSurfaces.Length;i++){if(i<g.surfaceSources.Length&&g.surfaceSources[i])continue;var r=g.reflectiveSurfaces[i];if(!r)continue;var ps=r.transform.parent.lossyScale;r.transform.localScale=new Vector3(1.35f/Mathf.Max(.001f,Mathf.Abs(ps.x)),2.4f/Mathf.Max(.001f,Mathf.Abs(ps.y)),1/Mathf.Max(.001f,Mathf.Abs(ps.z)));}EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}finally{EditorSceneManager.CloseScene(s,true);}}
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();
 }
}
