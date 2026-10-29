using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ResidentialDistrictPlayCheck
{
 const string Key="ResidentialDistrictPlayCheck.Active";
 static int stage,frames;static double start;
 static ResidentialDistrictPlayCheck(){EditorApplication.update+=Tick;}
 public static void Begin(){SessionState.SetBool(Key,true);stage=frames=0;start=EditorApplication.timeSinceStartup;EditorSceneManager.OpenScene(ResidentialDistrictBuilder.Stairs);EditorApplication.isPlaying=true;}
 static void Need(bool ok,string why){if(!ok)throw new Exception(why);}
 static RecoilPrototype World(){return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>()).Single();}
 static void StartWorld(RecoilPrototype w){typeof(RecoilPrototype).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,true);w.Player.enabled=false;}
 static void Step(RecoilPrototype w,float x,float y){var f=typeof(RecoilPrototype).GetField("simTime",BindingFlags.Instance|BindingFlags.NonPublic);f.SetValue(w,w.Clock+.02f);w.Player.Simulate(x,y,false,false,.02f);}
 static void Tick(){
  if(!SessionState.GetBool(Key,false))return;
  try{
   if(EditorApplication.timeSinceStartup-start>150)throw new Exception("Play verification timed out");
   if(!EditorApplication.isPlaying||EditorApplication.isCompiling)return;if(++frames<8)return;
   if(stage==1&&SceneManager.GetActiveScene().path!=ResidentialDistrictBuilder.Apartment)return;
   if(stage==2&&SceneManager.GetActiveScene().path!=ResidentialDistrictBuilder.Stairs)return;
   if(stage==3&&SceneManager.GetActiveScene().name!="RecoilPrototype")return;
   var w=World();StartWorld(w);
   if(stage==0){
    var p=w.Player;p.ResetController(new Vector2(5.7f,p.BodyHeight*.5f));
    for(int i=0;i<8;i++)Step(w,0,0);
    bool used=false,rear=false;for(int i=0;i<1200;i++){Step(w,0,1);used|=p.WalkingStairs;rear|=p.WalkingStairs&&Mathf.Abs(p.GameplayZ-.7f)<.05f;}
    Need(used,"Shared player never attached to residential stairs");Need(Mathf.Abs(p.Position.y-p.BodyHeight*.5f-6.4f)<.12f,"Player failed to reach third floor: "+p.Position);
    Need(rear,"Stair Z should match rear operation plane while climbing");
    p.Position=new Vector2(10.6f,6.4f+p.BodyHeight*.5f);p.SyncVisuals(0);
    var door=w.GetComponentsInChildren<RecoilDoor>().Single(d=>d.destinationScene==ResidentialDistrictBuilder.Apartment);
    Need(door.TryInteract(w),"Third floor door rejected player");stage=1;frames=0;return;
   }
   if(stage==1){
    Need(SceneManager.GetActiveScene().path==ResidentialDistrictBuilder.Apartment,"Apartment transition failed");Need(!w.GetComponent<KitchenMemoryController>(),"Death memory controller leaked into apartment");
    Need(!w.Player.GetComponent<Recoil3DActorVisual>().child,"Adult player was replaced by child");Need(Mathf.Abs(w.Player.Position.x-12.1f)<.15f,"Apartment destination spawn failed");
    var camera=w.ViewCamera;Need(camera,"Missing room camera");var control=camera.GetComponent<RecoilCameraController>();var before=camera.transform.position;control.SnapToTarget();Need(Vector3.Distance(before,camera.transform.position)<.03f,"Camera was not settled after arrival");
    var door=w.GetComponentsInChildren<RecoilDoor>().Single();Need(door.TryInteract(w),"Apartment return door rejected player");stage=2;frames=0;return;
   }
   if(stage==2){
    Need(SceneManager.GetActiveScene().path==ResidentialDistrictBuilder.Stairs,"Return to stairwell failed");Need(Mathf.Abs(w.Player.Position.y-w.Player.BodyHeight*.5f-6.4f)<.12f,"Return spawn lost floor height");
    var p=w.Player;p.ResetController(new Vector2(5.7f,6.4f+p.BodyHeight*.5f));for(int i=0;i<8;i++)Step(w,0,0);
    for(int i=0;i<1200;i++)Step(w,0,-1);
    Need(Mathf.Abs(p.Position.y-p.BodyHeight*.5f)<.12f,"Player failed to descend to ground floor");
    p.Position=new Vector2(3,p.BodyHeight*.5f);var door=w.GetComponentsInChildren<RecoilDoor>().Single(d=>d.destinationScene.Contains("RecoilPrototype"));Need(door.TryInteract(w),"Street exit rejected player");stage=3;frames=0;return;
   }
   if(stage==3){
    Need(SceneManager.GetActiveScene().name=="RecoilPrototype","Street return failed");Need(Mathf.Abs(w.Player.Position.x-203.2f)<.15f,"Street return spawn failed");
    var sweep=w.GetComponentInChildren<CheckpointSearchlight>();Need(sweep,"Searchlight missing at runtime");Physics.SyncTransforms();bool shelter=false,open=false;
    foreach(float phase in new[]{.05f,.15f,.25f,.35f,.45f}){sweep.Pose(phase);for(float x=132;x<184;x+=.25f){var q=new Vector3(x,1,0);if(sweep.Illuminates(q,false)){if(sweep.Illuminates(q))open=true;else shelter=true;}}}
    Need(shelter&&open,"Runtime searchlight shelter failed");
    File.WriteAllText("output/residential/play-verification.txt","PASS: shared adult player ascends both flights to floor 3 at Z=.7; enters apartment; camera settled on arrival; apartment returns to floor 3; descends to ground floor; exterior return spawn at x203.2; searchlight detects open lane and is occluded by booth colliders.\n");
    Debug.Log("RESIDENTIAL_PLAY_CHECK_PASS");SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
   }
  }catch(Exception e){File.WriteAllText("output/residential/play-verification.txt","FAILED\n"+e);Debug.LogException(e);SessionState.SetBool(Key,false);EditorApplication.isPlaying=false;EditorApplication.delayCall+=()=>EditorApplication.Exit(1);}
 }
}
