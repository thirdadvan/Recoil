using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class Recoil3DReview
{
    static string[] Paths {get{return EditorBuildSettings.scenes.Where(s=>s.enabled && s.path.StartsWith(Recoil3DScene.Folder)).Select(s=>s.path).ToArray();}}
    public static string RefineAndValidate()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");var previous=SceneManager.GetActiveScene();int meshes=0,doors=0;
        foreach(string path in Paths)
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
                var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
                var legacy=new HashSet<Transform>();
                foreach(var t in all.Where(t=>t.GetComponent<SpriteRenderer>() || t.GetComponent<BoxCollider2D>() || t.GetComponent<RecoilLight2D>() || t.GetComponent<RecoilStreetLamp>()))
                    for(var p=t;p && p!=world.transform;p=p.parent)legacy.Add(p);
                foreach(var t in legacy){var p=t.localPosition;p.z=0;t.localPosition=p;}
                foreach(var actor in world.GetComponentsInChildren<Recoil3DActorVisual>(true))
                {
                    actor.model.localRotation=Quaternion.Euler(0,actor.monster?90:-90,0);
                    var torso=actor.model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Torso");var scale=torso.localScale;scale.y=.325f;torso.localScale=scale;
                    foreach(var sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))sprite.forceRenderingOff=true;
                }
                foreach(var bridge in world.GetComponentsInChildren<Recoil3DSpriteVisual>(true))bridge.Sync();
                var camera=world.ViewCamera;if(!camera){var serial=new SerializedObject(world);camera=(Camera)serial.FindProperty("cam").objectReferenceValue;}
                var follow=camera.GetComponent<RecoilCameraController>();follow.perspective3D=true;follow.cameraDepth=-24;
                camera.orthographic=false;camera.fieldOfView=2*Mathf.Atan(follow.viewSize/24)*Mathf.Rad2Deg;camera.transform.position=new Vector3(world.Player.transform.position.x+follow.followOffset.x,world.Player.transform.position.y+follow.followOffset.y,-24);
                if(!world.GetComponent<Recoil3DScene>())throw new Exception("Missing scene identity "+path);
                foreach(var d in world.GetComponentsInChildren<RecoilDoor>(true))
                {if(!string.IsNullOrEmpty(d.destinationScene) && (!d.destinationScene.StartsWith(Recoil3DScene.Folder) || !File.Exists(d.destinationScene)))throw new Exception("Door leaves 3D campaign: "+d.name);doors++;}
                foreach(var t in all)if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)throw new Exception("Missing script: "+path+" / "+t.name);
                if(camera.orthographic)throw new Exception("Camera is not perspective.");
                var target=new Vector3(world.Player.transform.position.x,1,0);var ray=camera.ScreenPointToRay(camera.WorldToScreenPoint(target));float enter;
                if(!new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out enter) || Vector3.Distance(ray.GetPoint(enter),target)>.002f)throw new Exception("Perspective aim does not hit gameplay plane.");
                meshes+=world.GetComponentsInChildren<MeshRenderer>(true).Length;
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(previous);return Paths.Length+" scenes, "+meshes+" native mesh renderers, "+doors+" connected doors; scripts, perspective aiming and scene references valid.";
    }
    public static string Capture(string path,string file,Vector3 position,float view)
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(scene);
        Camera camera=null;Vector3 oldPosition=Vector3.zero;Quaternion oldRotation=Quaternion.identity;float oldFov=0;RenderTexture oldTarget=null;Scene oldScene=default(Scene);RenderTexture rt=null;Texture2D output=null;
        try
        {
            camera=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).First();oldPosition=camera.transform.position;oldRotation=camera.transform.rotation;oldFov=camera.fieldOfView;oldTarget=camera.targetTexture;oldScene=camera.scene;
            foreach(var bridge in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Recoil3DSpriteVisual>(true)))bridge.Sync();
            foreach(var light in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Recoil3DLight>(true)))light.Sync();
            camera.scene=scene;camera.transform.position=position;camera.orthographic=false;camera.transform.rotation=Quaternion.Euler(5,-5,0);camera.fieldOfView=2*Mathf.Atan(view/24)*Mathf.Rad2Deg;camera.aspect=16f/9;
            rt=new RenderTexture(1600,900,24);camera.targetTexture=rt;camera.Render();var active=RenderTexture.active;RenderTexture.active=rt;
            output=new Texture2D(1600,900,TextureFormat.RGB24,false);output.ReadPixels(new Rect(0,0,1600,900),0,0);output.Apply();RenderTexture.active=active;
            Directory.CreateDirectory("Captures/3D-Migration");File.WriteAllBytes("Captures/3D-Migration/"+file,output.EncodeToPNG());return file;
        }
        finally
        {if(camera){camera.targetTexture=oldTarget;camera.transform.position=oldPosition;camera.transform.rotation=oldRotation;camera.fieldOfView=oldFov;camera.scene=oldScene;}if(rt)UnityEngine.Object.DestroyImmediate(rt);if(output)UnityEngine.Object.DestroyImmediate(output);if(added)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
}
