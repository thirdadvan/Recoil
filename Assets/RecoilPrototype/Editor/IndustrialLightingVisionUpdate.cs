using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class IndustrialLightingVisionUpdate
{
 const string Marker="ProjectSettings/RecoilIndustrialVisionUpdate.txt";
 [MenuItem("Recoil/Levels/Apply Industrial Lamps and 80 Percent Vision")]
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
  if(File.Exists(Marker))return "Already applied; no repeat scaling.\n"+File.ReadAllText(Marker);
  var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  string backup="Backups/IndustrialLightingVision/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
  var report=new System.Text.StringBuilder();int actors=0,lamps=0;
  foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.IndexOf("Backup",StringComparison.OrdinalIgnoreCase)<0))
  {
   var scene=SceneManager.GetSceneByPath(path);bool wasLoaded=scene.isLoaded;if(!wasLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   var enemies=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RampagerController>(true)).ToArray();bool warehouse=path=="Assets/Scenes/RecoilFactoryWarehouse.unity";
   if(enemies.Length>0||warehouse)
   {
    EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+".unity",true);
    foreach(var enemy in enemies){float before=enemy.idealSightRange;enemy.idealSightRange=before*.8f;enemy.sightPitchOffset=-10;EditorUtility.SetDirty(enemy);EditorUtility.SetDirty(enemy.GetComponent<RampagerPerception>());actors++;report.AppendLine(scene.name+" / "+enemy.name+" / "+before+" -> "+enemy.idealSightRange);}
    if(warehouse)
    {
     var oldLights=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilLight2D>(true)).Where(l=>!(l is RecoilIndustrialLamp)).ToArray();
     foreach(var old in oldLights)
     {
      var go=old.gameObject;float intensity=old.intensity,radius=old.radius;bool enabled=old.enabled;
      UnityEngine.Object.DestroyImmediate(old);var lamp=go.AddComponent<RecoilIndustrialLamp>();lamp.intensity=intensity;lamp.radius=radius;lamp.coneLength=Mathf.Max(2,go.transform.position.y+1.4f);lamp.coneHalfWidth=Mathf.Min(2.2f,lamp.coneLength*.275f);lamp.edgeSoftness=.25f;lamp.scatterIntensity=.18f;lamp.enabled=enabled;lamps++;
      if(lamps==1)PrefabUtility.SaveAsPrefabAsset(go,"Assets/RecoilPrototype/Prefabs/IndustrialSpotLamp.prefab");
     }
    }
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
   }
   if(!wasLoaded)EditorSceneManager.CloseScene(scene,true);
  }
  // Update any saved actor prefabs as well, preserving their separate authored values.
  foreach(var path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/RecoilPrototype"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset||asset.GetComponentsInChildren<RampagerController>(true).Length==0)continue;
   File.Copy(path,backup+"/"+Path.GetFileName(path),true);var root=PrefabUtility.LoadPrefabContents(path);
   try{foreach(var enemy in root.GetComponentsInChildren<RampagerController>(true)){enemy.idealSightRange*=.8f;enemy.sightPitchOffset=-10;actors++;}PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
  }
  SceneManager.SetActiveScene(original);AssetDatabase.SaveAssets();string result="Applied / monsters="+actors+" / industrial lamps="+lamps+"\n"+report;
  File.WriteAllText(Marker,result);File.WriteAllText(backup+"/audit.txt",result);return result;
 }
}
