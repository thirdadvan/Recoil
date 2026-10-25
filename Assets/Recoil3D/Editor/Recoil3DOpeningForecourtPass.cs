using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;

public static partial class Recoil3DOpeningForecourtPass
{
    const string Root="Opening - roadside service station";
    const string Folder="Assets/Recoil3D/Opening";
    static int meshSerial;
    static Material cream,red,metal,rubber,glass,paint,concrete,asphalt,marking,earth,nearHill,farHill,trees,farRoad,window;
    static void FolderAt(string path){if(AssetDatabase.IsValidFolder(path))return;int n=path.LastIndexOf('/');FolderAt(path.Substring(0,n));AssetDatabase.CreateFolder(path.Substring(0,n),path.Substring(n+1));}
    static Material Material(string name,Color color,string template="Pitted concrete",bool distant=false,float contrast=.5f)
    {
        string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Recoil3D/Materials/"+template+".mat");m=distant?new Material(Shader.Find("Recoil/3D/Distant Atmosphere")):new Material(original);m.name=name;AssetDatabase.CreateAsset(m,path);}
        m.color=color;if(m.HasProperty("_Glossiness"))m.SetFloat("_Glossiness",.12f);if(distant)m.SetFloat("_Contrast",contrast);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    {var g=new GameObject(name).transform;g.SetParent(parent,false);g.localPosition=position;return g;}
    static GameObject Box(string name,Transform parent,Vector3 p,Vector3 size,Material material,PrimitiveType type=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return g;}
    static void Rod(string name,Transform p,Vector3 a,Vector3 b,float radius,Material material)
    {var g=Box(name,p,(a+b)*.5f,new Vector3(radius*2,Vector3.Distance(a,b)*.5f,radius*2),material,PrimitiveType.Cylinder);g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles)
    {var m=new Mesh();m.name=name;m.SetVertices(vertices);m.SetTriangles(triangles,0);m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,Folder+"/Meshes/"+(meshSerial++).ToString("0000")+"-"+name+".asset");return m;}
    static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material mat)
    {var g=new GameObject(name);g.transform.SetParent(parent,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.shadowCastingMode=ShadowCastingMode.Off;return g;}
    static void Quad(string name,Transform p,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Material mat)
    {MeshObject(name,p,SaveMesh(name,new List<Vector3>{a,b,c,d,a,b,c,d},new List<int>{0,1,2,0,2,3,6,5,4,7,6,4}),mat);}
    static void Loft(string name,Transform p,Vector4[] sections,float[] bottoms,Material mat)
    {
        var vs=new List<Vector3>();var ts=new List<int>();
        for(int i=0;i<sections.Length;i++){var s=sections[i];vs.Add(new Vector3(-s.y,bottoms[i],s.x));vs.Add(new Vector3(s.y,bottoms[i],s.x));vs.Add(new Vector3(s.z,s.w,s.x));vs.Add(new Vector3(-s.z,s.w,s.x));}
        for(int i=0;i<sections.Length-1;i++)for(int k=0;k<4;k++){int a=i*4+k,b=i*4+(k+1)%4,c=b+4,d=a+4;ts.AddRange(new[]{a,b,c,a,c,d});}
        ts.AddRange(new[]{0,3,2,0,2,1});int last=(sections.Length-1)*4;ts.AddRange(new[]{last,last+1,last+2,last,last+2,last+3});MeshObject(name,p,SaveMesh(name,vs,ts),mat);
    }
    static void Text(string name,Transform parent,Vector3 p,string text,float size,Color color)
    {var t=Group(name,parent,p).gameObject.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;}
    // Combine only this static art branch. Interaction nozzles, displays, lights and source objects remain independent.
    static void Combine(Transform root)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>(true).Where(f=>f.sharedMesh&&f.GetComponent<MeshRenderer>()&&f.GetComponent<MeshRenderer>().enabled).ToArray();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial))
        {
            var list=group.ToArray();if(list.Length<2)continue;
            var mesh=new Mesh();mesh.name="Batched "+root.name+" "+group.Key.name;
            mesh.CombineMeshes(list.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            AssetDatabase.CreateAsset(mesh,Folder+"/Meshes/"+(meshSerial++).ToString("0000")+"-batch.asset");MeshObject("Static detail - "+group.Key.name,root,mesh,group.Key);
            foreach(var f in list)UnityEngine.Object.DestroyImmediate(f.gameObject);
        }
    }
    static void HideLegacy(Transform root)
    {foreach(var v in root.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}foreach(var t in root.GetComponentsInChildren<TextMesh>(true))t.GetComponent<Renderer>().enabled=false;}
    static void Sedan(Transform oldCar)
    {
        HideLegacy(oldCar);oldCar.localScale=Vector3.one;oldCar.position=new Vector3(-60.3f,0,1.8f);
        var body=Group("Dun sedan - front toward camera",oldCar,Vector3.zero);
        Loft("Pressed steel body",body,new[]{new Vector4(-2.1f,.84f,.95f,.92f),new Vector4(-.8f,.9f,1,1.04f),new Vector4(1.35f,.9f,.98f,1.02f),new Vector4(2.1f,.83f,.91f,.87f)},new[]{.37f,.37f,.37f,.37f},paint);
        Loft("Sloping cabin and roof",body,new[]{new Vector4(-.92f,.87f,.86f,1.045f),new Vector4(-.4f,.91f,.73f,1.58f),new Vector4(.93f,.89f,.72f,1.58f),new Vector4(1.43f,.86f,.85f,1.03f)},new[]{1.025f,1.025f,1.025f,1.015f},paint);
        Quad("Windscreen",body,new Vector3(-.8f,1.08f,-.9f),new Vector3(.8f,1.08f,-.9f),new Vector3(.68f,1.535f,-.43f),new Vector3(-.68f,1.535f,-.43f),glass);
        Quad("Rear window",body,new Vector3(-.69f,1.52f,.97f),new Vector3(.69f,1.52f,.97f),new Vector3(.8f,1.08f,1.4f),new Vector3(-.8f,1.08f,1.4f),glass);
        foreach(float side in new[]{-1f,1f})
        {
            Quad("Front door window",body,new Vector3(side*.92f,1.08f,-.76f),new Vector3(side*.745f,1.53f,-.36f),new Vector3(side*.735f,1.53f,.18f),new Vector3(side*.92f,1.08f,.18f),glass);
            Quad("Rear door window",body,new Vector3(side*.92f,1.08f,.28f),new Vector3(side*.735f,1.53f,.28f),new Vector3(side*.735f,1.53f,.88f),new Vector3(side*.9f,1.08f,1.26f),glass);
            Box("Black window pillar",body,new Vector3(side*.84f,1.31f,.23f),new Vector3(.035f,.5f,.065f),rubber);
            Box("Door handle",body,new Vector3(side*1.015f,.99f,.06f),new Vector3(.045f,.045f,.19f),metal);
            Box("Rear handle",body,new Vector3(side*1.005f,.97f,1.02f),new Vector3(.04f,.045f,.17f),metal);
            Box("Door lower chrome strip",body,new Vector3(side*1.006f,.6f,.25f),new Vector3(.025f,.035f,2.3f),metal);
            Box("Wing mirror stalk",body,new Vector3(side*1.07f,1.1f,-.67f),new Vector3(.22f,.035f,.045f),metal);
            Box("Wing mirror housing",body,new Vector3(side*1.2f,1.15f,-.68f),new Vector3(.14f,.16f,.25f),paint);
            foreach(float z in new[]{-1.28f,1.38f})
            {
                var tyre=Box("Tyre",body,new Vector3(side*.96f,.35f,z),new Vector3(.68f,.125f,.68f),rubber,PrimitiveType.Cylinder);tyre.transform.localRotation=Quaternion.Euler(0,0,90);
                var hub=Box("Pressed wheel hub",body,new Vector3(side*1.09f,.35f,z),new Vector3(.39f,.012f,.39f),metal,PrimitiveType.Cylinder);hub.transform.localRotation=Quaternion.Euler(0,0,90);
            }
            Box("Rear lamp",body,new Vector3(side*.68f,.75f,2.13f),new Vector3(.39f,.2f,.06f),red);
        }
        Box("Front grille recess",body,new Vector3(0,.73f,-2.12f),new Vector3(1.75f,.3f,.07f),rubber);
        for(int i=0;i<5;i++)Box("Grille horizontal blade",body,new Vector3(0,.62f+i*.05f,-2.17f),new Vector3(.64f,.015f,.025f),metal);
        foreach(float x in new[]{-.73f,-.46f,.46f,.73f})
        {
            var ring=Box("Round headlight surround",body,new Vector3(x,.78f,-2.18f),new Vector3(.255f,.025f,.255f),metal,PrimitiveType.Cylinder);ring.transform.localRotation=Quaternion.Euler(90,0,0);
            var lens=Box("Unlit headlight glass",body,new Vector3(x,.78f,-2.21f),new Vector3(.21f,.025f,.21f),cream,PrimitiveType.Cylinder);lens.transform.localRotation=Quaternion.Euler(90,0,0);
        }
        Box("Front steel bumper",body,new Vector3(0,.49f,-2.23f),new Vector3(2.02f,.12f,.14f),metal);
        Box("Rear bumper",body,new Vector3(0,.48f,2.2f),new Vector3(1.98f,.12f,.14f),metal);
        Box("Number plate",body,new Vector3(0,.47f,-2.31f),new Vector3(.45f,.13f,.025f),cream);
        Rod("Left windscreen wiper",body,new Vector3(-.63f,1.1f,-.91f),new Vector3(-.12f,1.19f,-.82f),.012f,rubber);
        Rod("Right windscreen wiper",body,new Vector3(.1f,1.1f,-.91f),new Vector3(.6f,1.19f,-.82f),.012f,rubber);
        Combine(body);
    }
    static void Nozzle(Transform parent)
    {
        Box("Nozzle grip",parent,new Vector3(0,-.035f,0),new Vector3(.12f,.28f,.09f),rubber);
        Box("Metal trigger",parent,new Vector3(-.07f,-.04f,0),new Vector3(.035f,.16f,.04f),metal);
        Rod("Bent spout",parent,new Vector3(0,.11f,0),new Vector3(-.14f,.2f,0),.025f,metal);
        Rod("Spout end",parent,new Vector3(-.14f,.2f,0),new Vector3(-.25f,.18f,0),.022f,metal);
    }
    static void Pump(Transform parent,FuelPumpInteraction source,bool interactive,int number)
    {
        var art=Group("Dispenser - native 3D",parent,new Vector3(0,0,.65f));
        var details=Group("Static dispenser body",art,Vector3.zero);
        Box("Concrete pump island",details,new Vector3(0,.07f,0),new Vector3(1.6f,.14f,1.45f),concrete);
        Box("Painted plinth",details,new Vector3(0,.18f,0),new Vector3(1.08f,.16f,.75f),red);
        Box("Lower pump cabinet",details,new Vector3(0,.61f,0),new Vector3(.94f,.77f,.64f),cream);
        Box("Upper pump housing",details,new Vector3(0,1.3f,0),new Vector3(1.03f,.64f,.7f),cream);
        Box("Weathered red shoulder stripe",details,new Vector3(0,.97f,0),new Vector3(1.04f,.12f,.715f),red);
        Box("Black meter face",details,new Vector3(0,1.37f,-.36f),new Vector3(.83f,.33f,.035f),rubber);
        Box("Fuel grade label",details,new Vector3(-.26f,1.08f,-.36f),new Vector3(.23f,.1f,.025f),red);
        for(int i=0;i<4;i++)Box("Meter cabinet ventilation",details,new Vector3(.38f,.48f+i*.06f,-.33f),new Vector3(.08f,.016f,.025f),rubber);
        var hose=new GameObject("Looped rubber hose").AddComponent<LineRenderer>();hose.transform.SetParent(art,false);hose.useWorldSpace=false;hose.sharedMaterial=rubber;hose.startWidth=hose.endWidth=.045f;hose.positionCount=17;hose.shadowCastingMode=ShadowCastingMode.Off;
        for(int i=0;i<17;i++){float a=i/16f*Mathf.PI*1.65f;hose.SetPosition(i,new Vector3(.54f+.22f*Mathf.Sin(a),.8f+.57f*Mathf.Cos(a),-.18f));}
        var ready=Group("Meter - normal service",art,new Vector3(0,1.37f,-.386f));Text("Digital readout",ready,Vector3.zero,"IN SERVICE",.018f,new Color(.22f,.66f,.36f));
        var failed=Group("Meter - OUT OF ORDER",art,new Vector3(0,1.37f,-.386f));Text("Failure message",failed,Vector3.zero,"OUT OF\nORDER",.019f,new Color(.8f,.12f,.06f));
        if(interactive)
        {
            source.serviceDisplay=ready.gameObject;source.outOfOrderDisplay=failed.gameObject;
            var nozzle=Group("Native nozzle - follows pickup",source.nozzle,Vector3.zero);var s=source.nozzle.lossyScale;nozzle.localScale=new Vector3(1/Mathf.Abs(s.x),1/Mathf.Abs(s.y),1/Mathf.Abs(s.z));Nozzle(nozzle);
        }
        else
        {
            var display=art.gameObject.AddComponent<Recoil3DFuelPumpDisplay>();display.source=source;display.readyDisplay=ready.gameObject;display.failedDisplay=failed.gameObject;display.Sync();
            var nozzle=Group("Resting nozzle",art,new Vector3(.61f,1.12f,-.28f));Nozzle(nozzle);Combine(nozzle);
        }
        failed.gameObject.SetActive(false);Text("Pump number",art,new Vector3(0,1.77f,0),number.ToString("00"),.035f,new Color(.53f,.5f,.39f));Combine(details);
    }
    static float RoadX(float z){return -66.8f-.07f*Mathf.Max(0,z);}
    static void Road(Transform parent)
    {
        var root=Group("Road - foreground to misty horizon",parent,Vector3.zero);
        foreach(bool far in new[]{false,true})
        {
            float a=far?25:-30,b=far?155:25;var vs=new List<Vector3>();var tris=new List<int>();int count=far?26:12;
            for(int i=0;i<=count;i++){float z=Mathf.Lerp(a,b,i/(float)count);vs.Add(new Vector3(RoadX(z)-3.15f,.012f,z));vs.Add(new Vector3(RoadX(z)+3.15f,.012f,z));if(i>0){int n=i*2;tris.AddRange(new[]{n-2,n,n+1,n-2,n+1,n-1});}}
            MeshObject(far?"Road vanishes into countryside":"Worn two-lane asphalt",root,SaveMesh("Road strip",vs,tris),far?farRoad:asphalt);
        }
        for(float z=-25;z<140;z+=6)
        {
            var dash=Box("Faded centre dash",root,new Vector3(RoadX(z),.025f,z),new Vector3(.085f,.012f,2.4f),z<24?marking:farRoad);dash.transform.localRotation=Quaternion.Euler(0,-4,0);
        }
        for(float z=-25;z<100;z+=10)foreach(float side in new[]{-1f,1f})
        {Box("Roadside marker",root,new Vector3(RoadX(z)+side*3.7f,.35f,z),new Vector3(.12f,.7f,.1f),z<20?cream:trees);Box("Marker dark band",root,new Vector3(RoadX(z)+side*3.7f,.55f,z-.06f),new Vector3(.125f,.13f,.025f),rubber);}
        for(float z=3;z<80;z+=16)
        {
            float x=RoadX(z)-5.5f;Rod("Roadside utility pole",root,new Vector3(x,0,z),new Vector3(x,6.4f,z),.075f,metal);Box("Crossarm",root,new Vector3(x,6,z),new Vector3(2,.1f,.12f),metal);
            if(z<67)for(int line=-1;line<=1;line+=2)for(int n=0;n<6;n++)
            {float za=z+n*16/6f,zb=z+(n+1)*16/6f;float ya=6.15f-.45f*Mathf.Sin(n/6f*Mathf.PI),yb=6.15f-.45f*Mathf.Sin((n+1)/6f*Mathf.PI);Rod("Sagging overhead wire",root,new Vector3(RoadX(za)-5.5f+line*.8f,ya,za),new Vector3(RoadX(zb)-5.5f+line*.8f,yb,zb),.008f,metal);}
        }
        Combine(root);
    }
    static void Hill(Transform root,string name,float z,float height,Material mat)
    {
        var vs=new List<Vector3>();var tris=new List<int>();const int columns=56,rows=8;
        for(int j=0;j<=rows;j++)for(int i=0;i<=columns;i++)
        {float x=-155+i*195f/columns,depth=j/(float)rows;float ridge=height*(.61f+.16f*Mathf.Sin(i*.19f)+.16f*Mathf.Sin(i*.43f+1.7f));float zz=z+depth*38;float valley=Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,18,Mathf.Abs(x-RoadX(zz))));float y=-.08f+ridge*Mathf.Sin(depth*Mathf.PI)*valley;vs.Add(new Vector3(x,y,zz));if(i>0&&j>0){int n=j*(columns+1)+i;tris.AddRange(new[]{n-columns-2,n-1,n,n-columns-2,n,n-columns-1});}}
        MeshObject(name,root,SaveMesh(name,vs,tris),mat);
    }
    static void Roof(Transform parent,Vector3 p,Vector3 size,Material mat)
    {
        var g=Group("Pitched tile roof",parent,p);
        var v=new List<Vector3>{new Vector3(-size.x/2,0,-size.z/2),new Vector3(size.x/2,0,-size.z/2),new Vector3(0,size.y,-size.z/2),new Vector3(-size.x/2,0,size.z/2),new Vector3(size.x/2,0,size.z/2),new Vector3(0,size.y,size.z/2)};
        MeshObject("Roof geometry",g,SaveMesh("Village roof",v,new List<int>{0,2,1,3,4,5,0,3,5,0,5,2,1,2,5,1,5,4,0,1,4,0,4,3}),mat);
    }
    static void Crown(Transform p,Vector3 centre,Vector3 radius,int seed,Material mat)
    {
        var v=new List<Vector3>();var t=new List<int>();
        for(int j=0;j<=5;j++)for(int k=0;k<=8;k++)
        {
            float a=j*Mathf.PI/5,b=k*Mathf.PI/4,r=1+.13f*Mathf.Sin(k*2.7f+j*3.3f+seed);
            v.Add(centre+new Vector3(Mathf.Sin(a)*Mathf.Cos(b)*radius.x*r,Mathf.Cos(a)*radius.y,Mathf.Sin(a)*Mathf.Sin(b)*radius.z*r));
            if(j>0&&k>0){int n=j*9+k;t.AddRange(new[]{n-10,n-1,n,n-10,n,n-9});}
        }
        MeshObject("Irregular woodland crown",p,SaveMesh("Woodland crown",v,t),mat);
    }
    public static string ArrangePumpGrid(Scene scene)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Use the outdoor 3D scene outside Play mode.");
        var forecourt=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Forecourt - move all pump islands together");
        var interactions=forecourt.GetComponentsInChildren<FuelPumpInteraction>(true);
        if(interactions.Length<3)throw new Exception("Three interactive pump bodies are required.");
        var decorative=forecourt.GetComponentsInChildren<Recoil3DFuelPumpDisplay>(true).ToArray();
        if(decorative.Length!=6)throw new Exception("Six foreground/background pumps are required.");
        var rows=new Transform[3];
        for(int row=0;row<3;row++)
        {
            string name=row==0?"01 Foreground pump row":row==1?"02 Middle interactive pump row":"03 Background pump row";
            rows[row]=forecourt.Find(name);if(!rows[row])rows[row]=Group(name,forecourt,Vector3.zero);
            rows[row].localPosition=Vector3.zero;rows[row].localScale=Vector3.one;
        }
        for(int row=0;row<3;row++)for(int col=0;col<3;col++)
        {
            Transform pump=row==1?interactions[col].transform:decorative[(row==0?0:3)+col].transform.parent;
            pump.SetParent(rows[row],true);pump.position=new Vector3(-51+col*8,0,(row-1)*7.25f);pump.localScale=Vector3.one;
            int number=row*3+col+1;pump.name="Pump "+number.ToString("00")+(row==1?" - interactive":" - scenery");
            foreach(var text in pump.GetComponentsInChildren<TextMesh>(true))
            {
                if(text.name=="Digital readout"){text.text="IN SERVICE";text.characterSize=.018f;text.color=new Color(.22f,.66f,.36f);EditorUtility.SetDirty(text);}
                if(text.name=="Pump number"){text.text=number.ToString("00");EditorUtility.SetDirty(text);}
            }
            var interaction=pump.GetComponent<FuelPumpInteraction>();
            if(interaction)
            {
                var nozzle=interaction.nozzle.Find("Native nozzle - follows pickup");
                if(nozzle){var size=interaction.nozzle.lossyScale;nozzle.localScale=new Vector3(1/Mathf.Abs(size.x),1/Mathf.Abs(size.y),1/Mathf.Abs(size.z));}
            }
        }
        foreach(var display in decorative){display.source=interactions[0];EditorUtility.SetDirty(display);}
        for(int i=3;i<interactions.Length;i++)UnityEngine.Object.DestroyImmediate(interactions[i].gameObject);
        var canopy=forecourt.Find("Thin steel canopy - complete forecourt");var position=canopy.position;position.z=.65f;canopy.position=position;
        var fixtures=forecourt.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Fuel canopy light fixture")&&!t.name.StartsWith("3D mesh")).ToArray();
        for(int i=0;i<fixtures.Length;i++)
        {fixtures[i].SetParent(canopy,true);var p=fixtures[i].position;p.x=-51+i*(16f/Mathf.Max(1,fixtures.Length-1));p.z=.65f;p.y=4.32f;fixtures[i].position=p;}
        EditorSceneManager.MarkSceneDirty(scene);return "Nine equal-size dispensers in three aligned rows; green IN SERVICE; middle row remains interactive.";
    }
    public static string Refine(Scene scene)
    {
        if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Use the outdoor 3D scene outside Play mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        nearHill=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Near countryside atmosphere.mat");farHill=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Far countryside atmosphere.mat");trees=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Dark tree line.mat");window=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Distant isolated window.mat");
        earth=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/Road shoulder earth.mat");trees.SetFloat("_Contrast",.36f);EditorUtility.SetDirty(trees);
        var landscape=all.First(t=>t.name=="Countryside - low hills woods and mining settlement");var parent=landscape.parent;UnityEngine.Object.DestroyImmediate(landscape.gameObject);meshSerial=AssetDatabase.FindAssets("t:Mesh",new[]{Folder+"/Meshes"}).Select(AssetDatabase.GUIDToAssetPath).Select(p=>int.Parse(System.IO.Path.GetFileNameWithoutExtension(p).Split('-')[0])).DefaultIfEmpty(0).Max()+1;Landscape(parent);
        var far=all.First(t=>t.name=="Distant industrial town Z 35 to 70");
        for(int n=0;n<6;n++)
        {
            var block=far.Cast<Transform>().FirstOrDefault(t=>t.name=="Distant mill block "+n);if(!block||block.position.z>100)continue;
            var before=block.position;var stack=far.Cast<Transform>().FirstOrDefault(t=>t.name=="Chimney"&&Mathf.Abs(t.position.x-before.x-1)<.1f&&Mathf.Abs(t.position.z-before.z)<.1f);
            block.position+=new Vector3(110,0,100);if(stack)stack.position+=new Vector3(110,0,100);
        }
        // Split double-sided glass into independent face vertices, avoiding zero averaged normals.
        var car=all.First(t=>t.name=="Dun sedan - front toward camera");
        var glassRenderer=car.GetComponentsInChildren<MeshRenderer>().First(r=>r.sharedMaterial.name=="Smoked car glass");
        var mesh=glassRenderer.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var triangles=mesh.triangles;
        var flat=new Vector3[triangles.Length];var indices=new int[triangles.Length];for(int i=0;i<triangles.Length;i++){flat[i]=vertices[triangles[i]];indices[i]=i;}mesh.Clear();mesh.vertices=flat;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        BoundaryBank(all.First(t=>t.GetComponent<RecoilPrototype>()));
        var sign=all.FirstOrDefault(t=>t.name=="Unbranded service station fascia");if(sign)sign.GetComponent<TextMesh>().characterSize=.045f;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Refined wooded valley, road continuity, distant town placement and car glazing.";
    }
    static void BoundaryBank(Transform world)
    {
        if(world.Find("Roadside cutting - natural left boundary"))return;
        var wall=world.GetComponentsInChildren<Recoil3DSpriteVisual>(true).FirstOrDefault(v=>v.meshRenderer&&v.meshRenderer.bounds.center.x< -70&&v.meshRenderer.bounds.size.y>10&&v.meshRenderer.bounds.size.x<1);
        if(!wall)return;wall.suppressed=true;wall.Sync();EditorUtility.SetDirty(wall);
        var root=Group("Roadside cutting - natural left boundary",world,Vector3.zero);
        var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<22;i++)for(int level=0;level<5;level++)
        {
            float za=-47+i*4,zb=za+4,ya=level*2.85f,yb=(level+1)*2.85f;
            float xa=-76.5f-.25f*Mathf.Sin(za*.27f+level),xb=-76.5f-.25f*Mathf.Sin(zb*.27f+level);
            int n=vertices.Count;vertices.Add(new Vector3(xa,ya,za));vertices.Add(new Vector3(xa-.25f,yb,za));vertices.Add(new Vector3(xb-.25f,yb,zb));vertices.Add(new Vector3(xb,ya,zb));triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
        }
        for(int i=0;i<22;i++)
        {
            float za=-47+i*4,zb=za+4;int n=vertices.Count;
            vertices.Add(new Vector3(-76.75f,14.25f,za));vertices.Add(new Vector3(-95,17+Mathf.Sin(za*.13f)*2,za));vertices.Add(new Vector3(-95,17+Mathf.Sin(zb*.13f)*2,zb));vertices.Add(new Vector3(-76.75f,14.25f,zb));triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
        }
        MeshObject("Eroded roadside cutting",root,SaveMesh("Roadside cutting",vertices,triangles),earth);
    }
    static void Landscape(Transform parent)
    {
        var root=Group("Countryside - low hills woods and mining settlement",parent,Vector3.zero);
        Hill(root,"Near wooded ridge",36,13,nearHill);Hill(root,"Far low rolling hills",82,21,farHill);
        for(int i=0;i<26;i++)
        {
            float x=-140+i*5.5f+2*Mathf.Sin(i*6.2f),z=48+(i%4)*5;
            if(Mathf.Abs(x-RoadX(z))<8)continue;
            float col=(x+155)/195f*56,depth=(z-36)/38;
            float ground=-.08f+13*(.61f+.16f*Mathf.Sin(col*.19f)+.16f*Mathf.Sin(col*.43f+1.7f))*Mathf.Sin(depth*Mathf.PI);
            var tree=Group("Mixed woodland tree",root,new Vector3(x,ground,z));
            Rod("Trunk",tree,Vector3.zero,new Vector3(.15f,5.4f,0),.075f,trees);
            for(int k=0;k<3;k++)
            {
                float side=k%2==0?-1:1;
                Rod("Branch",tree,new Vector3(0,2.5f+k*.75f,0),new Vector3(side*1.4f,4.6f+k*.65f,.3f),.035f,trees);
                Crown(tree,new Vector3(side*.85f,4.75f+k*.55f,.2f),new Vector3(1.55f,1.1f,.95f),i*3+k,trees);
            }
        }
        for(int n=0;n<4;n++)
        {
            var house=Group("Low roadside settlement house "+n,root,new Vector3(-100+n*8,0,31+n%2*7));
            Box("Rendered masonry house",house,new Vector3(0,1.55f,0),new Vector3(5.6f,3.1f,5),nearHill);Roof(house,new Vector3(0,3.1f,0),new Vector3(6.1f,1.5f,5.6f),trees);
            Box("Brick chimney",house,new Vector3(1.2f,4.25f,.8f),new Vector3(.5f,1.5f,.6f),nearHill);
            for(int k=0;k<3;k++)Box("Small dim village window",house,new Vector3(-1.65f+k*1.65f,1.9f,-2.52f),new Vector3(.57f,.76f,.03f),k==1&&n==2?window:trees);
        }
        // One reclaimed spoil mound links the countryside to the coal-and-steel town.
        var mound=Group("Overgrown mine spoil heap",root,new Vector3(-25,0,64));
        var moundVertices=new List<Vector3>();var moundTriangles=new List<int>();
        for(int j=0;j<=16;j++)for(int i=0;i<=24;i++)
        {
            float x=-24+i*2,z=-18+j*2.25f;float h=11*Mathf.Exp(-((x*x/240)+(z*z/130)))-.4f;
            moundVertices.Add(new Vector3(x,h,z));if(i>0&&j>0){int n=j*25+i;moundTriangles.AddRange(new[]{n-26,n-1,n,n-26,n,n-25});}
        }
        MeshObject("Rounded reclaimed spoil terrain",mound,SaveMesh("Rounded spoil terrain",moundVertices,moundTriangles),farHill);
        Combine(root);
    }
    public static string Apply(Scene scene)
    {
        if(EditorApplication.isPlaying || scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Use the outdoor 3D scene outside Play mode.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();if(all.Any(t=>t.name==Root))return "Opening already authored; edit its objects in Scene.";
        FolderAt(Folder+"/Materials");FolderAt(Folder+"/Meshes");meshSerial=0;
        cream=Material("Pump aged ivory",new Color(.51f,.49f,.4f));red=Material("Faded petrol red",new Color(.33f,.09f,.055f),"Faded machine enamel");metal=Material("Dark galvanised steel",new Color(.25f,.28f,.27f),"Worn industrial steel");rubber=Material("Rubber and meter recess",new Color(.035f,.043f,.041f));glass=Material("Smoked car glass",new Color(.075f,.12f,.13f),"Soot covered glass");paint=Material("Dun sedan faded blue grey",new Color(.21f,.28f,.29f),"Faded machine enamel");concrete=Material("Weathered forecourt concrete",new Color(.29f,.29f,.255f));asphalt=Material("Worn road asphalt",new Color(.13f,.145f,.14f));marking=Material("Faded road paint",new Color(.4f,.41f,.35f));earth=Material("Road shoulder earth",new Color(.18f,.19f,.15f));
        nearHill=Material("Near countryside atmosphere",new Color(.009f,.015f,.015f),"",true,.65f);farHill=Material("Far countryside atmosphere",new Color(.014f,.022f,.024f),"",true,.36f);trees=Material("Dark tree line",new Color(.009f,.014f,.013f),"",true,.36f);farRoad=Material("Road fades into valley",new Color(.007f,.011f,.011f),"",true,.38f);window=Material("Distant isolated window",new Color(.19f,.22f,.16f),"",true,.25f);
        var world=all.First(t=>t.GetComponent<RecoilPrototype>());var root=Group(Root,world,Vector3.zero);var station=all.First(t=>t.name=="Service Station - editable exterior");
        var forecourt=Group("Forecourt - move all pump islands together",station,Vector3.zero);forecourt.gameObject.AddComponent<RecoilFuelStation>();
        var stations=all.Where(t=>t.name.StartsWith("Fuel Station - move this")).ToArray();int pumpNumber=0;
        for(int i=0;i<stations.Length;i++)
        {
            var old=stations[i];old.SetParent(forecourt,true);if(i==1)old.position=new Vector3(-49,0,0);
            var pumps=old.GetComponentsInChildren<FuelPumpInteraction>(true);
            foreach(var c in old.Cast<Transform>().Where(t=>t.name=="Canopy"||t.name=="Canopy stripe"||t.name=="Canopy support"))HideLegacy(c);
            foreach(var pump in pumps){HideLegacy(pump.transform);Pump(pump.transform,pump,true,++pumpNumber);EditorUtility.SetDirty(pump);}
            foreach(var fixture in old.Cast<Transform>().Where(t=>t.name.StartsWith("Fuel canopy light fixture")))
            {var pos=fixture.position;pos.y=4.32f;pos.z=2;fixture.position=pos;foreach(var sync in fixture.GetComponentsInChildren<Recoil3DLight>(true)){sync.intensityMultiplier=1.25f;EditorUtility.SetDirty(sync);}}
        }
        var source=forecourt.GetComponentsInChildren<FuelPumpInteraction>().First();
        foreach(float z in new[]{-4.5f,10f})foreach(float x in new[]{-51f,-43f,-35f})
        {var p=Group((z<0?"Foreground":"Background")+" pump island "+(++pumpNumber),forecourt,new Vector3(x,0,z));Pump(p,source,false,pumpNumber);}
        var canopy=Group("Thin steel canopy - complete forecourt",forecourt,new Vector3(-42.5f,0,4.5f));var staticArt=Group("Canopy steel and island surfacing",canopy,Vector3.zero);
        Box("Canopy flat roof",staticArt,new Vector3(0,4.65f,0),new Vector3(26,.22f,22),cream);
        Box("Faded red front fascia",staticArt,new Vector3(0,4.62f,-11.04f),new Vector3(26.05f,.24f,.075f),red);
        Box("Faded red rear fascia",staticArt,new Vector3(0,4.62f,11.04f),new Vector3(26.05f,.24f,.075f),red);
        foreach(float x in new[]{-12f,12f})foreach(float z in new[]{-10f,10f})
        {Box("Steel canopy column",staticArt,new Vector3(x,2.2f,z),new Vector3(.2f,4.4f,.2f),metal);Box("Column concrete foot",staticArt,new Vector3(x,.15f,z),new Vector3(.48f,.3f,.48f),concrete);}
        for(float z=-9;z<=10;z+=3)Box("Roof underside purlin",staticArt,new Vector3(0,4.42f,z),new Vector3(25.5f,.15f,.1f),metal);
        Box("Raised forecourt apron",staticArt,new Vector3(0,-.015f,1),new Vector3(30,.06f,26),concrete);
        for(int i=0;i<15;i++){float x=-14+i*2;Box("Concrete expansion joint",staticArt,new Vector3(x,.02f,1),new Vector3(.018f,.008f,25),rubber);}
        Combine(staticArt);Text("Unbranded service station fascia",canopy,new Vector3(0,4.64f,-11.1f),"PETROL  /  SERVICE",.045f,new Color(.55f,.52f,.42f));
        Sedan(all.First(t=>t.name=="Parked car"));Road(root);Landscape(root);ArrangePumpGrid(scene);ApplyAftermath(scene);
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();return "Rebuilt frontal sedan and road; 9 evenly spaced pumps across foreground, gameplay lane and background; added low countryside terrain, trees and village.";
    }
}
