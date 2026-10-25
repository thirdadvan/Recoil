using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// Replaces legacy art in the 3D scene; gameplay sources/colliders and the separate 2D scenes remain intact.
public static class Recoil3DNativeGeometryCleanup
{
    const string Folder="Assets/Recoil3D/NativeFixtures";
    static Material steel,dark,inside,lens,concrete;
    static Transform Group(string name,Transform parent,Vector3 world)
    {var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=world;t.rotation=Quaternion.identity;var scale=parent.lossyScale;t.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);return t;}
    static GameObject Box(string name,Transform parent,Vector3 pos,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=material;return g;}
    static void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material material)
    {var g=Box(name,parent,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),material,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static Material Mat(string name,Color colour)
    {string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(mat,path);}mat.color=colour;mat.SetFloat("_Metallic",.25f);mat.SetFloat("_Glossiness",.2f);mat.SetFloat("_Weathering",.6f);mat.SetFloat("_HazeDensity",0);EditorUtility.SetDirty(mat);return mat;}
    static void Retire(Recoil3DSpriteVisual visual)
    {
        visual.suppressed=true;if(visual.source)visual.source.forceRenderingOff=true;
        if(visual.meshRenderer)
        {
            var mesh=visual.meshRenderer.gameObject;
            if(mesh.transform.childCount==0&&mesh!=visual.gameObject)UnityEngine.Object.DestroyImmediate(mesh);
            else visual.meshRenderer.enabled=false;
        }
        EditorUtility.SetDirty(visual);
    }
    static Mesh HornMesh(string name,int side)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();
        Action<Vector3,Vector3,Vector3,Vector3> quad=(a,b,c,d)=>{int n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});if(side<0)triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});else triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});};
        int segments=20,rings=6;
        Func<int,int,float,Vector3> point=(r,n,shrink)=>{float t=r/(float)rings;float radius=Mathf.Lerp(.105f,.44f,t*t*.55f+t*.45f)-shrink;float angle=n*Mathf.PI*2/segments;return new Vector3(side*(.10f+t*1.12f),Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius);};
        for(int r=0;r<rings;r++)for(int n=0;n<segments;n++)
        {
            quad(point(r,n,0),point(r+1,n,0),point(r+1,n+1,0),point(r,n+1,0));
            quad(point(r,n,.025f),point(r,n+1,.025f),point(r+1,n+1,.025f),point(r+1,n,.025f));
        }
        for(int n=0;n<segments;n++)quad(point(rings,n,0),point(rings,n,.025f),point(rings,n+1,.025f),point(rings,n+1,0));
        var mesh=new Mesh();mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,Folder+"/"+name+".asset");return mesh;
    }
    static int Horns(Transform[] all)
    {
        int count=0;Mesh left=null,right=null;
        foreach(var horn in all.Where(t=>t&&t.name.StartsWith("Siren horn ")))
        {
            if(horn.Find("Native hollow horn"))continue;
            int side=horn.name.Contains("left")?-1:1;
            var obsolete=horn.Cast<Transform>().ToArray();foreach(var child in obsolete)UnityEngine.Object.DestroyImmediate(child.gameObject);
            var native=Group("Native hollow horn",horn,new Vector3(horn.position.x,horn.position.y,3));
            var shell=new GameObject("Continuous flared metal shell - open mouth");shell.transform.SetParent(native,false);
            if(side<0&&left==null)left=HornMesh("Siren horn left",-1);if(side>0&&right==null)right=HornMesh("Siren horn right",1);
            shell.AddComponent<MeshFilter>().sharedMesh=side<0?left:right;shell.AddComponent<MeshRenderer>().sharedMaterial=steel;
            Rod("Rear compression driver",native,new Vector3(-side*.22f,0,0),new Vector3(side*.12f,0,0),.15f,dark);
            // Recessed termination sits deep in the throat, never as an overlapping mouth card.
            Rod("Dark recessed throat",native,new Vector3(side*.11f,0,0),new Vector3(side*.14f,0,0),.078f,inside);
            Rod("Mounting saddle",native,new Vector3(0,-.17f,0),new Vector3(0,-.34f,0),.045f,steel);
            count++;
        }
        return count;
    }
    static int Lamps(Transform[] all)
    {
        int count=0;
        foreach(var lamp in all.Select(t=>t?t.GetComponent<RecoilStreetLamp>():null).Where(l=>l).ToArray())
        {
            var oldHead=lamp.headRenderer?lamp.headRenderer.transform:lamp.transform.parent;
            var root=oldHead.parent;if(root.Find("Native 6m street lamp - foreground"))continue;
            var pole=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Lamp Pole");
            float x=pole?pole.position.x:lamp.Origin.x+.5f;float ground=pole?pole.position.y-Mathf.Abs(pole.lossyScale.y)*.5f:0;
            // One metre is one Unity unit. 6m column: 140mm lower shaft, 76mm upper shaft.
            foreach(var legacy in root.GetComponentsInChildren<Recoil3DSpriteVisual>(true))Retire(legacy);
            root.position=new Vector3(x,ground,-1.6f);root.localScale=Vector3.one;
            var model=Group("Native 6m street lamp - foreground",root,root.position);
            Box("Small concrete footing",model,new Vector3(0,.045f,0),new Vector3(.29f,.09f,.29f),concrete);
            Box("Galvanised mounting plate",model,new Vector3(0,.105f,0),new Vector3(.24f,.025f,.24f),steel);
            Rod("Lower 140mm shaft",model,new Vector3(0,.12f,0),new Vector3(0,2.25f,0),.07f,steel);
            Rod("Upper 76mm shaft",model,new Vector3(0,2.25f,0),new Vector3(0,5.65f,0),.038f,steel);
            Rod("Stepped collar",model,new Vector3(0,2.20f,0),new Vector3(0,2.30f,0),.078f,steel);
            Vector3[] curve={new Vector3(0,5.55f,0),new Vector3(0,5.78f,.08f),new Vector3(0,5.93f,.25f),new Vector3(0,5.99f,.49f),new Vector3(0,5.99f,.75f)};
            for(int i=1;i<curve.Length;i++)Rod("Curved outreach tube "+i,model,curve[i-1],curve[i],.038f,steel);
            Box("Electrical service hatch",model,new Vector3(0,.66f,-.072f),new Vector3(.075f,.38f,.012f),dark);
            Box("Hatch latch",model,new Vector3(0,.77f,-.082f),new Vector3(.015f,.025f,.012f),steel);
            foreach(float dx in new[]{-.082f,.082f})foreach(float dz in new[]{-.082f,.082f})Rod("Anchor stud",model,new Vector3(dx,.11f,dz),new Vector3(dx,.16f,dz),.009f,dark);
            var head=Group("Lamp head - model light and interaction",model,root.position+new Vector3(0,5.99f,.75f));
            var housing=Box("Cast metal lantern housing",head,Vector3.zero,new Vector3(.24f,.15f,.65f),steel,PrimitiveType.Sphere);
            var glass=Box("Recessed glass diffuser",head,new Vector3(0,-.058f,0),new Vector3(.19f,.025f,.51f),lens);
            Box("Lantern rear cooling spine",head,new Vector3(0,.06f,-.15f),new Vector3(.075f,.045f,.25f),dark);
            var sync=lamp.GetComponent<Recoil3DLight>();var real=sync?sync.lightSource:null;
            oldHead.SetParent(head,true);oldHead.localPosition=Vector3.zero;oldHead.localRotation=Quaternion.identity;oldHead.localScale=Vector3.one;
            lamp.transform.SetParent(head,true);lamp.transform.localPosition=Vector3.zero;lamp.transform.localRotation=Quaternion.identity;lamp.transform.localScale=Vector3.one;
            lamp.lampHead=head;lamp.coneLength=6.5f;lamp.coneHalfWidth=3.5f;
            if(real)
            {
                real.transform.SetParent(head,true);real.transform.localPosition=new Vector3(0,-.09f,0);real.transform.localRotation=Quaternion.LookRotation(new Vector3(0,-1,.14f));
                real.range=9.3f;real.spotAngle=2*Mathf.Atan2(lamp.coneHalfWidth,lamp.coneLength)*Mathf.Rad2Deg;
                sync.streetLamp=lamp;sync.intensityMultiplier=5.2f;sync.Sync();EditorUtility.SetDirty(sync);
            }
            var visual=head.gameObject.AddComponent<Recoil3DStreetLampVisual>();visual.lamp=lamp;visual.lens=glass.GetComponent<Renderer>();
            var scatter=Group("Soft scattered light - source and point",head,head.position+new Vector3(0,-3,.4f));
            var source=scatter.gameObject.AddComponent<RecoilLight2D>();source.radius=5.5f;source.intensity=.2f;
            var fill=scatter.gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.range=5.5f;fill.shadows=LightShadows.None;
            var fillSync=scatter.gameObject.AddComponent<Recoil3DLight>();fillSync.source=source;fillSync.streetLamp=lamp;fillSync.lightSource=fill;fillSync.intensityMultiplier=3;fillSync.workingColour=sync?sync.workingColour:new Color(.69f,.78f,.73f);
            visual.scatterSource=source;visual.Sync();fillSync.Sync();
            // The source is retained for the switch/flicker colour state, but its old mesh is gone.
            if(pole)pole.gameObject.SetActive(false);EditorUtility.SetDirty(lamp);count++;
        }
        return count;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Open 3D outdoor scene.");
        var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();if(world.transform.Find("Native geometry cleanup - applied"))return "Already applied.";
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/Recoil3D","NativeFixtures");
        steel=Mat("Weathered galvanised fixture steel",new Color(.20f,.24f,.235f));dark=Mat("Fixture dark housings",new Color(.075f,.09f,.085f));inside=Mat("Horn interior darkness",new Color(.03f,.04f,.035f));lens=Mat("Streetlamp diffuser",new Color(.65f,.58f,.40f));concrete=Mat("Streetlamp footing concrete",new Color(.22f,.235f,.22f));
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();int layers=0,city=0,braces=0;
        foreach(var v in all.Select(t=>t.GetComponent<Recoil3DSpriteVisual>()).Where(v=>v&&!v.suppressed&&v.source).ToArray())
        {
            var replacement=v.transform.Find("Surface - unified industrial material");var surface=replacement?replacement.GetComponent<Recoil3DSpriteVisual>():null;
            if(surface&&!surface.suppressed&&surface.meshRenderer){Retire(v);layers++;}
        }
        foreach(var t in all.Where(t=>t&&t.name=="Distant city").ToArray()){UnityEngine.Object.DestroyImmediate(t.gameObject);city++;}
        all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        foreach(var visual in all.Select(t=>t.GetComponent<Recoil3DSpriteVisual>()).Where(v=>v&&v.meshRenderer&&!v.suppressed&&v.GetComponentInParent<TowerSirenTrigger>()).ToArray())
        {
            float offset=visual.name.StartsWith("Cross brace")?.17f:visual.name.StartsWith("Horizontal tie")?-.10f:0;
            if(Mathf.Abs(offset)>.001f){visual.depthOffset+=offset;visual.Sync();EditorUtility.SetDirty(visual);braces++;}
        }
        int horns=Horns(all),lamps=Lamps(all);
        Group("Native geometry cleanup - applied",world.transform,world.transform.position);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        return "Retired base meshes="+layers+", old skyline cards="+city+", rebuilt hollow horns="+horns+", separated tower braces="+braces+", rebuilt 6m foreground streetlamps="+lamps;
    }
}
