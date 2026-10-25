using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ServiceStoreNaturalLightingPass
{
    const string Path="Assets/Scenes/3D/RecoilServiceStore.unity";
    [MenuItem("Recoil/Art Tests/Use Window And Skylight In Service Store")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before updating store lighting.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(Path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        var otherRoots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
        try
        {
            SceneManager.SetActiveScene(scene);string backup="Backups/ServiceStoreNaturalLighting/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/RecoilServiceStore.unity",true);
            var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var look=all.Select(t=>t.GetComponent<WarehouseAtmosphereTest>()).Single(x=>x);
            look.lamps=new Light[0];look.decorativeLights=new Light[0];
            int removed=0;
            foreach(var t in all.Where(t=>t.name=="Indoor fixed path lighting"||t.name=="Store ceiling lamp 0"||t.name=="Store ceiling lamp 1"||t.name=="Store ceiling lamp 2").ToArray())
                if(t){UnityEngine.Object.DestroyImmediate(t.gameObject);removed++;}
            foreach(var t in all.Where(t=>t&&t.name=="Rear working lamp"))t.gameObject.SetActive(false);
            var lights=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).ToArray();
            var windows=lights.Where(l=>l.name=="Window light - projects gaps").ToArray();if(windows.Length==0)throw new Exception("Store windows missing");
            foreach(var l in windows)
            {
                bool exposed=l.transform.parent.name.Contains("one half exposed");
                var p=l.transform.position;var target=new Vector3(p.x+.45f,.35f,.8f);
                l.transform.rotation=Quaternion.LookRotation(target-p);l.intensity=exposed?2.5f:1.05f;l.range=17;l.spotAngle=58;l.innerSpotAngle=22;
                l.color=new Color(.58f,.70f,.76f);l.enabled=true;l.shadows=LightShadows.Soft;l.shadowResolution=LightShadowResolution.Medium;l.shadowBias=.015f;l.shadowNormalBias=.04f;
                // Existing boarded-window cookies retain the shape of the actual opening.
                EditorUtility.SetDirty(l);
            }
            var sky=lights.Single(l=>l.name=="Skylight daylight - child of opening");
            sky.intensity=1.45f;sky.range=7.5f;sky.spotAngle=105;sky.innerSpotAngle=28;sky.transform.rotation=Quaternion.Euler(90,0,0);sky.shadows=LightShadows.Soft;sky.shadowResolution=LightShadowResolution.Medium;sky.shadowBias=.01f;sky.shadowNormalBias=.04f;sky.enabled=true;
            var rim=lights.FirstOrDefault(l=>l.name=="Soft sky bounce on inner curb");if(rim){rim.intensity=1.3f;rim.range=1.7f;EditorUtility.SetDirty(rim);}
            var wash=lights.FirstOrDefault(l=>l.name=="Light washing inner frame - no floor reach");if(wash){wash.intensity=1.1f;EditorUtility.SetDirty(wash);}
            // The entrance projection is a separate exterior source: keep it readable
            // against the rear-window fill, without restoring any hanging-lamp cones.
            var entrance=lights.Single(l=>l.name=="Outside entrance light - projects into room");
            entrance.enabled=true;entrance.intensity=3;entrance.range=11;
            entrance.transform.rotation=Quaternion.LookRotation(new Vector3(entrance.transform.position.x,.02f,-.3f)-entrance.transform.position);
            entrance.shadows=LightShadows.Soft;entrance.shadowStrength=1;entrance.shadowResolution=LightShadowResolution.High;
            entrance.shadowBias=.006f;entrance.shadowNormalBias=.02f;entrance.renderMode=LightRenderMode.ForcePixel;
            var warehouseScene=SceneManager.GetSceneByPath(WarehouseInsideLightingTestBuilder.Destination);bool openedWarehouse=!warehouseScene.isLoaded;
            if(openedWarehouse)warehouseScene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination,OpenSceneMode.Additive);
            try
            {
                var reference=warehouseScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Single(l=>l.name=="Incident light through foreground return doorway");
                entrance.intensity=reference.intensity;entrance.color=reference.color;entrance.range=reference.range;
                entrance.spotAngle=reference.spotAngle;entrance.innerSpotAngle=reference.innerSpotAngle;entrance.cookie=reference.cookie;
                Debug.Log("STORE_DOOR_MATCHED_WAREHOUSE: intensity="+entrance.intensity+" range="+entrance.range+" angle="+entrance.spotAngle+" colour="+entrance.color);
            }
            finally{if(openedWarehouse)EditorSceneManager.CloseScene(warehouseScene,true);SceneManager.SetActiveScene(scene);}
            var shadows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DDoorwayShadows>(true)).Single();
            shadows.enabled=true;shadows.gameObject.SetActive(true);
            foreach(var silhouette in shadows.silhouettes)if(silhouette)
            {
                silhouette.gameObject.SetActive(true);
                foreach(var caster in silhouette.GetComponentsInChildren<Renderer>(true))
                {caster.enabled=true;caster.forceRenderingOff=false;caster.shadowCastingMode=ShadowCastingMode.ShadowsOnly;}
            }
            EditorUtility.SetDirty(entrance);EditorUtility.SetDirty(shadows);
            EditorUtility.SetDirty(sky);EditorUtility.SetDirty(look);look.ApplyLook();
            if(look.lamps.Length!=0||scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="Deep reflector at path light"))throw new Exception("Unmotivated hanging lamp or volume cone remains");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            foreach(var root in otherRoots)root.SetActive(false);
            var camera=look.GetComponent<Camera>();WarehousePathLightingPass.Capture(camera,"ServiceStore_NaturalLight_Entrance");
            shadows.Initialize();shadows.Advance(.85f);WarehousePathLightingPass.Capture(camera,"ServiceStore_DoorShadows_Pounding");shadows.ResetShadows();
            var follow=camera.GetComponent<RecoilCameraController>();bool followOn=follow&&follow.enabled;var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
            try
            {
                if(follow)follow.enabled=false;camera.fieldOfView=45;camera.transform.position=new Vector3(8,3.4f,-13);camera.transform.LookAt(new Vector3(8,2.3f,1.3f));WarehousePathLightingPass.Capture(camera,"ServiceStore_NaturalLight_Room");
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;if(follow)follow.enabled=followOn;}
            Debug.Log("SERVICE_STORE_NATURAL_LIGHT_PASS: removed "+removed+" generic lamp rigs; shader cones=0; rear window cookie lights="+windows.Length+"; skylight main; counter practical preserved; backup="+backup);
        }
        finally
        {
            foreach(var root in otherRoots)if(root)root.SetActive(true);
            if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);var world=previous.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DWorld>(true)).FirstOrDefault();if(world)world.ApplyAtmosphere();var look=previous.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).FirstOrDefault();if(look)look.ApplyLook();}
        }
    }
}
