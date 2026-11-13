using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class DoorBeamConsistencyPass
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 public static string Apply(){
 if(EditorApplication.isPlaying)throw new Exception("Stop Play first");var original=SceneManager.GetActiveScene();string backup="/tmp/Recoil-DoorBeamConsistency/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);var report=new List<string>();int count=0;
 foreach(var path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/3D"}).Select(AssetDatabase.GUIDToAssetPath)){
 var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
 try{var glows=All<DoorProximityGlow>(s).Where(g=>g.volumeEnabled&&g.VolumeLight&&g.useTrapezoidMesh).ToArray();if(glows.Length==0)continue;
 File.Copy(path,backup+"/"+s.name+".unity",true);
 foreach(var g in glows){float floor=g.volumeOrigin.y-(Mathf.Abs(g.apertureHalfSize.y-.76f)<.001f?.46f:1.15f); // Existing authoring convention: opening centre = floor + 1.15.
 // Preserve scene-specific X/Z anchor; use the requested height relative to this floor.
 g.volumeOrigin=new Vector3(g.volumeOrigin.x,floor+.46f,g.volumeOrigin.z);
 g.foregroundExtension=32;g.beamEdgeFeather=.02f;g.beamEndFeather=.31f;g.beamBrightness=1.8f;
 g.apertureHalfSize=new Vector2(.51f,.76f);g.volumeLength=3.35f;g.volumeSpread=.156f;
 g.restingDensity=.009f;g.activeDensity=.04f;g.fadeIn=.89f;g.fadeOut=2.39f;g.haloStrength=.34f;g.haloTint=new Color(.62f,.77f,.84f,1);g.editorPreview=0;
 // Keep each doorway's assigned light reference; do not copy another scene's world coordinates.
 g.ApplyBlend(0);EditorUtility.SetDirty(g);report.Add(s.name+" / "+g.name+": screenshot beam settings applied");count++;}
 var foreground=All<MeshRenderer>(s).FirstOrDefault(r=>r.name=="3D continuous foreground floor");
 if(foreground){var main=All<MeshRenderer>(s).Where(r=>r!=foreground&&r.enabled&&r.name.StartsWith("3D mesh - ")&&r.name.ToLowerInvariant().Contains("floor")&&r.bounds.size.x>5&&Mathf.Abs(r.bounds.max.y)<.25f&&r.bounds.min.z>foreground.bounds.min.z+.1f).OrderByDescending(r=>r.bounds.size.x).FirstOrDefault();
 if(main){var before=foreground.bounds;float front=before.min.z,back=main.bounds.min.z;float heightDifference=main.bounds.max.y-before.max.y;var t=foreground.transform;var scale=t.localScale;scale.z*=(back-front)/before.size.z;t.localScale=scale;var p=t.position;p.z+=(front+back)*.5f-before.center.z;p.y+=heightDifference;t.position=p;EditorUtility.SetDirty(t);
 if(Mathf.Abs(foreground.bounds.max.y-main.bounds.max.y)>.0001f||Mathf.Abs(foreground.bounds.max.z-main.bounds.min.z)>.0001f)throw new Exception("Floor join failed");report.Add("  Floor seam repaired: height "+heightDifference.ToString("F3")+"m; front/back slabs meet at Z="+back+" without overlapping top surfaces");}}
 EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new IOException("Scene save failed: "+path);
 }finally{if(opened)EditorSceneManager.CloseScene(s,true);}}
 SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();AssetDatabase.SaveAssets();Directory.CreateDirectory("output/door-beam-consistency");File.WriteAllLines("output/door-beam-consistency/result.txt",report);return count+" doors updated. Backup: "+backup+"\n"+string.Join("\n",report.ToArray());}
}
