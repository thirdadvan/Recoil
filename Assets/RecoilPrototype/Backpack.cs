using UnityEngine;

public sealed class Backpack : ContinuousInteractionItem
{
    void Reset()
    {
        itemId="backpack";displayName="Employee backpack";
        description="Hold E to search for an employee access card.";
        interactionDuration=1.5f;interactionRange=1.5f;
        resultId="employee_access_card";resultMessage="Employee access card acquired.";
    }
}
