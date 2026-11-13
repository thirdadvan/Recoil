Shader "Recoil/3D/Door Reflective Highlight"
{
 Properties { _Tint("Reflection tint",Color)=(.48,.65,.70,1) _Glow("Reflection strength",Range(0,1))=0 }
 SubShader {
 Tags { "Queue"="Transparent-2" "RenderType"="Transparent" }
 Blend One One ZWrite Off ZTest LEqual Cull Back Offset -1,-1
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma multi_compile_fog
 #include "UnityCG.cginc"
 float4 _Tint; float _Glow;
 struct appdata{float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
 struct v2f{float4 pos:SV_POSITION;float3 normal:TEXCOORD0;float3 view:TEXCOORD1;float2 uv:TEXCOORD2;UNITY_FOG_COORDS(3)};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.normal=UnityObjectToWorldNormal(v.normal);o.view=UnityWorldSpaceViewDir(mul(unity_ObjectToWorld,v.vertex).xyz);o.uv=v.uv;UNITY_TRANSFER_FOG(o,o.pos);return o;}
 fixed4 frag(v2f i):SV_Target{
 float3 n=normalize(i.normal),v=normalize(i.view);
 float rim=pow(1-saturate(abs(dot(n,v))),2);
 float2 edge=min(i.uv,1-i.uv);float bevel=1-smoothstep(.005,.045,min(edge.x,edge.y));
 float sheen=pow(saturate(dot(reflect(-v,n),normalize(float3(-.35,.6,-1)))),24);
 fixed4 c=fixed4(_Tint.rgb*_Glow*(bevel*.7+rim*.35+sheen*.2+.035),0);
 UNITY_APPLY_FOG_COLOR(i.fogCoord,c,fixed4(0,0,0,0));return c;
 }
 ENDCG
 }
 }
}
