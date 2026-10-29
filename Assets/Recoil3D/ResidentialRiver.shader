Shader "Recoil/3D/Residential River"
{
 Properties { _Color("Water",Color)=(.035,.065,.07,1) _FlowSpeed("Flow toward background",Float)=.65 _Ripple("Ripple strength",Range(0,1))=.25 }
 SubShader {
  Tags { "RenderType"="Opaque" }
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows vertex:vert
  #pragma target 3.0
  fixed4 _Color; float _FlowSpeed, _Ripple;
  struct Input { float3 worldPos; };
  void vert(inout appdata_full v) {
   float3 p=mul(unity_ObjectToWorld,v.vertex).xyz;
   float w=sin(p.z*2.8+p.x*1.5-_Time.y*_FlowSpeed*2.8)*.018+sin(p.z*6-p.x*3-_Time.y*_FlowSpeed*6)*.006;
   v.vertex.y+=w;
  }
  void surf(Input IN,inout SurfaceOutputStandard o) {
   float t=IN.worldPos.z-_Time.y*_FlowSpeed;
   float wave=sin(t*2.8+IN.worldPos.x*1.5)*sin(t*6-IN.worldPos.x*3);
   o.Albedo=_Color.rgb*(.75+wave*.25);
   o.Normal=normalize(float3(cos(t*2.8+IN.worldPos.x*1.5)*_Ripple, sin(t*6)*_Ripple, 1));
   o.Metallic=.18; o.Smoothness=.76;
   o.Emission=_Color.rgb*(.16+max(0,wave)*.10); o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
