using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RampagerPerception : MonoBehaviour
{
    [Min(.1f)] public float idealSightRange=11.946668f;
    [Range(10,180)] public float fieldOfView=100;
    [Range(0,1)] public float minimumExposure=.34f;
    [Header("Rear perception")]
    [Range(0,180),Tooltip("以正后方为中心的感知弱区总角度。")]
    public float rearSenseAngle=60;
    [Range(0,1),Tooltip("背后弱区保留的听觉与近距离感知范围；不改变实际攻击或融合触须长度。")]
    public float rearSenseRangeMultiplier=.6f;
    public struct Observation
    {public bool sees,seesPlayer,seesChild,shot,interaction,interactionStarted,highFall;public float interactionElapsed;}
    public event Action<Vector2> PlayerNoiseHeard;
    public Observation LastObservation {get;private set;}
    RecoilPrototype world;RampagerController owner;RampagerMovement movement;
    int observedShot,observedInteraction,observedFall;
    ContinuousInteractionItem observedHeldItem;int observedHeldSession=-1;
    public bool TargetIsChild {get;private set;}
    public Vector2 TargetPosition {get{return TargetIsChild && world.Child && world.Child.Alive?world.Child.Position:world.Player.Position;}}
    public Vector3 TargetWorldPosition {get{return TargetIsChild && world.Child && world.Child.Alive?world.Child.transform.position:world.Player.WorldPosition;}}
    float RangeFor(float illumination){return idealSightRange*illumination*(owner.State==RampagerController.Behaviour.Attack?1.5f:owner.WatchingPlayer?owner.watchSightMultiplier:1);}
    // Before runtime initialization, the Scene view previews the configured ideal range.
    public float SightRange {get{return world && world.Player && owner?RangeFor(TargetIsChild && world.Child && world.Child.Alive?world.Child.Illumination:world.Player.Illumination):idealSightRange;}}
    public void Initialize(RecoilPrototype context,RampagerController controller,RampagerMovement motor)
    {world=context;owner=controller;movement=motor;ResetPerception();}
    public void ResetPerception()
    {observedShot=observedInteraction=observedFall=0;observedHeldItem=null;observedHeldSession=-1;TargetIsChild=false;LastObservation=default(Observation);}
    public float PerceptionRangeMultiplier(Vector3 source)
    {
        Vector3 offset=source-transform.position;offset.y=0;
        if(offset.sqrMagnitude<.000001f || rearSenseAngle<=0)return 1;
        // Use the visible body heading, including its turn time, not the requested facing.
        float yaw=movement.VisualYaw*Mathf.Deg2Rad;
        Vector3 forward=new Vector3(-Mathf.Sin(yaw),0,-Mathf.Cos(yaw));
        return Vector3.Dot(offset.normalized,-forward)>=Mathf.Cos(rearSenseAngle*.5f*Mathf.Deg2Rad)-.000001f?rearSenseRangeMultiplier:1;
    }
    bool CanSenseClose(Vector3 center)
    {return CanTouch(center,owner.CloseSenseRadius*PerceptionRangeMultiplier(center));}
    bool CanSee(Vector3 center,float half,float illumination)
    {
        if(!world || !owner || owner.State==RampagerController.Behaviour.Sleeping)return false;
        if(!owner.WaveActive)return CanSenseClose(center);
        if(owner.State==RampagerController.Behaviour.Attack && CanTouch(center,owner.contactRadius))return true;
        Vector2 eye=movement.EyePosition,diff=(Vector2)center-eye;
        Vector3 depthDiff=new Vector3(diff.x,diff.y,center.z-movement.MovementPlaneZ);
        float distance=owner.Uses3DNavigation?depthDiff.magnitude:diff.magnitude;
        float facing=owner.Uses3DNavigation?Vector3.Dot(depthDiff.normalized,owner.SightDirection3D):Vector2.Dot(diff.normalized,owner.SightDirection);
        if(distance>RangeFor(illumination) || facing<Mathf.Cos(fieldOfView*.5f*Mathf.Deg2Rad))return false;
        int exposed=0;
        for(int i=-1;i<=1;i++)if(world.Clear(eye,(Vector2)center+Vector2.up*(half*i)))exposed++;
        return exposed/3f>=minimumExposure;
    }
    public bool CanSeePlayer(){return world && world.Player && CanSee(world.Player.WorldPosition,.65f,world.Player.Illumination);}
    public bool CanSeeChild(){return world && world.Child && world.Child.Alive && world.Child.isActiveAndEnabled && CanSee(world.Child.transform.position,world.Child.bodyHeight*.4f,world.Child.Illumination);}
    bool CanLocate(Vector3 p,float illumination){if(!owner.WaveActive)return CanSenseClose(p);Vector2 eye=movement.EyePosition;return Vector3.Distance(new Vector3(eye.x,eye.y,movement.MovementPlaneZ),(Vector3)p)<=RangeFor(illumination)&&world.Clear(eye,(Vector2)p);}
    public bool CanLocatePlayer(){return CanLocate(world.Player.WorldPosition,world.Player.Illumination);}
    // Attack tracking may turn toward either visible actor, selecting the nearest reachable one.
    public bool LocateTarget()
    {
        if(owner.PrioritizingWaterGunHolder){TargetIsChild=false;return CanLocatePlayer();}
        bool p=CanLocatePlayer(),c=world.Child&&world.Child.Alive&&world.Child.isActiveAndEnabled&&CanLocate(world.Child.transform.position,world.Child.Illumination);
        if(!p&&!c)return false;
        SelectTarget(p,c);return true;
    }
    void SelectTarget(bool p,bool c){TargetIsChild=!owner.PrioritizingWaterGunHolder&&c&&(!p||(world.Child.transform.position-transform.position).sqrMagnitude<(world.Player.WorldPosition-transform.position).sqrMagnitude);}
    public void SetTargetChild(bool child){TargetIsChild=child&&world.Child&&world.Child.Alive;}
    public void HearNoise(Vector2 source,float radius)
    {if(Vector2.Distance(movement.Position,source)<=radius)PlayerNoiseHeard?.Invoke(source);}
    public bool WaterHits(Vector2 start,Vector2 direction,float range)
    {var center=movement.Body.center;float along=Vector2.Dot(center-start,direction);return along>0 && along<range+.2f && new Vector2(Vector2.Distance(center,start+direction*along),movement.MovementPlaneZ-world.Player.GameplayZ).magnitude<.75f*movement.bodyScale;}
    public bool CanTouchPlayer(float radius)
    {return CanTouch(world.Player.WorldPosition,radius);}
    bool CanTouch(Vector3 center,float radius){return Vector3.Distance(new Vector3(movement.Position.x,movement.Position.y,movement.MovementPlaneZ),(Vector3)center)<radius&&world.Clear(movement.Position,(Vector2)center);}
    public bool ChildIsCloser {get{return world.Child && (world.Child.transform.position-transform.position).sqrMagnitude<(world.Player.WorldPosition-transform.position).sqrMagnitude;}}
    public bool CanTouchChild(float radius){return world.Child&&world.Child.Alive&&world.Child.isActiveAndEnabled&&CanTouch(world.Child.transform.position,radius);}
    public void Sense()
    {
        bool sees=CanSeePlayer(),child=CanSeeChild();SelectTarget(sees,child);var result=new Observation{sees=TargetIsChild?child:sees,seesPlayer=sees,seesChild=child};
        result.shot=sees && world.Clock-world.Player.LastShot<.18f && world.Player.ShotSequence!=observedShot;
        var held=world.ActiveInteraction;int session=held?held.Session:-1;
        bool beganHeld=held && (held!=observedHeldItem || session!=observedHeldSession);
        bool notified=world.Player.RecentInteraction && world.Player.InteractionSequence!=observedInteraction;
        result.interaction=sees && (notified || held);
        result.interactionStarted=sees && (notified || beganHeld);
        result.interactionElapsed=held?held.Progress:0;
        observedHeldItem=held;observedHeldSession=session;
        result.highFall=sees && world.Player.HighFall && world.Player.FallSequence!=observedFall;
        if(result.shot)observedShot=world.Player.ShotSequence;
        if(result.interaction)observedInteraction=world.Player.InteractionSequence;
        if(result.highFall)observedFall=world.Player.FallSequence;
        LastObservation=result;
    }
}
