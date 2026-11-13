using UnityEngine;

// Uses the existing saved inventory and pickup presentation.
public sealed class CrowbarPickup : ContinuousInteractionItem
{
    public const string InventoryId="crowbar";
    public GameObject model;
    void Awake(){if(!GetComponent<PickupVisibility>())gameObject.AddComponent<PickupVisibility>();}
    public override bool Advance(RecoilPrototype world,float dt)
    {
        if(Completed||world.HasItem(InventoryId))return true;
        if(!InRange(world)||!base.Advance(world,dt))return false;
        if(model)model.SetActive(false);return true;
    }
    public override void ResetItem(RecoilPrototype world=null)
    {base.ResetItem(world);if(model)model.SetActive(!Completed);}
}
