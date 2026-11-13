using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class UniversalInteractionVerification
{
    static void Need(bool ok,string message){if(!ok)throw new Exception("Universal interaction: "+message);}
    [MenuItem("Recoil/Debug/Verify Universal Interaction Key")]
    public static string Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Run outside Play mode.");
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var root=new GameObject("Universal interaction fixture");SceneManager.MoveGameObjectToScene(root,scene);
            var world=root.AddComponent<RecoilPrototype>();
            var playerGo=new GameObject("Dawn");playerGo.transform.SetParent(root.transform);
            var player=playerGo.AddComponent<RecoilPlayerController>();
            Func<string,Transform> part=name=>{var child=new GameObject(name).transform;child.SetParent(playerGo.transform);return child;};
            player.Configure(world,null,part("Coat"),part("Head"),part("Hat"),part("Gun"));
            typeof(RecoilPrototype).GetField("player",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(world,player);
            player.ResetController(new Vector2(0,100));
            Func<string,GameObject> prop=name=>{var go=new GameObject(name);go.transform.SetParent(root.transform);go.transform.position=player.WorldPosition;return go;};
            var door=prop("Blocked door").AddComponent<RecoilDoor>();door.completesLevel=false;door.requiredItemId="missing_key";door.interactionOffset=Vector2.zero;
            door.blockedByThreat=door.irreparableDamage=door.permanentlyBlocked=true;door.ResetDoor(world);
            Need(door.TryInteract(world)&&!door.IsOpen,"Normal mode must retain damaged/threat door restrictions");
            var talk=door.gameObject.AddComponent<DoorDialogue>();talk.world=world;talk.role=DoorDialogue.DoorRole.Manager;
            Need(talk.BlocksEntry(world),"Manager story prerequisite must exist without key");
            var clock=prop("Clock").AddComponent<WindUpArchiveClock>();clock.requiredReadFlag="unread_note";clock.resultId=WindUpArchiveClock.WoundFlag;
            Need(!clock.InRange(world),"Clock must require reading without key");
            var terminal=prop("Terminal").AddComponent<ArchiveTerminalConversation>();terminal.world=world;
            Need(!terminal.GirlfriendOptionVisible,"Name must be locked without story knowledge");
            var crane=prop("Crane").AddComponent<KBKStackerCrane>();crane.requiredPowerItem="missing_power";
            crane.startPoint=prop("Crane start").transform;crane.destinationPoint=prop("Crane end").transform;crane.travelRig=prop("Crane rig").transform;crane.liftCarriage=prop("Crane carriage").transform;
            Need(!crane.HasPower&&!crane.Activate(),"Crane must need power without key");
            var pump=prop("Broken pump").AddComponent<FuelPumpInteraction>();pump.startsBroken=true;pump.resultId="pump_checked";pump.ResetItem(world);
            Need(!pump.InRange(world),"Broken pump must remain unavailable without key");
            world.EquipUniversalKey();
            Need(world.InteractionRequirementsMet(false)&&world.HasInteractionRequirement("missing_key"),"K must bypass boolean and item prerequisites");
            Need(!world.HasItem("missing_key")&&!world.HasItem("unread_note")&&!world.HasItem(WindUpArchiveClock.KeyId),"Bypass must not fabricate inventory or completed story flags");
            Need(!talk.BlocksEntry(world)&&door.TryInteract(world)&&door.IsOpen,"K must bypass both story and damaged/threat door prerequisites");
            Need(clock.InRange(world)&&clock.Advance(world,.01f)&&clock.Winding,"K must wind an unread clock without a winding key");
            world.PropOperation.Simulate(clock.windingDuration);Need(clock.IsWound&&world.HasItem(WindUpArchiveClock.WoundFlag),"Bypassed clock must complete its real operation");
            Need(terminal.GirlfriendOptionVisible,"K must expose conditional terminal queries");
            Need(crane.HasPower&&crane.Activate()&&crane.Busy,"K must activate unpowered equipment");
            Need(pump.InRange(world)&&pump.Advance(world,.01f)&&world.HasItem("pump_checked"),"K must permit one real broken-pump interaction");
            Need(!pump.InRange(world)&&!pump.Advance(world,.01f),"K must not repeat completed pickups");
            door.ResetDoor(world);player.Position=new Vector2(100,100);
            Need(!door.InRange(world)&&!door.TryInteract(world),"K must preserve physical interaction range");
            world.SetProgressFlag(RecoilPrototype.UniversalKey,false);
            Need(!world.InteractionRequirementsMet(false)&&!crane.HasPower&&talk.BlocksEntry(world)&&!terminal.GirlfriendOptionVisible,"Removing K must restore original prerequisites");
            Need(SceneManager.sceneCount==1,"Verification must not duplicate the gameplay scene");
            return "PASS: normal prerequisites, K bypass, clock operation, story doors, terminal query, equipment power, broken pump, exact inventory, completion protection, proximity and normal-mode recovery.";
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
