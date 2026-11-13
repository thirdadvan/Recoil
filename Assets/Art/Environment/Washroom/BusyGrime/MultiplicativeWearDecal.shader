Shader "Recoil/3D/Multiplicative Wear Decal"
{
 Properties{
 _MainTex("Generated dirt RGBA",2D)="white"{}
 _Tint("Pigment and opacity",Color)=(.4,.35,.3,.6)
 _KeyWhite("Remove pale backing",Range(0,1))=0
 _BloodBoost("Blood warmth",Range(0,1))=.5
 _UseLocalUV("Use local patch fade",Float)=1
 }
 SubShader{Tags{"Queue"="Transparent-2" "RenderType"="Transparent"} Blend DstColor Zero ZWrite Off ZTest LEqual Cull Off Offset -1,-1
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_ST,_Tint;float _KeyWhite,_BloodBoost,_UseLocalUV;
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float2 local:TEXCOORD1;};
 v2f vert(appdata_full v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.texcoord,_MainTex);o.local=v.texcoord1.xy;return o;}
 fixed4 frag(v2f i):SV_Target{fixed4 dirt=tex2D(_MainTex,i.uv);float2 f=lerp(i.uv,i.local,_UseLocalUV);float edge=smoothstep(0,.025,f.x)*smoothstep(0,.025,1-f.x)*smoothstep(0,.025,f.y)*smoothstep(0,.025,1-f.y);float lum=dot(dirt.rgb,float3(.299,.587,.114));float ink=saturate((.90-lum)*2.6);float opacity=dirt.a*_Tint.a*edge*lerp(1,ink,_KeyWhite);float warm=saturate((dirt.r-dirt.b)*8)*saturate((.68-lum)*3);float3 pigment=dirt.rgb*_Tint.rgb;pigment=lerp(pigment,pigment*float3(1,.45,.4),warm*_BloodBoost);return fixed4(lerp(1,pigment,opacity),1);}
 ENDCG}
 }Fallback Off
}
