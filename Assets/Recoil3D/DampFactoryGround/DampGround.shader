Shader "Recoil/3D/Damp Industrial Ground"
{
 Properties {
  _Color("Tint and state alpha",Color)=(1,1,1,1)
  _GroundTint("Ground finish independent of legacy sprite tint",Color)=(1,1,1,1)
  _MainTex("Tileable base colour",2D)="white"{}
  _Kind("0 concrete, 1 asphalt, 2 silt gravel",Float)=0
  _Metres("Texture repeat metres",Float)=4
  _Wetness("Localized dampness",Range(0,1))=.55
  _JointSize("Slab dimensions metres",Vector)=(4.2,3.8,0,0)
  _LoadingWear("Localized loading apron wear",Range(0,1))=0
  _WearArea("Apron centre X Z and half extents",Vector)=(128.32,-.05,3.85,3.1)
  _ExposedConcreteTint("Abraded exposed concrete",Color)=(.34,.315,.255,1)
  _ApronGrimeTint("Loading grime and rubber soil",Color)=(.075,.067,.045,1)
 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 250
 CGPROGRAM
 #pragma surface surf RouteMoonStandard fullforwardshadows addshadow exclude_path:deferred
 #pragma target 3.0
 #include "../OperationPlaneMoonlight.cginc"
 sampler2D _MainTex;fixed4 _Color,_GroundTint;float _Kind,_Metres,_Wetness;float4 _JointSize;float _LoadingWear;float4 _WearArea;fixed4 _ExposedConcreteTint,_ApronGrimeTint;
 struct Input {float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 float height(float2 p){return noise(p*47)*.0014+noise(p*12)*.0018;}
 void surf(Input IN,inout SurfaceOutputStandard o)
 {
  clip(_Color.a-.001);float2 p=IN.worldPos.xz;
  float2 uv=p/max(.1,_Metres);
  float blend=smoothstep(.26,.74,noise(p*.09));
  float3 a=tex2D(_MainTex,uv).rgb,b=tex2D(_MainTex,uv*float2(-1,1)+float2(.371,.617)).rgb;
  float3 color=lerp(a,b,blend)*_GroundTint.rgb;
  float macro=noise(p*.17)*.65+noise(p*.57)*.35;
  float fine=noise(p*45);float joint=0;
  if(_Kind<.5)
  {
   float2 shifted=p+float2(0,floor(p.x/_JointSize.x)*.13);
   float2 cell=shifted/_JointSize.xy;
   float2 d=min(frac(cell),1-frac(cell))*_JointSize.xy;
   joint=1-smoothstep(.008,.026,min(d.x,d.y));
   color*=.85+.26*hash(floor(cell));
   color=lerp(color,color*.24,joint*(.42+.30*noise(p*3.1)));
   // Fine fissures wander in metre coordinates, independent of mesh UVs.
   float wave=abs(sin(p.x*1.13+noise(float2(p.y*.91,3))*7+p.y*.67));
   float crack=(1-smoothstep(.006,.025,wave))*smoothstep(.48,.72,noise(p*.8));
   color*=1-crack*.52;
  }
  else if(_Kind<1.5)
  {
   color+=.012;color*=.77+.30*macro;
   float2 patchCell=floor((p+float2(7,3))/float2(7.1,3.3));
   float2 edge=min(frac((p+float2(7,3))/float2(7.1,3.3)),1-frac((p+float2(7,3))/float2(7.1,3.3)));
   float patch=step(.73,hash(patchCell))*smoothstep(.01,.05,min(edge.x,edge.y));
   color*=1-patch*.25;
  }
  else{color=lerp(color,float3(.105,.089,.060),.61)*(.77+.30*macro);}
  float wall=1-smoothstep(.1,.85,abs(p.y-2.5));
  float front=smoothstep(10,20,-p.y)*(_Kind<.5?.90:.14);
  float silt=saturate(smoothstep(.48,.72,macro)*(front+wall*.45));
  color=lerp(color,float3(.115,.099,.070)*(.72+.28*fine),silt*.85);
  float damp=smoothstep(.47,.76,macro)*_Wetness;
  float pool=smoothstep(.77,.88,noise(p*.63)+noise(p*2.1)*.10)*_Wetness*.75;
  color*=1-damp*.42-pool*.18;
  float serviceMask=0;
  if(_LoadingWear>.001&&_Kind<.5)
  {
   float2 q=p-_WearArea.xy;
   float2 warp=float2(noise(p*.64),noise(p*.67+float2(31,17)))-.5;
   float2 norm=abs(q+warp*.95)/max(_WearArea.zw,float2(.1,.1));
   float irregular=noise(p*.75)*.65+noise(p*3.1)*.35;
   float shape=dot(norm,norm)+(irregular-.5)*.60;
   serviceMask=(1-smoothstep(.24,1.05,shape))*_LoadingWear;
   // Disconnected worn islands and untouched gaps replace the uniform apron colour field.
   float doorDistance=max(0,2.88-p.y);
   float proximity=1-smoothstep(.15,4.5,doorDistance);
   float contact=1-smoothstep(.05,1.8,doorDistance);
   float colonies=noise(p*1.15)*.72+noise(p*4.2)*.28;
   float breakup=smoothstep(lerp(.47,.30,proximity),lerp(.76,.61,proximity),colonies);
   serviceMask=saturate(serviceMask*breakup*lerp(.12,1.20,proximity));
   serviceMask*=1-smoothstep(2.82,3.02,p.y);
   float centre=max(contact*.85,smoothstep(.23,.76,noise(p*.93+float2(17,9))));
   float3 textureDetail=lerp(a,b,blend);
   float3 exposed=_ExposedConcreteTint.rgb*(.46+textureDetail*1.6)*(.84+.28*noise(p*2.1));
   exposed*=1-joint*.62;
   // The bright scoured centre is surrounded by darker ingrained loading grime.
   float3 loaded=lerp(_ApronGrimeTint.rgb*(.75+.35*fine),exposed,.43+.57*centre);
   float wheelLine=exp(-pow((abs(q.x)-.91)/.24,2));
   loaded*=1-wheelLine*.19*(.45+.55*noise(float2(q.x*23,q.y*2)));
   float dragArea=(1-smoothstep(1.35,2.0,abs(q.x-.28)))*(1-smoothstep(1.5,2.5,abs(q.y-.2)));
   float scratchX=q.x+q.y*.17+noise(p*.8)*.04;
   float score=max(exp(-pow((scratchX-.28)/.018,2)),max(exp(-pow((scratchX+.36)/.022,2)),exp(-pow((scratchX-1.11)/.014,2))));
   score*=dragArea*smoothstep(.24,.62,noise(float2(q.x*4,q.y*6)));
   loaded=lerp(loaded,exposed*1.23,score*.45);
   float rustWash=exp(-pow((q.x-2.25)/.43,2))*smoothstep(1.3,2.7,p.y)*(.45+.55*irregular);
   loaded=lerp(loaded,float3(.24,.135,.061),rustWash*.44);
   color=lerp(color,loaded,serviceMask*.76);
   // Strong local scouring confined to the first 1.75 metres in front of the threshold.
   float threshold=(1-smoothstep(.60,1.75,doorDistance))*(1-smoothstep(2.20,2.72,abs(p.x-128.32)));
   threshold*=1-smoothstep(2.84,2.98,p.y);
   float chips=smoothstep(.32,.65,noise(p*2.8)*.65+noise(p*8.4)*.35);
   float3 stripped=float3(.38,.37,.315)*(.52+textureDetail*1.6)*(.9+.16*noise(p*4.2));
   stripped*=1-joint*.66;
   color=lerp(color,stripped,threshold*(.30+.70*chips)*.88);
   damp*=1-serviceMask*.32;pool*=1-serviceMask*.38;
  }
  o.Albedo=color;o.Metallic=.025;
  o.Smoothness=lerp(.10,.40,damp);o.Smoothness=lerp(o.Smoothness,.85,pool);o.Smoothness=lerp(o.Smoothness,.20+fine*.06,serviceMask*.7);
  float e=.013;float h=height(p);float2 slope=float2(height(p+float2(e,0))-h,height(p+float2(0,e))-h)/e;
  o.Normal=normalize(float3(-slope.x*(1-pool),-slope.y*(1-pool),1));
  o.Occlusion=.88+.12*fine;o.Alpha=1;
 }
 ENDCG
 } Fallback "Standard"
}
