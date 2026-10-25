using UnityEngine;

[DisallowMultipleComponent,RequireComponent(typeof(RecoilPlayerController))]
public sealed class PlayerAmmoRingUI : MonoBehaviour
{
    [Min(1)] public float radius=18;
    [Min(1)] public float thickness=3;
    [Min(0)] public float hideDelay=2;
    [Min(.01f)] public float fadeDuration=.5f;
    RecoilPlayerController player;
    RecoilPrototype world;
    Texture2D ring;
    readonly Color32[] pixels=new Color32[128*128];
    int cachedAmmo=-1,cachedCapacity=-1;

    void OnEnable(){player=GetComponent<RecoilPlayerController>();world=GetComponentInParent<RecoilPrototype>();}
    void RebuildRing()
    {
        if(!ring){ring=new Texture2D(128,128,TextureFormat.RGBA32,false);ring.filterMode=FilterMode.Bilinear;ring.wrapMode=TextureWrapMode.Clamp;}
        int capacity=Mathf.Max(1,player.ammoCapacity);
        float step=360f/capacity,gap=Mathf.Min(5,step*.2f),inner=48*(1-Mathf.Clamp(thickness/radius,.02f,.9f));
        for(int y=0;y<128;y++)for(int x=0;x<128;x++)
        {
            float dx=x+.5f-64,dy=64-y-.5f,distance=Mathf.Sqrt(dx*dx+dy*dy);
            float angle=Mathf.Repeat(Mathf.Atan2(dy,dx)*Mathf.Rad2Deg+90,360);
            bool active=angle%step>=gap*.5f && angle%step<=step-gap*.5f;
            float alpha=active?Mathf.Clamp01(distance-inner)*Mathf.Clamp01(48-distance):0;
            var color=(int)(angle/step)<player.Ammo?new Color(.48f,.9f,1,alpha):new Color(.2f,.3f,.35f,alpha*.85f);
            pixels[y*128+x]=color;
        }
        ring.SetPixels32(pixels);ring.Apply(false,false);cachedAmmo=player.Ammo;cachedCapacity=player.ammoCapacity;
    }
    void OnGUI()
    {
        if(Event.current.type!=EventType.Repaint || !world || !world.Playing || !player.HasWaterGun || float.IsNegativeInfinity(player.LastAmmoUITrigger))return;
        float alpha=1-Mathf.SmoothStep(0,1,(world.Clock-player.LastAmmoUITrigger-hideDelay)/Mathf.Max(.01f,fadeDuration));
        if(alpha<=0)return;
        if(!ring || cachedAmmo!=player.Ammo || cachedCapacity!=player.ammoCapacity)RebuildRing();
        var camera=Camera.main;if(!camera)return;
        var center=camera.WorldToScreenPoint(player.WorldPosition+(Vector3)(player.Aim*.5f));if(center.z<=0)return;
        center.y+=36*Screen.height/720f;
        float size=radius*(128f/48)*Screen.height/720f;
        var matrix=GUI.matrix;var color=GUI.color;
        GUI.matrix=Matrix4x4.identity;GUI.color=new Color(1,1,1,alpha);
        GUI.DrawTexture(new Rect(center.x-size*.5f,Screen.height-center.y-size*.5f,size,size),ring);
        GUI.color=color;GUI.matrix=matrix;
    }
    void OnDestroy(){if(ring)Destroy(ring);}
}
