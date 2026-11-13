using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif

public static class GamepadControlVerification
{
    static void Check(bool ok,string text){if(!ok)throw new Exception("PS4 controls: "+text);}
    [InitializeOnLoadMethod]
    static void ResumeRequestedCheck()
    {
        EditorApplication.delayCall+=delegate {if(File.Exists("output/ps4-controls/run.request"))StartPlayCheck();};
    }
    [MenuItem("Recoil/Controls/Verify PS4 controls")]
    public static void StartPlayCheck()
    {
        Directory.CreateDirectory("output/ps4-controls");
        File.WriteAllText("output/ps4-controls/run.request","verification");
        if(!EditorApplication.isPlaying){EditorApplication.isPlaying=true;return;}
        int frames=0;
        EditorApplication.CallbackFunction poll=null;
        poll=delegate {
            if(!EditorApplication.isPlaying||EditorApplication.isCompiling||++frames<4)return;
            EditorApplication.update-=poll;
            File.Move("output/ps4-controls/run.request","output/ps4-controls/run-"+DateTime.Now.Ticks+".started");
            try {Debug.Log(Run());}
            catch(Exception e){Directory.CreateDirectory("output/ps4-controls");File.WriteAllText("output/ps4-controls/error.txt",e.ToString());Debug.LogException(e);}
            finally {EditorApplication.isPlaying=false;}
        };
        EditorApplication.update+=poll;
    }
    public static string Run()
    {
#if ENABLE_INPUT_SYSTEM
        var physical=Gamepad.all.Select(p=>p.layout+" / "+p.displayName).ToArray();
        Directory.CreateDirectory("output/ps4-controls");File.WriteAllText("output/ps4-controls/detected-devices.txt",string.Join("\n",physical));
        var pad=InputSystem.AddDevice<Gamepad>("Recoil controller verification");
        var flags=BindingFlags.Static|BindingFlags.NonPublic;
        Action<GamepadState> state=s=>{
            InputSystem.QueueStateEvent(pad,s);
            typeof(InputSystem).GetMethod("Update",BindingFlags.NonPublic|BindingFlags.Static,null,new[]{typeof(InputUpdateType)},null).Invoke(null,new object[]{InputUpdateType.Dynamic});
            pad.MakeCurrent();
            typeof(RecoilInput).GetField("lastMouse",flags).SetValue(null,(Vector2)Input.mousePosition);
            typeof(RecoilInput).GetField("frame",flags).SetValue(null,-1);RecoilInput.Sample();
        };
        try {
            state(new GamepadState());
            state(new GamepadState().WithButton(GamepadButton.West));
            Check(RecoilInput.UsingGamepad&&RecoilInput.ShootHeld&&!RecoilInput.JumpPressed&&!RecoilInput.InteractPressed,"Square must only shoot");
            state(new GamepadState());state(new GamepadState().WithButton(GamepadButton.South));
            Check(RecoilInput.JumpPressed&&RecoilInput.ConfirmPressed&&!RecoilInput.InteractPressed,"Cross jump/confirm");
            state(new GamepadState());state(new GamepadState().WithButton(GamepadButton.East));
            Check(RecoilInput.InteractPressed&&RecoilInput.InteractHeld&&!RecoilInput.ShootHeld,"Circle starts interaction");
            state(new GamepadState().WithButton(GamepadButton.East));Check(RecoilInput.InteractHeld&&!RecoilInput.InteractPressed,"Circle must stay held without retrigger");
            state(new GamepadState());Check(RecoilInput.InteractReleased,"Circle release for escort/hold actions");
            var move=new GamepadState{leftStick=new Vector2(.85f,0)};
            state(move);Check(RecoilInput.PadMovement.x>.5f&&!RecoilInput.RunHeld,"default movement stealth");
            state(move.WithButton(GamepadButton.LeftStick));Check(RecoilInput.RunHeld,"L3 starts running");
            state(move);Check(RecoilInput.RunHeld,"run stays on after L3 click release");
            state(new GamepadState());Check(!RecoilInput.RunHeld,"stopping restores stealth");
            state(new GamepadState{leftStick=Vector2.down}.WithButton(GamepadButton.South));Check(RecoilInput.PadMovement.y<-.8f&&RecoilInput.JumpPressed,"down + Cross drop-through");
            state(new GamepadState{rightStick=new Vector2(.6f,.8f)});var aim=RecoilInput.PadAim;
            Check(Vector2.Dot(aim,new Vector2(.6f,.8f))>.99f,"right stick aims in world XY");
            state(new GamepadState());Check(Vector2.Distance(aim,RecoilInput.PadAim)<.001f,"retain aim after stick release");
            state(new GamepadState{leftStick=new Vector2(.05f,-.05f),rightStick=new Vector2(.04f,.03f)});Check(RecoilInput.PadMovement==Vector2.zero,"stick drift dead zone");
            state(new GamepadState().WithButton(GamepadButton.Start));Check(RecoilInput.PausePressed,"Options pause");
            Check(RecoilInput.Prompt("Hold SHIFT + a direction key. E / OPEN").Contains("L3"),"controller tutorial prompt");
            var report="Detected physical pads: "+string.Join(" | ",physical)+"\nPassed: Square fire; Cross jump/confirm; Circle press/hold/release; left movement default stealth; L3 click run and stop reset; down+Cross; right aim retention; stick dead zone; Options pause; contextual prompts.\nKeyboard/mouse legacy backend retained (Both).\n";
            Directory.CreateDirectory("output/ps4-controls");File.WriteAllText("output/ps4-controls/verification.txt",report);return report;
        }
        finally {InputSystem.RemoveDevice(pad);typeof(RecoilInput).GetMethod("Reset",flags).Invoke(null,null);}
#else
        throw new Exception("New input backend is not enabled; restart Unity after enabling Both.");
#endif
    }
}
