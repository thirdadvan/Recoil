using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

// Opt-in regression checks. Never runs during ordinary Play.
public sealed partial class RecoilPrototype
{
    IEnumerator Start()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-recoil-verify")<0)yield break;
        yield return null;int result=0;
        try{VerifyMechanics();Debug.Log("RECOIL_ATTENTION_VERIFY_PASSED");}
        catch(Exception error){Debug.LogException(error);result=1;}
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(result);
#else
        Application.Quit(result);
#endif
    }
    static void Require(bool value,string message){if(!value)throw new Exception("Rampager verification: "+message);}
    static void VerifyField(object instance,string name,object value)
    {instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);}
    void EnemySteps(RampagerController enemy,int count,float dt=.01f)
    {for(int i=0;i<count;i++){simTime+=dt;enemy.Simulate(dt);}}
    // Run on a disposable, unobstructed test world: it changes positions and settings deliberately.
    public void VerifyMechanics()
    {
        Require(enemies.Count>=2 && player && SoundWaves,"test fixture: two enemies, player, sound manager");
        var enemy=enemies[0];var motor=enemy.GetComponent<RampagerMovement>();var perception=enemy.GetComponent<RampagerPerception>();
        enemy.GetComponent<RampagerAttack>().enabled=false;enemies[1].GetComponent<RampagerAttack>().enabled=false;
        Require(Mathf.Abs(enemy.idealSightRange-11.946668f)<.001f,"base sight default increased by forty percent");
        Require(Mathf.Abs(player.Noise.footstepRadius-4.16f)<.001f,"shared footstep and flight noise radius increased by thirty percent");
        enemy.spawnFacing=RampagerController.SpawnDirection.Right;enemy.idealSightRange=100;enemy.fieldOfView=180;
        Require(enemy.roamingNoticeDelay==new Vector2(.75f,.75f) && enemy.sleepingNoticeDelay==new Vector2(1.75f,1.75f),"fixed .75 roaming and 1.75 waking delays");
        enemy.returnToSleepAfterSound=false;enemy.sightPitchOffset=0;
        Action<RampagerController.Behaviour> reset=state=>{
            ActiveInteraction=null;enemy.initialState=state;enemy.ResetController();player.ResetController(new Vector2(2,100));
            motor.Position=new Vector2(0,100);simTime=100;
        };
        Action<float> intent=value=>VerifyField(player,"<HorizontalIntent>k__BackingField",value);
        reset(RampagerController.Behaviour.Roaming);
        Require(Mathf.Abs(enemy.CurrentMoveSpeed-enemy.patrolSpeed*.5f)<.001f,"normal patrol half speed");
        EnemySteps(enemy,20);Require(!enemy.WatchingPlayer,"sight alone does not initiate attention");
        enemy.HearNoise(player.Position,10);EnemySteps(enemy,74);
        Require(enemy.Noticing && !enemy.WatchingPlayer,"roaming notice delay");
        EnemySteps(enemy,2);Require(enemy.WatchingPlayer && enemy.State==RampagerController.Behaviour.Roaming,"sound leads to watch, not attack");
        VerifyField(player,"flying",true);EnemySteps(enemy,30);Require(enemy.State==RampagerController.Behaviour.Roaming && enemy.DetectionProgress==0,"normal flight is legal");
        VerifyField(player,"flying",false);VerifyField(player,"grounded",true);intent(1);EnemySteps(enemy,30);
        Require(enemy.DetectionProgress>.58f && enemy.DetectionProgress<.62f,"running anger at .5 seconds");
        intent(0);EnemySteps(enemy,20);Require(enemy.DetectionProgress>.38f && enemy.DetectionProgress<.42f,"anger decays at one full bar per second");
        VerifyField(player,"silentWalking",true);intent(1);EnemySteps(enemy,45);Require(enemy.DetectionProgress==0,"stealth remains safe");
        VerifyField(player,"silentWalking",false);EnemySteps(enemy,49);Require(enemy.State==RampagerController.Behaviour.Roaming,"running threshold not premature");
        EnemySteps(enemy,2);Require(enemy.State==RampagerController.Behaviour.Attack,"running reaches attack threshold");
        Require(Mathf.Abs(enemy.CurrentMoveSpeed-enemy.chaseSpeed*1.5f)<.001f,"base chase unchanged");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        VerifyField(player,"shotAt",simTime);VerifyField(player,"<ShotSequence>k__BackingField",1);EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"shooting while watched attacks immediately");
        reset(RampagerController.Behaviour.Sleeping);EnemySteps(enemy,30);Require(!enemy.Noticing,"sight cannot wake sleeping");
        enemy.HearNoise(player.Position,10);EnemySteps(enemy,174);Require(enemy.Waking && enemy.State==RampagerController.Behaviour.Sleeping,"sleep wake delay");
        EnemySteps(enemy,2);Require(enemy.WatchingPlayer && enemy.State==RampagerController.Behaviour.Roaming,"wake checks sight without immediate attack");
        reset(RampagerController.Behaviour.Roaming);player.Position=motor.Position;EnemySteps(enemy,1);Require(enemy.Noticing,"brushing initiates attention");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        player.Position=new Vector2(1000,100);EnemySteps(enemy,199);Require(enemy.WatchingPlayer,"two second lost sight not premature");
        EnemySteps(enemy,2);Require(enemy.ReturningHome || !enemy.WatchingPlayer,"lost sight returns");
        reset(RampagerController.Behaviour.Roaming);float baseRange=enemy.SightRange;enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        Require(Mathf.Abs(enemy.SightRange-baseRange*1.4f)<.001f,"watch range uses restored 1.4 multiplier");
        EnemySteps(enemy,2502);Require(enemy.WatchingPlayer && !enemy.DisengagingWatch,"visible target never times out of watch");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        player.Position=new Vector2(1000,100);EnemySteps(enemy,100);Require(enemy.DisengagingWatch,"distance loss begins disengagement");
        Require(Mathf.Abs(enemy.SightRange-baseRange*1.4f)<.001f,"disengagement retains enhanced range");
        player.Position=new Vector2(2,100);EnemySteps(enemy,1);
        Require(enemy.WatchingPlayer && !enemy.DisengagingWatch,"reacquisition cancels disengagement");
        EnemySteps(enemy,2002);Require(enemy.WatchingPlayer && !enemy.DisengagingWatch,"reacquired visible player remains watched indefinitely");
        reset(RampagerController.Behaviour.Roaming);
        var report=new RampagerController.Report{source=enemies[1],target=player,position=player.Position,sightTime=simTime,attack=true,id=9001};
        Require(enemy.ReceiveReport(report),"accept location report");EnemySteps(enemy,29);Require(enemy.Noticing,"report delay .3 seconds");
        EnemySteps(enemy,2);Require(enemy.WatchingPlayer && enemy.State!=RampagerController.Behaviour.Attack,"report requires own confirmation");
        Require(!enemy.ReceiveReport(report),"duplicate reports ignored");VerifyField(player,"grounded",true);intent(1);EnemySteps(enemy,24);
        Require(enemy.State==RampagerController.Behaviour.Roaming,"double anger .25 threshold not premature");EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"reported target anger doubles");
        reset(RampagerController.Behaviour.Roaming);report.id++;report.sightTime=simTime;player.Position=new Vector2(1000,100);enemy.ReceiveReport(report);EnemySteps(enemy,40);
        Require(!enemy.WatchingPlayer && enemy.State!=RampagerController.Behaviour.Attack,"report cannot see through distance or walls");
        reset(RampagerController.Behaviour.Roaming);enemy.ReceiveBuildingWarning(3);
        Require(Mathf.Abs(enemy.CurrentMoveSpeed-enemy.patrolSpeed)<.001f,"broadcast movement retains previous speed");
        intent(1);EnemySteps(enemy,49);Require(enemy.State!=RampagerController.Behaviour.Attack,"hold violation .5 tolerance");EnemySteps(enemy,2);
        Require(enemy.State==RampagerController.Behaviour.Attack && enemy.SwarmAttackBoost,"hold violation attacks after .5 seconds");
        Require(Mathf.Abs(enemy.CurrentMoveSpeed-enemy.chaseSpeed*2.1f)<.001f,"broadcast multiplier exactly once");
        enemy.EndSwarm(.01f);EnemySteps(enemy,2);Require(!enemy.WaveActive && enemy.SwarmAttackBoost && enemy.RedEyes,"broadcast attack retains boost and red eyes");
        reset(RampagerController.Behaviour.Roaming);enemy.ReceiveBuildingWave(1,1,3,1);intent(-1);EnemySteps(enemy,59);
        Require(enemy.State!=RampagerController.Behaviour.Attack,"hold timer reset before moving reverse timer");EnemySteps(enemy,2);
        Require(enemy.State==RampagerController.Behaviour.Attack,"moving reverse .5 seconds attacks");
        reset(RampagerController.Behaviour.Roaming);enemy.ReceiveBuildingWave(1,1,3,1);EnemySteps(enemy,61);
        Require(enemy.State==RampagerController.Behaviour.Attack,"moving still .5 seconds attacks");
        reset(RampagerController.Behaviour.Sleeping);enemy.ReceiveBuildingWarning(3);enemy.EndSwarm(.01f);EnemySteps(enemy,2);
        Require(enemy.State==RampagerController.Behaviour.Sleeping,"original sleeping enemy returns asleep after wave");
        reset(RampagerController.Behaviour.Sleeping);enemy.ReceiveBuildingWarning(3);VerifyField(player,"flying",true);EnemySteps(enemy,1);enemy.EndSwarm(.01f);EnemySteps(enemy,2);
        Require(enemy.WatchingPlayer,"wave-forced attack exit also returns to watching rather than sleeping");
        reset(RampagerController.Behaviour.Roaming);VerifyField(player,"shotAt",simTime);VerifyField(player,"<ShotSequence>k__BackingField",1);EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"visible unwatched shooting immediately attacks");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,20);
        VerifyField(player,"shotAt",simTime);VerifyField(player,"<ShotSequence>k__BackingField",1);EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"notice delay does not block visible shooting detection");
        reset(RampagerController.Behaviour.Roaming);player.NotifyForbiddenInteraction();EnemySteps(enemy,1);
        Require(enemy.Noticing && enemy.State==RampagerController.Behaviour.Roaming,"unwatched instant interaction initiates notice rather than attack");
        EnemySteps(enemy,74);Require(!enemy.WatchingPlayer,"interaction notice delay is not premature");EnemySteps(enemy,2);
        Require(enemy.WatchingPlayer && enemy.State==RampagerController.Behaviour.Roaming,"interaction causes watch at .75 seconds");
        player.NotifyForbiddenInteraction();EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"interaction while watched immediately attacks");
        reset(RampagerController.Behaviour.Roaming);var heldGO=new GameObject("Test held interaction");heldGO.transform.SetParent(transform);ActiveInteraction=heldGO.AddComponent<ContinuousInteractionItem>();EnemySteps(enemy,1);
        Require(enemy.Noticing && enemy.State==RampagerController.Behaviour.Roaming,"held interaction initiates notice");ActiveInteraction.Advance(this,.76f);EnemySteps(enemy,76);
        Require(enemy.State==RampagerController.Behaviour.Attack,"continuing interaction after becoming watched attacks");ActiveInteraction=null;
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,60);
        var ongoing=heldGO.GetComponent<ContinuousInteractionItem>();ongoing.Cancel();ongoing.Begin(2);ActiveInteraction=ongoing;EnemySteps(enemy,1);ongoing.Advance(this,.14f);EnemySteps(enemy,15);
        Require(enemy.WatchingPlayer && enemy.State==RampagerController.Behaviour.Roaming,"pre-existing interaction below .5 seconds does not instantly attack on gaze");
        ongoing.Advance(this,.35f);EnemySteps(enemy,1);Require(enemy.WatchingPlayer,"pre-existing .49 second interaction is safe");
        ongoing.Advance(this,.01f);EnemySteps(enemy,1);Require(enemy.WatchingPlayer,"exact .5 second boundary is not exceeded");
        ongoing.Advance(this,.011f);EnemySteps(enemy,1);Require(enemy.State==RampagerController.Behaviour.Attack,"pre-existing interaction exceeding .5 seconds attacks");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,60);
        ongoing.Cancel();ongoing.Begin(3);ActiveInteraction=ongoing;EnemySteps(enemy,1);ongoing.Advance(this,.14f);EnemySteps(enemy,15);
        ActiveInteraction=null;EnemySteps(enemy,70);Require(enemy.WatchingPlayer,"cancelled interaction does not leave a ticking attack timer");
        ongoing.Cancel();ongoing.Begin(4);ActiveInteraction=ongoing;EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack && ongoing.Progress==0,"new held interaction under gaze attacks at its start");ActiveInteraction=null;
        reset(RampagerController.Behaviour.Roaming);player.Position=new Vector2(-2,100);enemy.HearNoise(player.Position,10);EnemySteps(enemy,1);
        Require(enemy.Noticing && enemy.Facing<0,"sound attention faces player before gaze begins");
        player.Position=new Vector2(2,100);EnemySteps(enemy,1);Require(enemy.Noticing && enemy.Facing>0,"sound attention follows player crossing to other side");
        player.Position=new Vector2(-2,100);EnemySteps(enemy,1);Require(enemy.Facing<0,"attention continuously updates facing");
        reset(RampagerController.Behaviour.Roaming);player.Position=motor.Position;EnemySteps(enemy,1);
        player.Position=new Vector2(-2,100);EnemySteps(enemy,1);Require(enemy.Noticing && enemy.Facing<0,"brush attention follows player to left");
        player.Position=new Vector2(2,100);EnemySteps(enemy,1);Require(enemy.Noticing && enemy.Facing>0,"brush attention follows player to right");
        reset(RampagerController.Behaviour.Roaming);player.Position=new Vector2(1000,100);enemy.HearNoise(new Vector2(2,100),10);EnemySteps(enemy,76);
        player.Position=new Vector2(-2,100);EnemySteps(enemy,1);Require(enemy.Facing<0 && enemy.WatchingPlayer,"ordinary sound inspection keeps facing player");
        reset(RampagerController.Behaviour.Roaming);report.id++;report.sightTime=simTime;report.position=new Vector2(-2,100);enemy.ReceiveReport(report);EnemySteps(enemy,20);
        Require(enemy.Noticing && enemy.Facing<0,"report recipients keep reported direction instead of tracking hidden live player");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        var freightGO=new GameObject("Test instant freight interaction");freightGO.transform.SetParent(transform);
        var machine=freightGO.AddComponent<KBKStackerCrane>();
        var machineParts=new Transform[4];for(int i=0;i<4;i++){machineParts[i]=new GameObject("Freight part "+i).transform;machineParts[i].SetParent(freightGO.transform);}
        machine.startPoint=machineParts[0];machine.destinationPoint=machineParts[1];machine.travelRig=machineParts[2];machine.liftCarriage=machineParts[3];machine.liftCarriage.SetParent(machine.travelRig);
        var freightSwitch=freightGO.AddComponent<KBKCraneSwitch>();freightSwitch.crane=machine;freightSwitch.interactionDuration=0;freightSwitch.transform.position=player.Position;
        var previousItems=interactionItems;interactionItems=new ContinuousInteractionItem[]{freightSwitch};interactionHeld=false;
        int beforeSequence=player.InteractionSequence;UpdateContinuousInteraction(true,.01f);
        Require(machine.Busy && ActiveInteraction==null,"actual crane switch completes within its first interaction frame");
        Require(player.InteractionSequence==beforeSequence+1,"instant switch records start before clearing active interaction");EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"actual instant crane switch causes attack while watched");
        UpdateContinuousInteraction(true,.01f);Require(player.InteractionSequence==beforeSequence+1,"holding E does not repeatedly emit interaction start");
        UpdateContinuousInteraction(false,.01f);reset(RampagerController.Behaviour.Roaming);freightSwitch.transform.position=player.Position;
        machine.ResetMachine();interactionHeld=false;UpdateContinuousInteraction(true,.01f);EnemySteps(enemy,1);
        Require(enemy.Noticing && enemy.State==RampagerController.Behaviour.Roaming,"unwatched instant crane interaction causes notice");
        EnemySteps(enemy,76);Require(enemy.WatchingPlayer,"instant crane interaction still reaches normal watch after delay");
        UpdateContinuousInteraction(false,.01f);interactionItems=previousItems;
        reset(RampagerController.Behaviour.Roaming);var ladderGO=new GameObject("Test visible ladder");ladderGO.transform.SetParent(transform);ladderGO.transform.position=new Vector2(2,101);
        var ladder=ladderGO.AddComponent<ClimbableLadder>();var ladderBox=ladder.GetComponent<BoxCollider2D>();ladderBox.isTrigger=true;ladderBox.size=new Vector2(1,6);
        var previousLadders=ladders;ladders=new[]{ladder};Physics2D.SyncTransforms();player.Simulate(0,1,false,false,false,.01f);
        Require(player.OnLadder,"player actually attaches using ladder controls");EnemySteps(enemy,1);
        Require(enemy.Noticing && enemy.State==RampagerController.Behaviour.Roaming,"visible ladder climbing initiates roaming attention");
        EnemySteps(enemy,74);Require(!enemy.WatchingPlayer,"ladder attention respects .75 reaction delay");EnemySteps(enemy,2);
        Require(enemy.WatchingPlayer,"visible ladder climbing leads to watching");EnemySteps(enemy,100);
        Require(enemy.State==RampagerController.Behaviour.Roaming && enemy.DetectionProgress==0,"ladder climbing alone is not an attack violation");
        reset(RampagerController.Behaviour.Roaming);ladderGO.transform.position=new Vector2(1000,101);player.Position=new Vector2(1000,100);Physics2D.SyncTransforms();player.Simulate(0,1,false,false,false,.01f);EnemySteps(enemy,10);
        Require(player.OnLadder && !enemy.Noticing && !enemy.WatchingPlayer,"out-of-sight ladder climbing does not attract attention");
        reset(RampagerController.Behaviour.Sleeping);ladderGO.transform.position=new Vector2(2,101);Physics2D.SyncTransforms();player.Simulate(0,1,false,false,false,.01f);EnemySteps(enemy,100);
        Require(enemy.State==RampagerController.Behaviour.Sleeping && !enemy.Waking,"visual ladder climbing does not wake a sleeping monster");ladders=previousLadders;
        reset(RampagerController.Behaviour.Roaming);enemy.ReceiveBuildingWarning(3);VerifyField(player,"flying",true);EnemySteps(enemy,1);
        Require(enemy.State==RampagerController.Behaviour.Attack,"broadcast flight violation preserved");
        enemy.EndSwarm(.01f);EnemySteps(enemy,2);
        player.Position=new Vector2(2,105);Vector2 held=motor.Position;EnemySteps(enemy,5);
        Require(enemy.WatchingHighFlyer && Vector2.Distance(motor.Position,held)<.001f,"high flyer stop does not fall through to patrol");
        reset(RampagerController.Behaviour.Attack);player.Position=new Vector2(2,105);VerifyField(player,"flying",true);EnemySteps(enemy,999);
        Require(enemy.State==RampagerController.Behaviour.Attack,"quiet unreachable ten second cap not premature");EnemySteps(enemy,2);
        Require(enemy.WatchingPlayer,"quiet unreachable exits attack into watch at ten seconds");
        reset(RampagerController.Behaviour.Attack);VerifyField(player,"grounded",true);intent(1);
        // Keep moving target out of reach, so only the total chase limit can end this chase.
        for(int i=0;i<2001;i++){player.Position=new Vector2(motor.Position.x+5,100);EnemySteps(enemy,1);}
        Require(enemy.WatchingPlayer,"total chase ends into watch after twenty seconds");
        enemy.ResetController();Require(!enemy.Noticing && !enemy.WatchingPlayer && enemy.DetectionProgress==0,"reset clears attention");
        Require(Mathf.Abs(player.walkSpeed-2.52f)<.001f && Mathf.Abs(player.sneakSpeed-.99f)<.001f,"updated player speed defaults");
        reset(RampagerController.Behaviour.Roaming);enemy.HearNoise(player.Position,10);EnemySteps(enemy,76);
        bool wasStarted=started;started=true;
        var feedback=player.GetComponent<PlayerGazeFeedback>();if(!feedback)feedback=player.gameObject.AddComponent<PlayerGazeFeedback>();feedback.Initialize(this);
        feedback.Simulate(.01f);Require(feedback.WatcherCount==1 && feedback.AttackWatcherCount==0 && feedback.CurrentBeatRate==1,"normal gaze heartbeat rhythm");
        var heart=player.transform.Find("Heart - gaze feedback").GetComponent<SpriteRenderer>();
        Require(heart.color.r>heart.color.b,"normal gaze red heart");
        player.Position=new Vector2(1000,100);EnemySteps(enemy,1);feedback.Simulate(1);
        Require(heart.enabled && feedback.Visibility>.49f && feedback.Visibility<.51f,"heart fades instead of disappearing immediately");
        feedback.Simulate(1.01f);Require(!heart.enabled,"heart fully fades after two seconds");
        player.Position=new Vector2(2,100);EnemySteps(enemy,1);VerifyField(player,"shotAt",simTime);VerifyField(player,"<ShotSequence>k__BackingField",1);EnemySteps(enemy,1);
        feedback.Simulate(.01f);Require(feedback.AttackWatcherCount==1 && feedback.CurrentBeatRate==3 && heart.color.r>heart.color.b,"attack gaze uses fast red heart");
        VerifyField(player,"shotAt",-100f);player.Position=new Vector2(2,105);VerifyField(player,"flying",true);EnemySteps(enemy,1002);feedback.Simulate(.01f);
        Require(enemy.WatchingPlayer && feedback.AttackWatcherCount==0 && feedback.CurrentBeatRate==1 && heart.color.r>heart.color.b,"attack exit restores normal red watching feedback");
        feedback.ResetFeedback();started=wasStarted;
        Debug.Log("RECOIL_ATTENTION_VERIFY: 92 behaviour assertions passed");
    }
}
