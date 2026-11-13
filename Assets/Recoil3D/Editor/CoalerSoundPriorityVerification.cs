using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Isolated regression fixture: never loads or modifies an authored scene.
public static class CoalerSoundPriorityVerification
{
    static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static void Require(bool ok,string message){if(!ok)throw new Exception("Coaler sound priority: "+message);}
    [MenuItem("Recoil/AI/Verify Sound Investigation Priority")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Run outside Play mode.");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Temporary acoustic verification");SceneManager.MoveGameObjectToScene(root,scene);
            var world=root.AddComponent<RecoilPrototype>();Set(world,"started",true);Set(world,"simTime",100f);
            var go=new GameObject("Dawn");go.transform.SetParent(root.transform);var player=go.AddComponent<RecoilPlayerController>();
            Func<string,Transform> part=name=>{var t=new GameObject(name).transform;t.SetParent(go.transform);return t;};
            player.Configure(world,null,part("coat"),part("head"),part("hat"),part("gun"));Set(world,"player",player);
            player.ResetController(new Vector2(10,100));
            var enemyGo=new GameObject("Coaler");enemyGo.transform.SetParent(root.transform);enemyGo.transform.position=new Vector3(0,100,0);
            var enemy=enemyGo.AddComponent<RampagerController>();enemy.Initialize(world);enemy.returnToSleepAfterSound=false;enemy.roamingNoticeDelay=new Vector2(.01f,.01f);
            var motor=enemyGo.GetComponent<RampagerMovement>();var senses=enemyGo.GetComponent<RampagerPerception>();enemyGo.GetComponent<RampagerAttack>().enabled=false;
            Action reset=()=>{Set(world,"simTime",100f);enemy.initialState=RampagerController.Behaviour.Roaming;enemy.ResetController();player.ResetController(new Vector2(10,100));};
            Action<float> step=dt=>{Set(world,"simTime",world.Clock+dt);enemy.Simulate(dt);};
            reset();motor.SetVisualYaw(-90);
            enemy.HearNoise(new Vector3(-8,100,0),10);
            Require(!enemy.PrioritizingSound,"Ordinary rear sound must retain its reduced hearing range");
            enemy.HearNoise(new Vector3(-8,100,0),10,false);
            Require(enemy.PrioritizingSound,"Omnidirectional mechanical sound must be heard from behind within its advertised radius");
            // A nearby silent player must not overwrite a non-combat acoustic destination.
            reset();player.Position=new Vector2(1,100);step(.01f);
            Require(enemy.WatchingPlayer,"Close perception fixture must start watching");
            enemy.HearNoise(new Vector3(-5,100,0),20);step(.1f);
            Require(enemy.PrioritizingSound && enemy.LastKnownWorldPosition==new Vector3(-5,100,0) && enemy.Position.x<0,"Sound must override close watching and move away from the player");
            enemy.HearNoise(new Vector3(5,100,2),20);step(.01f);
            Require(enemy.PrioritizingSound && enemy.LastKnownWorldPosition==new Vector3(5,100,2),"New sound must replace the investigation including its depth");
            enemy.HearNoise(new Vector3(enemy.Position.x,100,0),20);step(enemy.soundSearchPause+.01f);
            Require(!enemy.PrioritizingSound && !enemy.WatchingPlayer,"Inspection must finish even with a nearby player");
            step(.01f);Require(enemy.WatchingPlayer,"Normal proximity behaviour must resume after inspection");
            reset();Set(enemy,"returning",true);player.Position=new Vector2(1,100);
            enemy.HearNoise(new Vector3(-5,100,0),20);step(.01f);
            Require(enemy.PrioritizingSound && !enemy.ReturningHome && enemy.LastKnownPosition.x==-5,"Sound must interrupt return navigation");
            reset();enemy.initialState=RampagerController.Behaviour.Sleeping;enemy.ResetController();player.Position=new Vector2(1,100);
            enemy.HearNoise(new Vector3(-5,100,0),20);step(2);
            Require(enemy.PrioritizingSound && enemy.State==RampagerController.Behaviour.Roaming && enemy.LastKnownPosition.x==-5,"Sound priority must survive waking beside a player");
            reset();enemy.HearNoise(new Vector3(-5,100,0),20);enemy.ReceiveBuildingWarning(3);
            Require(!enemy.PrioritizingSound,"Broadcast commands must retain their own navigation priority");
            reset();enemy.HearNoise(new Vector3(5,100,0),20);step(.01f);
            for(int i=0;i<1000 && enemy.Investigation>=enemy.soundSearchPause-.001f;i++)step(.01f);
            Require(enemy.PrioritizingSound && Mathf.Abs(5-enemy.Position.x-.22f)<.065f,"Idle investigation must reach the sound location, even after long travel");
            step(enemy.soundSearchPause+.01f);Require(!enemy.PrioritizingSound,"Idle investigation must finish");
            reset();player.Position=new Vector2(1,100);enemy.WatchForStory();enemy.HearNoise(new Vector3(-5,100,0),20);
            Set(enemy,"detectionProgress",1f);step(.01f);
            Require(enemy.PreparingAttack && !enemy.PrioritizingSound,"Full anger must end sound priority and start combat preparation");
            step(.51f);Require(enemy.State==RampagerController.Behaviour.Attack,"Combat preparation must still enter attack");
            enemy.HearNoise(new Vector3(-5,100,0),20);step(.01f);
            Require(!enemy.PrioritizingSound && enemy.LastKnownPosition==player.Position,"Combat must retain nearby live player targeting");
            reset();enemy.HearNoise(new Vector3(-5,100,0),20);
            enemy.ReceiveWaterImpact(new Vector2(2,100),Vector2.left,5,player.Position);
            Require(enemy.ReactingToWater && !enemy.PrioritizingSound,"Direct water impact must interrupt investigation");
            Require(SceneManager.sceneCount==1,"Verification must not add a loaded gameplay scene");
            Debug.Log("COALER_SOUND_PRIORITY_PASS: close watch, idle, return, sleeping, new sound/depth, arrival, anger/combat, direct hit, broadcast.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
