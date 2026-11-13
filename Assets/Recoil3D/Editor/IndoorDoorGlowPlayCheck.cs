using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class IndoorDoorGlowPlayCheck
{
 const string Key="IndoorDoorGlowPlayCheck.Active";
 static int stage, frame; static double began; static DoorProximityGlow glow; static RecoilPrototype world; static float[] lightLevels; static Light[] lights;
 static IndoorDoorGlowPlayCheck(){EditorApplication.update+=Tick;}
 public static void Begin(){SessionState.SetString(Key+".Scene",SceneManager.GetActiveScene().path);SessionState.SetBool(Key,true);EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilFactoryInterior.unity");EditorApplication.isPlaying=true;}
 static void Require(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Finish(string result){Directory.CreateDirectory("output/indoor-exits");File.WriteAllText("output/indoor-exits/play-check.txt",result);SessionState.SetBool(Key,false);SessionState.SetBool(Key+".Restore",true);EditorApplication.isPlaying=false;}
 static void Tick(){
  if(SessionState.GetBool(Key+".Restore",false)&&!EditorApplication.isPlaying&&!EditorApplication.isPlayingOrWillChangePlaymode){SessionState.SetBool(Key+".Restore",false);EditorSceneManager.OpenScene(SessionState.GetString(Key+".Scene","Assets/Scenes/3D/RecoilPrototype.unity"));return;}
  if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
  try{
   if(began==0)began=EditorApplication.timeSinceStartup;
   if(EditorApplication.timeSinceStartup-began>45)throw new Exception("Timed out");
   if(stage==0){world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(!world||!world.Player)return;typeof(RecoilPrototype).GetField("started",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,true);world.Player.enabled=false;foreach(var m in UnityEngine.Object.FindObjectsOfType<RampagerController>())m.gameObject.SetActive(false);glow=UnityEngine.Object.FindObjectsOfType<DoorProximityGlow>().First(g=>g.name.Contains("Return Outdoors"));world.Player.ResetController(glow.door.InteractionPosition);Require(glow.PlayerInActivationRange(),"Door activation range not detected");lights=glow.GetComponentsInChildren<Light>(true);lightLevels=lights.Select(l=>l.intensity).ToArray();frame=Time.frameCount;stage=1;return;}
   if(Time.frameCount-frame<65)return;
   if(stage==1){Require(glow.Blend>.99f,"Entering activation range did not brighten door: "+glow.Blend);Require(glow.volumeEnabled&&glow.VolumeDensity>=glow.activeDensity*.99f,"Door volume did not brighten");Require(glow.reflectiveSurfaces.Length==0,"Rectangular reflective surface remains");for(int i=0;i<lights.Length;i++)Require(Mathf.Abs(lights[i].intensity-lightLevels[i])<.001f,"Reflection changed light intensity");Require(glow.halo==null,"Old floor halo remains");world.Player.ResetController(glow.door.InteractionPosition+Vector2.right*5);Require(!glow.PlayerInActivationRange(),"Door range persisted after leaving");frame=Time.frameCount;stage=2;return;}
   Require(glow.Blend<.01f,"Leaving activation range did not restore light: "+glow.Blend);Finish("PASS: actual Play Mode entry brightens aperture scattering in the atmosphere shader, native light intensities unchanged over 65 frames, no floor halo; leaving activation range fades scattering to resting density. Original editor scene restored.");
  }catch(Exception e){Finish("FAIL: "+e);}
 }
}
