Shader "Recoil/3D/Screen Light Volume"
{
 Properties{_Color("Screen light colour",Color)=(.32,.85,.48,1) _Density("Air scattering",Float)=.075}
 SubShader{
  Tags{"Queue"="Transparent-12" "RenderType"="Transparent"}
  Cull Front ZWrite Off ZTest Always Blend One One
  Pass{
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "UnityCG.cginc"
   UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
   float4 _Color;float _Density;float4 _Shape; // aperture half XY, length, spread
   struct v2f{float4 vertex:SV_POSITION;float4 screen:TEXCOORD0;float3 world:TEXCOORD1;};
   v2f vert(appdata_base v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
   float4 frag(v2f i):SV_Target{
    float3 dir=normalize(i.world-_WorldSpaceCameraPos);
    float3 forward=-UNITY_MATRIX_V[2].xyz;
    float raw=SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen));
    // Reuse this pixel's existing depth: foreground silhouettes cut only their
    // covered portion out of the glow, without another camera or actor mask pass.
    float3 emitter=mul(unity_ObjectToWorld,float4(0,0,0,1)).xyz;
    float emitterEye=-mul(UNITY_MATRIX_V,float4(emitter,1)).z;
    if(LinearEyeDepth(raw)<emitterEye-.06)return 0;
    float scene=LinearEyeDepth(raw)/max(.001,dot(dir,forward));
    float3 o=mul(unity_WorldToObject,float4(_WorldSpaceCameraPos,1)).xyz;
    float3 d=mul((float3x3)unity_WorldToObject,dir);
    float2 farHalf=_Shape.xy+_Shape.z*_Shape.w;float3 lo=float3(-farHalf,0),hi=float3(farHalf,_Shape.z);
    float3 safe=lerp(float3(.00001,.00001,.00001),d,step(.00001,abs(d)));float3 t0=(lo-o)/safe,t1=(hi-o)/safe;
    float3 nearT=min(t0,t1),farT=max(t0,t1);
    float entry=max(0,max(nearT.x,max(nearT.y,nearT.z))),exit=min(scene,min(farT.x,min(farT.y,farT.z)));
    if(exit<=entry)return 0;
    float sum=0,step=(exit-entry)/32;
    [unroll]for(int j=0;j<32;j++){
     float3 p=o+d*(entry+(j+.5)*step);float2 halfSize=_Shape.xy+p.z*_Shape.w;
     float edge=1-max(abs(p.x)/max(.001,halfSize.x),abs(p.y)/max(.001,halfSize.y));
     float fade=smoothstep(0,.12,edge)*smoothstep(0,.05,p.z)*(1-smoothstep(_Shape.z*.60,_Shape.z,p.z));
     sum+=fade/(1+p.z*p.z*2)*step;
    }
    return float4(_Color.rgb*(1-exp(-sum*_Density)),0);
   }
   ENDCG
  }
 }
}
