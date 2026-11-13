Shader "Recoil/3D/Aged Bath Tiles"
{
 Properties{_Color("Glazed tile",Color)=(.4,.43,.39,1) _TileSize("Tile metres",Float)=.3}
 SubShader{Tags{"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows addshadow
 #pragma target 3.0
 fixed4 _Color;float _TileSize;
 struct Input{float3 worldPos;float3 worldNormal;};
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 void surf(Input IN,inout SurfaceOutputStandard o){
 float3 n=abs(IN.worldNormal);float2 uv=n.y>.7?IN.worldPos.xz:n.x>.7?IN.worldPos.zy:IN.worldPos.xy;
 float2 tile=uv/max(.05,_TileSize),cell=floor(tile),edge=min(frac(tile),1-frac(tile));
 float grout=1-smoothstep(.008,.02,min(edge.x,edge.y));float wear=hash(cell);
 float grime=.1*(sin(uv.x*3+sin(uv.y*5))*.5+.5)+.12*saturate(1-frac(IN.worldPos.y*.23));
 float3 enamel=_Color.rgb*(.88+wear*.18-grime);o.Albedo=lerp(enamel,float3(.13,.15,.14),grout*.9);
 o.Metallic=0;o.Smoothness=lerp(.27,.05,grout);o.Alpha=1;
 }
 ENDCG
 }Fallback "Standard"
}
