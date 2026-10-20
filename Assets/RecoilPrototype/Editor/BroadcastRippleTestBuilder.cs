using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class BroadcastRippleTestBuilder
{
    public const string TestScene="Assets/Scenes/Tests/BroadcastRippleTest.unity";
    [MenuItem("Recoil/Tests/Create Broadcast Refraction Scene")]
    public static void Create()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        if(File.Exists(TestScene))throw new InvalidOperationException("Test scene already exists; edit it directly.");
        Directory.CreateDirectory("Assets/Scenes/Tests");AssetDatabase.Refresh();
        if(!AssetDatabase.CopyAsset("Assets/Scenes/RecoilFactoryInterior.unity",TestScene))throw new InvalidOperationException("Could not copy the factory scene.");
        var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(TestScene,OpenSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
            world.name="RECOIL - Broadcast Refraction Test";world.locationLabel="BROADCAST REFRACTION TEST";
            world.enableCheckpoint=false;world.legacyEvidenceEnabled=false;world.startWithWaterGun=true;
            world.objectiveWithoutCard=world.objectiveWithCard="B: broadcast / 1: expand / 2: contract / V: refraction on-off / C: original rings";
            foreach(var enemy in world.GetComponentsInChildren<RampagerController>(true))enemy.gameObject.SetActive(false);
            foreach(var door in world.GetComponentsInChildren<RecoilDoor>(true))door.enabled=false;
            var so=new SerializedObject(world);
            var camera=(Camera)so.FindProperty("cam").objectReferenceValue;
            var spawn=(Transform)so.FindProperty("spawnPoint").objectReferenceValue;
            if(spawn)spawn.position=new Vector2(13,.845f);
            var controller=camera.GetComponent<RecoilCameraController>();controller.viewSize=5.4f;controller.followOffset=new Vector2(0,3.2f);controller.maximumMouseOffset=0;controller.minimumY=4;controller.maximumY=18;controller.preserveFootFraming=true;
            camera.orthographicSize=5.4f;camera.transform.position=new Vector3(13,4,-10);
            var manager=world.GetComponentInChildren<SoundWaveManager>(true);
            manager.buildingBroadcastsEnabled=true;manager.drawBroadcastCircles=false;manager.initialDelay=5;manager.interval=6;
            var effect=camera.gameObject.AddComponent<BroadcastRefractionEffect>();
            effect.refractionShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RecoilPrototype/Effects/BroadcastRefraction.shader");
            effect.broadcast=manager;effect.strength=.28f;effect.packetWidth=4;effect.wavelength=1.1f;
            var controls=camera.gameObject.AddComponent<BroadcastRippleTestControls>();controls.world=world;controls.effect=effect;controls.broadcast=manager;
            var lightGroup=new GameObject("Test reference lights - comparison aid").transform;lightGroup.SetParent(world.transform,false);
            for(int i=0;i<3;i++)
            {
                var fixture=new GameObject("Reference light "+(i+1)).transform;fixture.SetParent(lightGroup,false);fixture.localPosition=new Vector3(6+i*10,4.2f,0);
                var point=new GameObject("Light - fixture child");point.transform.SetParent(fixture,false);var lamp=point.AddComponent<RecoilLight2D>();lamp.radius=8;lamp.intensity=.65f;
            }
            controls.referenceLights=lightGroup.gameObject;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        finally
        {
            SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);
        }
        Debug.Log("BROADCAST_REFRACTION_TEST_CREATED: "+TestScene);
    }
    [MenuItem("Recoil/Tests/Add Refraction To 3D Factory Interior")]
    public static void Install3D()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        const string path="Assets/Scenes/RecoilFactoryArt3D.unity";
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            string folder="Backups/BroadcastRefraction3D/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(folder);EditorSceneManager.SaveScene(scene,folder+"/FactoryArt3D.unity",true);
            var camera=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();
            // Appended after FactoryArtCamera, preserving its existing colour-grading pass.
            var effect=camera.GetComponent<BroadcastRefractionEffect>();if(!effect)effect=camera.gameObject.AddComponent<BroadcastRefractionEffect>();
            effect.refractionShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RecoilPrototype/Effects/BroadcastRefraction.shader");
            effect.broadcast=null;effect.emitter=null;effect.effectEnabled=true;effect.perspectiveReferenceDistance=18;
            effect.strength=1f;effect.packetWidth=4.6f;effect.wavelength=1.2f;effect.irregularity=.25f;
            var demo=camera.GetComponent<BroadcastRefractionDemo3D>();if(!demo)demo=camera.gameObject.AddComponent<BroadcastRefractionDemo3D>();demo.effect=effect;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        }
        finally
        {
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(opened && scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
        Debug.Log("3D_FACTORY_REFRACTION_INSTALLED");
    }
}
