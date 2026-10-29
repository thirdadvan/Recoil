using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckpointSearchlightReadabilityPass
{
 const string ScenePath="Assets/Scenes/3D/RecoilPrototype.unity";
 public static void Configure(CheckpointSearchlight sweep)
 {
  var light=sweep.searchlight;
  var profile=light.GetComponent<VolumetricLightProfile>();if(!profile)profile=light.gameObject.AddComponent<VolumetricLightProfile>();
  profile.scatteringMultiplier=.22f;profile.fadeAboveHeight=true;profile.maximumVisibleHeight=8;profile.heightFeather=5;
  profile.shelters=sweep.shelters.OfType<BoxCollider>().Take(2).ToArray();
  light.intensity=11;light.shadowStrength=1;light.shadowResolution=UnityEngine.Rendering.LightShadowResolution.High;light.shadowBias=.015f;light.shadowNormalBias=.045f;
  EditorUtility.SetDirty(profile);EditorUtility.SetDirty(light);
 }
 public static void Apply()
 {
  if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play mode before saving searchlight changes");
  var original=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(ScenePath);bool added=!s.isLoaded;
  if(added)s=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
  var world=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).Single();
  var sweep=world.GetComponentInChildren<CheckpointSearchlight>();if(!sweep)throw new Exception("Checkpoint searchlight not found");
  var cam=world.ViewCamera;if(!cam)cam=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>()).First(c=>c.CompareTag("MainCamera"));
  world.GetComponent<Recoil3DWorld>().ApplyAtmosphere();var follow=cam.GetComponent<RecoilCameraController>();
  var position=cam.transform.position;var rotation=cam.transform.rotation;float fov=cam.fieldOfView;var playerPosition=world.Player.Position;
  bool followEnabled=follow.enabled;float phase=sweep.previewPhase;
  Directory.CreateDirectory("Captures/SearchlightReadability");
  try
  {
   var profile=sweep.searchlight.GetComponent<VolumetricLightProfile>();if(profile)profile.enabled=false;
   if(!File.Exists("Captures/SearchlightReadability/Before_West.png")){Capture(cam,follow,world,sweep,.125f,141,"Before_West");Capture(cam,follow,world,sweep,.25f,157,"Before_Bridge");}
   Configure(sweep);profile=sweep.searchlight.GetComponent<VolumetricLightProfile>();profile.enabled=true;
   Capture(cam,follow,world,sweep,.125f,141,"After_West");Capture(cam,follow,world,sweep,.25f,157,"After_Bridge");Capture(cam,follow,world,sweep,.38f,176,"After_East");
   Verify(sweep);
  }
  finally
  {
   world.Player.Position=playerPosition;world.Player.SyncVisuals(0);cam.transform.SetPositionAndRotation(position,rotation);cam.fieldOfView=fov;follow.enabled=followEnabled;sweep.Pose(phase);
  }
  EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);AssetDatabase.SaveAssets();
  if(original.IsValid()&&original.isLoaded&&original!=s)SceneManager.SetActiveScene(original);
  if(added)EditorSceneManager.CloseScene(s,true);
  Debug.Log("SEARCHLIGHT_READABILITY_PASS: weaker world-height-bounded fog, shelter shadows in volume, stronger surface spot, actual gameplay camera captures");
 }
 static void Capture(Camera cam,RecoilCameraController follow,RecoilPrototype world,CheckpointSearchlight sweep,float phase,float x,string name)
 {
  world.Player.Position=new Vector2(x,.845f);world.Player.SyncVisuals(0);follow.enabled=true;follow.SnapToTarget();follow.enabled=false;sweep.Pose(phase);Physics.SyncTransforms();
  // Use the authored gameplay projection and framing, not an elevated art-review camera.
  var previous=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(1440,810,24,RenderTextureFormat.ARGBHalf);Texture2D raw=null,output=null;
  try
  {
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1440,810,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1440,810),0,0);raw.Apply();var pixels=raw.GetPixels();
   if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;
   output=new Texture2D(1440,810,TextureFormat.RGB24,false,true);output.SetPixels(pixels);output.Apply();File.WriteAllBytes("Captures/SearchlightReadability/"+name+".png",output.EncodeToPNG());
  }
  finally{cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);if(raw)UnityEngine.Object.DestroyImmediate(raw);if(output)UnityEngine.Object.DestroyImmediate(output);}
 }
 static bool ShaderBoxOccludes(BoxCollider box,Vector3 light,Vector3 point)
 {
  var matrix=Matrix4x4.Translate(-box.center)*box.transform.worldToLocalMatrix;var a=matrix.MultiplyPoint3x4(light);var delta=matrix.MultiplyPoint3x4(point)-a;var extent=box.size*.5f;
  float enter=float.NegativeInfinity,exit=float.PositiveInfinity;
  for(int i=0;i<3;i++){float inverse=(delta[i]>=0?1:-1)/Mathf.Max(Mathf.Abs(delta[i]),.00001f);float ta=(-extent[i]-a[i])*inverse,tb=(extent[i]-a[i])*inverse;enter=Mathf.Max(enter,Mathf.Min(ta,tb));exit=Mathf.Min(exit,Mathf.Max(ta,tb));}
  return exit>Mathf.Max(enter,0)&&enter<.999f&&exit>0;
 }
 static void Verify(CheckpointSearchlight sweep)
 {
  var profile=sweep.searchlight.GetComponent<VolumetricLightProfile>();int samples=0;
  foreach(float phase in new[]{.05f,.125f,.25f,.38f,.45f}){
   sweep.Pose(phase);for(float x=132;x<184;x+=.2f){var p=new Vector3(x,1,0);if(!sweep.Illuminates(p,false))continue;
    bool blocked=profile.shelters.Any(c=>ShaderBoxOccludes(c,sweep.searchlight.transform.position,p));
    if(blocked==sweep.Illuminates(p))throw new Exception("Volume and gameplay shelter disagree at "+p);samples++;
   }
  }
  if(samples==0)throw new Exception("No sampled spotlight footprint");
  File.WriteAllText("Captures/SearchlightReadability/Verification.txt","PASS: "+samples+" samples; shader OBB shadow segments match actual shelter detection.\nNo range, sweep timing or detection-delay changes. Other lamps default to unmodified volume settings.\nScattering multiplier .22; world height fade 3..8m; native surface light 11; two actual booth colliders cast volumetric shelter shadows.\n");
 }
}
