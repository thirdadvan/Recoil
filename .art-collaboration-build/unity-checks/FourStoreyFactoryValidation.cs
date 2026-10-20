using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class FourStoreyFactoryValidation
{
 static void Check(bool ok,string detail){if(!ok)throw new Exception("FACTORY CHECK FAILED: "+detail);Debug.Log("FACTORY CHECK PASS: "+detail);}
 public static void Verify(){
  foreach(string path in new[]{"Assets/Scenes/RecoilPrototype.unity","Assets/Scenes/RecoilFactoryInterior.unity"}){
   var s=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);foreach(var c in s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilCameraController>(true))){c.maximumY=path.Contains("Interior")?27.2f:30; if(path.Contains("Interior"))c.minimumY=4.8f;EditorUtility.SetDirty(c);}EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
  }
  var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();var w=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>()).First();typeof(RecoilPrototype).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(w,null);var p=w.Player;var time=typeof(RecoilPrototype).GetField("simTime",BindingFlags.Instance|BindingFlags.NonPublic);float clock=0;Action<float,float,bool,bool> tick=(x,y,fire,jump)=>{clock+=.02f;time.SetValue(w,clock);p.Simulate(x,y,false,fire,jump,.02f);};
  Check(w.GetComponentsInChildren<WalkableStairway>().Length==3,"Three internal switchback routes");
  p.ResetController(new Vector2(28,.825f));tick(0,0,false,false);for(int i=0;i<100;i++)tick(0,1,false,false);Check(p.WalkingStairs&&p.Position.y>1,"W enters and climbs");var pos=p.Position;for(int i=0;i<25;i++)tick(0,0,false,false);Check((p.Position-pos).sqrMagnitude<.000001f,"Release pauses without gravity drift");for(int i=0;i<700;i++)tick(0,1,false,false);Check(Mathf.Abs(p.Position.y-23.625f)<.01f,"W reaches floor four across three flights");for(int i=0;i<750;i++)tick(0,-1,false,false);Check(Mathf.Abs(p.Position.y-.825f)<.01f,"S returns to floor one");
  p.ResetController(new Vector2(28,.825f));tick(0,0,false,false);for(int i=0;i<60;i++)tick(0,1,false,false);Check(p.WalkingStairs,"Attached before jumping");tick(0,0,false,true);Check(!p.WalkingStairs&&!p.Grounded,"Jump releases stairs");
  var door=w.GetComponentsInChildren<RecoilDoor>().First(d=>d.openStateItemId=="factory_shortcut_floor3");p.Position=door.InteractionPosition;Check(door.TryInteract(w)&&w.HasItem("factory_shortcut_floor3")&&!door.IsOpen,"Inside first interaction unlocks without premature scene travel");
  var stairs=w.GetComponentsInChildren<WalkableStairway>();foreach(var stair in stairs)Check(stair.Available&&stair.routePoints.Length==3,"Editable path valid: "+stair.name);
  Debug.Log("FOUR_STOREY_FACTORY_VALIDATION_COMPLETE");
 }
}
