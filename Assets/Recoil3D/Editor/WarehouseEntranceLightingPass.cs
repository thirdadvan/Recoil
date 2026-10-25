using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class WarehouseEntranceLightingPass
{
    [MenuItem("Recoil/Art Tests/Improve Warehouse Entrance And Lighting Cost")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Exit Play before editing the lighting study.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetActiveScene();if(scene.path!=WarehouseInsideLightingTestBuilder.Destination)scene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var test=all.Select(t=>t.GetComponent<WarehouseAtmosphereTest>()).Single(t=>t);var camera=test.GetComponent<Camera>();
        var door=all.First(t=>t.name=="Foreground return doorway - light and cutaway wall");
        var entry=door.GetComponentInChildren<Light>();
        entry.transform.position=new Vector3(door.position.x-1.5f,4.2f,door.position.z-4.6f);
        entry.transform.rotation=Quaternion.LookRotation(new Vector3(door.position.x+1.6f,.02f,.8f)-entry.transform.position);
        entry.intensity=3;entry.range=12.5f;entry.spotAngle=70;entry.shadowBias=.008f;entry.shadowNormalBias=.035f;entry.shadowStrength=.95f;
        int index=Array.IndexOf(test.originalLights,entry);if(index>=0)test.testIntensities[index]=3;
        foreach(var actor in all.Select(t=>t.GetComponent<Recoil3DActorVisual>()).Where(a=>a&&a.GetComponent<RecoilPlayerController>()&&a.model))
            foreach(var renderer in actor.model.GetComponentsInChildren<MeshRenderer>(true))renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        // The next fixed key begins farther inside; keep a dark gap beyond the doorway pool.
        foreach(var l in test.decorativeLights)if(l&&l.name.Contains("transition")&&l.transform.position.x<8)l.intensity=0;
        test.volumeResolutionDivider=4;test.volumeSteps=16;
        var budget=camera.GetComponent<WarehouseShadowBudget>();
        if(!budget)
        {
            budget=camera.gameObject.AddComponent<WarehouseShadowBudget>();
            budget.lights=test.originalLights.Concat(test.decorativeLights).Where(l=>l).Distinct().ToArray();
            budget.originalShadows=budget.lights.Select(l=>l.shadows).ToArray();budget.originalResolution=budget.lights.Select(l=>l.shadowResolution).ToArray();
        }
        budget.doorway=entry;
        var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;
        try
        {
            if(follow)follow.enabled=false;test.testLook=true;test.ApplyLook();
            camera.transform.SetPositionAndRotation(new Vector3(5,3.8f,-14),Quaternion.Euler(5,0,0));camera.fieldOfView=40;budget.Apply();
            WarehousePathLightingPass.Capture(camera,"Doorway_Entrance");
            camera.transform.SetPositionAndRotation(new Vector3(22,4.4f,-18),Quaternion.Euler(5,0,0));camera.fieldOfView=44;budget.Apply();
            WarehousePathLightingPass.Capture(camera,"Optimized_Centre");
        }
        finally{camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(follow)follow.enabled=on;budget.Apply();}
        int before=budget.originalShadows.Count(s=>s!=LightShadows.None),after=budget.lights.Count(l=>l&&l.shadows!=LightShadows.None);
        string report="Shadow-capable lights before: "+before+"; stable shadow-enabled lights: "+after+" (all overhead keys plus doorway, no camera-distance switching).\nVolume samples: half-resolution 24 steps -> quarter-resolution 16 steps (1/6 of raymarch samples). Blur/composite costs remain.\nCharacter mesh discovery: .5 seconds -> 2 seconds.\nDoor light: "+entry.transform.position+" intensity="+entry.intensity+" range="+entry.range+"; original aperture cookie and shadow-only wall retained.\nNo frame-time speedup is claimed without GPU profiling.\n";
        File.WriteAllText("Captures/WarehouseLightingTest/EntranceOptimization.txt",report);
        EditorUtility.SetDirty(test);EditorUtility.SetDirty(budget);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("WAREHOUSE_ENTRANCE_OPTIMIZED\n"+report);
    }
}
