using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class ServiceStoreRearSpacePass
{
    const string Marker="Store rear space shortened and stocked";
    static float Compress(float z){return z>9.6f?9.6f+(z-9.6f)*.5f:z;}
    [MenuItem("Recoil/Art Tests/Shorten And Stock Store Rear Space")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/3D/RecoilServiceStore.unity");if(!scene.isLoaded)scene=EditorSceneManager.OpenScene("Assets/Scenes/3D/RecoilServiceStore.unity");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var world=all.Select(t=>t.GetComponent<RecoilPrototype>()).Single(w=>w);
        if(world.transform.Find(Marker)){Debug.Log("STORE_REAR_SPACE already updated");return;}
        string backup="Backups/ServiceStoreRearSpace/"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);EditorSceneManager.SaveScene(scene,backup+"/RecoilServiceStore.unity",true);
        var doors=world.GetComponentsInChildren<RecoilDoor>(true);var doorPositions=doors.Select(d=>d.transform.position).ToArray();int collisions=world.GetComponentsInChildren<Collider2D>(true).Length;
        var rear=all.Single(t=>t.name=="Distant architecture Z 15 to 24");
        foreach(Transform t in rear)
        {
            // Compress only the unused rear span. Rigid furniture stays full-sized.
            var renderer=t.GetComponent<Renderer>();
            if(renderer&&t.name!="High wire glass window"&&t.name!="Window mullion"&&t.name!="Damp patch")
            {
                var b=renderer.bounds;float min=Compress(b.min.z),max=Compress(b.max.z);var p=t.position;p.z=(min+max)*.5f;t.position=p;
                if(b.size.z>.001f){var s=t.localScale;s.z*=(max-min)/b.size.z;t.localScale=s;}
            }
            else {var p=t.position;p.z=Compress(p.z);t.position=p;}
        }
        foreach(var t in all.Where(t=>t.name.StartsWith("Boarded window -")||t.name=="Moisture rising from wall base"||t.name=="Wear from wall base and fittings"))
        {if(t.position.z>9.6f){var p=t.position;p.z=Compress(p.z);t.position=p;}}
        foreach(var t in all.Where(t=>t.name=="Upper floor extends into office depth"))
        {var b=t.GetComponent<Renderer>().bounds;var p=t.position;p.z=(9.6f+15.8f)*.5f;t.position=p;var s=t.localScale;s.z*=6.2f/b.size.z;t.localScale=s;}
        var refit=all.Single(t=>t.name=="Service store spatial refit - fixtures and stock");
        foreach(var name in new[]{"Back wall - pantry and travel goods","Back wall - automotive fluids","Twin-door refrigerated drinks cabinet","Back counter cigarette and sundries cubbies"})
        {var t=refit.Find(name);if(t){var p=t.position;p.z+=6.2f;t.position=p;}}
        var marker=new GameObject(Marker).transform;marker.SetParent(world.transform,false);
        var template=refit.Find("Double-sided snack gondola");
        for(int i=0;i<4;i++)
        {var copy=UnityEngine.Object.Instantiate(template,marker,true);copy.name="Copied stocked gondola - rear row "+(i+1);copy.position=new Vector3(3.2f+i*3.25f,template.position.y,11.65f);}
        var floor=refit.Find("Worn tiled retail floor");var floorCopy=UnityEngine.Object.Instantiate(floor,marker,true);floorCopy.name="Continuation of tiled floor to nearer rear wall";
        var floorBounds=floor.GetComponentsInChildren<Renderer>().Select(r=>r.bounds).Aggregate((a,b)=>{a.Encapsulate(b);return a;});float length=15.8f-floorBounds.max.z;float factor=length/floorBounds.size.z;
        var sc=floorCopy.localScale;sc.z*=factor;floorCopy.localScale=sc;var fp=floorCopy.position;fp.z=floorBounds.max.z-floorBounds.min.z*factor;floorCopy.position=fp;
        foreach(var text in refit.GetComponentsInChildren<TextMesh>(true))text.gameObject.SetActive(false);
        foreach(var text in marker.GetComponentsInChildren<TextMesh>(true))text.gameObject.SetActive(false);
        for(int i=0;i<doors.Length;i++)if(doors[i].transform.position!=doorPositions[i])throw new Exception("Door moved");
        if(world.GetComponentsInChildren<Collider2D>(true).Length!=collisions)throw new Exception("Gameplay colliders changed");
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        var look=all.Select(t=>t.GetComponent<WarehouseAtmosphereTest>()).Single(x=>x);look.ApplyLook();var cam=look.GetComponent<Camera>();var pos=cam.transform.position;var rot=cam.transform.rotation;float fov=cam.fieldOfView;
        var follow=cam.GetComponent<RecoilCameraController>();bool on=follow&&follow.enabled;
        try{if(follow)follow.enabled=false;cam.transform.position=new Vector3(8,3.4f,-13);cam.transform.LookAt(new Vector3(8,2.3f,1.3f));cam.fieldOfView=45;WarehousePathLightingPass.Capture(cam,"StoreRear_Compact_GameView");cam.transform.position=new Vector3(8,13,-2);cam.transform.LookAt(new Vector3(8,0,8));cam.fieldOfView=62;WarehousePathLightingPass.Capture(cam,"StoreRear_Compact_Layout");}
        finally{cam.transform.SetPositionAndRotation(pos,rot);cam.fieldOfView=fov;if(follow)follow.enabled=on;}
        Debug.Log("STORE_REAR_SPACE_PASS: empty rear span 12.4m to 6.2m, back wall Z22 to15.8, four full-size copied stocked racks; category text removed; doors and collisions unchanged. Backup="+backup);
    }
}
