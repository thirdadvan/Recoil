using System.Collections.Generic;
using UnityEngine;

// Hard collision geometry also blocks the player's view. Soft roofs remain transparent.
[ExecuteAlways,DefaultExecutionOrder(1200),DisallowMultipleComponent,AddComponentMenu("Recoil/Hard Surface Sight Shadows")]
public sealed class RecoilSightOcclusion : MonoBehaviour
{
    public bool shadowsEnabled=true;
    [Range(0,1)] public float shadowOpacity=1;
    readonly List<Vector3> vertices=new List<Vector3>();
    readonly List<int> triangles=new List<int>();
    readonly List<Color> colors=new List<Color>();
    BoxCollider2D[] blockers;
    float nextBlockerRefresh;
    Mesh mesh;Material material;GameObject view;MeshRenderer renderer;
    RecoilPrototype world;Transform observer;SpriteRenderer darkness;
    public void Configure(RecoilPrototype context,Transform player,SpriteRenderer overlay)
    {world=context;observer=player;darkness=overlay;}
    void OnEnable(){blockers=null;}
    void EnsureView()
    {
        if(view)return;
        view=new GameObject("Hard surface sight shadow mesh"){hideFlags=HideFlags.HideAndDontSave};
        view.transform.SetParent(transform,false);view.transform.position=Vector3.zero;view.transform.rotation=Quaternion.identity;view.transform.localScale=Vector3.one;
        mesh=new Mesh{name="Sight shadow quads"};mesh.MarkDynamic();view.AddComponent<MeshFilter>().sharedMesh=mesh;
        material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
        renderer=view.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
    }
    public static bool IsBlocker(BoxCollider2D c)
    {
        if(!c || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger || c.GetComponent<SoftRoofPlatform>() || c.GetComponent<ClimbableLadder>())return false;
        var surface=c.GetComponent<RecoilSurfaceVisibility>();return !surface || surface.blocksSight;
    }
    void LateUpdate()
    {
        if(!world)world=GetComponentInParent<RecoilPrototype>();
        if(!observer && world && world.Player)observer=world.Player.transform;
        if(!darkness){var l=GetComponent<RecoilLighting2D>();if(l)darkness=l.GetComponentInChildren<SpriteRenderer>();}
        if(!observer || !world || !darkness)return;
        EnsureView();renderer.enabled=shadowsEnabled && darkness.enabled;
        if(!renderer.enabled)return;
        if(blockers==null || Time.realtimeSinceStartup>=nextBlockerRefresh)
        {blockers=world.GetComponentsInChildren<BoxCollider2D>(true);nextBlockerRefresh=Time.realtimeSinceStartup+.5f;}
        renderer.sortingLayerID=darkness.sortingLayerID;renderer.sortingOrder=darkness.sortingOrder+1;
        var camera=Camera.main;float reach=camera?Mathf.Max(100,4*camera.orthographicSize*(1+camera.aspect)):200;
        Vector2 origin=observer.position;
        vertices.Clear();triangles.Clear();colors.Clear();
        foreach(var c in blockers)
        {
            if(!IsBlocker(c))continue;
            var b=c.bounds;
            if(b.Contains(new Vector3(origin.x,origin.y,b.center.z)))continue;
            if(camera && Vector2.Distance(b.ClosestPoint(camera.transform.position),camera.transform.position)>reach*.6f)continue;
            Vector2 a=new Vector2(b.min.x,b.min.y),d=new Vector2(b.max.x,b.max.y);
            Edge(a,new Vector2(d.x,a.y),origin,reach);Edge(new Vector2(d.x,a.y),d,origin,reach);
            Edge(d,new Vector2(a.x,d.y),origin,reach);Edge(new Vector2(a.x,d.y),a,origin,reach);
        }
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    }
    void Edge(Vector2 a,Vector2 b,Vector2 origin,float reach)
    {
        // Extrude away from the observer. Overlapping opaque quads form the shadow union.
        int n=vertices.Count;Vector2 farA=a+(a-origin).normalized*reach,farB=b+(b-origin).normalized*reach;
        vertices.Add(view.transform.InverseTransformPoint(a));vertices.Add(view.transform.InverseTransformPoint(b));
        vertices.Add(view.transform.InverseTransformPoint(farB));vertices.Add(view.transform.InverseTransformPoint(farA));
        for(int i=0;i<4;i++)colors.Add(new Color(0,0,0,shadowOpacity));
        triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);triangles.Add(n);triangles.Add(n+2);triangles.Add(n+3);
    }
    void OnDisable(){if(renderer)renderer.enabled=false;}
    void OnTransformChildrenChanged(){blockers=null;}
    void OnDestroy()
    {
        if(Application.isPlaying){if(view)Destroy(view);if(mesh)Destroy(mesh);if(material)Destroy(material);}
        else{if(view)DestroyImmediate(view);if(mesh)DestroyImmediate(mesh);if(material)DestroyImmediate(material);}
    }
}
