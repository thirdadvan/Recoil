using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RemoveLuminousLensPass
{
    [MenuItem("Recoil/Art Tests/Remove All Placeholder Luminous Lenses")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.isPlaying=false;EditorApplication.delayCall+=AfterPlay;return;
        }
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        string backup="Backups/RemoveLuminousLenses/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        var active=SceneManager.GetActiveScene();int removed=0,changed=0;
        var report=new System.Text.StringBuilder();
        try
        {
            foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/3D"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                if(path.Contains("/ArtTestBackups/") || !File.ReadAllText(path).Contains("m_Name: Luminous lens"))continue;
                string saved=Path.Combine(backup,path);Directory.CreateDirectory(Path.GetDirectoryName(saved));File.Copy(path,saved);
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    var roots=scene.GetRootGameObjects();int lightCount=roots.Sum(g=>g.GetComponentsInChildren<Light>(true).Length);
                    var targets=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Luminous lens").ToArray();
                    foreach(var t in targets)
                    {
                        if(t.GetComponentsInChildren<Light>(true).Length>0)throw new Exception("Unexpected light attached to lens: "+path);
                        UnityEngine.Object.DestroyImmediate(t.gameObject);
                    }
                    if(lightCount!=roots.Sum(g=>g.GetComponentsInChildren<Light>(true).Length))throw new Exception("Lighting count changed: "+path);
                    if(targets.Length>0){removed+=targets.Length;changed++;EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.AppendLine(path+": removed "+targets.Length+" meshes; "+lightCount+" lights retained.");}
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
        finally
        {
            if(active.IsValid()&&active.isLoaded){SceneManager.SetActiveScene(active);foreach(var root in active.GetRootGameObjects())foreach(var look in root.GetComponentsInChildren<WarehouseAtmosphereTest>(true))look.ApplyLook();}
        }
        report.Insert(0,"Removed "+removed+" placeholder lenses from "+changed+" scenes. Actual lights preserved.\n");
        File.WriteAllText(backup+"/Report.txt",report.ToString());Debug.Log("PLACEHOLDER_LENSES_REMOVED\n"+report);
    }
    static void AfterPlay()
    {if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=AfterPlay;return;}Apply();}
}
