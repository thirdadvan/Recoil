using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class FreightRailInspection
{
 const string Dir="output/freight-rail";
 static FreightRailInspection(){EditorApplication.update+=Poll;}
 static void Poll(){if(!File.Exists(Dir+"/request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;string cmd=File.ReadAllText(Dir+"/request").Trim();File.Delete(Dir+"/request");try{if(cmd=="fix")Fix();Inspect();}catch(Exception e){File.WriteAllText(Dir+"/error.txt",e.ToString());Debug.LogException(e);}}

 static void Depth(Recoil3DSpriteVisual v,float centre,float thickness){v.depth=thickness;v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.transform.position.x,v.transform.position.y,centre)).z;v.Sync();EditorUtility.SetDirty(v);}
 static void Fix(){var prev=SceneManager.GetActiveScene();var report=new StringBuilder();foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryWarehouse.unity",WarehouseInsideLightingTestBuilder.Destination}){
 var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
 var c=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).Single();
 string backup="Backups/FreightRail/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"/"+s.name+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(s,backup,true))throw new Exception("Backup failed");
 var colliders=c.GetComponentsInChildren<Collider2D>(true).Select(x=>x.GetInstanceID()+"|"+x.bounds.ToString("F6")).ToArray();var start=c.startPoint.position;var end=c.destinationPoint.position;
 var visuals=c.GetComponentsInChildren<Recoil3DSpriteVisual>(true);var rails=visuals.Where(v=>v.name=="KBK suspension rail").ToArray();if(rails.Length!=2)throw new Exception("Expected two rail beams");
 var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ArtTests/Freight exposed guide steel.mat");if(!steel)throw new Exception("Steel material missing");
 int count=0;foreach(var v in visuals){
 if(v.name=="Rail ceiling suspension"){float z=rails[0].meshRenderer.bounds.center.z;Depth(v,z,.14f);v.meshRenderer.sharedMaterial=steel;EditorUtility.SetDirty(v.meshRenderer);count++;continue;}
 if(v.name!="Rail fixing plate"&&v.name!="Rail bolt"&&v.name!="Bright rail flange")continue;
 var rail=rails.OrderBy(r=>Mathf.Abs(r.meshRenderer.bounds.center.y-v.transform.position.y)).First().meshRenderer.bounds;
 if(v.name=="Rail fixing plate")Depth(v,rail.min.z-.025f,.035f);
 else if(v.name=="Rail bolt")Depth(v,rail.min.z-.055f,.018f);
 else Depth(v,rail.center.z,rail.size.z+.04f);
 count++;
 }
 Physics2D.SyncTransforms();if(c.startPoint.position!=start||c.destinationPoint.position!=end||!colliders.SequenceEqual(c.GetComponentsInChildren<Collider2D>(true).Select(x=>x.GetInstanceID()+"|"+x.bounds.ToString("F6"))))throw new Exception("Gameplay geometry changed");
 var preview=EditorSceneManager.NewPreviewScene();try{var copy=Object.Instantiate(c.gameObject);SceneManager.MoveGameObjectToScene(copy,preview);var crane=copy.GetComponent<KBKStackerCrane>();crane.requiredPowerItem="";crane.ResetMachine();float parked=crane.forkPlatform.Area.yMax;for(int trip=0;trip<2;trip++){if(!crane.Activate())throw new Exception("Crane failed to start");int ticks=0;while(crane.Busy&&ticks++<3000){crane.Simulate(.02f);var mast=crane.GetComponent<FreightTelescopicMast>();if(mast)mast.ApplyPose();}if(crane.Busy||Mathf.Abs(crane.forkPlatform.Area.yMax-parked)>.002f)throw new Exception("Crane failed return/lower check");}}finally{EditorSceneManager.ClosePreviewScene(preview);}
 EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Save failed");report.AppendLine("PASS "+s.name+": "+count+" visual parts corrected; collider bounds/endpoints unchanged; outbound and return cycles passed. Backup: "+backup);
 }finally{if(added)EditorSceneManager.CloseScene(s,true);if(prev.IsValid()&&prev.isLoaded)SceneManager.SetActiveScene(prev);}}
 File.WriteAllText(Dir+"/verification.txt",report.ToString());
 }
 static void Inspect(){const string path="Assets/Scenes/3D/RecoilFactoryWarehouse.unity";var prev=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
 var c=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<KBKStackerCrane>(true)).Single();
 var report=new StringBuilder();foreach(var r in c.GetComponentsInChildren<Renderer>(true)){if(!r.enabled||!r.gameObject.activeInHierarchy)continue;report.AppendLine(r.name+" | parent="+r.transform.parent.name+" | bounds="+r.bounds.ToString("F4")+" | mat="+(r.sharedMaterial?r.sharedMaterial.name:"none"));}
 File.WriteAllText(Dir+"/inspection.txt",report.ToString());
 var rails=c.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&r.name.Contains("suspension rail")).ToArray();var b=rails[0].bounds;foreach(var r in rails)b.Encapsulate(r.bounds);
 Capture(s,b.center,Mathf.Max(2,b.size.x*.28f),"Overhead-rail");
 var mast=c.GetComponent<FreightTelescopicMast>();if(mast&&mast.topAnchor)Capture(s,mast.topAnchor.position-Vector3.up*mast.sectionLength,3,"Mast-rails");
 }finally{if(added)EditorSceneManager.CloseScene(s,true);if(prev.IsValid()&&prev.isLoaded)SceneManager.SetActiveScene(prev);}}
 static void Capture(Scene s,Vector3 centre,float size,string name){var g=new GameObject("Temporary rail inspection camera");SceneManager.MoveGameObjectToScene(g,s);var cam=g.AddComponent<Camera>();cam.scene=s;cam.orthographic=true;cam.orthographicSize=size;cam.aspect=2;cam.transform.position=centre+new Vector3(1,1,-18);cam.transform.LookAt(centre);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.07f,.09f,.1f);var rt=new RenderTexture(1600,800,24);var tex=new Texture2D(1600,800,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,800),0,0);tex.Apply();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(g);}}
}
