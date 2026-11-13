using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Rebuilds only the right-hand compound boundary and its occupation aftermath.
public static class FactoryGateOccupationPass
{
    const string Folder = "Assets/Recoil3D/GateOccupation";
    const string Marker = "Right factory monumental gate and abandoned occupation camp - editable";
    static Material steel, paint, rust, concrete, wood, dark, ash, earth, cloth, enamel, road, gravel;
    static int meshSerial;
    static T[] All<T>(Scene s) where T : Component { return s.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray(); }
    static Material Mat(string name, Color c, float metal = 0)
    {
        string path = Folder + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Recoil/3D/Weathered Surface")); AssetDatabase.CreateAsset(m, path); }
        m.color = c; m.SetFloat("_Metallic", metal); m.SetFloat("_Glossiness", .12f);
        m.SetFloat("_Weathering", .8f); m.SetFloat("_HazeDensity", 0); EditorUtility.SetDirty(m); return m;
    }
    static Transform Group(Transform p, string n, Vector3 at)
    { var t = new GameObject(n).transform; t.SetParent(p, false); t.localPosition = at; return t; }
    static Transform Box(Transform p, string n, Vector3 at, Vector3 size, Material mat, PrimitiveType kind = PrimitiveType.Cube)
    {
        var t = GameObject.CreatePrimitive(kind).transform; t.name = n; Object.DestroyImmediate(t.GetComponent<Collider>());
        t.SetParent(p, false); t.localPosition = at; t.localScale = size; t.GetComponent<Renderer>().sharedMaterial = mat; return t;
    }
    static void Rod(Transform p, string n, Vector3 a, Vector3 b, float radius, Material mat)
    { var t = Box(p, n, (a + b) * .5f, new Vector3(radius * 2, (b - a).magnitude * .5f, radius * 2), mat, PrimitiveType.Cylinder); t.localRotation = Quaternion.FromToRotation(Vector3.up, b - a); }
    static Mesh SaveMesh(Mesh mesh, string n)
    {
        string path = Folder + "/" + n + "-" + meshSerial++ + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (old) { EditorUtility.CopySerialized(mesh, old); Object.DestroyImmediate(mesh); return old; }
        AssetDatabase.CreateAsset(mesh, path); return mesh;
    }
    static void Surface(Transform p, string n, List<Vector3> v, List<int> tris, Material mat)
    {
        var m = new Mesh(); m.name = n; m.SetVertices(v); m.SetTriangles(tris, 0);
        m.SetUVs(0, v.Select(a => new Vector2(a.x + a.z, a.y + a.z)).ToList()); m.RecalculateNormals(); m.RecalculateBounds();
        var t = Group(p, n, Vector3.zero); t.gameObject.AddComponent<MeshFilter>().sharedMesh = SaveMesh(m, n); t.gameObject.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }
    static void Fence(Transform p, float a, float b)
    {
        var g = Group(p, "Fixed perimeter fence " + a, Vector3.zero);
        for (float z = a; z <= b; z += .43f)
        { Box(g, "Square iron perimeter upright", new Vector3(0, 3.05f, z), new Vector3(.10f, 5.9f, .10f), steel); Rod(g, "Pointed iron top", new Vector3(0, 6, z), new Vector3(0, 6.28f, z), .038f, steel); }
        foreach (float y in new[] { .4f, 2.9f, 5.65f }) Box(g, "Fixed fence rail", new Vector3(0, y, (a+b)/2), new Vector3(.15f, .15f, b-a), paint);
        for (float z=a; z<=b; z+=8) { Box(g,"Boundary concrete footing",new Vector3(0,.35f,z),new Vector3(.72f,.7f,.72f),concrete);Box(g,"Boundary steel post",new Vector3(0,3.3f,z),new Vector3(.26f,6.4f,.26f),steel); }
    }
    static void Gate(Transform p)
    {
        // Road centre -6.4, lamps -1.6. The actual double-leaf opening is in FRONT of the lamps.
        const float near=-12.3f, far=-.5f, mid=-6.4f;
        foreach (float z in new[] { near, far })
        {
            var g=Group(p,"Monumental reinforced concrete gate pier",new Vector3(0,0,z));
            Box(g,"Wide plinth",new Vector3(0,.3f,0),new Vector3(1.7f,.6f,1.65f),concrete);
            Box(g,"Tall tapered-looking pier shaft",new Vector3(0,4.15f,0),new Vector3(1.12f,7.7f,1.1f),concrete);
            Box(g,"Pier recessed steel face",new Vector3(-.575f,4.2f,0),new Vector3(.04f,5.8f,.48f),paint);
            Box(g,"Heavy overhanging cap",new Vector3(0,8.13f,0),new Vector3(1.55f,.35f,1.5f),concrete);
            Box(g,"Upper steel cap",new Vector3(0,8.36f,0),new Vector3(1.3f,.12f,1.3f),steel);
            for(int i=0;i<4;i++) Box(g,"Heavy hinge plate",new Vector3(-.17f,1+i*1.65f,z==near?.61f:-.61f),new Vector3(.5f,.40f,.12f),rust);
        }
        for(int side=0;side<2;side++)
        {
            float a=side==0?near+.61f:mid+.07f,b=side==0?mid-.07f:far-.61f;
            var leaf=Group(p,"Closed heavy vehicle gate leaf "+side,Vector3.zero);
            foreach(float z in new[]{a,b}) Box(leaf,"Box section gate stile",new Vector3(0,3.48f,z),new Vector3(.25f,6.65f,.21f),paint);
            foreach(float y in new[]{.25f,1.75f,4.3f,6.65f}) Box(leaf,"Deep welded horizontal rail",new Vector3(0,y,(a+b)/2),new Vector3(.25f,.19f,b-a+.18f),paint);
            Box(leaf,"Riveted steel lower infill",new Vector3(.025f,1,(a+b)/2),new Vector3(.11f,1.25f,b-a),paint);
            for(float z=a+.25f;z<b-.15f;z+=.32f)
            {
                float top=6.75f+.72f*(1-Mathf.Abs(z-mid)/5.3f);
                Box(leaf,"Close-set heavy square gate bar",new Vector3(0,(top+1.8f)/2,z),new Vector3(.095f,top-1.8f,.095f),steel);
                var v=new List<Vector3>{new Vector3(-.08f,top,z-.085f),new Vector3(.08f,top,z-.085f),new Vector3(.08f,top,z+.085f),new Vector3(-.08f,top,z+.085f),new Vector3(0,top+.32f,z)};
                Surface(leaf,"Forged spear head",v,new List<int>{0,4,1,1,4,2,2,4,3,3,4,0},steel);
                foreach(float y in new[]{.48f,1.5f}) Box(leaf,"Round rivet head",new Vector3(-.064f,y,z),new Vector3(.07f,.07f,.07f),rust,PrimitiveType.Sphere);
            }
            Rod(leaf,"Heavy gate diagonal brace",new Vector3(.15f,.4f,a+.15f),new Vector3(.15f,6.5f,b-.15f),.055f,steel);
        }
        // Flat industrial header, without borrowing a real institution's name or emblem.
        Box(p,"Overhead steel portal lintel",new Vector3(0,8.15f,mid),new Vector3(.55f,.50f,11.8f),paint);
        foreach(float z in new[]{-10.5f,-8.5f,-6.5f,-4.5f,-2.5f}) Rod(p,"Portal web brace",new Vector3(0,7.85f,z),new Vector3(0,8.45f,z+1.2f),.045f,rust);
        Box(p,"Centre locking bar",new Vector3(-.2f,1.9f,mid),new Vector3(.18f,.14f,1.1f),steel);
        Box(p,"Heavy padlock body",new Vector3(-.32f,1.65f,mid),new Vector3(.16f,.36f,.28f),rust);
        Fence(p,-48,near-.8f); Fence(p,far+.8f,65.6f);
    }
    static void Workbench(Transform p,Vector3 at,Quaternion rot)
    {
        var t=Group(p,"Shoved heavy workshop bench",at);t.localRotation=rot;
        for(int i=0;i<5;i++)Box(t,"Thick worn worktop plank",new Vector3(0,.96f,(i-2)*.18f),new Vector3(2.25f,.12f,.17f),wood);
        foreach(float x in new[]{-.9f,.9f})foreach(float z in new[]{-.32f,.32f})Box(t,"Steel angle bench leg",new Vector3(x,.46f,z),new Vector3(.075f,.91f,.075f),steel);
        foreach(float z in new[]{-.32f,.32f})Box(t,"Bench lower stretcher",new Vector3(0,.23f,z),new Vector3(1.87f,.08f,.06f),rust);
        Box(t,"Old vice base",new Vector3(-.65f,1.08f,-.25f),new Vector3(.26f,.15f,.29f),steel);
        foreach(float x in new[]{-.74f,-.48f})Box(t,"Vice jaw",new Vector3(x,1.2f,-.25f),new Vector3(.08f,.22f,.29f),steel);
        Rod(t,"Vice screw",new Vector3(-.8f,1.14f,-.25f),new Vector3(-.3f,1.14f,-.25f),.035f,rust);
        var renderers=t.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);t.position+=Vector3.up*(.035f-bounds.min.y);
    }
    static void Reel(Transform p,Vector3 at,Quaternion rot)
    {
        var g=Group(p,"Dislodged timber cable reel",at);g.localRotation=rot;
        foreach(float y in new[]{-.42f,.42f})
        {
            Box(g,"Thick timber reel cheek",new Vector3(0,y,0),new Vector3(1.65f,.075f,1.65f),wood,PrimitiveType.Cylinder);
            for(int i=0;i<8;i++){float a=i*Mathf.PI/4;Box(g,"Reel through bolt",new Vector3(Mathf.Cos(a)*.58f,y+(y>0?.08f:-.08f),Mathf.Sin(a)*.58f),new Vector3(.065f,.06f,.065f),rust,PrimitiveType.Cylinder);}
        }
        Box(g,"Cable reel barrel",Vector3.zero,new Vector3(.75f,.40f,.75f),wood,PrimitiveType.Cylinder);
        for(int j=0;j<10;j++)Box(g,"Remaining dusty cable winding",new Vector3(0,-.34f+j*.073f,0),new Vector3(.93f,.03f,.93f),dark,PrimitiveType.Cylinder);
    }
    static void Bench(Transform p,Vector3 at,float yaw,bool broken)
    {
        var g=Group(p,broken?"Broken displaced camp bench":"Rough shared camp bench",at);g.localRotation=Quaternion.Euler(broken?0:0,yaw,broken?12:0);
        for(int i=0;i<3;i++)Box(g,"Seat plank",new Vector3(i==1&&broken?.2f:0,.47f,(i-1)*.14f),new Vector3(i==1&&broken?1.45f:2.05f,.065f,.13f),wood);
        foreach(float x in new[]{-.72f,.72f}){Box(g,"Bench trestle",new Vector3(x,.23f,0),new Vector3(.13f,.46f,.42f),wood);}
    }
    static void Cloth(Transform p,Vector3 at,float yaw)
    {
        var g=Group(p,"Dragged crumpled wool blanket",at);g.localRotation=Quaternion.Euler(0,yaw,0);
        var vs=new List<Vector3>();var tris=new List<int>();int n=16;
        for(int j=0;j<=n;j++)for(int i=0;i<=n;i++){float x=i/(float)n,z=j/(float)n;vs.Add(new Vector3((x-.5f)*1.7f,.045f+.13f*Mathf.Pow(Mathf.Sin(x*16+z*5),2)*Mathf.Sin(z*Mathf.PI), (z-.5f)*1.25f));}
        for(int j=0;j<n;j++)for(int i=0;i<n;i++){int a=j*(n+1)+i,b=a+1,c=a+n+1,d=c+1;tris.AddRange(new[]{a,c,b,b,c,d});}
        Surface(g,"Uneven frayed wool surface",vs,tris,cloth);
    }
    static void Mug(Transform p,Vector3 at,Quaternion rot)
    {
        var g=Group(p,"Chipped empty enamel mug",at);g.localRotation=rot;
        var v=new List<Vector3>();var tris=new List<int>();int n=32;
        // Outer wall, inner wall and lip: genuinely hollow cup.
        for(int ring=0;ring<4;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n;float r=ring==0?.061f:ring==1?.072f:ring==2?.064f:.054f;float y=ring==0?.007f:ring==3?.025f:.15f;v.Add(new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r));}
        for(int ring=0;ring<3;ring++)for(int i=0;i<n;i++){int a=ring*n+i,b=ring*n+(i+1)%n,c=a+n,d=b+n;tris.AddRange(new[]{a,b,c,b,d,c});}
        Surface(g,"Hollow enamel cup walls",v,tris,enamel);Box(g,"Cup base",new Vector3(0,.015f,0),new Vector3(.11f,.006f,.11f),enamel,PrimitiveType.Cylinder);
        for(int i=0;i<18;i++){float a=(-100+i*200f/18)*Mathf.Deg2Rad,b=(-100+(i+1)*200f/18)*Mathf.Deg2Rad;Rod(g,"Loop enamel handle",new Vector3(.075f+Mathf.Cos(a)*.043f,.08f+Mathf.Sin(a)*.053f,0),new Vector3(.075f+Mathf.Cos(b)*.043f,.08f+Mathf.Sin(b)*.053f,0),.008f,enamel);}
    }
    static void Camp(Transform p)
    {
        var g=Group(p,"Abandoned ransacked roadside workers camp",new Vector3(-5.3f,0,6.4f));
        var patchVertices=new List<Vector3>{new Vector3(0,.009f,0)};var patchTriangles=new List<int>();
        for(int i=0;i<30;i++){float a=i*Mathf.PI*2/30;float r=1+.08f*Mathf.Sin(i*1.9f)+.06f*Mathf.Cos(i*3.7f);patchVertices.Add(new Vector3(Mathf.Cos(a)*4.2f*r,.008f,Mathf.Sin(a)*3.2f*r));}
        for(int i=0;i<30;i++)patchTriangles.AddRange(new[]{0,1+(i+1)%30,1+i});
        Surface(g,"Irregular eroded occupation ground stain",patchVertices,patchTriangles,earth);
        Group(g,"Meshy shelter anchor",new Vector3(-1.3f,0,1.2f));
        Bench(g,new Vector3(-.1f,0,-1),-17,false);Bench(g,new Vector3(2,0,1.5f),31,true);
        Cloth(g,new Vector3(-2.4f,.02f,-1),-23);Cloth(g,new Vector3(.4f,.02f,2.2f),71);
        var crate=Group(g,"Broken bread transport box",new Vector3(1.2f,.1f,-1.75f));crate.localRotation=Quaternion.Euler(0,27,7);
        Box(crate,"Bread box bottom",Vector3.zero,new Vector3(.8f,.07f,.54f),wood);
        foreach(float z in new[]{-.27f,.27f})for(int i=0;i<3;i++)Box(crate,"Open slatted bread box side",new Vector3(0,.11f+i*.1f,z),new Vector3(.8f,.075f,.04f),wood);
        foreach(float x in new[]{-.4f,.4f})Box(crate,"Bread box end",new Vector3(x,.2f,0),new Vector3(.04f,.37f,.54f),wood);
        Box(g,"Torn bread crate lid",new Vector3(2.15f,.1f,-1.85f),new Vector3(.8f,.05f,.54f),wood).localRotation=Quaternion.Euler(0,51,0);
        var hearth=Group(g,"Cold scattered campfire remains",new Vector3(1.8f,0,-.3f));
        Box(hearth,"Diffuse cold ash bed",new Vector3(0,.016f,0),new Vector3(1.15f,.028f,.95f),ash,PrimitiveType.Sphere);
        var rng=new System.Random(19781027);
        for(int i=0;i<13;i++){float a=i*2*Mathf.PI/13;Box(hearth,"Displaced hearth brick",new Vector3(Mathf.Cos(a)*(.6f+(i%3)*.06f),.065f,Mathf.Sin(a)*.5f),new Vector3(.23f,.12f,.12f),concrete).localRotation=Quaternion.Euler(i%3*7,i*31,0);}
        for(int i=0;i<9;i++){var t=Box(hearth,"Charred split firewood",new Vector3((float)rng.NextDouble()*.65f-.32f,.05f+i*.008f,(float)rng.NextDouble()*.5f-.25f),new Vector3(.55f,.06f,.08f),dark);t.localRotation=Quaternion.Euler(0,i*43,5);}
        Group(g,"Meshy pot anchor",new Vector3(2.55f,.02f,-.85f));
        Mug(g,new Vector3(-.8f,.51f,-1),Quaternion.identity);Mug(g,new Vector3(.6f,.09f,-1.7f),Quaternion.Euler(87,18,0));Mug(g,new Vector3(2.45f,.07f,.65f),Quaternion.Euler(0,27,77));
        for(int i=0;i<12;i++){var t=Box(g,"Scattered broken shelter timber",new Vector3(-3+(float)rng.NextDouble()*6,.05f,-2.5f+(float)rng.NextDouble()*5),new Vector3(.45f+(float)rng.NextDouble()*.6f,.05f,.09f),wood);t.localRotation=Quaternion.Euler(0,i*47,0);}
    }
    static void Road(Transform p)
    {
        var g=Group(p,"Foreground roadway - in front of yard lamps",Vector3.zero);
        Box(g,"Gravel shoulders",new Vector3(-18,-.035f,-6.4f),new Vector3(60,.065f,7.9f),gravel);
        Box(g,"Worn foreground vehicle road",new Vector3(-18,.008f,-6.4f),new Vector3(60,.035f,6.4f),road);
        // Short curve beyond the locked boundary joins the existing estate road without relocating it.
        var vs=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<=20;i++){float t=i/20f;float z=Mathf.Lerp(-6.4f,1.1f,t*t*(3-2*t));float x=10*t;vs.Add(new Vector3(x,.026f,z-3.2f));vs.Add(new Vector3(x,.026f,z+3.2f));if(i>0){int a=(i-1)*2;tris.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}}
        Surface(g,"Road bends to existing estate approach",vs,tris,road);
        for(int i=0;i<26;i++){var t=Box(g,"Tyre-worn road patch",new Vector3(-46+i*2.2f,.03f,-6.4f+(i%3-1)*1.8f),new Vector3(.6f+(i%4)*.2f,.008f,.18f),dark);t.localRotation=Quaternion.Euler(0,i%5*7,0);}
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var s=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!s.isLoaded)throw new Exception("Open outdoor scene first");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Backups/GateOccupation");Directory.CreateDirectory("output/gate-occupation");AssetDatabase.Refresh();
        var world=All<RecoilPrototype>(s).Single();var prior=world.transform.Find(Marker);
        if(prior)throw new Exception("Already applied: use dedicated refinement methods rather than duplicating scene props");
        string backup="Backups/GateOccupation/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(s,backup,true);
        var doors=All<RecoilDoor>(s).Select(d=>d.name+"|"+d.transform.position+"|"+d.destinationScene+"|"+d.destinationSpawn).ToArray();
        var warehouse=All<Transform>(s).Single(t=>t.name=="Factory Warehouse - exterior");Vector3 warehousePosition=warehouse.position;
        var old=All<Transform>(s).Single(t=>t.name=="Continuous iron gate - locked across yard depth");
        float gx=old.position.x+12;old.gameObject.SetActive(false);
        var wall=All<Transform>(s).Single(t=>t.name=="Factory right perimeter wall");wall.position=new Vector3(gx,wall.position.y,wall.position.z);
        steel=Mat("Dark oxidised gate iron",new Color(.17f,.20f,.19f),.6f);paint=Mat("Peeling charcoal green gate paint",new Color(.13f,.19f,.175f),.3f);
        rust=Mat("Deep brown iron corrosion",new Color(.24f,.125f,.065f),.2f);concrete=Mat("Soot stained monumental pier concrete",new Color(.34f,.35f,.315f));
        wood=Mat("Splintered old workshop timber",new Color(.29f,.235f,.165f));dark=Mat("Cold soot and perished cable",new Color(.035f,.04f,.035f));
        ash=Mat("Wet grey campfire ash",new Color(.17f,.18f,.16f));earth=Mat("Trampled muddy camp ground",new Color(.13f,.14f,.105f));cloth=Mat("Rotten grey wool blanket",new Color(.25f,.275f,.245f));
        enamel=Mat("Chipped dirty ivory enamel",new Color(.58f,.57f,.47f),.15f);road=Mat("Gate approach worn asphalt",new Color(.095f,.11f,.105f));gravel=Mat("Gate road gravel shoulder",new Color(.18f,.18f,.145f));
        meshSerial=0;var root=Group(world.transform,Marker,Vector3.zero);root.position=new Vector3(gx,0,0);wall.SetParent(root,true);Gate(root);Road(root);Camp(root);
        var remnants=Group(root,"Old barricade shoved clear by inward gate swing",Vector3.zero);
        Group(remnants,"Meshy tool cabinet anchor",new Vector3(-4.9f,0,-10.8f));
        Group(remnants,"Meshy locker anchor",new Vector3(-2.2f,0,-2.05f));
        Workbench(remnants,new Vector3(-5.7f,.05f,-1.8f),Quaternion.Euler(0,22,79));
        Reel(remnants,new Vector3(-6.4f,.83f,-10.4f),Quaternion.Euler(0,23,87));
        // Disturbed arcs and parallel scrape marks, no debris lined up like a fresh barricade.
        for(int side=0;side<2;side++)for(int i=0;i<7;i++)
        {float a=(15+i*9)*Mathf.Deg2Rad;float hinge=side==0?-11.7f:-1.1f;float z=hinge+(side==0?1:-1)*Mathf.Cos(a)*4.5f;var t=Box(remnants,"Old gate sweep scrape",new Vector3(-Mathf.Sin(a)*4.5f,.033f,z),new Vector3(.75f,.008f,.045f),gravel);t.localRotation=Quaternion.Euler(0,side==0?-i*9:i*9,0);}
        Physics2D.SyncTransforms();
        if(warehouse.position!=warehousePosition)throw new Exception("Warehouse moved unexpectedly");
        if(!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>d.name+"|"+d.transform.position+"|"+d.destinationScene+"|"+d.destinationSpawn)))throw new Exception("Door routes changed");
        if(root.GetComponentsInChildren<Collider>(true).Any(c=>!c.transform.IsChildOf(wall))||root.GetComponentsInChildren<Collider2D>(true).Any(c=>!c.transform.IsChildOf(wall)))throw new Exception("Unexpected decoration collision");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);
        string report="Gate shifted +12m to X="+gx+"; warehouse unchanged at "+warehousePosition+". Vehicle opening Z=-12.3..-0.5 (road centre -6.4, lamps -1.6). 8.4m piers; 11.8m portal; closed double leaves; original hard boundary colliders moved with visual. Road joins existing estate approach beyond gate. Meshy anchors ready. Door routes unchanged; no new prop collisions. Backup="+backup;
        File.WriteAllText("output/gate-occupation/verification.txt",report);return report;
    }
}
