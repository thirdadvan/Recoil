Shader "Recoil/ArtTest/SoftBeam"
{
 Properties { _Color("Tint",Color)=(0.35,0.5,0.55,0.05) }
 SubShader {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" }
  Blend SrcAlpha One
  ZWrite Off Cull Off
  Pass {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma multi_compile_fog
   #include "UnityCG.cginc"
   float4 _Color;
   struct appdata { float4 vertex:POSITION;float2 uv:TEXCOORD0; };
   struct v2f { float4 vertex:SV_POSITION;float2 uv:TEXCOORD0; UNITY_FOG_COORDS(1) };
   v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.vertex);return o;}
   fixed4 frag(v2f i):SV_Target {
    float edge=pow(saturate(sin(i.uv.x*3.14159265)),2);
    float ends=saturate(i.uv.y*8)*pow(1-i.uv.y,1.3);
    fixed4 c=_Color;c.a*=edge*ends;
    UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(0,0,0,0));return c;
   }
   ENDCG
  }
 }
}
