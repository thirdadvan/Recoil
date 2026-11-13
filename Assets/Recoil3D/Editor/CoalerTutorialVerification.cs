using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class CoalerTutorialVerification
{
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void Require(bool value,string message){if(!value)throw new Exception(message);}
 public static string Run()
 {
  Require(EditorApplication.isPlaying,"Play required");
  var w=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();
  foreach(string name in new[]{"started","paused","dead","won"})typeof(RecoilPrototype).GetField(name,Flags).SetValue(w,name=="started");
  typeof(RecoilPrototype).GetMethod("CancelConversation",Flags).Invoke(w,null);
  var frame=typeof(RecoilPrototype).GetField("conversationInputFrame",Flags);frame.SetValue(w,-1);
  w.SetProgressFlag(CoalerGameplayTutorial.MovementFlag,false);w.SetProgressFlag(CoalerGameplayTutorial.AngerFlag,false);
  var tutorial=w.GetComponent<CoalerGameplayTutorial>();tutorial.Initialize(w);
  var energy=typeof(RampagerController).GetField("detectionProgress",Flags);foreach(var e in w.Rampagers)energy.SetValue(e,0f);
  var pressure=w.Player.GrabPressure;pressure.ResetPressure();pressure.AdvanceBudget(1.5625f,1);tutorial.Evaluate();
  Require(!w.ConversationActive&&!w.HasItem(CoalerGameplayTutorial.MovementFlag),"Triggered at exactly 50%");
  pressure.AdvanceBudget(.01f,1);tutorial.Evaluate();
  Require(w.ConversationActive&&w.HasItem(CoalerGameplayTutorial.MovementFlag),"Did not trigger below 50%");
  var seq=(NpcDialogueSequence)typeof(RecoilPrototype).GetField("conversation",Flags).GetValue(w);Require(seq.lines.Length==3,"Wrong line count");
  w.AdvanceConversation();w.AdvanceConversation();w.AdvanceConversation();
  var dialogue=typeof(RecoilPrototype).GetField("dialogue",Flags);
  Require(!w.ConversationActive&&(string)dialogue.GetValue(w)==CoalerGameplayTutorial.EscapeHint,"Wrong follow-up");
  frame.SetValue(w,-1);tutorial.Evaluate();Require(!w.ConversationActive,"Repeated escape tutorial");
  pressure.ResetPressure();tutorial.Initialize(w);
  var other=w.Rampagers.Last();energy.SetValue(other,.70f);tutorial.Evaluate();Require(!w.HasItem(CoalerGameplayTutorial.AngerFlag),"Triggered at exactly 70%");
  energy.SetValue(other,.71f);tutorial.Evaluate();Require(w.HasItem(CoalerGameplayTutorial.AngerFlag),"No hint from non-first Coaler over 70%");
  Require((string)dialogue.GetValue(w)==CoalerGameplayTutorial.NoiseHint,"Wrong noise hint");
  w.ShowDialogue("Sentinel",1);tutorial.Evaluate();Require((string)dialogue.GetValue(w)=="Sentinel","Repeated anger hint");
  var retry=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);
  Require((bool)retry.Invoke(null,new object[]{CoalerGameplayTutorial.MovementFlag})&&(bool)retry.Invoke(null,new object[]{CoalerGameplayTutorial.AngerFlag}),"Tutorial flags lost on retry");
  string result="PASS speed exactly50/below50, 3 English lines, escape follow-up, once-only display; anger exactly70/over70 from a non-first Coaler; tutorial flags retained on death retry.";
  Directory.CreateDirectory("output/coaler-tutorial");File.WriteAllText("output/coaler-tutorial/verification.txt",result);return result;
 }
}
