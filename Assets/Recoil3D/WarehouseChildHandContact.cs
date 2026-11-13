using UnityEngine;

// Runs after both character presentations; animation still owns legs and locomotion.
[DefaultExecutionOrder(1200),DisallowMultipleComponent]
public sealed class WarehouseChildHandContact : MonoBehaviour
{
    public EscortChild escort;
    public Animator childAnimator;
    [Min(.05f)] public float blendSeconds=.25f;
    [Range(0,180)] public float resumeFacingAngle=80f;
    [Range(0,180)] public float releaseFacingAngle=110f;
    public float Weight {get;private set;}
    public float PalmGap {get;private set;}
    public bool FacingAllowsContact {get;private set;}
    Animator playerAnimator;
    WarehouseChildAnimation presentation;
    Quaternion previousPlayerRotation;
    bool havePlayerRotation;
    bool childLeft=true;
    void Awake(){if(!escort)escort=GetComponentInParent<EscortChild>();if(!childAnimator)childAnimator=GetComponent<Animator>();}
    void LateUpdate()
    {
        var player=escort?escort.EscortPlayer:null;
        if(player&&(!playerAnimator||!playerAnimator.transform.IsChildOf(player.transform)))
        {var presentation=player.GetComponentInChildren<DawnPlayerAnimation>();playerAnimator=presentation?presentation.animator:null;}
        bool holding=escort&&escort.Following&&escort.Alive&&player&&!player.Climbing&&!player.WalkingStairs&&!escort.WalkingStairs;
        ApplyContact(playerAnimator,holding,Time.deltaTime);
    }
    public void ApplyContact(Animator other,bool holding,float dt)
    {
        bool valid=other&&other.isHuman&&childAnimator&&childAnimator.isHuman;
        if(!holding||!valid)FacingAllowsContact=false;
        else{
            Vector3 toward=Vector3.ProjectOnPlane(other.transform.position-childAnimator.transform.position,Vector3.up);
            Vector3 forward=Vector3.ProjectOnPlane(childAnimator.transform.forward,Vector3.up);
            if(toward.sqrMagnitude>.0001f&&forward.sqrMagnitude>.0001f){
                float angle=Vector3.Angle(forward,toward);
                // Hysteresis prevents repeatedly grabbing/releasing near a side-on pose.
                FacingAllowsContact=FacingAllowsContact?angle<Mathf.Max(resumeFacingAngle,releaseFacingAngle):angle<=resumeFacingAngle;
            }else FacingAllowsContact=false;
        }
        if(!presentation)presentation=GetComponent<WarehouseChildAnimation>();
        bool playerTurning=false;
        if(valid){
            playerTurning=havePlayerRotation&&Quaternion.Angle(previousPlayerRotation,other.transform.rotation)>Mathf.Max(.5f,60*dt);
            previousPlayerRotation=other.transform.rotation;havePlayerRotation=true;
        }else havePlayerRotation=false;
        // A locked wrist cannot remain on the far side of a rotating torso without twisting
        // the shoulder. Relax the visual grip during the turn, then blend back into contact.
        bool turning=playerTurning||(presentation&&presentation.TurningForHandhold);
        float blend=turning||!FacingAllowsContact?.10f:Mathf.Max(.05f,blendSeconds);
        Weight=Mathf.MoveTowards(Weight,holding&&valid&&!turning&&FacingAllowsContact?1:0,dt/blend);
        if(!valid||Weight<=0)return;
        var pu=other.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var cl=childAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var cr=childAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        if(Weight<.1f)childLeft=(cl.position-pu.position).sqrMagnitude<(cr.position-pu.position).sqrMagnitude;
        var cu=childLeft?cl:cr;
        var pl=other.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var ph=other.GetBoneTransform(HumanBodyBones.LeftHand);
        var cf=childAnimator.GetBoneTransform(childLeft?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var ch=childAnimator.GetBoneTransform(childLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        float pr=Vector3.Distance(pu.position,pl.position)+Vector3.Distance(pl.position,ph.position);
        float rr=Vector3.Distance(cu.position,cf.position)+Vector3.Distance(cf.position,ch.position);
        float childPalm=Vector3.Distance(ch.position,Palm(childAnimator,childLeft));
        float playerPalm=Vector3.Distance(ph.position,Palm(other,true));
        // Put the contact on the child's reach sphere: the elbow can extend naturally,
        // while the adult bends their arm to meet it. Never scale or stretch bones.
        float childReach=rr*.995f+childPalm;
        float playerReach=pr*.94f+playerPalm;
        Vector3 between=pu.position-cu.position;
        float distance=between.magnitude;
        Vector3 axis=distance>.0001f?between/distance:Vector3.right;
        Vector3 low=Vector3.ProjectOnPlane(Vector3.down+Vector3.back*.1f,axis).normalized;
        Vector3 contact;
        if(distance>.0001f&&distance<childReach+playerReach&&distance>Mathf.Abs(childReach-playerReach)){
            float along=(childReach*childReach-playerReach*playerReach+distance*distance)/(2*distance);
            float across=Mathf.Sqrt(Mathf.Max(0,childReach*childReach-along*along));
            contact=cu.position+axis*along+low*across;
        }else{
            contact=cu.position+axis*childReach;
            contact=pu.position+Vector3.ClampMagnitude(contact-pu.position,pr*.99f+playerPalm);
        }
        var chest=other.GetBoneTransform(HumanBodyBones.Chest);
        Vector3 adultOut=chest?(pu.position-chest.position).normalized:other.transform.right;
        bool childBehind=Vector3.Dot(childAnimator.transform.position-other.transform.position,other.transform.forward)<0;
        Vector3 adultHint=(childBehind?Vector3.up:Vector3.down)+adultOut*.35f;
        Solve(other,true,contact,Weight,adultHint);
        Solve(childAnimator,childLeft,contact,Weight,Vector3.down+Vector3.back*.3f);
        KeepFreeHandOutsideBody();
        PalmGap=Vector3.Distance(Palm(other,true),Palm(childAnimator,childLeft));
    }
    void KeepFreeHandOutsideBody()
    {
        bool freeLeft=!childLeft;
        var hips=childAnimator.GetBoneTransform(HumanBodyBones.Hips);
        var chest=childAnimator.GetBoneTransform(HumanBodyBones.Chest);
        var ls=childAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var rs=childAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var hand=childAnimator.GetBoneTransform(freeLeft?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        Vector3 across=(rs.position-ls.position).normalized;
        Vector3 forward=Vector3.Cross(across,Vector3.up).normalized;
        float halfWidth=Vector3.Distance(ls.position,rs.position)*.5f;
        float scale=Vector3.Distance(hips.position,chest.position)/.27f;
        if(hand.position.y<hips.position.y-.3f*scale||hand.position.y>chest.position.y+.08f*scale)return;
        Vector3 center=Vector3.Lerp(hips.position,chest.position,.5f);
        float depth=Mathf.Abs(Vector3.Dot(hand.position-center,forward));
        float depthWeight=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(halfWidth*.8f,halfWidth*1.3f,depth));
        float sign=freeLeft?-1:1;
        float lateral=Vector3.Dot(hand.position-center,across)*sign;
        float inwardExtent=0;
        foreach(var b in new[]{freeLeft?HumanBodyBones.LeftIndexDistal:HumanBodyBones.RightIndexDistal,freeLeft?HumanBodyBones.LeftMiddleDistal:HumanBodyBones.RightMiddleDistal,freeLeft?HumanBodyBones.LeftLittleDistal:HumanBodyBones.RightLittleDistal}){
            var tip=childAnimator.GetBoneTransform(b);
            if(tip)inwardExtent=Mathf.Max(inwardExtent,Vector3.Dot(hand.position-tip.position,across)*sign);
        }
        float clearance=halfWidth*.95f+Mathf.Max(.045f*scale,inwardExtent+.025f*scale);
        float correction=Mathf.Max(0,clearance-lateral)*depthWeight;
        if(correction<.0001f)return;
        Vector3 outside=across*sign;
        Solve(childAnimator,freeLeft,hand.position+outside*correction,Weight,Vector3.down+outside*.65f,false);
    }
    static Vector3 Palm(Animator a,bool left){var h=a.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var f=a.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);return f?Vector3.Lerp(h.position,f.position,.7f):h.position;}
    static void Solve(Animator a,bool left,Vector3 contact,float weight,Vector3 bendHint,bool grasp=true)
    {
        var u=a.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
        var l=a.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var h=a.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        var finger=a.GetBoneTransform(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);
        Quaternion uq=u.rotation,lq=l.rotation,hq=h.rotation;
        float x=Vector3.Distance(u.position,l.position),y=Vector3.Distance(l.position,h.position);
        Vector3 dir=(contact-u.position).normalized;
        float palm=finger?Vector3.Distance(h.position,finger.position)*.7f:.035f;
        Vector3 target=grasp?contact-dir*palm:contact;
        float d=Mathf.Clamp(Vector3.Distance(u.position,target),Mathf.Abs(x-y)+.001f,x+y-.002f);
        Vector3 axis=(target-u.position).normalized;
        Vector3 side=Vector3.ProjectOnPlane(bendHint,axis).normalized;
        if(side.sqrMagnitude<.0001f)side=Vector3.ProjectOnPlane(a.transform.right,axis).normalized;
        float along=(x*x-y*y+d*d)/(2*d);
        var elbow=u.position+axis*along+side*Mathf.Sqrt(Mathf.Max(0,x*x-along*along));
        u.rotation=Quaternion.FromToRotation(l.position-u.position,elbow-u.position)*u.rotation;
        l.rotation=Quaternion.FromToRotation(h.position-l.position,target-l.position)*l.rotation;
        if(grasp&&finger)h.rotation=Quaternion.FromToRotation(finger.position-h.position,contact-h.position)*h.rotation;
        if(!grasp)h.rotation=hq;
        Quaternion ut=u.rotation,lt=l.rotation,ht=h.rotation;
        u.rotation=Quaternion.Slerp(uq,ut,weight);l.rotation=Quaternion.Slerp(lq,lt,weight);h.rotation=Quaternion.Slerp(hq,ht,weight);
        // A loose grasp rather than two rigid, flat hands meeting at the wrists.
        var index=a.GetBoneTransform(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
        var little=a.GetBoneTransform(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
        if(grasp&&index&&little&&finger){
            Vector3 forward=(finger.position-h.position).normalized;
            Vector3 normal=Vector3.Cross(index.position-little.position,forward).normalized*(left?-1:1);
            Vector3 bendAxis=Vector3.Cross(forward,normal).normalized;
            HumanBodyBones[] joints=left?new[]{HumanBodyBones.LeftIndexProximal,HumanBodyBones.LeftMiddleProximal,HumanBodyBones.LeftRingProximal,HumanBodyBones.LeftLittleProximal}:new[]{HumanBodyBones.RightIndexProximal,HumanBodyBones.RightMiddleProximal,HumanBodyBones.RightRingProximal,HumanBodyBones.RightLittleProximal};
            foreach(var joint in joints){var b=a.GetBoneTransform(joint);if(b)b.rotation=Quaternion.AngleAxis(28*weight,bendAxis)*b.rotation;}
        }
    }
}
