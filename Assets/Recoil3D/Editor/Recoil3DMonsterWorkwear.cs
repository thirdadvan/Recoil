using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Saved model edits only: never scale actor roots or their patrol waypoints.
public static class Recoil3DMonsterWorkwear
{
    static Material cloth,pocket,boots;
    static Material Material(string name,Color colour)
    {
        string path="Assets/Recoil3D/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=colour;m.SetFloat("_Metallic",0);m.SetFloat("_Glossiness",.08f);m.SetFloat("_Weathering",.35f);m.SetFloat("_HazeDensity",0);m.SetColor("_EmissionColor",Color.black);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void Materials()
    {cloth=Material("Monster - ochre orange work trousers",new Color(.92f,.47f,.065f));pocket=Material("Monster - worn canvas pockets",new Color(.66f,.30f,.035f));boots=Material("Monster - rubber work boots",new Color(.055f,.052f,.043f));}
    static void Part(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {
        var t=parent.Find(name);GameObject g;
        if(t)g=t.gameObject;else{g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent,false);Object.DestroyImmediate(g.GetComponent<Collider>());}
        g.transform.localPosition=position;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
    }
    public static void Dress(Recoil3DActorVisual actor)
    {
        if(!actor.monster||!actor.model)return;Materials();
        var body=actor.model.Find("Body dimensions");if(!body)return;
        body.localScale=Vector3.one*1.3f;body.localPosition=new Vector3(0,.24f,0);
        var motor=actor.GetComponent<RampagerMovement>();if(motor){motor.bodyScale=1.3f;EditorUtility.SetDirty(motor);}
        Part("Workwear - trouser waist",body,new Vector3(0,-.235f,0),new Vector3(.42f,.19f,.31f),cloth);
        Part("Workwear - front bib",body,new Vector3(0,-.035f,-.16f),new Vector3(.29f,.26f,.035f),cloth);
        Part("Workwear - back bib",body,new Vector3(0,-.035f,.16f),new Vector3(.29f,.26f,.035f),cloth);
        foreach(float side in new[]{-1f,1f})
        {
            Part("Workwear - suspender front "+side,body,new Vector3(side*.11f,.20f,-.15f),new Vector3(.04f,.23f,.035f),cloth);
            Part("Workwear - suspender rear "+side,body,new Vector3(side*.11f,.20f,.15f),new Vector3(.04f,.23f,.035f),cloth);
        }
        foreach(var leg in new[]{actor.leftLeg,actor.rightLeg})if(leg)
        {
            var mesh=leg.Find(leg.name+" mesh");if(mesh){mesh.localPosition=new Vector3(0,-.215f,0);mesh.localScale=new Vector3(.16f,.43f,.19f);mesh.GetComponent<Renderer>().sharedMaterial=cloth;}
            Part("Workwear - cargo pocket",leg,new Vector3(leg==actor.leftLeg?-.085f:.085f,-.16f,0),new Vector3(.025f,.16f,.14f),pocket);
            Part("Workwear - reinforced knee",leg,new Vector3(0,-.30f,-.103f),new Vector3(.13f,.10f,.015f),pocket);
            Part("Workwear - safety boot",leg,new Vector3(0,-.46f,-.025f),new Vector3(.17f,.12f,.25f),boots);
        }
        actor.parts=actor.model.GetComponentsInChildren<Renderer>(true);EditorUtility.SetDirty(actor);
    }
    static void Crawler(SurfaceCrawlerVisuals visual)
    {
        if(!visual.visualRoot)return;Materials();visual.visualScale=1.3f;visual.visualRoot.localScale=Vector3.one*1.3f;
        // The crawler retains its six-limbed silhouette; rear paired limbs carry torn trouser fabric.
        var limbs=visual.upperLimbs;
        if(limbs!=null)for(int i=Mathf.Max(0,limbs.Length-2);i<limbs.Length;i++)
        {
            foreach(var v in limbs[i].GetComponentsInChildren<Recoil3DSpriteVisual>(true))
            {if(v.source){v.source.color=cloth.color;EditorUtility.SetDirty(v.source);}v.tint=Color.white;if(v.meshRenderer)v.meshRenderer.sharedMaterial=cloth;v.Sync();EditorUtility.SetDirty(v);}
        }
        EditorUtility.SetDirty(visual);
    }
    public static int ApplyScene(Scene scene)
    {
        int count=0;
        foreach(var a in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DActorVisual>(true)).Where(a=>a.monster)){Dress(a);count++;}
        foreach(var c in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SurfaceCrawlerVisuals>(true))){Crawler(c);count++;}
        // Navigation clearance follows the enlarged crawler without changing its AI or speed.
        foreach(var n in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<CrawlerNavigation>(true))){n.bodyRadius=.22f*1.3f;EditorUtility.SetDirty(n);}
        if(count>0)EditorSceneManager.MarkSceneDirty(scene);return count;
    }
    public static void ApplyCrawlerPrefab()
    {
        const string path="Assets/Recoil3D/Prefabs/SurfaceCrawler.prefab";
        var root=PrefabUtility.LoadPrefabContents(path);
        try{foreach(var c in root.GetComponentsInChildren<SurfaceCrawlerVisuals>(true))Crawler(c);PrefabUtility.SaveAsPrefabAsset(root,path);}
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
}
