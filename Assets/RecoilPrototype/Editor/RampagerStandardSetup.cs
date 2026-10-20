using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RampagerStandardSetup
{
 static bool Missing(RampagerController e){return !e.GetComponent<RampagerPerception>()||!e.GetComponent<RampagerMovement>()||!e.GetComponent<RampagerAttack>()||!e.GetComponent<RampagerVisualEffects>()||!e.GetComponent<RampagerPatrolRoute>();}
 [MenuItem("Recoil/Rampager/Apply Standard Components To Existing Scenes")]
 public static string Apply()
 {
  if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");
  var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i).path).ToArray();
  string folder="Backups/RampagerStandard/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(folder);
  int count=0,changed=0;var report=new System.Text.StringBuilder();
  var paths=AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.IndexOf("Backup",StringComparison.OrdinalIgnoreCase)<0).ToArray();
  foreach(var path in paths)
  {
   var scene=SceneManager.GetSceneByPath(path);bool wasLoaded=scene.isLoaded;if(!wasLoaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   var enemies=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RampagerController>(true)).ToArray();var missing=enemies.Where(Missing).ToArray();count+=enemies.Length;
   if(missing.Length>0)
   {
    EditorSceneManager.SaveScene(scene,folder+"/"+scene.name+".unity",true);
    foreach(var enemy in missing)enemy.EnsureStandardComponents();
    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);changed+=missing.Length;
   }
   if(enemies.Length>0)report.AppendLine(path+" / actors="+enemies.Length+" / repaired="+missing.Length);
   if(!wasLoaded)EditorSceneManager.CloseScene(scene,true);
  }
  SceneManager.SetActiveScene(original);AssetDatabase.SaveAssets();
  string result="STANDARD_RAMPAGERS / checked="+count+" / repaired="+changed+"\n"+report;
  File.WriteAllText(folder+"/audit.txt",result);return result;
 }
}
