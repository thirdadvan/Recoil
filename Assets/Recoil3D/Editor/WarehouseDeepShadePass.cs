using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseDeepShadePass
{
    const string Folder="Assets/Recoil3D/ArtTests/";
    [MenuItem("Recoil/Art Tests/Add Deep Reflector Shades To Warehouse")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){Debug.LogError("Exit Play before placing lamp shades.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetActiveScene();if(scene.path!=WarehouseInsideLightingTestBuilder.Destination)scene=EditorSceneManager.OpenScene(WarehouseInsideLightingTestBuilder.Destination);
        var roots=scene.GetRootGameObjects();var test=roots.SelectMany(g=>g.GetComponentsInChildren<WarehouseAtmosphereTest>(true)).Single();
        var outer=Material("Deep shade - aged charcoal enamel",new Color(.055f,.065f,.058f),new Color(.006f,.008f,.007f),.45f);
        var inner=Material("Deep shade - illuminated ivory lining",new Color(.43f,.48f,.40f),new Color(.12f,.155f,.135f),.18f);
        var metal=Material("Deep shade - suspension steel",new Color(.11f,.12f,.115f),new Color(.004f,.005f,.004f),.6f);
        var mesh=BuildMesh();var shades=new List<Transform>();int count=0;
        foreach(var l in test.decorativeLights.Concat(test.originalLights).Where(l=>l).Distinct().ToArray())
        {
            bool key=l.name.Contains("overhead key"),depth=l.name.Contains("isolated rack silhouette");
            bool landing=l.name.Contains("transition landing")&&l.intensity>0;
            bool rear=l.transform.parent&&l.transform.parent.name=="Rear working lamp";
            if(!key&&!depth&&!landing&&!rear)continue;
            var parent=l.transform.parent;if(parent.name=="Deep enamel pendant - shade owns light"){shades.Add(parent);continue;}
            var housing=new GameObject("Deep enamel pendant - shade owns light").transform;housing.SetParent(parent,false);
            housing.position=l.transform.position+Vector3.up*.035f;housing.rotation=Quaternion.identity;
            var inherited=parent.lossyScale;housing.localScale=new Vector3(1/Mathf.Max(.001f,Mathf.Abs(inherited.x)),1/Mathf.Max(.001f,Mathf.Abs(inherited.y)),1/Mathf.Max(.001f,Mathf.Abs(inherited.z)));
            // Keep lighting position/direction unchanged; the recessed emitter sits behind the lip.
            l.transform.SetParent(housing,true);
            float scale=rear?.75f:1;var bowl=new GameObject("Hollow reflector - dark shell and glowing lining");bowl.transform.SetParent(housing,false);bowl.transform.localScale=Vector3.one*scale;
            bowl.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=bowl.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{outer,inner};renderer.shadowCastingMode=ShadowCastingMode.Off;
            Part(housing,"Cast metal neck",new Vector3(0,.48f*scale,0),new Vector3(.15f,.09f,.15f)*scale,metal);
            // The hanging rod is visible above the hood and ends at the overhead structure.
            float top=Mathf.Max(housing.position.y+.8f,7.4f);float start=.57f*scale;float length=top-housing.position.y-start;
            Part(housing,"Suspension rod to overhead structure",new Vector3(0,start+length*.5f,0),new Vector3(.025f,length*.5f,.025f),metal);
            Part(housing,"Ceiling fixing plate",new Vector3(0,top-housing.position.y,0),new Vector3(.11f,.025f,.11f),metal);
            shades.Add(housing);count++;
        }
        var camera=test.GetComponent<Camera>();var follow=camera.GetComponent<RecoilCameraController>();bool enabled=follow&&follow.enabled;
        var p=camera.transform.position;var q=camera.transform.rotation;float f=camera.fieldOfView;
        try
        {
            if(follow)follow.enabled=false;test.testLook=true;test.ApplyLook();
            camera.transform.SetPositionAndRotation(new Vector3(14,4.4f,-18),Quaternion.Euler(5,0,0));camera.fieldOfView=44;
            var budget=camera.GetComponent<WarehouseShadowBudget>();if(budget)budget.Apply();WarehousePathLightingPass.Capture(camera,"DeepShades_Warehouse");
            if(shades.Count>0)
            {
                var lamp=shades.OrderBy(t=>Mathf.Abs(t.position.x-14)).First();
                camera.transform.position=lamp.position+new Vector3(1.1f,-1.2f,-2.1f);camera.transform.rotation=Quaternion.LookRotation(lamp.position+Vector3.up*.2f-camera.transform.position);camera.fieldOfView=48;
                WarehousePathLightingPass.Capture(camera,"DeepShades_Detail");
            }
        }
        finally{camera.transform.SetPositionAndRotation(p,q);camera.fieldOfView=f;if(follow)follow.enabled=enabled;}
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Debug.Log("WAREHOUSE_DEEP_SHADES_READY: "+count+" new shades; "+shades.Count+" total; existing light transforms and intensities preserved.");
    }
    static Material Material(string name,Color colour,Color emission,float metallic)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=colour;m.SetColor("_EmissionColor",emission);m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",.28f);m.SetFloat("_Weathering",.35f);m.SetFloat("_HazeDensity",0);m.enableInstancing=true;EditorUtility.SetDirty(m);return m;
    }
    static void Part(Transform parent,string name,Vector3 p,Vector3 scale,Material mat)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cylinder);g.name=name;g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=scale;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.GetComponent<Renderer>().sharedMaterial=mat;g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
    }
    static Mesh BuildMesh()
    {
        const string path=Folder+"Deep industrial reflector.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        // Revolved deep bowl: open underside, real inner wall, rolled rim, no luminous bulb/disc.
        float[] y={0,.025f,.065f,.13f,.23f,.32f,.40f,.44f};float[] radius={.40f,.405f,.38f,.34f,.28f,.215f,.135f,.085f};int n=32,rings=y.Length;
        var v=new List<Vector3>();var outside=new List<int>();var inside=new List<int>();
        for(int side=0;side<2;side++)for(int ring=0;ring<rings;ring++)for(int i=0;i<=n;i++)
        {float a=i*Mathf.PI*2/n;float r=radius[ring]-(side==1?.018f:0);v.Add(new Vector3(Mathf.Cos(a)*r,y[ring]+(side==1?.006f:0),Mathf.Sin(a)*r));}
        int offset=rings*(n+1);
        for(int side=0;side<2;side++)for(int r=0;r<rings-1;r++)for(int i=0;i<n;i++)
        {
            int a=side*offset+r*(n+1)+i,b=a+1,c=a+n+1,d=c+1;var tris=side==0?outside:inside;
            if(side==0)tris.AddRange(new[]{a,c,b,b,c,d});else tris.AddRange(new[]{a,b,c,b,d,c});
        }
        for(int i=0;i<n;i++)outside.AddRange(new[]{i,i+1,offset+i,i+1,offset+i+1,offset+i});
        var mesh=new Mesh{name="Deep industrial reflector - hollow double wall"};mesh.SetVertices(v);mesh.subMeshCount=2;mesh.SetTriangles(outside,0);mesh.SetTriangles(inside,1);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
}
