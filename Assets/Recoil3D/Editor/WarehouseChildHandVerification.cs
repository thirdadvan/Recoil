using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class WarehouseChildHandVerification
{
 public static string Verify(){
 var p=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Dawn/Dawn.fbx"));
 var c=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/WarehouseChildMeshy/WarehouseChild.prefab"));
 try{
 var pa=p.GetComponent<Animator>();var ca=c.GetComponent<Animator>();
 pa.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Characters/Dawn/Dawn.controller");
 foreach(var r in p.GetComponentsInChildren<SkinnedMeshRenderer>())r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Characters/Dawn/Dawn.mat");
 c.GetComponent<WarehouseChildAnimation>().manuallyDriven=true;
 var rig=c.GetComponent<WarehouseChildHandContact>();if(!rig)rig=c.AddComponent<WarehouseChildHandContact>();rig.childAnimator=ca;
 float maxGap=0,maxFootDrift=0;int samples=0;
 foreach(string motion in new[]{"FollowWalk","FollowRun"})foreach(int facing in new[]{-1,1})for(int frame=0;frame<24;frame++){
 p.transform.position=Vector3.zero;p.transform.rotation=Quaternion.Euler(0,facing*90,0);
 c.transform.position=new Vector3(-facing*.65f,0,0);c.transform.rotation=p.transform.rotation;c.transform.localScale=Vector3.one*.77f;
 pa.Rebind();pa.SetLayerWeight(1,0);pa.SetFloat("Speed",1);pa.Play("Locomotion",0,frame/24f);pa.Update(.0001f);
 ca.Rebind();ca.Play(motion,0,frame/24f);ca.Update(.0001f);
 var foot=ca.GetBoneTransform(HumanBodyBones.LeftFoot).position;rig.ApplyContact(pa,true,1);
 maxGap=Mathf.Max(maxGap,rig.PalmGap);maxFootDrift=Mathf.Max(maxFootDrift,Vector3.Distance(foot,ca.GetBoneTransform(HumanBodyBones.LeftFoot).position));samples++;
 if(motion=="FollowWalk"&&facing==1&&frame==8)Capture(p,c);
 }
 rig.ApplyContact(pa,false,1);if(rig.Weight!=0)throw new Exception("Release failed");
 if(maxGap>.06f||maxFootDrift>.001f)throw new Exception("Contact validation failed gap="+maxGap+" feet="+maxFootDrift);
 var report="PASS: "+samples+" samples, both travel directions; maximum palm gap="+maxGap+" m; foot displacement="+maxFootDrift+" m; release weight=0.\n";
 Directory.CreateDirectory("Captures/WarehouseChild");File.WriteAllText("Captures/WarehouseChild/HandContactVerification.txt",report);return report;
 }finally{UnityEngine.Object.DestroyImmediate(p);UnityEngine.Object.DestroyImmediate(c);}}
 static void Capture(GameObject player,GameObject child){
 var s=EditorSceneManager.NewPreviewScene();var bounds=new Bounds();bool first=true;
 foreach(var model in new[]{player,child})foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
 var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);var vs=mesh.vertices;var weights=mesh.boneWeights;var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();
 for(int i=0;i<vs.Length;i++){var w=weights[i];var v=vs[i];vs[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}
 mesh.vertices=vs;mesh.RecalculateBounds();mesh.RecalculateNormals();var proxy=new GameObject("Posed preview");SceneManager.MoveGameObjectToScene(proxy,s);proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;if(first){bounds=mesh.bounds;first=false;}else bounds.Encapsulate(mesh.bounds);
 }
 var co=new GameObject("Preview camera");SceneManager.MoveGameObjectToScene(co,s);var cam=co.AddComponent<Camera>();cam.scene=s;cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(s);cam.orthographic=true;cam.orthographicSize=bounds.size.y*.7f;cam.backgroundColor=new Color(.2f,.2f,.19f);cam.clearFlags=CameraClearFlags.SolidColor;cam.transform.position=bounds.center+new Vector3(1,.15f,-4);cam.transform.LookAt(bounds.center);
 var lo=new GameObject("Preview light");SceneManager.MoveGameObjectToScene(lo,s);var light=lo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;lo.transform.rotation=Quaternion.Euler(30,-25,0);
 var rt=new RenderTexture(1200,1000,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var im=new Texture2D(1200,1000,TextureFormat.RGB24,false);im.ReadPixels(new Rect(0,0,1200,1000),0,0);im.Apply();Directory.CreateDirectory("Captures/WarehouseChild");File.WriteAllBytes("Captures/WarehouseChild/HandContact.png",im.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(im);UnityEngine.Object.DestroyImmediate(rt);foreach(var f in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshFilter>()))UnityEngine.Object.DestroyImmediate(f.sharedMesh);EditorSceneManager.ClosePreviewScene(s);
 }
}
