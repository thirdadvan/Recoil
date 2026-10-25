using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

// Explicit editor operation: no runtime geometry rebuild and no changes to the 2D scenes.
public static class Recoil3DDepthGeometryAlignment
{
    [MenuItem("Recoil/3D/Align current scene to player depth planes")]
    static void AlignCurrentScene()
    {
        Debug.Log(Align(SceneManager.GetActiveScene()));
        // Leave the scene dirty so the user can review the alignment before saving.
    }
    static bool Live(Recoil3DSpriteVisual v)
    { return v && v.source && v.source.enabled && !v.suppressed && v.meshRenderer && v.gameObject.activeInHierarchy; }

    static void Depth(Recoil3DSpriteVisual v, float z, float width)
    {
        v.depth=width;
        v.depthOffset=v.transform.InverseTransformPoint(new Vector3(v.meshRenderer.bounds.center.x,v.meshRenderer.bounds.center.y,z)).z;
        v.Sync(); EditorUtility.SetDirty(v); EditorUtility.SetDirty(v.meshRenderer.transform);
    }

    static void Top(Recoil3DSpriteVisual v,float y)
    {
        var t=v.meshRenderer.transform;var p=t.position;
        p.y+=y-v.meshRenderer.bounds.max.y;t.position=p;EditorUtility.SetDirty(t);
    }

    static float PathHeight(WalkableStairway stair,Vector2 centre)
    {
        // Choose the closest flight, since the two flights overlap in X.
        float best=float.PositiveInfinity,height=centre.y;
        for(int i=1;i<stair.routePoints.Length;i++)
        {
            Vector2 a=stair.routePoints[i-1].position,b=stair.routePoints[i].position;
            float t=Mathf.Abs(b.x-a.x)>.0001f?Mathf.Clamp01((centre.x-a.x)/(b.x-a.x)):0;
            var point=Vector2.Lerp(a,b,t);float gap=(point-centre).sqrMagnitude;
            if(gap<best){best=gap;height=point.y;}
        }
        return height;
    }

    public static string Align(Scene scene,bool stairsOnly=false)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play mode first.");
        if(!scene.path.StartsWith("Assets/Scenes/3D/"))throw new InvalidOperationException("Only native 3D scenes may be aligned.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var plane=all.Select(t=>t.GetComponent<PlayerDepthPlane>()).FirstOrDefault(p=>p);
        if(!plane)return "No player depth plane: skipped.";
        float z=plane.PlatformZ;int steps=0,platforms=0,roofs=0;
        var stairs=all.Select(t=>t.GetComponent<WalkableStairway>()).Where(s=>s&&s.Available).ToArray();
        foreach(var stair in stairs)
        {
            var duplicate=all.FirstOrDefault(t=>t.name=="3D stairs - "+stair.name);
            if(duplicate && stair.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Any(v=>Live(v)&&v.name=="Grated steel tread"))
                duplicate.gameObject.SetActive(false);
            foreach(var point in stair.routePoints){var p=point.position;p.z=z;point.position=p;EditorUtility.SetDirty(point);}
            foreach(var v in stair.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(Live))
            {
                string label=v.name=="Surface - unified industrial material"?v.transform.parent.name:v.name;
                if(label=="Grated steel tread" || label=="Steel tread" || label=="Tread front edge")
                {
                    var b=v.meshRenderer.bounds;
                    Top(v,PathHeight(stair,new Vector2(b.center.x,b.center.y)));
                    Depth(v,z+.4f,.8f);steps++;
                }
                else if(label=="Load-bearing stringer")Depth(v,z+.4f,.8f);
                else if(label.Contains("rail")||label=="Guardrail upright"||label=="Rail post")Depth(v,z+.88f,.07f);
                else if(label=="Half landing")Depth(v,z+.4f,.8f);
            }
        }
        var escape=all.FirstOrDefault(t=>t.name=="Exterior fire escape - W up S down");
        if(escape)
            foreach(var v in escape.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(Live))
            {
                if(v.GetComponentInParent<WalkableStairway>())continue;
                string label=v.name=="Surface - unified industrial material"?v.transform.parent.name:v.name;
                if(label=="Landing safety rail"||label=="Bent railing - visual only")Depth(v,z+.88f,.07f);
                else if(label=="Fire escape load-bearing column"||label=="Column footing")Depth(v,z+.78f,.20f);
                else if(label=="Wall tie bracket")Depth(v,z+.7f,1.4f);
                else if(label=="Hanging broken grating - visual only")Depth(v,z+.4f,.8f);
            }
        foreach(var roof in all.Select(t=>t.GetComponent<SoftRoofPlatform>()).Where(r=>r&&r.Available))
        {
            bool stairPlatform=roof.GetComponentInParent<WalkableStairway>() || roof.name=="Steel landing deck" || roof.name=="Fire escape balcony";
            if(stairsOnly && !stairPlatform)continue;
            foreach(var v in roof.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(Live))
            {
                // Limit this operation to each platform's own body, not attached decoration.
                string label=v.name=="Surface - unified industrial material"?v.transform.parent.name:v.name;
                if(label!=roof.name)continue;
                var bounds=v.meshRenderer.bounds;
                if(stairPlatform)Depth(v,z+.4f,.8f);
                else if(bounds.min.z>z-.45f || bounds.max.z<z+.45f)
                    Depth(v,z,Mathf.Max(1.7f,bounds.size.z));
                Top(v,roof.Area.yMax);platforms++;
            }
        }
        if(!stairsOnly && scene.name=="RecoilPrototype")
        {
            foreach(var pair in new[]{new[]{"Factory parapet","Factory main facade - volumetric building"},new[]{"Roof parapet","Warehouse wall - volumetric building"},new[]{"Service roof terrain","Store facade - volumetric building"}})
            {
                var support=all.Select(t=>t.GetComponent<SoftRoofPlatform>()).First(r=>r&&r.name==pair[0]);
                var building=all.First(t=>t.name==pair[1]);
                var roof=building.GetComponentsInChildren<MeshRenderer>(true).Concat(support.GetComponentsInChildren<MeshRenderer>(true)).First(r=>r.name=="Solid flat roof");
                // The visible front edge starts on the rear gameplay plane; it must not cover the default lane.
                var b=roof.bounds;float front=z,back=b.max.z;
                var p=roof.transform.position;p.x=support.Area.center.x;p.z=(front+back)*.5f;roof.transform.position=p;
                var scale=roof.transform.localScale;
                scale.x*=support.Area.width/b.size.x;scale.z*=(back-front)/b.size.z;roof.transform.localScale=scale;
                // The existing complete roof is authoritative. Remove the floating duplicate strip.
                var collider=support.GetComponent<BoxCollider2D>();
                float top=roof.bounds.max.y;
                var offset=collider.offset;
                offset.y+=support.transform.InverseTransformVector(new Vector3(0,top-support.Area.yMax,0)).y;
                collider.offset=offset;
                foreach(var v in support.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.meshRenderer))
                {v.suppressed=true;v.Sync();EditorUtility.SetDirty(v);}
                // Keep the roof and its detailing with the gameplay object when it is moved later.
                foreach(var t in building.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="Roof seam"||t.name=="Solid flat roof"||t.name=="Roof vent"||t.name=="Vent rain cover").ToArray())
                    t.SetParent(support.transform,true);
                EditorUtility.SetDirty(collider);EditorUtility.SetDirty(roof.transform);roofs++;
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);
        return scene.name+": stairs="+stairs.Length+", tread surfaces="+steps+", platforms="+platforms+", joined roofs="+roofs+", platform Z="+z;
    }
}
