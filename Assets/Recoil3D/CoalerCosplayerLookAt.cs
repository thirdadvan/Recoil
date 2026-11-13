using UnityEngine;
using UnityEngine.Animations.Rigging;

// Updates targets before RigBuilder synchronizes the animation stream.
[DefaultExecutionOrder(-200), DisallowMultipleComponent]
public sealed class CoalerCosplayerLookAt : MonoBehaviour
{
    public Rig lookRig;
    public Transform lookTarget;
    public Transform distanceOrigin;
    public Transform head;
    [Min(0)] public float activationDistance=3f;
    [Min(.05f)] public float raiseTime=.5f;
    [Min(.05f)] public float lowerTime=.8f;
    [Min(.01f)] public float targetSmoothing=.18f;
    [Range(0,90)] public float maxYaw=80;
    public Vector2 pitchLimits=new Vector2(-35,45);
    [Tooltip("Distance uses the game's XY movement plane; visual Z offsets are not gameplay distance.")]
    public bool useGameplayPlane=true;
    public RecoilPlayerController player;
    float currentWeight,weightVelocity,nextSearch;
    Animator playerAnimator;
    public bool PlayerInRange {get;private set;}
    public float LookWeight {get{return currentWeight;}}
    void OnEnable(){if(lookRig)lookRig.weight=0;currentWeight=0;weightVelocity=0;}
    void Update()
    {
        if(!player || !player.isActiveAndEnabled){
            player=null;playerAnimator=null;
            if(Time.unscaledTime>=nextSearch){
                nextSearch=Time.unscaledTime+.5f;
                float best=float.PositiveInfinity;
                foreach(var candidate in FindObjectsOfType<RecoilPlayerController>()){
                    float d=Vector2.Distance(candidate.Position,distanceOrigin? (Vector2)distanceOrigin.position:(Vector2)transform.position);
                    if(d<best){best=d;player=candidate;}
                }
                if(player)foreach(var animator in player.GetComponentsInChildren<Animator>(true))if(animator.isHuman){playerAnimator=animator;break;}
            }
        }
        Vector3 position=player?player.WorldPosition:Vector3.zero;
        Vector3 point=position+Vector3.up*(player?player.BodyHeight*.35f:0);
        if(playerAnimator && playerAnimator.isActiveAndEnabled){var targetHead=playerAnimator.GetBoneTransform(HumanBodyBones.Head);if(targetHead)point=targetHead.position;}
        Advance(Time.deltaTime,position,point,player!=null);
    }
    public void Advance(float dt,Vector3 playerPosition,Vector3 playerHeadPosition,bool available)
    {
        if(!lookRig || !lookTarget || !head || dt<=0)return;
        Vector3 origin=distanceOrigin?distanceOrigin.position:transform.position;
        float distance=useGameplayPlane?Vector2.Distance(origin,playerPosition):Vector3.Distance(origin,playerPosition);
        PlayerInRange=available && distance<=activationDistance;
        if(PlayerInRange){
            Vector3 local=transform.InverseTransformDirection(playerHeadPosition-head.position);
            float yaw=Mathf.Clamp(Mathf.Atan2(local.x,local.z)*Mathf.Rad2Deg,-maxYaw,maxYaw);
            float pitch=Mathf.Clamp(Mathf.Atan2(local.y,new Vector2(local.x,local.z).magnitude)*Mathf.Rad2Deg,pitchLimits.x,pitchLimits.y);
            Vector3 desired=head.position+transform.TransformDirection(Quaternion.Euler(-pitch,yaw,0)*Vector3.forward)*2;
            lookTarget.position=Vector3.Lerp(lookTarget.position,desired,1-Mathf.Exp(-dt/targetSmoothing));
        }
        currentWeight=Mathf.SmoothDamp(currentWeight,PlayerInRange?1:0,ref weightVelocity,PlayerInRange?raiseTime:lowerTime,Mathf.Infinity,dt);
        if(!PlayerInRange && currentWeight<.0005f){currentWeight=0;weightVelocity=0;}
        lookRig.weight=currentWeight;
    }
    void OnDisable(){if(lookRig)lookRig.weight=0;currentWeight=0;weightVelocity=0;PlayerInRange=false;}
}
