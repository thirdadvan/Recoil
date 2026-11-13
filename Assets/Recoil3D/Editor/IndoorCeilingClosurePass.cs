using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class IndoorCeilingClosurePass
{
 public static string[] Paths(){return EditorBuildSettings.scenes.Where(s=>s.enabled&&s.path.Contains("/3D/")&&!s.path.EndsWith("/RecoilPrototype.unity")).Select(s=>s.path).Concat(new[]{"Assets/Scenes/3D/RecoilServiceStore_PS1.unity"}).Distinct().ToArray();}
 static bool Slab(MeshRenderer r)
 {
  if(!r.enabled||!r.gameObject.activeInHierarchy||!r.GetComponent<MeshFilter>())return false;
  string n=r.name.ToLowerInvariant();var b=r.bounds;
  bool ceiling=n.Contains("ceiling")||n.Contains("flat roof")||n.Contains("upper floor extends")||n=="roof slab"||n=="3d mesh - roof"||n=="combined roof structure - architecture - pitted concrete";
  if(!ceiling||b.size.x<2||b.size.z<1||b.size.y>1.1f)return false;
  return !new[]{"fixture","lamp","light","seam","joint","pipe","main","support","girder","rail","suspension","shade"}.Any(n.Contains);
 }
 public static string Audit(int start,int count)
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var previous=SceneManager.GetActiveScene().path;var result=new List<string>();
  try{foreach(var path in Paths().Skip(start).Take(count)){var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var cam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).FirstOrDefault();var slabs=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(Slab).ToArray();result.Add(Path.GetFileNameWithoutExtension(path)+" cam="+(cam?cam.transform.position.ToString():"none")+" | "+string.Join("; ",slabs.Select(r=>r.name+" ["+r.bounds.min+".."+r.bounds.max+"]")));}return string.Join("\n",result);}finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
 }

 const string Marker="Ceilings - camera-side closures - editable";
 sealed class ViewCheck {public int background;public byte[] png;}
 static ViewCheck RenderCheck(Camera original,float ceilingY)
 {
  var go=new GameObject("Temporary ceiling coverage camera");SceneManager.MoveGameObjectToScene(go,original.gameObject.scene);var cam=go.AddComponent<Camera>();cam.CopyFrom(original);cam.transform.SetPositionAndRotation(new Vector3(original.transform.position.x,ceilingY-1.5f,original.transform.position.z),original.transform.rotation);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.magenta;cam.enabled=false;
  var rt=RenderTexture.GetTemporary(640,400,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;Texture2D raw=null,img=null;
  try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(640,400,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,640,400),0,0);raw.Apply();var pixels=raw.GetPixels();int background=0;for(int y=260;y<400;y++)for(int x=0;x<640;x++){var c=pixels[y*640+x];if(c.r>.8f&&c.g<.2f&&c.b>.8f)background++;}if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(640,400,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();return new ViewCheck{background=background,png=img.EncodeToPNG()};}
  finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(img)Object.DestroyImmediate(img);Object.DestroyImmediate(go);}
 }
 public static string Apply(int start,int count)
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var previous=SceneManager.GetActiveScene().path;var report=new List<string>();Directory.CreateDirectory("output/indoor-ceiling-closure/backups");
  try
  {
   foreach(var path in Paths().Skip(start).Take(count))
   {
    var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);var cam=all.Select(t=>t.GetComponent<Camera>()).First(c=>c);
    if(world.transform.Find(Marker)){report.Add(Path.GetFileNameWithoutExtension(path)+": already closed");continue;}
    File.Copy(path,"output/indoor-ceiling-closure/backups/"+Path.GetFileName(path)+".backup",true);
    var colliders=all.SelectMany(t=>t.GetComponents<Collider2D>()).ToDictionary(c=>c,c=>c.bounds.ToString("F6"));var doors=all.SelectMany(t=>t.GetComponents<RecoilDoor>()).ToDictionary(d=>d,d=>EditorJsonUtility.ToJson(d));
    var candidates=all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r&&Slab(r)).OrderBy(r=>r.bounds.min.z).ToList();
    var selected=new List<MeshRenderer>();foreach(var r in candidates){var b=r.bounds;if(selected.Any(old=>Mathf.Abs(old.bounds.min.y-b.min.y)<.035f&&Mathf.Abs(old.bounds.max.y-b.max.y)<.035f&&old.bounds.min.x<=b.min.x+.02f&&old.bounds.max.x>=b.max.x-.02f))continue;selected.Add(r);}
    bool stair=path.EndsWith("/RecoilResidentialStairwell.unity");if(selected.Count==0&&!stair)throw new Exception("No ceiling source in "+path);
    float lowest=stair?9.8f:selected.Min(r=>r.bounds.min.y);var before=RenderCheck(cam,lowest);
    var group=new GameObject(Marker).transform;SceneManager.MoveGameObjectToScene(group.gameObject,scene);group.SetParent(world.transform,false);group.position=Vector3.zero;group.rotation=Quaternion.identity;group.localScale=Vector3.one;
    var boundsList=selected.Select(r=>r.bounds).ToList();var materials=selected.Select(r=>r.sharedMaterial).ToList();
    if(stair){boundsList.Add(new Bounds(new Vector3(6,9.9f,1.5f),new Vector3(13,.2f,9)));materials.Add(all.Select(t=>t.GetComponent<MeshRenderer>()).First(r=>r&&r.enabled&&r.name.Contains("Workers housing faded ochre plaster")).sharedMaterial);}
    bool store=path.Contains("RecoilServiceStore");float near=Mathf.Min(-30,cam.transform.position.z-6);int serial=0;
    foreach(var b in boundsList)
    {
     float end=store?-1.30f:Mathf.Min(-3,b.min.z);var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Foreground ceiling closure "+(++serial).ToString("D2");Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(group,false);go.transform.position=new Vector3(b.center.x,b.center.y,(near+end)*.5f);go.transform.localScale=new Vector3(b.size.x+1.30f,Mathf.Max(.18f,b.size.y),end-near+.02f);go.GetComponent<MeshRenderer>().sharedMaterial=materials[serial-1];GameObjectUtility.SetStaticEditorFlags(go,StaticEditorFlags.BatchingStatic);
     // The extension ends ahead of the gameplay plane and all genuine skylight apertures.
     if(go.GetComponent<Renderer>().bounds.max.z>-.99f)throw new Exception("Closure overlaps gameplay opening");
    }
    Physics2D.SyncTransforms();foreach(var c in colliders)if(c.Key.bounds.ToString("F6")!=c.Value)throw new Exception("Gameplay collision changed");foreach(var d in doors)if(EditorJsonUtility.ToJson(d.Key)!=d.Value)throw new Exception("Door or skylight route changed");
    var after=RenderCheck(cam,lowest);string name=Path.GetFileNameWithoutExtension(path);File.WriteAllBytes("output/indoor-ceiling-closure/"+name+"-before.png",before.png);File.WriteAllBytes("output/indoor-ceiling-closure/"+name+"-after.png",after.png);
    EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed");
    report.Add(name+": "+serial+" camera-side ceiling caps, Z="+near+".."+(store?-1.3f:-3)+"; top-view background pixels "+before.background+" -> "+after.background+"; collider/door state unchanged; skylights left intact.");
   }
   File.AppendAllLines("output/indoor-ceiling-closure/verification.txt",report);return string.Join("\n",report);
  }
  finally{if(!string.IsNullOrEmpty(previous))EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
 }
 public static string Refine(int start,int count)
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var previous=SceneManager.GetActiveScene().path;var reports=new List<string>();
  try{foreach(var path in Paths().Skip(start).Take(count)){
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var world=Object.FindObjectOfType<RecoilPrototype>();var group=world.transform.Find(Marker);if(!group)throw new Exception("Missing caps");
   bool store=path.Contains("RecoilServiceStore"),stair=path.Contains("RecoilResidentialStairwell");
   if(store){var first=group.GetChild(0);for(int i=group.childCount-1;i>0;i--)Object.DestroyImmediate(group.GetChild(i).gameObject);first.position=new Vector3(8,first.position.y,first.position.z);first.localScale=new Vector3(76,first.localScale.y,first.localScale.z);}
   else foreach(Transform cap in group){var scale=cap.localScale;scale.x+=60;cap.localScale=scale;}
   if(stair){var cap=group.GetChild(0);cap.position=new Vector3(cap.position.x,9.9f,(-30+6.1f)*.5f);cap.localScale=new Vector3(cap.localScale.x,.2f,36.12f);}
   var cam=Object.FindObjectOfType<Camera>();float ceiling=group.Cast<Transform>().Min(t=>t.GetComponent<Renderer>().bounds.min.y);var check=RenderCheck(cam,ceiling);var name=Path.GetFileNameWithoutExtension(path);File.WriteAllBytes("output/indoor-ceiling-closure/"+name+"-final.png",check.png);EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");reports.Add(name+": widened foreground closure; "+group.childCount+" caps; top-region background="+check.background+(stair?"; completed missing stairwell roof":""));
  }File.AppendAllLines("output/indoor-ceiling-closure/verification.txt",reports);return string.Join("\n",reports);}finally{EditorSceneManager.OpenScene(previous,OpenSceneMode.Single);}
 }

}
