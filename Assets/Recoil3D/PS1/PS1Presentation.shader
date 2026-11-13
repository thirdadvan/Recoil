Shader "Recoil/PS1 Presentation" {
Properties { _MainTex("Source",2D)="white"{} }
SubShader { Cull Off ZWrite Off ZTest Always
Pass { CGPROGRAM
#pragma vertex vert_img
#pragma fragment frag
#include "UnityCG.cginc"
sampler2D _MainTex; float _Steps,_Dither;
fixed4 frag(v2f_img i):SV_Target {
float3 c=tex2D(_MainTex,i.uv).rgb;
// Ordered 4x4 Bayer threshold in destination pixel coordinates.
float2 p=floor(i.pos.xy); float2 a=fmod(p,2);float2 b=fmod(floor(p/2),2);
float d=(4*(2*a.x+3*a.y-4*a.x*a.y)+(2*b.x+3*b.y-4*b.x*b.y)+.5)/16-.5;
c=saturate(c+d*_Dither/(_Steps-1));c=floor(c*(_Steps-1)+.5)/(_Steps-1);
return float4(c,1);
}
ENDCG } } }
