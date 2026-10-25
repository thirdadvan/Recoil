using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Focused regression checks for authored placement, route-less idle and the idle silhouette.
public static class RampagerScenePlacementVerification
{
    const string Report="Backups/SceneAuthoredMonsterIdle/20261022/verification.txt";
    static void Require(bool condition,string description){if(!condition)throw new Exception(description);}
    static void Set(object obj,string name,object value)
    {obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);}
    [MenuItem("Recoil/Verify Authored Monster Placement and Idle")]
    public static void RunBatch()
    {
        var setup=EditorSceneManager.GetSceneManagerSetup();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        try
        {
            var world=new GameObject("Temporary world").AddComponent<RecoilPrototype>();
            var p=new GameObject("Temporary player").AddComponent<RecoilPlayerController>();
            p.Configure(world,null,null,null,null,null);p.Position=new Vector2(1000,1000);Set(world,"player",p);
            var go=new GameObject("Temporary authored monster");
            Vector3 authored=new Vector3(3.25f,2.17f,4.9f);go.transform.position=authored;go.transform.rotation=Quaternion.Euler(0,37,0);
            var ai=go.AddComponent<RampagerController>();ai.EnsureStandardComponents();
            Require(!go.GetComponent<RampagerPatrolRoute>() && go.GetComponentsInChildren<RampagerPatrolPoint>().Length==0,"Automatic patrol points were generated");
            var sleep=new GameObject("Separate return-to-sleep point");sleep.transform.position=new Vector3(40,8,2);ai.sleepPoint=sleep.transform;
            ai.initialState=RampagerController.Behaviour.Sleeping;ai.Initialize(world);
            var motor=go.GetComponent<RampagerMovement>();
            Require(Vector3.Distance(go.transform.position,authored)<.00001f,"Sleeping spawn moved to its return point");
            Require(Mathf.Abs(Mathf.DeltaAngle(motor.VisualYaw,37))<.001f,"Authored yaw was overwritten");
            motor.Position=Vector2.zero;motor.MovementPlaneZ=-99;motor.Facing=1;motor.SetVisualYaw(-90);motor.ResetMovement();
            Require(Vector3.Distance(go.transform.position,authored)<.00001f && Mathf.Abs(motor.VisualYaw-37)<.001f,"Reset lost authored position, depth or yaw");
            ai.initialState=RampagerController.Behaviour.Roaming;ai.ResetController();
            for(int i=0;i<120;i++)ai.Simulate(1f/60);
            Require(ai.IdleWaiting && Vector3.Distance(go.transform.position,authored)<.00001f,"Route-less monster moved instead of waiting");
            Require(Mathf.Abs(motor.VisualYaw-37)<.001f,"Idle turned toward a hard-coded side");
            var route=go.AddComponent<RampagerPatrolRoute>();route.Initialize(ai);Require(!route.HasPoints,"Empty route has points");
            var point=new GameObject("Explicit point").AddComponent<RampagerPatrolPoint>();point.transform.SetParent(go.transform,false);point.transform.position=go.transform.position;point.duration=1;
            route.Initialize(ai);float dir,dist,look;Require(route.Evaluate(go.transform.position,.1f,motor.Facing,1,out dir,out dist,out look),"Explicit route ignored");
            Require(!route.RequestsFacing && dist==0,"Stay action changed the authored facing");
            point.action=RampagerPatrolPoint.Action.LookRight;route.ResetRoute();route.Evaluate(go.transform.position,.1f,motor.Facing,1,out dir,out dist,out look);
            Require(route.RequestsFacing && look==1,"Explicit look direction stopped working");
            Require(!world.showMonsterStateUI,"Monster labels/bars should be hidden by default");
            var specimen=new GameObject("Temporary silhouette specimen").AddComponent<RampagerTentacleVisual>();
            specimen.SetPreview(RampagerTentacleVisual.Form.Roaming);specimen.UpdatePose(0,true);
            var mesh=specimen.VisualRoot.Find("Back head torso shutters arms loose strands and fusion").GetComponent<MeshFilter>().sharedMesh;
            float standingHeight=mesh.bounds.max.y;
            specimen.SetPreview(RampagerTentacleVisual.Form.Idle);specimen.UpdatePose(0,true);
            Require(mesh.bounds.max.y<standingHeight-.15f,"Idle upper body has not slumped");
            var legs=specimen.VisualRoot.Find("Human legs - narrow tired stance");
            Require(legs.localPosition.sqrMagnitude<.00001f && Vector3.Dot(legs.up,Vector3.up)>.999f,"Idle legs should stay upright");
            foreach(var vertex in mesh.vertices)Require(!float.IsNaN(vertex.x) && !float.IsInfinity(vertex.y),"Invalid idle mesh");
            File.WriteAllText(Report,"PASS\nAuthored position/depth/yaw survive initialization and reset.\nSleep return point does not replace spawn.\nNo route means stationary idle; no implicit points or turn.\nExplicit stay and look patrol actions work.\nMonster labels and anger bars default hidden.\nIdle upper body slumps while legs stay upright.\n");
            Debug.Log("RECOIL_AUTHORED_IDLE_VERIFICATION_PASSED");
        }
        catch(Exception error){File.WriteAllText(Report,"FAILED\n"+error);Debug.LogException(error);throw;}
        finally
        {
            foreach(var root in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(root);
            if(setup.Length>0 && Array.TrueForAll(setup,s=>!string.IsNullOrEmpty(s.path)))EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
