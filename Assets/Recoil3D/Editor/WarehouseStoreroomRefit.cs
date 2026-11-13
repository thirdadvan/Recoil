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

public static class WarehouseStoreroomRefit
{
    const string AssetsFolder="Assets/Recoil3D/StoreroomRefit";
    static Material steel,paint,concrete,wood,paper,rubber,red,ivory,copper;
    static int meshIndex,parts;
    static Material Mat(string name,Color color,float metallic=0)
    {
        string p=AssetsFolder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,p);}
        m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.16f);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string name,Transform parent,Vector3 position)
    {var t=new GameObject(name).transform;SceneManager.MoveGameObjectToScene(t.gameObject,parent.gameObject.scene);t.SetParent(parent,false);t.localPosition=position;return t;}
    static Transform Part(string name,Transform parent,Vector3 pos,Vector3 size,Material mat,PrimitiveType shape=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(shape);g.name=name;SceneManager.MoveGameObjectToScene(g,parent.gameObject.scene);g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;parts++;return g.transform;}
    static void Rod(string name,Transform parent,Vector3 a,Vector3 b,float radius,Material mat)
    {var t=Part(name,parent,(a+b)*.5f,new Vector3(radius*2,(a-b).magnitude*.5f,radius*2),mat,PrimitiveType.Cylinder);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Ring(string name,Transform parent,Vector3 p,float radius,float tube,Material mat)
    {
        const int n=20,k=6;var vs=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<=n;i++)for(int j=0;j<=k;j++){float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/k;float r=radius+Mathf.Cos(b)*tube;vs.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,Mathf.Sin(b)*tube));}
        for(int i=0;i<n;i++)for(int j=0;j<k;j++){int q=i*(k+1)+j;tris.AddRange(new[]{q,q+k+1,q+1,q+1,q+k+1,q+k+2});}
        var mesh=new Mesh();mesh.SetVertices(vs);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();var t=Group(name,parent,p);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;parts++;
    }
    static void Batch(Transform root)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.GetComponent<MeshRenderer>()).ToArray();
        foreach(var group in filters.GroupBy(f=>f.GetComponent<MeshRenderer>().sharedMaterial).ToArray())
        {
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.name=root.name+" material batch "+meshIndex;
            mesh.CombineMeshes(group.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());mesh.RecalculateBounds();
            string path=AssetsFolder+"/Mesh "+meshIndex+++".asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path))AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);
            var t=Group("Combined - "+group.Key.name,root,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
        }
        foreach(var f in filters)if(f)Object.DestroyImmediate(f.gameObject);
    }
    static void Box(Transform g,Vector3 p,int seed)
    {
        float w=.26f+(seed%3)*.065f,h=.22f+(seed%2)*.055f,d=.34f;
        var t=Group("Dusty tied spares carton",g,p);t.localRotation=Quaternion.Euler(0,(seed%5-2)*4,0);
        Part("Carton",t,new Vector3(0,h*.5f,0),new Vector3(w,h,d),paper);
        Part("Packing band",t,new Vector3(0,h*.5f,-d*.5f-.002f),new Vector3(.035f,h,.008f),wood);
        Part("Faded stock label",t,new Vector3(w*.14f,h*.55f,-d*.5f-.007f),new Vector3(w*.42f,h*.40f,.009f),ivory);
    }
    static void Bin(Transform parent,Vector3 p,int index)
    {
        var g=Group("Open steel small-parts bin",parent,p);float w=.36f,d=.37f;
        Part("Base",g,new Vector3(0,.015f,0),new Vector3(w,.03f,d),paint);
        foreach(float x in new[]{-w*.5f,w*.5f})Part("Raised side",g,new Vector3(x,.12f,0),new Vector3(.018f,.24f,d),paint);
        Part("Back",g,new Vector3(0,.12f,d*.5f),new Vector3(w,.24f,.018f),paint);
        Part("Front lip",g,new Vector3(0,.055f,-d*.5f),new Vector3(w,.11f,.02f),paint);
        Part("Label slot",g,new Vector3(0,.06f,-d*.5f-.014f),new Vector3(.17f,.05f,.012f),ivory);
        for(int j=0;j<5;j++)
        {
            var q=new Vector3(-.12f+j*.052f,.065f+(j%2)*.018f,-.07f+(j%3)*.055f);
            if(index%2==0){Ring("Bearing race",g,q,.033f,.011f,steel);}
            else{Rod("Spare bolt",g,q,q+new Vector3(.035f,.045f,.04f),.009f,steel);}
        }
    }
    static void Motor(Transform parent,Vector3 p,float scale)
    {
        var g=Group("Crated spare induction motor",parent,p);g.localScale=Vector3.one*scale;
        Part("Base runners",g,new Vector3(0,.06f,0),new Vector3(.7f,.12f,.5f),wood);
        var body=Part("Motor barrel",g,new Vector3(0,.31f,0),new Vector3(.44f,.3f,.44f),paint,PrimitiveType.Cylinder);body.localRotation=Quaternion.Euler(0,0,90);
        for(int i=0;i<8;i++)Part("Cooling rib",g,new Vector3(-.25f+i*.07f,.31f,0),new Vector3(.025f,.46f,.45f),steel);
        Rod("Drive shaft",g,new Vector3(.28f,.31f,0),new Vector3(.46f,.31f,0),.035f,steel);
        Part("Terminal box",g,new Vector3(-.03f,.58f,0),new Vector3(.24f,.16f,.23f),paint);
    }
    static void Shelf(Transform parent,Vector3 p,int kind,string name)
    {
        var g=Group(name,parent,p);const float width=1.65f,depth=.72f;
        foreach(float x in new[]{-width*.5f,width*.5f})foreach(float z in new[]{-depth*.5f,depth*.5f})
        {Part("Angle iron upright",g,new Vector3(x,1.31f,z),new Vector3(.045f,2.62f,.045f),steel);Part("Floor footplate",g,new Vector3(x,.025f,z),new Vector3(.11f,.05f,.12f),steel);}
        for(int row=0;row<4;row++)
        {
            float y=.18f+row*.61f;Part("Bent steel shelf",g,new Vector3(0,y,0),new Vector3(width+.06f,.055f,depth+.04f),paint);
            if(row==0&&kind==2){Motor(g,new Vector3(0,y+.03f,0),.85f);continue;}
            for(int n=0;n<3;n++)
            {
                var q=new Vector3(-.55f+n*.55f,y+.03f,-.03f);
                if(kind==0&&row<3)Bin(g,q,row+n);
                else if(kind==1&&row%2==0){Ring("Coiled drive belt",g,q+Vector3.up*.17f,.15f,.025f,rubber);Ring("Gasket set",g,q+new Vector3(.05f,.16f,.06f),.11f,.017f,rubber);}
                else if(kind==3&&row<2){Part("Insulator and fuse carton",g,q+Vector3.up*.12f,new Vector3(.39f,.24f,.36f),paper);for(int f=0;f<3;f++)Rod("Ceramic fuse",g,q+new Vector3(-.12f+f*.12f,.26f,-.12f),q+new Vector3(-.12f+f*.12f,.38f,-.12f),.025f,ivory);}
                else Box(g,q,n+row*5+kind*11);
            }
        }
        Rod("Diagonal back brace",g,new Vector3(-.8f,.2f,.37f),new Vector3(.8f,2.5f,.37f),.014f,steel);Batch(g);
    }
    static void DoorEnclosure(RecoilPrototype world)
    {
        var door=world.GetComponentsInChildren<RecoilDoor>(true).Single(d=>d.name=="Locked storeroom");float x=door.transform.position.x;
        var old=world.transform.Find("Locked storeroom entrance - background wall");
        // Keep all light and gameplay components; retire only the obsolete renderers.
        foreach(var v in old.GetComponentsInChildren<Recoil3DSpriteVisual>(true))if(v.name=="Storeroom alcove backing"||v.GetComponentInParent<RecoilDoor>()) {v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}
        foreach(var r in door.GetComponentsInChildren<Renderer>(true))r.enabled=false;
        var surround=door.transform.Find("Door surround - recessed steel and anchors");if(surround)surround.gameObject.SetActive(false);
        var g=Group("Storeroom - grounded brick and steel enclosure",world.transform,new Vector3(x,0,0));
        foreach(float side in new[]{-1f,1f})
        {
            float inner=side<0?-3.25f:.76f,outer=side<0?-.76f:3.25f,mid=(inner+outer)*.5f,w=outer-inner;
            Part("Low brick plinth",g,new Vector3(mid,.52f,2.16f),new Vector3(w,1.04f,.26f),concrete);
            Part("Faded sheet-steel front",g,new Vector3(mid,2.24f,2.17f),new Vector3(w,2.40f,.19f),paint);
            for(float xx=inner+.18f;xx<outer;xx+=.43f)Part("Sheet seam",g,new Vector3(xx,2.24f,2.06f),new Vector3(.023f,2.36f,.025f),steel);
            Part("Return wall",g,new Vector3(side*3.25f,1.72f,5.15f),new Vector3(.22f,3.44f,6.22f),concrete);
            Part("Corner upright",g,new Vector3(side*3.25f,1.73f,2.13f),new Vector3(.12f,3.46f,.18f),steel);
        }
        Part("Lintel over actual doorway",g,new Vector3(0,2.99f,2.16f),new Vector3(1.52f,.90f,.26f),paint);
        Part("Rear enclosure wall",g,new Vector3(0,1.72f,8.27f),new Vector3(6.72f,3.44f,.22f),concrete);
        Part("Flat steel roof",g,new Vector3(0,3.47f,5.13f),new Vector3(6.86f,.10f,6.53f),steel);
        for(int n=0;n<15;n++)Part("Corrugated roof rib",g,new Vector3(-3.25f+n*.46f,3.54f,5.13f),new Vector3(.025f,.045f,6.52f),steel);
        Part("Continuous floor skirting",g,new Vector3(0,.045f,2.13f),new Vector3(6.72f,.09f,.34f),steel);
        Batch(g);
        var d=Group("Storeroom - replacement steel door assembly",door.transform,Vector3.zero);d.position=new Vector3(x,0,1.99f);
        Part("Closed steel door",d,new Vector3(0,1.235f,.025f),new Vector3(1.22f,2.44f,.07f),paint);
        foreach(float xx in new[]{-.69f,.69f}){Part("Full-height folded jamb",d,new Vector3(xx,1.30f,.025f),new Vector3(.12f,2.60f,.21f),steel);for(int k=0;k<3;k++)Part("Anchor plate",d,new Vector3(xx,.28f+k*.90f,-.10f),new Vector3(.10f,.12f,.025f),steel);}
        Part("Jamb head",d,new Vector3(0,2.56f,.025f),new Vector3(1.50f,.10f,.21f),steel);
        Part("Threshold grounded on slab",d,new Vector3(0,.0125f,-.025f),new Vector3(1.38f,.025f,.34f),steel);
        Part("Recessed inspection glazing",d,new Vector3(0,1.94f,-.018f),new Vector3(.54f,.40f,.016f),rubber);
        foreach(float xx in new[]{-.29f,.29f})Part("Inspection window rim",d,new Vector3(xx,1.94f,-.04f),new Vector3(.035f,.46f,.025f),steel);
        foreach(float y in new[]{1.71f,2.17f})Part("Inspection window rim",d,new Vector3(0,y,-.04f),new Vector3(.61f,.035f,.025f),steel);
        for(float xx=-.24f;xx<.28f;xx+=.08f)Rod("Window security mesh",d,new Vector3(xx,1.75f,-.052f),new Vector3(xx,2.13f,-.052f),.006f,steel);
        for(float y=1.78f;y<2.13f;y+=.08f)Rod("Window security mesh",d,new Vector3(-.25f,y,-.052f),new Vector3(.25f,y,-.052f),.006f,steel);
        foreach(float y in new[]{.35f,1.25f,2.12f})Rod("Hinge barrel",d,new Vector3(-.61f,y,-.065f),new Vector3(-.61f,y+.16f,-.065f),.035f,steel);
        Part("Rim lock case",d,new Vector3(.46f,1.14f,-.045f),new Vector3(.18f,.23f,.08f),steel);
        Rod("Pull handle",d,new Vector3(.45f,1.0f,-.13f),new Vector3(.45f,1.25f,-.13f),.018f,steel);
        Part("Kick plate",d,new Vector3(0,.30f,-.018f),new Vector3(1.15f,.39f,.018f),steel);Batch(d);
    }
    static void Interior(RecoilPrototype world)
    {
        var g=Group("Storeroom - dense maintenance spares and supplies",world.transform,Vector3.zero);
        // The play lane remains clear at Z=0 / 0.7. Furnishings start behind Z=1.25.
        var shell=Group("Enclosing masonry behind shelves",g,Vector3.zero);
        Part("Painted rear masonry",shell,new Vector3(4,1.9f,7.55f),new Vector3(8,3.8f,.18f),concrete);
        Part("Worn lower wall paint",shell,new Vector3(4,.62f,7.44f),new Vector3(8,1.24f,.025f),paint);
        Batch(shell);
        Shelf(g,new Vector3(1.02f,0,6.65f),0,"Shelf 01 - bearings nuts and fasteners");
        Shelf(g,new Vector3(3.02f,0,6.65f),1,"Shelf 02 - belts seals and gaskets");
        Shelf(g,new Vector3(5.02f,0,6.65f),2,"Shelf 03 - motors and boxed spares");
        Shelf(g,new Vector3(7.02f,0,6.65f),3,"Shelf 04 - electrical fuses and supplies");
        Shelf(g,new Vector3(.98f,0,4.05f),0,"Shelf 05 - picking stock and loose parts");
        Shelf(g,new Vector3(6.98f,0,4.10f),1,"Shelf 06 - secondary boxed supplies");
        var bench=Group("Repair bench and tool board",g,new Vector3(3.1f,0,3.30f));
        foreach(float x in new[]{-.72f,.72f})foreach(float z in new[]{-.30f,.30f})Part("Workbench leg",bench,new Vector3(x,.45f,z),new Vector3(.07f,.90f,.07f),steel);
        Part("Worn timber worktop",bench,new Vector3(0,.95f,0),new Vector3(1.65f,.10f,.80f),wood);
        Part("Tool board",bench,new Vector3(0,1.7f,.36f),new Vector3(1.60f,1.15f,.07f),paint);
        for(int n=0;n<5;n++){float x=-.61f+n*.29f;Rod("Hanging spanner",bench,new Vector3(x,1.38f,.30f),new Vector3(x+.03f,1.84f-(n%2)*.12f,.30f),.018f,steel);Ring("Spanner ring end",bench,new Vector3(x,1.35f,.30f),.045f,.015f,steel);}
        Part("Bench vice base",bench,new Vector3(.46f,1.05f,-.10f),new Vector3(.31f,.10f,.24f),steel);Part("Vice jaws",bench,new Vector3(.46f,1.16f,-.10f),new Vector3(.33f,.16f,.09f),steel);
        Rod("Vice handle",bench,new Vector3(.46f,1.12f,-.30f),new Vector3(.46f,1.36f,-.30f),.012f,steel);
        Box(bench,new Vector3(-.4f,1.01f,.02f),5);Batch(bench);
        var reels=Group("Loose reels pallets and repair stock",g,Vector3.zero);
        for(int i=0;i<3;i++)
        {
            var p=new Vector3(4.5f+i*.72f,.32f,3.5f+(i%2)*.23f);
            foreach(float z in new[]{-.20f,.20f}){var t=Part("Wooden cable reel flange",reels,p+new Vector3(0,0,z),new Vector3(.62f,.045f,.62f),wood,PrimitiveType.Cylinder);t.localRotation=Quaternion.Euler(90,0,0);}
            var barrel=Part("Coiled black cable",reels,p,new Vector3(.44f,.18f,.44f),rubber,PrimitiveType.Cylinder);barrel.localRotation=Quaternion.Euler(90,0,0);
            Rod("Reel spindle",reels,p+Vector3.back*.28f,p+Vector3.forward*.28f,.025f,steel);
        }
        Motor(reels,new Vector3(1.0f,0,2.05f),1.0f);Motor(reels,new Vector3(6.6f,0,2.0f),.8f);
        for(int n=0;n<8;n++)Box(reels,new Vector3(3.9f+(n%4)*.47f,(n/4)*.29f,5.0f),n+10);
        for(int n=0;n<5;n++){var t=Part("Rolled repair canvas",reels,new Vector3(7.4f,.2f+n*.14f,5.2f),new Vector3(.36f,.11f,.40f),ivory,PrimitiveType.Capsule);t.localRotation=Quaternion.Euler(0,13*n,90);}
        var cabinet=Group("Closed lubricant and paint cabinet",reels,new Vector3(7.4f,0,2.05f));Part("Steel cabinet",cabinet,new Vector3(0,.78f,0),new Vector3(.8f,1.56f,.65f),paint);
        foreach(float x in new[]{-.20f,.20f}){Part("Cabinet leaf",cabinet,new Vector3(x,.82f,-.34f),new Vector3(.37f,1.38f,.035f),steel);Part("Latch",cabinet,new Vector3(x*.2f,.84f,-.37f),new Vector3(.025f,.16f,.025f),steel);}
        for(int i=0;i<4;i++){var p=new Vector3(6.4f+i*.22f,1.62f,2.05f);Part("Closed oil tin",reels,p,new Vector3(.16f,.25f,.18f),red);Rod("Tin handle",reels,p+new Vector3(-.04f,.14f,0),p+new Vector3(.04f,.14f,0),.012f,steel);}
        Batch(reels);
        // Small foreground stacks stop at the removed front wall, never behind the camera.
        var front=Group("Foreground packing at cutaway wall",g,Vector3.zero);
        for(int i=0;i<4;i++)Box(front,new Vector3(6.45f+i*.38f,0,-1.6f),i+30);Batch(front);
    }
    static void HideLegacyCrates(RecoilPrototype world)
    {
        foreach(var v in world.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
        if(v.name=="Spare parts crate"||v.name=="Crate strap"||v.name=="Background spare parts crate"||v.name=="Foreground spare parts crate")
        {v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}
    }
    [MenuItem("Recoil/Art/Rebuild warehouse storeroom enclosure and contents")]
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit Play first");
        Directory.CreateDirectory(AssetsFolder);AssetDatabase.Refresh();meshIndex=parts=0;
        steel=Mat("Oxidised steel",new Color(.19f,.22f,.21f),.5f);paint=Mat("Faded green steel",new Color(.21f,.27f,.23f),.25f);concrete=Mat("Dusty brick and plaster",new Color(.34f,.33f,.28f));wood=Mat("Old wood and cable reels",new Color(.28f,.21f,.13f));paper=Mat("Dusty kraft cartons",new Color(.35f,.29f,.20f));rubber=Mat("Rubber belts and dark recess",new Color(.035f,.042f,.038f));red=Mat("Faded red oil tins",new Color(.30f,.13f,.09f));ivory=Mat("Aged labels and ceramic",new Color(.57f,.53f,.40f));copper=Mat("Copper fittings",new Color(.34f,.22f,.13f),.4f);
        string backup="Backups/StoreroomRefit/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        var report=new List<string>();
        foreach(string path in new[]{"Assets/Scenes/3D/RecoilFactoryWarehouse.unity","Assets/Scenes/3D/RecoilWarehouseStoreroom.unity"})
        {
            var s=SceneManager.GetSceneByPath(path);if(!s.isLoaded)s=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(s,backup+"/"+Path.GetFileName(path),true);
            var w=s.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).Single();
            if(w.transform.Find("Storeroom - grounded brick and steel enclosure")||w.transform.Find("Storeroom - dense maintenance spares and supplies"))throw new Exception("Refit already applied; edit the existing groups");
            var doors=w.GetComponentsInChildren<RecoilDoor>(true).ToDictionary(d=>d.GetInstanceID(),d=>d.transform.position+"|"+d.destinationScene+"|"+d.requiredItemId);
            var collisions=w.GetComponentsInChildren<Collider2D>(true).ToDictionary(c=>c.GetInstanceID(),c=>c.transform.position+"|"+c.enabled);
            if(path.EndsWith("RecoilFactoryWarehouse.unity"))DoorEnclosure(w);else{HideLegacyCrates(w);Interior(w);}
            if(doors.Any(pair=>{var d=EditorUtility.InstanceIDToObject(pair.Key) as RecoilDoor;return !d||d.transform.position+"|"+d.destinationScene+"|"+d.requiredItemId!=pair.Value;}))throw new Exception("Door interaction changed");
            if(collisions.Any(pair=>{var c=EditorUtility.InstanceIDToObject(pair.Key) as Collider2D;return !c||c.transform.position+"|"+c.enabled!=pair.Value;}))throw new Exception("Gameplay collision changed");
            EditorSceneManager.MarkSceneDirty(s);EditorSceneManager.SaveScene(s);report.Add(path+": geometry saved; door logic and collision unchanged.");
        }
        AssetDatabase.SaveAssets();Directory.CreateDirectory("output/storeroom-refit");string result=string.Join("\n",report.ToArray())+"\nParts created="+parts+"; mesh assets="+meshIndex+"; backup="+backup;File.WriteAllText("output/storeroom-refit/verification.txt",result);return result;
    }
}
