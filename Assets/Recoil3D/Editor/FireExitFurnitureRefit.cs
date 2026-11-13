using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static class FireExitFurnitureRefit
{
    const string Target="Assets/Scenes/3D/RecoilPrototype.unity",Source="Assets/Scenes/3D/FactoryRooms/FactoryManagerOfficeF4.unity";
    const string Marker="Doorway pile - reused indoor furniture";
    static T[] All<T>(Scene s)where T:Component{return s.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<T>(true)).ToArray();}
    static Bounds Bounds(Transform t){var rs=t.GetComponentsInChildren<MeshRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();if(rs.Length==0)throw new Exception("No visible geometry: "+t.name);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}
    static Transform Place(Transform source,Transform parent,string name,float x,float y,float back,Vector3 rotation)
    {
        var t=Object.Instantiate(source.gameObject,parent).transform;t.name=name;t.localScale=Vector3.one;t.rotation=Quaternion.Euler(rotation);t.position=Vector3.zero;
        foreach(var c in t.Cast<Transform>().Where(c=>c.name.StartsWith("Archive carton")).ToArray())Object.DestroyImmediate(c.gameObject);
        var b=Bounds(t);t.position+=new Vector3(x-b.center.x,y-b.min.y,back-b.max.z);return t;
    }
    [MenuItem("Recoil/Environment/Refit Blocked Fire Exit Furniture")]
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit mode required");
        var original=SceneManager.GetActiveScene();var s=SceneManager.GetSceneByPath(Target);bool opened=!s.isLoaded;if(opened)s=EditorSceneManager.OpenScene(Target,OpenSceneMode.Additive);
        var src=SceneManager.GetSceneByPath(Source);bool sourceOpened=!src.isLoaded;if(sourceOpened)src=EditorSceneManager.OpenScene(Source,OpenSceneMode.Additive);
        try{
            if(s.isDirty)throw new Exception("Target scene has unsaved edits");
            Directory.CreateDirectory("Backups/FireExitFurniture");string backup="Backups/FireExitFurniture/"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";if(!EditorSceneManager.SaveScene(s,backup,true))throw new Exception("Backup failed");
            var physics=All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled).ToArray();var doors=All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d)).ToArray();
            var pile=All<Transform>(s).Single(t=>t.name=="Floor 2 entrance - impassable piled freight");
            foreach(var t in pile.Cast<Transform>().Where(t=>t.name!=Marker).ToArray()){if(t.GetComponentsInChildren<Collider2D>(true).Length+t.GetComponentsInChildren<Collider>(true).Length>0)throw new Exception("Unexpected furniture collision");t.gameObject.SetActive(false);}
            var existing=pile.Find(Marker);if(existing)Object.DestroyImmediate(existing.gameObject);
            var root=new GameObject(Marker).transform;root.SetParent(pile,false);
            var cabinet=All<Transform>(src).First(t=>t.name=="Steel filing cabinet - four drawers");var chair=All<Transform>(src).Single(t=>t.name=="Visitor tubular chair");
            Place(cabinet,root,"Indoor filing cabinet - braced against door",74.20f,7.60f,2.80f,new Vector3(0,-4,-6));
            var leaning=Place(cabinet,root,"Indoor filing cabinet - leaning across entrance",73.40f,7.60f,2.78f,new Vector3(0,5,18));
            Place(chair,root,"Indoor visitor chair - feet on landing",72.62f,7.60f,2.73f,new Vector3(0,-12,0));
            Place(chair,root,"Indoor visitor chair - tipped into pile",73.01f,7.60f,2.18f,new Vector3(12,8,-24));
            Place(chair,root,"Indoor visitor chair - supported on cabinet",73.67f,Bounds(leaning).max.y-.035f,2.77f,new Vector3(73,-14,15));
            // The old sprite-depth landing ended at Z1.5 while the rebuilt doorway is at Z2.85.
            var landing=All<Transform>(s).Single(t=>t.name=="Fire exit landing"&&t.parent.name=="Floor 2 exit and landing");var deck=landing.GetComponentsInChildren<MeshRenderer>().Single(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.bounds.size.x>4&&r.bounds.size.y<.3f);
            var size=deck.bounds.size;var scale=deck.transform.localScale;scale.z*=2.15f/size.z;deck.transform.localScale=scale;var at=deck.transform.position;at.z=1.775f;deck.transform.position=at;
            Physics2D.SyncTransforms();
            if(!physics.SequenceEqual(All<Collider2D>(s).Select(c=>c.GetInstanceID()+"|"+c.bounds+"|"+c.enabled))||!doors.SequenceEqual(All<RecoilDoor>(s).Select(d=>EditorJsonUtility.ToJson(d))))throw new Exception("Gameplay collision or door routes changed");
            if(root.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Length!=0||root.GetComponentsInChildren<Collider>(true).Length!=0)throw new Exception("Replacement is not visual-only native geometry");
            EditorSceneManager.MarkSceneDirty(s);if(!EditorSceneManager.SaveScene(s))throw new Exception("Scene save failed");
            Directory.CreateDirectory("Captures/FireExitFurniture");Capture(s);
            var result="PASS reused office filing cabinets x2 and visitor chairs x3; backs at Z2.18–2.80 by door Z2.85; grounded bases at Y7.60, upper chair supported on cabinet; landing extended to doorway; original collision and door routes unchanged. Backup: "+backup;
            File.WriteAllText("Captures/FireExitFurniture/Verification.txt",result);return result;
        }finally{if(sourceOpened)EditorSceneManager.CloseScene(src,true);if(opened)EditorSceneManager.CloseScene(s,true);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);}
    }
    static void Capture(Scene s)
    {
        var go=new GameObject("Temporary fire exit inspection camera");SceneManager.MoveGameObjectToScene(go,s);var cam=go.AddComponent<Camera>();cam.scene=s;cam.orthographic=true;cam.orthographicSize=2.35f;cam.aspect=1.4f;cam.transform.position=new Vector3(69.8f,11.1f,-9);cam.transform.LookAt(new Vector3(73.65f,9.05f,2));cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.06f,.07f,.075f);
        var rt=new RenderTexture(1400,1000,24);var old=RenderTexture.active;Texture2D tex=null;
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex=new Texture2D(1400,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1400,1000),0,0);tex.Apply();File.WriteAllBytes("Captures/FireExitFurniture/Refit.png",tex.EncodeToPNG());}
        finally{RenderTexture.active=old;cam.targetTexture=null;if(tex)Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(go);}
    }
}
