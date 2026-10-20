using System;
using System.Collections.Generic;
using UnityEngine;

// A small surface graph: only supported lines are connected, never arbitrary empty-space nodes.
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider2D))]
[AddComponentMenu("Recoil/Crawler/Surface Navigation")]
public sealed class CrawlerNavigation : MonoBehaviour
{
    [Min(.1f)] public float nodeSpacing=.5f;
    [Min(.05f)] public float bodyRadius=.22f;
    [Min(.1f)] public float dynamicRefreshInterval=.5f;
    public bool showGraph;
    public int Version {get; private set;}
    public int NodeCount {get{return nodes.Count;}}
    public struct Waypoint
    {
        public Transform support;
        public Vector2 localPosition,normal;
        public Vector2 Position {get{return support?(Vector2)support.TransformPoint(localPosition):localPosition;}}
    }
    sealed class Node
    {
        public Waypoint point;
        public readonly List<int> links=new List<int>(4);
    }
    sealed class Segment
    {
        public Vector2 a,b,normal;
        public Transform support;
        public readonly List<float> cuts=new List<float>();
    }
    sealed class Source
    {
        public BoxCollider2D collider;
        public bool hard;
        public Bounds snapshot;
        public bool enabled;
    }
    readonly List<Source> sources=new List<Source>();
    readonly List<Node> nodes=new List<Node>();
    readonly List<Segment> segments=new List<Segment>();
    readonly Dictionary<Vector2Int,List<int>> buckets=new Dictionary<Vector2Int,List<int>>();
    ClimbableLadder[] ladders;
    WalkableStairway[] stairs;
    BoxCollider2D volume;
    RecoilPrototype world;
    float refreshAt;
    float[] cost=new float[0];
    int[] parent=new int[0];
    bool[] closed=new bool[0];
    readonly List<int> reversePath=new List<int>();
    struct HeapEntry {public int node;public float priority;}
    readonly List<HeapEntry> heap=new List<HeapEntry>();
    public Rect Area {get{if(!volume)volume=GetComponent<BoxCollider2D>();var b=volume.bounds;return new Rect(b.min,b.size);}}
    float Offset {get{return bodyRadius+.025f;}}
    public void Initialize(RecoilPrototype context)
    {
        world=context;volume=GetComponent<BoxCollider2D>();sources.Clear();
        foreach(var c in context.GetComponentsInChildren<BoxCollider2D>(true))
        {
            if(c==volume)continue;
            bool soft=c.GetComponent<SoftRoofPlatform>();
            if(soft || (!c.isTrigger && !c.GetComponent<ClimbableLadder>()))sources.Add(new Source{collider=c,hard=!soft});
        }
        ladders=context.GetComponentsInChildren<ClimbableLadder>(true);
        stairs=context.GetComponentsInChildren<WalkableStairway>(true);
        Rebuild();
    }
    public void Refresh(float now)
    {
        if(now<refreshAt)return;refreshAt=now+Mathf.Max(.1f,dynamicRefreshInterval);
        foreach(var s in sources)
        {
            bool active=s.collider && s.collider.enabled && s.collider.gameObject.activeInHierarchy;
            if(active!=s.enabled || (active && ((s.collider.bounds.center-s.snapshot.center).sqrMagnitude>.0025f || (s.collider.bounds.size-s.snapshot.size).sqrMagnitude>.0025f)))
            {Rebuild();return;}
        }
    }
    public void Rebuild()
    {
        nodes.Clear();segments.Clear();buckets.Clear();
        if(!world)return;
        foreach(var s in sources)
        {
            s.enabled=s.collider && s.collider.enabled && s.collider.gameObject.activeInHierarchy;
            if(!s.enabled)continue;s.snapshot=s.collider.bounds;var b=s.snapshot;float d=Offset;
            if(s.hard)
            {
                AddSegment(new Vector2(b.min.x-d,b.max.y+d),new Vector2(b.max.x+d,b.max.y+d),Vector2.up,s.collider.transform);
                AddSegment(new Vector2(b.min.x-d,b.min.y-d),new Vector2(b.max.x+d,b.min.y-d),Vector2.down,s.collider.transform);
                AddSegment(new Vector2(b.min.x-d,b.min.y-d),new Vector2(b.min.x-d,b.max.y+d),Vector2.left,s.collider.transform);
                AddSegment(new Vector2(b.max.x+d,b.min.y-d),new Vector2(b.max.x+d,b.max.y+d),Vector2.right,s.collider.transform);
            }
            else AddSegment(new Vector2(b.min.x,b.max.y+d),new Vector2(b.max.x,b.max.y+d),Vector2.up,s.collider.transform);
        }
        if(ladders!=null)foreach(var l in ladders)if(l && l.Available)
        {var r=l.Area;AddSegment(new Vector2(r.center.x,r.yMin+Offset),new Vector2(r.center.x,r.yMax+Offset),Vector2.up,l.transform);}
        if(stairs!=null)foreach(var s in stairs)if(s && s.Available)
        {
            float validLength=s.Length-(s.upperLandingAvailable?0:s.brokenUpperDistance),walked=0;
            for(int i=1;i<s.routePoints.Length && walked<validLength;i++)
            {
                Vector2 a=s.routePoints[i-1].position,b=s.routePoints[i].position;float length=Vector2.Distance(a,b);
                b=Vector2.Lerp(a,b,Mathf.Clamp01((validLength-walked)/Mathf.Max(.001f,length)));walked+=length;
                Vector2 tangent=(b-a).normalized,normal=new Vector2(-tangent.y,tangent.x);if(normal.y<0)normal=-normal;
                AddSegment(a+Vector2.up*Offset,b+Vector2.up*Offset,normal,s.transform);
            }
        }
        // Split at real intersections so ladders meet floors exactly, regardless of sample spacing.
        for(int i=0;i<segments.Count;i++)for(int j=i+1;j<segments.Count;j++)SplitIntersection(segments[i],segments[j]);
        foreach(var s in segments)
        {
            int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(s.a,s.b)/Mathf.Max(.1f,nodeSpacing)));
            for(int i=0;i<=count;i++)s.cuts.Add(i/(float)count);
            s.cuts.Sort();int previous=-1;float previousT=-1;
            foreach(float t in s.cuts)
            {
                if(t-previousT<.00001f)continue;previousT=t;Vector2 p=Vector2.Lerp(s.a,s.b,t);
                if(!PointClear(p,bodyRadius)){previous=-1;continue;}
                int id=GetNode(p,s.normal,s.support);
                if(previous>=0 && previous!=id && SweepClear(nodes[previous].point.Position,p,bodyRadius))Connect(previous,id);
                previous=id;
            }
        }
        cost=new float[nodes.Count];parent=new int[nodes.Count];closed=new bool[nodes.Count];Version++;
    }
    void AddSegment(Vector2 a,Vector2 b,Vector2 normal,Transform support)
    {
        var r=Area;float d=Offset-.0005f;var allowed=new Rect(r.xMin+d,r.yMin+d,Mathf.Max(0,r.width-2*d),Mathf.Max(0,r.height-2*d));
        float enter,exit;if(!Clip(a,b,allowed,out enter,out exit))return;
        Vector2 delta=b-a;b=a+delta*exit;a+=delta*enter;
        if((b-a).sqrMagnitude<.00001f)return;
        segments.Add(new Segment{a=a,b=b,normal=normal,support=support});
    }
    static float Cross(Vector2 a,Vector2 b){return a.x*b.y-a.y*b.x;}
    static void SplitIntersection(Segment a,Segment b)
    {
        Vector2 r=a.b-a.a,s=b.b-b.a,q=b.a-a.a;float cross=Cross(r,s);
        if(Mathf.Abs(cross)<.00001f)
        {
            if(Mathf.Abs(Cross(q,r))>.001f)return;
            AddProjection(a,b.a);AddProjection(a,b.b);AddProjection(b,a.a);AddProjection(b,a.b);return;
        }
        float t=Cross(q,s)/cross,u=Cross(q,r)/cross;
        if(t>=-.0001f && t<=1.0001f && u>=-.0001f && u<=1.0001f){a.cuts.Add(Mathf.Clamp01(t));b.cuts.Add(Mathf.Clamp01(u));}
    }
    static void AddProjection(Segment s,Vector2 p)
    {float t=Vector2.Dot(p-s.a,s.b-s.a)/(s.b-s.a).sqrMagnitude;if(t>=0 && t<=1)s.cuts.Add(t);}
    int GetNode(Vector2 p,Vector2 normal,Transform support)
    {
        const float merge=.035f;var key=new Vector2Int(Mathf.FloorToInt(p.x/merge),Mathf.FloorToInt(p.y/merge));
        for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)
        {
            List<int> ids;if(!buckets.TryGetValue(key+new Vector2Int(x,y),out ids))continue;
            foreach(int id in ids)if((nodes[id].point.Position-p).sqrMagnitude<merge*merge)return id;
        }
        var n=new Node{point=new Waypoint{support=support,localPosition=support?(Vector2)support.InverseTransformPoint(p):p,normal=normal}};
        int index=nodes.Count;nodes.Add(n);List<int> bucket;if(!buckets.TryGetValue(key,out bucket)){bucket=new List<int>();buckets.Add(key,bucket);}bucket.Add(index);return index;
    }
    void Connect(int a,int b){if(!nodes[a].links.Contains(b))nodes[a].links.Add(b);if(!nodes[b].links.Contains(a))nodes[b].links.Add(a);}
    public Waypoint GetPoint(int id){return nodes[id].point;}
    public bool TryAttach(Vector2 p,float maximum,out Waypoint point)
    {
        point=default(Waypoint);float best=maximum*maximum;bool found=false;
        foreach(var n in nodes){float distance=(n.point.Position-p).sqrMagnitude;if(distance<=best && SweepClear(p,n.point.Position,bodyRadius)){best=distance;point=n.point;found=true;}}
        return found;
    }
    public bool TryPath(Vector2 start,Vector2 target,float attackRange,List<Waypoint> result)
    {
        result.Clear();int first=-1;float nearest=.85f*.85f;
        for(int i=0;i<nodes.Count;i++){float d=(nodes[i].point.Position-start).sqrMagnitude;if(d<nearest && SweepClear(start,nodes[i].point.Position,bodyRadius)){nearest=d;first=i;}}
        if(first<0)return false;
        for(int i=0;i<nodes.Count;i++){cost[i]=float.PositiveInfinity;parent[i]=-1;closed[i]=false;}
        heap.Clear();cost[first]=0;Push(first,Heuristic(nodes[first].point.Position,target,attackRange));
        int best=first;float bestDistance=(nodes[first].point.Position-target).sqrMagnitude;
        while(heap.Count>0)
        {
            int id=Pop();if(closed[id])continue;closed[id]=true;Vector2 p=nodes[id].point.Position;float gap=(p-target).sqrMagnitude;
            if(gap<bestDistance){bestDistance=gap;best=id;}
            if(gap<=attackRange*attackRange && SweepClear(p,target,bodyRadius)){best=id;break;}
            foreach(int next in nodes[id].links)
            {
                if(closed[next])continue;float g=cost[id]+Vector2.Distance(p,nodes[next].point.Position);
                if(g>=cost[next])continue;cost[next]=g;parent[next]=id;Push(next,g+Heuristic(nodes[next].point.Position,target,attackRange));
            }
        }
        reversePath.Clear();for(int id=best;id>=0;id=parent[id])reversePath.Add(id);
        for(int i=reversePath.Count-1;i>=0;i--)result.Add(nodes[reversePath[i]].point);
        return result.Count>0;
    }
    public bool TryRepositionPath(Vector2 start,Vector2 target,List<Waypoint> result)
    {
        int best=-1;float score=float.PositiveInfinity;float oldGap=Vector2.Distance(start,target);
        for(int i=0;i<nodes.Count;i++)
        {
            Vector2 p=nodes[i].point.Position;float distance=Vector2.Distance(p,start);
            if(distance<1.4f || distance>2.5f)continue;
            float value=distance+(oldGap<2? -Vector2.Distance(p,target)*.4f:0);
            if(value<score){score=value;best=i;}
        }
        if(best<0){result.Clear();return false;}
        Vector2 goal=nodes[best].point.Position;
        return TryPath(start,goal,.05f,result) && result.Count>1 && Vector2.Distance(result[result.Count-1].Position,goal)<.08f;
    }
    static float Heuristic(Vector2 p,Vector2 target,float range){return Mathf.Max(0,Vector2.Distance(p,target)-range);}
    void Push(int node,float priority)
    {
        var e=new HeapEntry{node=node,priority=priority};heap.Add(e);int index=heap.Count-1;
        while(index>0){int p=(index-1)/2;if(heap[p].priority<=priority)break;heap[index]=heap[p];index=p;}heap[index]=e;
    }
    int Pop()
    {
        int answer=heap[0].node,last=heap.Count-1;var tail=heap[last];heap.RemoveAt(last);if(last==0)return answer;
        int i=0;while(i*2+1<heap.Count){int c=i*2+1;if(c+1<heap.Count && heap[c+1].priority<heap[c].priority)c++;if(heap[c].priority>=tail.priority)break;heap[i]=heap[c];i=c;}heap[i]=tail;return answer;
    }
    public bool PointClear(Vector2 p,float radius)
    {
        var r=Area;if(p.x<r.xMin+radius || p.x>r.xMax-radius || p.y<r.yMin+radius || p.y>r.yMax-radius)return false;
        foreach(var s in sources)if(s.hard && Active(s) && Expanded(s.collider.bounds,radius-.002f).Contains(p))return false;
        return true;
    }
    static bool Active(Source s){return s.collider && s.collider.enabled && s.collider.gameObject.activeInHierarchy;}
    static Rect Expanded(Bounds b,float radius){return new Rect(b.min.x-radius,b.min.y-radius,b.size.x+radius*2,b.size.y+radius*2);}
    public bool SweepClear(Vector2 a,Vector2 b,float radius)
    {Vector2 end,normal;Transform support;return !MoveWithCollision(a,b-a,radius,false,out end,out normal,out support);}
    // Swept body collision also prevents fast lunges tunnelling through thin walls.
    public bool MoveWithCollision(Vector2 a,Vector2 delta,float radius,bool landOnSoft,out Vector2 end,out Vector2 normal,out Transform support)
    {
        float fraction=1;normal=Vector2.zero;support=null;
        foreach(var s in sources)if(Active(s))
        {
            var b=s.collider.bounds;float t,exit;Vector2 n;
            if(s.hard)
            {
                var r=Expanded(b,radius);
                if(!Clip(a,a+delta,r,out t,out exit) || t<0 || t>fraction)continue;
                Vector2 impact=a+delta*t;float left=Mathf.Abs(impact.x-r.xMin),right=Mathf.Abs(impact.x-r.xMax),down=Mathf.Abs(impact.y-r.yMin),up=Mathf.Abs(impact.y-r.yMax);
                float min=Mathf.Min(Mathf.Min(left,right),Mathf.Min(down,up));n=min==left?Vector2.left:min==right?Vector2.right:min==down?Vector2.down:Vector2.up;
            }
            else
            {
                if(!landOnSoft || delta.y>=-.00001f || a.y<b.max.y+radius-.01f)continue;
                t=(b.max.y+radius-a.y)/delta.y;float x=a.x+delta.x*t;
                if(t<0 || t>fraction || x<b.min.x-radius || x>b.max.x+radius)continue;n=Vector2.up;
            }
            fraction=t;normal=n;support=s.collider.transform;
        }
        end=a+delta*fraction;if(support)end+=normal*.025f;return support;
    }
    public static bool Clip(Vector2 a,Vector2 b,Rect r,out float enter,out float exit)
    {
        enter=0;exit=1;Vector2 delta=b-a;
        for(int axis=0;axis<2;axis++)
        {
            float p=axis==0?a.x:a.y,d=axis==0?delta.x:delta.y,lo=axis==0?r.xMin:r.yMin,hi=axis==0?r.xMax:r.yMax;
            if(Mathf.Abs(d)<.000001f){if(p<lo || p>hi)return false;}
            else{float x=(lo-p)/d,y=(hi-p)/d;if(x>y){float tmp=x;x=y;y=tmp;}enter=Mathf.Max(enter,x);exit=Mathf.Min(exit,y);if(enter>exit)return false;}
        }
        return true;
    }
    void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    void OnDrawGizmosSelected()
    {
        Gizmos.color=new Color(.5f,.8f,1,.5f);var r=Area;Gizmos.DrawWireCube(r.center,r.size);
        if(!showGraph)return;Gizmos.color=new Color(.4f,1,.65f,.6f);
        for(int i=0;i<nodes.Count;i++)foreach(int j in nodes[i].links)if(j>i)Gizmos.DrawLine(nodes[i].point.Position,nodes[j].point.Position);
    }
}
