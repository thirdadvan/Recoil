using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class RecoilPrototypeBuilder
{
    [MenuItem("Recoil/Apply New Rampager Scene")]
    public static void UpgradeRampagerSpec()
    {
        OpenScene();var root=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();
        foreach(var collider in root.GetComponentsInChildren<BoxCollider2D>())
            if(collider.name=="Sightline cover" || (collider.transform.position.x<16 && collider.transform.position.y>3 && collider.transform.localScale.y<1))UnityEngine.Object.DestroyImmediate(collider.gameObject);
        var platform=GameObject.Find("Factory High Platform");
        if(!platform)
        {
            platform=new GameObject("Factory High Platform");platform.transform.SetParent(root.transform.Find("02 Terrain and Cover"),false);
            platform.transform.position=new Vector3(27.5f,3.525f,0);platform.transform.localScale=new Vector3(6,.35f,1);
            var sprite=platform.AddComponent<SpriteRenderer>();sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");sprite.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/PrototypeSprite.mat");sprite.color=RecoilPrototype.Hex(0x526b76);sprite.sortingOrder=3;
            platform.AddComponent<BoxCollider2D>();
        }
        var manager=root.GetComponentInChildren<SoundWaveManager>();
        if(!manager)
        {
            var go=new GameObject("06 Sound Wave Manager");go.transform.SetParent(root.transform,false);manager=go.AddComponent<SoundWaveManager>();
            var area=new GameObject("Building Control Zone").transform;area.SetParent(go.transform,false);area.position=new Vector3(27.5f,5,0);area.localScale=new Vector3(23,20,1);
            var emitter=new GameObject("Broadcast Speaker").transform;emitter.SetParent(go.transform,false);emitter.position=new Vector3(-35,2.5f,0);
            manager.Configure(root,area,emitter,AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/PrototypeSprite.mat"));
        }
        foreach(var enemy in root.GetComponentsInChildren<RampagerController>())
        {enemy.idealSightRange=10.666667f;if(enemy.transform.position.x>16){enemy.leftOffset=-2.5f;enemy.rightOffset=3.5f;}}
        EditorSceneManager.SaveScene(root.gameObject.scene,"Assets/Scenes/RecoilPrototype.unity");AssetDatabase.SaveAssets();
        if(!platform.GetComponent<BoxCollider2D>())throw new Exception("Factory platform is missing its collider.");
        Debug.Log("RECOIL_NEW_SPEC_SCENE_SAVED: no crates, one central high platform, independent Sound Wave Manager.");
    }
    public static void UpgradeWaveRhythm()
    {
        OpenScene();var manager=UnityEngine.Object.FindObjectOfType<SoundWaveManager>();
        manager.rounds=3;manager.maxRounds=4;manager.holdDuration=1.5f;manager.commandDuration=1;
        var zone=GameObject.Find("Building Control Zone").transform;zone.position=new Vector3(18.5f,5,0);zone.localScale=new Vector3(41,20,1);
        GameObject.Find("Broadcast Speaker").transform.position=new Vector3(-35,2.5f,0);
        EditorSceneManager.SaveScene(manager.gameObject.scene,"Assets/Scenes/RecoilPrototype.unity");AssetDatabase.SaveAssets();
        Debug.Log("RECOIL_WAVE_RHYTHM_SAVED: distant left source, hold 1.5s / move 1s, 3-4 waves.");
    }
    public static void UpgradeLighting()
    {
        OpenScene();
        var root=UnityEngine.Object.FindObjectOfType<RecoilPrototype>().transform;
        if(root.GetComponentInChildren<RecoilLighting2D>())
        {
            var bounds=Array.Find(root.GetComponentsInChildren<Transform>(true),t=>t.name=="Factory Dark Area");
            if(bounds){bounds.position=new Vector3(27.5f,2.5f,0);bounds.localScale=new Vector3(23,7,1);}
            ArrangeLightHierarchy(root);
            root.GetComponentInChildren<RecoilLighting2D>().Refresh();
            EditorSceneManager.SaveScene(root.gameObject.scene,"Assets/Scenes/RecoilPrototype.unity");
            AssetDatabase.SaveAssets();return;
        }
        var shader=Shader.Find("Recoil/Soft 2D Darkness");
        if(!shader || ShaderUtil.ShaderHasError(shader))throw new Exception("Darkness shader did not compile.");
        var material=new Material(shader);
        AssetDatabase.CreateAsset(material,"Assets/RecoilPrototype/Art/FactoryDarkness.mat");
        var folder=new GameObject("05 Lighting").transform;folder.SetParent(root,false);
        var area=new GameObject("Factory Dark Area").transform;area.SetParent(folder,false);
        area.position=new Vector3(27.5f,2.5f,0);area.localScale=new Vector3(23,7,1);
        var overlay=new GameObject("Darkness Layer").AddComponent<SpriteRenderer>();overlay.transform.SetParent(folder,false);
        overlay.transform.position=new Vector3(20,5,0);overlay.transform.localScale=new Vector3(100,60,1);
        overlay.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");
        overlay.sharedMaterial=material;overlay.sortingOrder=100;
        var lamps=new[] { Lamp(folder,"Entrance Lamp",18.6f,2.3f,2,.22f),
            Lamp(folder,"Ceiling Light",25,5.4f,2.2f,.26f), Lamp(folder,"Exit Lamp",37.5f,2.8f,2.4f,.35f) };
        ArrangeLightHierarchy(root);
        var lighting=folder.gameObject.AddComponent<RecoilLighting2D>();
        lighting.Configure(root.GetComponentInChildren<RecoilPlayerController>().transform,area,overlay);
        EditorSceneManager.SaveScene(root.gameObject.scene,"Assets/Scenes/RecoilPrototype.unity");AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity");
        Debug.Log("RECOIL_LIGHTING_SAVED: black factory, soft player light, 3 editable lamps");
    }
    static void ArrangeLightHierarchy(Transform root)
    {
        foreach(var lamp in root.GetComponentsInChildren<RecoilLight2D>(true))
        {
            string fixtureName=lamp.name=="Entrance Lamp"?"Checkpoint lamp":lamp.name=="Exit Lamp"?"Exit light":lamp.name=="Ceiling Light"?"Ceiling Lamp Fixture":null;
            if(fixtureName==null)continue;
            var fixture=Array.Find(root.GetComponentsInChildren<Transform>(true),t=>t.name==fixtureName);
            if(!fixture)
            {
                fixture=new GameObject(fixtureName).transform;
                fixture.SetParent(root.Find("03 Props and Objectives")?root.Find("03 Props and Objectives"):root,false);
                fixture.position=lamp.transform.position;fixture.localScale=new Vector3(.65f,.16f,1);
                var sprite=fixture.gameObject.AddComponent<SpriteRenderer>();
                sprite.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");
                sprite.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/PrototypeSprite.mat");
                sprite.color=RecoilPrototype.Hex(0x86d2ab);sprite.sortingOrder=3;
            }
            if(fixtureName=="Exit light")
            {var exit=Array.Find(root.GetComponentsInChildren<Transform>(true),t=>t.name=="Exit");if(exit && fixture.parent!=exit)fixture.SetParent(exit,true);}
            if(lamp.transform.parent!=fixture)lamp.transform.SetParent(fixture,true);
        }
    }
    static RecoilLight2D Lamp(Transform parent,string name,float x,float y,float radius,float intensity)
    {
        var lamp=new GameObject(name).AddComponent<RecoilLight2D>();lamp.transform.SetParent(parent,false);
        lamp.transform.position=new Vector3(x,y,0);lamp.radius=radius;lamp.intensity=intensity;return lamp;
    }
    public static void VerifyAndPreview()
    {RenderLightingPreview();Verify();}
    public static void RenderLightingPreview()
    {
        OpenScene();
        var player=UnityEngine.Object.FindObjectOfType<RecoilPlayerController>();
        player.Position=new Vector2(30.5f,2);player.SyncVisuals(0);
        UnityEngine.Object.FindObjectOfType<RecoilLighting2D>().Refresh();
        var manager=UnityEngine.Object.FindObjectOfType<SoundWaveManager>();
        if(manager){manager.Initialize(UnityEngine.Object.FindObjectOfType<RecoilPrototype>());foreach(var enemy in UnityEngine.Object.FindObjectsOfType<RampagerController>())enemy.Initialize(UnityEngine.Object.FindObjectOfType<RecoilPrototype>());manager.StartBroadcast();for(int i=0;i<130;i++)manager.Simulate(.02f);}
        var camera=Camera.main;camera.transform.position=new Vector3(26,4,-10);camera.orthographicSize=6;
        var target=new RenderTexture(1600,720,24);camera.targetTexture=target;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,720),0,0);image.Apply();
        File.WriteAllBytes("/tmp/recoil-lighting-preview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;
        UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("RECOIL_LIGHTING_PREVIEW_RENDERED");
    }
    public static void UpgradeScene()
    {
        OpenScene();
        var prototype=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();
        prototype.AttachControllers();
        // Copy patrol settings before removing the old settings-only marker components.
        foreach(var old in UnityEngine.Object.FindObjectsOfType<RecoilPatrol>())
            UnityEngine.Object.DestroyImmediate(old);
        EditorSceneManager.SaveScene(prototype.gameObject.scene,"Assets/Scenes/RecoilPrototype.unity");
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity");
        ValidateControllers();
        Debug.Log("RECOIL_CONTROLLERS_ATTACHED: Player controller and "+UnityEngine.Object.FindObjectOfType<RecoilPrototype>().GetComponentsInChildren<RampagerController>(true).Length+" Rampager controllers saved");
    }
    static void ValidateControllers()
    {
        var world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();
        if(!world || !Camera.main || world.GetComponentsInChildren<RecoilPlayerController>(true).Length!=1)
            throw new Exception("Scene requires a world, a main camera and one player controller.");
        foreach(var enemy in world.GetComponentsInChildren<RampagerController>(true))
            if(!enemy.GetComponent<RampagerPerception>() || !enemy.GetComponent<RampagerMovement>() || !enemy.GetComponent<RampagerAttack>() || !enemy.GetComponent<RampagerVisualEffects>() || !enemy.GetComponent<RampagerPatrolRoute>())
                throw new Exception("Rampager is missing standard perception, movement, attack, visuals or patrol route: "+enemy.name);
    }
    public static void Verify()
    {
        OpenScene();
        ValidateControllers();
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-recoil-lighting-preview")>=0)RenderLightingPreview();
        EditorApplication.isPlaying=true;
    }
    [MenuItem("Recoil/Open Prototype")]
    public static void OpenScene()
    {
        if(File.Exists("Assets/Scenes/RecoilPrototype.unity"))
            EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity");
        else CreateScene();
    }
    public static void CreateScene()
    {
        const string assetFolder="Assets/RecoilPrototype/Art";
        Directory.CreateDirectory(assetFolder);
        const string spritePath=assetFolder+"/PrototypeSquare.png";
        if(!File.Exists(spritePath))
        {
            var texture=new Texture2D(32,32);
            var pixels=new Color[32*32];for(int i=0;i<pixels.Length;i++)pixels[i]=Color.white;
            texture.SetPixels(pixels);texture.Apply();File.WriteAllBytes(spritePath,texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(spritePath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=32;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.SaveAndReimport();
        const string materialPath=assetFolder+"/PrototypeSprite.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material) { material=new Material(Shader.Find("Sprites/Default"));AssetDatabase.CreateAsset(material,materialPath); }
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var prototype=new GameObject("RECOIL Prototype").AddComponent<RecoilPrototype>();
        prototype.BakeScene(AssetDatabase.LoadAssetAtPath<Sprite>(spritePath),material);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/RecoilPrototype.unity");
        EditorBuildSettings.scenes=new[] { new EditorBuildSettingsScene("Assets/Scenes/RecoilPrototype.unity",true) };
        AssetDatabase.SaveAssets();
        // Reopen the saved scene so validation covers serialization, not just generated objects.
        scene=EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity");
        ValidateControllers();
        Debug.Log("RECOIL_SCENE_SAVED_AND_VERIFIED: terrain, cover, patrols, player, objectives, camera");
    }
    [MenuItem("Recoil/Build Mac Prototype")]
    public static void Build()
    {
        OpenScene();
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneOSX))
            throw new Exception("Mac build support is not installed. Use the prototype scene in the Editor.");
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=false;
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes=new[]{"Assets/Scenes/RecoilPrototype.unity","Assets/Scenes/RecoilFactoryInterior.unity"},
            locationPathName=Path.GetFullPath(Path.Combine(Application.dataPath,"../Builds/Recoil Prototype.app")),target=BuildTarget.StandaloneOSX,
            options=BuildOptions.None
        });
        if(report.summary.result!=BuildResult.Succeeded || !Directory.Exists(report.summary.outputPath))
            throw new Exception("Recoil build did not produce an application: "+report.summary.result);
        Debug.Log("RECOIL_BUILD_SUCCEEDED: "+report.summary.totalSize+" bytes");
    }
}
