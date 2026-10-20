using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FreightEquipmentLightingSetup
{
    static SpriteRenderer Lens(string name,SpriteRenderer parent,Vector2 position,Vector2 size)
    {
        var t=new GameObject(name).transform;t.SetParent(parent.transform,false);t.position=position;
        Vector3 scale=parent.transform.lossyScale;t.localScale=new Vector3(size.x/Mathf.Max(.001f,Mathf.Abs(scale.x)),size.y/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=parent.sprite;r.sharedMaterial=parent.sharedMaterial;r.sortingOrder=Mathf.Max(12,parent.sortingOrder+1);r.color=RecoilPrototype.Hex(0x293128);return r;
    }
    static FreightEquipmentLighting.Lamp Lamp(SpriteRenderer lens,bool status,bool work)
    {
        var t=new GameObject(work?"Downward work light":"Indicator light").transform;t.SetParent(lens.transform,false);
        Vector3 scale=lens.transform.lossyScale;t.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(scale.x)),1/Mathf.Max(.001f,Mathf.Abs(scale.y)),1/Mathf.Max(.001f,Mathf.Abs(scale.z)));
        RecoilLight2D source;
        if(work){var cone=t.gameObject.AddComponent<RecoilIndustrialLamp>();cone.coneLength=3.2f;cone.coneHalfWidth=1.2f;cone.edgeSoftness=.35f;cone.scatterIntensity=.18f;cone.radius=2.2f;cone.intensity=.5f;source=cone;}
        else{source=t.gameObject.AddComponent<RecoilLight2D>();source.radius=1.7f;source.intensity=.38f;}
        var native=new GameObject("Realtime Light").transform;native.SetParent(t,false);native.localPosition=new Vector3(0,0,-.3f);
        var light=native.gameObject.AddComponent<Light>();light.type=work?LightType.Spot:LightType.Point;light.range=work?3.7f:1.7f;light.intensity=work?1.2f:.7f;light.shadows=LightShadows.None;
        if(work){light.spotAngle=48;native.rotation=Quaternion.LookRotation(new Vector3(0,-1,.12f),Vector3.forward);}
        source.enabled=false;light.enabled=false;
        return new FreightEquipmentLighting.Lamp{illumination=source,realtimeLight=light,lens=lens,statusIndicator=status,onColor=RecoilPrototype.Hex(0xffe2aa)};
    }
    public static FreightEquipmentLighting Install(KBKStackerCrane crane)
    {
        var device=crane.GetComponent<FreightEquipmentLighting>();if(device && device.lamps!=null && device.lamps.Length>0)return device;
        if(!crane.statusLamp)throw new Exception("Freight is missing its switch indicator.");
        var renderers=crane.GetComponentsInChildren<SpriteRenderer>(true);
        var housing=renderers.First(r=>r.name=="Trolley housing");var carriage=renderers.First(r=>r.name=="Fork carriage");
        if(!device)device=crane.gameObject.AddComponent<FreightEquipmentLighting>();
        var beacon=Lens("Freight trolley power beacon",housing,new Vector2(housing.bounds.max.x-.18f,housing.bounds.max.y+.09f),new Vector2(.16f,.15f));
        var work=Lens("Freight carriage work lamp",carriage,new Vector2(carriage.bounds.min.x+.06f,carriage.bounds.max.y-.16f),new Vector2(.24f,.075f));
        device.lamps=new[]{Lamp(crane.statusLamp,true,false),Lamp(beacon,true,false),Lamp(work,false,true)};device.RefreshLighting(true);EditorUtility.SetDirty(device);return device;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying || EditorApplication.isCompiling)throw new Exception("Requires idle Edit mode.");
        var original=SceneManager.GetActiveScene();int enemies=0,players=0,devices=0;var report=new System.Text.StringBuilder();
        foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);if(path.IndexOf("Backup",StringComparison.OrdinalIgnoreCase)>=0)continue;
            var scene=SceneManager.GetSceneByPath(path);bool loaded=scene.isLoaded;if(!loaded)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Backups/FreightLightsAndRanges_20261020_172239/Unsaved_"+scene.name+".unity",true);
            bool changed=false;
            foreach(var root in scene.GetRootGameObjects())
            {
                foreach(var e in root.GetComponentsInChildren<RampagerController>(true))
                {e.idealSightRange=11.946668f;e.watchSightMultiplier=1.4f;EditorUtility.SetDirty(e.GetComponent<RampagerPerception>());EditorUtility.SetDirty(e);enemies++;changed=true;}
                foreach(var n in root.GetComponentsInChildren<PlayerNoiseEmitter>(true))
                {n.footstepRadius=4.16f;EditorUtility.SetDirty(n);players++;changed=true;}
                foreach(var c in root.GetComponentsInChildren<KBKStackerCrane>(true)){Install(c);devices++;changed=true;}
            }
            if(changed){EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed: "+path);report.AppendLine(path);}
            if(!loaded)EditorSceneManager.CloseScene(scene,true);
        }
        const string prefab="Assets/RecoilPrototype/Prefabs/KBKStackerCrane.prefab";var content=PrefabUtility.LoadPrefabContents(prefab);
        try{Install(content.GetComponent<KBKStackerCrane>());PrefabUtility.SaveAsPrefabAsset(content,prefab);}finally{PrefabUtility.UnloadPrefabContents(content);}
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
        return "Enemies="+enemies+", player emitters="+players+", freight devices="+devices+"\n"+report;
    }
}
