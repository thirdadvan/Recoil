using UnityEngine;

// A shared rigid weapon pose, followed by length-preserving two-bone arm solves.
// Grip transforms describe the palm frame: forward along the fingers, up out of
// the palm. The avatar-specific wrist frame is calibrated from its finger bones.
public sealed class DawnRigidWeaponGrip
{
    sealed class Arm
    {
        public Transform upper, lower, hand;
        public Quaternion palmToWrist;
        Vector3 palmOffsetRatio;
        public float Length => Vector3.Distance(upper.position, lower.position) + Vector3.Distance(lower.position, hand.position);

        public Arm(Animator animator, bool right)
        {
            upper = animator.GetBoneTransform(right ? HumanBodyBones.RightUpperArm : HumanBodyBones.LeftUpperArm);
            lower = animator.GetBoneTransform(right ? HumanBodyBones.RightLowerArm : HumanBodyBones.LeftLowerArm);
            hand = animator.GetBoneTransform(right ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
            var middle = animator.GetBoneTransform(right ? HumanBodyBones.RightMiddleProximal : HumanBodyBones.LeftMiddleProximal);
            var index = animator.GetBoneTransform(right ? HumanBodyBones.RightIndexProximal : HumanBodyBones.LeftIndexProximal);
            var little = animator.GetBoneTransform(right ? HumanBodyBones.RightLittleProximal : HumanBodyBones.LeftLittleProximal);
            var thumb = animator.GetBoneTransform(right ? HumanBodyBones.RightThumbProximal : HumanBodyBones.LeftThumbProximal);
            if (!hand || !middle || !index || !little) { palmToWrist = Quaternion.identity; return; }
            Vector3 forward = hand.InverseTransformDirection(middle.position - hand.position).normalized;
            Vector3 across = hand.InverseTransformDirection(index.position - little.position).normalized;
            Vector3 normal = Vector3.Cross(forward, across).normalized;
            if (thumb && Vector3.Dot(hand.InverseTransformDirection(thumb.position - hand.position), normal) < 0) normal = -normal;
            palmToWrist = Quaternion.Inverse(Quaternion.LookRotation(forward, normal));
            if(!right && Valid)
            {
                // The support marker is a palm contact, not the wrist joint.
                // Normalize to arm length so avatar scaling preserves contact.
                Vector3 knuckles=(middle.position+index.position+little.position)/3;
                Vector3 palm=Vector3.Lerp(hand.position,knuckles,.6f);
                palmOffsetRatio=Quaternion.Inverse(hand.rotation)*(palm-hand.position)/Length;
            }
        }

        public bool Valid => upper && lower && hand;
        public Vector3 PalmPosition => hand.position+hand.rotation*(palmOffsetRatio*Length);
        public Vector3 WristPosition(Transform grip) => grip.position-(grip.rotation*palmToWrist)*(palmOffsetRatio*Length);

        public void Solve(Transform grip, Vector3 pole, float downwardBend=0)
        {
            Vector3 shoulder = upper.position;
            float a = Vector3.Distance(shoulder, lower.position), b = Vector3.Distance(lower.position, hand.position);
            Vector3 target = WristPosition(grip);
            Vector3 elbow = ElbowPosition(shoulder, target, a, b, pole, downwardBend);
            upper.rotation = Quaternion.FromToRotation(lower.position - shoulder, elbow - shoulder) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(hand.position - lower.position, target - lower.position) * lower.rotation;
            hand.rotation = grip.rotation * palmToWrist;
        }

        public static Vector3 ElbowPosition(Vector3 shoulder, Vector3 target, float a, float b, Vector3 pole, float downwardBend=0)
        {
            Vector3 direction = target - shoulder;
            float distance = Mathf.Clamp(direction.magnitude, Mathf.Abs(a - b) + .00001f, a + b - .00001f);
            direction.Normalize();
            Vector3 bend = Vector3.ProjectOnPlane(pole - shoulder, direction).normalized;
            if (bend.sqrMagnitude < .01f) bend = Vector3.ProjectOnPlane(Vector3.forward, direction).normalized;
            // A lateral pole alone can choose the upward half of the elbow
            // circle. Select the lower half while retaining a lateral bend;
            // a full vertical reflection would push the sleeve into the coat.
            Vector3 down=Vector3.ProjectOnPlane(Vector3.down,direction);
            if(downwardBend>0 && down.sqrMagnitude>.00001f)
            {
                down.Normalize();
                float vertical=Vector3.Dot(bend,down);
                if(vertical<0)
                {
                    Vector3 lateral=bend-vertical*down;
                    if(lateral.sqrMagnitude<.00001f)lateral=Vector3.Cross(direction,down);
                    lateral.Normalize();
                    float drop=Mathf.Min(-vertical,.2f);
                    Vector3 lowered=lateral*Mathf.Sqrt(1-drop*drop)+down*drop;
                    bend=Vector3.Lerp(bend,lowered,downwardBend).normalized;
                    if(bend.sqrMagnitude<.00001f)bend=down;
                }
            }
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            float height = Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            return shoulder + direction * along + bend * height;
        }
    }

    readonly Arm right, left;
    struct LoweredReference
    {
        public Vector3 forward,mainOffset,supportOffset,mainHand,mainUpper,supportUpper;
        public float mainForearm,supportForearm,mainReach,supportReach,mainClearance,supportClearance,angle,turn;
    }
    struct LoweredPose
    {
        public Vector3 position,mainElbow,supportElbow;
        public float lean,clearance;
        public bool valid;
    }
    readonly Transform chest, head,spine;
    Quaternion chestBase, headBase,spineBase;
    int lastPoseRevision=int.MinValue;
    public float MainPositionError { get; private set; }
    public float SupportPositionError { get; private set; }
    public float MainRotationError { get; private set; }
    public float SupportRotationError { get; private set; }
    public float DownwardBodyLean { get; private set; }
    public float DownwardWaistLean { get; private set; }
    public bool Valid => right.Valid && left.Valid;

    public DawnRigidWeaponGrip(Animator animator)
    {
        right = new Arm(animator, true); left = new Arm(animator, false);
        chest=animator.GetBoneTransform(HumanBodyBones.Chest);
        head=animator.GetBoneTransform(HumanBodyBones.Head);
        spine=animator.GetBoneTransform(HumanBodyBones.Spine);
    }

    public void Apply(Transform weapon, RetroWaterBlaster gun, Vector3 aim, Vector3 bodyForward, float recoil, bool supportHandOnGun, int poseRevision)
    {
        if (!Valid || !gun.mainGrip || !gun.supportGrip) return;
        DownwardBodyLean=0;
        DownwardWaistLean=0;
        float downPose=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,.9f,-aim.y));
        if(chest && supportHandOnGun)
        {
            if(lastPoseRevision!=poseRevision)
            {chestBase=chest.rotation;if(head)headBase=head.rotation;if(spine)spineBase=spine.rotation;lastPoseRevision=poseRevision;}
            if(spine)spine.rotation=spineBase;
            // A slight upper-body turn brings the support shoulder forward,
            // allowing the two-handed level pose shown in the photo references.
            float turn=Mathf.Lerp(32,16,Mathf.Abs(aim.y));
            float lean=aim.y>0?aim.y*10:0;
            // Downward lean is added only when the arm-only pose reaches its
            // clearance/reach limit. Never roll one shoulder upward.
            chest.rotation=Quaternion.AngleAxis(lean,Vector3.Cross(bodyForward,Vector3.up))*Quaternion.AngleAxis(turn,Vector3.up)*chestBase;
            if(head)head.rotation=headBase;
        }
        gun.SetFacing(new Vector2(aim.x, aim.y));
        // The two grip offsets and rotations are never changed by aiming or recoil.
        Vector3 mainOffset = right.WristPosition(gun.mainGrip) - weapon.position;
        Vector3 supportOffset = left.WristPosition(gun.supportGrip) - weapon.position;
        Vector3 mainCentre = right.upper.position - mainOffset;
        Vector3 supportCentre = left.upper.position - supportOffset;
        // Preserve the established level/upward arm pose. Downward aiming is
        // solved from its upper-arm directions and separate forearm lengths.
        float mainReach = right.Length * (.89f-recoil*.007f);
        float supportReach = left.Length * (.985f-recoil*.007f);
        float forwardClearance=Mathf.Lerp(.055f,.30f,Mathf.Abs(aim.y));
        forwardClearance=Mathf.Lerp(forwardClearance,.26f,downPose);
        Vector3 desired = right.upper.position + aim * (mainReach * .88f) + bodyForward * forwardClearance - Vector3.up * .015f - mainOffset;
        desired -= aim * (recoil * .025f);
        float chestLevel=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.65f,Mathf.Abs(aim.y)));
        float gunHeight=chest?chest.position.y+.025f:desired.y;
        Vector3 extended;
        Vector3 mainElbow=Vector3.zero,supportElbow=Vector3.zero;
        Vector3 loweredPosition=desired;
        bool loweredPose=supportHandOnGun && chest && aim.y<0 && TryLoweredPose(aim,bodyForward,mainOffset,supportOffset,mainReach,supportReach,gunHeight,out loweredPosition,out mainElbow,out supportElbow);
        if(loweredPose)desired=loweredPosition;
        else if(supportHandOnGun && OnBothReachSurfaces(desired,mainCentre,mainReach,supportCentre,supportReach,gunHeight,chestLevel,out extended))
            desired=extended;
        else for (int i = 0; i < 96; i++)
        {
            if (supportHandOnGun) desired = InsideSphere(desired, supportCentre, supportReach);
            desired = InsideSphere(desired, mainCentre, mainReach);
        }
        weapon.position = desired;
        if(loweredPose && head)head.rotation=headBase;
        // Use anatomical shoulder sides, including the torso's current turn and
        // lean. Strong lateral poles keep sleeves outside the coat rather than
        // folding the elbows down/back through the ribcage.
        Vector3 across = (right.upper.position-left.upper.position).normalized;
        Vector3 mainPole = right.upper.position + (right.WristPosition(gun.mainGrip) - right.upper.position) * .5f - Vector3.up * .04f + bodyForward * .06f + across * .5f;
        Vector3 supportPole = left.upper.position + (left.WristPosition(gun.supportGrip) - left.upper.position) * .5f - Vector3.up * .04f + bodyForward * .06f - across * .5f;
        if(loweredPose){mainPole=mainElbow;supportPole=supportElbow;}
        float downwardBend=DownwardBendWeight(Mathf.Asin(Mathf.Clamp01(-aim.y))*Mathf.Rad2Deg);
        right.Solve(gun.mainGrip, mainPole,downwardBend);
        if (supportHandOnGun) left.Solve(gun.supportGrip, supportPole);
        MainPositionError = Vector3.Distance(right.PalmPosition, gun.mainGrip.position);
        SupportPositionError = supportHandOnGun ? Vector3.Distance(left.PalmPosition, gun.supportGrip.position) : 0;
        MainRotationError = Quaternion.Angle(right.hand.rotation, gun.mainGrip.rotation * right.palmToWrist);
        SupportRotationError = supportHandOnGun ? Quaternion.Angle(left.hand.rotation, gun.supportGrip.rotation * left.palmToWrist) : 0;
    }

    bool TryLoweredPose(Vector3 aim,Vector3 bodyForward,Vector3 mainOffset,Vector3 supportOffset,float mainReach,float supportReach,float gunHeight,out Vector3 result,out Vector3 mainElbow,out Vector3 supportElbow)
    {
        result=mainElbow=supportElbow=Vector3.zero;
        Vector3 levelDirection=bodyForward.x<0?Vector3.left:Vector3.right;
        Quaternion unpitch=Quaternion.FromToRotation(aim,levelDirection);
        Vector3 levelMainOffset=unpitch*mainOffset,levelSupportOffset=unpitch*supportOffset;
        Quaternion levelChest=Quaternion.AngleAxis(32,Vector3.up)*chestBase;
        Quaternion toLevel=levelChest*Quaternion.Inverse(chest.rotation);
        Vector3 levelRight=chest.position+toLevel*(right.upper.position-chest.position);
        Vector3 levelLeft=chest.position+toLevel*(left.upper.position-chest.position);
        Vector3 levelAcross=(levelRight-levelLeft).normalized;
        Vector3 levelDesired=levelRight+levelDirection*(mainReach*.88f)+bodyForward*.055f-Vector3.up*.015f-levelMainOffset;
        Vector3 levelOrigin;
        if(!OnBothReachSurfaces(levelDesired,levelRight-levelMainOffset,mainReach,levelLeft-levelSupportOffset,supportReach,gunHeight,1,out levelOrigin))return false;
        float ra=Vector3.Distance(right.upper.position,right.lower.position),rb=Vector3.Distance(right.lower.position,right.hand.position);
        float la=Vector3.Distance(left.upper.position,left.lower.position),lb=Vector3.Distance(left.lower.position,left.hand.position);
        Vector3 levelMainHand=levelOrigin+levelMainOffset,levelSupportHand=levelOrigin+levelSupportOffset;
        Vector3 levelMainElbow=Arm.ElbowPosition(levelRight,levelMainHand,ra,rb,levelRight+(levelMainHand-levelRight)*.5f-Vector3.up*.04f+bodyForward*.06f+levelAcross*.5f);
        Vector3 levelSupportElbow=Arm.ElbowPosition(levelLeft,levelSupportHand,la,lb,levelLeft+(levelSupportHand-levelLeft)*.5f-Vector3.up*.04f+bodyForward*.06f-levelAcross*.5f);
        var reference=new LoweredReference
        {
            forward=bodyForward,mainOffset=mainOffset,supportOffset=supportOffset,
            mainHand=levelMainHand,mainUpper=levelMainElbow-levelRight,supportUpper=levelSupportElbow-levelLeft,
            mainForearm=rb,supportForearm=lb,angle=Mathf.Asin(Mathf.Clamp01(-aim.y))*Mathf.Rad2Deg,
            mainReach=mainReach,supportReach=supportReach,
            turn=Mathf.Lerp(32,16,Mathf.Abs(aim.y)),
            mainClearance=ArmClearance(levelRight,levelMainElbow,levelMainHand,bodyForward,Quaternion.identity),
            supportClearance=ArmClearance(levelLeft,levelSupportElbow,levelSupportHand,bodyForward,Quaternion.identity)
        };
        // Try the arms alone first. Only when they cannot keep the gun in
        // front of the body comfortably do we add the minimum torso lean.
        var pose=SampleLoweredPose(reference,0);
        if(!Comfortable(reference,pose))
        {
            float lo=0,hi=reference.angle;
            var end=SampleLoweredPose(reference,hi);
            if(end.valid)
            {
                for(int i=0;i<9;i++)
                {
                    float mid=(lo+hi)*.5f;
                    if(Comfortable(reference,SampleLoweredPose(reference,mid)))hi=mid;
                    else lo=mid;
                }
                pose=SampleLoweredPose(reference,hi);
            }
        }
        // Sampling mutates the chest, so restore the selected sample even if
        // an extreme candidate could not form a forearm intersection circle.
        pose=SampleLoweredPose(reference,pose.lean);
        if(!pose.valid)return false;
        DownwardBodyLean=pose.lean;
        BlendCloseDownwardPose(reference,levelOrigin,levelMainOffset,levelSupportOffset,levelRight,levelLeft,ref pose);
        result=pose.position;mainElbow=pose.mainElbow;supportElbow=pose.supportElbow;
        return true;
    }

    void BlendCloseDownwardPose(LoweredReference reference,Vector3 levelOrigin,Vector3 levelMainOffset,Vector3 levelSupportOffset,Vector3 levelRight,Vector3 levelLeft,ref LoweredPose pose)
    {
        if(!spine)return;
        float blend=Mathf.SmoothStep(0,1,Mathf.InverseLerp(35,90,reference.angle));
        if(blend<=0)return;
        Quaternion stableChest=chest.rotation;
        Vector3 pitchAxis=Vector3.Cross(reference.forward,Vector3.up).normalized;
        float phase=Mathf.SmoothStep(0,1,reference.angle/90);
        float bodyPhase=Mathf.SmoothStep(0,1,Mathf.InverseLerp(25,90,reference.angle));
        float waist=50*bodyPhase;
        Quaternion closeChest=Quaternion.AngleAxis(5*bodyPhase,reference.forward)*Quaternion.AngleAxis(-35*bodyPhase,pitchAxis)*Quaternion.AngleAxis(Mathf.Lerp(32,60,bodyPhase),Vector3.up)*chestBase;
        spine.rotation=Quaternion.AngleAxis(-waist,pitchAxis)*spineBase;
        chest.rotation=closeChest;
        // The waist hinge absorbs the shoulder lift of the staggered two-hand
        // grip. Keep the trigger-side shoulder at its relaxed level height.
        if(right.upper.position.y>levelRight.y+.025f)
        {
            float lo=waist,hi=90;
            for(int i=0;i<12;i++)
            {
                float mid=(lo+hi)*.5f;
                spine.rotation=Quaternion.AngleAxis(-mid,pitchAxis)*spineBase;chest.rotation=closeChest;
                if(right.upper.position.y>levelRight.y+.025f)lo=mid;else hi=mid;
            }
            waist=hi;
            spine.rotation=Quaternion.AngleAxis(-waist,pitchAxis)*spineBase;chest.rotation=closeChest;
        }
        Quaternion closeSpine=spine.rotation;
        float mainReach=reference.mainReach,supportReach=reference.supportReach;
        // Use the same bend as level aim at the trigger hand, while the front
        // hand stays near full reach. Both wrists remain on the rigid gun.
        Vector3 centre,vertical,horizontal;
        if(!ReachCircle(levelRight-levelMainOffset,mainReach,levelLeft-levelSupportOffset,supportReach,out centre,out vertical,out horizontal))
        {spine.rotation=spineBase;chest.rotation=stableChest;return;}
        Vector3 delta=levelOrigin-centre;
        float start=Mathf.Atan2(Vector3.Dot(delta,horizontal.normalized),Vector3.Dot(delta,vertical.normalized))*Mathf.Rad2Deg;
        if(!ReachCircle(right.upper.position-reference.mainOffset,mainReach,left.upper.position-reference.supportOffset,supportReach,out centre,out vertical,out horizontal))
        {spine.rotation=spineBase;chest.rotation=stableChest;return;}
        float theta=Mathf.LerpAngle(start,205,phase)*Mathf.Deg2Rad;
        Vector3 closePosition=centre+vertical*Mathf.Cos(theta)+horizontal*Mathf.Sin(theta);
        Vector3 side=Vector3.Slerp((levelRight-levelLeft).normalized,Vector3.Cross(Vector3.up,reference.forward),phase).normalized;
        Vector3 mainHand=closePosition+reference.mainOffset,supportHand=closePosition+reference.supportOffset;
        float ra=reference.mainUpper.magnitude,rb=reference.mainForearm,la=reference.supportUpper.magnitude,lb=reference.supportForearm;
        Vector3 closeMain=Arm.ElbowPosition(right.upper.position,mainHand,ra,rb,right.upper.position+(mainHand-right.upper.position)*.5f-Vector3.up*.04f+reference.forward*.06f+side*.5f,DownwardBendWeight(reference.angle));
        Vector3 closeSupport=Arm.ElbowPosition(left.upper.position,supportHand,la,lb,left.upper.position+(supportHand-left.upper.position)*.5f-Vector3.up*.04f+reference.forward*.06f-side*.5f);
        spine.rotation=Quaternion.Slerp(spineBase,closeSpine,blend);
        Quaternion blendedChest=Quaternion.Slerp(stableChest,closeChest,blend);
        chest.rotation=blendedChest;
        float actualWaist=waist*blend;
        if(right.upper.position.y>levelRight.y+.025f)
        {
            float lo=actualWaist,hi=90;
            for(int i=0;i<12;i++)
            {
                float mid=(lo+hi)*.5f;
                spine.rotation=Quaternion.AngleAxis(-mid,pitchAxis)*spineBase;chest.rotation=blendedChest;
                if(right.upper.position.y>levelRight.y+.025f)lo=mid;else hi=mid;
            }
            actualWaist=hi;
            spine.rotation=Quaternion.AngleAxis(-actualWaist,pitchAxis)*spineBase;chest.rotation=blendedChest;
        }
        pose.position=Vector3.Lerp(pose.position,closePosition,blend);
        for(int i=0;i<96;i++)
        {
            pose.position=InsideSphere(pose.position,left.upper.position-reference.supportOffset,(la+lb)*.995f);
            pose.position=InsideSphere(pose.position,right.upper.position-reference.mainOffset,(ra+rb)*.995f);
        }
        pose.mainElbow=Vector3.Lerp(pose.mainElbow,closeMain,blend);
        pose.supportElbow=Vector3.Lerp(pose.supportElbow,closeSupport,blend);
        DownwardWaistLean=actualWaist;
        DownwardBodyLean=Mathf.Lerp(pose.lean,35*bodyPhase,blend);
    }

    static bool ReachCircle(Vector3 a,float ra,Vector3 b,float rb,out Vector3 centre,out Vector3 vertical,out Vector3 horizontal)
    {
        centre=vertical=horizontal=Vector3.zero;
        float d=Vector3.Distance(a,b);
        if(d<.00001f||d>=ra+rb||d<=Mathf.Abs(ra-rb))return false;
        Vector3 axis=(b-a)/d;
        float along=(ra*ra-rb*rb+d*d)/(2*d);
        centre=a+axis*along;
        float radius=Mathf.Sqrt(Mathf.Max(0,ra*ra-along*along));
        vertical=Vector3.ProjectOnPlane(Vector3.up,axis).normalized*radius;
        horizontal=Vector3.Cross(axis,vertical);
        return true;
    }

    LoweredPose SampleLoweredPose(LoweredReference reference,float lean)
    {
        Vector3 pitchAxis=Vector3.Cross(reference.forward,Vector3.up).normalized;
        Quaternion torsoPitch=Quaternion.AngleAxis(-lean,pitchAxis);
        chest.rotation=torsoPitch*Quaternion.AngleAxis(reference.turn,Vector3.up)*chestBase;
        float armAngle=Mathf.Max(0,reference.angle-lean);
        float drop=Mathf.Pow(Mathf.Clamp01(armAngle/60),3);
        var pose=new LoweredPose{lean=lean};
        // Pitch in the body's forward/down plane, preserving the level pose's
        // lateral opening. Do not flare the elbows farther as aiming changes.
        pose.mainElbow=right.upper.position+Quaternion.AngleAxis(-8*drop,pitchAxis)*reference.mainUpper;
        pose.supportElbow=left.upper.position+Quaternion.AngleAxis(-32*drop,pitchAxis)*reference.supportUpper;
        Vector3 wanted=reference.mainHand-reference.mainOffset;
        pose.valid=OnBothReachSurfaces(wanted,pose.mainElbow-reference.mainOffset,reference.mainForearm,pose.supportElbow-reference.supportOffset,reference.supportForearm,wanted.y,0,out pose.position);
        if(pose.valid)
        {
            pose.mainElbow=Arm.ElbowPosition(right.upper.position,pose.position+reference.mainOffset,reference.mainUpper.magnitude,reference.mainForearm,pose.mainElbow,DownwardBendWeight(reference.angle));
            float main=ArmClearance(right.upper.position,pose.mainElbow,pose.position+reference.mainOffset,reference.forward,torsoPitch)-reference.mainClearance;
            float support=ArmClearance(left.upper.position,pose.supportElbow,pose.position+reference.supportOffset,reference.forward,torsoPitch)-reference.supportClearance;
            pose.clearance=Mathf.Min(main,support);
        }
        return pose;
    }

    static float DownwardBendWeight(float angle)
    {
        return Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,14,angle));
    }

    static bool Comfortable(LoweredReference reference,LoweredPose pose)
    {
        Vector3 hand=pose.position+reference.mainOffset;
        return pose.valid && pose.clearance>=-.01f && hand.y<=reference.mainHand.y+.04f && Vector3.Dot(hand-reference.mainHand,reference.forward)>=-.02f;
    }

    float ArmClearance(Vector3 shoulder,Vector3 elbow,Vector3 hand,Vector3 forward,Quaternion torsoPitch)
    {
        // An inexpensive torso envelope, calibrated against the accepted level
        // pose. Actual skinned-surface intersections are checked by the editor
        // regression test; no meshes are baked during gameplay.
        Quaternion unlean=Quaternion.Inverse(torsoPitch);
        Vector3 side=Vector3.Cross(Vector3.up,forward).normalized;
        float minimum=float.PositiveInfinity;
        for(int segment=0;segment<2;segment++)
        for(int i=0;i<=6;i++)
        {
            Vector3 point=segment==0?Vector3.Lerp(shoulder,elbow,Mathf.Lerp(.4f,1,i/6f)):Vector3.Lerp(elbow,hand,i/6f*.9f);
            Vector3 p=unlean*(point-chest.position)+Vector3.up*.06f;
            float x=Vector3.Dot(p,forward)/.18f,z=Vector3.Dot(p,side)/.20f,y=p.y/.28f;
            minimum=Mathf.Min(minimum,x*x+z*z+y*y);
        }
        return minimum;
    }

    static Vector3 InsideSphere(Vector3 point, Vector3 centre, float radius)
    {
        Vector3 delta = point - centre;
        return delta.sqrMagnitude > radius * radius ? centre + delta.normalized * radius : point;
    }

    static bool OnBothReachSurfaces(Vector3 desired, Vector3 a, float ra, Vector3 b, float rb, float targetHeight, float heightWeight, out Vector3 result)
    {
        result=desired;
        Vector3 delta=b-a;
        float distance=delta.magnitude;
        if(distance<.00001f || distance>=ra+rb || distance<=Mathf.Abs(ra-rb))return false;
        Vector3 axis=delta/distance;
        float along=(ra*ra-rb*rb+distance*distance)/(2*distance);
        Vector3 centre=a+axis*along;
        float radius=Mathf.Sqrt(Mathf.Max(0,ra*ra-along*along));
        Vector3 outward=Vector3.ProjectOnPlane(desired-centre,axis);
        if(outward.sqrMagnitude<.00001f)outward=Vector3.ProjectOnPlane(Vector3.up,axis);
        if(outward.sqrMagnitude<.00001f)outward=Vector3.ProjectOnPlane(Vector3.forward,axis);
        result=centre+outward.normalized*radius;
        if(heightWeight>0)
        {
            // Pick the chest-height point ON the same reach circle. Translating
            // the gun downward afterwards would break the fixed hand contacts.
            Vector3 vertical=Vector3.ProjectOnPlane(Vector3.up,axis);
            float verticalMagnitude=vertical.magnitude;
            if(verticalMagnitude>.0001f)
            {
                vertical/=verticalMagnitude;
                float y=Mathf.Lerp(result.y,targetHeight,heightWeight);
                float verticalOffset=Mathf.Clamp((y-centre.y)/verticalMagnitude,-radius,radius);
                Vector3 horizontal=Vector3.Cross(axis,vertical).normalized;
                float horizontalOffset=Mathf.Sqrt(Mathf.Max(0,radius*radius-verticalOffset*verticalOffset));
                if(Vector3.Dot(desired-centre,horizontal)<0)horizontal=-horizontal;
                result=centre+vertical*verticalOffset+horizontal*horizontalOffset;
            }
        }
        return true;
    }
}
