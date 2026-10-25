using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

// One-time saved set dressing. Collision, interaction and navigation remain on the gameplay plane.
public static class Recoil3DPropDepthPlacement
{
    const string Marker="Prop composition - foreground and background placement";
    static void Depth(Transform t,float z){var p=t.position;p.z=z;t.position=p;}
    static bool Decoration(Transform t)
    {
        if(t.GetComponentsInChildren<Collider2D>(true).Any(c=>!c.isTrigger))return false;
        if(t.GetComponentInParent<Recoil3DActorVisual>()||t.GetComponentInParent<ContinuousInteractionItem>()||t.GetComponentInParent<ReadableProp>()||t.GetComponentInParent<RecoilDoor>())return false;
        for(var p=t;p;p=p.parent)if(p.name.Contains("impassable")||p.name.Contains("Cargo pile")||p.name.Contains("Truck"))return false;
        return true;
    }
    static Transform Bundle(Transform item,string name)
    {
        var parent=item.parent;var pos=item.position;var group=new GameObject(name).transform;group.SetParent(parent,false);group.position=pos;
        // Crate braces/straps are siblings in the original 2D hierarchy. Always move them together.
        var companions=parent.Cast<Transform>().Where(t=>t!=group&&Vector2.Distance(t.position,pos)<.02f&&(t==item||t.name=="Crate brace"||t.name=="Crate strap")).ToArray();
        foreach(var t in companions)t.SetParent(group,true);return group;
    }
    static int ForegroundCargo(Scene scene)
    {
        if(!scene.path.EndsWith("/RecoilFactoryWarehouse.unity"))return 0;
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        if(all.Any(t=>t.name=="Foreground packing crate - visual only"))return 0;
        var racks=all.Where(t=>t.name=="Steel rack - pallets and freight"&&t.gameObject.activeInHierarchy).OrderBy(t=>t.position.x).ToArray();int count=0;
        foreach(var rack in racks.Where((r,i)=>i==0||i==racks.Length-1))
        {
            var crate=rack.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Packing crate"&&t.position.y<1.5f&&t.position.z>4&&t.position.z<6).OrderBy(t=>Mathf.Abs(t.position.x-rack.position.x)).FirstOrDefault();
            if(!crate)continue;crate.name="Foreground packing crate - visual only";Depth(crate,count==0?-4.5f:-5.2f);count++;
        }
        if(count>0)EditorSceneManager.MarkSceneDirty(scene);return count;
    }
    public static int Apply(Scene scene)
    {
        var roots=scene.GetRootGameObjects();var world=roots.SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
        int foreground=ForegroundCargo(scene);
        if(world.transform.Find(Marker))return foreground;
        var all=roots.SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();int moved=0;
        foreach(var t in all.Where(t=>t.gameObject.activeInHierarchy&&t.name=="Freight crate"&&Decoration(t)).ToArray())
        {
            // Keep the collapsed rack and its cargo contiguous across the road.
            if(t.parent.name.Contains("collapsed"))continue;
            var group=Bundle(t,"Background freight crate - editable together");
            int index=Mathf.RoundToInt(t.position.x*10+t.position.y*3);Depth(group,3.1f+(Mathf.Abs(index)%3)*.65f);
            foreach(var v in group.GetComponentsInChildren<Recoil3DSpriteVisual>(true))
            {v.depth=v.name=="Freight crate"?1.15f:.035f;v.depthOffset=v.name=="Freight crate"?0:-.60f;v.Sync();}
            moved++;
        }
        int crate=0;
        foreach(var t in all.Where(t=>t.gameObject.activeInHierarchy&&t.name=="Spare parts crate"&&Decoration(t)).ToArray())
        {
            var g=Bundle(t,crate==0?"Foreground spare parts crate":"Background spare parts crate");Depth(g,crate==0?-2.4f:1.9f+crate*.25f);
            foreach(var v in g.GetComponentsInChildren<Recoil3DSpriteVisual>(true)){v.depth=v.name=="Spare parts crate"?.9f:.035f;v.depthOffset=v.name=="Spare parts crate"?0:-.48f;v.Sync();}
            crate++;moved++;
        }
        foreach(var t in all.Where(t=>t.gameObject.activeInHierarchy&&Decoration(t)&&
            (t.name=="Cabinet"||t.name=="Desk and chair"||t.name=="Waiting chair"||t.name=="Chair")).ToArray())
        {
            // Furniture stays as a complete group, clear of the player's silhouette and door handles.
            float z=t.name=="Cabinet"?2.15f:t.name=="Desk and chair"?1.65f:1.3f;Depth(t,z);moved++;
        }
        if(moved>0){var marker=new GameObject(Marker);marker.transform.SetParent(world.transform,false);EditorSceneManager.MarkSceneDirty(scene);}
        return moved+foreground;
    }
}
