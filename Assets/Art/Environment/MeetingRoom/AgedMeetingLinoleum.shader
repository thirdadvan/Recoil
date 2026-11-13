Shader "Recoil/3D/Aged Meeting Linoleum"
{
 Properties{_TileA("Worn linoleum A",Color)=(.26,.28,.24,1) _TileB("Worn linoleum B",Color)=(.235,.25,.22,1)}
 SubShader{Tags{"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _TileA,_TileB;
 struct Input{float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o){float2 uv=IN.worldPos.xz*2;float2 f=frac(uv),cell=floor(uv);float parity=fmod(abs(cell.x+cell.y),2);float seam=1-smoothstep(.006,.015,min(min(f.x,1-f.x),min(f.y,1-f.y)));float dirt=noise(IN.worldPos.xz*7)*.07+noise(IN.worldPos.xz*41)*.035;float variation=hash(cell)*.045;float3 c=lerp(_TileA.rgb,_TileB.rgb,parity);o.Albedo=c*(.88+dirt+variation)*(1-seam*.16);o.Smoothness=.09;o.Metallic=0;o.Alpha=1;}
 ENDCG
 }Fallback "Diffuse"
}
