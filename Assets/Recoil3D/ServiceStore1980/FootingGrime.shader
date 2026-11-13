Shader "Recoil/3D/Building Footing Moss And Silt"
{
 Properties {_MainTex("UV",2D)="white"{} _Ground("Ground strip",Float)=0 _Opacity("Amount",Range(0,1))=.68 _MossTint("Moss colour",Color)=(.105,.145,.037,1) _SiltTint("Silt colour",Color)=(.085,.070,.043,1)}
 SubShader {Tags {"Queue"="Transparent-6" "RenderType"="Transparent"} Cull Off ZWrite Off Offset -1,-1
 CGPROGRAM
 #pragma surface surf RouteMoonStandard alpha:fade exclude_path:deferred
 #pragma target 3.0
 #include "../OperationPlaneMoonlight.cginc"
 sampler2D _MainTex;float _Ground,_Opacity;fixed4 _MossTint,_SiltTint;
 struct Input {float2 uv_MainTex;float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o)
 {
  float3 w=IN.worldPos;float along=w.x+w.z*.79;
  float patch=noise(float2(along*2.7,w.y*7+w.z*.3));float grain=noise(float2(along*49,w.y*51+w.z*18));
  float top=IN.uv_MainTex.y;float edge=1-smoothstep(.16,.92,top+(patch-.5)*.6);
  if(_Ground>.5)
  {
   // Keep the wall seed at the junction, then break growth into two-dimensional ground colonies.
   patch=lerp(patch,patch*.55+noise(w.xz*5.8)*.45,smoothstep(0,.35,top));
   grain=noise(w.xz*65);
   edge=1-smoothstep(.12,.94,top+(patch-.5)*.8);
  }
  float clusters=smoothstep(.31,.74,patch+grain*.17);
  if(_Ground>.5)clusters=smoothstep(.34,.64,patch+grain*.17);
  float colourCoverage=_Ground>.5?.65+.35*clusters:clusters;
  o.Albedo=lerp(_SiltTint.rgb,_MossTint.rgb,colourCoverage)*(.67+.33*grain);
  o.Metallic=0;o.Smoothness=_Ground>.5?.07+patch*.05:.14+patch*.17;o.Occlusion=.9;
  float density=.25+.75*clusters;
  if(_Ground>.5)density=lerp(.52+.48*clusters,.12+.88*clusters,smoothstep(0,.25,top));
  o.Alpha=edge*density*_Opacity;
 }
 ENDCG
 } Fallback Off
}
