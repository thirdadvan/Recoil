using UnityEngine;

// Visual joints only: never changes collision, movement, recoil or noise.
[DisallowMultipleComponent,RequireComponent(typeof(Recoil3DActorVisual))]
[AddComponentMenu("Recoil/3D Player Leg Pose")]
public sealed class Recoil3DPlayerLegPose : MonoBehaviour
{
    public Transform leftHip,rightHip,leftKnee,rightKnee;
    [Min(.2f),Tooltip("悬浮/下落时蹬车一圈的秒数。")]
    public float pedalPeriod=2.4f;
    [Range(0,90)] public float pedalHipSwing=30;
    [Range(0,90)] public float pedalKneeBend=45;
    [Header("Recoil acceleration — one leg raised, one extended")]
    public float raisedHipAngle=65,raisedKneeAngle=-85;
    public float extendedHipAngle=-18,extendedKneeAngle=-12;
    [Min(1),Tooltip("姿态过渡速度；落地时平滑收回蹬车姿态。")]
    public float blendSpeed=12;
    public enum LegState { Grounded,Recoil,Pedalling,Attached,Departing }
    public LegState CurrentState {get;private set;}
    float pedalPhase;
    Vector4 angles;

    // Called once by ActorVisual, using the same pause-aware time step.
    public void ApplyPose(RecoilPlayerController player,Vector3 delta,float stride,float dt)
    {
        if(!leftHip||!rightHip||!leftKnee||!rightKnee)return;
        Vector4 target;
        if(player.DepthPlane && player.DepthPlane.Departing){CurrentState=LegState.Departing;target=new Vector4(25,-15,-40,-25);}
        else if(player.Climbing||player.HoldingHandhold)
        {
            CurrentState=LegState.Attached;
            target=new Vector4(12,-8,-18,-12);
        }
        else if(player.Grounded||player.WalkingStairs)
        {
            CurrentState=LegState.Grounded;
            bool moving=(Mathf.Abs(delta.x)>.0001f || (player.DepthPlane && player.DepthPlane.StairEntering && Mathf.Abs(delta.z)>.0001f)) && delta.sqrMagnitude<1;
            float swing=moving?Mathf.Sin(stride)*(player.Running?42:22):0;
            // Signed stride runs the knee/hip cycle backwards while backpedalling.
            target=new Vector4(swing,-swing,
                moving?-Mathf.Max(0,-Mathf.Cos(stride))*(player.Running?58:26):0,
                moving?-Mathf.Max(0,Mathf.Cos(stride))*(player.Running?58:26):0);
            if(player.AimLocomotion && player.AimLocomotion.IsBraking)
                target=new Vector4(-12,14,-8,-18);
        }
        else if(player.RecoilAccelerating)
        {
            CurrentState=LegState.Recoil;
            target=new Vector4(raisedHipAngle,extendedHipAngle,raisedKneeAngle,extendedKneeAngle);
        }
        else
        {
            CurrentState=LegState.Pedalling;
            pedalPhase=Mathf.Repeat(pedalPhase+dt*Mathf.PI*2/Mathf.Max(.2f,pedalPeriod),Mathf.PI*2);
            float cycle=Mathf.Sin(pedalPhase),bend=Mathf.Cos(pedalPhase);
            target=new Vector4(15+cycle*pedalHipSwing,15-cycle*pedalHipSwing,
                -pedalKneeBend-bend*25,-pedalKneeBend+bend*25);
        }
        angles=Vector4.Lerp(angles,target,1-Mathf.Exp(-blendSpeed*Mathf.Max(0,dt)));
        leftHip.localRotation=Quaternion.Euler(angles.x,0,0);
        rightHip.localRotation=Quaternion.Euler(angles.y,0,0);
        leftKnee.localRotation=Quaternion.Euler(angles.z,0,0);
        rightKnee.localRotation=Quaternion.Euler(angles.w,0,0);
        if(player.Grounded&&!player.WalkingStairs&&(!player.DepthPlane||!player.DepthPlane.Departing))
        {
            PlantFoot(player,leftHip,leftKnee,Mathf.Cos(stride)>.05f);
            PlantFoot(player,rightHip,rightKnee,Mathf.Cos(stride)<-.05f);
        }
    }
    static void PlantFoot(RecoilPlayerController player,Transform hip,Transform knee,bool stance)
    {
        Vector3 foot=knee.TransformPoint(new Vector3(0,-.26f,0));float ground;
        if(!player.TryGetFootSupport(foot,out ground))return;
        if(!stance&&player.LowStepProgress>=1&&foot.y>=ground+.008f)return;
        var target=foot;target.y=ground+.006f;
        // During the step the advancing foot lifts above the lip, then settles onto it.
        if(player.LowStepProgress<1&&ground>player.WorldPosition.y-player.BodyHeight*.5f-.04f)
            target.y+=Mathf.Sin(player.LowStepProgress*Mathf.PI)*.06f;
        Vector3 local=hip.parent.InverseTransformPoint(target)-hip.localPosition;
        float upper=knee.localPosition.magnitude,lower=.26f,d=Mathf.Clamp(new Vector2(local.y,local.z).magnitude,.04f,upper+lower-.001f);
        float direction=Mathf.Atan2(-local.z,-local.y);
        float shoulder=Mathf.Acos(Mathf.Clamp((upper*upper+d*d-lower*lower)/(2*upper*d),-1,1));
        float bend=Mathf.PI-Mathf.Acos(Mathf.Clamp((upper*upper+lower*lower-d*d)/(2*upper*lower),-1,1));
        hip.localRotation=Quaternion.Euler((direction+shoulder)*Mathf.Rad2Deg,0,0);
        knee.localRotation=Quaternion.Euler(-bend*Mathf.Rad2Deg,0,0);
    }
}
