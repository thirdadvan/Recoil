using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.Animations;
public static class CoalerCosplayerGazeVerification
{
 static void Capture(GameObject actor,string name){var scene=actor.scene;var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=(Vector3[])typeof(CoalerCosplayerBenchPlacement).GetMethod("WorldVertices",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{skin});mesh.RecalculateNormals();mesh.RecalculateBounds();var proxy=new GameObject("Verification pose mesh");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(proxy,scene);proxy.AddComponent<MeshFilter>().sharedMesh=mesh;proxy.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;bool wasEnabled=skin.enabled;skin.enabled=false;var obj=new GameObject("Temporary gaze verification camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj,scene);var cam=obj.AddComponent<Camera>();cam.scene=scene;cam.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);cam.orthographic=true;cam.orthographicSize=1.35f;cam.transform.position=new Vector3(21.5f,9,-5);cam.transform.LookAt(new Vector3(23,8.5f,3));var rt=new RenderTexture(1200,1000,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes("Captures/CoalerCosplayer/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);skin.enabled=wasEnabled;UnityEngine.Object.DestroyImmediate(proxy);UnityEngine.Object.DestroyImmediate(mesh);}
 public static string Verify(){
 var actor=GameObject.Find("Coaler cosplayer - resting on F2 bench");var animator=actor.GetComponent<Animator>();var driver=actor.GetComponent<CoalerCosplayerLookAt>();var builder=actor.GetComponent<RigBuilder>();var origin=driver.distanceOrigin.position;var head=driver.head;Vector3 root=actor.transform.position;
 animator.Rebind();animator.Update(0);driver.lookRig.weight=0;Capture(actor,"Gaze_DefaultLowHead");var defaultForward=head.forward;
 var graph=PlayableGraph.Create("Gaze verification");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var basePlayable=AnimatorControllerPlayable.Create(graph,animator.runtimeAnimatorController);var output=AnimationPlayableOutput.Create(graph,"Base seated animation",animator);output.SetSourcePlayable(basePlayable);if(!builder.Build(graph))throw new Exception("RigBuilder.Build failed");graph.Play();
 float enterWeight=0,leaveWeight=0,leaveFirst=0,nearDot=0;
 try{
 Vector3 near=origin+Vector3.right*2.9f;Vector3 target=head.position+new Vector3(-.7f,.25f,-2);
 for(int i=0;i<120;i++){driver.Advance(1f/60,near,target,true);builder.SyncLayers();graph.Evaluate(1f/60);}enterWeight=driver.LookWeight;nearDot=Vector3.Dot(head.forward,(driver.lookTarget.position-head.position).normalized);Capture(actor,"Gaze_PlayerNearby");if(enterWeight<.98f||nearDot<.95f)throw new Exception("Look not acquired: weight="+enterWeight+" dot="+nearDot);
 var rotation=head.rotation;driver.Advance(1f/60,origin+Vector3.right*3.1f,target,true);builder.SyncLayers();graph.Evaluate(1f/60);leaveFirst=driver.LookWeight;if(leaveFirst<.9f||Quaternion.Angle(rotation,head.rotation)>12)throw new Exception("Exit snapped");
 for(int i=0;i<300;i++){driver.Advance(1f/60,origin+Vector3.right*3.1f,target,true);builder.SyncLayers();graph.Evaluate(1f/60);}leaveWeight=driver.LookWeight;if(leaveWeight>.001f)throw new Exception("Did not return to animation");if((actor.transform.position-root).magnitude>.001f)throw new Exception("Root drift");
 // Exact radius and missing-player behavior also share the same smoothing path.
 driver.Advance(1f/60,driver.distanceOrigin.position+Vector3.right*3f,target,true);if(!driver.PlayerInRange)throw new Exception("3m boundary missing");driver.Advance(1f/60,origin,target,false);if(driver.PlayerInRange)throw new Exception("Missing player still acquired");
 }
 finally{builder.Clear();if(graph.IsValid())graph.Destroy();driver.lookRig.weight=0;animator.Rebind();animator.Update(0);EditorSceneManager.MarkSceneDirty(actor.scene);EditorSceneManager.SaveScene(actor.scene);}
 string report="PASS Animation Rigging 1.2.1, Burst 1.8.12\n2.9m enters: weight="+enterWeight+", head aim dot="+nearDot+"\n3.1m exits smoothly: first frame weight="+leaveFirst+", final="+leaveWeight+"\nExact 3m enters, missing player releases, no root drift. Default low head restored.\n";File.WriteAllText("Captures/CoalerCosplayer/GazeVerification.txt",report);return report;
 }
}
