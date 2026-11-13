using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class WarehouseChildVerification
{
 static Vector3[] WorldVertices(SkinnedMeshRenderer r){var m=r.sharedMesh;var vs=m.vertices;var weights=m.boneWeights;var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*m.bindposes[i]).ToArray();var result=new Vector3[vs.Length];for(int i=0;i<vs.Length;i++){var w=weights[i];var v=vs[i];result[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}return result;}
 static void Capture(GameObject model,string path){
 var scene=EditorSceneManager.NewPreviewScene();var skin=model.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=WorldVertices(skin);mesh.RecalculateBounds();mesh.RecalculateNormals();var proxy=new GameObject("Posed child");SceneManager.MoveGameObjectToScene(proxy,scene);proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;
 foreach(var r in model.GetComponentsInChildren<MeshRenderer>()){var o=UnityEngine.Object.Instantiate(r.gameObject);SceneManager.MoveGameObjectToScene(o,scene);o.transform.position=r.transform.position;o.transform.rotation=r.transform.rotation;o.transform.localScale=r.transform.lossyScale;}
 var bounds=mesh.bounds;var obj=new GameObject("Verification camera");SceneManager.MoveGameObjectToScene(obj,scene);var camera=obj.AddComponent<Camera>();camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.orthographic=true;camera.orthographicSize=bounds.size.y*.64f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.19f,.19f,.18f);camera.transform.position=bounds.center+new Vector3(-.4f,.12f,-3);camera.transform.LookAt(bounds.center);
 foreach(var p in new[]{new Vector3(-2,3,-4),new Vector3(2,2,-2)}){var lightObj=new GameObject("Softbox");SceneManager.MoveGameObjectToScene(lightObj,scene);var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=p.x<0?.85f:.4f;light.transform.rotation=Quaternion.LookRotation(-p);}
 var rt=new RenderTexture(1000,1100,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1000,1100,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1000,1100),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(mesh);
 }
 public static string Verify(){
 const string folder="Assets/Art/Characters/WarehouseChildMeshy";Directory.CreateDirectory("Captures/WarehouseChild");var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/WarehouseChild.prefab"));go.transform.rotation=Quaternion.Euler(0,180,0);var a=go.GetComponent<Animator>();var driver=go.GetComponent<WarehouseChildAnimation>();driver.manuallyDriven=true;
 try{
 if(!a.avatar.isHuman||!a.avatar.isValid)throw new Exception("Invalid avatar");float eatRange=0,minMouth=100,cryRange=0;Vector3 firstHand=Vector3.zero;Quaternion firstHead=Quaternion.identity;float minFoot=100,maxFoot=-100;
 foreach(string state in new[]{"Crying","Eating"}){
 driver.food.SetActive(state=="Eating");a.Rebind();a.Play(state,0,0);a.Update(.0001f);var root=go.transform.position;
 for(int k=0;k<=120;k++){a.Play(state,0,k/120f);a.Update(.0001f);var hand=a.GetBoneTransform(HumanBodyBones.RightHand).position;var head=a.GetBoneTransform(HumanBodyBones.Head);if(k==0){firstHand=hand;firstHead=head.rotation;}if(state=="Eating"){eatRange=Mathf.Max(eatRange,Vector3.Distance(firstHand,hand));minMouth=Mathf.Min(minMouth,Vector3.Distance(hand,head.position));}else cryRange=Mathf.Max(cryRange,Quaternion.Angle(firstHead,head.rotation));var foot=a.GetBoneTransform(HumanBodyBones.LeftFoot).position.y;minFoot=Mathf.Min(minFoot,foot);maxFoot=Mathf.Max(maxFoot,foot);if((go.transform.position-root).magnitude>.001f)throw new Exception("Root drift");if(k==15||k==45)Capture(go,"Captures/WarehouseChild/"+state+"_"+k+".png");}
 }
 if(eatRange<.1f||minMouth>.2f||cryRange<2)throw new Exception("Animation too weak: eat="+eatRange+" mouth="+minMouth+" cry="+cryRange);
 driver.StartEating();driver.Advance(.02f);a.Update(.3f);if(driver.CurrentAction!="Eating"||!driver.food.activeSelf)throw new Exception("Eating switch failed");driver.StopEating();driver.Advance(.02f);a.Update(.3f);if(driver.CurrentAction!="Crying"||driver.food.activeSelf)throw new Exception("Cry return failed");
 string report="PASS\nCharacter triangles="+go.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.triangles.Length/3+"\nHumanoid avatar valid; Crying default\nEating hand travel="+eatRange+"; closest hand to head="+minMouth+"\nCrying head excursion="+cryRange+" degrees\nFoot bone height variation across both clips="+(maxFoot-minFoot)+"\nStartEating/StopEating and food visibility pass; loops no root drift\n";File.WriteAllText("Captures/WarehouseChild/Verification.txt",report);return report;
 }finally{UnityEngine.Object.DestroyImmediate(go);}
 }
}
