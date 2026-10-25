using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class WarehouseCharacterFillPass
{
    [MenuItem("Recoil/Art Tests/Add Warehouse Camera Character Fill")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Exit Play before applying character fill.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetActiveScene();if(scene.path!=WarehouseInsideLightingTestBuilder.Destination)scene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination);
        if(!string.IsNullOrEmpty(LayerMask.LayerToName(30))){Debug.LogError("Render layer 30 is occupied; choose a free layer first.");return;}
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single().GetComponent<Camera>();
        var fill=camera.GetComponent<CameraCharacterFill>();if(!fill)fill=camera.gameObject.AddComponent<CameraCharacterFill>();
        var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;
        try
        {
            if(follow)follow.enabled=false;camera.fieldOfView=44;
            foreach(float x in new[]{8f,22f})
            {
                camera.transform.SetPositionAndRotation(new Vector3(x,4.4f,-18),Quaternion.Euler(5,0,0));
                fill.fillEnabled=false;WarehousePathLightingPass.Capture(camera,"CharacterFill_Before_"+x);
                fill.fillEnabled=true;WarehousePathLightingPass.Capture(camera,"CharacterFill_After_"+x);
            }
        }
        finally{camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(follow)follow.enabled=on;fill.fillEnabled=true;}
        EditorUtility.SetDirty(fill);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("WAREHOUSE_CHARACTER_FILL_READY: shadowless camera spotlight; character meshes only; F7 toggle.");
    }
}
