using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

// An idempotent detail pass: all additions remain inside the existing movable clock model.
public static class ArchiveClockReliefPass
{
    const string Folder="Assets/Art/Environment/ArchiveRoom/";
    const string DetailName="Clock relief - bezel cabinet and crown";
    static Transform G(string name,Transform parent,Vector3 position){var t=new GameObject(name).transform;t.SetParent(parent,false);t.localPosition=position;return t;}
    static Material Mat(string name,Color color,float metallic=0,float smooth=.2f)
    {
        string path=Folder+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(m,path);}
        m.color=color;m.SetFloat("_Metallic",metallic);m.SetFloat("_Glossiness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static Transform MeshPart(string name,Transform parent,Vector3 position,Mesh mesh,Material material)
    {var t=G(name,parent,position);t.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;t.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;return t;}
    static Transform Box(string name,Transform parent,Vector3 position,Vector3 size,Material material)
    {var t=MeshPart(name,parent,position,Resources.GetBuiltinResource<Mesh>("Cube.fbx"),material);t.localScale=size;return t;}
    // A closed bevel profile revolved around Z gives actual highlights and recessed inner walls.
    static Mesh Ring(string name,Vector2[] profile)
    {
        string path=Folder+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old)return old;
        const int segments=64;var v=new List<Vector3>();var triangles=new List<int>();
        for(int j=0;j<profile.Length;j++)for(int i=0;i<=segments;i++){
            float a=i*Mathf.PI*2/segments;v.Add(new Vector3(Mathf.Sin(a)*profile[j].x,Mathf.Cos(a)*profile[j].x,profile[j].y));
        }
        for(int j=0;j<profile.Length;j++)for(int i=0;i<segments;i++){
            int a=j*(segments+1)+i,b=((j+1)%profile.Length)*(segments+1)+i;
            triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
        }
        var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh MinuteMarks()
    {
        const string path=Folder+"Clock raised minute track.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old)return old;
        var pieces=new List<CombineInstance>();var cube=Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        for(int i=0;i<60;i++){if(i%5==0)continue;float a=i*Mathf.PI/30;
            pieces.Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(new Vector3(Mathf.Sin(a)*.178f,Mathf.Cos(a)*.178f,0),Quaternion.Euler(0,0,-i*6),new Vector3(.002f,.007f,.003f))});}
        var mesh=new Mesh{name="Clock raised minute track - shared mesh"};mesh.CombineMeshes(pieces.ToArray(),true,true);AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before applying clock detail.");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(!scene.path.EndsWith("/FactoryArchiveF3.unity")||UnityEngine.SceneManagement.SceneManager.sceneCount!=1)throw new Exception("Use the single archive scene.");
        var clock=Object.FindObjectOfType<WindUpArchiveClock>();if(!clock)throw new Exception("Clock missing.");
        var model=clock.transform.parent.Find("Clock model - cabinet hands and pendulum");if(!model)throw new Exception("Clock model missing.");
        var hour=clock.hourHand;var minute=clock.minuteHand;var pendulum=clock.pendulum;var sound=clock.soundOrigin;var audio=clock.chime;var pose=model.localToWorldMatrix;
        Undo.RegisterFullObjectHierarchyUndo(model.gameObject,"Add clock dial relief and crown detail");
        var previous=model.Find(DetailName);if(previous)Undo.DestroyObjectImmediate(previous.gameObject);
        var root=G(DetailName,model,Vector3.zero);
        var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Faded walnut veneer.mat");
        var edge=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Environment/ManagerOffice/Dark wood edges.mat");
        var brass=Mat("Clock bevelled aged brass",new Color(.38f,.31f,.19f),.55f,.32f);
        var shadow=Mat("Clock recessed dark trim",new Color(.055f,.043f,.027f));
        var ink=Mat("Clock raised charcoal indices",new Color(.035f,.032f,.023f),.15f,.27f);
        var bezel=Ring("Clock sculpted bezel profile",new[]{new Vector2(.190f,-.193f),new Vector2(.190f,-.205f),new Vector2(.195f,-.216f),new Vector2(.207f,-.216f),new Vector2(.218f,-.202f),new Vector2(.218f,-.177f)});
        MeshPart("Stepped brass bezel - recessed dial",root,new Vector3(0,1.75f,0),bezel,brass);
        var inner=Ring("Clock fine inner chapter ring",new[]{new Vector2(.154f,-.193f),new Vector2(.154f,-.195f),new Vector2(.156f,-.195f),new Vector2(.156f,-.193f)});
        MeshPart("Fine inset chapter ring",root,new Vector3(0,1.75f,0),inner,brass);
        MeshPart("Raised minute indices - one mesh",root,new Vector3(0,1.75f,-.196f),MinuteMarks(),ink);
        foreach(var tr in model.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Bold hour marker"))){tr.localPosition=new Vector3(tr.localPosition.x,tr.localPosition.y,-.199f);tr.localScale=new Vector3(tr.localScale.x,tr.localScale.y,.009f);tr.GetComponent<MeshRenderer>().sharedMaterial=ink;}
        foreach(var pivot in new[]{hour,minute})foreach(var renderer in pivot.GetComponentsInChildren<MeshRenderer>()){renderer.sharedMaterial=ink;var scale=renderer.transform.localScale;scale.z=.007f;renderer.transform.localScale=scale;}
        // Stepped rails surround a visibly recessed upper dial panel.
        foreach(float x in new[]{-.254f,.254f}){
            Box("Hood outer stile",root,new Vector3(x,1.758f,-.174f),new Vector3(.044f,.612f,.068f),wood);
            Box("Hood inner moulding",root,new Vector3(x*.883f,1.758f,-.198f),new Vector3(.013f,.538f,.024f),edge);
            Box("Stile inset flute",root,new Vector3(x,1.758f,-.211f),new Vector3(.009f,.438f,.005f),shadow);
            foreach(float y in new[]{1.478f,2.038f})Box("Hood column cap",root,new Vector3(x,y,-.186f),new Vector3(.063f,.030f,.080f),edge);
        }
        foreach(float y in new[]{1.458f,2.058f}){
            Box("Hood outer cross moulding",root,new Vector3(0,y,-.181f),new Vector3(.565f,.032f,.073f),wood);
            Box("Hood inner cross moulding",root,new Vector3(0,y+(y<1.7f?.022f:-.022f),-.201f),new Vector3(.464f,.012f,.024f),edge);
        }
        Box("Cornice lower shadow reveal",root,new Vector3(0,2.078f,-.010f),new Vector3(.620f,.018f,.371f),shadow);
        Box("Cornice projecting lower lip",root,new Vector3(0,2.092f,-.015f),new Vector3(.666f,.017f,.410f),wood);
        Box("Cornice upper bead",root,new Vector3(0,2.169f,-.009f),new Vector3(.712f,.014f,.418f),wood);
        Box("Crown raised top cap",root,new Vector3(0,2.217f,.003f),new Vector3(.646f,.024f,.380f),edge);
        Box("Crown top highlight lip",root,new Vector3(0,2.234f,.003f),new Vector3(.666f,.010f,.395f),wood);
        for(int i=0;i<9;i++)Box("Cornice dentil "+(i+1).ToString("00"),root,new Vector3((i-4)*.065f,2.077f,-.191f),new Vector3(.029f,.035f,.037f),edge);
        // Door-edge beads give the lower chamber the same solid joinery as the hood.
        foreach(float x in new[]{-.211f,.211f})Box("Pendulum door inset bead",root,new Vector3(x,.83f,-.177f),new Vector3(.010f,1.12f,.014f),wood);
        foreach(float y in new[]{.262f,1.393f})Box("Pendulum door cross bead",root,new Vector3(0,y,-.179f),new Vector3(.430f,.012f,.014f),edge);
        Box("Plinth recessed shadow band",root,new Vector3(0,.147f,-.003f),new Vector3(.674f,.010f,.394f),shadow);
        Box("Plinth stepped foot lip",root,new Vector3(0,.022f,-.006f),new Vector3(.714f,.025f,.431f),wood);
        if(clock.hourHand!=hour||clock.minuteHand!=minute||clock.pendulum!=pendulum||clock.soundOrigin!=sound||clock.chime!=audio||model.localToWorldMatrix!=pose)throw new Exception("Clock bindings or placement changed.");
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();if(!EditorSceneManager.SaveScene(scene))throw new Exception("Scene save failed.");
        Selection.activeGameObject=clock.transform.parent.gameObject;
        return "Saved clock relief: bevelled brass bezel, recessed dial, raised hour and minute markers, hood mouldings, fluted stiles, layered cornice/dentils and stepped plinth. Original hands, pendulum, paper, interaction and audio preserved.";
    }
}
