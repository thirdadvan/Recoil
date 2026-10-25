using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Authored light placement in the existing, separate warehouse experiment only.
public static class WarehousePathLightingPass
{
    [MenuItem("Recoil/Art Tests/Apply Warehouse Path Lighting Study")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Exit Play before editing the lighting study.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=WarehouseInsideLightingTestBuilder.Destination)
            scene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var test=all.Select(t=>t.GetComponent<WarehouseAtmosphereTest>()).Single(t=>t);
        var camera=test.GetComponent<Camera>();var vision=camera.GetComponent<IndoorVisionFog>();
        if(!test.useAuthoredPathLighting)test.originalVisionFogEnabled=vision&&vision.effectEnabled;
        test.useAuthoredPathLighting=true;
        foreach(var light in test.decorativeLights)if(light)UnityEngine.Object.DestroyImmediate(light.gameObject);
        var fixtures=all.Where(t=>t && t.name.StartsWith("Hanging industrial lamp")).OrderBy(t=>t.position.x).ToArray();
        var keys=new List<Light>();var lights=new List<Light>();
        foreach(var fixture in fixtures)
        {
            float x=fixture.position.x;
            // Top light reveals walking surfaces and the upper shelf landings.
            var top=Spot(fixture,"Study path - overhead key",new Vector3(x,fixture.position.y-.25f,1.2f),new Vector3(x,.2f,1),new Color(.62f,.72f,.72f),2.1f,9.2f,78,true);
            lights.Add(top);if(keys.Count<3 && fixture.position.y>5)keys.Add(top);
            // Broad, very weak reflected fill, fixed in the room rather than following Dawn.
            lights.Add(Spot(fixture,"Study path - readable floor and ladder",new Vector3(x,3.2f,-3.4f),new Vector3(x,1.3f,1.5f),new Color(.43f,.53f,.56f),.48f,8,95,false));
        }
        if(fixtures.Length>1)
        {
            // Bridge the longer unlit stretches at the entrance and beyond the final rack.
            foreach(float x in new[]{3f,38.5f})
            {
                var parent=fixtures.OrderBy(f=>Mathf.Abs(f.position.x-x)).First();
                lights.Add(Spot(parent,"Study path - transition landing",new Vector3(x,5.2f,.4f),new Vector3(x,.3f,1),new Color(.57f,.67f,.69f),1.25f,9,86,true));
                lights.Add(Spot(parent,"Study path - transition reflected fill",new Vector3(x,2.8f,-3),new Vector3(x,1,1),new Color(.43f,.53f,.56f),.30f,7,100,false));
            }
            foreach(var fixture in new[]{fixtures[1],fixtures[Mathf.Min(3,fixtures.Length-1)]})
            {
                float x=fixture.position.x;
                lights.Add(Spot(fixture,"Study depth - isolated rack silhouette",new Vector3(x,6.1f,12),new Vector3(x+1,1.8f,15),new Color(.40f,.49f,.53f),.38f,6,46,true));
            }
        }
        test.lamps=keys.ToArray();test.decorativeLights=lights.ToArray();
        var world=all.Select(t=>t?t.GetComponent<Recoil3DWorld>():null).First(w=>w);
        for(int i=0;i<test.originalLights.Length;i++)
        {
            var l=test.originalLights[i];if(!l)continue;
            float original=test.originalIntensities[i];
            test.testIntensities[i]=l==world.playerVisualLight?0:l.type==LightType.Directional?.08f:original*(l.transform.position.z>7?.10f:.38f);
        }
        test.testAmbient=new Color(.007f,.009f,.012f);
        test.testFog=new Color(.009f,.013f,.017f);test.testStart=26;test.testEnd=82;
        test.scattering=.075f;test.softGlow=.035f;test.testLook=true;test.ApplyLook();
        Directory.CreateDirectory("Captures/WarehouseLightingTest");
        var follow=camera.GetComponent<RecoilCameraController>();bool followEnabled=follow&&follow.enabled;
        var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;
        try
        {
            if(follow)follow.enabled=false;
            camera.fieldOfView=44;camera.allowHDR=true;
            foreach(float x in new[]{8f,22f,36f})
            {
                camera.transform.SetPositionAndRotation(new Vector3(x,4.4f,-18),Quaternion.Euler(5,0,0));
                test.testLook=false;test.ApplyLook();Capture(camera,"Path_Before_"+x);
                test.testLook=true;test.ApplyLook();Capture(camera,"Path_After_"+x);
            }
        }
        finally
        {
            camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;
            if(follow)follow.enabled=followEnabled;test.testLook=true;test.ApplyLook();
        }
        EditorUtility.SetDirty(test);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText("Captures/WarehouseLightingTest/PathLighting.txt","Fixed path lighting; no player-following reveal or player visual fill in test look. F6 restores original profile.\n"+string.Join("\n",lights.Select(l=>l.name+" @ "+l.transform.position+" intensity="+l.intensity))+"\nOriginal gameplay warehouse is unchanged.");
        Debug.Log("WAREHOUSE_PATH_LIGHTING_READY: "+lights.Count+" lights; scene="+scene.path);
    }
    static Light Spot(Transform parent,string name,Vector3 position,Vector3 target,Color colour,float strength,float range,float angle,bool shadow)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=position;g.transform.rotation=Quaternion.LookRotation(target-position);
        var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=colour;l.intensity=strength;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.55f;
        l.shadows=shadow?LightShadows.Soft:LightShadows.None;l.shadowStrength=.85f;l.shadowBias=.025f;l.shadowNormalBias=.12f;l.renderMode=LightRenderMode.ForcePixel;return l;
    }
    public static void Capture(Camera camera,string name)
    {
        var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGBHalf);var previous=camera.targetTexture;var active=RenderTexture.active;
        Texture2D image=null,output=null;
        try
        {
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            image=new Texture2D(1440,810,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,1440,810),0,0);image.Apply();
            var pixels=image.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
            output=new Texture2D(1440,810,TextureFormat.RGB24,false,true);output.SetPixels(pixels);output.Apply();File.WriteAllBytes("Captures/WarehouseLightingTest/"+name+".png",output.EncodeToPNG());
        }
        finally{camera.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(image)UnityEngine.Object.DestroyImmediate(image);if(output)UnityEngine.Object.DestroyImmediate(output);}
    }
}
