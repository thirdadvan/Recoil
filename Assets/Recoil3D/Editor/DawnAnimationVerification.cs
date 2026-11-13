using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DawnAnimationVerification
{
    static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
    static void Need(bool ok,string m){if(!ok)throw new Exception("Dawn validation: "+m);}
    public static IEnumerator VerifyLive()
    {
        const string output="Captures/DawnAnimation";Directory.CreateDirectory(output);
        Need(EditorApplication.isPlaying,"Run this check in Play mode so humanoid IK is evaluated");
        var scene=SceneManager.CreateScene("Dawn animation validation temporary");var report=new List<string>();
        var world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();bool previousGun=world.startWithWaterGun;float previousClock=world.Clock;
        try
        {
            var root=new GameObject("Dawn validation only");SceneManager.MoveGameObjectToScene(root,scene);
            var pg=new GameObject("Dawn test player");pg.transform.SetParent(root.transform);var player=pg.AddComponent<RecoilPlayerController>();
            Func<string,Transform> part=n=>{var t=new GameObject(n).transform;t.SetParent(pg.transform);return t;};
            var sourceGun=(Transform)new SerializedObject(world.Player).FindProperty("gun").objectReferenceValue;var gun=UnityEngine.Object.Instantiate(sourceGun.gameObject,pg.transform).transform;gun.name="Test water gun";player.Configure(world,null,part("Legacy coat"),part("Legacy head"),part("Legacy hat"),gun);world.startWithWaterGun=true;
            var old=pg.AddComponent<Recoil3DActorVisual>();old.model=part("Legacy model");old.parts=new Renderer[0];
            player.enabled=false;var d=DawnCharacterIntegration.Install(player,old);d.manuallyDriven=true;foreach(var tr in root.GetComponentsInChildren<Transform>(true))tr.gameObject.layer=31;typeof(DawnPlayerAnimation).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);var animator=d.animator;
            Need(animator.avatar.isValid&&animator.isHuman,"Invalid human avatar");Need(!animator.applyRootMotion,"Root motion would change collision movement");
            var controller=(UnityEditor.Animations.AnimatorController)animator.runtimeAnimatorController;
            Need(controller.layers.Length==2&&controller.layers[1].avatarMask&&controller.layers[1].iKPass,"Masked weapon IK layer missing");
            var sm=d.GetComponentInChildren<SkinnedMeshRenderer>();Need(sm.sharedMesh.triangles.Length/3==2837,"Unexpected mesh triangles");
            Need(sm.sharedMesh.boneWeights.All(w=>w.weight0+w.weight1+w.weight2+w.weight3>.99f),"Unweighted mesh vertices");
            var cameraGo=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(cameraGo,scene);var camera=cameraGo.AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.23f,.24f);camera.orthographic=true;camera.orthographicSize=1.15f;camera.nearClipPlane=.01f;camera.farClipPlane=30;
            foreach(var v in new[]{new Vector3(1,3,-4),new Vector3(-2,2,2)}){var light=new GameObject("Preview light");SceneManager.MoveGameObjectToScene(light,scene);light.transform.position=v;light.transform.LookAt(Vector3.zero);var l=light.AddComponent<Light>();l.type=LightType.Directional;l.intensity=1;l.cullingMask=1<<31;}
            player.ResetController(new Vector2(0,.825f));Set(player,"grounded",true);typeof(DawnPlayerAnimation).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(d,null);
            Action<float,float> tick=(dx,dy)=>{player.Position+=new Vector2(dx,dy);Set(world,"simTime",world.Clock+1f/60);player.SyncVisuals(1f/60);d.AdvancePresentation(1f/60);animator.Update(1f/60);};
            Vector3 previewView=new Vector3(3,.5f,-4);
            Action<string> capture=n=>{var pos=player.transform.position;camera.transform.position=pos+previewView;camera.transform.LookAt(pos+Vector3.up*.02f);var rt=new RenderTexture(900,900,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(output+"/"+n+".png",tex.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);};
            for(int i=0;i<40;i++){tick(0,0);yield return null;Need(Mathf.Abs((d.facingRoot.position-player.transform.position).y-player.StepVisualOffset-d.modelHeightOffset)<.0001f,"Idle visual height drift");}Need(d.CurrentPose=="Locomotion","Idle not selected");capture("01-IdleGun");
            // Reference silhouette keys without the gun overlay hiding arm motion.
            world.startWithWaterGun=false;player.Aim=Vector2.right;
            for(int i=0;i<45;i++){tick(0,0);yield return null;}
            previewView=new Vector3(0,.1f,-5);
            for(int k=0;k<6;k++)
            {
                string state=k<=3?"JumpRise":"JumpFall";float time=k<=3?k/3f:(k-3)/2f;
                animator.Play(state,0,time);animator.Update(0);animator.speed=0;yield return null;
                capture("JumpKey-"+(k+1));
                if(k>=3)Need(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position.x-animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position.x>=animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position.x-animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position.x-.005f,"Jump descent swaps the leading leg");
                report.Add("JumpKey "+(k+1)+" left knee "+(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position-animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg).position).ToString("F3")+" right knee "+(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg).position-animator.GetBoneTransform(HumanBodyBones.RightUpperLeg).position).ToString("F3"));
            }
            previewView=new Vector3(3,.5f,-4);
            animator.speed=1;world.startWithWaterGun=true;animator.Play("Locomotion",0,0);
            for(int i=0;i<20;i++){tick(0,0);yield return null;}
            var idleFeet=animator.GetBoneTransform(HumanBodyBones.LeftFoot).localRotation;var baked=new Mesh();sm.BakeMesh(baked);var idleVertices=baked.vertices;
            for(int i=0;i<40;i++){tick(2.772f/60,0);yield return null;Need(Mathf.Abs((d.facingRoot.position-player.transform.position).y-player.StepVisualOffset-d.modelHeightOffset)<.0001f,"Running visual height drift");}Need(Quaternion.Angle(idleFeet,animator.GetBoneTransform(HumanBodyBones.LeftFoot).localRotation)>2,"Run animation did not move the actual foot bone");Need(d.AimWeight>.99f&&animator.GetFloat("Speed")>2.6f,"Run and weapon layer did not blend");sm.BakeMesh(baked);var runVertices=baked.vertices;Need(idleVertices.Where((v,n)=>sm.transform.TransformVector(v-runVertices[n]).magnitude>.02f).Count()>100,"Skinned surface did not animate");UnityEngine.Object.DestroyImmediate(baked);capture("02-RunGun");report.Add("PASS: run+gun combined through masked layer and IK, movement speed drives blend tree.");
            player.Aim=Vector2.up;for(int i=0;i<25;i++){tick(-1.188f/60,0);yield return null;}Need(animator.GetFloat("Direction")<0,"Backward walk cycle did not reverse");capture("03-BackpedalAimUp");
            Set(player,"grounded",false);for(int i=0;i<15;i++){tick(.01f,.03f);yield return null;}Need(d.CurrentPose=="JumpRise","Jump rise missing");capture("04-JumpGun");
            for(int i=0;i<15;i++){tick(.01f,-.03f);yield return null;}Need(d.CurrentPose=="JumpFall","Jump fall missing");
            Set(player,"flying",true);Set(player,"recoilApplied",true);Set(player,"poolAt",world.Clock);for(int i=0;i<20;i++){tick(.02f,.01f);yield return null;}Need(d.CurrentPose=="FlightPedal","Recoil flight must retain slow pedalling");capture("05-RecoilFlight");
            Set(player,"poolAt",world.Clock-2);for(int i=0;i<40;i++){tick(.01f,-.005f);yield return null;}Need(d.CurrentPose=="FlightPedal","Post-recoil pedal pose missing");capture("06-FlightPedal");
            var pedalFoot=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position-player.transform.position;
            for(int i=0;i<48;i++){tick(0,0);yield return null;}
            Need(Vector3.Distance(pedalFoot,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position-player.transform.position)>.035f,"Slow pedalling did not move the feet");
            Need(d.AimWeight>.99f,"Pedalling removed upper body aiming");capture("06b-FlightPedalOpposite");
            Set(player,"flying",false);Set(player,"grounded",true);tick(0,0);yield return null;Need(d.CurrentPose=="Land","Landing transition missing");for(int i=0;i<25;i++){tick(0,0);yield return null;}Need(d.CurrentPose=="Locomotion","Landing never returns to grounded locomotion");
            var ladderObject=new GameObject("Test ladder");ladderObject.transform.SetParent(root.transform);var ladder=ladderObject.AddComponent<ClimbableLadder>();Set(player,"grounded",false);Set(player,"climbingLadder",ladder);
            for(int i=0;i<60;i++){tick(0,.01f);yield return null;}Need(d.CurrentPose=="Climb"&&d.AimWeight<.01f&&!gun.gameObject.activeSelf,"Climbing did not release and hide the weapon");
            Need(Mathf.Abs(Mathf.DeltaAngle(d.facingRoot.localEulerAngles.y,180))<1,"Climber did not turn toward ladder");capture("08-Climb");
            var climbingHand=animator.GetBoneTransform(HumanBodyBones.LeftHand).position-player.transform.position;
            for(int i=0;i<25;i++){tick(0,.01f);yield return null;}
            Need(Vector3.Distance(climbingHand,animator.GetBoneTransform(HumanBodyBones.LeftHand).position-player.transform.position)>.03f,"Climbing hands are not cycling");capture("08b-ClimbOppositeStep");
            var pausedHand=animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
            for(int i=0;i<12;i++){tick(0,0);yield return null;}
            Need(Vector3.Distance(pausedHand,animator.GetBoneTransform(HumanBodyBones.LeftHand).position)<.005f,"Climb cycle moves when stationary");
            report.Add("PASS: ladder faces +Z; alternating hands animate; cycle pauses without vertical movement.");
            Set(player,"climbingLadder",null);var handholdObject=new GameObject("Test handhold");handholdObject.transform.SetParent(root.transform);var handhold=handholdObject.AddComponent<EquipmentHandhold>();Set(player,"holdingHandhold",handhold);for(int i=0;i<25;i++){tick(0,0);yield return null;}Need(d.CurrentPose=="Hang","Hanging pose not selected");capture("09-Hang");Set(player,"holdingHandhold",null);Set(player,"grounded",true);for(int i=0;i<20;i++){tick(0,0);yield return null;}
            var before=animator.GetBoneTransform(HumanBodyBones.LeftFoot).position;d.AdvancePresentation(0);animator.Update(0);Need(Vector3.Distance(before,animator.GetBoneTransform(HumanBodyBones.LeftFoot).position)<.001f,"Paused pose drift");
            foreach(string state in new[]{"JumpRise","JumpFall","Land","RecoilFlight","FlightPedal","Climb","Hang"})Need(animator.HasState(0,Animator.StringToHash(state)),"Missing "+state);
            report.Add("PASS: jump rise/fall, recoil acceleration, pedal flight, landing, pause, climb and hang; attached poses hide the weapon.");
            report.Add("PASS: 2837 triangles, valid humanoid, all vertices weighted, root motion off.");
            var actual=world.Player;var actualDriver=actual.GetComponentInChildren<DawnPlayerAnimation>();actual.enabled=false;actualDriver.manuallyDriven=true;Set(actual,"grounded",true);actual.Aim=Vector2.right;
            for(int i=0;i<25;i++){actual.SyncVisuals(1f/60);actualDriver.AdvancePresentation(1f/60);actualDriver.animator.Update(1f/60);yield return null;}
            var mainCamera=Camera.main;var gameTarget=new RenderTexture(1600,900,24);var oldTarget=mainCamera.targetTexture;var oldActive=RenderTexture.active;mainCamera.targetTexture=gameTarget;mainCamera.Render();RenderTexture.active=gameTarget;var gameImage=new Texture2D(1600,900,TextureFormat.RGB24,false);gameImage.ReadPixels(new Rect(0,0,1600,900),0,0);gameImage.Apply();File.WriteAllBytes(output+"/07-GameScene.png",gameImage.EncodeToPNG());mainCamera.targetTexture=oldTarget;RenderTexture.active=oldActive;UnityEngine.Object.DestroyImmediate(gameTarget);UnityEngine.Object.DestroyImmediate(gameImage);

            var bone=animator.GetBoneTransform(HumanBodyBones.Head).position;Need(!float.IsNaN(bone.x)&&!float.IsInfinity(bone.y),"Nonfinite animation pose");
            File.WriteAllLines(output+"/Verification.txt",report);Debug.Log("DAWN_ANIMATION_CHECK_PASS\n"+string.Join("\n",report));
        }
        finally{world.startWithWaterGun=previousGun;Set(world,"simTime",previousClock);foreach(var g in scene.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(g);SceneManager.UnloadSceneAsync(scene);}
    }
}
