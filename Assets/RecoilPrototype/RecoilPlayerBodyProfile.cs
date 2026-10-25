using UnityEngine;

// Optional actor proportions; movement/input remain in the common player controller.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Player Body Profile")]
public sealed class RecoilPlayerBodyProfile : MonoBehaviour
{
    [Min(.1f)] public float standingHeight=1.12f;
    [Min(.05f)] public float halfWidth=.22f;
    public Transform[] legs=System.Array.Empty<Transform>(),shoes=System.Array.Empty<Transform>();
    void OnValidate(){standingHeight=Mathf.Max(.1f,standingHeight);halfWidth=Mathf.Max(.05f,halfWidth);}
    public void Apply(Transform body,Transform head,Transform hair)
    {
        body.localPosition=new Vector2(0,-.04f);body.localScale=new Vector3(.4f,.7f,1);
        head.localPosition=new Vector2(0,.36f);head.localScale=new Vector3(.34f,.32f,1);
        hair.localPosition=new Vector2(0,.5f);hair.localScale=new Vector3(.35f,.12f,1);
        float foot=-standingHeight*.5f;
        for(int i=0;i<legs.Length;i++)if(legs[i]){legs[i].localPosition=new Vector2(i==0?-.095f:.095f,foot+.1f);legs[i].localScale=new Vector3(.12f,.2f,1);}
        for(int i=0;i<shoes.Length;i++)if(shoes[i]){shoes[i].localPosition=new Vector2(i==0?-.095f:.095f,foot+.035f);shoes[i].localScale=new Vector3(.16f,.07f,1);}
    }
}
