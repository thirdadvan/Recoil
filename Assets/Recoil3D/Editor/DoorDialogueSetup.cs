using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class DoorDialogueSetup
{
    const string Folder="Assets/RecoilPrototype/Dialogue/";
    static NpcDialogueSequence.Line N(string text,string flag=null){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="The Manager",text=text,progressFlag=flag};}
    static NpcDialogueSequence.Line P(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text=text};}
    static NpcDialogueSequence.ResponseLine R(string text,bool player=false,bool slide=false){return new NpcDialogueSequence.ResponseLine{role=player?NpcDialogueSequence.SpeakerRole.Player:NpcDialogueSequence.SpeakerRole.Npc,speaker=player?"Dawn":"The Manager",text=text,slidePhotoUnderDoor=slide};}
    static NpcDialogueSequence Asset(string name,params NpcDialogueSequence.Line[] lines)
    {
        string path=Folder+name+".asset";var a=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!a){a=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(a,path);}a.lines=lines;EditorUtility.SetDirty(a);return a;
    }
    public static NpcDialogueSequence CreateHandoverContent()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/RecoilDialogue");AssetDatabase.Refresh();
        const string path="Assets/Resources/RecoilDialogue/ManagerDoor_GlassesHandover.asset";
        var asset=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!asset){asset=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(asset,path);}
        asset.lines=new[]{P("I found your glasses."),N("My glasses? Please... slide them under the door.",DoorDialogue.GlassesMentionedFlag),P("(Slides the glasses under the door.)"),N("Yes... these are mine. Thank you."),N("All right. I'm opening the door.")};
        EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);return asset;
    }
    [MenuItem("Recoil/Dialogue/Install Door Conversations")]
    public static string Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        CreateHandoverContent();
        var peaceful=Asset("ManagerDoor_RepeatPeaceful",N("Really? Then have you found my glasses? I can't see a thing without them!",DoorDialogue.PeacefulFlag));
        var threat=Asset("ManagerDoor_RepeatThreat",N("I've lost my glasses. I can't see a thing without them!",DoorDialogue.ThreatFlag));
        var silent=Asset("ManagerDoor_RepeatSilent",N("(Thud.) ...My head! I can't see a thing without my glasses!",DoorDialogue.SilentFlag),P("(Silence.)"));
        var refusal=N("I'm sorry. It's too dangerous. Please ask someone else.");
        refusal.choices=new[]{
            new NpcDialogueSequence.Choice{id="manager_reassure",label="It's safe",text="I've killed all the Coalers outside. It's safe now.",continuation=peaceful},
            new NpcDialogueSequence.Choice{id="manager_threaten",label="Force entry",text="If you don't open this door, I'll pry it open and throw you downstairs.",response=new[]{R("No, please! Who are you looking for?"),R("(Slides the photograph under the door.)",true,true)},continuation=threat}
        };
        var refusalAsset=Asset("ManagerDoor_Refusal",refusal);
        var first=N("Who... who's out there?");first.choices=new[]{
            new NpcDialogueSequence.Choice{id="manager_ask",label="Find someone",text="I'm looking for someone.",response=new[]{R("I'm sorry. It's just me in here. There's no one else."),R("If you open the door, we can talk properly.",true)},continuation=refusalAsset},
            new NpcDialogueSequence.Choice{id="manager_silence",label="Remain silent",text="(Silence.)",response=new[]{R("Ah... don't come any closer!"),R("(Silence.)",true),R("Ah... where did I put my bat?"),R("(Silence.)",true)},continuation=silent}
        };
        var opening=Asset("ManagerDoor_FirstConversation",first);
        var storeLine=N("Come inside, quickly! It's dangerous out there.");storeLine.speaker="The Counter Sales";
        var store=Asset("StoreDoor_Invitation",storeLine);
        var previous=SceneManager.GetActiveScene();var report=new List<string>();
        foreach(var path in new[]{"Assets/Scenes/3D/RecoilPrototype.unity","Assets/Scenes/RecoilPrototype.unity","Assets/Scenes/3D/RecoilFactoryInterior.unity","Assets/Scenes/RecoilFactoryInterior.unity"}){
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                if(scene.isDirty)throw new Exception("Unsaved scene: "+path);
                string backup="Backups/DoorDialogue/2026-11-11/"+path;Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                var roots=scene.GetRootGameObjects();var world=roots.SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
                var door=roots.SelectMany(g=>g.GetComponentsInChildren<RecoilDoor>(true)).First(d=>d.destinationScene.Contains("ServiceStore")||d.destinationScene.Contains("ManagerOffice"));
                bool isStore=door.destinationScene.Contains("ServiceStore");
                var actor=door.GetComponent<DoorDialogue>();if(!actor)actor=door.gameObject.AddComponent<DoorDialogue>();
                actor.role=isStore?DoorDialogue.DoorRole.Store:DoorDialogue.DoorRole.Manager;actor.world=world;actor.door=door;
                var anchor=door.transform.Find("Voice behind door");if(!anchor){anchor=new GameObject("Voice behind door").transform;anchor.SetParent(door.transform,false);}
                anchor.position=door.transform.position+Vector3.up*.55f;actor.voiceAnchor=anchor;
                actor.opening=isStore?store:opening;actor.peacefulRepeat=peaceful;actor.threatRepeat=threat;actor.silentRepeat=silent;
                EditorUtility.SetDirty(actor);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);report.Add(path+": "+door.name);
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return string.Join("\n",report);
    }
}
