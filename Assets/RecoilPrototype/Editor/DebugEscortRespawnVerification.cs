using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class DebugEscortRespawnVerification
{
    const string Folder="output/debug-escort",Request=Folder+"/run.request";
    const string Warehouse="Assets/Scenes/3D/RecoilFactoryWarehouse.unity",Store="Assets/Scenes/3D/RecoilWarehouseStoreroom.unity";
    static int stage,lastFrame=-1;
    static string fixtureContents;
    static DebugEscortRespawnVerification(){EditorApplication.update+=Poll;}
    [MenuItem("Recoil/Debug/Verify current scene and warehouse escort retries")]
    public static string Schedule()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first");
        Directory.CreateDirectory(Folder);File.WriteAllText(Request,SceneManager.GetActiveScene().path);
        EditorPrefs.SetBool(GameManager.DebugScenePreference,true);EditorApplication.isPlaying=true;return "Debug / escort integration check scheduled.";
    }
    static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
    static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static object Invoke(object o,string name,params object[] args){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);}
    static Vector2 Marker(RecoilPrototype w,string name){return w.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name).position;}
    static void Pause(RecoilPrototype w){Set(w,"paused",true);}
    static void CheckRetry(RecoilPrototype w,bool right)
    {
        Invoke(w,"ResetRun",false);Pause(w);
        var expected=Marker(w,right?"Storeroom Return Spawn":"Entrance Spawn");
        Need(Vector2.Distance(w.Player.Position,expected)<.03f,"Retry moved to wrong side; right="+right+" actual="+w.Player.Position);
        Need(w.Child&&w.Child.Alive&&w.Child.Following,"Retry did not restore the living child beside Dawn");
        Need(Vector2.Distance(w.Child.Position,w.Player.Position)<1.2f,"Child respawned away from Dawn");
    }
    static void Leave(RecoilPrototype w)
    {var door=w.GetComponentsInChildren<RecoilDoor>(true).Single(d=>d.name=="Leave warehouse");w.TravelToScene(door.destinationScene,door.destinationSpawn);}
    static void Poll()
    {
        if(!File.Exists(Request)||!EditorApplication.isPlaying||EditorApplication.isCompiling||Time.frameCount==lastFrame)return;
        lastFrame=Time.frameCount;
        try
        {
            var scene=SceneManager.GetActiveScene();var w=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).FirstOrDefault();if(!w||!w.Player)return;
            Pause(w);var manager=GameManager.EnsureInstance();Need(manager.debugStartInCurrentScene,"Debug mode disabled unexpectedly");
            if(stage==0)
            {
                Need(scene.path==File.ReadAllText(Request),"Debug startup changed the starting scene");
                Need(w.SaveSessionStarted,"Debug did not start the open scene");
                string fixture=Path.GetFullPath(Folder+"/unrelated-save.json");
                var save=new RecoilSaveData{world=new RecoilWorldSave{scene="Assets/Scenes/3D/RecoilPrototype.unity"}};
                RecoilSaveStore.Write(fixture,save);fixtureContents=File.ReadAllText(fixture);
                Set(manager,"savePathOverride",fixture);manager.ReloadDiskSave();
                Need(!manager.HasSave&&!manager.ContinueGame(),"Debug read an unrelated saved scene");
                Need(SceneManager.GetActiveScene().path==scene.path,"Continue bypassed debug scene lock");
                stage=1;RecoilSceneLoader.Load(Warehouse);return;
            }
            if(stage==1)
            {
                Need(w.IsWarehouseScene,"Warehouse did not load");Need(!w.HasItem(RecoilPrototype.WarehouseChildEvacuated),"Fresh debug run has stale evacuation progress");
                Need(Vector2.Distance(w.RetryPosition,Marker(w,"Entrance Spawn"))<.03f,"Initial warehouse entry should use left spawn");
                stage=2;w.TravelToScene(Store,"Entrance Spawn");return;
            }
            if(stage==2)
            {
                Need(scene.name=="RecoilWarehouseStoreroom"&&w.Child,"Storeroom/child missing");Invoke(w.Child,"TakeHand");
                Need(w.HasItem(RecoilPrototype.WarehouseRescueStarted)&&w.EscortingChild,"Taking the hand did not start escort progress");
                stage=3;w.TravelToScene(Warehouse,"Storeroom Return Spawn");return;
            }
            if(stage==3)
            {
                Need(w.WarehouseRescuePending&&!w.HasItem(RecoilPrototype.WarehouseChildEvacuated),"Returning from storage incorrectly completed escort");
                CheckRetry(w,true);
                w.Child.TakeHit();Need(!(bool)Invoke(w,"TryEnterDeathMemory"),"Memory appeared on first death");CheckRetry(w,true);
                w.Child.TakeHit();stage=4;Need((bool)Invoke(w,"TryEnterDeathMemory"),"Second death did not enter memory");return;
            }
            if(stage==4)
            {
                Need(scene.name=="RecoilKitchenMemory","Wrong death interlude");stage=5;Need(RecoilPrototype.ReturnFromKitchenMemory(),"Memory return failed");return;
            }
            if(stage==5)
            {
                Need(w.IsWarehouseScene&&w.WarehouseRescuePending,"Memory return lost rescue phase");
                Need(Vector2.Distance(w.Player.Position,Marker(w,"Storeroom Return Spawn"))<.03f,"Memory return moved player left");
                CheckRetry(w,true); // This reproduced the old bug: entry spawn was lost after memory.
                w.SetProgressFlag(EscortChild.EscortFlag,false);w.Child.ResetChild(false);
                EscortChild.RestoreWaitSave(new RecoilChildWaitSave{scene=Warehouse,position=w.Player.Position+Vector2.left,depth=0});Invoke(w,"ResetEscortChild");
                Need(!w.EscortingChild,"Could not leave child waiting");stage=6;Leave(w);return;
            }
            if(stage==6)
            {
                Need(!w.HasItem(RecoilPrototype.WarehouseChildEvacuated),"Leaving without the child completed escort");stage=7;w.TravelToScene(Warehouse,"Entrance Spawn");return;
            }
            if(stage==7)
            {
                Need(w.WarehouseRescuePending,"Incomplete rescue was lost on re-entry");CheckRetry(w,true);stage=8;Leave(w);return;
            }
            if(stage==8)
            {
                Need(w.HasItem(RecoilPrototype.WarehouseChildEvacuated)&&w.EscortingChild,"Actual escorted exit did not commit evacuation");stage=9;w.TravelToScene(Warehouse,"Entrance Spawn");return;
            }
            Need(w.IsWarehouseScene&&w.HasItem(RecoilPrototype.WarehouseChildEvacuated),"Completed escort lost on warehouse re-entry");
            CheckRetry(w,false);w.Child.TakeHit();CheckRetry(w,false);
            Need(File.ReadAllText(Path.GetFullPath(Folder+"/unrelated-save.json"))==fixtureContents,"Debug overwrote the saved game");
            Finish("PASS\nOpen scene starts directly; old saves ignored and untouched.\nInitial warehouse entry uses left; child hand-taking starts rescue; storage return and ordinary death use right.\nSecond-death kitchen memory return AND subsequent retry use right.\nLeaving alone does not complete rescue; retry restores both actors on right.\nOnly an actual exit with the child commits evacuation; after re-entry both actors retry from left.\n");
        }
        catch(Exception e){Finish("FAILED\n"+e);Debug.LogException(e);}
    }
    static void Finish(string result){File.WriteAllText(Folder+"/verification.txt",result);File.Delete(Request);EditorApplication.isPlaying=false;stage=0;lastFrame=-1;}
}
