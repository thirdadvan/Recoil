using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoalerCoreVisualCheck
{
    [MenuItem("Recoil/Art Tests/Check Coaler Core and Lower Tentacles")]
    public static void Capture()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before the art check.");return;}
        var previous=SceneManager.GetActiveScene();Color oldAmbient=RenderSettings.ambientLight;var oldMode=RenderSettings.ambientMode;
        var previousRoots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
        foreach(var root in previousRoots)root.SetActive(false);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        Directory.CreateDirectory("Captures/CoalerCore");
        try{
            var go=new GameObject("Temporary Coaler visual check");go.layer=30;go.transform.position=new Vector3(0,.8f,0);
            var v=go.AddComponent<RampagerTentacleVisual>();
            string folder="Assets/Recoil3D/Materials/TentacleWorker/";
            v.tentacleMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Black fibrous upper body.mat");
            v.trouserMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Worn safety orange.mat");
            v.patchMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Charcoal knee reinforcements.mat");
            v.bootMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Heavy work boots.mat");
            v.coreMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Dark red suspended core.mat");
            v.badgeMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Faded identity card.mat");
            v.followAI=false;v.previewForm=RampagerTentacleVisual.Form.Approaching;v.Build();
            var cam=new GameObject("Temporary art check camera").AddComponent<Camera>();cam.cullingMask=1<<30;cam.transform.position=new Vector3(3.0f,2.3f,-7.6f);cam.transform.LookAt(new Vector3(0,1.6f,0));cam.fieldOfView=35;cam.allowHDR=true;cam.backgroundColor=Color.black;cam.clearFlags=CameraClearFlags.SolidColor;
            var key=new GameObject("Temporary neutral key").AddComponent<Light>();key.type=LightType.Directional;key.cullingMask=1<<30;key.intensity=.65f;key.transform.rotation=Quaternion.Euler(30,-25,0);
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.12f,.12f,.12f);
            v.SetPreview(RampagerTentacleVisual.Form.Approaching);v.UpdatePose(0,true);Image(cam,"Approaching");
            var light=go.GetComponentsInChildren<Light>(true).Single();
            if(!light.enabled||light.intensity<=0)throw new Exception("Exposed core light is disabled");
            key.enabled=false;RenderSettings.ambientLight=Color.black;Image(cam,"ApproachingDark");
            v.SetPreview(RampagerTentacleVisual.Form.Watching);v.UpdatePose(0,true);
            if(!light.enabled)throw new Exception("Watching core should glow");
            v.SetPreview(RampagerTentacleVisual.Form.Fusion);v.UpdatePose(0,true);
            if(!light.enabled)throw new Exception("Fusion core should glow");Image(cam,"FusionDark");
            v.SetPreview(RampagerTentacleVisual.Form.Watching);v.previewSoundEnergy=50;v.UpdatePose(0,true);key.enabled=true;RenderSettings.ambientLight=new Color(.12f,.12f,.12f);Image(cam,"Energy50");
            v.previewSoundEnergy=100;v.UpdatePose(0,true);Image(cam,"Energy100");
            v.SetPreview(RampagerTentacleVisual.Form.Enraged);v.UpdatePose(0,true);key.enabled=true;RenderSettings.ambientLight=new Color(.12f,.12f,.12f);Image(cam,"Enraged");key.enabled=false;RenderSettings.ambientLight=Color.black;Image(cam,"EnragedDark");
            if(light.enabled)throw new Exception("Enraged core light should be off");
            v.SetPreview(RampagerTentacleVisual.Form.Roaming);v.UpdatePose(0,true);
            if(light.enabled)throw new Exception("Roaming core light should be off");
            var distant=new GameObject("Temporary five metre fusion target");distant.layer=30;distant.transform.position=new Vector3(5,1.2f,0);v.previewTarget=distant.transform;
            v.fusionProgress=1;v.SetPreview(RampagerTentacleVisual.Form.Fusion);v.UpdatePose(0,true);
            var skin=v.VisualRoot.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Back head torso shutters arms loose strands and fusion");
            if(skin.bounds.max.x<4.7f)throw new Exception("Fusion tentacles do not reach the five metre target");
            key.enabled=true;RenderSettings.ambientLight=new Color(.12f,.12f,.12f);cam.transform.position=new Vector3(5,3,-13);cam.transform.LookAt(new Vector3(2.1f,1.4f,0));cam.fieldOfView=40;Image(cam,"FusionFiveMetres");
            File.WriteAllText("Captures/CoalerCore/Check.txt","PASS: exposed Watching/Approaching/Fusion core emits light; Enraged/Roaming core light disabled. Dark capture has no ambient or external key light.\n");
            Debug.Log("COALER_CORE_CHECK_PASS");
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;foreach(var root in previousRoots)if(root)root.SetActive(true);}
    }
    static void Image(Camera cam,string name)
    {
        var rt=RenderTexture.GetTemporary(1000,1000,24,RenderTextureFormat.ARGBHalf);var active=RenderTexture.active;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
            var hdr=new Texture2D(1000,1000,TextureFormat.RGBAFloat,false,true);hdr.ReadPixels(new Rect(0,0,1000,1000),0,0);hdr.Apply();var pixels=hdr.GetPixels();
            if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            var output=new Texture2D(1000,1000,TextureFormat.RGB24,false,true);output.SetPixels(pixels);output.Apply();File.WriteAllBytes("Captures/CoalerCore/"+name+".png",output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(hdr);UnityEngine.Object.DestroyImmediate(output);
        }finally{cam.targetTexture=null;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
}
