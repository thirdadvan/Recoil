Shader "Recoil/3D/Archive Shelf Standard" {
Properties { _Color("Colour",Color)=(1,1,1,1) _MainTex("Surface",2D)="white"{} _Metallic("Metallic",Range(0,1))=0 _Glossiness("Smoothness",Range(0,1))=.12 _EmissionColor("Emission",Color)=(0,0,0,0) }
SubShader { Tags{"RenderType"="Opaque"} LOD 200
CGPROGRAM
#pragma surface surf Standard fullforwardshadows addshadow
#pragma target 3.0
sampler2D _MainTex; fixed4 _Color,_EmissionColor;half _Metallic,_Glossiness;
struct Input {float2 uv_MainTex;float4 color:COLOR;};
void surf(Input i,inout SurfaceOutputStandard o){o.Albedo=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb*i.color.rgb;o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Occlusion=1;o.Emission=_EmissionColor.rgb*i.color.rgb;o.Alpha=1;}
ENDCG
}Fallback "Standard"
}
