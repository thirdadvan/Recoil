using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
[InitializeOnLoad]
public static class WindowBoardingPass
{
 const string Dir="output/window-boarding",PathScene="Assets/Scenes/3D/RecoilPrototype.unity";
 static WindowBoardingPass(){EditorApplication.update+=Poll;}
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
 static void Poll(){string request=Dir+"/request";if(!File.Exists(request)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;string cmd=File.ReadAllText(request).Trim();File.Delete(request);try{Run(cmd);}catch(Exception e){File.WriteAllText(Dir+"/error.txt",e.ToString());Debug.LogException(e);}}
 static void Run(string cmd){var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(PathScene);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(PathScene,OpenSceneMode.Additive);try{
 if(cmd=="apply"){Apply(scene);return;}
 var warehouse=All<Transform>(scene).Single(t=>t.name=="Factory Warehouse - exterior");var report=new StringBuilder();
 foreach(var t in warehouse.GetComponentsInChildren<Transform>(true))if(t.name.ToLower().Contains("window")||t.name.ToLower().Contains("wall")){report.AppendLine(t.name+" | pos="+t.position+" | scale="+t.lossyScale);foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true))report.AppendLine("  "+r.name+" bounds="+r.bounds+" mat="+(r.sharedMaterial?r.sharedMaterial.name:"none"));}
 foreach(var t in All<Transform>(scene).Where(t=>t.name.StartsWith("Office window ")))report.AppendLine("OFFICE "+t.name+" | "+t.position+" | "+string.Join(",",t.Cast<Transform>().Select(x=>x.name).ToArray()));
 File.WriteAllText(Dir+"/inspection.txt",report.ToString());
 }finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}}
 const string AssetsDir="Assets/Recoil3D/WindowBoardingVariants",Marker="Window boarding variety - shared geometry";
 static Mesh SaveMesh(Mesh mesh,string name){string path=AssetsDir+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){Object.DestroyImmediate(mesh);return old;}mesh.name=name;AssetDatabase.CreateAsset(mesh,path);return mesh;}
 static Mesh PartMesh(){
  var poly=new[]{new Vector2(-.5f,-.48f),new Vector2(.45f,-.5f),new Vector2(.5f,-.18f),new Vector2(.46f,.04f),new Vector2(.5f,.49f),new Vector2(-.48f,.5f),new Vector2(-.46f,.12f),new Vector2(-.5f,-.04f)};
  var v=new List<Vector3>();var tris=new List<int>();var uv=new List<Vector2>();int n=poly.Length;
  for(int z=0;z<2;z++)foreach(var a in poly){v.Add(new Vector3(a.x,a.y,z==0?-.5f:.5f));uv.Add(a+Vector2.one*.5f);}
  for(int i=1;i<n-1;i++){tris.AddRange(new[]{0,i+1,i,n,n+i,n+i+1});}
  for(int i=0;i<n;i++){int j=(i+1)%n;tris.AddRange(new[]{i,j,n+j,i,n+j,n+i});}
  var m=new Mesh();m.SetVertices(v);m.SetTriangles(tris,0);m.SetUVs(0,uv);m.RecalculateNormals();m.RecalculateBounds();return m;
 }
 static Mesh Variant(Mesh source,int style){var parts=new List<CombineInstance>();Action<float,float,float,float,float> plank=(x,y,l,h,a)=>parts.Add(new CombineInstance{mesh=source,transform=Matrix4x4.TRS(new Vector3(x,y,-parts.Count*.006f),Quaternion.Euler(0,0,a),new Vector3(l,h,.075f))});
  switch(style){
   case 0:plank(-.01f,-.35f,1.08f,.16f,2);plank(.02f,-.11f,1.01f,.18f,-4);plank(-.03f,.14f,1.05f,.15f,5);plank(.02f,.37f,.96f,.16f,-2);break;
   case 1:plank(0,-.29f,1.07f,.16f,0);plank(0,.29f,1.03f,.18f,-3);plank(-.02f,0,1.25f,.13f,37);plank(.02f,0,1.23f,.13f,-37);break;
   case 2:for(int i=0;i<5;i++)plank(-.4f+i*.2f,(i%2)*.025f,.96f-(i%3)*.05f,.15f,87+i%3*3);plank(0,-.21f,1.02f,.09f,3);break;
   case 3:plank(-.11f,-.33f,.87f,.17f,-5);plank(.13f,-.06f,.84f,.17f,3);plank(-.09f,.26f,.9f,.16f,8);plank(.03f,0,1.25f,.1f,-38);break;
   case 4:plank(-.02f,-.37f,1.05f,.2f,1);plank(.02f,-.1f,1.08f,.17f,-5);plank(-.17f,.25f,.72f,.17f,5);plank(.32f,.23f,.47f,.12f,84);break;
   default:plank(-.36f,0,.99f,.17f,94);plank(-.1f,-.08f,.88f,.18f,87);plank(.17f,.03f,.96f,.14f,92);plank(.39f,.13f,.75f,.15f,85);plank(-.02f,.31f,1.02f,.1f,-7);break;
  }
  var m=new Mesh();m.CombineMeshes(parts.ToArray(),true,true);return SaveMesh(m,"Boarding layout "+style);
 }
 static GameObject Render(Transform parent,string name,Vector3 position,Vector3 scale,Mesh mesh,Material material){var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(parent,false);g.transform.position=position;g.transform.localScale=scale;g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=material;return g;}
 static void Depth(Transform t,float centre,float thickness){foreach(var v in t.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer&&!v.suppressed)){v.depth=thickness/Mathf.Abs(v.transform.lossyScale.z);v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.transform.position.x,v.transform.position.y,centre)).z;v.Sync();EditorUtility.SetDirty(v);}}
 static void Capture(Scene s,Vector3 centre,float halfHeight,string name){
  var g=new GameObject("Temporary window review camera");SceneManager.MoveGameObjectToScene(g,s);var cam=g.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=halfHeight;cam.aspect=2;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.1f,.11f);cam.transform.position=new Vector3(centre.x,centre.y,-18);cam.transform.rotation=Quaternion.identity;cam.farClipPlane=250;
  var target=new RenderTexture(1400,700,24);var previous=RenderTexture.active;var texture=new Texture2D(1400,700,TextureFormat.RGB24,false);
  try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;texture.ReadPixels(new Rect(0,0,1400,700),0,0);texture.Apply();File.WriteAllBytes(Dir+"/"+name+".png",texture.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(texture);Object.DestroyImmediate(g);}
 }
 static void Apply(Scene s){
  var world=All<RecoilPrototype>(s).Single();if(world.transform.Find(Marker)){File.WriteAllText(Dir+"/verification.txt","Already applied");return;}
  string backup="Backups/WindowBoarding/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!EditorSceneManager.SaveScene(s,backup,true))throw new Exception("Backup failed");
  string[] routes=All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();string[] collisions=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds).ToArray();
  Directory.CreateDirectory(AssetsDir);AssetDatabase.Refresh();var source=PartMesh();var variants=Enumerable.Range(0,6).Select(i=>Variant(source,i)).ToArray();
  var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ConflictAftermath/Salvaged barricade timber.mat");var steel=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/ConflictRemnants/Pitted dark iron.mat");if(!wood||!steel)throw new Exception("Shared material missing");
  var marker=new GameObject(Marker).transform;marker.SetParent(world.transform,false);
  var warehouse=All<Transform>(s).Single(t=>t.name=="Factory Warehouse - exterior");var wall=warehouse.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Warehouse wall").GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="3D mesh - Surface - unified industrial material");float front=wall.bounds.min.z;
  var windows=warehouse.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="High wired window").OrderBy(t=>t.position.x).ToArray();if(windows.Length!=4)throw new Exception("Expected 4 warehouse facade windows");
  var cubeGo=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=cubeGo.GetComponent<MeshFilter>().sharedMesh;var frameParts=new List<CombineInstance>();
  foreach(var a in new[]{new Vector3(-.52f,0,0),new Vector3(.52f,0,0),new Vector3(0,-.55f,0),new Vector3(0,.55f,0),Vector3.zero})frameParts.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(a,Quaternion.identity,Mathf.Abs(a.x)>.1f?new Vector3(.045f,1.14f,.12f):Mathf.Abs(a.y)>.1f?new Vector3(1.09f,.085f,.12f):new Vector3(.025f,1,.12f))});
  var fm=new Mesh();fm.CombineMeshes(frameParts.ToArray(),true,true);fm=SaveMesh(fm,"Shared industrial window frame");Object.DestroyImmediate(cubeGo);
  for(int i=0;i<windows.Length;i++){var pane=windows[i];var group=new GameObject("Warehouse boarded window "+(i+1)).transform;group.SetParent(warehouse,false);group.position=new Vector3(pane.position.x,pane.position.y,front-.15f);pane.SetParent(group,true);Depth(pane,front-.085f,.035f);
   foreach(var mull in warehouse.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Window mullion"&&Mathf.Abs(t.position.x-pane.position.x)<.1f).ToArray()){foreach(var v in mull.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}foreach(var sprite in mull.GetComponentsInChildren<SpriteRenderer>(true))sprite.enabled=false;}
   Render(group,"Raised steel frame",group.position,new Vector3(2.2f,1,1),fm,steel);
   Render(group,"Varied timber - shared layout "+i,new Vector3(pane.position.x,pane.position.y,front-.27f),new Vector3(2.2f,1,1),variants[i],wood);
   var glass=pane.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.enabled).bounds;if(glass.max.z>=front-.025f)throw new Exception("Warehouse glass remains inside wall");
  }
  int removed=0,index=0;var offices=All<Transform>(s).Where(t=>t.name.StartsWith("Office window ")).OrderBy(t=>t.position.y).ThenBy(t=>t.position.x).ToArray();
  foreach(var group in offices){var pane=group.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="Dirty office glazing");var glass=pane.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.enabled).bounds;var old=group.Cast<Transform>().Where(t=>t.name=="Salvaged plank across factory glazing").ToArray();if(old.Length==0)continue;var frame=group.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Window centre mullion").GetComponentsInChildren<MeshRenderer>(true).First(r=>r.enabled).bounds;
   int style=(index*5+index/4)%6;Render(group,"Varied timber - shared layout "+style,new Vector3(glass.center.x,glass.center.y,frame.min.z-.05f),new Vector3(glass.size.x,glass.size.y,1),variants[style],wood);foreach(var t in old){Object.DestroyImmediate(t.gameObject);removed++;}index++;
  }
  Object.DestroyImmediate(source);Physics2D.SyncTransforms();if(!routes.SequenceEqual(All<RecoilDoor>(s).Select(d=>d.name+"|"+d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position)))throw new Exception("Door changes");if(!collisions.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds)))throw new Exception("Collision changes");
  Capture(s,new Vector3(wall.bounds.center.x,4,0),8,"Warehouse-windows");if(offices.Length>0)Capture(s,new Vector3(offices.Average(t=>t.position.x),offices.Average(t=>t.position.y),0),10,"Factory-boarding-variants");EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Scene save failed");AssetDatabase.SaveAssets();File.WriteAllText(Dir+"/verification.txt","PASS\n4 warehouse windows brought in front of wall at Z="+front+" with raised frames and wooden boarding.\n"+index+" office windows varied; "+removed+" original plank renderers replaced with "+index+" combined renderers.\n6 shared boarding meshes + 1 shared frame mesh. Existing wood/steel materials reused; no textures, lights or colliders added.\nDoor routes and gameplay collision unchanged.\nBackup: "+backup);
 }

}
