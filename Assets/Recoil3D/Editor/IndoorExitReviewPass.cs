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

public static class IndoorExitReviewPass
{
 static string[] Paths{get{return EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Where(File.Exists).Distinct().ToArray();}}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static string Full(Transform t){return t.parent?Full(t.parent)+"/"+t.name:t.name;}
 static bool Prop(string name){string n=name.ToLowerInvariant();return new[]{"barrel","drum","crate","carton","packing box","cargo box","litter bin","packing case","storage box","basket","piled freight"}.Any(n.Contains);}
 public static void Inspect(){
  Directory.CreateDirectory("output/indoor-exits");var b=new StringBuilder();var original=SceneManager.GetActiveScene();
  foreach(string path in Paths){var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try{
    b.AppendLine("SCENE "+path);var doors=All<RecoilDoor>(s);var renderers=All<MeshRenderer>(s).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
    foreach(var d in doors){b.AppendLine("DOOR "+Full(d.transform)+" pos="+d.transform.position+" interaction="+d.InteractionPosition+" target="+d.destinationScene+" range="+d.interactionRange);
     foreach(var l in d.GetComponentsInChildren<Light>(true))b.AppendLine(" DOOR LIGHT "+Full(l.transform)+" p="+l.transform.position+" I="+l.intensity+" shadow="+l.shadows+" cookie="+(l.cookie?l.cookie.name:"none"));
     foreach(var r in renderers.Where(r=>Prop(r.name)||Prop(r.transform.parent?r.transform.parent.name:"")))if(Mathf.Abs(r.bounds.center.x-d.InteractionPosition.x)<3&&r.bounds.min.y<d.InteractionPosition.y+1&&r.bounds.max.y>d.InteractionPosition.y-1.2f&&r.bounds.min.z<3)b.AppendLine(" PROP "+Full(r.transform)+" bounds="+r.bounds);
    }
    if(path.Contains("RecoilFactoryInterior")){
     foreach(var l in All<Light>(s).Where(l=>l.transform.position.x<8))b.AppendLine("LEFT LIGHT "+Full(l.transform)+" p="+l.transform.position+" intensity="+l.intensity+" enabled="+l.enabled+" shadows="+l.shadows+" range="+l.range+" type="+l.type);
     foreach(var r in renderers.Where(r=>r.bounds.min.x<5&&r.bounds.max.x>-.5f))b.AppendLine("LEFT MESH "+Full(r.transform)+" bounds="+r.bounds+" mat="+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name)));
     foreach(var group in renderers.GroupBy(r=>r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh:null).Where(g=>g.Key)){
      var list=group.ToArray();for(int i=0;i<list.Length;i++)for(int j=i+1;j<list.Length;j++)if(list[i].bounds.min.x<6&&Vector3.Distance(list[i].bounds.center,list[j].bounds.center)<.04f&&Vector3.Distance(list[i].bounds.size,list[j].bounds.size)<.04f)b.AppendLine("DUPLICATE "+Full(list[i].transform)+" == "+Full(list[j].transform));
     }
    }
   }finally{if(added)EditorSceneManager.CloseScene(s,true);}
  }
  if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);File.WriteAllText("output/indoor-exits/inspection.txt",b.ToString());Debug.Log("INDOOR_EXIT_INSPECTION_READY");
 }

 static bool NearDoor(Renderer r,RecoilDoor d){var b=r.bounds;var p=d.InteractionPosition;return b.min.x<p.x+3&&b.max.x>p.x-3&&b.min.y<p.y+1.1f&&b.max.y>p.y-1.3f&&b.min.z<3.2f&&b.max.z> -15;}
 static bool Protected(Transform t){return t.GetComponentInParent<RecoilDoor>()||t.GetComponentInChildren<RecoilDoor>(true)||t.GetComponentInChildren<ContinuousInteractionItem>(true)||t.GetComponentInChildren<ReadableProp>(true)||t.GetComponentInChildren<RampagerController>(true)||t.GetComponentInChildren<KBKStackerCrane>(true)||t.GetComponentInChildren<ClimbableLadder>(true);}
 static int ClearExitProps(Scene s,List<string> report){
  var doors=All<RecoilDoor>(s);var removed=new HashSet<GameObject>();
  foreach(var r in All<MeshRenderer>(s).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray()){
   if(!Prop(r.name)&&!Prop(r.transform.parent?r.transform.parent.name:""))continue;if(!doors.Any(d=>NearDoor(r,d)))continue;
   var t=r.transform;
   // A combined accessory mesh is already a material-specific piece. Do not remove its entire shop.
   if(!t.name.StartsWith("Combined")){
    if(t.name.StartsWith("3D mesh")&&t.parent)t=t.parent;
    while(t.parent&&Prop(t.parent.name)&&!t.parent.GetComponent<RecoilPrototype>())t=t.parent;
   }
   if(Protected(t))continue;removed.Add(t.gameObject);
  }
  foreach(var go in removed){report.Add(s.name+": cleared exit prop "+Full(go.transform));go.SetActive(false);EditorUtility.SetDirty(go);}
  return removed.Count;
 }
 static void DisableLight(Light l,WarehouseAtmosphereTest look){
  if(look){int i=Array.IndexOf(look.originalLights,l);if(i>=0){if(i<look.originalIntensities.Length)look.originalIntensities[i]=0;if(i<look.testIntensities.Length)look.testIntensities[i]=0;}look.decorativeLights=look.decorativeLights.Where(x=>x!=l).ToArray();look.lamps=look.lamps.Where(x=>x!=l).ToArray();EditorUtility.SetDirty(look);}
  l.intensity=0;l.gameObject.SetActive(false);EditorUtility.SetDirty(l);EditorUtility.SetDirty(l.gameObject);
 }
 static void FixFactory(Scene s,List<string> report){
  if(s.name!="RecoilFactoryInterior")return;var look=All<WarehouseAtmosphereTest>(s).Single();
  var floor=All<Recoil3DSpriteVisual>(s).Single(v=>v.name=="Factory Floor");
  foreach(var edge in floor.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v!=floor&&v.name=="Edge"&&v.meshRenderer)){
   var a=floor.meshRenderer.bounds;var b=edge.meshRenderer.bounds;
   if(Mathf.Abs(a.max.y-b.max.y)<.003f){edge.suppressed=true;edge.Sync();EditorUtility.SetDirty(edge);report.Add("Factory left flicker: suppressed coplanar legacy floor Edge (top="+b.max.y+") over the main floor (top="+a.max.y+"); physical floor retained.");}
  }
  var entry=All<RecoilDoor>(s).Single(d=>d.name=="Factory Door - Return Outdoors");
  foreach(var l in entry.GetComponentsInChildren<Light>(true).Where(l=>l.GetComponentInParent<Recoil3DLight>())){DisableLight(l,look);report.Add("Factory left lighting: retired legacy exit fixture light "+l.name);}
  foreach(var l in All<Light>(s).Where(l=>l.transform.parent&&l.transform.parent.name=="Indoor fixed path lighting"&&Mathf.Abs(l.transform.position.x-entry.transform.position.x)<.03f&&l.transform.position.y<3.5f).ToArray()){
   DisableLight(l,look);report.Add("Factory left lighting: removed duplicate doorway overhead/reflected source "+l.name);
  }
  foreach(var l in entry.GetComponentsInChildren<Light>(true).Where(l=>l.gameObject.activeInHierarchy)){
   l.shadows=LightShadows.Soft;l.shadowBias=.015f;l.shadowNormalBias=.045f;l.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Medium;EditorUtility.SetDirty(l);
  }
 }
 static float FloorAt(Scene s,RecoilDoor d){
  var p=d.InteractionPosition;var floors=All<BoxCollider2D>(s).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&(!c.isTrigger||c.GetComponent<SoftRoofPlatform>())&&c.bounds.size.x>1&&c.bounds.size.y<1.3f&&c.bounds.min.x<=p.x+.1f&&c.bounds.max.x>=p.x-.1f&&c.bounds.max.y<=p.y+.15f&&c.bounds.max.y>p.y-3).ToArray();
  return floors.Length>0?floors.Max(c=>c.bounds.max.y):d.transform.position.y-1.25f;
 }
 static Material HaloMaterial(){
  const string path="Assets/Recoil3D/Materials/Door activation halo.mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Door Proximity Halo"));AssetDatabase.CreateAsset(m,path);}return m;
 }
 static DoorProximityGlow AddGlow(Scene s,RecoilDoor d,Material material){
  var glow=d.GetComponent<DoorProximityGlow>();if(!glow)glow=d.gameObject.AddComponent<DoorProximityGlow>();
  glow.door=d;glow.world=All<RecoilPrototype>(s).Single();
  var lights=d.GetComponentsInChildren<Light>(true).Where(l=>l.gameObject.activeInHierarchy&&!l.GetComponentInParent<RampagerTentacleVisual>()&&!l.GetComponentInParent<Recoil3DActorVisual>()).ToArray();
  bool skylight=d.autoTravelAboveOpening||d.name.ToLowerInvariant().Contains("skylight");
  float floor=FloorAt(s,d);
  if(lights.Length==0){
   var t=new GameObject("Door activation light - soft threshold spill").transform;t.SetParent(d.transform,false);t.position=new Vector3(d.InteractionPosition.x,floor+2.55f,1.1f);t.rotation=Quaternion.LookRotation(new Vector3(d.InteractionPosition.x,floor+.02f,.2f)-t.position);
   var l=t.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.spotAngle=65;l.innerSpotAngle=35;l.range=4.8f;l.intensity=.42f;l.color=new Color(.48f,.60f,.64f);l.shadows=LightShadows.None;l.renderMode=LightRenderMode.ForcePixel;lights=new[]{l};
  }
  glow.doorwayLights=lights;
  var plane=d.transform.Find("Door activation halo - depth tested");if(!plane){var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Door activation halo - depth tested";Object.DestroyImmediate(go.GetComponent<Collider>());plane=go.transform;plane.SetParent(d.transform,false);}
  if(skylight){plane.position=d.transform.position+new Vector3(0,-.015f,-.18f);plane.rotation=Quaternion.identity;plane.localScale=new Vector3(2.1f,.25f,1);}
  else {plane.position=new Vector3(d.InteractionPosition.x,floor+.022f,.05f);plane.rotation=Quaternion.Euler(90,0,0);plane.localScale=new Vector3(1.65f,2.4f,1);}
  var renderer=plane.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;glow.halo=renderer;
  glow.haloTint=lights[0].color;glow.editorPreview=0;glow.ApplyBlend(0);EditorUtility.SetDirty(glow);return glow;
 }
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before changing exits");
  string backup="Backups/IndoorExitReadability/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);Directory.CreateDirectory("Captures/IndoorExits");
  var original=SceneManager.GetActiveScene();var report=new List<string>();int props=0,doors=0;var material=HaloMaterial();
  foreach(string path in Paths){var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
   try{
    EditorSceneManager.SaveScene(s,backup+"/"+(path.Contains("/Tests/")?"Test_":"")+s.name+".unity",true);
    var exits=All<RecoilDoor>(s);var locations=exits.ToDictionary(d=>d,d=>d.transform.position);var routes=exits.ToDictionary(d=>d,d=>d.destinationScene+"|"+d.destinationSpawn);
    if(s.name=="RecoilFactoryInterior")Capture(s,exits.First(d=>d.name=="Factory Door - Return Outdoors"),"Factory_Left_Before",0);
    props+=ClearExitProps(s,report);FixFactory(s,report);
    foreach(var d in exits){var glow=AddGlow(s,d,material);doors++;VerifyGlow(glow);}
    foreach(var d in exits){if(d.transform.position!=locations[d]||d.destinationScene+"|"+d.destinationSpawn!=routes[d])throw new Exception("Door route/position changed: "+d.name);}
    foreach(var r in All<MeshRenderer>(s).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&(Prop(r.name)||Prop(r.transform.parent?r.transform.parent.name:""))))if(!Protected(r.transform)&&exits.Any(d=>NearDoor(r,d)))report.Add("RECHECK remaining prop mesh: "+Full(r.transform));
    EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
    if(s.name=="RecoilFactoryInterior"||s.name=="RecoilFactoryWarehouse"){
     var d=exits.First(d=>!string.IsNullOrEmpty(d.destinationScene)&&d.destinationScene.EndsWith("RecoilPrototype.unity"));Capture(s,d,s.name+"_Exit_Idle",0);Capture(s,d,s.name+"_Exit_Active",1);
    }
    report.Add(s.name+": "+exits.Length+" doors configured; routes and interaction origins unchanged.");
   }finally{if(added)EditorSceneManager.CloseScene(s,true);}
  }
  if(original.IsValid()&&original.isLoaded){SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(original).FirstOrDefault();if(look)look.ApplyLook();}
  AssetDatabase.SaveAssets();File.WriteAllText("output/indoor-exits/result.txt","PASS: "+Paths.Length+" indoor scenes, "+doors+" door glows, "+props+" exit obstructions removed.\nGlow has .22s fade in / .4s fade out, 1.85x native light, depth-tested soft threshold halo; uses actual nearest-door InRange and line of sight.\n"+string.Join("\n",report)+"\nBackup: "+backup);
  Debug.Log("INDOOR_EXIT_READABILITY_PASS: scenes="+Paths.Length+", doors="+doors+", removed props="+props+"; backup="+backup);
 }
 static void VerifyGlow(DoorProximityGlow glow){
  var baseValues=glow.doorwayLights.Select(l=>l.intensity).ToArray();for(int i=0;i<120;i++)glow.ApplyBlend(1);
  for(int i=0;i<baseValues.Length;i++)if(Mathf.Abs(glow.doorwayLights[i].intensity-baseValues[i]*glow.brightnessMultiplier)>.001f)throw new Exception("Door glow accumulates brightness: "+glow.name);
  glow.ApplyBlend(0);for(int i=0;i<baseValues.Length;i++)if(Mathf.Abs(glow.doorwayLights[i].intensity-baseValues[i])>.001f)throw new Exception("Door light did not restore");
 }
 static void Capture(Scene s,RecoilDoor d,string name,float glowAmount){
  var world=All<RecoilPrototype>(s).Single();var cam=All<Camera>(s).First(c=>c.CompareTag("MainCamera"));var follow=cam.GetComponent<RecoilCameraController>();bool wasOn=follow&&follow.enabled;var p=cam.transform.position;var q=cam.transform.rotation;float f=cam.fieldOfView;var player=world.Player.Position;var cameraScene=cam.scene;var glow=d.GetComponent<DoorProximityGlow>();float floor=FloorAt(s,d);
  var otherRenderers=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Where(other=>other!=s&&other.isLoaded).SelectMany(other=>All<Renderer>(other)).Where(r=>r.enabled).ToArray();
  var otherLights=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Where(other=>other!=s&&other.isLoaded).SelectMany(other=>All<Light>(other)).Where(l=>l.enabled).ToArray();
  try{
   foreach(var r in otherRenderers)r.enabled=false;foreach(var l in otherLights)l.enabled=false;
   world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();var look=cam.GetComponent<WarehouseAtmosphereTest>();if(look)look.ApplyLook();if(follow)follow.enabled=false;cam.scene=s;
   world.Player.Position=new Vector2(d.InteractionPosition.x+1,floor+world.Player.BodyHeight*.5f);world.Player.SyncVisuals(0);if(glow){glow.editorPreview=glowAmount;glow.ApplyBlend(glowAmount);}
   cam.transform.SetPositionAndRotation(new Vector3(d.InteractionPosition.x+3,floor+3.7f,-15),Quaternion.Euler(5,0,0));cam.fieldOfView=39;
   var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var prev=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,image=null;
   try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1280,720,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1280,720),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;image=new Texture2D(1280,720,TextureFormat.RGB24,false,true);image.SetPixels(pixels);image.Apply();File.WriteAllBytes("Captures/IndoorExits/"+name+".png",image.EncodeToPNG());}
   finally{cam.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(image)Object.DestroyImmediate(image);}
  }finally{foreach(var r in otherRenderers)if(r)r.enabled=true;foreach(var l in otherLights)if(l)l.enabled=true;if(glow){glow.editorPreview=0;glow.ApplyBlend(0);}world.Player.Position=player;world.Player.SyncVisuals(0);cam.transform.SetPositionAndRotation(p,q);cam.fieldOfView=f;cam.scene=cameraScene;if(follow)follow.enabled=wasOn;}
 }
}
