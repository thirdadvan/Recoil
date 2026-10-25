using UnityEngine;

// Ordinary Coaler coordination. Broadcast obedience retains its existing branch in Simulate.
public sealed partial class RampagerController
{
    [Header("Acoustic perception — ordinary behaviour")]
    [Tooltip("每次真实声波到达时，最近/最远距离获得的能量。广播指令不计入。")]
    public Vector2 soundEnergyNearFar=new Vector2(35,15);
    [Min(0),Tooltip("连续脚步之间不衰减；最后一声过此时间后才按 Anger Decay Time 下降。")]
    public float soundQuietDelay=.6f;
    [Min(.01f),Tooltip("四组外延触须全部亮起时的原地蓄力时间。")]
    public float fullTentacleEnrageDuration=.5f;
    [Min(.01f),Tooltip("发光组未满时，全部点亮再蓄力的总时间。")]
    public float growingTentacleEnrageDuration=1;
    [Min(0),Tooltip("声音检查在衰减半径减去此距离的边缘停下。")]
    public float soundInvestigationInset=.5f;
    [Min(.01f)] public float soundSearchPause=.75f;
    [Min(.01f)] public float soundMemoryDuration=7.5f;
    float heardRadius,lastSoundAt=-100,lastEnergyAt=-100,attackPreparationRemaining;
    float soundPulseAt=-100,soundPulseStrength;
    int preparationStartCount;float preparationStartEnergy;float attackPreparationDuration,soundBlockedTime;
    RampagerGrab closeSense;
    Vector3 cachedSound,cachedDestination;bool hasSoundDestination;
    public bool PreparingAttack {get{return attackPreparationRemaining>0;}}
    public float AttackPreparationProgress {get{return PreparingAttack?Mathf.Clamp01(1-attackPreparationRemaining/Mathf.Max(.01f,attackPreparationDuration)):0;}}
    public int LitTentacleGroups {get{return Mathf.Clamp(Mathf.FloorToInt(TentacleGlowEnergy*4+.00001f),0,4);}}
    public float TentacleGlowEnergy
    {get{return State==Behaviour.Attack?1:PreparingAttack?Mathf.Lerp(preparationStartEnergy,1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.55f,AttackPreparationProgress))):Mathf.Clamp01(detectionProgress);}}
    public float AttackChargeProgress
    {get{return PreparingAttack?(preparationStartCount>=4?AttackPreparationProgress:Mathf.InverseLerp(.55f,1,AttackPreparationProgress)):State==Behaviour.Attack?1:0;}}
    void RequestAttack()
    {if(State!=Behaviour.Attack && !PreparingAttack)StartAcousticAttackPreparation();}
    void AdvanceAttackPreparation(float dt)
    {
        attackPreparationRemaining=Mathf.Max(0,attackPreparationRemaining-dt);
        TrackPosition(LastKnownWorldPosition);
        Motor.SimulateVertical(dt);if(depthArrival)depthArrival.Stop();
        if(attackPreparationRemaining<=.000001f)EnterAttack();
    }
    void StartAcousticAttackPreparation()
    {
        float startingEnergy=detectionProgress;
        if(State==Behaviour.Sleeping)SetState(Behaviour.Roaming);
        waking=noticing=checking=false;wakeRemaining=noticeRemaining=0;watching=true;CancelReturn();
        if(patrolRoute)patrolRoute.Suspend();
        preparationStartEnergy=Mathf.Clamp01(startingEnergy);
        preparationStartCount=Mathf.Clamp(Mathf.FloorToInt(preparationStartEnergy*4+.00001f),0,4);
        attackPreparationDuration=Mathf.Max(.01f,preparationStartCount>=4?fullTentacleEnrageDuration:growingTentacleEnrageDuration);
        attackPreparationRemaining=attackPreparationDuration;Attack.CancelAttack();
    }
    public float SoundEnergy {get{return detectionProgress*100;}}
    public float SoundResponse
    {get{return !world?0:soundPulseStrength*Mathf.Sin(Mathf.Clamp01((world.Clock-soundPulseAt)/.4f)*Mathf.PI);}}
    public float CloseSenseRadius
    {get{if(!closeSense)closeSense=GetComponent<RampagerGrab>();return closeSense && closeSense.isActiveAndEnabled?closeSense.reach:contactRadius;}}
    void ResetAcoustics()
    {lastSoundAt=lastEnergyAt=soundPulseAt=-100;attackPreparationRemaining=soundPulseStrength=0;heardRadius=1;hasSoundDestination=false;preparationStartEnergy=0;preparationStartCount=0;attackPreparationDuration=1;soundBlockedTime=0;StopAttackSearch();}
    void RememberSound(Vector2 source,float depth)
    {
        lastHeardPosition=target=source;lastHeardDepth=targetDepth=depth;lastSoundAt=world.Clock;
        inspectionRemaining=soundSearchPause;soundBlockedTime=0;StopAttackSearch();
    }
    void ReceiveAcousticNoise(Vector2 source,float depth,float radius)
    {
        RememberSound(source,depth);
        float contribution=Mathf.Lerp(soundEnergyNearFar.x,soundEnergyNearFar.y,
            Mathf.Clamp01(DistanceToSound(new Vector3(source.x,source.y,depth))/Mathf.Max(.01f,radius)));
        soundPulseAt=world.Clock;soundPulseStrength=Mathf.Clamp01(contribution/Mathf.Max(1,soundEnergyNearFar.x));
        if(State==Behaviour.Attack)
        {
            // A committed strike keeps its target until impact. New sound only changes the next destination.
            ReportSight(true);return;
        }
        lastEnergyAt=world.Clock;
        if(!PreparingAttack)detectionProgress=Mathf.Clamp01(detectionProgress+contribution*.01f);
        if(!WatchingPlayer && !PreparingAttack)BeginNotice(source,false,depth);
    }
    void ReceiveAcousticReport(Report report)
    {
        // Calls pass a remembered location, with no energy and no propagation feedback loop.
        RememberSound(report.position,report.depth);
        if(State!=Behaviour.Attack && !WatchingPlayer && !PreparingAttack)BeginNotice(report.position,true,report.depth);
    }
    void BeginAcousticWatch()
    {
        checking=noticing=waking=false;wakeRemaining=noticeRemaining=0;
        watching=true;watchElapsed=0;inspectionRemaining=soundSearchPause;
        disengaging=false;disengageRemaining=0;CancelReturn();
        if(patrolRoute)patrolRoute.Suspend();TrackPosition(LastKnownWorldPosition);
    }
    void BeginCloseWatch()
    {
        if(!watching && !noticing && !returning && !waitingToSleep)
        {returnPosition=Position;returnDepth=Motor.MovementPlaneZ;}
        attentionFromSleeping=false;BeginAcousticWatch();
    }
    // Airborne sources are investigated on a supporting surface beneath the *remembered* sound.
    // Never use the player's current Flying flag or current X/Z to choose this destination.
    Vector3 AcousticDestination()
    {
        Vector3 destination=LastKnownWorldPosition;
        if(hasSoundDestination && cachedSound==destination)return cachedDestination;
        cachedSound=destination;hasSoundDestination=true;
        float ceiling=target.y-.3f,best=float.NegativeInfinity;
        foreach(var r in world.SolidRects)
            if(target.x>=r.xMin && target.x<=r.xMax && r.yMax<=ceiling && r.yMax>best && SoundSurfaceExists(r.yMax))best=r.yMax;
        foreach(var roof in world.SoftRoofs)
        {
            if(!roof || !roof.Available)continue;var r=roof.Area;
            if(target.x>=r.xMin && target.x<=r.xMax && r.yMax<=ceiling && r.yMax>best && SoundSurfaceExists(r.yMax))best=r.yMax;
        }
        destination.y=float.IsNegativeInfinity(best)?Position.y:best+.8f;
        cachedDestination=destination;return destination;
    }
    bool SoundSurfaceExists(float y)
    {
        if(!depthArrival || !depthArrival.navigation || !depthArrival.navigation.Ready)return true;
        Vector3 surface;return depthArrival.navigation.Project(new Vector3(target.x,y,targetDepth),out surface);
    }
    void SimulateAcoustics(float dt)
    {
        // Sleeping only wakes to sound or a direct hit. Nearby actors do not wake it silently.
        if(State==Behaviour.Sleeping)
        {
            if(waking){wakeRemaining=Mathf.Max(0,wakeRemaining-dt);if(wakeRemaining<=.000001f)CompleteNotice();}
            if(State==Behaviour.Sleeping){Motor.SimulateVertical(dt);if(depthArrival)depthArrival.Stop();return;}
        }
        if(noticing)
        {noticeRemaining=Mathf.Max(0,noticeRemaining-dt);if(noticeRemaining<=.000001f)CompleteNotice();}
        Senses.Sense();bool close=Senses.LastObservation.sees;
        if(close)StopAttackSearch();
        if(close)
        {
            target=Senses.TargetPosition;targetDepth=Senses.TargetWorldPosition.z;lastSight=world.Clock;
            if(State!=Behaviour.Attack && !watching && !noticing)BeginCloseWatch();
        }
        if(State!=Behaviour.Attack && !PreparingAttack)
        {
            float quietNow=Mathf.Max(0,world.Clock-lastEnergyAt-soundQuietDelay);
            float quietBefore=Mathf.Max(0,world.Clock-dt-lastEnergyAt-soundQuietDelay);
            detectionProgress=Mathf.Max(0,detectionProgress-(quietNow-quietBefore)/Mathf.Max(.01f,angerDecayTime));
            if(detectionProgress>=1-.000001f && !noticing && !waking)
            {StartAcousticAttackPreparation();}
        }
        if(PreparingAttack){AdvanceAttackPreparation(dt);return;}
        if(State==Behaviour.Attack)
        {
            chaseElapsed+=dt;
            // Proximity is the only live-position exception; otherwise silence expires the sound memory.
            if(!close && world.Clock-lastSoundAt>=soundMemoryDuration)BeginReturn(attentionFromSleeping);
        }
        Vector3 destination=close?LastKnownWorldPosition:AcousticDestination();float budget=0,stopDistance=0;bool navigate=false;
        if(State==Behaviour.Attack || WatchingPlayer)
        {
            TrackPosition(LastKnownWorldPosition);
            if(noticing){if(depthArrival)depthArrival.Stop();Motor.SimulateVertical(dt);return;}
            float gap=new Vector2(destination.x-Position.x,destination.z-Motor.MovementPlaneZ).magnitude;
            stopDistance=State!=Behaviour.Attack?Mathf.Max(.22f,CloseSenseRadius-soundInvestigationInset):.22f;
            if(gap>stopDistance+.04f)
            {budget=Mathf.Min(gap-stopDistance,CurrentMoveSpeed*dt);navigate=true;inspectionRemaining=soundSearchPause;}
            else if(State!=Behaviour.Attack && !close)
            {inspectionRemaining=Mathf.Max(0,inspectionRemaining-dt);if(inspectionRemaining<=0)BeginReturn(attentionFromSleeping || returnToSleepAfterSound);}
        }
        if(State==Behaviour.Attack && !close)
            UpdateAttackSearch(dt,ref destination,ref budget,ref navigate,ref stopDistance);
        if(State==Behaviour.Roaming && !watching && !noticing)
        {
            if(returning || waitingToSleep)
            {
                destination=new Vector3(returnPosition.x,returnPosition.y,returnDepth);
                float gap=new Vector2(destination.x-Position.x,destination.z-Motor.MovementPlaneZ).magnitude;
                if(gap>.12f){returning=true;waitingToSleep=false;sleepRemaining=0;budget=Mathf.Min(gap,CurrentMoveSpeed*dt);navigate=true;}
                else if(returnAsSleeping)
                {
                    returning=false;
                    if(!waitingToSleep){waitingToSleep=true;sleepRemaining=sleepDelay;}else sleepRemaining=Mathf.Max(0,sleepRemaining-dt);
                    if(sleepRemaining<=0)SetState(Behaviour.Sleeping);
                }
                else{CancelReturn();trackedPitch=roamingSightPitch;}
            }
            else
            {
                float direction,distance,look;
                if(patrolRoute && patrolRoute.Evaluate(transform.position,stun>0?0:dt,Facing,CurrentMoveSpeed,out direction,out distance,out look))
                {if(patrolRoute.RequestsFacing)Motor.Facing=look;if(distance>0){navigate=true;destination=patrolRoute.CurrentPoint.Destination;budget=distance;}}
            }
        }
        Attack.Simulate(dt,State==Behaviour.Attack && stun<=0);
        Vector3 beforeSearchMove=transform.position;
        bool blocked=false;
        if(navigate && budget>0 && !Attack.MovementLocked && stun<=0)
        {
            if(depthArrival && !Motor.Airborne)blocked=!depthArrival.MoveTo(destination,budget,stopDistance,dt,WatchingPlayer || State==Behaviour.Attack,true);
            else{float direction=Mathf.Abs(destination.x-Position.x)>.0001f?Mathf.Sign(destination.x-Position.x):0;if(direction!=0)blocked=!Motor.Move(direction,budget,false);}
        }
        else if(depthArrival)depthArrival.Stop();
        ConstrainAttackSearchStep(beforeSearchMove);
        if(WatchingPlayer){soundBlockedTime=blocked?soundBlockedTime+dt:0;if(soundBlockedTime>=soundMemoryDuration)BeginReturn(attentionFromSleeping || returnToSleepAfterSound);}
        if(SwarmAttackBoost && !Attack.MovementLocked && stun<=0)Motor.TryJump(target,blocked);
        Motor.SimulateVertical(dt);
        if(!Attack.MovementLocked)Attack.TryAttack(State==Behaviour.Attack && stun<=0);
    }
}
