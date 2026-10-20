using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Walkable Stairway")]
public sealed class WalkableStairway : MonoBehaviour
{
    [Tooltip("按从低到高的顺序放置脚底路径点，包括折返平台。所有点应属于此组件的子物体。")]
    public Transform[] routePoints;
    [Min(.1f)] public float travelSpeed=3.6f;
    [Min(.1f)] public float entryRadius=1.25f;
    [Min(.01f)] public float entryHeightTolerance=.3f;
    [Min(.05f),Tooltip("空中按方向键时，脚底与楼梯路径的最大抓取距离。") ]
    public float catchRadius=.55f;
    [Tooltip("关闭表示上层平台损坏，不能从上端进入或抓住平台缺口。")]
    public bool upperLandingAvailable=true;
    [Min(0)] public float brokenUpperDistance=.8f;
    public float Length {get{float length=0;if(routePoints!=null)for(int i=1;i<routePoints.Length;i++)if(routePoints[i-1]&&routePoints[i])length+=Vector2.Distance(routePoints[i-1].position,routePoints[i].position);return length;}}
    public bool Available {get{if(!isActiveAndEnabled || routePoints==null || routePoints.Length<2)return false;foreach(var point in routePoints)if(!point)return false;return Length>.01f;}}
    public Vector2 PositionAt(float distance)
    {
        if(!Available)return transform.position;
        distance=Mathf.Clamp(distance,0,Length);
        for(int i=1;i<routePoints.Length;i++){
            var a=(Vector2)routePoints[i-1].position;var b=(Vector2)routePoints[i].position;float length=Vector2.Distance(a,b);
            if(distance<=length || i==routePoints.Length-1)return Vector2.Lerp(a,b,length>.0001f?distance/length:0);
            distance-=length;
        }
        return routePoints[routePoints.Length-1].position;
    }
    public bool TryEntry(Vector2 feet,float vertical,out float distance)
    {
        distance=vertical>0?0:Length;if(vertical<0&&!upperLandingAvailable)return false;if(!Available || Mathf.Abs(vertical)<.1f)return false;
        var endpoint=PositionAt(distance);
        return Mathf.Abs(feet.x-endpoint.x)<=entryRadius && Mathf.Abs(feet.y-endpoint.y)<=entryHeightTolerance;
    }
    public bool TryCatch(Vector2 feet,out float distance,out float separation)
    {
        distance=0;separation=float.PositiveInfinity;if(!Available)return false;float walked=0;
        for(int i=1;i<routePoints.Length;i++)
        {
            Vector2 a=routePoints[i-1].position,b=routePoints[i].position,delta=b-a;
            float length=delta.magnitude,t=delta.sqrMagnitude>.0001f?Mathf.Clamp01(Vector2.Dot(feet-a,delta)/delta.sqrMagnitude):0;
            float gap=Vector2.Distance(feet,a+delta*t);
            if(gap<separation){separation=gap;distance=walked+length*t;}walked+=length;
        }
        return separation<=catchRadius && (upperLandingAvailable || distance<Length-brokenUpperDistance);
    }
    int SegmentAt(float distance,float heading,out float start,out float end)
    {
        start=0;end=0;
        for(int i=1;i<routePoints.Length;i++)
        {
            end=start+Vector2.Distance(routePoints[i-1].position,routePoints[i].position);
            if(distance<end-.0001f || (distance<=end+.0001f && heading<0) || i==routePoints.Length-1)return i;
            start=end;
        }
        return routePoints.Length-1;
    }
    public Vector2 Advance(Vector2 feet,float horizontal,float vertical,float speedMultiplier,float dt,ref float distance,ref bool approaching,ref float heading,out bool finished)
    {
        finished=false;float step=travelSpeed*Mathf.Max(0,speedMultiplier)*dt;
        float start,end;int segment=SegmentAt(distance,heading,out start,out end);
        Vector2 tangent=routePoints[segment].position-routePoints[segment-1].position;
        float intent=Mathf.Abs(vertical)>.1f?Mathf.Sign(vertical):Mathf.Abs(horizontal)>.1f?Mathf.Sign(horizontal*tangent.x):0;
        bool atTurn=distance>.0001f && distance<Length-.0001f && (Mathf.Abs(distance-start)<.0001f || Mathf.Abs(distance-end)<.0001f);
        // At a switchback, wait for the horizontal key matching the next flight.
        // W/S explicitly selects ascent/descent when both branches share an X direction.
        if(Mathf.Abs(vertical)<.1f && atTurn && intent*heading<0)intent=0;
        if(approaching)
        {
            if(intent==0)return feet;
            if((distance==0 && intent<0) || (distance>0 && intent>0)){finished=true;return feet;}
            heading=intent;var entry=PositionAt(distance);var next=Vector2.MoveTowards(feet,entry,step);
            if(Vector2.Distance(next,entry)<.001f)approaching=false;
            return next;
        }
        if(intent!=0)heading=intent;
        float nextDistance=distance+intent*step;
        if(Mathf.Abs(vertical)<.1f && intent!=0)nextDistance=Mathf.Clamp(nextDistance,start,end);
        distance=Mathf.Clamp(nextDistance,0,Length);
        finished=(intent<0 && distance<=.0001f)||(intent>0 && distance>=Length-.0001f);
        return PositionAt(distance);
    }
    void OnDrawGizmos(){if(routePoints==null)return;Gizmos.color=new Color(.35f,.9f,.75f,.7f);for(int i=0;i<routePoints.Length;i++)if(routePoints[i]){Gizmos.DrawWireSphere(routePoints[i].position,.12f);if(i>0&&routePoints[i-1])Gizmos.DrawLine(routePoints[i-1].position,routePoints[i].position);}}
}
