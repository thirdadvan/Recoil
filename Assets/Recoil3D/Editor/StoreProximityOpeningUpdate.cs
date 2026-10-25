using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoreProximityOpeningUpdate
{
    const string Folder="Backups/StoreProximityOpening/20261023/";
    [MenuItem("Recoil/Story/Apply Store Proximity Opening")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before saving story content.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=SceneManager.GetActiveScene();int count=0;
        try
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);if(!File.ReadAllText(path).Contains("store_outdoor_warning_heard"))continue;
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var stories=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ServiceStoreOutdoorEncounter>(true)).ToArray();if(stories.Length==0)continue;
                    string backup=Folder+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                    foreach(var story in stories)
                    {
                        story.stealthTutorial=ServiceStoreOutdoorEncounter.RunTutorial;EditorUtility.SetDirty(story);
                        if(!story.warning)throw new Exception("Outdoor warning dialogue is missing");
                        story.warning.lines=new[]{new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineOne},new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineTwo}};
                        EditorUtility.SetDirty(story.warning);count++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save "+path);
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            AssetDatabase.SaveAssets();Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Applied.txt",$"Updated {count} outdoor opening(s). Trigger: the first Coaler watches Dawn through close perception. No sound subscription or linked-monster commands. New two-line English warning, followed by Shift/run-noise tutorial. Existing completion flag retained.\n");
            Debug.Log("STORE_PROXIMITY_OPENING_APPLIED: "+count);
        }
        finally{SceneManager.SetActiveScene(previous);}
    }
}
