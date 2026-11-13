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
public static class FreightRailSurfaceDiagnosis
{
 const string Dir="output/freight-rail-surfaces";
 static FreightRailSurfaceDiagnosis(){EditorApplication.update+=Poll;}
 static void Poll(){if(!File.Exists(Dir+"/request")||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;string cmd=File.ReadAllText(Dir+"/request").Trim();File.Delete(Dir+"/request");try{if(cmd=="fix"){Run();Fix();}Run();}catch(Exception e){File.WriteAllText(Dir+"/error.txt",e.ToString());Debug.LogException(e);}}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}

 static void Fix(){var previous=SceneManager.GetActiveScene();var report=new StringBuilder();foreach(var name in new[]{"Normal","No-shadows","Crane-only"}){var f=Dir+"/"+name+".png";if(File.Exists(f))File.Copy(f,Dir+"/Before-"+name+".png",true);}
 foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryWarehouse.unity",WarehouseInsideLightingTestBuilder.Destination}){var scene=SceneManager.GetSceneByPath(path);bool added=!scene.isLoaded;if(added)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);try{
 var crane=All<KBKStackerCrane>(scene).Single();var rails=crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.name=="KBK suspension rail").ToArray();if(rails.Length!=2)throw new Exception("Expected two rail beams");
 var structure=All<MeshRenderer>(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.GetComponentInParent<KBKStackerCrane>()&&(r.transform.parent.name=="Roof beam"||r.transform.parent.name=="Steel column")).ToArray();if(structure.Length==0)throw new Exception("Warehouse structural beams not found");
 var collision=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6")).ToArray();var start=crane.startPoint.position;var end=crane.destinationPoint.position;var platform=crane.forkPlatform.Area;
 string backup="Backups/FreightRailStructure/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"/"+scene.name+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Backup failed");
 float target=structure.Min(r=>r.bounds.min.z)-rails.Max(r=>r.meshRenderer.bounds.extents.z)-.10f;
 float delta=target-rails[0].meshRenderer.bounds.center.z;int count=0;
 foreach(var v in crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>new[]{"KBK suspension rail","Bright rail flange","Rail fixing plate","Rail bolt","Rail ceiling suspension"}.Contains(v.name))){v.depthOffset+=v.transform.InverseTransformVector(Vector3.forward*delta).z;v.Sync();EditorUtility.SetDirty(v);EditorUtility.SetDirty(v.meshRenderer.transform);count++;}
 foreach(var t in crane.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Trolley wheel")){t.position+=Vector3.forward*delta;EditorUtility.SetDirty(t);}
 foreach(var rail in rails)foreach(var r in structure){var b=rail.meshRenderer.bounds;var a=r.bounds;if(b.min.x<a.max.x&&b.max.x>a.min.x&&b.min.y<a.max.y&&b.max.y>a.min.y&&b.max.z>a.min.z-.09f)throw new Exception("Rail still intersects structural beam");}
 Physics2D.SyncTransforms();if(start!=crane.startPoint.position||end!=crane.destinationPoint.position||platform!=crane.forkPlatform.Area||!collision.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds.ToString("F6"))))throw new Exception("Gameplay collision/endpoints changed");
 var preview=EditorSceneManager.NewPreviewScene();try{var copy=Object.Instantiate(crane.gameObject);SceneManager.MoveGameObjectToScene(copy,preview);var c=copy.GetComponent<KBKStackerCrane>();c.requiredPowerItem="";c.ResetMachine();float parked=c.forkPlatform.Area.yMax;for(int trip=0;trip<2;trip++){if(!c.Activate())throw new Exception("Crane did not start");int ticks=0;while(c.Busy&&ticks++<3000){c.Simulate(.02f);var mast=c.GetComponent<FreightTelescopicMast>();if(mast)mast.ApplyPose();}if(c.Busy||Mathf.Abs(c.forkPlatform.Area.yMax-parked)>.002f)throw new Exception("Crane motion failed");}}finally{EditorSceneManager.ClosePreviewScene(preview);}
 EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");report.AppendLine("PASS "+scene.name+": "+(count+2)+" rail/wheel visual parts shifted "+delta.ToString("F3")+" m in Z; structural clearance >= 0.10 m. All collision, platform area and endpoints unchanged. Outbound/return lift cycles passed. Backup: "+backup);
 }finally{if(added)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}}
 File.WriteAllText(Dir+"/verification.txt",report.ToString());}
 static void Run(){var previous=SceneManager.GetActiveScene();const string path="Assets/Scenes/3D/RecoilFactoryWarehouse.unity";var s=SceneManager.GetSceneByPath(path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);try{
 var crane=All<KBKStackerCrane>(s).Single();var rails=crane.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.name=="KBK suspension rail").ToArray();var b=rails[0].meshRenderer.bounds;var report=new StringBuilder();
 foreach(var r in All<MeshRenderer>(s).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.Intersects(new Bounds(b.center,new Vector3(b.size.x,.8f,1))))){report.AppendLine(r.name+" parent="+r.transform.parent.name+" bounds="+r.bounds.ToString("F5")+" shadow="+r.shadowCastingMode+" receive="+r.receiveShadows);}
 foreach(var l in All<Light>(s).Where(l=>l.enabled))report.AppendLine("LIGHT "+l.name+" shadow="+l.shadows+" bias="+l.shadowBias+" normalBias="+l.shadowNormalBias);
 File.WriteAllText(Dir+"/inspection.txt",report.ToString());
 var go=new GameObject("Temporary rail surface camera");SceneManager.MoveGameObjectToScene(go,s);var cam=go.AddComponent<Camera>();cam.scene=s;cam.orthographic=true;cam.orthographicSize=.65f;cam.aspect=2;cam.transform.position=new Vector3(13,7.6f,-19.5f);cam.transform.LookAt(new Vector3(12,6.6f,3));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;var review=go.AddComponent<Light>();review.type=LightType.Point;review.range=35;review.intensity=2;review.shadows=LightShadows.None;
 var lights=All<Light>(s);var shadows=lights.Select(l=>l.shadows).ToArray();var others=All<MeshRenderer>(s).Where(r=>r.enabled&&!r.GetComponentInParent<KBKStackerCrane>()).ToArray();
 try{Capture(cam,"Normal");foreach(var l in lights)l.shadows=LightShadows.None;Capture(cam,"No-shadows");foreach(var r in others)r.enabled=false;Capture(cam,"Crane-only");}
 finally{for(int i=0;i<lights.Length;i++)if(lights[i])lights[i].shadows=shadows[i];foreach(var r in others)if(r)r.enabled=true;Object.DestroyImmediate(go);}
 }finally{if(added)EditorSceneManager.CloseScene(s,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}}
 static void Capture(Camera cam,string name){var rt=new RenderTexture(1600,800,24);var tex=new Texture2D(1600,800,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1600,800),0,0);tex.Apply();File.WriteAllBytes(Dir+"/"+name+".png",tex.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=old;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}}
}
