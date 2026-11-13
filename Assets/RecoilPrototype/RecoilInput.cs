using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Read once per frame so gameplay, conversations and menus share the same edges.
// Input System supplies platform-independent DualShock button/stick layouts.
public static class RecoilInput
{
    static int frame=-1;
    static bool usingPad,runLatched,previousUp,previousDown;
    static Vector2 lastAim=Vector2.right,lastMouse,padMove,padAim;
    static bool square,cross,crossDown,circle,circleDown,circleUp,options,upDown,downDown;
    static bool previousCross,previousCircle,previousOptions,previousL3;
    static int previousDevice=-1;
    public static bool UsingGamepad {get{Sample();return usingPad;}}
    public static Vector2 PadMovement {get{Sample();return usingPad?padMove:Vector2.zero;}}
    public static Vector2 PadAim {get{Sample();return lastAim;}}
    public static Vector2 LookStick {get{Sample();return usingPad?padAim:Vector2.zero;}}
    public static Vector2 PointerStick {get{Sample();return padAim.sqrMagnitude>padMove.sqrMagnitude?padAim:padMove;}}
    public static bool InventoryPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var dual=UnityEngine.InputSystem.Gamepad.current as UnityEngine.InputSystem.DualShock.DualShockGamepad;
            if(dual!=null&&dual.touchpadButton.wasPressedThisFrame)return true;
#endif
            return Input.GetKeyDown(KeyCode.I);
        }
    }
    public static bool FilesPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var pad=UnityEngine.InputSystem.Gamepad.current;
            if(pad!=null&&pad.rightShoulder.wasPressedThisFrame)return true;
#endif
            return Input.GetKeyDown(KeyCode.J)||Input.GetKeyDown(KeyCode.Tab);
        }
    }
    public static bool ShootHeld {get{Sample();return square||Input.GetMouseButton(0);}}
    public static bool JumpHeld {get{Sample();return cross||Input.GetKey(KeyCode.Space);}}
    public static bool JumpPressed {get{Sample();return crossDown||Input.GetKeyDown(KeyCode.Space);}}
    public static bool InteractHeld {get{Sample();return circle||Input.GetKey(KeyCode.E);}}
    public static bool InteractPressed {get{Sample();return circleDown||Input.GetKeyDown(KeyCode.E);}}
    public static bool InteractReleased {get{Sample();return circleUp||Input.GetKeyUp(KeyCode.E);}}
    public static bool ConfirmHeld {get{Sample();return cross;}}
    public static bool ConfirmPressed {get{Sample();return crossDown;}}
    public static bool PausePressed {get{Sample();return options||Input.GetKeyDown(KeyCode.Escape);}}
    public static bool RunHeld {get{Sample();return (usingPad&&runLatched)||Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);}}
    public static bool DownPressed {get{Sample();return downDown||Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.DownArrow);}}
    public static bool UpPressed {get{Sample();return upDown||Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.UpArrow);}}

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {frame=-1;usingPad=runLatched=previousUp=previousDown=false;previousCross=previousCircle=previousOptions=previousL3=false;previousDevice=-1;lastAim=Vector2.right;lastMouse=Input.mousePosition;}
    public static Vector2 DeadZone(Vector2 value,float dead=.2f)
    {float length=value.magnitude;return length<=dead?Vector2.zero:value/length*Mathf.Clamp01((length-dead)/(1-dead));}
    public static void Sample()
    {
        if(frame==Time.frameCount)return;frame=Time.frameCount;
        square=cross=crossDown=circle=circleDown=circleUp=options=upDown=downDown=false;
        padMove=padAim=Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var pad=Gamepad.current;
        if(pad!=null&&pad.added) {
            if(previousDevice!=pad.deviceId){previousCross=previousCircle=previousOptions=previousL3=previousUp=previousDown=false;previousDevice=pad.deviceId;}
            padMove=DeadZone(pad.leftStick.ReadValue());
            var dpad=pad.dpad.ReadValue();if(dpad.sqrMagnitude>.1f)padMove=dpad;
            padAim=DeadZone(pad.rightStick.ReadValue());
            square=pad.buttonWest.isPressed;cross=pad.buttonSouth.isPressed;crossDown=cross&&!previousCross;
            circle=pad.buttonEast.isPressed;circleDown=circle&&!previousCircle;circleUp=!circle&&previousCircle;
            bool start=pad.startButton.isPressed,l3=pad.leftStickButton.isPressed;
            options=start&&!previousOptions;bool l3Down=l3&&!previousL3;
            if(padMove.sqrMagnitude>.01f||padAim.sqrMagnitude>.01f||square||cross||circle||options||l3Down)usingPad=true;
            if(l3Down)runLatched=!runLatched;
            if(padMove.sqrMagnitude<.01f)runLatched=false;
            bool up=padMove.y>.5f;upDown=up&&!previousUp;previousUp=up;
            bool down=padMove.y<-.5f;downDown=down&&!previousDown;previousDown=down;
            if(padAim.sqrMagnitude>.01f)lastAim=padAim.normalized;
            previousCross=cross;previousCircle=circle;previousOptions=start;previousL3=l3;
        }else {circleUp=previousCircle;usingPad=runLatched=previousUp=previousDown=false;previousCross=previousCircle=previousOptions=previousL3=false;previousDevice=-1;}
#endif
        Vector2 mouse=Input.mousePosition;
        bool keyboard=Input.GetKeyDown(KeyCode.A)||Input.GetKeyDown(KeyCode.D)||Input.GetKeyDown(KeyCode.W)||Input.GetKeyDown(KeyCode.S)||Input.GetKeyDown(KeyCode.LeftArrow)||Input.GetKeyDown(KeyCode.RightArrow)||Input.GetKeyDown(KeyCode.UpArrow)||Input.GetKeyDown(KeyCode.DownArrow)||Input.GetKeyDown(KeyCode.E)||Input.GetKeyDown(KeyCode.Space)||Input.GetMouseButtonDown(0);
        if(keyboard||(mouse-lastMouse).sqrMagnitude>16)usingPad=false;
        lastMouse=mouse;
    }
    public static string Prompt(string text)
    {
        if(!UsingGamepad||string.IsNullOrEmpty(text))return text;
        return text.Replace("Hold SHIFT + a direction key.","Move with the left stick and click L3 to run.")
            .Replace("Hold Shift","Click L3").Replace("Hold SHIFT","Click L3")
            .Replace("HOLD E","HOLD CIRCLE").Replace("Press E","Press CIRCLE")
            .Replace("E  /","CIRCLE /").Replace("E /","CIRCLE /")
            .Replace("ESC /","OPTIONS /").Replace("CLICK / SPACE  •  CONTINUE","CROSS / CONTINUE");
    }
}
