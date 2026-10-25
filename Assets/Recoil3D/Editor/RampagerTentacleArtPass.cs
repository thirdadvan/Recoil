using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RampagerTentacleArtPass
{
    const string Folder="Assets/Recoil3D/Materials/TentacleWorker";
    public const string TestScene="Assets/Scenes/Tests/RampagerMorphologyTest.unity";
    static Material Material(string name,Color colour,float gloss,float metal=0,bool emit=false)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.shader=Shader.Find("Recoil/3D/Weathered Surface");
        m.color=colour;m.SetFloat("_Glossiness",gloss);m.SetFloat("_Metallic",metal);m.enableInstancing=true;
        // Existing opaque dither shader preserves depth, shadows and story fades.
        m.SetFloat("_Weathering",.34f);m.SetFloat("_HazeDensity",0);m.DisableKeyword("_ALPHABLEND_ON");m.renderQueue=2000;
        if(emit){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",new Color(.31f,.006f,.012f));}
        EditorUtility.SetDirty(m);return m;
    }
    public static void Configure(RampagerTentacleVisual v)
    {
        System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
        v.tentacleMaterial=Material("Black fibrous upper body",new Color(.032f,.036f,.033f),.32f,.025f);
        v.trouserMaterial=Material("Worn safety orange",new Color(.85f,.31f,.035f),.13f);
        v.patchMaterial=Material("Charcoal knee reinforcements",new Color(.105f,.10f,.085f),.11f);
        v.bootMaterial=Material("Heavy work boots",new Color(.035f,.03f,.023f),.16f);
        v.coreMaterial=Material("Dark red suspended core",new Color(.28f,.012f,.018f),.64f,.05f,true);
        v.badgeMaterial=Material("Faded identity card",new Color(.64f,.59f,.44f),.1f);
        v.Build();EditorUtility.SetDirty(v);
    }
    [MenuItem("Recoil/3D/Apply Tentacle Worker Visuals")]
    public static void ApplyAll()
    {
        const string backup="Backups/TentacleMonster/20261022/Scenes";System.IO.Directory.CreateDirectory(backup);
        int count=0,sceneCount=0;
        foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes/3D"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;
            if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var actors=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DActorVisual>(true)).Where(a=>a.monster).ToArray();
                if(actors.Length==0)continue;
                EditorSceneManager.SaveScene(scene,backup+"/"+System.IO.Path.GetFileName(path),true);
                foreach(var actor in actors)
                {
                    var visual=actor.GetComponent<RampagerTentacleVisual>();if(!visual)visual=actor.gameObject.AddComponent<RampagerTentacleVisual>();
                    Configure(visual);if(actor.model)actor.model.gameObject.SetActive(false);count++;
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);sceneCount++;
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        AssetDatabase.SaveAssets();Debug.Log("Tentacle worker visual applied: "+count+" actors across "+sceneCount+" scenes. 2D originals retained.");
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material material)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.position=p;g.transform.localScale=scale;g.GetComponent<Renderer>().sharedMaterial=material;return g;}
    [MenuItem("Recoil/3D/Create Tentacle Worker Art Test")]
    public static void CreateTest()
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes/Tests");
        if(System.IO.File.Exists(TestScene))throw new System.InvalidOperationException("Art test scene already exists; open it instead of overwriting.");
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        var setup=new GameObject("Tentacle worker - art prototype controls").AddComponent<RampagerMorphologyDemo>();
        var actor=new GameObject("Rampager - seven form specimen");actor.transform.position=new Vector3(0,.8f,0);
        var v=actor.AddComponent<RampagerTentacleVisual>();v.followAI=false;v.previewForm=RampagerTentacleVisual.Form.Watching;Configure(v);setup.specimen=v;
        var neutral=new Material(Shader.Find("Standard"));neutral.color=new Color(.23f,.25f,.25f);neutral.SetFloat("_Glossiness",.12f);AssetDatabase.CreateAsset(neutral,Folder+"/Test stage.mat");
        Primitive("Matte ground",PrimitiveType.Cube,new Vector3(0,-.08f,0),new Vector3(12,.15f,12),neutral);
        Primitive("Background wall",PrimitiveType.Cube,new Vector3(0,2,3.2f),new Vector3(12,4.5f,.2f),neutral);
        var target=new GameObject("Grey player placeholder");target.transform.position=new Vector3(1.28f,.8f,-.1f);v.previewTarget=target.transform;
        Primitive("Placeholder torso",PrimitiveType.Capsule,target.transform.position,new Vector3(.34f,.43f,.27f),neutral).transform.SetParent(target.transform,true);
        Primitive("Placeholder head",PrimitiveType.Sphere,new Vector3(1.28f,1.46f,-.1f),new Vector3(.23f,.29f,.23f),neutral).transform.SetParent(target.transform,true);
        foreach(float x in new[]{1.18f,1.38f})Primitive("Placeholder leg",PrimitiveType.Capsule,new Vector3(x,.34f,-.1f),new Vector3(.14f,.32f,.16f),neutral).transform.SetParent(target.transform,true);
        var cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.transform.position=new Vector3(3.1f,2.05f,-6.3f);cam.transform.LookAt(new Vector3(.25f,1.05f,0));cam.fieldOfView=34;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.10f,.12f,.13f);cam.gameObject.AddComponent<AudioListener>();
        var key=new GameObject("Large soft key").AddComponent<Light>();key.type=LightType.Directional;key.intensity=1.15f;key.color=new Color(.86f,.91f,1);key.transform.rotation=Quaternion.Euler(35,-25,0);key.shadows=LightShadows.Soft;
        var rim=new GameObject("Warm silhouette rim").AddComponent<Light>();rim.type=LightType.Point;rim.transform.position=new Vector3(-2,2.5f,1.5f);rim.range=7;rim.intensity=2.3f;rim.color=new Color(1,.66f,.37f);
        RenderSettings.ambientLight=new Color(.24f,.25f,.27f);RenderSettings.fog=false;
        foreach(var r in scene.GetRootGameObjects())foreach(var t in r.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        cam.cullingMask=1<<30;
        EditorSceneManager.SaveScene(scene,TestScene);AssetDatabase.SaveAssets();SceneManager.SetActiveScene(previous);
        Debug.Log("Created "+TestScene);
    }
}

[CustomEditor(typeof(RampagerTentacleVisual))]
public sealed class RampagerTentacleVisualInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();var v=(RampagerTentacleVisual)target;
        EditorGUILayout.HelpBox("游戏运行时强制跟随怪物 AI，Attack 对应 Enraged。预览和自动循环仅用于编辑器或独立展示角色。长臂始终为自身左侧。",MessageType.Info);
        if(GUILayout.Button("Rebuild procedural visual")){v.Build();EditorUtility.SetDirty(v);}
        using(new EditorGUI.DisabledScope(Application.isPlaying && v.GetComponent<RampagerController>()))
        for(int i=0;i<8;i++)if(GUILayout.Button(((RampagerTentacleVisual.Form)i).ToString())){Undo.RecordObject(v,"Preview monster form");v.SetPreview((RampagerTentacleVisual.Form)i);for(int f=0;f<50;f++)v.PreviewTick(.05f);EditorUtility.SetDirty(v);SceneView.RepaintAll();}
    }
}
