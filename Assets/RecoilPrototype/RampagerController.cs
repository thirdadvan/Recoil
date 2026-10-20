using System.Collections.Generic;
using UnityEngine;

// State coordination; sensing, collision locomotion, patrol actions and attack execution stay separate.
[DisallowMultipleComponent,RequireComponent(typeof(RampagerPerception),typeof(RampagerMovement))]
[RequireComponent(typeof(RampagerAttack),typeof(RampagerVisualEffects))]
public sealed class RampagerController : MonoBehaviour
{
    public enum Behaviour { Roaming=0, Attack=2, Sleeping=4 }
    public enum SpawnDirection { [InspectorName("向左")] Left, [InspectorName("向右")] Right }
    public enum ReportEvent { Shooting, Flying, ForbiddenInteraction, ConfirmedTarget }
    // 'attack' describes the sender. A recipient receives a location, never permission to attack.
    public struct Report {public RampagerController source;public RecoilPlayerController target;public EscortChild childTarget;public Vector2 position;public float sightTime;public bool attack;public int id;public ReportEvent eventType;}
    [SerializeField] RecoilPrototype world;
    [Header("Initial state and return")]
    public Behaviour initialState=Behaviour.Roaming;
    public SpawnDirection spawnFacing=SpawnDirection.Left;
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
    [Range(.05f,1)] public float roamingSpeedMultiplier=.5f;
    [Min(.1f),Tooltip("注视时缓慢接近，但停在此水平距离外。")]
    public float watchStandOff=1.2f;
    [Header("Anger while watching")]
    [Min(.01f),Tooltip("普通地面跑动，从零怒气到攻击所需秒数。")]
    public float runningAngerTime=.5f;
    [Min(0),Tooltip("进入注视前已开始的互动，总互动时长超过此秒数时攻击。注视后新开始的互动立即攻击。")]
    public float continuingInteractionAttackTime=.5f;
    [Min(.01f)] public float angerDecayTime=1;
    [Min(1)] public float reportedAngerMultiplier=2;
    [Header("Visual detection")]
    [Range(-90,90)] public float sightPitchOffset=-10,roamingSightPitch=-25,waveSightPitch=15;
    [Header("Reports and chase exit")]
    public float reportRadius=10,reportFreshness=3;
    [Min(.01f)] public float safeUnreachableTime=10,maximumChaseTime=20;
    [Header("Attack movement")]
    [Min(1)] public float attackSpeedMultiplier=1.5f;
    [Min(0)] public float highFlightStopBodies=1.5f;
    [Header("Broadcast obedience — preserved")]
    [Min(.01f)] public float stoppedTolerance=.5f,reverseTolerance=.5f;
    [Header("Runtime (read only)")]
    [SerializeField] Behaviour currentState;
    readonly HashSet<int> reports=new HashSet<int>();static int reportSequence;
    RampagerMovement motor;RampagerPerception senses;RampagerAttack attack;RampagerPatrolRoute patrolRoute;
    RampagerVisualEffects visuals;SpriteRenderer body,eye;int eyeOrder;
    Vector2 target,sleepPosition,returnPosition,lastHeardPosition,previousPlayer,previousChild;
    float inspectionRemaining,sleepRemaining,detectionProgress,lastSight=-100,stun,trackedPitch;
    float wakeRemaining,noticeRemaining,watchElapsed,chaseElapsed,safeElapsed,lastReportAt=-100;
    float disengageRemaining;bool disengaging;
    float waveDelay,waveMove,waveHold,waveRecovery,stopped,reverse;
    int waveAction=-1,lastWaveId,waveDirection;
    bool waking,noticing,watching,checking,returning,waitingToSleep,returnAsSleeping,attentionFromSleeping,reportedAttention,brushing;
    bool waveActive,attackWaveBoost,sleepingBeforeWave,waterGunPriority;
    public Behaviour State {get{return currentState;}}
    public float SpawnFacingSign {get{return spawnFacing==SpawnDirection.Right?1:-1;}}
    public float Facing {get{return Motor.Facing;}}
    public Vector2 Position {get{return Motor.Position;}}
    public Vector2 LastKnownPosition {get{return target;}}
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
    public Vector2 SightDirection {get{float a=SightPitch*Mathf.Deg2Rad;return new Vector2(Facing*Mathf.Cos(a),Mathf.Sin(a));}}
    public float CurrentMoveSpeed {get{return State==Behaviour.Attack?chaseSpeed*attackSpeedMultiplier*(SwarmAttackBoost?Motor.swarmAttackSpeedMultiplier:1):patrolSpeed*(waveActive?1:roamingSpeedMultiplier);}}
    public float WaveDelay {get{return waveDelay;}}
    public float WaveMove {get{return waveMove;}}
    public float WaveHold {get{return waveHold;}}
    public bool WaveMoving {get{return waveActive && waveDelay<=0 && waveMove>0;}}
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
        var movementComponent=Motor;var perceptionComponent=Senses;var attackComponent=Attack;
        if(!GetComponent<RampagerVisualEffects>())gameObject.AddComponent<RampagerVisualEffects>();
        if(!GetComponent<RampagerPatrolRoute>())RampagerPatrolRoute.CreateDefaultPoints(this);
    }
    public void Initialize(RecoilPrototype owner)
    {
        EnsureStandardComponents();world=owner;Motor.Initialize(owner);sleepPosition=sleepPoint?(Vector2)sleepPoint.position:Motor.Home;
        Senses.Initialize(owner,this,Motor);Attack.Initialize(owner,Senses);
        Senses.PlayerNoiseHeard-=OnNoiseHeard;Senses.PlayerNoiseHeard+=OnNoiseHeard;
        var eyeTransform=eye?eye.transform:transform.Find("Eye");if(eyeTransform){eye=eyeTransform.GetComponent<SpriteRenderer>();if(eye)eyeOrder=eye.sortingOrder;}
        visuals=GetComponent<RampagerVisualEffects>();body=visuals.Initialize(GetComponent<SpriteRenderer>(),eyeTransform);
        patrolRoute=GetComponent<RampagerPatrolRoute>();if(patrolRoute)patrolRoute.Initialize(this);ResetController();
    }
    public void ResetController()
    {
        Motor.ResetMovement(SpawnFacingSign);Senses.ResetPerception();Attack.ResetAttack();if(patrolRoute)patrolRoute.ResetRoute();
        waking=noticing=watching=checking=returning=waitingToSleep=returnAsSleeping=attentionFromSleeping=reportedAttention=brushing=false;
        waveActive=attackWaveBoost=sleepingBeforeWave=waterGunPriority=false;
        wakeRemaining=noticeRemaining=watchElapsed=chaseElapsed=safeElapsed=inspectionRemaining=sleepRemaining=detectionProgress=stun=waveDelay=waveMove=waveHold=waveRecovery=stopped=reverse=0;
        reports.Clear();lastReportAt=lastSight=-100;waveAction=-1;lastWaveId=WaveIndex=0;waveDirection=0;WatchingHighFlyer=false;
        disengaging=false;disengageRemaining=0;
        currentState=initialState;trackedPitch=roamingSightPitch;if(State==Behaviour.Sleeping)Motor.Position=sleepPosition;
        returnPosition=Position;target=lastHeardPosition=Position;
        previousPlayer=world.Player.Position;previousChild=world.Child?world.Child.Position:Vector2.zero;
        if(State==Behaviour.Attack){target=world.Player.Position;lastSight=world.Clock;}
        if(visuals)visuals.ClearTremble();transform.position=Position;
    }
    void Update(){if(world && world.Playing)Simulate(world.FrameDelta);}
    public bool CanSeePlayer(){return world && State!=Behaviour.Sleeping && Senses.CanSeePlayer();}
    public void HearNoise(Vector2 source,float radius){Senses.HearNoise(source,radius);}
    static float Delay(Vector2 range){return Random.Range(Mathf.Max(.01f,Mathf.Min(range.x,range.y)),Mathf.Max(.01f,Mathf.Max(range.x,range.y)));}
    void OnNoiseHeard(Vector2 source){BeginNotice(source,false);}
    void BeginNotice(Vector2 source,bool fromReport)
    {
        if(State==Behaviour.Attack || waveActive || WatchingPlayer)return;
        lastHeardPosition=source;
        if(noticing || waking)
        {
            // Repeated footsteps update the position without extending the reaction indefinitely.
            if(fromReport){reportedAttention=true;if(waking)wakeRemaining=Mathf.Min(wakeRemaining,reportNoticeDelay);else noticeRemaining=Mathf.Min(noticeRemaining,reportNoticeDelay);}
            return;
        }
        attentionFromSleeping=State==Behaviour.Sleeping;
        if(!returning && !waitingToSleep)returnPosition=attentionFromSleeping?sleepPosition:Position;
        CancelReturn();checking=false;inspectionRemaining=0;reportedAttention=fromReport;
        if(patrolRoute)patrolRoute.Suspend();
        if(attentionFromSleeping || fromReport)Motor.LookAt(source.x);else TrackPosition(world.Player.Position);
        if(attentionFromSleeping){waking=true;wakeRemaining=fromReport?reportNoticeDelay:Delay(sleepingNoticeDelay);}
        else{noticing=true;noticeRemaining=fromReport?reportNoticeDelay:Delay(roamingNoticeDelay);}
    }
    void CompleteNotice()
    {
        if(State==Behaviour.Sleeping)SetState(Behaviour.Roaming);
        waking=noticing=false;wakeRemaining=noticeRemaining=0;
        Motor.LookAt(lastHeardPosition.x);TrackPosition(lastHeardPosition);checking=true;inspectionRemaining=soundInspectionDuration;
    }
    void BeginWatching()
    {checking=false;inspectionRemaining=0;watching=true;watchElapsed=detectionProgress=0;
        disengaging=false;disengageRemaining=0;
        CancelReturn();if(patrolRoute)patrolRoute.Suspend();TrackPlayer();}
    public void TakeWaterHit(Vector2 start,Vector2 direction,float range,Vector2 source)
    {
        if(!world || !Senses.WaterHits(start,direction,range))return;
        if(State!=Behaviour.Attack && !watching && !noticing && !waking)returnPosition=Position;
        stun=.65f;Attack.CancelAttack();Senses.SetTargetChild(false);EnterAttack();waterGunPriority=true;target=source;lastSight=world.Clock;TrackPosition(source);
    }
    public bool ReceiveReport(Report report)
    {
        if(!world || !report.attack || report.source==this || !report.source || report.target!=world.Player || report.sightTime>world.Clock || world.Clock-report.sightTime>reportFreshness || reports.Contains(report.id))return false;
        reports.Add(report.id);
        // Never read another monster's live target or enter Attack from a report.
        if(State==Behaviour.Attack)return true;
        if(WatchingPlayer){reportedAttention=true;return true;}
        BeginNotice(report.position,true);return true;
    }
    void ReportSight(bool sees)
    {
        if(State!=Behaviour.Attack || !sees || world.Clock-lastReportAt<.5f)return;
        lastReportAt=world.Clock;
        world.PublishReport(new Report{source=this,target=world.Player,position=target,sightTime=world.Clock,attack=true,id=++reportSequence,eventType=ReportEvent.ConfirmedTarget},reportRadius);
    }
    void SetState(Behaviour next)
    {
        WatchingHighFlyer=false;if(State==next)return;
        if(patrolRoute)patrolRoute.Suspend();currentState=next;
        disengaging=false;disengageRemaining=0;
        waking=noticing=watching=checking=false;wakeRemaining=noticeRemaining=inspectionRemaining=watchElapsed=detectionProgress=0;
        stopped=reverse=0;waveAction=-1;CancelReturn();
        if(next!=Behaviour.Attack){attackWaveBoost=false;waterGunPriority=false;Attack.CancelAttack();}
        if(next==Behaviour.Sleeping){sleepingBeforeWave=false;waveActive=false;waveDelay=waveMove=waveHold=waveRecovery=0;Motor.Facing=SpawnFacingSign;}
        trackedPitch=waveActive?waveSightPitch:roamingSightPitch;
    }
    void EnterAttack()
    {
        if(State==Behaviour.Attack)return;
        bool boosted=waveActive || attackWaveBoost;SetState(Behaviour.Attack);attackWaveBoost=boosted;chaseElapsed=safeElapsed=0;
        target=Senses.TargetPosition;lastSight=world.Clock;TrackPlayer();
    }
    void TrackPosition(Vector2 p)
    {Motor.LookAt(p.x);Vector2 diff=p-(Position+Vector2.up*.45f);trackedPitch=Mathf.Atan2(diff.y,Mathf.Abs(diff.x))*Mathf.Rad2Deg;}
    void TrackPlayer(){TrackPosition(Senses.TargetPosition);}
    void CancelReturn(){returning=waitingToSleep=false;sleepRemaining=0;}
    void BeginReturn(bool sleep)
    {
        SetState(Behaviour.Roaming);watching=checking=noticing=waking=false;detectionProgress=inspectionRemaining=0;
        disengaging=false;disengageRemaining=0;
        returning=true;waitingToSleep=false;returnAsSleeping=sleep;sleepRemaining=0;reportedAttention=false;
        if(sleep)returnPosition=sleepPosition;if(patrolRoute)patrolRoute.Suspend();
    }
    void FinishSoundInspection()
    {inspectionRemaining=0;checking=false;BeginReturn(attentionFromSleeping || returnToSleepAfterSound);}
    void CheckBrush()
    {
        var p=world.Player;bool overlap=Motor.Body.Overlaps(new Rect(p.Position.x-.32f,p.Position.y-p.BodyHeight*.5f,.64f,p.BodyHeight));
        if(overlap && !brushing)BeginNotice(p.Position,false);
        brushing=overlap;
    }
    bool QuietTarget(float dt)
    {
        if(Senses.TargetIsChild && world.Child)
        {return world.Child.SilentWalking || (world.Child.Position-previousChild).sqrMagnitude<=dt*dt*.0225f;}
        return world.Player.SilentWalking || world.Player.Crouched || (world.Player.Position-previousPlayer).sqrMagnitude<=dt*dt*.0225f;
    }
    bool GroundRunning(float dt)
    {
        if(Senses.TargetIsChild && world.Child)
            return world.Child.Grounded && !world.Child.SilentWalking && Mathf.Abs(world.Child.Position.x-previousChild.x)>dt*.15f;
        return world.Player.Grounded && !world.Player.SilentWalking && !world.Player.Crouched && Mathf.Abs(world.Player.HorizontalIntent)>.1f && !world.Player.MovementBlocked;
    }
    void UpdateWatching(RampagerPerception.Observation observation,float dt)
    {
        watchElapsed+=dt;
        if(disengaging && observation.sees){disengaging=false;disengageRemaining=0;}
        if(observation.seesPlayer && (observation.shot || observation.interactionStarted ||
            observation.interaction && observation.interactionElapsed>continuingInteractionAttackTime+.000001f))
        {Senses.SetTargetChild(false);detectionProgress=1;EnterAttack();return;}
        if(observation.sees && GroundRunning(dt))detectionProgress=Mathf.Min(1,detectionProgress+dt*(reportedAttention?reportedAngerMultiplier:1)/Mathf.Max(.01f,runningAngerTime));
        else detectionProgress=Mathf.Max(0,detectionProgress-dt/Mathf.Max(.01f,angerDecayTime));
        if(detectionProgress>=1-.000001f){EnterAttack();return;}
        if(!disengaging && !observation.sees)
        {
            disengaging=true;disengageRemaining=watchLostSightTime;
        }
        if(disengaging)
        {
            disengageRemaining=Mathf.Max(0,disengageRemaining-dt);
            if(disengageRemaining<=.000001f)BeginReturn(attentionFromSleeping);
        }
    }
    // Broadcasts are modifiers and movement commands, not a fourth behaviour state.
    public bool ReceiveBuildingWarning(float duration)
    {
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
        stopped=reverse=0;waveAction=-1;waveDelay=world.SoundWaves.moveDelay;waveMove=world.SoundWaves.commandDuration;waveHold=world.SoundWaves.holdDuration;waveRecovery=0;
        if(State!=Behaviour.Attack)Motor.Facing=waveDirection;return true;
    }
    public void EndSwarm(float duration){if(waveActive)waveRecovery=Mathf.Max(.01f,duration);}
    void AdvanceWave(float dt,out float delayDt,out float moveDt,out float holdDt)
    {
        delayDt=moveDt=holdDt=0;if(!waveActive)return;
        if(waveRecovery>0)
        {
            waveRecovery=Mathf.Max(0,waveRecovery-dt);
            if(waveRecovery<=0)
            {
                bool restoreSleep=sleepingBeforeWave;sleepingBeforeWave=false;
                stopped=reverse=0;waveAction=-1;waveActive=false;waveDelay=waveMove=waveHold=0;
                if(restoreSleep)
                {
                    if(State==Behaviour.Attack){SetState(Behaviour.Roaming);BeginWatching();attentionFromSleeping=true;}
                    else SetState(Behaviour.Sleeping);
                }
                else if(State==Behaviour.Roaming){if(watching)BeginWatching();else FinishSoundInspection();}
            }
            return;
        }
        delayDt=Mathf.Min(dt,waveDelay);waveDelay-=delayDt;
        moveDt=Mathf.Min(dt-delayDt,waveMove);waveMove-=moveDt;
        holdDt=dt-delayDt-moveDt;waveHold=Mathf.Max(0,waveHold-holdDt);
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
        if(stopped>=stoppedTolerance-.000001f || reverse>=reverseTolerance-.000001f)EnterAttack();
    }
    void RememberPositions()
    {previousPlayer=world.Player.Position;previousChild=world.Child?world.Child.Position:Vector2.zero;}
    public void Simulate(float dt)
    {
        if(!world || dt<=0)return;
        stun=Mathf.Max(0,stun-dt);float delayDt,moveDt,holdDt;AdvanceWave(dt,out delayDt,out moveDt,out holdDt);
        if(!waveActive)CheckBrush();
        if(State==Behaviour.Sleeping)
        {
            if(waking){wakeRemaining=Mathf.Max(0,wakeRemaining-dt);if(wakeRemaining<=.000001f)CompleteNotice();}
            if(State==Behaviour.Sleeping){Motor.SimulateVertical(dt);RememberPositions();return;}
        }
        if(noticing && !waveActive)
        {
            noticeRemaining=Mathf.Max(0,noticeRemaining-dt);
            if(noticeRemaining<=.000001f)CompleteNotice();
        }
        // Ordinary attention follows the player throughout the reaction/inspection, even after crossing sides.
        // Sleeping monsters and report recipients still inspect the heard/reported location first.
        if(!waveActive && (noticing || checking) && !reportedAttention && !attentionFromSleeping)
            TrackPosition(world.Player.Position);
        if((WatchingPlayer || State==Behaviour.Attack) && Senses.LocateTarget())TrackPlayer();
        Senses.Sense();var observation=Senses.LastObservation;bool sees=observation.sees;
        if(sees){target=Senses.TargetPosition;lastSight=world.Clock;}
        if(noticing && !waveActive)
        {
            // Reaction delays must not make a roaming monster blind to a visible gunshot.
            if(observation.seesPlayer && observation.shot){Senses.SetTargetChild(false);EnterAttack();}
            else{Motor.SimulateVertical(dt);RememberPositions();return;}
        }
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
            if(observation.seesPlayer && (world.Player.Violating || observation.shot || observation.interaction || observation.highFall))EnterAttack();
            else{CheckWaveIntent(observation,delayDt,false);CheckWaveIntent(observation,moveDt,true);CheckWaveIntent(observation,holdDt,false);}
        }
        else
        {
            bool alreadyWatching=WatchingPlayer;
            if(!alreadyWatching && observation.seesPlayer && observation.shot)
            {Senses.SetTargetChild(false);EnterAttack();}
            else
            {
                if(checking)
                {
                    if(sees)BeginWatching();
                    else{inspectionRemaining=Mathf.Max(0,inspectionRemaining-dt);if(inspectionRemaining<=0)FinishSoundInspection();}
                }
                if(WatchingPlayer)
                {
                    // An action first seen before watching is not a new interaction under an established gaze.
                    if(!alreadyWatching)observation.interactionStarted=false;
                    UpdateWatching(observation,dt);
                }
                else if(!checking && (observation.interaction || observation.seesPlayer && world.Player.OnLadder))
                    BeginNotice(world.Player.Position,false);
            }
        }
        Attack.Simulate(dt,State==Behaviour.Attack && stun<=0);
        float direction=0,distance=0;bool turnOnBlock=false;
        WatchingHighFlyer=State==Behaviour.Attack && sees && !Senses.TargetIsChild && world.Player.Flying &&
            world.Player.Position.y-world.Player.BodyHeight*.5f-(Position.y-.8f)>world.Player.StandingHeight*highFlightStopBodies;
        if(WatchingHighFlyer)TrackPlayer();
        if(State==Behaviour.Attack && !WatchingHighFlyer)
        {direction=Motor.Toward(target.x);distance=CurrentMoveSpeed*dt;}
        else if(State!=Behaviour.Attack && waveActive)
        {if(moveDt>0){direction=waveDirection;distance=CurrentMoveSpeed*moveDt;}}
        else if(WatchingPlayer)
        {
            if(sees && Mathf.Abs(target.x-Position.x)>watchStandOff)
            {direction=Mathf.Sign(target.x-Position.x);distance=Mathf.Min(Mathf.Abs(target.x-Position.x)-watchStandOff,CurrentMoveSpeed*dt);}
        }
        else if(State==Behaviour.Roaming && !checking && !noticing)
        {
            if(returning || waitingToSleep)
            {
                float delta=returnPosition.x-Position.x;
                if(Mathf.Abs(delta)>.025f)
                {returning=true;waitingToSleep=false;sleepRemaining=0;direction=Mathf.Sign(delta);distance=Mathf.Min(Mathf.Abs(delta),CurrentMoveSpeed*dt);}
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
                float look=Facing;bool route=patrolRoute && patrolRoute.Evaluate(Position,stun>0?0:dt,Facing,CurrentMoveSpeed,out direction,out distance,out look);
                if(route)Motor.Facing=look;
                else{direction=Facing;if(Position.x<=Motor.Left)direction=1;else if(Position.x>=Motor.Right)direction=-1;distance=CurrentMoveSpeed*dt;turnOnBlock=true;}
            }
        }
        bool blocked=false;
        if(!Attack.MovementLocked && stun<=0 && direction!=0)blocked=!Motor.Move(direction,distance,turnOnBlock);
        if(SwarmAttackBoost && !Attack.MovementLocked && stun<=0 && !WatchingHighFlyer)Motor.TryJump(target,blocked);
        Motor.SimulateVertical(dt);if(!Attack.MovementLocked)Attack.TryAttack(State==Behaviour.Attack && stun<=0);
        RememberPositions();
    }
    public void SyncVisuals()
    {
        transform.position=Position;if(visuals)visuals.ClearTremble();
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
