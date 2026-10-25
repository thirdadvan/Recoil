using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoalerAcousticAIVerification
{
    static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static void Require(bool ok,string message){if(!ok)throw new Exception("Acoustic Coaler: "+message);}
    [MenuItem("Recoil/AI/Verify Acoustic Coaler")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Run verification outside Play mode.");return;}
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
            float nearRadius=enemy.CloseSenseRadius;
            player.Position=enemy.Position+Vector2.left*(nearRadius*.8f);
            Require(!senses.CanSeePlayer(),"Rear proximity beyond 60 percent must not reveal Dawn");
            Require(senses.CanTouchPlayer(nearRadius),"Rear weakness must not shorten physical fusion or attack reach");
            player.Position=enemy.Position+Vector2.left*(nearRadius*.59f);
            Require(senses.CanSeePlayer(),"Rear proximity inside 60 percent must still reveal Dawn");
            player.Position=enemy.Position+Vector2.right*(nearRadius*.8f);
            Require(senses.CanSeePlayer(),"Front proximity range must remain unchanged");
            enemy.HearNoise(new Vector3(-8,100,0),10);Require(enemy.SoundEnergy==0,"Rear sound outside the reduced range must not be heard");
            enemy.HearNoise(new Vector3(8,100,0),10);Require(enemy.SoundEnergy>0,"Front sound range must remain unchanged");
            motor.SetVisualYaw(0);
            Require(Mathf.Abs(senses.PerceptionRangeMultiplier(enemy.transform.position+Vector3.forward*3)-.6f)<.001f,"Rear sector must rotate into Z with the body");
            Require(senses.PerceptionRangeMultiplier(enemy.transform.position+Vector3.right*3)==1,"Side range must remain unchanged after turning");
            reset();Require(!senses.CanSeePlayer(),"Must not see a distant player");
            player.Position=new Vector2(1,100);Require(senses.CanSeePlayer(),"Fusion area must reveal player irrespective of facing");
            step(.01f);Require(enemy.WatchingPlayer,"Entering proximity must begin watching");
            Set(player,"silentWalking",false);Set(player,"grounded",true);Set(player,"<ActualHorizontalSpeed>k__BackingField",player.walkSpeed);
            Set(player,"shotAt",world.Clock);Set(player,"<ShotSequence>k__BackingField",1);
            step(.1f);Require(enemy.SoundEnergy==0 && enemy.State!=RampagerController.Behaviour.Attack,"Running and empty shots must not add passive anger");
            reset();motor.MovementPlaneZ=enemy.CloseSenseRadius+.5f;motor.ApplyPosition();player.Position=new Vector2(0,100);
            Require(!senses.CanSeePlayer(),"Fusion perception must include Z separation");
            motor.MovementPlaneZ=enemy.CloseSenseRadius-.25f;motor.ApplyPosition();Require(senses.CanSeePlayer(),"Nearby actor across Z must be sensed");
            reset();enemy.HearNoise(new Vector3(5,100,0),10);step(.01f);
            for(int i=0;i<1000 && enemy.Investigation>=.749f;i++)step(.01f);
            Require(enemy.WatchingPlayer && Mathf.Abs(5-enemy.Position.x-(enemy.CloseSenseRadius-enemy.soundInvestigationInset))<.065f,"Investigation must reach the inset fusion edge before pausing, even beyond 7.5s travel");
            step(.76f);Require(!enemy.WatchingPlayer,"Inspection pause must end in return");
            reset();enemy.WatchForStory();enemy.HearNoise(new Vector3(10,100,0),20);Set(enemy,"detectionProgress",1f);step(.01f);
            Require(enemy.PreparingAttack && enemy.LitTentacleGroups==4,"Complete fibres must use the short preparation");
            step(.47f);Require(enemy.State!=RampagerController.Behaviour.Attack,"Four-fibre preparation must not finish before .5s");
            step(.03f);Require(enemy.State==RampagerController.Behaviour.Attack,"Four-fibre preparation must finish at .5s");
            reset();enemy.HearNoise(new Vector3(0,100,0),10);Require(Mathf.Abs(enemy.SoundEnergy-35)<.001f,"Near pulse must contribute 35");
            step(.01f);Require(enemy.WatchingPlayer,"Sound must begin attention after notice delay");
            step(.19f);enemy.HearNoise(new Vector3(0,100,0),10);step(.2f);enemy.HearNoise(new Vector3(0,100,0),10);
            Require(enemy.LitTentacleGroups==4,"Full sound energy must illuminate all four groups");
            step(.01f);Require(enemy.PreparingAttack && enemy.State!=RampagerController.Behaviour.Attack,"Full energy must start windup rather than immediate attack");
            step(.47f);Require(enemy.State!=RampagerController.Behaviour.Attack,"Full-energy transformation must last .5 seconds");
            step(.03f);Require(enemy.State==RampagerController.Behaviour.Attack,"Full energy must attack after transformation");
            enemy.HearNoise(new Vector3(-4,104,0),20);player.Position=new Vector2(30,100);step(.1f);
            Require(enemy.LastKnownPosition==new Vector2(-4,104),"Attack must retain the sound position rather than follow a silent distant player");
            step(7.41f);Require(enemy.State==RampagerController.Behaviour.Roaming,"No sound for 7.5 seconds must end attack");
            reset();enemy.HearNoise(new Vector3(0,100,0),10);step(.01f);step(.76f);
            Require(!enemy.WatchingPlayer && enemy.State==RampagerController.Behaviour.Roaming,"Absent target must return after .75s at source");
            reset();enemy.TakeWaterHit(new Vector2(10,100),Vector2.left,20,player.Position);step(1.01f);player.Position=new Vector2(-3,104);
            enemy.HearNoise(new Vector3(-3,104,0),20);step(.1f);Require(enemy.Position.x<0,"Airborne sound must be pursued horizontally rather than stopped by height");
            reset();enemy.TakeWaterHit(new Vector2(10,100),Vector2.left,20,player.Position);
            for(int i=0;i<25;i++){enemy.HearNoise(new Vector3(-4,100,0),20);step(1);}
            Require(enemy.State==RampagerController.Behaviour.Attack,"Fresh sound must sustain attack beyond old 20s cap");
            player.Position=enemy.Position+Vector2.right;step(8);
            Require(enemy.State==RampagerController.Behaviour.Attack,"Proximity must retain the live target during silence");
            reset();Set(enemy,"detectionProgress",.75f);Set(enemy,"lastEnergyAt",world.Clock);step(.85f);
            Require(Mathf.Abs(enemy.TentacleGlowEnergy-.5f)<.001f && enemy.LitTentacleGroups==2,"Group glow must fade with sound energy rather than retain a heard-pulse count");
            reset();enemy.HearNoise(new Vector3(10,100,0),10);Require(Mathf.Abs(enemy.SoundEnergy-15)<.001f,"Edge pulse must contribute 15");
            step(.59f);Require(Mathf.Abs(enemy.SoundEnergy-15)<.001f,"Continuous sound grace must prevent decay between footsteps");
            step(.2f);Require(enemy.SoundEnergy==0,"Energy must decay once quiet");
            reset();enemy.initialState=RampagerController.Behaviour.Sleeping;enemy.ResetController();player.Position=new Vector2(1,100);step(.1f);
            Require(enemy.State==RampagerController.Behaviour.Sleeping,"Proximity must not silently wake sleeping Coaler");
            enemy.HearNoise(new Vector3(0,100,0),10);step(.1f);Require(enemy.Waking,"Sound must retain waking animation");
            reset();player.Position=new Vector2(2,100);enemy.TakeWaterHit(new Vector2(2,100),Vector2.left,5,player.Position);
            Require(enemy.PreparingAttack,"Direct water hit must start missing-fibre preparation");
            step(.4f);float beforeHit=enemy.AttackPreparationProgress;
            enemy.TakeWaterHit(new Vector2(2,100),Vector2.left,5,player.Position);
            Require(enemy.AttackPreparationProgress>=beforeHit,"Repeated water hit must not restart preparation");
            step(.61f);Require(enemy.State==RampagerController.Behaviour.Attack && enemy.TentacleGlowEnergy==1,"Direct hit must attack with persistent full red glow after the one-second preparation");
            player.Position=new Vector2(30,100);enemy.HearNoise(new Vector3(enemy.Position.x,enemy.Position.y,0),20);step(.01f);
            Require(enemy.SearchingForPlayer,"An empty last-heard location must begin local search");
            step(.45f);Require(enemy.SearchRoarAmount>.5f,"Lost target search must open the tentacles in a roar");
            for(int i=0;i<30;i++){step(.1f);var d=enemy.transform.position-enemy.SearchCentre;Require(new Vector2(d.x,d.z).magnitude<=2.001f,"Local search escaped its two metre radius");}
            enemy.HearNoise(new Vector3(enemy.Position.x+5,100,0),20);Require(!enemy.SearchingForPlayer,"New sound must interrupt local search and update the target");
            reset();player.Position=new Vector2(2,100);enemy.idealSightRange=100;enemy.fieldOfView=180;motor.Facing=1;motor.SetVisualYaw(-90);
            enemy.ReceiveBuildingWarning(3);Set(player,"shotAt",world.Clock);Set(player,"<ShotSequence>k__BackingField",7);step(.01f);
            Require(enemy.PreparingAttack,"Broadcast shooting must still immediately trigger attack preparation");
            step(1);Require(enemy.State==RampagerController.Behaviour.Attack && enemy.SwarmAttackBoost,"Broadcast attack must retain its boost after preparation");
            reset();var story=root.AddComponent<ServiceStoreOutdoorEncounter>();story.world=world;story.leftMonster=enemy;
            var opening=ScriptableObject.CreateInstance<NpcDialogueSequence>();opening.lines=new[]{new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineOne},new NpcDialogueSequence.Line{speaker="VOICE FROM THE STORE",text=ServiceStoreOutdoorEncounter.OpeningLineTwo}};story.warning=opening;
            enemy.HearNoise(new Vector3(0,100,0),10);step(.01f);
            Require(enemy.WatchingPlayer && !story.ProximityReady,"Sound-only attention outside close range must not trigger the opening");
            var otherGo=new GameObject("Independent second Coaler");otherGo.transform.SetParent(root.transform);otherGo.transform.position=new Vector3(20,100,0);var other=otherGo.AddComponent<RampagerController>();other.Initialize(world);
            player.Position=new Vector2(3,100);step(.01f);Require(story.ProximityReady,"Silent close perception must enable the opening");
            var late=typeof(ServiceStoreOutdoorEncounter).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);late.Invoke(story,null);
            Require(world.ConversationActive && !other.WatchingPlayer,"Proximity must begin dialogue without commanding other Coalers");
            world.AdvanceConversation();world.AdvanceConversation();Require(world.HasItem(story.completedFlag),"Opening completion must be remembered");
            late.Invoke(story,null);Require(!world.ConversationActive,"Completed opening must not repeat");UnityEngine.Object.DestroyImmediate(opening);
            Directory.CreateDirectory("Backups/CoalerAcousticAI/20261023");
            File.WriteAllText("Backups/CoalerAcousticAI/20261023/Verification.txt","PASS: no distant vision; close fusion-range detection; no passive running/missed-shot anger; near/far sound energy; continuous-footstep grace and quiet decay; energy-driven four-group glow, .5s full / 1s partial preparation; bounded local search/roar and sound reacquisition; inset-edge investigation; Z proximity; airborne-sound pursuit; remembered sound rather than live distant player; 7.5s attack expiry; sleep wake; direct-hit preparation does not restart on repeated hits; broadcast violation and boost retained with new preparation; opening requires close perception, leaves other Coalers independent, and completes once.\n");
            Debug.Log("COALER_ACOUSTIC_AI_CHECK_PASS");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
