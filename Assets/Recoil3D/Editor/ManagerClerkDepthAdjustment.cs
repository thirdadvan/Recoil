using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class ManagerClerkDepthAdjustment
{
    [MenuItem("Recoil/Factory Manager/Reduce size 10 percent and match clerk depth")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        const string managerPath="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity",clerkPath="Assets/Scenes/3D/RecoilServiceStore.unity";
        var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(managerPath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(managerPath,OpenSceneMode.Additive);
        var store=SceneManager.GetSceneByPath(clerkPath);bool openedStore=!store.isLoaded;if(openedStore)store=EditorSceneManager.OpenScene(clerkPath,OpenSceneMode.Additive);
        try
        {
            string backup="Backups/ManagerClerkDepth/2026-11-05";Directory.CreateDirectory(backup);if(!File.Exists(backup+"/ManagerOffice.unity"))EditorSceneManager.SaveScene(scene,backup+"/ManagerOffice.unity",true);
            var clerk=store.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="Meshy clerk - rigged lowpoly");
            var manager=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="FactoryManager");var model=manager.Find("Meshy manager - timid lowpoly");var manners=model.GetComponent<FactoryManagerMannerisms>();var animator=model.GetComponent<Animator>();var item=manager.GetComponent<ContinuousInteractionItem>();string interaction=EditorJsonUtility.ToJson(item);
            animator.Rebind();animator.Update(0);float sole=FactoryManagerMeshyIntegration.SkinBounds(model.gameObject).min.y;float oldScale=manners.poseScale;Vector3 oldSize=model.localScale;
            if(manners.poseScale>1.2f){model.localScale*=.9f;manners.poseScale*=.9f;}
            var managerPosition=manager.position;managerPosition.z=clerk.parent.position.z;manager.position=managerPosition;var modelPosition=model.position;modelPosition.z=clerk.position.z;model.position=modelPosition;
            animator.Update(0);model.position+=Vector3.up*(sole-FactoryManagerMeshyIntegration.SkinBounds(model.gameObject).min.y);var bounds=FactoryManagerMeshyIntegration.SkinBounds(model.gameObject);
            if(Mathf.Abs(model.position.z-clerk.position.z)>.0001f||Mathf.Abs(bounds.min.y-sole)>.005f||EditorJsonUtility.ToJson(item)!=interaction)throw new Exception("Depth, sole or interaction validation failed");
            EditorUtility.SetDirty(manners);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Captures/FactoryManagerOffice");
            string report="PASS: relative scale="+(model.localScale.x/oldSize.x)+", poseScale="+manners.poseScale+", clerk model world Z="+clerk.position.z+", manager model world Z="+model.position.z+", sole="+bounds.min.y+"; interaction configuration unchanged.";
            File.WriteAllText("Captures/FactoryManagerOffice/ClerkDepthVerification.txt",report);Debug.Log("MANAGER_CLERK_DEPTH_PASS "+report);
        }
        finally{if(openedStore)EditorSceneManager.CloseScene(store,true);if(opened)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
    }
}
