using UnityEngine;

// Presentation only. The existing controller remains the sole owner of movement and firing.
[DefaultExecutionOrder(850),DisallowMultipleComponent]
public sealed partial class DawnPlayerAnimation : MonoBehaviour
{
    public Animator animator;
    public Transform facingRoot;
    public RecoilPlayerController player;
    public SpriteRenderer visibilitySource;
    public Transform weapon;
    RetroWaterBlaster retroWeapon;
    DawnRigidWeaponGrip rigidGrip;
    int gripPoseRevision;
    public DawnRigidWeaponGrip RigidGrip => rigidGrip;
    public float transitionTime=.14f;
    [System.NonSerialized] public bool manuallyDriven;
    public string CurrentPose {get;private set;}="Locomotion";
    public float VerticalSpeed {get;private set;}
    public float AimWeight {get;private set;}
    RecoilPrototype world;
    Renderer[] renderers;Color[] colors;MaterialPropertyBlock block;SpriteRenderer[] legacySprites;
    [Tooltip("Fixed visual lift in world units; independent of the animation pose.")]
    public float modelHeightOffset=.08f;
    Vector3 previous;float yaw=-90,landTime,shotKick,climbPhase,aimFacingSign=1;bool wasSupported=true;int lastShot;
    static readonly int Speed=Animator.StringToHash("Speed"),Direction=Animator.StringToHash("Direction"),ClimbSpeed=Animator.StringToHash("ClimbSpeed");
    void Awake()
    {
        if(!player)player=GetComponentInParent<RecoilPlayerController>();
        if(!animator)animator=GetComponent<Animator>();
        world=player?player.GetComponentInParent<RecoilPrototype>():null;
        previous=player?player.transform.position:transform.position;
        legacySprites=player?player.GetComponentsInChildren<SpriteRenderer>(true):new SpriteRenderer[0];
        renderers=GetComponentsInChildren<Renderer>(true);colors=new Color[renderers.Length];block=new MaterialPropertyBlock();
        for(int i=0;i<renderers.Length;i++)colors[i]=renderers[i].sharedMaterial?renderers[i].sharedMaterial.color:Color.white;
        if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;}
        if(animator && animator.isHuman)rigidGrip=new DawnRigidWeaponGrip(animator);
    }
    void ApplyFixedHeight()
    {
        if(facingRoot&&player)facingRoot.localPosition=player.transform.InverseTransformVector(Vector3.up*(player.StepVisualOffset+modelHeightOffset)+(PropOperationActive?Vector3.forward*propApproachDepth*PropReachBlend:Vector3.zero));
    }
    void Update()
    {
        if(manuallyDriven)return;
        if(PropOperationActive){UpdatePropOperationPresentation();return;}
        if(ConversationPresentationActive){AdvanceConversationPresentation(Time.unscaledDeltaTime);return;}
        AdvancePresentation(world&&!world.Playing?0:Time.deltaTime);
    }
    public void AdvancePresentation(float dt)
    {
        if(!player||!animator||!facingRoot)return;
        animator.speed=dt>0?1:0;
        Vector3 delta=player.transform.position-previous;previous=player.transform.position;
        if(dt<=0)return;
        bool teleport=delta.sqrMagnitude>4;
        VerticalSpeed=teleport?0:delta.y/Mathf.Max(.0001f,dt);
        if(Mathf.Abs(player.Aim.x)>.01f)aimFacingSign=Mathf.Sign(player.Aim.x);
        float sign=player.AimLocomotion?player.AimLocomotion.FacingSign:aimFacingSign;
        bool entering=player.DepthPlane&&player.DepthPlane.StairEntering;
        // The imported model has a 180 degree correction under this pivot.
        // Ladder climbs face into the scene (+Z), independent of aim direction.
        float targetYaw=(player.Climbing||entering)?180:sign>=0?-90:90;
        yaw=Mathf.MoveTowardsAngle(yaw,targetYaw,(player.AimLocomotion?player.AimLocomotion.bodyTurnSpeed:240)*dt);
        float shown=player.DepthPlane?Mathf.LerpAngle(yaw,0,player.DepthPlane.DepartureFacing):yaw;
        facingRoot.localRotation=Quaternion.Euler(0,shown,0);
        ApplyFixedHeight();
        bool supported=player.Grounded||player.WalkingStairs;
        float horizontal=teleport?0:(entering?delta.z:delta.x)/Mathf.Max(.0001f,dt);
        float movement=Mathf.Abs(horizontal);
        if(player.Climbing)climbPhase+=VerticalSpeed*dt/1.44f;
        ApplyWeaponPresentation();
        animator.SetFloat(Speed,movement,.10f,dt);
        animator.SetFloat(Direction,Mathf.Abs(horizontal)<.025f?1:Mathf.Sign(horizontal)*sign,.08f,dt);
        animator.SetFloat(ClimbSpeed,Mathf.Abs(VerticalSpeed)<.015f?0:Mathf.Clamp(VerticalSpeed/1.2f,-3,3));
        if(!wasSupported&&supported&&!teleport)landTime=.16f;
        landTime=Mathf.Max(0,landTime-dt);wasSupported=supported;
        string pose;
        if(player.HoldingHandhold)pose="Hang";
        else if(player.Climbing)pose="Climb";
        else if(player.DepthPlane&&player.DepthPlane.Departing)pose="JumpFall";
        else if(supported)pose=landTime>0?"Land":"Locomotion";
        else if(player.Flying)pose="FlightPedal";
        else if(VerticalSpeed>.15f)pose="JumpRise";
        else pose="JumpFall";
        if(pose!=CurrentPose){animator.CrossFadeInFixedTime(pose,transitionTime,0);CurrentPose=pose;}
        float holding=player.HasWaterGun&&!player.Climbing&&!player.HoldingHandhold?1:0;
        AimWeight=Mathf.MoveTowards(AimWeight,holding,dt*8);
        if(animator.layerCount>1)animator.SetLayerWeight(1,AimWeight);
        if(player.ShotSequence!=lastShot){lastShot=player.ShotSequence;shotKick=1;}
        shotKick=Mathf.MoveTowards(shotKick,0,dt*7);
    }
    void OnAnimatorIK(int layer)
    {
        if(layer!=1||!animator||!animator.isHuman||!player)return;
        gripPoseRevision++;
        if(ConversationPresentationActive||PropOperationActive)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,0);animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);
            animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow,0);animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow,0);
            animator.SetLookAtWeight(0);return;
        }
        if(player.Climbing)
        {
            // Independent ladder-space targets: hands reach upward and forward,
            // never toward the hidden weapon. Phase stops when the player stops.
            Vector3 forward=facingRoot.TransformDirection(Vector3.back);
            Vector3 across=Vector3.Cross(Vector3.up,forward).normalized;
            float cycle=Mathf.Sin(climbPhase*Mathf.PI*2);
            LadderHand(AvatarIKGoal.LeftHand,AvatarIKHint.LeftElbow,-1,cycle,forward,across);
            LadderHand(AvatarIKGoal.RightHand,AvatarIKHint.RightElbow,1,-cycle,forward,across);
            animator.SetLookAtWeight(.25f,0,.6f,0,.5f);
            animator.SetLookAtPosition(player.WorldPosition+Vector3.up+forward);
            return;
        }
        float w=AimWeight;
        if(retroWeapon)
        {
            // Modular gun arms are solved together after animation evaluation.
            // Native hand IK must not fight the fixed palm frames in that pass.
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand,0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand,0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand,0);
            if(world&&world.EscortingChild&&!player.WalkingStairs&&!world.Child.WalkingStairs)
                SetHand(AvatarIKGoal.LeftHand,AvatarIKHint.LeftElbow,world.Child.transform.position+Vector3.up*.28f,player.Aim,1,Vector3.forward);
            animator.SetLookAtWeight(w*.55f,.04f,.7f,0,.55f);
            animator.SetLookAtPosition(animator.GetBoneTransform(HumanBodyBones.Head).position+(Vector3)player.Aim*3);
            return;
        }
        // Weapon transform is still positioned by RecoilPlayerController.SyncVisuals.
        Vector3 aim=new Vector3(player.Aim.x,player.Aim.y,0).normalized;
        Vector3 grip=PresentationCenter+aim*.65f+Vector3.back*.22f;
        Vector3 depth=Vector3.forward;
        Vector3 gunUp=Vector3.Cross(Vector3.forward,aim);
        var main=grip-aim*(.075f+shotKick*.035f)-gunUp*.06f-depth*.025f;
        var support=grip+aim*.045f-gunUp*.035f+depth*.02f;
        SetHand(AvatarIKGoal.RightHand,AvatarIKHint.RightElbow,main,aim,w,depth);
        if(world&&world.EscortingChild&&!player.WalkingStairs&&!world.Child.WalkingStairs)support=world.Child.transform.position+Vector3.up*.28f;
        SetHand(AvatarIKGoal.LeftHand,AvatarIKHint.LeftElbow,support,aim,world&&world.EscortingChild&&!player.WalkingStairs&&!world.Child.WalkingStairs?1:w,depth);
        animator.SetLookAtWeight(w*.45f,.12f,.65f,0,.55f);
        animator.SetLookAtPosition(player.WorldPosition+Vector3.up*.4f+aim*3);
    }
    void LadderHand(AvatarIKGoal goal,AvatarIKHint hint,float side,float cycle,Vector3 forward,Vector3 across)
    {
        var center=PresentationCenter;
        animator.SetIKPositionWeight(goal,1);
        animator.SetIKPosition(goal,center+across*(side*.20f)+forward*.27f+Vector3.up*(.58f+cycle*.16f));
        animator.SetIKRotationWeight(goal,0);
        animator.SetIKHintPositionWeight(hint,.85f);
        animator.SetIKHintPosition(hint,center+across*(side*.32f)+forward*.12f+Vector3.up*.28f);
    }
    void SetHand(AvatarIKGoal goal,AvatarIKHint hint,Vector3 position,Vector3 aim,float weight,Vector3 depth)
    {
        animator.SetIKPositionWeight(goal,weight);animator.SetIKPosition(goal,position);
        animator.SetIKRotationWeight(goal,0);
        animator.SetIKRotation(goal,Quaternion.LookRotation(aim,Vector3.forward)*Quaternion.Euler(0,90,goal==AvatarIKGoal.RightHand?90:-90));
        animator.SetIKHintPositionWeight(hint,weight*.65f);
        animator.SetIKHintPosition(hint,player.WorldPosition+Vector3.up*.02f+(goal==AvatarIKGoal.RightHand?-depth:depth)*.28f);
    }
    Vector3 PresentationCenter=>player.WorldPosition+Vector3.up*(player.StandingHeight*.15f+modelHeightOffset);
    void ApplyWeaponPresentation()
    {
        if(!weapon||!player)return;
        if(ConversationPresentationActive||PropOperationActive||(world&&world.PropOperationActive)){weapon.gameObject.SetActive(false);return;}
        if(!retroWeapon)retroWeapon=weapon.GetComponentInChildren<RetroWaterBlaster>(true);
        weapon.gameObject.SetActive(player.HasWaterGun&&!player.Climbing&&!player.HoldingHandhold);
        weapon.position=PresentationCenter+Vector3.up*(retroWeapon ? .26f : 0)+(Vector3)(player.Aim*(retroWeapon ? .30f : .65f))+Vector3.back*(retroWeapon ? 0 : .22f);
        if(retroWeapon)retroWeapon.SetFacing(player.Aim);
    }
    void LateUpdate()
    {
        if(!player||renderers==null)return;
        // Native cache is not restored when scripts reload during Play Mode.
        if(block==null)block=new MaterialPropertyBlock();
        ApplyFixedHeight();
        if(PropOperationActive)FinalizePropOperationPose();else if(ConversationPresentationActive)FinalizeConversationPose();else FinalizeWeaponGrip();
        bool visible=!visibilitySource||visibilitySource.enabled;
        float alpha=visibilitySource?visibilitySource.color.a:1;
        for(int i=0;i<renderers.Length;i++)if(renderers[i])
        {
            renderers[i].enabled=visible;
            var c=colors[i];c.a=alpha;block.Clear();block.SetColor("_Color",c);renderers[i].SetPropertyBlock(block);
        }
        // Keep legacy sprites hidden while retaining their gameplay visibility signals.
        foreach(var s in legacySprites)if(!s.name.StartsWith("Heart"))s.forceRenderingOff=true;
    }

    // Run after the Animator so walking, upper-body aiming and recoil cannot
    // move a wrist away from its fixed position AND rotation on the weapon.
    public void FinalizeWeaponGrip()
    {
        if(ConversationPresentationActive||PropOperationActive)return;
        ApplyWeaponPresentation();
        if(!retroWeapon || !animator || !animator.isHuman || !player || !player.HasWaterGun || player.Climbing || player.HoldingHandhold)return;
        if(rigidGrip==null)rigidGrip=new DawnRigidWeaponGrip(animator);
        Vector3 forward=facingRoot?facingRoot.TransformDirection(Vector3.back):(player.Aim.x<0?Vector3.left:Vector3.right);
        rigidGrip.Apply(weapon,retroWeapon,player.Aim,forward,shotKick,!(world&&world.EscortingChild),gripPoseRevision);
    }

    public bool TryGetWaterMuzzle(out Vector3 position)
    {
        position=Vector3.zero;
        if(!weapon||!player)return false;
        if(!retroWeapon)retroWeapon=weapon.GetComponentInChildren<RetroWaterBlaster>(true);
        if(!retroWeapon||!retroWeapon.muzzle||player.Climbing||player.HoldingHandhold)return false;
        // Input can change before this frame's Animator evaluation. Resolve the
        // current rigid pose now so firing never uses last frame's gun angle.
        weapon.rotation=Quaternion.Euler(0,0,Mathf.Atan2(player.Aim.y,player.Aim.x)*Mathf.Rad2Deg);
        FinalizeWeaponGrip();position=retroWeapon.muzzle.position;return true;
    }
}
