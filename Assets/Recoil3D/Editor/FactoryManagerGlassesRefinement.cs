using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class FactoryManagerGlassesRefinement
{
 [Serializable] class Geometry {public Vector3[] vertices;public int[] triangles;public Vector3[] originalFrameVertices;}
 const string Folder="Assets/Art/Characters/FactoryManagerMeshy";
 const string ScenePath="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity";
 public static string Install(){
 if(EditorApplication.isPlaying)throw new Exception("Edit mode required");
 var data=JsonUtility.FromJson<Geometry>(File.ReadAllText("ArtSource/FactoryManager_Glasses_2026-11-07/GlassesGeometry.json"));
 var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Manager.fbx"));
 Mesh mesh,bodyMesh=null;int removed=0;
 try{var head=source.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);
 // Blender world: X right, Z up, -Y forward. Convert to Unity's FBX bind space before attaching to head.
 var verts=data.vertices.Select(v=>head.InverseTransformPoint(new Vector3(v.x,v.z,-v.y))).ToArray();
 var flat=data.triangles.Select(i=>verts[i]).ToArray();mesh=new Mesh{name="Thick square glasses - head local"};mesh.vertices=flat;mesh.triangles=Enumerable.Range(0,flat.Length).ToArray();mesh.RecalculateNormals();mesh.RecalculateBounds();
 var existing=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/ManagerGlasses.asset");if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,Folder+"/ManagerGlasses.asset");
 var skin=source.GetComponentInChildren<SkinnedMeshRenderer>();var points=Skin(skin);var original=skin.sharedMesh;var indices=original.triangles;
 bool[] oldFrame=points.Select(p=>data.originalFrameVertices!=null&&data.originalFrameVertices.Any(q=>(p-q).sqrMagnitude<.00000009f)).ToArray();
 var kept=new System.Collections.Generic.List<int>();for(int i=0;i<indices.Length;i+=3){if(oldFrame[indices[i]]&&oldFrame[indices[i+1]]&&oldFrame[indices[i+2]]){removed++;continue;}kept.Add(indices[i]);kept.Add(indices[i+1]);kept.Add(indices[i+2]);}
 if(removed<20||removed>300)throw new Exception("Unexpected old glasses triangle count "+removed);
 bodyMesh=UnityEngine.Object.Instantiate(original);bodyMesh.name="Manager body without old thin glasses";bodyMesh.triangles=kept.ToArray();
 var oldBody=AssetDatabase.LoadAssetAtPath<Mesh>(Folder+"/Manager_NoOldGlasses.asset");if(oldBody){EditorUtility.CopySerialized(bodyMesh,oldBody);UnityEngine.Object.DestroyImmediate(bodyMesh);bodyMesh=oldBody;}else AssetDatabase.CreateAsset(bodyMesh,Folder+"/Manager_NoOldGlasses.asset");
 }finally{UnityEngine.Object.DestroyImmediate(source);}
 var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Dark brown glasses.mat");if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,Folder+"/Dark brown glasses.mat");}mat.color=new Color(0x49/255f,0x30/255f,0x20/255f);mat.SetFloat("_Glossiness",.22f);mat.SetFloat("_Metallic",0);EditorUtility.SetDirty(mat);
 var s=SceneManager.GetSceneByPath(ScenePath);if(!s.isLoaded)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
 Directory.CreateDirectory("Backups/FactoryManagerGlasses/2026-11-07");if(!File.Exists("Backups/FactoryManagerGlasses/2026-11-07/ManagerOffice.unity"))EditorSceneManager.SaveScene(s,"Backups/FactoryManagerGlasses/2026-11-07/ManagerOffice.unity",true);
 var manager=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Animator>(true)).Single(a=>a.GetComponent<FactoryManagerMannerisms>());
 var headBone=manager.GetBoneTransform(HumanBodyBones.Head);var old=headBone.Find("Deep brown square glasses");if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
 manager.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh=bodyMesh;
 var glasses=new GameObject("Deep brown square glasses");glasses.AddComponent<MeshFilter>().sharedMesh=mesh;glasses.AddComponent<MeshRenderer>().sharedMaterial=mat;glasses.transform.SetParent(headBone,false);
 PrefabUtility.SaveAsPrefabAsset(glasses,Folder+"/ManagerGlasses.prefab");EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
 Preview(manager,false);Preview(manager,true);
 return "Installed 212-triangle dark brown square rims and thicker temples; removed "+removed+" old thin-frame triangles; parented to animated head.";
 }
 static Vector3[] Skin(SkinnedMeshRenderer r){var m=r.sharedMesh;var vs=m.vertices;var weights=m.boneWeights;var matrices=r.bones.Select((b,i)=>b.localToWorldMatrix*m.bindposes[i]).ToArray();for(int i=0;i<vs.Length;i++){var w=weights[i];var v=vs[i];vs[i]=matrices[w.boneIndex0].MultiplyPoint3x4(v)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(v)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(v)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(v)*w.weight3;}return vs;}
 static void Preview(Animator actor,bool profile){
 var scene=EditorSceneManager.NewPreviewScene();var meshes=new System.Collections.Generic.List<Mesh>();
 try{
 foreach(var r in actor.GetComponentsInChildren<Renderer>()){
 Mesh mesh=null;var skin=r as SkinnedMeshRenderer;if(skin){mesh=UnityEngine.Object.Instantiate(skin.sharedMesh);mesh.vertices=Skin(skin);}else{var f=r.GetComponent<MeshFilter>();if(!f)continue;mesh=UnityEngine.Object.Instantiate(f.sharedMesh);mesh.vertices=mesh.vertices.Select(v=>r.transform.TransformPoint(v)).ToArray();}mesh.RecalculateBounds();meshes.Add(mesh);var o=new GameObject("Preview geometry");SceneManager.MoveGameObjectToScene(o,scene);o.AddComponent<MeshFilter>().sharedMesh=mesh;o.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
 }
 var head=actor.GetBoneTransform(HumanBodyBones.Head);var target=head.position+actor.transform.up*.105f*actor.transform.lossyScale.x;
 var co=new GameObject("Glasses preview camera");SceneManager.MoveGameObjectToScene(co,scene);var camera=co.AddComponent<Camera>();camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.orthographic=true;camera.orthographicSize=.235f;camera.backgroundColor=new Color(.25f,.26f,.24f);camera.clearFlags=CameraClearFlags.SolidColor;
 co.transform.position=target+(profile?actor.transform.right:actor.transform.forward+actor.transform.right*.18f);co.transform.LookAt(target);
 foreach(var dir in new[]{new Vector3(-2,3,-4),new Vector3(2,1,3)}){var lo=new GameObject("Softbox");SceneManager.MoveGameObjectToScene(lo,scene);var light=lo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=dir.y>2?1.15f:.5f;lo.transform.rotation=Quaternion.LookRotation(-dir);}
 var rt=new RenderTexture(1000,1000,24);camera.targetTexture=rt;var previous=RenderTexture.active;var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);try{camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();Directory.CreateDirectory("Captures/FactoryManager");File.WriteAllBytes("Captures/FactoryManager/Glasses_"+(profile?"Side":"Front")+".png",image.EncodeToPNG());}finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
 }finally{foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);EditorSceneManager.ClosePreviewScene(scene);}
 }
}
