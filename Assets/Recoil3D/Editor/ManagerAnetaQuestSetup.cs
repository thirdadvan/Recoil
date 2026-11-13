using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class ManagerAnetaQuestSetup
{
    const string Folder="Assets/RecoilPrototype/Dialogue/";
    static NpcDialogueSequence.Line P(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text=text};}
    static NpcDialogueSequence.Line N(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="The Manager",text=text};}
    static NpcDialogueSequence Asset(string name,params NpcDialogueSequence.Line[] lines)
    {
        string path=Folder+name+".asset";var a=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!a){a=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(a,path);}a.lines=lines;EditorUtility.SetDirty(a);AssetDatabase.SaveAssetIfDirty(a);return a;
    }
    static void Backup(Scene scene)
    {
        if(scene.isDirty)throw new Exception("Preserving unsaved edits; cannot modify scene: "+scene.path);
        string path="Backups/ManagerAnetaQuest/2026-11-13/"+scene.path;Directory.CreateDirectory(Path.GetDirectoryName(path));if(!File.Exists(path))File.Copy(scene.path,path);
    }
    static T[] All<T>(Scene s) where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Scene Open(string path,List<Scene> opened){var s=SceneManager.GetSceneByPath(path);if(!s.isLoaded){s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);opened.Add(s);}return s;}
    static void Save(Scene s){EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Could not save "+s.path);}
    [MenuItem("Recoil/Dialogue/Install Manager Aneta Investigation")]
    public static string Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        var original=SceneManager.GetActiveScene();var opened=new List<Scene>();var report=new List<string>();
        var denial=Asset("ManagerAneta_Denial",
            P("The woman in the photo. Her name is Aneta. Did she work here?"),N("Please... keep your voice down."),N("I don't know her. I really don't."),
            P("Are you sure?"),N("I've had a terrible few days. I can't take any more trouble."),P("I'm asking about Aneta."),
            N("And I'm trying to help. Please don't be angry with me."),P("Where are the employee records?"),N("The archive room. You can look for yourself."),N("Just... leave everything where you found it. They'll hold me responsible."));
        var reminder=Asset("ManagerAneta_DenialReminder",N("I don't know her. Please... don't make this harder for me."),N("If you must check, use the terminal in the archive room."));
        var request=Asset("ManagerAneta_AdmissionRequest",
            P("I found Aneta's electronic employment record. She worked here."),N("...I'm sorry. I was frightened."),P("So you do know her."),
            N("Yes. Please, keep your voice down. I don't want anyone hearing this."),P("Tell me what happened to her."),N("I will. I promise. But could you help me first?"),P("With what?"),
            N("The freight lift has no power. The warehouse is full of cargo."),N("The government is coming for another shipment. If it isn't ready, they'll blame me."),
            P("You can tell me about Aneta now."),N("Please... I can barely think. Just help me get through this first."),
            N("The power panel is in the warehouse storage room. Here. Take the key."),N("There's no one else I can ask."),
            P("I'll restore the power. Then you answer my questions."),N("Thank you. I knew you wouldn't leave me like this."));
        var already=Asset("ManagerAneta_AlreadyPowered",
            P("I found Aneta's electronic employment record. She worked here."),N("...I'm sorry. I was frightened."),N("Yes. I knew her. Please, keep your voice down."),
            N("I was going to ask you to restore the freight lift. The government is coming for another shipment."),P("The freight lift already has power."),
            N("You got it working? ...Thank you. Here, take the warehouse storage room key."),P("Now you answer my questions."),N("Yes. I... I'll tell you what I know."));
        var pending=Asset("ManagerAneta_Pending",N("You haven't restored the power yet?"),N("I'm sorry to keep asking. I just thought... you said you'd help."),P("And you said you'd tell me about Aneta."),N("I haven't forgotten. Please, just this one thing."),N("The power panel is inside the warehouse storage room. You have the key."));
        var restored=Asset("ManagerAneta_FreightRestored",P("The freight lift has power. Now, tell me about Aneta."),N("You got it working? ...Thank you."),N("All right. I'll tell you what I know."));
        var record=AssetDatabase.LoadAssetAtPath<ArchivePersonRecord>("Assets/Resources/RecoilInformation/archive_aneta.asset");
        if(!record)throw new Exception("Existing electronic Aneta record is missing.");
        record.fields=new[]{"NAME: ANETA","EMPLOYER: THIS FACTORY","EMPLOYMENT RECORD: CONFIRMED","CROSS-REFERENCE: FACTORY MANAGEMENT / FOURTH FLOOR","CURRENT LOCATION: NOT RECORDED","FILE STATUS: RESTRICTED"};
        record.dialogueSummary="ANETA. Factory employment record confirmed. Further enquiries are referred to factory management on the fourth floor. Her current location is not recorded.";
        EditorUtility.SetDirty(record);AssetDatabase.SaveAssetIfDirty(record);
        try{
            foreach(string prefix in new[]{"Assets/Scenes/3D/","Assets/Scenes/"}){
                string officePath=prefix+"FactoryRooms/FactoryManagerOfficeF4.unity";
                var office=Open(officePath,opened);Backup(office);var world=All<RecoilPrototype>(office).First();
                var legacy=All<ContinuousInteractionItem>(office).FirstOrDefault(i=>i.itemId=="factory_manager_warehouse_key");
                var manager=legacy?legacy.transform:All<ManagerAnetaConversation>(office).Select(a=>a.transform).FirstOrDefault();
                if(!manager)throw new Exception("Manager missing: "+officePath);
                if(legacy)Object.DestroyImmediate(legacy);
                var talk=manager.GetComponent<ManagerAnetaConversation>();if(!talk)talk=manager.gameObject.AddComponent<ManagerAnetaConversation>();
                talk.world=world;talk.interactionPoint=manager;talk.denial=denial;talk.denialReminder=reminder;talk.admissionRequest=request;talk.requestAlreadyPowered=already;talk.pendingReminder=pending;talk.powerRestored=restored;
                var animator=manager.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.isHuman);
                if(animator)talk.headAnchor=animator.GetBoneTransform(HumanBodyBones.Head);
                if(!talk.headAnchor){var head=manager.Find("Manager dialogue head");if(!head){head=new GameObject("Manager dialogue head").transform;head.SetParent(manager,false);head.localPosition=Vector3.up*.65f;}talk.headAnchor=head;}
                EditorUtility.SetDirty(talk);Save(office);report.Add(officePath+": staged manager dialogue; legacy free-key interaction removed");
                var outside=Open(prefix+"RecoilPrototype.unity",opened);var store=Open(prefix+"RecoilWarehouseStoreroom.unity",opened);Backup(outside);Backup(store);
                var outsidePanel=All<ElectricalPanelInteraction>(outside).FirstOrDefault();var panels=All<ElectricalPanelInteraction>(store);
                if(panels.Length>1)throw new Exception("Duplicate storage-room distribution boxes.");
                var panel=panels.FirstOrDefault();
                if(outsidePanel){
                    if(panel)Object.DestroyImmediate(outsidePanel.gameObject);
                    else{outsidePanel.transform.SetParent(null,true);SceneManager.MoveGameObjectToScene(outsidePanel.gameObject,store);panel=outsidePanel;}
                }
                if(!panel)throw new Exception("Distribution box not found in source or storage room: "+prefix);
                panel.transform.SetParent(All<RecoilPrototype>(store).First().transform,true);
                panel.transform.position=new Vector3(7.2f,1.5f,prefix.Contains("/3D/")?21.55f:0);
                panel.gameObject.name="Warehouse storage room distribution box - interactive";
                panel.interactionPrompt="E / INSPECT FREIGHT POWER PANEL";EditorUtility.SetDirty(panel);Save(outside);Save(store);
                report.Add(prefix+": distribution box moved inside the keyed storage room; Freight routing retained");
            }
            // The 3D archive already has its powered terminal. The legacy 2D room
            // gets the same information record via an explicit file interaction.
            var archive=Open("Assets/Scenes/FactoryRooms/FactoryArchiveF3.unity",opened);
            Backup(archive);var archiveWorld=All<RecoilPrototype>(archive).First();
            if(!All<ArchiveRecordInteraction>(archive).Any()){
                var file=new GameObject("Aneta electronic employment record - legacy terminal interaction");file.transform.SetParent(archiveWorld.transform,false);file.transform.position=new Vector3(6.2f,.9f,0);
                var interaction=file.AddComponent<ArchiveRecordInteraction>();interaction.world=archiveWorld;interaction.record=record;
            }
            Save(archive);report.Add("Electronic employment evidence updated; no manager signature; original loaded archive scene left unsaved and untouched.");
        }finally{
            if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            foreach(var scene in opened.AsEnumerable().Reverse())if(scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
        }
        return string.Join("\n",report);
    }
}
