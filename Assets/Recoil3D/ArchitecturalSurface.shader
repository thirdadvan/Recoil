Shader "Recoil/3D/Metric Architectural Surface"
{
 Properties {
 _Color("State colour (alpha)",Color)=(1,1,1,1)
 _BaseTint("Finish",Color)=(.3,.31,.3,1)
 _MortarTint("Mortar",Color)=(.16,.17,.16,1)
 _Kind("0 concrete, 1 masonry, 2 plaster, 3 steel, 4 wood",Float)=0
 _BrickSize("Brick length / course height / joint",Vector)=(.26,.085,.006,0)
 _PlasterCoverage("Plaster coverage",Range(0,1))=.4
 _Metallic("Metal",Range(0,1))=0
 _Glossiness("Smoothness",Range(0,1))=.18
 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 #include "UnityCG.cginc"
 fixed4 _Color,_BaseTint,_MortarTint;float4 _BrickSize;float _Kind,_PlasterCoverage,_Metallic,_Glossiness;
 struct Input {float3 worldPos;float3 worldNormal;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o)
 {
   clip(_Color.a-.001);
   // Object-aligned metres: scaling a wall adds bricks rather than stretching individual bricks.
   float3 sc=float3(length(unity_ObjectToWorld._m00_m10_m20),length(unity_ObjectToWorld._m01_m11_m21),length(unity_ObjectToWorld._m02_m12_m22));
   float3 p=mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz*sc;
   float3 n=abs(mul((float3x3)unity_WorldToObject,IN.worldNormal));
   float2 uv=n.z>n.x&&n.z>n.y?p.xy:n.x>n.y?p.zy:p.xz;
   float grain=noise(uv*34),patch=noise(uv*.7)+.35*noise(uv*3.1);
   float3 c=_BaseTint.rgb*(.86+.1*grain+.1*patch);float smooth=_Glossiness;
   if(_Kind>.5 && _Kind<1.5)
   {
     float2 size=max(_BrickSize.xy,float2(.04,.025));float row=floor(uv.y/size.y);float2 brickUV=float2(uv.x+fmod(abs(row),2)*size.x*.5,uv.y)/size;
     float2 edge=min(frac(brickUV),1-frac(brickUV))*size;
     float joint=1-smoothstep(_BrickSize.z*.55,_BrickSize.z,max(.0001,min(edge.x,edge.y)));
     float variation=.82+.25*hash(floor(brickUV));float3 brick=_BaseTint.rgb*variation*(.88+.16*grain);
     float plaster=smoothstep(1-_PlasterCoverage-.08,1-_PlasterCoverage+.08,noise(uv*.85)+noise(uv*5)*.12);
     c=lerp(lerp(brick,_MortarTint.rgb,joint),float3(.27,.285,.275)*(.88+.18*grain),plaster*.35);smooth=.11;
   }
   else if(_Kind>1.5&&_Kind<2.5)
   {float peeled=smoothstep(.71,.79,patch);c=lerp(c,_MortarTint.rgb,peeled*.7);smooth=.1;}
   else if(_Kind>2.5&&_Kind<3.5)
   {float oxidised=smoothstep(.83,1.08,patch);c=lerp(c,float3(.24,.13,.075),oxidised*.55);smooth=lerp(smooth,.07,oxidised);}
   else if(_Kind>3.5)
   {float fibre=sin(uv.y*290+noise(float2(uv.x*.9,uv.y*7))*8)*.5+.5;c*=.82+.18*fibre;smooth=.08;}
   o.Albedo=c;o.Metallic=_Metallic;o.Smoothness=smooth;o.Occlusion=.95;o.Alpha=1;
 }
 ENDCG
 } Fallback "Standard"
}
