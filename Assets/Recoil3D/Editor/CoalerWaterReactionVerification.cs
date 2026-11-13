using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class CoalerWaterReactionVerification
{
 static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
 static void Need(bool ok,string message){if(!ok)throw new Exception("Water reaction: "+message);}
 public static void Verify(){
  var scene=EditorSceneManager.NewPreviewScene();var results=new System.Collections.Generic.List<string>();
  try{
   var root=new GameObject("Water reaction verification");SceneManager.MoveGameObjectToScene(root,scene);var w=root.AddComponent<RecoilPrototype>();Set(w,"started",true);Set(w,"simTime",100f);
   var playerGO=new GameObject("Dawn");playerGO.transform.SetParent(root.transform);var player=playerGO.AddComponent<RecoilPlayerController>();Func<string,Transform> part=n=>{var t=new GameObject(n).transform;t.SetParent(playerGO.transform);return t;};player.Configure(w,null,part("coat"),part("head"),part("hat"),part("gun"));Set(w,"player",player);player.ResetController(new Vector2(8,100));
   Func<string,float,RampagerController> make=(name,x)=>{var go=new GameObject(name);go.transform.SetParent(root.transform);go.transform.position=new Vector3(x,100,0);var ai=go.AddComponent<RampagerController>();ai.Initialize(w);ai.GetComponent<RampagerAttack>().enabled=false;return ai;};
   var a=make("Front Coaler",0);var b=make("Rear Coaler",-3);Set(w,"enemies",new System.Collections.Generic.List<RampagerController>{a,b});
   Action<float> tick=dt=>{Set(w,"simTime",w.Clock+dt);a.Simulate(dt);};
   foreach(var state in new[]{RampagerController.Behaviour.Roaming,RampagerController.Behaviour.Attack,RampagerController.Behaviour.Sleeping}){
    a.initialState=state;a.ResetController();var pos=a.Position;a.TakeWaterHit(new Vector2(5,100),Vector2.left,10,player.Position);
    Need(a.ReactingToWater&&!a.ProtectingCore,"Body hit did not start guard: "+state);Need(!a.GetComponent<RampagerGrab>().Evaluate(),"Fusion active while guarding");
    tick(1.19f);Need(a.ReactingToWater&&a.Position==pos,"Body hit must stop movement for 1.2 seconds");tick(.02f);Need(!a.ReactingToWater&&a.State==RampagerController.Behaviour.Attack&&!a.PreparingAttack,"Body reaction must finish directly in Attack");
    results.Add("PASS: "+state+" body hit pauses movement/fusion for1.2s, then Attack without an extra charge delay");
   }
   a.initialState=RampagerController.Behaviour.Roaming;a.ResetController();b.ResetController();float distance=w.ApplyWaterHit(new Vector2(5,100),Vector2.left,15,player.Position);
   Need(a.ReactingToWater&&!b.ReactingToWater&&distance<5.1f,"Water must stop at the closest body, no penetration");results.Add("PASS: nearest-target selection and truncated water ray; rear target unaffected");
   a.ResetController();a.GetComponent<RampagerMovement>().SetNavigationPosition(new Vector3(0,100,3));float miss;Need(!a.TryWaterIntersection(new Vector2(5,100),Vector2.left,10,out miss),"Distant Z plane incorrectly intercepts water");a.ResetController();
   a.WatchForStory();var art=a.gameObject.AddComponent<RampagerTentacleVisual>();art.SetPreview(RampagerTentacleVisual.Form.Approaching);art.UpdatePose(0,true);Need(art.CoreExposed,"Approach preview core must be exposed");
   var core=art.CoreWorldPosition;var origin=new Vector2(core.x+4,core.y);a.TakeWaterHit(origin,Vector2.left,10,player.Position);
   Need(a.ProtectingCore&&!a.WaterForcesRed,"Core hit must keep tentacles black");tick(.99f);Need(a.CoreFoldProgress<1&&!a.CoreStill,"Layered fold must take1s");tick(.02f);Need(a.CoreStill,"Core must hold still after folding");float heldProgress=a.WaterReactionProgress;a.ReceiveWaterImpact(new Vector2(5,100),Vector2.left,10,player.Position);Need(Mathf.Abs(a.WaterReactionProgress-heldProgress)<.0001f,"Repeated blocked shot rewound core sequence");tick(2.48f);Need(a.CoreStill,"Still section must last2.5s");tick(.02f);Need(!a.CoreStill&&a.CoreAttackProgress>0,"Final attack transition must start at3.5s");tick(.50f);Need(!a.ReactingToWater&&a.State==RampagerController.Behaviour.Attack,"Core recovery must finish at4s");results.Add("PASS: exposed-core hit stays black;1s layered fold,2.5s still,0.5s attack transition");
   a.ResetController();a.WatchForStory();art.UpdatePose(0,true);a.TakeWaterHit(new Vector2(5,99.6f),Vector2.left,10,player.Position);Need(a.ReactingToWater&&!a.ProtectingCore,"Approach body miss must use ordinary guard");results.Add("PASS: exposed-state body hit remains distinct from actual core hit");
   Directory.CreateDirectory("output/water-reaction");File.WriteAllLines("output/water-reaction/verification.txt",results);
  }finally{EditorSceneManager.ClosePreviewScene(scene);}
 }
}
