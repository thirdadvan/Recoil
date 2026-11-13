Shader "Recoil/3D/Door Proximity Halo"
{
 Properties { _Tint("Door light colour",Color)=(.48,.65,.70,1) _Glow("Proximity glow",Range(0,1))=0 }
 SubShader {
  Tags { "Queue"="Transparent-3" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend One One Cull Off ZWrite Off ZTest LEqual
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   float4 _Tint;float _Glow;
   struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
   struct v2f {float4 position:SV_POSITION;float2 uv:TEXCOORD0;UNITY_FOG_COORDS(1)};
   v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.position);return o;}
   fixed4 frag(v2f i):SV_Target{
    float2 p=abs(i.uv*2-1);float edge=max(p.x,p.y);
    float mask=pow(1-smoothstep(.2,1,edge),1.7);
    fixed4 c=fixed4(_Tint.rgb*(_Glow*mask),0);UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(0,0,0,0));return c;
   }
   ENDCG
  }
 }
}
