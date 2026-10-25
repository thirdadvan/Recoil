using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseFreightDetailPass
{
    const string Marker="Freight thin raised underframe and drive details";
    [MenuItem("Recoil/Art Tests/Refine Warehouse Freight And Pendant Lining")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;EditorApplication.delayCall+=AfterPlay;return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        ApplyScene("Assets/Scenes/3D/RecoilFactoryWarehouse.unity");
        ApplyScene(WarehouseInsideLightingTestBuilder.Destination);
    }
    static void ApplyScene(string path)
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var backup="Backups/WarehouseFreightRefinement/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+".unity",true);
            var roots=scene.GetRootGameObjects();var crane=roots.SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).Single();
            var device=crane.GetComponent<FreightEquipmentLighting>();var noise=crane.GetComponent<FreightNoiseEmitter>();
            var steel=Mat("Freight exposed guide steel",new Color(.24f,.29f,.28f),.65f);var dark=Mat("Freight motor dark enamel",new Color(.08f,.12f,.11f),.35f);
            var yellow=Mat("Freight thin deck ochre",new Color(.66f,.49f,.15f),.35f);var lens=Mat("Freight powered green lens",new Color(.04f,.09f,.05f),.1f);
            if(!crane.transform.Find(Marker))
            {
                var marker=G(Marker,crane.transform,crane.transform.position);
                // Raise the entire lifting assembly, including its bearing collider, by a small lip.
                crane.liftCarriage.position+=Vector3.up*.16f;Physics2D.SyncTransforms();var area=crane.forkPlatform.Area;
                var deckVisual=crane.forkPlatform.GetComponent<Recoil3DSpriteVisual>();var bounds=deckVisual&&deckVisual.meshRenderer?deckVisual.meshRenderer.bounds:new Bounds(crane.forkPlatform.transform.position,new Vector3(area.width,.12f,2.4f));
                float z=bounds.center.z,depth=Mathf.Max(1.5f,bounds.size.z),top=area.yMax;
                foreach(var v in crane.liftCarriage.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
                    if(v.source&&(v.source.name.StartsWith("Fork deck")||v.source.name.StartsWith("Fork front face")||v.source.name.StartsWith("Fork bright upper")||v.source.name.StartsWith("Fork underside")||v.source.name.StartsWith("Black hazard stripe")||v.source.name.StartsWith("Fork corner"))){v.suppressed=true;v.Sync();}
                foreach(var r in crane.liftCarriage.GetComponentsInChildren<MeshRenderer>(true))if(r.name=="Carriage depth linkage - under deck")r.enabled=false;
                var deck=G("Thin fork deck - bearing matches soft platform",crane.liftCarriage,new Vector3(area.center.x,top,z));
                Shape("Thin steel walking plate",deck,new Vector3(0,-.035f,0),new Vector3(area.width,.07f,depth),yellow);
                foreach(float x in new[]{-area.width*.34f,area.width*.34f})Shape("Exposed underside runner",deck,new Vector3(x,-.105f,0),new Vector3(.12f,.06f,depth-.12f),steel);
                Shape("Thin foreground deck edge",deck,new Vector3(0,-.06f,-depth*.5f),new Vector3(area.width,.09f,.055f),steel);
                var carriage=crane.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Fork carriage");
                var carrierVisual=carriage.GetComponent<Recoil3DSpriteVisual>();var cb=carrierVisual&&carrierVisual.meshRenderer?carrierVisual.meshRenderer.bounds:carriage.bounds;
                var linkage=G("Thin fork linkage and guide rollers",crane.liftCarriage,new Vector3(cb.center.x,top-.105f,cb.center.z));
                Shape("Reduced depth linkage",linkage,new Vector3(0,0,(z-cb.center.z)*.5f),new Vector3(.32f,.06f,Mathf.Max(.5f,z-cb.center.z)),steel);
                foreach(float y in new[]{.24f,.72f})foreach(float x in new[]{-.21f,.21f})
                {var roller=Shape("Visible carriage guide roller",linkage,new Vector3(x,y,-.18f),new Vector3(.12f,.055f,.12f),steel,PrimitiveType.Cylinder);roller.localRotation=Quaternion.Euler(90,0,0);}
                var lamps=new List<FreightEquipmentLighting.Lamp>(device.lamps);
                var control=crane.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Ground switch housing");
                var cv=control.GetComponent<Recoil3DSpriteVisual>();var controlBounds=cv&&cv.meshRenderer?cv.meshRenderer.bounds:control.bounds;
                lamps.Add(Indicator(control.transform,"Control station - green supply indicator",new Vector3(controlBounds.center.x,controlBounds.max.y-.11f,controlBounds.min.z-.04f),lens,steel));
                lamps.Add(Indicator(crane.liftCarriage,"Lower carriage - green supply indicator",new Vector3(cb.center.x,cb.center.y,cb.min.z-.04f),lens,steel));
                device.lamps=lamps.ToArray();
                var motor=crane.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Motor and chain hoist");
                var mv=motor.GetComponent<Recoil3DSpriteVisual>();Vector3 motorPosition=mv&&mv.meshRenderer?mv.meshRenderer.bounds.center:motor.position;
                var drive=G("Hoist gearmotor - visual housing",crane.travelRig,motorPosition+Vector3.back*.18f);
                var casing=Shape("Hoist motor cylindrical casing",drive,Vector3.zero,new Vector3(.34f,.31f,.34f),dark,PrimitiveType.Cylinder);casing.localRotation=Quaternion.Euler(0,0,90);
                for(int i=0;i<6;i++){var fin=Shape("Motor cooling fin",drive,new Vector3(-.24f+i*.085f,0,0),new Vector3(.39f,.012f,.39f),steel,PrimitiveType.Cylinder);fin.localRotation=Quaternion.Euler(0,0,90);}
                Shape("Reduction gearbox",drive,new Vector3(.38f,-.02f,0),new Vector3(.22f,.30f,.30f),dark);
                if(!crane.GetComponent<FreightMotorAudio>())crane.gameObject.AddComponent<FreightMotorAudio>();
            }
            FixCarriageFace(crane,yellow,steel);
            var lining=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Deep shade - illuminated ivory lining.mat");
            if(lining){lining.SetColor("_EmissionColor",new Color(.19f,.24f,.205f));EditorUtility.SetDirty(lining);}
            device.RefreshLighting(true);EditorUtility.SetDirty(device);EditorUtility.SetDirty(noise);
            if(path==WarehouseInsideLightingTestBuilder.Destination)Capture(roots,crane,device);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            Debug.Log("WAREHOUSE_FREIGHT_DETAIL_READY: "+path+"; thin deck raised 0.16m; exposed runners and rollers; two power-linked green pilot lamps; sound follows moving fork platform; brighter lining.");
        }
        finally{if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);if(opened)EditorSceneManager.CloseScene(scene,true);}
    }
    static void AfterPlay(){if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=AfterPlay;return;}Apply();}
    static void FixCarriageFace(KBKStackerCrane crane,Material yellow,Material steel)
    {
        var old=crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true).First(v=>v.source&&v.source.name=="Fork carriage");
        var bounds=old.meshRenderer.bounds;
        // Old sprite extrusions put the body, cover and bolts on the same front plane.
        foreach(var v in crane.liftCarriage.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
            if(v.source&&(v.source.name=="Fork carriage"||v.source.name=="Carriage service cover"||v.source.name=="Carriage roller bolt"||v.source.name=="FREIGHT")){v.suppressed=true;v.Sync();}
        const string name="Solid carriage front - separated depth layers";
        var panel=crane.liftCarriage.Find(name);
        if(!panel)
        {
            panel=G(name,crane.liftCarriage,bounds.center+Vector3.back*.20f);
            Shape("One solid carriage shell",panel,Vector3.zero,new Vector3(bounds.size.x,bounds.size.y,.18f),yellow);
            var cover=Mat("Freight recessed service plate",new Color(.16f,.21f,.18f),.5f);
            Shape("Service cover - in front of shell",panel,new Vector3(0,0,-.11f),new Vector3(bounds.size.x*.68f,bounds.size.y*.72f,.02f),cover);
        }
        foreach(var t in crane.liftCarriage.GetComponentsInChildren<Transform>(true))
        {
            if(t.name=="Lower carriage - green supply indicator")t.position=new Vector3(bounds.center.x,bounds.center.y,panel.position.z-.14f);
            if(t.name=="Visible carriage guide roller"){var p=t.localPosition;p.z=-.38f;t.localPosition=p;}
        }
    }
    static Transform G(string name,Transform parent,Vector3 p)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=p;t.rotation=Quaternion.identity;var s=parent.lossyScale;t.localScale=new Vector3(1/Mathf.Abs(s.x),1/Mathf.Abs(s.y),1/Mathf.Abs(s.z));return t;}
    static Transform Shape(string name,Transform parent,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;return g.transform;}
    static Material Mat(string name,Color c,float metal)
    {string path="Assets/Recoil3D/ArtTests/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=c;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.3f);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;}
    static FreightEquipmentLighting.Lamp Indicator(Transform parent,string name,Vector3 p,Material lens,Material steel)
    {
        var root=G(name,parent,p);var ring=Shape("Metal retaining bezel",root,Vector3.zero,new Vector3(.10f,.023f,.10f),steel,PrimitiveType.Cylinder);ring.localRotation=Quaternion.Euler(90,0,0);
        var glass=Shape("Domed green pilot lens",root,new Vector3(0,0,-.025f),new Vector3(.075f,.075f,.035f),lens,PrimitiveType.Sphere).GetComponent<Renderer>();
        var light=new GameObject("Local green spill - powered only").AddComponent<Light>();light.transform.SetParent(root,false);light.transform.localPosition=Vector3.back*.14f;light.type=LightType.Point;light.range=1.7f;light.intensity=.75f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.ForcePixel;light.enabled=false;
        return new FreightEquipmentLighting.Lamp{meshLens=glass,realtimeLight=light,statusIndicator=true,powerOnlyIndicator=true,offColor=new Color(.035f,.065f,.04f)};
    }
    static void Capture(GameObject[] roots,KBKStackerCrane crane,FreightEquipmentLighting device)
    {
        var study=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();var camera=study.GetComponent<Camera>();var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;
        var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;string power=crane.requiredPowerItem;
        try
        {
            if(follow)follow.enabled=false;var area=crane.forkPlatform.Area;
            camera.transform.SetPositionAndRotation(new Vector3(area.center.x,2.5f,-11),Quaternion.Euler(5,0,0));camera.fieldOfView=42;
            study.ApplyLook();var budget=camera.GetComponent<WarehouseShadowBudget>();if(budget)budget.Apply();
            WarehousePathLightingPass.Capture(camera,"FreightRefit_Unpowered");
            crane.requiredPowerItem="";device.RefreshLighting(true);WarehousePathLightingPass.Capture(camera,"FreightRefit_Powered");
        }
        finally{crane.requiredPowerItem=power;device.RefreshLighting(true);camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(follow)follow.enabled=on;}
    }
}
