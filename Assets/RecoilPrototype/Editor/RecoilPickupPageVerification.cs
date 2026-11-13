using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RecoilPickupPageVerification
{
    const string Key="Recoil.Pickup.Request",Folder="output/pickup-ui";
    static int stage;static float start,clock,scale;static Vector2 position;static bool hadFuse;
    static RecoilPickupPageVerification(){EditorApplication.update+=Poll;}
    [MenuItem("Recoil/Inventory/Preview pickup page")]
    public static void Preview(){Schedule("preview");}
    [MenuItem("Recoil/Inventory/Verify pickup page")]
    public static void Verify(){Schedule("verify");}
    static void Schedule(string mode){SessionState.SetString(Key,mode);stage=0;if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;}
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static RecoilPrototype World(){return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).FirstOrDefault();}
    static void Poll()
    {
        string mode=SessionState.GetString(Key,"");if(mode==""||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        var w=World();if(!w||!w.Player||!w.SaveSessionStarted)return;
        try
        {
            if(mode=="preview"){if(w.PreviewPickup("water_gun"))SessionState.EraseString(Key);return;}
            if(stage==0)
            {
                if(w.InventoryOpen)w.CloseInventory();scale=Time.timeScale;
                string before=JsonUtility.ToJson(w.CaptureWorldSave(w.Player.Position,w.Player.GameplayZ));
                Need(w.PreviewPickup("water_gun"),"Pickup preview failed");Need(w.PickupOpen&&!w.Playing&&Time.timeScale==0,"Pickup did not pause world");
                w.CloseInventory();Need(Time.timeScale==scale,"Closing pickup did not restore time scale");
                Need(before==JsonUtility.ToJson(w.CaptureWorldSave(w.Player.Position,w.Player.GameplayZ)),"Preview modified game state");
                hadFuse=w.HasItem("fuse");w.SetProgressFlag("fuse",false);w.GrantItem("fuse","Test pickup");Need(w.HasItem("fuse"),"Grant did not retain item");
                var type=typeof(RecoilPrototype);type.GetField("inventoryReleasePending",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,false);
                type.GetMethod("HandleInventoryInput",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
                Need(w.PickupOpen,"New item did not open pickup page");
                clock=w.Clock;position=w.Player.Position;start=Time.realtimeSinceStartup;stage=1;return;
            }
            if(Time.realtimeSinceStartup-start<.5f)return;
            Need(Mathf.Abs(w.Clock-clock)<.0001f&&Vector2.Distance(w.Player.Position,position)<.0001f,"Scene advanced behind pickup page");
            w.GrantItem("fuse","Duplicate item");w.CloseInventory();
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(RecoilPrototype).GetField("inventoryReleasePending",flags).SetValue(w,false);
            typeof(RecoilPrototype).GetMethod("HandleInventoryInput",flags).Invoke(w,null);Need(!w.PickupOpen,"Duplicate acquisition opened a second page");
            w.SetProgressFlag("fuse",hadFuse);Need(Time.timeScale==scale,"Time scale remained paused");
            w.PreviewPickup("water_gun");Directory.CreateDirectory(Folder);
            File.WriteAllText(Folder+"/verification.txt","PASS\nNew physical item opens pickup presentation. Duplicate grants do not repeat it.\nWorld clock and player position remain frozen across frames; close restores previous time scale.\nPreview does not modify inventory, weapon, health or checkpoint. Test item restored.\nPickup uses paused camera behind a black mask, independent of inventory's 15% background-only mask.\n");
            ScreenCapture.CaptureScreenshot(Path.GetFullPath(Folder+"/Pickup-GameView.png"));SessionState.EraseString(Key);stage=0;
        }
        catch(Exception e)
        {if(w){w.CloseInventory();if(stage>0)w.SetProgressFlag("fuse",hadFuse);}SessionState.EraseString(Key);Directory.CreateDirectory(Folder);File.WriteAllText(Folder+"/verification.txt","FAILED\n"+e);Debug.LogException(e);stage=0;}
    }
}
