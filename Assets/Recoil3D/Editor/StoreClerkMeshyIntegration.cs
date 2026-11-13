using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class StoreClerkMeshyIntegration
{
    const string Folder="Assets/Art/Characters/StoreClerkMeshy";
    const string ScenePath="Assets/Scenes/3D/RecoilServiceStore.unity";
    static void Need(bool condition,string reason){if(!condition)throw new Exception(reason);}
    [MenuItem("Recoil/Store Clerk/Install Meshy clerk")]
    public static void Apply()
    {
        Need(!EditorApplication.isPlaying,"Stop Play before replacing the clerk");
        Directory.CreateDirectory("Captures/StoreClerk");
        var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Clerk.fbx");
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.isReadable=true;importer.importAnimation=false;importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Clerk.fbx");
        var temp=UnityEngine.Object.Instantiate(source);var animator=temp.GetComponent<Animator>();
        Need(animator&&animator.avatar.isValid&&animator.avatar.isHuman,"Invalid clerk humanoid avatar");
        var handler=new HumanPoseHandler(animator.avatar,temp.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);handler.Dispose();
        var names=HumanTrait.MuscleName;var neutral=(float[])pose.muscles.Clone();
        Action<string,float> set=(n,v)=>{int i=Array.IndexOf(names,n);if(i>=0)neutral[i]=v;};
        foreach(string side in new[]{"Left","Right"}){set(side+" Arm Down-Up",-.82f);set(side+" Arm Front-Back",.05f);set(side+" Forearm Stretch",.65f);}
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/ClerkIdle.anim");
        if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Folder+"/ClerkIdle.anim");}
        clip.ClearCurves();clip.frameRate=30;clip.name="ClerkIdle";
        for(int i=0;i<names.Length;i++)
        {
            var curve=new AnimationCurve();for(int k=0;k<=90;k++){float t=k/30f;float v=neutral[i];if(names[i]=="Chest Front-Back")v+=Mathf.Sin(t*Mathf.PI*2/3)*.012f;curve.AddKey(t,v);}
            clip.SetCurve("",typeof(Animator),names[i],curve);
        }
        foreach(var item in new[]{("RootT.x",0f),("RootT.y",pose.bodyPosition.y),("RootT.z",0f),("RootQ.x",pose.bodyRotation.x),("RootQ.y",pose.bodyRotation.y),("RootQ.z",pose.bodyRotation.z),("RootQ.w",pose.bodyRotation.w)})clip.SetCurve("",typeof(Animator),item.Item1,AnimationCurve.Constant(0,3,item.Item2));
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalOrientation=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
        UnityEngine.Object.DestroyImmediate(temp);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Clerk.controller");
        if(!controller){controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Clerk.controller");var state=controller.layers[0].stateMachine.AddState("Counter idle");state.motion=clip;controller.layers[0].stateMachine.defaultState=state;}
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Clerk.mat");if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Folder+"/Clerk.mat");}
        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Clerk_BaseColor.png");Need(material.mainTexture,"Missing clerk texture");material.color=Color.white;material.SetFloat("_Glossiness",0);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();
        var previous=SceneManager.GetActiveScene();if(previous.isDirty)EditorSceneManager.SaveScene(previous);
        var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            Directory.CreateDirectory("Backups/StoreClerkMeshy/2026-11-03");string backup="Backups/StoreClerkMeshy/2026-11-03/RecoilServiceStore.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
            var clerk=all.Single(t=>t.name=="Adult store clerk - assembled 2D art");
            var story=all.Select(t=>t.GetComponent<ServiceStoreEncounter>()).FirstOrDefault(s=>s);
            // Some store revisions intentionally move the encounter outdoors.
            string storyBefore=story?EditorJsonUtility.ToJson(story):null;
            int collisions=all.Sum(t=>t.GetComponents<Collider2D>().Length);Vector3 approach=story&&story.approachPoint?story.approachPoint.position:Vector3.zero;
            foreach(Transform child in clerk)child.gameObject.SetActive(false);
            var old=clerk.Find("Meshy clerk - rigged lowpoly");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(source,scene);go.name="Meshy clerk - rigged lowpoly";go.transform.SetParent(clerk,false);go.transform.localRotation=Quaternion.Euler(0,195,0);
            animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=material;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up,new Vector3(4,4,4));}
            animator.Rebind();animator.Update(0);var bounds=SkinBounds(go);go.transform.localScale*=1.95f/bounds.size.y;bounds=SkinBounds(go);
            float floor=clerk.position.y-.15f;go.transform.position+=Vector3.up*(floor+.025f-bounds.min.y);bounds=SkinBounds(go);
            Need(bounds.size.y>1.90f&&bounds.size.y<2,"Unexpected clerk height");Need(Mathf.Abs(bounds.min.y-floor-.025f)<.005f,"Clerk feet misaligned");
            Need(!story||EditorJsonUtility.ToJson(story)==storyBefore,"Story component changed");Need(!story||!story.approachPoint||story.approachPoint.position==approach,"Story interaction moved");Need(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).Count()==collisions,"Gameplay colliders changed");
            int triangles=go.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3);Need(triangles<4000,"Clerk exceeds lowpoly budget");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Capture(scene,go,bounds,"ClerkClose",true);Capture(scene,go,bounds,"ClerkInStore",false);
            File.WriteAllText("Captures/StoreClerk/Verification.txt","PASS\nTriangles: "+triangles+"\nHeight: "+bounds.size.y+"\nSole height: "+bounds.min.y+"\nAvatar valid; idle controller assigned; store dialogue, approach point and colliders preserved.\n");
            Debug.Log("STORE_CLERK_MESHY_PASS: "+triangles+" triangles, scene saved");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
    static Bounds SkinBounds(GameObject root)
    {
        bool initialized=false;Bounds bounds=new Bounds();
        foreach(var renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var mesh=renderer.sharedMesh;var bones=renderer.bones;var matrices=bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();var weights=mesh.boneWeights;var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++){var w=weights[i];var v=vertices[i];var p=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;if(!initialized){bounds=new Bounds(p,Vector3.zero);initialized=true;}else bounds.Encapsulate(p);}
        }
        return bounds;
    }
    static void Capture(Scene scene,GameObject model,Bounds bounds,string name,bool close)
    {
        var cameraObject=new GameObject("Clerk verification camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;
        camera.orthographic=true;camera.orthographicSize=close?1.22f:2.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.12f,.14f,.15f);camera.nearClipPlane=.01f;camera.farClipPlane=100;
        Vector3 target=bounds.center;camera.transform.position=target+new Vector3(0,close?.25f:1.2f,close?-3.5f:-5);camera.transform.LookAt(target);
        var rt=new RenderTexture(1200,1000,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes("Captures/StoreClerk/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
