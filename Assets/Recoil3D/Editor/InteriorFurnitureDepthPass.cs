using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class InteriorFurnitureDepthPass
{
 static readonly string[] Homes={"Assets/Scenes/3D/RecoilKitchenMemory.unity","Assets/Scenes/3D/RecoilResidentialApartment.unity"};
 const string Store="Assets/Scenes/3D/RecoilServiceStore.unity";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static string Path(Transform t){return t.parent?Path(t.parent)+"/"+t.name:t.name;}
 public static void Inspect(){
  Directory.CreateDirectory("output/furniture-depth");var b=new StringBuilder();
  foreach(string path in Homes.Concat(new[]{Store})){
   var s=EditorSceneManager.OpenScene(path);b.AppendLine("SCENE "+path);
   foreach(var t in All<Transform>(s).Where(t=>!t.name.StartsWith("3D mesh")&&(Homes.Contains(path)?t.name.Contains("Bed")||t.name.Contains("bed")||t.name.Contains("Cabinet")||t.name.Contains("cabinet")||t.name.Contains("Stove")||t.name.Contains("Counter")||t.name.Contains("Mother")||t.name.Contains("cooking")||t.name.Contains("Furniture")||t.name.Contains("Wardrobe"):t.name.Contains("counter")||t.name.Contains("Counter")||t.name.Contains("clerk")||t.name.Contains("water gun")||t.name.Contains("pickup")))){
    b.AppendLine(Path(t)+" p="+t.position+" scale="+t.lossyScale+" components="+string.Join(",",t.GetComponents<Component>().Where(c=>c).Select(c=>c.GetType().Name)));
    foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).Take(6))b.AppendLine(" mesh "+r.name+" "+r.bounds);
   }
  }File.WriteAllText("output/furniture-depth/inspection.txt",b.ToString());
 }

 const string Marker="Furniture behind walking planes - 20261030";
 static Bounds BoundsOf(Transform root){var rs=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();if(rs.Length==0)throw new Exception("No furniture mesh: "+root.name);var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;}
 static void Place(Transform t,float centreZ){var p=t.position;p.z+=centreZ-BoundsOf(t).center.z;t.position=p;EditorUtility.SetDirty(t);}
 static Transform Bundle(Transform parent,string name,IEnumerable<Transform> items){var g=new GameObject(name).transform;g.SetParent(parent,false);foreach(var t in items.ToArray())t.SetParent(g,true);return g;}
 static void Need(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Home(Scene s,List<string> report){
  var world=All<RecoilPrototype>(s).Single();if(world.transform.Find(Marker)){report.Add(s.name+": already adjusted");return;}
  var bed=All<Transform>(s).Single(t=>t.name=="Bed - foreground");Place(bed,1.8f);bed.name="Bed - behind walking lane";
  var bedroom=bed.parent;
  foreach(var t in bedroom.Cast<Transform>().Where(t=>t.name.StartsWith("Wardrobe")).ToArray())Place(t,t.name=="Wardrobe"?3:t.name.Contains("handle")?2.83f:2.94f);
  var bedside=Bundle(bedroom,"Bedside cabinet and lamp - move together",bedroom.Cast<Transform>().Where(t=>t.name.StartsWith("Bedside ")).ToArray());Place(bedside,2.4f);
  var kitchen=All<Transform>(s).Single(t=>t.name=="Kitchen - mother cooking");
  var items=kitchen.Cast<Transform>().Where(t=>!t.name.Contains("wall")&&!t.name.Contains("Wall")&&t.name!="Pendant - kitchen").ToArray();
  var fittings=Bundle(kitchen,"Kitchen cabinets worktop stove and fittings - rear plane",items);
  foreach(Transform t in fittings){
   float z=t.name.Contains("seam")?2.94f:t.name.Contains("handle")?2.83f:t.name=="Countertop"?2.9f:t.name=="Stove"?2.8f:t.name=="Oven window"||t.name=="Stove control"?2.70f:t.name.Contains("cabinet")?3:2.8f;
   Place(t,z);
  }
  // Keep the cooking pose and the subsequent walking destination in the same depth lane.
  var story=world.GetComponent<KitchenMemoryController>();if(story){foreach(var t in new[]{story.mother,story.motherCookingPoint,story.bedroomRestPoint})if(t){var p=t.position;p.z=2.4f;t.position=p;EditorUtility.SetDirty(t);}}
  foreach(var v in world.GetComponentsInChildren<Recoil3DSpriteVisual>(true))v.Sync();
  Need(BoundsOf(bed).min.z>1.3f,"Bed overlaps the playable planes");Need(BoundsOf(bedside).min.z>1.3f,"Bedside cabinet overlaps the playable planes");Need(BoundsOf(fittings).min.z>2.4f,"Kitchen fittings overlap the playable planes");
  var marker=new GameObject(Marker);marker.transform.SetParent(world.transform,false);
  report.Add(s.name+": bed centre Z=1.8, wardrobe Z=3, bedside cabinet/lamp Z=2.4; cabinet fronts, handles, stove and worktop aligned behind both playable planes.");
 }
 static void Shop(Scene s,List<string> report){
  var world=All<RecoilPrototype>(s).Single();if(world.transform.Find(Marker)){report.Add(s.name+": already adjusted");return;}
  var counter=All<Transform>(s).Single(t=>t.name=="Service counter - solid shopfitting and register");var p=counter.position;p.z+=.9f;counter.position=p;
  foreach(string name in new[]{"Adult store clerk - assembled 2D art","Warm counter pendant"}){var t=All<Transform>(s).Single(x=>x.name==name);p=t.position;p.z+=.9f;t.position=p;EditorUtility.SetDirty(t);}
  var pickup=All<ContinuousInteractionItem>(s).Single(t=>t.name=="Water gun pickup");var visual=pickup.GetComponent<Recoil3DSpriteVisual>();Need(visual&&visual.meshRenderer,"Missing pickup visual");visual.depthOffset+=.9f/Mathf.Max(.001f,Mathf.Abs(visual.transform.lossyScale.z));visual.Sync();EditorUtility.SetDirty(visual);
  // The retired 2D counter must not remain an invisible blocker on the walking lane.
  var old=All<Transform>(s).Single(t=>t.name=="Rear counter");foreach(var c in old.GetComponentsInChildren<Collider2D>(true)){c.enabled=false;EditorUtility.SetDirty(c);}
  Need(BoundsOf(counter).min.z>2.5f,"Counter is still too close to the walking lane");Need(visual.meshRenderer.bounds.center.z>2.8f,"Pickup did not move with counter");
  var marker=new GameObject(Marker);marker.transform.SetParent(world.transform,false);report.Add(s.name+": entire counter/register/stock +0.9m Z; clerk, pendant and pickup move with it; retired 2D counter blocker disabled; pickup XY interaction point retained.");
 }
 public static void Apply(){
  Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Stop Play first");var original=SceneManager.GetActiveScene();string backup="Backups/InteriorFurnitureDepth/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);Directory.CreateDirectory("Captures/FurnitureDepth");var report=new List<string>();
  foreach(string path in Homes.Concat(new[]{Store})){
   var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
   try{
    EditorSceneManager.SaveScene(s,backup+"/"+s.name+".unity",true);
    var doors=All<RecoilDoor>(s).ToDictionary(d=>d,d=>(Vector2)d.transform.position);var interactions=All<ContinuousInteractionItem>(s).ToDictionary(i=>i,i=>(Vector2)i.transform.position);
    if(path==Store)Shop(s,report);else Home(s,report);
    Physics2D.SyncTransforms();foreach(var kv in doors)Need((Vector2)kv.Key.transform.position==kv.Value,"Door interaction moved");foreach(var kv in interactions)Need((Vector2)kv.Key.transform.position==kv.Value,"Item/NPC interaction moved");
    EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);Capture(s,path==Store);
   }finally{if(added)EditorSceneManager.CloseScene(s,true);}
  }
  if(original.IsValid()&&original.isLoaded){SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();}
  AssetDatabase.SaveAssets();Directory.CreateDirectory("output/furniture-depth");File.WriteAllText("output/furniture-depth/result.txt","PASS: furniture clearance and unchanged door/item/NPC XY interaction positions.\n"+string.Join("\n",report)+"\nBackup: "+backup);Debug.Log("INTERIOR_FURNITURE_DEPTH_PASS: "+string.Join("; ",report));
 }
 static void Capture(Scene s,bool shop){
  var world=All<RecoilPrototype>(s).Single();var cam=All<Camera>(s).Single(c=>c.CompareTag("MainCamera"));var follow=cam.GetComponent<RecoilCameraController>();var position=cam.transform.position;var rotation=cam.transform.rotation;float fov=cam.fieldOfView;bool on=follow&&follow.enabled;var player=world.Player.Position;var cameraScene=cam.scene;
  try{world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();cam.scene=s;if(follow)follow.enabled=false;
   if(shop){world.Player.Position=new Vector2(13.7f,world.Player.BodyHeight*.5f);world.Player.SyncVisuals(0);cam.transform.SetPositionAndRotation(new Vector3(13.6f,2.5f,-12),Quaternion.Euler(4,0,0));cam.fieldOfView=40;SaveCapture(cam,s.name+"_Counter");}
   else foreach(float x in new[]{4.8f,19.4f}){world.Player.Position=new Vector2(x,world.Player.BodyHeight*.5f);world.Player.SyncVisuals(0);cam.transform.SetPositionAndRotation(new Vector3(x,2.4f,-14),Quaternion.Euler(4,0,0));cam.fieldOfView=38;SaveCapture(cam,s.name+(x<10?"_Bedroom":"_Kitchen"));}
  }finally{world.Player.Position=player;world.Player.SyncVisuals(0);cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;cam.scene=cameraScene;if(follow)follow.enabled=on;}
 }
 static void SaveCapture(Camera cam,string name){
  var rt=RenderTexture.GetTemporary(1280,720,24,RenderTextureFormat.ARGBHalf);var prev=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,image=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1280,720,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1280,720),0,0);raw.Apply();var colors=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;image=new Texture2D(1280,720,TextureFormat.RGB24,false,true);image.SetPixels(colors);image.Apply();File.WriteAllBytes("Captures/FurnitureDepth/"+name+".png",image.EncodeToPNG());}
  finally{cam.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(image)Object.DestroyImmediate(image);}
 }
}
