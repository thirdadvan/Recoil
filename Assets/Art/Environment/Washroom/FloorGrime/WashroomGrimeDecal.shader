Shader "Recoil/3D/Washroom Grime Decal"
{
 Properties {
  _MainTex("Generated grime and footprint RGBA",2D)="white"{}
  _Tint("Dirt colour and opacity",Color)=(.42,.40,.36,.70)
  _Wetness("Residual water sheen",Range(0,1))=.28
  _BloodBoost("Blood warmth",Range(0,1))=.9
  _KeyWhite("Remove pale backing",Range(0,1))=0
  _UseLocalUV("Use local patch fade",Float)=0
 }
 SubShader {
 Tags {"Queue"="Transparent-4" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Cull Off ZWrite Off Offset -1,-1
 CGPROGRAM
 #pragma surface surf Standard alpha:fade keepalpha fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Tint;half _Wetness,_BloodBoost,_KeyWhite,_UseLocalUV;
 struct Input{float2 uv_MainTex;float2 uv2_MainTex;};
 void surf(Input IN,inout SurfaceOutputStandard o){
 fixed4 dirt=tex2D(_MainTex,IN.uv_MainTex);
 float2 fadeUV=lerp(IN.uv_MainTex,IN.uv2_MainTex,_UseLocalUV);float edge=smoothstep(0,.025,fadeUV.x)*smoothstep(0,.025,1-fadeUV.x)*smoothstep(0,.025,fadeUV.y)*smoothstep(0,.025,1-fadeUV.y);
 float3 colour=dirt.rgb*_Tint.rgb;float warm=saturate((dirt.r-dirt.b)*8)*saturate((.68-dot(dirt.rgb,float3(.299,.587,.114)))*3);o.Albedo=lerp(colour,colour*float3(1,.45,.4),warm*_BloodBoost);o.Metallic=0;
 o.Smoothness=.07+_Wetness*saturate(dirt.a)*.7;
 float ink=saturate((.90-dot(dirt.rgb,float3(.299,.587,.114)))*2.6);o.Alpha=dirt.a*_Tint.a*edge*lerp(1,ink,_KeyWhite);
 }
 ENDCG
 } Fallback Off
}
