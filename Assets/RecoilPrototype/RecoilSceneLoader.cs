using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor play can load authored scene assets even before its in-memory build list refreshes.
public static class RecoilSceneLoader
{
    static string LoadName(string scene)
    {string path=scene.Trim().Replace('\\','/');return path.EndsWith(".unity",StringComparison.OrdinalIgnoreCase)?path.Substring(0,path.Length-6):path;}
    static int BuildIndex(string scene)
    {
        string name=LoadName(scene);
        for(int i=0;i<SceneManager.sceneCountInBuildSettings;i++)
        {
            string path=LoadName(SceneUtility.GetScenePathByBuildIndex(i));
            string shortName=path.Substring(path.LastIndexOf('/')+1);
            if(string.Equals(path,name,StringComparison.OrdinalIgnoreCase) || string.Equals(shortName,name,StringComparison.OrdinalIgnoreCase))return i;
        }
        return -1;
    }
#if UNITY_EDITOR
    static string EditorAssetPath(string scene)
    {
        string path=LoadName(scene)+".unity";
        return path.StartsWith("Assets/",StringComparison.OrdinalIgnoreCase) && UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path)?path:null;
    }
#endif
    public static bool CanLoad(string scene)
    {
        if(string.IsNullOrWhiteSpace(scene))return false;
#if UNITY_EDITOR
        if(UnityEditor.EditorApplication.isPlaying && EditorAssetPath(scene)!=null)return true;
#endif
        return BuildIndex(scene)>=0;
    }
    public static void Load(string scene)
    {
#if UNITY_EDITOR
        string assetPath=EditorAssetPath(scene);
        if(UnityEditor.EditorApplication.isPlaying && assetPath!=null)
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(assetPath,new LoadSceneParameters(LoadSceneMode.Single));return;
        }
#endif
        int index=BuildIndex(scene);
        if(index<0)throw new InvalidOperationException("Scene is not enabled in Build Settings: "+scene);
        SceneManager.LoadScene(index,LoadSceneMode.Single);
    }
}
