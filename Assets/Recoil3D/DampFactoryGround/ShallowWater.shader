Shader "Recoil/3D/Shallow Industrial Puddle"
{
 Properties {_MainTex("UV",2D)="white"{} _Color("Water tint",Color)=(.035,.045,.039,.65) _ReflectionCube("Local factory reflection",Cube)="black"{} }
 SubShader { Tags {"Queue"="Transparent-3" "RenderType"="Transparent"} ZWrite Off Cull Off Offset -1,-1
 CGPROGRAM
 #pragma surface surf RouteMoonStandard alpha:fade exclude_path:deferred
 #pragma target 3.0
 #include "../OperationPlaneMoonlight.cginc"
 sampler2D _MainTex;samplerCUBE _ReflectionCube;fixed4 _Color;
 struct Input {float2 uv_MainTex;float3 worldPos;float3 worldRefl;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o){float r=length((IN.uv_MainTex-.5)*2);float edge=1-smoothstep(.56,.96,r+(noise(IN.worldPos.xz*4)-.5)*.18);o.Albedo=_Color.rgb;o.Metallic=.05;o.Smoothness=.94;o.Occlusion=1;o.Emission=texCUBE(_ReflectionCube,IN.worldRefl).rgb*.42;o.Alpha=edge*_Color.a;}
 ENDCG
 } Fallback Off
}
