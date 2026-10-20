using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(200),DisallowMultipleComponent]
[AddComponentMenu("Recoil/Crawler/Warehouse Encounter")]
public sealed class WarehouseCrawlerEncounter : MonoBehaviour
{
    [Min(.1f),Tooltip("任意原怪物连续实际看见玩家的时间；全部丢失视线则清零。")]
    public float discoveryDuration=20;
    [Range(0,.3f),Tooltip("在画面四周额外留白，避免新怪物在镜头边缘突然出现。")]
    public float offscreenMargin=.08f;
    public SurfaceCrawlerController crawler;
    public CrawlerNavigation navigation;
    [Tooltip("门口候选出生点。为空时只在视角外的可爬路线出生。")]
    public Transform[] doorSpawnPoints;
    public float DiscoveryTime {get;private set;}
    public bool Spawned {get;private set;}
    public bool TestSuppressed {get;private set;}
    public bool WaitingForHiddenSpawn {get{return !TestSuppressed && !Spawned && DiscoveryTime>=discoveryDuration;}}
    public bool ObservingPlayer {get{return isActiveAndEnabled && crawler && crawler.CanSeePlayer();}}
    RecoilPrototype world;
    Camera view;
    float nextSpawnAttempt;
    readonly List<CrawlerNavigation.Waypoint> candidatePath=new List<CrawlerNavigation.Waypoint>();
    readonly List<CrawlerNavigation.Waypoint> candidates=new List<CrawlerNavigation.Waypoint>();
    readonly List<float> candidateScores=new List<float>();
    public void Initialize(RecoilPrototype context,Camera camera)
    {
        world=context;view=camera;
        if(!navigation || !crawler){Debug.LogError("Crawler encounter needs navigation and crawler references.",this);enabled=false;return;}
        navigation.Initialize(context);crawler.Initialize(context,navigation);
    }
    public void ResetEncounter()
    {
        DiscoveryTime=nextSpawnAttempt=0;Spawned=TestSuppressed=false;
        if(navigation)navigation.Rebuild();if(crawler)crawler.ResetCrawler();
    }
    void LateUpdate()
    {
        if(!world || !world.Playing || world.FrameDelta<=0 || !crawler || !navigation)return;
        if(Input.GetKeyDown(KeyCode.L)){TestDismiss();return;}
        if(Input.GetKeyDown(KeyCode.P))TestRespawn();
        if(TestSuppressed)return;
        navigation.Refresh(world.Clock);
        if(!Spawned)
        {
            bool observed=false;
            foreach(var enemy in world.Rampagers)if(enemy && enemy.isActiveAndEnabled && enemy.State!=RampagerController.Behaviour.Sleeping)
            {
                if(enemy.CanSeePlayer()){observed=true;break;}
            }
            AdvanceDiscovery(world.FrameDelta,observed);
            if(WaitingForHiddenSpawn && world.Clock>=nextSpawnAttempt)
            {nextSpawnAttempt=world.Clock+.5f;TrySpawn();}
        }
        if(Spawned)crawler.Simulate(world.FrameDelta);
    }
    // Warehouse-only test shortcut. Reuse the single crawler rather than accumulating enemies.
    public bool TestRespawn()
    {
        if(!world || !navigation || !crawler || !view)return false;
        ResetEncounter();DiscoveryTime=discoveryDuration;
        bool appeared=TrySpawn();nextSpawnAttempt=world.Clock+.5f;
        world.ShowInteractionMessage(appeared?"TEST / CRAWLER SPAWNED":"TEST / CRAWLER WAITING FOR AN OFFSCREEN SPAWN");
        return appeared;
    }
    public void TestDismiss()
    {
        ResetEncounter();TestSuppressed=true;
        if(world)world.ShowInteractionMessage("TEST / CRAWLER DISABLED (P TO RESPAWN)");
    }
    public void AdvanceDiscovery(float dt,bool actuallyObserved)
    {
        if(TestSuppressed || Spawned || dt<=0)return;
        if(DiscoveryTime>=discoveryDuration)return; // Once summoned, wait for a safe entrance even if the player escapes sight.
        DiscoveryTime=actuallyObserved?Mathf.Min(discoveryDuration,DiscoveryTime+dt):0;
    }
    public bool IsOffscreen(Vector2 p)
    {
        if(!view)return false;float extent=navigation.bodyRadius+.45f;
        Vector3 low=view.WorldToViewportPoint(new Vector3(p.x-extent,p.y-extent,0)),high=view.WorldToViewportPoint(new Vector3(p.x+extent,p.y+extent,0));
        return high.z>0 && (high.x < -offscreenMargin || low.x>1+offscreenMargin || high.y < -offscreenMargin || low.y>1+offscreenMargin);
    }
    public bool TrySpawn()
    {
        if(TestSuppressed || Spawned || !world || !view || !navigation || !crawler || DiscoveryTime<discoveryDuration)return false;
        candidates.Clear();candidateScores.Clear();Vector2 target=world.Player.Position;
        // Consider the closest hidden nodes first. Hard cap path requests per attempt.
        for(int i=0;i<navigation.NodeCount;i++)
        {
            var point=navigation.GetPoint(i);if(!IsOffscreen(point.Position))continue;
            AddCandidate(point,(point.Position-target).sqrMagnitude);
        }
        foreach(var point in candidates)if(Reachable(point,target)){Spawn(point);return true;}
        if(doorSpawnPoints!=null)foreach(var marker in doorSpawnPoints)if(marker)
        {
            CrawlerNavigation.Waypoint point;
            if(navigation.TryAttach(marker.position,1,out point) && IsOffscreen(point.Position) && Reachable(point,target))
            {Spawn(point);return true;}
        }
        return false;
    }
    void AddCandidate(CrawlerNavigation.Waypoint point,float score)
    {
        int index=candidateScores.BinarySearch(score);if(index<0)index=~index;
        if(index>=8)return;candidates.Insert(index,point);candidateScores.Insert(index,score);
        if(candidates.Count>8){candidates.RemoveAt(8);candidateScores.RemoveAt(8);}
    }
    bool Reachable(CrawlerNavigation.Waypoint point,Vector2 target)
    {
        if(!navigation.PointClear(point.Position,navigation.bodyRadius) || !navigation.TryPath(point.Position,target,crawler.pounceRange,candidatePath))return false;
        Vector2 end=candidatePath[candidatePath.Count-1].Position;
        return Vector2.Distance(end,target)<=crawler.pounceRange && navigation.SweepClear(end,target,navigation.bodyRadius);
    }
    void Spawn(CrawlerNavigation.Waypoint point){crawler.Appear(point);Spawned=true;}
    void OnDisable(){if(crawler)crawler.ResetCrawler();}
}
