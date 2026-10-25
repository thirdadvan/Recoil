using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class WarehouseCollapsedRackBuilder
{
    const string ScenePath="Assets/Scenes/RecoilFactoryWarehouse.unity";
    const string CollapsedName="Steel rack - collapsed toward camera - editable";
    static Transform Group(string name,Transform parent,Vector2 p){return FactoryWarehouseBuilder.G(name,parent,p);}
    static GameObject Box(string name,Transform parent,Vector2 p,Vector2 size,int color,int order)
    {return FactoryWarehouseBuilder.B(name,parent,p,size,color,order);}
    static void Beam(string name,Transform parent,Vector2 a,Vector2 b,float width,int color,int order)
    {
        var t=Box(name,parent,(a+b)*.5f,new Vector2(Vector2.Distance(a,b),width),color,order).transform;
        t.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
    }
    static void Cargo(Transform root,Vector2 center,Vector2 size,float tilt,int index)
    {
        var crate=Group("Solid cargo "+index+" - sight transparent",root,center);
        var collision=crate.gameObject.AddComponent<BoxCollider2D>();collision.size=size;
        crate.gameObject.AddComponent<RecoilSurfaceVisibility>().blocksSight=false;
        var art=Group("Crate artwork",crate,center);int[] colors={0x786b4c,0x62715b,0x897a55,0x6c6550};
        Box("Crate body",art,center,size,colors[index%colors.Length],9);
        for(int row=1;row<=3;row++)Box("Board joint",art,center+Vector2.up*(size.y*(row/4f-.5f)),new Vector2(size.x*.98f,.022f),0x3b493b,10);
        foreach(float sign in new[]{-1f,1f})
        {
            float x=center.x+sign*size.x*.39f;
            Box("Steel binding",art,new Vector2(x,center.y),new Vector2(.055f,size.y*.98f),0x35483d,11);
            foreach(float sy in new[]{-1f,1f})Box("Binding rivet",art,new Vector2(x,center.y+sy*size.y*.39f),new Vector2(.025f,.025f),0xada98a,12);
        }
        Box("Worn top edge",art,center+Vector2.up*(size.y*.5f-.025f),new Vector2(size.x,.045f),0x9c956e,11);
        Box("Freight paper label",art,center+new Vector2(size.x*.12f,size.y*.13f),new Vector2(size.x*.22f,size.y*.12f),0xaaa78b,12);
        Box("Label stain",art,center+new Vector2(size.x*.16f,size.y*.13f),new Vector2(size.x*.11f,size.y*.045f),0x676b52,13);
        art.localRotation=Quaternion.Euler(0,0,tilt);
    }
    [MenuItem("Recoil/Art/Collapse Central Warehouse Rack")]
    public static string Apply()
    {
        if(EditorApplication.isPlaying)throw new Exception("Exit Play before editing the warehouse.");
        var original=SceneManager.GetActiveScene();var scene=SceneManager.GetSceneByPath(ScenePath);bool loaded=scene.isLoaded;
        if(!loaded)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var world=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RecoilPrototype>(true)).First();
            if(world.GetComponentsInChildren<Transform>(true).Any(t=>t.name==CollapsedName))return "Central collapsed rack already exists; edit its saved objects.";
            var racks=world.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Steel rack - pallets and freight").OrderBy(t=>t.position.x).ToArray();
            if(racks.Length<3)throw new Exception("Expected at least three warehouse racks.");
            string backup="Backups/WarehouseCollapsedRack/"+DateTime.Now.ToString("yyyyMMdd_HHmmss");Directory.CreateDirectory(backup);
            if(!EditorSceneManager.SaveScene(scene,backup+"/Warehouse.unity",true))throw new Exception("Warehouse backup failed.");
            var serialized=new SerializedObject(world);FactoryWarehouseBuilder.UseArt((Sprite)serialized.FindProperty("sceneSprite").objectReferenceValue,(Material)serialized.FindProperty("sceneMaterial").objectReferenceValue);
            var rack=racks[2];float x=rack.position.x;var ladder=rack.GetComponentInChildren<ClimbableLadder>(true);
            foreach(var r in rack.GetComponentsInChildren<SpriteRenderer>(true))if(!ladder || !r.transform.IsChildOf(ladder.transform))r.enabled=false;
            if(ladder)
            {
                ladder.enabled=false;ladder.GetComponent<BoxCollider2D>().enabled=false;
                ladder.transform.position=new Vector3(x-3,.12f,-.15f);ladder.transform.rotation=Quaternion.Euler(0,0,-82);
                foreach(var r in ladder.GetComponentsInChildren<SpriteRenderer>(true))r.sortingOrder=15;
            }
            rack.name=CollapsedName;
            var frame=Group("Fallen steel frame - foreground perspective",rack,new Vector2(x,0));
            Vector2 backLeft=new Vector2(x-1.45f,1.4f),backRight=new Vector2(x+1.45f,1.4f),frontLeft=new Vector2(x-2.65f,.07f),frontRight=new Vector2(x+2.65f,.07f);
            Beam("Bent left rack upright",frame,backLeft,frontLeft,.13f,0x7b8271,14);Beam("Bent right rack upright",frame,backRight,frontRight,.13f,0x8e927b,14);
            Beam("Rear shelf twisted beam",frame,backLeft,backRight,.12f,0x6f6650,6);
            Beam("Front shelf beam - fallen toward viewer",frame,frontLeft,frontRight,.16f,0x9b8458,14);
            for(int i=1;i<=2;i++){float t=i/3f;Beam("Collapsed shelf crossbeam",frame,Vector2.Lerp(backLeft,frontLeft,t),Vector2.Lerp(backRight,frontRight,t),.08f,0x626e5d,14);}
            Beam("Broken diagonal brace",frame,frontLeft+new Vector2(.1f,.1f),backRight-new Vector2(.1f,.1f),.045f,0x475a4b,7);
            Beam("Detached steel brace",frame,new Vector2(x-2.8f,.3f),new Vector2(x-1.7f,1.05f),.07f,0x8a8c71,13);
            foreach(float sign in new[]{-1f,1f})
            {
                Vector2 foot=new Vector2(x+sign*1.47f,.1f);
                Box("Torn rack base plate",frame,foot,new Vector2(.4f,.12f),0x5d6c5a,12);
                Beam("Buckled upright remnant",frame,foot,foot+new Vector2(sign*.3f,1.6f),.09f,0x637665,6);
                for(int j=0;j<4;j++)Box("Upright perforation",frame,foot+new Vector2(sign*.3f*j/4f,.25f+j*.31f),new Vector2(.035f,.06f),0x293d34,7);
            }
            var pile=Group("Cargo pile - 3.7m - solid, sight transparent",rack,new Vector2(x,0));int n=0;
            Cargo(pile,new Vector2(x-2.05f,.55f),new Vector2(1.1f,1.1f),-3,n++);
            Cargo(pile,new Vector2(x-.95f,.5f),new Vector2(1.2f,1),2,n++);
            Cargo(pile,new Vector2(x+.2f,.65f),new Vector2(1.25f,1.3f),-2,n++);
            Cargo(pile,new Vector2(x+1.35f,.475f),new Vector2(1.2f,.95f),4,n++);
            Cargo(pile,new Vector2(x+2.35f,.425f),new Vector2(.8f,.85f),-5,n++);
            Cargo(pile,new Vector2(x-1.55f,1.55f),new Vector2(1.25f,1.1f),3,n++);
            Cargo(pile,new Vector2(x-.4f,1.6f),new Vector2(1.15f,1.2f),-4,n++);
            Cargo(pile,new Vector2(x+.85f,1.4f),new Vector2(1.25f,1.15f),2,n++);
            Cargo(pile,new Vector2(x+1.95f,1.25f),new Vector2(.85f,.85f),-6,n++);
            Cargo(pile,new Vector2(x-.9f,2.6f),new Vector2(1.3f,1),-4,n++);
            Cargo(pile,new Vector2(x+.3f,2.65f),new Vector2(1.15f,1.1f),5,n++);
            Cargo(pile,new Vector2(x+1.25f,2.25f),new Vector2(.9f,.95f),-5,n++);
            Cargo(pile,new Vector2(x-.35f,3.25f),new Vector2(1,.9f),0,n++);
            var debris=Group("Loose boards and damaged pallet",rack,new Vector2(x,0));
            for(int i=0;i<7;i++){float offset=(i-3)*.83f;Beam("Scattered pallet plank",debris,new Vector2(x+offset-.35f,.03f+(i%2)*.045f),new Vector2(x+offset+.34f,.065f+(i%3)*.04f),.055f,0x7b7453,13);}
            var crane=world.GetComponentInChildren<KBKStackerCrane>();
            if(crane)crane.liftHeight=Mathf.Max(crane.liftHeight,4.1f);
            Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Warehouse save failed.");
            return "Central rack collapsed at x="+x+"; 13 sight-transparent solid cargo colliders; top=3.7m; ladder disabled. Backup: "+backup;
        }
        finally{if(!loaded)EditorSceneManager.CloseScene(scene,true);if(original.isLoaded)SceneManager.SetActiveScene(original);}
    }
}
