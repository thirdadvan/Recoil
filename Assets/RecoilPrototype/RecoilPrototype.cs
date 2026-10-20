using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene context, objectives and UI. Actors and camera own their behavior components.
[DefaultExecutionOrder(-1000)]
public sealed partial class RecoilPrototype : MonoBehaviour
{
    class Block { public BoxCollider2D collider; }
    readonly List<Block> blocks = new List<Block>();
    [SerializeField] RecoilPlayerController player;
    [SerializeField] List<RampagerController> enemies = new List<RampagerController>();
    [SerializeField] bool sceneAuthored;
    [SerializeField] Sprite sceneSprite;
    [SerializeField] Material sceneMaterial;
    [SerializeField] Camera cam;
    [SerializeField] Transform actor, coat, head, hat, gun, clue, door;
    [SerializeField] Transform spawnPoint, checkpointPoint, factoryEntrance;
    [SerializeField] List<BoxCollider2D> solidObjects = new List<BoxCollider2D>();
    [Header("Scene presentation")]
    public string locationLabel="OUTDOOR YARD";
    public string objectiveWithoutCard="",objectiveWithCard="";
    public bool enableCheckpoint=true;
    public bool legacyEvidenceEnabled=true;
    [Tooltip("独立测试场景可以直接配枪；室外开场关闭，去超市拾取。")]
    public bool startWithWaterGun=true;
    public bool HasWaterGun {get{return startWithWaterGun || HasItem("water_gun");}}
    public void SetProgressFlag(string id,bool enabled){if(enabled)inventory.Add(id);else inventory.Remove(id);}
    public void RememberProgress(string id){if(!string.IsNullOrEmpty(id))inventory.Add(id);}
    RecoilCameraController cameraController;
    void ConfigureCamera()
    {cameraController=cam.GetComponent<RecoilCameraController>();if(!cameraController)cameraController=cam.gameObject.AddComponent<RecoilCameraController>();cameraController.Initialize(this,actor);}
    sealed class SceneTravel
    {public string scene,spawn;public string[] items;public int health;}
    static SceneTravel pendingTravel;
    readonly List<string> entryInventory=new List<string>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearPendingTravel(){pendingTravel=null;}
    public void TravelToScene(string scene,string destinationSpawn)
    {
        pendingTravel=new SceneTravel{scene=scene,spawn=destinationSpawn,items=new List<string>(inventory).ToArray(),health=player.Health};
        RecoilSceneLoader.Load(scene);
    }
    Sprite square; Material flat;
    RecoilLighting2D lighting;
    WarehouseCrawlerEncounter crawlerEncounter;
    PlayerDialogueTrigger[] dialogueTriggers;
    ClimbableLadder[] ladders;
    SwingingChain[] chains;
    EquipmentHandhold[] handholds;
    WalkableStairway[] stairways;
    public WalkableStairway FindStairway(Vector2 feet,float vertical,out float distance)
    {distance=0;if(stairways!=null)foreach(var stair in stairways)if(stair && stair.TryEntry(feet,vertical,out distance))return stair;return null;}
    public WalkableStairway FindStairwayCatch(Vector2 feet,out float distance)
    {
        distance=0;float nearest=float.PositiveInfinity;WalkableStairway best=null;
        if(stairways!=null)foreach(var stair in stairways)if(stair && stair.TryCatch(feet,out var d,out var gap) && gap<nearest)
        {nearest=gap;distance=d;best=stair;}
        return best;
    }
    SoftRoofPlatform[] softRoofs;
    public IEnumerable<SoftRoofPlatform> SoftRoofs {get{return softRoofs??System.Array.Empty<SoftRoofPlatform>();}}
    public ClimbableLadder FindLadder(Vector2 position,float height)
    {if(EscortingChild)return null;if(ladders!=null)foreach(var ladder in ladders)if(ladder && ladder.Contains(position,height))return ladder;return null;}
    public SwingingChain FindChain(Vector2 position,float height)
    {if(chains!=null)foreach(var chain in chains)if(chain && chain.Contains(position,height))return chain;return null;}
    public EquipmentHandhold FindHandhold(Vector2 position,float height)
    {if(handholds!=null)foreach(var hold in handholds)if(hold && hold.Contains(position,height))return hold;return null;}
    public SoundWaveManager SoundWaves {get;private set;}
    public void RegisterSoundWaveManager(SoundWaveManager manager){SoundWaves=manager;}
    public IEnumerable<RampagerController> Rampagers {get{return enemies;}}
    public bool IndoorDarknessActive {get{return lighting && lighting.HasIndoorArea;}}
    public bool InStreetLampCone(Vector2 p)
    {foreach(var lamp in RecoilStreetLamp.Active)if(lamp.BelongsTo(this) && lamp.Contains(p))return true;return false;}
    public RecoilStreetLamp NearbyStreetLamp
    {get{foreach(var lamp in RecoilStreetLamp.Active)if(lamp.InRange(this))return lamp;return null;}}
    public float EnvironmentBrightness(Vector2 p)
    {bool inCone;return EnvironmentBrightness(p,out inCone);}
    public float EnvironmentBrightness(Vector2 p,out bool inCone)
    {
        if(lighting)return lighting.EnvironmentBrightnessAt(p,out inCone);
        inCone=InStreetLampCone(p);return inCone?1:.28f;
    }
    public void PublishReport(RampagerController.Report report,float radius)
    {foreach(var e in enemies)if(e!=report.source && Vector2.Distance(e.Position,report.source.Position)<=radius)e.ReceiveReport(report);if(SoundWaves)SoundWaves.EmitAid(report.source.Position,radius);}
    readonly HashSet<string> inventory=new HashSet<string>();
    public const string UniversalKey="debug_universal_door_key";
    public EscortChild Child {get;private set;}
    bool spawnedEscortChild;
    public bool EscortingChild {get{return Child && Child.Alive && Child.Following;}}
    void ResetEscortChild()
    {
        bool escort=HasItem(EscortChild.EscortFlag);
        Vector2 waitPosition;bool waiting=EscortChild.TryWaitingPosition(gameObject.scene.path,out waitPosition);
        if(!escort && EscortChild.WaitingElsewhere(gameObject.scene.path)){if(Child){Child.ResetChild(false);Child.gameObject.SetActive(false);}return;}
        if(!escort && !waiting && spawnedEscortChild && Child){Destroy(Child.gameObject);Child=null;spawnedEscortChild=false;}
        if(!Child && (escort||waiting)){var prefab=Resources.Load<GameObject>("RecoilEscortChild");if(prefab)Child=Instantiate(prefab,transform).GetComponent<EscortChild>();if(Child){Child.Initialize(this);spawnedEscortChild=true;}}
        if(Child){Child.gameObject.SetActive(true);Child.ResetChild(escort);if(waiting&&!escort)Child.RestoreWaiting(waitPosition);}
    }
    public void EquipUniversalKey(){RememberProgress(UniversalKey);if(doors!=null)foreach(var d in doors)if(d)d.ResetDoor(this);ShowInteractionMessage("TEST KEY / All doors unlocked.");}
    public void ChildKilled(){dead=true;deathReason="The child was killed.";InterruptInteraction();nextHudTextRefresh=float.NegativeInfinity;}
    RecoilDoor[] doors;
    public bool HasClue {get{return hasClue;}}
    public void CompleteLevel(string message){won=true;Say(message);}
    public RecoilDoor NearbyDoor
    {
        get
        {
            RecoilDoor closest=null;float distance=float.PositiveInfinity;
            if(doors!=null)foreach(var candidate in doors)if(candidate && candidate.InRange(this))
            {float d=(candidate.InteractionPosition-player.Position).sqrMagnitude;if(d<distance){closest=candidate;distance=d;}}
            return closest;
        }
    }
    ElectricalPanelInteraction[] electricalPanels;
    public ElectricalPanelInteraction ActiveElectricalPanel {get;private set;}
    public ElectricalPanelInteraction NearbyElectricalPanel {get{return electricalPanels==null?null:System.Array.Find(electricalPanels,p=>p && p.InRange(this));}}
    public bool TryInteractElectricalPanel(bool pressed){var panel=NearbyElectricalPanel;if(!pressed||ReaderInputConsumed||!Playing||!panel)return false;var exit=NearbyDoor;if(exit && Vector2.Distance(player.Position,exit.InteractionPosition)<Vector2.Distance(player.Position,panel.transform.position))return false;InterruptInteraction();player.NotifyForbiddenInteraction();ActiveElectricalPanel=panel;FrameDelta=0;ReaderInputConsumed=true;return true;}
    public void CloseElectricalPanel(){if(ActiveElectricalPanel){var state=WarehousePowerState.Read(this);if(state.HoldingCable){state.ReturnCable();state.Save(this);}}ActiveElectricalPanel=null;ReaderInputConsumed=true;}
    ReadableProp[] readableProps;
    public ReadableProp ActiveReadable {get;private set;}
    public bool ReaderInputConsumed {get;private set;}
    public ReadableProp NearbyReadable
    {
        get {ReadableProp closest=null;float best=float.PositiveInfinity;
            if(readableProps!=null)foreach(var prop in readableProps)if(prop && prop.InRange(this))
            {float distance=(player.Position-(Vector2)prop.transform.position).sqrMagnitude;if(distance<best){best=distance;closest=prop;}}
            return closest;}
    }
    public bool OpenReadable(ReadableProp prop)
    {
        if(!Playing || !prop || !prop.InRange(this))return false;
        InterruptInteraction();player.NotifyForbiddenInteraction();ActiveReadable=prop;FrameDelta=0;ReaderInputConsumed=true;return true;
    }
    public void CloseReadable(){ActiveReadable=null;ReaderInputConsumed=true;}
    public bool TryInteractReadable(bool pressed)
    {
        if(!pressed || ReaderInputConsumed)return false;
        var prop=NearbyReadable;if(!prop)return false;
        var exit=NearbyDoor;
        if(exit && Mathf.Abs(exit.InteractionPosition.x-player.Position.x)<Mathf.Abs(prop.transform.position.x-player.Position.x))return false;
        return OpenReadable(prop);
    }
    ContinuousInteractionItem[] interactionItems;
    int interactionSession;
    bool interactionHeld;
    public ContinuousInteractionItem ActiveInteraction {get;private set;}
    public ContinuousInteractionItem NearbyInteraction
    {get{if(interactionItems!=null)foreach(var item in interactionItems)if(item.InRange(this) && (!EscortingChild || item.interactionDuration<=0))return item;return null;}}
    // Includes hold items even when escort restrictions currently prevent using them.
    public bool InObjectInteractionRange
    {
        get
        {
            if(interactionItems!=null)foreach(var item in interactionItems)if(item && item.isActiveAndEnabled && item.InRange(this))return true;
            if(NearbyDoor || NearbyReadable || NearbyElectricalPanel || NearbyStreetLamp)return true;
            return legacyEvidenceEnabled && clue && !hasClue && Vector2.Distance(player.Position,EvidencePosition)<1.5f;
        }
    }
    public bool CanAskChildToWait {get{return EscortingChild && !InObjectInteractionRange;}}
    public bool HasItem(string id){return inventory.Contains(id);}
    public void GrantItem(string id,string message){if(!string.IsNullOrEmpty(id))inventory.Add(id);ShowInteractionMessage(message);}
    public void UpdateContinuousInteraction(bool held,float dt)
    {
        if(EscortingChild && ActiveInteraction && ActiveInteraction.interactionDuration>0){ActiveInteraction.Cancel();ActiveInteraction=null;}
        // A cancelled or completed hold must be released before a new session starts.
        if(!held){if(ActiveInteraction)ActiveInteraction.Cancel();ActiveInteraction=null;interactionHeld=false;return;}
        if(ActiveInteraction && !ActiveInteraction.InRange(this)){ActiveInteraction.Cancel();ActiveInteraction=null;}
        if(!interactionHeld && !ActiveInteraction)
        {
            ActiveInteraction=NearbyInteraction;
            if(ActiveInteraction)
            {
                ActiveInteraction.Begin(++interactionSession);
                // Instant switches may finish and clear ActiveInteraction before monsters run this frame.
                player.NotifyForbiddenInteraction();
            }
        }
        interactionHeld=true;
        if(ActiveInteraction && ActiveInteraction.Advance(this,dt))ActiveInteraction=null;
    }
    bool sceneBound;
    Transform sceneryRoot, terrainRoot, propsRoot, actorsRoot;
    float monologueUntil, dialogueUntil, elapsed, simTime;
    bool dead, won, started, paused, checkpoint, hasClue;
    string monologue = "", dialogue = "", deathReason = "";
    // Self-talk has no screen placement yet; never occupies the bottom dialogue panel.
    public string SelfTalk {get{return monologue;}}
    public bool SelfTalkActive {get{return simTime<monologueUntil;}}
    GUIStyle label, small, title, center, hudTitle, enemyLabel;
    const float HudTextInterval=.1f;
    float nextHudTextRefresh=float.NegativeInfinity;
    string hudObjective="",hudStatus="",hudHealth="",hudIllumination="",hudWave="",hudDialogue="",hudDeath="",hudSummary="";
    readonly Dictionary<RampagerController,string> hudEnemyLabels=new Dictionary<RampagerController,string>();
    Texture2D white;
    Vector2 spawn = new Vector2(2, 1.1f);
    int attempts = 1;
    public RecoilPlayerController Player {get{return player;}}
    public float Clock {get{return simTime;}}
    public float FrameDelta {get;private set;}
    public bool Playing {get{return started && !paused && !dead && !won && !ActiveReadable && !ActiveElectricalPanel;}}
    public Material SprayMaterial {get{return flat;}}
    public IEnumerable<Rect> SolidRects
    {
        get
        {
            foreach(var block in blocks)
            {
                if(block.collider)
                {if(!block.collider.enabled || block.collider.isTrigger || !block.collider.gameObject.activeInHierarchy)continue;var bounds=block.collider.bounds;yield return new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y);}
            }
        }
    }

    void Awake()
    {
        Application.targetFrameRate = 60;
        square = sceneSprite ? sceneSprite : Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f,.5f), 1);
        white = Texture2D.whiteTexture;
        flat = sceneMaterial ? sceneMaterial : new Material(Shader.Find("Sprites/Default"));
        if(!cam) cam = Camera.main;
        if (!cam) cam = new GameObject("Prototype Camera").AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex(0x10191e);
        if(!sceneAuthored){BuildLevel();AttachControllers();}
        BindScene();
        lighting=GetComponentInChildren<RecoilLighting2D>();
        SoundWaves=GetComponentInChildren<SoundWaveManager>();if(SoundWaves)SoundWaves.Initialize(this);
        var travel=pendingTravel;
        bool arriving=travel!=null && (travel.scene==gameObject.scene.path || travel.scene==gameObject.scene.name || travel.scene==gameObject.scene.path.Replace(".unity",""));
        pendingTravel=null;
        if(arriving)
        {
            entryInventory.AddRange(travel.items);
            if(!string.IsNullOrEmpty(travel.spawn))
                foreach(var marker in GetComponentsInChildren<Transform>(true))if(marker.name==travel.spawn){spawn=marker.position;break;}
        }
        ResetRun(true);
        if(arriving){player.RestoreHealth(travel.health);started=true;Say(locationLabel);}
        RegisterMemorySceneEntry();
#if UNITY_EDITOR
        Debug.Log("RECOIL_SCENE_READY: "+gameObject.scene.path+" | Rampagers: "+enemies.Count,this);
#endif
    }

    // Called once by the editor builder. All authored objects and references are saved in the scene.
    public void BakeScene(Sprite sprite, Material material)
    {
        sceneSprite=square=sprite; sceneMaterial=flat=material;
        sceneryRoot=Folder("01 Background"); terrainRoot=Folder("02 Terrain and Cover");
        propsRoot=Folder("03 Props and Objectives"); actorsRoot=Folder("04 Characters");
        cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();
        cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=6.7f;
        cam.backgroundColor=Hex(0x10191e);cam.clearFlags=CameraClearFlags.SolidColor;
        cam.transform.position=new Vector3(8,5,-10);
        spawnPoint=Folder("Player Spawn");spawnPoint.position=spawn;
        checkpointPoint=Folder("Factory Checkpoint");checkpointPoint.position=new Vector2(18.6f,1);
        factoryEntrance=Folder("Factory Entrance Boundary");factoryEntrance.position=new Vector2(16,0);
        BuildLevel();AttachControllers();actor.position=spawnPoint.position;gun.position=(Vector2)actor.position+Vector2.right*.5f;
        sceneAuthored=true;
    }
    Transform Folder(string name) { var t=new GameObject(name).transform;t.SetParent(transform,false);return t; }
    public void AttachControllers()
    {
        ConfigureCamera();
        actor.name="Player";
        player=actor.GetComponent<RecoilPlayerController>();
        if(!player)player=actor.gameObject.AddComponent<RecoilPlayerController>();
        if(!actor.GetComponent<PlayerLightExposure>())actor.gameObject.AddComponent<PlayerLightExposure>();
        player.Configure(this,cam,coat,head,hat,gun);
        foreach(var patrol in GetComponentsInChildren<RecoilPatrol>(true))
        {
            var controller=patrol.GetComponent<RampagerController>();
            if(!controller)controller=patrol.gameObject.AddComponent<RampagerController>();
            controller.Configure(this,patrol.leftOffset,patrol.rightOffset);
        }
        enemies=new List<RampagerController>(GetComponentsInChildren<RampagerController>());
        foreach(var enemy in enemies)enemy.EnsureStandardComponents();
#if UNITY_EDITOR
        if(!GetComponentInChildren<RecoilStreetLamp>())
        {var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RecoilPrototype/Prefabs/StreetLamp.prefab");if(prefab){var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,propsRoot?propsRoot:transform);instance.transform.localPosition=new Vector3(22,0,0);}}
#endif
        if(!GetComponentInChildren<Backpack>())
        {
            var bag=Box("Employee Backpack",new Vector2(28.8f,.55f),new Vector2(.65f,.9f),Hex(0xb68b55),8,propsRoot?propsRoot:transform);
            var item=bag.gameObject.AddComponent<Backpack>();item.itemId="backpack";item.displayName="Employee backpack";item.interactionDuration=1.5f;item.resultId="employee_access_card";item.resultMessage="Employee access card acquired.";
        }
    }
    void BindScene()
    {
        ConfigureCamera();
        if(player)
        {
            player.InitializeNoise(this);
            var gaze=player.GetComponent<PlayerGazeFeedback>();if(!gaze)gaze=player.gameObject.AddComponent<PlayerGazeFeedback>();gaze.Initialize(this);
        }
        if(player && !player.GetComponent<PlayerAmmoRingUI>())player.gameObject.AddComponent<PlayerAmmoRingUI>();
        if(player && !player.GetComponent<PlayerLightExposure>())player.gameObject.AddComponent<PlayerLightExposure>();
        if(door && !door.GetComponent<RecoilDoor>())door.gameObject.AddComponent<RecoilDoor>();
        Child=GetComponentInChildren<EscortChild>(true);if(Child)Child.Initialize(this);
        doors=GetComponentsInChildren<RecoilDoor>(true);
        electricalPanels=GetComponentsInChildren<ElectricalPanelInteraction>(true);
        dialogueTriggers=GetComponentsInChildren<PlayerDialogueTrigger>(true);
        ladders=GetComponentsInChildren<ClimbableLadder>(true);softRoofs=GetComponentsInChildren<SoftRoofPlatform>(true);
        chains=GetComponentsInChildren<SwingingChain>(true);
        handholds=GetComponentsInChildren<EquipmentHandhold>(true);
        stairways=GetComponentsInChildren<WalkableStairway>(true);
        interactionItems=GetComponentsInChildren<ContinuousInteractionItem>(true);readableProps=GetComponentsInChildren<ReadableProp>(true);
        spawn=spawnPoint? (Vector2)spawnPoint.position : spawn;
        blocks.Clear();Physics2D.SyncTransforms();
        solidObjects=new List<BoxCollider2D>(GetComponentsInChildren<BoxCollider2D>(true));
        foreach(var c in solidObjects)if(c && !c.isTrigger && !c.GetComponent<SoftRoofPlatform>() && !c.GetComponent<ClimbableLadder>())
        {blocks.Add(new Block {collider=c});}
        if(!sceneBound)
        {
            enemies=new List<RampagerController>(GetComponentsInChildren<RampagerController>());
            foreach(var enemy in enemies)enemy.Initialize(this);
            sceneBound=true;
            crawlerEncounter=GetComponentInChildren<WarehouseCrawlerEncounter>(true);
            if(crawlerEncounter)crawlerEncounter.Initialize(this,cam);
        }
    }
    public float FactoryX { get { return factoryEntrance ? factoryEntrance.position.x : 16; } }
    Vector2 CheckpointPosition { get { return checkpointPoint ? (Vector2)checkpointPoint.position : new Vector2(18.6f,1); } }
    Vector2 EvidencePosition { get { return clue? (Vector2)clue.position : Vector2.zero; } }

    public static Color Hex(int n, float a = 1) { return new Color(((n>>16)&255)/255f, ((n>>8)&255)/255f, (n&255)/255f, a); }
    Transform Box(string name, Vector2 p, Vector2 size, Color color, int order = 0, Transform parent = null)
    {
        var t = new GameObject(name).transform;
        if(!parent) parent=order<0?sceneryRoot:(name=="Solid terrain"||name=="Sightline cover")?terrainRoot:propsRoot;
        t.SetParent(parent, false);
        t.localPosition = p; t.localScale = new Vector3(size.x,size.y,1);
        var sr = t.gameObject.AddComponent<SpriteRenderer>(); sr.sprite = square; sr.color = color; sr.sortingOrder = order;
        return t;
    }
    void Solid(float x, float y, float w, float h, bool cover = false)
    {
        var r = new Rect(x,y,w,h);
        var t = Box(cover ? "Sightline cover" : "Solid terrain", r.center, r.size, Hex(cover ? 0x576567 : 0x303d43), 2);
        var collider=t.gameObject.AddComponent<BoxCollider2D>();collider.size=Vector2.one;
        blocks.Add(new Block {collider=collider});
        solidObjects.Add(collider);
        Box("Edge", new Vector2(0,.5f-.04f/h), new Vector2(1,.08f/h), Hex(0x7a8b89), 3,t);
        if (cover)
        {
            Box("Crate band", Vector2.zero, new Vector2(.08f/w,1), Hex(0x35464a), 3,t);
            Box("Crate band", Vector2.zero, new Vector2(1,.08f/h), Hex(0x35464a), 3,t);
        }
    }
    void BuildLevel()
    {
        // Outdoor yard -> low factory -> evidence -> exit. Cover blocks actual sightlines.
        for (int i=0;i<15;i++)
        {
            float h = 3 + (i*7%6);
            Box("Distant city",new Vector2(i*3-3,h/2+1),new Vector2(2.3f,h),Hex(0x19262d),-20);
            for (int j=0;j<3;j++) Box("Window",new Vector2(i*3-3.5f,2+j*1.4f),new Vector2(.24f,.48f),Hex(0x35433f),-19);
        }
        Box("Factory interior",new Vector2(27,3),new Vector2(22,6),Hex(0x1c262c),-10);
        for(int i=0;i<7;i++) Box("Factory column",new Vector2(17+i*3.5f,3),new Vector2(.18f,6),Hex(0x29383e),-9);
        Solid(-2,-1,42,1); Solid(-2,0,.5f,14); Solid(39,0,.5f,14);
        Solid(16,6,23,.6f); Solid(24.5f,3.35f,6,.35f);
        Box("Checkpoint lamp",new Vector2(18.6f,2.3f),new Vector2(.22f,.22f),Hex(0x7bd4b0),5);
        Box("Entrance frame",new Vector2(16.1f,3),new Vector2(.15f,6),Hex(0x627274),4);
        Box("Parked car",new Vector2(2.4f,.45f),new Vector2(2.4f,.65f),Hex(0x344852),1);
        Box("Car cabin",new Vector2(2.6f,.98f),new Vector2(1.2f,.5f),Hex(0x435c65),1);
        clue = Box("Missing person's evidence",new Vector2(31.5f,.8f),new Vector2(.4f,.55f),Hex(0xf1d697),5);
        door = Box("Exit",new Vector2(37.5f,1.25f),new Vector2(1.1f,2.5f),Hex(0x31554e),1);
        door.gameObject.AddComponent<RecoilDoor>();
        Box("Exit light",new Vector2(37.5f,2.8f),new Vector2(.65f,.16f),Hex(0x86d2ab),3);
        actor = new GameObject("Dun - detective").transform;
        actor.SetParent(actorsRoot,false);
        coat = Box("Coat",new Vector2(0,-.1f),new Vector2(.6f,1.15f),Hex(0xb4c1bc),10,actor);
        head = Box("Head",new Vector2(0,.58f),new Vector2(.42f,.4f),Hex(0xd9cab3),11,actor);
        hat=Box("Hat",new Vector2(0,.82f),new Vector2(.65f,.14f),Hex(0x637a7e),12,actor);
        gun = Box("Blue plastic water gun",Vector2.zero,new Vector2(.6f,.18f),Hex(0x56d6fa),13);
        AddEnemy(12,9.8f,15.4f); AddEnemy(25,23.5f,26.2f); AddEnemy(32.8f,29.2f,33.2f);
    }
    void AddEnemy(float x,float left,float right)
    {
        var view=Box("Rampager",new Vector2(x,.8f),new Vector2(.65f,1.6f),Hex(0xb87570),9);
        if(actorsRoot)view.SetParent(actorsRoot,true);
        var controller=view.gameObject.AddComponent<RampagerController>();
        controller.Configure(this,left-x,right-x);enemies.Add(controller);
        RampagerPatrolRoute.CreateDefaultPoints(controller);
        Box("Eye",new Vector2(-.15f,.4f),new Vector2(.12f,.08f),Hex(0xf5ca97),10,view);
    }
    void ResetRun(bool full)
    {
        deathCountedForMemory=false;
        ActiveElectricalPanel=null;ActiveReadable=null;ReaderInputConsumed=false;nextHudTextRefresh=float.NegativeInfinity;hudEnemyLabels.Clear();dialogue="";dialogueUntil=float.NegativeInfinity;
        if (full) { checkpoint=false; hasClue=false;  elapsed=0; attempts=1; }
        foreach(var lamp in RecoilStreetLamp.Active)if(lamp.BelongsTo(this))lamp.ResetLamp();
        var retained=new List<string>();
        if(!full)foreach(var id in inventory)if(RetainedOnRetry(id))retained.Add(id);
        inventory.Clear();foreach(var item in entryInventory)inventory.Add(item);foreach(var id in retained)inventory.Add(id);ActiveInteraction=null;interactionHeld=false;interactionSession=0;
        if(interactionItems!=null)foreach(var item in interactionItems)item.ResetItem(this);
        if(doors!=null)foreach(var exitDoor in doors)if(exitDoor)exitDoor.ResetDoor(this);
        if(dialogueTriggers!=null)foreach(var trigger in dialogueTriggers)if(trigger)trigger.ResetTrigger();
        player.ResetController(checkpoint ? CheckpointPosition : spawn);
        var gazeFeedback=player.GetComponent<PlayerGazeFeedback>();if(gazeFeedback)gazeFeedback.ResetFeedback();
        ResetEscortChild();
        dead=won=paused=hasClue=false;if(clue)clue.gameObject.SetActive(legacyEvidenceEnabled);
        foreach(var enemy in enemies)enemy.ResetController();
        if(crawlerEncounter)crawlerEncounter.ResetEncounter();
        if(SoundWaves)SoundWaves.ResetManager();
        Say(checkpoint ? "The doorway. Again. This time, slower." : "A plastic water gun. I somehow remember what it does.");
        if(cameraController)cameraController.SnapToTarget();
    }
    public void Say(string s) { monologue=s; monologueUntil=simTime+5; nextHudTextRefresh=float.NegativeInfinity; }
    public void ShowDialogue(string message,float duration=5)
    {dialogue=message??"";dialogueUntil=simTime+Mathf.Max(.1f,duration);nextHudTextRefresh=float.NegativeInfinity;}
    public void ShowInteractionMessage(string message){ShowDialogue(message);}
    void Update()
    {
        if(dead && TryEnterDeathMemory())return;
        ReaderInputConsumed=false;
        if(ActiveElectricalPanel){if(Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Escape))CloseElectricalPanel();FrameDelta=0;return;}
        if(ActiveReadable)
        {
            if(Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))CloseReadable();
            FrameDelta=0;return;
        }
        Physics2D.SyncTransforms();
        if(Input.GetKeyDown(KeyCode.R)){attempts++;bool full=Input.GetKey(KeyCode.LeftShift);if(full){EscortChild.ClearWaiting();RestartFullGameMemory();}ResetRun(full);}
        if(Input.GetKeyDown(KeyCode.Escape) && started && !dead && !won)paused=!paused;
        if(Playing && Input.GetKeyDown(KeyCode.B) && SoundWaves)SoundWaves.StartBroadcast();
        FrameDelta=Playing?Mathf.Min(Time.deltaTime,.033f):0;
        simTime+=FrameDelta;elapsed+=FrameDelta;
    }
    void RecoverPlayerHealth(float dt)
    {bool observed=false;foreach(var enemy in enemies)if(enemy.CanSeePlayer()){observed=true;break;}if(!observed && crawlerEncounter)observed=crawlerEncounter.ObservingPlayer;player.AdvanceRecovery(dt,observed);}
    void LateUpdate()
    {
        if(Playing)
        {
            if(doors!=null)foreach(var exit in doors)if(exit && exit.TryAutoTravel(this))return;
            if(dialogueTriggers!=null)foreach(var trigger in dialogueTriggers)if(trigger)trigger.CheckPlayer(this);
            RecoverPlayerHealth(FrameDelta);
        }
        UpdateVisuals(FrameDelta);
    }
    public void InterruptInteraction()
    {if(ActiveInteraction)ActiveInteraction.Cancel();ActiveInteraction=null;interactionHeld=true;}
    public void CatchPlayer(){CatchPlayer(Vector2.zero);}
    public void CatchPlayer(Vector2 knockback)
    {
        if(!player.TakeMonsterHit())return;
        if(knockback.sqrMagnitude>0)player.ApplyMonsterKnockback(knockback);
        if(player.Health<=0){dead=true;deathReason="Your health ran out.";}
    }
    public void ObservePlayer(bool interact)
    {
        if(enableCheckpoint && player.Position.x>CheckpointPosition.x-.6f && !checkpoint) { checkpoint=true; Say("Walk normally to blend in. Visible shooting or interaction irritates them. Release E quickly."); }
        if(interact && NearbyStreetLamp){NearbyStreetLamp.Interact(this);return;}
        if(interact && !NearbyInteraction && !ActiveInteraction)
        {
            if(legacyEvidenceEnabled && clue && !hasClue && Vector2.Distance(player.Position,EvidencePosition)<1.5f)
            { player.NotifyForbiddenInteraction();hasClue=true; clue.gameObject.SetActive(false); Say("A child's name. An address. Why does the handwriting look familiar?"); }
            var exitDoor=NearbyDoor;if(exitDoor)exitDoor.TryInteract(this);
        }
    }
    public void ApplyWaterHit(Vector2 start,Vector2 direction,float range,Vector2 source)
    {foreach(var enemy in enemies)enemy.TakeWaterHit(start,direction,range,source);if(crawlerEncounter && crawlerEncounter.crawler)crawlerEncounter.crawler.TakeWaterHit(start,direction,range);}
    public static float RayRect(Vector2 origin,Vector2 dir,Rect r,float max)
    {
        float near=0,far=max;
        for(int axis=0;axis<2;axis++)
        {
            float p=axis==0?origin.x:origin.y, d=axis==0?dir.x:dir.y;
            float lo=axis==0?r.xMin:r.yMin, hi=axis==0?r.xMax:r.yMax;
            if(Mathf.Abs(d)<.00001f) { if(p<lo || p>hi) return max; }
            else { float a=(lo-p)/d,b=(hi-p)/d; if(a>b) { float c=a;a=b;b=c; } near=Mathf.Max(near,a);far=Mathf.Min(far,b);if(near>far)return max; }
        }
        return near<=max && far>=0 ? Mathf.Max(0,near) : max;
    }
    public bool Clear(Vector2 a,Vector2 b)
    {
        float distance=Vector2.Distance(a,b); if(distance<.001f)return true;
        foreach(var block in blocks)
        {
            var c=block.collider;if(!RecoilSightOcclusion.IsBlocker(c))continue;
            var bounds=c.bounds;var rect=new Rect(bounds.min.x,bounds.min.y,bounds.size.x,bounds.size.y);
            if(RayRect(a,(b-a)/distance,rect,distance)<distance-.03f)return false;
        }
        return true;
    }
    void UpdateVisuals(float dt)
    {
        player.SyncVisuals(dt);
        foreach(var enemy in enemies)enemy.SyncVisuals();
    }
    void Styles()
    {
        if(label!=null)return;
        label=new GUIStyle(GUI.skin.label) {fontSize=19,normal={textColor=Hex(0xdce7e4)}};
        small=new GUIStyle(label) {fontSize=14};
        title=new GUIStyle(label) {fontSize=48,fontStyle=FontStyle.Bold};
        center=new GUIStyle(label) {alignment=TextAnchor.MiddleCenter,wordWrap=true};
        hudTitle=new GUIStyle(title){fontSize=29};
        enemyLabel=new GUIStyle(small){alignment=TextAnchor.MiddleCenter,fontSize=13};
    }
    // Text is shared by all IMGUI events; geometry and interactive controls remain live.
    void RefreshHudText()
    {
        float now=Time.unscaledTime;
        if(!player || now<nextHudTextRefresh)return;
        nextHudTextRefresh=now+HudTextInterval;
        string sceneObjective=HasItem("employee_access_card")?objectiveWithCard:objectiveWithoutCard;
        hudObjective=!string.IsNullOrEmpty(sceneObjective)?sceneObjective:hasClue?"02  /  Reach the green exit. Press E.":"01  /  Search the factory for the missing person's clue.";
        string status=player.Climbing?"CLIMBING":player.Flying?"HOVERING":player.Crouched?"CROUCHING":player.SilentWalking?"SILENT WALK":player.Grounded?"GROUNDED":"FALLING";
        if(EscortingChild)status+=" / HOLDING HANDS";
        hudStatus=status+"   /   "+(checkpoint?"FACTORY CHECKPOINT":locationLabel);
        hudHealth="HEALTH: "+player.Health+" / "+player.maxHealth;
        hudIllumination="ILLUMINATION: "+(player.Illumination*100).ToString("0")+"%";
        hudDialogue=dialogue;
        hudDeath=deathReason+"  Cover blocks sight; C quiets your steps.";
        hudSummary="Clue recovered   /   "+Mathf.FloorToInt(elapsed)+" seconds   /   "+attempts+" attempts";
        if(SoundWaves && SoundWaves.Contains(player.Position))
        {
            var sw=SoundWaves;string direction=sw.Direction<0?"< LEFT / SHRINKING":"RIGHT > / EXPANDING";
            string phase=sw.CueDelay>0?"MOVE IN "+sw.CueDelay.ToString("0.0")+"s":sw.CueHold>0?"HOLD "+sw.CueHold.ToString("0.0")+"s":sw.CueMove>0?"MOVE "+sw.CueMove.ToString("0.0")+"s":"WAITING FOR NEXT WAVE";
            hudWave=sw.Phase==SoundWaveManager.BroadcastPhase.Idle?"B: test waves / next in "+sw.Remaining.ToString("0.0")+"s":sw.Phase==SoundWaveManager.BroadcastPhase.Recovery?"SEQUENCE COMPLETE":sw.Phase==SoundWaveManager.BroadcastPhase.Warning?"RED ALERT / FIRST DIRECTION IN "+sw.Remaining.ToString("0.0")+"s":phase+" / "+direction+" / wave "+sw.CueIndex+"/"+sw.TotalWaves;
        }
        foreach(var e in enemies)
        {
            string state=e.State==RampagerController.Behaviour.Attack?"ATTACK":e.State==RampagerController.Behaviour.Sleeping?"SLEEPING":"ROAMING";
            if(e.Waking)state="WAKING / "+e.WakeRemaining.ToString("0.0")+"s";
            else if(e.SwarmAttackBoost)state="ATTACK / WAVE BOOST";
            else if(e.Noticing)state="NOTICING / "+e.NoticeRemaining.ToString("0.0")+"s";
            else if(e.WatchingPlayer)state="WATCHING / ANGER "+(e.DetectionProgress*100).ToString("0")+"%";
            else if(e.WaveActive)state=e.WaveMoving?(e.WaveDirection<0?"< LEFT":"RIGHT >"):"WAVE / HOLD";
            else if(e.Investigation>0)state="SOUND CHECK / "+e.Investigation.ToString("0.0")+"s";
            else if(e.ReturningHome)state="RETURNING";
            else if(e.WaitingToSleep)state="SLEEP IN "+e.SleepRemaining.ToString("0.0")+"s";
            hudEnemyLabels[e]=state;
        }
    }
    void Text(float x,float y,float w,float h,string s,GUIStyle style=null) { GUI.Label(new Rect(x,y,w,h),s,style??label); }
    void Panel(Rect r,Color c) { GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white; }
    void OnGUI()
    {
        if(ActiveElectricalPanel){ActiveElectricalPanel.DrawPage(this);return;}
        Styles(); RefreshHudText(); GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
        if(ActiveReadable)
        {
            Panel(new Rect(0,0,1280,720),Hex(0x05090d,.98f));
            if(ActiveReadable.pageImage)
                GUI.DrawTexture(new Rect(160,16,960,644),ActiveReadable.pageImage,ScaleMode.ScaleToFit,true);
            else {Text(270,60,740,50,ActiveReadable.displayName,center);Text(270,125,740,500,ActiveReadable.pageText,label);}
            Text(350,674,580,28,"E / CLOSE     ESC / CLOSE",center);return;
        }
        Panel(new Rect(0,0,1280,96),Hex(0x0c151b,.94f));
        Text(28,14,280,40,"R E C O I L",hudTitle);
        Text(29,53,360,24,"PLAYABLE STUDY  /  TOWN ENTRANCE",small);
        Text(480,20,770,30,hudObjective);
        Text(480,55,770,25,"A/D move   •   Mouse + hold click fire   •   W launch   •   W/S fly   •   C crouch / Shift silent walk   •   E hold to search",small);
        Panel(new Rect(0,664,1280,56),Hex(0x0c151b,.94f));
        Text(28,678,380,24,hudStatus,small);
        Text(28,597,390,24,hudHealth,small);
        Text(28,621,390,24,hudIllumination,small);
        Text(930,678,330,24,"R retry   /   Shift+R restart   /   Esc pause",small);
        if(HasItem("employee_access_card"))Text(430,650,450,22,"INVENTORY: EMPLOYEE ACCESS CARD",small);
        if(CanAskChildToWait && Playing){Text(390,370,500,26,"HOLD E / ASK THE CHILD TO WAIT",center);if(Child.WaitHoldProgress>0){Panel(new Rect(440,400,400,5),Hex(0x35464c));Panel(new Rect(440,400,400*Child.WaitHoldProgress,5),Hex(0xe2c88d));}}
        if(NearbyElectricalPanel && Playing)Text(390,408,500,28,NearbyElectricalPanel.interactionPrompt,center);
        if(NearbyReadable && Playing)Text(390,408,500,28,NearbyReadable.interactionPrompt,center);
        if(NearbyStreetLamp && Playing)Text(390,440,500,28,"E / SWITCH OFF LAMP HEAD",center);
        var nearby=NearbyInteraction;
        if(nearby && started && !dead && !won)
        {
            Text(390,470,500,28,(nearby.interactionDuration<=0?"E / ":"HOLD E / ")+nearby.displayName,center);
            if(ActiveInteraction)
            {
                Panel(new Rect(440,508,400,8),Hex(0x35464c));Panel(new Rect(440,508,400*Mathf.Clamp01(ActiveInteraction.Progress/ActiveInteraction.interactionDuration),8),Hex(0xe2c88d));
                Text(320,528,640,24,"SEARCHING / RELEASE E TO CANCEL. INTERACTION SEEN DURING WAVES TRIGGERS ATTACK.",small);
            }
        }
        if(player.Flying)
        {
            Panel(new Rect(435,684,130,6),Hex(0x35464c));Panel(new Rect(435,684,130*Mathf.Clamp01(1-(simTime-player.LastShot)),6),Hex(0x63d3ec));
            Text(580,675,330,25,player.Ammo>0?"Fire again within 1 second":"NO WATER / LAND TO REFILL",small);
        }
        if(Playing && (ActiveInteraction || (simTime<dialogueUntil && !string.IsNullOrEmpty(dialogue))))
        {
            string message=ActiveInteraction?"INTERACTING / "+ActiveInteraction.displayName:hudDialogue;
            Panel(new Rect(255,578,770,60),Hex(0x101a20,.9f));Text(270,583,740,50,message,center);
        }
        if(started && !dead && !won)
        {
            if(SoundWaves && SoundWaves.Contains(player.Position))
            {
                Panel(new Rect(230,110,820,65),Hex(0x102733,.94f));Text(245,115,790,28,hudWave,center);
                Text(245,145,790,22,"EXPAND > RIGHT / SHRINK < LEFT. Random direction. Delay 0.1s, MOVE 1s, then WAIT 1.5s.",small);
            }
            foreach(var e in enemies) if(e.Alert>.02f && (!lighting || lighting.VisibilityAt(e.Position+Vector2.up*1.25f)>.08f))
            {
                Vector3 p=cam.WorldToViewportPoint(e.Position+Vector2.up*1.25f);
                Panel(new Rect(p.x*1280-24,(1-p.y)*720,48,5),Hex(0x4b4140));
                Panel(new Rect(p.x*1280-24,(1-p.y)*720,48*e.Alert,5),Hex(0xef8f75));
            }
            foreach(var e in enemies)if(!lighting || lighting.VisibilityAt(e.Position+Vector2.up*.45f)>.08f)
            {
                Vector3 p=cam.WorldToViewportPoint(e.Position+Vector2.up*1.55f);
                string state;
                if(hudEnemyLabels.TryGetValue(e,out state))Text(p.x*1280-55,(1-p.y)*720-8,110,20,state,enemyLabel);
            }
            if(legacyEvidenceEnabled && clue && !hasClue && Vector2.Distance(player.Position,EvidencePosition)<1.5f) Text(440,495,400,40,"E  /  Examine the note",center);
            var exitDoor=NearbyDoor;if(exitDoor)Text(440,495,400,40,exitDoor.CurrentPrompt,center);
        }
        if(!started || dead || won || paused)
        {
            Panel(new Rect(0,96,1280,568),Hex(0x0a141b,.94f));
            Text(260,151,760,70,!started?"R E C O I L":dead?"CAUGHT":won?"AN ADDRESS. A MEMORY.":"PAUSED",title);
            if(!started)
            {
                Text(260,245,760,55,"A detective. A plastic water gun. A town that won't let go.");
                Text(260,311,800,135,"Fire in any direction to enter flight, including while falling.\nW / Space rises, S descends. Three shots; landing refills one shot per second.\nEach shot stores 0.75 seconds of recoil. Falling over one body height angers witnesses.\nInside: walking is safe; shooting or flying is a violation. Hold E 1.5s to search the backpack for an employee access card.");
                if(SoundWaves && SoundWaves.buildingBroadcastsEnabled)
                    Text(260,465,760,60,"B tests the broadcast. RIGHT: expanding rings. LEFT: shrinking rings. Follow the arrows.",small);
                if(GUI.Button(new Rect(260,532,300,52),"ENTER THE TOWN")) { started=true;Say("Fire. Then use W to rise. I remember this."); }
            }
            else if(dead)
            {
                Text(260,258,760,50,hudDeath);
                if(GUI.Button(new Rect(260,360,300,52),"RETRY FROM CHECKPOINT  [R]")) { attempts++;ResetRun(false); }
            }
            else if(won)
            {
                Text(260,258,800,60,"“I know this address. I wish I didn't.”");
                Text(260,336,800,60,hudSummary);
                Text(260,420,800,50,"Prototype complete. Try a quieter route, or stay airborne outside.",small);
                if(GUI.Button(new Rect(260,510,300,52),"PLAY AGAIN"))ResetRun(true);
            }
            else if(GUI.Button(new Rect(260,360,300,52),"CONTINUE"))paused=false;
        }
    }
}
