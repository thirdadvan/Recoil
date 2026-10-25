using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// One shared surface per scene. Builds from gameplay solids, not thousands of decorative meshes.
[DefaultExecutionOrder(900),DisallowMultipleComponent,AddComponentMenu("Recoil/3D/Rampager Shared Navigation")]
public sealed class RampagerDepthNavigation : MonoBehaviour
{
    [Min(.1f)] public float agentRadius=.43f;
    [Min(.5f)] public float agentHeight=2.08f;
    [Min(.04f)] public float voxelSize=.12f;
    [Tooltip("没有对应3D外观的碰撞地形，其导航体积在Z轴上的范围。")]
    public Vector2 fallbackDepth=new Vector2(-3,10);
    NavMeshData data;NavMeshDataInstance instance;
    sealed class StairConnection {public Vector3 start,end;public Vector3[] points;}
    readonly List<NavMeshLinkInstance> links=new List<NavMeshLinkInstance>();
    readonly List<StairConnection> stairsLinks=new List<StairConnection>();
    RecoilPrototype world;bool sceneReady;
    public bool Ready {get{return data && instance.valid;}}
    public int SourceCount {get;private set;}
    public float LastBuildMilliseconds {get;private set;}
    public int BuildCount {get;private set;}
    public void Initialize(RecoilPrototype context){world=context;}
    public bool EnsureReady(){if(!sceneReady||!isActiveAndEnabled)return false;if(Ready)return true;return Rebuild();}
    void Start()
    {
        // Wait for all scene Awake/Start setup (including stair paths) before the single shared bake.
        sceneReady=true;if(!world)world=GetComponentInParent<RecoilPrototype>();if(!world)return;
        var enemies=world.GetComponentsInChildren<RampagerController>();if(enemies.Length==0)return;
        if(EnsureReady())foreach(var enemy in enemies)enemy.PrepareNavigationSpawn();
    }
    [ContextMenu("Rebuild runtime navigation")]
    public bool Rebuild()
    {
        if(!Application.isPlaying)return false;
        if(!world)world=GetComponentInParent<RecoilPrototype>();if(!world)return false;
        var timer=System.Diagnostics.Stopwatch.StartNew();
        ClearLinks();if(instance.valid)instance.Remove();if(data)Destroy(data);
        var sources=new List<NavMeshBuildSource>();var bounds=new Bounds();bool first=true;
        foreach(var c in world.GetComponentsInChildren<BoxCollider2D>())
        {
            var soft=c.GetComponent<SoftRoofPlatform>();
            if(!c.enabled || c.isTrigger && !soft || soft && (!soft.Available || soft.GetComponentInParent<KBKStackerCrane>()) || c.GetComponent<ClimbableLadder>() || c.GetComponentInParent<RampagerController>() || c.GetComponentInParent<RecoilPlayerController>())continue;
            var b=c.bounds;float z0=fallbackDepth.x,z1=fallbackDepth.y;
            var visual=c.GetComponent<Recoil3DSpriteVisual>();
            if(visual && visual.meshRenderer){var meshBounds=visual.meshRenderer.bounds;z0=meshBounds.min.z;z1=meshBounds.max.z;}
            var volume=new Bounds(new Vector3(b.center.x,b.center.y,(z0+z1)*.5f),new Vector3(b.size.x,b.size.y,Mathf.Max(.1f,z1-z0)));
            AddBox(sources,volume.center,volume.size,Quaternion.identity);
            if(first){bounds=volume;first=false;}else bounds.Encapsulate(volume);
        }
        // Native 3D terrain authored later works too; triggers and actor shapes are not navigation solids.
        foreach(var collider in world.GetComponentsInChildren<Collider>())
        {
            if(!collider.enabled||collider.isTrigger||collider.GetComponentInParent<RampagerController>()||collider.GetComponentInParent<RecoilPlayerController>()||collider.GetComponentInParent<EscortChild>())continue;
            var box=collider as BoxCollider;var mesh=collider as MeshCollider;
            if(box)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center),size=box.size,area=0});
            else if(mesh && mesh.sharedMesh)sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Mesh,transform=mesh.transform.localToWorldMatrix,sourceObject=mesh.sharedMesh,area=0});
            else continue;
            if(first){bounds=collider.bounds;first=false;}else bounds.Encapsulate(collider.bounds);
        }
        SourceCount=sources.Count;if(first)return false;
        var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=agentRadius;settings.agentHeight=agentHeight;
        settings.agentClimb=.3f;settings.agentSlope=55;settings.overrideVoxelSize=true;settings.voxelSize=voxelSize;settings.minRegionArea=.2f;
        bounds.Expand(new Vector3(4,6,4));data=NavMeshBuilder.BuildNavMeshData(settings,sources,bounds,Vector3.zero,Quaternion.identity);
        if(data)instance=NavMesh.AddNavMeshData(data);
        if(Ready)foreach(var stairs in world.GetComponentsInChildren<WalkableStairway>())if(stairs.Available && stairs.upperLandingAvailable)
        {
            NavMeshHit start,end;var points=stairs.routePoints;
            if(!NavMesh.SamplePosition(points[0].position,out start,1.2f,NavMesh.AllAreas)||!NavMesh.SamplePosition(points[points.Length-1].position,out end,1.2f,NavMesh.AllAreas))continue;
            if(Mathf.Abs(start.position.y-points[0].position.y)>.6f || Mathf.Abs(end.position.y-points[points.Length-1].position.y)>.6f)continue;
            var link=NavMesh.AddLink(new NavMeshLinkData{startPosition=start.position,endPosition=end.position,bidirectional=true,width=0,area=0,agentTypeID=settings.agentTypeID,costModifier=1});
            if(!link.valid)continue;links.Add(link);
            var route=new Vector3[points.Length];for(int i=0;i<route.Length;i++)route[i]=points[i].position;
            stairsLinks.Add(new StairConnection{start=start.position,end=end.position,points=route});
        }
        timer.Stop();LastBuildMilliseconds=(float)timer.Elapsed.TotalMilliseconds;BuildCount++;return Ready;
    }
    static void AddBox(List<NavMeshBuildSource> sources,Vector3 center,Vector3 size,Quaternion rotation)
    {sources.Add(new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(center,rotation,Vector3.one),size=size,area=0});}
    public bool Project(Vector3 feet,out Vector3 surface)
    {
        surface=feet;if(!EnsureReady())return false;NavMeshHit hit;
        if(!NavMesh.SamplePosition(feet,out hit,1.2f,NavMesh.AllAreas)||Mathf.Abs(hit.position.y-feet.y)>.6f)return false;
        surface=hit.position;return true;
    }
    public bool FindPath(Vector3 origin,Vector3 destination,NavMeshPath path,bool allowPartial=false)
    {
        if(!EnsureReady())return false;
        NavMeshHit start,end;
        if(!NavMesh.SamplePosition(origin,out start,1.2f,NavMesh.AllAreas)||Mathf.Abs(start.position.y-origin.y)>.6f)return false;
        if(!NavMesh.SamplePosition(destination,out end,1.2f,NavMesh.AllAreas)||Mathf.Abs(end.position.y-destination.y)>.6f)return false;
        return NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,path)&&(path.status==NavMeshPathStatus.PathComplete || allowPartial && path.status==NavMeshPathStatus.PathPartial);
    }
    // Expand off-mesh stair links to the existing player path, including every switchback.
    public Vector3[] Corners(NavMeshPath path,out bool[] protectedSegments)
    {
        var raw=path.corners;var result=new List<Vector3>();var protect=new List<bool>();
        if(raw.Length>0){result.Add(raw[0]);protect.Add(false);}
        for(int i=0;i<raw.Length-1;i++)
        {
            StairConnection match=null;bool reverse=false;
            foreach(var stair in stairsLinks)
            {
                if(Vector3.Distance(raw[i],stair.start)<.3f && Vector3.Distance(raw[i+1],stair.end)<.3f){match=stair;break;}
                if(Vector3.Distance(raw[i],stair.end)<.3f && Vector3.Distance(raw[i+1],stair.start)<.3f){match=stair;reverse=true;break;}
            }
            if(match!=null)for(int step=0;step<match.points.Length;step++){int at=reverse?match.points.Length-1-step:step;result.Add(match.points[at]);protect.Add(true);}
            result.Add(raw[i+1]);protect.Add(match!=null);
        }
        protectedSegments=protect.ToArray();return result.ToArray();
    }
    void ClearLinks(){foreach(var link in links)if(link.valid)link.Remove();links.Clear();stairsLinks.Clear();}
    void OnDisable(){ClearLinks();if(instance.valid)instance.Remove();if(data){Destroy(data);data=null;}}
}
