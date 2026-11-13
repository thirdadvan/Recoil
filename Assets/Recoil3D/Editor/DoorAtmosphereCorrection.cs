using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class DoorAtmosphereCorrection
{
 static T[] All<T>(Scene s) where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static bool RoomEntrance(Scene s,RecoilDoor d){string t=Path.GetFileNameWithoutExtension(d.destinationScene);return s.name=="RecoilFactoryInterior"&&t!="RecoilPrototype"||(s.name=="RecoilFactoryWarehouse"||s.name=="RecoilWarehouseLightingTest")&&t=="RecoilWarehouseStoreroom"||s.name=="RecoilResidentialStairwell"&&t=="RecoilResidentialApartment";}
 static float Floor(Scene s,RecoilDoor d){var p=d.InteractionPosition;var floors=All<BoxCollider2D>(s).Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&(!c.isTrigger||c.GetComponent<SoftRoofPlatform>())&&c.bounds.size.x>1&&c.bounds.size.y<1.3f&&c.bounds.min.x<=p.x+.1f&&c.bounds.max.x>=p.x-.1f&&c.bounds.max.y<=p.y+.15f&&c.bounds.max.y>p.y-3).ToArray();return floors.Length>0?floors.Max(c=>c.bounds.max.y):d.transform.position.y-1.25f;}
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var original=SceneManager.GetActiveScene();string backup="Backups/DoorAtmosphereCorrection/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);var report=new List<string>();
  foreach(var p in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&!e.path.EndsWith("/RecoilPrototype.unity")).Select(e=>e.path).Concat(new[]{"Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity"}).Distinct()){
   if(!File.Exists(p))continue;var s=EditorSceneManager.OpenScene(p,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);try{
    EditorSceneManager.SaveScene(s,backup+"/"+(p.Contains("/Tests/")?"Test_":"")+s.name+".unity",true);
    foreach(var d in All<RecoilDoor>(s)){
     var g=d.GetComponent<DoorProximityGlow>();if(!g)g=d.gameObject.AddComponent<DoorProximityGlow>();g.ApplyBlend(0);
     foreach(var t in d.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Door reflective highlight"||t.name=="Door activation halo - depth tested").ToArray())Object.DestroyImmediate(t.gameObject);
     g.halo=null;g.reflectiveSurfaces=new Renderer[0];g.surfaceSources=new Renderer[0];g.doorwayLights=new Light[0];g.door=d;g.world=All<RecoilPrototype>(s).Single();
     bool entrance=RoomEntrance(s,d);bool invalid=s.name=="RecoilFactoryInterior"&&(d.name=="Third floor left fire exit - unlock shortcut"||d.irreparableDamage);
     bool roof=d.name.ToLowerInvariant().Contains("skylight");g.enabled=!invalid;g.volumeEnabled=!entrance&&!invalid&&!roof;
     g.volumeOrigin=new Vector3(d.InteractionPosition.x,Floor(s,d)+1.15f,-3.1f);g.apertureHalfSize=new Vector2(.68f,1.15f);g.volumeLength=3.5f;g.volumeSpread=.12f;g.restingDensity=.006f;g.activeDensity=.042f;g.editorPreview=0;g.ApplyBlend(0);EditorUtility.SetDirty(g);
     if(entrance){
      d.gameObject.SetActive(true);
      foreach(var v in d.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=false;v.Sync();EditorUtility.SetDirty(v);}
      foreach(var r in d.GetComponentsInChildren<MeshRenderer>(true))if(r.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly&&!r.name.Contains("Unseen")){r.enabled=true;EditorUtility.SetDirty(r);}
      report.Add(s.name+" / "+d.name+": room-entry door geometry visible; destination preserved, no exit shaft");
     }else report.Add(s.name+" / "+d.name+": aperture volume="+g.volumeEnabled+", removed rectangular highlight surfaces");
    }
    if(s.name=="RecoilFactoryInterior")FixCorridor(s,report);
    EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
    if(s.name=="RecoilFactoryInterior"||s.name=="RecoilServiceStore"){
     var capture=typeof(IndoorExitReviewPass).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
     var door=All<RecoilDoor>(s).First(d=>d.GetComponent<DoorProximityGlow>().volumeEnabled);
     capture.Invoke(null,new object[]{s,door,"Corrected_"+s.name+"_Rest",0f});capture.Invoke(null,new object[]{s,door,"Corrected_"+s.name+"_Active",1f});
     if(s.name=="RecoilFactoryInterior")capture.Invoke(null,new object[]{s,All<RecoilDoor>(s).First(d=>d.name=="SHIFT OFFICE"),"Corrected_Upper_Corridor",0f});
    }
   }finally{EditorSceneManager.CloseScene(s,true);}
  }
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();AssetDatabase.SaveAssets();Directory.CreateDirectory("output/door-atmosphere");File.WriteAllText("output/door-atmosphere/result.txt",string.Join("\n",report)+"\nBackup: "+backup);
 }
 static void FixCorridor(Scene s,List<string> report){
  var look=All<WarehouseAtmosphereTest>(s).Single();var lamps=All<Light>(s).Where(l=>l.name=="Downward soft corridor illumination").ToArray();
  // Real fixtures were present but absent from the volume list; at 3.1m throw the old
  // 0.8/range4.7 profile also left the usable corridor very weakly lit.
  foreach(var l in lamps){l.intensity=3.2f;l.range=5.5f;l.spotAngle=118;l.innerSpotAngle=70;l.color=new Color(.72f,.79f,.75f);l.transform.localPosition=new Vector3(0,-.15f,-.6f);l.transform.rotation=Quaternion.LookRotation(new Vector3(0,-3.1f,-.6f));l.enabled=true;
   foreach(var r in l.transform.parent.GetComponentsInChildren<MeshRenderer>())if(r.name=="Frosted fluorescent tube"){
    var m=r.sharedMaterial;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(1.5f,1.65f,1.5f));EditorUtility.SetDirty(m);
   }
  }
  look.lamps=look.lamps.Where(l=>l).Concat(lamps).Distinct().ToArray();
  // Reset cached baseline on the edited component, so the former 0.8 is not written back.
  var cache=typeof(WarehouseAtmosphereTest).GetField("decorativeBaseIntensities",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(look) as Dictionary<Light,float>;if(cache!=null)foreach(var l in lamps)cache.Remove(l);
  look.ApplyLook();EditorUtility.SetDirty(look);
  report.Add("Upper corridors: "+lamps.Length+" real fixtures at intensity3.2, range5.5, aimed toward the walking plane; luminous diffusers and registered volumetric shafts.");
 }

 public static void CaptureAndVerify(){
  var original=SceneManager.GetActiveScene();var report=new List<string>();
  foreach(string path in new[]{"Assets/Scenes/3D/RecoilServiceStore.unity","Assets/Scenes/3D/RecoilFactoryInterior.unity"}){
   var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
    var all=All<Transform>(s);if(all.Any(t=>t.name=="Door reflective highlight"||t.name=="Door activation halo - depth tested"))throw new Exception("Rectangular card still present");
    foreach(var d in All<RecoilDoor>(s).Where(d=>RoomEntrance(s,d))){if(!d.gameObject.activeInHierarchy||!d.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled))throw new Exception("Room entrance not visible: "+d.name);report.Add("Visible room-entry geometry and preserved destination: "+d.name);}
    var capture=typeof(IndoorExitReviewPass).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);var door=All<RecoilDoor>(s).First(d=>d.GetComponent<DoorProximityGlow>().volumeEnabled);
    capture.Invoke(null,new object[]{s,door,"Corrected_"+s.name+"_Rest",0f});capture.Invoke(null,new object[]{s,door,"Corrected_"+s.name+"_Active",1f});
    if(s.name=="RecoilFactoryInterior"){capture.Invoke(null,new object[]{s,All<RecoilDoor>(s).First(d=>d.name=="SHIFT OFFICE"),"Corrected_Upper_Corridor",0f});report.Add("Visible active corridor fixtures: "+All<Light>(s).Count(l=>l.name=="Downward soft corridor illumination"&&l.enabled&&l.gameObject.activeInHierarchy&&l.intensity>3));}
   }finally{EditorSceneManager.CloseScene(s,true);}
  }
  var shader=Shader.Find("Recoil/Art Tests/Warehouse Atmosphere");if(!shader.isSupported||ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Atmosphere shader error");report.Add("Depth-tested aperture volume shader: supported, no compile errors");
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();File.WriteAllLines("output/door-atmosphere/verification.txt",report);
 }
}
