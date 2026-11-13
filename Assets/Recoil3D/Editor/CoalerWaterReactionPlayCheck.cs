using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
[InitializeOnLoad]
public static class CoalerWaterReactionPlayCheck
{
 const string Key="CoalerWaterReactionPlayCheck.Active";static RecoilPrototype world;static RampagerController ai;static RampagerTentacleVisual art;static Camera cam;static int stage,frames,lastFrame;static double began;static Vector3[] heldVertices;
 static CoalerWaterReactionPlayCheck(){EditorApplication.update+=Tick;}
 public static void Begin(){SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
 static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
 static void Need(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Finish(string result){Directory.CreateDirectory("output/water-reaction");File.WriteAllText("output/water-reaction/play-verification.txt",result);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;}
 static void SetupCoaler(){ai.initialState=RampagerController.Behaviour.Roaming;ai.ResetController();ai.GetComponent<RampagerMovement>().SetNavigationPosition(new Vector3(100,5,0));ai.GetComponent<RampagerMovement>().SetVisualYaw(0);ai.WatchForStory();art.UpdatePose(0,true);}
 static void Tick(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
  try{
   if(began==0)began=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-began>50)throw new Exception("Play check timed out");
   if(stage==0){world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(!world||!world.Player)return;Set(world,"started",true);Set(world,"simTime",100f);Set(world,"<FrameDelta>k__BackingField",.02f);world.enabled=false;world.Player.enabled=false;world.Player.ResetController(new Vector2(105,5));
    var actors=UnityEngine.Object.FindObjectsOfType<RampagerController>();ai=actors.First(a=>a.GetComponent<RampagerTentacleVisual>());foreach(var actor in actors)actor.enabled=false;
    art=ai.GetComponent<RampagerTentacleVisual>();var depth=ai.GetComponent<RampagerDepthArrival>();if(depth)depth.enabled=false;
    cam=Camera.main;var follow=cam.GetComponent<RecoilCameraController>();if(follow)follow.enabled=false;cam.transform.SetPositionAndRotation(new Vector3(101,6.1f,-7),Quaternion.LookRotation(new Vector3(100,5.4f,0)-new Vector3(101,6.1f,-7)));cam.fieldOfView=35;
    var fill=new GameObject("Temporary water reaction verification light").AddComponent<Light>();fill.type=LightType.Point;fill.range=12;fill.intensity=2.5f;fill.transform.position=new Vector3(98,8,-3);
    SetupCoaler();ai.ReceiveWaterImpact(new Vector2(105,4.7f),Vector2.left,10,world.Player.Position);Need(ai.ReactingToWater&&!ai.ProtectingCore,"Ordinary reaction missing");frames=0;lastFrame=Time.frameCount;stage=1;return;
   }
   if(lastFrame==Time.frameCount)return;lastFrame=Time.frameCount;frames++;Set(world,"simTime",world.Clock+.02f);ai.Simulate(.02f);art.UpdatePose(.02f,false);
   if(stage==1){
    if(frames==12)Capture("Water_Guard");if(frames==43)Capture("Water_Shake");
    if(frames<59)Need(ai.ReactingToWater,"Guard expired before1.2s");if(frames<61)return;
    Need(!ai.ReactingToWater&&ai.State==RampagerController.Behaviour.Attack,"Ordinary guard did not become Attack");SetupCoaler();var core=art.CoreWorldPosition;ai.ReceiveWaterImpact(new Vector2(core.x+5,core.y),Vector2.left,10,world.Player.Position);Need(ai.ProtectingCore&&!ai.WaterForcesRed,"Black core reaction missing");art.UpdatePose(.02f,false);Capture("Core_Immediate_Protection");stage=2;frames=0;return;
   }
   if(frames==25)Capture("Core_Layered_Fold_Halfway");
   if(frames==60){Need(ai.CoreStill&&!art.CoreExposed,"Core should be covered in the still section");heldVertices=art.VisualRoot.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;Capture("Core_Black_Still");}
   if(frames==150){Need(ai.CoreStill,"Still interval ended early");var now=art.VisualRoot.GetComponentInChildren<MeshFilter>().sharedMesh.vertices;Need(now.Length==heldVertices.Length,"Held mesh changed");float delta=0;for(int i=0;i<now.Length;i++)delta=Mathf.Max(delta,Vector3.Distance(now[i],heldVertices[i]));Need(delta<.0001f,"Folded pose moved during still interval: "+delta);}
   if(frames==185){Need(ai.CoreAttackProgress>0&&!ai.WaterForcesRed,"Final0.5s transition missing");Capture("Core_Final_Attack_Transition");}
   if(frames<202)return;Need(!ai.ReactingToWater&&ai.State==RampagerController.Behaviour.Attack,"Core recovery failed to reach Attack at4s");Capture("Core_Recovery_Attack");Finish("PASS: actual Play Mode ordinary1.2s guard/shake; black layered1s core fold;2.5s frozen mesh verified;0.5s transition into Attack. No application exit.");

  }catch(Exception e){Finish("FAIL: "+e);}
 }
 static void Capture(string name){
  Directory.CreateDirectory("Captures/WaterReaction");var target=RenderTexture.GetTemporary(900,720,24,RenderTextureFormat.ARGBHalf);var old=cam.targetTexture;var active=RenderTexture.active;Texture2D raw=null,img=null;
  try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;raw=new Texture2D(900,720,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,900,720),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;img=new Texture2D(900,720,TextureFormat.RGB24,false,true);img.SetPixels(pixels);img.Apply();File.WriteAllBytes("Captures/WaterReaction/"+name+".png",img.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=active;RenderTexture.ReleaseTemporary(target);if(raw)UnityEngine.Object.DestroyImmediate(raw);if(img)UnityEngine.Object.DestroyImmediate(img);}
 }
}
