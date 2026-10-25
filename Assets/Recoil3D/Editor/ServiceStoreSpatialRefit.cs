using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ServiceStoreSpatialRefit
{
    const string ScenePath="Assets/Scenes/3D/RecoilServiceStore.unity",Folder="Assets/Recoil3D/StoreRefit/",Marker="Service store spatial refit - fixtures and stock";
    static Material steel,enamel,cream,wood,dark,red,ochre,green,blue,paper,glass,label;
    static System.Random random;
    static int products;
    [MenuItem("Recoil/Art Tests/Rebuild Service Store Fixtures And Coaler Shadows")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Stop Play before refitting the shop.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var otherRoots=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene&&s.isLoaded).SelectMany(s=>s.GetRootGameObjects()).Where(g=>g.activeSelf).ToArray();
        try
        {
            SceneManager.SetActiveScene(scene);string backup="Backups/ServiceStoreSpatialRefit/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/RecoilServiceStore.unity",true);
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();SetupMaterials();random=new System.Random(1623);products=0;
            var roots=scene.GetRootGameObjects();var world=roots.SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();
            var doors=world.GetComponentsInChildren<RecoilDoor>(true);var positions=doors.Select(d=>d.transform.position).ToArray();int collisions=world.GetComponentsInChildren<Collider2D>(true).Length;
            var old=world.transform.Find(Marker);if(old)UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach(var visual in world.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>new[]{"Shelf","Shelf backing","Shop stock","Rear counter","Counter top"}.Contains(v.name)))
            {visual.suppressed=true;visual.Sync();EditorUtility.SetDirty(visual);}
            foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Counter sign"))t.gameObject.SetActive(false);
            var root=Group(Marker,world.transform,Vector3.zero);root.position=Vector3.zero;root.rotation=Quaternion.identity;var scale=world.transform.lossyScale;root.localScale=new Vector3(1/scale.x,1/scale.y,1/scale.z);
            Floor(root);
            Gondola(root,new Vector3(4.5f,0,4.8f),"Double-sided snack gondola",0);
            Gondola(root,new Vector3(8.0f,0,5.2f),"Double-sided pantry gondola",1);
            WallShelf(root,new Vector3(2.7f,0,8.8f),"Back wall - pantry and travel goods",2.65f,0);
            WallShelf(root,new Vector3(6.1f,0,8.8f),"Back wall - automotive fluids",2.65f,2);
            Cooler(root,new Vector3(11.1f,0,8.35f));
            Coffee(root,new Vector3(1.05f,0,5.2f));
            Freezer(root,new Vector3(10.2f,0,2.65f));
            Checkout(root,new Vector3(13.7f,0,2.35f));
            Accessories(root);
            FitCounter(world);
            ReplaceShadows(world);
            for(int i=0;i<doors.Length;i++)if(doors[i].transform.position!=positions[i])throw new Exception("An interaction door moved during art refit");
            if(world.GetComponentsInChildren<Collider2D>(true).Length!=collisions)throw new Exception("Gameplay collision changed during art refit");
            var look=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();if(look.lamps.Length!=0)throw new Exception("Unexpected pendant-light volume sources in store");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            foreach(var r in otherRoots)r.SetActive(false);Capture(scene);
            File.WriteAllText("Captures/WarehouseLightingTest/StoreRefit-References.txt","References studied (spatial organization, not copied artwork):\nhttps://www.rnservis.cz/reference/ostatni/muso-orlova/\nhttps://www.w8sro.cz/cerpaci-stanice/hrabova\nhttps://www.ecopetrol.bg/benzinostancii/eco-petrol-breznik/\nhttps://www.osti.gov/servlets/purl/2324007\nApplied: low double-sided gondolas; rear beverage refrigeration; coffee and pastry side station; checkout impulse merchandise; automotive fluid shelf; depleted stock and weathered fixtures.\nGeometry and goods are procedural meshes. Existing natural light, dialogue, water-gun pickup, skylight exit, and 2D originals preserved.\nProducts: "+products+"\n");
            Debug.Log("SERVICE_STORE_SPATIAL_REFIT_PASS: "+products+" volumetric products; 2 stocked gondolas; 2 wall bays; double-door cooler; coffee and bakery; chest freezer; checkout; 2 current Coaler shadow models; doors/colliders unchanged; backup="+backup);
        }
        finally
        {
            foreach(var r in otherRoots)if(r)r.SetActive(true);
            if(opened)EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()&&previous.isLoaded){SceneManager.SetActiveScene(previous);var w=previous.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DWorld>(true)).FirstOrDefault();if(w)w.ApplyAtmosphere();var l=previous.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).FirstOrDefault();if(l)l.ApplyLook();}
        }
    }
    static Material Mat(string name,Color colour,float metal=0,float smooth=.15f)
    {
        var path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}m.color=colour;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",smooth);m.SetFloat("_Weathering",.42f);m.SetFloat("_HazeDensity",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void SetupMaterials()
    {
        steel=Mat("Worn rack galvanised metal",new Color(.20f,.23f,.21f),.45f);enamel=Mat("Aged olive enamel",new Color(.22f,.25f,.20f),.12f);cream=Mat("Yellowed refrigerator enamel",new Color(.49f,.48f,.37f));wood=Mat("Worn counter laminate",new Color(.27f,.18f,.105f));dark=Mat("Rubber and cabinet recess",new Color(.035f,.045f,.041f));paper=Mat("Faded paper and price tickets",new Color(.59f,.52f,.37f));
        red=Mat("Faded brick packaging",new Color(.42f,.13f,.08f));ochre=Mat("Faded mustard packaging",new Color(.49f,.34f,.12f));green=Mat("Olive bottles",new Color(.16f,.29f,.19f),0,.30f);blue=Mat("Dusty blue packaging",new Color(.14f,.26f,.31f));
        glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Old cooler glass.mat");if(!glass){glass=new Material(Shader.Find("Standard"));glass.name="Old cooler glass";glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHAPREMULTIPLY_ON");glass.renderQueue=3000;glass.color=new Color(.14f,.22f,.20f,.13f);glass.SetFloat("_Glossiness",.30f);AssetDatabase.CreateAsset(glass,Folder+"Old cooler glass.mat");}
        label=AssetDatabase.LoadAssetAtPath<Material>(Folder+"Depth-tested store labels.mat");if(!label){label=new Material(Shader.Find("Recoil/3D/World Text"));AssetDatabase.CreateAsset(label,Folder+"Depth-tested store labels.mat");}
    }
    static Transform Group(string name,Transform parent,Vector3 p){var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=p;return g;}
    static Transform Part(string name,Transform parent,Vector3 p,Vector3 size,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;r.shadowCastingMode=mat==glass?ShadowCastingMode.Off:ShadowCastingMode.On;return g.transform;
    }
    static void Cylinder(string name,Transform parent,Vector3 p,float diameter,float height,Material mat){Part(name,parent,p,new Vector3(diameter,height*.5f,diameter),mat,PrimitiveType.Cylinder);}
    static void Text(Transform parent,string value,Vector3 p,float size)
    {
        var t=Group("Painted category - "+value,parent,p);var mesh=t.gameObject.AddComponent<TextMesh>();mesh.text=value;mesh.fontSize=48;mesh.characterSize=size;mesh.anchor=TextAnchor.MiddleCenter;mesh.color=new Color(.50f,.49f,.39f);t.GetComponent<Renderer>().sharedMaterial=label;t.gameObject.AddComponent<Recoil3DDepthText>();
    }
    static void Batch(Transform root)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<Renderer>()).ToArray();
        int number=0;foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial).ToArray())
        {
            var pieces=group.ToArray();var path=Folder+root.name+" - batch "+number+++".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(!mesh){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);mesh.indexFormat=IndexFormat.UInt32;mesh.CombineMeshes(pieces.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            var t=Group("Combined - "+group.Key.name,root,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=group.Key;renderer.shadowCastingMode=group.Key==glass?ShadowCastingMode.Off:ShadowCastingMode.On;
        }
        foreach(var piece in filters)if(piece)UnityEngine.Object.DestroyImmediate(piece.gameObject);
    }
    static void Product(Transform root,Vector3 p,int kind,int colour,float yaw=0)
    {
        products++;var g=Group("Stock - "+new[]{"bottle","can","carton","snack bag","motor oil"}[kind],root,p);g.localRotation=Quaternion.Euler(0,yaw,(float)(random.NextDouble()-.5)*6);var mat=new[]{red,ochre,green,blue}[colour%4];
        if(kind==0)
        {Cylinder("Bottle body",g,new Vector3(0,.105f,0),.09f,.21f,mat);Part("Bottle shoulder",g,new Vector3(0,.225f,0),new Vector3(.09f,.07f,.09f),mat,PrimitiveType.Sphere);Cylinder("Bottle neck",g,new Vector3(0,.267f,0),.042f,.065f,mat);Cylinder("Screw cap",g,new Vector3(0,.303f,0),.048f,.028f,ochre);Cylinder("Paper band",g,new Vector3(0,.12f,0),.092f,.065f,paper);}
        else if(kind==1)
        {Cylinder("Tin can",g,new Vector3(0,.083f,0),.087f,.166f,mat);Cylinder("Can rolled rim",g,new Vector3(0,.171f,0),.091f,.009f,steel);Part("Faded front label",g,new Vector3(0,.09f,-.044f),new Vector3(.06f,.065f,.008f),paper);}
        else if(kind==2)
        {Part("Cardboard carton",g,new Vector3(0,.115f,0),new Vector3(.13f,.23f,.09f),mat);Part("Folded top",g,new Vector3(0,.237f,0),new Vector3(.13f,.018f,.09f),paper);Part("Printed product panel",g,new Vector3(0,.135f,-.047f),new Vector3(.09f,.115f,.006f),paper);}
        else if(kind==3)
        {Part("Puffed packet",g,new Vector3(0,.11f,0),new Vector3(.15f,.22f,.07f),mat,PrimitiveType.Sphere);foreach(float y in new[]{.014f,.207f})Part("Crimped bag seam",g,new Vector3(0,y,0),new Vector3(.14f,.013f,.045f),mat);Part("Packet printing",g,new Vector3(0,.12f,-.035f),new Vector3(.08f,.10f,.005f),paper);}
        else
        {Part("Rectangular oil bottle",g,new Vector3(0,.14f,0),new Vector3(.16f,.28f,.10f),mat);Part("Handle side",g,new Vector3(.062f,.325f,0),new Vector3(.025f,.10f,.06f),mat);Part("Handle top",g,new Vector3(.025f,.365f,0),new Vector3(.10f,.025f,.06f),mat);Cylinder("Sealed oil cap",g,new Vector3(-.04f,.30f,0),.047f,.035f,dark);Part("Oil label",g,new Vector3(0,.15f,-.052f),new Vector3(.115f,.15f,.006f),paper);}
    }
    static void Floor(Transform parent)
    {
        var g=Group("Worn tiled retail floor",parent,Vector3.zero);var tile=Mat("Old grey-green floor tile",new Color(.17f,.20f,.18f));var alternate=Mat("Old discoloured floor tile",new Color(.20f,.205f,.175f));
        for(int x=0;x<24;x++)for(int z=0;z<15;z++){float xx=.33f+x*.66f,zz=-.94f+z*.66f;Part("Tile with grout gap",g,new Vector3(xx,.012f,zz),new Vector3(.645f,.014f,.645f),(x+z*3)%11==0?alternate:tile);}
        Batch(g);
    }
    static void Gondola(Transform parent,Vector3 p,string name,int assortment)
    {
        var g=Group(name,parent,p);Part("Raised plinth",g,new Vector3(0,.12f,0),new Vector3(1.04f,.24f,3.1f),enamel);Part("Double-sided central spine",g,new Vector3(0,.89f,0),new Vector3(.065f,1.48f,2.98f),enamel);
        foreach(float z in new[]{-1.45f,0,1.45f})Part("Slotted upright",g,new Vector3(0,.92f,z),new Vector3(.085f,1.65f,.065f),steel);
        foreach(float y in new[]{.29f,.72f,1.16f})foreach(float side in new[]{-1f,1f})
        {
            Part("Cantilevered steel shelf",g,new Vector3(side*.27f,y,0),new Vector3(.50f,.045f,2.95f),cream);Part("Continuous price ticket rail",g,new Vector3(side*.51f,y+.025f,0),new Vector3(.035f,.07f,2.95f),enamel);
            for(int j=0;j<12;j++)if(random.NextDouble()>.28){Product(g,new Vector3(side*.30f,y+.025f,-1.3f+j*.235f),(j+assortment)%4,j+assortment,side<0?-90:90);if(j%4==0)Part("Faded price insert",g,new Vector3(side*.531f,y+.03f,-1.3f+j*.235f),new Vector3(.009f,.045f,.12f),paper);}
        }
        Part("Front endcap shelf",g,new Vector3(0,.55f,-1.61f),new Vector3(.94f,.045f,.32f),cream);for(int i=0;i<5;i++)Product(g,new Vector3(-.35f+i*.17f,.58f,-1.65f),3,i);
        Part("Bent endcap price strip",g,new Vector3(0,.55f,-1.79f),new Vector3(.95f,.07f,.025f),enamel);Batch(g);
    }
    static void WallShelf(Transform parent,Vector3 p,string name,float width,int assortment)
    {
        var g=Group(name,parent,p);Part("Solid footed cabinet base",g,new Vector3(0,.12f,0),new Vector3(width,.24f,.73f),enamel);Part("Recessed back panel",g,new Vector3(0,1.0f,.30f),new Vector3(width,1.8f,.055f),dark);
        foreach(float x in new[]{-width*.5f,width*.5f})Part("Punched vertical shelf post",g,new Vector3(x,1,0),new Vector3(.055f,1.9f,.62f),steel);
        for(int level=0;level<4;level++)
        {float y=.30f+level*.43f;Part("Wall shelf",g,new Vector3(0,y,0),new Vector3(width,.045f,.67f),cream);Part("Front ticket edge",g,new Vector3(0,y+.025f,-.35f),new Vector3(width,.075f,.025f),enamel);for(int j=0;j<12;j++)if(random.NextDouble()>.22)Product(g,new Vector3(-width*.45f+j*width*.08f,y+.025f,-.13f),assortment==2?4:(j+level)%4,j+level);}
        Batch(g);Text(g,assortment==2?"MOTOR OIL / CAR CARE":"PANTRY / TRAVEL",new Vector3(0,1.98f,-.34f),.025f);
    }
    static void Cooler(Transform parent,Vector3 p)
    {
        var g=Group("Twin-door refrigerated drinks cabinet",parent,p);Part("Insulated cabinet",g,new Vector3(0,1.21f,0),new Vector3(2.65f,2.42f,.94f),cream);Part("Dark interior cavity",g,new Vector3(0,1.30f,-.49f),new Vector3(2.45f,1.96f,.035f),dark);
        for(int level=0;level<4;level++){float y=.38f+level*.43f;Part("Wire drink shelf",g,new Vector3(0,y,-.56f),new Vector3(2.37f,.025f,.25f),steel);for(int j=0;j<17;j++)if(random.NextDouble()>.3)Product(g,new Vector3(-1.08f+j*.135f,y+.02f,-.57f),level==2?1:0,j);}
        foreach(float x in new[]{-1.25f,0,1.25f})Part("Door aluminium stile",g,new Vector3(x,1.31f,-.75f),new Vector3(.055f,2,.065f),steel);
        foreach(float x in new[]{-.625f,.625f}){Part("Smudged glazed cooler door",g,new Vector3(x,1.31f,-.72f),new Vector3(1.18f,1.9f,.018f),glass);Part("Heavy vertical door handle",g,new Vector3(x+.46f,1.33f,-.83f),new Vector3(.035f,.55f,.055f),steel);}
        foreach(float y in new[]{.31f,2.31f})Part("Glazing horizontal frame",g,new Vector3(0,y,-.75f),new Vector3(2.55f,.055f,.065f),steel);
        for(int i=0;i<16;i++)Part("Compressor ventilation slot",g,new Vector3(-1.16f+i*.155f,.15f,-.49f),new Vector3(.09f,.018f,.025f),dark);
        Batch(g);Text(g,"COLD DRINKS",new Vector3(0,2.44f,-.50f),.036f);
    }
    static void Coffee(Transform parent,Vector3 p)
    {
        var g=Group("Side coffee station and pastry cabinet",parent,p);g.localRotation=Quaternion.Euler(0,32,0);
        Part("Coffee base cabinet",g,new Vector3(0,.47f,0),new Vector3(1.55f,.94f,.82f),enamel);Part("Laminate worktop",g,new Vector3(0,.96f,0),new Vector3(1.63f,.08f,.89f),wood);
        foreach(float x in new[]{-.38f,.38f}){Part("Cupboard front",g,new Vector3(x,.52f,-.427f),new Vector3(.73f,.74f,.035f),enamel);Part("Drawer pull",g,new Vector3(x,.78f,-.46f),new Vector3(.19f,.025f,.035f),steel);}
        Part("Old espresso machine",g,new Vector3(-.32f,1.28f,.04f),new Vector3(.65f,.55f,.51f),dark);Part("Stainless espresso face",g,new Vector3(-.32f,1.31f,-.235f),new Vector3(.61f,.28f,.035f),steel);Part("Drip tray",g,new Vector3(-.32f,1.03f,-.30f),new Vector3(.62f,.055f,.28f),steel);
        foreach(float x in new[]{-.46f,-.22f}){Part("Coffee outlet",g,new Vector3(x,1.16f,-.29f),new Vector3(.035f,.12f,.055f),steel);Cylinder("Paper coffee cup",g,new Vector3(x,1.10f,-.30f),.10f,.12f,paper);}
        Cylinder("Bean hopper",g,new Vector3(-.32f,1.66f,.03f),.27f,.22f,glass);Cylinder("Hopper lid",g,new Vector3(-.32f,1.79f,.03f),.29f,.025f,dark);
        for(int i=0;i<6;i++)Cylinder("Stacked takeaway cup",g,new Vector3(.24f,1.04f+i*.035f,-.20f),.10f,.08f,paper);
        Part("Napkin dispenser",g,new Vector3(.53f,1.10f,-.17f),new Vector3(.18f,.23f,.16f),steel);Part("Protruding napkin",g,new Vector3(.53f,1.23f,-.17f),new Vector3(.13f,.10f,.012f),paper);
        Batch(g);Text(g,"COFFEE",new Vector3(0,1.96f,-.1f),.038f);
        var pastry=Group("Small bread and pastry case",parent,new Vector3(2.25f,0,6.75f));Part("Baker display pedestal",pastry,new Vector3(0,.5f,0),new Vector3(1.35f,1,.65f),wood);Part("Enclosed pastry glass",pastry,new Vector3(0,1.26f,0),new Vector3(1.3f,.5f,.60f),glass);foreach(float y in new[]{1.04f,1.30f}){Part("Baking tray",pastry,new Vector3(0,y,0),new Vector3(1.25f,.025f,.55f),steel);for(int i=0;i<5;i++)Part("Stale wrapped roll",pastry,new Vector3(-.48f+i*.23f,y+.065f,-.05f),new Vector3(.19f,.12f,.31f),ochre,PrimitiveType.Capsule);}Batch(pastry);
    }
    static void Freezer(Transform parent,Vector3 p)
    {
        var g=Group("Low chest ice-cream freezer",parent,p);Part("Insulated chest",g,new Vector3(0,.48f,0),new Vector3(1.35f,.94f,.86f),cream);Part("Top freezer recess",g,new Vector3(0,.96f,0),new Vector3(1.24f,.025f,.75f),dark);
        foreach(float x in new[]{-.32f,.32f})Part("Sliding glass lid",g,new Vector3(x,.99f,0),new Vector3(.61f,.025f,.73f),glass);foreach(float x in new[]{-.62f,0,.62f})Part("Freezer lid rail",g,new Vector3(x,1.01f,0),new Vector3(.04f,.025f,.79f),steel);
        foreach(float x in new[]{-.45f,.45f})Part("Freezer feet",g,new Vector3(x,.04f,0),new Vector3(.18f,.08f,.60f),dark);Part("Freezer faded stripe",g,new Vector3(0,.55f,-.44f),new Vector3(1.14f,.25f,.015f),blue);Batch(g);
    }
    static void Checkout(Transform parent,Vector3 p)
    {
        var g=Group("Service counter - solid shopfitting and register",parent,p);Part("Checkout base cabinet",g,new Vector3(0,.49f,0),new Vector3(3.35f,.98f,1.05f),enamel);Part("Countertop thickness",g,new Vector3(0,1.02f,-.04f),new Vector3(3.52f,.10f,1.18f),wood);Part("Counter kick plate",g,new Vector3(0,.12f,-.55f),new Vector3(3.3f,.14f,.045f),dark);
        foreach(float x in new[]{-1.08f,0,1.08f})Part("Recessed cabinet panel",g,new Vector3(x,.56f,-.54f),new Vector3(.98f,.68f,.035f),wood);
        Part("Cash drawer",g,new Vector3(-.65f,1.11f,-.03f),new Vector3(.57f,.12f,.43f),dark);var register=Part("Chunky old till",g,new Vector3(-.65f,1.29f,.03f),new Vector3(.47f,.27f,.39f),cream);register.localRotation=Quaternion.Euler(-10,0,0);Part("Till keys",g,new Vector3(-.65f,1.39f,-.13f),new Vector3(.39f,.018f,.13f),dark);Part("Customer-facing display",g,new Vector3(-.65f,1.51f,.12f),new Vector3(.33f,.17f,.065f),dark);Part("Dim register screen",g,new Vector3(-.65f,1.51f,.079f),new Vector3(.28f,.11f,.01f),green);
        Part("Counter impulse tray",g,new Vector3(.6f,1.09f,-.24f),new Vector3(.9f,.06f,.36f),steel);for(int j=0;j<6;j++)Product(g,new Vector3(.24f+j*.14f,1.13f,-.23f),j%2==0?1:2,j);
        Part("Old desk telephone",g,new Vector3(1.28f,1.15f,.07f),new Vector3(.30f,.17f,.25f),dark);Part("Telephone handset",g,new Vector3(1.28f,1.26f,.07f),new Vector3(.39f,.075f,.13f),dark);
        Batch(g);
        var behind=Group("Back counter cigarette and sundries cubbies",parent,new Vector3(14.2f,0,7.6f));Part("Back-counter storage cupboard",behind,new Vector3(0,1.20f,0),new Vector3(2.25f,2.4f,.55f),wood);
        for(int row=0;row<4;row++){float y=.5f+row*.43f;Part("Recessed sundries compartment",behind,new Vector3(0,y+.15f,-.29f),new Vector3(2.12f,.35f,.035f),dark);for(int i=0;i<10;i++)if(random.NextDouble()>.25)Part("Small boxed travel supplies",behind,new Vector3(-.93f+i*.20f,y+.12f,-.35f),new Vector3(.12f,.24f,.10f),i%3==0?red:paper);}Batch(behind);
    }
    static void Accessories(Transform parent)
    {
        var g=Group("Foreground bench baskets and depleted delivery stock",parent,Vector3.zero);
        foreach(float x in new[]{5.15f,6.55f})Part("Bench leg",g,new Vector3(x,.23f,-1.0f),new Vector3(.09f,.46f,.42f),steel);Part("Low waiting bench seat",g,new Vector3(5.85f,.47f,-1.0f),new Vector3(1.75f,.08f,.48f),wood);
        for(int i=0;i<3;i++){var t=Part("Weathered delivery carton",g,new Vector3(14.8f+i*.16f,.21f,5.1f+i*.5f),new Vector3(.55f,.42f,.43f),paper);t.localRotation=Quaternion.Euler(0,12+i*18,0);Part("Carton packing tape",t,new Vector3(0,.501f,0),new Vector3(.13f,.012f,1.01f),ochre);}
        Cylinder("Entrance litter bin",g,new Vector3(.7f,.34f,2.0f),.45f,.68f,enamel);Cylinder("Bin lid",g,new Vector3(.7f,.69f,2.0f),.48f,.05f,dark);
        for(int i=0;i<4;i++){float y=.12f+i*.075f;Part("Shopping basket base",g,new Vector3(3.25f,y,1.65f),new Vector3(.50f,.035f,.37f),green);foreach(float side in new[]{-1f,1f}){Part("Basket side",g,new Vector3(3.25f+side*.25f,y+.07f,1.65f),new Vector3(.025f,.14f,.37f),green);Part("Basket end",g,new Vector3(3.25f,y+.07f,1.65f+side*.18f),new Vector3(.50f,.14f,.025f),green);}}
        Batch(g);
    }
    static void FitCounter(RecoilPrototype world)
    {
        var clerk=world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Adult store clerk - assembled 2D art");
        if(clerk){var p=clerk.position;p.z=3.20f;clerk.position=p;EditorUtility.SetDirty(clerk);}
        var pendant=world.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Warm counter pendant");
        if(pendant){var p=pendant.position;p.z=2.10f;pendant.position=p;EditorUtility.SetDirty(pendant);}
        foreach(var v in world.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.name=="Blue plastic water gun"||v.name=="Water gun pickup"))
            if(v.meshRenderer){v.depthOffset+=(1.95f-v.meshRenderer.bounds.center.z)/Mathf.Max(.001f,v.transform.lossyScale.z);v.Sync();EditorUtility.SetDirty(v);}
    }
    [MenuItem("Recoil/Art Tests/Finish And Verify Store Refit Placement")]
    public static void FinishAndVerify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before fitting the store");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Open the service store");
        var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).Single();FitCounter(world);
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Folder.TrimEnd('/')}))
        {string path=AssetDatabase.GUIDToAssetPath(guid);var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);mesh.name=System.IO.Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(mesh);}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        var shadows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DDoorwayShadows>(true)).Single();
        if(shadows.silhouettes.Length!=2||shadows.GetComponentsInChildren<RampagerTentacleVisual>(true).Length!=2)throw new Exception("Current Coaler shadow models failed to reload");
        shadows.RebuildPoses();shadows.ResetShadows();shadows.Advance(1.0f);
        var model=shadows.GetComponentInChildren<RampagerTentacleVisual>();var meshFilter=model.VisualRoot.GetComponentsInChildren<MeshFilter>().Single(f=>f.sharedMesh.name=="Dynamic combined curved tentacles");var before=meshFilter.sharedMesh.vertices;
        shadows.Advance(.6f);var after=meshFilter.sharedMesh.vertices;if(!after.Zip(before,(a,b)=>(a-b).sqrMagnitude).Any(d=>d>.000001f))throw new Exception("Coaler tentacle pounding is not animated");
        shadows.ResetShadows();Capture(scene);
        var look=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();var camera=look.GetComponent<Camera>();var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;var ambient=RenderSettings.ambientLight;var main=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional);float oldIntensity=main?main.intensity:0;
        try{camera.transform.position=new Vector3(8,5.5f,-9);camera.transform.LookAt(new Vector3(8,1,4.8f));camera.fieldOfView=53;RenderSettings.ambientLight=new Color(.12f,.13f,.13f);if(main)main.intensity=.45f;WarehousePathLightingPass.Capture(camera,"StoreRefit_LayoutReview");}
        finally{camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;RenderSettings.ambientLight=ambient;if(main)main.intensity=oldIntensity;}
        Debug.Log("SERVICE_STORE_REFIT_RELOAD_PASS: 2 current Coaler models stay shadow-only and animate after reopening; clerk behind counter; pickup on countertop; static mesh asset names corrected; saved natural lighting retained.");
    }
    static void ReplaceShadows(RecoilPrototype world)
    {
        var shadows=world.GetComponentInChildren<Recoil3DDoorwayShadows>(true);if(!shadows||shadows.silhouettes.Length!=2)throw new Exception("Expected the two doorway silhouettes");
        for(int i=0;i<shadows.silhouettes.Length;i++)
        {
            var person=shadows.silhouettes[i];foreach(Transform child in person.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
            var model=Group("Coaler - current procedural shadow model",person,Vector3.zero);model.position=person.position+Vector3.up*.8f;model.rotation=Quaternion.Euler(0,180,0);
            var v=model.gameObject.AddComponent<RampagerTentacleVisual>();v.shadowOnly=true;v.followAI=false;v.previewForm=RampagerTentacleVisual.Form.Roaming;v.variationSeed=317+i*41;
            string folder="Assets/Recoil3D/Materials/TentacleWorker/";
            v.tentacleMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Black fibrous upper body.mat");v.trouserMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Worn safety orange.mat");v.patchMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Charcoal knee reinforcements.mat");v.bootMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Heavy work boots.mat");v.coreMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Dark red suspended core.mat");v.badgeMaterial=AssetDatabase.LoadAssetAtPath<Material>(folder+"Faded identity card.mat");v.Build();EditorUtility.SetDirty(v);
        }
        shadows.RebuildPoses();shadows.ResetShadows();EditorUtility.SetDirty(shadows);
    }
    static void Capture(Scene scene)
    {
        var look=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();look.ApplyLook();var camera=look.GetComponent<Camera>();var follow=camera.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;var p=camera.transform.position;var q=camera.transform.rotation;float fov=camera.fieldOfView;
        var shadows=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Recoil3DDoorwayShadows>(true)).Single();
        try
        {
            if(follow)follow.enabled=false;camera.fieldOfView=48;camera.transform.position=new Vector3(8,4.3f,-13);camera.transform.LookAt(new Vector3(8,1.7f,4.3f));WarehousePathLightingPass.Capture(camera,"StoreRefit_Overview");
            camera.transform.position=new Vector3(2.7f,2.5f,-7.5f);camera.transform.LookAt(new Vector3(5,1.1f,4.6f));camera.fieldOfView=48;WarehousePathLightingPass.Capture(camera,"StoreRefit_Aisles");
            camera.transform.position=new Vector3(10.4f,2.8f,-7.8f);camera.transform.LookAt(new Vector3(12.6f,1.2f,4.5f));WarehousePathLightingPass.Capture(camera,"StoreRefit_Counter");
            camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=fov;shadows.Advance(1.1f);WarehousePathLightingPass.Capture(camera,"StoreRefit_CoalerDoorShadows");shadows.Advance(.6f);WarehousePathLightingPass.Capture(camera,"StoreRefit_CoalerDoorShadows_Moving");
            if(shadows.silhouettes.Any(t=>t.GetComponentInChildren<RampagerController>(true)))throw new Exception("Door shadow model unexpectedly has AI");
            if(shadows.silhouettes.SelectMany(t=>t.GetComponentsInChildren<Renderer>(true)).Any(r=>r.shadowCastingMode!=ShadowCastingMode.ShadowsOnly))throw new Exception("Visible monster mesh at the cutaway doorway");
        }
        finally{shadows.ResetShadows();camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=fov;if(follow)follow.enabled=on;}
    }
}
