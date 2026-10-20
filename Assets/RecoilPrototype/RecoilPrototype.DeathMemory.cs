using System.Collections.Generic;
using UnityEngine;

// Death interlude flow. State survives scene changes, but resets when a new Play session starts.
public sealed partial class RecoilPrototype
{
    public const string KitchenMemoryScene="Assets/Scenes/RecoilKitchenMemory.unity";
    sealed class MemoryReturn
    {
        public string scene;public Vector2 position;public string[] entryItems,retryItems;
        public bool checkpoint;public int attempts;public float elapsed;
    }
    static MemoryReturn memoryReturn;
    static bool returningFromMemory;
    bool deathCountedForMemory;
    public static int DeathsThisSession {get;private set;}
    public static int KitchenMemoryVisits {get;private set;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetDeathMemorySession(){memoryReturn=null;returningFromMemory=false;KitchenMemoryVisits=DeathsThisSession=0;}
    void RegisterMemorySceneEntry()
    {
        if(gameObject.scene.path==KitchenMemoryScene)started=true;
        if(!returningFromMemory || memoryReturn==null || memoryReturn.scene!=gameObject.scene.path)return;
        var saved=memoryReturn;returningFromMemory=false;memoryReturn=null;
        checkpoint=saved.checkpoint;attempts=saved.attempts;elapsed=saved.elapsed;
        entryInventory.Clear();entryInventory.AddRange(saved.entryItems);
        player.ResetController(saved.position);started=true;
        if(cameraController)cameraController.SnapToTarget();
    }
    bool ShouldPlayDeathMemory()
    {
        if(!dead || gameObject.scene.path==KitchenMemoryScene)return false;
        // Update runs on every game-over frame. Count this death only once until retry.
        if(!deathCountedForMemory){deathCountedForMemory=true;DeathsThisSession++;}
        return DeathsThisSession==2 && KitchenMemoryVisits==0 && memoryReturn==null;
    }
    bool TryEnterDeathMemory()
    {
        if(!ShouldPlayDeathMemory() || !RecoilSceneLoader.CanLoad(KitchenMemoryScene))return false;
        var retry=new HashSet<string>(entryInventory);
        foreach(var id in inventory)if(RetainedOnRetry(id))retry.Add(id);
        memoryReturn=new MemoryReturn{scene=gameObject.scene.path,position=checkpoint?CheckpointPosition:spawn,entryItems=entryInventory.ToArray(),retryItems=new List<string>(retry).ToArray(),checkpoint=checkpoint,attempts=attempts+1,elapsed=elapsed};
        KitchenMemoryVisits++;InterruptInteraction();FrameDelta=0;
        RecoilSceneLoader.Load(KitchenMemoryScene);return true;
    }
    static bool RetainedOnRetry(string id)
    {return id=="water_gun" || id==UniversalKey || id==EscortChild.EscortFlag || id.StartsWith("station_") || id.StartsWith("factory_shortcut_") || id.StartsWith("warehouse_power_");}
    public static bool ReturnFromKitchenMemory()
    {
        if(memoryReturn==null || returningFromMemory)return false;
        if(!RecoilSceneLoader.CanLoad(memoryReturn.scene))return false;
        returningFromMemory=true;
        pendingTravel=new SceneTravel{scene=memoryReturn.scene,spawn="",items=memoryReturn.retryItems,health=3};
        RecoilSceneLoader.Load(memoryReturn.scene);return true;
    }
    void RestartFullGameMemory(){ResetDeathMemorySession();}
}
