using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RecoilInventoryPreview
{
    const string Request="Recoil.Inventory.Request";
    static RecoilInventoryPreview(){EditorApplication.update+=Poll;}
    [MenuItem("Recoil/Inventory/Preview with sample items")]
    public static void SchedulePreview(){Schedule("preview");}
    [MenuItem("Recoil/Inventory/Verify implementation")]
    public static void ScheduleVerification(){Schedule("verify");}
    static void Schedule(string mode)
    {SessionState.SetString(Request,mode);if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;}
    static RecoilPrototype World()
    {return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).FirstOrDefault();}
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Poll()
    {
        string request=SessionState.GetString(Request,"");if(request==""||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var world=World();if(!world||!world.Player||!world.SaveSessionStarted)return;
        SessionState.EraseString(Request);
        try
        {
            if(request=="preview"){Need(world.OpenInventory(true),"Cannot open preview in current gameplay state");return;}
            Verify(world);
        }
        catch(Exception e){Directory.CreateDirectory("output/inventory-ui");File.WriteAllText("output/inventory-ui/verification.txt","FAILED\n"+e);Debug.LogException(e);}
    }
    static void Verify(RecoilPrototype w)
    {
        var field=typeof(RecoilPrototype).GetField("inventoryUI",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
        float scale=Time.timeScale;string before=JsonUtility.ToJson(w.CaptureWorldSave(w.Player.Position,w.Player.GameplayZ));
        Need(w.OpenInventory(true),"Preview did not open");var ui=(RecoilInventoryUI)field.GetValue(w);
        Need(w.InventoryOpen&&!w.Playing&&Time.timeScale==0,"Inventory did not suspend gameplay");
        Need(ui.ItemCount==3&&ui.SelectedCell==0,"Default selection or sample catalogue wrong");
        Need(ui.SlotOwner(0)==ui.SlotOwner(1)&&ui.SlotOwner(2)==ui.SlotOwner(5),"Multi-cell ownership mismatch");
        Need(ui.SlotOwner(4)<0&&ui.SlotOwner(6)<0&&ui.SlotOwner(7)<0&&ui.SlotOwner(8)<0,"Empty cell layout wrong");
        ui.MoveSelection(1,0);Need(ui.SelectedCell==2,"Navigation selected same gun twice");
        ui.MoveSelection(0,1);Need(ui.SelectedCell==8,"Navigation selected same fuse twice");
        ui.MoveSelection(-1,0);ui.MoveSelection(0,-1);ui.MoveSelection(-1,0);Need(ui.SelectedItem.kind=="key","Key selection failed");
        w.CloseInventory();Need(Time.timeScale==scale&&!w.InventoryOpen,"Close did not restore time scale");
        string after=JsonUtility.ToJson(w.CaptureWorldSave(w.Player.Position,w.Player.GameplayZ));Need(before==after,"Preview altered gameplay inventory, weapon or checkpoint");
        bool oldFlag=w.HasItem("water_gun_unequipped");
        if(w.OwnsWaterGun)
        {
            int ammo=w.Player.Ammo;w.SetWaterGunEquipped(false);Need(!w.HasWaterGun,"Unequip still allows gun use");
            w.SetWaterGunEquipped(true);Need(w.HasWaterGun&&w.Player.Ammo==ammo,"Equip changed ammo or failed");w.SetProgressFlag("water_gun_unequipped",oldFlag);
        }
        Need(w.OpenInventory(false),"Actual inventory did not open");ui=(RecoilInventoryUI)field.GetValue(w);Need(!ui.Preview,"Actual inventory uses sample data");w.CloseInventory();
        Directory.CreateDirectory("output/inventory-ui");File.WriteAllText("output/inventory-ui/verification.txt","PASS\n3x3 slots, horizontal/vertical item ownership, first-item default, directional navigation skips same item.\nPreview leaves real inventory/weapon/health/progress unchanged. Open freezes gameplay; close restores prior time scale.\nActual inventory uses owned items. Equip/unequip respects ownership and preserves ammo.\n");
        w.OpenInventory(true);
        SessionState.SetFloat("Recoil.Inventory.CaptureAt",Time.realtimeSinceStartup+.25f);EditorApplication.update-=Capture;EditorApplication.update+=Capture;
    }
    static void Capture()
    {
        if(!EditorApplication.isPlaying||Time.realtimeSinceStartup<SessionState.GetFloat("Recoil.Inventory.CaptureAt",0))return;
        EditorApplication.update-=Capture;
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("output/inventory-ui/Inventory-GameView.png"));
    }
}
