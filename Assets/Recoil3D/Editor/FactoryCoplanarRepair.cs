using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class FactoryCoplanarRepair
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 static string Full(Transform t){return t.parent?Full(t.parent)+"/"+t.name:t.name;}
 static void WorldZ(Recoil3DSpriteVisual v,float z){v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.meshRenderer.bounds.center.x,v.meshRenderer.bounds.center.y,z)).z;v.Sync();EditorUtility.SetDirty(v);}
 public static void Apply(){
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");const string path="Assets/Scenes/3D/RecoilFactoryInterior.unity";
  var original=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
  var report=new List<string>();string backup="Backups/FactoryCoplanarRepair/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
  try{
   EditorSceneManager.SaveScene(s,backup+"/RecoilFactoryInterior.unity",true);
   // Restore the exact visual objects retired by the previous pass. Sources,
   // colliders and decorative ownership never need to be reconstructed or deleted.
   foreach(var v in All<Recoil3DSpriteVisual>(s).Where(v=>v.suppressed&&((v.name=="Edge"&&v.transform.parent&&(v.transform.parent.name=="Factory Floor"||v.transform.parent.name=="Left Factory Wall"||v.transform.parent.name=="Right Factory Wall"||v.transform.parent.name=="Low Factory Ceiling"))||v.name=="Steel landing deck"&&Full(v.transform).Contains("Left fire escape")))){
    if(!v.meshRenderer){var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="3D mesh - "+v.name;Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(v.transform,false);var r=g.GetComponent<MeshRenderer>();var sibling=All<Recoil3DSpriteVisual>(s).First(x=>x.name=="Factory Floor"&&x.meshRenderer);r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/Sprite state mesh.mat");var block=new MaterialPropertyBlock();block.SetColor("_Color",v.source.color*v.tint);r.SetPropertyBlock(block);v.meshRenderer=r;var size=v.source.drawMode==SpriteDrawMode.Simple?(Vector2)v.source.sprite.bounds.size:v.source.size;g.transform.localScale=new Vector3(size.x,size.y,v.depth/Mathf.Max(.001f,Mathf.Abs(v.transform.lossyScale.z)));g.transform.localPosition=new Vector3(v.source.sprite.bounds.center.x,v.source.sprite.bounds.center.y,v.depthOffset);}
    v.suppressed=false;v.Sync();
    // A raised trim lip is still present, but no longer has an identical exposed top face.
    var p=v.meshRenderer.transform.localPosition;p.y+=.018f/Mathf.Max(.001f,Mathf.Abs(v.transform.lossyScale.y));v.meshRenderer.transform.localPosition=p;EditorUtility.SetDirty(v);report.Add("Restored with 18mm visual lip: "+Full(v.transform));
   }
   foreach(var v in All<Recoil3DSpriteVisual>(s).Where(v=>v.meshRenderer&&!v.suppressed)){
    if(v.name=="Open alcove wall"){
     // Both plaster walls had front Z=6.8. Keep both pieces; separate the inset back wall.
     WorldZ(v,7.045f);report.Add("Alcove wall recessed 45mm: "+Full(v.transform));
     foreach(var t in v.transform.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Wear from wall base and fittings")){var p=t.position;p.z=6.825f;t.position=p;}
    }
    string parent=v.transform.parent?v.transform.parent.name:"";
    if(v.name=="Screen"&&parent=="Desk and chair"){WorldZ(v,4.628f);report.Add("Terminal screen front offset 22mm: "+Full(v.transform));}
    if(v.name=="Corridor dado"){WorldZ(v,6.64f);report.Add("Dado attached to actual rear wall, removing overlap with water cooler and reception counter");}
    if(v.name=="Corridor skirting"){WorldZ(v,6.49f);report.Add("Skirting placed in front of dado with distinct depth");}
    if(v.name=="Water cooler"&&parent=="Open break and drinking alcove"){WorldZ(v,2.72f);report.Add("Water cooler separated from corridor dado");}
    if(v.name=="Water bottle"&&parent=="Open break and drinking alcove")WorldZ(v,2.69f);
    // Flat 2D office furniture was extruded to the same .24m depth for every part.
    // Give each layer a physical front surface instead of deleting the furniture.
    if(v.name=="Drawer pedestal"||v.name=="Desk leg") {WorldZ(v,4.68f);report.Add("Desk support recessed 30mm: "+Full(v.transform));}
    if(v.name=="Chair back"){WorldZ(v,4.72f);report.Add("Chair separated from desk front: "+Full(v.transform));}
   }
   // The deep extensions overlapped floor slabs over Z=6..11. Join them at the slab's back edge.
   foreach(var r in All<MeshRenderer>(s).Where(r=>r.name=="Upper floor extends into office depth")){
    var b=r.bounds;if(b.min.z<10.99f){float end=b.max.z;var scale=r.transform.localScale;scale.z*=Mathf.Max(.01f,end-11.025f)/b.size.z;r.transform.localScale=scale;var p=r.transform.position;p.z=(end+11.025f)*.5f;r.transform.position=p;report.Add("Joined deep floor extension at Z11.025: "+Full(r.transform));}
   }
   EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);Directory.CreateDirectory("output/water-reaction");File.WriteAllText("output/water-reaction/geometry-repair.txt",string.Join("\n",report)+"\nBackup: "+backup);
  }finally{if(opened)EditorSceneManager.CloseScene(s,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
 }

 public static void DiagnoseShadows(){
  var s=SceneManager.GetActiveScene();var d=All<RecoilDoor>(s).First(x=>x.name=="CHANGING ROOM");var offset=d.interactionOffset;var lights=All<Light>(s);var shadows=lights.Select(l=>l.shadows).ToArray();var capture=typeof(IndoorExitReviewPass).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
  try{d.interactionOffset+=new Vector2(6,0);capture.Invoke(null,new object[]{s,d,"Coplanar_And_Shadow_Before",0f});foreach(var l in lights)l.shadows=LightShadows.None;capture.Invoke(null,new object[]{s,d,"Surface_Without_Shadow_Diagnostic",0f});}
  finally{d.interactionOffset=offset;for(int i=0;i<lights.Length;i++)lights[i].shadows=shadows[i];}
 }
}
