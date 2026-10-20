using UnityEngine;

[DisallowMultipleComponent,AddComponentMenu("Recoil/Rampager Visual Effects")]
public sealed class RampagerVisualEffects : MonoBehaviour
{
    Transform visualRoot;
    SpriteRenderer visualBody;

    public SpriteRenderer Initialize(SpriteRenderer source,Transform eye)
    {
        if(visualRoot)return visualBody;
        visualRoot=new GameObject("Visuals - Tremble").transform;
        visualRoot.SetParent(transform,false);
        if(source)
        {
            // Keep the actor root and every gameplay component at the authoritative position.
            visualBody=visualRoot.gameObject.AddComponent<SpriteRenderer>();
            visualBody.sprite=source.sprite;visualBody.sharedMaterials=source.sharedMaterials;
            visualBody.color=source.color;visualBody.flipX=source.flipX;visualBody.flipY=source.flipY;
            visualBody.sortingLayerID=source.sortingLayerID;visualBody.sortingOrder=source.sortingOrder;
            visualBody.drawMode=source.drawMode;visualBody.size=source.size;
            visualBody.tileMode=source.tileMode;visualBody.maskInteraction=source.maskInteraction;
            visualBody.spriteSortPoint=source.spriteSortPoint;visualBody.enabled=source.enabled;
            visualRoot.gameObject.layer=source.gameObject.layer;
            source.enabled=false;
        }
        if(eye)eye.SetParent(visualRoot,false);
        return visualBody;
    }
    public void ClearTremble(){if(visualRoot)visualRoot.localPosition=Vector3.zero;}
    void OnDisable(){ClearTremble();}
}
