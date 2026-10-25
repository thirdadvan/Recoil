using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class Recoil3DStoreWarmCornerPass
{
    static Material Mat(string name,Color colour,Color glow)
    {
        string path="Assets/Recoil3D/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=colour;m.SetColor("_EmissionColor",glow);m.SetFloat("_Metallic",.3f);m.SetFloat("_Glossiness",.45f);m.SetFloat("_Weathering",.15f);m.SetFloat("_HazeDensity",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform Group(string name,Transform parent,Vector3 p){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=p;return t;}
    static GameObject Part(string name,Transform root,Vector3 p,Vector3 scale,Material mat,PrimitiveType type=PrimitiveType.Cube)
    {var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=p;g.transform.localScale=scale;UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;return g;}
    static void Label(string name,Transform root,Vector3 p,string value,float size,Color colour)
    {var t=Group(name,root,p);var text=t.gameObject.AddComponent<TextMesh>();text.text=value;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.fontSize=64;text.characterSize=size;text.color=colour;}
    static void Batch(Transform root,string prefix)
    {
        var filters=root.GetComponentsInChildren<MeshFilter>();int number=0;
        foreach(var group in filters.GroupBy(f=>f.GetComponent<Renderer>().sharedMaterial))
        {
            var items=group.ToArray();var mesh=new Mesh();mesh.name=prefix+" "+number;mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(items.Select(f=>new CombineInstance{mesh=f.sharedMesh,transform=root.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray());
            AssetDatabase.CreateAsset(mesh,"Assets/Recoil3D/StoreCorner/"+prefix+"-"+number+++".asset");
            var t=Group("Batched "+group.Key.name,root,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=group.Key;
            foreach(var f in items)UnityEngine.Object.DestroyImmediate(f.gameObject);
        }
    }
    public static string Apply()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/Scenes/3D/RecoilServiceStore.unity")throw new Exception("Open service store outside Play.");
        var world=UnityEngine.Object.FindObjectOfType<RecoilPrototype>();if(world.transform.Find("Clerk corner - warm light and vintage slot machine"))return "Already authored";
        if(!AssetDatabase.IsValidFolder("Assets/Recoil3D/StoreCorner"))AssetDatabase.CreateFolder("Assets/Recoil3D","StoreCorner");
        var shaft=world.GetComponentsInChildren<Light>(true).First(l=>l.name=="Skylight daylight - child of opening");shaft.range=2.8f;shaft.spotAngle=80;shaft.intensity=2;
        var rim=world.GetComponentsInChildren<Light>(true).First(l=>l.name=="Soft sky bounce on inner curb");rim.range=1.7f;rim.intensity=2.5f;
        // A short reach keeps the cold light on the frame rather than painting a disc on the floor.
        foreach(var sync in world.GetComponentsInChildren<Recoil3DLight>(true).Where(l=>l.transform.position.x>12&&l.transform.position.x<15&&l.transform.position.z<3))
        {sync.workingColour=new Color(1,.65f,.32f);sync.intensityMultiplier=5.5f;sync.Sync();EditorUtility.SetDirty(sync);}
        var fixture=world.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Store ceiling lamp 2");var sprite=fixture.GetComponent<SpriteRenderer>();sprite.color=new Color(1,.72f,.38f);fixture.GetComponent<Recoil3DSpriteVisual>().Sync();
        var root=Group("Clerk corner - warm light and vintage slot machine",world.transform,Vector3.zero);
        var machine=Group("Vintage slot machine - move whole cabinet",root,new Vector3(11.12f,0,1.3f));
        var body=Group("Burgundy enamel cabinet and brass trim",machine,Vector3.zero);
        var burgundy=Mat("Slot machine burgundy enamel",new Color(.25f,.035f,.055f),Color.black);
        var brass=Mat("Slot machine champagne brass",new Color(.54f,.34f,.12f),new Color(.035f,.018f,.004f));
        var black=Mat("Slot machine dark glass",new Color(.025f,.025f,.032f),Color.black);
        var ivory=Mat("Slot machine ivory reels",new Color(.78f,.7f,.49f),new Color(.22f,.18f,.095f));
        var cream=Mat("Slot machine warm marquee",new Color(.85f,.61f,.22f),new Color(.9f,.43f,.065f));
        Part("Heavy pedestal",body,new Vector3(0,.12f,0),new Vector3(1.03f,.24f,.8f),black);
        Part("Enamel cabinet",body,new Vector3(0,.98f,0),new Vector3(.92f,1.75f,.66f),burgundy);
        Part("Golden cabinet base",body,new Vector3(0,.25f,-.05f),new Vector3(.98f,.07f,.74f),brass);
        Part("Reel glass recess",body,new Vector3(0,1.36f,-.35f),new Vector3(.82f,.6f,.035f),black);
        foreach(float x in new[]{-.27f,0,.27f})Part("Backlit reel",body,new Vector3(x,1.36f,-.378f),new Vector3(.235f,.45f,.035f),ivory);
        foreach(float y in new[]{1.1f,1.62f})Part("Reel frame",body,new Vector3(0,y,-.4f),new Vector3(.86f,.055f,.07f),brass);
        foreach(float x in new[]{-.445f,.445f})Part("Brass cabinet edge",body,new Vector3(x,1.27f,-.4f),new Vector3(.045f,1.49f,.065f),brass);
        var shelf=Part("Sloped control shelf",body,new Vector3(0,.96f,-.46f),new Vector3(.94f,.12f,.35f),brass);shelf.transform.localRotation=Quaternion.Euler(-12,0,0);
        Part("Coin return panel",body,new Vector3(0,.63f,-.35f),new Vector3(.59f,.28f,.045f),black);
        Part("Coin return lip",body,new Vector3(0,.49f,-.4f),new Vector3(.55f,.035f,.16f),brass);
        Part("Coin slot",body,new Vector3(.29f,.79f,-.36f),new Vector3(.025f,.12f,.025f),brass);
        Part("Marquee surround",body,new Vector3(0,1.94f,-.03f),new Vector3(1.06f,.46f,.75f),brass);
        Part("Glowing marquee",body,new Vector3(0,1.94f,-.42f),new Vector3(.88f,.29f,.025f),cream);
        var crown=Part("Rounded crown",body,new Vector3(0,2.19f,0),new Vector3(1.02f,.25f,.7f),burgundy,PrimitiveType.Sphere);
        var lever=Part("Side lever",body,new Vector3(.57f,1.36f,-.02f),new Vector3(.045f,.42f,.045f),brass);lever.transform.localRotation=Quaternion.Euler(0,0,-20);
        Part("Lever grip",body,new Vector3(.65f,1.57f,-.02f),Vector3.one*.13f,burgundy,PrimitiveType.Sphere);
        Label("Marquee lettering",machine,new Vector3(0,1.94f,-.447f),"LUCKY STAR",.023f,new Color(.23f,.055f,.04f));
        for(int i=0;i<3;i++)Label("Reel symbol",machine,new Vector3(-.27f+i*.27f,1.36f,-.401f),i==1?"★":"7",.064f,new Color(.57f,.045f,.035f));
        Label("Payout engraving",machine,new Vector3(0,.64f,-.38f),"JACKPOT",.019f,new Color(.82f,.58f,.2f));
        var colours=new[]{new Color(1,.37f,.045f),new Color(.8f,.055f,.035f),new Color(.08f,.55f,.38f),new Color(.32f,.16f,.8f)};
        var circuits=new Transform[4];for(int i=0;i<4;i++)circuits[i]=Group("Bulb circuit "+i,machine,Vector3.zero);
        int index=0;Action<Vector3> bulb=p=>{int group=index++%4;var mat=Mat("Slot bulb circuit "+group,colours[group]*.7f,colours[group]);Part("Jewel bulb",circuits[group],p,Vector3.one*.055f,mat,PrimitiveType.Sphere);};
        for(int i=0;i<14;i++){float x=-.49f+i*.98f/13;bulb(new Vector3(x,2.16f,-.449f));bulb(new Vector3(x,1.72f,-.449f));}
        for(int i=1;i<6;i++){bulb(new Vector3(-.49f,1.72f+i*.44f/6,-.449f));bulb(new Vector3(.49f,1.72f+i*.44f/6,-.449f));}
        for(int i=0;i<4;i++)Part("Illuminated button",body,new Vector3(-.29f+i*.19f,1.04f,-.52f),new Vector3(.11f,.035f,.08f),cream);
        Batch(body,"slot-cabinet");foreach(var circuit in circuits)Batch(circuit,"slot-bulbs-"+circuit.name.Last());
        var animation=machine.gameObject.AddComponent<Recoil3DSlotMachineLights>();animation.circuits=circuits.Select(t=>t.GetComponentInChildren<Renderer>()).ToArray();animation.colours=colours;
        var spill=Group("Warm machine spill - one real light",machine,new Vector3(0,1.65f,-.7f)).gameObject.AddComponent<Light>();spill.type=LightType.Point;spill.color=new Color(1,.48f,.18f);spill.intensity=.85f;spill.range=3;spill.renderMode=LightRenderMode.ForcePixel;
        // Practical light by the clerk, with its lamp model and light under one editable fixture.
        var lamp=Group("Warm counter pendant",root,new Vector3(13.6f,3.2f,-.35f));
        Part("Pendant cable",lamp,new Vector3(0,.48f,0),new Vector3(.018f,.95f,.018f),black);
        Part("Brass shade",lamp,Vector3.zero,new Vector3(.6f,.13f,.6f),brass,PrimitiveType.Cylinder);
        Part("Warm diffuser",lamp,new Vector3(0,-.14f,0),new Vector3(.48f,.025f,.48f),cream,PrimitiveType.Cylinder);
        var practical=Group("Warm light - child of shade",lamp,new Vector3(0,-.19f,0)).gameObject.AddComponent<Light>();practical.type=LightType.Spot;practical.transform.localRotation=Quaternion.Euler(90,0,0);practical.spotAngle=85;practical.range=4.5f;practical.intensity=2.2f;practical.color=new Color(1,.64f,.32f);practical.renderMode=LightRenderMode.ForcePixel;
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);return "Local cool skylight rim, warm clerk corner, brass and burgundy slot cabinet with 38 bulbs on four animated circuits.";
    }
}
