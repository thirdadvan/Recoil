using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Explicit, narrowly scoped authoring repair; never rebuilds the factory or gameplay.
public static class FactoryUpperWallRestoration
{
    const string ScenePath = "Assets/Scenes/3D/RecoilFactoryInterior.unity";
    const string Output = "output/upper-wall-restoration";
    [InitializeOnLoadMethod]
    static void RequestedInspection()
    {
        EditorApplication.delayCall += delegate {
            string fittings=Output+"/fittings.request";
            if(File.Exists(fittings)&&!EditorApplication.isPlayingOrWillChangePlaymode) {
                File.Move(fittings,Output+"/fittings.running");
                try { FinishFittings(); }
                catch(Exception e){File.WriteAllText(Output+"/error.txt",e.ToString());Debug.LogException(e);}
                return;
            }
            string repair = Output + "/restore.request";
            if (File.Exists(repair) && !EditorApplication.isPlayingOrWillChangePlaymode) {
                File.Move(repair, Output + "/restore.running");
                try { Restore(); }
                catch (Exception e) { File.WriteAllText(Output + "/error.txt", e.ToString()); Debug.LogException(e); }
                return;
            }
            string request = Output + "/inspect.request";
            if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Move(request, Output + "/inspect.running");
            try { Inspect(); }
            catch (Exception e) { File.WriteAllText(Output + "/error.txt", e.ToString()); Debug.LogException(e); }
        };
    }
    [MenuItem("Recoil/Repair/Attach upper corridor posters to restored walls")]
    public static void FinishFittings()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first.");
        var s=SceneManager.GetSceneByPath(ScenePath);
        if(!s.isLoaded)throw new Exception("Load factory interior first.");
        int count=0;
        foreach(var t in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="Official poster - defaced by workers"&&t.position.y>7.6f&&t.position.x<18.25f)) {
            var p=t.position;p.z=2.213f;t.position=p;count++;
        }
        var all=s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach(var floor in all.Where(t=>t.name.Contains("public corridor"))) {
            var restored=floor.Find("Restored corridor masonry - doors and recessed alcove");
            if(!restored||restored.childCount!=9)throw new Exception("Incomplete wall assembly: "+floor.name);
            foreach(var r in restored.GetComponentsInChildren<MeshRenderer>())if(!r.enabled||!r.sharedMaterial||r.sharedMaterial.shader.name=="Hidden/InternalErrorShader")throw new Exception("Invalid restored renderer");
        }
        EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
        Capture(s,"Upper-walls-restored");
        File.AppendAllText(Output+"/restoration.txt","\nReattached "+count+" defaced propaganda posters to the new corridor plaster. All 27 new wall/trim/return pieces have valid enabled materials.\n");
        Debug.Log("FACTORY_UPPER_WALL_FITTINGS_VERIFIED");
    }
    static Transform Block(Transform parent,string name,Vector3 centre,Vector3 size,MeshRenderer reference)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name=name;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(parent,false);
        g.transform.position=centre;
        g.transform.localScale=size;
        var r=g.GetComponent<MeshRenderer>();
        var block=new MaterialPropertyBlock(); reference.GetPropertyBlock(block);
        var colour=block.GetColor("_Color");
        string folder="Assets/Recoil3D/UpperCorridorRestoration";
        if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Recoil3D","UpperCorridorRestoration");
        string asset=folder+"/"+reference.sharedMaterial.name+" "+ColorUtility.ToHtmlStringRGBA(colour)+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(asset);
        if(!material){material=new Material(reference.sharedMaterial);if(colour.a>0)material.SetColor("_Color",colour);AssetDatabase.CreateAsset(material,asset);}
        r.sharedMaterial=material;
        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
        r.receiveShadows=true;
        return g.transform;
    }
    static void HideLegacy(Recoil3DSpriteVisual v)
    {
        v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);
    }
    [MenuItem("Recoil/Repair/Restore factory upper corridor walls")]
    public static void Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first.");
        Directory.CreateDirectory(Output);
        var scene=SceneManager.GetSceneByPath(ScenePath);
        if(!scene.isLoaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var floors=all.Where(t=>t.name=="Floor 2 public corridor"||t.name=="Floor 3 public corridor"||t.name=="Floor 4 public corridor").ToArray();
        if(floors.Length!=3)throw new Exception("Expected three original upper corridors.");
        if(floors.Any(t=>t.Find("Restored corridor masonry - doors and recessed alcove")))throw new Exception("Already restored; edit existing wall pieces.");
        string backup="Backups/FactoryUpperWallRestoration/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
        Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/Interior.unity",true);
        var doors=all.Select(t=>t.GetComponent<RecoilDoor>()).Where(d=>d).ToArray();
        var routes=doors.Select(d=>d.destinationScene+"|"+d.destinationSpawn+"|"+d.transform.position).ToArray();
        var collisions=all.SelectMany(t=>t.GetComponents<Collider2D>()).ToDictionary(c=>c.GetInstanceID(),c=>c.bounds);
        var report=new StringBuilder();report.AppendLine("Backup: "+backup);
        foreach(var floor in floors)
        {
            float y=floor.position.y;
            var wall=floor.Find("Corridor wall").GetComponent<Recoil3DSpriteVisual>();
            var dado=floor.Find("Corridor dado").GetComponent<Recoil3DSpriteVisual>();
            var skirting=floor.Find("Corridor skirting").GetComponent<Recoil3DSpriteVisual>();
            var alcove=floor.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Single(v=>v.name=="Open alcove wall");
            var alcoveBounds=alcove.meshRenderer.bounds;
            float left=wall.meshRenderer.bounds.min.x,right=wall.meshRenderer.bounds.max.x;
            float openingLeft=alcoveBounds.min.x,openingRight=alcoveBounds.max.x;
            var root=new GameObject("Restored corridor masonry - doors and recessed alcove").transform;
            root.SetParent(floor,false);
            // Doors face the gameplay plane at Z~2. Main masonry now sits immediately
            // behind their frames. Leave the original open room recessed at Z~7.
            foreach(var interval in new[]{new Vector2(left,openingLeft),new Vector2(openingRight,right)}) {
                float width=interval.y-interval.x,x=(interval.x+interval.y)*.5f;
                Block(root,"Aged plaster corridor wall",new Vector3(x,y+1.85f,2.39f),new Vector3(width,3.74f,.30f),wall.meshRenderer);
                Block(root,"Green lower wall - attached to plaster",new Vector3(x,y+.65f,2.219f),new Vector3(width,1.30f,.025f),dado.meshRenderer);
                Block(root,"Corridor skirting - attached to lower wall",new Vector3(x,y+.08f,2.19f),new Vector3(width,.12f,.025f),skirting.meshRenderer);
            }
            float rear=alcoveBounds.min.z;
            foreach(float x in new[]{openingLeft-.075f,openingRight+.075f}) {
                Block(root,"Alcove masonry return",new Vector3(x,y+1.85f,(2.39f+rear)*.5f),new Vector3(.15f,3.74f,rear-2.39f),wall.meshRenderer);
            }
            Block(root,"Alcove opening lintel",new Vector3((openingLeft+openingRight)*.5f,y+3.49f,2.39f),new Vector3(openingRight-openingLeft,.46f,.30f),wall.meshRenderer);
            // End the green paint at the opening; do not stretch a solid strip across
            // the room opening or cover the existing wash bay/furniture.
            HideLegacy(wall);HideLegacy(dado);HideLegacy(skirting);
            report.AppendLine(floor.name+": restored plaster + lower paint + skirting; main front Z=2.24; recessed opening X="+openingLeft+".."+openingRight+", rear Z="+rear);
        }
        for(int i=0;i<doors.Length;i++)if(routes[i]!=doors[i].destinationScene+"|"+doors[i].destinationSpawn+"|"+doors[i].transform.position)throw new Exception("Door changed.");
        var after=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Collider2D>(true)).ToArray();
        if(after.Length!=collisions.Count||after.Any(c=>!collisions.ContainsKey(c.GetInstanceID())||c.bounds!=collisions[c.GetInstanceID()]))throw new Exception("Gameplay collision changed.");
        report.AppendLine("All door positions/routes and all 2D collision bounds unchanged. Original 2D scene untouched. No new lights.");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Capture(scene,"Upper-walls-restored");
        AssetDatabase.SaveAssets();File.WriteAllText(Output+"/restoration.txt",report.ToString());
        Debug.Log("FACTORY_UPPER_WALL_RESTORATION_DONE");
    }
    static string PathOf(Transform t) { return t.parent ? PathOf(t.parent)+"/"+t.name : t.name; }
    [MenuItem("Recoil/Repair/Inspect factory upper walls")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Output);
        Scene s = SceneManager.GetSceneByPath(ScenePath);
        if (!s.isLoaded) s = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        var sb = new StringBuilder();
        sb.AppendLine("Scene="+s.path+" dirty="+s.isDirty+" Play="+EditorApplication.isPlaying);
        foreach (var t in s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)))
        {
            if (t.name=="Corridor wall" || t.name=="Factory interior" || t.name=="Corridor dado" || t.name=="Open alcove wall" || t.name=="Far wall" || t.name.Contains("public corridor"))
            {
                sb.AppendLine(PathOf(t)+" active="+t.gameObject.activeInHierarchy+" position="+t.position+" scale="+t.lossyScale);
                foreach(var r in t.GetComponentsInChildren<MeshRenderer>(true)) sb.AppendLine(" renderer="+r.name+" enabled="+r.enabled+" bounds="+r.bounds+" layer="+r.gameObject.layer+" mesh="+(r.GetComponent<MeshFilter>()?r.GetComponent<MeshFilter>().sharedMesh:null));
            }
        }
        File.WriteAllText(Output+"/inspection.txt",sb.ToString());
        Capture(s,"Upper-walls-before");
        Debug.Log("FACTORY_UPPER_WALL_INSPECTION_DONE");
    }
    static void Capture(Scene scene, string prefix)
    {
        var method=typeof(FactoryConflictCapture).GetMethod("Shot",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
        foreach(int floor in new[]{2,3,4}) {
            float y=floor==2?7.6f:floor==3?11.6f:15.6f;
            method.Invoke(null,new object[]{scene,prefix+"-F"+floor,new Vector3(14,y+3.1f,-15),new Vector3(14,y+2,6.8f),52f});
        }
    }
}
