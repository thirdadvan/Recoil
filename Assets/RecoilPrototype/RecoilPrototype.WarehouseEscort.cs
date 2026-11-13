using UnityEngine;

// Escort progress is distinct from the temporary hand-holding state.
public sealed partial class RecoilPrototype
{
    public const string WarehouseRescueStarted="warehouse_child_rescue_started";
    public const string WarehouseChildEvacuated="warehouse_child_evacuated";
    public bool IsWarehouseScene {get{return gameObject.scene.name=="RecoilFactoryWarehouse";}}
    public bool WarehouseRescuePending {get{return IsWarehouseScene&&!HasItem(WarehouseChildEvacuated)&&(HasItem(WarehouseRescueStarted)||HasItem(EscortChild.EscortFlag));}}
    Transform warehouseRightRetry,warehouseLeftRetry;
    Vector2 WarehouseMarker(string name,ref Transform cache,Vector2 fallback)
    {
        if(!cache)foreach(var t in GetComponentsInChildren<Transform>(true))if(t.name==name){cache=t;break;}
        return cache?(Vector2)cache.position:fallback;
    }
    public Vector2 RetryPosition
    {
        get
        {
            if(WarehouseRescuePending)return WarehouseMarker("Storeroom Return Spawn",ref warehouseRightRetry,spawn);
            if(IsWarehouseScene&&HasItem(WarehouseChildEvacuated))return WarehouseMarker("Entrance Spawn",ref warehouseLeftRetry,spawn);
            return checkpoint?CheckpointPosition:spawn;
        }
    }
    public void BeginWarehouseChildRescue()
    {
        if(IsWarehouseScene||gameObject.scene.name=="RecoilWarehouseStoreroom")RememberProgress(WarehouseRescueStarted);
    }
    static string SceneFileName(string scene)
    {string s=scene.Replace('\\','/');s=s.Substring(s.LastIndexOf('/')+1);return s.EndsWith(".unity")?s.Substring(0,s.Length-6):s;}
    bool CompletingWarehouseEscort(string destination)
    {
        if(!IsWarehouseScene||!EscortingChild)return false;
        string name=SceneFileName(destination);
        return name!="RecoilWarehouseStoreroom"&&name!="RecoilKitchenMemory"&&name!="RecoilFactoryWarehouse";
    }
    void PrepareWarehouseEscortRetry(bool full)
    {
        if(full||!IsWarehouseScene||!(WarehouseRescuePending||HasItem(WarehouseChildEvacuated)))return;
        // Retrying an escort restores both actors together, even if the child was waiting or killed.
        EscortChild.ClearWaiting();inventory.Add(EscortChild.EscortFlag);
    }
}
