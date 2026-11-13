using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DawnCharacterIntegration
{
    public const float VisualScale=1.3f;
    public const string Root="Assets/Art/Characters/Dawn";
    const string Model=Root+"/Dawn.fbx",Controller=Root+"/Dawn.controller";
    static string[] names=HumanTrait.MuscleName;
    static float[] idle;
    static Vector3 body;
    static Quaternion rotation;
    static void Set(float[] p,string name,float value){int i=Array.IndexOf(names,name);if(i>=0)p[i]=value;}
    static void Side(float[] p,string side,float hip,float knee,float arm=-.65f,float elbow=.4f)
    {Set(p,side+" Upper Leg Front-Back",Mathf.Clamp(idle[Array.IndexOf(names,side+" Upper Leg Front-Back")]+hip,-1,1));Set(p,side+" Lower Leg Stretch",Mathf.Clamp(idle[Array.IndexOf(names,side+" Lower Leg Stretch")]+knee-1,-1,1));Set(p,side+" Arm Down-Up",arm);Set(p,side+" Forearm Stretch",elbow);}
    [MenuItem("Recoil/Dawn/1 Build animation assets")]
    public static void BuildAssets()
    {
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        importer.isReadable=true; // Sole contact reads the small lowpoly mesh in player builds.
        importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var motion=(ModelImporter)AssetImporter.GetAtPath(Root+"/Dawn_Motion.fbx");motion.animationType=ModelImporterAnimationType.Human;motion.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;motion.sourceAvatar=source.GetComponent<Animator>().avatar;
        var clips=motion.defaultClipAnimations;
        foreach(var c in clips){c.loopTime=true;c.loopPose=true;c.lockRootRotation=true;c.lockRootHeightY=true;c.lockRootPositionXZ=true;c.keepOriginalOrientation=true;c.keepOriginalPositionXZ=true;c.keepOriginalPositionY=true;}
        motion.clipAnimations=clips;motion.SaveAndReimport();
        var temp=UnityEngine.Object.Instantiate(source);temp.hideFlags=HideFlags.HideAndDontSave;temp.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        var animator=temp.GetComponent<Animator>();
        var imported=AssetDatabase.LoadAllAssetsAtPath(Root+"/Dawn_Motion.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
        var walk=imported.First(c=>c.name.Contains("Walking"));var run=imported.First(c=>c.name.Contains("Running"));
        // Capture the actual T-pose neutral values, not muscle-range midpoints.
        var handler=new HumanPoseHandler(animator.avatar,temp.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);
        idle=(float[])pose.muscles.Clone();body=pose.bodyPosition;body.x=body.z=0;rotation=pose.bodyRotation;
        // Neutralise gait without discarding the avatar's calibrated torso and arm axes.
        
        foreach(var side in new[]{"Left","Right"}){Set(idle,side+" Arm Front-Back",0);Set(idle,side+" Arm Down-Up",-.8f);Set(idle,side+" Forearm Stretch",.8f);}
        
        handler.Dispose();UnityEngine.Object.DestroyImmediate(temp);
        var idleClip=Make("Idle",1.8f,true,t=>{var p=Copy();Set(p,"Chest Front-Back",Mathf.Sin(t*Mathf.PI*2)*.025f);return p;});
        var rise=Make("JumpRise",.36f,false,t=>JumpPose(t*.6f));
        var fall=Make("JumpFall",.38f,false,t=>JumpPose(.6f+t*.4f));
        var land=Make("Land",.18f,false,LandingPose);
        var recoil=Make("RecoilFlight",.75f,true,t=>{var p=Copy();Side(p,"Left",.65f,-.55f,-.3f,.4f);Side(p,"Right",-.2f,.75f,-.3f,.4f);Set(p,"Spine Front-Back",-.1f);return p;});
        var pedal=Make("FlightPedal",3.2f,true,SlowPedalPose);
        var climb=Make("Climb",1.2f,true,ClimbPose);
        var hang=Make("Hang",1.6f,true,t=>{var p=Copy();Side(p,"Left",.15f,.3f,.9f,.7f);Side(p,"Right",.08f,.4f,.9f,.7f);return p;});
        var hold=Make("HoldGun",1,true,t=>{var p=Copy();foreach(var side in new[]{"Left","Right"}){Set(p,side+" Arm Down-Up",-.2f);Set(p,side+" Arm Front-Back",.7f);Set(p,side+" Forearm Stretch",0);foreach(string finger in new[]{"Index","Middle","Ring","Little"})foreach(int n in new[]{1,2,3})Set(p,side+" "+finger+" "+n+" Stretched",-.45f);}return p;});
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Controller);
        foreach(var l in controller.layers)UnityEngine.Object.DestroyImmediate(l.stateMachine,true);
        controller.layers=new AnimatorControllerLayer[0];controller.parameters=new AnimatorControllerParameter[0];
        controller.AddParameter("Speed",AnimatorControllerParameterType.Float);controller.AddParameter("Direction",AnimatorControllerParameterType.Float);controller.AddParameter("ClimbSpeed",AnimatorControllerParameterType.Float);
        controller.AddLayer("Locomotion and air");var layer=controller.layers[0];layer.defaultWeight=1;var sm=layer.stateMachine;
        var tree=new BlendTree{name="Idle Walk Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,controller);tree.AddChild(idleClip,0);tree.AddChild(walk,1.188f);tree.AddChild(run,2.772f);
        var locomotion=sm.AddState("Locomotion");locomotion.motion=tree;locomotion.speedParameter="Direction";locomotion.speedParameterActive=true;sm.defaultState=locomotion;
        foreach(var pair in new[]{( "JumpRise",rise),("JumpFall",fall),("Land",land),("RecoilFlight",recoil),("FlightPedal",pedal),("Climb",climb),("Hang",hang)}){var s=sm.AddState(pair.Item1);s.motion=pair.Item2;if(pair.Item1=="Climb"){s.speedParameter="ClimbSpeed";s.speedParameterActive=true;}}
        controller.layers=new[]{layer};controller.AddLayer("Weapon upper body");
        var mask=AssetDatabase.LoadAssetAtPath<AvatarMask>(Root+"/UpperBody.mask");if(!mask){mask=new AvatarMask();AssetDatabase.CreateAsset(mask,Root+"/UpperBody.mask");}
        for(int i=0;i<(int)AvatarMaskBodyPart.LastBodyPart;i++)mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,false);
        foreach(var b in new[]{AvatarMaskBodyPart.Head,AvatarMaskBodyPart.LeftArm,AvatarMaskBodyPart.RightArm,AvatarMaskBodyPart.LeftFingers,AvatarMaskBodyPart.RightFingers,AvatarMaskBodyPart.LeftHandIK,AvatarMaskBodyPart.RightHandIK})mask.SetHumanoidBodyPartActive(b,true);
        var layers=controller.layers;layers[1].avatarMask=mask;layers[1].defaultWeight=0;layers[1].iKPass=true;layers[1].blendingMode=AnimatorLayerBlendingMode.Override;var hs=layers[1].stateMachine.AddState("Hold and aim");hs.motion=hold;layers[1].stateMachine.defaultState=hs;controller.layers=layers;
        var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Dawn.mat");if(!material){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,Root+"/Dawn.mat");}material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Dawn_BaseColor.png");material.color=Color.white;material.SetFloat("_Glossiness",0);material.SetFloat("_Metallic",0);EditorUtility.SetDirty(material);
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(mask);AssetDatabase.SaveAssets();Debug.Log("Dawn animation assets ready: humanoid blend tree, 8 authored clips, masked weapon layer and IK.");
    }
    // Six silhouette keys: push-off, knee drive, split stride, apex, trail-leg
    // recovery and reaching for landing. No root translation: physics owns the arc.
    static readonly float[,] jumpKeys={
        {.12f,.35f,-.18f,.85f,-.55f,.35f,-.7f,.5f},
        {.52f,.28f,-.22f,.55f,-.35f,.05f,-.65f,.35f},
        {.58f,.40f,-.32f,.48f,-.4f,.12f,-.6f,.25f},
        {.35f,.82f,-.25f,.28f,-.55f,.2f,-.4f,.12f},
        {.24f,.65f,-.14f,.60f,-.65f,.35f,-.35f,.15f},
        {.10f,.88f,.05f,.88f,-.65f,.45f,-.55f,.35f}
    };
    static float[] JumpPose(float t)
    {
        float f=Mathf.Clamp01(t)*5;int a=Mathf.Min(4,Mathf.FloorToInt(f));float u=Mathf.SmoothStep(0,1,f-a);
        var v=new float[8];for(int i=0;i<8;i++)v[i]=Mathf.Lerp(jumpKeys[a,i],jumpKeys[a+1,i],u);
        var p=Copy();Side(p,"Left",-v[0]*2.3f,v[1],v[4],v[5]);Side(p,"Right",-v[2]*2.3f,v[3],v[6],v[7]);
        Set(p,"Left Arm Front-Back",.35f);Set(p,"Right Arm Front-Back",-.2f);Set(p,"Spine Front-Back",.10f);return p;
    }
    static float[] LandingPose(float t)
    {
        var p=Copy();float bend=t<.3f?Mathf.SmoothStep(.2f,1,t/.3f):1-Mathf.SmoothStep(0,1,(t-.3f)/.7f);
        Side(p,"Left",-.28f*bend,1-.5f*bend);Side(p,"Right",-.25f*bend,1-.48f*bend);Set(p,"Spine Front-Back",.18f*bend);return p;
    }
    static float[] ClimbPose(float t)
    {
        var p=Copy();float phase=Mathf.Sin(t*Mathf.PI*2);
        foreach(string side in new[]{"Left","Right"})
        {
            float lift=(side=="Left"?phase:-phase)*.5f+.5f;
            Side(p,side,-(.12f+.42f*lift)*2.2f,.82f-.60f*lift,.35f+.40f*lift,.35f-.45f*lift);
            Set(p,side+" Arm Front-Back",.75f);Set(p,side+" Hand Down-Up",-.15f);
            Set(p,side+" Upper Leg In-Out",.08f);
        }
        Set(p,"Spine Front-Back",.06f);return p;
    }
    static float[] SlowPedalPose(float t)
    {
        var p=Copy();
        foreach(string side in new[]{"Left","Right"})
        {
            float phase=t*Mathf.PI*2+(side=="Right"?Mathf.PI:0);
            // Solve a circular ankle path in the sagittal plane using two equal
            // leg segments. The opposite foot is always half a revolution away.
            float x=.28f+.28f*Mathf.Cos(phase),y=1.42f+.28f*Mathf.Sin(phase);
            float distance=Mathf.Sqrt(x*x+y*y);
            float hip=(Mathf.Atan2(x,y)+Mathf.Acos(Mathf.Clamp(distance*.5f,-1,1)))*Mathf.Rad2Deg;
            float knee=2*Mathf.Acos(Mathf.Clamp(distance*.5f,-1,1))*Mathf.Rad2Deg;
            Side(p,side,-hip/70f,1-knee/80f,-.5f,.4f);
            Set(p,side+" Foot Up-Down",Mathf.Sin(phase)*.10f);
        }
        return p;
    }
    [MenuItem("Recoil/Dawn/5 Update slow flight pedalling")]
    public static void UpdateSlowFlight()
    {
        ReadNeutral();Make("FlightPedal",3.2f,true,SlowPedalPose);AssetDatabase.SaveAssets();
        Debug.Log("DAWN_SLOW_PEDAL_UPDATED: 3.2 seconds per revolution");
    }
    static void ReadNeutral()
    {
        var neutral=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/Idle.anim");
        idle=new float[names.Length];body=Vector3.zero;rotation=Quaternion.identity;
        foreach(var binding in AnimationUtility.GetCurveBindings(neutral))
        {
            float v=AnimationUtility.GetEditorCurve(neutral,binding).Evaluate(0);int m=Array.IndexOf(names,binding.propertyName);
            if(m>=0)idle[m]=v;
            switch(binding.propertyName){case "RootT.y":body.y=v;break;case "RootQ.x":rotation.x=v;break;case "RootQ.y":rotation.y=v;break;case "RootQ.z":rotation.z=v;break;case "RootQ.w":rotation.w=v;break;}
        }
    }
    [MenuItem("Recoil/Dawn/4 Update ladder and jump poses")]
    public static void UpdateClimbJump()
    {
        // Read calibrated neutral values from Idle, retaining unrelated animation edits.
        var neutral=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/Idle.anim");
        idle=new float[names.Length];body=Vector3.zero;rotation=Quaternion.identity;
        foreach(var binding in AnimationUtility.GetCurveBindings(neutral))
        {
            float v=AnimationUtility.GetEditorCurve(neutral,binding).Evaluate(0);int m=Array.IndexOf(names,binding.propertyName);
            if(m>=0)idle[m]=v;
            switch(binding.propertyName){case "RootT.y":body.y=v;break;case "RootQ.x":rotation.x=v;break;case "RootQ.y":rotation.y=v;break;case "RootQ.z":rotation.z=v;break;case "RootQ.w":rotation.w=v;break;}
        }
        Make("JumpRise",.36f,false,t=>JumpPose(t*.6f));Make("JumpFall",.38f,false,t=>JumpPose(.6f+t*.4f));
        Make("Land",.18f,false,LandingPose);Make("Climb",1.2f,true,ClimbPose);AssetDatabase.SaveAssets();
        Debug.Log("DAWN_CLIMB_JUMP_UPDATED");
    }
    static float[] Copy(){return (float[])idle.Clone();}
    static AnimationClip Make(string name,float duration,bool loop,Func<float,float[]> sample)
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Root+"/Animations/"+name+".anim");if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Root+"/Animations/"+name+".anim");}clip.ClearCurves();clip.name=name;clip.frameRate=30;
        int count=Mathf.CeilToInt(duration*30)+1;var curves=names.Select(_=>new AnimationCurve()).ToArray();
        for(int n=0;n<count;n++){float t=(float)n/(count-1),time=t*duration;var p=sample(t);for(int m=0;m<names.Length;m++)curves[m].AddKey(new Keyframe(time,p[m]));}
        for(int m=0;m<names.Length;m++)clip.SetCurve("",typeof(Animator),names[m],curves[m]);
        foreach(var v in new[]{("RootT.x",0f),("RootT.y",body.y),("RootT.z",0f),("RootQ.x",rotation.x),("RootQ.y",rotation.y),("RootQ.z",rotation.z),("RootQ.w",rotation.w)})clip.SetCurve("",typeof(Animator),v.Item1,AnimationCurve.Constant(0,duration,v.Item2));
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;settings.loopBlend=loop;settings.keepOriginalOrientation=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalPositionY=true;settings.loopBlendOrientation=true;settings.loopBlendPositionY=true;settings.loopBlendPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);return clip;
    }
    [MenuItem("Recoil/Dawn/2 Replace adult player in build scenes")]
    public static void ReplaceScenes()
    {
        var active=SceneManager.GetActiveScene();string original=active.path;Directory.CreateDirectory("Backups/DawnReplacement/2026-11-02/Scenes");
        // User explicitly requested retaining the current unsaved edits.
        if(active.isDirty)EditorSceneManager.SaveScene(active);
        int count=0;
        foreach(var item in EditorBuildSettings.scenes.Where(x=>x.enabled&&x.path.StartsWith("Assets/Scenes/3D/")))
        {
            string backup="Backups/DawnReplacement/2026-11-02/Scenes/"+Path.GetFileName(item.path);if(!File.Exists(backup))File.Copy(item.path,backup);
            var scene=SceneManager.GetSceneByPath(item.path);bool opened=!scene.IsValid()||!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(item.path,OpenSceneMode.Additive);
            foreach(var p in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPlayerController>(true)))
            {var v=p.GetComponent<Recoil3DActorVisual>();if(!v||v.child||p.GetComponentInChildren<DawnPlayerAnimation>(true))continue;Install(p,v);count++;}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
        AssetDatabase.SaveAssets();Debug.Log("Dawn replaced in "+count+" adult scenes. Child memory preserved. Active scene retained: "+original);
    }
    public static DawnPlayerAnimation Install(RecoilPlayerController player,Recoil3DActorVisual old)
    {
        if(old.model)old.model.gameObject.SetActive(false);old.enabled=false;var leg=player.GetComponent<Recoil3DPlayerLegPose>();if(leg)leg.enabled=false;
        var pivot=new GameObject("Dawn - animated visual").transform;pivot.SetParent(player.transform,false);var scale=player.transform.lossyScale;pivot.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));pivot.localRotation=Quaternion.Euler(0,-90,0);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model),player.gameObject.scene);go.name="Dawn skinned model";go.transform.SetParent(pivot,false);go.transform.localRotation=Quaternion.Euler(0,180,0);
        var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.Rebind();animator.Update(0);
        AlignModel(go.transform,animator,player);
        foreach(var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Dawn.mat");r.updateWhenOffscreen=true;r.localBounds=new Bounds(new Vector3(0,.8f,0),new Vector3(3,3,3));}
        var driver=go.AddComponent<DawnPlayerAnimation>();driver.animator=animator;driver.facingRoot=pivot;driver.player=player;driver.visibilitySource=old.visibilitySource;
        var serialized=new SerializedObject(player);driver.weapon=serialized.FindProperty("gun").objectReferenceValue as Transform;ConfigureWeapon(driver.weapon);EditorUtility.SetDirty(player.gameObject);return driver;
    }
    static void AlignModel(Transform model,Animator animator,RecoilPlayerController player)
    {
        model.localPosition=Vector3.zero;model.localScale=Vector3.one;
        animator.Rebind();animator.Update(0);
        // FBX mesh/bone nodes carry a centimetre scale. Use world-space skeleton
        // landmarks so BakeMesh's scale convention cannot apply that scale twice.
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var head=animator.GetBoneTransform(HumanBodyBones.Head);
        var top=model.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.EndsWith("HeadTop_End"));
        float min=Mathf.Min(left.position.y,right.position.y)-.065f;
        float max=top?top.position.y+.10f:head.position.y+.25f;
        model.localScale*=player.StandingHeight*VisualScale/Mathf.Max(.01f,max-min);
        min=Mathf.Min(left.position.y,right.position.y)-.065f*model.localScale.y;
        model.position+=Vector3.up*(player.transform.position.y-player.StandingHeight*.5f-min);

    }
    public static void AlignExistingScenes()
    {
        var active=SceneManager.GetActiveScene();if(active.isDirty)EditorSceneManager.SaveScene(active);
        foreach(var item in EditorBuildSettings.scenes.Where(x=>x.enabled&&x.path.StartsWith("Assets/Scenes/3D/")))
        {
            var scene=SceneManager.GetSceneByPath(item.path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(item.path,OpenSceneMode.Additive);
            foreach(var d in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DawnPlayerAnimation>(true))){AlignModel(d.transform,d.animator,d.player);ConfigureWeapon(d.weapon);EditorUtility.SetDirty(d);}
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
        }
    }

    static void ConfigureWeapon(Transform weapon)
    {
        if(!weapon)return;
        if(AssetDatabase.LoadAssetAtPath<GameObject>(RetroWaterBlasterIntegration.Assembly))
        {RetroWaterBlasterIntegration.Install(weapon);return;}
        if(weapon.Find("Dawn water gun visual"))return;
        foreach(var v in weapon.GetComponentsInChildren<Recoil3DSpriteVisual>(true))v.enabled=false;
        foreach(var r in weapon.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;
        var root=new GameObject("Dawn water gun visual").transform;root.SetParent(weapon,false);
        var size=weapon.lossyScale;root.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(size.x)),1/Mathf.Max(.001f,Mathf.Abs(size.y)),1/Mathf.Max(.001f,Mathf.Abs(size.z)));
        var orange=GunMaterial("Gun orange",new Color(.94f,.31f,.055f));var dark=GunMaterial("Gun dark",new Color(.1f,.13f,.14f));var tank=GunMaterial("Gun tank",new Color(.19f,.36f,.4f));
        GunPart(root,"Body",new Vector3(0,0,0),new Vector3(.25f,.09f,.065f),orange);
        GunPart(root,"Barrel",new Vector3(.15f,.015f,0),new Vector3(.09f,.045f,.045f),dark);
        GunPart(root,"Grip",new Vector3(-.075f,-.07f,0),new Vector3(.055f,.12f,.05f),orange);
        GunPart(root,"Reservoir",new Vector3(-.03f,.07f,0),new Vector3(.11f,.065f,.07f),tank);
        GunPart(root,"Trigger guard",new Vector3(-.02f,-.055f,0),new Vector3(.06f,.015f,.025f),dark);
    }
    static Material GunMaterial(string name,Color colour)
    {
        string path=Root+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}m.color=colour;m.SetFloat("_Glossiness",0);return m;
    }
    static void GunPart(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());}

}
