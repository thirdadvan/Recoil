using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryYardSirenLightingPass
{
    const string Outside="Assets/Scenes/3D/RecoilPrototype.unity",Marker="Factory spacing tower and streetlight revision";
    static Transform[] All(Scene s){return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();}
    static void Require(bool ok,string why){if(!ok)throw new Exception(why);}
    [MenuItem("Recoil/Art Tests/Revise Factory Yard Tower And Broadcast")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
        if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Cannot save scene");
        string backup="Backups/FactoryYardSiren/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        var scene=SceneManager.GetSceneByPath(Outside);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Outside);
        var all=All(scene);var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).Single(w=>w);var tower=world.GetComponentInChildren<TowerSirenTrigger>(true);
        if(world.transform.Find(Marker))return "Already applied; use verification to inspect.";
        EditorSceneManager.SaveScene(scene,backup+"/RecoilPrototype.unity",true);
        var shift=Vector3.right*5.5f;var factory=world.transform.Find("Factory Building - four storeys");float oldFactoryX=factory.position.x;
        foreach(string name in new[]{"Factory Building - four storeys","Factory warehouse yard and sewer - editable","3D factory yard - editable perimeter truck and gate","Factory yard - relocated midground industry","Truck - soft canvas roof","Factory Checkpoint"})world.transform.Find(name).position+=shift;
        // Legacy boundary collision and detached background details must move with the enclosure.
        foreach(var t in all.Where(t=>t.parent&&(t.parent.name=="01 Background"||t.parent.name=="02 Terrain and Cover"||t.parent.name=="05 Lighting")&&t.position.x>9.5f))t.position+=shift;
        foreach(var actor in world.GetComponentsInChildren<RampagerController>(true).Where(a=>a.transform.position.x>oldFactoryX&&!a.transform.IsChildOf(factory)))actor.transform.position+=shift;
        foreach(var t in all.Where(t=>t.name=="Factory left return wall into Z"||t.name=="Factory right return wall into Z"))Require(t.position.x>oldFactoryX,"Factory return walls failed to move");
        // Stretch the steelwork, preserving the dimensions of platforms, sirens and equipment.
        foreach(string name in new[]{"01 Steel lattice","02 Rear frame and cables"}){var t=tower.transform.Find(name);var sc=t.localScale;sc.y*=1.5f;t.localScale=sc;}
        var platforms=tower.transform.Find("03 Maintenance platforms");
        foreach(Transform t in platforms){float deck=Mathf.Round((t.position.y-8)/6)*6+8;var p=t.position;p.y+=deck*.5f;t.position=p;}
        var array=tower.transform.Find("04 Siren array and signal equipment");
        foreach(Transform t in array){var p=t.position;p.y*=1.5f;t.position=p;}
        var ladder=tower.GetComponentInChildren<ClimbableLadder>(true);var lp=ladder.transform.position;lp.y*=1.5f;lp.z=world.GetComponentInChildren<PlayerDepthPlane>(true).PlatformZ;ladder.transform.position=lp;
        var box=ladder.GetComponent<BoxCollider2D>();var size=box.size;size.y*=1.5f;box.size=size;
        foreach(Transform t in ladder.transform){var p=t.position; // Undo parent translation before scaling original world heights below.
            p.y=(t.position.y-lp.y/3)*1.5f;t.position=p;
            if(t.name.Contains("rail")){var sc=t.localScale;sc.y*=1.5f;t.localScale=sc;}}
        var rungs=ladder.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.name.StartsWith("Ladder rung")&&!v.name.StartsWith("3D")).OrderBy(v=>v.transform.position.y).ToArray();
        float bottom=.65f,top=ladder.Area.yMax-.2f;
        for(int i=0;i<rungs.Length;i++){var p=rungs[i].transform.position;p.y=bottom+i*.6f;rungs[i].transform.position=p;}
        if(rungs.Length>0)for(int i=rungs.Length;bottom+i*.6f<top;i++){var t=UnityEngine.Object.Instantiate(rungs[0].gameObject,ladder.transform).transform;t.name="Ladder rung extended "+i;var p=t.position;p.y=bottom+i*.6f;t.position=p;}
        float z=lp.z;
        foreach(var v in tower.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
        {
            if(v.suppressed||!v.meshRenderer)continue;
            if(v.GetComponentInParent<ClimbableLadder>()){v.depthOffset=0;v.depth=v.name.Contains("rung")?.10f:.08f;}
            else if(v.GetComponentInParent<SoftRoofPlatform>()){v.depth=3;v.depthOffset=(z+1.5f-v.transform.position.z)/v.transform.lossyScale.z;}
            v.Sync();EditorUtility.SetDirty(v);
        }
        Physics2D.SyncTransforms();
        Require(Mathf.Abs(tower.highestPlatform.position.y-30)<.01f,"Tower platform height");Require(ladder.Area.yMax>tower.highestPlatform.position.y+1,"Ladder must reach last platform");
        // The visual yaw leaves 2D physics upright; compute the section through the playable plane.
        var truck=world.transform.Find("Truck - soft canvas roof");var model=truck.Find("Long closed cargo truck - reference orientation");model.localRotation=Quaternion.Euler(0,15,0);
        var barrier=truck.Find("Loading lane closed beside truck nose");barrier.localRotation=model.localRotation;
        float c=Mathf.Cos(15*Mathf.Deg2Rad),sn=Mathf.Sin(15*Mathf.Deg2Rad);float section=-.5f/c+Mathf.Tan(15*Mathf.Deg2Rad)*z;
        var cargo=truck.Find("Truck solid cargo body");var cp=cargo.position;cp.x=truck.position.x+section;cargo.position=cp;var cs=cargo.localScale;cs.x=5.8f/c;cargo.localScale=cs;
        foreach(var roof in truck.GetComponentsInChildren<SoftRoofPlatform>(true)){var b=roof.GetComponent<BoxCollider2D>();var p=roof.transform.position;p.x=truck.position.x+section;roof.transform.position=p;var bs=b.size;bs.x=(5.9f/c)/Mathf.Abs(roof.transform.lossyScale.x);b.size=bs;var o=b.offset;o.x=0;b.offset=o;}
        SetupLights(world,scene);
        var waves=world.GetComponentInChildren<SoundWaveManager>(true);waves.directionWaveDuration=waves.warningDuration;waves.commandReactionDelay=.5f;waves.drawBroadcastCircles=false;
        var outsideFx=All(scene).Select(t=>t.GetComponent<BroadcastRefractionEffect>()).First(f=>f);outsideFx.emitter=tower.waveOrigin;
        var marker=new GameObject(Marker);marker.transform.SetParent(world.transform,false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        int rooms=ConfigureInteriorSources(scene,tower.waveOrigin.position,factory.position.x,backup);
        SceneManager.SetActiveScene(scene);world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();Capture(world);
        Debug.Log("FACTORY_YARD_SIREN_PASS: factory/enclosure +5.5m; tower +50%; ladder/platform front Z="+z+"; truck yaw15; 3 right-side poles / one lit; interiors="+rooms+". Backup="+backup);
        return backup;
    }
    static void SetupLights(RecoilPrototype world,Scene scene)
    {
        var existing=world.GetComponentsInChildren<RecoilStreetLamp>(true).ToArray();var template=existing.OrderByDescending(l=>l.Origin.x).First();var templateRoot=template.transform;
        while(templateRoot.parent&&templateRoot.parent!=world.transform&&!templateRoot.Find("Native 6m street lamp - foreground"))templateRoot=templateRoot.parent;
        var group=new GameObject("Factory right yard - three street lights one working").transform;group.SetParent(world.transform,false);
        float right=world.transform.Find("Factory Building - four storeys").position.x+32;
        for(int i=0;i<3;i++)
        {
            var copy=UnityEngine.Object.Instantiate(templateRoot,group,true);copy.name="Yard street light "+(i+1)+(i==1?" - working":" - failed");copy.position=new Vector3(right+4+i*9,0,-1.6f);
            var lamp=copy.GetComponentInChildren<RecoilStreetLamp>(true);lamp.initiallyOn=i==1;lamp.unstableStreetLamp=false;lamp.ResetLamp();
            foreach(var visual in copy.GetComponentsInChildren<Recoil3DStreetLampVisual>(true))visual.Sync();
        }
        foreach(var lamp in world.GetComponentsInChildren<RecoilStreetLamp>(true))
        {
            foreach(var sync in lamp.lampHead.GetComponentsInChildren<Recoil3DLight>(true))
            {
                if(sync.lightSource.type==LightType.Spot){sync.lightSource.spotAngle=72;sync.lightSource.innerSpotAngle=36;sync.lightSource.range=11;}
                else {sync.lightSource.range=6.5f;sync.intensityMultiplier=4;}
                sync.Sync();EditorUtility.SetDirty(sync);
            }
        }
        var camera=All(scene).Select(t=>t.GetComponent<Camera>()).First(c=>c&&c.CompareTag("MainCamera"));var look=camera.GetComponent<WarehouseAtmosphereTest>();if(!look)look=camera.gameObject.AddComponent<WarehouseAtmosphereTest>();
        look.manageSceneLighting=false;look.testLook=true;look.showTestControls=false;look.effectShader=Shader.Find("Recoil/Art Tests/Warehouse Atmosphere");look.scattering=.15f;look.softGlow=.12f;look.volumeSteps=24;look.volumeResolutionDivider=3;
        look.originalLights=new Light[0];look.decorativeLights=new Light[0];
        look.lamps=world.GetComponentsInChildren<Recoil3DLight>(true).Where(l=>l.streetLamp&&l.lightSource&&l.lightSource.type==LightType.Spot).Select(l=>l.lightSource).Distinct().ToArray();EditorUtility.SetDirty(look);
        Require(group.GetComponentsInChildren<RecoilStreetLamp>(true).Count(l=>l.IsOn)==1,"Exactly one right yard lamp must work");
    }
    static int ConfigureInteriorSources(Scene exterior,Vector3 origin,float exteriorLeft,string backup)
    {
        int count=0;
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")&&e.path!=Outside))
        {
            var s=SceneManager.GetSceneByPath(entry.path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);
            try
            {
                var waves=All(s).Select(t=>t.GetComponent<SoundWaveManager>()).FirstOrDefault(w=>w&&w.buildingBroadcastsEnabled);if(!waves)continue;
                EditorSceneManager.SaveScene(s,backup+"/"+s.name+".unity",true);
                var source=waves.transform.Find("Broadcast Speaker");if(!source)throw new Exception("Missing emitter "+s.name);
                source.position=new Vector3(origin.x-exteriorLeft,origin.y,origin.z);waves.directionWaveDuration=waves.warningDuration;waves.commandReactionDelay=.5f;waves.drawBroadcastCircles=false;
                foreach(var fx in All(s).Select(t=>t.GetComponent<BroadcastRefractionEffect>()).Where(f=>f)){fx.emitter=source;EditorUtility.SetDirty(fx);}
                EditorUtility.SetDirty(waves);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);count++;
            }
            finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(exterior);}
        }
        return count;
    }
    static void Capture(RecoilPrototype world)
    {
        var camera=world.GetComponentsInChildren<Camera>(true).FirstOrDefault();if(!camera)camera=Camera.main;var p=camera.transform.position;var r=camera.transform.rotation;float f=camera.fieldOfView;
        var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;
        try{if(follow)follow.enabled=false;camera.transform.position=new Vector3(15,18,-40);camera.transform.LookAt(new Vector3(22,16,3));camera.fieldOfView=57;WarehousePathLightingPass.Capture(camera,"FactoryRevision_TowerAndWall");camera.transform.position=new Vector3(68,9,-30);camera.transform.LookAt(new Vector3(63,4,2));camera.fieldOfView=48;WarehousePathLightingPass.Capture(camera,"FactoryRevision_Truck");camera.transform.position=new Vector3(94,5,-20);camera.transform.LookAt(new Vector3(94,2,1));camera.fieldOfView=48;WarehousePathLightingPass.Capture(camera,"FactoryRevision_StreetLights");}
        finally{camera.transform.SetPositionAndRotation(p,r);camera.fieldOfView=f;if(follow)follow.enabled=on;}
    }
}
