using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class StoreClerkConversationSetup
{
    public const string DialoguePath="Assets/RecoilPrototype/Dialogue/StoreClerk_FirstConversation.asset";
    public const string ReminderPath="Assets/RecoilPrototype/Dialogue/StoreClerk_ManagerReminder.asset";
    public static readonly string[] ScenePaths={"Assets/Scenes/3D/RecoilServiceStore_PS1.unity","Assets/Scenes/3D/RecoilServiceStore.unity","Assets/Scenes/RecoilServiceStore.unity"};
    static NpcDialogueSequence.Line P(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text=text};}
    static NpcDialogueSequence.Line N(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="The Counter Sales",text=text};}
    static NpcDialogueSequence.ResponseLine R(string text,NpcDialogueSequence.SpeakerRole role=NpcDialogueSequence.SpeakerRole.Npc,bool photo=false)
    {return new NpcDialogueSequence.ResponseLine{role=role,speaker=role==NpcDialogueSequence.SpeakerRole.Player?"Dawn":"The Counter Sales",text=text,offerPhoto=photo};}
    static NpcDialogueSequence Asset(string path,NpcDialogueSequence.Line[] lines)
    {
        var asset=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!asset){asset=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(asset,path);}
        asset.lines=lines;EditorUtility.SetDirty(asset);return asset;
    }
    static void CreateContent(out NpcDialogueSequence first,out NpcDialogueSequence reminder)
    {
        const string photo="I'm looking for someone.";
        const string recognition="Aneta. I know her. She is well known around this town.";
        var choice=N("We don't get many strangers here.");
        choice.choices=new[]{
            new NpcDialogueSequence.Choice{id="show_photo",label="Show photo",text=photo,offerPhoto=true,response=new[]{R(recognition)}},
            new NpcDialogueSequence.Choice{id="ask_why_alone",label="Why alone?",text="Why are you here all alone?",response=new[]{
                R("Someone always needs a little help."),R(photo,NpcDialogueSequence.SpeakerRole.Player,true),R(recognition)}}
        };
        first=Asset(DialoguePath,new[]{
            P("Thank you for saving me back there."),choice,
            P("Well known?"),N("Yes. She's been... trying to find a way to help this town."),
            P("Do you know where she is?"),N("I may have seen her not long ago. But the town government has it in for her. She keeps a low profile."),
            P("Is someone trying to kill her?"),N("Maybe. But knowing her, she should be all right."),
            P("Who might know something about her?"),N("You could ask the factory manager. Though he might not tell you the truth."),
            P("Where can I find him?"),N("His office is on the fourth floor of the factory. Try there."),
            N("Oh, and Aneta left this water gun for you the last time I saw her."),N("The town is dangerous these days. Please be careful.")
        });
        reminder=Asset(ReminderPath,new[]{N("His office is on the fourth floor of the factory. Try there."),N("The town is dangerous these days. Please be careful.")});
        AssetDatabase.SaveAssets();
    }
    static Transform[] All(Scene scene){return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();}
    static void ReplaceTableGun(Transform holder,bool is3D)
    {
        foreach(var item in holder.GetComponents<ContinuousInteractionItem>())UnityEngine.Object.DestroyImmediate(item);
        foreach(var collider in holder.GetComponentsInChildren<Collider2D>(true))collider.enabled=false;
        if(!is3D)return;
        var existing=holder.GetComponentInChildren<RetroWaterBlaster>(true);
        var oldVisual=holder.GetComponent<Recoil3DSpriteVisual>();
        float depth=oldVisual && oldVisual.meshRenderer?oldVisual.meshRenderer.transform.position.z:2.85f;
        foreach(var v in holder.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.enabled=false;EditorUtility.SetDirty(v);}
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(RetroWaterBlasterIntegration.Assembly);
        if(!prefab)throw new Exception("Latest water blaster prefab missing.");
        GameObject model;
        if(existing)model=existing.gameObject;
        else{
            foreach(var r in holder.GetComponentsInChildren<Renderer>(true))r.enabled=false;
            model=(GameObject)PrefabUtility.InstantiatePrefab(prefab,holder.gameObject.scene);
        }
        model.name="Table display - latest retro water blaster";
        model.transform.SetParent(holder,false);var scale=holder.lossyScale;
        model.transform.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1/Mathf.Abs(scale.z));
        model.transform.position=new Vector3(holder.position.x,holder.position.y,depth);
        model.transform.rotation=Quaternion.identity;
        var gun=model.GetComponent<RetroWaterBlaster>();gun.SetTankSize(RetroWaterBlaster.TankSize.Small);gun.SetFacing(Vector2.right);
        Bounds bounds=new Bounds();bool any=false;
        foreach(var r in model.GetComponentsInChildren<Renderer>())if(r.gameObject.activeInHierarchy){if(!any){bounds=r.bounds;any=true;}else bounds.Encapsulate(r.bounds);}
        if(any)model.transform.position+=Vector3.up*(holder.position.y-Mathf.Abs(holder.lossyScale.y)*.5f-bounds.min.y);
    }
    [MenuItem("Recoil/Dialogue/Install Clerk Aneta Conversation")]
    public static string Install()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required.");
        NpcDialogueSequence first,reminder;CreateContent(out first,out reminder);
        var previous=SceneManager.GetActiveScene();var report=new List<string>();
        foreach(string path in ScenePaths){
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{
                if(scene.isDirty)throw new Exception("Scene has unsaved edits: "+path);
                string backup="Backups/StoreClerkDialogue/2026-11-11/"+path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                var all=All(scene);var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).First(w=>w);
                var clerk=all.First(t=>t.name=="Adult store clerk - assembled 2D art");
                var talk=clerk.GetComponent<StoreClerkConversation>();if(!talk)talk=clerk.gameObject.AddComponent<StoreClerkConversation>();
                talk.world=world;talk.rescue=world.GetComponentInChildren<ServiceStoreEncounter>(true);talk.firstConversation=first;talk.reminder=reminder;
                var animator=clerk.GetComponentsInChildren<Animator>(true).FirstOrDefault(a=>a.isHuman);
                if(animator)talk.headAnchor=animator.GetBoneTransform(HumanBodyBones.Head);
                else{var head=clerk.Find("Conversation head anchor");if(!head){head=new GameObject("Conversation head anchor").transform;head.SetParent(clerk,false);head.localPosition=new Vector3(0,1.7f,0);}talk.headAnchor=head;}
                var point=all.FirstOrDefault(t=>t.name=="Counter front - reassurance distance origin");
                if(!point){point=new GameObject("Clerk interaction - counter front").transform;point.SetParent(world.transform,true);point.position=new Vector3(12.95f,.825f,0);}
                talk.interactionPoint=point;talk.interactionRange=1.5f;
                var pickup=all.FirstOrDefault(t=>t.name=="Water gun pickup");
                if(pickup){ReplaceTableGun(pickup,path.Contains("/3D/"));talk.tableGunDisplay=pickup.gameObject;}
                world.startWithWaterGun=false;world.objectiveWithoutCard=world.objectiveWithCard="Talk to The Counter Sales for clues about Aneta.";
                EditorUtility.SetDirty(world);EditorUtility.SetDirty(talk);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                report.Add(path+": clerk dialogue installed; table pickup removed; "+(path.Contains("/3D/")?"latest 3D gun display":"2D display retained"));
            }finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);AssetDatabase.SaveAssets();return string.Join("\n",report);
    }
}
