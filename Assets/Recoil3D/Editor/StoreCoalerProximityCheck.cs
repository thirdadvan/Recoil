using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StoreCoalerProximityCheck
{
    const string Pending="Recoil.StoreProximityCheck";
    static StoreCoalerProximityCheck(){EditorApplication.playModeStateChanged+=OnPlay;}
    static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    [MenuItem("Recoil/AI/Check Store Foreground Proximity")]
    public static void Run()
    {
        if(EditorApplication.isPlaying){Debug.LogWarning("Run the store placement check outside Play to preserve the active game.");return;}
        SessionState.SetBool(Pending,true);EditorApplication.isPlaying=true;
    }
    static void OnPlay(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false))return;
        SessionState.SetBool(Pending,false);EditorApplication.delayCall+=()=>{EditorApplication.delayCall+=()=>Capture(true);};
    }
    static void Capture(bool exit)
    {
        try
        {
            var world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(!world || !world.Player)throw new Exception("No initialized gameplay world");
            var player=world.Player;Directory.CreateDirectory("Captures/CoalerXZ");
            using(var log=new StreamWriter("Captures/CoalerXZ/StoreActualScene.txt",false))
            {
                log.WriteLine(world.gameObject.scene.path);
                foreach(var c in world.GetComponentsInChildren<BoxCollider2D>())if(!c.isTrigger && c.bounds.max.x>-37 && c.bounds.min.x<-20 && c.bounds.min.y<3 && c.bounds.max.y>0)
                    log.WriteLine($"Nearby solid {c.name}: {c.bounds}; enabled={c.enabled}");
                foreach(var c in world.GetComponentsInChildren<Collider>())if(c.bounds.max.x>-37 && c.bounds.min.x<-20 && c.bounds.min.y<3 && c.bounds.max.y>0 && !c.GetComponentInParent<RampagerController>() && !c.GetComponentInParent<RecoilPlayerController>())
                    log.WriteLine($"Nearby native {c.name}: {c.bounds}; enabled={c.enabled}");
                foreach(var enemy in world.GetComponentsInChildren<RampagerController>())
                {
                    if(!enemy.name.StartsWith("Store exterior Rampager"))continue;
                    var motor=enemy.GetComponent<RampagerMovement>();var sense=enemy.GetComponent<RampagerPerception>();var grab=enemy.GetComponent<RampagerGrab>();
                    Vector3 spawn=enemy.transform.position;player.ResetController(new Vector2(spawn.x,.8f));enemy.WatchForStory();
                    log.WriteLine($"{enemy.name} spawn {spawn:F3}; Dawn {player.WorldPosition:F3}; radius {grab.reach}; approach speed {enemy.CurrentMoveSpeed:F3}m/s");
                    for(int axis=0;axis<2;axis++)
                    {
                        float maximum=-1,maximumFusion=-1;
                        for(int i=0;i<=70;i++)
                        {
                            float d=i*.05f;var p=player.WorldPosition+(axis==0?Vector3.left:Vector3.back)*d;p.y=spawn.y;motor.SetNavigationPosition(p);
                            if(sense.CanSeePlayer())maximum=d;if(grab.Evaluate())maximumFusion=d;
                        }
                        log.WriteLine($"{(axis==0?"X":"Z")} max sensed {maximum:F3}; max fusion {maximumFusion:F3}");
                    }
                    if(enemy.name=="Store exterior Rampager 1")for(int axis=0;axis<2;axis++)
                    {
                        enemy.ResetController();motor.SetNavigationPosition(new Vector3(spawn.x-(axis==0?5:0),spawn.y,player.GameplayZ-(axis==1?3:0)));enemy.WatchForStory();
                        for(int i=0;i<4000;i++){Set(world,"simTime",world.Clock+.01f);enemy.Simulate(.01f);}
                        var delta=enemy.transform.position-player.WorldPosition;log.WriteLine($"{(axis==0?"X":"Z")} actual nav final {enemy.transform.position:F3}; horizontal gap {new Vector2(delta.x,delta.z).magnitude:F4}; last known {enemy.LastKnownWorldPosition:F3}; phase {enemy.GetComponent<RampagerDepthArrival>().Phase}; path {enemy.GetComponent<RampagerDepthArrival>().PathCount}");
                    }
                    motor.SetNavigationPosition(spawn);
                }
            }
            Debug.Log("STORE_COALER_PROXIMITY_CHECK_COMPLETE");
        }
        catch(Exception error){Debug.LogException(error);}
        finally{if(exit)EditorApplication.isPlaying=false;}
    }
}
