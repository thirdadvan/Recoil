using UnityEngine;

[DefaultExecutionOrder(-700),DisallowMultipleComponent,AddComponentMenu("Recoil/KBK Stacker Crane")]
public sealed class KBKStackerCrane : MonoBehaviour
{
    public enum MotionPhase { Idle, Raising, Travelling, Lowering }
    public Transform startPoint,destinationPoint,travelRig,liftCarriage;
    public SoftRoofPlatform forkPlatform;
    public SpriteRenderer statusLamp;
    public string requiredPowerItem="";
    public bool HasPower {get{if(string.IsNullOrEmpty(requiredPowerItem))return true;if(!world)world=GetComponentInParent<RecoilPrototype>();return world && world.HasItem(requiredPowerItem);}}
    [Min(.1f)] public float liftHeight=3.4f,liftDuration=2,travelDuration=5,lowerDuration=2;
    [Tooltip("落点再次操作开关时，按同样的升起、移动、下降顺序返回起点。")]
    public bool allowReturn=true;
    public MotionPhase Phase {get;private set;}
    public bool AtDestination {get;private set;}
    public bool Busy {get{return Phase!=MotionPhase.Idle;}}
    float phaseElapsed;bool targetDestination;
    Vector2 from,to;
    RecoilPrototype world;
    Vector3 rigOffset,liftOffset;
    bool captured;
    void Capture(){if(captured||!travelRig||!liftCarriage)return;rigOffset=travelRig.position-(startPoint?startPoint.position:transform.position);liftOffset=liftCarriage.localPosition;captured=true;}
    public void ResetMachine(){var noise=GetComponent<FreightNoiseEmitter>();if(noise)noise.ResetNoise();Capture();Phase=MotionPhase.Idle;AtDestination=false;phaseElapsed=0;ApplyPose(startPoint? (Vector2)startPoint.position:(Vector2)transform.position,0);UpdateLamp();}
    public bool Activate()
    {
        if(!HasPower || Busy || !startPoint || !destinationPoint || !travelRig || !liftCarriage || (AtDestination&&!allowReturn))return false;
        Capture();targetDestination=!AtDestination;from=AtDestination?(Vector2)destinationPoint.position:(Vector2)startPoint.position;to=targetDestination?(Vector2)destinationPoint.position:(Vector2)startPoint.position;
        phaseElapsed=0;Phase=MotionPhase.Raising;UpdateLamp();return true;
    }
    void Update(){if(!world)world=GetComponentInParent<RecoilPrototype>();if(world && world.Playing)Simulate(world.FrameDelta,world);UpdateLamp();}
    public void Simulate(float dt,RecoilPrototype context=null)
    {
        if(!HasPower || !Busy || dt<=0)return;Capture();Rect previous=forkPlatform?forkPlatform.Area:default(Rect);Vector2 before=forkPlatform?(Vector2)forkPlatform.transform.position:Vector2.zero;
        phaseElapsed+=dt;float duration=Phase==MotionPhase.Travelling?travelDuration:Phase==MotionPhase.Raising?liftDuration:lowerDuration;
        float t=Mathf.Clamp01(phaseElapsed/Mathf.Max(.1f,duration)),blend=t*t*(3-2*t);
        if(Phase==MotionPhase.Raising)ApplyPose(from,liftHeight*blend);
        else if(Phase==MotionPhase.Travelling)ApplyPose(Vector2.Lerp(from,to,blend),liftHeight);
        else ApplyPose(to,liftHeight*(1-blend));
        Physics2D.SyncTransforms();
        if(forkPlatform && context && context.Player)context.Player.CarryBySoftPlatform(forkPlatform,previous,(Vector2)forkPlatform.transform.position-before);
        if(forkPlatform && context && context.Child)context.Child.CarryBySoftPlatform(forkPlatform,previous,(Vector2)forkPlatform.transform.position-before);
        if(t>=1){phaseElapsed=0;if(Phase==MotionPhase.Raising)Phase=MotionPhase.Travelling;else if(Phase==MotionPhase.Travelling)Phase=MotionPhase.Lowering;else{Phase=MotionPhase.Idle;AtDestination=targetDestination;}}
        UpdateLamp();
    }
    void ApplyPose(Vector2 floor,float elevation){if(!captured)return;travelRig.position=(Vector3)floor+rigOffset;liftCarriage.localPosition=liftOffset+Vector3.up*elevation;}
    void UpdateLamp(){if(statusLamp)statusLamp.color=RecoilPrototype.Hex(!HasPower?0x352d28:Busy?0xd7a64b:0x53db79);}
    void OnDrawGizmosSelected(){if(!startPoint||!destinationPoint)return;Gizmos.color=new Color(1,.7f,.25f,.8f);Vector3 a=startPoint.position,b=destinationPoint.position;Gizmos.DrawLine(a,a+Vector3.up*liftHeight);Gizmos.DrawLine(a+Vector3.up*liftHeight,b+Vector3.up*liftHeight);Gizmos.DrawLine(b+Vector3.up*liftHeight,b);}
}
