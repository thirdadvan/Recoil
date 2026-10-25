using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class IndoorLightingRollout
{
    public static void VerifyAndApply(){AutomaticStepVerification.Verify();Apply();CaptureAll();}
    public static void CaptureAll()
    {
        string previous=SceneManager.GetActiveScene().path;
        foreach(string path in Directory.GetFiles("Assets/Scenes/3D","*.unity",SearchOption.AllDirectories).Where(p=>!p.Contains("/Tests/")&&!p.EndsWith("/RecoilPrototype.unity")).OrderBy(p=>p))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
            foreach(var link in All<Recoil3DLight>(scene))link.Sync();
            var look=All<WarehouseAtmosphereTest>(scene).Single();look.ApplyLook();
            var camera=look.GetComponent<Camera>();WarehousePathLightingPass.Capture(camera,"Indoor_"+scene.name);
            Debug.Log("INDOOR_ISOLATED_CAPTURE: "+scene.name);
        }
        if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
    }
    [MenuItem("Recoil/Art Tests/Apply Warehouse Look To All Interiors")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before applying indoor lighting.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var active=SceneManager.GetActiveScene();
        var templateScene=SceneManager.GetSceneByPath(WarehouseInsideLightingTestBuilder.Destination);bool openedTemplate=!templateScene.isLoaded;
        if(openedTemplate)templateScene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination,OpenSceneMode.Additive);
        var template=All<WarehouseAtmosphereTest>(templateScene).Single();
        string backup="Backups/IndoorLightingRollout/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        Directory.CreateDirectory("Captures/WarehouseLightingTest");var report=new List<string>();
        try
        {
            foreach(string path in Directory.GetFiles("Assets/Scenes/3D","*.unity",SearchOption.AllDirectories).Where(p=>!p.Contains("/Tests/")&&!p.EndsWith("/RecoilPrototype.unity")).OrderBy(p=>p))
            {
                var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                try
                {
                    SceneManager.SetActiveScene(scene);
                    if(All<WarehouseAtmosphereTest>(scene).Any()){report.Add(path+": already installed");continue;}
                    EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+".unity",true);
                    var world=All<Recoil3DWorld>(scene).Single();var camera=All<Camera>(scene).First(c=>c.CompareTag("MainCamera"));
                    var existing=All<Light>(scene).Where(l=>(l.hideFlags&HideFlags.DontSave)==0&&!l.GetComponentInParent<Recoil3DActorVisual>()&&!l.GetComponentInParent<KBKStackerCrane>()&&!(world.emergencyLights??new Light[0]).Contains(l)).ToArray();
                    var look=camera.gameObject.AddComponent<WarehouseAtmosphereTest>();EditorUtility.CopySerialized(template,look);
                    look.showTestControls=false;look.testLook=true;look.originalAmbient=world.ambientColour;look.originalFog=world.fogColour;look.originalStart=world.fogStartDistance;look.originalEnd=world.fogEndDistance;
                    look.originalLights=new Light[0];look.originalIntensities=look.testIntensities=new float[0];look.decorativeLights=new Light[0];look.contactShadows=null;look.lamps=new Light[0];
                    look.originalVisionFogEnabled=camera.GetComponent<IndoorVisionFog>()&&camera.GetComponent<IndoorVisionFog>().effectEnabled;
                    world.ambientColour=look.testAmbient;world.fogColour=look.testFog;world.fogStartDistance=look.testStart;world.fogEndDistance=look.testEnd;
                    bool warehouse=scene.name=="RecoilFactoryWarehouse";
                    var cones=new List<Light>();
                    foreach(var light in existing)
                    {
                        float desired;
                        int i=Array.FindIndex(template.originalLights,l=>l&&l.name==light.name&&(l.transform.position-light.transform.position).sqrMagnitude<.02f);
                        if(warehouse&&i>=0)desired=template.testIntensities[i];
                        else if(light==world.playerVisualLight)desired=0;
                        else if(light.type==LightType.Directional)desired=.08f;
                        else
                        {
                            bool doorway=Parents(light.transform).Any(t=>t.name.Contains("Foreground return doorway"));
                            bool down=light.type==LightType.Spot&&light.transform.forward.y<-.5f;
                            bool warm=light.color.r>light.color.b*1.25f;
                            float factor=doorway?1:light.transform.position.z>7?.10f:down?.75f:warm?.85f:.38f;
                            desired=light.intensity*factor;
                            if(down){cones.Add(light);light.shadows=LightShadows.Soft;light.shadowResolution=LightShadowResolution.Low;}
                        }
                        var link=All<Recoil3DLight>(scene).FirstOrDefault(x=>x.lightSource==light);
                        if(link){float before=light.intensity;if(before>.0001f)link.intensityMultiplier*=desired/before;EditorUtility.SetDirty(link);}
                        light.intensity=desired;EditorUtility.SetDirty(light);
                    }
                    if(warehouse)
                    {
                        var rig=new GameObject("Warehouse tested lighting - production");SceneManager.MoveGameObjectToScene(rig,scene);rig.transform.SetParent(world.transform,false);
                        var copied=new Dictionary<Transform,GameObject>();var decorative=new List<Light>();
                        foreach(var source in template.decorativeLights.Where(l=>l))
                        {
                            Transform node=source.transform.parent&&source.transform.parent.name=="Deep enamel pendant - shade owns light"?source.transform.parent:source.transform;
                            GameObject copy;if(!copied.TryGetValue(node,out copy))
                            {copy=UnityEngine.Object.Instantiate(node.gameObject,rig.transform);copy.name=node.name;copy.transform.SetPositionAndRotation(node.position,node.rotation);copy.transform.localScale=node.lossyScale;copied[node]=copy;}
                            var target=copy.GetComponentsInChildren<Light>(true).First(l=>l.name==source.name);decorative.Add(target);if(template.lamps.Contains(source))cones.Add(target);
                        }
                        look.decorativeLights=decorative.ToArray();
                        if(template.contactShadows){look.contactShadows=UnityEngine.Object.Instantiate(template.contactShadows,rig.transform);look.contactShadows.transform.SetPositionAndRotation(template.contactShadows.transform.position,template.contactShadows.transform.rotation);look.contactShadows.transform.localScale=template.contactShadows.transform.lossyScale;}
                    }
                    look.lamps=cones.ToArray();
                    var fill=camera.GetComponent<CameraCharacterFill>();if(!fill)fill=camera.gameObject.AddComponent<CameraCharacterFill>();EditorUtility.CopySerialized(template.GetComponent<CameraCharacterFill>(),fill);
                    camera.allowHDR=true;camera.depthTextureMode|=DepthTextureMode.Depth;
                    foreach(var mesh in All<Recoil3DActorVisual>(scene).SelectMany(a=>a.GetComponentsInChildren<MeshRenderer>(true)))mesh.shadowCastingMode=ShadowCastingMode.On;
                    look.ApplyLook();world.ApplyAtmosphere();
                    Check(scene,look,world);EditorUtility.SetDirty(look);EditorUtility.SetDirty(world);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                    // Render the actual saved camera framing; no player or camera relocation.
                    var oldScene=camera.scene;try{camera.scene=scene;WarehousePathLightingPass.Capture(camera,"Indoor_"+scene.name);}finally{camera.scene=oldScene;}
                    string line=path+": lights="+existing.Length+", volume cones="+cones.Count+", quarter resolution/16 steps";report.Add(line);Debug.Log("INDOOR_LOOK_APPLIED: "+line);
                }
                finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
            }
        }
        finally
        {
            if(openedTemplate)EditorSceneManager.CloseScene(templateScene,true);
            if(active.IsValid()&&active.isLoaded){SceneManager.SetActiveScene(active);var w=All<Recoil3DWorld>(active).FirstOrDefault();if(w)w.ApplyAtmosphere();var l=All<WarehouseAtmosphereTest>(active).FirstOrDefault();if(l)l.ApplyLook();}
            File.WriteAllLines("Captures/WarehouseLightingTest/IndoorRollout.txt",report);AssetDatabase.SaveAssets();
        }
        Debug.Log("ALL_INTERIOR_LOOKS_READY: "+report.Count+" scenes; backup="+backup);
    }
    public static void AddPathKeysAndCapture()
    {
        string previous=SceneManager.GetActiveScene().path;
        foreach(string path in Directory.GetFiles("Assets/Scenes/3D","*.unity",SearchOption.AllDirectories).Where(p=>!p.Contains("/Tests/")&&!p.EndsWith("/RecoilPrototype.unity")&&!p.EndsWith("/RecoilFactoryWarehouse.unity")&&!p.EndsWith("/RecoilServiceStore.unity")).OrderBy(p=>p))
        {
            var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var look=All<WarehouseAtmosphereTest>(scene).Single();
            if(scene.GetRootGameObjects().Any(g=>g.name=="Indoor fixed path lighting"))continue;
            var rig=new GameObject("Indoor fixed path lighting").transform;
            var sources=All<Light>(scene).Where(l=>l.type==LightType.Spot&&l.transform.forward.y<-.5f&&l.transform.position.y>2&&!l.GetComponentInParent<Recoil3DActorVisual>()&&!l.GetComponentInParent<KBKStackerCrane>()).ToArray();
            var keys=new List<Light>();var fills=new List<Light>();
            foreach(var source in sources)
            {
                var p=source.transform.position;p.z=1.2f;
                if(keys.Any(l=>Mathf.Abs(l.transform.position.x-p.x)<2.5f&&Mathf.Abs(l.transform.position.y-p.y)<2))continue;
                bool warm=source.color.r>source.color.b*1.25f;
                var key=NewSpot(rig,"Indoor overhead key",p,p+Vector3.down*5,warm?source.color:new Color(.62f,.72f,.72f),2.1f,9.2f,78,true);keys.Add(key);
                var f=new Vector3(p.x,p.y-2.3f,-3.4f);fills.Add(NewSpot(rig,"Indoor fixed reflected fill",f,new Vector3(p.x,p.y-4.2f,1.5f),new Color(.43f,.53f,.56f),.48f,8,95,false));
                // The source remains a room fixture, independent of camera and player.
                var bowl=new GameObject("Deep reflector at path light");bowl.transform.SetParent(key.transform,false);bowl.transform.rotation=Quaternion.identity;bowl.transform.position=p+Vector3.up*.035f;
                bowl.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Recoil3D/ArtTests/Deep industrial reflector.asset");
                var mr=bowl.AddComponent<MeshRenderer>();mr.sharedMaterials=new[]{AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Deep shade - aged charcoal enamel.mat"),AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Deep shade - illuminated ivory lining.mat")};mr.shadowCastingMode=ShadowCastingMode.Off;
            }
            look.lamps=keys.ToArray();look.decorativeLights=keys.Concat(fills).ToArray();look.ApplyLook();EditorUtility.SetDirty(look);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("INDOOR_PATH_KEYS: "+scene.name+" keys="+keys.Count);
        }
        if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);
        CaptureAll();
    }
    static Light NewSpot(Transform parent,string name,Vector3 p,Vector3 target,Color colour,float intensity,float range,float angle,bool shadows)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=p;g.transform.rotation=Quaternion.LookRotation(target-p);
        var l=g.AddComponent<Light>();l.type=LightType.Spot;l.color=colour;l.intensity=intensity;l.range=range;l.spotAngle=angle;l.innerSpotAngle=angle*.55f;l.shadows=shadows?LightShadows.Soft:LightShadows.None;l.shadowResolution=LightShadowResolution.Low;l.shadowBias=.025f;l.shadowNormalBias=.12f;l.renderMode=LightRenderMode.ForcePixel;return l;
    }
    static IEnumerable<Transform> Parents(Transform t){while(t){yield return t;t=t.parent;}}
    static T[] All<T>(Scene scene)where T:Component{return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static void Check(Scene scene,WarehouseAtmosphereTest look,Recoil3DWorld world)
    {
        if(!look.effectShader||!look.effectShader.isSupported||look.volumeSteps!=16||look.volumeResolutionDivider!=4)throw new Exception("Invalid indoor shader profile: "+scene.name);
        if(look.lamps.Any(l=>!l||l.gameObject.scene!=scene)||look.decorativeLights.Any(l=>!l||l.gameObject.scene!=scene))throw new Exception("Cross-scene light reference: "+scene.name);
        if(All<Light>(scene).Any(l=>l==world.playerVisualLight&&l.intensity>.001f))throw new Exception("Player world fill must be disabled");
    }
}
