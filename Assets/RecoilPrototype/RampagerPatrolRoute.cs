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
    public RampagerPatrolPoint[] OrderedPoints()
    {return GetComponentsInChildren<RampagerPatrolPoint>().Where(p=>p.enabled && p.GetComponentInParent<RampagerController>()==GetComponent<RampagerController>()).OrderBy(p=>p.order).ToArray();}
    public void Initialize(RampagerController controller)
    {owner=controller;points=OrderedPoints();foreach(var point in points)point.CapturePosition();ResetRoute();}
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
    {if(context!=null && context.Point && context.Point.customAction)context.Point.customAction.Exit(context,true);context=null;}
    void OnDisable(){Suspend();}
    // Called only while roaming. Movement remains under the controller's collision handling.
    public bool Evaluate(Vector2 position,float dt,float facing,float speed,out float direction,out float distance,out float look)
    {
        direction=distance=0;look=facing;
        if(!isActiveAndEnabled || points.Length==0)return false;
        if(complete){look=lastFacing;return true;}
        // Bound the loop so a looping route consisting of zero-duration coincident points cannot hang.
        for(int skipped=0;skipped<points.Length;skipped++)
        {
            var point=points[index];
            if(!point || !point.isActiveAndEnabled){Advance();if(complete)return true;continue;}
            float delta=point.Destination.x-position.x;
            if(Mathf.Abs(delta)>arrivalDistance)
            {direction=Mathf.Sign(delta);distance=Mathf.Min(Mathf.Abs(delta),speed*dt);look=direction;return true;}
            if(context==null)
            {context=new RampagerPatrolContext(owner,point,facing);if(point.customAction)point.customAction.Enter(context);}
            float actionDt=Mathf.Min(dt,Mathf.Max(0,point.duration-context.Elapsed));
            context.Elapsed+=actionDt;
            if(point.customAction)point.customAction.Tick(context,actionDt);
            else switch(point.action)
            {
                case RampagerPatrolPoint.Action.LookLeft:context.Facing=-1;break;
                case RampagerPatrolPoint.Action.LookRight:context.Facing=1;break;
                case RampagerPatrolPoint.Action.LookAround:context.Facing=((int)(context.Elapsed/Mathf.Max(.05f,point.lookInterval))%2==0)?-1:1;break;
            }
            look=context.Facing<0?-1:context.Facing>0?1:facing;lastFacing=look;
            if(context.Elapsed<point.duration)return true;
            if(point.customAction)point.customAction.Exit(context,false);
            context=null;Advance();
            // A non-zero action consumes this frame. The next visit starts next frame.
            if(point.duration>0 || complete)return true;
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
