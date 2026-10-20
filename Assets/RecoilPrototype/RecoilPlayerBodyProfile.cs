using UnityEngine;

// Optional actor proportions; movement/input remain in the common player controller.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Player Body Profile")]
public sealed class RecoilPlayerBodyProfile : MonoBehaviour
{
    [Min(.1f)] public float standingHeight=1.12f,crouchingHeight=.66f;
    [Min(.05f)] public float halfWidth=.22f;
    public Transform[] legs=System.Array.Empty<Transform>(),shoes=System.Array.Empty<Transform>();
    void OnValidate(){standingHeight=Mathf.Max(.1f,standingHeight);crouchingHeight=Mathf.Clamp(crouchingHeight,.1f,standingHeight);halfWidth=Mathf.Max(.05f,halfWidth);}
    public void Apply(Transform body,Transform head,Transform hair,bool crouched)
    {
        body.localPosition=new Vector2(0,-.04f);body.localScale=new Vector3(.4f,crouched?.4f:.7f,1);
        head.localPosition=new Vector2(0,crouched?.13f:.36f);head.localScale=new Vector3(.34f,crouched?.26f:.32f,1);
        hair.localPosition=new Vector2(0,crouched?.27f:.5f);hair.localScale=new Vector3(.35f,.12f,1);
        float foot=-(crouched?crouchingHeight:standingHeight)*.5f;
        for(int i=0;i<legs.Length;i++)if(legs[i]){legs[i].localPosition=new Vector2(i==0?-.095f:.095f,foot+(crouched?.065f:.1f));legs[i].localScale=new Vector3(.12f,crouched?.13f:.2f,1);}
        for(int i=0;i<shoes.Length;i++)if(shoes[i]){shoes[i].localPosition=new Vector2(i==0?-.095f:.095f,foot+.035f);shoes[i].localScale=new Vector3(.16f,.07f,1);}
    }
}
