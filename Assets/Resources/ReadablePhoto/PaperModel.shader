Shader "Recoil/UI/Curled Paper Model"
{
 Properties{_MainTex("Paper",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1)}
 SubShader{Tags{"Queue"="Transparent"} Cull Off ZWrite On ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;fixed4 _Tint;
 struct input{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
 struct output{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 normal:TEXCOORD1;};
 output vert(input v){output o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.normal=v.normal;return o;}
 fixed4 frag(output i):SV_Target{fixed4 c=tex2D(_MainTex,i.uv)*_Tint;float light=.52+.48*abs(dot(normalize(i.normal),normalize(float3(-.32,.42,-1))));c.rgb*=light;return c;}
 ENDCG}
 }Fallback Off
}
