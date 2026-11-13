Shader "Recoil/3D/Archive Record Surface"
{
 Properties{_Color("Tint",Color)=(1,1,1,1)}
 SubShader{Tags{"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf RouteMoonStandard fullforwardshadows addshadow exclude_path:deferred
 #pragma target 3.0
 #include "OperationPlaneMoonlight.cginc"
 fixed4 _Color;
 struct Input{float4 color:COLOR;float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){float grain=frac(sin(dot(floor(i.worldPos*95),float3(12.9898,78.233,39.425)))*43758.5453);o.Albedo=i.color.rgb*_Color.rgb*(.95+.05*grain);o.Metallic=0;o.Smoothness=.10;o.Occlusion=1;o.Emission=o.Albedo*.008;o.Alpha=1;}
 ENDCG
 }Fallback "Standard"
}
