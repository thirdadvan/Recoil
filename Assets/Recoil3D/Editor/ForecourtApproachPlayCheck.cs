using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class ForecourtApproachPlayCheck
{
 const string Key="ForecourtApproachPlayCheck.Active";static int stage,frames;static RecoilPrototype w;static RecoilPlayerController p;static double began;static float ceilingPeak;static string result;
 static ForecourtApproachPlayCheck(){EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=Changed;}
 static void Set(string name,object value){typeof(RecoilPrototype).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(w,value);}
 public static void Begin(){var s=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(s.isDirty)throw new Exception("Save current scene before check");SessionState.SetString(Key+".Return",s.path);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilPrototype.unity");EditorApplication.isPlaying=true;}
 static void Changed(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Key+".Restore",false)){SessionState.SetBool(Key+".Restore",false);var path=SessionState.GetString(Key+".Return","");if(!string.IsNullOrEmpty(path))EditorSceneManager.OpenScene(path);}}
 static void Finish(string message){Directory.CreateDirectory("output/forecourt-expansion");File.WriteAllText("output/forecourt-expansion/play-verification.txt",message);SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Restore",true);EditorApplication.isPlaying=false;}
 static void Need(bool value,string message){if(!value)throw new Exception(message);}
 static void Step(float x=0,bool jump=false){Set("simTime",w.Clock+.02f);Set("<FrameDelta>k__BackingField",.02f);p.Simulate(x,0,false,jump,.02f,false,true);}
 static void Tick(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;try{if(began==0)began=EditorApplication.timeSinceStartup;if(EditorApplication.timeSinceStartup-began>70)throw new Exception("Check timed out");
 if(stage==0){w=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(!w||!w.Player)return;p=w.Player;Set("started",true);w.enabled=false;p.enabled=false;foreach(var ai in UnityEngine.Object.FindObjectsOfType<RampagerController>())ai.enabled=false;Need(Mathf.Abs(p.Position.x+79)<.1f,"Spawn not moved20m");for(int i=0;i<100;i++)Step(-1);Need(p.Position.x> -79.12f,"Player passed left through starting car: "+p.Position);result="PASS: starting car blocks leftward movement at X="+p.Position.x+".";p.ResetController(new Vector2(-79,1.1f));stage=1;return;}
 if(stage==1){for(int i=0;i<50&&p.Position.x< -28;i++){Step(1);frames++;Need(p.Position.y>-.1f,"Fell through approach ground");Need(Mathf.Abs(p.transform.position.z)<.04f,"Unexpected operation plane change");if(frames>7000)throw new Exception("Central route blocked at "+p.Position);}
 if(p.Position.x< -28)return;result+="\nPASS: actual player walked from X=-79 to X=-28 without blockers/falling or Z-plane changes.";p.ResetController(new Vector2(-43,6.5f));stage=2;return;}
 if(stage==2){for(int i=0;i<120;i++)Step();float feet=p.Position.y-p.BodyHeight*.5f;Need(Mathf.Abs(feet-4.76f)<.04f,"Could not stand on hard canopy: feet="+feet);result+="\nPASS: falling from above lands on canopy top Y=4.76.";p.ResetController(new Vector2(-43,3.1f));ceilingPeak=p.Position.y;Step(0,true);stage=3;return;}
 for(int i=0;i<100;i++){Step();ceilingPeak=Mathf.Max(ceilingPeak,p.Position.y);}Need(ceilingPeak+p.BodyHeight*.5f<=4.56f,"Jump passed through solid canopy");Finish(result+"\nPASS: jump from below cannot pass through canopy. Original editor scene restored.");
 }catch(Exception e){Finish("FAIL: "+e);}}
}
