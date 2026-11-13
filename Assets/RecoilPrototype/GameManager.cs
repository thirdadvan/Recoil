using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-2000),DisallowMultipleComponent]
[AddComponentMenu("Recoil/Game Manager")]
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance {get;private set;}
    public static GameManager EnsureInstance()
    {
        if(!Instance)Instance=new GameObject("GameManager").AddComponent<GameManager>();
        return Instance;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap(){EnsureInstance();}
    public const string DebugScenePreference="Recoil.Debug.StartInCurrentScene";
    [Header("Debug startup")]
    [Tooltip("从当前打开场景直接开始，忽略磁盘存档；场景之间仍传递本轮道具和护送进度。")]
#if UNITY_EDITOR
    public bool debugStartInCurrentScene=true;
#else
    public bool debugStartInCurrentScene=false;
#endif
    public string LastError {get;private set;}
    public string LastSavedUtc {get;private set;}
    public string SavePath {get{if(!string.IsNullOrEmpty(savePathOverride))return savePathOverride;return Path.Combine(Application.persistentDataPath,
#if UNITY_EDITOR
        "EditorSaves",
#else
        "Saves",
#endif
        "slot-1.json");}}
    public bool HasSave {get{return !debugStartInCurrentScene&&diskSave!=null;}}
    public bool SessionStarted {get;private set;}
    public event Action Saved,Loaded;
    string savePathOverride=null; // Used by isolated editor verification via reflection.
    RecoilPrototype world;
    RecoilSaveData diskSave,pendingLoad;
    readonly Dictionary<string,string> sections=new Dictionary<string,string>();
    Vector2 safePosition;
    float safeDepth;
    KBKStackerCrane[] cranes=new KBKStackerCrane[0];
    bool hasSafePosition,startingNewGame;
    void Awake()
    {
        if(Instance && Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
#if UNITY_EDITOR
        debugStartInCurrentScene=UnityEditor.EditorPrefs.GetBool(DebugScenePreference,true);
#endif
        ReloadDiskSave();
    }
    public void ReloadDiskSave()
    {
        if(debugStartInCurrentScene){diskSave=null;LastError=null;LastSavedUtc=null;return;}
        string error;RecoilSaveStore.TryRead(SavePath,out diskSave,out error);
        LastError=error;LastSavedUtc=diskSave!=null?diskSave.savedUtc:null;
    }
    public void Bind(RecoilPrototype context)
    {
        world=context;cranes=context.GetComponentsInChildren<KBKStackerCrane>(true);safePosition=context.SaveFallbackPosition;safeDepth=context.Player.GameplayZ;hasSafePosition=true;
        if(pendingLoad!=null && RecoilSceneLoader.SameScene(context.gameObject.scene.path,pendingLoad.world.scene))
        {
            var data=pendingLoad;pendingLoad=null;
            sections.Clear();foreach(var section in data.sections)if(section!=null&&!string.IsNullOrEmpty(section.id))sections[section.id]=section.json;
            EscortChild.RestoreWaitSave(data.child);RecoilPrototype.RestoreMemorySave(data.memory);
            context.RestoreWorldSave(data.world);
            safePosition=data.world.position;safeDepth=data.world.depth;SessionStarted=true;
            if(Loaded!=null)Loaded();
        }
        else if(debugStartInCurrentScene){context.BeginGame();}
        else if(startingNewGame){startingNewGame=false;context.BeginGame();}
        else if(context.SaveSessionStarted)SessionStarted=true;
    }
    void LateUpdate()
    {
        if(!world || !SessionStarted)return;
        if(world.CanCaptureSave && world.Player.Grounded && !world.Player.Climbing && !world.Player.WalkingStairs && !OnMovingEquipment())
        {safePosition=world.Player.Position;safeDepth=world.Player.GameplayZ;hasSafePosition=true;}
    }
    bool OnMovingEquipment()
    {
        if(!world || !world.Player)return false;
        float feet=world.Player.Position.y-world.Player.BodyHeight*.5f;
        foreach(var crane in cranes)
        {
            if(!crane || !crane.forkPlatform)continue;
            var area=crane.forkPlatform.Area;
            if(world.Player.Position.x>=area.xMin-.3f&&world.Player.Position.x<=area.xMax+.3f&&Mathf.Abs(feet-area.yMax)<.12f)return true;
        }
        return false;
    }
    public void StartSession(){SessionStarted=true;}
    public bool SaveGame()
    {
        if(debugStartInCurrentScene||!world || !SessionStarted || !world.CanCaptureSave || pendingLoad!=null)return false;
        try
        {
            CaptureParticipants();
            var data=new RecoilSaveData{world=world.CaptureWorldSave(hasSafePosition?safePosition:world.SaveFallbackPosition,safeDepth),memory=RecoilPrototype.CaptureMemorySave(),child=EscortChild.CaptureWaitSave()};
            foreach(var pair in sections)data.sections.Add(new RecoilSaveSection{id=pair.Key,json=pair.Value});
            RecoilSaveStore.Write(SavePath,data);diskSave=data;LastSavedUtc=data.savedUtc;LastError=null;
            if(Saved!=null)Saved();return true;
        }
        catch(Exception e){LastError=e.Message;Debug.LogWarning("Save failed: "+e.Message,this);return false;}
    }
    public bool ContinueGame()
    {
        if(debugStartInCurrentScene)return false;
        ReloadDiskSave();
        if(diskSave==null || !RecoilSceneLoader.CanLoad(diskSave.world.scene))
        {LastError="No compatible save or its scene is unavailable.";return false;}
        pendingLoad=diskSave;SessionStarted=true;
        RecoilPrototype.ClearTravelForLoad();
        RecoilSceneLoader.Load(diskSave.world.scene);return true;
    }
    public void NewGame()
    {
        if(debugStartInCurrentScene){if(world)world.BeginGame();return;}
        pendingLoad=null;sections.Clear();SessionStarted=false;startingNewGame=true;EscortChild.ClearWaiting();RecoilPrototype.ClearSessionForNewGame();
        // Existing disk save remains recoverable until the new game has actually loaded.
        RecoilSceneLoader.Load(Recoil3DScene.Is3D?"Assets/Scenes/3D/RecoilPrototype.unity":"Assets/Scenes/RecoilPrototype.unity");
    }
    public void SetCheckpoint()
    {
        if(!world || !world.CanCaptureSave || !world.Player.Grounded || world.Player.Climbing || world.Player.WalkingStairs || OnMovingEquipment())return;
        safePosition=world.Player.Position;safeDepth=world.Player.GameplayZ;hasSafePosition=true;
    }
    public void ResetSafePosition(){if(world){safePosition=world.SaveFallbackPosition;safeDepth=world.Player.GameplayZ;hasSafePosition=true;}}
    public void PutSection<T>(string id,T value)
    {
        if(string.IsNullOrWhiteSpace(id))throw new ArgumentException("Save section needs a stable ID.");
        sections[id]=JsonUtility.ToJson(value);
    }
    public bool TryGetSection<T>(string id,out T value)
    {
        string json;if(sections.TryGetValue(id,out json)){value=JsonUtility.FromJson<T>(json);return true;}
        value=default(T);return false;
    }
    void CaptureParticipants()
    {
        if(!world)return;
        var ids=new HashSet<string>();
        foreach(var behaviour in world.gameObject.scene.GetRootGameObjects())
        foreach(var component in behaviour.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var participant=component as IRecoilSaveParticipant;if(participant==null)continue;
            if(string.IsNullOrEmpty(participant.SaveId)||!ids.Add(participant.SaveId))throw new InvalidDataException("Duplicate/empty save participant ID: "+participant.SaveId);
            sections[participant.SaveId]=participant.CaptureSaveJson();
        }
    }
    public void RestoreParticipants(RecoilPrototype context)
    {
        if(world!=context)return;
        foreach(var root in context.gameObject.scene.GetRootGameObjects())
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var participant=component as IRecoilSaveParticipant;string json;
            if(participant!=null&&!string.IsNullOrEmpty(participant.SaveId)&&sections.TryGetValue(participant.SaveId,out json))participant.RestoreSaveJson(json);
        }
    }
}
