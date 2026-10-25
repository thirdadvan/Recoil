using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(SurfaceCrawlerMovement),typeof(SurfaceCrawlerVisuals))]
[AddComponentMenu("Recoil/Crawler/Controller")]
public sealed class SurfaceCrawlerController : MonoBehaviour
{
    public enum Behaviour { Waiting, Crawling, PreparingPounce, Pouncing, Recovering }
    [Header("Pounce")]
    [Min(.1f)] public float pounceRange=6;
    [Min(.01f)] public float preparationTime=1.2f;
    [Min(.05f)] public float recoveryTime=.8f;
    [Min(.1f)] public float replanInterval=.5f;
    [Min(0)] public float knockbackStrength=5;
    [Header("Water hit")]
    [Min(0)] public float waterStunDuration=.65f;
    public Behaviour State {get;private set;}
    public bool Present {get{return State!=Behaviour.Waiting;}}
    public float PreparationProgress {get{return State==Behaviour.PreparingPounce?1-Mathf.Clamp01(remaining/Mathf.Max(.01f,preparationTime)):0;}}
    public SurfaceCrawlerMovement Motor {get;private set;}
    readonly List<CrawlerNavigation.Waypoint> path=new List<CrawlerNavigation.Waypoint>();
    RecoilPrototype world;
    CrawlerNavigation navigation;
    SurfaceCrawlerVisuals visuals;
    Vector2 pounceVelocity;
    float remaining,replanRemaining,travelRemaining,pounceDepthVelocity;
    int pathIndex,graphVersion;
    bool struck,repositioning;
    public void Initialize(RecoilPrototype context,CrawlerNavigation graph)
    {
        world=context;navigation=graph;Motor=GetComponent<SurfaceCrawlerMovement>();Motor.Initialize(graph);
        visuals=GetComponent<SurfaceCrawlerVisuals>();ResetCrawler();
    }
    public void ResetCrawler()
    {
        State=Behaviour.Waiting;remaining=replanRemaining=0;path.Clear();pathIndex=0;struck=repositioning=false;
        if(Motor)Motor.ResetMotor();if(visuals)visuals.Show(false);
    }
    public void Appear(CrawlerNavigation.Waypoint point)
    {
        Motor.ResetMotor();Motor.Attach(point);repositioning=false;State=Behaviour.Crawling;replanRemaining=0;path.Clear();pathIndex=0;visuals.Show(true);
        visuals.Pose(Motor.SurfaceNormal,world.Player.Position-Motor.Position,0,0,State,0);
    }
    public bool CanSeePlayer()
    {return Present && world && world.Player && Vector3.Distance(transform.position,world.Player.WorldPosition)<=pounceRange && world.Clear(Motor.Position,world.Player.Position);}
    public void Simulate(float dt)
    {
        if(!Present || !world || !world.Player || dt<=0)return;
        float previousDepth=transform.position.z;
        float nextDepth=State==Behaviour.Pouncing?previousDepth+pounceDepthVelocity*Mathf.Min(dt,travelRemaining):Mathf.MoveTowards(previousDepth,world.Player.GameplayZ,Motor.crawlSpeed*dt);
        Motor.CarryWithSurface();Vector2 target=world.Player.Position;
        if(State==Behaviour.Crawling)
        {
            if(!Motor.Attached)Motor.Settle(dt);
            if(Motor.Attached)
            {
                if(repositioning)
                {
                    if(path.Count==0 || graphVersion!=navigation.Version)
                    {
                        bool found=navigation.TryRepositionPath(Motor.Position,target,path);BeginPath();
                        if(!found)repositioning=false;
                    }
                    Motor.Crawl(path,ref pathIndex,dt);
                    if(pathIndex>=path.Count){repositioning=false;path.Clear();replanRemaining=0;}
                }
                else if(CanPrepare(target)){State=Behaviour.PreparingPounce;remaining=preparationTime;path.Clear();}
                else
                {
                    replanRemaining-=dt;
                    if(replanRemaining<=0 || graphVersion!=navigation.Version)
                    {navigation.TryPath(Motor.Position,target,pounceRange,path);BeginPath();replanRemaining=replanInterval;}
                    Motor.Crawl(path,ref pathIndex,dt);
                }
            }
        }
        else if(State==Behaviour.PreparingPounce)
        {
            remaining-=dt;
            if(!Motor.Attached){State=Behaviour.Crawling;replanRemaining=0;}
            else if(remaining<=.000001f)
            {
                // Aim is locked only at takeoff. Dodging during the lunge remains possible.
                Vector3 delta=world.Player.WorldPosition-new Vector3(Motor.Position.x,Motor.Position.y,nextDepth);float length=Mathf.Min(delta.magnitude,pounceRange);
                if(delta.sqrMagnitude<.001f)delta=Vector3.right;
                Vector2 endpoint=Motor.Position+(Vector2)delta.normalized*length;
                if(navigation.SweepClear(Motor.Position,endpoint,navigation.bodyRadius))
                {pounceVelocity=(Vector2)delta.normalized*Motor.pounceSpeed;pounceDepthVelocity=delta.normalized.z*Motor.pounceSpeed;travelRemaining=length/Mathf.Max(.1f,Motor.pounceSpeed);struck=false;Motor.Detach();State=Behaviour.Pouncing;}
                else{State=Behaviour.Crawling;replanRemaining=0;}
            }
        }
        else if(State==Behaviour.Pouncing)
        {
            float step=Mathf.Min(dt,travelRemaining);Vector2 before;bool collided=Motor.Burst(pounceVelocity,step,out before);travelRemaining-=step;
            float enter,exit;float halfHeight=world.Player.BodyHeight*.5f+navigation.bodyRadius;
            var playerBox=new Rect(target.x-.32f-navigation.bodyRadius,target.y-halfHeight,.64f+navigation.bodyRadius*2,halfHeight*2);
            if(!struck && CrawlerNavigation.Clip(before,Motor.Position,playerBox,out enter,out exit) && Mathf.Abs(Mathf.Lerp(previousDepth,nextDepth,Mathf.Clamp01(enter))-world.Player.GameplayZ)<=navigation.bodyRadius+.25f)
            {
                struck=true;Vector2 direction=pounceVelocity.normalized;
                world.CatchPlayer(new Vector2(direction.x*knockbackStrength,Mathf.Clamp(direction.y*knockbackStrength,-3,3)+1));
                BeginRecovery(recoveryTime);
            }
            else if(collided || travelRemaining<=.000001f)BeginRecovery(recoveryTime);
        }
        else if(State==Behaviour.Recovering)
        {
            Motor.Settle(dt);remaining-=dt;
            if(remaining<=0){State=Behaviour.Crawling;replanRemaining=0;path.Clear();}
        }
        transform.position=new Vector3(Motor.Position.x,Motor.Position.y,nextDepth);
        visuals.Pose(Motor.SurfaceNormal,State==Behaviour.Pouncing?pounceVelocity:target-Motor.Position,Motor.DistanceTravelled,PreparationProgress,State,dt);
    }
    void BeginPath()
    {
        pathIndex=0;graphVersion=navigation.Version;
        // Avoid walking back to the previous sample every time the moving target causes a replan.
        if(path.Count>1 && navigation.SweepClear(Motor.Position,path[1].Position,navigation.bodyRadius))pathIndex=1;
    }
    bool CanPrepare(Vector2 target)
    {return (world.Player.WorldPosition-transform.position).sqrMagnitude<=pounceRange*pounceRange && navigation.SweepClear(Motor.Position,target,navigation.bodyRadius);}
    void BeginRecovery(float duration){State=Behaviour.Recovering;remaining=Mathf.Max(.05f,duration);path.Clear();repositioning=true;}
    public void TakeWaterHit(Vector2 start,Vector2 direction,float range)
    {
        if(!Present || Mathf.Abs(transform.position.z-world.Player.GameplayZ)>navigation.bodyRadius+.18f)return;float along=Vector2.Dot(Motor.Position-start,direction);
        if(along<0 || along>range || Vector2.Distance(Motor.Position,start+direction*along)>navigation.bodyRadius+.18f)return;
        BeginRecovery(waterStunDuration);
    }
    void OnDrawGizmosSelected()
    {Gizmos.color=new Color(1,.35f,.2f,.35f);Gizmos.DrawWireSphere(transform.position,pounceRange);}
}
