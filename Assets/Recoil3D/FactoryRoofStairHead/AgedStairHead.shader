Shader "Recoil/3D/Aged Stair Head"
{
 Properties {
  _MainTex("UV",2D)="white"{}
  _Color("State",Color)=(1,1,1,1)
  _BaseTint("Finish",Color)=(.25,.24,.2,1)
  _MortarTint("Old mortar",Color)=(.18,.18,.15,1)
  _Kind("0 concrete, 1 brick, 2 plaster, 3 painted iron",Float)=0
  _BrickSize("Brick dimensions",Vector)=(.26,.085,.007,0)
  _Metallic("Metal",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.1
 }
 SubShader {
  Tags {"RenderType"="Opaque"} LOD 200
  CGPROGRAM
  #pragma surface surf RouteMoonStandard fullforwardshadows addshadow exclude_path:deferred
  #pragma target 3.0
  #include "../OperationPlaneMoonlight.cginc"
  #include "UnityCG.cginc"
  sampler2D _MainTex;fixed4 _Color,_BaseTint,_MortarTint;float4 _BrickSize;float _Kind,_Metallic,_Glossiness;
  struct Input {float3 worldPos;float3 worldNormal;float2 uv_MainTex;};
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
  void surf(Input IN,inout SurfaceOutputStandard o)
  {
   clip(_Color.a-.001);
   float3 n=abs(IN.worldNormal);float3 p=mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz;
   float2 uv=n.z>n.x&&n.z>n.y?p.xy:n.x>n.y?p.zy:p.xz;
   float coarse=noise(uv*1.3),fine=noise(uv*61),chips=noise(uv*18);
   float streak=noise(float2(uv.x*7,uv.y*.45))*.65+noise(float2(uv.x*25,uv.y*1.2))*.35;
   float3 c=_BaseTint.rgb*(.68+.3*fine+.17*coarse);
   float metal=_Metallic,smooth=_Glossiness;
   if(_Kind>.5&&_Kind<1.5)
   {
    float2 size=_BrickSize.xy;float row=floor(uv.y/size.y);float2 b=float2(uv.x+fmod(abs(row),2)*size.x*.5,uv.y)/size;
    float2 edge=min(frac(b),1-frac(b))*size;float joint=1-smoothstep(_BrickSize.z*.45,_BrickSize.z,min(edge.x,edge.y));
    c*=.75+.4*hash(floor(b));c=lerp(c,_MortarTint.rgb*(.7+.3*fine),joint);
    float salts=smoothstep(.60,.80,noise(uv*3.6)+fine*.12);c=lerp(c,float3(.36,.35,.29),salts*.37);
    c*=.72+.28*streak;smooth=.055;
   }
   else if(_Kind>1.5&&_Kind<2.5)
   {
    float border=length((IN.uv_MainTex-.5)*2);clip(1-border-(noise(uv*32)-.4)*.20);
    clip(.84-noise(uv*27));
    c*=.7+.3*streak;c=lerp(c,float3(.18,.19,.14),smoothstep(.58,.83,coarse)*.4);smooth=.035;
   }
   else if(_Kind>2.5&&_Kind<3.5)
   {
    float oxidised=smoothstep(.61,.76,noise(uv*5.4)+chips*.22);float3 oxide=lerp(float3(.08,.05,.022),float3(.21,.095,.035),fine);
    c=lerp(c,oxide,oxidised*.86);metal*=1-oxidised*.8;smooth*=1-oxidised*.85;
   }
   else{c*=.7+.3*streak;smooth=.045;}
   // Soot under the coping and a broken moisture line where masonry meets roof.
   float damp=(1-smoothstep(.14,.85,p.y+coarse*.2))*(.45+.55*coarse);
   c=lerp(c,c*float3(.40,.49,.31),damp*.65);
   float soot=smoothstep(2.5,3.03,p.y)*(1-smoothstep(3.04,3.20,p.y));c*=1-soot*(.12+.25*(1-streak));
   o.Albedo=c;o.Metallic=metal;o.Smoothness=smooth;o.Occlusion=.86+.12*fine;o.Alpha=1;
  }
  ENDCG
 } Fallback "Standard"
}
