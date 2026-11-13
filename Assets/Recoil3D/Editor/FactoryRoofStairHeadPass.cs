using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Adds an editable architectural stair head over the existing right stairwell.
// The closed roof door is scenery; existing level transitions and collision stay intact.
public static class FactoryRoofStairHeadPass
{
    const string Folder="Assets/Recoil3D/FactoryRoofStairHead";
    const string ScenePath="Assets/Scenes/3D/RecoilPrototype.unity";
    const string Marker="Factory rooftop brick stair head - aged and editable";
    static Material brick,plaster,concrete,steel,rust,tar,glass,damp,runoff;
    static T[] All<T>(Scene scene) where T:Component {return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Transform Group(Transform p,string n,Vector3 at){var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=at;return t;}
    static Transform Box(Transform p,string n,Vector3 at,Vector3 size,Material m)
    {var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;Object.DestroyImmediate(t.GetComponent<Collider>());t.name=n;t.SetParent(p,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
    static void Beam(Transform p,string n,Vector3 a,Vector3 b,float width,Material m)
    {var t=Box(p,n,(a+b)/2,new Vector3(width,width,Vector3.Distance(a,b)),m);t.localRotation=Quaternion.LookRotation(b-a);}
    static Material Surface(string name,int kind,Color tint,float metal=0)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Aged Stair Head"));AssetDatabase.CreateAsset(m,path);}m.shader=Shader.Find("Recoil/3D/Aged Stair Head");
        m.SetColor("_Color",Color.white);m.SetColor("_BaseTint",tint);m.SetColor("_MortarTint",new Color(.185f,.19f,.16f));m.SetFloat("_Kind",kind);m.SetVector("_BrickSize",new Vector4(.26f,.085f,.007f,0));m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.1f);EditorUtility.SetDirty(m);return m;
    }
    static Material Stain(string name,int shape,Color tint)
    {string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Architectural Runoff"));AssetDatabase.CreateAsset(m,path);}m.SetFloat("_Shape",shape);m.SetColor("_Tint",tint);return m;}
    static void Decal(Transform p,string n,Vector3 at,Vector2 size,Material mat,bool side=false)
    {var t=GameObject.CreatePrimitive(PrimitiveType.Quad).transform;Object.DestroyImmediate(t.GetComponent<Collider>());t.name=n;t.SetParent(p,false);t.localPosition=at;t.localScale=new Vector3(size.x,size.y,1);if(side)t.localRotation=Quaternion.Euler(0,-90,0);t.GetComponent<Renderer>().sharedMaterial=mat;}
    static void PlasterPatch(Transform p,string n,Vector3 at,Vector2 size,int seed,bool side=false)
    {
        var random=new System.Random(seed);const int count=30;var verts=new Vector3[count+1];var uv=new Vector2[count+1];var triangles=new int[count*3];uv[0]=new Vector2(.5f,.5f);
        for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;float r=.77f+(float)random.NextDouble()*.23f;verts[i+1]=new Vector3(Mathf.Cos(angle)*size.x*.5f*r,Mathf.Sin(angle)*size.y*.5f*r,0);uv[i+1]=new Vector2(verts[i+1].x/size.x+.5f,verts[i+1].y/size.y+.5f);triangles[i*3]=0;triangles[i*3+1]=(i+1)%count+1;triangles[i*3+2]=i+1;}
        var mesh=new Mesh{name=n};mesh.vertices=verts;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Chipped plaster patch.asset"));var t=Group(p,n,at);if(side)t.localRotation=Quaternion.Euler(0,-90,0);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=plaster;
    }
    static void Build(Transform p)
    {
        // 4.0 m wide, 4.0 m deep, 3.05 m high. Front lies behind the player plane.
        Box(p,"Old concrete foundation curb",new Vector3(0,.08f,2),new Vector3(4.2f,.16f,4.2f),concrete);
        Box(p,"Left brick pier beside iron door",new Vector3(-1.65f,1.55f,.17f),new Vector3(.7f,2.9f,.34f),brick);
        Box(p,"Brick front wall between door and narrow window",new Vector3(.12f,1.55f,.17f),new Vector3(.98f,2.9f,.34f),brick);
        Box(p,"Right window jamb",new Vector3(1.78f,1.55f,.17f),new Vector3(.44f,2.9f,.34f),brick);
        Box(p,"Brick lintel above closed door",new Vector3(-.85f,2.65f,.17f),new Vector3(.9f,.7f,.34f),brick);
        Box(p,"Brick under narrow window",new Vector3(1.08f,.82f,.17f),new Vector3(.94f,1.44f,.34f),brick);
        Box(p,"Brick lintel over narrow window",new Vector3(1.08f,2.71f,.17f),new Vector3(.94f,.58f,.34f),brick);
        Box(p,"Left structural brick side",new Vector3(-1.83f,1.55f,2.1f),new Vector3(.34f,2.9f,3.8f),brick);
        Box(p,"Right structural brick side",new Vector3(1.83f,1.55f,2.1f),new Vector3(.34f,2.9f,3.8f),brick);
        Box(p,"Rear structural brick wall over stairwell",new Vector3(0,1.55f,3.83f),new Vector3(3.6f,2.9f,.34f),brick);
        var door=Group(p,"Closed old iron rooftop door",new Vector3(-.85f,.17f,-.015f));
        Box(door,"Dark recessed door reveal",new Vector3(0,1.05f,.065f),new Vector3(1.02f,2.17f,.13f),tar);
        Box(door,"Peeling grey green iron leaf",new Vector3(0,1.04f,-.012f),new Vector3(.86f,2.08f,.07f),steel);
        foreach(float x in new[]{-.49f,.49f})Box(door,"Rusty angle iron jamb",new Vector3(x,1.08f,-.035f),new Vector3(.065f,2.2f,.09f),rust);
        Box(door,"Corroded steel door lintel",new Vector3(0,2.17f,-.035f),new Vector3(1.04f,.075f,.09f),rust);
        foreach(float y in new[]{.24f,1.02f,1.84f}){Box(door,"Exposed hinge",new Vector3(-.405f,y,-.073f),new Vector3(.11f,.16f,.07f),rust);Decal(door,"Rust weep below hinge",new Vector3(-.39f,y-.18f,-.09f),new Vector2(.18f,.45f),runoff);}
        Box(door,"Old iron lock plate",new Vector3(.29f,1.02f,-.07f),new Vector3(.09f,.25f,.018f),rust);
        Beam(door,"Simple pull handle",new Vector3(.30f,.97f,-.135f),new Vector3(.30f,1.13f,-.135f),.026f,steel);
        foreach(float y in new[]{.3f,.85f,1.5f})Box(door,"Paint chipped horizontal reinforcement",new Vector3(0,y,-.055f),new Vector3(.75f,.035f,.018f),steel);
        Decal(door,"Damp rusty lower door sheet",new Vector3(0,.29f,-.057f),new Vector2(.84f,.53f),damp);
        Box(p,"Worn shallow doorstep",new Vector3(-.85f,.10f,-.24f),new Vector3(1.2f,.20f,.48f),concrete);
        // Small single-slope sheet-metal canopy with visible brackets and drip edge.
        var canopy=Box(p,"Small sloping iron rain canopy",new Vector3(-.85f,2.43f,-.39f),new Vector3(1.54f,.055f,.98f),steel);canopy.localRotation=Quaternion.Euler(-9,0,0);
        for(float x=-1.58f;x<-.1f;x+=.17f)Box(p,"Raised canopy sheet seam",new Vector3(x,2.44f,-.39f),new Vector3(.018f,.025f,.96f),rust).localRotation=Quaternion.Euler(-9,0,0);
        Box(p,"Rusted canopy front drip lip",new Vector3(-.85f,2.35f,-.88f),new Vector3(1.6f,.09f,.04f),rust);
        foreach(float x in new[]{-1.39f,-.31f})Beam(p,"Triangular canopy wall support",new Vector3(x,2.04f,-.015f),new Vector3(x,2.35f,-.77f),.038f,rust);
        // Fixed, narrow and dirty window. The opening is actually split out of the brick wall.
        Box(p,"Narrow dark window reveal",new Vector3(1.08f,1.98f,.20f),new Vector3(.95f,.88f,.08f),tar);
        Box(p,"Old dirty window glass",new Vector3(1.08f,1.98f,.028f),new Vector3(.77f,.70f,.025f),glass);
        foreach(float x in new[]{.64f,1.52f})Box(p,"Narrow iron window jamb",new Vector3(x,1.98f,-.004f),new Vector3(.045f,.86f,.08f),rust);
        foreach(float y in new[]{1.56f,2.40f})Box(p,"Narrow iron window horizontal frame",new Vector3(1.08f,y,-.004f),new Vector3(.92f,.045f,.08f),rust);
        Box(p,"Fixed centre window mullion",new Vector3(1.08f,1.98f,-.026f),new Vector3(.028f,.8f,.055f),steel);
        Box(p,"Chipped sloping concrete sill",new Vector3(1.08f,1.53f,-.075f),new Vector3(1.08f,.10f,.44f),concrete).localRotation=Quaternion.Euler(-6,0,0);
        // Separate patches leave clearly readable exposed brick instead of uniform clean plaster.
        PlasterPatch(p,"Broken limewash left of door",new Vector3(-1.66f,1.5f,-.006f),new Vector2(.62f,2.25f),34);
        PlasterPatch(p,"Flaking plaster above doorway",new Vector3(-.9f,2.76f,-.006f),new Vector2(1.02f,.43f),64);
        PlasterPatch(p,"Large irregular lime plaster remnant",new Vector3(.14f,1.44f,-.006f),new Vector2(.83f,2.15f),79);
        PlasterPatch(p,"Broken limewash below window",new Vector3(1.04f,.94f,-.006f),new Vector2(.88f,1.16f),20);
        PlasterPatch(p,"Side wall weathered plaster island",new Vector3(2.006f,1.69f,2.16f),new Vector2(2.8f,1.9f),51,true);
        Decal(p,"Rising damp across brick foot",new Vector3(0,.43f,-.014f),new Vector2(4,.66f),damp);
        Decal(p,"Window sill black runoff",new Vector3(1.12f,1.04f,-.019f),new Vector2(.88f,.96f),runoff);
        foreach(float x in new[]{-1.68f,.24f,1.8f})Decal(p,"Roof edge dirty rain streak",new Vector3(x,2.52f,-.018f),new Vector2(.36f,.94f),runoff);
        Decal(p,"Side wall damp base",new Vector3(2.012f,.47f,2.05f),new Vector2(3.8f,.72f),damp,true);
        Box(p,"Old flat reinforced concrete roof slab",new Vector3(0,3.05f,2),new Vector3(4.2f,.17f,4.2f),concrete);
        Box(p,"Dark tar felt roof",new Vector3(0,3.15f,2),new Vector3(4.15f,.04f,4.15f),tar);
        foreach(float z in new[]{.6f,1.65f,2.7f,3.75f})Box(p,"Overlapped bituminous roof seam",new Vector3(0,3.18f,z),new Vector3(4.1f,.014f,.05f),tar);
        Box(p,"Older roof felt patch",new Vector3(.8f,3.18f,2.2f),new Vector3(.92f,.019f,1.27f),steel).localRotation=Quaternion.Euler(0,7,0);
        Box(p,"Rusty eaves gutter",new Vector3(0,3.02f,-.16f),new Vector3(4.3f,.10f,.14f),rust);
        Beam(p,"Downpipe along right jamb",new Vector3(1.94f,2.98f,-.12f),new Vector3(1.94f,.25f,-.12f),.065f,steel);
        foreach(float y in new[]{.5f,1.7f,2.7f})Box(p,"Rusty pipe wall clip",new Vector3(1.94f,y,-.10f),new Vector3(.11f,.04f,.15f),rust);
        Beam(p,"Roof ventilation riser",new Vector3(-1.15f,3.16f,3.05f),new Vector3(-1.15f,3.69f,3.05f),.16f,steel);
        Box(p,"Low rain cap over vent",new Vector3(-1.15f,3.77f,3.05f),new Vector3(.38f,.075f,.38f),rust);
    }
    static void Batch(Transform root)
    {
        var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.sharedMaterial!=damp&&r.sharedMaterial!=runoff).ToArray();
        foreach(var mat in rs.Select(r=>r.sharedMaterial).Distinct())
        {var members=rs.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh{name="Stair head combined "+mat.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(members.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Combined "+mat.name+".asset"));var t=Group(root,"Combined geometry - "+mat.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;foreach(var r in members)r.enabled=false;}
    }
    static void Shot(Camera camera,string name)
    {
        Directory.CreateDirectory("Captures/FactoryRoofStairHead");var target=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGBHalf);var oldTarget=camera.targetTexture;var oldActive=RenderTexture.active;Texture2D raw=null,image=null;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;raw=new Texture2D(1440,900,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1440,900),0,0);raw.Apply();var colors=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<colors.Length;i++)colors[i]=colors[i].gamma;image=new Texture2D(1440,900,TextureFormat.RGB24,false,true);image.SetPixels(colors);image.Apply();File.WriteAllBytes("Captures/FactoryRoofStairHead/"+name+".png",image.EncodeToPNG());}
        finally{camera.targetTexture=oldTarget;RenderTexture.active=oldActive;RenderTexture.ReleaseTemporary(target);if(raw)Object.DestroyImmediate(raw);if(image)Object.DestroyImmediate(image);}
    }
    public static string Capture()
    {
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var otherRenderers=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>All<Renderer>(s)).Where(r=>r.enabled).ToArray();var otherLights=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>All<Light>(s)).Where(l=>l.enabled).ToArray();
        try
        {
            foreach(var r in otherRenderers)r.enabled=false;foreach(var l in otherLights)l.enabled=false;SceneManager.SetActiveScene(scene);All<Recoil3DWorld>(scene).Single().ApplyAtmosphere();var camera=All<Camera>(scene).First(c=>c.CompareTag("MainCamera"));var position=camera.transform.position;var rotation=camera.transform.rotation;float fov=camera.fieldOfView;var follow=camera.GetComponent<RecoilCameraController>();bool wasFollowing=follow&&follow.enabled;GameObject temp=null;
            try
            {
                if(follow)follow.enabled=false;camera.fieldOfView=48;camera.transform.position=new Vector3(110,25.4f,-9);camera.transform.LookAt(new Vector3(102.32f,21.55f,4.2f));Shot(camera,"StairHeadSceneLighting");
                temp=new GameObject("Temporary stair head inspection light");SceneManager.MoveGameObjectToScene(temp,scene);var light=temp.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.91f,.78f);light.intensity=1.45f;light.transform.rotation=Quaternion.Euler(42,-28,0);Shot(camera,"StairHeadInspection");
                camera.fieldOfView=52;camera.transform.position=new Vector3(95,29,-28);camera.transform.LookAt(new Vector3(94,17,7));Shot(camera,"FactoryRoofContext");
            }
            finally{if(temp)Object.DestroyImmediate(temp);camera.transform.SetPositionAndRotation(position,rotation);camera.fieldOfView=fov;if(follow)follow.enabled=wasFollowing;}
            return "Saved three factory roof stair head screenshots";
        }
        finally{foreach(var r in otherRenderers)if(r)r.enabled=true;foreach(var l in otherLights)if(l)l.enabled=true;if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);var world=All<Recoil3DWorld>(previous).FirstOrDefault();if(world)world.ApplyAtmosphere();var look=All<WarehouseAtmosphereTest>(previous).FirstOrDefault();if(look)look.ApplyLook();}
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit play before scene edits");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Backups/FactoryRoofStairHead");Directory.CreateDirectory("output/factory-roof-stair-head");
        var prev=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            if(All<Transform>(scene).Any(t=>t.name==Marker))throw new Exception("Stair head already exists");
            EditorSceneManager.SaveScene(scene,"Backups/FactoryRoofStairHead/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity",true);
            var collisions=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled).ToArray();var doors=All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();
            brick=Surface("Old red brown structural brick",1,new Color(.29f,.225f,.17f));plaster=Surface("Chipped dirty grey lime plaster",2,new Color(.36f,.37f,.32f));concrete=Surface("Damp worn concrete coping",0,new Color(.28f,.29f,.25f));steel=Surface("Peeling grey green iron",3,new Color(.19f,.235f,.205f),.45f);rust=Surface("Deep corroded iron edges",3,new Color(.30f,.14f,.065f),.25f);tar=Surface("Old bitumen felt and dark reveal",0,new Color(.065f,.073f,.064f));glass=Surface("Opaque soot covered window glass",4,new Color(.10f,.16f,.17f),.25f);glass.SetFloat("_Glossiness",.32f);damp=Stain("Old green black rising damp",1,new Color(.07f,.09f,.043f,.62f));runoff=Stain("Soot rust water streak",0,new Color(.105f,.067f,.032f,.58f));
            var factory=All<Transform>(scene).Single(t=>t.name=="Factory Building - four storeys");var root=Group(factory,Marker,new Vector3(28,19.8f,2.4f));Build(root);Batch(root);
            if(!collisions.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled)))throw new Exception("Existing collision changed");
            if(!doors.SequenceEqual(All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Existing level transitions changed");
            // Even the projecting step and rain canopy stay behind z=1.5, clear of the roof walking band.
            float front=root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).Min(r=>r.bounds.min.z);if(front<1.5f)throw new Exception("Rooftop walking band obstructed");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            string result="PASS: aged brick stair head at "+root.position+", aligned with interior right stairwell x=28. Dimensions 4 x 4 x 3.05 m. Foremost projection z="+front.ToString("F2")+"; original Collider2D and RecoilDoor states unchanged. Closed decorative door; no new level transition.\n";File.WriteAllText("output/factory-roof-stair-head/verification.txt",result);return result;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(prev);}
    }
}
