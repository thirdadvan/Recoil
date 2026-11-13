Shader "Recoil/3D/Low Cost Local Rain"
{
 Properties { _Tint("Rain tint",Color)=(.65,.72,.75,1) _Splash("Ground impact ring",Float)=0 _GroundY("Ground clipping height",Float)=.025 }
 SubShader {Tags {"Queue"="Transparent+12" "RenderType"="Transparent" "IgnoreProjector"="True"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.0
 #include "UnityCG.cginc"
 fixed4 _Tint;float _Splash,_GroundY;int _ShelterCount;float4 _ShelterMin[16],_ShelterMax[16];
 struct appdata {float4 vertex:POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;};
 struct v2f {float4 position:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;o.color=v.color*_Tint;return o;}
 fixed4 frag(v2f i):SV_Target
 {
  clip(i.world.y-_GroundY);
  [loop]for(int k=0;k<_ShelterCount;k++){
   float inside=step(_ShelterMin[k].x,i.world.x)*step(i.world.x,_ShelterMax[k].x)*step(_ShelterMin[k].z,i.world.z)*step(i.world.z,_ShelterMax[k].z)*step(i.world.y,_ShelterMax[k].y-.02);
   clip(.5-inside);
  }
  float alpha;
  if(_Splash>.5){float r=length((i.uv-.5)*2);alpha=(1-smoothstep(.055,.17,abs(r-.68)))*smoothstep(.03,.16,r);}
  else{float across=1-smoothstep(.025,.36,abs(i.uv.x-.5));float ends=smoothstep(0,.16,i.uv.y)*(1-smoothstep(.66,1,i.uv.y));alpha=across*ends;}
  alpha*=i.color.a*(1-smoothstep(25,42,distance(_WorldSpaceCameraPos,i.world)));
  return fixed4(i.color.rgb,alpha);
 }
 ENDCG
 }
 } Fallback Off
}
