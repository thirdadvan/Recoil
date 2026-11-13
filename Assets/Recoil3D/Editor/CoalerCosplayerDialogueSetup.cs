using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CoalerCosplayerDialogueSetup
{
    public const string ScenePath="Assets/Scenes/3D/RecoilFactoryInterior.unity";
    public const string AssetPath="Assets/RecoilPrototype/Dialogue/Cosplayer_ManagerGlasses.asset";
    static NpcDialogueSequence.Line N(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="The Disguised Man",text=text};}
    static NpcDialogueSequence.Line P(string text){return new NpcDialogueSequence.Line{role=NpcDialogueSequence.SpeakerRole.Player,speaker="Dawn",text=text};}
    static NpcDialogueSequence.ResponseLine R(string text){return new NpcDialogueSequence.ResponseLine{role=NpcDialogueSequence.SpeakerRole.Npc,speaker="The Disguised Man",text=text};}
    public const string IntroductionPath="Assets/Resources/RecoilDialogue/Cosplayer_Introduction.asset";
    public const string ReminderPath="Assets/Resources/RecoilDialogue/Cosplayer_GirlfriendReminder.asset";
    static NpcDialogueSequence Save(string path,params NpcDialogueSequence.Line[] lines)
    {
        var asset=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!asset){asset=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(asset,path);}
        asset.lines=lines;EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);return asset;
    }
    public static NpcDialogueSequence CreateContent()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources/RecoilDialogue");AssetDatabase.Refresh();
        var name=N("Ewa. I love her. I have to find her.");name.progressFlag=CoalerCosplayerConversation.GirlfriendNameFlag;
        Save(IntroductionPath,
            P("What are you doing here?"),N("I'm looking for my girlfriend."),P("What's her name?"),name,
            N("She came to work at this factory. I haven't heard from her since."),N("If you hear anything about her, please tell me."));
        Save(ReminderPath,N("I'm still looking for Ewa. I love her. I'm not leaving without her."),N("If you hear anything about her, please tell me."));
        var choice=N("What, did he send you to get them back?");
        choice.choices=new[]{
            new NpcDialogueSequence.Choice{id="request",label="Ask politely",text="I'm looking for someone too. Please tell me where you put the glasses.",response=new[]{R("You're looking for someone too? ...All right. Don't tell him I told you.")}},
            new NpcDialogueSequence.Choice{id="demand",label="Demand answers",text="Settle your grudge with him later. Where are the glasses? Tell me now.",response=new[]{R("You like giving orders, don't you? ...Fine. They're no use to me anyway.")}},
            new NpcDialogueSequence.Choice{id="intimidate",label="Threaten exposure",text="Tell me where the glasses are, and I won't expose you to the manager.",response=new[]{R("...Keep your voice down. I'll tell you. Just leave her out of this.")}}
        };
        var location=N("The warehouse roof. I threw them up there.");location.progressFlag=CoalerCosplayerConversation.GlassesLocationFlag;
        return Save(AssetPath,N("I did. Keep it down. His glasses are perfectly fine."),P("Why take them?"),
            N("Ewa told me what that manager is like. I don't want him recognizing me."),N("And I wanted to make his life a little harder."),choice,
            location,P("The warehouse roof... I'll take a look."),N("Take them and go. And don't mention me to the manager."));
    }
    [MenuItem("Recoil/Dialogue/Install Cosplayer Conversation")]
    public static string Install()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try {
            if(scene.isDirty)throw new Exception("Preserving unsaved scene edits: "+scene.path);
            CoalerCosplayerLookAt actor=null;RecoilPrototype world=null;
            foreach(var root in scene.GetRootGameObjects()){
                if(!actor)actor=root.GetComponentInChildren<CoalerCosplayerLookAt>(true);
                if(!world)world=root.GetComponentInChildren<RecoilPrototype>(true);
            }
            if(!actor || !world)throw new Exception("Factory cosplayer or game world missing.");
            var component=actor.GetComponent<CoalerCosplayerConversation>();
            if(!component)component=actor.gameObject.AddComponent<CoalerCosplayerConversation>();
            component.world=world;component.dialogue=CreateContent();component.headAnchor=actor.head;component.distanceOrigin=actor.distanceOrigin;component.interactionRange=2.7f;
            EditorUtility.SetDirty(component);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Installed girlfriend introduction and manager-gated Glasses topic with 3 branches; "+ScenePath;
        } finally {
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            if(opened)EditorSceneManager.CloseScene(scene,true);
        }
    }
}
