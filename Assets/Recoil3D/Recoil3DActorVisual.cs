using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(800),DisallowMultipleComponent]
public sealed class Recoil3DActorVisual : MonoBehaviour
{
    public Transform model,leftLeg,rightLeg,leftArm,rightArm,head;
    public Renderer[] parts;
    public Renderer eye;
    public SpriteRenderer visibilitySource;
    public bool child,monster;
    [UnityEngine.Serialization.FormerlySerializedAs("turnSpeed"),Tooltip("主角和孩子的转身速度。怪物使用 RampagerMovement 的 Turn Speed。") ] public float characterTurnSpeed=240;
    RampagerDepthArrival depthArrival;
    RampagerGrab grab;RampagerAttack attack;
    RampagerController ai;RecoilPlayerController player;EscortChild escort;
    Color[] baseColours;MaterialPropertyBlock block;
    SpriteRenderer[] legacySprites;
    Vector3 previous;float stride,yaw;
    Recoil3DPlayerLegPose playerLegPose;
    void OnEnable(){legacySprites=GetComponentsInChildren<SpriteRenderer>(true);foreach(var s in legacySprites)if(s && !s.name.StartsWith("Heart"))s.forceRenderingOff=true;}
    void Start()
    {ai=GetComponent<RampagerController>();grab=GetComponent<RampagerGrab>();attack=GetComponent<RampagerAttack>();depthArrival=GetComponent<RampagerDepthArrival>();player=GetComponent<RecoilPlayerController>();playerLegPose=GetComponent<Recoil3DPlayerLegPose>();escort=GetComponent<EscortChild>();legacySprites=GetComponentsInChildren<SpriteRenderer>(true);previous=transform.position;yaw=monster && ai?(ai.Facing>0?-90:90):-90;CacheColours();}
    static void ReachArm(Transform arm,Vector3 target)
    {if(arm)arm.localRotation=Quaternion.FromToRotation(Vector3.down,arm.parent.InverseTransformDirection(target-arm.position).normalized);}
    void CacheColours()
    {if(baseColours!=null)return;baseColours=new Color[parts.Length];for(int i=0;i<parts.Length;i++)baseColours[i]=parts[i].sharedMaterial.color;block=new MaterialPropertyBlock();}
    void LateUpdate()
    {
        if(monster){var tentacles=GetComponent<RampagerTentacleVisual>();if(tentacles && tentacles.isActiveAndEnabled)return;}
        if(!model)return;CacheColours();
        if(legacySprites==null)legacySprites=GetComponentsInChildren<SpriteRenderer>(true);
        if(legacySprites!=null)foreach(var legacy in legacySprites)if(legacy && !legacy.name.StartsWith("Heart"))legacy.forceRenderingOff=true;
        if(!Application.isPlaying)return;
        if(ai){var effect=ai.GetComponent<RampagerVisualEffects>();if(effect && effect.VisualBody)visibilitySource=effect.VisualBody;}
        if(visibilitySource)visibilitySource.forceRenderingOff=true;
        bool visible=!visibilitySource || visibilitySource.enabled;
        float alpha=visibilitySource?visibilitySource.color.a:1;
        model.gameObject.SetActive(visible);
        if(!visible)return;
        Vector3 delta=transform.position-previous;previous=transform.position;
        float dt=Time.deltaTime;
        var world=GetComponentInParent<RecoilPrototype>();if(world && !world.Playing)dt=0;
        float sign=ai?ai.Facing:player?(player.AimLocomotion?player.AimLocomotion.FacingSign:player.Aim.x):Mathf.Abs(delta.x)>.001f?Mathf.Sign(delta.x):Mathf.Sign(-Mathf.Sin(yaw*Mathf.Deg2Rad));
        bool enteringStairs=player && player.DepthPlane && player.DepthPlane.StairEntering;
        float desired=enteringStairs?180:sign>=0?-90:90;
        yaw=ai?ai.GetComponent<RampagerMovement>().VisualYaw:Mathf.MoveTowardsAngle(yaw,desired,(player&&player.AimLocomotion?player.AimLocomotion.bodyTurnSpeed:characterTurnSpeed)*dt);
        if(depthArrival && depthArrival.isActiveAndEnabled && !depthArrival.JoinedPlane)yaw=depthArrival.VisualYaw;
        float displayYaw=player && player.DepthPlane?Mathf.LerpAngle(yaw,0,player.DepthPlane.DepartureFacing):yaw;
        bool running=player && player.Running;
        model.localRotation=Quaternion.Euler(0,displayYaw,monster?7:0)*Quaternion.Euler(running?-9:0,0,0);
        float travel=player?(enteringStairs?delta.z:delta.x*(sign>=0?1:-1)):delta.magnitude;
        if(dt>0 && delta.sqrMagnitude<1)stride+=travel*(running?6:8);
        float swing=delta.magnitude>.0001f?Mathf.Sin(stride)*(running?40:22):0;
        bool sleeping=ai && ai.State==RampagerController.Behaviour.Sleeping;
        model.localPosition=new Vector3(0,sleeping?-.16f:running?Mathf.Abs(Mathf.Sin(stride))*.035f:0,0);
        if(player)model.localPosition+=model.parent.InverseTransformVector(Vector3.up*player.StepVisualOffset);
        if(leftLeg)leftLeg.localRotation=Quaternion.Euler(swing,0,0);if(rightLeg)rightLeg.localRotation=Quaternion.Euler(-swing,0,0);
        if(leftArm)leftArm.localRotation=Quaternion.Euler(-swing*(running?.9f:.55f),0,monster?8:0);if(rightArm)rightArm.localRotation=Quaternion.Euler(swing*(running?.9f:.55f),0,monster?-8:0);
        if(grab && grab.IsGrabbing && world && world.Player)
        {
            var target=world.Player.WorldPosition+Vector3.up*.1f;
            ReachArm(leftArm,target);ReachArm(rightArm,target);
        }
        if(player && player.AimLocomotion)
        {
            if(playerLegPose)playerLegPose.ApplyPose(player,delta,stride,dt);
            bool braking=!playerLegPose && player.AimLocomotion.IsBraking && player.Grounded;
            bool supported=player.Grounded||player.WalkingStairs;
            if(braking)
            {
                if(leftLeg)leftLeg.localRotation=Quaternion.Euler(-12,0,0);
                if(rightLeg)rightLeg.localRotation=Quaternion.Euler(14,0,0);
            }
            else if(!playerLegPose && !supported)
            {
                if(leftLeg)leftLeg.localRotation=Quaternion.Euler(12,0,0);
                if(rightLeg)rightLeg.localRotation=Quaternion.Euler(-8,0,0);
            }
            if(player.HasWaterGun&&!player.Climbing&&!player.HoldingHandhold)
            {
                var arm=player.AimLocomotion.FacingSign>0?leftArm:rightArm;
                if(arm)
                {
                    Vector3 grip=player.WorldPosition+(Vector3)(player.Aim*.42f);
                    Vector3 direction=arm.parent.InverseTransformDirection(grip-arm.position).normalized;
                    arm.localRotation=Quaternion.FromToRotation(Vector3.down,direction);
                }
            }
        }
        if(head)head.localRotation=Quaternion.Euler(sleeping?35:0,0,0);
        ApplyAttackPose();
        for(int i=0;i<parts.Length;i++)if(parts[i]){var c=baseColours[i];c.a=alpha;block.Clear();block.SetColor("_Color",c);parts[i].SetPropertyBlock(block);}
        if(eye){block.Clear();var c=ai && ai.RedEyes?new Color(1,.08f,.025f,alpha):new Color(.14f,.12f,.1f,alpha);block.SetColor("_Color",c);block.SetColor("_EmissionColor",ai && ai.RedEyes?new Color(1.8f,.04f,.01f):Color.black);eye.SetPropertyBlock(block);}
    }
    void ApplyAttackPose()
    {
        if(!monster || !attack || !attack.isActiveAndEnabled || attack.Phase==RampagerAttack.AttackPhase.Ready)return;
        float progress=attack.PhaseProgress;
        bool windup=attack.Phase==RampagerAttack.AttackPhase.Windup;
        float anticipation=windup?Mathf.SmoothStep(0,1,Mathf.Clamp01(progress/.6f)):0;
        float strike=windup?Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.6f)/.4f)):1;
        float recover=windup?0:Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.2f)/.8f));
        float force=strike*(1-recover);
        model.localRotation*=Quaternion.Euler(Mathf.Lerp(10*anticipation,-16,strike)*(1-recover),0,0);
        model.localPosition+=Vector3.up*(.025f*anticipation-.035f*force);
        bool useLeft=(attack.StrikeSequence&1)==1;
        AttackArm(useLeft?leftArm:rightArm,attack.StrikePosition+Vector3.up*.12f,anticipation,strike,recover,useLeft?1:-1);
        var guard=useLeft?rightArm:leftArm;
        if(guard)guard.localRotation=Quaternion.Slerp(guard.localRotation,Quaternion.Euler(-55,0,useLeft?-22:22),Mathf.Max(anticipation,force)*(1-recover));
        if(head)head.localRotation=Quaternion.Euler(-12*force,0,0);
        if(leftLeg)leftLeg.localRotation=Quaternion.Euler(-10*force,0,0);
        if(rightLeg)rightLeg.localRotation=Quaternion.Euler(14*force,0,0);
    }
    static void AttackArm(Transform arm,Vector3 target,float anticipation,float strike,float recover,float side)
    {
        if(!arm)return;
        Quaternion rest=arm.localRotation;
        Quaternion prepared=Quaternion.Slerp(rest,Quaternion.Euler(48,0,side*38),anticipation);
        Quaternion extended=Quaternion.FromToRotation(Vector3.down,arm.parent.InverseTransformDirection(target-arm.position).normalized);
        arm.localRotation=Quaternion.Slerp(Quaternion.Slerp(prepared,extended,strike),rest,recover);
    }

}
