using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class DampFactoryGroundPass
{
    const string Folder="Assets/Recoil3D/DampFactoryGround";
    const string Path="Assets/Scenes/3D/RecoilPrototype.unity";
    const string Marker="Damp factory ground - concept C applied";
    static Material concrete,asphalt,soil,water,moss,silt,iron,stone;
    static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Transform G(Transform p,string name,Vector3 position){var t=new GameObject(name).transform;t.SetParent(p,false);t.position=position;return t;}
    static Transform B(Transform p,string name,Vector3 position,Vector3 size,Material m)
    {var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;Object.DestroyImmediate(t.GetComponent<Collider>());t.name=name;t.SetParent(p,false);t.position=position;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
    static Material Ground(string name,Texture2D texture,int kind,Color tint)
    {
        var m=new Material(Shader.Find("Recoil/3D/Damp Industrial Ground"));m.name=name;m.mainTexture=texture;m.color=Color.white;m.SetColor("_GroundTint",tint);m.SetFloat("_Kind",kind);m.SetFloat("_Metres",4);m.SetFloat("_Wetness",kind==2?.3f:.80f);m.SetVector("_JointSize",new Vector4(4.2f,3.8f,0,0));m.enableInstancing=true;AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;
    }
    static Material Wear(string name,Color color,int shape)
    {var m=new Material(Shader.Find("Recoil/3D/Architectural Runoff"));m.name=name;m.SetColor("_Tint",color);m.SetFloat("_Shape",shape);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
    static Material Hardware(string name,Color color)
    {var m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));m.name=name;m.color=color;m.SetFloat("_Weathering",.85f);m.SetFloat("_HazeDensity",0);m.SetFloat("_Glossiness",.13f);AssetDatabase.CreateAsset(m,Folder+"/"+name+".mat");return m;}
    static void TextureSettings(string file)
    {var i=AssetImporter.GetAtPath(file) as TextureImporter;if(!i)throw new Exception("Texture not imported: "+file);i.sRGBTexture=true;i.mipmapEnabled=true;i.wrapMode=TextureWrapMode.Repeat;i.anisoLevel=8;i.maxTextureSize=2048;i.textureCompression=TextureImporterCompression.CompressedHQ;i.npotScale=TextureImporterNPOTScale.None;i.SaveAndReimport();}
    static Mesh SaveMesh(Mesh mesh,string name)
    {mesh.name=name;AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name+".asset"));return mesh;}
    static Transform Surface(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material material,List<Vector2> customUV=null)
    {
        var m=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};m.SetVertices(vertices);m.SetTriangles(triangles,0);m.SetUVs(0,customUV??vertices.Select(v=>new Vector2(v.x/4,v.z/4)).ToList());m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();SaveMesh(m,name);var t=G(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=m;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
    }
    static void Rect(Transform root,string name,float x0,float x1,float z0,float z1,float y,Material material)
    {Surface(root,name,new List<Vector3>{new Vector3(x0,y,z0),new Vector3(x0,y,z1),new Vector3(x1,y,z0),new Vector3(x1,y,z1)},new List<int>{0,1,2,2,1,3},material);}
    static void Retexture(Renderer renderer,Material material)
    {
        var f=renderer.GetComponent<MeshFilter>();if(!f||!f.sharedMesh)throw new Exception("Missing ground mesh: "+renderer.name);
        // Clone the already cut mesh, retaining both current sewer holes and all vertex positions.
        var mesh=Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++){var p=f.transform.TransformPoint(vertices[i]);var n=normals.Length>i?normals[i]:Vector3.up;uv[i]=Mathf.Abs(n.y)>.5f?new Vector2(p.x,p.z)/4:Mathf.Abs(n.x)>.5f?new Vector2(p.z,p.y)/4:new Vector2(p.x,p.y)/4;}
        mesh.uv=uv;mesh.RecalculateTangents();f.sharedMesh=SaveMesh(mesh,"UV metres - "+renderer.name);renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();EditorUtility.SetDirty(f);EditorUtility.SetDirty(renderer);
    }
    static void ConnectRoad(Transform root,Renderer west,Renderer east)
    {
        float from=west.bounds.max.x-.08f,to=east.bounds.min.x+.08f;
        var f=west.GetComponent<MeshFilter>();var points=f.sharedMesh.vertices.Select(v=>f.transform.TransformPoint(v)).Where(v=>v.x>20&&v.x<50).ToArray();
        float westZ=(points.Min(v=>v.z)+points.Max(v=>v.z))*.5f,westWidth=points.Max(v=>v.z)-points.Min(v=>v.z);float eastZ=east.bounds.center.z;
        foreach(bool shoulder in new[]{true,false})
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<=64;i++)
            {float u=i/64f;float x=Mathf.Lerp(from,to,u),smooth=u*u*(3-2*u),z=Mathf.Lerp(westZ,eastZ,smooth);float half=Mathf.Lerp(westWidth,east.bounds.size.z,smooth)*.5f+(shoulder?.52f:0);float y=shoulder?-.002f:Mathf.Lerp(.0235f,east.bounds.max.y+.0005f,u);vertices.Add(new Vector3(x,y,z-half));vertices.Add(new Vector3(x,y,z+half));if(i>0){int n=i*2;triangles.AddRange(new[]{n-2,n-1,n,n,n-1,n+1});}}
            Surface(root,shoulder?"Continuous damp gravel shoulder through missing road section":"Continuous asphalt link - foreground of yard lamps",vertices,triangles,shoulder?soil:asphalt);
        }
    }
    static void Patch(Transform root,string name,Vector3 center,Vector2 size,Material material,int seed)
    {
        var rng=new System.Random(seed);const int count=24;var v=new List<Vector3>{center};var uv=new List<Vector2>{new Vector2(.5f,.5f)};var tri=new List<int>();
        for(int i=0;i<count;i++){float angle=i*Mathf.PI*2/count;float r=.83f+(float)rng.NextDouble()*.17f;var q=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*r;v.Add(center+new Vector3(q.x*size.x*.5f,0,q.y*size.y*.5f));uv.Add(q*.5f+Vector2.one*.5f);tri.AddRange(new[]{0,(i+1)%count+1,i+1});}
        var t=Surface(root,name,v,tri,material,uv);t.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static void Grate(Transform root,float x,float z)
    {
        var g=G(root,"Flush silted rainwater grate",new Vector3(x,0,z));B(g,"Dark shallow drain recess",new Vector3(x,.008f,z),new Vector3(.9f,.012f,.28f),silt);
        foreach(float zz in new[]{-.16f,.16f})B(g,"Old cast iron grate edge",new Vector3(x,.016f,z+zz),new Vector3(1,.018f,.025f),iron);
        for(int j=0;j<11;j++)B(g,"Fixed cross bar",new Vector3(x-.43f+j*.086f,.019f,z),new Vector3(.027f,.022f,.29f),iron);
        Patch(root,"Silt around rainwater inlet",new Vector3(x,.004f,z),new Vector2(2.0f,.8f),moss,(int)x+81);
    }
    static void Details(Transform root)
    {
        var detail=G(root,"Localized shallow water silt and moss - walking plane kept clear",Vector3.zero);
        int seed=300;
        foreach(float x in new[]{61f,69f,82f,96f,111f,121f,134f,145f,154f})
        {
            float y=x<103.82f?.029f:.031f;float center=x<103.82f?Mathf.Lerp(-3.8f,-6.4f,Mathf.SmoothStep(0,1,(x-57.71f)/46.11f)):-6.4f;
            Patch(detail,"Shallow road edge puddle",new Vector3(x,y,center+2.15f),new Vector2(1.3f+(seed%3)*.6f,.52f+(seed%2)*.18f),water,seed++);
            Patch(detail,"Broken dirty puddle in foreground concrete",new Vector3(x+1,.003f,-12.5f-(seed%4)*2.8f),new Vector2(1.8f+(seed%3)*.5f,.65f),water,seed++);
        }
        foreach(float x in new[]{75.8f,84.5f,100.7f,114.5f,128f,138f})Patch(detail,"Small wet depression beside facade",new Vector3(x,.005f,-2.25f),new Vector2(1.4f,.5f),water,seed++);
        foreach(float x in new[]{76f,80f,85f,91f,96f,102f,112f,117f,123f,129f,136f,139f})
        {Patch(detail,"Irregular moss and silt at wall footing",new Vector3(x,.006f,2.48f),new Vector2(2.6f,.55f),moss,seed++);Patch(detail,"Rain washed sediment at apron edge",new Vector3(x+.8f,.003f,-2.65f),new Vector2(2,.48f),moss,seed++);}
        foreach(float x in new[]{66f,93f,120f,145f})Grate(detail,x,-2.83f);
        var rng=new System.Random(725);
        for(int i=0;i<70;i++)
        {float x=75+(float)rng.NextDouble()*65,z=i%2==0?2.55f:-2.68f;float size=.035f+(float)rng.NextDouble()*.08f;var t=B(detail,"Small washed brick and coal fragment",new Vector3(x,.018f,z+(float)rng.NextDouble()*.18f),new Vector3(size,.028f,size*.7f),i%3==0?iron:stone);t.rotation=Quaternion.Euler(0,i*47,0);}
    }
    static void BatchDetails(Transform root)
    {
        var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
        foreach(var material in rs.Select(r=>r.sharedMaterial).Distinct())
        {var members=rs.Where(r=>r.sharedMaterial==material).ToArray();var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(members.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);SaveMesh(mesh,"Combined damp yard - "+material.name);var t=G(root,"Batched damp yard - "+material.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var r=t.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=material;if(material==water||material==moss)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;foreach(var original in members)original.enabled=false;}
    }
    static string[] Collision(Scene s){return All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.enabled+"|"+c.bounds).ToArray();}
    static string[] Doors(Scene s){return All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();}
    static string[] Spawns(Scene s){return All<Transform>(s).Where(t=>t.name.Contains("Spawn")).Select(t=>t.GetInstanceID()+"|"+t.position).ToArray();}
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Exit play first");Directory.CreateDirectory(Folder);Directory.CreateDirectory("output/damp-factory-ground");Directory.CreateDirectory("Backups/DampFactoryGround");
        var previous=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(Path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(Path,OpenSceneMode.Additive);
        try
        {
            if(All<Transform>(scene).Any(t=>t.name==Marker))throw new Exception("Ground pass already applied");
            if(ShaderUtil.ShaderHasError(Shader.Find("Recoil/3D/Damp Industrial Ground"))||ShaderUtil.ShaderHasError(Shader.Find("Recoil/3D/Shallow Industrial Puddle")))throw new Exception("Shader error");
            var backup="Backups/DampFactoryGround/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-RecoilPrototype.unity";EditorSceneManager.SaveScene(scene,backup,true);
            var collisions=Collision(scene);var doors=Doors(scene);var spawns=Spawns(scene);
            TextureSettings(Folder+"/Textures/OldConcrete.png");TextureSettings(Folder+"/Textures/WornAsphalt.png");
            concrete=Ground("Damp worn slab concrete",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/OldConcrete.png"),0,new Color(.79f,.81f,.79f));
            asphalt=Ground("Damp cracked industrial asphalt",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/WornAsphalt.png"),1,new Color(.67f,.69f,.68f));
            soil=Ground("Wet silt and gravel shoulders",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/OldConcrete.png"),2,new Color(.5f,.47f,.37f));
            water=new Material(Shader.Find("Recoil/3D/Shallow Industrial Puddle"));water.name="Shallow rainwater in old depressions";water.color=new Color(.037f,.046f,.043f,.68f);AssetDatabase.CreateAsset(water,Folder+"/"+water.name+".mat");
            moss=Wear("Sparse moss and sediment wash",new Color(.085f,.105f,.035f,.54f),2);silt=Hardware("Black silt inside drain",new Color(.035f,.040f,.032f));iron=Hardware("Old dark iron grate and coal",new Color(.095f,.105f,.085f));stone=Hardware("Damp scattered brick grit",new Color(.21f,.16f,.11f));
            var world=All<RecoilPrototype>(scene).Single();var root=G(world.transform,Marker,Vector3.zero);var renderers=All<MeshRenderer>(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
            var west=renderers.Single(r=>r.name=="Curved public road enters factory rear gate");var east=renderers.Single(r=>r.name=="Worn foreground vehicle road");ConnectRoad(root,west,east);
            // Add only missing foreground/rear visual surfaces. Existing cut walking ground stays in place.
            Rect(root,"Continuous foreground concrete ties station to factory",-112,164,-40,-3,-.003f,concrete);
            Rect(root,"Rear factory ground beyond existing walking strip",45.7f,164,9,32,-.003f,concrete);
            int changed=0;
            foreach(var r in renderers)
            {
                bool yard=r.name=="3D mesh - Solid terrain"&&r.bounds.max.y<.3f;
                bool station=r.name=="Static detail - Weathered forecourt concrete";
                bool road=r.name=="Curved public road enters factory rear gate"||r.name=="Worn foreground vehicle road"||r.name=="Road bends to existing estate approach"||r.name=="Extended clear road between left queue and forecourt"||r.name=="Worn two-lane asphalt"||r.name=="Static checkpoint detail - Blended aged entrance asphalt";
                bool shoulder=r.name=="Worn gravel road shoulders"||r.name=="Gravel shoulders"||r.name=="Static checkpoint detail - Dirty gravel road shoulder";
                if(yard||station||road||shoulder){Retexture(r,shoulder?soil:road?asphalt:concrete);changed++;}
            }
            Details(root);BatchDetails(root);
            if(!collisions.SequenceEqual(Collision(scene)))throw new Exception("Original collision changed");if(!doors.SequenceEqual(Doors(scene)))throw new Exception("Original sewer or door state changed");if(!spawns.SequenceEqual(Spawns(scene)))throw new Exception("Spawn moved");
            if(root.GetComponentsInChildren<Collider>(true).Length+root.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("New decorative blockers");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            string report="PASS concept C: "+changed+" existing surfaces retextured using metre-scale UVs and preserved source vertices/holes. Connected road X="+west.bounds.max.x.ToString("F2")+".."+east.bounds.min.x.ToString("F2")+"; correct right road Z=-9.6..-3.2 in front of lamps. Foreground ties and rear visual ground added. 24 shallow puddles; sparse footing moss/silt, 4 flush grates and 70 small fragments. Original Collider2D states, all door JSON, and every spawn position unchanged. New geometry has no colliders. Backup="+backup+"\n";
            File.WriteAllText("output/damp-factory-ground/verification.txt",report);return report;
        }
        finally{if(opened)EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(previous);var w=All<Recoil3DWorld>(previous).FirstOrDefault();if(w)w.ApplyAtmosphere();}
    }
}
