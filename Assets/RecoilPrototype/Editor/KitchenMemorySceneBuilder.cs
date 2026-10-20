using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class KitchenMemorySceneBuilder
{
 [MenuItem("Recoil/Story/Create Kitchen Death Memory")]
 public static void Build(){
  if(EditorApplication.isPlaying)throw new Exception("Exit Play before scene creation.");if(File.Exists(RecoilPrototype.KitchenMemoryScene))throw new Exception("Kitchen scene exists; edit the saved scene.");
  var active=SceneManager.GetActiveScene();var s=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(s);
  new GameObject("Kitchen memory - childhood apartment").AddComponent<KitchenMemoryController>();
  var cam=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;
  var light=new GameObject("Main Light",typeof(Light)).GetComponent<Light>();light.type=LightType.Directional;light.intensity=.15f;
  EditorSceneManager.SaveScene(s,RecoilPrototype.KitchenMemoryScene);var list=EditorBuildSettings.scenes.ToList();if(!list.Any(x=>x.path==RecoilPrototype.KitchenMemoryScene)){list.Add(new EditorBuildSettingsScene(RecoilPrototype.KitchenMemoryScene,true));EditorBuildSettings.scenes=list.ToArray();}
  SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(s,true);KitchenMemoryGraphicRefit.Apply();
 }
 public static void Capture(bool bedroomClosed=false,string file="kitchen-memory.png"){
  var active=SceneManager.GetActiveScene();var s=EditorSceneManager.OpenScene(RecoilPrototype.KitchenMemoryScene,OpenSceneMode.Additive);var logic=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<KitchenMemoryController>()).First();var cam=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)).First();if(bedroomClosed){logic.TalkToMother();for(int n=0;n<300;n++)logic.AdvanceMother(.05f);}
  var renderers=Enumerable.Range(0,SceneManager.sceneCount).SelectMany(i=>SceneManager.GetSceneAt(i).GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)).ToArray();var enabled=renderers.Select(r=>r.enabled).ToArray();var target=cam.targetTexture;float aspect=cam.aspect,size=cam.orthographicSize;var rt=new RenderTexture(1800,700,24);var old=RenderTexture.active;
  try{foreach(var r in renderers)if(r.gameObject.scene!=s)r.enabled=false;cam.aspect=1800f/700;cam.orthographicSize=Mathf.Max(3.3f,12.4f/cam.aspect);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1800,700,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1800,700),0,0);image.Apply();Directory.CreateDirectory("output/level-previews");File.WriteAllBytes("output/level-previews/"+file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
  finally{cam.targetTexture=target;cam.aspect=aspect;cam.orthographicSize=size;RenderTexture.active=old;for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].enabled=enabled[i];UnityEngine.Object.DestroyImmediate(rt);SceneManager.SetActiveScene(active);EditorSceneManager.CloseScene(s,true);}
 }
}
