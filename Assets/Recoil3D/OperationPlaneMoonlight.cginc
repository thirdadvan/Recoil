#ifndef RECOIL_OPERATION_PLANE_MOONLIGHT
#define RECOIL_OPERATION_PLANE_MOONLIGHT
#include "UnityPBSLighting.cginc"
// x=enabled, y=plane Z, z=full-strength half width, w=fade width.
float4 _RecoilRouteMoonBand;
// x=foreground full-strength distance, y=foreground fade distance.
float4 _RecoilRouteMoonForeground;
float4 _RecoilRouteMoonDirection;
float _RecoilRouteMoonMultiplier;
float _RecoilRouteMoonMinimum;
// Optional Y mask: enabled, floor minimum, floor maximum, feather.
float4 _RecoilRouteMoonHeight;
float4 _RecoilRoutePlaneDiffuseFill;
inline half4 LightingRouteMoonStandard(SurfaceOutputStandard s,half3 viewDir,UnityGI gi)
{
    return LightingStandard(s,viewDir,gi);
}
inline void LightingRouteMoonStandard_GI(SurfaceOutputStandard s,UnityGIInput data,inout UnityGI gi)
{
    LightingStandard_GI(s,data,gi);
    float delta=data.worldPos.z-_RecoilRouteMoonBand.y;
    float depth=abs(delta);
    float2 falloff=delta<0?_RecoilRouteMoonForeground.xy:_RecoilRouteMoonBand.zw;
    float band=1-smoothstep(falloff.x,falloff.x+max(.01,falloff.y),depth);
    float feather=max(.01,_RecoilRouteMoonHeight.w);
    float heightMask=smoothstep(_RecoilRouteMoonHeight.y-feather,_RecoilRouteMoonHeight.y,data.worldPos.y)
        *(1-smoothstep(_RecoilRouteMoonHeight.z,_RecoilRouteMoonHeight.z+feather,data.worldPos.y));
    band*=lerp(1,heightMask,saturate(_RecoilRouteMoonHeight.x));
    // Only the referenced directional moon receives the multiplier.
    // Native GI, local lamps and native shadow attenuation remain unchanged.
    float directional=1-step(.5,_WorldSpaceLightPos0.w);
    float selected=step(.9995,dot(normalize(gi.light.dir),_RecoilRouteMoonDirection.xyz));
    float strength=lerp(saturate(_RecoilRouteMoonMinimum),max(0,_RecoilRouteMoonMultiplier),band);
    gi.light.color*=lerp(1,strength,_RecoilRouteMoonBand.x*directional*selected);
    // Soft spatial bounce is independent of Unity's selected sun and of ceiling
    // shadow maps. It remains diffuse, depth-limited and height-limited.
    float facing=.4+.6*saturate(dot(normalize(s.Normal),_RecoilRouteMoonDirection.xyz));
    gi.indirect.diffuse+=_RecoilRoutePlaneDiffuseFill.rgb*(band*_RecoilRouteMoonBand.x*facing);
}
#endif
