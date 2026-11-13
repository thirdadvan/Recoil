using System.Collections.Generic;
using UnityEngine;

public sealed partial class RecoilPrototype
{
    public bool SaveSessionStarted {get{return started;}}
    public bool CanCaptureSave {get{return started&&!dead&&!won&&player&&player.Health>0&&!PropOperationActive;}}
    public Vector2 SaveFallbackPosition {get{return RetryPosition;}}
    public void BeginGame()
    {started=true;GameManager.EnsureInstance().StartSession();Say("Fire. Then use the direction controls to fly.");}
    public static void ClearTravelForLoad(){pendingTravel=null;memoryReturn=null;returningFromMemory=false;}
    public static void ClearSessionForNewGame(){ClearTravelForLoad();ResetDeathMemorySession();}
    public RecoilWorldSave CaptureWorldSave(Vector2 safePosition,float safeDepth)
    {
        var items=new List<string>(inventory);
        // A cable held by the UI is a transient gesture; reconnect it from its unplugged position.
        items.Remove("warehouse_power_lighting_held");items.Remove("warehouse_power_freight_held");
        return new RecoilWorldSave{scene=gameObject.scene.path,position=safePosition,depth=safeDepth,entrySpawn=spawn,checkpoint=checkpoint,hasClue=hasClue,attempts=attempts,elapsed=elapsed,items=items,entryItems=new List<string>(entryInventory),weapon=player.CaptureWeaponSave(),health=player.Health};
    }
    public void RestoreWorldSave(RecoilWorldSave data)
    {
        spawn=data.entrySpawn;checkpoint=data.checkpoint;attempts=Mathf.Max(1,data.attempts);elapsed=data.elapsed;
        // Reset interactable visuals against saved flags, then retain the original retry baseline.
        entryInventory.Clear();entryInventory.AddRange(data.items);ResetRun(false);
        entryInventory.Clear();entryInventory.AddRange(data.entryItems);
        hasClue=data.hasClue;if(clue)clue.gameObject.SetActive(legacyEvidenceEnabled&&!hasClue);
        player.ResetController(WarehouseRescuePending?RetryPosition:data.position);player.RestoreSavedDepth(data.depth);player.RestoreWeaponSave(data.weapon);player.RestoreHealth(data.health);
        ResetEscortChild();started=true;
        if(cameraController)cameraController.SnapToTarget();
    }
    public static RecoilMemorySave CaptureMemorySave()
    {
        var data=new RecoilMemorySave{deaths=DeathsThisSession,visits=KitchenMemoryVisits,returning=returningFromMemory};
        if(memoryReturn!=null)
        {
            data.hasReturn=true;data.hasEntrySpawn=true;data.entrySpawn=memoryReturn.entrySpawn;data.weapon=memoryReturn.weapon;data.scene=memoryReturn.scene;data.position=memoryReturn.position;
            data.entryItems=memoryReturn.entryItems;data.retryItems=memoryReturn.retryItems;
            data.checkpoint=memoryReturn.checkpoint;data.attempts=memoryReturn.attempts;data.elapsed=memoryReturn.elapsed;
        }
        return data;
    }
    public static void RestoreMemorySave(RecoilMemorySave data)
    {
        if(data==null)return;
        DeathsThisSession=Mathf.Max(0,data.deaths);KitchenMemoryVisits=Mathf.Max(0,data.visits);returningFromMemory=data.returning;
        memoryReturn=data.hasReturn?new MemoryReturn{scene=data.scene,position=data.position,entrySpawn=data.hasEntrySpawn?data.entrySpawn:data.position,entryItems=data.entryItems??new string[0],retryItems=data.retryItems??new string[0],checkpoint=data.checkpoint,attempts=data.attempts,elapsed=data.elapsed,weapon=data.weapon}:null;
    }
}
