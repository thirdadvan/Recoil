using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DefaultStealthRunUpdate
{
    const string Record="Backups/DefaultStealthRun/20261023/Applied.txt";
    [MenuItem("Recoil/Movement/Apply Default Stealth And Shift Run")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before updating scene tuning.");return;}
        if(File.Exists(Record)){Debug.Log("DEFAULT_STEALTH_ALREADY_APPLIED");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var original=SceneManager.GetActiveScene();int players=0,monsters=0,grabs=0,scenes=0;
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                SceneManager.SetActiveScene(scene);
                var components=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MonoBehaviour>(true)).Where(c=>c).ToArray();
                var actors=components.OfType<RecoilPlayerController>().ToArray();var enemies=components.OfType<RampagerController>().ToArray();var contacts=components.OfType<RampagerGrab>().ToArray();
                bool tips=components.Any(c=>c is PlayerDialogueTrigger||c is ServiceStoreOutdoorEncounter||c is ServiceStoreEncounter);
                if(actors.Length+enemies.Length+contacts.Length==0&&!tips)continue;
                string backup="Backups/DefaultStealthRun/20261023/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));File.Copy(path,backup,true);
                foreach(var player in actors){player.walkSpeed*=1.1f;player.sneakSpeed*=1.2f;EditorUtility.SetDirty(player);players++;}
                foreach(var enemy in enemies){enemy.runningAngerTime*=2;EditorUtility.SetDirty(enemy);monsters++;}
                foreach(var grab in contacts){grab.reach*=1.5f;EditorUtility.SetDirty(grab);grabs++;}
                foreach(var component in components)
                {
                    string field=component is PlayerDialogueTrigger?"message":component is ServiceStoreOutdoorEncounter?"stealthTutorial":component is ServiceStoreEncounter?"stealthHint":null;
                    if(field==null)continue;
                    var so=new SerializedObject(component);var property=so.FindProperty(field);string text=property.stringValue;
                    if(text.IndexOf("shift",StringComparison.OrdinalIgnoreCase)<0||text.IndexOf("stealth",StringComparison.OrdinalIgnoreCase)<0)continue;
                    property.stringValue="Use A/D to stealth walk. Hold SHIFT to run.";so.ApplyModifiedPropertiesWithoutUndo();
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);scenes++;
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(original);AssetDatabase.SaveAssets();
        string result="Players="+players+"; Coaler anger="+monsters+"; Fusion reach="+grabs+"; Scenes="+scenes;
        File.WriteAllText(Record,result+"\nDefault stealth 1.188 m/s; Shift run 2.772 m/s. Ordinary run anger time 1 s; standard fusion reach 2.025 m. Custom authored values scaled proportionally.\n");
        Debug.Log("DEFAULT_STEALTH_RUN_APPLIED: "+result);
    }
}
