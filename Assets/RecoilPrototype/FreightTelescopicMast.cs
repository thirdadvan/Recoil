using UnityEngine;

[ExecuteAlways,DefaultExecutionOrder(-640),RequireComponent(typeof(KBKStackerCrane))]
public sealed class FreightTelescopicMast : MonoBehaviour
{
    public Transform topAnchor;
    public Transform[] sections=new Transform[0];
    [Min(.1f)] public float sectionLength=1.85f;
    KBKStackerCrane crane;
    public float Bottom {get;private set;}
    public void ApplyPose()
    {
        if(!crane)crane=GetComponent<KBKStackerCrane>();
        if(!topAnchor||!crane.forkPlatform||sections.Length<2)return;
        float target=crane.forkPlatform.Area.yMax+.1f;
        float spacing=Mathf.Clamp((topAnchor.position.y-target-sectionLength)/(sections.Length-1),0,sectionLength*.90f);
        for(int i=0;i<sections.Length;i++)if(sections[i])sections[i].position=topAnchor.position-Vector3.up*(sectionLength*.5f+i*spacing);
        Bottom=topAnchor.position.y-sectionLength-(sections.Length-1)*spacing;
    }
    void LateUpdate(){ApplyPose();}
    void OnEnable(){ApplyPose();}
}
