using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseInsideLightingTestBuilder
{
    const string Source="Assets/Scenes/3D/RecoilFactoryWarehouse.unity";
    public const string Destination="Assets/Scenes/3D/Tests/RecoilWarehouseLightingTest.unity";
    const string Folder="Assets/Recoil3D/ArtTests/";
    [MenuItem("Recoil/Art Tests/Create INSIDE Warehouse Lighting Test")]
    public static void Create()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before creating the lighting test.");return;}
        Directory.CreateDirectory("Captures/WarehouseLightingTest");
        var hash=Hash(Source);
        if(File.Exists(Destination)){Debug.LogError("Lighting test already exists; open "+Destination);return;}
        Directory.CreateDirectory("Assets/Scenes/3D/Tests");AssetDatabase.Refresh();
        if(!AssetDatabase.CopyAsset(Source,Destination))throw new Exception("Could not copy warehouse");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(Destination,OpenSceneMode.Additive);
        try{
            SceneManager.SetActiveScene(scene);
            var roots=scene.GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
            var lens=all.Select(t=>t.GetComponent<Camera>()).First(c=>c && c.CompareTag("MainCamera"));
            var atmosphere=world.GetComponent<Recoil3DWorld>();atmosphere.ApplyAtmosphere();
            var lights=all.Select(t=>t.GetComponent<Light>()).Where(l=>l).ToArray();
            var test=lens.gameObject.AddComponent<WarehouseAtmosphereTest>();test.effectShader=Shader.Find("Recoil/Art Tests/Warehouse Atmosphere");
            if(!test.effectShader)throw new Exception("Atmosphere shader missing");
            test.originalLights=lights;test.originalIntensities=lights.Select(l=>l.intensity).ToArray();
            test.testIntensities=lights.Select(l=>l==atmosphere.playerVisualLight?l.intensity*.72f:l.type==LightType.Directional?l.intensity*.18f:l.transform.position.z>8?l.intensity*.25f:l.intensity*.72f).ToArray();
            test.originalAmbient=atmosphere.ambientColour;test.originalFog=atmosphere.fogColour;
            test.originalStart=atmosphere.fogStartDistance;test.originalEnd=atmosphere.fogEndDistance;
            var rig=new GameObject("INSIDE lighting study - presentation only").transform;rig.SetParent(world.transform,false);
            var lamps=new List<Light>();
            var fixtures=all.Where(t=>t.name.StartsWith("Hanging industrial lamp")).OrderBy(t=>t.position.x).ToArray();
            foreach(var fixture in fixtures.Where((t,i)=>i%Mathf.Max(1,fixtures.Length/3)==0).Take(3)){
                var g=new GameObject("Study dust cone and local key");g.transform.SetParent(fixture,false);g.transform.position=new Vector3(fixture.position.x,Mathf.Max(5.5f,fixture.position.y)-.22f,1.8f);g.transform.rotation=Quaternion.Euler(90,0,0);
                var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.63f,.72f,.73f);l.intensity=2.3f;l.range=10;l.spotAngle=62;l.shadows=LightShadows.Soft;l.shadowStrength=.8f;l.renderMode=LightRenderMode.ForcePixel;lamps.Add(l);
            }
            // Deliberately decorative: no RecoilLight2D registration, so AI exposure is unchanged.
            var accents=new List<Light>(lamps);
            foreach(var fixture in fixtures.Take(3)){
                var g=new GameObject("Study local reflected light - visual only");g.transform.SetParent(fixture,false);g.transform.position=new Vector3(fixture.position.x,1.3f,3);
                var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=4;l.intensity=.28f;l.color=new Color(.32f,.43f,.46f);l.shadows=LightShadows.None;accents.Add(l);
            }
            test.lamps=lamps.ToArray();test.decorativeLights=accents.ToArray();
            var shadowRoot=new GameObject("Soft grounding shadows - test only");shadowRoot.transform.SetParent(rig,false);test.contactShadows=shadowRoot;
            var mat=new Material(Shader.Find("Recoil/Art Tests/Soft Contact Shadow"));AssetDatabase.CreateAsset(mat,Folder+"WarehouseContactShadow.mat");
            int shadows=0;
            foreach(var t in all.Where(t=>t.name.StartsWith("Steel rack -")||t.name=="Steel drum")){
                var renderers=t.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();if(renderers.Length==0)continue;
                var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                if(bounds.min.y>1)continue;
                var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name="Soft rack contact";UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(shadowRoot.transform,false);
                g.transform.position=new Vector3(bounds.center.x,.018f,bounds.center.z);g.transform.rotation=Quaternion.Euler(90,0,0);g.transform.localScale=new Vector3(Mathf.Min(7,bounds.size.x+1),Mathf.Min(12,bounds.size.z+1),1);g.GetComponent<Renderer>().sharedMaterial=mat;shadows++;
            }
            // Capture the same camera framing for both versions, then restore the playable camera.
            Vector3 oldPosition=lens.transform.position;Quaternion oldRotation=lens.transform.rotation;float oldFov=lens.fieldOfView;
            var follow=lens.GetComponent<RecoilCameraController>();bool followOn=follow&&follow.enabled;if(follow)follow.enabled=false;
            var solids=all.Select(t=>t.GetComponent<BoxCollider2D>()).Where(c=>c&&!c.isTrigger&&c.bounds.size.x>15&&c.bounds.size.y<2).ToArray();
            float middle=solids.Length>0?solids.OrderByDescending(c=>c.bounds.size.x).First().bounds.center.x:20;
            lens.transform.position=new Vector3(middle,4.5f,-19);lens.transform.rotation=Quaternion.Euler(5,0,0);lens.fieldOfView=46;lens.allowHDR=true;
            test.testLook=false;test.ApplyLook();Capture(lens,"Original");
            test.testLook=true;test.ApplyLook();Capture(lens,"Study");
            lens.transform.SetPositionAndRotation(oldPosition,oldRotation);lens.fieldOfView=oldFov;if(follow)follow.enabled=followOn;
            EditorUtility.SetDirty(test);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            var report="Source unchanged: "+(hash==Hash(Source))+"\nScene: "+Destination+"\nLights: "+lights.Length+"; key cones: "+lamps.Count+"; grounding shadows: "+shadows+"\nFixtures: "+string.Join(", ",fixtures.Select(f=>f.name+" @ "+f.position))+"\nF6 switches original/test presentation. Gameplay settings, AI and source scene unchanged.\nThis first test uses bounded unshadowed atmospheric scattering; direct key lights cast real shadows.\n";
            File.WriteAllText("Captures/WarehouseLightingTest/Report.txt",report);Debug.Log("WAREHOUSE_LIGHTING_TEST_READY\n"+report);
        }finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);}
    }
    [MenuItem("Recoil/Art Tests/Open Warehouse Lighting Test")]
    public static void OpenTest()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(Destination)){
            Directory.CreateDirectory("Assets/Scenes/3D/Tests");AssetDatabase.Refresh();
            string error=AssetDatabase.MoveAsset("Assets/Scenes/Tests/RecoilWarehouseLightingTest.unity",Destination);
            if(!string.IsNullOrEmpty(error))throw new Exception(error);
        }
        var scene=EditorSceneManager.OpenScene(Destination,OpenSceneMode.Single);
        SceneManager.SetActiveScene(scene);
        var test=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).First();
        if(!test.useAuthoredPathLighting)test.scattering=.28f;
        foreach(var light in test.decorativeLights)if(light && light.name.StartsWith("Study local reflected"))light.intensity=.07f;
        var owner=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Recoil3DWorld>()).First();
        if(!test.useAuthoredPathLighting)
            for(int i=0;i<test.originalLights.Length;i++)if(test.originalLights[i]==owner.playerVisualLight)test.testIntensities[i]=test.originalIntensities[i];
        var lens=test.GetComponent<Camera>();var follow=lens.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;
        var p=lens.transform.position;var q=lens.transform.rotation;float fov=lens.fieldOfView;
        if(follow)follow.enabled=false;
        lens.transform.position=new Vector3(14,4,-18);lens.transform.rotation=Quaternion.Euler(5,0,0);lens.fieldOfView=40;
        test.testLook=false;test.ApplyLook();Capture(lens,"Original");test.testLook=true;test.ApplyLook();Capture(lens,"Study");
        lens.transform.position+=Vector3.up*1.5f;Capture(lens,"CameraRaised");
        lens.transform.position-=Vector3.up*3;Capture(lens,"CameraLowered");
        lens.transform.SetPositionAndRotation(p,q);lens.fieldOfView=fov;if(follow)follow.enabled=on;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.AppendAllText("Captures/WarehouseLightingTest/Report.txt","Final scene: "+Destination+"\n");
    }
    [MenuItem("Recoil/Art Tests/Set Test Main Light To 0.08")]
    public static void SetMainLightIntensity()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!=Destination){Debug.LogError("Open the warehouse lighting test outside Play mode first.");return;}
        var test=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();
        var main=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>(true)).Single(l=>l.name=="Main Light" && l.type==LightType.Directional);
        int index=Array.IndexOf(test.originalLights,main);
        if(index<0)throw new Exception("Main Light was not registered in the test lighting profile.");
        test.testIntensities[index]=.08f;main.intensity=.08f;
        EditorUtility.SetDirty(test);EditorUtility.SetDirty(main);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Debug.Log("WAREHOUSE_MAIN_LIGHT_SAVED: actual="+main.intensity+"; test="+test.testIntensities[index]);
    }
    static string Hash(string p){using(var h=SHA256.Create())return Convert.ToBase64String(h.ComputeHash(File.ReadAllBytes(p)));}
    static void Capture(Camera camera,string name){
        var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGBHalf);var old=camera.targetTexture;var active=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1440,810,TextureFormat.RGBAFloat,false,true);image.ReadPixels(new Rect(0,0,1440,810),0,0);image.Apply();
            if(QualitySettings.activeColorSpace==ColorSpace.Linear){var pixels=image.GetPixels();for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;image.SetPixels(pixels);image.Apply();}
            var output=new Texture2D(1440,810,TextureFormat.RGB24,false,true);output.SetPixels(image.GetPixels());output.Apply();
            File.WriteAllBytes("Captures/WarehouseLightingTest/"+name+".png",output.EncodeToPNG());UnityEngine.Object.DestroyImmediate(output);UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);}
    }
}
