using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class ArchiveClockVisualRebuild
{
 const string Folder="Assets/Art/Environment/ArchiveRoom";
 static Transform Group(string name,Transform p,Vector3 at){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=at;return t;}
 static Mesh cube,cylinder;static Material wood,edge,dark,brass,face,ink;
 static Transform Part(string name,Transform p,Vector3 at,Vector3 size,Material mat,Mesh mesh=null){var t=Group(name,p,at);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh?mesh:cube;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return t;}
 static Transform Disc(string name,Transform p,Vector3 at,float radius,float depth,Material mat){var t=Part(name,p,at,new Vector3(radius,depth,radius),mat,cylinder);t.localRotation=Quaternion.Euler(90,0,0);return t;}
 static Material Plain(string name,Color color,bool unlit=false){var path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find(unlit?"Unlit/Color":"Standard"));AssetDatabase.CreateAsset(m,path);}m.color=color;if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.15f);return m;}
 static Mesh Cylinder()
 {
  var path=Folder+"/Clock unit cylinder 64.asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh)return mesh;
  var vertices=new System.Collections.Generic.List<Vector3>();var triangles=new System.Collections.Generic.List<int>();
  for(int i=0;i<64;i++){float a=i*Mathf.PI/32,b=(i+1)*Mathf.PI/32;var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));int k=vertices.Count;vertices.AddRange(new[]{p+Vector3.down*.5f,p+Vector3.up*.5f,q+Vector3.up*.5f,q+Vector3.down*.5f});triangles.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});k=vertices.Count;vertices.AddRange(new[]{Vector3.up*.5f,p+Vector3.up*.5f,q+Vector3.up*.5f});triangles.AddRange(new[]{k,k+2,k+1});k=vertices.Count;vertices.AddRange(new[]{Vector3.down*.5f,p+Vector3.down*.5f,q+Vector3.down*.5f});triangles.AddRange(new[]{k,k+1,k+2});}
  mesh=new Mesh{name="Clock cylinder - radius one height one"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
 }
 public static string Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(!scene.path.EndsWith("/FactoryArchiveF3.unity"))throw new Exception("Open archive only");
  var clock=Object.FindObjectOfType<WindUpArchiveClock>();if(!clock)throw new Exception("Clock not found");var model=clock.transform.Find("Wooden longcase clock - editable assembly");if(!model)throw new Exception("Assembly not found");
  var rootPose=clock.transform.localToWorldMatrix;var modelPose=model.localToWorldMatrix;var oldOrigin=clock.soundOrigin;var oldAudio=clock.chime;var audioJson=EditorJsonUtility.ToJson(oldAudio);
  cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");cylinder=Cylinder();wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Faded walnut veneer.mat");edge=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Dark wood edges.mat");dark=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Bakelite and rubber.mat");brass=Plain("Clock restrained aged brass",new Color(.34f,.29f,.18f));face=Plain("Clock clean aged ivory dial",new Color(.72f,.69f,.57f));ink=Plain("Clock clear charcoal markings",new Color(.025f,.028f,.023f),true);
  Undo.RegisterFullObjectHierarchyUndo(clock.gameObject,"Rebuild clear small clock dial");
  foreach(var child in model.Cast<Transform>().ToArray())if(child!=oldOrigin)Undo.DestroyObjectImmediate(child.gameObject);
  var body=Group("Clock cabinet and clear small dial - fixed parts",model,Vector3.zero);
  Part("Narrow wooden clock cabinet",body,new Vector3(0,1.05f,.045f),new Vector3(.59f,2.10f,.30f),wood);
  Part("Dark recessed pendulum chamber",body,new Vector3(0,.82f,-.113f),new Vector3(.425f,1.14f,.014f),dark);
  Part("Lower clock plinth",body,new Vector3(0,.075f,0),new Vector3(.69f,.15f,.41f),edge);
  Part("Upper plinth trim",body,new Vector3(0,.17f,-.005f),new Vector3(.62f,.04f,.36f),wood);
  Part("Upper clock cornice",body,new Vector3(0,2.12f,0),new Vector3(.69f,.09f,.40f),edge);
  Part("Cornice top trim",body,new Vector3(0,2.185f,.01f),new Vector3(.61f,.04f,.35f),wood);
  foreach(float x in new[]{-.24f,.24f})Part("Narrow door frame stile",body,new Vector3(x,.83f,-.144f),new Vector3(.05f,1.20f,.055f),edge);
  foreach(float y in new[]{.24f,1.42f})Part("Door frame cross rail",body,new Vector3(0,y,-.145f),new Vector3(.53f,.045f,.055f),wood);
  Part("Wooden dial backing panel",body,new Vector3(0,1.75f,-.131f),new Vector3(.51f,.55f,.035f),edge);
  Disc("Thin brass bezel - 42cm diameter",body,new Vector3(0,1.75f,-.165f),.21f,.028f,brass);
  Disc("Small clock dial - 38cm diameter",body,new Vector3(0,1.75f,-.186f),.191f,.012f,face);
  for(int i=0;i<12;i++){float a=i*Mathf.PI/6;var tick=Part("Bold hour marker "+(i==0?12:i),body,new Vector3(Mathf.Sin(a)*.170f,1.75f+Mathf.Cos(a)*.170f,-.196f),new Vector3(i%3==0?.011f:.007f,i%3==0?.027f:.020f,.003f),ink);tick.localRotation=Quaternion.Euler(0,0,-i*30);}
  var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");foreach(int number in new[]{12,3,6,9}){float a=number*Mathf.PI/6;var label=Group("Large dial numeral "+number,body,new Vector3(Mathf.Sin(a)*.127f,1.75f+Mathf.Cos(a)*.127f,-.201f));var text=label.gameObject.AddComponent<TextMesh>();text.font=font;text.text=number.ToString();text.fontSize=96;text.characterSize=.005f;text.fontStyle=FontStyle.Bold;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=new Color(.035f,.037f,.03f);label.GetComponent<MeshRenderer>().sharedMaterial=font.material;label.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
  var hour=Group("Hour hand pivot",model,new Vector3(0,1.75f,-.213f));hour.localRotation=Quaternion.Euler(0,0,60);Part("Short bold hour hand",hour,new Vector3(0,.045f,0),new Vector3(.014f,.105f,.004f),ink);
  var minute=Group("Minute hand pivot",model,new Vector3(0,1.75f,-.221f));minute.localRotation=Quaternion.Euler(0,0,-60);Part("Long slender minute hand",minute,new Vector3(0,.065f,0),new Vector3(.008f,.145f,.004f),ink);
  Disc("Central hand hub",body,new Vector3(0,1.75f,-.229f),.014f,.008f,brass);
  Disc("Winding key socket",body,new Vector3(.072f,1.689f,-.200f),.009f,.004f,ink);
  foreach(float x in new[]{-.13f,.13f}){Part("Continuous weight suspension chain",body,new Vector3(x,1.12f,-.146f),new Vector3(.004f,.38f,.004f),brass);Part("Long clock drive weight",body,new Vector3(x,.86f,-.146f),new Vector3(.041f,.17f,.046f),brass);}
  var swing=Group("Pendulum suspension - silent until wound",model,new Vector3(0,1.30f,-.183f));Part("Continuous pendulum rod",swing,new Vector3(0,-.39f,0),new Vector3(.009f,.80f,.009f),brass);Disc("Small round pendulum bob",swing,new Vector3(0,-.79f,-.004f),.083f,.018f,brass);
  Part("Clock door latch",body,new Vector3(.237f,.79f,-.186f),new Vector3(.019f,.049f,.022f),brass);
  clock.pendulum=swing;clock.hourHand=hour;clock.minuteHand=minute;EditorUtility.SetDirty(clock);
  if(clock.soundOrigin!=oldOrigin||clock.chime!=oldAudio||audioJson!=EditorJsonUtility.ToJson(oldAudio))throw new Exception("Audio or wave source changed");
  for(int i=0;i<16;i++)if(Mathf.Abs(rootPose[i]-clock.transform.localToWorldMatrix[i])>.0001f||Mathf.Abs(modelPose[i]-model.localToWorldMatrix[i])>.0001f)throw new Exception("Clock placement changed");
  EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Save failed");AssetDatabase.SaveAssets();Selection.activeGameObject=clock.gameObject;
  return "Saved rebuilt clock: dial diameter .382m (old actual .968m); 12 bold ticks, four large cardinal numbers, separated hands; preserved root/model poses, winding script, gong audio and wave origin.";
 }
}
