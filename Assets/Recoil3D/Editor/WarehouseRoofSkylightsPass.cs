using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class WarehouseRoofSkylightsPass
{
    const string Folder="Assets/Recoil3D/WarehouseSkylights";
    const string Outside="Assets/Scenes/3D/RecoilPrototype.unity",Inside="Assets/Scenes/3D/RecoilFactoryWarehouse.unity";
    const string Marker="Warehouse sealed roof lanterns - natural light";
    static Material iron,rust,glass,concrete;
    static T[] All<T>(Scene scene)where T:Component{return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Transform G(Transform parent,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=at;return t;}
    static Transform Box(Transform parent,string name,Vector3 at,Vector3 size,Material material)
    {var t=GameObject.CreatePrimitive(PrimitiveType.Cube).transform;t.name=name;Object.DestroyImmediate(t.GetComponent<Collider>());t.SetParent(parent,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=material;return t;}
    static Material Mat(string name,Color color,float metal)
    {string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(mat,path);}mat.color=color;mat.SetFloat("_HazeDensity",0);mat.SetFloat("_Weathering",.75f);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Glossiness",.12f);return mat;}
    static void Beam(Transform p,string n,Vector3 a,Vector3 b,float width,Material mat)
    {var t=Box(p,n,(a+b)/2,new Vector3(width,width,(b-a).magnitude),mat);t.localRotation=Quaternion.LookRotation(b-a);}
    static void Pane(Transform p,string name,Vector3 a,Vector3 b,Vector3 c,Vector3 d)
    {
        var mesh=new Mesh();mesh.name=name;mesh.vertices=new[]{a,b,c,d,a,b,c,d};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.up,Vector2.one,Vector2.zero,Vector2.right,Vector2.up,Vector2.one};mesh.triangles=new[]{0,2,1,1,2,3,4,5,6,5,7,6};mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Dirty wired glass pane.asset"));var t=G(p,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=t.gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=glass;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
    }
    static void Lantern(Transform parent,float x,float y,float z,float width,float length,int index)
    {
        var g=G(parent,"Sealed wired glass roof lantern "+index,new Vector3(x,y,z));float half=width/2,depth=length/2,upstand=.22f,ridge=.90f;
        foreach(float side in new[]{-1f,1f})
        {
            Box(g,"Weathered concrete kerb",new Vector3(side*(half+.09f),.09f,0),new Vector3(.18f,.24f,length+.34f),concrete);
            Box(g,"Rusted steel eaves rail",new Vector3(side*half,upstand,0),new Vector3(.09f,.12f,length+.12f),iron);
        }
        foreach(float end in new[]{-depth,depth})Box(g,"Concrete end kerb",new Vector3(0,.09f,end),new Vector3(width,.24f,.18f),concrete);
        Box(g,"Continuous capped ridge",new Vector3(0,ridge,0),new Vector3(.14f,.12f,length+.3f),iron);
        int bays=Mathf.CeilToInt(length/1.15f);float step=length/bays;
        for(int i=0;i<=bays;i++)
        {
            float zz=-depth+i*step;
            foreach(float side in new[]{-1f,1f})Beam(g,"Close-set roof glazing bar",new Vector3(side*half,upstand,zz),new Vector3(0,ridge,zz),.065f,iron);
            if(i<bays)foreach(float side in new[]{-1f,1f})
            {
                float left=zz+.035f,right=zz+step-.035f;
                Pane(g,"Dirty intact wired glass",new Vector3(side*(half-.035f),upstand+.015f,left),new Vector3(side*.035f,ridge-.015f,left),new Vector3(side*(half-.035f),upstand+.015f,right),new Vector3(side*.035f,ridge-.015f,right));
            }
        }
        // Permanent close grid under the glass; never a traversable roof exit.
        for(float zz=-depth+.20f;zz<depth;zz+=.40f)Box(g,"Fixed anti-entry steel crossbar",new Vector3(0,.03f,zz),new Vector3(width,.035f,.035f),iron);
        for(float xx=-half+.20f;xx<half;xx+=.40f)Box(g,"Fixed anti-entry steel longitudinal bar",new Vector3(xx,.03f,0),new Vector3(.035f,.035f,length),iron);
        foreach(float zz in new[]{-depth,depth})foreach(float xx in new[]{-half,0,half})Box(g,"Corroded frame fixing",new Vector3(xx,.26f,zz),new Vector3(.10f,.035f,.10f),rust);
    }
    static void RoofPanels(Transform p,Bounds bounds,Rect[] holes,Material material)
    {
        var xs=new List<float>{bounds.min.x,bounds.max.x};var zs=new List<float>{bounds.min.z,bounds.max.z};foreach(var h in holes){xs.Add(h.xMin);xs.Add(h.xMax);zs.Add(h.yMin);zs.Add(h.yMax);}xs=xs.Distinct().OrderBy(x=>x).ToList();zs=zs.Distinct().OrderBy(z=>z).ToList();
        for(int i=0;i<xs.Count-1;i++)for(int j=0;j<zs.Count-1;j++)
        {float x=(xs[i]+xs[i+1])/2,z=(zs[j]+zs[j+1])/2;if(holes.Any(h=>h.Contains(new Vector2(x,z))))continue;Box(p,"Solid roof panel around sealed glazing",new Vector3(x,bounds.center.y,z),new Vector3(xs[i+1]-xs[i],bounds.size.y,zs[j+1]-zs[j]),material);}
    }
    static Texture2D Cookie()
    {
        string path=Folder+"/Wired glass aperture cookie.asset";var old=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(old)return old;
        const int n=256;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false,true);tex.name="Wired glass aperture cookie";tex.wrapMode=TextureWrapMode.Clamp;var pixels=new Color[n*n];
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {
            float u=(x+.5f)/n,v=(y+.5f)/n;float edge=(1-Mathf.SmoothStep(.34f,.42f,Mathf.Abs(u-.5f)))*(1-Mathf.SmoothStep(.43f,.49f,Mathf.Abs(v-.5f)));
            float bars=(Mathf.Abs(u-.5f)<.018f||Mathf.Abs((v*7)%1-.5f)<.035f)?.10f:1;float grime=.78f+.15f*Mathf.Sin(x*.17f+y*.09f)*Mathf.Sin(y*.13f);float a=edge*bars*grime;pixels[y*n+x]=new Color(a,a,a,a);
        }
        tex.SetPixels(pixels);tex.Apply();AssetDatabase.CreateAsset(tex,path);return tex;
    }
    static void NaturalLights(Transform parent,float x,int index)
    {
        var g=G(parent,"Daylight under sealed skylight "+index,new Vector3(x,7.10f,2.6f));var light=g.gameObject.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(.48f,.62f,.72f);light.intensity=2.6f;light.range=12.5f;light.spotAngle=64;light.innerSpotAngle=46;light.cookie=Cookie();light.shadows=LightShadows.Soft;light.shadowResolution=UnityEngine.Rendering.LightShadowResolution.Medium;light.shadowBias=.008f;light.shadowNormalBias=.025f;light.renderMode=LightRenderMode.ForcePixel;g.localRotation=Quaternion.LookRotation(new Vector3(.25f,-6.7f,-2.3f));
        var bounce=G(parent,"Weak reflected sky fill "+index,new Vector3(x,1.7f,.8f));var fill=bounce.gameObject.AddComponent<Light>();fill.type=LightType.Point;fill.color=new Color(.40f,.51f,.60f);fill.intensity=.18f;fill.range=4.8f;fill.shadows=LightShadows.None;
        // Native environmental lights, not RecoilLight2D electric fixtures: power-independent.
    }
    static void BatchOpaque(Transform root)
    {
        var rs=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.sharedMaterial!=glass).ToArray();
        foreach(var mat in rs.Select(r=>r.sharedMaterial).Distinct())
        {var chosen=rs.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.CombineMeshes(chosen.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Combined roof structure.asset"));var t=G(root,"Combined roof structure - "+mat.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;foreach(var r in chosen)r.enabled=false;}
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");Directory.CreateDirectory(Folder);Directory.CreateDirectory("Backups/WarehouseSkylights");Directory.CreateDirectory("output/warehouse-skylights");
        iron=Mat("Oxidised steel glazing frame",new Color(.17f,.22f,.21f),.5f);rust=Mat("Old roof fixing corrosion",new Color(.25f,.135f,.07f),.25f);concrete=Mat("Damp skylight concrete kerb",new Color(.31f,.33f,.29f),0);
        glass=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Dirty translucent wired glass.mat");if(!glass){glass=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(glass,Folder+"/Dirty translucent wired glass.mat");}glass.color=new Color(.19f,.27f,.29f,.52f);glass.SetFloat("_Mode",3);glass.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.SrcAlpha);glass.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);glass.SetInt("_ZWrite",0);glass.EnableKeyword("_ALPHABLEND_ON");glass.renderQueue=3000;glass.SetFloat("_Glossiness",.24f);glass.EnableKeyword("_EMISSION");glass.SetColor("_EmissionColor",new Color(.035f,.065f,.085f));
        var previous=SceneManager.GetActiveScene();var report=new System.Text.StringBuilder();
        foreach(string path in new[]{Outside,Inside})
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                var world=All<RecoilPrototype>(scene).Single();if(All<Transform>(scene).Any(t=>t.name==Marker))throw new Exception("Skylights already installed in "+path);
                EditorSceneManager.SaveScene(scene,"Backups/WarehouseSkylights/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+scene.name+".unity",true);
                var collisions=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled).ToArray();var routes=All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position).ToArray();var root=G(world.transform,Marker,Vector3.zero);root.position=Vector3.zero;
                if(path==Outside)
                {
                    var warehouse=All<Transform>(scene).Single(t=>t.name=="Factory Warehouse - exterior");root.SetParent(warehouse,true);var roof=warehouse.GetComponentsInChildren<MeshRenderer>().Single(r=>r.name=="Solid flat roof");Bounds bounds=roof.bounds;var exteriorCenters=new[]{bounds.center.x-9,bounds.center.x,bounds.center.x+9};var holes=exteriorCenters.Select(x=>new Rect(x-1.2f,6,2.4f,18)).ToArray();RoofPanels(root,bounds,holes,roof.sharedMaterial);roof.enabled=false;
                    foreach(var seam in warehouse.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Roof seam"))
                    {float z=seam.bounds.center.z;if(z>6&&z<24){seam.enabled=false;float a=bounds.min.x;foreach(var h in holes){Box(root,"Roof seam clear of glazing",new Vector3((a+h.xMin)/2,seam.bounds.center.y,z),new Vector3(h.xMin-a,.025f,.04f),iron);a=h.xMax;}Box(root,"Roof seam clear of glazing",new Vector3((a+bounds.max.x)/2,seam.bounds.center.y,z),new Vector3(bounds.max.x-a,.025f,.04f),iron);}}
                    for(int i=0;i<3;i++)Lantern(root,exteriorCenters[i],bounds.max.y,15,2.4f,18,i);
                    report.AppendLine("Exterior: three 18m wired-glass lanterns; roof front edge and original roof collision retained.");
                }
                else
                {
                    var ceiling=All<Transform>(scene).Single(t=>t.name=="Ceiling");var visual=ceiling.GetComponentsInChildren<Recoil3DSpriteVisual>().Single(v=>v.meshRenderer&&v.meshRenderer.name=="3D mesh - Ceiling");var renderer=visual.meshRenderer;Bounds bounds=renderer.bounds;var centers=new[]{9.5f,26f,42.5f};var holes=centers.Select(x=>new Rect(x-2.2f,-1,4.4f,10.5f)).ToArray();RoofPanels(root,bounds,holes,renderer.sharedMaterial);visual.suppressed=true;renderer.enabled=false;EditorUtility.SetDirty(visual);
                    for(int i=0;i<3;i++){Lantern(root,centers[i],bounds.max.y,4.25f,4.4f,10.5f,i);NaturalLights(root,centers[i],i);}
                    var look=All<WarehouseAtmosphereTest>(scene).Single();var apertures=centers.Select((x,i)=>G(root,"Fixed skylight volume aperture "+i,new Vector3(x,7.13f,2.6f))).ToArray();look.skylightOpening=apertures[0];look.additionalSkylightOpenings=apertures.Skip(1).ToArray();look.skylightWidthDepth=new Vector2(4.4f,8);look.skylightHeight=7.1f;look.skylightSpread=.025f;look.skylightFeather=.4f;look.skylightDensity=.0045f;look.skylightColour=new Color(.43f,.57f,.68f);EditorUtility.SetDirty(look);
                    report.AppendLine("Interior: matching three sealed apertures, six independent natural lights, three restrained rectangular daylight volumes; no electric-light power bindings.");
                }
                BatchOpaque(root);Physics2D.SyncTransforms();if(!collisions.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled)))throw new Exception("Original roof/ceiling collision changed");if(!routes.SequenceEqual(All<RecoilDoor>(scene).Select(d=>EditorJsonUtility.ToJson(d)+"|"+d.transform.position)))throw new Exception("Door route changed");
                AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
        SceneManager.SetActiveScene(previous);var presentation=All<Recoil3DWorld>(previous).FirstOrDefault();if(presentation)presentation.ApplyAtmosphere();File.WriteAllText("output/warehouse-skylights/verification.txt",report.ToString());return report.ToString();
    }
}
