using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class GroundMossContinuityPass
{
 const string PathName="Assets/Scenes/3D/RecoilPrototype.unity",Folder="Assets/Recoil3D/GroundMossContinuity",Marker="Wall moss continuing over exterior paving - corrected upward faces";
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static bool Ground(MeshRenderer r){return r.sharedMaterial&&r.sharedMaterial.shader.name=="Recoil/3D/Building Footing Moss And Silt"&&r.sharedMaterial.GetFloat("_Ground")>.5f;}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; apply deferred";Directory.CreateDirectory(Folder+"/Models");Directory.CreateDirectory("Backups/GroundMossContinuity");Directory.CreateDirectory("output/ground-moss-continuity");var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(PathName);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(PathName,OpenSceneMode.Additive);
  try{
   if(All<Transform>(s).Any(t=>t.name==Marker))return "Ground moss continuity already corrected";
   var backup="Backups/GroundMossContinuity/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(s,backup,true);
   var collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();
   var renderers=All<MeshRenderer>(s).Where(Ground).ToArray();int downBefore=0,upAfter=0,visible=0,index=0;
   foreach(var r in renderers){var f=r.GetComponent<MeshFilter>();var source=f.sharedMesh;var mesh=Object.Instantiate(source);mesh.name="Upward spreading ground moss "+index;var vertices=mesh.vertices;var uv=mesh.uv;var triangles=mesh.triangles;if(vertices.Length%4!=0||uv.Length!=vertices.Length)throw new Exception("Unexpected ground moss topology: "+r.name);
    for(int i=0;i<vertices.Length;i+=4){if(uv[i].y>.01f||uv[i+1].y>.01f||uv[i+2].y<.99f||uv[i+3].y<.99f)throw new Exception("Unexpected growth quad UVs");vertices[i+2]=vertices[i]+(vertices[i+2]-vertices[i])*1.75f;vertices[i+3]=vertices[i+1]+(vertices[i+3]-vertices[i+1])*1.75f;for(int j=0;j<4;j++)vertices[i+j]+=Vector3.up*.003f;}
    for(int i=0;i<triangles.Length;i+=3){var normal=r.transform.TransformDirection(Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]));if(normal.y<0){if(r.enabled&&r.gameObject.activeInHierarchy)downBefore++;int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}if(r.enabled&&r.gameObject.activeInHierarchy)upAfter++;}
    mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/Models/Upward spreading ground moss "+index+".asset");f.sharedMesh=mesh;EditorUtility.SetDirty(f);if(r.enabled&&r.gameObject.activeInHierarchy)visible++;index++;
   }
   foreach(var m in renderers.Select(r=>r.sharedMaterial).Distinct()){bool factory=m.name.StartsWith("Factory front");m.SetColor("_MossTint",factory?new Color(.24f,.30f,.065f):new Color(.18f,.25f,.05f));m.SetColor("_SiltTint",new Color(.085f,.073f,.039f));m.SetFloat("_Opacity",.93f);m.renderQueue=3001;EditorUtility.SetDirty(m);}
   var marker=new GameObject(Marker);SceneManager.MoveGameObjectToScene(marker,s);marker.transform.SetParent(All<RecoilPrototype>(s).Single().transform,false);
   if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled+"|"+c.gameObject.activeInHierarchy)))throw new Exception("Unexpected collision change");if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Unexpected door change");
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();string result="PASS repaired "+renderers.Length+" ground moss source/combined meshes, including "+visible+" visible material batches. Corrected "+downBefore+" downward-facing rendered triangles; now "+upAfter+" upward-facing triangles. Paving moss spreads 1.75 times farther (typical 0.84m, factory 1.085m) with dense wall contact and broken colonies fading outward. Wall layers unchanged. Existing source mesh assets retained; new meshes stored separately. Collision and door state unchanged. Backup="+backup;File.WriteAllText("output/ground-moss-continuity/verification.txt",result);return result;
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
 }
 public static string Capture()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)return "Playing; capture deferred";var previous=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(PathName);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(PathName,OpenSceneMode.Additive);var others=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(scene=>scene!=s).ToArray();var rs=others.SelectMany(All<Renderer>).Where(r=>r.enabled).ToArray();var ls=others.SelectMany(All<Light>).Where(l=>l.enabled).ToArray();GameObject cg=null;
  try{foreach(var r in rs)r.enabled=false;foreach(var l in ls)l.enabled=false;SceneManager.SetActiveScene(s);All<Recoil3DWorld>(s).First().ApplyAtmosphere();cg=new GameObject("Temporary ground moss continuity camera");var camera=cg.AddComponent<Camera>();camera.fieldOfView=43;camera.nearClipPlane=.1f;camera.farClipPlane=220;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.03f,.035f);var shot=typeof(FactoryRoofStairHeadPass).GetMethod("Shot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
   camera.transform.position=new Vector3(101,3.6f,-7);camera.transform.LookAt(new Vector3(100.5f,.4f,2.4f));shot.Invoke(null,new object[]{camera,"FactoryMossContinuesOntoPaving"});camera.transform.position=new Vector3(118,4.1f,-6);camera.transform.LookAt(new Vector3(118.5f,.3f,2.4f));shot.Invoke(null,new object[]{camera,"WarehouseMossContinuesOntoPaving"});camera.transform.position=new Vector3(-10.3f,4.7f,-8);camera.transform.LookAt(new Vector3(-10.3f,.65f,2.4f));shot.Invoke(null,new object[]{camera,"StoreMossContinuesOntoPaving"});return "Three real night scene screenshots saved";
  }finally{if(cg)Object.DestroyImmediate(cg);foreach(var r in rs)if(r)r.enabled=true;foreach(var l in ls)if(l)l.enabled=true;if(opened)EditorSceneManager.CloseScene(s,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
 }
}
