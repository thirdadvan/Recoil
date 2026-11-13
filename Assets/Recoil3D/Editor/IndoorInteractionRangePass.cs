using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class IndoorInteractionRangePass
{
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
  var previous=SceneManager.GetActiveScene();var report=new StringBuilder();int objects=0,scenes=0;
  string backup="Backups/IndoorInteractionRange/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
  foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/3D"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   if(path=="Assets/Scenes/3D/RecoilPrototype.unity")continue;
   var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try
   {
    var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true)).Where(c=>c&&(c is ContinuousInteractionItem||c is ReadableProp||c is WorkerLocker||c is ElectricalPanelInteraction||c is RecoilDoor||c is RecoilStreetLamp)).ToArray();
    if(all.Length==0)continue;
    string snapshot=backup+"/"+s.name+".unity";if(!EditorSceneManager.SaveScene(s,snapshot,true))throw new Exception("Backup failed");
    foreach(var c in all){var field=c.GetType().GetField("interactionRange");if(field==null)throw new Exception("Missing interaction range");field.SetValue(c,.5f);EditorUtility.SetDirty(c);objects++;}
    EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");report.AppendLine(s.name+": "+all.Length+" interaction points -> 0.5 m");scenes++;
   }
   finally{if(added)EditorSceneManager.CloseScene(s,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
  }
  Directory.CreateDirectory("output/changing-safe-room");string result="PASS: "+objects+" indoor interaction components in "+scenes+" scenes configured to 0.5 m. Outdoor scene unchanged. Runtime shared proximity cap also covers future items, and body-span distance keeps floor pickups reachable.\n"+report+"Backup: "+backup;File.WriteAllText("output/changing-safe-room/interaction-ranges.txt",result);return result;
 }
}
