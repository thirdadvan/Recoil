using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StairwellBackgroundBuilder
{
 const string Path="Assets/Scenes/RecoilFactoryInterior.unity";
 [MenuItem("Recoil/Levels/Add Interior Stairwell Background")]
 public static void Build()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing the stairwell background.");
  var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(Path);bool loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
  var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();var stairs=w.GetComponentsInChildren<WalkableStairway>().OrderBy(s=>s.routePoints[0].position.y).ToArray();var stairwell=stairs[0].transform.parent;
  if(stairwell.Find("Stairwell background - editable"))throw new Exception("Background already exists. Edit the saved background object.");
  Directory.CreateDirectory("Backups/StairwellBackground");EditorSceneManager.SaveScene(scene,"Backups/StairwellBackground/Interior_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);SceneManager.SetActiveScene(scene);
  var serial=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  float left=stairs.SelectMany(s=>s.routePoints).Min(p=>p.position.x),right=stairs.SelectMany(s=>s.routePoints).Max(p=>p.position.x)+1.2f;
  Physics2D.SyncTransforms();float top=w.GetComponentsInChildren<BoxCollider2D>().First(c=>c.name=="Low Factory Ceiling").bounds.min.y;
  var root=FactoryWarehouseBuilder.G("Stairwell background - editable",stairwell,new Vector2(left,0));
  FactoryWarehouseBuilder.B("Aged stairwell back wall",root,new Vector2((left+right)/2,top/2),new Vector2(right-left,top),0x414d43,-9);
  foreach(var stair in stairs)
  {
   float y=stair.routePoints[0].position.y;
   FactoryWarehouseBuilder.B("Green painted lower wall",root,new Vector2((left+right)/2,y+.68f),new Vector2(right-left,1.36f),0x293e33,-8);
   FactoryWarehouseBuilder.B("Storey wall band",root,new Vector2((left+right)/2,y+.055f),new Vector2(right-left,.11f),0x677366,-7);
   FactoryWarehouseBuilder.B("Wall paint seam",root,new Vector2((left+right)/2,y+1.38f),new Vector2(right-left,.035f),0x657361,-7);
   FactoryWarehouseBuilder.B("Conduit branch",root,new Vector2(right-.64f,y+2.1f),new Vector2(.78f,.055f),0x687461,-6);
  }
  foreach(float x in new[]{left+.13f,right-.16f})FactoryWarehouseBuilder.B("Concrete stairwell edge pier",root,new Vector2(x,top/2),new Vector2(.22f,top),0x5b675a,-6);
  FactoryWarehouseBuilder.B("Vertical service conduit",root,new Vector2(right-.27f,top/2),new Vector2(.065f,top-.4f),0x78806a,-6);
  for(int i=0;i<12;i++){float y=.8f+i*1.5f;if(y>top-.4f)break;FactoryWarehouseBuilder.B("Conduit bracket",root,new Vector2(right-.27f,y),new Vector2(.16f,.045f),0x394d3e,-5);}
  for(int i=0;i<8;i++){float x=left+.9f+(i%3)*1.5f,y=2.3f+i*1.9f;if(y>top-.7f)break;FactoryWarehouseBuilder.B("Damp plaster patch",root,new Vector2(x,y),new Vector2(.65f+(i%2)*.35f,.16f+(i%3)*.12f),0x39493e,-8);}
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
  if(!loaded)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);
  Directory.CreateDirectory("output/level-previews");File.WriteAllText("output/level-previews/stairwell-background-result.txt","STAIRWELL_BACKGROUND_COMPLETE: background-only, no collision or lights.");Debug.Log("STAIRWELL_BACKGROUND_COMPLETE");
 }
 [MenuItem("Recoil/Levels/Add Stairwell Floor Numbers")]
 public static void AddFloorNumbers()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing floor signs.");
  var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(Path);bool loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
  var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();var stairs=w.GetComponentsInChildren<WalkableStairway>().OrderBy(s=>s.routePoints[0].position.y).ToArray();
  var background=stairs[0].transform.parent.Find("Stairwell background - editable");if(!background)throw new Exception("Create the stairwell background first.");
  if(background.Find("Floor numbers - ground floor is 1"))throw new Exception("Floor signs already exist; edit their saved objects.");
  Directory.CreateDirectory("Backups/StairwellBackground");EditorSceneManager.SaveScene(scene,"Backups/StairwellBackground/FloorSigns_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);SceneManager.SetActiveScene(scene);
  var serial=new SerializedObject(w);FactoryWarehouseBuilder.UseArt((Sprite)serial.FindProperty("sceneSprite").objectReferenceValue,(Material)serial.FindProperty("sceneMaterial").objectReferenceValue);
  var root=FactoryWarehouseBuilder.G("Floor numbers - ground floor is 1",background,background.position);
  var levels=stairs.Select(s=>s.routePoints[0].position.y).Concat(new[]{stairs.Last().routePoints.Last().position.y}).ToArray();
  float x=stairs[0].routePoints[0].position.x+.95f;
  for(int i=0;i<levels.Length;i++)
  {
   var sign=FactoryWarehouseBuilder.G("Floor "+(i+1)+" sign",root,new Vector2(x,levels[i]+2.05f));
   FactoryWarehouseBuilder.B("Aged sign plate",sign,sign.position,new Vector2(1.1f,1.4f),0x536052,-6);
   FactoryWarehouseBuilder.B("Sign top border",sign,(Vector2)sign.position+Vector2.up*.7f,new Vector2(1.1f,.035f),0x919580,-5);
   var number=FactoryWarehouseBuilder.G("Floor number",sign,(Vector2)sign.position-Vector2.up*.13f);var text=number.gameObject.AddComponent<TextMesh>();text.text=(i+1).ToString();text.fontSize=96;text.characterSize=.23f;text.anchor=TextAnchor.MiddleCenter;text.color=RecoilPrototype.Hex(0xc3c2a7);text.GetComponent<MeshRenderer>().sortingOrder=-4;
   var heading=FactoryWarehouseBuilder.G("FLOOR",sign,(Vector2)sign.position+Vector2.up*.48f);var label=heading.gameObject.AddComponent<TextMesh>();label.text="FLOOR";label.fontSize=48;label.characterSize=.047f;label.anchor=TextAnchor.MiddleCenter;label.color=RecoilPrototype.Hex(0xb2b49b);label.GetComponent<MeshRenderer>().sortingOrder=-4;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();if(!loaded)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(original);
  File.WriteAllText("output/level-previews/stairwell-floor-signs-result.txt","STAIRWELL_FLOOR_SIGNS_COMPLETE: 1, 2, 3, 4. Ground floor is 1.");Debug.Log("STAIRWELL_FLOOR_SIGNS_COMPLETE");
 }

}
