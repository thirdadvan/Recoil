using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

public static class Recoil3DRemainingNotesPass
{
    static void PlaceZ(Transform t,float z){var p=t.position;p.z=z;t.position=p;}
    static void VisualZ(Recoil3DSpriteVisual v,float center,float depth)
    {v.depth=depth;v.depthOffset+=(center-v.meshRenderer.bounds.center.z)/Mathf.Max(.001f,Mathf.Abs(v.transform.lossyScale.z));v.Sync();}
    public static string Store(Scene scene)
    {
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var store=all.First(t=>t.name=="Service Station - editable exterior");
        var facade=all.First(t=>t.name=="Store facade").GetComponentsInChildren<Recoil3DSpriteVisual>(true).First(v=>v.source.enabled&&!v.suppressed);
        float front=facade.meshRenderer.bounds.min.z;
        foreach(var v in store.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source&&v.source.enabled&&!v.suppressed))
        {
            string context=v.name+" "+(v.transform.parent?v.transform.parent.name:"");
            // The roof is playable at Z=0: connect its existing front edge to the building instead of shifting its collision.
            if(context.Contains("Service roof terrain"))VisualZ(v,10.4f,21.5f);
            else if(context.Contains("Roof fascia"))VisualZ(v,-.34f,.20f);
            else if(context.Contains("Roof skylight curb"))VisualZ(v,0,1.6f);
        }
        foreach(var name in new[]{"Shop sign","Service sign text","Station light fixture"})
        {var t=all.FirstOrDefault(x=>x.name==name);if(t)PlaceZ(t,front-(name=="Service sign text"?.19f:.12f));}
        var roofLabel=all.FirstOrDefault(t=>t.name=="Roof skylight label");if(roofLabel)PlaceZ(roofLabel,-.86f);
        EditorSceneManager.MarkSceneDirty(scene);return "Connected roof to building; attached sign and lamp to facade.";
    }
    public static int Stairwell(Scene scene)
    {
        var background=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Stairwell background - editable");int count=0;
        foreach(var v in background.GetComponentsInChildren<Recoil3DSpriteVisual>(true).Where(v=>v.source&&v.source.enabled&&!v.suppressed))
        {
            var n=v.name+" "+(v.transform.parent?v.transform.parent.name:"");float z=2.25f,d=.025f;
            if(n.Contains("Aged stairwell back wall")){z=2.55f;d=.35f;}
            else if(n.Contains("conduit")||n.Contains("Conduit")){z=2.08f;d=.06f;}
            else if(n.Contains("pier")){z=2.10f;d=.24f;}
            else if(n.Contains("sign")||n.Contains("Sign")){z=1.98f;d=.035f;}
            VisualZ(v,z,d);count++;
        }
        foreach(var text in background.GetComponentsInChildren<TextMesh>(true))
        {PlaceZ(text.transform,1.93f);if(text.name=="Floor number")text.characterSize=.065f; text.color=new Color(.62f,.63f,.55f);EditorUtility.SetDirty(text);}
        EditorSceneManager.MarkSceneDirty(scene);return count;
    }
    static RampagerController Actor(RecoilPrototype world,RampagerController template,string name,Vector3 p,RampagerDepthNavigation graph,bool sleeping)
    {
        var go=UnityEngine.Object.Instantiate(template.gameObject,world.transform);go.name=name;go.transform.localScale=Vector3.one;go.transform.position=p;
        foreach(var point in go.GetComponentsInChildren<RampagerPatrolPoint>(true))UnityEngine.Object.DestroyImmediate(point.gameObject);
        var actor=go.GetComponent<RampagerController>();actor.initialState=sleeping?RampagerController.Behaviour.Sleeping:RampagerController.Behaviour.Roaming;actor.sleepPoint=null;actor.HideStatusLabel=false;
        actor.Configure(world,-1.5f,1.5f);RampagerPatrolRoute.CreateDefaultPoints(actor);
        var visual=go.GetComponent<Recoil3DActorVisual>();visual.model.localScale=Vector3.one;
        Recoil3DMonsterWorkwear.Dress(visual);
        var depth=go.AddComponent<RampagerDepthArrival>();depth.navigation=graph;
        return actor;
    }
    public static int AddDepthActors(Scene scene,bool outdoor)
    {
        var world=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<RecoilPrototype>(true)).First();
        const string name="Rampager Navigation - automatic shared";if(world.transform.Find(name))return 0;
        var template=world.GetComponentsInChildren<RampagerController>(true).First();
        var graph=new GameObject(name).AddComponent<RampagerDepthNavigation>();graph.transform.SetParent(world.transform,false);
        if(outdoor)
        {
            Actor(world,template,"Rampager - foreground sound arrival",new Vector3(2,.8f,-3.1f),graph,false);
            Actor(world,template,"Rampager - background sound arrival",new Vector3(6.6f,.8f,3.4f),graph,true);
            EditorSceneManager.MarkSceneDirty(scene);return 2;
        }
        Actor(world,template,"Rampager - workshop background sound arrival",new Vector3(5,.8f,3.4f),graph,false);
        EditorSceneManager.MarkSceneDirty(scene);return 1;
    }
}
