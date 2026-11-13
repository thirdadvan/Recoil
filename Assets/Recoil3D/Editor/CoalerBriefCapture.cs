using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
public static class CoalerBriefCapture
{
 static Scene stage;static Camera cam;static GameObject actor;static RampagerTentacleVisual art;static Vector3 origin=new Vector3(1000,1000,1000);const string Folder="output/CoalerBrief/.build/images/";
 static void Set(string name,object value){typeof(RampagerTentacleVisual).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(art,value);}
 static Transform Piece(string name,Transform parent,Vector3 p,Vector3 scale,Material m,PrimitiveType type){var t=GameObject.CreatePrimitive(type).transform;t.name=name;t.SetParent(parent,false);t.localPosition=p;t.localScale=scale;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
 static void Pose(RampagerTentacleVisual.Form form,float energy=0){art.previewTarget=null;art.previewSoundEnergy=energy;art.SetPreview(form);Set("clock",2.1f);art.UpdatePose(0,true);}
 static void Shot(string name,Vector3 camera,Vector3 target,float fov=33){cam.transform.position=origin+camera;cam.transform.LookAt(origin+target);cam.fieldOfView=fov;var rt=RenderTexture.GetTemporary(1200,1000,24,RenderTextureFormat.ARGBHalf);var prev=RenderTexture.active;Texture2D raw=null,img=null;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1200,1000,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1200,1000),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(1200,1000,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();File.WriteAllBytes(Folder+name+".png",img.EncodeToPNG());}finally{cam.targetTexture=null;RenderTexture.active=prev;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(img)Object.DestroyImmediate(img);}}
 static void Light(string name,Vector3 p,Color colour,float intensity){var l=new GameObject(name).AddComponent<Light>();SceneManager.MoveGameObjectToScene(l.gameObject,stage);l.type=LightType.Directional;l.color=colour;l.intensity=intensity;l.transform.rotation=Quaternion.LookRotation(origin+Vector3.up-p);}
 public static string Capture(){if(EditorApplication.isPlaying)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);var old=SceneManager.GetActiveScene();bool fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;stage=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var materials=new System.Collections.Generic.List<Material>();try{
 SceneManager.SetActiveScene(stage);RenderSettings.fog=false;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.19f,.21f,.24f);
 actor=new GameObject("Current Coaler procedural prototype");SceneManager.MoveGameObjectToScene(actor,stage);actor.transform.position=origin+Vector3.up*.8f;art=actor.AddComponent<RampagerTentacleVisual>();art.followAI=false;art.autoCycle=false;
 var floor=new Material(Shader.Find("Standard"));floor.color=new Color(.12f,.135f,.14f);floor.SetFloat("_Glossiness",.12f);materials.Add(floor);var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(ground,stage);ground.transform.position=origin+new Vector3(0,-.14f,0);ground.transform.localScale=new Vector3(200,.1f,200);ground.GetComponent<Renderer>().sharedMaterial=floor;ground.GetComponent<Renderer>().enabled=false;
 cam=new GameObject("Brief camera").AddComponent<Camera>();SceneManager.MoveGameObjectToScene(cam.gameObject,stage);cam.scene=stage;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.07f,.085f,.09f);cam.nearClipPlane=.05f;cam.farClipPlane=100;cam.allowHDR=true;
 Light("Soft key",origin+new Vector3(-3,5,-4),new Color(.91f,.95f,1),1.45f);Light("Warm fill",origin+new Vector3(4,2,-2),new Color(1,.83f,.68f),.58f);Light("Cool rim",origin+new Vector3(0,4,4),new Color(.65f,.79f,1),1.8f);
 Pose(RampagerTentacleVisual.Form.Roaming);Shot("cover",new Vector3(3.1f,2.05f,-6.7f),new Vector3(0,1.12f,0));Shot("front",new Vector3(0,1.45f,-6.7f),new Vector3(0,1.12f,0));Shot("back",new Vector3(0,1.6f,6.7f),new Vector3(0,1.12f,0));Shot("side",new Vector3(6.4f,1.65f,-.8f),new Vector3(0,1.12f,0));Shot("proportion_side",new Vector3(-6.4f,1.65f,-.8f),new Vector3(0,1.12f,0));
 Pose(RampagerTentacleVisual.Form.Watching);Shot("cavity",new Vector3(.65f,1.65f,-3.3f),new Vector3(0,1.50f,0),36);Shot("watching",new Vector3(1.6f,1.65f,-6.6f),new Vector3(0,1.1f,0));
 Pose(RampagerTentacleVisual.Form.Dormant);Shot("dormant",new Vector3(2.4f,3.0f,-5.3f),new Vector3(0,.35f,0),38);
 Pose(RampagerTentacleVisual.Form.Awakening);Set("clock",1.8f);art.UpdatePose(0,true);Shot("awakening",new Vector3(2.2f,1.9f,-6),new Vector3(0,.8f,0),38);
 Pose(RampagerTentacleVisual.Form.Idle);Shot("idle",new Vector3(1.6f,1.65f,-7),new Vector3(0,1.1f,0),40);
 Pose(RampagerTentacleVisual.Form.Roaming);Set("gaitStrength",1f);Set("stride",1.1f);art.UpdatePose(0,true);Shot("roaming",new Vector3(2.8f,1.65f,-6.6f),new Vector3(0,1.1f,0));
 Pose(RampagerTentacleVisual.Form.Approaching);Shot("approaching",new Vector3(1.4f,1.7f,-7),new Vector3(0,1.1f,-.15f),38);
 foreach(float energy in new[]{25f,50f,75f,100f}){Pose(RampagerTentacleVisual.Form.Approaching,energy);Shot("sound"+energy,new Vector3(1.4f,1.7f,-7),new Vector3(0,1.1f,-.15f),38);}
 Pose(RampagerTentacleVisual.Form.Enraged,100);Shot("enraged",new Vector3(1.5f,1.7f,-7),new Vector3(0,1.2f,0),38);Shot("enraged_side",new Vector3(6,1.9f,-2.0f),new Vector3(0,1.2f,0),38);
 var human=new GameObject("Grey target placeholder").transform;SceneManager.MoveGameObjectToScene(human.gameObject,stage);human.position=origin+new Vector3(1.55f,0,-.55f);var grey=new Material(Shader.Find("Standard"));grey.color=new Color(.46f,.48f,.46f);materials.Add(grey);Piece("Body",human,new Vector3(0,.95f,0),new Vector3(.33f,.70f,.23f),grey,PrimitiveType.Capsule);Piece("Head",human,new Vector3(0,1.54f,0),new Vector3(.27f,.32f,.27f),grey,PrimitiveType.Sphere);foreach(float x in new[]{-.1f,.1f})Piece("Leg",human,new Vector3(x,.34f,0),new Vector3(.15f,.35f,.17f),grey,PrimitiveType.Capsule);foreach(float x in new[]{-.25f,.25f})Piece("Arm",human,new Vector3(x,1.02f,0),new Vector3(.12f,.33f,.12f),grey,PrimitiveType.Capsule);
 foreach(float amount in new[]{.15f,.55f,1f}){Pose(RampagerTentacleVisual.Form.Fusion);art.previewTarget=human;art.fusionProgress=amount;art.UpdatePose(0,true);Shot("fusion"+Mathf.RoundToInt(amount*100),new Vector3(3.2f,2.05f,-7.7f),new Vector3(.65f,1.15f,-.3f),38);}
 return "22 prototype screenshots captured";
 }finally{EditorSceneManager.CloseScene(stage,true);SceneManager.SetActiveScene(old);RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;RenderSettings.ambientMode=mode;foreach(var m in materials)if(m)Object.DestroyImmediate(m);}}
}
