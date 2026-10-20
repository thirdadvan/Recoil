using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ForegroundDoorExampleBuilder
{
    [MenuItem("Recoil/Examples/Create Foreground Door Example")]
    public static void Create()
    {
        const string path="Assets/Scenes/ForegroundDoorExample.unity";
        if(System.IO.File.Exists(path)){Debug.Log("Foreground door example already exists: "+path);return;}
        var previous=SceneManager.GetActiveScene();bool dirty=false;
        for(int i=0;i<SceneManager.sceneCount;i++)dirty|=SceneManager.GetSceneAt(i).isDirty;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var square=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RecoilPrototype/Art/PrototypeSquare.png");
        SpriteRenderer Box(string name,Transform parent,Vector2 position,Vector2 size,Color color,int order)
        {
            var obj=new GameObject(name);if(parent)obj.transform.SetParent(parent,false);
            obj.transform.localPosition=position;obj.transform.localScale=new Vector3(size.x,size.y,1);
            var sprite=obj.AddComponent<SpriteRenderer>();sprite.sprite=square;sprite.color=color;sprite.sortingOrder=order;return sprite;
        }
        Box("Background wall",null,new Vector2(0,2),new Vector2(18,10),new Color(.09f,.12f,.14f),-20);
        Box("Walkway",null,new Vector2(0,-.23f),new Vector2(18,.45f),new Color(.24f,.27f,.28f),0);
        var player=new GameObject("Example Player (A D)");player.transform.position=new Vector3(-3.5f,0,0);player.AddComponent<ForegroundDoorExamplePlayer>();
        Box("Coat",player.transform,new Vector2(0,.65f),new Vector2(.55f,1.05f),new Color(.65f,.78f,.74f),10);
        Box("Head",player.transform,new Vector2(0,1.32f),new Vector2(.38f,.4f),new Color(.86f,.73f,.55f),11);
        Box("Hat",player.transform,new Vector2(0,1.56f),new Vector2(.6f,.12f),new Color(.35f,.48f,.49f),12);
        var door=new GameObject("Foreground Door - visual only");
        Box("Door panel",door.transform,new Vector2(0,1.35f),new Vector2(1.9f,2.7f),new Color(.27f,.34f,.31f),40);
        Box("Left jamb",door.transform,new Vector2(-1.04f,1.42f),new Vector2(.18f,2.84f),new Color(.44f,.47f,.42f),42);
        Box("Right jamb",door.transform,new Vector2(1.04f,1.42f),new Vector2(.18f,2.84f),new Color(.44f,.47f,.42f),42);
        Box("Lintel",door.transform,new Vector2(0,2.85f),new Vector2(2.26f,.18f),new Color(.5f,.52f,.46f),42);
        Box("Lower panel",door.transform,new Vector2(0,.6f),new Vector2(1.5f,.7f),new Color(.2f,.26f,.24f),41);
        Box("Window",door.transform,new Vector2(0,2.05f),new Vector2(1.25f,.65f),new Color(.11f,.19f,.18f),41);
        Box("Handle",door.transform,new Vector2(.67f,1.3f),new Vector2(.09f,.28f),new Color(.8f,.68f,.4f),43);
        var fade=door.AddComponent<RecoilForegroundOccluder>();fade.player=player.transform;fade.foregroundRenderers=door.GetComponentsInChildren<SpriteRenderer>();fade.overlapCenter=new Vector2(0,1.4f);fade.overlapSize=new Vector2(2.3f,2.9f);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=4.3f;camera.transform.position=new Vector3(0,1.8f,-10);camera.backgroundColor=new Color(.07f,.09f,.1f);camera.gameObject.AddComponent<AudioListener>();
        EditorSceneManager.SaveScene(scene,path);
        if(dirty){EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);Debug.Log("Created foreground door example. Open "+path+" to preview; your unsaved scene was preserved.");}
        else EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        Debug.Log("RECOIL_FOREGROUND_DOOR_EXAMPLE_CREATED");
    }
}
