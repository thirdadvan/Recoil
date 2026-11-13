using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
public static class WarehouseChildJogLoopRepair
{
 const string F="Assets/Art/Characters/WarehouseChildMeshy/";
 public static string Run(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
 Directory.CreateDirectory("Backups/ChildJogLoop_2026-11-07");foreach(var p in new[]{F+"Child.controller",F+"MeshyAnimations/ChildJogLazy.fbx.meta"}){var b="Backups/ChildJogLoop_2026-11-07/"+Path.GetFileName(p);if(!File.Exists(b))File.Copy(p,b);}
 var source=AssetDatabase.LoadAllAssetsAtPath(F+"MeshyAnimations/ChildJogLazy.fbx").OfType<AnimationClip>().First(c=>!c.name.StartsWith("__preview"));
 var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(F+"Child.fbx"));var a=go.GetComponent<Animator>();var handler=new HumanPoseHandler(a.avatar,a.transform);
 try{
 Func<float,float[]> sample=t=>{source.SampleAnimation(go,t/30f);var p=new HumanPose();handler.GetHumanPose(ref p);return p.muscles.Concat(new[]{p.bodyPosition.y}).ToArray();};
 float best=float.MaxValue,start=0,period=0;
 for(int s=20;s<=55;s++)for(int d=198;d<=214;d++){float dur=d/10f;var p=sample(s);var q=sample(s+dur);var pm=sample(s-.25f);var pp=sample(s+.25f);var qm=sample(s+dur-.25f);var qp=sample(s+dur+.25f);float score=0;for(int m=0;m<p.Length;m++){float diff=p[m]-q[m];float vel=(pp[m]-pm[m])-(qp[m]-qm[m]);score+=diff*diff+vel*vel*.25f;}if(score<best){best=score;start=s;period=dur;}}
 int count=60;float length=period/30f;var values=Enumerable.Range(0,count+1).Select(i=>sample(start+period*i/count)).ToArray();int channels=values[0].Length;var result=new AnimationClip{name="ChildJogLoop",frameRate=60};
 for(int m=0;m<channels;m++){float drift=values[count][m]-values[0][m];var v=Enumerable.Range(0,count+1).Select(i=>values[i][m]-drift*i/count).ToArray();v[count]=v[0];var keys=new Keyframe[count+1];float dt=length/count;for(int i=0;i<=count;i++){int prev=i==0?count-1:i-1;int next=i==count?1:i+1;float tangent=(v[next]-v[prev])/(2*dt);keys[i]=new Keyframe(i*dt,v[i],tangent,tangent);}result.SetCurve("",typeof(Animator),m<HumanTrait.MuscleCount?HumanTrait.MuscleName[m]:"RootT.y",new AnimationCurve(keys));}
 foreach(var name in new[]{"RootT.x","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"})result.SetCurve("",typeof(Animator),name,AnimationCurve.Constant(0,length,name=="RootQ.w"?1:0));
 var settings=AnimationUtility.GetAnimationClipSettings(result);settings.loopTime=true;settings.loopBlend=false;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(result,settings);
 var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(F+"ChildJogLoop.anim");if(existing){EditorUtility.CopySerialized(result,existing);UnityEngine.Object.DestroyImmediate(result);result=existing;}else AssetDatabase.CreateAsset(result,F+"ChildJogLoop.anim");
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(F+"Child.controller");controller.layers[0].stateMachine.states.First(s=>s.state.name=="FollowRun").state.motion=result;EditorUtility.SetDirty(controller);EditorUtility.SetDirty(result);AssetDatabase.SaveAssets();
 // Sample the installed clip around its seam; compare bone positions and curve velocities.
 result.SampleAnimation(go,0);var bones=new[]{HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftHand,HumanBodyBones.RightHand,HumanBodyBones.Head};var initial=bones.Select(b=>a.GetBoneTransform(b).position).ToArray();result.SampleAnimation(go,length);float seam=bones.Select((b,i)=>Vector3.Distance(initial[i],a.GetBoneTransform(b).position)).Max();float tangentError=0;foreach(var binding in AnimationUtility.GetCurveBindings(result)){var c=AnimationUtility.GetEditorCurve(result,binding);tangentError=Mathf.Max(tangentError,Mathf.Abs(c.keys[0].outTangent-c.keys[c.length-1].inTangent));}
 string report="Jog loop repaired\nSource frames "+start+".."+(start+period)+"; duration "+length+" sec\nMaximum foot/hand/head seam distance "+seam+" m\nEndpoint tangent mismatch "+tangentError+"\nRoot XZ fixed at zero and rotation identity. FollowRun assigned ChildJogLoop.anim.\n";Directory.CreateDirectory("Captures/WarehouseChild");File.WriteAllText("Captures/WarehouseChild/JogLoopVerification.txt",report);if(seam>.001f||tangentError>.001f)throw new Exception(report);return report;
 }finally{handler.Dispose();UnityEngine.Object.DestroyImmediate(go);}
 }
}
