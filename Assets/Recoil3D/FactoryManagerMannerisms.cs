using UnityEngine;

// The folder follows the chest; both hands share its targets, so breathing cannot separate them.
[DisallowMultipleComponent, RequireComponent(typeof(Animator))]
public sealed class FactoryManagerMannerisms : MonoBehaviour
{
    public Transform folderAnchor;
    [Range(0,1)] public float handWeight=1;
    [Min(.1f)] public float poseScale=1;
    Animator animator;
    void Awake(){animator=GetComponent<Animator>();}
    void OnAnimatorIK(int layer)
    {
        if(!animator)animator=GetComponent<Animator>();
        if(!folderAnchor || !animator.isHuman)return;
        Hand(AvatarIKGoal.LeftHand, AvatarIKHint.LeftElbow,-1);
        Hand(AvatarIKGoal.RightHand, AvatarIKHint.RightElbow,1);
    }
    void Hand(AvatarIKGoal goal, AvatarIKHint hint, float side)
    {
        var right=transform.right;var up=transform.up;var forward=transform.forward;
        var center=folderAnchor.position;
        animator.SetIKPositionWeight(goal,handWeight);
        animator.SetIKRotationWeight(goal,0);
        animator.SetIKPosition(goal,center+(right*(side*.06f)-up*.11f+forward*.03f)*poseScale);
        animator.SetIKHintPositionWeight(hint,.8f);
        animator.SetIKHintPosition(hint,center+(right*(side*.19f)+up*.08f-forward*.13f)*poseScale);
    }
    void LateUpdate()
    {
        if(!animator || !animator.isHuman || !folderAnchor)return;
        OrientHand(true,-1);OrientHand(false,1);
    }
    void OrientHand(bool left,float side)
    {
        var hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
        var index=animator.GetBoneTransform(left?HumanBodyBones.LeftIndexProximal:HumanBodyBones.RightIndexProximal);
        var little=animator.GetBoneTransform(left?HumanBodyBones.LeftLittleProximal:HumanBodyBones.RightLittleProximal);
        if(!hand || !index || !little)return;
        var i=hand.InverseTransformDirection(index.position-hand.position);
        var l=hand.InverseTransformDirection(little.position-hand.position);
        var localBasis=Quaternion.LookRotation((i+l).normalized,Vector3.Cross(i,l).normalized*side);
        // This rig's index/little cross product points toward the back of the hand.
        // Point that normal outward so the palms rest against the folder/body.
        var elbow=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
        var alongArm=elbow?(hand.position-elbow.position).normalized:-transform.up;
        var fingers=Vector3.Slerp(alongArm,-transform.up,.3f).normalized;
        var targetBasis=Quaternion.LookRotation(fingers,transform.forward);
        hand.rotation=Quaternion.Slerp(hand.rotation,targetBasis*Quaternion.Inverse(localBasis),handWeight);
    }

}
