using System.Linq;
using UnityEngine;

[AddComponentMenu("Recoil/Rampager Patrol Route")]
[DisallowMultipleComponent,RequireComponent(typeof(RampagerController))]
public sealed class RampagerPatrolRoute : MonoBehaviour
{
    [Tooltip("最后一个点结束后回到第一个点。关闭则完成一次后停留在最后一点。")]
    public bool loop=true;
    [Min(.001f)] public float arrivalDistance=.025f;
    RampagerPatrolPoint[] points=new RampagerPatrolPoint[0];
    RampagerController owner;
    RampagerPatrolContext context;
    int index;
    bool complete;
    float lastFacing=-1;
    public RampagerPatrolPoint CurrentPoint {get{return points.Length>0?points[index]:null;}}
    public float Elapsed {get{return context!=null?context.Elapsed:0;}}
    public bool Complete {get{return complete;}}
    public bool RequestsFacing {get;private set;}
    public int TentacleSweepSide {get;private set;}
    public float TentacleSweepProgress {get;private set;}
    public static float ActionDuration(RampagerPatrolPoint point)
    {return point.duration*(!point.customAction&&point.action!=RampagerPatrolPoint.Action.Stay?2:1);}
    public bool HasPoints
    {
        get{if(!Application.isPlaying)return OrderedPoints().Length>0;for(int i=0;i<points.Length;i++)if(points[i] && points[i].isActiveAndEnabled)return true;return false;}
    }
    public RampagerPatrolPoint[] OrderedPoints()
    {return GetComponentsInChildren<RampagerPatrolPoint>().Where(p=>p.enabled && p.GetComponentInParent<RampagerController>()==GetComponent<RampagerController>()).OrderBy(p=>p.order).ToArray();}
    public void Initialize(RampagerController controller)
    {owner=controller;points=OrderedPoints();var motor=owner.GetComponent<RampagerDepthArrival>();
        foreach(var point in points){point.CapturePosition();Vector3 surface;if(motor && motor.navigation && motor.navigation.Project(point.Destination-Vector3.up*.8f,out surface))point.CapturePosition(surface+Vector3.up*.8f);}
        ResetRoute();}
    public static void CreateDefaultPoints(RampagerController controller)
    {
        var route=controller.GetComponent<RampagerPatrolRoute>();
        if(!route)route=controller.gameObject.AddComponent<RampagerPatrolRoute>();
        if(route.OrderedPoints().Length>0)return;
        var offsets=new[]{controller.leftOffset,0,controller.rightOffset,0};
        var actions=new[]{RampagerPatrolPoint.Action.LookRight,RampagerPatrolPoint.Action.LookAround,RampagerPatrolPoint.Action.LookLeft,RampagerPatrolPoint.Action.Stay};
        for(int i=0;i<offsets.Length;i++)
        {
            var go=new GameObject($"Patrol Point {i:00}");go.transform.SetParent(controller.transform,false);
            go.transform.position=controller.transform.position+Vector3.right*offsets[i];
            var point=go.AddComponent<RampagerPatrolPoint>();point.order=i;point.action=actions[i];point.duration=i==1?3:1.5f;
        }
    }
    public void ResetRoute(){Suspend();index=0;complete=false;lastFacing=-1;}
    public void Suspend()
    {if(context!=null && context.Point && context.Point.customAction)context.Point.customAction.Exit(context,true);context=null;TentacleSweepSide=0;TentacleSweepProgress=0;}
    void OnDisable(){Suspend();}
    // Called only while roaming. Movement remains under the controller's collision handling.
    public bool Evaluate(Vector3 position,float dt,float facing,float speed,out float direction,out float distance,out float look)
    {
        direction=distance=0;look=facing;RequestsFacing=false;TentacleSweepSide=0;TentacleSweepProgress=0;
        if(!isActiveAndEnabled || points.Length==0)return false;
        if(complete){look=lastFacing;return true;}
        // Bound the loop so a looping route consisting of zero-duration coincident points cannot hang.
        for(int skipped=0;skipped<points.Length;skipped++)
        {
            var point=points[index];
            if(!point || !point.isActiveAndEnabled){Advance();if(complete)return true;continue;}
            float delta=point.Destination.x-position.x;
            bool navigation=owner.GetComponent<RampagerDepthArrival>();
            float separation=navigation?new Vector2(delta,point.Destination.z-position.z).magnitude:Mathf.Abs(delta);
            if(navigation && Mathf.Abs(point.Destination.y-position.y)>.3f)separation=Vector3.Distance(point.Destination,position);
            if(separation>(navigation?Mathf.Max(.12f,arrivalDistance):arrivalDistance))
            {direction=Mathf.Sign(delta);distance=Mathf.Min(separation,speed*dt);look=direction;RequestsFacing=direction!=0;return true;}
            if(context==null)
            {context=new RampagerPatrolContext(owner,point,facing);if(point.customAction)point.customAction.Enter(context);}
            float actionDuration=ActionDuration(point);
            float actionDt=Mathf.Min(dt,Mathf.Max(0,actionDuration-context.Elapsed));
            context.Elapsed+=actionDt;
            if(point.customAction)point.customAction.Tick(context,actionDt);
            else if(point.action!=RampagerPatrolPoint.Action.Stay)
            {
                float segment=point.action==RampagerPatrolPoint.Action.LookAround?Mathf.Max(.05f,point.lookInterval)*2:Mathf.Max(.001f,actionDuration);
                int cycle=Mathf.FloorToInt(context.Elapsed/segment);
                TentacleSweepSide=point.action==RampagerPatrolPoint.Action.LookLeft?-1:point.action==RampagerPatrolPoint.Action.LookRight?1:cycle%2==0?-1:1;
                TentacleSweepProgress=point.action==RampagerPatrolPoint.Action.LookAround?Mathf.Repeat(context.Elapsed,segment)/segment:Mathf.Clamp01(context.Elapsed/segment);
            }
            RequestsFacing=point.customAction&&context.Facing!=facing;
            look=context.Facing<0?-1:context.Facing>0?1:facing;lastFacing=look;
            if(context.Elapsed<actionDuration)return true;
            if(point.customAction)point.customAction.Exit(context,false);
            context=null;Advance();
            // A non-zero action consumes this frame. The next visit starts next frame.
            if(actionDuration>0 || complete)return true;
        }
        return true;
    }
    void Advance(){if(index+1<points.Length)index++;else if(loop)index=0;else complete=true;}
    void OnValidate(){arrivalDistance=Mathf.Max(.001f,arrivalDistance);}
    void OnDrawGizmosSelected()
    {
        var ordered=Application.isPlaying?points:OrderedPoints();
        Gizmos.color=new Color(.3f,.85f,1,.8f);
        for(int i=0;i<ordered.Length;i++)if(ordered[i] && ordered[(i+1)%ordered.Length] && (i+1<ordered.Length || loop))
            Gizmos.DrawLine(ordered[i].Destination,ordered[(i+1)%ordered.Length].Destination);
    }
}
