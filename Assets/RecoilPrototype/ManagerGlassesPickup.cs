using UnityEngine;

// Uses the shared interaction and inventory flags, including save/load and scene travel.
public sealed class ManagerGlassesPickup : ContinuousInteractionItem
{
    public const string CollectedFlag="manager_glasses_collected";
    public const string InventoryId = "manager_glasses";
    public GameObject model;

    void Awake()
    {
        if (!GetComponent<PickupVisibility>()) gameObject.AddComponent<PickupVisibility>();
    }

    void Reset()
    {
        itemId = resultId = InventoryId;
        displayName = "Manager's glasses";
        description = "Pick up the manager's dark brown square glasses.";
        interactionDuration = 0;
        interactionRange = 1.5f;
        resultMessage = "Manager's glasses added to inventory.";
    }

    public override bool Advance(RecoilPrototype world, float dt)
    {
        if (Completed || world.HasItem(InventoryId) || world.HasItem(CollectedFlag) || world.HasItem(DoorDialogue.GlassesReturnedFlag)) {SetVisible(false);return true;}
        if (!InRange(world)) return false;
        if (!base.Advance(world, dt)) return false;
        world.RememberProgress(CollectedFlag);SetVisible(false);
        return true;
    }

    public override void ResetItem(RecoilPrototype world = null)
    {
        base.ResetItem(world);
        if(world && (world.HasItem(CollectedFlag)||world.HasItem(DoorDialogue.GlassesReturnedFlag)))Completed=true;
        SetVisible(!Completed);
    }

    void SetVisible(bool visible)
    {
        if (model) model.SetActive(visible);
    }
}
