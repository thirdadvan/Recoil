using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class KBKStackerCraneValidation
{
 static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(o,v);}
 static void Require(bool v,string msg){if(!v)throw new Exception(msg);}
 public static void BuildAndValidate(){KBKStackerCraneBuilder.Build();Validate();}
 public static void Validate()
 {
  var scene=EditorSceneManager.OpenScene("Assets/Scenes/RecoilFactoryWarehouse.unity",OpenSceneMode.Single);var roots=scene.GetRootGameObjects();var w=roots.SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();typeof(RecoilPrototype).GetMethod("Awake",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(w,null);
  var p=w.Player;var c=w.GetComponentInChildren<KBKStackerCrane>();Require(c,"Crane missing");if(!string.IsNullOrEmpty(c.requiredPowerItem)){Require(!c.HasPower&&!c.Activate(),"Unpowered crane activated");w.SetProgressFlag(c.requiredPowerItem,true);}Physics2D.SyncTransforms();Require(Mathf.Abs(c.forkPlatform.Area.yMax)<.001f,"Deck is not flush with ground");Require(c.forkPlatform.GetComponent<BoxCollider2D>().isTrigger && !RecoilSightOcclusion.IsBlocker(c.forkPlatform.GetComponent<BoxCollider2D>()),"Deck is not a transparent soft platform");
  p.ResetController(new Vector2(c.forkPlatform.Area.center.x,p.BodyHeight/2));for(int i=0;i<8;i++){Set(w,"simTime",w.Clock+.02f);p.Simulate(0,0,false,false,.02f);}Require(p.Grounded,"Player not supported before lift");
  var camera=roots.SelectMany(r=>r.GetComponentsInChildren<Camera>()).First();camera.GetComponent<RecoilCameraController>().enabled=false;camera.orthographic=true;camera.orthographicSize=10;camera.aspect=1.6f;camera.transform.position=new Vector3(15,3.6f,-10);
  foreach(var l in w.GetComponentsInChildren<RecoilLighting2D>())l.enabled=false;foreach(var s in w.GetComponentsInChildren<RecoilSightOcclusion>())s.enabled=false;foreach(var r in w.GetComponentsInChildren<SpriteRenderer>())if(r.name=="Darkness Layer")r.enabled=false;
  Directory.CreateDirectory("output/level-previews/kbk-frames");Capture(camera,0);
  Require(c.Activate(),"Switch did not activate");Require(!c.Activate(),"Busy crane restarted");int steps=0,frame=1;bool raised=false,traveled=false;
  while(c.Busy && steps<600)
  {
   Set(w,"simTime",w.Clock+.02f);c.Simulate(.02f,w);p.Simulate(0,0,false,false,.02f);steps++;
   Require(Mathf.Abs(p.Position.y-p.BodyHeight/2-c.forkPlatform.Area.yMax)<.04f,"Rider detached during motion");
   if(c.Phase==KBKStackerCrane.MotionPhase.Travelling){raised=true;Require(Mathf.Abs(c.forkPlatform.Area.yMax-c.liftHeight)<.002f,"Horizontal travel before full lift");}
   if(c.Phase==KBKStackerCrane.MotionPhase.Lowering){traveled=true;Require(Mathf.Abs(c.forkPlatform.Area.center.x-c.destinationPoint.position.x)<.002f,"Lowering before destination");}
   if(steps%10==0)Capture(camera,frame++);
  }
  Require(raised&&traveled&&c.AtDestination&&!c.Busy,"Crane did not complete the ordered sequence");Require(Mathf.Abs(p.Position.x-c.destinationPoint.position.x)<.02f,"Rider did not travel with platform");Require(Mathf.Abs(c.forkPlatform.Area.yMax)<.002f,"Destination deck did not lower to floor");Capture(camera,frame++);
  Require(c.Activate(),"Return did not activate");for(int i=0;i<600&&c.Busy;i++){Set(w,"simTime",w.Clock+.02f);c.Simulate(.02f,w);p.Simulate(0,0,false,false,.02f);}Require(!c.Busy&&!c.AtDestination,"Return sequence failed");
  c.Activate();c.Simulate(1,w);Physics2D.SyncTransforms();float deck=c.forkPlatform.Area.yMax;
  p.ResetController(new Vector2(c.forkPlatform.Area.center.x,p.BodyHeight/2));Set(p,"velocity",Vector2.up*10);Set(p,"grounded",false);bool crossed=false;
  for(int i=0;i<220;i++){Set(w,"simTime",w.Clock+.02f);p.Simulate(0,0,false,false,.02f);if(p.Position.y-p.BodyHeight/2>deck+.1f)crossed=true;}
  Require(crossed&&p.Grounded&&Mathf.Abs(p.Position.y-p.BodyHeight/2-deck)<.03f,"Soft platform pass-through / landing failed");p.Simulate(0,-1,false,true,.02f);for(int i=0;i<30;i++)p.Simulate(0,0,false,false,.02f);Require(p.Position.y-p.BodyHeight/2<deck-.1f,"Down+jump did not drop through");
  File.WriteAllText("output/level-previews/kbk-validation.txt","KBK_VALIDATION_COMPLETE: flush deck, ordered lift/travel/lower, rider transport, return, upward pass-through, landing, down+jump verified.");UnityEngine.Debug.Log("KBK_VALIDATION_COMPLETE");
 }
 static void Capture(Camera camera,int i){var rt=new RenderTexture(1200,750,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1200,750,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,750),0,0);image.Apply();File.WriteAllBytes("output/level-previews/kbk-frames/"+i.ToString("D3")+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}
}
