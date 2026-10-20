Shader "Recoil/ArtTest/FilmGrade"
{
 Properties { _MainTex("Source",2D)="white"{} _Exposure("Exposure",Range(0.1,6))=2.5 _Grain("Grain",Range(0,0.1))=0.003 _Vignette("Vignette",Range(0,1))=0.5 }
 SubShader { Cull Off ZWrite Off ZTest Always
 Pass { CGPROGRAM
 #pragma vertex vert_img
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float _Grain,_Vignette,_Exposure;
 fixed4 frag(v2f_img i):SV_Target {
  float3 c=tex2D(_MainTex,i.uv).rgb*_Exposure;
  float l=dot(c,float3(.2126,.7152,.0722));c=lerp(l.xxx,c,.72);
  c=pow(max(c,0),1.02);c*=float3(.91,.98,1.02);
  float2 q=i.uv*2-1;c*=1-_Vignette*saturate(dot(q,q)*.42);
  float noise=frac(sin(dot(i.uv*_ScreenParams.xy,float2(12.9898,78.233))+floor(_Time.y*12))*43758.5453)-.5;
  c+=noise*_Grain;return float4(max(c,0),1);
 }
 ENDCG } }
 Fallback Off
}
