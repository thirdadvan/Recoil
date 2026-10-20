using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Crawler/Movement")]
public sealed class SurfaceCrawlerMovement : MonoBehaviour
{
    [Min(.1f)] public float crawlSpeed=.9f;
    [Min(.1f)] public float pounceSpeed=13;
    [Min(.1f)] public float fallGravity=14;
    public Vector2 Position {get{return transform.position;}}
    public Vector2 SurfaceNormal {get;private set;}=Vector2.up;
    public bool Attached {get{return support;}}
    public float DistanceTravelled {get;private set;}
    CrawlerNavigation navigation;
    Transform support;
    Vector2 supportLocal,fallVelocity;
    public void Initialize(CrawlerNavigation graph){navigation=graph;}
    public void ResetMotor(){support=null;fallVelocity=Vector2.zero;SurfaceNormal=Vector2.up;DistanceTravelled=0;}
    public void Attach(CrawlerNavigation.Waypoint point)
    {transform.position=point.Position;support=point.support;supportLocal=point.localPosition;SurfaceNormal=point.normal;fallVelocity=Vector2.zero;}
    public void CarryWithSurface()
    {if(support)transform.position=support.TransformPoint(supportLocal);}
    public void Detach(){support=null;fallVelocity=Vector2.zero;}
    public void Crawl(List<CrawlerNavigation.Waypoint> path,ref int index,float dt)
    {
        CarryWithSurface();if(!Attached || !navigation)return;float remaining=Mathf.Max(0,crawlSpeed)*dt;
        while(index<path.Count && remaining>0)
        {
            var goal=path[index];Vector2 before=Position,next=Vector2.MoveTowards(before,goal.Position,remaining),normal;Transform hit;Vector2 end;
            if(navigation.MoveWithCollision(before,next-before,navigation.bodyRadius,false,out end,out normal,out hit))break;
            float distance=Vector2.Distance(before,next);transform.position=next;DistanceTravelled+=distance;remaining-=distance;
            if((next-goal.Position).sqrMagnitude<.00001f){support=goal.support;SurfaceNormal=goal.normal;index++;}
            if(support)supportLocal=support.InverseTransformPoint(Position);
            if(distance<.000001f && index<path.Count && (path[index].Position-next).sqrMagnitude>.00001f)break;
        }
    }
    public bool Burst(Vector2 velocity,float dt,out Vector2 before)
    {
        before=Position;Vector2 end,normal;Transform hit;
        bool collided=navigation.MoveWithCollision(before,velocity*dt,navigation.bodyRadius,true,out end,out normal,out hit);
        transform.position=end;
        if(collided){support=hit;supportLocal=hit.InverseTransformPoint(end);SurfaceNormal=normal;fallVelocity=Vector2.zero;}
        return collided;
    }
    public void Settle(float dt)
    {
        CarryWithSurface();if(Attached)return;
        CrawlerNavigation.Waypoint nearby;
        // Catch a nearby ladder or stair only, never teleport from midair to a distant surface.
        if(navigation.TryAttach(Position,.12f,out nearby)){Attach(nearby);return;}
        fallVelocity.y-=fallGravity*dt;Vector2 before;Burst(fallVelocity,dt,out before);
    }
}
