using UnityEngine;
using System.Collections.Generic;

public sealed partial class RecoilPrototype
{
    RecoilInventoryUI inventoryUI;
    readonly Queue<RecoilInventoryUI.Item> pendingPickups=new Queue<RecoilInventoryUI.Item>();
    public bool PickupOpen {get{return InventoryOpen&&inventoryUI.IsPickup;}}
    public void QueuePickup(string id,string itemName=null,string description=null,Texture2D icon=null)
    {if(!string.IsNullOrEmpty(id)){pendingPickups.Enqueue(RecoilInventoryUI.Describe(id,itemName,description,icon));TryShowPendingPickup();}}
    public bool PreviewPickup(string id)
    {
        if(!started||dead||won||paused||PropOperationActive||ConversationActive||ActiveReadable||ActiveElectricalPanel||LockerBlocksGameplay)return false;
        if(inventoryUI==null)inventoryUI=new RecoilInventoryUI(this);
        if(InventoryOpen)inventoryUI.Close();
        FrameDelta=0;inventoryUI.OpenPickup(RecoilInventoryUI.Describe(id),true);return true;
    }
    bool inventoryReleasePending;
    int inventoryReleaseFrame=-1;
    public bool InventoryOpen {get{return inventoryUI!=null&&inventoryUI.IsOpen;}}
    public bool OwnsWaterGun {get{return startWithWaterGun||HasItem("water_gun");}}
    public bool WaterGunEquipped {get{return OwnsWaterGun&&!HasItem("water_gun_unequipped");}}
    bool InventoryBlocksGameplay {get{return InventoryOpen||inventoryReleasePending||Time.frameCount==inventoryReleaseFrame;}}
    public void SetWaterGunEquipped(bool equipped)
    {if(OwnsWaterGun)SetProgressFlag("water_gun_unequipped",!equipped);}
    public bool OpenInventory(bool preview=false)
    {
        if(!started||dead||won||paused||PropOperationActive||ConversationActive||ActiveReadable||ActiveElectricalPanel||LockerBlocksGameplay)return false;
        if(inventoryUI==null)inventoryUI=new RecoilInventoryUI(this);
        if(InventoryOpen)return true;
        InterruptInteraction();FrameDelta=0;inventoryUI.Open(preview);return true;
    }
    public void CloseInventory()
    {
        if(inventoryUI!=null)inventoryUI.Close();
        inventoryReleasePending=true;inventoryReleaseFrame=Time.frameCount;FrameDelta=0;
    }
    bool TryShowPendingPickup()
    {
        if(pendingPickups.Count==0||InventoryOpen||inventoryReleasePending||!started||dead||won||paused||PropOperationActive||ConversationActive||ActiveReadable||ActiveElectricalPanel||LockerBlocksGameplay)return false;
        if(inventoryUI==null)inventoryUI=new RecoilInventoryUI(this);
        FrameDelta=0;ReaderInputConsumed=true;inventoryUI.OpenPickup(pendingPickups.Dequeue());return true;
    }
    bool HandleInventoryInput()
    {
        if(InventoryOpen){inventoryUI.Tick();FrameDelta=0;ReaderInputConsumed=true;return true;}
        if(inventoryReleasePending)
        {
            if(!Input.GetMouseButton(0)&&!RecoilInput.ConfirmHeld&&!RecoilInput.InteractHeld&&!Input.GetKey(KeyCode.Return)&&!Input.GetKey(KeyCode.Space))inventoryReleasePending=false;
            inventoryReleaseFrame=Time.frameCount;FrameDelta=0;ReaderInputConsumed=true;return true;
        }
        if(TryShowPendingPickup())return true;
        if(RecoilInput.InventoryPressed&&OpenInventory()){ReaderInputConsumed=true;return true;}
        return false;
    }
}
