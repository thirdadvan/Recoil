using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class KitchenMotherMeshyIntegration
{
 public const string Folder="Assets/Art/Characters/KitchenMotherMeshy";
 public const string ScenePath="Assets/Scenes/3D/RecoilKitchenMemory.unity";
 public static string Install(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
 var im=AssetImporter.GetAtPath(Folder+"/Mother.fbx") as ModelImporter;if(!im)throw new Exception("Missing Mother.fbx");
 im.animationType=ModelImporterAnimationType.Human;im.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;im.isReadable=true;im.importNormals=ModelImporterNormals.Calculate;im.normalSmoothingAngle=0;im.normalSmoothingSource=ModelImporterNormalSmoothingSource.FromAngle;im.importAnimation=false;im.SaveAndReimport();
 var source=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Mother.fbx");var temp=UnityEngine.Object.Instantiate(source);AnimationClip cook;
 try{
 var a=temp.GetComponent<Animator>();if(!a.avatar||!a.avatar.isValid||!a.avatar.isHuman)throw new Exception("Invalid humanoid");
 var h=new HumanPoseHandler(a.avatar,a.transform);var p=new HumanPose();h.GetHumanPose(ref p);var values=(float[])p.muscles.Clone();var names=HumanTrait.MuscleName;
 Action<string,float> set=(n,v)=>{int i=Array.IndexOf(names,n);if(i>=0)values[i]=v;};
 set("Spine Front-Back",.08f);set("Chest Front-Back",.05f);set("Head Nod Down-Up",-.14f);
 foreach(var side in new[]{"Left","Right"}){set(side+" Arm Down-Up",-.56f);set(side+" Arm Front-Back",.45f);set(side+" Forearm Stretch",-.5f);set(side+" Forearm Twist",.1f);foreach(var finger in new[]{"Index","Middle","Ring","Little"}){set(side+" "+finger+" 1 Stretched",.35f);set(side+" "+finger+" 2 Stretched",.2f);set(side+" "+finger+" 3 Stretched",.1f);}}
 cook=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/MotherCooking.anim");if(!cook){cook=new AnimationClip();AssetDatabase.CreateAsset(cook,Folder+"/MotherCooking.anim");}cook.ClearCurves();cook.frameRate=30;
 for(int m=0;m<names.Length;m++){var curve=new AnimationCurve();for(int i=0;i<=120;i++){float t=i/30f,v=values[m],wave=Mathf.Sin(t*Mathf.PI*.5f);if(names[m]=="Chest Front-Back")v+=wave*.012f;if(names[m]=="Right Arm Front-Back")v+=wave*.035f;if(names[m]=="Right Forearm Stretch")v+=Mathf.Cos(t*Mathf.PI*.5f)*.025f;curve.AddKey(t,v);}cook.SetCurve("",typeof(Animator),names[m],curve);}
 string[] rootNames={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};float[] root={0,p.bodyPosition.y,0,p.bodyRotation.x,p.bodyRotation.y,p.bodyRotation.z,p.bodyRotation.w};for(int i=0;i<7;i++)cook.SetCurve("",typeof(Animator),rootNames[i],AnimationCurve.Constant(0,4,root[i]));var cs=AnimationUtility.GetAnimationClipSettings(cook);cs.loopTime=true;cs.loopBlend=true;cs.keepOriginalPositionXZ=true;cs.keepOriginalPositionY=true;cs.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(cook,cs);h.Dispose();EditorUtility.SetDirty(cook);
 }finally{UnityEngine.Object.DestroyImmediate(temp);}
 var walkPath=Folder+"/MotherWalk.fbx";var wi=AssetImporter.GetAtPath(walkPath) as ModelImporter;if(wi){wi.animationType=ModelImporterAnimationType.Human;wi.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;wi.sourceAvatar=source.GetComponent<Animator>().avatar;wi.importAnimation=true;var clips=wi.defaultClipAnimations;foreach(var c in clips){c.name="MotherWalk";c.loopTime=true;c.loopPose=true;c.lockRootPositionXZ=false;c.lockRootRotation=false;c.lockRootHeightY=true;c.keepOriginalPositionY=true;c.keepOriginalPositionXZ=true;c.keepOriginalOrientation=true;}wi.clipAnimations=clips;wi.SaveAndReimport();}
 var walk=wi?AssetDatabase.LoadAllAssetsAtPath(walkPath).OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview")):null;if(!walk)throw new Exception("Missing native walking clip");
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Mother.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/Mother.controller");var sm=controller.layers[0].stateMachine;foreach(var pair in new[]{new {name="Cook",clip=cook},new{name="Walk",clip=walk}}){var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name==pair.name);if(!state)state=sm.AddState(pair.name);state.motion=pair.clip;if(pair.name=="Cook")sm.defaultState=state;EditorUtility.SetDirty(state);}
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Mother.mat");if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Folder+"/Mother.mat");}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Mother_BaseColor.png");mat.color=Color.white;mat.SetFloat("_Glossiness",0);EditorUtility.SetDirty(mat);
 var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);Directory.CreateDirectory("Backups/KitchenMotherMeshy/2026-11-08");if(!File.Exists("Backups/KitchenMotherMeshy/2026-11-08/KitchenMemory.unity"))EditorSceneManager.SaveScene(s,"Backups/KitchenMotherMeshy/2026-11-08/KitchenMemory.unity",true);
 var story=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KitchenMemoryController>(true)).Single();var mother=story.mother;var interaction=mother.GetComponent<KitchenMotherConversation>();var before=EditorJsonUtility.ToJson(interaction);var position=mother.position;
 foreach(Transform t in mother)if(t.name!="Meshy mother - lowpoly")t.gameObject.SetActive(false);var old=mother.Find("Meshy mother - lowpoly");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var model=(GameObject)PrefabUtility.InstantiatePrefab(source,s);model.name="Meshy mother - lowpoly";model.transform.SetParent(mother,false);model.transform.localRotation=Quaternion.identity;model.transform.localPosition=new Vector3(0,0,-.3f);var animator=model.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
 foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){r.sharedMaterial=mat;r.updateWhenOffscreen=true;r.localBounds=new Bounds(Vector3.up,new Vector3(4,4,4));}
 animator.Rebind();animator.Update(0);var bounds=FactoryManagerMeshyIntegration.SkinBounds(model);model.transform.localScale*=1.65f/bounds.size.y;bounds=FactoryManagerMeshyIntegration.SkinBounds(model);model.transform.position+=Vector3.up*(mother.position.y-.8f+.015f-bounds.min.y);
 var motion=model.AddComponent<KitchenMotherMeshyAnimation>();motion.animator=animator;motion.story=story;var prefab=UnityEngine.Object.Instantiate(model);prefab.GetComponent<KitchenMotherMeshyAnimation>().story=null;PrefabUtility.SaveAsPrefabAsset(prefab,Folder+"/Mother.prefab");UnityEngine.Object.DestroyImmediate(prefab);
 if(before!=EditorJsonUtility.ToJson(interaction)||position!=mother.position)throw new Exception("Interaction changed");EditorUtility.SetDirty(controller);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
 return "Installed mother, "+model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3)+" triangles. Original conversation and memory story retained.";
 }
}
