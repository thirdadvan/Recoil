using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// One-time authored addition. Existing outdoor objects and the factory are preserved.
public static class ServiceStationSceneBuilder
{
    const string Inside="Assets/Scenes/RecoilServiceStore.unity";
    static Sprite sprite;
    static Material material;
    static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static GameObject Box(string name,Vector2 p,Vector2 size,int color,int order,Transform parent,bool solid=false)
    {
        var g=new GameObject(name);g.transform.SetParent(parent,false);g.transform.position=p;g.transform.localScale=new Vector3(size.x,size.y,1);
        var r=g.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;
        if(solid)g.AddComponent<BoxCollider2D>();return g;
    }
    static void Label(string name,string text,Vector2 p,float scale,Transform parent)
    {
        var t=Group(name,parent);t.position=p;var m=t.gameObject.AddComponent<TextMesh>();m.text=text;m.fontSize=64;m.characterSize=scale;m.anchor=TextAnchor.MiddleCenter;m.color=new Color(.64f,.75f,.68f);m.GetComponent<MeshRenderer>().sortingOrder=6;
    }
    static void Set(UnityEngine.Object target,string name,UnityEngine.Object value)
    {var s=new SerializedObject(target);s.FindProperty(name).objectReferenceValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
    static RecoilDoor Door(GameObject g,RecoilPrototype world,string prompt,string destination,string spawn)
    {var d=g.AddComponent<RecoilDoor>();d.requiresClue=false;d.completesLevel=false;d.interactionPrompt=prompt;d.destinationScene=destination;d.destinationSpawn=spawn;return d;}
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit play mode.");
        if(System.IO.File.Exists(Inside))throw new InvalidOperationException("Store already exists; edit the saved scenes instead of regenerating.");
        var outdoor=SceneManager.GetActiveScene();if(outdoor.path!="Assets/Scenes/RecoilPrototype.unity")throw new InvalidOperationException("Open the outdoor scene first.");
        var world=outdoor.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>()).First();
        var serialized=new SerializedObject(world);sprite=(Sprite)serialized.FindProperty("sceneSprite").objectReferenceValue;material=(Material)serialized.FindProperty("sceneMaterial").objectReferenceValue;
        if(!sprite || !material)throw new InvalidOperationException("Missing prototype sprite/material.");
        float first=world.GetComponentsInChildren<RampagerController>().Min(e=>e.transform.position.x);
        float center=first-10; // Left of the first guard, clear of its patrol route.
        var station=Group("Service Station - editable exterior",world.transform);
        Box("Store facade",new Vector2(center,2.1f),new Vector2(12,4.2f),0x293940,-3,station);
        Box("Roof fascia",new Vector2(center,4.45f),new Vector2(12.4f,.35f),0x657575,2,station);
        var roof=Box("Service roof terrain",new Vector2(center,4.55f),new Vector2(12.4f,.25f),0x4d6065,3,station,true);roof.AddComponent<SoftRoofPlatform>();roof.GetComponent<BoxCollider2D>().isTrigger=true;
        Box("Shop sign",new Vector2(center,3.48f),new Vector2(9.8f,.55f),0x344b46,2,station);
        Label("Service sign text","SERVICE  /  MINI MART",new Vector2(center,3.5f),.085f,station);
        for(int i=0;i<4;i++){float x=center-2.5f+i*1.75f;Box("Shop window",new Vector2(x,1.8f),new Vector2(1.45f,1.7f),0x182b32,0,station);Box("Window crossbar",new Vector2(x,1.8f),new Vector2(1.45f,.055f),0x788179,1,station);}
        var entryObject=Box("Service store entrance",new Vector2(center-4.4f,1.3f),new Vector2(1.25f,2.6f),0x55716b,1,station);
        var entry=Door(entryObject,world,"E / ENTER SERVICE STORE",Inside,"Store Entrance Spawn");entry.rememberOnTravel="station_entrance_broken";
        Box("Door glass",new Vector2(center-4.4f,1.65f),new Vector2(.9f,1.25f),0x1f3135,2,station);
        var canopy=Group("Fuel pump canopy",station);float forecourt=center+8;
        Box("Canopy",new Vector2(forecourt,3.85f),new Vector2(4.8f,.28f),0x5d726b,1,canopy);
        Box("Canopy stripe",new Vector2(forecourt,3.64f),new Vector2(4.8f,.12f),0x9b5c45,2,canopy);
        for(int i=0;i<2;i++){float x=forecourt-1.4f+i*2.8f;Box("Canopy support",new Vector2(x,1.9f),new Vector2(.12f,3.8f),0x485b5e,-1,canopy);var pump=Group("Fuel pump "+(i+1),canopy);
            Box("Pump body",new Vector2(x,.8f),new Vector2(.65f,1.6f),0x98654c,1,pump);Box("Pump display",new Vector2(x,1.25f),new Vector2(.4f,.26f),0x122a2c,2,pump);Box("Pump hose",new Vector2(x+.4f,.65f),new Vector2(.06f,1.1f),0x131c20,2,pump);
        }
        var roofSpawn=Group("Service Roof Spawn",world.transform);roofSpawn.position=new Vector2(center,4.675f+.825f+.02f);
        Box("Roof skylight curb",new Vector2(center,4.8f),new Vector2(2,.15f),0x7f9896,4,station);
        Label("Roof skylight label","SKYLIGHT",new Vector2(center,5.05f),.055f,station);
        var spawn=(Transform)serialized.FindProperty("spawnPoint").objectReferenceValue;spawn.position=new Vector2(center-7.5f,1.1f);
        world.Player.transform.position=spawn.position;world.startWithWaterGun=false;
        world.enableCheckpoint=false;world.objectiveWithoutCard="Visit the service store. Find the water gun, then search the employee backpack.";
        // Move the parked car alongside the new start, preserving its existing children.
        var car=world.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Parked car");
        if(car){float delta=spawn.position.x+1.2f-car.position.x;car.position+=Vector3.right*delta;var cabin=world.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name=="Car cabin");if(cabin && !cabin.IsChildOf(car))cabin.position+=Vector3.right*delta;}
        var solids=world.GetComponentsInChildren<BoxCollider2D>();var floor=solids.Where(c=>c.bounds.size.x>50 && c.bounds.size.y<2).First();
        float minimum=spawn.position.x-5,maximum=floor.bounds.max.x;
        floor.transform.position=new Vector2((minimum+maximum)/2,floor.transform.position.y);floor.transform.localScale=new Vector3(maximum-minimum,floor.transform.localScale.y,1);
        var leftWall=solids.Where(c=>c.bounds.size.x<1 && c.bounds.size.y>10).OrderBy(c=>c.transform.position.x).First();leftWall.transform.position=new Vector2(minimum+.25f,leftWall.transform.position.y);
        EditorSceneManager.MarkSceneDirty(outdoor);EditorSceneManager.SaveScene(outdoor);
        // Create the separate interior from explicit objects, not a factory rebuild.
        var store=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(store);
        var root=new GameObject("RECOIL - Service Store");var context=root.AddComponent<RecoilPrototype>();context.locationLabel="SERVICE STORE";context.enableCheckpoint=false;context.legacyEvidenceEnabled=false;context.startWithWaterGun=false;
        context.objectiveWithoutCard="Find the water gun at the rear counter. Fly up and open the central skylight.";context.objectiveWithCard=context.objectiveWithoutCard;
        var s=new SerializedObject(context);s.FindProperty("sceneAuthored").boolValue=true;s.FindProperty("sceneSprite").objectReferenceValue=sprite;s.FindProperty("sceneMaterial").objectReferenceValue=material;s.ApplyModifiedPropertiesWithoutUndo();
        var terrain=Group("02 Terrain",root.transform);var props=Group("03 Props and objectives",root.transform);var backdrop=Group("01 Background",root.transform);var actors=Group("04 Characters",root.transform);
        Box("Store background",new Vector2(8,2.35f),new Vector2(17,5),0x1b2b31,-10,backdrop);
        Box("Store floor",new Vector2(8,-.5f),new Vector2(17,1),0x39484a,0,terrain,true);
        Box("Left wall",new Vector2(-.25f,2.4f),new Vector2(.5f,4.8f),0x39484a,1,terrain,true);
        Box("Right wall",new Vector2(16.25f,2.4f),new Vector2(.5f,4.8f),0x39484a,1,terrain,true);
        Box("Ceiling left",new Vector2(3.25f,4.7f),new Vector2(6.5f,.35f),0x39484a,1,terrain,true);
        Box("Ceiling right",new Vector2(12.75f,4.7f),new Vector2(6.5f,.35f),0x39484a,1,terrain,true);
        for(int i=0;i<3;i++){float x=4+i*3;Box("Shelf backing",new Vector2(x,1.35f),new Vector2(1.8f,2.5f),0x304047,-3,props);
            for(int level=0;level<3;level++){Box("Shelf",new Vector2(x,.55f+level*.7f),new Vector2(1.8f,.09f),0x61716a,-2,props);for(int k=0;k<5;k++)Box("Shop stock",new Vector2(x-.65f+k*.3f,.78f+level*.7f),new Vector2(.18f,.35f),k%2==0?0x766348:0x546c65,-2,props);}}
        Box("Rear counter",new Vector2(14.25f,.5f),new Vector2(2.6f,1),0x67594a,2,props,true);Box("Counter top",new Vector2(14.25f,1.05f),new Vector2(2.75f,.1f),0x999588,3,props);
        Label("Counter sign","LOST & FOUND",new Vector2(14.2f,2.2f),.065f,props);
        var pickup=Box("Water gun pickup",new Vector2(13.05f,1.4f),new Vector2(.65f,.25f),0x50ceef,6,props).AddComponent<ContinuousInteractionItem>();pickup.itemId="water_gun_pickup";pickup.displayName="Water gun";pickup.description="A plastic water gun. Use its recoil to reach the skylight.";pickup.interactionDuration=0;pickup.interactionRange=1.8f;pickup.resultId="water_gun";pickup.resultMessage="Fire, then use the direction keys to fly.";
        var entrance=Box("Damaged store entrance",new Vector2(1.15f,1.3f),new Vector2(1.15f,2.6f),0x3b504d,0,props);var broken=Door(entrance,context,"E / RETURN OUTSIDE","Assets/Scenes/RecoilPrototype.unity","Player Spawn");broken.permanentlyBlocked=true;broken.blockedByItemId="station_entrance_broken";broken.blockedMessage="The entrance has jammed. Find another way out.";
        var damage=Group("Broken door visual",entrance.transform);damage.localPosition=Vector3.zero;
        Box("Bent locking bar",new Vector2(1.15f,1.15f),new Vector2(1,.14f),0x8e6555,4,damage).transform.rotation=Quaternion.Euler(0,0,27);Box("Broken glass",new Vector2(1.15f,1.9f),new Vector2(.55f,.1f),0x9da7a2,4,damage).transform.rotation=Quaternion.Euler(0,0,-37);broken.damagedVisual=damage;
        Label("Broken entrance sign","JAMMED",new Vector2(1.15f,2.9f),.065f,props);
        var hatchObject=new GameObject("Central skylight");hatchObject.transform.SetParent(props,false);hatchObject.transform.position=new Vector2(8,4.7f);
        var lid=Box("Closed skylight",new Vector2(8,4.7f),new Vector2(3,.18f),0x708e94,3,hatchObject.transform,true);
        var open=Box("Open skylight lid",new Vector2(9.55f,5.3f),new Vector2(.16f,1.15f),0x708e94,3,hatchObject.transform);open.SetActive(false);
        var hatch=Door(hatchObject,context,"E / OPEN SKYLIGHT","Assets/Scenes/RecoilPrototype.unity","Service Roof Spawn");hatch.interactionOffset=new Vector2(0,-.65f);hatch.interactionRange=1.25f;hatch.requiredItemId="water_gun";hatch.lockedMessage="I need the water gun to reach this.";hatch.openStateItemId="station_skylight_open";hatch.requiresFlyingToOpen=true;hatch.openingBarrier=lid.GetComponent<Collider2D>();hatch.closedVisual=lid.transform;hatch.openVisual=open.transform;
        var spawnInside=Group("Store Entrance Spawn",root.transform);spawnInside.position=new Vector2(2,.85f);
        var player=UnityEngine.Object.Instantiate(world.Player,actors);player.name="Player";player.transform.position=spawnInside.position;
        var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=RecoilPrototype.Hex(0x10191e);camera.transform.position=new Vector3(4,2.8f,-10);
        var cameraControl=camera.gameObject.AddComponent<RecoilCameraController>();cameraControl.target=player.transform;cameraControl.viewSize=4;cameraControl.followOffset=new Vector2(1.5f,1.8f);cameraControl.minimumY=2.8f;cameraControl.maximumY=3.5f;
        var ps=new SerializedObject(player);player.Configure(context,camera,(Transform)ps.FindProperty("coat").objectReferenceValue,(Transform)ps.FindProperty("head").objectReferenceValue,(Transform)ps.FindProperty("hat").objectReferenceValue,(Transform)ps.FindProperty("gun").objectReferenceValue);
        Set(context,"player",player);Set(context,"actor",player.transform);Set(context,"cam",camera);Set(context,"spawnPoint",spawnInside);Set(context,"checkpointPoint",spawnInside);Set(context,"factoryEntrance",Group("Store Boundary",root.transform));
        Set(context,"coat",ps.FindProperty("coat").objectReferenceValue);Set(context,"head",ps.FindProperty("head").objectReferenceValue);Set(context,"hat",ps.FindProperty("hat").objectReferenceValue);Set(context,"gun",ps.FindProperty("gun").objectReferenceValue);
        var lighting=Group("05 Lighting",root.transform);var dark=Box("Darkness Layer",new Vector2(8,3),new Vector2(100,60),0xffffff,150,lighting).GetComponent<SpriteRenderer>();dark.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RecoilPrototype/Art/FactoryDarkness.mat");
        var overlay=lighting.gameObject.AddComponent<RecoilLighting2D>();overlay.outdoorAmbient=.22f;overlay.unlitAmbientScale=.6f;overlay.playerLightIntensity=.45f;
        var lights=new RecoilLight2D[3];for(int i=0;i<3;i++){var lamp=Box("Store ceiling lamp "+i,new Vector2(3+i*5,4.2f),new Vector2(.5f,.09f),0xa7c7bd,6,lighting);var child=Group("Light - child of fixture",lamp.transform);child.position=lamp.transform.position;child.localScale=new Vector3(2,11,1);lights[i]=child.gameObject.AddComponent<RecoilLight2D>();lights[i].radius=3;lights[i].intensity=.42f;}
        overlay.Configure(player.transform,null,dark);
        var wave=Group("06 Sound Wave Manager - disabled broadcasts",root.transform).gameObject.AddComponent<SoundWaveManager>();wave.buildingBroadcastsEnabled=false;
        var mainLight=new GameObject("Main Light",typeof(Light)).GetComponent<Light>();mainLight.type=LightType.Directional;mainLight.intensity=.15f;
        EditorSceneManager.SaveScene(store,Inside);
        var build=EditorBuildSettings.scenes.ToList();if(!build.Any(e=>e.path==Inside))build.Add(new EditorBuildSettingsScene(Inside,true));EditorBuildSettings.scenes=build.ToArray();
        EditorSceneManager.CloseScene(store,true);SceneManager.SetActiveScene(outdoor);AssetDatabase.SaveAssets();
        return "Station center "+center+"; first monster "+first+"; new interior "+Inside;
    }
}
