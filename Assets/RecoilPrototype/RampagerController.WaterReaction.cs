using UnityEngine;

public sealed partial class RampagerController
{
    [Header("Water impact reaction")]
    [Min(.1f)] public float waterBlockDuration=1.2f;
    [HideInInspector] public float exposedCoreRecoveryDuration=4;
    [Min(.1f)] public float coreFoldDuration=1,coreStillDuration=2.5f,coreAttackTransitionDuration=.5f;
    public float CoreReactionTotal {get{return coreFoldDuration+coreStillDuration+coreAttackTransitionDuration;}}
    public float CoreReactionElapsed {get{return ProtectingCore?waterReactionDuration-waterReactionRemaining:0;}}
    public float CoreFoldProgress {get{return ProtectingCore?Mathf.Clamp01(CoreReactionElapsed/Mathf.Max(.01f,coreFoldDuration)):0;}}
    public float CoreAttackProgress {get{return ProtectingCore?Mathf.Clamp01((CoreReactionElapsed-coreFoldDuration-coreStillDuration)/Mathf.Max(.01f,coreAttackTransitionDuration)):0;}}
    public bool CoreStill {get{return ProtectingCore&&CoreReactionElapsed>=coreFoldDuration&&CoreAttackProgress<=0;}}
    float waterReactionRemaining,waterReactionDuration;
    bool waterCoreHit;
    public bool ReactingToWater {get{return waterReactionRemaining>0;}}
    public bool ProtectingCore {get{return ReactingToWater&&waterCoreHit;}}
    public float WaterReactionProgress {get{return ReactingToWater?Mathf.Clamp01(1-waterReactionRemaining/Mathf.Max(.01f,waterReactionDuration)):0;}}
    // Core protection stays black; normal attack colour resumes after the final transition.
    public bool WaterForcesRed {get{return false;}}
    public bool TryWaterIntersection(Vector2 start,Vector2 direction,float range,out float distance)
    {
        distance=range;if(!isActiveAndEnabled||!world||!world.Player)return false;
        Vector2 centre=Motor.Body.center;
        return RecoilPrototype.WaterRayDistance(new Vector3(start.x,start.y,world.Player.GameplayZ-.1f),(Vector3)direction,new Vector3(centre.x,centre.y,Motor.MovementPlaneZ),.75f*Motor.bodyScale,range,out distance);
    }
    public void ReceiveWaterImpact(Vector2 start,Vector2 direction,float range,Vector2 source)
    {
        if(!world)return;
        soundInvestigationPriority=false;
        var art=GetComponent<RampagerTentacleVisual>();
        bool core=WatchingPlayer&&art&&art.WaterRayHitsCore(new Vector3(start.x,start.y,world.Player.GameplayZ-.1f),(Vector3)direction,range);
        if(State!=Behaviour.Attack&&!watching&&!noticing&&!waking){returnPosition=Position;returnDepth=Motor.MovementPlaneZ;}
        if(!ReactingToWater||!waterCoreHit){waterCoreHit=core;waterReactionDuration=core?CoreReactionTotal:waterBlockDuration;waterReactionRemaining=waterReactionDuration;}
        // An already protected core keeps its four-second sequence monotonic.
        // Further body impacts stop the stream but do not rewind the layered fold/hold.
        attackPreparationRemaining=0;waking=noticing=checking=false;wakeRemaining=noticeRemaining=0;CancelReturn();StopAttackSearch();
        if(State==Behaviour.Sleeping){SetState(Behaviour.Roaming);waterCoreHit=core;}
        Attack.CancelAttack();Senses.SetTargetChild(false);target=lastHeardPosition=source;targetDepth=lastHeardDepth=world.Player.GameplayZ;
        lastSoundAt=lastSight=world.Clock;waterGunPriority=true;
        if(patrolRoute)patrolRoute.Suspend();var grabbing=GetComponent<RampagerGrab>();if(grabbing)grabbing.Release();
        TrackPosition(LastKnownWorldPosition);if(depthArrival)depthArrival.Stop();
    }
    bool SimulateWaterReaction(float dt)
    {
        if(!ReactingToWater)return false;
        waterReactionRemaining=Mathf.Max(0,waterReactionRemaining-dt);
        Attack.CancelAttack();Motor.SimulateVertical(dt);if(depthArrival)depthArrival.Stop();
        if(!ReactingToWater){waterCoreHit=false;EnterAttack();waterGunPriority=true;lastSoundAt=world.Clock;}
        return true;
    }
    void ResetWaterReaction(){waterReactionRemaining=waterReactionDuration=0;waterCoreHit=false;}
}
