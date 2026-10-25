using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Saved native decoration only. No runtime generation, added colliders, door conditions or layout changes.
public static class Recoil3DArchitecturePolish
{
    const string Marker="Architectural finish - metric masonry and wear";
    static Material brick,concrete,plaster,greenPaint,steel,wood,rustStain,damp,puddle,shadow;
    static readonly Dictionary<Material,Material> doorFinishes=new Dictionary<Material,Material>();
    static Material Finish(string name,int kind,Color c,float metallic=0,float smooth=.14f,float coverage=0)
    {
        string path="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Metric Architectural Surface"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseTint",c);m.SetColor("_MortarTint",new Color(.15f,.16f,.15f));m.SetFloat("_Kind",kind);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);m.SetFloat("_PlasterCoverage",coverage);m.SetVector("_BrickSize",new Vector4(.26f,.085f,.006f,0));m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static Material StainMat(string n,Color c,int shape)
    {var path="Assets/Recoil3D/Materials/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Architectural Runoff"));AssetDatabase.CreateAsset(m,path);}m.SetColor("_Tint",c);m.SetFloat("_Shape",shape);EditorUtility.SetDirty(m);return m;}
    static void Materials()
    {
        brick=Finish("Architecture - metric brick and worn plaster",1,new Color(.27f,.235f,.205f),0,.12f,.15f);
        concrete=Finish("Architecture - pitted concrete",0,new Color(.255f,.275f,.27f));plaster=Finish("Architecture - aged room plaster",2,new Color(.31f,.325f,.305f));
        greenPaint=Finish("Architecture - worn lower wall paint",2,new Color(.16f,.215f,.19f));steel=Finish("Architecture - matte painted steel",3,new Color(.225f,.255f,.245f),.4f,.2f);wood=Finish("Architecture - old timber",4,new Color(.205f,.18f,.13f));shadow=Finish("Architecture - recessed joints",0,new Color(.045f,.052f,.05f));
        rustStain=StainMat("Wear - rust beneath metal joints",new Color(.24f,.115f,.055f,.5f),0);damp=StainMat("Wear - rising wall damp",new Color(.055f,.082f,.068f,.48f),1);puddle=StainMat("Wear - shallow ground moisture",new Color(.045f,.065f,.06f,.48f),2);
        foreach(string n in new[]{"Worn industrial steel","Flaking oxidised metal","Faded machine enamel"})
        {var m=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+n+".mat");if(m){m.SetFloat("_Metallic",n.Contains("steel")?.42f:.25f);m.SetFloat("_Glossiness",n.Contains("steel")?.20f:.13f);m.SetFloat("_Weathering",.6f);EditorUtility.SetDirty(m);}}
    }
    static Transform MetricGroup(string n,Transform p,Vector3 position)
    {var t=new GameObject(n).transform;t.SetParent(p,false);t.position=position;t.rotation=Quaternion.identity;var sc=p.lossyScale;t.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(sc.x)),1/Mathf.Max(.001f,Mathf.Abs(sc.y)),1/Mathf.Max(.001f,Mathf.Abs(sc.z)));return t;}
    static GameObject Part(string n,Transform p,Vector3 pos,Vector3 size,Material m)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=n;g.transform.SetParent(p,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=m;g.isStatic=true;return g;}
    static void Stain(string n,Transform p,Vector3 pos,Vector2 size,Material m,bool ground=false)
    {var g=GameObject.CreatePrimitive(PrimitiveType.Quad);g.name=n;g.transform.SetParent(p,false);g.transform.position=pos;g.transform.rotation=ground?Quaternion.Euler(90,0,0):Quaternion.identity;var sc=p.lossyScale;g.transform.localScale=new Vector3(size.x/Mathf.Abs(sc.x),size.y/Mathf.Abs(sc.y),1/Mathf.Abs(sc.z));UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;g.isStatic=true;}
    static string Context(Transform t)
    {return (t.name+" "+(t.parent?t.parent.name:"")+" "+(t.parent&&t.parent.parent?t.parent.parent.name:"")).ToLowerInvariant();}
    static bool Visible(MeshRenderer r)
    {if(!r.enabled||!r.gameObject.activeInHierarchy||r.GetComponent<TextMesh>())return false;var v=r.GetComponentInParent<Recoil3DSpriteVisual>();return !v||(v.source&&v.source.enabled&&!v.suppressed);}
    static bool Forbidden(MeshRenderer r,string n)
    {return r.GetComponentInParent<Recoil3DActorVisual>()||r.GetComponentInParent<ReadableProp>()||r.GetComponentInParent<RecoilDoor>()||n.Contains("window")||n.Contains("glass")||n.Contains("lamp")||n.Contains("light")||n.Contains("sign")||n.Contains("poster")||n.Contains("truck")||n.Contains("canvas")||n.Contains("canopy")||n.Contains("silhouette")||n.Contains("crate")||n.Contains("cargo")||n.Contains("tread")||n.Contains("ladder")||n.Contains("handhold");}
    static void AddWallWear(MeshRenderer wall,bool domestic,bool outdoor)
    {
        var b=wall.bounds;if(domestic||b.size.x<3||b.size.y<2||b.size.z>1)return;
        var root=MetricGroup("Wear from wall base and fittings",wall.transform,Vector3.zero);
        float h=Mathf.Min(.5f,b.size.y*.12f);Stain("Moisture rising from wall base",root,new Vector3(b.center.x,b.min.y+h*.5f,b.min.z-.018f),new Vector2(b.size.x,h),damp);
        if(outdoor&&b.min.y<.3f)Stain("Moisture at wall-foot joint",root,new Vector3(b.center.x-1,.012f,b.min.z-.65f),new Vector2(Mathf.Min(3,b.size.x*.25f),1.0f),puddle,true);
    }
    static Material DoorFinish(Material original)
    {
        Material m;if(doorFinishes.TryGetValue(original,out m))return m;
        var path="Assets/Recoil3D/Materials/Door finish - "+original.name+".mat";m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(original);AssetDatabase.CreateAsset(m,path);}if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",.35f);if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.18f);m.enableInstancing=true;doorFinishes.Add(original,m);EditorUtility.SetDirty(m);return m;
    }
    static bool DressDoor(RecoilDoor d,bool domestic)
    {
        var layers=d.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source&&v.source.enabled&&!v.suppressed&&v.meshRenderer&&v.meshRenderer.bounds.size.x>.6f&&v.meshRenderer.bounds.size.x<2.2f&&v.meshRenderer.bounds.size.y>1.6f&&v.meshRenderer.bounds.size.y<3.5f).ToArray();
        var leaf=layers.Where(v=>!Context(v.transform).Contains("shadow")&&!v.name.ToLower().Contains("frame")).OrderBy(v=>v.name.ToLower().Contains("leaf")?0:1).ThenBy(v=>v.meshRenderer.bounds.size.x).FirstOrDefault();if(!leaf)return false;
        var b=leaf.meshRenderer.bounds;float width=b.size.x,height=b.size.y,z=b.center.z;
        foreach(var v in layers)if(v!=leaf&&(v.name.ToLower().Contains("frame")||v.name.ToLower().Contains("shadow"))){v.suppressed=true;v.Sync();}
        leaf.depth=.055f;leaf.Sync();leaf.meshRenderer.sharedMaterial=DoorFinish(leaf.meshRenderer.sharedMaterial);
        var g=MetricGroup("Door surround - recessed steel and anchors",d.transform,new Vector3(b.center.x,b.center.y,z));
        var trim=domestic?wood:steel;float t=.085f;
        Part("Dark inner reveal",g,new Vector3(0,0,.035f),new Vector3(width+.05f,height+.05f,.018f),shadow);
        foreach(float side in new[]{-1f,1f})
        {
            Part("Folded jamb with thickness",g,new Vector3(side*(width*.5f+t*.5f),0,-.085f),new Vector3(t,height+t*2,.28f),trim);
            if(!domestic)foreach(float y in new[]{-height*.33f,height*.33f})Part("Door anchor bolt - 35mm",g,new Vector3(side*(width*.5f+t*.5f),y,-.24f),new Vector3(.035f,.035f,.018f),steel);
        }
        Part("Deep door head",g,new Vector3(0,height*.5f+t*.5f,-.085f),new Vector3(width+t*2,t,.28f),trim);
        Part("Worn threshold",g,new Vector3(0,-height*.5f+.015f,-.06f),new Vector3(width,.03f,.30f),trim);
        foreach(var v in d.GetComponentsInChildren<Recoil3DSpriteVisual>(true))if(v.name.ToLower().Contains("inset")){v.depth=.02f;v.Sync();var p=v.meshRenderer.transform.position;p.z=z-.045f;v.meshRenderer.transform.position=p;}
        if(!domestic)Stain("Runoff below lower hinge",g,new Vector3(b.min.x-.025f,b.min.y+.22f,z-.245f),new Vector2(.12f,.4f),rustStain);
        return true;
    }
    public static int NormalizeShellsAndHardware(Scene scene)
    {
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();int changed=0;
        foreach(string name in new[]{"Store facade","Control hut","Factory main facade","Warehouse wall"})
        {
            var root=all.FirstOrDefault(t=>t.name==name);if(!root)continue;
            var face=root.GetComponentsInChildren<Recoil3DSpriteVisual>(true).FirstOrDefault(v=>v.source&&v.source.enabled&&!v.suppressed&&v.meshRenderer&&v.meshRenderer.bounds.size.x>1&&v.meshRenderer.bounds.size.y>1);
            if(!face)continue;float delta=.38f-face.depth;face.depth=.38f;face.depthOffset+=delta*.5f/Mathf.Max(.001f,Mathf.Abs(face.transform.lossyScale.z));face.Sync();
            var shell=root.Find(name+" - volumetric building");if(!shell)continue;
            foreach(var r in shell.GetComponentsInChildren<MeshRenderer>(true))
            {var sc=r.transform.localScale;if(r.name=="Side return wall"){sc.x*=.38f/r.bounds.size.x;r.transform.localScale=sc;changed++;}else if(r.name=="Rear masonry"){sc.z*=.38f/r.bounds.size.z;r.transform.localScale=sc;}}
            var bounds=face.meshRenderer.bounds;
            foreach(var window in shell.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Isolated lit window").ToArray())
            {
                bool left=window.position.x<bounds.center.x;var pos=window.position;pos.x=left?bounds.min.x-.22f:bounds.max.x+.22f;window.position=pos;window.rotation=Quaternion.Euler(0,left?90:-90,0);
                foreach(Transform t in window)
                {var sc=t.localScale;if(t.name=="Dirty illuminated glass"||t.name=="Window crossbar"){sc.x=1.25f;t.localScale=sc;}else if(t.name=="Window vertical glazing bar"){var p=t.localPosition;p.x=p.x<0?-.625f:.625f;t.localPosition=p;}}
            }
        }
        foreach(var r in all.Select(t=>t.GetComponent<MeshRenderer>()).Where(r=>r&&Visible(r)&&!r.GetComponentInParent<Recoil3DActorVisual>()))
        {string n=Context(r.transform);var b=r.bounds;if(!(n.Contains("rivet")||n.Contains("fastener"))||Mathf.Max(b.size.x,b.size.y)<=.06f||Mathf.Max(b.size.x,b.size.y)>=.4f)continue;var sc=r.transform.localScale;float f=.055f/Mathf.Max(b.size.x,b.size.y);sc.x*=f;sc.y*=f;r.transform.localScale=sc;var v=r.GetComponentInParent<Recoil3DSpriteVisual>();if(v){v.depth=Mathf.Min(v.depth,.02f);v.Sync();}changed++;}
        return changed;
    }
    [MenuItem("Recoil/3D/Polish Architectural Scale And Wear")]
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play first.");for(int i=0;i<SceneManager.sceneCount;i++)if(SceneManager.GetSceneAt(i).isDirty)throw new Exception("Save current scene edits first.");
        Materials();doorFinishes.Clear();var previous=SceneManager.GetActiveScene();int surfaces=0,doors=0,bolts=0,scenes=0;
        foreach(var e in EditorBuildSettings.scenes.Where(e=>e.enabled&&e.path.StartsWith(Recoil3DScene.Folder)))
        {
            var s=SceneManager.GetSceneByPath(e.path);bool added=!s.isLoaded;if(added)s=EditorSceneManager.OpenScene(e.path,OpenSceneMode.Additive);SceneManager.SetActiveScene(s);
            try
            {
                var w=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();if(w.transform.Find(Marker))continue;
                bool outdoor=e.path.EndsWith("/RecoilPrototype.unity"),domestic=e.path.EndsWith("/RecoilKitchenMemory.unity"),sewer=e.path.EndsWith("/RecoilFactorySewer.unity");
                var renderers=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(Visible).ToArray();
                foreach(var r in renderers)
                {
                    string n=Context(r.transform);if(Forbidden(r,n))continue;var b=r.bounds;Material selected=null;
                    bool wall=(n.Contains("wall")||n.Contains("facade")||n.Contains("masonry")||n.Contains("elevation"))&&b.size.y>.5f&&Mathf.Max(b.size.x,b.size.z)>1;
                    if(wall){selected=outdoor&&!n.Contains("perimeter")&&!n.Contains("hard wall")?brick:domestic?plaster:sewer?concrete:n.Contains("green painted")?greenPaint:n.Contains("corridor")||n.Contains("alcove")?plaster:concrete;}
                    else if((n.Contains("floor")||n.Contains("ceiling")||n.Contains("roof")||n.Contains("foundation"))&&Mathf.Max(b.size.x,b.size.z)>2)selected=concrete;
                    else if(n.Contains("weathered timber board"))selected=wood;
                    if(selected){r.sharedMaterial=selected;surfaces++;if(wall)AddWallWear(r,domestic,outdoor);}
                    if((n.Contains("rivet")||n.Contains("fastener")||n.Contains("rusty nail"))&&Mathf.Max(b.size.x,b.size.y)>.06f&&Mathf.Max(b.size.x,b.size.y)<.4f)
                    {var sc=r.transform.localScale;float f=.055f/Mathf.Max(b.size.x,b.size.y);sc.x*=f;sc.y*=f;r.transform.localScale=sc;var v=r.GetComponentInParent<Recoil3DSpriteVisual>();if(v){v.depth=Mathf.Min(v.depth,.02f);v.Sync();}bolts++;}
                }
                // Localised runoff belongs to metal sills and pipe joints, never an arbitrary screen-wide dirt layer.
                if(!domestic)
                {
                    int n=0;foreach(var r in renderers)
                    {
                        string c=Context(r.transform);var b=r.bounds;
                        bool window=(r.name=="Dirty illuminated glass"||r.name=="High wire glass window"||c.Contains("shop window"))&&b.size.x>.5f&&b.size.y>.5f;
                        bool coupling=c.Contains("coupling")&&b.size.y<1;
                        if(!(window||coupling)||n++%2!=0)continue;
                        var parent=MetricGroup("Wear beneath sill or pipe joint",r.transform,Vector3.zero);
                        Stain(window?"Rust runoff beneath metal sill":"Seepage below pipe coupling",parent,new Vector3(b.center.x,b.min.y-.22f,b.min.z-.025f),new Vector2(window?Mathf.Min(.7f,b.size.x*.35f):.16f,.45f),rustStain);
                    }
                }
                foreach(var d in w.GetComponentsInChildren<RecoilDoor>(true))if(DressDoor(d,domestic))doors++;
                bolts+=NormalizeShellsAndHardware(s);
                new GameObject(Marker).transform.SetParent(w.transform,false);EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);scenes++;
            }
            finally{if(added)EditorSceneManager.CloseScene(s,true);}
        }
        AssetDatabase.SaveAssets();SceneManager.SetActiveScene(previous);return scenes+" scenes; "+surfaces+" architectural surfaces, "+doors+" door surrounds, "+bolts+" hardware sizes.";
    }
}
