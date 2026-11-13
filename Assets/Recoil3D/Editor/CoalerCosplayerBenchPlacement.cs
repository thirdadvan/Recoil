using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class CoalerCosplayerBenchPlacement
{
 const string Folder="Assets/Art/Characters/CoalerCosplayer";
 static Animator actor;
 static void Aim(HumanBodyBones bone,HumanBodyBones child,Vector3 direction){var t=actor.GetBoneTransform(bone);t.rotation=Quaternion.FromToRotation(actor.GetBoneTransform(child).position-t.position,direction)*t.rotation;}
 static Vector3[] WorldVertices(SkinnedMeshRenderer r){var m=r.sharedMesh;var vs=m.vertices;var weights=m.boneWeights;var matrices=new Matrix4x4[r.bones.Length];for(int i=0;i<matrices.Length;i++)matrices[i]=r.bones[i].localToWorldMatrix*m.bindposes[i];var result=new Vector3[vs.Length];for(int i=0;i<vs.Length;i++){var w=weights[i];var v=vs[i];result[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}return result;}
 static Mesh RearHangingCostume(MeshFilter filter,Bounds seat,Vector3 hip)
 {
 var source=filter.sharedMesh;var verts=new System.Collections.Generic.List<Vector3>();var uvs=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>[] {new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>(),new System.Collections.Generic.List<int>()};
 Action<Vector3[],int> face=(points,material)=>{int start=verts.Count;foreach(var p in points){verts.Add(filter.transform.InverseTransformPoint(p));uvs.Add(new Vector2(p.x,p.y));}for(int k=1;k<points.Length-1;k++){triangles[material].Add(start);triangles[material].Add(start+k);triangles[material].Add(start+k+1);}};
 // Keep the original raised stitches at the waist attachments.
 if(source.subMeshCount>2){var sv=source.vertices;var st=source.GetTriangles(2);for(int i=0;i<st.Length;i+=3)face(new[]{filter.transform.TransformPoint(sv[st[i]]),filter.transform.TransformPoint(sv[st[i+1]]),filter.transform.TransformPoint(sv[st[i+2]])},2);}
 foreach(float side in new[]{-1f,1f})foreach(bool rear in new[]{false,true}){
 float cx=23;float top=seat.max.y;float back=seat.max.z;
 Vector3[] path=rear?new[]{new Vector3(cx+side*.16f,hip.y-.10f,hip.z+.17f),new Vector3(cx+side*.19f,top+.15f,back+.03f),new Vector3(cx+side*.24f,top+.10f,back+.17f),new Vector3(cx+side*.29f,top-.26f,back+.20f),new Vector3(cx+side*.35f,7.84f,back+.25f)}:new[]{new Vector3(cx+side*.23f,hip.y-.02f,hip.z-.02f),new Vector3(cx+side*.28f,top+.15f,back-.12f),new Vector3(cx+side*.34f,top+.12f,back+.16f),new Vector3(cx+side*.42f,top-.22f,back+.22f),new Vector3(cx+side*.53f,7.76f,back+.30f)};
 float[] radii=rear?new[]{.035f,.042f,.035f,.019f,.0024f}:new[]{.0384f,.048f,.0408f,.024f,.0024f};
 var rings=new Vector3[path.Length][];for(int j=0;j<path.Length;j++){var tangent=(path[Mathf.Min(j+1,path.Length-1)]-path[Mathf.Max(0,j-1)]).normalized;var axis=Vector3.Cross(tangent,Vector3.right).normalized;var other=Vector3.Cross(tangent,axis).normalized;rings[j]=new Vector3[6];for(int k=0;k<6;k++){float angle=k*Mathf.PI/3;rings[j][k]=path[j]+radii[j]*(axis*Mathf.Cos(angle)+other*Mathf.Sin(angle));}}
 for(int j=0;j<path.Length-1;j++)for(int k=0;k<6;k++)face(new[]{rings[j][k],rings[j][(k+1)%6],rings[j+1][(k+1)%6],rings[j+1][k]},(j+k)%7==0?0:1);
 var cap=(Vector3[])rings[0].Clone();Array.Reverse(cap);face(cap,1);face(rings[rings.Length-1],1);
 }
 var mesh=new Mesh();mesh.name="Costume_Hips_Seated";mesh.SetVertices(verts);mesh.SetUVs(0,uvs);mesh.subMeshCount=3;for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
 }
 public static void Apply(){
 var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/3D/RecoilFactoryInterior.unity"||EditorApplication.isPlaying)throw new Exception("Open factory interior in Edit mode");
 Directory.CreateDirectory("Backups/CoalerCosplayerSeated/2026-11-06");string backup="Backups/CoalerCosplayerSeated/2026-11-06/FactoryInterior.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
 var old=GameObject.Find("Coaler cosplayer - resting on F2 bench");if(old)UnityEngine.Object.DestroyImmediate(old);
 var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/CoalerCosplayer.prefab"),scene);go.name="Coaler cosplayer - resting on F2 bench";go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;actor=go.GetComponent<Animator>();actor.Rebind();actor.Update(0);
 var lf=actor.GetBoneTransform(HumanBodyBones.LeftFoot).rotation;var rf=actor.GetBoneTransform(HumanBodyBones.RightFoot).rotation;
 var neutralNeck=actor.GetBoneTransform(HumanBodyBones.Neck).rotation;var neutralHead=actor.GetBoneTransform(HumanBodyBones.Head).rotation;
 var pelvis=actor.GetBoneTransform(HumanBodyBones.Hips);pelvis.rotation=Quaternion.AngleAxis(25,Vector3.right)*pelvis.rotation;
 Aim(HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,new Vector3(-.18f,-.015f,.39f));Aim(HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,new Vector3(.18f,-.015f,.39f));Aim(HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,new Vector3(0,-.43f,.035f));Aim(HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,new Vector3(0,-.43f,.035f));actor.GetBoneTransform(HumanBodyBones.LeftFoot).rotation=lf;actor.GetBoneTransform(HumanBodyBones.RightFoot).rotation=rf;

 var spine=actor.GetBoneTransform(HumanBodyBones.Spine);spine.rotation=Quaternion.AngleAxis(15,Vector3.right)*spine.rotation;
 var chest=actor.GetBoneTransform(HumanBodyBones.Chest);chest.rotation=Quaternion.AngleAxis(10,Vector3.right)*chest.rotation;
 var neck=actor.GetBoneTransform(HumanBodyBones.Neck);neck.rotation=Quaternion.AngleAxis(32,Vector3.right)*neutralNeck;
 var head=actor.GetBoneTransform(HumanBodyBones.Head);head.rotation=Quaternion.AngleAxis(30,Vector3.right)*neutralHead;
 foreach(bool left in new[]{true,false}){
 var thigh=actor.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);var knee=actor.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
 Vector3 support=Vector3.Lerp(thigh.position,knee.position,.7f)+Vector3.up*.075f;
 var upper=left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm;var elbow=left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm;var hand=left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand;
 Aim(upper,elbow,support-actor.GetBoneTransform(upper).position);Aim(elbow,hand,new Vector3(left?.11f:-.11f,-.25f,.08f));
 }

 var handler=new HumanPoseHandler(actor.avatar,go.transform);var pose=new HumanPose();handler.GetHumanPose(ref pose);handler.Dispose();
 var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/CosplayerSeatedRest.anim");if(!clip){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,Folder+"/CosplayerSeatedRest.anim");}clip.ClearCurves();clip.frameRate=30;
 for(int i=0;i<pose.muscles.Length;i++){var n=HumanTrait.MuscleName[i];if(n.Contains("Stretched"))pose.muscles[i]=.35f;var c=new AnimationCurve();for(int k=0;k<=120;k++){float t=k/30f,v=pose.muscles[i];if(n=="Chest Front-Back")v+=Mathf.Sin(t*Mathf.PI*.5f)*.035f;if(n=="Spine Front-Back")v+=Mathf.Sin(t*Mathf.PI*.5f)*.015f;if(n=="Left Shoulder Down-Up"||n=="Right Shoulder Down-Up")v+=Mathf.Sin(t*Mathf.PI*.5f)*.012f;if(n=="Head Turn Left-Right")v+=Mathf.Sin(t*Mathf.PI*.5f)*.008f;c.AddKey(t,v);}clip.SetCurve("",typeof(Animator),n,c);}
 string[] keys={"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"};float[] vals={pose.bodyPosition.x,pose.bodyPosition.y,pose.bodyPosition.z,pose.bodyRotation.x,pose.bodyRotation.y,pose.bodyRotation.z,pose.bodyRotation.w};for(int i=0;i<keys.Length;i++)clip.SetCurve("",typeof(Animator),keys[i],AnimationCurve.Constant(0,4,vals[i]));
 var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;settings.keepOriginalOrientation=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
 var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/CosplayerSeated.controller");if(!controller)controller=AnimatorController.CreateAnimatorControllerAtPath(Folder+"/CosplayerSeated.controller");var sm=controller.layers[0].stateMachine;var state=sm.defaultState;if(!state)state=sm.AddState("Seated rest");state.motion=clip;sm.defaultState=state;
 actor.runtimeAnimatorController=controller;actor.applyRootMotion=false;actor.Rebind();actor.Update(0);go.transform.localScale=Vector3.one*1.2f;go.transform.position=new Vector3(23,7.6f,3);go.transform.rotation=Quaternion.Euler(0,180,0);
 var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();float sole=100;foreach(var v in WorldVertices(skin))sole=Mathf.Min(sole,v.y);go.transform.position+=Vector3.up*(7.615f-sole);
 var hip=actor.GetBoneTransform(HumanBodyBones.Hips).position;float contact=hip.y;foreach(var w in WorldVertices(skin)){if(Mathf.Abs(w.x-hip.x)<.24f && w.z>hip.z-.03f && w.y>hip.y-.32f && w.y<hip.y)contact=Mathf.Min(contact,w.y);}
 var bench=GameObject.Find("Bench");bench.transform.localScale=new Vector3(4.56f,.16f,1);bench.transform.position=new Vector3(23,contact+.008f-.08f,0);var visual=bench.GetComponent<Recoil3DSpriteVisual>();visual.depth=.58f;visual.depthOffset=3.12f;visual.Sync();EditorUtility.SetDirty(visual);
 var seatBounds=visual.meshRenderer.bounds;
 foreach(var filter in go.GetComponentsInChildren<MeshFilter>())if(filter.name=="Costume_Hips"){
 var seated=RearHangingCostume(filter,seatBounds,hip);string path=Folder+"/Costume_Hips_Seated.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){saved.Clear();EditorUtility.CopySerialized(seated,saved);saved.UploadMeshData(false);UnityEngine.Object.DestroyImmediate(seated);EditorUtility.SetDirty(saved);}else{AssetDatabase.CreateAsset(seated,path);saved=seated;}filter.sharedMesh=saved;
 }

 Directory.CreateDirectory("Captures/CoalerCosplayer");File.WriteAllText("Captures/CoalerCosplayer/RelaxedSeatVerification.txt","Scale 1.2; sole 7.615; seat top "+(contact+.008f)+"; hips "+hip+"; bench 4.56 x 0.16 x 0.58\n");

 CoalerCosplayerMidTentacles.Apply(go);
 CoalerCosplayerGazeSetup.Attach(go);
 EditorUtility.SetDirty(clip);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Selection.activeGameObject=go;
 }
}
