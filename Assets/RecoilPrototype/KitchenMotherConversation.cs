using UnityEngine;

// Uses the same instantaneous E interaction and HUD as other game items.
[AddComponentMenu("Recoil/Kitchen Mother Conversation")]
public sealed class KitchenMotherConversation : ContinuousInteractionItem
{
    public KitchenMemoryController story;
    public override bool Advance(RecoilPrototype world,float dt)
    {
        Progress=interactionDuration;Completed=true;
        if(story)story.TalkToMother();return true;
    }
    public override void ResetItem(RecoilPrototype world=null)
    {base.ResetItem(world);if(story && world)story.ResetStory(world);}
}
