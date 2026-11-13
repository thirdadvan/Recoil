Shader "Recoil/3D/Occupied Corridor Finish" {
 Properties { _Color("Base",Color)=(.45,.45,.4,1) _BaseY("Floor elevation",Float)=0 _Kind("0 plaster 1 paint 2 concrete",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque"} LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 fixed4 _Color; float _BaseY,_Kind;
 struct Input {float3 worldPos; float3 worldNormal;};
 float h(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float n(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(h(i),h(i+float2(1,0)),f.x),lerp(h(i+float2(0,1)),h(i+1),f.x),f.y);}
 void surf(Input I,inout SurfaceOutputStandard o){float3 p=I.worldPos;float2 uv=abs(I.worldNormal.y)>.7?p.xz:p.xy;float broad=n(uv*1.7),fine=n(uv*94),ch=n(uv*19);float y=p.y-_BaseY;float3 c=_Color.rgb*(.89+broad*.15+(fine-.5)*.14);float damp=(1-smoothstep(.02,.55+broad*.1,y));
 if(_Kind<1.5){float edge=smoothstep(.56,.7,n(uv*4)+n(uv*27)*.18);c=lerp(c,float3(.35,.33,.28),edge*(_Kind>.5?.62:.28));float runs=smoothstep(.66,.82,n(float2(p.x*8,p.y*.22)))*(1-smoothstep(.5,2.8,y));c*=1-runs*.19; c=lerp(c,c*float3(.57,.61,.48),damp*.65);}
 else {float2 g=abs(frac(p.xz/1.2)-.5);float seam=1-smoothstep(.0,.009,min(g.x,g.y));c*=1-seam*.19;float scraped=smoothstep(.62,.8,n(p.xz*float2(18,.65)))*smoothstep(.4,.65,broad);c=lerp(c,c*1.22,scraped*.32);}
 o.Albedo=c;o.Metallic=0;o.Smoothness=_Kind>1.5?.13:.08;o.Occlusion=.94;
 }
 ENDCG
 } Fallback "Standard"
}
