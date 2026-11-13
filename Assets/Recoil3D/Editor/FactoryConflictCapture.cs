using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class FactoryConflictCapture
{
 static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();}
 public static void Capture(){
  Directory.CreateDirectory("Captures/FactoryConflict");var original=SceneManager.GetActiveScene();
  foreach(var path in new[]{"Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/3D/FactoryRooms/FactoryWashroomF2.unity","Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity","Assets/Scenes/3D/RecoilPrototype.unity"}){
   var s=SceneManager.GetSceneByPath(path);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
   try{
    if(s.name=="RecoilFactoryInterior"){Shot(s,"Rest_Area_And_Wash_Bay",new Vector3(22.5f,10.7f,-11.5f),new Vector3(22.5f,9.25f,4.8f),43);Shot(s,"Wash_Bay_Detail",new Vector3(25,10.6f,-5),new Vector3(25,9.0f,5),42);Shot(s,"Temporary_Treatment_And_Posters",new Vector3(18.7f,9.8f,-7.5f),new Vector3(19,8.5f,3.0f),42);}
    else if(s.name=="FactoryWashroomF2")Shot(s,"Washroom_Bloodied_Laundry",new Vector3(4.8f,3.1f,-9),new Vector3(4.8f,1.2f,2.8f),43);
    else if(s.name=="FactoryChangingRoomF2")Shot(s,"Changing_Room_Personal_Effects",new Vector3(5.5f,3.5f,-12),new Vector3(5.5f,1.4f,3.0f),43);
    else {Shot(s,"Factory_Entrance_Cleared_Barricade",new Vector3(57,8,-27),new Vector3(57,5,3),55);Shot(s,"Compound_Gate_Barricade",new Vector3(111,5,-24),new Vector3(111,2,2),50);}
   }finally{if(opened)EditorSceneManager.CloseScene(s,true);}
  }
  SceneManager.SetActiveScene(original);var w=All<Recoil3DWorld>(original).FirstOrDefault();if(w)w.ApplyAtmosphere();
 }
 static void Shot(Scene s,string name,Vector3 p,Vector3 target,float fov){
  var cam=All<Camera>(s).First(c=>c.CompareTag("MainCamera"));var oldScene=cam.scene;var position=cam.transform.position;var rotation=cam.transform.rotation;float field=cam.fieldOfView;var follow=cam.GetComponent<RecoilCameraController>();bool enabled=follow&&follow.enabled;
  var otherRenderers=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Where(other=>other!=s&&other.isLoaded).SelectMany(other=>All<Renderer>(other)).Where(r=>r.enabled).ToArray();var otherLights=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).Where(other=>other!=s&&other.isLoaded).SelectMany(other=>All<Light>(other)).Where(l=>l.enabled).ToArray();
  try{
   foreach(var r in otherRenderers)r.enabled=false;foreach(var l in otherLights)l.enabled=false;var world=All<Recoil3DWorld>(s).First();world.ApplyAtmosphere();var look=cam.GetComponent<WarehouseAtmosphereTest>();if(look)look.ApplyLook();if(follow)follow.enabled=false;cam.scene=s;cam.transform.position=p;cam.transform.LookAt(target);cam.fieldOfView=fov;
   var rt=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGBHalf);var prev=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,img=null;
   try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1440,900,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1440,900),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(1440,900,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();File.WriteAllBytes("Captures/FactoryConflict/"+name+".png",img.EncodeToPNG());}
   finally{cam.targetTexture=prev;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(img)Object.DestroyImmediate(img);}
  }finally{cam.scene=oldScene;cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=field;if(follow)follow.enabled=enabled;foreach(var r in otherRenderers)if(r)r.enabled=true;foreach(var l in otherLights)if(l)l.enabled=true;}
 }
}
