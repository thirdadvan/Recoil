using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FactoryWarehouseValidation
{
 static T Find<T>(UnityEngine.SceneManagement.Scene s) where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).First();}
 static void Require(bool v,string message){if(!v)throw new Exception(message);}
 static Texture2D Capture(string path,Vector2 center,float size,bool layout,string file,Vector2? playerPosition=null)
 {
  var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var world=Find<RecoilPrototype>(scene);
  typeof(RecoilPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(world,null);
  if(playerPosition.HasValue)world.Player.ResetController(playerPosition.Value);
  var camera=Find<Camera>(scene);var controller=camera.GetComponent<RecoilCameraController>();controller.enabled=false;
  camera.orthographic=true;camera.orthographicSize=size;camera.aspect=1.6f;camera.transform.position=new Vector3(center.x,center.y,-10);
  var lighting=Find<RecoilLighting2D>(scene);var shadows=Find<RecoilSightOcclusion>(scene);
  var overlay=lighting.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Darkness Layer");
  if(layout){lighting.enabled=false;overlay.enabled=false;shadows.enabled=false;}
  else{lighting.Refresh();shadows.Configure(world,world.Player.transform,overlay);typeof(RecoilSightOcclusion).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(shadows,null);}
  var rt=new RenderTexture(1600,1000,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
  var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();
  Directory.CreateDirectory("output/level-previews");File.WriteAllBytes("output/level-previews/"+file,tex.EncodeToPNG());
  camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);return tex;
 }
 public static void ValidateAndCapture()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
  foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled))
  {
   var checkedScene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);
   var roots=checkedScene.GetRootGameObjects();
   foreach(var root in roots)foreach(var t in root.GetComponentsInChildren<Transform>(true))Require(!t.GetComponents<Component>().Any(c=>c==null),"Missing script: "+t.name);
   var checkedWorld=roots.SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).FirstOrDefault();if(!checkedWorld)continue;
   Require(checkedWorld.GetComponentInChildren<RecoilSightOcclusion>(),"Missing sight shadows: "+entry.path);
   foreach(var d in checkedWorld.GetComponentsInChildren<RecoilDoor>())if(!string.IsNullOrEmpty(d.destinationScene))
   {
    Require(EditorBuildSettings.scenes.Any(e=>e.enabled&&e.path==d.destinationScene),"Unregistered door: "+d.destinationScene);
    if(!string.IsNullOrEmpty(d.destinationSpawn))Require(File.ReadAllText(d.destinationScene).Contains("m_Name: "+d.destinationSpawn),"Missing destination spawn: "+d.destinationSpawn);
   }
  }
  UnityEngine.Object.DestroyImmediate(Capture("Assets/Scenes/RecoilFactoryWarehouse.unity",new Vector2(15,3.6f),10,true,"warehouse-layout.png"));
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var world=Find<RecoilPrototype>(scene);
  Require(world.Clear(new Vector2(23,1),new Vector2(27,1)),"Warehouse corridor still has a wall across the door");
  Require(!world.Clear(new Vector2(29,1),new Vector2(31,1)),"Hard side wall must still block sight");
  var roof=world.GetComponentsInChildren<BoxCollider2D>().First(c=>c.name=="Ceiling");
  Require(roof.enabled && !roof.isTrigger && !RecoilSightOcclusion.IsBlocker(roof),"Hard roof collision/sight flags are wrong");
  Require(world.Clear(new Vector2(10,6.8f),new Vector2(10,8)),"Hard roof blocked sight");
  var door=world.GetComponentsInChildren<RecoilDoor>().First(d=>d.requiredItemId=="warehouse_locked_room_key");
  Require(!door.openingBarrier && door.destinationScene=="Assets/Scenes/RecoilWarehouseStoreroom.unity","Storeroom door must use an independent room");
  var image=Capture("Assets/Scenes/RecoilFactoryWarehouse.unity",new Vector2(26.5f,2.2f),3.2f,false,"warehouse-door-corrected.png",new Vector2(25.1f,.845f));
  foreach(float x in new[]{26.05f,26.95f})
  {
   var pixel=image.GetPixel((int)((x-26.5f)/10.24f*1600+800),(int)((1-2.2f)/6.4f*1000+500));
   Require(pixel.r+pixel.g+pixel.b>.03f,"Door was cut off by shadow at x="+x);
  }
  UnityEngine.Object.DestroyImmediate(image);
  UnityEngine.Object.DestroyImmediate(Capture("Assets/Scenes/RecoilFactorySewer.unity",new Vector2(17,1.6f),11,true,"factory-sewer-layout.png"));
  UnityEngine.Object.DestroyImmediate(Capture("Assets/Scenes/RecoilPrototype.unity",new Vector2(85,6),19,true,"factory-yard-layout.png"));
  scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();world=Find<RecoilPrototype>(scene);
  var soft=world.GetComponentsInChildren<SoftRoofPlatform>(true).First();Require(!RecoilSightOcclusion.IsBlocker(soft.GetComponent<BoxCollider2D>()),"Soft platform casts sight shadow");
  Debug.Log("WAREHOUSE_VALIDATION_COMPLETE: scene links, missing scripts, hard roof sight/collision separation, soft platform exclusion, complete rendered door verified. No scenes saved.");
 }
}
