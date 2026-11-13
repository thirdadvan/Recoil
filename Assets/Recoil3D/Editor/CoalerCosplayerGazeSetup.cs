using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Animations.Rigging;
public static class CoalerCosplayerGazeSetup
{
 static MultiAimConstraintData.Axis ClosestAxis(Transform bone,Vector3 direction){Vector3 local=bone.InverseTransformDirection(direction);float x=Mathf.Abs(local.x),y=Mathf.Abs(local.y),z=Mathf.Abs(local.z);if(x>y&&x>z)return local.x>0?MultiAimConstraintData.Axis.X:MultiAimConstraintData.Axis.X_NEG;if(y>z)return local.y>0?MultiAimConstraintData.Axis.Y:MultiAimConstraintData.Axis.Y_NEG;return local.z>0?MultiAimConstraintData.Axis.Z:MultiAimConstraintData.Axis.Z_NEG;}
 static void Aim(string name,Transform parent,Transform bone,Transform target,Transform root,float weight){var go=new GameObject(name);go.transform.SetParent(parent,false);var c=go.AddComponent<MultiAimConstraint>();var d=c.data;d.constrainedObject=bone;d.aimAxis=ClosestAxis(bone,root.forward);d.upAxis=ClosestAxis(bone,Vector3.up);d.worldUpType=MultiAimConstraintData.WorldUpType.SceneUp;d.maintainOffset=false;d.limits=new Vector2(-100,100);var sources=new WeightedTransformArray();sources.Add(new WeightedTransform(target,1));d.sourceObjects=sources;c.data=d;c.weight=weight;}
 public static void Attach(GameObject go){
 var animator=go.GetComponent<Animator>();var builder=go.GetComponent<RigBuilder>();if(!builder)builder=go.AddComponent<RigBuilder>();builder.Clear();builder.layers.Clear();var old=go.transform.Find("Player gaze rig");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 var root=new GameObject("Player gaze rig");root.transform.SetParent(go.transform,false);var rig=root.AddComponent<Rig>();rig.weight=0;
 var target=new GameObject("Player head target").transform;target.SetParent(root.transform,false);var head=animator.GetBoneTransform(HumanBodyBones.Head);target.position=head.position+go.transform.forward*2;
 Aim("Neck - gentle player aim",root.transform,animator.GetBoneTransform(HumanBodyBones.Neck),target,go.transform,.3f);
 Aim("Head - player aim",root.transform,head,target,go.transform,1f);
 builder.layers.Add(new RigLayer(rig));var driver=go.GetComponent<CoalerCosplayerLookAt>();if(!driver)driver=go.AddComponent<CoalerCosplayerLookAt>();driver.lookRig=rig;driver.lookTarget=target;driver.head=head;driver.distanceOrigin=animator.GetBoneTransform(HumanBodyBones.Hips);driver.activationDistance=3;driver.raiseTime=.5f;driver.lowerTime=.8f;driver.useGameplayPlane=true;EditorUtility.SetDirty(builder);EditorUtility.SetDirty(driver);
 }
 public static void Install(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");const string path="Assets/Scenes/3D/RecoilFactoryInterior.unity";var s=SceneManager.GetSceneByPath(path);if(!s.isLoaded)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);Directory.CreateDirectory("Backups/CoalerCosplayerSeated/2026-11-06/GazeRig");EditorSceneManager.SaveScene(s,"Backups/CoalerCosplayerSeated/2026-11-06/GazeRig/FactoryInterior.unity",true);
 CoalerCosplayerBenchPlacement.Apply();var go=GameObject.Find("Coaler cosplayer - resting on F2 bench");Attach(go);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);Selection.activeGameObject=go;
 }
}
