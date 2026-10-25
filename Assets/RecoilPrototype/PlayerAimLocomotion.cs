using UnityEngine;

// Optional tuning layer, called by the existing player controller. No independent update or rigidbody.
[DisallowMultipleComponent,RequireComponent(typeof(RecoilPlayerController))]
[AddComponentMenu("Recoil/Player Aim and Locomotion")]
public sealed class PlayerAimLocomotion : MonoBehaviour
{
    [Header("Body facing")]
    [Tooltip("关闭时身体快速跟随鼠标；开启时由左右移动输入决定朝向。")]
    public bool movementControlsFacing=false;
    [Min(1),Tooltip("身体每秒转动角度，1440约为180度/0.125秒。独立于刹停和加速。")]
    public float bodyTurnSpeed=1440;
    [Header("Ground aiming arc")]
    public bool limitGroundAim=false;
    [Range(90,180),Tooltip("以身体正前方水平线为0度；105表示上下可抬到105度，只允许略微指向身后。游戏手感值，不等同于解剖测量。")]
    public float movingAimHalfAngle=105;
    [Range(90,180)] public float standingAimHalfAngle=125;
    [Tooltip("飞行、下落、梯子、铁链、把手上保留全方向瞄准和后座力。")]
    public bool unrestrictedAirAim=true;
    [Range(0,25),Tooltip("鼠标在正后方附近时保留上一次抬枪侧，避免上下翻跳。")]
    public float rearAimDeadZone=10;
    [Header("Ground acceleration — seconds from/to full speed")]
    public bool useGroundBraking=true;
    [Min(.01f)] public float accelerationTime=.14f;
    [Min(.01f)] public float stopTime=.12f;
    [Min(.01f)] public float reversalBrakeTime=.09f;
    public float FacingSign {get;private set;}=1;
    public bool IsBraking {get;private set;}
    public bool AimLimited {get;private set;}
    public float RequestedLocalAimAngle {get;private set;}
    public float ActualLocalAimAngle {get;private set;}
    float boundarySide=1;
    public void ResetTuning(){FacingSign=boundarySide=1;IsBraking=AimLimited=false;RequestedLocalAimAngle=ActualLocalAimAngle=0;}
    public void UpdateFacing(float horizontal,Vector2 requestedAim)
    {
        if(movementControlsFacing){if(Mathf.Abs(horizontal)>.1f)FacingSign=Mathf.Sign(horizontal);}
        else if(Mathf.Abs(requestedAim.x)>.01f)FacingSign=Mathf.Sign(requestedAim.x);
    }
    public Vector2 ResolveAim(Vector2 requested,bool supported,bool freePose,bool moving)
    {
        if(requested.sqrMagnitude<.00001f)requested=new Vector2(FacingSign,0);requested.Normalize();
        float angle=Mathf.Atan2(requested.y,requested.x*FacingSign)*Mathf.Rad2Deg;
        RequestedLocalAimAngle=angle;AimLimited=false;ActualLocalAimAngle=angle;
        if(!limitGroundAim||unrestrictedAirAim&&(!supported||freePose))return requested;
        float limit=Mathf.Clamp(moving?movingAimHalfAngle:standingAimHalfAngle,90,180);
        if(Mathf.Abs(angle)<=limit){if(Mathf.Abs(angle)>2)boundarySide=Mathf.Sign(angle);return requested;}
        if(Mathf.Abs(angle)<180-rearAimDeadZone)boundarySide=Mathf.Sign(angle);
        float clamped=boundarySide*limit;ActualLocalAimAngle=clamped;AimLimited=true;
        float radians=clamped*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(radians)*FacingSign,Mathf.Sin(radians));
    }
    public float AdvanceHorizontal(float current,float input,float maximum,float dt,bool onGround)
    {
        IsBraking=false;if(dt<=0)return current;
        if(!useGroundBraking||!onGround)return Mathf.MoveTowards(current,input*maximum,22*dt);
        float target=input*maximum;
        if(Mathf.Abs(input)>.1f&&current*input<0)
        {
            IsBraking=true;
            return Mathf.MoveTowards(current,0,Mathf.Max(maximum,Mathf.Abs(current))/Mathf.Max(.01f,reversalBrakeTime)*dt);
        }
        float duration=Mathf.Abs(target)<Mathf.Abs(current)?stopTime:accelerationTime;
        return Mathf.MoveTowards(current,target,Mathf.Max(maximum,Mathf.Abs(current))/Mathf.Max(.01f,duration)*dt);
    }
}
