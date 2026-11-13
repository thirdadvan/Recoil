using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class ArchiveRecordDimmingPass
{
 const string Folder="Assets/Art/Environment/ArchiveRoom/Ransacked/";
 static Material DimMaterial(Material source){string path=Folder+"Shelf surface - "+source.name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}mat.CopyPropertiesFromMaterial(source);mat.shader=Shader.Find(source.shader.name=="Standard"?"Recoil/3D/Archive Shelf Standard":"Recoil/3D/Archive Shelf Surface");mat.enableInstancing=true;EditorUtility.SetDirty(mat);return mat;}
 static void StoreMesh(Mesh mesh,MeshFilter target,string name){string path=Folder+"Dimmed "+name.Replace('/','-')+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);}else{saved=mesh;AssetDatabase.CreateAsset(saved,path);}target.sharedMesh=saved;EditorUtility.SetDirty(target);}
 public static string Apply(){
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||!scene.path.EndsWith("/FactoryArchiveF3.unity"))throw new Exception("Open archive in Edit mode");
  var racks=UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name.Contains("frame shelves and varied records")||t.name.StartsWith("Rear archive rack ")&&t.name.Contains("complete records assembly")).ToArray();if(racks.Length!=7)throw new Exception("Expected seven racks");int batches=0,recordParts=0;
  foreach(var rack in racks){
   var sources=rack.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&!r.name.StartsWith("Static detail - ")).ToArray();
   foreach(var batch in rack.Cast<Transform>().Where(t=>t.name.StartsWith("Static detail - "))){var renderer=batch.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;
    string materialName=batch.name.Substring("Static detail - ".Length);var parts=sources.Where(r=>r.sharedMaterial&&r.sharedMaterial.name==materialName).ToArray();if(parts.Length==0)continue;
    var mesh=new Mesh{name="Archive shelf - records at 75 percent",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(parts.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=rack.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);
    var colors=new Color[mesh.vertexCount];int offset=0;foreach(var part in parts){bool record=false;for(var t=part.transform;t&&t!=rack;t=t.parent)if(t.name.StartsWith("Upright bound file ")||t.name.StartsWith("Tied archive carton ")){record=true;break;}if(record)recordParts++;int count=part.GetComponent<MeshFilter>().sharedMesh.vertexCount;for(int i=0;i<count;i++)colors[offset++]=record?new Color(.75f,.75f,.75f,1):Color.white;}
    mesh.colors=colors;StoreMesh(mesh,batch.GetComponent<MeshFilter>(),rack.name+" - "+materialName);renderer.sharedMaterial=DimMaterial(parts[0].sharedMaterial);EditorUtility.SetDirty(renderer);batches++;
   }
  }
  // The original eight individually editable floor documents use their own small meshes.
  foreach(var t in UnityEngine.Object.FindObjectsOfType<Transform>().Where(t=>t.name.StartsWith("Floor document "))){var mf=t.GetComponent<MeshFilter>();var mr=t.GetComponent<MeshRenderer>();if(!mf||!mr)continue;var copy=UnityEngine.Object.Instantiate(mf.sharedMesh);copy.colors=Enumerable.Repeat(new Color(.75f,.75f,.75f,1),copy.vertexCount).ToArray();StoreMesh(copy,mf,t.name);if(mr.sharedMaterial.shader.name!="Recoil/3D/Archive Shelf Surface")mr.sharedMaterial=DimMaterial(mr.sharedMaterial);EditorUtility.SetDirty(mr);}
  var floor=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Shared archive records and paper.mat");Undo.RecordObject(floor,"Set floor records to 75 percent brightness");float value=QualitySettings.activeColorSpace==ColorSpace.Linear?Mathf.LinearToGammaSpace(.75f):.75f;floor.color=new Color(value,value,value,1);EditorUtility.SetDirty(floor);
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();bool savedScene=EditorSceneManager.SaveScene(scene);return "Saved="+savedScene+"; shelf batches="+batches+"; dimmed record parts="+recordParts+"; ground coefficient=0.75. Shelf frames retain coefficient=1.0. Batch count unchanged.";
 }
}
