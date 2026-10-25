using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Crawler/Visuals")]
public sealed class SurfaceCrawlerVisuals : MonoBehaviour
{
    public Transform visualRoot,abdomen,head;
    public Transform[] upperLimbs,lowerLimbs;
    public SpriteRenderer[] eyes;
    [Min(.1f)] public float visualScale=1;
    float poseTime;
    public void Show(bool show){if(visualRoot)visualRoot.gameObject.SetActive(show);if(!show)poseTime=0;}
    public void Pose(Vector2 normal,Vector2 target,float distance,float preparation,SurfaceCrawlerController.Behaviour state,float dt)
    {
        if(!visualRoot)return;poseTime+=dt;
        float rotation=Mathf.Atan2(normal.y,normal.x)*Mathf.Rad2Deg-90;
        visualRoot.localRotation=Quaternion.Euler(0,0,rotation);
        Vector2 tangent=new Vector2(normal.y,-normal.x);float facing=Vector2.Dot(target,tangent)>=0?1:-1;
        visualRoot.localScale=new Vector3(facing,1,1)*visualScale;
        bool tense=state==SurfaceCrawlerController.Behaviour.PreparingPounce;
        bool moving=state==SurfaceCrawlerController.Behaviour.Crawling;
        if(abdomen)abdomen.localPosition=new Vector3(-.06f,(tense?-.025f:0)+Mathf.Sin(poseTime*2)*.008f,0);
        if(head)head.localPosition=new Vector3(.34f,tense?.04f:.005f,0);
        if(upperLimbs!=null)for(int i=0;i<upperLimbs.Length;i++)
        {
            float gait=moving?Mathf.Sin(distance*12+i*Mathf.PI*.8f)*18:0;
            if(upperLimbs[i])upperLimbs[i].localRotation=Quaternion.Euler(0,0,(i%2==0?-30:28)+gait+(tense?preparation*18:0));
            if(lowerLimbs!=null && i<lowerLimbs.Length && lowerLimbs[i])lowerLimbs[i].localRotation=Quaternion.Euler(0,0,(i%2==0?65:-55)-gait*.7f);
        }
        if(eyes!=null)foreach(var eye in eyes)if(eye)eye.color=tense?Color.Lerp(RecoilPrototype.Hex(0xa95843),RecoilPrototype.Hex(0xffb28a),(.5f+.5f*Mathf.Sin(poseTime*15))*preparation):RecoilPrototype.Hex(0xb67450);
    }
}
