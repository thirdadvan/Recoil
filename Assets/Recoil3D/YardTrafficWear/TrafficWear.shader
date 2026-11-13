Shader "Recoil/3D/Aged Yard Traffic Marks"
{
 Properties { _MainTex("UV",2D)="white"{} _Tint("Rubber or dragged material",Color)=(.035,.033,.027,.45) _Type("0 rolling tyre, 1 drag scuff, 2 braking rubber, 3 exposed substrate",Float)=0 }
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Offset -1,-1
 CGPROGRAM
 #pragma surface surf RouteMoonStandard alpha:fade exclude_path:deferred
 #pragma target 3.0
 #include "../OperationPlaneMoonlight.cginc"
 fixed4 _Tint;float _Type;
 struct Input {float2 uv_MainTex;float3 worldPos;float4 color:COLOR;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o)
 {
  float2 uv=IN.uv_MainTex;float patch=noise(IN.worldPos.xz*4.1),grain=noise(IN.worldPos.xz*83);
  float edge=1-smoothstep(.32,.50,abs(uv.x-.5)+(grain-.5)*.12);float wear=.25+.75*smoothstep(.18,.68,patch+grain*.12);float pattern=1;
  if(_Type<.5){float groove=smoothstep(.08,.21,abs(frac(uv.x*4.5+.12)-.5));float blocks=.58+.42*smoothstep(.12,.28,frac(uv.y*7.8+abs(uv.x-.5)*1.25));pattern=(.42+.58*groove)*blocks;}
  else if(_Type<1.5){float fibres=.25+.75*noise(float2(uv.x*47,uv.y*1.5));pattern=fibres*smoothstep(.12,.44,patch);}
  else if(_Type<2.5){pattern=.68+.32*noise(float2(uv.x*35,uv.y*.7));}
  else{pattern=.72+.28*noise(float2(uv.x*31,uv.y*4.2));wear=.55+.45*smoothstep(.18,.68,patch);}
  o.Albedo=_Tint.rgb*(.8+.2*grain);o.Metallic=0;o.Smoothness=.08;o.Occlusion=1;o.Alpha=_Tint.a*IN.color.a*edge*wear*pattern;
 }
 ENDCG
 } Fallback Off
}
