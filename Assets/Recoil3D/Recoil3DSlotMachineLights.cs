using UnityEngine;

// Four batched bulb circuits share one update; bulbs are emissive meshes, not individual lights.
[DisallowMultipleComponent]
public sealed class Recoil3DSlotMachineLights : MonoBehaviour
{
    public Renderer[] circuits;
    public Color[] colours;
    [Min(.05f)] public float stepInterval=.16f;
    [Range(0,1)] public float dimBrightness=.25f;
    float nextStep;int step;
    MaterialPropertyBlock block;
    void OnEnable(){nextStep=0;block=new MaterialPropertyBlock();}
    void Update()
    {
        if(Time.time<nextStep)return;nextStep=Time.time+stepInterval;step++;
        if(circuits==null||colours==null||colours.Length==0)return;
        for(int i=0;i<circuits.Length;i++)if(circuits[i])
        {
            float brightness=(step+i)%4<2?1:dimBrightness;
            block.SetColor("_EmissionColor",colours[i%colours.Length]*brightness);
            circuits[i].SetPropertyBlock(block);
        }
    }
}
