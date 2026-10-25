using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoalerIdleUnfurlCheck
{
    [MenuItem("Recoil/Tests/Verify Slow Idle Unfurl")]
    public static void Verify()
    {
        var previous=SceneManager.GetActiveScene();var roots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
        var ambient=RenderSettings.ambientLight;bool fog=RenderSettings.fog;
        foreach(var root in roots)root.SetActive(false);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var actor=new GameObject("Idle unfurl verification");actor.transform.position=Vector3.up*.8f;
            var v=actor.AddComponent<RampagerTentacleVisual>();v.SetPreview(RampagerTentacleVisual.Form.Roaming);v.UpdatePose(0,true);
            var cam=new GameObject("Verification camera").AddComponent<Camera>();cam.transform.position=new Vector3(3,2.5f,-7.6f);cam.transform.LookAt(new Vector3(0,1.5f,0));cam.fieldOfView=35;cam.backgroundColor=new Color(.08f,.09f,.10f);cam.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Verification key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=new Color(.18f,.18f,.18f);RenderSettings.fog=false;
            v.SetPreview(RampagerTentacleVisual.Form.Idle);v.UpdatePose(0,false);
            Check(v.IdleUnfurlAmount==0,"Must enter Idle folded");
            for(int i=0;i<35;i++)v.PreviewTick(.025f);
            Check(Mathf.Abs(v.IdleUnfurlAmount-.5f)<.001f,"Half unfolded at .875 seconds");WarehousePathLightingPass.Capture(cam,"Coaler_Idle_Unfurl_Half");
            for(int i=0;i<34;i++)v.PreviewTick(.025f);
            Check(v.IdleUnfurlAmount<1,"Must not finish before 1.75 seconds");v.PreviewTick(.025f);
            Check(Mathf.Abs(v.IdleUnfurlAmount-1)<.00001f,"Must fully unfold at 1.75 seconds");WarehousePathLightingPass.Capture(cam,"Coaler_Idle_Unfurl_Full");
            var mesh=v.VisualRoot.GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh.name=="Dynamic combined curved tentacles").sharedMesh;var before=mesh.vertices;
            for(int i=0;i<40;i++)v.PreviewTick(.025f);
            Check(before.Zip(mesh.vertices,(a,b)=>(a-b).sqrMagnitude).Any(d=>d>.001f),"Expanded tentacles must visibly sway");WarehousePathLightingPass.Capture(cam,"Coaler_Idle_LargerSway");
            v.SetPreview(RampagerTentacleVisual.Form.Enraged);v.UpdatePose(0,true);Check(v.IdleUnfurlAmount==0,"Attack must clear the idle pose");
            v.SetPreview(RampagerTentacleVisual.Form.Idle);v.PreviewTick(.025f);Check(v.IdleUnfurlAmount<.01f,"Re-entering Idle must restart unfurl");
            Debug.Log("COALER_IDLE_UNFURL_PASS: .875s half, 1.75s complete, no early completion; larger sway changes geometry; attack interruption and idle restart verified.");
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);foreach(var root in roots)if(root)root.SetActive(true);RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;}
    }
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
}
