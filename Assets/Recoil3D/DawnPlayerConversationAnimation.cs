using UnityEngine;

// A full-body turn carries the existing hand/gun pose together. Then release the
// support hand, lower the trigger arm to the hip and stow the gun. No aiming IK
// runs against this pose, even though ordinary gameplay is paused.
public sealed partial class DawnPlayerAnimation
{
    public const float ConversationPreparationDuration=.8f;
    [Header("Conversation presentation")]
    [Range(.1f,.5f)] public float conversationTurnDuration=.32f;
    [Range(1,6)] public float conversationBreathDegrees=3.2f;
    [Range(1.5f,6)] public float conversationBreathPeriod=3.2f;
    public bool ConversationPresentationActive {get;private set;}
    public float ConversationPresentationTime {get;private set;}
    public float ConversationTurnProgress {get;private set;}
    public float ConversationHolsterProgress {get;private set;}
    public bool ConversationHadWeapon {get;private set;}
    public float ConversationBreathAmount {get;private set;}
    public bool ConversationNeededTurn {get;private set;}
    Quaternion conversationStartRotation,conversationTargetRotation;
    Vector3 conversationWeaponInHand;
    Quaternion conversationWeaponRotation;
    Transform conversationRightHand;
    Transform[] conversationBones;
    Quaternion[] conversationCapturedPose;
    float conversationTargetSign=1;
    static readonly HumanBodyBones[] ConversationBoneIds={
        HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,
        HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
        HumanBodyBones.RightShoulder,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand
    };
    public void BeginConversationPresentation(Vector3 partner)
    {
        if(!player || !animator || !animator.isHuman || !facingRoot)return;
        conversationTargetSign=Mathf.Abs(partner.x-player.WorldPosition.x)>.01f?Mathf.Sign(partner.x-player.WorldPosition.x):
            (player.AimLocomotion?player.AimLocomotion.FacingSign:aimFacingSign);
        conversationStartRotation=facingRoot.localRotation;
        conversationTargetRotation=Quaternion.Euler(0,conversationTargetSign>=0?-90:90,0);
        ConversationNeededTurn=Quaternion.Angle(conversationStartRotation,conversationTargetRotation)>35;
        ConversationHadWeapon=player.HasWaterGun && weapon && weapon.gameObject.activeInHierarchy;
        conversationBones=new Transform[ConversationBoneIds.Length];conversationCapturedPose=new Quaternion[ConversationBoneIds.Length];
        for(int i=0;i<conversationBones.Length;i++){
            conversationBones[i]=animator.GetBoneTransform(ConversationBoneIds[i]);
            if(conversationBones[i])conversationCapturedPose[i]=conversationBones[i].localRotation;
        }
        conversationRightHand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        if(weapon && conversationRightHand){
            conversationWeaponInHand=conversationRightHand.InverseTransformPoint(weapon.position);
            conversationWeaponRotation=Quaternion.Inverse(conversationRightHand.rotation)*weapon.rotation;
        }
        ConversationPresentationActive=true;ConversationPresentationTime=0;ConversationTurnProgress=ConversationNeededTurn?0:1;
        ConversationHolsterProgress=0;ConversationBreathAmount=0;shotKick=0;VerticalSpeed=0;
        // The ordinary idle clip supplies the relaxed target pose and keeps the feet planted.
        animator.speed=1;animator.SetFloat(Speed,0);animator.SetFloat(Direction,1);animator.SetFloat(ClimbSpeed,0);
        if(CurrentPose!="Locomotion")animator.CrossFadeInFixedTime("Locomotion",.12f,0);
        CurrentPose="Conversation";AimWeight=0;if(animator.layerCount>1)animator.SetLayerWeight(1,0);
        if(player.AimLocomotion)player.AimLocomotion.FaceConversationPartner(conversationTargetSign);
    }
    public void AdvanceConversationPresentation(float dt)
    {
        if(!ConversationPresentationActive || !animator || !facingRoot)return;
        dt=Mathf.Max(0,dt);ConversationPresentationTime+=dt;
        float turnEnd=ConversationNeededTurn?Mathf.Clamp(conversationTurnDuration,.1f,.5f):0;
        ConversationTurnProgress=turnEnd>0?Mathf.SmoothStep(0,1,ConversationPresentationTime/turnEnd):1;
        ConversationHolsterProgress=Mathf.SmoothStep(0,1,Mathf.Clamp01((ConversationPresentationTime-turnEnd)/(ConversationPreparationDuration-turnEnd)));
        facingRoot.localRotation=Quaternion.Slerp(conversationStartRotation,conversationTargetRotation,ConversationTurnProgress);
        yaw=facingRoot.localEulerAngles.y;aimFacingSign=conversationTargetSign;
        // Synchronize gameplay aim only once the body has actually completed its turn.
        if(ConversationTurnProgress>=1)player.Aim=new Vector2(conversationTargetSign,0);
        previous=player.transform.position;animator.speed=1;
        conversationPhotoWeight=Mathf.MoveTowards(conversationPhotoWeight,conversationPhotoOffering?1:0,dt*4);
        animator.SetFloat(Speed,0);animator.SetFloat(Direction,1);animator.SetFloat(ClimbSpeed,0);
        if(animator.layerCount>1)animator.SetLayerWeight(1,0);AimWeight=0;
        ApplyFixedHeight();
    }
    public void FinalizeConversationPose()
    {
        if(!ConversationPresentationActive || !animator || conversationBones==null)return;
        float lower=ConversationHolsterProgress;
        for(int i=0;i<conversationBones.Length;i++){
            var bone=conversationBones[i];if(!bone)continue;
            // Release the support hand first; the gun remains rigidly attached to the right palm.
            float blend=i>=4&&i<=7?Mathf.SmoothStep(0,1,Mathf.Clamp01(lower*1.8f)):
                i>=8?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,1,lower)):lower;
            bone.localRotation=Quaternion.Slerp(conversationCapturedPose[i],bone.localRotation,blend);
        }
        float breathWeight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,lower));
        float phase=ConversationPresentationTime*Mathf.PI*2/Mathf.Max(1.5f,conversationBreathPeriod);
        ConversationBreathAmount=Mathf.Sin(phase)*breathWeight;
        AddConversationBreath(HumanBodyBones.Spine,new Vector3(ConversationBreathAmount*.9f,0,Mathf.Sin(phase*.5f)*.45f*breathWeight));
        AddConversationBreath(HumanBodyBones.Chest,new Vector3(ConversationBreathAmount*conversationBreathDegrees,0,0));
        AddConversationBreath(HumanBodyBones.Neck,new Vector3(-ConversationBreathAmount*1.2f,0,0));
        AddConversationBreath(HumanBodyBones.LeftShoulder,new Vector3(0,0,ConversationBreathAmount*1.4f));
        AddConversationBreath(HumanBodyBones.RightShoulder,new Vector3(0,0,-ConversationBreathAmount*1.4f));
        ApplyConversationPhotoOffer();
        if(weapon){
            bool visible=ConversationHadWeapon && lower<.96f;
            weapon.gameObject.SetActive(visible);
            if(visible && conversationRightHand){
                weapon.position=conversationRightHand.TransformPoint(conversationWeaponInHand);
                weapon.rotation=conversationRightHand.rotation*conversationWeaponRotation;
            }
        }
    }
    void AddConversationBreath(HumanBodyBones id,Vector3 angles)
    {
        var bone=animator.GetBoneTransform(id);if(bone)bone.localRotation*=Quaternion.Euler(angles);
    }
    public void EndConversationPresentation()
    {
        if(!ConversationPresentationActive)return;
        ConversationPresentationActive=false;ConversationBreathAmount=0;CurrentPose="Locomotion";
        SetConversationPhotoOffer(false);conversationPhotoWeight=0;
        previous=player?player.transform.position:transform.position;
        if(animator){animator.speed=1;animator.SetFloat(Speed,0);}
        if(weapon && player)weapon.gameObject.SetActive(player.HasWaterGun&&!player.Climbing&&!player.HoldingHandhold);
    }
}
