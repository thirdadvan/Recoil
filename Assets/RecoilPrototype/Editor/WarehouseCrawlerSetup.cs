using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseCrawlerSetup
{
    const string ScenePath="Assets/Scenes/RecoilFactoryWarehouse.unity";
    const string PrefabPath="Assets/RecoilPrototype/Prefabs/SurfaceCrawler.prefab";
    static Sprite sprite;static Material material;
    static Transform Group(string name,Transform parent,Vector2 local)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=local;return t;}
    static Transform Shape(string name,Transform parent,Vector2 local,Vector2 size,int color,int order=10,float rotation=0)
    {
        var t=Group(name,parent,local);t.localScale=new Vector3(size.x,size.y,1);t.localRotation=Quaternion.Euler(0,0,rotation);
        var r=t.gameObject.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=material;r.color=RecoilPrototype.Hex(color);r.sortingOrder=order;return t;
    }
    static SurfaceCrawlerController CreateCreature(Transform parent)
    {
        var root=Group("Surface Crawler - slow climber",parent,Vector2.zero);
        var controller=root.gameObject.AddComponent<SurfaceCrawlerController>();var visual=root.GetComponent<SurfaceCrawlerVisuals>();
        visual.visualRoot=Group("Visual - articulated silhouette",root,Vector2.zero);
        visual.abdomen=Group("Abdomen",visual.visualRoot,new Vector2(-.06f,0));
        Shape("Carapace",visual.abdomen,Vector2.zero,new Vector2(.67f,.27f),0x7b8171,11,-6);
        Shape("Raised back",visual.abdomen,new Vector2(-.13f,.1f),new Vector2(.3f,.12f),0x565e53,12,-10);
        Shape("Spine",visual.abdomen,new Vector2(-.04f,.045f),new Vector2(.5f,.035f),0xa1a18a,12,-6);
        for(int i=0;i<4;i++)Shape("Carapace seam",visual.abdomen,new Vector2(-.25f+i*.13f,0),new Vector2(.018f,.21f),0x363d36,13,-13);
        visual.head=Group("Head",visual.visualRoot,new Vector2(.34f,.005f));
        Shape("Skull",visual.head,Vector2.zero,new Vector2(.25f,.21f),0x9b9b83,13,-15);
        Shape("Dark face",visual.head,new Vector2(.085f,-.025f),new Vector2(.1f,.15f),0x272c29,14,-15);
        visual.eyes=new SpriteRenderer[2];
        for(int i=0;i<2;i++)visual.eyes[i]=Shape("Eye",visual.head,new Vector2(.12f,.015f-i*.055f),new Vector2(.045f,.025f),0xb67450,15).GetComponent<SpriteRenderer>();
        visual.upperLimbs=new Transform[6];visual.lowerLimbs=new Transform[6];
        for(int i=0;i<6;i++)
        {
            var shoulder=Group("Limb "+(i+1)+" - shoulder",visual.visualRoot,new Vector2(-.25f+(i/2)*.22f,-.025f));
            visual.upperLimbs[i]=shoulder;Shape("Upper limb",shoulder,new Vector2(0,-.09f),new Vector2(.045f,.23f),i%2==0?0x8e907a:0x444f44,10);
            var elbow=Group("Elbow",shoulder,new Vector2(0,-.19f));visual.lowerLimbs[i]=elbow;
            Shape("Lower limb",elbow,new Vector2(0,-.1f),new Vector2(.032f,.22f),0x727c68,10);
            Shape("Claw",elbow,new Vector2(.035f,-.19f),new Vector2(.1f,.035f),0xafaa8c,11);
        }
        visual.Show(false);return controller;
    }
    [MenuItem("Recoil/Levels/Add Warehouse Surface Crawler")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play first.");
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();
        if(world.GetComponentInChildren<WarehouseCrawlerEncounter>(true))throw new InvalidOperationException("Warehouse already contains its crawler encounter.");
        var folder="Backups/WarehouseCrawler/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(folder);EditorSceneManager.SaveScene(scene,folder+"/Warehouse.unity",true);
        var serialized=new SerializedObject(world);sprite=(Sprite)serialized.FindProperty("sceneSprite").objectReferenceValue;material=(Material)serialized.FindProperty("sceneMaterial").objectReferenceValue;
        if(!sprite || !material)throw new InvalidOperationException("Scene art references are missing.");
        SceneManager.SetActiveScene(scene);
        var parent=Group("Warehouse Crawler Encounter - editable",world.transform,Vector2.zero);
        var encounter=parent.gameObject.AddComponent<WarehouseCrawlerEncounter>();
        var hard=world.GetComponentsInChildren<BoxCollider2D>(true);
        float left=hard.First(c=>c.name=="Left hard wall").bounds.max.x,right=hard.First(c=>c.name=="Right hard wall").bounds.min.x;
        float bottom=hard.First(c=>c.name=="Floor").bounds.max.y,top=hard.First(c=>c.name=="Ceiling").bounds.min.y;
        var navObject=Group("Climbable navigation bounds",parent,new Vector2((left+right)*.5f,(bottom+top)*.5f));
        var box=navObject.gameObject.AddComponent<BoxCollider2D>();box.isTrigger=true;box.size=new Vector2(right-left,top-bottom);
        encounter.navigation=navObject.gameObject.AddComponent<CrawlerNavigation>();encounter.navigation.showGraph=true;
        var creature=CreateCreature(parent);PrefabUtility.SaveAsPrefabAsset(creature.gameObject,PrefabPath);UnityEngine.Object.DestroyImmediate(creature.gameObject);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);creature=((GameObject)PrefabUtility.InstantiatePrefab(prefab,parent)).GetComponent<SurfaceCrawlerController>();encounter.crawler=creature;
        encounter.doorSpawnPoints=world.GetComponentsInChildren<RecoilDoor>(true).Select(d=>Group(d.name+" - crawler entry",d.transform,new Vector2(0,bottom+.245f-d.transform.position.y))).ToArray();
        // Markers belong to their doors, so later door edits also move the crawler entrance.
        encounter.discoveryDuration=20;creature.pounceRange=6;creature.preparationTime=1.2f;
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
        Debug.Log("WAREHOUSE_CRAWLER_INSTALLED / 20s discovery / 6m pounce / 1.2s preparation");
    }
}
