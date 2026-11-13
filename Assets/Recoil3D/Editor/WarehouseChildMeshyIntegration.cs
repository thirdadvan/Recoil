using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class WarehouseChildMeshyIntegration
{
 public const string Folder="Assets/Art/Characters/WarehouseChildMeshy";
 public const string ScenePath="Assets/Scenes/3D/RecoilWarehouseStoreroom.unity";
 static Animator actor;static float unit;
 static Transform Bone(HumanBodyBones b){return actor.GetBoneTransform(b);}
 static void Rotate(HumanBodyBones b,float x,float y=0,float z=0){var t=Bone(b);if(t)t.rotation=Quaternion.Euler(x,y,z)*t.rotation;}
 static void Arm(bool left,Vector3 target,Vector3 pole){
 var upper=Bone(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);var lower=Bone(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);var hand=Bone(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
 Vector3 origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,hand.position);Vector3 direction=(target-origin).normalized;float d=Mathf.Clamp(Vector3.Distance(target,origin),Mathf.Abs(a-b)+.001f,a+b-.002f);target=origin+direction*d;float along=(a*a-b*b+d*d)/(2*d);float across=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));var side=Vector3.ProjectOnPlane(pole-origin,direction).normalized;Vector3 elbow=origin+direction*along+side*across;upper.rotation=Quaternion.FromToRotation(lower.position-origin,elbow-origin)*upper.rotation;lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,target-lower.position)*lower.rotation;
 }
 static void Fingers(bool left,Vector3 direction){var hand=Bone(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);var finger=Bone(left?HumanBodyBones.LeftMiddleProximal:HumanBodyBones.RightMiddleProximal);if(finger)hand.rotation=Quaternion.FromToRotation(finger.position-hand.position,direction)*hand.rotation;}
 static void Pose(string mode,float time){
 float phase=time*Mathf.PI*.5f,breath=Mathf.Sin(phase),sob=Mathf.Sin(phase*6)+.35f*Mathf.Sin(phase*9);
 if(mode=="Crying"){
 // Two sharp inhalations followed by a long release; periodic over the four-second clip.
 float cycle=Mathf.Repeat(time,2f);
 float inhale=cycle<.22f?Mathf.SmoothStep(0,1,cycle/.22f):cycle<.43f?Mathf.Lerp(1,.55f,(cycle-.22f)/.21f):cycle<.62f?Mathf.SmoothStep(.55f,1.25f,(cycle-.43f)/.19f):1.25f*(1-Mathf.SmoothStep(0,1,(cycle-.62f)/1.38f));
 Rotate(HumanBodyBones.Spine,15-inhale*5);Rotate(HumanBodyBones.Chest,17-inhale*8,0,2);
 Rotate(HumanBodyBones.Neck,8+inhale*3,0,-4);Rotate(HumanBodyBones.Head,24+inhale*18,-12,-9);
 Rotate(HumanBodyBones.LeftShoulder,0,0,-inhale*6);Rotate(HumanBodyBones.RightShoulder,0,0,inhale*7);
 var h=Bone(HumanBodyBones.Head).position;var c=Bone(HumanBodyBones.Chest).position;
 // Raised left elbow stays outside the shoulder, wrist folds across toward the opposite ear.
 Arm(true,h+new Vector3(.10f,-.055f,.34f)*unit,c+new Vector3(-.46f,.12f,.52f)*unit);
 Fingers(true,new Vector3(.10f,.015f,-.025f));
 var hips=Bone(HumanBodyBones.Hips).position;
 Arm(false,hips+new Vector3(.20f,-.22f,.04f)*unit,hips+new Vector3(.38f,-.06f,.08f)*unit);
 Fingers(false,new Vector3(.01f,-.12f,.015f));
 }
 else if(mode=="Walking"){
 // Five small, slightly hesitant strides over the four-second loop.
 float cycle=time*Mathf.PI*2/.8f;
 float left=Mathf.Sin(cycle),right=-left;
 Rotate(HumanBodyBones.LeftUpperLeg,-left*22,0,-1.5f);
 Rotate(HumanBodyBones.RightUpperLeg,-right*22,0,1.5f);
 float leftKnee=4+Mathf.Max(0,left)*21,rightKnee=4+Mathf.Max(0,right)*21;
 Rotate(HumanBodyBones.LeftLowerLeg,leftKnee);Rotate(HumanBodyBones.RightLowerLeg,rightKnee);
 Rotate(HumanBodyBones.LeftFoot,left*22-leftKnee);Rotate(HumanBodyBones.RightFoot,right*22-rightKnee);
 Rotate(HumanBodyBones.Spine,4,Mathf.Sin(cycle)*1.5f,-Mathf.Sin(cycle)*1.2f);
 Rotate(HumanBodyBones.Head,5,-Mathf.Sin(cycle)*1.2f,-3);
 var hips=Bone(HumanBodyBones.Hips).position;
 Arm(true,hips+new Vector3(-.17f,-.21f,.05f-left*.045f)*unit,hips+new Vector3(-.35f,-.02f,.10f)*unit);
 Arm(false,hips+new Vector3(.17f,-.21f,.05f+left*.055f)*unit,hips+new Vector3(.35f,-.02f,.10f)*unit);
 Fingers(true,Vector3.down);Fingers(false,Vector3.down);
 }
 else if(mode=="Eating"){
 float cycle=time*Mathf.PI*2;float lift=Mathf.Pow(.5f-.5f*Mathf.Cos(cycle),.8f);
 Rotate(HumanBodyBones.Spine,8+Mathf.Sin(cycle)*1.5f);Rotate(HumanBodyBones.Chest,8+Mathf.Sin(cycle)*2);Rotate(HumanBodyBones.Head,12+lift*9,0,-3);
 var c=Bone(HumanBodyBones.Chest).position;var h=Bone(HumanBodyBones.Head).position;var bowl=c+new Vector3(-.025f,-.015f,.24f)*unit;
 Arm(true,bowl,c+new Vector3(-.4f,-.15f,.10f)*unit);Fingers(true,new Vector3(.11f,.02f,.10f));
 var mouth=h+new Vector3(.015f,-.055f,.125f)*unit;Arm(false,Vector3.Lerp(bowl+new Vector3(.025f,.025f,.015f)*unit,mouth,lift),c+new Vector3(.35f,-.08f,.18f)*unit);Fingers(false,new Vector3(-.09f,.04f,.06f));
 }
 else{
 // Mildly dejected, with a quiet four-second breath and open space beside the clothes.
 Rotate(HumanBodyBones.Spine,6+breath*.8f);Rotate(HumanBodyBones.Chest,4+breath*.6f);
 Rotate(HumanBodyBones.Neck,3);Rotate(HumanBodyBones.Head,12+breath*.5f,0,-3);
 Rotate(HumanBodyBones.LeftShoulder,0,0,3);Rotate(HumanBodyBones.RightShoulder,0,0,-3);
 var hips=Bone(HumanBodyBones.Hips).position;
 Arm(true,hips+new Vector3(-.27f,-.30f,.035f)*unit,hips+new Vector3(-.42f,-.02f,.08f)*unit);
 Arm(false,hips+new Vector3(.27f,-.30f,.035f)*unit,hips+new Vector3(.42f,-.02f,.08f)*unit);
 Fingers(true,new Vector3(-.008f,-.13f,.01f));Fingers(false,new Vector3(.008f,-.13f,.01f));
 }
 }
 static AnimationClip CreateClip(GameObject source,string mode){
 var go=UnityEngine.Object.Instantiate(source);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;actor=go.GetComponent<Animator>();actor.Rebind();actor.Update(.0001f);unit=(Bone(HumanBodyBones.Head).position.y-Bone(HumanBodyBones.LeftFoot).position.y)/1.45f;
 var transforms=go.GetComponentsInChildren<Transform>();var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var handler=new HumanPoseHandler(actor.avatar,go.transform);var pose=new HumanPose();
 var names=HumanTrait.MuscleName;var curves=Enumerable.Range(0,names.Length+7).Select(i=>new AnimationCurve()).ToArray();
 for(int frame=0;frame<=120;frame++){for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];}float t=frame/30f;Pose(mode,t);handler.GetHumanPose(ref pose);for(int i=0;i<names.Length;i++){float v=pose.muscles[i];if(mode!="Walking"&&names[i].Contains("Stretched"))v=mode=="Eating"?-.15f:.15f;if(names[i].Contains("Spread"))v=0;curves[i].AddKey(t,v);}float[] root={pose.bodyPosition.x,pose.bodyPosition.y,pose.bodyPosition.z,pose.bodyRotation.x,pose.bodyRotation.y,pose.bodyRotation.z,pose.bodyRotation.w};for(int i=0;i<7;i++)curves[names.Length+i].AddKey(t,root[i]);}
 string path=Folder+"/Child"+mode+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.ClearCurves();clip.name="Child"+mode;clip.frameRate=30;for(int i=0;i<names.Length;i++)clip.SetCurve("",typeof(Animator),names[i],curves[i]);string[] keys={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};for(int i=0;i<7;i++)clip.SetCurve("",typeof(Animator),keys[i],curves[names.Length+i]);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.loopBlend=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);handler.Dispose();UnityEngine.Object.DestroyImmediate(go);return clip;
 }
 static Material Mat(string name,Color color){string p=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(p);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,p);}mat.color=color;mat.SetFloat("_Glossiness",0);EditorUtility.SetDirty(mat);return mat;}
 static GameObject Food(Animator a){
 var anchor=new GameObject("Food held in left palm");var hand=a.GetBoneTransform(HumanBodyBones.LeftHand);anchor.transform.position=hand.position+Vector3.up*.02f*unit;anchor.transform.rotation=Quaternion.identity;anchor.transform.SetParent(hand,true);
 var bowl=GameObject.CreatePrimitive(PrimitiveType.Cylinder);bowl.name="Small food bowl";UnityEngine.Object.DestroyImmediate(bowl.GetComponent<Collider>());bowl.transform.SetParent(anchor.transform,false);bowl.transform.localScale=new Vector3(.18f,.022f,.18f)*unit;bowl.GetComponent<Renderer>().sharedMaterial=Mat("Bowl",new Color(.28f,.24f,.18f));
 for(int i=0;i<3;i++){var bread=GameObject.CreatePrimitive(PrimitiveType.Cube);bread.name="Food piece";UnityEngine.Object.DestroyImmediate(bread.GetComponent<Collider>());bread.transform.SetParent(anchor.transform,false);bread.transform.localPosition=new Vector3((i-1)*.035f,.027f,i%2*.025f)*unit;bread.transform.localRotation=Quaternion.Euler(5,i*28,13);bread.transform.localScale=new Vector3(.055f,.035f,.055f)*unit;bread.GetComponent<Renderer>().sharedMaterial=Mat("Food",new Color(.62f,.43f,.22f));}anchor.SetActive(false);return anchor;
 }
 [MenuItem("Recoil/Child/Rebuild child-specific walk")]
 public static string RebuildWalking(){
 if(EditorApplication.isPlaying)throw new Exception("Exit Play before changing the controller");
 var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Child.fbx");
 var clip=CreateClip(source,"Walking");
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Child.controller");
 var state=controller.layers[0].stateMachine.states.Single(s=>s.state.name=="FollowWalk").state;
 state.motion=clip;EditorUtility.SetDirty(state);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
 return "FollowWalk now uses ChildWalking (child rig, short-step 0.8-second cycle, looping, root motion off).";
 }
 public static void RebuildIdle(){CreateClip(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Child.fbx"),"Idle");AssetDatabase.SaveAssets();}
 public static void RebuildCrying(){CreateClip(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Child.fbx"),"Crying");AssetDatabase.SaveAssets();}
 static AnimationClip ImportMotion(string name,Avatar avatar){
 string path=Folder+"/MeshyAnimations/"+name+".fbx";var im=(ModelImporter)AssetImporter.GetAtPath(path);im.animationType=ModelImporterAnimationType.Human;im.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;im.sourceAvatar=avatar;im.importAnimation=true;
 var clips=im.defaultClipAnimations;foreach(var clip in clips){clip.name=name;clip.loopTime=true;clip.loopPose=true;clip.lockRootPositionXZ=false;clip.lockRootHeightY=true;clip.lockRootRotation=false;clip.keepOriginalOrientation=true;clip.keepOriginalPositionY=true;clip.keepOriginalPositionXZ=true;}if(name=="ChildWalkRelaxed")foreach(var clip in clips){clip.firstFrame=45;clip.lastFrame=76;clip.loopPose=false;}if(name=="ChildJogLazy")foreach(var clip in clips){clip.firstFrame=2;clip.lastFrame=83.8f;clip.loopPose=true;}im.clipAnimations=clips;im.SaveAndReimport();
 if(name=="ChildWalkRelaxed"){var clean=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/ChildWalkLoop.anim");if(clean)return clean;}var result=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));if(!result)throw new Exception("Missing motion "+path);return result;
 }
 public static string ApplyMeshyMotions(){
 var avatar=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Child.fbx").GetComponent<Animator>().avatar;
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Child.controller");var sm=controller.layers[0].stateMachine;
 foreach(var pair in new[]{new[]{"FollowWalk","ChildWalkRelaxed"},new[]{"FollowRun","ChildJogLazy"}}){var state=sm.states.Select(x=>x.state).FirstOrDefault(x=>x.name==pair[0]);if(state==null)state=sm.AddState(pair[0]);state.motion=ImportMotion(pair[1],avatar);EditorUtility.SetDirty(state);}AssetDatabase.SaveAssets();return "Meshy walk/run assigned";
 }
 public static void BuildAssets(){
 Directory.CreateDirectory(Folder);AssetDatabase.ImportAsset(Folder+"/Child.fbx",ImportAssetOptions.ForceUpdate);var importer=(ModelImporter)AssetImporter.GetAtPath(Folder+"/Child.fbx");importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.importAnimation=false;importer.isReadable=true;importer.SaveAndReimport();var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Child.fbx");var avatar=source.GetComponent<Animator>().avatar;if(!avatar||!avatar.isHuman||!avatar.isValid)throw new Exception("Child Humanoid avatar invalid");
 var crying=CreateClip(source,"Crying");var eating=CreateClip(source,"Eating");var idle=CreateClip(source,"Idle");var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Child.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Child.controller");var sm=controller.layers[0].stateMachine;foreach(var state in sm.states)sm.RemoveState(state.state);var cry=sm.AddState("Crying");cry.motion=crying;sm.defaultState=cry;sm.AddState("Eating").motion=eating;sm.AddState("FollowIdle").motion=idle;sm.AddState("FollowWalk").motion=ImportMotion("ChildWalkRelaxed",avatar);sm.AddState("FollowRun").motion=ImportMotion("ChildJogLazy",avatar);
 var material=Mat("Child",Color.white);material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Child_BaseColor.png");
 var go=UnityEngine.Object.Instantiate(source);go.name="Warehouse child - Meshy";actor=go.GetComponent<Animator>();actor.runtimeAnimatorController=controller;actor.applyRootMotion=false;actor.cullingMode=AnimatorCullingMode.AlwaysAnimate;actor.Rebind();actor.Update(.0001f);unit=(Bone(HumanBodyBones.Head).position.y-Bone(HumanBodyBones.LeftFoot).position.y)/1.45f;foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>()){skin.sharedMaterial=material;skin.updateWhenOffscreen=true;skin.localBounds=new Bounds(Vector3.up,new Vector3(4,4,4));}
 var transforms=go.GetComponentsInChildren<Transform>();var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();actor.Play("Eating",0,0);actor.Update(.0001f);var food=Food(actor);for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];}actor.Play("Crying",0,0);actor.Update(.0001f);var driver=go.AddComponent<WarehouseChildAnimation>();driver.animator=actor;driver.food=food;driver.restAction=WarehouseChildAnimation.RestAction.Crying;var contact=go.AddComponent<WarehouseChildHandContact>();contact.childAnimator=actor;PrefabUtility.SaveAsPrefabAsset(go,Folder+"/WarehouseChild.prefab");UnityEngine.Object.DestroyImmediate(go);AssetDatabase.SaveAssets();
 }
 public static void InstallOnChild(EscortChild child){
 string before=EditorJsonUtility.ToJson(child);var visual=child.GetComponent<Recoil3DActorVisual>();if(visual){visual.enabled=false;if(visual.model)visual.model.gameObject.SetActive(false);}foreach(var sprite in child.GetComponentsInChildren<SpriteRenderer>(true))sprite.forceRenderingOff=true;var old=child.transform.Find("Warehouse child - Meshy");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/WarehouseChild.prefab");var model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,child.gameObject.scene);model.transform.SetParent(child.transform,false);model.transform.localRotation=Quaternion.Euler(0,180,0);var a=model.GetComponent<Animator>();a.Rebind();a.Update(.0001f);
 var bounds=FactoryManagerMeshyIntegration.SkinBounds(model);model.transform.localScale*=child.bodyHeight/bounds.size.y;bounds=FactoryManagerMeshyIntegration.SkinBounds(model);model.transform.position+=Vector3.up*(child.transform.position.y-child.bodyHeight*.5f-bounds.min.y);model.GetComponent<WarehouseChildAnimation>().escort=child;
 if(before!=EditorJsonUtility.ToJson(child))throw new Exception("Child gameplay fields changed");
 }
 public static void Install(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");BuildAssets();Directory.CreateDirectory("Backups/WarehouseChildMeshy/2026-11-06");var scene=SceneManager.GetSceneByPath(ScenePath);if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);if(!File.Exists("Backups/WarehouseChildMeshy/2026-11-06/WarehouseStoreroom.unity"))EditorSceneManager.SaveScene(scene,"Backups/WarehouseChildMeshy/2026-11-06/WarehouseStoreroom.unity",true);var child=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<EscortChild>(true)).Single();InstallOnChild(child);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
 string path="Assets/RecoilPrototype/Resources/Recoil3DEscortChild.prefab";if(!File.Exists("Backups/WarehouseChildMeshy/2026-11-06/Recoil3DEscortChild.prefab"))File.Copy(path,"Backups/WarehouseChildMeshy/2026-11-06/Recoil3DEscortChild.prefab",true);var root=PrefabUtility.LoadPrefabContents(path);try{InstallOnChild(root.GetComponent<EscortChild>());PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();
 }
}
