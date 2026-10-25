using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoalerFusionReach35Update
{
    const string Folder="Backups/CoalerFusionReach35/20261023/";
    [MenuItem("Recoil/AI/Set Fusion Tentacle Reach To 3.5 Metres")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before saving reach tuning.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=SceneManager.GetActiveScene();int count=0,scenes=0;
        try
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var contacts=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RampagerGrab>(true)).ToArray();
                    if(contacts.Length==0)continue;
                    var backup=Folder+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                    foreach(var contact in contacts){contact.reach=3.5f;EditorUtility.SetDirty(contact);count++;}
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save "+path);scenes++;
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Applied.txt",$"Fusion tentacle/contact range = 3.5 metres. Components={count}, scenes={scenes}. Tentacles follow the actual target position; movement tuning unchanged.\n");
            Debug.Log($"COALER_FUSION_REACH_35_APPLIED: {count} components in {scenes} scenes");
        }
        finally{SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();}
    }
}
