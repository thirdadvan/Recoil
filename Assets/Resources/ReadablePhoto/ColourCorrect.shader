Shader "Recoil/UI/Readable Photo Colour"
{
 Properties{_MainTex("Photo",2D)="white"{}}
 SubShader{Tags{"Queue"="Overlay"} Cull Off ZWrite Off ZTest Always Blend SrcAlpha OneMinusSrcAlpha
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;return o;}
 fixed4 frag(v2f i):SV_Target{return tex2D(_MainTex,i.uv);}
 ENDCG}
 }Fallback Off
}
