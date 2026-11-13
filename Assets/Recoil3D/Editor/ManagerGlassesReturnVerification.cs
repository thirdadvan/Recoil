using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ManagerGlassesReturnVerification
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Need(bool value,string message){if(!value)throw new Exception(message);}
    static void Set(object obj,string field,object value){obj.GetType().GetField(field,Private).SetValue(obj,value);}
    static void Arm(RecoilPrototype w){Set(w,"conversationInputFrame",-1);Set(w,"conversationReleasePending",false);Set(w,"conversationReadyAt",-1f);}
    [MenuItem("Recoil/Dialogue/Verify Glasses Door Handover")]
    public static string Run()
    {
        Need(!EditorApplication.isPlayingOrWillChangePlaymode,"Edit mode required");
        var preview=EditorSceneManager.NewPreviewScene();var report=new List<string>();
        try{
            Func<string,GameObject> make=name=>{var g=new GameObject(name);g.hideFlags=HideFlags.HideAndDontSave;SceneManager.MoveGameObjectToScene(g,preview);return g;};
            var world=make("Preview world").AddComponent<RecoilPrototype>();
            var player=make("Preview Dawn").AddComponent<RecoilPlayerController>();Set(world,"player",player);Set(player,"world",world);Set(world,"started",true);
            var camera=make("Preview camera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=Vector3.back*10;camera.enabled=false;Set(world,"cam",camera);
            var g=make("Preview manager door");g.transform.position=Vector3.up*.6f;
            var door=g.AddComponent<RecoilDoor>();door.requiresClue=false;door.completesLevel=false;
            var talk=g.AddComponent<DoorDialogue>();talk.role=DoorDialogue.DoorRole.Manager;talk.world=world;talk.door=door;talk.voiceAnchor=g.transform;
            Need(talk.BlocksEntry(world),"Door initially unlocked");world.RememberProgress(ManagerGlassesPickup.InventoryId);
            Need(talk.BlocksEntry(world),"Holding glasses bypasses door");Need(talk.CurrentSequence==Resources.Load<NpcDialogueSequence>(DoorDialogue.HandoverResource),"Wrong handover resource");
            Need(door.TryInteract(world)&&world.ConversationActive&&!door.IsOpen,"Door interaction did not start handover");Arm(world);
            Need(world.CurrentConversationText=="I found your glasses.","Wrong first line");
            for(int i=0;i<2;i++){world.RevealConversationText();world.AdvanceConversation();}
            Need(world.CurrentConversationText.Contains("Slides the glasses"),"Missing action bubble");
            world.CancelConversationWith(talk.voiceAnchor);Arm(world);
            Need(world.HasItem(ManagerGlassesPickup.InventoryId)&&!world.HasItem(DoorDialogue.GlassesReturnedFlag)&&talk.BlocksEntry(world),"Cancellation consumed glasses or unlocked door");
            report.Add("PASS holding glasses remains locked; action bubble; interrupted dialogue safely replayable");
            Need(talk.StartConversation(),"Replay failed");Arm(world);int guard=0;
            while(world.ConversationActive&&guard++<20){world.RevealConversationText();world.AdvanceConversation();}
            Need(!world.ConversationActive&&world.HasItem(DoorDialogue.GlassesReturnedFlag)&&!world.HasItem(ManagerGlassesPickup.InventoryId),"Handover completion incorrect");
            Need(world.HasItem(ManagerGlassesPickup.CollectedFlag)&&world.HasItem(DoorDialogue.GlassesMentionedFlag),"Missing persistent handover flags");
            var opening=g.GetComponent<ManagerDoorOpening>();Need(opening&&!opening.FullyOpen&&talk.BlocksEntry(world),"Door became enterable before visual opening");
            opening.Advance(.2f);Need(opening.Progress>0&&!opening.FullyOpen&&talk.BlocksEntry(world),"Opening progress gate failed");
            opening.Advance(.3f);Arm(world);Need(opening.FullyOpen&&!talk.BlocksEntry(world)&&door.TryInteract(world)&&door.IsOpen,"Opened door not enterable");
            report.Add("PASS final confirmation consumes glasses; 0.45-second opening completes before entry unlocks");
            var pickup=make("Preview glasses").AddComponent<ManagerGlassesPickup>();pickup.ResetItem(world);Need(pickup.Completed,"Returned glasses respawn on reset");
            var restoredDoor=make("Restored manager door").AddComponent<RecoilDoor>();var restored=restoredDoor.gameObject.AddComponent<ManagerDoorOpening>();restored.Initialize(world,restoredDoor);Need(restored.FullyOpen,"Returned door did not restore opened state");
            var retained=typeof(RecoilPrototype).GetMethod("RetainedOnRetry",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var flag in new[]{ManagerGlassesPickup.CollectedFlag,DoorDialogue.GlassesReturnedFlag,DoorDialogue.ManagerDoorOpenFlag,DoorDialogue.GlassesMentionedFlag})Need((bool)retained.Invoke(null,new object[]{flag}),"Retry lost "+flag);
            report.Add("PASS opened-state restoration, no glasses respawn, all milestone flags retained on retry");
            // Test the same mesh reparenting used by authored 3D corridor doors.
            var visualDoor=make("Visual manager door").AddComponent<RecoilDoor>();
            var leaf=make("Door leaf");leaf.transform.SetParent(visualDoor.transform,false);
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cube);mesh.transform.SetParent(leaf.transform,false);mesh.transform.localScale=new Vector3(1.13f,2.38f,.06f);
            var visual=leaf.AddComponent<Recoil3DSpriteVisual>();visual.meshRenderer=mesh.GetComponent<MeshRenderer>();
            var presentation=visualDoor.gameObject.AddComponent<ManagerDoorOpening>();presentation.Initialize(world,visualDoor);
            Need(presentation.FullyOpen&&Quaternion.Angle(mesh.transform.rotation,Quaternion.identity)>70,"3D leaf did not rotate visibly");
            var aperture=visualDoor.transform.Find("Manager office open doorway");Need(aperture&&aperture.gameObject.activeSelf&&!aperture.GetComponent<Collider>(),"Open doorway missing or blocking entry");
            report.Add("PASS 3D leaf visibly swings open; dark doorway has no added collision");
        }finally{EditorSceneManager.ClosePreviewScene(preview);}
        return string.Join("\n",report);
    }
}
