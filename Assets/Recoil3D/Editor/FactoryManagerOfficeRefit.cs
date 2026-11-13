using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

// Restores the authored two-desk office as actual volumetric furniture.
public static class FactoryManagerOfficeRefit
{
    const string Path="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity";
    const string AssetsFolder="Assets/Art/Environment/ManagerOffice";
    const string Marker="Manager office - restored 3D furniture and fluorescent lighting";
    static Transform root;
    static Material wood,edge,metal,dark,cream,paper,green,glass,cloth,tube;
    static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Material Mat(string n,int hex)
    {
        string p=AssetsFolder+"/"+n+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,p);}m.color=RecoilPrototype.Hex(hex);m.SetFloat("_Glossiness",.12f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string n,Transform p,Vector3 position)
    {var t=new GameObject(n).transform;t.SetParent(p,false);t.localPosition=position;return t;}
    static Transform Box(string n,Transform p,Vector3 position,Vector3 size,Material m)
    {
        var t=Group(n,p,position);t.localScale=size;t.gameObject.AddComponent<MeshFilter>().sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx");var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;return t;
    }
    static Transform Cyl(string n,Transform p,Vector3 position,float radius,float height,Material m,int sides=12)
    {
        string path=AssetsFolder+"/Cylinder"+sides+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){var vs=new List<Vector3>();var tris=new List<int>();for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides,b=(j+1)*Mathf.PI*2/sides;var a0=new Vector3(Mathf.Cos(a),-.5f,Mathf.Sin(a));var b0=new Vector3(Mathf.Cos(b),-.5f,Mathf.Sin(b));int k=vs.Count;vs.AddRange(new[]{a0,b0,b0+Vector3.up,a0+Vector3.up,Vector3.down*.5f,a0,b0,Vector3.up*.5f,b0+Vector3.up,a0+Vector3.up});tris.AddRange(new[]{k,k+2,k+1,k,k+3,k+2,k+4,k+5,k+6,k+7,k+8,k+9});}mesh=new Mesh();mesh.SetVertices(vs);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);}
        var t=Group(n,p,position);t.localScale=new Vector3(radius,height,radius);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=m;return t;
    }
    static void Bar(string n,Transform p,Vector3 a,Vector3 b,float width,Material m)
    {var t=Box(n,p,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),m);t.rotation=p.rotation*Quaternion.FromToRotation(Vector3.up,(b-a).normalized);}
    static void Files(Transform p,Vector3 pos,int count)
    {var g=Group("Paper files and correspondence",p,pos);for(int i=0;i<count;i++){var b=Box("Cardboard file "+i,g,new Vector3((i%2)*.015f,i*.022f,0),new Vector3(.29f,.018f,.22f),i%3==0?paper:cream);b.localRotation=Quaternion.Euler(0,i%2==0?4:-3,0);}}
    static void Chair(Transform p,Vector3 pos,bool swivel)
    {
        var g=Group(swivel?"Old swivel office chair":"Visitor tubular chair",p,pos);
        Box("Vinyl seat",g,new Vector3(0,.48f,0),new Vector3(.48f,.10f,.47f),cloth);
        var back=Box("Vinyl backrest",g,new Vector3(0,.83f,.20f),new Vector3(.47f,.53f,.085f),cloth);back.localRotation=Quaternion.Euler(8,0,0);
        for(int side=-1;side<=1;side+=2)Bar("Back frame",g,new Vector3(side*.18f,.42f,.17f),new Vector3(side*.18f,1,.23f),.026f,metal);
        if(swivel){Cyl("Centre stem",g,new Vector3(0,.25f,0),.045f,.4f,metal);for(int i=0;i<5;i++){float a=i*Mathf.PI*2/5;var end=new Vector3(Mathf.Cos(a)*.31f,.08f,Mathf.Sin(a)*.31f);Bar("Five star base",g,new Vector3(0,.14f,0),end,.035f,metal);Box("Caster",g,end-Vector3.up*.025f,new Vector3(.07f,.07f,.05f),dark);}}
        else for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Bar("Tube leg",g,new Vector3(x*.19f,.45f,z*.18f),new Vector3(x*.22f,.03f,z*.21f),.027f,metal);
    }
    static void Desk(float x,float z,bool terminal)
    {
        var d=Group(terminal?"Desk A - manager paperwork":"Desk B - production reports",root,new Vector3(x,0,z));
        Box("Laminated wood worktop",d,new Vector3(0,.79f,0),new Vector3(1.95f,.065f,.83f),wood);
        Box("Dark edge band",d,new Vector3(0,.758f,0),new Vector3(1.96f,.025f,.84f),edge);
        Box("Modesty panel",d,new Vector3(0,.46f,.29f),new Vector3(1.8f,.55f,.035f),wood);
        Box("Drawer pedestal",d,new Vector3(.63f,.38f,0),new Vector3(.45f,.73f,.72f),edge);
        for(int i=0;i<3;i++){Box("Wood drawer",d,new Vector3(.63f,.19f+i*.21f,-.37f),new Vector3(.42f,.19f,.025f),wood);Box("Metal handle",d,new Vector3(.63f,.21f+i*.21f,-.393f),new Vector3(.17f,.022f,.035f),metal);}
        for(int zSide=-1;zSide<=1;zSide+=2)Box("Steel desk leg",d,new Vector3(-.82f,.38f,zSide*.31f),new Vector3(.045f,.75f,.045f),metal);
        Box("Desk blotter",d,new Vector3(.15f,.83f,-.05f),new Vector3(.58f,.008f,.38f),green);Files(d,new Vector3(.65f,.836f,.20f),5);
        if(terminal)
        {
            Box("Beige terminal base",d,new Vector3(-.48f,.87f,.13f),new Vector3(.50f,.08f,.41f),cream);
            Box("CRT housing",d,new Vector3(-.48f,1.10f,.18f),new Vector3(.49f,.41f,.42f),cream);
            Box("Black CRT bezel",d,new Vector3(-.48f,1.11f,-.039f),new Vector3(.405f,.32f,.024f),dark);
            Box("Unlit green CRT glass",d,new Vector3(-.48f,1.11f,-.055f),new Vector3(.35f,.265f,.008f),glass);
            var keyboard=Box("Keyboard",d,new Vector3(-.45f,.855f,-.26f),new Vector3(.52f,.045f,.18f),cream);keyboard.localRotation=Quaternion.Euler(-8,0,0);
            for(int row=0;row<3;row++)for(int col=0;col<10;col++)Box("Keyboard key",d,new Vector3(-.675f+col*.047f,.886f,-.315f+row*.045f),new Vector3(.034f,.008f,.03f),edge);
        }
        else
        {
            Box("Manual typewriter base",d,new Vector3(-.43f,.865f,0),new Vector3(.48f,.09f,.38f),dark);
            var top=Box("Typewriter keyboard",d,new Vector3(-.43f,.93f,-.10f),new Vector3(.43f,.065f,.20f),metal);top.localRotation=Quaternion.Euler(-16,0,0);
            Box("Typewriter carriage",d,new Vector3(-.43f,1.03f,.12f),new Vector3(.55f,.07f,.09f),dark);
            Box("Paper in roller",d,new Vector3(-.43f,1.17f,.14f),new Vector3(.28f,.26f,.009f),paper);
            for(int row=0;row<3;row++)for(int col=0;col<8;col++)Box("Typewriter key",d,new Vector3(-.60f+col*.048f,.98f,-.17f+row*.046f),new Vector3(.032f,.012f,.031f),cream);
        }
        var phone=Group("Black rotary telephone",d,new Vector3(.35f,.84f,.22f));Box("Phone base",phone,Vector3.up*.04f,new Vector3(.27f,.08f,.20f),dark);Cyl("Rotary dial",phone,new Vector3(0,.09f,-.025f),.065f,.012f,metal);
        Box("Handset bridge",phone,new Vector3(0,.15f,.055f),new Vector3(.30f,.045f,.06f),dark);for(int side=-1;side<=1;side+=2)Box("Handset ear piece",phone,new Vector3(side*.125f,.128f,.055f),new Vector3(.08f,.075f,.10f),dark);
        for(int i=0;i<10;i++)Box("Phone spiral cord",phone,new Vector3(.18f+(i%2)*.02f,.025f-i*.013f,.035f),new Vector3(.04f,.013f,.018f),dark);
        Cyl("Enamel office mug",d,new Vector3(.05f,.89f,.23f),.047f,.11f,cream);Cyl("Tea surface",d,new Vector3(.05f,.948f,.23f),.039f,.004f,edge);
        Box("Pen tray",d,new Vector3(.10f,.845f,-.28f),new Vector3(.27f,.025f,.055f),edge);Box("Pencil",d,new Vector3(.10f,.862f,-.28f),new Vector3(.21f,.008f,.008f),wood);
        Chair(root,new Vector3(x,.0f,z+.68f),true);
    }
    static void Cabinet(float x,float z)
    {
        var g=Group("Steel filing cabinet - four drawers",root,new Vector3(x,0,z));Box("Steel carcase",g,new Vector3(0,1,0),new Vector3(.70f,2,.58f),green);
        for(int i=0;i<4;i++){Box("Drawer front",g,new Vector3(0,.28f+i*.47f,-.301f),new Vector3(.65f,.43f,.032f),metal);Box("Drawer handle",g,new Vector3(0,.31f+i*.47f,-.333f),new Vector3(.22f,.025f,.04f),dark);Box("Label frame",g,new Vector3(0,.4f+i*.47f,-.325f),new Vector3(.18f,.055f,.008f),cream);}
        Box("Archive carton",g,new Vector3(0,2.14f,0),new Vector3(.6f,.27f,.46f),wood);Box("Archive carton label",g,new Vector3(0,2.14f,-.235f),new Vector3(.20f,.08f,.005f),paper);
    }
    static void Fixture(float x,float z,int id,List<Light> lights)
    {
        var g=Group("Fluorescent fitting "+id+" - twin tube",root,new Vector3(x,3.62f,z));
        Box("Painted steel reflector",g,new Vector3(0,.04f,0),new Vector3(1.42f,.075f,.33f),cream);
        for(int side=-1;side<=1;side+=2){var t=Cyl("Fluorescent tube",g,new Vector3(0,-.025f,side*.09f),.022f,1.25f,tube);t.localRotation=Quaternion.Euler(0,0,90);for(int end=-1;end<=1;end+=2)Box("Tube socket",g,new Vector3(end*.64f,-.01f,side*.09f),new Vector3(.055f,.055f,.055f),metal);}
        var source=g.gameObject.AddComponent<RecoilLight2D>();source.radius=4.8f;source.intensity=.43f;
        var key=Group("Fluorescent diffuse downlight",g,new Vector3(0,-.12f,0));key.localRotation=Quaternion.Euler(90,0,0);var l=key.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=new Color(.88f,.96f,.89f);l.range=7;l.spotAngle=138;l.innerSpotAngle=95;l.intensity=2.6f;l.shadows=LightShadows.Soft;l.shadowResolution=LightShadowResolution.Low;l.shadowBias=.025f;l.shadowNormalBias=.06f;l.renderMode=LightRenderMode.ForcePixel;lights.Add(l);
        var fill=Group("Fluorescent reflected light",g,new Vector3(0,-.28f,0)).gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.color=l.color;fill.range=5.6f;fill.intensity=.75f;fill.renderMode=LightRenderMode.ForcePixel;fill.shadows=LightShadows.None;lights.Add(fill);
    }
    static void Depth(Transform t,float z,float thickness)
    {var v=t.GetComponent<Recoil3DSpriteVisual>();if(!v)return;v.depth=thickness/Mathf.Abs(t.lossyScale.z);v.depthOffset=t.InverseTransformPoint(new Vector3(t.position.x,t.position.y,z)).z;v.Sync();EditorUtility.SetDirty(v);}
    static string PhysicsState(Scene scene){return string.Join("\n",All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+EditorJsonUtility.ToJson(c)).OrderBy(x=>x));}
    [MenuItem("Recoil/Factory Manager/Restore office furniture and fluorescent lamps")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        Directory.CreateDirectory(AssetsFolder);AssetDatabase.Refresh();Directory.CreateDirectory("Captures/FactoryManagerOffice");Directory.CreateDirectory("Backups/ManagerOfficeRestoration/2026-11-05");
        var previous=SceneManager.GetActiveScene();if(previous.isDirty){EditorSceneManager.SaveScene(previous,"Backups/ManagerOfficeRestoration/2026-11-05/Unsaved-"+previous.name+".unity",true);EditorSceneManager.SaveScene(previous);}
        var scene=SceneManager.GetSceneByPath(Path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        try
        {
            string backup="Backups/ManagerOfficeRestoration/2026-11-05/ManagerOffice.unity";if(!File.Exists(backup))EditorSceneManager.SaveScene(scene,backup,true);
            var all=All<Transform>(scene);var world=All<RecoilPrototype>(scene).Single();var manager=all.Single(t=>t.name=="FactoryManager");var item=manager.GetComponent<ContinuousInteractionItem>();string itemBefore=EditorJsonUtility.ToJson(item),physics=PhysicsState(scene);Vector3 originalManager=manager.position;
            var doors=All<RecoilDoor>(scene);string routes=string.Join("\n",doors.Select(d=>EditorJsonUtility.ToJson(d)));
            var old=world.transform.Find(Marker);if(old)Object.DestroyImmediate(old.gameObject);
            root=Group(Marker,world.transform,Vector3.zero);
            wood=Mat("Faded walnut veneer",0x776047);edge=Mat("Dark wood edges",0x463d31);metal=Mat("Painted warm grey steel",0x687069);dark=Mat("Bakelite and rubber",0x252a27);cream=Mat("Aged ivory plastic",0xb3b09a);paper=Mat("Old paper",0xc4bda2);green=Mat("Grey olive filing steel",0x505e50);glass=Mat("Unlit CRT glass",0x192f28);cloth=Mat("Faded olive vinyl",0x465447);tube=Mat("Fluorescent glass",0xe1eddd);tube.EnableKeyword("_EMISSION");tube.SetColor("_EmissionColor",new Color(.75f,.88f,.72f)*1.5f);EditorUtility.SetDirty(tube);
            var architecture=all.Single(t=>t && t.name=="Room architecture - editable");
            foreach(Transform t in architecture)
            {
                if(new[]{"Desk and chair","Cabinet","Wall paperwork","Visitor sofa","Ceiling fixture","MANAGER"}.Contains(t.name))t.gameObject.SetActive(false);
                if(t.name=="Room back wall")
                {
                    Depth(t,5.55f,.30f);var surface=t.GetComponent<Recoil3DSpriteVisual>();surface.suppressed=false;surface.source.color=new Color(.56f,.57f,.50f);surface.Sync();EditorUtility.SetDirty(surface);EditorUtility.SetDirty(surface.source);
                }
                if(t.name=="Left wall"||t.name=="Right wall")
                {var surface=t.GetComponent<Recoil3DSpriteVisual>();surface.source.color=new Color(.56f,.57f,.50f);surface.Sync();EditorUtility.SetDirty(surface.source);}
                if(t.name=="Dado")Depth(t,5.36f,.07f);
                if(t.name=="Room floor"||t.name=="Room ceiling"||t.name=="Left wall"||t.name=="Right wall")Depth(t,1.25f,8.5f);
            }
            foreach(var oldLight in All<Light>(scene))if(oldLight!=world.GetComponent<Recoil3DWorld>().playerVisualLight)oldLight.enabled=false;
            foreach(var link in All<Recoil3DLight>(scene))link.enabled=false;
            foreach(var t in all.Where(t=>t && (t.name=="Indoor fixed path lighting"||t.name=="Foreground silhouettes Z minus 5")))t.gameObject.SetActive(false);
            Desk(4.3f,1.75f,true);Desk(8.15f,2.45f,false);Cabinet(11.9f,4.8f);Cabinet(11.1f,4.8f);
            var sofa=Group("Visitor sofa - worn olive vinyl",root,new Vector3(10.8f,0,1.35f));Box("Sofa base",sofa,new Vector3(0,.33f,0),new Vector3(1.65f,.35f,.7f),edge);Box("Sofa back",sofa,new Vector3(0,.77f,.29f),new Vector3(1.65f,.68f,.18f),cloth);
            for(int side=-1;side<=1;side+=2){Box("Seat cushion",sofa,new Vector3(side*.39f,.55f,-.05f),new Vector3(.75f,.16f,.61f),cloth);Box("Padded arm",sofa,new Vector3(side*.86f,.6f,0),new Vector3(.15f,.39f,.75f),cloth);for(int z=-1;z<=1;z+=2)Box("Sofa foot",sofa,new Vector3(side*.72f,.08f,z*.25f),new Vector3(.06f,.16f,.06f),metal);}
            Chair(root,new Vector3(3.2f,0,.55f),false);
            var board=Group("Production supervision noticeboard",root,new Vector3(6.5f,2.25f,5.28f));Box("Wood frame",board,Vector3.zero,new Vector3(2.3f,1.15f,.06f),edge);Box("Cork surface",board,new Vector3(0,0,-.035f),new Vector3(2.19f,1.04f,.012f),wood);
            for(int i=0;i<6;i++){var sheet=Box("Pinned production report",board,new Vector3(-.88f+(i%3)*.72f,.2f-(i/3)*.43f,-.049f),new Vector3(.35f,.38f,.004f),paper);sheet.localRotation=Quaternion.Euler(0,0,i%2==0?-3:4);for(int line=0;line<4;line++)Box("Printed report line",board,new Vector3(-.88f+(i%3)*.72f,.29f-(i/3)*.43f-line*.055f,-.053f),new Vector3(.24f,.008f,.002f),metal);}
            var shelf=Group("Open ledger shelf",root,new Vector3(2.25f,0,4.85f));for(int side=-1;side<=1;side+=2)Box("Shelf upright",shelf,new Vector3(side*.60f,1,0),new Vector3(.055f,2,.45f),green);for(int row=0;row<4;row++){Box("Shelf plank",shelf,new Vector3(0,.25f+row*.48f,0),new Vector3(1.25f,.045f,.45f),wood);for(int col=0;col<6;col++){Box("Bound ledger",shelf,new Vector3(-.47f+col*.18f,.43f+row*.48f,-.04f),new Vector3(.13f,.31f,.28f),col%2==0?green:edge);Box("Ledger spine label",shelf,new Vector3(-.47f+col*.18f,.43f+row*.48f,-.184f),new Vector3(.07f,.065f,.003f),paper);}}
            var clock=Group("Office wall clock",root,new Vector3(9.35f,2.9f,5.24f));var frame=Cyl("Clock rim",clock,Vector3.zero,.27f,.05f,dark,16);frame.localRotation=Quaternion.Euler(90,0,0);var face=Cyl("Clock face",clock,new Vector3(0,0,-.033f),.242f,.008f,paper,16);face.localRotation=Quaternion.Euler(90,0,0);Box("Clock minute hand",clock,new Vector3(.055f,.055f,-.042f),new Vector3(.012f,.17f,.005f),dark).localRotation=Quaternion.Euler(0,0,-45);Box("Clock hour hand",clock,new Vector3(-.045f,.01f,-.045f),new Vector3(.11f,.016f,.005f),dark);
            var lamps=new List<Light>();Fixture(3.1f,.5f,1,lamps);Fixture(8.3f,.5f,2,lamps);Fixture(4.3f,3.8f,3,lamps);Fixture(10.2f,3.8f,4,lamps);
            var look=All<WarehouseAtmosphereTest>(scene).FirstOrDefault();if(look){look.originalLights=new Light[0];look.originalIntensities=new float[0];look.testIntensities=new float[0];look.decorativeLights=lamps.ToArray();look.lamps=lamps.Where(l=>l.type==LightType.Spot).ToArray();look.testAmbient=new Color(.10f,.115f,.10f);look.originalAmbient=look.testAmbient;look.testStart=35;look.testEnd=72;look.scattering=.025f;look.softGlow=.025f;if(look.contactShadows)look.contactShadows.SetActive(false);look.contactShadows=null;EditorUtility.SetDirty(look);look.ApplyLook();}
            var world3=world.GetComponent<Recoil3DWorld>();world3.ambientColour=new Color(.10f,.115f,.10f);world3.fogStartDistance=35;world3.fogEndDistance=72;EditorUtility.SetDirty(world3);
            if(PhysicsState(scene)!=physics || itemBefore!=EditorJsonUtility.ToJson(item)||manager.position!=originalManager||routes!=string.Join("\n",doors.Select(d=>EditorJsonUtility.ToJson(d))))throw new Exception("Gameplay configuration changed");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText("Captures/FactoryManagerOffice/Restoration.txt","PASS: prior two desks, terminal/typewriter, cabinets, sofa and paperwork rebuilt in 3D; wall/dado depths aligned; 4 twin-tube fluorescent fixtures. Manager/key interaction, door routes and colliders unchanged.\n");
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
    }
    public static void Capture(Camera camera,string name,int width=1600,int height=1000)
    {
        var rt=new RenderTexture(width,height,24);camera.targetTexture=rt;camera.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(width,height,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply();File.WriteAllBytes("Captures/FactoryManagerOffice/"+name+".png",tex.EncodeToPNG());RenderTexture.active=old;camera.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);
    }
}
