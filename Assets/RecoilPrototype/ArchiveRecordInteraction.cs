using UnityEngine;

// Legacy 2D fallback; the authored 3D archive uses ArchiveTerminalConversation.
[DefaultExecutionOrder(-750)]
public sealed class ArchiveRecordInteraction : MonoBehaviour
{
    public RecoilPrototype world;
    public ArchivePersonRecord record;
    bool InRange {get{return world && world.Player && world.Clear(world.Player.Position,transform.position) && RecoilInteractionRange.Contains(world,transform.position,1.5f);}}
    void Update(){if(world && world.PlayerControlsAvailable && !world.ReaderInputConsumed && RecoilInput.InteractPressed && InRange){world.InformationJournal.Acquire(record);}}
    void OnGUI(){if(world && world.PlayerControlsAvailable && InRange)world.DrawConversationTalkPrompt(transform,RecoilInput.UsingGamepad?"CIRCLE / READ EMPLOYMENT RECORD":"E / READ EMPLOYMENT RECORD");}
}
