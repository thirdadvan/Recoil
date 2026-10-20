using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>Creates a separate editable 3D study from the SAVED interior layout. Never rewrites gameplay scenes.</summary>
public static class FactoryArtTestBuilder
{
    const string Folder="Assets/RecoilArtTest";
    const string ScenePath="Assets/Scenes/RecoilFactoryArt3D.unity";
    static Scene scene;
    static Transform architecture,background,foreground,details,cast;
    static Material concrete,steel,rust,dark,paint,glass,whiteLight,redLight,skin,coat,beam;
    static System.Random random;
    static Vector3 V(float x,float y,float z){return new Vector3(x,y,z);}
    static Transform Group(string name,Transform parent=null)
    {
        var g=new GameObject(name);SceneManager.MoveGameObjectToScene(g,scene);
        if(parent)g.transform.SetParent(parent,false);return g.transform;
    }
    static GameObject Shape(string name,PrimitiveType kind,Vector3 p,Vector3 scale,Material mat,Transform parent)
    {
        var g=GameObject.CreatePrimitive(kind);g.name=name;SceneManager.MoveGameObjectToScene(g,scene);
        g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        var r=g.GetComponent<Renderer>();r.sharedMaterial=mat;g.isStatic=true;return g;
    }
    static GameObject Box(string name,Vector3 p,Vector3 s,Material mat,Transform parent){return Shape(name,PrimitiveType.Cube,p,s,mat,parent);}
    static GameObject Rod(string name,Vector3 a,Vector3 b,float radius,Material mat,Transform parent)
    {
        var g=Shape(name,PrimitiveType.Cylinder,(a+b)*.5f,V(radius*2,Vector3.Distance(a,b)*.5f,radius*2),mat,parent);
        g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);return g;
    }
    static Material Mat(string name,Color color,float metallic,float smooth,bool texture=false,Color? emission=null)
    {
        string path=Folder+"/Materials/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);
        if(texture)
        {
            string tp=Folder+"/Materials/"+name+"_Surface.png";
            if(!System.IO.File.Exists(tp))
            {
                var t=new Texture2D(128,128,TextureFormat.RGB24,false);
                for(int y=0;y<128;y++)for(int x=0;x<128;x++){
                    float n=Mathf.PerlinNoise(x*.16f+1,y*.16f+8)*.5f+Mathf.PerlinNoise(x*.7f,y*.7f)*.3f+.2f;
                    if(name=="Concrete")n*=y%32==0?.5f:1;
                    t.SetPixel(x,y,new Color(n,n,n));
                }
                t.Apply();System.IO.File.WriteAllBytes(tp,t.EncodeToPNG());Object.DestroyImmediate(t);AssetDatabase.ImportAsset(tp);
            }
            m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(tp);m.mainTextureScale=new Vector2(3,2);
        }
        if(emission.HasValue){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value);}
        EditorUtility.SetDirty(m);return m;
    }
    static void Text(string name,string message,Vector3 p,float size,Color color,Transform parent)
    {
        var g=Group(name,parent);g.localPosition=p;g.localRotation=Quaternion.Euler(0,180,0);
        var t=g.gameObject.AddComponent<TextMesh>();t.text=message;t.fontSize=64;t.characterSize=size*.4f;
        t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color*.6f;
        // TextMesh faces -Z with identity rotation; factory camera is on the -Z side.
        g.localRotation=Quaternion.identity;
    }
    static void Fixture(string name,Vector3 p,Color color,float intensity,bool emergency=false)
    {
        var root=Group(name,background);root.localPosition=p;
        Box("Fixture housing",Vector3.zero,V(.7f,.15f,.4f),dark,root);
        var bulb=Box("Luminous strip",V(0,-.09f,-.08f),V(.52f,.04f,.22f),emergency?redLight:whiteLight,root);
        var pivot=Group("Light - child of fixture",root);pivot.localPosition=V(0,-.14f,-.1f);pivot.localRotation=Quaternion.Euler(90,0,0);
        var l=pivot.gameObject.AddComponent<Light>();l.type=LightType.Spot;l.color=color;l.intensity=emergency?intensity:intensity*1.7f;l.range=8;l.spotAngle=72;
        l.shadows=LightShadows.Soft;l.shadowStrength=.75f;l.shadowBias=.04f;
        if(emergency){var pulse=root.gameObject.AddComponent<FactoryArtLamp>();pulse.source=l;pulse.luminousSurface=bulb.GetComponent<Renderer>();pulse.peakIntensity=intensity;pulse.phase=p.x*.11f;}
        else
        {
            // Soft additive quad shows airborne haze; geometry and lighting remain real 3D.
            var g=Group("Light shaft - artistic haze",root);g.localPosition=V(0,-.1f,0);
            var mesh=new Mesh();mesh.vertices=new[]{V(-.08f,0,0),V(.08f,0,0),V(-2,-4.6f,0),V(2,-4.6f,0)};
            mesh.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,1),new Vector2(1,1)};mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateNormals();
            string mp=Folder+"/Materials/BeamMesh.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(mp);
            if(!existing){AssetDatabase.CreateAsset(mesh,mp);existing=mesh;}else Object.DestroyImmediate(mesh);
            g.gameObject.AddComponent<MeshFilter>().sharedMesh=existing;var r=g.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=beam;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
    }
    static void Figure(string name,Vector3 p,bool monster)
    {
        var root=Group(name,cast);root.localPosition=V(p.x,0,0);float h=monster?1.6f:1.7f;
        Shape("Torso",PrimitiveType.Capsule,V(0,h*.58f,0),V(monster?.4f:.36f,.38f,.26f),monster?dark:coat,root);
        Shape("Head",PrimitiveType.Sphere,V(.03f,h*.91f,0),V(.23f,.29f,.23f),skin,root);
        Rod("Left leg",V(-.1f,.68f,0),V(-.15f,.08f,-.02f),.06f,dark,root);
        Rod("Right leg",V(.1f,.68f,0),V(.2f,.08f,.02f),.06f,dark,root);
        Rod("Left arm",V(-.18f,1.16f,0),V(-.25f,.63f,-.03f),.045f,monster?skin:coat,root);
        Rod("Right arm",V(.18f,1.16f,0),V(.26f,.63f,0),.045f,monster?skin:coat,root);
        if(monster){root.localRotation=Quaternion.Euler(0,0,8);Box("Eye glint",V(-.04f,1.48f,-.119f),V(.035f,.02f,.012f),redLight,root);}
        else{Box("Satchel",V(.2f,.85f,.02f),V(.18f,.3f,.3f),rust,root);}
    }
    [MenuItem("Recoil/Art Test/Create Separate 3D Factory Study")]
    public static string Build()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Exit Play before creating art scene.");
        if(System.IO.File.Exists(ScenePath))throw new System.InvalidOperationException("Art scene already exists. Edit it directly; generator will not overwrite it.");
        System.IO.Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
        var original=SceneManager.GetActiveScene();var source=SceneManager.GetSceneByPath("Assets/Scenes/RecoilFactoryInterior.unity");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/RecoilFactoryInterior.unity",OpenSceneMode.Additive);
        var solids=new List<KeyValuePair<string,Bounds>>();var monsterPositions=new List<Vector3>();Vector3 player=V(3,0,0);
        foreach(var root in source.GetRootGameObjects()){
            foreach(var c in root.GetComponentsInChildren<BoxCollider2D>())if(c.name=="Maintenance Platform")solids.Add(new KeyValuePair<string,Bounds>(c.name,c.bounds));
            foreach(var e in root.GetComponentsInChildren<RampagerController>())monsterPositions.Add(e.transform.position);
            foreach(var p in root.GetComponentsInChildren<RecoilPlayerController>())player=p.transform.position;
        }
        scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);random=new System.Random(42);
        concrete=Mat("Concrete",new Color(.24f,.27f,.28f),0,.22f,true);
        steel=Mat("Weathered steel",new Color(.17f,.23f,.25f),.7f,.38f,true);
        rust=Mat("Oxidized metal",new Color(.26f,.13f,.075f),.45f,.25f,true);
        dark=Mat("Silhouette metal",new Color(.035f,.044f,.046f),.5f,.25f);
        paint=Mat("Old green paint",new Color(.14f,.21f,.18f),.3f,.32f,true);
        glass=Mat("Wet floor patch",new Color(.025f,.038f,.045f),.1f,.35f);
        whiteLight=Mat("Cold luminous glass",new Color(.35f,.6f,.65f),0,.4f,false,new Color(.8f,1.25f,1.4f));
        redLight=Mat("Emergency red glass",new Color(.5f,.04f,.01f),0,.3f,false,new Color(2,.04f,.01f));
        skin=Mat("Ashen skin",new Color(.34f,.32f,.29f),0,.2f);
        coat=Mat("Player ochre coat",new Color(.35f,.27f,.12f),0,.3f);
        beam=new Material(Shader.Find("Recoil/ArtTest/SoftBeam"));AssetDatabase.CreateAsset(beam,Folder+"/Materials/Haze.mat");
        architecture=Group("01 Structure - existing XY layout");background=Group("02 Depth - machinery and lighting Z 2 to 12");foreground=Group("03 Foreground - Z minus 4");details=Group("04 Surface details");cast=Group("05 Scale reference characters - art only");
        Box("Factory floor",V(15,-.45f,4),V(31,.9f,17),concrete,architecture);
        Box("Low ceiling",V(15,5.2f,4),V(31,.4f,17),dark,architecture);
        Box("Rear concrete wall",V(15,2.5f,12),V(31,5.4f,.5f),concrete,architecture);
        Box("Left end wall",V(-.3f,2.5f,4),V(.5f,5.4f,17),concrete,architecture);
        Box("Right end wall",V(30.3f,2.5f,4),V(.5f,5.4f,17),concrete,architecture);
        foreach(var item in solids){var b=item.Value;Box(item.Key,V(b.center.x,b.center.y,1.2f),V(b.size.x,b.size.y,3.4f),steel,architecture);
            for(int k=-1;k<=1;k+=2)Box("Platform support",V(b.center.x+k*b.extents.x*.8f,b.min.y*.5f,2.1f),V(.15f,b.min.y,.18f),rust,architecture);
            for(int k=0;k<10;k++)Box("Platform edge grating",V(b.min.x+k*b.size.x/10,b.max.y+.015f,-.4f),V(.035f,.04f,.16f),rust,details);
        }
        // Repeating structural bays give a low oppressive roof and true perspective depth.
        for(int x=0;x<=30;x+=5){var bay=Group("Structural bay "+x,background);
            Box("Rear pilaster",V(x,2.5f,11.65f),V(.35f,5,.6f),steel,bay);
            Box("Roof cross beam",V(x,4.86f,4),V(.24f,.27f,16),steel,bay);
            Box("Wall plinth",V(x+2.5f,.35f,11.66f),V(4.7f,.7f,.18f),dark,bay);
            for(int k=0;k<3;k++)Box("Wall seam",V(x+1+k*1.4f,2.5f,11.7f),V(.025f,4.4f,.02f),dark,bay);
        }
        for(int i=0;i<3;i++){
            Rod("Ceiling main pipe "+i,V(.2f,4.55f-i*.16f,4+i*.6f),V(29.8f,4.55f-i*.16f,4+i*.6f),.09f+i*.015f,i==1?rust:steel,background);
            for(int x=1;x<30;x+=3)Rod("Pipe collar",V(x-.04f,4.55f-i*.16f,4+i*.6f),V(x+.04f,4.55f-i*.16f,4+i*.6f),.13f, dark,background);
        }
        // Distant equipment, behind the playable plane.
        for(int i=0;i<6;i++){
            float x=4+i*4.2f;var machine=Group("Pressure vessel "+(i+1),background);machine.localPosition=V(x,0,7.2f+(i%2)*2);
            Shape("Tank",PrimitiveType.Cylinder,V(0,1.55f,0),V(1.25f,1.35f,1.25f),i%2==0?steel:paint,machine);
            Shape("Top dome",PrimitiveType.Sphere,V(0,2.9f,0),V(1.25f,.4f,1.25f),steel,machine);
            for(int k=0;k<3;k++)Shape("Tank band",PrimitiveType.Cylinder,V(0,.6f+k*.9f,0),V(1.29f,.025f,1.29f),rust,machine);
            Rod("Feed pipe",V(0,3,0),V(0,4.5f,0),.07f,steel,machine);
            Box("Pump base",V(0,.15f,0),V(1.7f,.3f,1.5f),dark,machine);
            Rod("Outlet",V(.6f,.65f,0),V(1.3f,.65f,0),.08f,rust,machine);
            Text("Tank stencil","0"+(i+1),V(0,1.8f,-.64f),.12f,new Color(.5f,.53f,.47f),machine);
        }
        // Raised back service catwalk, rails and ladder.
        Box("Rear catwalk",V(19,3.3f,9.6f),V(12,.15f,2),steel,background);
        for(int x=13;x<=25;x+=2){Rod("Rail post",V(x,3.35f,8.6f),V(x,4.15f,8.6f),.025f,rust,background);}
        Rod("Upper guard rail",V(13,4.15f,8.6f),V(25,4.15f,8.6f),.027f,rust,background);
        Rod("Lower guard rail",V(13,3.75f,8.6f),V(25,3.75f,8.6f),.023f,rust,background);
        for(int k=0;k<2;k++)Rod("Ladder side",V(26+k*.45f,.1f,8.4f),V(26+k*.45f,3.3f,8.4f),.035f,steel,background);
        for(int k=0;k<10;k++)Rod("Ladder rung",V(26,.2f+k*.3f,8.4f),V(26.45f,.2f+k*.3f,8.4f),.025f,steel,background);
        // Return door remains at the original left-hand position, recessed in depth.
        var door=Group("Entrance service door",background);door.localPosition=V(1.1f,0,2.5f);
        Box("Frame",V(0,1.25f,0),V(1.35f,2.6f,.25f),steel,door);
        Box("Door leaf",V(0,1.2f,-.16f),V(1.1f,2.35f,.12f),paint,door);
        Box("Door inset",V(0,1.65f,-.23f),V(.65f,.65f,.02f),dark,door);
        Box("Handle",V(.4f,1,-.27f),V(.035f,.25f,.08f),rust,door);
        Text("Exit sign","EXIT",V(0,2.85f,-.2f),.11f,new Color(.45f,.68f,.56f),door);
        // Electrical cabinets, louvers and sparse status points.
        for(int i=0;i<3;i++){var cabinet=Group("Switchgear "+i,background);cabinet.localPosition=V(7+i*.9f,0,3.5f);
            Box("Cabinet",V(0,.95f,0),V(.8f,1.9f,.55f),paint,cabinet);Box("Seam",V(0,.95f,-.286f),V(.025f,1.7f,.018f),dark,cabinet);
            for(int k=0;k<7;k++)Box("Vent",V(0,1.5f-k*.055f,-.29f),V(.5f,.025f,.02f),dark,cabinet);
            Box("Warning indicator",V(.22f,.86f,-.3f),V(.055f,.055f,.02f),redLight,cabinet);
        }
        for(int x=0;x<30;x++){
            Box("Floor expansion joint",V(x,.008f,3.5f),V(.018f,.014f,15),dark,details);
            if(x%3==0)Box("Drain slot",V(x,.015f,1.3f),V(.7f,.025f,.28f),dark,details);
        }
        for(int i=0;i<24;i++){
            float x=(float)random.NextDouble()*29,z=(float)random.NextDouble()*10-2;
            var wet=Shape("Damp patch",PrimitiveType.Cylinder,V(x,.012f,z),V(.4f+(float)random.NextDouble()*1.8f,.004f,.3f+(float)random.NextDouble()),glass,details);wet.transform.localRotation=Quaternion.Euler(0,random.Next(180),0);
        }
        for(int i=0;i<40;i++){float x=(float)random.NextDouble()*30;Box("Wall runoff stain",V(x,1.4f+(float)random.NextDouble()*1.5f,11.72f),V(.02f+(float)random.NextDouble()*.15f,.3f+(float)random.NextDouble()*2,.012f),dark,details);}
        // Foreground is deliberately sparse so it frames rather than hides the route.
        foreach(float x in new[]{-1f,11f,29f}){
            Rod("Foreground support",V(x,-.4f,-4),V(x,5.3f,-4),.14f,dark,foreground);
            Box("Support foot",V(x,.12f,-4),V(.65f,.25f,.6f),dark,foreground);
            for(int y=1;y<5;y++)Box("Brace plate",V(x,y,-4.15f),V(.4f,.13f,.07f),steel,foreground);
        }
        Rod("Foreground low pipe",V(-1,.15f,-3.8f),V(30,.15f,-3.8f),.11f,dark,foreground);
        for(int x=21;x<29;x+=2){Rod("Near barrier post",V(x,0,-3),V(x,.72f,-3),.025f,dark,foreground);}
        Rod("Near barrier rail",V(21,.72f,-3),V(29,.72f,-3),.03f,dark,foreground);
        Fixture("Cold fixture - entry",V(4,4.8f,1.5f),new Color(.55f,.78f,.86f),2.5f);
        Fixture("Cold fixture - platform",V(16,4.8f,2),new Color(.55f,.78f,.86f),3);
        Fixture("Cold fixture - far machinery",V(24,4.8f,7),new Color(.46f,.7f,.78f),3);
        Fixture("Emergency fixture",V(21,4.6f,1.8f),new Color(1,.035f,.008f),3,true);
        var fill=Group("Main Light - weak cold fill");fill.localRotation=Quaternion.Euler(45,-20,0);var sun=fill.gameObject.AddComponent<Light>();sun.type=LightType.Directional;sun.color=new Color(.45f,.6f,.7f);sun.intensity=.22f;sun.shadows=LightShadows.Soft;
        Figure("Player scale reference",player,false);int index=0;foreach(var p in monsterPositions)Figure("Rampager reference "+(++index),p,true);
        Text("Wall sector stencil","SECTOR 04  /  PRESSURE CONTROL",V(15,4.2f,11.7f),.17f,new Color(.32f,.38f,.35f),background);
        var camera=Group("Art Camera - A D pan - wheel zoom - Home reset");camera.localPosition=V(15,2.8f,-16);camera.localRotation=Quaternion.Euler(2,0,0);
        var cam=camera.gameObject.AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=false;cam.fieldOfView=33;cam.nearClipPlane=.1f;cam.farClipPlane=80;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.008f,.014f,.018f);cam.allowHDR=true;
        camera.gameObject.AddComponent<AudioListener>();var artCamera=camera.gameObject.AddComponent<FactoryArtCamera>();
        artCamera.filmGrade=new Material(Shader.Find("Recoil/ArtTest/FilmGrade"));AssetDatabase.CreateAsset(artCamera.filmGrade,Folder+"/Materials/FilmGrade.mat");
        RenderSettings.skybox=null;RenderSettings.reflectionIntensity=.08f;
        for(int i=0;i<3;i++){
            var bounce=Group("Equipment bounce light "+i,background);bounce.localPosition=V(6+i*9,2.7f,5);
            var light=bounce.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.45f,.65f,.7f);light.intensity=1.6f;light.range=9;light.shadows=LightShadows.None;
        }
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.18f,.22f,.25f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=new Color(.022f,.038f,.045f);RenderSettings.fogStartDistance=25;RenderSettings.fogEndDistance=55;
        DynamicGI.UpdateEnvironment();
        EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
        if(opened)EditorSceneManager.CloseScene(source,true);
        // Preserve any unsaved open gameplay scene. Keep art study active alongside it.
        if(original.isLoaded && original!=scene){
            if(original.isDirty){
                System.IO.Directory.CreateDirectory("Assets/Scenes/ArtTestBackups");
                string backup="Assets/Scenes/ArtTestBackups/"+original.name+"_"+System.DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
                if(!EditorSceneManager.SaveScene(original,backup,true))throw new System.InvalidOperationException("Could not preserve unsaved scene; left it open.");
                Debug.Log("Preserved unsaved scene edits: "+backup);
            }
            EditorSceneManager.CloseScene(original,true);
        }
        if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.in2DMode=false;SceneView.lastActiveSceneView.LookAt(V(15,2.5f,3),Quaternion.Euler(3,0,0),20);}
        Selection.activeGameObject=camera.gameObject;
        return ScenePath+" | reference monsters: "+monsterPositions.Count+" | separate 3D art study, no gameplay changes";
    }
}
