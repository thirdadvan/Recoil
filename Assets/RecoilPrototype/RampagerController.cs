using System.Collections.Generic;
using UnityEngine;

// State coordination; sensing, collision locomotion, patrol actions and attack execution stay separate.
[DisallowMultipleComponent,RequireComponent(typeof(RampagerPerception),typeof(RampagerMovement))]
[RequireComponent(typeof(RampagerAttack),typeof(RampagerVisualEffects))]
public sealed partial class RampagerController : MonoBehaviour
{
    public enum Behaviour { Roaming=0, Attack=2, Sleeping=4 }
    public enum ReportEvent { Shooting, Flying, ForbiddenInteraction, ConfirmedTarget }
    // 'attack' describes the sender. A recipient receives a location, never permission to attack.
    public struct Report {public RampagerController source;public RecoilPlayerController target;public EscortChild childTarget;public Vector2 position;public float sightTime;public float depth;public bool attack;public int id;public ReportEvent eventType;}
    [SerializeField] RecoilPrototype world;
    [Header("Initial state and return")]
    public Behaviour initialState=Behaviour.Roaming;
    public Transform sleepPoint;
    [Tooltip("没有看见目标的声音检查结束后，是否返回休眠点。")]
    public bool returnToSleepAfterSound=true;
    [Min(.01f)] public float soundInspectionDuration=1.5f,sleepDelay=10;
    [Header("Attention response")]
    public Vector2 roamingNoticeDelay=new Vector2(.75f,.75f);
    public Vector2 sleepingNoticeDelay=new Vector2(1.75f,1.75f);
    [Min(.01f)] public float reportNoticeDelay=.3f;
    [Min(.01f),Tooltip("失去目标后，仍保留增强视野的脱离过渡时间。")]
    public float watchLostSightTime=2;
    [Min(1)] public float watchSightMultiplier=1.4f;
    [Range(.05f,2),Tooltip("漫游与 Approach 共用的移动倍率；广播阶段使用独立的原有速度。") ] public float roamingSpeedMultiplier=.6f;
    [Min(.1f),Tooltip("注视时缓慢接近，但停在此水平距离外。")]
    public float watchStandOff=1.2f;
    // Serialized compatibility for earlier scene tuning tools; ordinary anger is now acoustic.
    [Header("Legacy tuning (unused by ordinary acoustic AI)")]
    [Min(.01f),Tooltip("普通地面跑动，从零怒气到攻击所需秒数。")]
    [HideInInspector] public float runningAngerTime=1.5f;
    [Min(0),Tooltip("进入注视前已开始的互动，总互动时长超过此秒数时攻击。注视后新开始的互动立即攻击。")]
    [HideInInspector] public float continuingInteractionAttackTime=.5f;
    [Min(.01f)] public float angerDecayTime=1;
    [HideInInspector] public float reportedAngerMultiplier=2;
    [Header("Visual detection")]
    [Range(-90,90)] public float sightPitchOffset=-10,roamingSightPitch=-25,waveSightPitch=15;
    [Header("Reports and chase exit")]
    public float reportRadius=10,reportFreshness=3;
    [Min(.01f)] public float safeUnreachableTime=10,maximumChaseTime=20;
    [Header("Attack movement")]
    [Min(1)] public float attackSpeedMultiplier=1.5f;
    [HideInInspector] public float highFlightStopBodies=1.5f; // Legacy scene compatibility; pursuit no longer stops for height.
    [Header("Broadcast obedience — preserved")]
    [Min(.01f)] public float stoppedTolerance=.75f,reverseTolerance=.75f;
    [Header("Runtime (read only)")]
    [SerializeField] Behaviour currentState;
    readonly HashSet<int> reports=new HashSet<int>();static int reportSequence;
    RampagerMovement motor;RampagerPerception senses;RampagerAttack attack;RampagerPatrolRoute patrolRoute;
    RampagerDepthArrival depthArrival;
    public bool Uses3DNavigation {get{return depthArrival && depthArrival.isActiveAndEnabled;}}
    public bool OnGameplayPlane {get{return !Uses3DNavigation || Mathf.Abs(Motor.MovementPlaneZ-(world && world.Player?world.Player.GameplayZ:0))<.08f;}}
    public float DistanceToSound(Vector2 source){return DistanceToSound((Vector3)source);}
    public float DistanceToSound(Vector3 source){return Vector3.Distance(transform.position,source);}
    RampagerVisualEffects visuals;SpriteRenderer body,eye;int eyeOrder;
    Vector2 target,sleepPosition,returnPosition,lastHeardPosition,previousPlayer,previousChild;
    float inspectionRemaining,sleepRemaining,detectionProgress,lastSight=-100,stun,trackedPitch;
    float sleepDepth,returnDepth,targetDepth,lastHeardDepth;
    float wakeRemaining,noticeRemaining,watchElapsed,chaseElapsed,safeElapsed,lastReportAt=-100;
    float disengageRemaining;bool disengaging;
    float waveDelay,waveMove,waveHold,waveRecovery,stopped,reverse;
    int waveAction=-1,lastWaveId,waveDirection;
    bool waking,noticing,watching,checking,returning,waitingToSleep,returnAsSleeping,attentionFromSleeping,reportedAttention;
    bool waveActive,attackWaveBoost,sleepingBeforeWave,waterGunPriority;
    public Behaviour State {get{return currentState;}}
    // Idle is a roaming substate; all attention, combat and broadcast rules stay shared.
    public bool IdleWaiting {get{return State==Behaviour.Roaming && !waveActive && !watching && !noticing && !checking && !returning && !waitingToSleep && !HasPatrolRoute;}}
    public bool HasPatrolRoute {get{var route=patrolRoute?patrolRoute:GetComponent<RampagerPatrolRoute>();return route && route.isActiveAndEnabled && route.HasPoints && !route.Complete;}}
    public float Facing {get{return Motor.Facing;}}
    public Vector2 Position {get{return Motor.Position;}}
    public Vector2 LastKnownPosition {get{return target;}}
    public Vector3 LastKnownWorldPosition {get{return new Vector3(target.x,target.y,targetDepth);}}
    // Depth-facing uses the same attention source as the coordinator, including last heard/report positions.
    public Vector3 AttentionWorldPosition {get{if(!waveActive)return new Vector3(target.x,target.y,targetDepth);return WatchingPlayer || State==Behaviour.Attack?new Vector3(target.x,target.y,targetDepth):attentionFromSleeping || reportedAttention?new Vector3(lastHeardPosition.x,lastHeardPosition.y,lastHeardDepth):world.Player.WorldPosition;}}
    public Vector2 SleepPosition {get{return sleepPosition;}}
    public float LastSightTime {get{return lastSight;}}
    public bool Waking {get{return State==Behaviour.Sleeping && waking;}}
    public float WakeRemaining {get{return wakeRemaining;}}
    public bool Noticing {get{return noticing || Waking;}}
    public float NoticeRemaining {get{return Waking?wakeRemaining:noticeRemaining;}}
    public bool WatchingPlayer {get{return watching && State==Behaviour.Roaming && !waveActive;}}
    public bool PlayerUnderGaze {get{return (WatchingPlayer || State==Behaviour.Attack || waveActive) && !Senses.TargetIsChild && Senses.LastObservation.seesPlayer;}}
    public bool DisengagingWatch {get{return WatchingPlayer && disengaging;}}
    public float GazeFeedbackStrength {get{return DisengagingWatch?Mathf.Clamp01(disengageRemaining/Mathf.Max(.01f,watchLostSightTime)):1;}}
    public bool ReturningHome {get{return returning;}}
    public bool ReturningToSleep {get{return returning && returnAsSleeping;}}
    public bool WaitingToSleep {get{return waitingToSleep;}}
    public float SleepRemaining {get{return sleepRemaining;}}
    public float DetectionProgress {get{return detectionProgress;}}
    public float Investigation {get{return inspectionRemaining;}}
    public float Alert {get{return State==Behaviour.Attack?1:detectionProgress;}}
    public float WatchElapsed {get{return watchElapsed;}}
    public float ChaseElapsed {get{return chaseElapsed;}}
    public float SafeElapsed {get{return safeElapsed;}}
    public bool WaveActive {get{return waveActive;}}
    public bool PrioritizingWaterGunHolder {get{return State==Behaviour.Attack && waterGunPriority;}}
    public bool SwarmAttackBoost {get{return State==Behaviour.Attack && attackWaveBoost;}}
    public bool RedEyes {get{return waveActive || SwarmAttackBoost || WatchingPlayer || State==Behaviour.Attack;}}
    public bool WatchingHighFlyer {get;private set;}
    public float SightPitch {get{float pitch=WatchingPlayer || State==Behaviour.Attack || checking?trackedPitch:waveActive?waveSightPitch:roamingSightPitch;return Mathf.Clamp(pitch+sightPitchOffset,-90,90);}}
    public float SightRange {get{return Senses.SightRange;}}
    public Vector2 SightDirection {get{float a=SightPitch*Mathf.Deg2Rad;float horizontal=world && world.gameObject.scene.path.StartsWith(Recoil3DScene.Folder)?-Mathf.Sin(Motor.VisualYaw*Mathf.Deg2Rad):Facing;return new Vector2(horizontal*Mathf.Cos(a),Mathf.Sin(a));}}
    public Vector3 SightDirection3D
    {get{float a=SightPitch*Mathf.Deg2Rad,yaw=Motor.VisualYaw*Mathf.Deg2Rad;return new Vector3(-Mathf.Sin(yaw)*Mathf.Cos(a),Mathf.Sin(a),-Mathf.Cos(yaw)*Mathf.Cos(a));}}
    public float CurrentMoveSpeed {get{return State==Behaviour.Attack?chaseSpeed*attackSpeedMultiplier*(SwarmAttackBoost?Motor.swarmAttackSpeedMultiplier:1):patrolSpeed*(waveActive?1:roamingSpeedMultiplier);}}
    public float WaveDelay {get{return waveDelay;}}
    public float WaveMove {get{return waveMove;}}
    public float WaveHold {get{return waveHold;}}
    public bool WaveMoving {get{return waveActive && (world && world.SoundWaves?world.SoundWaves.MoveStep>0:waveDelay<=0 && waveMove>0);}}
    public int WaveDirection {get{return waveDirection;}}
    public int WaveIndex {get;private set;}
    // Builders use these forwarding properties; existing authored components remain intact.
    public float leftOffset {get{return Motor.leftOffset;}set{Motor.leftOffset=value;}}
    public float rightOffset {get{return Motor.rightOffset;}set{Motor.rightOffset=value;}}
    public float patrolSpeed {get{return Motor.patrolSpeed;}set{Motor.patrolSpeed=value;}}
    public float investigateSpeed {get{return Motor.investigateSpeed;}set{Motor.investigateSpeed=value;}}
    public float chaseSpeed {get{return Motor.chaseSpeed;}set{Motor.chaseSpeed=value;}}
    public float idealSightRange {get{return Senses.idealSightRange;}set{Senses.idealSightRange=value;}}
    public float fieldOfView {get{return Senses.fieldOfView;}set{Senses.fieldOfView=value;}}
    public float minimumExposure {get{return Senses.minimumExposure;}set{Senses.minimumExposure=value;}}
    public float contactRadius {get{return Attack.contactRadius;}set{Attack.contactRadius=value;}}
    public float attackInterval {get{return Attack.attackInterval;}set{Attack.attackInterval=value;}}
    RampagerMovement Motor {get{if(!motor)motor=GetComponent<RampagerMovement>();if(!motor)motor=gameObject.AddComponent<RampagerMovement>();return motor;}}
    RampagerPerception Senses {get{if(!senses)senses=GetComponent<RampagerPerception>();if(!senses)senses=gameObject.AddComponent<RampagerPerception>();return senses;}}
    RampagerAttack Attack {get{if(!attack)attack=GetComponent<RampagerAttack>();if(!attack)attack=gameObject.AddComponent<RampagerAttack>();return attack;}}
    public void Configure(RecoilPrototype owner,float l,float r){world=owner;leftOffset=l;rightOffset=r;}
    public void EnsureStandardComponents()
    {
        if(!GetComponent<RampagerGrab>())gameObject.AddComponent<RampagerGrab>();
        var movementComponent=Motor;var perceptionComponent=Senses;var attackComponent=Attack;
        if(!GetComponent<CoalerRoarAudio>())gameObject.AddComponent<CoalerRoarAudio>();
        if(gameObject.scene.path.StartsWith(Recoil3DScene.Folder) && !GetComponent<RampagerDepthArrival>())gameObject.AddComponent<RampagerDepthArrival>();
        if(!GetComponent<RampagerVisualEffects>())gameObject.AddComponent<RampagerVisualEffects>();
        // Patrol points are authored explicitly. Missing routes remain idle.
    }
    public void Initialize(RecoilPrototype owner)
    {
        EnsureStandardComponents();world=owner;Motor.Initialize(owner);depthArrival=GetComponent<RampagerDepthArrival>();if(depthArrival)depthArrival.Initialize(owner,this,Motor);sleepPosition=sleepPoint?(Vector2)sleepPoint.position:Motor.Home;
        Senses.Initialize(owner,this,Motor);Attack.Initialize(owner,Senses);
        Senses.PlayerNoiseHeard-=OnNoiseHeard;Senses.PlayerNoiseHeard+=OnNoiseHeard;
        var eyeTransform=eye?eye.transform:transform.Find("Eye");if(eyeTransform){eye=eyeTransform.GetComponent<SpriteRenderer>();if(eye)eyeOrder=eye.sortingOrder;}
        visuals=GetComponent<RampagerVisualEffects>();body=visuals.Initialize(GetComponent<SpriteRenderer>(),eyeTransform);
        patrolRoute=GetComponent<RampagerPatrolRoute>();if(patrolRoute)patrolRoute.Initialize(this);ResetController();
    }
    public void PrepareNavigationSpawn()
    {
        // Navigation may project destinations, but must never relocate the authored spawn.
        if(patrolRoute)patrolRoute.Initialize(this);
    }
    public void ResetController()
    {
        Motor.ResetMovement();if(depthArrival){depthArrival.ResetTransit();sleepPosition=sleepPoint?(Vector2)sleepPoint.position:Position;}Senses.ResetPerception();Attack.ResetAttack();if(patrolRoute)patrolRoute.Initialize(this);
        waking=noticing=watching=checking=returning=waitingToSleep=returnAsSleeping=attentionFromSleeping=reportedAttention=false;
        waveActive=attackWaveBoost=sleepingBeforeWave=waterGunPriority=false;
        wakeRemaining=noticeRemaining=watchElapsed=chaseElapsed=safeElapsed=inspectionRemaining=sleepRemaining=detectionProgress=stun=waveDelay=waveMove=waveHold=waveRecovery=stopped=reverse=0;
        reports.Clear();lastReportAt=lastSight=-100;waveAction=-1;lastWaveId=WaveIndex=0;waveDirection=0;WatchingHighFlyer=false;
        disengaging=false;disengageRemaining=0;ResetAcoustics();
        currentState=initialState;trackedPitch=roamingSightPitch;
        sleepDepth=sleepPoint?sleepPoint.position.z:Motor.MovementPlaneZ;returnDepth=Motor.MovementPlaneZ;
        returnPosition=Position;target=lastHeardPosition=Position;targetDepth=lastHeardDepth=Motor.MovementPlaneZ;
        previousPlayer=world.Player.Position;previousChild=world.Child?world.Child.Position:Vector2.zero;
        // An authored attacking spawn waits for sound; it does not know the player location.
        if(State==Behaviour.Attack)lastSoundAt=world.Clock;
        if(visuals)visuals.ClearTremble();Motor.ApplyPosition();
    }
    void Update(){if(world && world.Playing)Simulate(world.FrameDelta);}
    public bool CanSeePlayer(){return world && State!=Behaviour.Sleeping && Senses.CanSeePlayer();}
    public void HearNoise(Vector2 source,float radius){HearNoise((Vector3)source,radius);}
    public void HearNoise(Vector3 source,float radius){float effectiveRadius=radius*Senses.PerceptionRangeMultiplier(source);if(DistanceToSound(source)<=effectiveRadius){lastHeardDepth=source.z;heardRadius=radius;Senses.HearNoise(source,effectiveRadius);}}
    static float Delay(Vector2 range){return Random.Range(Mathf.Max(.01f,Mathf.Min(range.x,range.y)),Mathf.Max(.01f,Mathf.Max(range.x,range.y)));}
    void OnNoiseHeard(Vector2 source){if(!waveActive)ReceiveAcousticNoise(source,lastHeardDepth,heardRadius);}
    void BeginNotice(Vector2 source,bool fromReport,float depth=float.NaN)
    {
        if(State==Behaviour.Attack || waveActive || WatchingPlayer)return;
        lastHeardPosition=source;lastHeardDepth=float.IsNaN(depth)?world.Player.GameplayZ:depth;
        if(noticing || waking)
        {
            // Repeated footsteps update the position without extending the reaction indefinitely.
            if(fromReport){reportedAttention=true;if(waking)wakeRemaining=Mathf.Min(wakeRemaining,reportNoticeDelay);else noticeRemaining=Mathf.Min(noticeRemaining,reportNoticeDelay);}
            return;
        }
        attentionFromSleeping=State==Behaviour.Sleeping;
        if(!returning && !waitingToSleep){returnPosition=attentionFromSleeping?sleepPosition:Position;returnDepth=attentionFromSleeping?sleepDepth:Motor.MovementPlaneZ;}
        CancelReturn();checking=false;inspectionRemaining=0;reportedAttention=fromReport;
        if(patrolRoute)patrolRoute.Suspend();
        TrackPosition(new Vector3(source.x,source.y,lastHeardDepth));
        if(attentionFromSleeping){waking=true;wakeRemaining=fromReport?reportNoticeDelay:Delay(sleepingNoticeDelay);}
        else{noticing=true;noticeRemaining=fromReport?reportNoticeDelay:Delay(roamingNoticeDelay);}
    }
    void CompleteNotice()
    {
        if(State==Behaviour.Sleeping){float energy=detectionProgress;SetState(Behaviour.Roaming);if(!waveActive)detectionProgress=energy;}
        waking=noticing=false;wakeRemaining=noticeRemaining=0;
        if(!waveActive){BeginAcousticWatch();return;}
        Motor.LookAt(lastHeardPosition.x);TrackPosition(new Vector3(lastHeardPosition.x,lastHeardPosition.y,lastHeardDepth));checking=true;inspectionRemaining=soundInspectionDuration;
    }
    void BeginWatching()
    {checking=false;inspectionRemaining=0;watching=true;watchElapsed=detectionProgress=0;
        disengaging=false;disengageRemaining=0;
        CancelReturn();if(patrolRoute)patrolRoute.Suspend();if(waveActive)TrackPlayer();else{target=lastHeardPosition;targetDepth=lastHeardDepth;TrackPosition(LastKnownWorldPosition);}}
    // Explicit story commands reuse the standard state/perception/movement components.
    public bool HideStatusLabel {get;set;}
    public void WatchForStory()
    {SetState(Behaviour.Roaming);waking=noticing=false;wakeRemaining=noticeRemaining=0;
        attentionFromSleeping=reportedAttention=false;Senses.SetTargetChild(false);
        lastHeardPosition=world.Player.Position;lastHeardDepth=world.Player.GameplayZ;lastSoundAt=world.Clock;BeginWatching();Senses.Sense();}
    public void SleepForStory(){SetState(Behaviour.Sleeping);Senses.ResetPerception();}
    public void MoveForStory(Vector2 position){Motor.Position=position;Motor.ApplyPosition();}
    public void TakeWaterHit(Vector2 start,Vector2 direction,float range,Vector2 source)
    {
        if(!world || !Senses.WaterHits(start,direction,range))return;
        if(State!=Behaviour.Attack && !watching && !noticing && !waking){returnPosition=Position;returnDepth=Motor.MovementPlaneZ;}
        lastSoundAt=world.Clock;
        stun=State==Behaviour.Attack?.65f:0;Attack.CancelAttack();Senses.SetTargetChild(false);target=source;targetDepth=world.Player.GameplayZ;RequestAttack();waterGunPriority=true;lastSight=world.Clock;TrackPosition(new Vector3(source.x,source.y,targetDepth));
    }
    public bool ReceiveReport(Report report)
    {
        if(!world || !report.attack || report.source==this || !report.source || report.target!=world.Player || report.sightTime>world.Clock || world.Clock-report.sightTime>reportFreshness || reports.Contains(report.id))return false;
        reports.Add(report.id);
        if(!waveActive){ReceiveAcousticReport(report);return true;}
        // Never read another monster's live target or enter Attack from a report.
        if(State==Behaviour.Attack)return true;
        if(WatchingPlayer){reportedAttention=true;return true;}
        BeginNotice(report.position,true,report.depth);return true;
    }
    void ReportSight(bool sees)
    {
        if(State!=Behaviour.Attack || !sees || world.Clock-lastReportAt<.5f)return;
        lastReportAt=world.Clock;
        world.PublishReport(new Report{source=this,target=world.Player,position=target,depth=targetDepth,sightTime=world.Clock,attack=true,id=++reportSequence,eventType=ReportEvent.ConfirmedTarget},reportRadius);
    }
    void SetState(Behaviour next)
    {
        WatchingHighFlyer=false;if(State==next)return;attackPreparationRemaining=0;StopAttackSearch();
        if(patrolRoute)patrolRoute.Suspend();currentState=next;
        disengaging=false;disengageRemaining=0;
        waking=noticing=watching=checking=false;wakeRemaining=noticeRemaining=inspectionRemaining=watchElapsed=detectionProgress=0;
        stopped=reverse=0;waveAction=-1;CancelReturn();
        if(next!=Behaviour.Attack){attackWaveBoost=false;waterGunPriority=false;Attack.CancelAttack();}
        if(next==Behaviour.Sleeping){StopAttackSearch();sleepingBeforeWave=false;waveActive=false;waveDelay=waveMove=waveHold=waveRecovery=0;}
        trackedPitch=waveActive?waveSightPitch:roamingSightPitch;
    }
    void EnterAttack()
    {
        if(State==Behaviour.Attack)return;
        bool boosted=waveActive || attackWaveBoost;SetState(Behaviour.Attack);attackWaveBoost=boosted;chaseElapsed=safeElapsed=0;
        if(waveActive){target=Senses.TargetPosition;targetDepth=Senses.TargetWorldPosition.z;lastSight=world.Clock;TrackPlayer();}
        else{TrackPosition(LastKnownWorldPosition);ReportSight(true);}
    }
    void TrackPosition(Vector3 p)
    {Motor.LookAt(p.x);Vector2 diff=(Vector2)p-Motor.EyePosition;trackedPitch=Mathf.Atan2(diff.y,new Vector2(diff.x,p.z-Motor.MovementPlaneZ).magnitude)*Mathf.Rad2Deg;}
    void TrackPlayer(){TrackPosition(Senses.TargetWorldPosition);}
    void CancelReturn(){returning=waitingToSleep=false;sleepRemaining=0;}
    void BeginReturn(bool sleep)
    {
        attackPreparationRemaining=0;StopAttackSearch();
        SetState(Behaviour.Roaming);watching=checking=noticing=waking=false;detectionProgress=inspectionRemaining=0;
        disengaging=false;disengageRemaining=0;
        returning=true;waitingToSleep=false;returnAsSleeping=sleep;sleepRemaining=0;reportedAttention=false;
        if(sleep){returnPosition=sleepPosition;returnDepth=sleepDepth;}if(patrolRoute)patrolRoute.Suspend();
    }
    void FinishSoundInspection()
    {inspectionRemaining=0;checking=false;BeginReturn(attentionFromSleeping || returnToSleepAfterSound);}
    bool QuietTarget(float dt)
    {
        if(Senses.TargetIsChild && world.Child)
        {return world.Child.SilentWalking || (world.Child.Position-previousChild).sqrMagnitude<=dt*dt*.0225f;}
        return world.Player.SilentWalking || (world.Player.Position-previousPlayer).sqrMagnitude<=dt*dt*.0225f;
    }
    // Broadcasts are modifiers and movement commands, not a fourth behaviour state.
    public bool ReceiveBuildingWarning(float duration)
    {
        attackPreparationRemaining=0;
        if(!waveActive)sleepingBeforeWave=State==Behaviour.Sleeping;
        if(State==Behaviour.Sleeping)SetState(Behaviour.Roaming);
        waveActive=true;if(State==Behaviour.Attack)attackWaveBoost=true;
        watching=noticing=waking=checking=false;noticeRemaining=wakeRemaining=0;detectionProgress=inspectionRemaining=0;CancelReturn();trackedPitch=waveSightPitch;
        stopped=reverse=0;waveAction=0;waveDelay=waveMove=waveRecovery=0;waveHold=Mathf.Max(0,duration);WaveIndex=0;if(patrolRoute)patrolRoute.Suspend();return true;
    }
    public bool ReceiveBuildingWave(int id,int index,int total,int direction)
    {
        if(id<=lastWaveId)return false;
        if(!waveActive)ReceiveBuildingWarning(0);lastWaveId=id;WaveIndex=index;waveDirection=direction<0?-1:1;
        stopped=reverse=0;waveAction=-1;waveDelay=world.SoundWaves.CommandMoveDelay;waveMove=world.SoundWaves.commandDuration;waveHold=world.SoundWaves.holdDuration;waveRecovery=0;
        if(State!=Behaviour.Attack)Motor.Facing=waveDirection;return true;
    }
    public void EndSwarm(float duration){if(waveActive)waveRecovery=Mathf.Max(.01f,duration);}
    void AdvanceWave(float dt,out float delayDt,out float moveDt,out float holdDt)
    {
        delayDt=moveDt=holdDt=0;if(!waveActive)return;
        if(waveRecovery>0)
        {
            var recoveryBroadcast=world.SoundWaves;
            waveRecovery=recoveryBroadcast && recoveryBroadcast.Phase==SoundWaveManager.BroadcastPhase.Recovery?
                recoveryBroadcast.Remaining:Mathf.Max(0,waveRecovery-dt);
            if(waveRecovery<=0)
            {
                bool restoreSleep=sleepingBeforeWave;sleepingBeforeWave=false;
                stopped=reverse=0;waveAction=-1;waveActive=false;waveDelay=waveMove=waveHold=0;lastSoundAt=world.Clock;
                if(restoreSleep)
                {
                    if(State==Behaviour.Attack){SetState(Behaviour.Roaming);BeginWatching();attentionFromSleeping=true;}
                    else SetState(Behaviour.Sleeping);
                }
                else if(State==Behaviour.Roaming){if(watching)BeginWatching();else FinishSoundInspection();}
            }
            return;
        }
        var broadcast=world.SoundWaves;
        if(broadcast && broadcast.RedAlert)
        {
            waveDelay=broadcast.CueDelay;waveMove=broadcast.CueMove;waveHold=broadcast.CueHold;
            delayDt=Mathf.Min(dt,broadcast.DelayStep);moveDt=Mathf.Min(dt,broadcast.MoveStep);holdDt=Mathf.Min(dt,broadcast.HoldStep);
            return;
        }
        delayDt=Mathf.Min(dt,waveDelay);waveDelay-=delayDt;
        moveDt=Mathf.Min(dt-delayDt,waveMove);waveMove-=moveDt;
        holdDt=dt-delayDt-moveDt;waveHold=Mathf.Max(0,waveHold-holdDt);
    }
    void CheckWaveBehaviour(RampagerPerception.Observation seen,float reactionDt,float movingDt,float holdingDt)
    {
        // Reaction is a grace window, not another hold command. No timer carries
        // from it into movement. The same manager slices drive physical movement.
        if(reactionDt>.000001f){stopped=reverse=0;waveAction=-1;}
        CheckWaveIntent(seen,movingDt,true);
        CheckWaveIntent(seen,holdingDt,false);
    }
    void CheckWaveIntent(RampagerPerception.Observation seen,float dt,bool moving)
    {
        if(dt<=.000001f || State==Behaviour.Attack)return;
        int action=moving?1:0;
        // Every move/hold transition starts its own continuous violation timers.
        if(waveAction!=action){stopped=reverse=0;waveAction=action;}
        if(!seen.seesPlayer || world.Player.MovementBlocked){stopped=reverse=0;return;}
        float intent=world.Player.HorizontalIntent;
        stopped=moving && Mathf.Abs(intent)<.1f?stopped+dt:0;
        reverse=(moving?intent*waveDirection<0:Mathf.Abs(intent)>.1f)?reverse+dt:0;
        if(stopped>=stoppedTolerance-.000001f || reverse>=reverseTolerance-.000001f)RequestAttack();
    }
    void RememberPositions()
    {previousPlayer=world.Player.Position;previousChild=world.Child?world.Child.Position:Vector2.zero;}
    public void Simulate(float dt)
    {
        if(!world || dt<=0)return;
        if(depthArrival){bool tracking=Noticing||WatchingPlayer||State==Behaviour.Attack||checking;if(tracking || !depthArrival.Travelling)depthArrival.Face(AttentionWorldPosition,tracking,dt);}
        else Motor.AdvanceTurn(dt);
        stun=Mathf.Max(0,stun-dt);float delayDt,moveDt,holdDt;AdvanceWave(dt,out delayDt,out moveDt,out holdDt);
        if(PreparingAttack){AdvanceAttackPreparation(dt);return;}
        if(!waveActive){SimulateAcoustics(dt);return;}
        if(State==Behaviour.Sleeping)
        {
            if(waking){wakeRemaining=Mathf.Max(0,wakeRemaining-dt);if(wakeRemaining<=.000001f)CompleteNotice();}
            if(State==Behaviour.Sleeping){Motor.SimulateVertical(dt);if(depthArrival)depthArrival.Stop();RememberPositions();return;}
        }
        if((WatchingPlayer || State==Behaviour.Attack) && Senses.LocateTarget())TrackPlayer();
        Senses.Sense();var observation=Senses.LastObservation;bool sees=observation.sees;
        if(sees)StopAttackSearch();
        if(sees){target=Senses.TargetPosition;targetDepth=Senses.TargetWorldPosition.z;lastSight=world.Clock;}
        if(State==Behaviour.Attack)
        {
            chaseElapsed+=dt;ReportSight(sees);
            bool inReach=Senses.CanTouchPlayer(contactRadius) || Senses.CanTouchChild(contactRadius);
            safeElapsed=sees && QuietTarget(dt) && !inReach?safeElapsed+dt:0;
            if(safeElapsed>=safeUnreachableTime-.000001f || chaseElapsed>=maximumChaseTime-.000001f)
            {SetState(Behaviour.Roaming);BeginWatching();}
        }
        else if(waveActive)
        {
            // Preserve the existing wave violations, including flying/interaction and per-action .5s checks.
            if(observation.seesPlayer && (world.Player.Violating || observation.shot || observation.interaction || observation.highFall))RequestAttack();
            else CheckWaveBehaviour(observation,delayDt,moveDt,holdDt);
        }
        if(PreparingAttack){AdvanceAttackPreparation(dt);return;}
        Attack.Simulate(dt,State==Behaviour.Attack && stun<=0);
        float direction=0,distance=0;bool turnOnBlock=false,navigate=false;
        Vector3 destination=transform.position;float stopDistance=0;
        WatchingHighFlyer=false;
        if(State==Behaviour.Attack)
        {direction=Motor.Toward(target.x);distance=CurrentMoveSpeed*dt;navigate=true;destination=AcousticDestination();}
        else if(State!=Behaviour.Attack && waveActive)
        {if(moveDt>0){direction=waveDirection;distance=CurrentMoveSpeed*moveDt;navigate=true;destination=transform.position+Vector3.right*waveDirection*Mathf.Max(1,distance);}}
        else if(WatchingPlayer)
        {
            float separation=new Vector2(target.x-Position.x,targetDepth-Motor.MovementPlaneZ).magnitude;
            if(sees && separation>watchStandOff)
            {direction=Mathf.Sign(target.x-Position.x);distance=Mathf.Min(separation-watchStandOff,CurrentMoveSpeed*dt);navigate=true;destination=new Vector3(target.x,Position.y,targetDepth);stopDistance=watchStandOff;}
        }
        else if(State==Behaviour.Roaming && !checking && !noticing)
        {
            if(returning || waitingToSleep)
            {
                float delta=returnPosition.x-Position.x;
                destination=new Vector3(returnPosition.x,returnPosition.y,returnDepth);
                float remaining=depthArrival?new Vector2(delta,returnDepth-Motor.MovementPlaneZ).magnitude:Mathf.Abs(delta);
                if(remaining>(depthArrival ? .12f : .04f))
                {returning=true;waitingToSleep=false;sleepRemaining=0;direction=Mathf.Sign(delta);distance=Mathf.Min(remaining,CurrentMoveSpeed*dt);navigate=true;}
                else if(returnAsSleeping)
                {
                    returning=false;
                    if(!waitingToSleep){waitingToSleep=true;sleepRemaining=sleepDelay;}
                    else sleepRemaining=Mathf.Max(0,sleepRemaining-dt);
                    if(sleepRemaining<=0)SetState(Behaviour.Sleeping);
                }
                else{CancelReturn();trackedPitch=roamingSightPitch;}
            }
            else
            {
                float look=Facing;bool route=patrolRoute && patrolRoute.Evaluate(transform.position,stun>0?0:dt,Facing,CurrentMoveSpeed,out direction,out distance,out look);
                if(route){if(patrolRoute.RequestsFacing)Motor.Facing=look;if(distance>0){navigate=true;destination=patrolRoute.CurrentPoint.Destination;}}
                // No route: wait at the current position instead of inventing a patrol range.
            }
        }
        if(State==Behaviour.Attack && !sees){UpdateAttackSearch(dt,ref destination,ref distance,ref navigate,ref stopDistance);direction=Motor.Toward(destination.x);}
        Vector3 beforeSearchMove=transform.position;
        bool blocked=false;
        if(!Attack.MovementLocked && stun<=0 && distance>0)
        {
            if(depthArrival && navigate && !Motor.Airborne)
            {blocked=!depthArrival.MoveTo(destination,distance,stopDistance,dt,WatchingPlayer||State==Behaviour.Attack,State==Behaviour.Attack);if(blocked && turnOnBlock)Motor.Facing=-Motor.Facing;}
            else if(direction!=0)blocked=!Motor.Move(direction,distance,turnOnBlock);
        }
        else if(depthArrival)depthArrival.Stop();
        ConstrainAttackSearchStep(beforeSearchMove);
        if(SwarmAttackBoost && !Attack.MovementLocked && stun<=0 && !WatchingHighFlyer)Motor.TryJump(target,blocked);
        Motor.SimulateVertical(dt);if(!Attack.MovementLocked)Attack.TryAttack(State==Behaviour.Attack && stun<=0);
        RememberPositions();
    }
    public void SyncVisuals()
    {
        Motor.ApplyPosition();if(visuals)visuals.ClearTremble();
        if(eye)
        {
            eye.color=RedEyes?RecoilPrototype.Hex(0xff3434):State==Behaviour.Sleeping?RecoilPrototype.Hex(0x554842):RecoilPrototype.Hex(0xf5ca97);
            eye.sortingOrder=RedEyes?160:eyeOrder;var p=eye.transform.localPosition;p.x=Facing*.15f;eye.transform.localPosition=p;
        }
        if(body)body.color=stun>0?RecoilPrototype.Hex(0x8adceb):State==Behaviour.Sleeping?RecoilPrototype.Hex(0x665751):Color.Lerp(RecoilPrototype.Hex(0xb87570),RecoilPrototype.Hex(0xff6159),Alert);
    }
    void OnDestroy(){if(senses)senses.PlayerNoiseHeard-=OnNoiseHeard;}
    void OnDrawGizmosSelected(){Gizmos.color=new Color(.5f,.8f,1);Gizmos.DrawWireSphere(Application.isPlaying?(Vector3)sleepPosition:sleepPoint?sleepPoint.position:transform.position,.3f);}
}
