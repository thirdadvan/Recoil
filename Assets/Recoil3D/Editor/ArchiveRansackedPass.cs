using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// Existing individual records are moved out of the shelf batches. All floor pieces remain editable.
public static class ArchiveRansackedPass
{
 const string Folder="Assets/Art/Environment/ArchiveRoom/Ransacked/",Marker="Ransacked archive - floor records and papers";
 static System.Random random;static Mesh cube;static Material material;static readonly List<Rect> blocked=new List<Rect>(),occupied=new List<Rect>();
 static readonly Dictionary<string,Mesh> variants=new Dictionary<string,Mesh>();
 static float Range(float a,float b){return Mathf.Lerp(a,b,(float)random.NextDouble());}
 static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 sealed class Geometry
 {
  public readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();public readonly List<Color> colors=new List<Color>();public readonly List<int> triangles=new List<int>();
  public void Add(Mesh mesh,Matrix4x4 matrix,Color color){int start=vertices.Count;var v=mesh.vertices;var n=mesh.normals;var nm=matrix.inverse.transpose;if(QualitySettings.activeColorSpace==ColorSpace.Linear)color=color.linear;for(int i=0;i<v.Length;i++){vertices.Add(matrix.MultiplyPoint3x4(v[i]));normals.Add(n.Length==v.Length?nm.MultiplyVector(n[i]).normalized:Vector3.up);colors.Add(color);}foreach(int index in mesh.triangles)triangles.Add(start+index);}
  public void Box(Vector3 center,Vector3 size,Color color){Add(cube,Matrix4x4.TRS(center,Quaternion.identity,size),color);}
  public Mesh Finish(string name){var mesh=new Mesh{name=name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;}
 }
 static Mesh SaveMesh(Mesh mesh,string name){string path=Folder+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}AssetDatabase.CreateAsset(mesh,path);return mesh;}
 static Color Tint(MeshRenderer r){return r.sharedMaterial&&r.sharedMaterial.HasProperty("_Color")?r.sharedMaterial.color:Color.gray;}
 static Mesh RecordMesh(Transform source)
 {
  var parts=source.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&!r.name.StartsWith("Static ")).ToArray();
  var g=new Geometry();foreach(var r in parts)g.Add(r.GetComponent<MeshFilter>().sharedMesh,source.worldToLocalMatrix*r.localToWorldMatrix,Tint(r));
  var mesh=g.Finish("Fallen archive record");var b=mesh.bounds;
  string key=(source.name.StartsWith("Tied")?"Carton":"Binder")+Mathf.RoundToInt(b.size.x*1000)+"-"+Mathf.RoundToInt(b.size.y*1000)+"-"+string.Join("-",parts.Select(r=>r.sharedMaterial.name).Distinct().ToArray());
  if(variants.ContainsKey(key)){Object.DestroyImmediate(mesh);return variants[key];}
  var saved=SaveMesh(mesh,"Record variant "+variants.Count.ToString("D3"));variants[key]=saved;return saved;
 }
 static Bounds GeometryBounds(Transform source)
 {
  bool first=true;var bounds=new Bounds();foreach(var r in source.GetComponentsInChildren<Renderer>(true)){if(!r.enabled||!r.gameObject.activeInHierarchy||r.GetComponent<ScreenLightScattering>())continue;if(first){bounds=r.bounds;first=false;}else bounds.Encapsulate(r.bounds);}return bounds;
 }
 static Rect Footprint(Bounds b,float margin=0){return Rect.MinMaxRect(b.min.x-margin,b.min.z-margin,b.max.x+margin,b.max.z+margin);}
 static Bounds RotatedBounds(Mesh mesh,Quaternion rotation)
 {var b=mesh.bounds;var result=new Bounds(rotation*b.center,Vector3.zero);for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)result.Encapsulate(rotation*(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z))));return result;}
 static bool Place(Transform item,Mesh mesh,Quaternion rotation,Vector3 origin,bool lid=false)
 {
  var b=RotatedBounds(mesh,rotation);
  for(int attempt=0;attempt<6000;attempt++){
   float x,z;
   if(attempt<500&&random.NextDouble()<.65){x=origin.x+Range(-1.7f,1.7f);z=origin.z+(random.NextDouble()<.5?-1:1)*Range(.65f,1.8f);}
   else{x=Range(.55f,13.40f);z=Range(-4.3f,6.42f);}
   var rect=new Rect(x+b.min.x-.025f,z+b.min.z-.025f,b.size.x+.05f,b.size.z+.05f);
   if(rect.xMin<.18f||rect.xMax>13.82f||rect.yMin< -4.6f||rect.yMax>6.58f||rect.Overlaps(new Rect(0,-.43f,14,.86f)))continue;
   if(blocked.Any(r=>r.Overlaps(rect))||occupied.Any(r=>r.Overlaps(rect)))continue;
   item.position=new Vector3(x,.005f-b.min.y,z);item.rotation=rotation;item.localScale=Vector3.one;occupied.Add(rect);return true;
  }return false;
 }
 static void Renderer(Transform root,Mesh mesh){var f=root.GetComponent<MeshFilter>();if(!f)f=root.gameObject.AddComponent<MeshFilter>();f.sharedMesh=mesh;var r=root.GetComponent<MeshRenderer>();if(!r)r=root.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.enabled=true;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;}
 static void RebatchRack(Transform rack)
 {
  var batches=rack.Cast<Transform>().Where(t=>t.name.StartsWith("Static detail - ")).ToArray();foreach(var b in batches)b.GetComponent<MeshRenderer>().enabled=false;
  var parts=rack.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.GetComponent<MeshFilter>()&&!r.name.StartsWith("Static detail - ")).ToArray();
  foreach(var mat in parts.Select(r=>r.sharedMaterial).Distinct()){
   var list=parts.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh{name="Stripped rack - "+rack.name+" - "+mat.name};mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(list.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=rack.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);
   var saved=SaveMesh(mesh,"Stripped "+rack.name.Replace('/','-')+" - "+mat.name.Replace('/','-'));var target=rack.Find("Static detail - "+mat.name);if(!target){target=Group("Static detail - "+mat.name,rack);target.gameObject.AddComponent<MeshFilter>();target.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;}target.GetComponent<MeshFilter>().sharedMesh=saved;target.GetComponent<MeshRenderer>().enabled=true;
  }foreach(var r in parts)r.enabled=false;
 }
 static Mesh Paper(int variant)
 {
  string key="Paper"+variant;if(variants.ContainsKey(key))return variants[key];var g=new Geometry();float width=variant%4==0?.148f:.21f,depth=variant%4==0?.21f:.297f;var page=new Color(.66f+variant*.006f,.63f+variant*.005f,.51f+variant*.004f);g.Box(new Vector3(0,.001f,0),new Vector3(width,.002f,depth),page);
  Color ink=new Color(.25f,.25f,.21f);int lines=variant%3==0?10:6;for(int i=0;i<lines;i++){float length=width*(.45f+((variant+i*3)%7)*.055f);g.Box(new Vector3(-width*.32f+length*.5f,.0023f,-depth*.34f+i*depth*.055f),new Vector3(length,.0004f,.0018f),ink);}
  if(variant%3==0)g.Box(new Vector3(width*.18f,.0024f,depth*.28f),new Vector3(width*.33f,.0004f,.017f),new Color(.36f,.24f,.21f));
  var mesh=SaveMesh(g.Finish("Loose archive page "+variant),"Loose page "+variant.ToString("D2"));variants[key]=mesh;return mesh;
 }
 static Mesh OpenCarton(bool green)
 {
  string key="OpenCarton"+green;if(variants.ContainsKey(key))return variants[key];var g=new Geometry();var c=green?new Color(.29f,.31f,.25f):new Color(.39f,.31f,.22f);
  g.Box(new Vector3(0,.011f,0),new Vector3(.43f,.022f,.42f),c*.78f);foreach(float x in new[]{-.207f,.207f})g.Box(new Vector3(x,.14f,0),new Vector3(.016f,.28f,.42f),c);foreach(float z in new[]{-.202f,.202f})g.Box(new Vector3(0,.14f,z),new Vector3(.40f,.28f,.016f),c);
  g.Box(new Vector3(-.09f,.16f,-.212f),new Vector3(.17f,.055f,.002f),new Color(.62f,.60f,.48f));for(int i=0;i<3;i++)g.Box(new Vector3(-.12f+i*.035f,.16f,-.214f),new Vector3(.019f,.006f,.001f),new Color(.18f,.19f,.16f));
  var mesh=SaveMesh(g.Finish("Opened empty archive carton"),"Open carton "+(green?"green":"brown"));variants[key]=mesh;return mesh;
 }
 static Mesh Lid(){const string key="Lid";if(variants.ContainsKey(key))return variants[key];var g=new Geometry();g.Box(Vector3.zero,new Vector3(.45f,.024f,.44f),new Color(.38f,.31f,.22f));var mesh=SaveMesh(g.Finish("Discarded archive carton lid"),"Discarded carton lid");variants[key]=mesh;return mesh;}
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Use Edit mode.");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Use the single archive scene.");
  var all=Object.FindObjectsOfType<Transform>();if(all.Any(t=>t.name==Marker))throw new Exception("Ransacking pass already applied; edit existing floor objects instead of duplicating them.");
  var racks=all.Where(t=>t.name.Contains("frame shelves and varied records")||t.name.StartsWith("Rear archive rack ")&&t.name.Contains("complete records assembly")).OrderBy(t=>t.name).ToArray();if(racks.Length!=7)throw new Exception("Expected seven archive racks.");
  if(!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))AssetDatabase.CreateFolder("Assets/Art/Environment/ArchiveRoom","Ransacked");random=new System.Random(19781113);cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");blocked.Clear();occupied.Clear();variants.Clear();
  string matPath=Folder+"Shared archive records and paper.mat";material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!material){material=new Material(Shader.Find("Recoil/3D/Archive Record Surface"));material.name="Shared archive records and paper";material.enableInstancing=true;AssetDatabase.CreateAsset(material,matPath);}
  foreach(var box in Object.FindObjectsOfType<BoxCollider>())if(box.name=="Navigation collision - solid prop")blocked.Add(Footprint(box.bounds,.045f));
  foreach(var t in all)if(t.name=="Pedestal desk top"||t.name.Contains("Card index cabinet ")&&t.name.Contains("whole prop")||t.name=="Pulled-out office chair - whole prop"||t.name=="Records trolley - whole prop"){
   var b=t.name=="Pedestal desk top"?t.GetComponent<Renderer>().bounds:GeometryBounds(t);if(b.size.x>.02f&&b.size.z>.02f)blocked.Add(Footprint(b,.06f));
  }
  foreach(var existingFloor in all.Where(t=>t.name=="Floor paper and displaced cartons"))foreach(var r in existingFloor.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled))blocked.Add(Footprint(r.bounds,.012f));
  var archive=all.Single(t=>t.name=="Archive room - editable records and right computer workstation");Undo.RegisterFullObjectHierarchyUndo(archive.gameObject,"Ransack archive shelves and scatter records");var root=Group(Marker,archive);var records=Group("Fallen archive records - individually movable",root);var papers=Group("Scattered papers - individually movable",root);
  int total=0,moved=0,cartons=0,opened=0,lids=0;
  foreach(var rack in racks){
   var files=rack.Cast<Transform>().Where(t=>t.name.StartsWith("Upright bound file ")||t.name.StartsWith("Tied archive carton ")).ToArray();total+=files.Length;
   foreach(bool carton in new[]{true,false}){
    var selection=files.Where(t=>t.name.StartsWith(carton?"Tied archive carton ":"Upright bound file ")).OrderBy(t=>random.NextDouble()-((Mathf.RoundToInt((t.localPosition.y-.176f)/.51f)==1||Mathf.RoundToInt((t.localPosition.y-.176f)/.51f)==3)?.7f:0)).ToArray();int count=Mathf.CeilToInt(selection.Length*(carton?.76f:rack.name.StartsWith("Rear")?.65f:.75f));
    foreach(var item in selection.Take(count)){
     var originalPosition=item.position;var mesh=RecordMesh(item);bool open=carton&&cartons%3==0;if(open)mesh=OpenCarton(cartons%2==0);
     var rotation=Quaternion.Euler(0,Range(-180,180),0)*(carton?Quaternion.Euler(cartons%5==0&&!open?90:0,0,0):Quaternion.Euler(0,0,90));
     var trial=Group("Placement candidate",root);bool placed=Place(trial,mesh,rotation,originalPosition);if(!placed){Object.DestroyImmediate(trial.gameObject);throw new Exception("No non-intersecting floor placement for "+item.name);}
     var at=trial.position;var q=trial.rotation;Object.DestroyImmediate(trial.gameObject);item.SetParent(records,true);item.position=at;item.rotation=q;item.localScale=Vector3.one;foreach(var r in item.GetComponentsInChildren<MeshRenderer>(true))r.enabled=false;Renderer(item,mesh);item.name=(carton?(open?"Opened empty carton ":"Fallen carton "):"Fallen bound record ")+(++moved).ToString("D3");
     if(carton){cartons++;if(open){opened++;var lid=Group("Discarded carton lid - separately movable",records);var lidMesh=Lid();if(Place(lid,lidMesh,Quaternion.Euler(0,Range(-180,180),0),at,true)){Renderer(lid,lidMesh);lid.SetParent(item,true);lids++;}else Object.DestroyImmediate(lid.gameObject);}}
    }
   }RebatchRack(rack);
  }
  int paperCount=0;var paperRects=new List<Rect>();var paperTops=new List<float>();
  Vector2[] spill={new Vector2(2.6f,-1.0f),new Vector2(5.2f,2.75f),new Vector2(8.1f,3.5f),new Vector2(10.5f,-.75f),new Vector2(7.2f,5.75f)};
  for(int i=0;i<560;i++){
   var mesh=Paper(i%12);var rotation=Quaternion.Euler(0,Range(-180,180),0);var b=RotatedBounds(mesh,rotation);bool placed=false;Vector3 position=Vector3.zero;
   for(int attempt=0;attempt<3000;attempt++){
    var centre=spill[random.Next(spill.Length)];bool cluster=attempt<500&&random.NextDouble()<.78;float x=cluster?centre.x+Range(-1.4f,1.4f):Range(.4f,13.5f),z=cluster?centre.y+Range(-.8f,.8f):Range(-4.4f,6.45f);
    var rect=new Rect(x+b.min.x,z+b.min.z,b.size.x,b.size.z);if(rect.xMin<.15f||rect.xMax>13.85f||rect.yMin< -4.6f||rect.yMax>6.6f||blocked.Any(r=>r.Overlaps(rect))||occupied.Any(r=>r.Overlaps(rect)))continue;
    int overlap=paperRects.Count(r=>r.Overlaps(rect));if(overlap>5)continue;float bottom=.004f;for(int j=0;j<paperRects.Count;j++)if(paperRects[j].Overlaps(rect))bottom=Mathf.Max(bottom,paperTops[j]+.0008f);position=new Vector3(x,bottom-b.min.y,z);paperRects.Add(rect);paperTops.Add(bottom+b.size.y);placed=true;break;
   }
   if(!placed)continue;var sheet=Group("Loose archive sheet "+(++paperCount).ToString("D3"),papers);sheet.position=position;sheet.rotation=rotation;Renderer(sheet,mesh);
  }
  // Runtime static batching retains the individual hierarchy and source meshes for editing.
  foreach(var r in root.GetComponentsInChildren<MeshRenderer>())if(r.enabled)GameObjectUtility.SetStaticEditorFlags(r.gameObject,StaticEditorFlags.BatchingStatic);
  EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);System.IO.Directory.CreateDirectory("output/archive-ransacked");
  string report="Moved "+moved+" of "+total+" shelf records; cartons on floor="+cartons+", visibly opened="+opened+", detached lids="+lids+"; new loose sheets="+paperCount+". Existing eight editable pages retained. Ground props use one instanced shared material, cached meshes and static batching. Shelf/clock collision footprints and Z -0.43..0.43 bulky-prop-free walking lane retained.";
  System.IO.File.WriteAllText("output/archive-ransacked/BuildReport.txt",report);return report;
 }
}
