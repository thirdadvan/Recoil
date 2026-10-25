using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Adds only the tutorial assembly; never rebuilds existing store/outdoor geometry.
public static class ServiceStoreEncounterBuilder
{
    const string Path="Assets/Scenes/RecoilServiceStore.unity";
    static Sprite sprite;static Material material;
    static Transform Group(string name,Transform parent,Vector2 position)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=position;return t;}
    static void Shape(string name,Transform parent,Vector2 local,Vector2 size,int hex,int order,float angle=0)
    {
        var t=Group(name,parent,Vector2.zero);t.localPosition=local;t.localScale=new Vector3(size.x,size.y,1);t.localRotation=Quaternion.Euler(0,0,angle);
        var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(hex);r.sortingOrder=order;
    }
    static NpcDialogueSequence Sequence(string file,params string[] lines)
    {
        const string folder="Assets/RecoilPrototype/Dialogue";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/RecoilPrototype","Dialogue");
        string path=folder+"/"+file+".asset";
        var data=AssetDatabase.LoadAssetAtPath<NpcDialogueSequence>(path);
        if(!data){data=ScriptableObject.CreateInstance<NpcDialogueSequence>();AssetDatabase.CreateAsset(data,path);}
        data.lines=lines.Select(text=>new NpcDialogueSequence.Line{speaker="STORE CLERK",text=text}).ToArray();EditorUtility.SetDirty(data);return data;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(Path);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        if(scene.isDirty)throw new InvalidOperationException("Store has unsaved edits; save or back it up before applying.");
        try
        {
            SceneManager.SetActiveScene(scene);
            var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>()).Single();
            if(world.GetComponentInChildren<ServiceStoreEncounter>(true))return "Encounter already exists; edit it in Inspector.";
            var serialized=new SerializedObject(world);sprite=(Sprite)serialized.FindProperty("sceneSprite").objectReferenceValue;material=(Material)serialized.FindProperty("sceneMaterial").objectReferenceValue;
            if(!sprite || !material)throw new InvalidOperationException("Missing scene artwork source.");
            var root=Group("Store Clerk Encounter - editable",world.transform,new Vector2(13.8f,0));
            var encounter=root.gameObject.AddComponent<ServiceStoreEncounter>();encounter.world=world;
            encounter.introduction=Sequence("StoreClerk_Introduction","Stay there.","If you want to live, slowly walk to me.");
            encounter.reassurance=Sequence("StoreClerk_Reassurance","Be nice... Go to sleep!");
            encounter.approachPoint=Group("Counter front - reassurance distance origin",root,new Vector2(12.95f,.825f));
            var clerk=Group("Adult store clerk - assembled 2D art",root,new Vector2(14.05f,.15f));
            // Restrained silhouette, fitted work cardigan, blouse, bobbed hair; feet stay behind counter.
            Shape("Skirt",clerk,new Vector2(0,.58f),new Vector2(.46f,.75f),0x263b3b,0);
            Shape("Left leg",clerk,new Vector2(-.12f,.13f),new Vector2(.11f,.3f),0x766454,0);
            Shape("Right leg",clerk,new Vector2(.12f,.13f),new Vector2(.11f,.3f),0x766454,0);
            Shape("Waist",clerk,new Vector2(0,1.01f),new Vector2(.33f,.24f),0x35534e,1);
            Shape("Cardigan shoulders",clerk,new Vector2(0,1.25f),new Vector2(.51f,.34f),0x48655a,1);
            Shape("Blouse",clerk,new Vector2(-.03f,1.24f),new Vector2(.17f,.34f),0xb8ae8b,2);
            Shape("Left sleeve",clerk,new Vector2(-.31f,1.09f),new Vector2(.15f,.48f),0x3b584e,1,-16);
            Shape("Right sleeve",clerk,new Vector2(.3f,1.09f),new Vector2(.15f,.48f),0x3b584e,1,13);
            Shape("Left hand on counter",clerk,new Vector2(-.35f,.94f),new Vector2(.15f,.09f),0xb09578,4);
            Shape("Right hand on counter",clerk,new Vector2(.34f,.94f),new Vector2(.15f,.09f),0xb09578,4);
            Shape("Neck",clerk,new Vector2(-.02f,1.49f),new Vector2(.13f,.18f),0xae9175,1);
            Shape("Hair silhouette",clerk,new Vector2(0,1.7f),new Vector2(.4f,.49f),0x222c2c,1,-4);
            Shape("Face",clerk,new Vector2(-.05f,1.7f),new Vector2(.26f,.33f),0xc0a487,2,-4);
            Shape("Fringe",clerk,new Vector2(-.03f,1.87f),new Vector2(.34f,.12f),0x25302e,3,9);
            Shape("Side hair",clerk,new Vector2(.14f,1.68f),new Vector2(.1f,.36f),0x25302e,3,-4);
            Shape("Eye",clerk,new Vector2(-.115f,1.73f),new Vector2(.06f,.018f),0x353b32,3);
            Shape("Mouth",clerk,new Vector2(-.09f,1.61f),new Vector2(.065f,.015f),0x805c53,3);
            Shape("Name badge",clerk,new Vector2(.13f,1.3f),new Vector2(.11f,.045f),0xc3b58c,3);
            var monster=Group("Rampager - beside store counter",root,new Vector2(12.45f,.8f));
            var body=monster.gameObject.AddComponent<SpriteRenderer>();body.sprite=sprite;body.sharedMaterial=material;body.drawMode=SpriteDrawMode.Sliced;body.size=new Vector2(.65f,1.6f);body.color=RecoilPrototype.Hex(0xb87570);body.sortingOrder=0;
            Shape("Eye",monster,new Vector2(-.15f,.45f),new Vector2(.12f,.06f),0xff3434,1);
            var enemy=monster.gameObject.AddComponent<RampagerController>();enemy.Configure(world,-2,2);enemy.initialState=RampagerController.Behaviour.Sleeping;
            enemy.watchStandOff=30;enemy.idealSightRange=48;enemy.reportRadius=0;enemy.EnsureStandardComponents();
            encounter.monster=enemy;
            var hatch=world.GetComponentsInChildren<RecoilDoor>(true).Single(d=>d.name=="Central skylight");hatch.startsOpened=true;hatch.ResetDoor(world);EditorUtility.SetDirty(hatch);
            world.objectiveWithoutCard="Follow the clerk's instructions. Find the water gun, then leave through the open skylight.";world.objectiveWithCard=world.objectiveWithoutCard;
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            return "Store tutorial saved. Outdoor left untouched.";
        }
        finally{if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
}
