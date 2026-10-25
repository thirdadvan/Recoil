using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WarehouseCrawlerValidation
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Set(object owner,string name,object value){owner.GetType().GetField(name,Private).SetValue(owner,value);}
    [MenuItem("Recoil/Validate/Warehouse Crawler")]
    public static string Run()
    {
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/RecoilFactoryWarehouse.unity");
        Check(scene.isLoaded,"Open the warehouse first.");
        var original=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
        var preview=EditorSceneManager.NewPreviewScene();GameObject copy=null;
        var lines=new List<string>();
        try
        {
            copy=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,preview);
            var w=copy.GetComponent<RecoilPrototype>();
            var cameraObject=new GameObject("Validation camera");SceneManager.MoveGameObjectToScene(cameraObject,preview);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;Set(w,"cam",camera);
            typeof(RecoilPrototype).GetMethod("BindScene",Private).Invoke(w,null);
            var encounter=w.GetComponentInChildren<WarehouseCrawlerEncounter>(true);var nav=encounter.navigation;var crawler=encounter.crawler;var player=w.Player;
            player.ResetController(new Vector2(13,.825f));
            var path=new List<CrawlerNavigation.Waypoint>();
            Check(nav.NodeCount>100 && nav.NodeCount<1500,"Unexpected graph size.");
            Check(nav.TryPath(new Vector2(10,.245f),new Vector2(45,6.955f),.06f,path),"Floor to ceiling path missing.");
            Check(Vector2.Distance(path.Last().Position,new Vector2(45,6.955f))<.1f && path.Any(p=>p.Position.x>51.8f),"Floor/ceiling route: end="+path.Last().Position+" minX="+path.Min(p=>p.Position.x)+" maxX="+path.Max(p=>p.Position.x)+" nodes="+nav.NodeCount+" bounds="+nav.Area);
            Check(path.All(p=>nav.PointClear(p.Position,nav.bodyRadius)),"Route intersects solid geometry.");
            var ladder=w.GetComponentsInChildren<ClimbableLadder>().First();var r=ladder.Area;
            Check(nav.TryPath(new Vector2(r.center.x,r.yMax+.245f),new Vector2(10,.245f),.1f,path) && path.Last().Position.y<.3f,"Ladder does not connect to floor.");
            lines.Add("PASS: floor -> wall -> ceiling and ladder -> floor; "+nav.NodeCount+" cached nodes.");
            var timer=Stopwatch.StartNew();for(int i=0;i<100;i++)nav.TryPath(new Vector2(10,.245f),new Vector2(45,6.955f),.06f,path);timer.Stop();
            lines.Add("Pathfinding: 100 full warehouse paths in "+timer.ElapsedMilliseconds+" ms (Editor, not a frame profiler).");
            CrawlerNavigation.Waypoint floorStart;
            Check(nav.TryAttach(new Vector2(10,.245f),.5f,out floorStart),"Movement start missing.");
            crawler.Motor.Attach(floorStart);nav.TryPath(floorStart.Position,new Vector2(45,6.955f),.06f,path);
            int cursor=0;for(int i=0;i<6500 && cursor<path.Count;i++)crawler.Motor.Crawl(path,ref cursor,.02f);
            Check(cursor==path.Count && crawler.Motor.Position.y>6.8f,"Crawler motor stalled at a wall/ceiling corner.");
            var stairObject=new GameObject("Validation switchback stair");stairObject.transform.SetParent(w.transform);
            var stair=stairObject.AddComponent<WalkableStairway>();stair.routePoints=new Transform[3];
            Vector2[] stairPositions={new Vector2(35,0),new Vector2(38,2),new Vector2(35,4)};
            for(int i=0;i<3;i++){var t=new GameObject("Stair point").transform;t.SetParent(stairObject.transform);t.position=stairPositions[i];stair.routePoints[i]=t;}
            nav.Initialize(w);Check(nav.TryPath(new Vector2(34.9f,.245f),new Vector2(35,4.245f),.06f,path) && path.Last().Position.y>4.1f,"Switchback stairs not connected.");
            UnityEngine.Object.DestroyImmediate(stairObject);nav.Initialize(w);
            lines.Add("PASS: actual crawling around floor/wall/ceiling corners and switchback stair routing.");
            encounter.ResetEncounter();encounter.AdvanceDiscovery(19.9f,true);Check(!encounter.WaitingForHiddenSpawn,"Spawn threshold early.");encounter.AdvanceDiscovery(.1f,false);Check(encounter.DiscoveryTime==0,"Lost sight must reset discovery.");
            encounter.AdvanceDiscovery(20.1f,true);Check(encounter.WaitingForHiddenSpawn,"20-second threshold missing.");
            camera.transform.position=new Vector3(26,3.6f,-10);camera.orthographic=true;camera.orthographicSize=40;camera.aspect=2;
            Check(!encounter.TrySpawn(),"Crawler spawned while the entire warehouse was visible.");
            camera.transform.position=new Vector3(13,3,-10);camera.orthographicSize=4.2f;camera.aspect=16f/9;
            Check(encounter.TrySpawn() && encounter.IsOffscreen(crawler.Motor.Position),"Offscreen reachable spawn failed.");Check(!encounter.TrySpawn(),"Duplicate spawn allowed.");
            lines.Add("PASS: continuous 20s timer, loss-of-sight reset, hidden-only spawn and one-instance limit.");
            encounter.ResetEncounter();CrawlerNavigation.Waypoint ground;
            Check(nav.TryAttach(new Vector2(10,.245f),.5f,out ground),"Ground attach failed.");
            crawler.Appear(ground);crawler.Simulate(.01f);Check(crawler.State==SurfaceCrawlerController.Behaviour.PreparingPounce,"Should prepare within 6m.");Vector2 before=crawler.Motor.Position;
            crawler.Simulate(1.18f);Check(crawler.State==SurfaceCrawlerController.Behaviour.PreparingPounce && crawler.Motor.Position==before,"Did not pause during preparation.");
            crawler.Simulate(.019f);Check(crawler.State==SurfaceCrawlerController.Behaviour.PreparingPounce,"Pounced before 1.2 seconds.");
            crawler.Simulate(.002f);Check(crawler.State==SurfaceCrawlerController.Behaviour.Pouncing,"Did not launch after 1.2 seconds.");
            Set(player,"flying",true);Set(player,"pool",Vector2.up*4);
            for(int i=0;i<60 && player.Health==3;i++)crawler.Simulate(.02f);
            Check(player.Health==2 && !player.Flying && player.RecoilPool==Vector2.zero,"Pounce did not damage/cancel flight.");
            Check(crawler.State==SurfaceCrawlerController.Behaviour.Recovering,"Attack did not enter recovery.");
            Vector2 playerBefore=player.Position;player.Simulate(0,0,false,false,.02f);Check(player.Position.x>playerBefore.x,"Pounce knockback missing.");
            for(int i=0;i<25;i++)crawler.Simulate(.02f);Check(player.Health==2,"One pounce dealt multiple hits.");
            for(int i=0;i<30 && crawler.State==SurfaceCrawlerController.Behaviour.Recovering;i++)crawler.Simulate(.02f);
            Vector2 landing=crawler.Motor.Position;for(int i=0;i<20;i++)crawler.Simulate(.02f);
            Check(crawler.State!=SurfaceCrawlerController.Behaviour.PreparingPounce && Vector2.Distance(landing,crawler.Motor.Position)>.1f,"Must reposition before the next pounce.");
            lines.Add("PASS: stationary 1.2s preparation, one hit, knockback, flight cancellation, recovery and reposition.");
            player.ResetController(new Vector2(20,.825f));crawler.ResetCrawler();crawler.Appear(ground);crawler.Simulate(.01f);Check(crawler.State==SurfaceCrawlerController.Behaviour.Crawling,"Pounce started beyond 6m.");
            crawler.ResetCrawler();player.Position=new Vector2(13,.825f);crawler.Appear(ground);crawler.Simulate(.01f);crawler.Simulate(1.2f);Check(crawler.State==SurfaceCrawlerController.Behaviour.Pouncing,"Dodge test launch failed.");
            player.Position=new Vector2(13,4);for(int i=0;i<40;i++)crawler.Simulate(.02f);Check(player.Health==3,"Lunge changed direction after takeoff.");
            lines.Add("PASS: 6m limit and dodgeable direction locked at takeoff.");
            var obstacle=new GameObject("Validation obstruction");obstacle.transform.SetParent(w.transform);obstacle.transform.position=new Vector2(11.5f,2);var box=obstacle.AddComponent<BoxCollider2D>();box.size=new Vector2(.1f,4);Physics2D.SyncTransforms();nav.Initialize(w);
            Vector2 end,normal;Transform support;Check(nav.MoveWithCollision(new Vector2(10,2),new Vector2(5,0),nav.bodyRadius,false,out end,out normal,out support) && end.x<11.5f,"Fast pounce passed through thin wall.");
            crawler.ResetCrawler();Check(nav.TryAttach(new Vector2(10,.245f),.5f,out ground),"Obstructed start attach failed.");player.Position=new Vector2(13,.825f);crawler.Appear(ground);crawler.Simulate(.01f);Check(crawler.State!=SurfaceCrawlerController.Behaviour.PreparingPounce,"Pounce prepared through solid wall.");
            lines.Add("PASS: swept collision prevents tunnelling and through-wall pounces.");
            UnityEngine.Object.DestroyImmediate(obstacle);Physics2D.SyncTransforms();nav.Initialize(w);
            var fork=w.GetComponentInChildren<KBKStackerCrane>().forkPlatform;var originalFork=fork.transform.position;fork.transform.position+=Vector3.up*3;Physics2D.SyncTransforms();int version=nav.Version;nav.Refresh(100);Check(nav.Version>version,"Moving Freight did not update graph.");fork.transform.position=originalFork;
            encounter.ResetEncounter();Check(!encounter.Spawned && encounter.DiscoveryTime==0 && !crawler.Present && !crawler.GetComponent<SurfaceCrawlerVisuals>().visualRoot.gameObject.activeSelf,"Retry reset failed.");
            lines.Add("PASS: moving Freight refresh and retry reset; original Rampagers="+original.GetComponentsInChildren<RampagerController>().Length+".");
        }
        finally
        {
            if(copy)UnityEngine.Object.DestroyImmediate(copy);EditorSceneManager.ClosePreviewScene(preview);
        }
        string output=string.Join("\n",lines.ToArray());Directory.CreateDirectory("output/validation");File.WriteAllText("output/validation/warehouse-crawler.txt",output);UnityEngine.Debug.Log(output);return output;
    }
}
