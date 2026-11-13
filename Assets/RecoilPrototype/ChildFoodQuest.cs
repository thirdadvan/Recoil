using UnityEngine;

public static class ChildFoodQuest
{
    public const string Hungry="child_food_hungry_known",Fed="child_food_fed",Ready="child_food_escort_ready";
    public const string Sandwich="food_warm_sandwich",Biscuits="food_damp_biscuits";
    public const string SandwichConsumed="child_food_sandwich_consumed",BiscuitsConsumed="child_food_biscuits_consumed";
    public static void RestoreConsumption(RecoilPrototype world)
    {if(world.HasItem(SandwichConsumed))world.SetProgressFlag(Sandwich,false);if(world.HasItem(BiscuitsConsumed))world.SetProgressFlag(Biscuits,false);}
    public const string SandwichGiven="child_food_sandwich_given",BiscuitsGiven="child_food_biscuits_given";
    public static string AvailableFood(RecoilPrototype world){return world.HasItem(Sandwich)?Sandwich:world.HasItem(Biscuits)?Biscuits:null;}
    public static bool EscortReady(RecoilPrototype world){return world.HasItem(Ready)||world.HasItem(EscortChild.EscortFlag);}
    public enum Provider{CounterSales,DisguisedMan,Manager}
    public static bool BeginNpc(RecoilPrototype world,NpcDialogueSequence source,System.Action completed,System.Action<int> lineShown,Transform anchor,Provider provider)
    {
        if(!world.HasItem(Hungry))return world.BeginConversation(source,completed,lineShown,anchor);
        var session=anchor.GetComponent<ChildFoodTopics>();if(!session)session=anchor.gameObject.AddComponent<ChildFoodTopics>();
        return session.Begin(world,source,completed,lineShown,provider);
    }
}
