using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Editor-only art pass. Decorative children preserve all collision and interaction transforms.
#pragma warning disable 0618 // Unity 2022 exposes the legacy sprite-sheet importer.
public static class EasternIndustrialExteriorBuilder
{
 const string Art="Assets/RecoilPrototype/Art/EasternIndustrial/";
 static Sprite brick,plaster,steel,glass,door; static Material material;
 static Color Tone(float value){return new Color(value,value,value,1);}
 static void Import(){
  AssetDatabase.ImportAsset(Art+"MaterialAtlas.png",ImportAssetOptions.ForceSynchronousImport);
  var i=(TextureImporter)AssetImporter.GetAtPath(Art+"MaterialAtlas.png");
  i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Multiple;FullRect(i);i.npotScale=TextureImporterNPOTScale.None;i.mipmapEnabled=false;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.Uncompressed;
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"MaterialAtlas.png");var dims=PngDimensions(Art+"MaterialAtlas.png");float w=dims.x/2,h=dims.y/2;i.spritePixelsPerUnit=w/4f;
  i.spritesheet=new[]{Meta("Brick",new Rect(0,h,w,h)),Meta("Plaster",new Rect(w,h,w,h)),Meta("Steel",new Rect(0,0,w,h)),Meta("Glass",new Rect(w,0,w,h))};i.SaveAndReimport();
  var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+"MaterialAtlas.png").OfType<Sprite>().ToArray();brick=sprites.First(s=>s.name=="Brick");plaster=sprites.First(s=>s.name=="Plaster");steel=sprites.First(s=>s.name=="Steel");glass=sprites.First(s=>s.name=="Glass");
  AssetDatabase.ImportAsset(Art+"SteelDoor.png",ImportAssetOptions.ForceSynchronousImport);i=(TextureImporter)AssetImporter.GetAtPath(Art+"SteelDoor.png");i.textureType=TextureImporterType.Sprite;i.spriteImportMode=SpriteImportMode.Multiple;FullRect(i);i.spritePixelsPerUnit=400;i.npotScale=TextureImporterNPOTScale.None;i.mipmapEnabled=false;i.maxTextureSize=2048;
  i.spritesheet=new[]{Meta("IndustrialDoor",DoorRect())};i.SaveAndReimport();door=AssetDatabase.LoadAllAssetsAtPath(Art+"SteelDoor.png").OfType<Sprite>().First();
 }
 static Vector2 PngDimensions(string path){var b=File.ReadAllBytes(path);return new Vector2((b[16]<<24)|(b[17]<<16)|(b[18]<<8)|b[19],(b[20]<<24)|(b[21]<<16)|(b[22]<<8)|b[23]);}
 static Rect DoorRect(){var d=PngDimensions(Art+"SteelDoor.png");return new Rect(Mathf.Round(d.x*.186f),Mathf.Round(d.y*.026f),Mathf.Round(d.x*.626f),Mathf.Round(d.y*.958f));}
 static void FullRect(TextureImporter i){var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);}
 static SpriteMetaData Meta(string name,Rect rect){return new SpriteMetaData{name=name,rect=rect,pivot=new Vector2(.5f,.5f),alignment=(int)SpriteAlignment.Center};}
 static void Surface(SpriteRenderer old,Sprite asset,float tint){
  var existing=old.transform.Find("Surface - unified industrial material");if(existing){existing.GetComponent<SpriteRenderer>().color=Tone(tint);return;}
  Vector3 scale=old.transform.lossyScale;Vector2 size=Vector2.Scale(old.sprite.bounds.size,new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
  var t=new GameObject("Surface - unified industrial material").transform;t.SetParent(old.transform,false);t.localPosition=Vector3.zero;t.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1);
  var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=asset;r.sharedMaterial=old.sharedMaterial;r.sortingLayerID=old.sortingLayerID;r.sortingOrder=old.sortingOrder;r.color=Tone(tint);r.drawMode=SpriteDrawMode.Tiled;r.tileMode=SpriteTileMode.Continuous;r.size=size;
  old.enabled=false;
 }
 static void Door(RecoilDoor d){
  if(d.transform.Find("Unified steel exit - art only"))return;
  var old=d.GetComponentsInChildren<SpriteRenderer>(true);var leaf=old.FirstOrDefault(r=>r.name.Contains("warped")||r.name.Contains("fire door")||r.name.Contains("artwork"));Quaternion rotation=leaf?leaf.transform.rotation:Quaternion.identity;
  foreach(var r in old)if(!r.name.Contains("light")&&!r.name.Contains("Light"))r.enabled=false;
  var t=new GameObject("Unified steel exit - art only").transform;t.SetParent(d.transform,false);t.position=d.transform.position;t.rotation=rotation;
  Vector3 s=t.parent.lossyScale;t.localScale=new Vector3(1.45f/door.bounds.size.x/Mathf.Abs(s.x),2.5f/door.bounds.size.y/Mathf.Abs(s.y),1);
  var sr=t.gameObject.AddComponent<SpriteRenderer>();sr.sprite=door;sr.sharedMaterial=material;sr.sortingOrder=3;sr.color=Tone(d.irreparableDamage?.62f:.95f);
  // Separate material makes the recess legible without turning it into a glowing marker.
  var frame=new GameObject("Recess shadow - art only").transform;frame.SetParent(d.transform,false);frame.position=d.transform.position;
  frame.localScale=new Vector3(1.65f/plaster.bounds.size.x/Mathf.Abs(s.x),2.7f/plaster.bounds.size.y/Mathf.Abs(s.y),1);var f=frame.gameObject.AddComponent<SpriteRenderer>();f.sprite=plaster;f.sharedMaterial=material;f.sortingOrder=2;f.color=Tone(.28f);
 }
 [MenuItem("Recoil/Art/Unify Eastern Industrial Exteriors")]
 public static void Apply(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing scene art.");
  var original=SceneManager.GetActiveScene();var loaded=Enumerable.Range(0,SceneManager.sceneCount).Select(n=>SceneManager.GetSceneAt(n).path).ToArray();
  Import();var scene=SceneManager.GetSceneByPath("Assets/Scenes/RecoilPrototype.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity",OpenSceneMode.Additive);
  string backup="Backups/EasternIndustrialExterior/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Outdoor.unity",true);
  var w=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();var so=new SerializedObject(w);material=(Material)so.FindProperty("sceneMaterial").objectReferenceValue;
  foreach(var r in w.GetComponentsInChildren<SpriteRenderer>(true).ToArray()){
   if(!r.sprite||r.name.StartsWith("Surface -"))continue;string n=r.name;bool factory=r.GetComponentsInParent<Transform>(true).Any(t=>t.name=="Factory Building - four storeys"||t.name=="Factory Warehouse - exterior"||t.name=="Factory right perimeter wall");
   if(n=="Factory main facade"||n=="Warehouse wall")Surface(r,brick,.72f);
   else if(n=="Store facade")Surface(r,plaster,.72f);
   else if(n=="Shop window"||n=="Door glass")Surface(r,glass,.70f);
   else if(n=="Service store entrance"||n=="Roof fascia"||n=="Window crossbar")Surface(r,steel,.86f);
   else if(n=="Left side elevation"||n=="Hard perimeter wall"||n=="Wall cap"||n=="Service roof terrain"||factory&&(n.Contains("concrete")||n.Contains("belt course")||n.Contains("parapet")||n.Contains("sill")||n.Contains("footing")))Surface(r,plaster,.65f);
   else if(factory&&(n.Contains("glazing")||n.Contains("wired window")))Surface(r,glass,.53f);
   else if(factory&&(n.Contains("rail")||n.Contains("stringer")||n.Contains("tread")||n.Contains("landing")||n.Contains("Landing")||n.Contains("Grating")||n.Contains("grating")||n.Contains("jamb")||n.Contains("mullion")||n.Contains("Transom")||n.Contains("column")||n.Contains("rib")||n.Contains("bracket")||n.Contains("canopy")||n.Contains("crate")||n.Contains("cabinet")||n.Contains("Cabinet")||n.Contains("shutter")||n.Contains("Shutter")||n.Contains("slat")))Surface(r,steel,n.Contains("jamb")||n.Contains("mullion")||n.Contains("Transom")?1.15f:n.Contains("tread")?.93f:.90f);
  }
  foreach(var d in w.GetComponentsInChildren<RecoilDoor>(true))if(d.destinationScene=="Assets/Scenes/RecoilFactoryInterior.unity"||d.destinationScene=="Assets/Scenes/RecoilFactoryWarehouse.unity")Door(d);
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(original);
  if(!loaded.Contains(scene.path))EditorSceneManager.CloseScene(scene,true);
  Debug.Log("EASTERN_INDUSTRIAL_EXTERIORS_COMPLETE");
 }
 public static void Capture(float x=50,float y=10,float zoom=12,string file="eastern-industrial-exterior.png"){
  var scene=SceneManager.GetSceneByPath("Assets/Scenes/RecoilPrototype.unity");bool added=!scene.isLoaded;var original=SceneManager.GetActiveScene();if(added)scene=EditorSceneManager.OpenScene("Assets/Scenes/RecoilPrototype.unity",OpenSceneMode.Additive);
  var cam=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();var pos=cam.transform.position;float size=cam.orthographicSize;float aspect=cam.aspect;var target=cam.targetTexture;
  var renderers=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToArray();var states=renderers.Select(r=>r.enabled).ToArray();var rt=new RenderTexture(1800,1000,24);var active=RenderTexture.active;
  try{foreach(var r in renderers)if(r.gameObject.scene!=scene||r.name=="Darkness Layer"||r.GetComponentInParent<RecoilSightOcclusion>())r.enabled=false;
   cam.transform.position=new Vector3(x,y,-10);cam.orthographicSize=zoom;cam.aspect=1.8f;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1800,1000,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,1000),0,0);image.Apply();Directory.CreateDirectory("output/level-previews");File.WriteAllBytes("output/level-previews/"+file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
  }finally{cam.transform.position=pos;cam.orthographicSize=size;cam.aspect=aspect;cam.targetTexture=target;RenderTexture.active=active;for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=states[i];UnityEngine.Object.DestroyImmediate(rt);SceneManager.SetActiveScene(original);if(added)EditorSceneManager.CloseScene(scene,true);}
 }
}
