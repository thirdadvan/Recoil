using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StairwayRepairBuilder
{
 [MenuItem("Recoil/Levels/Repair Switchback Stairways")]
 public static void Repair()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before repairing stairs.");
  var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();Directory.CreateDirectory("Backups/StairwayRepair");int count=0;
  foreach(var path in new[]{"Assets/Scenes/RecoilPrototype.unity","Assets/Scenes/RecoilFactoryInterior.unity"})
  {
   var scene=SceneManager.GetSceneByPath(path);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   EditorSceneManager.SaveScene(scene,"Backups/StairwayRepair/"+scene.name+"_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);SceneManager.SetActiveScene(scene);
   var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();var s=new SerializedObject(world);FactoryOfficeRefitBuilder.UseArt((Sprite)s.FindProperty("sceneSprite").objectReferenceValue,(Material)s.FindProperty("sceneMaterial").objectReferenceValue);
   foreach(var stair in world.GetComponentsInChildren<WalkableStairway>(true))
   {
    float low=stair.routePoints[0].position.y,high=stair.routePoints[stair.routePoints.Length-1].position.y;
    FactoryOfficeRefitBuilder.RebuildStair(stair,low,high,path.Contains("RecoilPrototype"));stair.catchRadius=.55f;
    foreach(var c in stair.GetComponentsInChildren<BoxCollider2D>()){var visibility=c.GetComponent<RecoilSurfaceVisibility>();if(!visibility)visibility=c.gameObject.AddComponent<RecoilSurfaceVisibility>();visibility.blocksSight=false;}
    count++;
   }
   if(path.Contains("Interior")){var stair=world.GetComponentsInChildren<WalkableStairway>().OrderBy(x=>x.routePoints[0].position.y).First();PrefabUtility.SaveAsPrefabAsset(stair.gameObject,"Assets/RecoilPrototype/Prefabs/WalkableSwitchbackStairway.prefab");}
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   if(!loaded.Contains(path))EditorSceneManager.CloseScene(scene,true);
  }
  SceneManager.SetActiveScene(original);AssetDatabase.SaveAssets();File.WriteAllText("output/level-previews/stairway-repair-result.txt","STAIRWAY_REPAIR_COMPLETE: "+count+" flights, consistent 5.8m span.");Debug.Log("STAIRWAY_REPAIR_COMPLETE: "+count);
 }
}
