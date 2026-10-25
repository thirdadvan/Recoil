using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class CoalerTentacleAnimationCheck
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Check(bool ok,string why){if(!ok)throw new Exception("Coaler animation: "+why);}
    static object Get(object o,string field){return o.GetType().GetField(field,Private).GetValue(o);}
    [MenuItem("Recoil/Tests/Verify Coaler Tentacle Animations")]
    public static void VerifyAndCapture()
    {
        VerifyPatrol();CoalerAcousticAIVerification.Verify();
        var previous=SceneManager.GetActiveScene();var activeRoots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
        var ambient=RenderSettings.ambientLight;bool fog=RenderSettings.fog;foreach(var root in activeRoots)root.SetActive(false);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        try
        {
            var go=new GameObject("Temporary tentacle animation check");go.transform.position=Vector3.up*.8f;var v=go.AddComponent<RampagerTentacleVisual>();v.followAI=false;v.previewForm=RampagerTentacleVisual.Form.Enraged;
            string folder="Assets/Recoil3D/Materials/TentacleWorker/";
            foreach(var pair in new[]{new[]{"tentacleMaterial","Black fibrous upper body"},new[]{"trouserMaterial","Worn safety orange"},new[]{"patchMaterial","Charcoal knee reinforcements"},new[]{"bootMaterial","Heavy work boots"},new[]{"coreMaterial","Dark red suspended core"},new[]{"badgeMaterial","Faded identity card"}})
                typeof(RampagerTentacleVisual).GetField(pair[0]).SetValue(v,AssetDatabase.LoadAssetAtPath<Material>(folder+pair[1]+".mat"));
            v.Build();v.UpdatePose(0,true);
            var strands=(IEnumerable)Get(v,"strands");var controls=typeof(RampagerTentacleVisual).GetMethod("Controls",Private);var curve=(Vector3[])Get(v,"curve");int bundles=0;
            foreach(var strand in strands)
            {
                int group=(int)strand.GetType().GetField("group").GetValue(strand);if(group!=1&&group!=4&&group!=5&&group!=6)continue;
                controls.Invoke(v,new[]{strand,(object)Vector3.zero});Vector3 tipDirection=curve[3]-curve[2];
                Check(curve[2].z>curve[0].z+.20f,"Enraged strand does not arch backwards");
                Check(Vector3.Dot(tipDirection.normalized,Vector3.back)>.9f,"Enraged tip does not point forwards");bundles++;
            }
            var cam=new GameObject("Temporary animation camera").AddComponent<Camera>();cam.transform.position=new Vector3(3,2.5f,-7.6f);cam.transform.LookAt(new Vector3(0,1.5f,0));cam.fieldOfView=35;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.09f,.10f);cam.allowHDR=true;
            var key=new GameObject("Temporary key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.2f;key.transform.rotation=Quaternion.Euler(35,-30,0);
            RenderSettings.ambientLight=new Color(.18f,.18f,.18f);RenderSettings.fog=false;
            WarehousePathLightingPass.Capture(cam,"Coaler_Enraged_Front");cam.transform.position=new Vector3(6,2.2f,0);cam.transform.LookAt(new Vector3(0,1.4f,0));WarehousePathLightingPass.Capture(cam,"Coaler_Enraged_Side");
            cam.transform.position=new Vector3(3,2.5f,-7.6f);cam.transform.LookAt(new Vector3(0,1.5f,0));v.SetPreview(RampagerTentacleVisual.Form.Idle);v.UpdatePose(0,true);
            foreach(var strand in strands)
            {
                int group=(int)strand.GetType().GetField("group").GetValue(strand);if(group!=4&&group!=5&&group!=6)continue;
                controls.Invoke(v,new[]{strand,(object)Vector3.zero});Check(curve[1].y<curve[0].y-.3f,"Idle roots must arch down first");Check(curve[3].y>curve[2].y+.4f,"Idle tips must curl upwards");Check(new Vector2(curve[3].x-curve[0].x,curve[3].z-curve[0].z).magnitude>.6f,"Idle tips must unfurl outwards");
            }
            WarehousePathLightingPass.Capture(cam,"Coaler_Idle_Anemone");var mesh=v.VisualRoot.GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh.name=="Dynamic combined curved tentacles").sharedMesh;var before=mesh.vertices;
            v.PreviewTick(.5f);var after=mesh.vertices;Check(after.Zip(before,(a,b)=>(a-b).sqrMagnitude).Any(d=>d>.000001f),"Idle must gently sway");
            foreach(int side in new[]{-1,1})
            {
                v.previewSweepSide=0;v.UpdatePose(0,true);before=mesh.vertices;var rotation=v.VisualRoot.rotation;
                v.previewSweepSide=side;v.previewSweepProgress=.5f;v.UpdatePose(0,true);after=mesh.vertices;
                Check(Quaternion.Angle(rotation,v.VisualRoot.rotation)<.001f,"Sweep must not turn the body");
                int moved=0,index=0;foreach(var strand in strands)
                {
                    bool changed=false;for(int vertex=index*90;vertex<(index+1)*90;vertex++)if((after[vertex]-before[vertex]).sqrMagnitude>.000001f){changed=true;break;}
                    if(changed){moved++;int group=(int)strand.GetType().GetField("group").GetValue(strand);Check(group==6,"Only the chosen lowest strand should sweep");}index++;
                }
                Check(moved==1,"Exactly one lower strand must sweep");WarehousePathLightingPass.Capture(cam,side<0?"Coaler_Sweep_Left":"Coaler_Sweep_Right");
            }
            Check(mesh.vertices.All(p=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)),"Mesh contains invalid vertices");
            Debug.Log("COALER_TENTACLE_ANIMATION_PASS: "+bundles+" Enraged upper/lower fibres arch backwards and point forwards; idle arches down then unfurls outwards/upwards and sways; isolated left/right lower sweeps; body remains still.");
        }
        finally{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);foreach(var root in activeRoots)if(root)root.SetActive(true);RenderSettings.ambientLight=ambient;RenderSettings.fog=fog;}
    }
    static void VerifyPatrol()
    {
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var go=new GameObject("Temporary patrol sweep check");SceneManager.MoveGameObjectToScene(go,scene);var owner=go.AddComponent<RampagerController>();var route=go.AddComponent<RampagerPatrolRoute>();route.loop=false;
            var child=new GameObject("Look point");child.transform.SetParent(go.transform,false);var point=child.AddComponent<RampagerPatrolPoint>();point.duration=.25f;
            foreach(var action in new[]{RampagerPatrolPoint.Action.LookLeft,RampagerPatrolPoint.Action.LookRight})
            {
                point.action=action;route.Initialize(owner);float direction,distance,look;route.Evaluate(go.transform.position,.25f,1,1,out direction,out distance,out look);
                Check(!route.Complete&&Mathf.Abs(route.TentacleSweepProgress-.5f)<.001f,"A .25 second look must take .5 seconds");Check(!route.RequestsFacing&&look==1,"Built-in looks must not request a body turn");Check(route.TentacleSweepSide==(action==RampagerPatrolPoint.Action.LookLeft?-1:1),"Wrong lower strand requested");
                route.Evaluate(go.transform.position,.25f,1,1,out direction,out distance,out look);Check(route.Complete,"Doubled look duration never completes");route.Suspend();Check(route.TentacleSweepSide==0,"Interrupted look retains a sweep");
            }
            point.action=RampagerPatrolPoint.Action.LookAround;point.duration=1;point.lookInterval=.25f;route.Initialize(owner);float d,s,l;
            route.Evaluate(go.transform.position,.25f,1,1,out d,out s,out l);Check(route.TentacleSweepSide==-1,"LookAround must begin left");route.Evaluate(go.transform.position,.5f,1,1,out d,out s,out l);Check(route.TentacleSweepSide==1,"LookAround must alternate right");
            point.action=RampagerPatrolPoint.Action.Stay;Check(RampagerPatrolRoute.ActionDuration(point)==point.duration,"Stay timing must remain unchanged");
            Debug.Log("COALER_PATROL_SWEEP_PASS: look duration/interval doubled; body turn removed; left/right alternate; interruption clears sweep; Stay unchanged.");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
