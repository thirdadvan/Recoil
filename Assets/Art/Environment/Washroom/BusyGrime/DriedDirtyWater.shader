Shader "Recoil/3D/Dried Dirty Water"
{
 Properties{_Tint("Dried water pigment and opacity",Color)=(.30,.27,.22,.40) _Seed("Patch variation",Float)=1}
 SubShader{Tags{"Queue"="Transparent-3" "RenderType"="Transparent"} Blend DstColor Zero ZWrite Off ZTest LEqual Cull Off Offset -1,-1
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Tint;float _Seed;
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 fixed4 frag(v2f i):SV_Target{float2 p=(i.uv-.5)*2;float n=noise(p*3+_Seed)*.55+noise(p*11-_Seed)*.25+noise(p*32)*.20;float d=length(p)+.23*(n-.5)+.06*sin(atan2(p.y,p.x)*7+_Seed);float outline=(1-smoothstep(.055,.13,abs(d-.74)))*.65;float inner=(1-smoothstep(.57,.88,d))*(.12+.36*n);float oldRing=(1-smoothstep(.018,.07,abs(d-.57)))*.12;float a=saturate(max(outline,inner)+oldRing)*_Tint.a;float grain=.87+.13*noise(p*120);float3 pigment=_Tint.rgb*grain;return fixed4(lerp(1,pigment,a),1);}
 ENDCG}
 }Fallback Off
}
