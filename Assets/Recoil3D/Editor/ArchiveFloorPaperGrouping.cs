using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;
public static class ArchiveFloorPaperGrouping
{
 const string Folder="Assets/Art/Environment/ArchiveRoom/";
 static Mesh Combine(MeshRenderer[] sources,Transform space,string name)
 {
  var mesh=new Mesh{name=name};mesh.CombineMeshes(sources.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=space.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);return mesh;
 }
 static Mesh Save(Mesh mesh,string name)
 {var path=Folder+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing){Object.DestroyImmediate(mesh);return existing;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("FactoryArchiveF3.unity"))throw new Exception("Archive required");
  var group=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Floor paper and displaced cartons");
  if(group.Find("Loose floor documents - individually movable"))return "Already grouped";
  var sheets=group.Cast<Transform>().Where(t=>t.name.StartsWith("Loose typed record ")).ToArray();if(sheets.Length!=8)throw new Exception("Expected eight floor documents");
  var batches=group.Cast<Transform>().Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r&&r.name.StartsWith("Static detail - ")).ToArray();var paperBatch=batches.Single(r=>r.sharedMaterial.name=="Old paper");
  var targetPositions=sheets.ToDictionary(t=>t,t=>paperBatch.localToWorldMatrix.MultiplyPoint3x4(group.worldToLocalMatrix.MultiplyPoint3x4(t.position)));
  var template=sheets[0].GetComponentsInChildren<MeshRenderer>(true);var mats=template.Select(r=>r.sharedMaterial).Distinct().ToArray();var temporary=mats.Select(mat=>Combine(template.Where(r=>r.sharedMaterial==mat).ToArray(),sheets[0],"Sheet component")).ToArray();var mesh=new Mesh{name="Loose archive document - shared paper print and stamp"};mesh.CombineMeshes(temporary.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);foreach(var temp in temporary)Object.DestroyImmediate(temp);mesh=Save(mesh,"Loose archive document - shared paper print and stamp");
  Undo.RegisterFullObjectHierarchyUndo(group.gameObject,"Make floor documents individually movable");
  // Rebuild only the remaining carton geometry in the original material batches.
  var cartonRenderers=group.Cast<Transform>().Where(t=>t.name.StartsWith("Tied archive carton ")).SelectMany(t=>t.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
  foreach(var batch in batches){var remaining=cartonRenderers.Where(r=>r.sharedMaterial==batch.sharedMaterial).ToArray();if(remaining.Length==0){Undo.DestroyObjectImmediate(batch.gameObject);continue;}var replacement=Save(Combine(remaining,group,"Floor cartons - "+batch.sharedMaterial.name),"Floor cartons only - "+batch.sharedMaterial.name);batch.GetComponent<MeshFilter>().sharedMesh=replacement;EditorUtility.SetDirty(batch.GetComponent<MeshFilter>());}
  var parent=new GameObject("Loose floor documents - individually movable").transform;parent.SetParent(group,false);parent.position=sheets.Select(t=>targetPositions[t]).Aggregate(Vector3.zero,(sum,pos)=>sum+pos)/sheets.Length;
  int index=0;foreach(var sheet in sheets){var rotation=sheet.rotation;sheet.SetParent(parent,true);sheet.position=targetPositions[sheet];sheet.rotation=rotation;sheet.name="Floor document "+(++index).ToString("D2");for(int i=sheet.childCount-1;i>=0;i--)Undo.DestroyObjectImmediate(sheet.GetChild(i).gameObject);sheet.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;sheet.gameObject.AddComponent<MeshRenderer>().sharedMaterials=mats;}
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();Selection.activeGameObject=parent.gameObject;
  return "Saved eight independently selectable floor document objects under one English parent; shared mesh and materials; retained visible paper positions and carton batches.";
 }
}
