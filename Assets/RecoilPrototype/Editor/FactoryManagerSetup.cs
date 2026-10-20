using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Adds only the NPC to the existing authored office; never regenerates room geometry.
public static class FactoryManagerSetup
{
    const string RoomPath="Assets/Scenes/FactoryRooms/FactoryManagerOfficeF4.unity";
    const string PrefabPath="Assets/RecoilPrototype/Prefabs/FactoryManager.prefab";
    static Sprite sprite;static Material material;
    static Transform Shape(string name,Transform parent,Vector2 p,Vector2 size,int color,int order=10)
    {
        var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;t.localScale=new Vector3(size.x,size.y,1);
        var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;return t;
    }
    [MenuItem("Recoil/Levels/Add Manager And Warehouse Key")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var active=SceneManager.GetActiveScene();var room=SceneManager.GetSceneByPath(RoomPath);bool opened=!room.isLoaded;
        if(opened)room=EditorSceneManager.OpenScene(RoomPath,OpenSceneMode.Additive);
        try
        {
            var world=room.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
            if(world.GetComponentsInChildren<ContinuousInteractionItem>(true).Any(i=>i.itemId=="factory_manager_warehouse_key"))throw new InvalidOperationException("Manager already installed.");
            string backup="Backups/FactoryManager/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(room,backup+"/ManagerOffice.unity",true);
            var data=new SerializedObject(world);sprite=(Sprite)data.FindProperty("sceneSprite").objectReferenceValue;material=(Material)data.FindProperty("sceneMaterial").objectReferenceValue;
            if(!sprite || !material)throw new InvalidOperationException("Office art references are missing.");
            SceneManager.SetActiveScene(room);
            var root=new GameObject("Factory Manager - E to talk").transform;root.SetParent(world.transform,false);
            var talk=root.gameObject.AddComponent<ContinuousInteractionItem>();
            talk.itemId="factory_manager_warehouse_key";talk.displayName="TALK TO THE MANAGER";talk.description="The manager has a key to the warehouse storeroom.";
            talk.interactionDuration=0;talk.interactionRange=1.8f;talk.resultId="warehouse_locked_room_key";
            talk.resultMessage="Manager: Take this key. It opens the warehouse storeroom.\nWarehouse key acquired.";
            var appearance=new GameObject("Appearance - grouped").transform;appearance.SetParent(root,false);
            Shape("Left trouser leg",appearance,new Vector2(-.12f,-.5f),new Vector2(.18f,.55f),0x343d3c);
            Shape("Right trouser leg",appearance,new Vector2(.12f,-.5f),new Vector2(.18f,.55f),0x303836);
            Shape("Left shoe",appearance,new Vector2(-.14f,-.78f),new Vector2(.25f,.09f),0x1c2423,11);
            Shape("Right shoe",appearance,new Vector2(.14f,-.78f),new Vector2(.25f,.09f),0x1c2423,11);
            Shape("Jacket",appearance,new Vector2(0,-.025f),new Vector2(.52f,.76f),0x526361,12);
            Shape("Shirt",appearance,new Vector2(0,.23f),new Vector2(.22f,.28f),0xb1aa91,13);
            Shape("Tie",appearance,new Vector2(0,.1f),new Vector2(.045f,.27f),0x776148,14);
            Shape("Left sleeve",appearance,new Vector2(-.31f,-.045f),new Vector2(.13f,.6f),0x485957,11);
            Shape("Right sleeve",appearance,new Vector2(.31f,-.045f),new Vector2(.13f,.6f),0x485957,11);
            Shape("Left hand",appearance,new Vector2(-.31f,-.37f),new Vector2(.11f,.15f),0xb1a48c,12);
            Shape("Right hand",appearance,new Vector2(.31f,-.37f),new Vector2(.11f,.15f),0xb1a48c,12);
            Shape("Neck",appearance,new Vector2(0,.43f),new Vector2(.14f,.15f),0xa5977e,12);
            Shape("Head",appearance,new Vector2(0,.6f),new Vector2(.33f,.35f),0xb1a48c,13);
            Shape("Grey hair",appearance,new Vector2(0,.775f),new Vector2(.35f,.09f),0x96998b,14);
            Shape("Glasses",appearance,new Vector2(0,.64f),new Vector2(.35f,.035f),0x333d39,14);
            Shape("Jacket seam",appearance,new Vector2(.03f,-.13f),new Vector2(.014f,.45f),0x334443,13);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabPath);UnityEngine.Object.DestroyImmediate(root.gameObject);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),world.transform);
            // Between the two existing desks; feet touch the unchanged office floor.
            instance.transform.localPosition=new Vector3(6.2f,.825f,0);
            EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);AssetDatabase.SaveAssets();
            Debug.Log("FACTORY_MANAGER_ADDED / instantaneous E dialogue / warehouse_locked_room_key");
        }
        finally
        {
            if(active.IsValid() && active.isLoaded)SceneManager.SetActiveScene(active);
            if(opened && room.isLoaded)EditorSceneManager.CloseScene(room,true);
        }
    }
}
