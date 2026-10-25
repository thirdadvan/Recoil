using UnityEngine;

// Idempotent upgrade of the existing editable geometric player, without rebuilding its scene.
public static class Recoil3DPlayerLegRig
{
    public static void Configure(Recoil3DActorVisual visual)
    {
        if(!visual || !visual.GetComponent<RecoilPlayerController>())return;
        var pose=visual.GetComponent<Recoil3DPlayerLegPose>();
        if(!pose)pose=visual.gameObject.AddComponent<Recoil3DPlayerLegPose>();
        pose.leftHip=visual.leftLeg;pose.rightHip=visual.rightLeg;
        pose.leftKnee=BuildLeg(visual.leftLeg);pose.rightKnee=BuildLeg(visual.rightLeg);
        visual.parts=visual.model.GetComponentsInChildren<Renderer>(true);
    }
    static Transform BuildLeg(Transform hip)
    {
        if(!hip)return null;
        var existing=hip.Find("Knee pivot");if(existing)return existing;
        var upper=hip.GetComponentInChildren<MeshRenderer>();if(!upper)return null;
        upper.transform.localPosition=new Vector3(0,-.13f,0);
        upper.transform.localScale=new Vector3(.13f,.26f,.15f);
        var knee=new GameObject("Knee pivot").transform;
        knee.SetParent(hip,false);knee.localPosition=new Vector3(0,-.26f,0);
        Mesh("Lower leg",knee,new Vector3(0,-.125f,0),new Vector3(.12f,.25f,.14f),upper.sharedMaterial);
        Mesh("Shoe",knee,new Vector3(0,-.245f,-.035f),new Vector3(.14f,.03f,.22f),upper.sharedMaterial);
        return knee;
    }
    static void Mesh(string name,Transform parent,Vector3 position,Vector3 scale,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;
        go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
    }
}
