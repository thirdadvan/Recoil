using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class RampagerPerception : MonoBehaviour
{
    [Min(.1f)] public float idealSightRange=11.946668f;
    [Range(10,180)] public float fieldOfView=100;
    [Range(0,1)] public float minimumExposure=.34f;
    public struct Observation
    {public bool sees,seesPlayer,seesChild,shot,interaction,interactionStarted,highFall;public float interactionElapsed;}
    public event Action<Vector2> PlayerNoiseHeard;
    public Observation LastObservation {get;private set;}
    RecoilPrototype world;RampagerController owner;RampagerMovement movement;
    int observedShot,observedInteraction,observedFall;
    ContinuousInteractionItem observedHeldItem;int observedHeldSession=-1;
    public bool TargetIsChild {get;private set;}
    public Vector2 TargetPosition {get{return TargetIsChild && world.Child && world.Child.Alive?world.Child.Position:world.Player.Position;}}
    float RangeFor(float illumination){return idealSightRange*illumination*(owner.State==RampagerController.Behaviour.Attack?1.5f:owner.WatchingPlayer?owner.watchSightMultiplier:1);}
    // Before runtime initialization, the Scene view previews the configured ideal range.
    public float SightRange {get{return world && world.Player && owner?RangeFor(TargetIsChild && world.Child && world.Child.Alive?world.Child.Illumination:world.Player.Illumination):idealSightRange;}}
    public void Initialize(RecoilPrototype context,RampagerController controller,RampagerMovement motor)
    {world=context;owner=controller;movement=motor;ResetPerception();}
    public void ResetPerception()
    {observedShot=observedInteraction=observedFall=0;observedHeldItem=null;observedHeldSession=-1;TargetIsChild=false;LastObservation=default(Observation);}
    bool CanSee(Vector2 center,float half,float illumination)
    {
        if(!world || !owner || owner.State==RampagerController.Behaviour.Sleeping)return false;
        if(owner.State==RampagerController.Behaviour.Attack && CanTouch(center,owner.contactRadius))return true;
        Vector2 eye=movement.Position+Vector2.up*.45f,diff=center-eye;
        if(diff.magnitude>RangeFor(illumination) || Vector2.Dot(diff.normalized,owner.SightDirection)<Mathf.Cos(fieldOfView*.5f*Mathf.Deg2Rad))return false;
        int exposed=0;
        for(int i=-1;i<=1;i++)if(world.Clear(eye,center+Vector2.up*(half*i)))exposed++;
        return exposed/3f>=minimumExposure;
    }
    public bool CanSeePlayer(){return world && world.Player && CanSee(world.Player.Position,world.Player.Crouched?.35f:.65f,world.Player.Illumination);}
    public bool CanSeeChild(){return world && world.Child && world.Child.Alive && world.Child.isActiveAndEnabled && CanSee(world.Child.Position,world.Child.bodyHeight*.4f,world.Child.Illumination);}
    bool CanLocate(Vector2 p,float illumination){Vector2 eye=movement.Position+Vector2.up*.45f;return Vector2.Distance(eye,p)<=RangeFor(illumination)&&world.Clear(eye,p);}
    public bool CanLocatePlayer(){return CanLocate(world.Player.Position,world.Player.Illumination);}
    // Attack tracking may turn toward either visible actor, selecting the nearest reachable one.
    public bool LocateTarget()
    {
        if(owner.PrioritizingWaterGunHolder){TargetIsChild=false;return CanLocatePlayer();}
        bool p=CanLocatePlayer(),c=world.Child&&world.Child.Alive&&world.Child.isActiveAndEnabled&&CanLocate(world.Child.Position,world.Child.Illumination);
        if(!p&&!c)return false;
        SelectTarget(p,c);return true;
    }
    void SelectTarget(bool p,bool c){TargetIsChild=!owner.PrioritizingWaterGunHolder&&c&&(!p||(world.Child.Position-movement.Position).sqrMagnitude<(world.Player.Position-movement.Position).sqrMagnitude);}
    public void SetTargetChild(bool child){TargetIsChild=child&&world.Child&&world.Child.Alive;}
    public void HearNoise(Vector2 source,float radius)
    {if(Vector2.Distance(movement.Position,source)<=radius)PlayerNoiseHeard?.Invoke(source);}
    public bool WaterHits(Vector2 start,Vector2 direction,float range)
    {float along=Vector2.Dot(movement.Position-start,direction);return along>0 && along<range+.2f && Vector2.Distance(movement.Position,start+direction*along)<.75f;}
    public bool CanTouchPlayer(float radius)
    {return CanTouch(world.Player.Position,radius);}
    bool CanTouch(Vector2 center,float radius){return Vector2.Distance(movement.Position,center)<radius&&world.Clear(movement.Position,center);}
    public bool ChildIsCloser {get{return world.Child && (world.Child.Position-movement.Position).sqrMagnitude<(world.Player.Position-movement.Position).sqrMagnitude;}}
    public bool CanTouchChild(float radius){return world.Child&&world.Child.Alive&&world.Child.isActiveAndEnabled&&CanTouch(world.Child.Position,radius);}
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
