using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class OutdoorCameraFramingPass
{
    const string Request="output/outdoor-camera/reduce.request";
    static OutdoorCameraFramingPass(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        File.Delete(Request);
        try{Apply();}catch(Exception e){File.WriteAllText("output/outdoor-camera/result.txt","FAILED\n"+e);Debug.LogException(e);}
    }
    static void Apply()
    {
        const string path="Assets/Scenes/3D/RecoilPrototype.unity";
        var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var follow=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilCameraController>(true)).Single(c=>c.outdoorHeightZoomEnabled);
            if(!follow.perspective3D)throw new Exception("Expected perspective outdoor camera");
            string backup="Backups/OutdoorCamera/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));
            if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
            float oldSize=follow.viewSize,oldMultiplier=follow.maximumHeightZoomMultiplier;
            float depth=Mathf.Max(1,Mathf.Abs(follow.cameraDepth));
            float oldFov=2*Mathf.Atan(oldSize/depth)*Mathf.Rad2Deg;
            float newFov=oldFov*.8f;
            Undo.RecordObject(follow,"Reduce outdoor FOV and height expansion");
            follow.viewSize=depth*Mathf.Tan(newFov*Mathf.Deg2Rad*.5f);
            follow.maximumHeightZoomMultiplier=1+(oldMultiplier-1)*.8f;
            var camera=follow.GetComponent<Camera>();Undo.RecordObject(camera,"Update outdoor lens");
            camera.orthographicSize=follow.viewSize;camera.fieldOfView=newFov;
            EditorUtility.SetDirty(follow);EditorUtility.SetDirty(camera);
            for(int i=0;i<=10;i++)
            {
                float t=i/10f;float y=Mathf.Lerp(follow.heightZoomStartY,follow.heightZoomMaximumY,t);
                float expected=follow.viewSize*(1+(oldMultiplier-1)*.8f*t);
                if(Mathf.Abs(follow.ViewSizeAtHeight(y)-expected)>.0001f)throw new Exception("Height framing verification failed");
            }
            if(Mathf.Abs((2*Mathf.Atan(follow.viewSize/depth)*Mathf.Rad2Deg)/oldFov-.8f)>.0001f)throw new Exception("FOV verification failed");
            EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");
            File.WriteAllText("output/outdoor-camera/result.txt","PASS\nBase FOV: "+oldFov+" -> "+newFov+" degrees\nView size: "+oldSize+" -> "+follow.viewSize+"\nHeight multiplier: "+oldMultiplier+" -> "+follow.maximumHeightZoomMultiplier+"\n11 height samples verified; indoor scenes unchanged.\nBackup: "+backup);
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
    }
}
