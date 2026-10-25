using System.Collections.Generic;
using UnityEngine;

// A camera-local light affects visual meshes only. Temporary layer changes exist
// solely during this camera's render, leaving physics and scene serialization intact.
[ExecuteAlways,DefaultExecutionOrder(1400),RequireComponent(typeof(Camera))]
public sealed class CameraCharacterFill : MonoBehaviour
{
    public bool fillEnabled=true;
    [Range(0,2)] public float intensity=.65f;
    public Color colour=new Color(.68f,.75f,.78f);
    [Min(1)] public float range=48;
    [Range(30,170)] public float coneAngle=115;
    [Range(0,31)] public int characterRenderLayer=30;
    public float cameraUpOffset=.6f;
    Light fill;WarehouseAtmosphereTest study;float refreshAt=-1;
    readonly List<Renderer> actors=new List<Renderer>();
    readonly List<int> layers=new List<int>();
    readonly List<Light> actorLights=new List<Light>();
    readonly List<int> lightMasks=new List<int>();
    bool assigned;
    void OnEnable(){study=GetComponent<WarehouseAtmosphereTest>();refreshAt=-1;}
    void Update(){if(Application.isPlaying&&Input.GetKeyDown(KeyCode.F7))fillEnabled=!fillEnabled;}
    void EnsureLight()
    {
        if(fill)return;
        var g=new GameObject("Camera character fill - visual only");g.hideFlags=HideFlags.HideAndDontSave;
        g.transform.SetParent(transform,false);fill=g.AddComponent<Light>();fill.type=LightType.Spot;
        fill.shadows=LightShadows.None;fill.renderMode=LightRenderMode.ForcePixel;fill.bounceIntensity=0;
    }
    void RefreshActors()
    {
        if(Time.realtimeSinceStartup<refreshAt)return;
        refreshAt=Time.realtimeSinceStartup+2;actors.Clear();actorLights.Clear();
        var seen=new HashSet<Renderer>();var seenLights=new HashSet<Light>();
        foreach(var root in gameObject.scene.GetRootGameObjects())
        foreach(var actor in root.GetComponentsInChildren<Recoil3DActorVisual>(true))
        {
            foreach(var r in actor.GetComponentsInChildren<Renderer>(true))
                if((r is MeshRenderer || r is SkinnedMeshRenderer)&&seen.Add(r))actors.Add(r);
            foreach(var l in actor.GetComponentsInChildren<Light>(true))if(seenLights.Add(l))actorLights.Add(l);
        }
    }
    void OnPreCull()
    {
        Restore();EnsureLight();bool active=fillEnabled&&(!study||study.testLook);
        fill.enabled=active;if(!active)return;
        fill.transform.localPosition=Vector3.up*cameraUpOffset;fill.transform.localRotation=Quaternion.identity;
        fill.color=colour;fill.intensity=intensity;fill.range=range;fill.spotAngle=coneAngle;fill.innerSpotAngle=coneAngle*.75f;
        fill.cullingMask=1<<characterRenderLayer;RefreshActors();layers.Clear();lightMasks.Clear();
        foreach(var r in actors){layers.Add(r?r.gameObject.layer:0);if(r)r.gameObject.layer=characterRenderLayer;}
        // Preserve the core's existing chest illumination on temporarily reassigned meshes.
        foreach(var l in actorLights){lightMasks.Add(l?l.cullingMask:0);if(l)l.cullingMask|=1<<characterRenderLayer;}
        assigned=true;
    }
    void OnPostRender(){Restore();}
    void Restore()
    {
        if(!assigned)return;
        for(int i=0;i<actors.Count;i++)if(actors[i])actors[i].gameObject.layer=layers[i];
        for(int i=0;i<actorLights.Count;i++)if(actorLights[i])actorLights[i].cullingMask=lightMasks[i];
        assigned=false;
    }
    void OnDisable(){Restore();if(fill){DestroyImmediate(fill.gameObject);fill=null;}}
}
