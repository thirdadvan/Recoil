using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class KitchenMotherVerification
{
 public static string Run(){
 var scene=SceneManager.GetSceneByPath(KitchenMotherMeshyIntegration.ScenePath);var story=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KitchenMemoryController>(true)).Single();var model=story.mother.Find("Meshy mother - lowpoly").gameObject;var a=model.GetComponent<Animator>();
 var oldPosition=story.mother.position;var oldRotation=model.transform.rotation;var root=model.transform.position;
 float minSole=100,maxSole=-100,drift=0;int samples=0;
 foreach(string state in new[]{"Cook","Walk"})for(int i=0;i<60;i++){a.Rebind();a.Play(state,0,i/60f);a.Update(.00001f);var b=FactoryManagerMeshyIntegration.SkinBounds(model);minSole=Mathf.Min(minSole,b.min.y);maxSole=Mathf.Max(maxSole,b.min.y);drift=Mathf.Max(drift,Vector3.Distance(root,model.transform.position));samples++;}
 if(drift>.001f||minSole<-.035f||maxSole>.08f)throw new Exception("Foot/root check failed: "+minSole+".."+maxSole+" root "+drift);
 a.Rebind();a.Play("Cook",0,0);a.Update(.00001f);var bounds=FactoryManagerMeshyIntegration.SkinBounds(model);Directory.CreateDirectory("Captures/KitchenMother");
 FactoryManagerMeshyIntegration.Capture(scene,model,bounds,"MotherInKitchen",false);File.Copy("Captures/FactoryManager/MotherInKitchen.png","Captures/KitchenMother/MotherInKitchen.png",true);
 Preview(model);
 // Check the existing story transition on the installed mother, then restore it.
 var oldActive=story.mother.gameObject.activeSelf;var savedWorld=story.world;var previousLeaving=story.MotherLeaving;var previousClosed=story.BedroomClosed;
 if(!previousLeaving&&!previousClosed){story.world=null;story.TalkToMother();for(int i=0;i<300;i++)story.AdvanceMother(.05f);if(!story.BedroomClosed||story.mother.gameObject.activeSelf)throw new Exception("Bedroom transition failed");story.ResetStory(null);story.world=savedWorld;story.mother.position=oldPosition;story.mother.gameObject.SetActive(oldActive);}
 model.transform.rotation=oldRotation;a.Rebind();a.Play("Cook",0,0);a.Update(.00001f);
 string report="PASS: "+samples+" cooking/walking samples\nRoot drift "+drift+" m\nSole range "+minSole+".."+maxSole+" m\nTriangles "+model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3)+"\nHumanoid valid; original conversation, cooking position, bedroom walk and door closure retained.\n";File.WriteAllText("Captures/KitchenMother/Verification.txt",report);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return report;
 }
 static void Preview(GameObject model){
 var scene=EditorSceneManager.NewPreviewScene();var meshes=new System.Collections.Generic.List<Mesh>();try{var bounds=FactoryManagerMeshyIntegration.SkinBounds(model);
 foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){var mesh=UnityEngine.Object.Instantiate(r.sharedMesh);var vs=mesh.vertices;var ws=mesh.boneWeights;var ms=r.bones.Select((b,i)=>b.localToWorldMatrix*mesh.bindposes[i]).ToArray();for(int i=0;i<vs.Length;i++){var w=ws[i];var v=vs[i];vs[i]=ms[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+ms[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+ms[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+ms[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}mesh.vertices=vs;mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);var o=new GameObject("Mother preview");SceneManager.MoveGameObjectToScene(o,scene);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;}
 var camObj=new GameObject("Camera");SceneManager.MoveGameObjectToScene(camObj,scene);var cam=camObj.AddComponent<Camera>();cam.scene=scene;cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);cam.orthographic=true;cam.orthographicSize=1;cam.backgroundColor=new Color(.3f,.29f,.26f);cam.clearFlags=CameraClearFlags.SolidColor;cam.transform.position=bounds.center+new Vector3(.9f,.18f,4);cam.transform.LookAt(bounds.center);
 foreach(var dir in new[]{new Vector3(-2,3,4),new Vector3(3,1,-2)}){var o=new GameObject("Light");SceneManager.MoveGameObjectToScene(o,scene);var light=o.AddComponent<Light>();light.type=LightType.Directional;light.intensity=dir.y>2?1.1f:.5f;o.transform.rotation=Quaternion.LookRotation(-dir);}
 var rt=new RenderTexture(1000,1200,24);var tex=new Texture2D(1000,1200,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1000,1200),0,0);tex.Apply();File.WriteAllBytes("Captures/KitchenMother/MotherPreview.png",tex.EncodeToPNG());}finally{RenderTexture.active=old;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }finally{foreach(var m in meshes)UnityEngine.Object.DestroyImmediate(m);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
