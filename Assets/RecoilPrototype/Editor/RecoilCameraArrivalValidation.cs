using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Opens authored scenes for checks only; never saves or changes their camera settings.
public static class RecoilCameraArrivalValidation
{
    public static void Validate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play before validating camera arrivals.");
        for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++)
            if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save your scene edits before validating camera arrivals.");
        var targetMethod=typeof(RecoilCameraController).GetMethod("TargetPosition",BindingFlags.Instance|BindingFlags.NonPublic);
        var mouseMethod=typeof(RecoilCameraController).GetMethod("MouseOffset",BindingFlags.Instance|BindingFlags.NonPublic);
        int count=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
        {
            var scene=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Single);
            var roots=scene.GetRootGameObjects();
            var controller=roots.SelectMany(r=>r.GetComponentsInChildren<RecoilCameraController>()).FirstOrDefault();
            var world=roots.SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).FirstOrDefault();
            if(!controller || !world)continue;
            var camera=controller.GetComponent<Camera>();
            var markers=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("Spawn") || t.name.StartsWith("Room Return - ")).ToArray();
            var probe=new GameObject("Camera arrival validation target");controller.target=probe.transform;controller.Initialize(world,probe.transform);
            foreach(var marker in markers)
            {
                probe.transform.position=marker.position;
                controller.transform.position=new Vector3(-90,70,controller.cameraDepth);
                controller.SnapToTarget();var first=controller.transform.position;
                var desired=(Vector3)targetMethod.Invoke(controller,new object[]{true});
                Require(Vector3.Distance(first,desired)<.001f,"Post-arrival drift: "+entry.path+" / "+marker.name);
                controller.transform.position=new Vector3(90,-70,controller.cameraDepth);
                controller.SnapToTarget();
                Require(Vector3.Distance(first,controller.transform.position)<.001f,"Arrival depends on previous camera position: "+marker.name);
                var rect=camera.pixelRect;
                var pointer=new Vector3(rect.x+rect.width*.85f,rect.y+rect.height*.15f,0);
                var offset=(Vector2)mouseMethod.Invoke(controller,new object[]{pointer});
                controller.transform.position+=new Vector3(25,-25,0);
                var other=(Vector2)mouseMethod.Invoke(controller,new object[]{pointer});
                Require(Vector2.Distance(offset,other)<.001f,"Mouse feedback after arrival: "+marker.name);
                Require(offset.magnitude<=controller.maximumMouseOffset+.001f,"Maximum mouse offset exceeded");
                if(controller.limitDownwardMouseOffset)Require(offset.y>=-controller.maximumDownwardMouseOffset-.001f,"Downward limit exceeded");
                count++;
            }
            UnityEngine.Object.DestroyImmediate(probe);
        }
        Require(count>0,"No arrival markers tested");
        Debug.Log("CAMERA_ARRIVAL_VALIDATION_COMPLETE: "+count+" arrival markers; immediate framing, stable mouse offset, limits respected. No scenes saved.");
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
}
