using UnityEngine;

public sealed partial class DawnPlayerAnimation
{
    public bool PropOperationActive {get;private set;}
    public float PropOperationTime {get;private set;}
    Vector3 propTarget;bool propTyping,propWeaponVisible;Quaternion propStartYaw;float propApproachDepth;
    Transform[] propPoseBones;Quaternion[] propBasePose;
    bool propHeldTyping,propTypingReturning;float propTypingReturnStart,propTypingReturnDuration;
    float propDuration=2;Vector3 propInsertedGrip;GameObject propWindingKey;Material propKeyMaterial;
    public bool PropKeyInserted {get;private set;}
    float PropProgress {get{return Mathf.Clamp01(PropOperationTime/Mathf.Max(.05f,propDuration));}}
    float PropReachBlend {get{return propTyping?Mathf.SmoothStep(0,1,PropOperationTime/.28f)*PropTypingReturnWeight:Mathf.SmoothStep(0,1,PropProgress/.35f)*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.78f,1,PropProgress)));}}
    float PropTypingReturnWeight {get{if(propHeldTyping)return propTypingReturning?1-Mathf.SmoothStep(0,1,(PropOperationTime-propTypingReturnStart)/propTypingReturnDuration):1;return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(.35f,propDuration-.55f),propDuration,PropOperationTime));}}
    float PropTypingTurnWeight {get{return propHeldTyping?(propTypingReturning?1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(propTypingReturnStart+propTypingReturnDuration*.25f,propTypingReturnStart+propTypingReturnDuration,PropOperationTime)):1):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(Mathf.Max(.35f,propDuration-.4f),propDuration,PropOperationTime));}}
    public void HoldTypingOperation(){propHeldTyping=true;propTypingReturning=false;}
    public void BeginTypingReturn(float duration){propTypingReturning=true;propTypingReturnStart=PropOperationTime;propTypingReturnDuration=Mathf.Max(.1f,duration);CurrentPose="Leave terminal";if(weapon)weapon.gameObject.SetActive(false);}
    public float TerminalReturnWeight {get{return PropTypingReturnWeight;}}
    public float PropWristTurn {get{return propTyping?0:Mathf.Sin(Mathf.Clamp01(Mathf.InverseLerp(.35f,.78f,PropProgress))*Mathf.PI*3)*42;}}
    public void BeginPropOperation(Vector3 target,bool typing,float duration=2)
    {
        if(!animator||!animator.isHuman||!facingRoot)return;
        propHeldTyping=propTypingReturning=false;PropOperationActive=true;PropOperationTime=0;propDuration=duration;PropKeyInserted=false;propTarget=target;propTyping=typing;propStartYaw=facingRoot.localRotation;propApproachDepth=Mathf.Clamp(target.z-player.WorldPosition.z-.42f,0,typing?.28f:.60f);
        var ids=new[]{HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand};
        propPoseBones=new Transform[ids.Length];propBasePose=new Quaternion[ids.Length];for(int i=0;i<ids.Length;i++){propPoseBones[i]=animator.GetBoneTransform(ids[i]);if(propPoseBones[i])propBasePose[i]=propPoseBones[i].localRotation;}
        propWeaponVisible=weapon&&weapon.gameObject.activeSelf;if(weapon)weapon.gameObject.SetActive(false);
        animator.speed=1;animator.SetFloat(Speed,0);animator.SetFloat(Direction,1);animator.SetFloat(ClimbSpeed,0);
        animator.CrossFadeInFixedTime("Locomotion",.12f,0);if(animator.layerCount>1)animator.SetLayerWeight(1,0);
        if(!typing)CreateWindingKey();
        CurrentPose=typing?"Operate terminal":"Wind clock";AimWeight=0;shotKick=0;previous=player.transform.position;
    }
    public void SetPropOperationTime(float value){PropOperationTime=value;}
    void UpdatePropOperationPresentation()
    {
        if(!PropOperationActive||!facingRoot||!animator)return;
        float turn=Mathf.SmoothStep(0,1,PropOperationTime/.28f);
        turn*=propTyping?PropTypingTurnWeight:1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1,PropProgress));
        facingRoot.localRotation=Quaternion.Slerp(propStartYaw,Quaternion.Euler(0,180,0),turn);
        animator.speed=1;animator.SetFloat(Speed,0);animator.SetFloat(ClimbSpeed,0);if(animator.layerCount>1)animator.SetLayerWeight(1,0);
        if(weapon)weapon.gameObject.SetActive(false);ApplyFixedHeight();
    }
    void FinalizePropOperationPose()
    {
        if(!PropOperationActive||!animator)return;
        for(int i=0;i<propPoseBones.Length;i++)if(propPoseBones[i]){if(Time.deltaTime>0)propBasePose[i]=propPoseBones[i].localRotation;else propPoseBones[i].localRotation=propBasePose[i];}
        float blend=propTyping?Mathf.SmoothStep(0,1,PropOperationTime/.35f)*PropTypingReturnWeight:PropReachBlend,phase=PropOperationTime*13;
        var spine=animator.GetBoneTransform(HumanBodyBones.Spine);if(spine)spine.rotation=Quaternion.AngleAxis((propTyping?65:7)*blend,Vector3.right)*spine.rotation;
        Vector3 centre=propTarget;if(propTyping)centre.y+=.04f;centre.y=Mathf.Clamp(centre.y,player.WorldPosition.y-.12f,player.WorldPosition.y+1.05f);
        // The operation stays on the existing gameplay plane; reach is length-clamped.
        centre.z=Mathf.Min(centre.z,player.WorldPosition.z+propApproachDepth+.65f);
        var right=propTyping?centre+new Vector3(.105f,Mathf.Sin(phase)*.013f,0):propTarget-Vector3.forward*.14f;
        if(!propTyping && PropKeyInserted)right=propInsertedGrip;
        var left=centre+new Vector3(-.105f,propTyping?Mathf.Sin(phase+Mathf.PI)*.013f:-.06f,0);
        SolvePropArm(true,right,blend,propTyping?Mathf.Sin(phase)*6:PropWristTurn);
        if(!propTyping){
            var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
            if(!PropKeyInserted && PropProgress>=.35f && Vector3.Distance(hand.position,right)<.075f){PropKeyInserted=true;propInsertedGrip=right;}
            if(propWindingKey){propWindingKey.transform.position=(PropKeyInserted&&PropProgress<=.78f)?propTarget:hand.position+Vector3.forward*.14f;propWindingKey.transform.rotation=Quaternion.Euler(0,0,PropWristTurn);}
        }
        SolvePropArm(false,left,blend,propTyping?Mathf.Sin(phase+Mathf.PI)*6:0);
    }
    void SolvePropArm(bool right,Vector3 target,float blend,float wrist)
    {
        var upper=animator.GetBoneTransform(right?HumanBodyBones.RightUpperArm:HumanBodyBones.LeftUpperArm);
        var lower=animator.GetBoneTransform(right?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
        var hand=animator.GetBoneTransform(right?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);if(!upper||!lower||!hand)return;
        target=Vector3.Lerp(hand.position,target,blend);var shoulder=upper.position;
        float a=Vector3.Distance(shoulder,lower.position),b=Vector3.Distance(lower.position,hand.position);
        target=shoulder+Vector3.ClampMagnitude(target-shoulder,(a+b)*.985f);
        Vector3 pole=shoulder+Vector3.down*.45f+Vector3.right*(right?.28f:-.28f);
        var elbow=PropElbow(shoulder,target,a,b,pole);
        upper.rotation=Quaternion.FromToRotation(lower.position-shoulder,elbow-shoulder)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
        if(!propTyping && right){
            var finger=animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
            if(finger)hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,Vector3.forward)*hand.rotation;
        }
        hand.rotation=Quaternion.AngleAxis(wrist,propTyping?(hand.position-lower.position).normalized:Vector3.forward)*hand.rotation;
    }
    static Vector3 PropElbow(Vector3 shoulder,Vector3 target,float a,float b,Vector3 pole)
    {
        var direction=(target-shoulder).normalized;float distance=Mathf.Clamp(Vector3.Distance(target,shoulder),Mathf.Abs(a-b)+.0001f,a+b-.0001f);
        var bend=Vector3.ProjectOnPlane(pole-shoulder,direction).normalized;if(bend.sqrMagnitude<.001f)bend=Vector3.down;
        float along=(a*a-b*b+distance*distance)/(2*distance);
        return shoulder+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
    }
    void CreateWindingKey()
    {
        propWindingKey=new GameObject("Winding key - hand operation");propWindingKey.transform.SetParent(player.transform,false);
        propKeyMaterial=new Material(Shader.Find("Standard"));propKeyMaterial.color=new Color(.36f,.28f,.13f);propKeyMaterial.SetFloat("_Metallic",.6f);propKeyMaterial.SetFloat("_Glossiness",.3f);
        Vector3[] positions={new Vector3(0,0,-.032f),new Vector3(0,0,-.076f),new Vector3(-.022f,0,-.076f),new Vector3(.022f,0,-.076f)};
        Vector3[] sizes={new Vector3(.010f,.010f,.064f),new Vector3(.050f,.009f,.010f),new Vector3(.009f,.026f,.010f),new Vector3(.009f,.026f,.010f)};
        for(int i=0;i<positions.Length;i++){var piece=GameObject.CreatePrimitive(PrimitiveType.Cube);piece.name="Winding key part "+i;piece.transform.SetParent(propWindingKey.transform,false);piece.transform.localPosition=positions[i];piece.transform.localScale=sizes[i];var collider=piece.GetComponent<Collider>();collider.enabled=false;Destroy(collider);piece.GetComponent<Renderer>().sharedMaterial=propKeyMaterial;}
    }
    public void EndPropOperation()
    {
        if(!PropOperationActive)return;PropOperationActive=false;propHeldTyping=propTypingReturning=false;CurrentPose="Locomotion";
        if(facingRoot){facingRoot.localRotation=propStartYaw;yaw=propStartYaw.eulerAngles.y;}
        if(animator){animator.SetFloat(Speed,0);animator.SetFloat(ClimbSpeed,0);animator.CrossFadeInFixedTime("Locomotion",transitionTime,0);}
        if(propWindingKey)Destroy(propWindingKey);if(propKeyMaterial)Destroy(propKeyMaterial);propWindingKey=null;propKeyMaterial=null;PropKeyInserted=false;
        if(weapon&&player)weapon.gameObject.SetActive(propWeaponVisible&&player.HasWaterGun);
        previous=player?player.transform.position:transform.position;
    }
}
