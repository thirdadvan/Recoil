using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class StairwayControlValidation
{
 static void Set(object o,string n,object v){o.GetType().GetField(n,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,v);}
 static void Require(bool ok,string msg){if(!ok)throw new Exception(msg);}
 static void Tick(RecoilPrototype w,RecoilPlayerController p,float x,float y){Set(w,"simTime",w.Clock+.02f);p.Simulate(x,y,false,false,.02f);}
 public static void Validate()
 {
  int cases=0;
  foreach(var path in new[]{"Assets/Scenes/RecoilPrototype.unity","Assets/Scenes/RecoilFactoryInterior.unity"})
  {
   var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();
   typeof(RecoilPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);
   var p=w.Player;
   foreach(var s in w.GetComponentsInChildren<WalkableStairway>())
   {
    Require(Mathf.Abs(Mathf.Abs(s.routePoints[1].position.x-s.routePoints[0].position.x)-5.8f)<.001f,"Unequal stair span");
    foreach(bool flight in new[]{false,true})foreach(var input in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
    {
     var feet=s.PositionAt(s.Length*.3f);p.ResetController(feet+Vector2.up*(p.BodyHeight*.5f+.15f));Set(p,"flying",flight);Set(p,"velocity",flight?Vector2.zero:Vector2.down*3);Set(p,"shotAt",w.Clock);Set(p,"pool",Vector2.one*2);
     Tick(w,p,input.x,input.y);Require(p.WalkingStairs && !p.Flying,"Airborne stair catch failed: "+s.name+" / "+input);
     var stopped=p.Position;for(int i=0;i<15;i++)Tick(w,p,0,0);Require(Vector2.Distance(stopped,p.Position)<.001f,"Caught player kept falling");cases++;
    }
    float first=Vector2.Distance(s.routePoints[0].position,s.routePoints[1].position),distance=first+.4f;
    p.ResetController(s.PositionAt(distance)+Vector2.up*p.BodyHeight*.5f);Set(p,"walkingStairway",s);Set(p,"stairDistance",distance);Set(p,"stairApproaching",false);Set(p,"stairHeading",1f);Set(p,"grounded",true);
    float direction=Mathf.Sign(s.routePoints[2].position.x-s.routePoints[1].position.x);
    for(int i=0;i<160 && p.WalkingStairs;i++)Tick(w,p,direction,0);
    Tick(w,p,direction,0);
    Require(!p.WalkingStairs,"Horizontal walking did not leave staircase at floor");Require(Mathf.Abs(p.Position.y-(s.routePoints[2].position.y+p.BodyHeight*.5f))<.05f,"Exit height incorrect");cases++;
   }
  }
  FourStoreyFactoryBuilder.CaptureLayout();Debug.Log("STAIR_CONTROL_VALIDATION_COMPLETE: "+cases+" Unity player simulations passed. No scenes saved.");
 }
}
