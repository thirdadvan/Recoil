using UnityEngine;
using UnityEngine.AI;

// Compatibility name for existing scenes. Pure locomotion adapter: no AI state or wake/watch/chase rules.
[DisallowMultipleComponent,RequireComponent(typeof(RampagerController)),AddComponentMenu("Recoil/3D/Rampager Navigation Motor")]
public sealed class RampagerDepthArrival : MonoBehaviour
{
    public enum TravelPhase { Waiting, Reacting, Travelling, WaitingForPath, Joined }
    [Tooltip("自动找到场景共用的导航数据，无需设置起点或连接路径。")]
    public RampagerDepthNavigation navigation;
    [Min(.1f)] public float repathInterval=.5f;
    [SerializeField] TravelPhase phase;
    public TravelPhase Phase {get{return phase;}}
    public bool JoinedPlane {get{return Mathf.Abs(transform.position.z)<.08f;}}
    public bool Travelling {get{return phase==TravelPhase.Travelling;}}
    public float VisualYaw {get;private set;}
    public Vector3 SpawnPosition {get{return spawn;}}
    public int PathCount {get{return corners.Length;}}
    NavMeshPath path;
    bool[] protectedSegments=new bool[0];
    Vector3[] corners=new Vector3[0];Vector3 spawn,lastDestination;
    RampagerMovement motor;bool initialized,hasDestination;int index;float retry;
    public void Initialize(RecoilPrototype context,RampagerController controller,RampagerMovement movement)
    {
        motor=movement;if(path==null)path=new NavMeshPath();if(!initialized){spawn=transform.position;initialized=true;}
        navigation=context.GetComponentInChildren<RampagerDepthNavigation>(true);
        if(!navigation)navigation=context.gameObject.AddComponent<RampagerDepthNavigation>();navigation.Initialize(context);
    }
    public void ResetTransit()
    {
        if(!initialized)return;corners=new Vector3[0];protectedSegments=new bool[0];index=0;retry=0;hasDestination=false;
        motor.ResetMovement();
        phase=TravelPhase.Waiting;VisualYaw=motor.VisualYaw;
    }
    public void Face(Vector3 focus,bool tracking,float dt)
    {
        if(!tracking && !motor.TurnRequested)return;
        Vector3 direction=focus-transform.position;
        float desired=tracking&&new Vector2(direction.x,direction.z).sqrMagnitude>.00001f?Mathf.Atan2(-direction.x,-direction.z)*Mathf.Rad2Deg:motor.Facing>0?-90:90;
        VisualYaw=Mathf.MoveTowardsAngle(VisualYaw,desired,motor.turnSpeed*Mathf.Max(0,dt));motor.SetVisualYaw(VisualYaw);
    }
    public void Stop(){phase=TravelPhase.Waiting;}
    // Controller supplies destination, speed budget and stopping distance for every ordinary monster.
    public bool MoveTo(Vector3 destination,float budget,float stopDistance,float dt,bool keepFacing,bool allowPartial=false)
    {
        if(!initialized||!navigation||budget<=0)return false;
        Vector3 before=transform.position;destination.y-=.8f;
        Vector3 feet=before-Vector3.up*.8f;
        if(Vector3.Distance(feet,destination)<=stopDistance+(stopDistance>0?.025f:.1f)){Stop();return true;}
        retry-=Mathf.Max(0,dt);
        if(!hasDestination || (index>=protectedSegments.Length || !protectedSegments[index]) && ((destination-lastDestination).sqrMagnitude>.36f || retry<=0))
        {
            retry=repathInterval;lastDestination=destination;hasDestination=true;
            if(!navigation.FindPath(feet,destination,path,allowPartial)){corners=new Vector3[0];protectedSegments=new bool[0];index=0;phase=TravelPhase.WaitingForPath;return false;}
            corners=navigation.Corners(path,out protectedSegments);index=0;
        }
        if(corners.Length==0)return false;
        phase=TravelPhase.Travelling;
        budget=Mathf.Min(budget,Mathf.Max(0,Vector3.Distance(feet,destination)-stopDistance));
        while(index<corners.Length && budget>0)
        {
            Vector3 goal=corners[index];float gap=Vector3.Distance(feet,goal);
            if(gap<.025f){index++;continue;}
            Vector3 next=Vector3.MoveTowards(feet,goal,budget);budget-=Vector3.Distance(feet,next);feet=next;
            if(Vector3.Distance(feet,goal)<.001f)index++;else break;
        }
        Vector3 position=feet+Vector3.up*.8f;
        motor.SetNavigationPosition(position);
        if(!keepFacing && (position-before).sqrMagnitude>.000001f)
        {
            var step=position-before;if(Mathf.Abs(step.x)>.001f)motor.LookAt(position.x+step.x);
            Face(position+step,true,dt);
        }
        if(index>=corners.Length)Stop();return (position-before).sqrMagnitude>.0000001f;
    }
    void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;for(int i=index;i<corners.Length;i++)Gizmos.DrawLine(i==index?transform.position:corners[i-1]+Vector3.up*.8f,corners[i]+Vector3.up*.8f);}
}
