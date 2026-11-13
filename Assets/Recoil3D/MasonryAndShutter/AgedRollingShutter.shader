Shader "Recoil/3D/Aged Rolling Shutter"
{
 Properties { _PaintTint("Aged grey green paint",Color)=(.31,.35,.32,1) _Pitch("Slat pitch metres",Float)=.07613 _Bottom("Curtain bottom",Float)=.12 _ServiceWear("Distinct loading wear",Range(0,1))=0 _DoorCentreX("Door centre X",Float)=128.32 _BleachedTint("Bleached old zinc paint",Color)=(.52,.52,.44,1) _LowerTint("Rubbed lower curtain",Color)=(.43,.415,.34,1) }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf RouteMoonStandard fullforwardshadows addshadow exclude_path:deferred
 #pragma target 3.0
 #include "../OperationPlaneMoonlight.cginc"
 fixed4 _PaintTint,_BleachedTint,_LowerTint;float _Pitch,_Bottom,_ServiceWear,_DoorCentreX;
 struct Input {float3 worldPos;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
 void surf(Input IN,inout SurfaceOutputStandard o)
 {
  float2 p=IN.worldPos.xy;float grain=noise(p*85);float patch=noise(p*3.1);float streak=noise(float2(p.x*17,p.y*.7));
  float phase=frac((p.y-_Bottom)/max(.02,_Pitch));float seam=1-smoothstep(.014,.075,min(phase,1-phase));
  float edgeWear=seam*smoothstep(.42,.75,patch+streak*.15);float chips=smoothstep(.86,1.01,noise(p*7.5)+grain*.17);
  float lower=1-smoothstep(.04,.65,p.y);float rust=saturate(edgeWear*.57+chips*.44+lower*.28*streak);
  float3 paint=_PaintTint.rgb*(.83+.13*grain+.08*streak);float3 oxide=float3(.20,.098,.041)*(.74+.28*grain);
  if(_ServiceWear>.001)
  {
   float x=p.x-_DoorCentreX;float broad=noise(float2(x*.68,p.y*.42));
   float fade=saturate(.18+broad*.72+smoothstep(1.6,4.8,p.y)*.15);
   paint=lerp(paint,_BleachedTint.rgb*(.82+.15*grain),fade*_ServiceWear);
   float lowerBand=1-smoothstep(.65,1.65,p.y+(broad-.5)*.55);
   paint=lerp(paint,_LowerTint.rgb*(.76+.24*grain),lowerBand*.78*_ServiceWear);
   float rubbing=(1-smoothstep(.92,1.6,p.y))*(1-smoothstep(1.3,2.25,abs(x)));
   rubbing*=smoothstep(.46,.76,noise(float2(x*2.5,p.y*32)));
   paint=lerp(paint,float3(.38,.38,.32),rubbing*.42*_ServiceWear);
   float guideRust=exp(-pow((abs(x)-2.39)/.14,2))*(.45+.55*streak);
   rust=saturate(rust+_ServiceWear*(lowerBand*seam*.3+guideRust*.48+lower*.25*patch));
   paint*=1-_ServiceWear*.10*(1-streak)*smoothstep(1,4.6,p.y);
  }
  o.Albedo=lerp(paint,oxide,rust);o.Albedo*=1-seam*.23;o.Metallic=lerp(.44,.06,rust);o.Smoothness=lerp(.23,.07,rust);o.Occlusion=.94;
 }
 ENDCG
 } Fallback "Standard"
}
