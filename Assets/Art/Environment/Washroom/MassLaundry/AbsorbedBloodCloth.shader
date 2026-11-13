Shader "Recoil/3D/Absorbed Blood Cloth"
{
 Properties { _MainTex("UV",2D)="white" {} _Color("Aged fabric",Color)=(.48,.46,.36,1) _BloodColor("Dry absorbed blood",Color)=(.17,.035,.022,1) _Seed("Pattern variation",Float)=0 }
 SubShader {
 Tags { "RenderType"="Opaque" } LOD 200 Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 fixed4 _Color,_BloodColor;float _Seed;
 struct Input {float2 uv_MainTex;float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float2 uv=IN.uv_MainTex;float n=noise(uv*9+_Seed*3.1)*.55+noise(uv*23-_Seed)*.30+noise(uv*61)*.15;
 float2 p=(uv-float2(.40+sin(_Seed)*.13,.49))*float2(2.3,1.7);
 float d=length(p)+n*.31;
 float core=1-smoothstep(.33,.54,d);float soak=1-smoothstep(.49,.67,d);
 float2 p2=(uv-float2(.80,.82))*float2(4.3,3.7);float smear=1-smoothstep(.24,.49,length(p2)+n*.2);
 float stain=saturate(max(core,smear)*.87+soak*.13);
 float weave=(sin(uv.x*1500)*sin(uv.y*1500))*.012;
 float dirt=(noise(uv*38)*.09+noise(uv*7)*.07);
 o.Albedo=lerp(_Color.rgb*(.91+dirt+weave),_BloodColor.rgb*(.82+n*.40),stain);o.Metallic=0;o.Smoothness=.045;o.Alpha=1;
 }
 ENDCG
 } FallBack "Diffuse"
}
