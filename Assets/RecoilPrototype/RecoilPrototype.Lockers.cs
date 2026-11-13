using UnityEngine;

public sealed partial class RecoilPrototype
{
    WorkerLocker[] workerLockers;
    public WorkerLocker ActiveLocker {get;private set;}
    bool lockerReleasePending,lockerCloseArmed;
    int lockerReleaseFrame=-1;
    float lockerPreviousTimeScale=1;
    CursorLockMode lockerCursorLock;
    bool lockerCursorVisible;
    bool LockerBlocksGameplay {get{return ActiveLocker||lockerReleasePending||lockerReleaseFrame==Time.frameCount;}}
    public WorkerLocker NearbyLocker
    {get{WorkerLocker best=null;float distance=float.PositiveInfinity;if(workerLockers!=null)foreach(var locker in workerLockers)if(locker&&locker.InRange(this)){float d=((Vector2)locker.transform.position-player.Position).sqrMagnitude;if(d<distance){distance=d;best=locker;}}return best;}}
    public bool OpenLocker(WorkerLocker locker)
    {
        if(!PlayerControlsAvailable||ConversationActive||!locker||!locker.InRange(this))return false;
        InterruptInteraction();player.NotifyForbiddenInteraction();ActiveLocker=locker;locker.BeginOpen();lockerCloseArmed=false;
        lockerPreviousTimeScale=Time.timeScale;lockerCursorLock=Cursor.lockState;lockerCursorVisible=Cursor.visible;
        Time.timeScale=0;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;ReaderInputConsumed=true;FrameDelta=0;return true;
    }
    public void CloseLocker()
    {
        if(!ActiveLocker)return;
        ActiveLocker.CloseDoor();ActiveLocker=null;Time.timeScale=lockerPreviousTimeScale;Cursor.lockState=lockerCursorLock;Cursor.visible=lockerCursorVisible;
        lockerReleasePending=true;lockerReleaseFrame=Time.frameCount;FrameDelta=0;ReaderInputConsumed=true;
    }
    bool HandleLockerInput()
    {
        if(ActiveLocker)
        {
            ActiveLocker.AdvanceOpening(Time.unscaledDeltaTime);
            if(!RecoilInput.InteractHeld&&!RecoilInput.ConfirmHeld&&!Input.GetKey(KeyCode.Return)&&!Input.GetKey(KeyCode.Space))lockerCloseArmed=true;
            if(RecoilInput.PausePressed||(lockerCloseArmed&&ActiveLocker.PageReady&&(RecoilInput.InteractPressed||RecoilInput.ConfirmPressed||Input.GetKeyDown(KeyCode.Return))))CloseLocker();
            ReaderInputConsumed=true;FrameDelta=0;return true;
        }
        if(lockerReleasePending)
        {
            if(!RecoilInput.InteractHeld&&!RecoilInput.ConfirmHeld&&!Input.GetKey(KeyCode.Return)&&!Input.GetKey(KeyCode.Space)&&!Input.GetMouseButton(0))lockerReleasePending=false;
            lockerReleaseFrame=Time.frameCount;ReaderInputConsumed=true;FrameDelta=0;return true;
        }
        if(!RecoilInput.InteractPressed||!PlayerControlsAvailable||ConversationActive)return false;
        var locker=NearbyLocker;if(!locker)return false;
        var exit=NearbyDoor;if(exit&&(exit.InteractionPosition-player.Position).sqrMagnitude<((Vector2)locker.transform.position-player.Position).sqrMagnitude)return false;
        return OpenLocker(locker);
    }
    void DrawLockerPage()
    {
        var matrix=GUI.matrix;var colour=GUI.color;GUI.matrix=Matrix4x4.identity;
        GUI.color=new Color(0,0,0,.88f);GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
        // Reserved surface for the future locker interface. No items or placeholder copy.
        GUI.color=new Color(.065f,.07f,.066f,1);GUI.DrawTexture(new Rect(Screen.width*.14f,Screen.height*.13f,Screen.width*.72f,Screen.height*.70f),Texture2D.whiteTexture);
        GUI.color=new Color(.7f,.72f,.68f);GUI.Label(new Rect(Screen.width*.14f,Screen.height*.87f,300,36),RecoilInput.UsingGamepad?"CIRCLE / BACK":"ESC / BACK");GUI.color=colour;GUI.matrix=matrix;
    }
    void OnDisable(){CloseLocker();}
}
