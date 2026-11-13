Shader "Recoil/3D/Weathered Wall Lettering"
{
 Properties { _MainTex("Font atlas",2D)="white"{} _Color("Paint",Color)=(.28,.12,.07,1) }
 SubShader {
  Tags { "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows alphatest:_Cutoff exclude_path:deferred
  #pragma target 3.0
  sampler2D _MainTex; fixed4 _Color;
  struct Input { float2 uv_MainTex; float3 worldPos; float4 color:COLOR; };
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float grain=frac(sin(dot(floor(IN.worldPos.xy*92),float2(12.9898,78.233)))*43758.5453);
   o.Albedo=_Color.rgb*IN.color.rgb*(.7+.3*grain);
   o.Alpha=tex2D(_MainTex,IN.uv_MainTex).a*step(.15,grain);
   clip(o.Alpha-.45);o.Occlusion=1;o.Smoothness=.02;
  }
  ENDCG
 }
 Fallback "Transparent/Cutout/Diffuse"
}
