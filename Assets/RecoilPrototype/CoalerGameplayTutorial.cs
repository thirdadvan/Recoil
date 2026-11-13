using UnityEngine;

// Shared by every world: tutorials follow gameplay thresholds, never a specific Coaler.
[DisallowMultipleComponent,AddComponentMenu("Recoil/Tutorials/Coaler Gameplay Tutorial")]
public sealed class CoalerGameplayTutorial : MonoBehaviour
{
    public const string MovementFlag="coaler_tutorial_escape_seen";
    public const string AngerFlag="coaler_tutorial_noise_seen";
    public const string EscapeHint="Try to escape the Coaler's grasp without provoking it.";
    public const string NoiseHint="Slow down. Prolonged noise will provoke the Coalers.";
    public static readonly string[] EscapeLines={
        "If a Coaler has you tangled in its tendrils and you can barely move...",
        "Try running to break free.\nHold SHIFT + a direction key.",
        "Be careful: running can provoke the Coalers."
    };
    [Range(0,1)] public float speedThreshold=.5f;
    [Range(0,100)] public float angerThreshold=70;
    [Min(1)] public float hintDuration=8;
    RecoilPrototype world;
    NpcDialogueSequence escape;
    bool movementPending,angerPending;
    float showNoiseAfter;
    public void Initialize(RecoilPrototype owner)
    {
        if(world)world.RunReset-=ResetPending;
        world=owner;ResetPending();
        if(world)world.RunReset+=ResetPending;
    }
    void ResetPending(){movementPending=angerPending=false;showNoiseAfter=0;}
    void LateUpdate(){Evaluate();}
    public void Evaluate()
    {
        if(!world || !world.Player || !world.Playing)return;
        if(!world.HasItem(MovementFlag) && world.Player.GrabPressure && world.Player.GrabPressure.SpeedMultiplier<speedThreshold-.000001f)
            movementPending=true;
        if(!world.HasItem(AngerFlag))foreach(var enemy in world.Rampagers)
            if(enemy && enemy.isActiveAndEnabled && enemy.SoundEnergy>angerThreshold+.0001f){angerPending=true;break;}
        if(movementPending && !world.HasItem(MovementFlag))
        {
            if(!escape)
            {
                escape=ScriptableObject.CreateInstance<NpcDialogueSequence>();
                escape.name="Coaler escape tutorial";escape.hideFlags=HideFlags.DontSave;
                escape.lines=new NpcDialogueSequence.Line[EscapeLines.Length];
                for(int i=0;i<EscapeLines.Length;i++)escape.lines[i]=new NpcDialogueSequence.Line{speaker="TUTORIAL",text=EscapeLines[i]};
            }
            if(world.BeginConversation(escape,FinishEscape))
            {world.RememberProgress(MovementFlag);movementPending=false;}
            return;
        }
        if(angerPending && !world.HasItem(AngerFlag) && world.Clock>=showNoiseAfter)
        {
            world.ShowDialogue(NoiseHint,hintDuration);
            world.RememberProgress(AngerFlag);angerPending=false;
        }
    }
    void FinishEscape()
    {
        world.ShowDialogue(EscapeHint,hintDuration);
        // Preserve the escape instruction if the anger threshold was crossed at the same time.
        showNoiseAfter=world.Clock+hintDuration;
    }
    void OnDestroy()
    {
        if(world)world.RunReset-=ResetPending;
        if(escape)Destroy(escape);
    }
}
