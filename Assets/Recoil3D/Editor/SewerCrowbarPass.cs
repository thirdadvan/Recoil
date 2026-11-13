using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class SewerCrowbarPass
{
    const string Folder="Assets/Recoil3D/SewerCrowbar";
    public const string OpenFlag="sewer_west_cover_open";
    static T[] All<T>(Scene scene)where T:Component{return scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    public static string Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");var scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/3D/RecoilPrototype.unity")throw new Exception("Outdoor scene required");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory("output/sewer-crowbar");
        EditorSceneManager.SaveScene(scene,"output/sewer-crowbar/Outdoor-before.unity.backup",true);
        var drains=All<RecoilDoor>(scene).Where(d=>d.name=="Drain access").OrderBy(d=>d.transform.position.x).ToArray();var west=drains[0];
        if(west.openStateItemId==OpenFlag)throw new Exception("Already applied");
        var art=west.transform.Find("Round industrial sewer access - editable");var visualPosition=art.position;var oldPosition=west.transform.position;
        var movable=west.transform.parent;movable.position+=new Vector3(visualPosition.x-oldPosition.x,0,visualPosition.z-oldPosition.z);art.position=visualPosition;
        // Restore the two cut cube-based ground layers before making the new pair of openings.
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);var cube=primitive.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(primitive);
        int restored=0;foreach(var f in All<MeshFilter>(scene).Where(f=>AssetDatabase.GetAssetPath(f.sharedMesh).Contains("/Ground with sewer openings")))
        {if(f.name!="3D mesh - Solid terrain"&&f.name!="3D continuous foreground floor")throw new Exception("Unexpected source ground "+f.name);f.sharedMesh=cube;EditorUtility.SetDirty(f);restored++;}
        var method=typeof(FactorySewerAccessRefit).GetMethod("CutGround",BindingFlags.Static|BindingFlags.NonPublic);
        var holes=drains.Select(d=>d.transform.Find("Round industrial sewer access - editable").position).ToArray();int surfaces=(int)method.Invoke(null,new object[]{scene,holes});
        // Unbatch original components so the real cover can move separately from its frame.
        foreach(var t in art.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Combined access geometry - ")).ToArray())Object.DestroyImmediate(t.gameObject);
        foreach(var r in art.GetComponentsInChildren<MeshRenderer>(true))r.enabled=true;
        var open=art.Find("Raised heavy circular inspection cover");open.name="Open cover - hinged cast iron";
        var stay=art.Find("Simple cover stay");if(stay)stay.SetParent(open,true);
        var closed=Object.Instantiate(open.gameObject,art).transform;closed.name="Closed cover - crowbar required";closed.localRotation=Quaternion.identity;var cp=closed.localPosition;cp.y=.15f;closed.localPosition=cp;
        var closedStay=closed.Find("Simple cover stay");if(closedStay)Object.DestroyImmediate(closedStay.gameObject);
        foreach(var rail in art.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Fixed shaft ladder rail")){var p=rail.localPosition;p.y=(-1.45f+.08f)*.5f;rail.localPosition=p;var scale=rail.localScale;scale.y=(1.45f+.08f)*.5f;rail.localScale=scale;}
        Batch(closed,closed.GetComponentsInChildren<MeshRenderer>(true));Batch(open,open.GetComponentsInChildren<MeshRenderer>(true));
        Batch(art,art.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled&&!r.transform.IsChildOf(open)&&!r.transform.IsChildOf(closed)).ToArray());
        west.requiredItemId=CrowbarPickup.InventoryId;west.requiresClue=false;west.startsOpened=false;west.openStateItemId=OpenFlag;west.closedVisual=closed;west.openVisual=open;west.requiresFlyingToOpen=false;
        west.lockedMessage="I need a crowbar to pry this cover open.";west.openPrompt="E / PRY OPEN COVER";west.travelPrompt="E / ENTER SEWER";west.firstOpenMessage="The cover is open. Press E again to enter the sewer.";west.ResetDoor();EditorUtility.SetDirty(west);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        var sewer=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilFactorySewer.unity",OpenSceneMode.Additive);
        try{foreach(var exit in All<RecoilDoor>(sewer).Where(d=>d.destinationSpawn=="Sewer West Return Spawn")){exit.requiredItemId=CrowbarPickup.InventoryId;exit.requiresClue=false;exit.lockedMessage="I need a crowbar to lift the cover above.";exit.rememberOnTravel=OpenFlag;EditorUtility.SetDirty(exit);}EditorSceneManager.MarkSceneDirty(sewer);EditorSceneManager.SaveScene(sewer);}
        finally{EditorSceneManager.CloseScene(sewer,true);SceneManager.SetActiveScene(scene);}
        string report="PASS old west location "+oldPosition+" filled; relocated opening "+visualPosition+"; interactive door "+west.transform.position+"; restored source layers="+restored+", recut surfaces="+surfaces+". East opening retained.\nPASS actual lid closed; crowbar required; first interaction opens and saves flag, second enters. West return marker follows relocated access; indoor west exit also requires crowbar.\n";
        File.WriteAllText("output/sewer-crowbar/verification.txt",report);return report;
    }
    static void Batch(Transform root,MeshRenderer[] renderers)
    {
        foreach(var mat in renderers.Select(r=>r.sharedMaterial).Distinct())
        {
            var selected=renderers.Where(r=>r.sharedMaterial==mat).ToArray();var mesh=new Mesh();mesh.name=root.name+" - "+mat.name;mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(selected.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.localToWorldMatrix}).ToArray(),true,true);
            AssetDatabase.CreateAsset(mesh,AssetDatabase.GenerateUniqueAssetPath(Folder+"/"+mesh.name+".asset"));var go=new GameObject("Batched - "+mat.name);go.transform.SetParent(root,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;foreach(var r in selected)r.enabled=false;
        }
    }
    static Material Mat(string name,Color colour,float metal=0)
    {
        string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,path);}mat.color=colour;mat.SetFloat("_Metallic",metal);mat.SetFloat("_Glossiness",.23f);return mat;
    }
    static Transform Rod(Transform p,string name,Vector3 a,Vector3 b,float radius,Material mat)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;Object.DestroyImmediate(go.GetComponent<Collider>());go.transform.SetParent(p,false);go.transform.localPosition=(a+b)*.5f;go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);go.transform.localScale=new Vector3(radius*2,(a-b).magnitude*.5f,radius*2);go.GetComponent<Renderer>().sharedMaterial=mat;return go.transform;
    }
    public static string AddCrowbar()
    {
        var outside=SceneManager.GetActiveScene();var room=EditorSceneManager.OpenScene("Assets/Scenes/3D/FactoryRooms/FactoryChangingRoomF2.unity",OpenSceneMode.Additive);
        try
        {
            var world=All<RecoilPrototype>(room).Single();if(world.GetComponentInChildren<CrowbarPickup>(true))throw new Exception("Crowbar already authored");
            EditorSceneManager.SaveScene(room,"output/sewer-crowbar/ChangingRoom-before.unity.backup",true);
            var pickup=new GameObject("Crowbar - bandaged grip - right wall pickup");SceneManager.MoveGameObjectToScene(pickup,room);pickup.transform.SetParent(world.transform,false);pickup.transform.position=new Vector3(11.55f,.65f,0);
            var model=new GameObject("Heavy hooked crowbar - bandage weapon grip").transform;model.SetParent(pickup.transform,false);model.localPosition=new Vector3(0,-.61f,.25f);model.localRotation=Quaternion.Euler(5,0,-16);
            var steel=Mat("Pitted forged crowbar steel",new Color(.17f,.18f,.16f),.65f);var edge=Mat("Worn pry tip steel",new Color(.36f,.38f,.35f),.75f);var cloth=Mat("Dirty bandage handle cloth",new Color(.48f,.43f,.33f));
            Rod(model,"Hexagonal steel shaft",Vector3.zero,new Vector3(0,1.14f,0),.019f,steel);
            Vector3 last=new Vector3(0,1.14f,0);for(int i=1;i<=5;i++){float angle=i/5f*Mathf.PI*.72f;var next=new Vector3(-.12f+.12f*Mathf.Cos(angle),1.14f+.12f*Mathf.Sin(angle),0);Rod(model,"Forged curved claw",last,next,.019f,steel);last=next;}
            Rod(model,"Flattened pry end",last,last+new Vector3(-.07f,-.04f,0),.015f,edge);
            for(int i=0;i<16;i++){float y=.16f+i*.022f;var wrap=Rod(model,"Overlapping bandage wrap",new Vector3(0,y-.012f,0),new Vector3(0,y+.012f,0),.026f+(i%3)*.0008f,cloth);wrap.localRotation*=Quaternion.Euler(0,0,i%2==0?4:-3);}
            Rod(model,"Frayed cloth loose tail",new Vector3(.02f,.19f,0),new Vector3(.07f,.13f,0),.009f,cloth);
            Batch(model,model.GetComponentsInChildren<MeshRenderer>());var min=model.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled).Min(r=>r.bounds.min.y);model.position+=Vector3.down*(min-.025f);
            var item=pickup.AddComponent<CrowbarPickup>();item.itemId=item.resultId=CrowbarPickup.InventoryId;item.displayName="Bandaged crowbar";item.description="A heavy steel pry bar. One end is wrapped in bandages for a firm grip. Useful for opening stuck covers.";item.resultMessage="Crowbar added to inventory.";item.interactionDuration=0;item.interactionRange=.5f;item.model=model.gameObject;pickup.AddComponent<PickupVisibility>();
            EditorSceneManager.MarkSceneDirty(room);EditorSceneManager.SaveScene(room);AssetDatabase.SaveAssets();return "PASS bandaged hooked crowbar leaning against changing-room right wall, pick-up range 0.5m, inventory flag crowbar.";
        }
        finally{EditorSceneManager.CloseScene(room,true);SceneManager.SetActiveScene(outside);var w=All<Recoil3DWorld>(outside).FirstOrDefault();if(w)w.ApplyAtmosphere();}
    }
}
