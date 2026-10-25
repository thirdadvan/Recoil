using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RunAngerAndShotUpdate
{
    const string Record="Backups/RunAngerAndMissedShot/20261023/Applied.txt";
    static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    [MenuItem("Recoil/Movement/Apply 1.5s Run Anger And Check Shots")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before saving scene tuning.");return;}
        if(!File.Exists(Record))
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var previous=SceneManager.GetActiveScene();int count=0;
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try{
                    SceneManager.SetActiveScene(scene);
                    var enemies=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RampagerController>(true)).ToArray();
                    if(enemies.Length==0)continue;
                    string backup="Backups/RunAngerAndMissedShot/20261023/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(path,backup,true);
                    foreach(var enemy in enemies){enemy.runningAngerTime=1.5f;EditorUtility.SetDirty(enemy);count++;}
                    EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
            SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();File.WriteAllText(Record,"Updated "+count+" Coalers: running anger time 1.5s.\n");
        }
        Verify();
        CoalerCoreVisualCheck.Capture();
    }
    static void Verify(){CoalerAcousticAIVerification.Verify();}
}
