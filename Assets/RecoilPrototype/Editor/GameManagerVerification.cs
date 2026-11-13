using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GameManagerVerification
{
    const string Request="output/game-manager/run.request",Report="output/game-manager/verification.txt";
    static int stage,wait;
    static string testPath,origin;
    static bool hooked;
    static GameManagerVerification(){EditorApplication.update+=Poll;}
    [MenuItem("Recoil/Save System/Verify save and scene transitions")]
    public static string Schedule()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play before starting isolated save checks.");
        Directory.CreateDirectory("output/game-manager");File.WriteAllText(Request,"run");
        EditorApplication.isPlaying=true;return "Save integration checks scheduled.";
    }
    static void Check(bool ok,string detail){if(!ok)throw new Exception(detail);}
    static void Poll()
    {
        if(!File.Exists(Request)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        if(++wait<(stage==0?25:3))return;
        try
        {
            var world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(!world||!world.Player)return;
            var manager=GameManager.EnsureInstance();
            if(!hooked)
            {
                testPath=Path.GetFullPath("output/game-manager/isolated-slot.json");
                typeof(GameManager).GetField("savePathOverride",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(manager,testPath);
                hooked=true;manager.debugStartInCurrentScene=false;manager.ReloadDiskSave();
            }
            if(stage==0)
            {
                TestStore();origin=world.gameObject.scene.path;
                world.BeginGame();
                world.RememberProgress("verification_key");world.RememberProgress("warehouse_power_freight_connected");
                world.Player.RestoreWeaponSave(new RecoilWeaponSave{capacity=15,ammo=11,automaticRecoil=true,shotCooldown=.5f});world.Player.RestoreHealth(2);
                EscortChild.RestoreWaitSave(new RecoilChildWaitSave{scene="Assets/Scenes/3D/RecoilWarehouseStoreroom.unity",position=new Vector2(3,1),depth=.7f});
                RecoilPrototype.RestoreMemorySave(new RecoilMemorySave{deaths=0,visits=0});
                Check(manager.SaveGame(),"Could not save live world: "+manager.LastError);
                RecoilPrototype.RestoreMemorySave(new RecoilMemorySave{deaths=1,visits=0});
                Check(manager.SaveGame(),"Explicit narrative save failed");
                // Check the actual on-disk serializer rather than the manager's in-memory snapshot.
                RecoilSaveData data;string error;Check(RecoilSaveStore.TryRead(testPath,out data,out error),"Live save unreadable");
                Check(data.world.weapon.ammo==11&&data.world.weapon.capacity==15&&data.world.health==2,"Weapon/health not captured");
                world.SetProgressFlag("verification_key",false);world.Player.RestoreWeaponSave(new RecoilWeaponSave{capacity=3,ammo=0});
                Check(manager.ContinueGame(),"Continue failed: "+manager.LastError);stage=1;wait=0;return;
            }
            if(stage==1)
            {
                Check(world.gameObject.scene.path==origin,"Continue loaded wrong scene");
                Check(world.HasItem("verification_key")&&world.HasItem("warehouse_power_freight_connected"),"Inventory/progress lost on load");
                Check(world.Player.ammoCapacity==15 && world.Player.Ammo==11 && world.Player.Health==2,"Weapon/health lost on continue");
                Check(RecoilPrototype.DeathsThisSession==1,"Death counter lost");
                Check(EscortChild.WaitingElsewhere(origin),"Child waiting scene lost");
                world.TravelToScene("Assets/Scenes/3D/RecoilServiceStore.unity","");stage=2;wait=0;return;
            }
            if(stage==2)
            {
                Check(world.gameObject.scene.path.EndsWith("RecoilServiceStore.unity"),"Travel did not load store");
                Check(world.HasItem("verification_key")&&world.Player.ammoCapacity==15&&world.Player.Ammo==11&&world.Player.Health==2,"Travel reset inventory/ammo/health");
                Check(manager.SaveGame(),"Destination save failed");
                Check(manager.ContinueGame(),"Destination reload failed");stage=3;wait=0;return;
            }
            Check(world.gameObject.scene.path.EndsWith("RecoilServiceStore.unity")&&world.HasItem("verification_key")&&world.Player.ammoCapacity==15,"Saved destination did not resume");
            File.WriteAllText(Report,"PASS\nJSON round-trip; primary+backup; corrupted primary recovery; invalid/future version rejection; live inventory/weapon/health restoration; death count; waiting child; actual Continue reload; cross-scene ammo/inventory/health transfer; destination resume.\nAll test saves isolated under output/game-manager; normal EditorSaves and player Saves untouched.\n");
            Finish();
        }
        catch(Exception e){File.WriteAllText(Report,"FAILED\n"+e);Debug.LogException(e);Finish();}
    }
    static void Finish(){File.Delete(Request);EditorApplication.isPlaying=false;stage=wait=0;hooked=false;}
    static void TestStore()
    {
        string path=Path.GetFullPath("output/game-manager/store-test.json");
        if(File.Exists(path))File.Delete(path);if(File.Exists(path+".bak"))File.Delete(path+".bak");
        var data=new RecoilSaveData{world=new RecoilWorldSave{scene="Assets/Scenes/3D/RecoilPrototype.unity",position=new Vector2(1,2)}};
        data.world.items.Add("door_open");RecoilSaveStore.Write(path,data);
        data.world.weapon.capacity=15;data.world.weapon.ammo=7;RecoilSaveStore.Write(path,data);
        RecoilSaveData read;string error;Check(RecoilSaveStore.TryRead(path,out read,out error)&&read.world.weapon.ammo==7&&read.world.items.Contains("door_open"),"JSON round trip failed");
        File.WriteAllText(path,"{broken");Check(RecoilSaveStore.TryRead(path,out read,out error)&&read.world.weapon.capacity==3,"Backup recovery failed");
        data.version=999;Check(!RecoilSaveStore.Valid(data),"Future save accepted");data.version=1;data.world.position=new Vector2(float.NaN,1);Check(!RecoilSaveStore.Valid(data),"NaN position accepted");
        data.world.position=Vector2.zero;data.version=999;File.WriteAllText(path,JsonUtility.ToJson(data));
        data.version=1;bool protectedFuture=false;try{RecoilSaveStore.Write(path,data);}catch(InvalidDataException){protectedFuture=true;}
        Check(protectedFuture,"Overwrote a newer save version");
    }
}
