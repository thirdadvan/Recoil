using UnityEngine;

// Indoor prompts and activation share the same proximity test. Measuring from the
// player's vertical body span keeps floor pickups reachable without a long horizontal range.
public static class RecoilInteractionRange
{
    public const float IndoorRange=.5f;
    public static bool IsIndoor(RecoilPrototype world)
    {return world&&world.gameObject.scene.IsValid()&&!string.IsNullOrEmpty(world.gameObject.scene.path)&&world.gameObject.scene.name!="RecoilPrototype";}
    public static float BodyDistance(Vector2 playerCentre,float height,Vector2 point)
    {float half=Mathf.Max(.1f,height)*.5f;var closest=new Vector2(playerCentre.x,Mathf.Clamp(point.y,playerCentre.y-half,playerCentre.y+half));return Vector2.Distance(closest,point);}
    public static bool Contains(RecoilPrototype world,Vector2 point,float authoredRange)
    {
        if(!world||!world.Player)return false;
        if(IsIndoor(world))return BodyDistance(world.Player.Position,world.Player.BodyHeight,point)<=IndoorRange;
        return Vector2.Distance(world.Player.Position,point)<=authoredRange;
    }
}
