Shader "Recoil/Art Tests/Soft Contact Shadow"{
 Properties{_Strength("Strength",Range(0,1))=.45}
 SubShader{Tags{"Queue"="Transparent-10" "RenderType"="Transparent"}
 Blend DstColor Zero ZWrite Off Cull Off Offset -1,-1
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float _Strength;
 struct v2f{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy*2-1;return o;}
 fixed4 frag(v2f i):SV_Target{float a=pow(saturate(1-dot(i.uv,i.uv)),2)*_Strength;return fixed4((1-a).xxx,1);}
 ENDCG}
 }Fallback Off
}
