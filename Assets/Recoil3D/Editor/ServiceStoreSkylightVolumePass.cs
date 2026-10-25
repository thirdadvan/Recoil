using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ServiceStoreSkylightVolumePass
{
    [MenuItem("Recoil/Art Tests/Add Broad Store Skylight Volume")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        const string path="Assets/Scenes/3D/RecoilServiceStore.unity";
        var scene=SceneManager.GetSceneByPath(path);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(path);
        var backup="Backups/ServiceStoreSkylight/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(scene,backup+"/RecoilServiceStore.unity",true);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var look=all.Select(t=>t.GetComponent<WarehouseAtmosphereTest>()).Single(x=>x);
        var daylight=all.Select(t=>t.GetComponent<Light>()).Single(l=>l&&l.name=="Skylight daylight - child of opening");
        var anchor=daylight.transform.Find("Rectangular skylight volume aperture");
        if(!anchor){anchor=new GameObject("Rectangular skylight volume aperture").transform;anchor.SetParent(daylight.transform,false);}
        anchor.position=new Vector3(daylight.transform.position.x,4.65f,daylight.transform.position.z);
        look.skylightOpening=anchor;look.skylightWidthDepth=new Vector2(3,2.6f);look.skylightHeight=4.65f;
        look.skylightSpread=.10f;look.skylightFeather=.32f;look.skylightDensity=.018f;look.skylightColour=new Color(.46f,.60f,.68f);
        // Existing shadowed light gives a restrained real floor response, not an
        // additive decal: fixtures and actors continue to occlude the skylight.
        daylight.intensity=1.65f;daylight.range=7.5f;daylight.spotAngle=76;daylight.innerSpotAngle=48;
        daylight.shadows=LightShadows.Soft;daylight.renderMode=LightRenderMode.ForcePixel;
        look.ApplyLook();EditorUtility.SetDirty(daylight);EditorUtility.SetDirty(look);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(look.lamps.Length!=0)throw new Exception("Store must not regain pendant cones");
        var shader=look.effectShader;
        if(!shader||UnityEditor.ShaderUtil.ShaderHasError(shader))throw new Exception("Atmosphere shader failed compilation");
        var camera=look.GetComponent<Camera>();var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        var follow=camera.GetComponent<RecoilCameraController>();bool enabled=follow&&follow.enabled;
        try
        {
            if(follow)follow.enabled=false;
            camera.fieldOfView=45;camera.transform.position=new Vector3(8,3.4f,-13);camera.transform.LookAt(new Vector3(8,2.3f,1.3f));
            float density=look.skylightDensity;look.skylightDensity=0;WarehousePathLightingPass.Capture(camera,"StoreSkylight_VolumeOff");look.skylightDensity=density;
            WarehousePathLightingPass.Capture(camera,"StoreSkylight_BroadVolume");
            camera.transform.position+=new Vector3(1,1,0);camera.transform.LookAt(new Vector3(8,2.3f,1.3f));WarehousePathLightingPass.Capture(camera,"StoreSkylight_AlternateView");
            if(UnityEditor.ShaderUtil.ShaderHasError(shader))throw new Exception("Atmosphere shader failed after rendering");
        }
        finally{camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;if(follow)follow.enabled=enabled;}
        Debug.Log("STORE_SKYLIGHT_VOLUME_PASS: rectangular 3 x 2.6m aperture; world anchored, soft broad shaft, subtle shadowed floor light; no pendant cones; shader/render OK. Backup="+backup);
    }
}
