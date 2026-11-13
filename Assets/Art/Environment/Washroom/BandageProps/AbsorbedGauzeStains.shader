Shader "Recoil/3D/Absorbed Gauze Stains"
{
 Properties { _MainTex("Original generated fabric",2D)="white"{} _Color("Dusty fabric tint",Color)=(.86,.84,.76,1) _BloodColor("Old absorbed blood",Color)=(.19,.033,.021,1) _BoundsCenter("Mesh centre",Vector)=(0,0,0,0) _BoundsSize("Mesh size",Vector)=(1,1,1,0) }
 SubShader{ Tags{"RenderType"="Opaque"} Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows vertex:vert
 #pragma target 3.0
 sampler2D _MainTex;fixed4 _Color,_BloodColor;float4 _BoundsCenter,_BoundsSize;
 struct Input{float2 uv_MainTex;float3 localPos;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.localPos=v.vertex.xyz;}
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o){float3 q=(IN.localPos-_BoundsCenter.xyz)/max(_BoundsSize.xyz,.0001);float3 a=(q-float3(-.19,.12,-.18))/float3(.29,.48,.27);float3 b=(q-float3(.28,-.04,.18))/float3(.22,.39,.30);float n=noise(q.xz*24)+noise(q.xy*33)*.35;float d=min(length(a),length(b))+.18*n;float stain=1-smoothstep(.66,1.03,d);fixed3 fabric=tex2D(_MainTex,IN.uv_MainTex).rgb*_Color.rgb;o.Albedo=lerp(fabric,_BloodColor.rgb*(.86+.18*n),stain*.94);o.Smoothness=.045;o.Metallic=0;o.Alpha=1;}
 ENDCG
 }Fallback "Diffuse"
}
