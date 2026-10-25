using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class FreightTelescopicUpgrade
{
    [MenuItem("Recoil/Art Tests/Upgrade Freight To Telescopic Mast")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.isPlaying=false;EditorApplication.delayCall+=AfterPlay;return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var active=SceneManager.GetActiveScene();string backup="Backups/FreightTelescopic/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryWarehouse.unity",WarehouseInsideLightingTestBuilder.Destination})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.SaveScene(scene,backup+"/"+scene.name+".unity",true);
                var roots=scene.GetRootGameObjects();var crane=roots.SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).Single();
                var mast=crane.GetComponent<FreightTelescopicMast>();
                if(!mast)
                {
                    crane.liftCarriage.position+=Vector3.up*.08f;Physics2D.SyncTransforms();
                    foreach(var t in crane.liftCarriage.GetComponentsInChildren<Transform>(true))
                    {
                        if(t.name=="Thin steel walking plate"){t.localPosition=new Vector3(0,-.07f,0);var s=t.localScale;s.y=.14f;t.localScale=s;}
                        if(t.name=="Exposed underside runner"){var p=t.localPosition;p.y=-.17f;t.localPosition=p;}
                        if(t.name=="Thin foreground deck edge"){var p=t.localPosition;p.y=-.09f;t.localPosition=p;var s=t.localScale;s.y=.18f;t.localScale=s;}
                        if(t.name=="Thin fork linkage and guide rollers"){var p=t.position;p.y=crane.forkPlatform.Area.yMax-.17f;t.position=p;}
                    }
                    var original=crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source&&v.source.name=="Guided steel mast").ToArray();
                    float x=original.Average(v=>v.meshRenderer.bounds.center.x),z=original.Average(v=>v.meshRenderer.bounds.center.z);
                    float top=Mathf.Max(original.Max(v=>v.meshRenderer.bounds.max.y),crane.travelRig.position.y+6.55f);
                    foreach(var v in crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
                        if(v.source&&new[]{"Guided steel mast","Mast cross brace","Polished mast guide","Lift chain dark channel","Chain link"}.Contains(v.source.name)){v.suppressed=true;v.Sync();}
                    mast=crane.gameObject.AddComponent<FreightTelescopicMast>();mast.topAnchor=G("Telescopic mast - fixed upper sleeve",crane.travelRig,new Vector3(x,top,z));
                    mast.sectionLength=(top-crane.forkPlatform.Area.yMax-.10f)/3.45f;
                    mast.sections=new Transform[4];
                    var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Freight exposed guide steel.mat");
                    for(int i=0;i<4;i++)
                    {
                        float width=.58f-i*.065f,depth=.43f-i*.055f;var section=G("Nested mast section "+(i+1),mast.topAnchor,mast.topAnchor.position);mast.sections[i]=section;
                        foreach(float xx in new[]{-width*.5f,width*.5f})foreach(float zz in new[]{-depth*.5f,depth*.5f})
                            Box("Telescopic guide rail",section,new Vector3(xx,0,zz),new Vector3(.045f,mast.sectionLength,.045f),mat);
                        foreach(float yy in new[]{-mast.sectionLength*.5f+.035f,mast.sectionLength*.5f-.035f})
                        {
                            foreach(float zz in new[]{-depth*.5f,depth*.5f})Box("Sliding sleeve collar",section,new Vector3(0,yy,zz),new Vector3(width+.05f,.065f,.055f),mat);
                            foreach(float xx in new[]{-width*.5f,width*.5f})Box("Sliding sleeve side",section,new Vector3(xx,yy,0),new Vector3(.055f,.065f,depth),mat);
                        }
                    }
                }
                float low=Mathf.Min(crane.startPoint.position.x,crane.destinationPoint.position.x),high=Mathf.Max(crane.startPoint.position.x,crane.destinationPoint.position.x),obstacle=0;
                foreach(var c in roots.SelectMany(g=>g.GetComponentsInChildren<BoxCollider2D>(true)))
                {
                    if(c.isTrigger||!c.enabled||c.GetComponentInParent<KBKStackerCrane>())continue;var b=c.bounds;
                    if(b.max.x>low&&b.min.x<high&&b.max.y>.4f&&b.max.y<5)obstacle=Mathf.Max(obstacle,b.max.y);
                }
                crane.liftHeight=Mathf.Max(4.2f,obstacle+.3f-crane.forkPlatform.Area.yMax);
                float maxLift=mast.topAnchor.position.y-mast.sectionLength-.10f-crane.forkPlatform.Area.yMax;
                if(crane.liftHeight>maxLift+.01f)throw new Exception("Mast retraction clearance insufficient for cargo; requested "+crane.liftHeight+", maximum "+maxLift);
                mast.ApplyPose();
                var noise=crane.GetComponent<FreightNoiseEmitter>();noise.soundRadiusMultiplier=1.6f;
                var drive=crane.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Hoist gearmotor - visual housing"||t.name=="Hoist gearmotor - spatial sound origin");
                noise.soundOrigin=drive?drive:mast.topAnchor;
                var audio=crane.GetComponent<FreightMotorAudio>();if(audio)audio.volume=.48f;
                foreach(var actor in roots.SelectMany(g=>g.GetComponentsInChildren<Recoil3DActorVisual>(true)))if(actor.GetComponent<RecoilPlayerController>())Recoil3DPlayerLegRig.Configure(actor);
                VerifyCrane(crane,obstacle);
                if(path==WarehouseInsideLightingTestBuilder.Destination)Capture(roots,crane,mast);
                EditorUtility.SetDirty(crane);EditorUtility.SetDirty(mast);EditorUtility.SetDirty(noise);if(audio)EditorUtility.SetDirty(audio);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
                Debug.Log("FREIGHT_TELESCOPIC_READY: "+path+"; obstacle top="+obstacle+"; lift="+crane.liftHeight+"; noise x1.6; motor volume="+(audio?audio.volume:0));
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(active.IsValid()&&active.isLoaded)SceneManager.SetActiveScene(active);}
        }
    }
    static void AfterPlay(){if(EditorApplication.isPlayingOrWillChangePlaymode){EditorApplication.delayCall+=AfterPlay;return;}Apply();}
    static Transform G(string name,Transform parent,Vector3 p)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=p;t.rotation=Quaternion.identity;var s=parent.lossyScale;t.localScale=new Vector3(1/Mathf.Abs(s.x),1/Mathf.Abs(s.y),1/Mathf.Abs(s.z));return t;}
    static void Box(string name,Transform parent,Vector3 p,Vector3 size,Material material)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;}
    static void VerifyCrane(KBKStackerCrane original,float obstacle)
    {
        var scene=EditorSceneManager.NewPreviewScene();GameObject copy=null;
        try
        {
            copy=UnityEngine.Object.Instantiate(original.gameObject);SceneManager.MoveGameObjectToScene(copy,scene);
            var c=copy.GetComponent<KBKStackerCrane>();c.requiredPowerItem="";c.ResetMachine();var m=c.GetComponent<FreightTelescopicMast>();float parked=c.forkPlatform.Area.yMax;
            for(int trip=0;trip<2;trip++)
            {
                if(!c.Activate())throw new Exception("Crane cycle failed to start");
                int ticks=0;
                while(c.Busy&&ticks++<3000)
                {
                    c.Simulate(.02f);m.ApplyPose();
                    if(c.Phase==KBKStackerCrane.MotionPhase.Travelling&&(m.Bottom<obstacle+.15f||c.forkPlatform.Area.yMax<obstacle+.15f))throw new Exception("Cargo intersects telescopic crane during travel");
                    if((c.GetComponent<FreightNoiseEmitter>().ActiveOrigin.position-m.topAnchor.position).magnitude>1.2f)throw new Exception("Sound origin is not on upper assembly");
                }
                if(c.Busy||Mathf.Abs(c.forkPlatform.Area.yMax-parked)>.002f)throw new Exception("Crane did not lower to its original deck height");
            }
            Debug.Log("FREIGHT_TELESCOPIC_CYCLE_CHECK_PASS");
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void Capture(GameObject[] roots,KBKStackerCrane c,FreightTelescopicMast mast)
    {
        var study=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();var camera=study.GetComponent<Camera>();var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;
        var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;
        var local=c.liftCarriage.localPosition;
        try
        {
            if(follow)follow.enabled=false;camera.transform.SetPositionAndRotation(new Vector3(c.forkPlatform.Area.center.x,4,-15),Quaternion.Euler(5,0,0));camera.fieldOfView=44;
            mast.ApplyPose();WarehousePathLightingPass.Capture(camera,"Telescopic_Extended");
            c.liftCarriage.localPosition+=Vector3.up*c.liftHeight;Physics2D.SyncTransforms();mast.ApplyPose();WarehousePathLightingPass.Capture(camera,"Telescopic_Retracted");
        }
        finally{c.liftCarriage.localPosition=local;Physics2D.SyncTransforms();mast.ApplyPose();camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(follow)follow.enabled=on;}
    }
}
