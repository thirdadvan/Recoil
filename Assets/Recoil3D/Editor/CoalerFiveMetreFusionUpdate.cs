using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoalerFiveMetreFusionUpdate
{
    const string Folder="Backups/CoalerFiveMetreFusion/20261023/";
    [MenuItem("Recoil/AI/Apply Five Metre Fusion And Faster Roaming")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before applying scene tuning.");return;}
        if(File.Exists(Folder+"Applied.txt")){Debug.Log("COALER_FIVE_METRE_ALREADY_APPLIED");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=SceneManager.GetActiveScene();int scenes=0,enemies=0,grabs=0;
        try
        {
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
            {
                var path=AssetDatabase.GUIDToAssetPath(guid);var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    var controllers=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RampagerController>(true)).ToArray();
                    if(controllers.Length==0)continue;
                    var backup=Folder+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                    foreach(var enemy in controllers)
                    {
                        enemy.roamingSpeedMultiplier*=1.2f;EditorUtility.SetDirty(enemy);enemies++;
                        var grab=enemy.GetComponent<RampagerGrab>();if(!grab)grab=enemy.gameObject.AddComponent<RampagerGrab>();
                        grab.reach=5;EditorUtility.SetDirty(grab);grabs++;
                    }
                    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Could not save "+path);scenes++;
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"Applied.txt",$"Scenes={scenes}; ordinary Coalers={enemies}; fusion radii set to 5m={grabs}. Existing ordinary roaming multipliers increased 20%; Approach shares CurrentMoveSpeed. Attack and broadcast movement tuning retained. Fusion arms already use the actual target world position with no X/Z length clamp.\n");
            Debug.Log($"COALER_FIVE_METRE_APPLIED: scenes={scenes}, Coalers={enemies}, ranges={grabs}");
        }
        finally{SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();}
    }
}
