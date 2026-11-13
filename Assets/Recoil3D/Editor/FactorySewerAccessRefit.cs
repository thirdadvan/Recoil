using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FactorySewerAccessRefit
{
    const string Folder="Assets/Recoil3D/SewerAccessRefit";
    const string Marker="Round industrial sewer access - editable";
    static Material concrete,iron,rust,lining,black;
    struct Vertex
    {
        public Vector3 p,n;public Vector2 uv;
        public static Vertex Mix(Vertex a,Vertex b,float t){return new Vertex{p=Vector3.Lerp(a.p,b.p,t),n=Vector3.Lerp(a.n,b.n,t).normalized,uv=Vector2.Lerp(a.uv,b.uv,t)};}
    }
    static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Material Mat(string name,Color color,float metal=0)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Recoil/3D/Weathered Surface"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metal);m.SetFloat("_Glossiness",.12f);m.SetFloat("_Weathering",.8f);m.SetFloat("_HazeDensity",0);EditorUtility.SetDirty(m);return m;
    }
    static Transform G(Transform p,string name,Vector3 at){var t=new GameObject(name).transform;t.SetParent(p,false);t.localPosition=at;return t;}
    static Transform B(Transform p,string name,Vector3 at,Vector3 size,Material m,PrimitiveType kind=PrimitiveType.Cube)
    {var t=GameObject.CreatePrimitive(kind).transform;Object.DestroyImmediate(t.GetComponent<Collider>());t.name=name;t.SetParent(p,false);t.localPosition=at;t.localScale=size;t.GetComponent<Renderer>().sharedMaterial=m;return t;}
    static Transform MeshObject(Transform parent,string name,List<Vector3> vertices,List<int> triangles,Material material)
    {
        var mesh=new Mesh();mesh.name=name;mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x,v.z)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+name+".asset"));var t=G(parent,name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;
    }
    static void Ring(Transform parent,string name,float outer,float inner,float top,float bottom,Material material)
    {
        var v=new List<Vector3>();var tri=new List<int>();const int n=64;
        // Closed radial cross-section. Separate face vertices retain the cast edges.
        var profile=new[]{new Vector2(inner,top),new Vector2(outer,top),new Vector2(outer,bottom),new Vector2(inner,bottom)};
        for(int face=0;face<4;face++)for(int i=0;i<n;i++)
        {
            float a=i*Mathf.PI*2/n,b=(i+1)*Mathf.PI*2/n;var p=profile[face];var q=profile[(face+1)%4];int at=v.Count;
            v.Add(new Vector3(Mathf.Cos(a)*p.x,p.y,Mathf.Sin(a)*p.x));v.Add(new Vector3(Mathf.Cos(b)*p.x,p.y,Mathf.Sin(b)*p.x));v.Add(new Vector3(Mathf.Cos(a)*q.x,q.y,Mathf.Sin(a)*q.x));v.Add(new Vector3(Mathf.Cos(b)*q.x,q.y,Mathf.Sin(b)*q.x));
            tri.AddRange(new[]{at,at+1,at+2,at+1,at+3,at+2});
        }
        MeshObject(parent,name,v,tri,material);
    }
    static void Rod(Transform p,string name,Vector3 a,Vector3 b,float r,Material material)
    {var t=B(p,name,(a+b)/2,new Vector3(r*2,(a-b).magnitude/2,r*2),material,PrimitiveType.Cylinder);t.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Build(Transform drain,int index)
    {
        foreach(var renderer in drain.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        foreach(var proxy in drain.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){proxy.suppressed=true;EditorUtility.SetDirty(proxy);}
        var root=G(drain,Marker,new Vector3(0,-drain.position.y,0));
        Ring(root,"Weathered circular concrete collar "+index,1.02f,.79f,.065f,-.08f,concrete);
        Ring(root,"Cast iron recessed seat and frame "+index,.81f,.60f,.13f,-.025f,iron);
        Ring(root,"Inner concrete access shaft "+index,.601f,.566f,.10f,-1.6f,lining);
        B(root,"Unlit shaft depth",new Vector3(0,-1.59f,0),new Vector3(1.13f,.01f,1.13f),black,PrimitiveType.Cylinder);
        foreach(float y in new[]{-.4f,-.85f,-1.3f})Ring(root,"Concrete lining joint "+index,.569f,.562f,y+.012f,y-.012f,black);
        for(int i=0;i<8;i++){float a=(i+.5f)*Mathf.PI/4;B(root,"Recessed frame anchor bolt",new Vector3(Mathf.Cos(a)*.744f,.141f,Mathf.Sin(a)*.744f),new Vector3(.065f,.026f,.065f),rust,PrimitiveType.Cylinder);}
        foreach(float x in new[]{-.19f,.19f})Rod(root,"Fixed shaft ladder rail",new Vector3(x,-1.45f,.41f),new Vector3(x,.43f,.41f),.022f,rust);
        for(int i=0;i<7;i++)Rod(root,"Shaft ladder rung",new Vector3(-.20f,-1.35f+i*.25f,.405f),new Vector3(.20f,-1.35f+i*.25f,.405f),.023f,iron);
        foreach(float x in new[]{-.3f,.3f})
        {B(root,"Hinge mounting lug",new Vector3(x,.17f,.76f),new Vector3(.13f,.12f,.22f),iron);Rod(root,"Heavy cover hinge pin",new Vector3(x-.09f,.20f,.74f),new Vector3(x+.09f,.20f,.74f),.045f,rust);}
        var lid=G(root,"Raised heavy circular inspection cover",new Vector3(0,.20f,.74f));
        var disc=G(lid,"Cast cover plate",new Vector3(0,0,-.74f));
        B(disc,"Thick circular iron cover",Vector3.zero,new Vector3(1.39f,.034f,1.39f),iron,PrimitiveType.Cylinder);
        Ring(disc,"Cast cover perimeter bead "+index,.70f,.66f,.052f,-.052f,rust);
        Ring(disc,"Underside reinforcing ring "+index,.50f,.467f,-.034f,-.065f,iron);
        for(int i=0;i<10;i++){float a=i*Mathf.PI/5;Rod(disc,"Cast radial underside reinforcing rib",new Vector3(Mathf.Cos(a)*.12f,-.051f,Mathf.Sin(a)*.12f),new Vector3(Mathf.Cos(a)*.63f,-.051f,Mathf.Sin(a)*.63f),.018f,iron);}
        for(int row=-4;row<=4;row++)for(int col=-4;col<=4;col++)
        {float x=col*.13f,z=row*.13f;if(x*x+z*z>.35f)continue;var grip=B(disc,"Raised anti-slip cast lozenge",new Vector3(x,.043f,z),new Vector3(.065f,.017f,.022f),iron);grip.localRotation=Quaternion.Euler(0,(row+col)%2==0?35:-35,0);}
        foreach(float x in new[]{-.30f,.30f})B(disc,"Recessed lifting key socket",new Vector3(x,.049f,0),new Vector3(.15f,.008f,.045f),black);
        lid.localRotation=Quaternion.Euler(index==0?72:64,0,index==0?-2:2);
        Rod(root,"Simple cover stay",new Vector3(-.43f,.17f,.73f),new Vector3(-.43f,.77f,.29f),.014f,rust);
        // Edge chips and rain-darkened collar joints; no floating world-space DRAIN label.
        for(int i=0;i<5;i++){float a=i*1.37f;var chip=B(root,"Concrete collar chipped edge",new Vector3(Mathf.Cos(a)*.94f,.052f,Mathf.Sin(a)*.94f),new Vector3(.13f,.024f,.055f),lining);chip.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);}
        Batch(root);
    }
    static void Batch(Transform root)
    {
        var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).ToArray();
        foreach(var material in renderers.Select(r=>r.sharedMaterial).Distinct())
        {
            var selected=renderers.Where(r=>r.sharedMaterial==material).ToArray();var mesh=new Mesh();mesh.name="Sewer access - "+material.name;mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(selected.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+mesh.name+".asset"));var t=G(root,"Combined access geometry - "+material.name,Vector3.zero);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
            foreach(var r in selected)r.enabled=false;
        }
    }
    static float Side(Vertex v,Vector2 a,Vector2 b){return (b.x-a.x)*(v.p.z-a.y)-(b.y-a.y)*(v.p.x-a.x);}
    static List<Vertex> Clip(List<Vertex> polygon,Vector2 a,Vector2 b,bool keepInside)
    {
        var result=new List<Vertex>();if(polygon.Count==0)return result;
        Vertex last=polygon[polygon.Count-1];float before=Side(last,a,b);bool lastIn=keepInside?before>=0:before<=0;
        foreach(var next in polygon){float after=Side(next,a,b);bool nextIn=keepInside?after>=0:after<=0;if(lastIn!=nextIn)result.Add(Vertex.Mix(last,next,before/(before-after)));if(nextIn)result.Add(next);last=next;before=after;lastIn=nextIn;}return result;
    }
    static int CutGround(Scene scene,Vector3[] holes)
    {
        int changed=0;
        foreach(var renderer in All<MeshRenderer>(scene).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.min.y<.15f&&r.bounds.max.y<.45f).ToArray())
        {
            bool ground=renderer.name=="3D mesh - Solid terrain"||renderer.name=="3D continuous foreground floor"||renderer.name.Contains("Damp earth")||renderer.name.Contains("Road worn asphalt")||renderer.name=="Road bends to existing estate approach"||renderer.name=="Worn foreground vehicle road"||renderer.name=="Gravel shoulders"||renderer.name=="Curved public road enters factory rear gate"||renderer.name=="Worn gravel road shoulders";
            if(!ground)continue;var filter=renderer.GetComponent<MeshFilter>();if(!filter||!filter.sharedMesh)continue;
            var affected=holes.Where(h=>h.x+.60f>renderer.bounds.min.x&&h.x-.60f<renderer.bounds.max.x&&h.z+.60f>renderer.bounds.min.z&&h.z-.60f<renderer.bounds.max.z).ToArray();if(affected.Length==0)continue;
            var source=filter.sharedMesh;var positions=source.vertices;var normals=source.normals;var uvs=source.uv;var output=new List<Vertex>();var submeshes=new List<int[]>();
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=new List<int>();var triangles=source.GetTriangles(sub);
                for(int i=0;i<triangles.Length;i+=3)
                {
                    var initial=new List<Vertex>();for(int j=0;j<3;j++){int k=triangles[i+j];initial.Add(new Vertex{p=renderer.transform.TransformPoint(positions[k]),n=normals.Length>k?normals[k]:Vector3.up,uv=uvs.Length>k?uvs[k]:Vector2.zero});}
                    var pieces=new List<List<Vertex>>{initial};
                    foreach(var hole in affected)
                    {
                        var nextPieces=new List<List<Vertex>>();
                        foreach(var piece in pieces)
                        {
                            if(piece.Max(v=>v.p.y)<-1.65f||piece.Min(v=>v.p.y)>.16f||piece.Max(v=>v.p.x)<hole.x-.60f||piece.Min(v=>v.p.x)>hole.x+.60f||piece.Max(v=>v.p.z)<hole.z-.60f||piece.Min(v=>v.p.z)>hole.z+.60f){nextPieces.Add(piece);continue;}
                            var inside=piece;
                            for(int edge=0;edge<32&&inside.Count>=3;edge++)
                            {float a=edge*Mathf.PI*2/32,b=(edge+1)*Mathf.PI*2/32;var p=new Vector2(hole.x+Mathf.Cos(a)*.595f,hole.z+Mathf.Sin(a)*.595f);var q=new Vector2(hole.x+Mathf.Cos(b)*.595f,hole.z+Mathf.Sin(b)*.595f);var outside=Clip(inside,p,q,false);if(outside.Count>=3)nextPieces.Add(outside);inside=Clip(inside,p,q,true);}
                        }
                        pieces=nextPieces;
                    }
                    foreach(var polygon in pieces){int start=output.Count;output.AddRange(polygon);for(int j=1;j<polygon.Count-1;j++)indices.AddRange(new[]{start,start+j,start+j+1});}
                }
                submeshes.Add(indices.ToArray());
            }
            // Separate overlapping legacy earth/asphalt layers to avoid coplanar radial artifacts.
            float offset=renderer.name.Contains("Damp earth")?-.025f:renderer.name.Contains("Road worn asphalt")?.04f:renderer.name=="Road bends to existing estate approach"?.035f:0;
            var mesh=new Mesh();mesh.name=source.name+" with open sewer throats";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.SetVertices(output.Select(v=>renderer.transform.InverseTransformPoint(v.p+Vector3.up*offset)).ToList());mesh.SetNormals(output.Select(v=>v.n).ToList());mesh.SetUVs(0,output.Select(v=>v.uv).ToList());mesh.subMeshCount=submeshes.Count;
            for(int i=0;i<submeshes.Count;i++){var triangles=submeshes[i];if(renderer.name=="Road bends to existing estate approach")for(int j=0;j<triangles.Length;j+=3){int swap=triangles[j+1];triangles[j+1]=triangles[j+2];triangles[j+2]=swap;}mesh.SetTriangles(triangles,i);}if(renderer.name=="Road bends to existing estate approach")mesh.RecalculateNormals();mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/Ground with sewer openings.asset"));filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);changed++;
        }
        return changed;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");if(!scene.isLoaded)throw new Exception("Open outdoor scene first");
        var drains=All<RecoilDoor>(scene).Where(d=>d.name=="Drain access").OrderBy(d=>d.transform.position.x).ToArray();if(drains.Length!=2)throw new Exception("Expected two sewer access points");if(drains.Any(d=>d.transform.Find(Marker)))throw new Exception("Refit already applied");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("Backups/SewerAccessRefit");Directory.CreateDirectory("output/sewer-access-refit");string backup="Backups/SewerAccessRefit/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";EditorSceneManager.SaveScene(scene,backup,true);AssetDatabase.Refresh();
        var all=All<Transform>(scene);var gate=all.Single(t=>t.name=="Factory right perimeter wall");var eastSpawn=all.Single(t=>t.name=="Sewer East Return Spawn");var westSpawn=all.Single(t=>t.name=="Sewer West Return Spawn");Vector3 westBefore=westSpawn.position;var routes=drains.Select(d=>d.destinationScene+"|"+d.destinationSpawn).ToArray();
        var before=All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.enabled+"|"+c.bounds).ToArray();
        drains[1].transform.position=new Vector3(gate.position.x+6,drains[1].transform.position.y,0);eastSpawn.position=new Vector3(drains[1].transform.position.x+1.6f,.845f,0);
        concrete=Mat("Chipped access collar concrete",new Color(.32f,.34f,.30f));iron=Mat("Weathered cast iron inspection cover",new Color(.16f,.185f,.17f),.5f);rust=Mat("Rusted hinge and ladder iron",new Color(.29f,.17f,.09f),.3f);lining=Mat("Damp concrete shaft lining",new Color(.16f,.20f,.17f));black=Mat("Unlit shaft and recessed sockets",new Color(.012f,.018f,.016f));
        for(int i=0;i<drains.Length;i++)Build(drains[i].transform,i);
        int surfaces=CutGround(scene,drains.Select(d=>new Vector3(d.transform.position.x,0,0)).ToArray());
        if(westSpawn.position!=westBefore||!routes.SequenceEqual(drains.Select(d=>d.destinationScene+"|"+d.destinationSpawn)))throw new Exception("Unexpected route change");
        Physics2D.SyncTransforms();if(!before.SequenceEqual(All<Collider2D>(scene).Select(c=>c.GetInstanceID()+"|"+c.enabled+"|"+c.bounds)))throw new Exception("Gameplay collision changed");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        string report="Two open circular inspection manholes rebuilt with thick hinged cast covers, recessed frames, concrete collars, 1.6m shaft liners and fixed ladders. New copied ground meshes cut around both openings: "+surfaces+"; original shared meshes retained. West drain="+drains[0].transform.position+" / spawn="+westSpawn.position+" unchanged. East drain="+drains[1].transform.position+" / east return spawn="+eastSpawn.position+"; gate="+gate.position.x+". Routes unchanged; original gameplay collisions unchanged. Backup="+backup;File.WriteAllText("output/sewer-access-refit/verification.txt",report);return report;
    }
    public static string ValidateRoutes()
    {
        var outside=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");var all=All<Transform>(outside);var gate=all.Single(t=>t.name=="Factory right perimeter wall");var drains=All<RecoilDoor>(outside).Where(d=>d.name=="Drain access").OrderBy(d=>d.transform.position.x).ToArray();
        var east=all.Single(t=>t.name=="Sewer East Return Spawn");if(drains[1].transform.position.x<gate.position.x+3||east.position.x<gate.position.x+3)throw new Exception("East sewer route is not outside gate");
        var sewer=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilFactorySewer.unity");bool opened=!sewer.isLoaded;if(opened)sewer=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilFactorySewer.unity",OpenSceneMode.Additive);
        try
        {
            foreach(var drain in drains){if(!All<Transform>(sewer).Any(t=>t.name==drain.destinationSpawn))throw new Exception("Missing sewer arrival marker");if(drain.GetComponentsInChildren<Collider>(true).Length+drain.GetComponentsInChildren<Collider2D>(true).Length>0)throw new Exception("New entry geometry blocks gameplay");}
            foreach(var exit in All<RecoilDoor>(sewer).Where(d=>d.destinationSpawn=="Sewer West Return Spawn"||d.destinationSpawn=="Sewer East Return Spawn"))
            {var spawn=all.Single(t=>t.name==exit.destinationSpawn);if(exit.destinationScene!=outside.path)throw new Exception("Wrong outdoor return scene");if(Mathf.Abs(spawn.position.y-.845f)>.01f)throw new Exception("Spawn height wrong");}
            string result="PASS both outdoor->sewer arrival markers and sewer->outdoor named return routes; east exit and return spawn are beyond gate, west return unchanged; no entry prop collision.";File.AppendAllText("output/sewer-access-refit/verification.txt","\n"+result);return result;
        }
        finally{if(opened)EditorSceneManager.CloseScene(sewer,true);SceneManager.SetActiveScene(outside);var w=All<Recoil3DWorld>(outside).FirstOrDefault();if(w)w.ApplyAtmosphere();}
    }
    public static void Capture()
    {
        var s=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilPrototype.unity");var drains=All<RecoilDoor>(s).Where(d=>d.name=="Drain access").OrderBy(d=>d.transform.position.x).ToArray();
        Shot("WestAccess",new Vector3(drains[0].transform.position.x-2.8f,2.9f,-3.7f),new Vector3(drains[0].transform.position.x,0,0),true,45);
        Shot("EastAccess",new Vector3(drains[1].transform.position.x+3.0f,3.0f,-3.6f),new Vector3(drains[1].transform.position.x,.1f,0),true,45);
        Shot("EastOutsideGate",new Vector3(169,11,-19),new Vector3(151,2,-2),true,58);
        Shot("EastOutsideGateNight",new Vector3(169,9,-19),new Vector3(152,2,-2),false,58);
    }
    static void Shot(string name,Vector3 p,Vector3 target,bool inspection,float fov)
    {
        Directory.CreateDirectory("Captures/SewerAccessRefit");var go=new GameObject("Temporary sewer inspection camera");GameObject lg=null;
        try
        {
            var cam=go.AddComponent<Camera>();cam.CopyFrom(Camera.main);cam.enabled=false;cam.fieldOfView=fov;cam.transform.position=p;cam.transform.LookAt(target);
            if(inspection){lg=new GameObject("Temporary inspection lighting");var light=lg.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.4f;light.color=new Color(1,.94f,.85f);light.shadows=LightShadows.None;light.transform.rotation=Quaternion.Euler(48,-25,0);}
            var rt=RenderTexture.GetTemporary(1400,1000,24,RenderTextureFormat.ARGBHalf);var previous=RenderTexture.active;Texture2D raw=null,image=null;
            try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;raw=new Texture2D(1400,1000,TextureFormat.RGBAFloat,false,true);raw.ReadPixels(new Rect(0,0,1400,1000),0,0);raw.Apply();var pixels=raw.GetPixels();if(QualitySettings.activeColorSpace==ColorSpace.Linear)for(int i=0;i<pixels.Length;i++)pixels[i]=pixels[i].gamma;image=new Texture2D(1400,1000,TextureFormat.RGB24,false,true);image.SetPixels(pixels);image.Apply();File.WriteAllBytes("Captures/SewerAccessRefit/"+name+".png",image.EncodeToPNG());}
            finally{cam.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(raw)Object.DestroyImmediate(raw);if(image)Object.DestroyImmediate(image);}
        }
        finally{Object.DestroyImmediate(go);if(lg)Object.DestroyImmediate(lg);}
    }
}
