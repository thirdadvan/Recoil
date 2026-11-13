using UnityEngine;

public sealed partial class DawnPlayerAnimation
{
    bool conversationPhotoOffering;
    float conversationPhotoWeight;
    GameObject conversationPhoto;
    Material conversationPhotoMaterial;
    public void SetConversationPhotoOffer(bool offering)
    {
        conversationPhotoOffering=offering;
        if(!offering && conversationPhoto)conversationPhoto.SetActive(false);
    }
    void ApplyConversationPhotoOffer()
    {
        if(!animator || !animator.isHuman || !facingRoot || conversationPhotoWeight<=0)return;
        var upper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var lower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);
        if(!upper || !lower || !hand)return;
        Vector3 forward=facingRoot.TransformDirection(Vector3.back).normalized;
        Vector3 across=Vector3.Cross(Vector3.up,forward).normalized;
        Vector3 target=upper.position+forward*.43f-Vector3.up*.19f;
        float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
        Vector3 delta=target-upper.position;float d=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,a+b-.001f);
        Vector3 direction=delta.normalized;target=upper.position+direction*d;
        Vector3 pole=upper.position+across*.3f-Vector3.up*.3f;
        Vector3 bend=Vector3.ProjectOnPlane(pole-upper.position,direction).normalized;
        float along=(a*a-b*b+d*d)/(2*d),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
        Vector3 elbow=upper.position+direction*along+bend*height;
        var upperRotation=Quaternion.FromToRotation(lower.position-upper.position,elbow-upper.position)*upper.rotation;
        upper.rotation=Quaternion.Slerp(upper.rotation,upperRotation,conversationPhotoWeight);
        var lowerRotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
        lower.rotation=Quaternion.Slerp(lower.rotation,lowerRotation,conversationPhotoWeight);
        if(!conversationPhoto){
            // The camera sees the back of the print; the story's actual portrait is not replaced by unrelated art.
            conversationPhoto=GameObject.CreatePrimitive(PrimitiveType.Cube);conversationPhoto.name="Conversation photograph - handed to clerk";
            Destroy(conversationPhoto.GetComponent<Collider>());conversationPhoto.transform.SetParent(hand,false);
            var scale=hand.lossyScale;
            conversationPhoto.transform.localScale=new Vector3(.14f/Mathf.Abs(scale.x),.10f/Mathf.Abs(scale.y),.004f/Mathf.Abs(scale.z));
            conversationPhoto.transform.localPosition=new Vector3(0,.055f/Mathf.Abs(scale.y),.025f/Mathf.Abs(scale.z));conversationPhoto.transform.localRotation=Quaternion.Euler(0,0,15);
            conversationPhotoMaterial=new Material(Shader.Find("Standard"));conversationPhotoMaterial.color=new Color(.88f,.85f,.77f);
            conversationPhoto.GetComponent<Renderer>().sharedMaterial=conversationPhotoMaterial;
        }
        conversationPhoto.SetActive(conversationPhotoOffering && conversationPhotoWeight>.2f);
    }
    void OnDestroy()
    {
        if(conversationPhoto)Destroy(conversationPhoto);
        if(conversationPhotoMaterial)Destroy(conversationPhotoMaterial);
    }
}
