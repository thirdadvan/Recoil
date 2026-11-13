using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Explicit authoring operation. Reuses the existing clothes, meshes and materials.
public static class ChangingClothesToWashBay
{
 const string Source="Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity";
 const string Target="Assets/Scenes/3D/RecoilFactoryInterior.unity";
 const string Marker="Ceiling suspended work clothes - moved from changing room";
 const string Output="output/wash-bay-clothes";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static string[] Collisions(Scene s){return All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();}
 [MenuItem("Recoil/Art/Move Changing Room Clothes To F2 Wash Bay")]
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
  Directory.CreateDirectory(Output);var previous=SceneManager.GetActiveScene();var source=SceneManager.GetSceneByPath(Source);var target=SceneManager.GetSceneByPath(Target);
  bool openedSource=!source.isLoaded,openedTarget=!target.isLoaded;
  if(openedSource)source=EditorSceneManager.OpenScene(Source,OpenSceneMode.Additive);
  if(openedTarget)target=EditorSceneManager.OpenScene(Target,OpenSceneMode.Additive);
  try
  {
   var bay=All<Transform>(target).Single(t=>t.name=="Tiled workers wash bay - reference1");
   if(bay.Find(Marker))return "Already relocated";
   var rail=All<Transform>(source).Single(t=>t.name=="Overhead changing rail");
   var names=new[]{"Overhead changing rail","Rusty hanging chain","Clothes hook","Work shirt on hanging hook"};
   var parts=rail.parent.Cast<Transform>().Where(t=>names.Contains(t.name)).ToArray();
   if(parts.Length!=26||parts.Count(t=>t.name=="Work shirt on hanging hook")!=15)throw new Exception("Expected five complete outfits with rail, chains and hooks");
   if(parts.Any(t=>t.GetComponentsInChildren<Collider>(true).Length+t.GetComponentsInChildren<Collider2D>(true).Length>0))throw new Exception("Clothing unexpectedly has gameplay colliders");
   var a=Collisions(source);var b=Collisions(target);
   var routes=All<RecoilDoor>(source).Concat(All<RecoilDoor>(target)).Select(d=>d.GetInstanceID()+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position.ToString("F6")).ToArray();
   string backup="Backups/WashBayClothes/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
   if(!EditorSceneManager.SaveScene(source,backup+"/ChangingRoom.unity",true)||!EditorSceneManager.SaveScene(target,backup+"/FactoryInterior.unity",true))throw new Exception("Backup failed");
   var ceiling=bay.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Tiled lintel").bounds;
   var wall=bay.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Tiled rear wall").bounds;
   // Leave the outfits forward of the wet shower fittings, with the rail below the lintel.
   Vector3 at=new Vector3(wall.center.x,ceiling.min.y-.10f,4.15f);
   Vector3 delta=at-rail.position;var group=new GameObject(Marker).transform;SceneManager.MoveGameObjectToScene(group.gameObject,target);group.SetParent(bay,true);group.position=at;
   foreach(var t in parts){t.SetParent(null,true);SceneManager.MoveGameObjectToScene(t.gameObject,target);t.SetParent(group,true);t.position+=delta;EditorUtility.SetDirty(t);}
   var cloth=group.GetComponentsInChildren<MeshRenderer>();
   foreach(var r in cloth)if(r.bounds.max.y>ceiling.min.y-.04f||r.bounds.min.x<wall.min.x+.03f||r.bounds.max.x>wall.max.x-.03f)throw new Exception("Clothes exceed ceiling/wall clearance");
   var fittings=bay.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Downward metal shower head"||r.name=="Wash bay enamel fluorescent housing"||r.name=="Wash bay luminous diffuser");
   foreach(var r in cloth)foreach(var f in fittings)if(r.bounds.Intersects(f.bounds))throw new Exception("Clothes intersect shower or lamp");
   Physics2D.SyncTransforms();
   if(!a.SequenceEqual(Collisions(source))||!b.SequenceEqual(Collisions(target)))throw new Exception("Gameplay collision changed");
   if(!routes.SequenceEqual(All<RecoilDoor>(source).Concat(All<RecoilDoor>(target)).Select(d=>d.GetInstanceID()+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position.ToString("F6"))))throw new Exception("Door routes changed");
   if(All<Transform>(source).Any(t=>names.Contains(t.name)))throw new Exception("Source clothing remains");
   EditorSceneManager.MarkSceneDirty(source);EditorSceneManager.MarkSceneDirty(target);
   if(!EditorSceneManager.SaveScene(source)||!EditorSceneManager.SaveScene(target))throw new Exception("Save failed");
   SceneManager.SetActiveScene(target);Capture(target,new Vector3(wall.center.x,9.25f,4.3f));
   string result="PASS: moved five outfits (15 cloth meshes), five chains, five hooks and one rail from changing room to F2 wash-bay dry zone. Rail position="+at+". Ceiling clearance and shower/lamp separation verified. All colliders and door routes unchanged. Existing shared meshes/materials reused; no permanent lights added. Backup: "+backup;
   File.WriteAllText(Output+"/verification.txt",result);return result;
  }
  finally{if(openedSource)EditorSceneManager.CloseScene(source,true);if(openedTarget)EditorSceneManager.CloseScene(target,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
 }
 static void Capture(Scene s,Vector3 centre)
 {
  var go=new GameObject("Temporary wash-bay inspection camera");SceneManager.MoveGameObjectToScene(go,s);var camera=go.AddComponent<Camera>();camera.scene=s;camera.orthographic=false;camera.fieldOfView=32;camera.aspect=16f/9;camera.transform.position=centre+new Vector3(-.5f,.8f,-13);camera.transform.LookAt(centre);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var rt=new RenderTexture(1440,810,24);var tex=new Texture2D(1440,810,TextureFormat.RGB24,false);var old=RenderTexture.active;
  try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1440,810),0,0);tex.Apply();File.WriteAllBytes(Output+"/F2-wash-bay.png",tex.EncodeToPNG());}
  finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(go);}
 }
}
