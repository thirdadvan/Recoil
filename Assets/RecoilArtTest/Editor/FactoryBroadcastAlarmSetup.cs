using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryBroadcastAlarmSetup
{
    static Transform Group(string name,Transform parent,Vector3 local)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
    static Renderer Box(string name,Transform parent,Vector3 local,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=local;g.transform.localScale=size;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=material;return r;
    }
    [MenuItem("Recoil/Tests/Add 3D Factory Broadcast Alarm Lights")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var s=SceneManager.GetActiveScene();if(s.path!="Assets/Scenes/RecoilFactoryArt3D.unity")throw new InvalidOperationException("Open the existing 3D factory interior first.");
        if(s.GetRootGameObjects().Any(g=>g.name=="07 Broadcast alarm lights - editable"))throw new InvalidOperationException("Alarm fixtures already installed.");
        var demo=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BroadcastRefractionDemo3D>(true)).First();
        string backup="Backups/FactoryBroadcastAlarm/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(s,backup+"/FactoryArt3D.unity",true);
        var root=Group("07 Broadcast alarm lights - editable",null,Vector3.zero);
        var housing=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilArtTest/Materials/Silhouette metal.mat");
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilArtTest/Materials/Emergency red glass.mat");
        for(int i=0;i<3;i++)
        {
            var fixture=Group("Red alarm fixture "+(i+1),root,new Vector3(6+i*10,6.8f,-2.5f));
            Box("Housing",fixture,Vector3.zero,new Vector3(.85f,.18f,.45f),housing);
            var bulb=Box("Red glass",fixture,new Vector3(0,-.12f,-.05f),new Vector3(.54f,.09f,.28f),glass);
            var light=Group("Light - fixture child",fixture,new Vector3(0,-.22f,-.18f)).gameObject.AddComponent<Light>();
            light.type=LightType.Point;light.color=new Color(1,.035f,.012f);light.range=11.5f;light.intensity=0;
            light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;
            var pulse=fixture.gameObject.AddComponent<BroadcastAlarmLamp3D>();pulse.source=light;pulse.luminousSurface=bulb;pulse.broadcastDemo=demo;pulse.peakIntensity=5;pulse.phaseOffset=i*.04f;
        }
        foreach(var legacy in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<FactoryArtLamp>(true)))
        {
            legacy.enabled=false;var pulse=legacy.GetComponent<BroadcastAlarmLamp3D>();if(!pulse)pulse=legacy.gameObject.AddComponent<BroadcastAlarmLamp3D>();
            pulse.broadcastDemo=demo;pulse.source=legacy.source;pulse.luminousSurface=legacy.luminousSurface;pulse.peakIntensity=legacy.peakIntensity;pulse.phaseOffset=.12f;if(legacy.source)legacy.source.intensity=0;
        }
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
        Debug.Log("3D_BROADCAST_RED_LIGHTS_ADDED / 1.4s cycle / .45s soft pulse");
    }
}
