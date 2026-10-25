using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class CoalerXZDistanceVerification
{
    static void Set(object o,string field,object value){o.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
    static void Check(bool ok,string message){if(!ok)throw new Exception("Coaler X/Z: "+message);}
    [MenuItem("Recoil/AI/Check Equal X And Z Distances")]
    public static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Run the distance check outside Play.");return;}
        var scene=EditorSceneManager.NewPreviewScene();NavMeshData data=null;NavMeshDataInstance instance=default(NavMeshDataInstance);RampagerDepthNavigation nav=null;
        try
        {
            var root=new GameObject("Temporary X/Z distance check");SceneManager.MoveGameObjectToScene(root,scene);
            var world=root.AddComponent<RecoilPrototype>();Set(world,"started",true);Set(world,"simTime",100f);
            var actor=new GameObject("Dawn");actor.transform.SetParent(root.transform);var player=actor.AddComponent<RecoilPlayerController>();
            Func<string,Transform> part=name=>{var p=new GameObject(name).transform;p.SetParent(actor.transform);return p;};
            player.Configure(world,null,part("coat"),part("head"),part("hat"),part("gun"));Set(world,"player",player);player.ResetController(new Vector2(0,100));
            nav=root.AddComponent<RampagerDepthNavigation>();nav.Initialize(world);
            var settings=NavMesh.GetSettingsByIndex(0);settings.agentRadius=.43f;settings.agentHeight=2.08f;settings.overrideVoxelSize=true;settings.voxelSize=.12f;settings.agentClimb=.3f;
            var sources=new System.Collections.Generic.List<NavMeshBuildSource>{new NavMeshBuildSource{shape=NavMeshBuildSourceShape.Box,transform=Matrix4x4.TRS(new Vector3(0,98.95f,0),Quaternion.identity,Vector3.one),size=new Vector3(40,.5f,40)}};
            data=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,100,0),new Vector3(44,8,44)),Vector3.zero,Quaternion.identity);
            Check(data,"Test navigation bake failed");instance=NavMesh.AddNavMeshData(data);Check(instance.valid,"Test navigation registration failed");
            Set(nav,"data",data);Set(nav,"instance",instance);Set(nav,"sceneReady",true);
            float[] stops=new float[2];float[] energies=new float[2];
            for(int axis=0;axis<2;axis++)
            {
                var go=new GameObject(axis==0?"X-axis Coaler":"Z-axis Coaler");go.transform.SetParent(root.transform);go.transform.position=axis==0?new Vector3(-5,100,0):new Vector3(0,100,-5);
                var enemy=go.AddComponent<RampagerController>();go.AddComponent<RampagerDepthArrival>();enemy.Initialize(world);enemy.GetComponent<RampagerAttack>().enabled=false;enemy.returnToSleepAfterSound=false;
                var motor=go.GetComponent<RampagerMovement>();var sense=go.GetComponent<RampagerPerception>();
                Action<float> step=dt=>{Set(world,"simTime",world.Clock+dt);enemy.Simulate(dt);};
                enemy.HearNoise(new Vector3(0,100,0),6);energies[axis]=enemy.SoundEnergy;
                for(int i=0;i<1400;i++)step(.01f);
                stops[axis]=new Vector2(enemy.Position.x,motor.MovementPlaneZ).magnitude;
                Check(Mathf.Abs(stops[axis]-(enemy.CloseSenseRadius-enemy.soundInvestigationInset))<.065f,"Axis "+axis+" failed to reach investigation edge: "+stops[axis]);
                motor.SetNavigationPosition(axis==0?new Vector3(-(enemy.CloseSenseRadius-.1f),100,0):new Vector3(0,100,-(enemy.CloseSenseRadius-.1f)));Check(sense.CanSeePlayer(),"Axis "+axis+" failed close detection at 1.9m");
                motor.SetNavigationPosition(axis==0?new Vector3(-(enemy.CloseSenseRadius+.1f),100,0):new Vector3(0,100,-(enemy.CloseSenseRadius+.1f)));Check(!sense.CanSeePlayer(),"Axis "+axis+" exceeded close detection radius");
                UnityEngine.Object.DestroyImmediate(go);
            }
            Check(Mathf.Abs(stops[0]-stops[1])<.05f,"X/Z investigation stopping distances differ");Check(Mathf.Abs(energies[0]-energies[1])<.001f,"X/Z received energy differs");
            Directory.CreateDirectory("Captures/CoalerXZ");File.WriteAllText("Captures/CoalerXZ/Verification.txt",$"PASS flat-surface navigation and proximity: X stop {stops[0]:F4}m, Z stop {stops[1]:F4}m; sound energy X {energies[0]:F4}, Z {energies[1]:F4}; close sense verified just inside / outside the configured radius on both axes.\n");
            // Inspect the authored scene too, without changing or saving it.
            foreach(var worldInScene in UnityEngine.Object.FindObjectsOfType<RecoilPrototype>())
            {
                if(worldInScene==world)continue;
                using(var report=new StreamWriter("Captures/CoalerXZ/SceneInspection.txt",false))
                {
                    report.WriteLine(worldInScene.gameObject.scene.path);
                    foreach(var enemy in worldInScene.GetComponentsInChildren<RampagerController>(true))
                    {var arrival=enemy.GetComponent<RampagerDepthArrival>();report.WriteLine($"{enemy.name}: position={enemy.transform.position:F3}, close radius={enemy.CloseSenseRadius}, navigation={(arrival?arrival.enabled:false)}");}
                    foreach(var collider in worldInScene.GetComponentsInChildren<BoxCollider2D>())
                    {
                        var visual=collider.GetComponent<Recoil3DSpriteVisual>();if(!collider.isTrigger && visual && visual.meshRenderer)
                        {var b=visual.meshRenderer.bounds;report.WriteLine($"Solid {collider.name}: X {b.min.x:F2}..{b.max.x:F2}, Y {b.min.y:F2}..{b.max.y:F2}, Z {b.min.z:F2}..{b.max.z:F2}");}
                    }
                }
                break;
            }
            Debug.Log("COALER_XZ_DISTANCE_CHECK_PASS");
        }
        finally
        {
            if(instance.valid)instance.Remove();if(nav){Set(nav,"instance",default(NavMeshDataInstance));Set(nav,"data",null);}if(data)UnityEngine.Object.DestroyImmediate(data);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}
