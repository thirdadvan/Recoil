using UnityEngine;

// Opening dialogue observes the first Coaler's ordinary close perception. It does not command any monster.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Story/Service Store Outdoor Warning")]
public sealed class ServiceStoreOutdoorEncounter : MonoBehaviour
{
    public const string OpeningLineOne="Don't let them get too close.";
    public const string OpeningLineTwo="But keep the noise down. It'll irritate them.";
    public const string RunTutorial="Hold Shift to run.\nRunning makes noise and may provoke the Coalers.";
    public RecoilPrototype world;
    public RampagerController leftMonster;
    public NpcDialogueSequence warning;
    public string completedFlag="store_outdoor_warning_heard";
    [TextArea] public string stealthTutorial=RunTutorial;
    [Min(1)] public float tutorialDuration=8;
    bool played;
    public bool ProximityReady
    {get{return world && world.Player && leftMonster && leftMonster.isActiveAndEnabled && leftMonster.WatchingPlayer && !leftMonster.PreparingAttack && leftMonster.CanSeePlayer();}}
    void Start()
    {
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        if(!world||!leftMonster||!warning){enabled=false;return;}
        world.RunReset+=ResetEncounter;ResetEncounter();
    }
    void ResetEncounter(){played=world&&world.HasItem(completedFlag);}
    void LateUpdate()
    {
        if(!world || !world.Playing || played || world.ConversationActive || !ProximityReady)return;
        if(world.BeginConversation(warning,FinishWarning))played=true;
    }
    void FinishWarning()
    {world.RememberProgress(completedFlag);world.ShowDialogue(stealthTutorial,tutorialDuration);}
    void OnDestroy(){if(world)world.RunReset-=ResetEncounter;}
}
