using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FactoryYardSirenVerification
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    public static string Verify()
    {
        var scene=SceneManager.GetActiveScene();var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();var tower=world.GetComponentInChildren<TowerSirenTrigger>(true);var plane=world.GetComponentInChildren<PlayerDepthPlane>(true);var ladder=tower.GetComponentInChildren<ClimbableLadder>(true);
        Physics2D.SyncTransforms();Check(Mathf.Abs(ladder.transform.position.z-plane.PlatformZ)<.001f,"Ladder depth");
        foreach(var roof in tower.GetComponentsInChildren<SoftRoofPlatform>(true))
        {var visual=roof.GetComponent<Recoil3DSpriteVisual>();Check(Mathf.Abs(visual.meshRenderer.bounds.min.z-plane.PlatformZ)<.01f,"Platform front edge depth");Check(Mathf.Abs(visual.meshRenderer.bounds.max.y-roof.Area.yMax)<.01f,"Platform top height");}
        Check(Mathf.Abs(tower.highestPlatform.position.y-30)<.01f,"Top platform height");Check(Mathf.Abs(tower.waveOrigin.position.y-37.275f)<.02f,"Siren source height");
        var group=world.transform.Find("Factory right yard - three street lights one working");Check(group.GetComponentsInChildren<RecoilStreetLamp>(true).Length==3,"Three added poles");Check(group.GetComponentsInChildren<RecoilStreetLamp>(true).Count(l=>l.IsOn)==1,"Only one pole powered");
        var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"));var look=camera.GetComponent<WarehouseAtmosphereTest>();Check(look&&!look.manageSceneLighting,"Outdoor volume must preserve atmosphere");Check(!ShaderUtil.ShaderHasError(look.effectShader),"Volume shader error");
        var truck=world.transform.Find("Truck - soft canvas roof");float yaw=truck.Find("Long closed cargo truck - reference orientation").eulerAngles.y;Check(Mathf.Abs(yaw-15)<.01f,"Truck angle");
        // Match both blocking collision and the soft roof to the angled model's section.
        var model=truck.Find("Long closed cargo truck - reference orientation");var deck=model.GetComponentsInChildren<Renderer>().First(r=>r.name=="Flat cargo roof");
        foreach(var roof in truck.GetComponentsInChildren<SoftRoofPlatform>(true))
        {var collider=roof.GetComponent<BoxCollider2D>();var offset=collider.offset;offset.y+=roof.transform.InverseTransformVector(Vector3.up*(deck.bounds.max.y-roof.Area.yMax)).y;collider.offset=offset;EditorUtility.SetDirty(collider);}
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        VerifyTiming();
        Debug.Log("FACTORY_YARD_SIREN_VERIFIED: tower platform tops/edges and ladder align; roof/collision synchronized to angled truck; exactly 1 of 3 new lamps on; shader OK; command movement waits starts .5 seconds after pulse onset.");
        return "Verified tower, truck, lamp state, shader and wave timing.";
    }
    public static string RefineTruckAndReaction()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
        if(!EditorSceneManager.SaveOpenScenes())throw new Exception("Save failed");
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilPrototype.unity");
        string backup="Backups/FactoryReactionRefinement/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");System.IO.Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/RecoilPrototype.unity",true);
        var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();
        var truck=world.transform.Find("Truck - soft canvas roof");var model=truck.Find("Long closed cargo truck - reference orientation");model.localRotation=Quaternion.Euler(0,15,0);truck.Find("Loading lane closed beside truck nose").localRotation=model.localRotation;
        float z=world.GetComponentInChildren<PlayerDepthPlane>(true).PlatformZ,c=Mathf.Cos(15*Mathf.Deg2Rad),section=-.5f/c+Mathf.Tan(15*Mathf.Deg2Rad)*z;
        var cargo=truck.Find("Truck solid cargo body");var p=cargo.position;p.x=truck.position.x+section;cargo.position=p;var scale=cargo.localScale;scale.x=5.8f/c;cargo.localScale=scale;
        foreach(var roof in truck.GetComponentsInChildren<SoftRoofPlatform>(true))
        {var collider=roof.GetComponent<BoxCollider2D>();p=roof.transform.position;p.x=truck.position.x+section;roof.transform.position=p;var size=collider.size;size.x=5.9f/c/Mathf.Abs(roof.transform.lossyScale.x);collider.size=size;}
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith("Assets/Scenes/3D/")))
        {
            var s=SceneManager.GetSceneByPath(entry.path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(entry.path,OpenSceneMode.Additive);
            try{var managers=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SoundWaveManager>(true)).ToArray();if(managers.Length>0){if(s!=scene)EditorSceneManager.SaveScene(s,backup+"/"+s.name+".unity",true);foreach(var manager in managers){manager.commandReactionDelay=.5f;EditorUtility.SetDirty(manager);}EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);}}
            finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(scene);}
        }
        world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();
        Verify();return backup;
    }
    static void VerifyTiming()
    {
        var original=SceneManager.GetActiveScene();var test=EditorSceneManager.NewPreviewScene();
        try
        {
            var go=new GameObject("Temporary broadcast timing verification");SceneManager.MoveGameObjectToScene(go,test);var world=go.AddComponent<RecoilPrototype>();var waves=go.AddComponent<SoundWaveManager>();waves.drawBroadcastCircles=false;waves.buildingBroadcastsEnabled=false;waves.Initialize(world);waves.directionWaveDuration=waves.warningDuration;waves.commandReactionDelay=.5f;
            Check(waves.StartBroadcastAt(Vector2.zero),"Start broadcast");waves.Simulate(waves.warningDuration);Check(waves.Phase==SoundWaveManager.BroadcastPhase.Commands,"First command");
            float delay=waves.CommandMoveDelay;
            // Frame subdivision must not let any move slice leak into the reaction gap.
            for(int i=0;i<49;i++){waves.Simulate(.01f);Check(waves.MoveStep<.00001f,"Monster moves before reaction delay");}
            Check(Mathf.Abs(delay-.5f)<.001f,"Pulse must give .5-second movement delay from onset");waves.Simulate(.03f);Check(waves.MoveStep>.001f,"Monster never moves after cue");
            Check(Mathf.Abs(waves.DirectionVisualDuration-waves.warningDuration)<.001f,"Warning and commands differ in visual duration");
        }
        finally{foreach(var root in test.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(test);SceneManager.SetActiveScene(original);}
    }
}
