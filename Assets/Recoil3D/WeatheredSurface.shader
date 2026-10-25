Shader "Recoil/3D/Weathered Surface" {
Properties { _TentacleEnergyStrength("Tentacle energy emission",Float)=0 _Color("Colour",Color)=(.35,.4,.4,1) _MainTex("Surface",2D)="white"{} _Metallic("Metal",Range(0,1))=.2 _Glossiness("Smoothness",Range(0,1))=.25 _EmissionColor("Emission",Color)=(0,0,0,0) _Weathering("Patina",Range(0,1))=.25 _DepthHazeColour("Depth haze",Color)=(.012,.016,.018,1) _HazeDensity("Depth haze density",Range(0,.1))=.006 }
SubShader { Tags{"RenderType"="Opaque"} LOD 200
CGPROGRAM
#pragma surface surf Standard fullforwardshadows addshadow
#pragma target 3.0
sampler2D _MainTex; fixed4 _Color,_EmissionColor,_DepthHazeColour; half _TentacleEnergyStrength; half _HazeDensity, _Metallic,_Glossiness,_Weathering;
struct Input {float2 uv_MainTex;float3 worldPos;float4 screenPos;float4 color:COLOR;float3 viewDir;};
float hash(float3 p){return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453);}
void surf(Input IN,inout SurfaceOutputStandard o){
 float n=hash(floor(IN.worldPos*28));float broad=sin(IN.worldPos.y*17+sin(IN.worldPos.x*3)*2)*.5+.5;
 float2 pixel=floor(IN.screenPos.xy/IN.screenPos.w*_ScreenParams.xy);
 float d=frac(dot(pixel,float2(.754877666,.569840296)));
 clip(_Color.a-d-.00001);
 fixed3 c=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;
 o.Albedo=c*lerp(1,.72+n*.25+broad*.12,_Weathering);o.Metallic=_Metallic;o.Smoothness=_Glossiness;float haze=1-exp(-max(0,IN.worldPos.z)*_HazeDensity);o.Emission=IN.color.rgb*_TentacleEnergyStrength*(.2+.8*saturate(normalize(IN.viewDir).z))+_EmissionColor.rgb+_DepthHazeColour.rgb*haze+c*.006;o.Alpha=1;
}
ENDCG
} Fallback "Standard"
}
